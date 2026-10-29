using System.Data;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using myNewspaper.Web.Data;

namespace myNewspaper.Web.Saved;

public sealed record SavedArticle(string Url, string Title, string Source = "", string? Image = null,
    string Summary = "", string? Date = null, string? DateLabel = null, DateTimeOffset? SavedAt = null, bool Read = false, string Kind = "article");
public sealed record SavedImport(Guid ClientId, SavedArticle[] Articles);
public sealed record SavedRemoval(string Url);

public sealed class SavedListFullException : Exception;

// Separate from scraper-owned rows: copies survive feed retention and deployments.
public sealed class SharedArticles(IDbContextFactory<WebScraperContext> factory)
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);
    public const int Maximum = 500;
    public static SavedArticle Normalize(SavedArticle article)
    {
        ArgumentNullException.ThrowIfNull(article);
        static string Text(string? value, int maximum) => (value ?? "").Length <= maximum
            ? value ?? "" : throw new ArgumentException("Article text is too long.");
        if (article.Kind is not ("article" or "video")) throw new ArgumentException("Invalid saved content type.");
        return article with
        {
            Url = NormalizeUrl(article.Url),
            Title = Text(article.Title, 1000),
            Source = Text(article.Source, 500),
            Summary = Text(article.Summary, 4000),
            Image = string.IsNullOrEmpty(article.Image) ? null : NormalizeUrl(article.Image),
            Date = Text(article.Date, 100),
            DateLabel = Text(article.DateLabel, 200),
            SavedAt = article.SavedAt ?? DateTimeOffset.UtcNow
        };
    }
    public static string NormalizeUrl(string? value)
    {
        if (value is null || value.Length > 2048 || !Uri.TryCreate(value, UriKind.Absolute, out var uri)
            || uri.Scheme is not ("http" or "https") || !string.IsNullOrEmpty(uri.UserInfo))
            throw new ArgumentException("A valid HTTP or HTTPS article URL is required.");
        return uri.AbsoluteUri;
    }

    // profileId is the owner: null is the legacy shared list (MyDailyFeedSavedList, Id = 1), used only while profiles
    // are switched off; otherwise the profile's own row in MyDailyFeedProfileSaved, created on its first save.
    // SharedArticles is a singleton, so callers pass the current profile instead of it being injected.
    private static string Select(int? profileId, bool locked) => profileId is null
        ? $"SELECT Articles FROM dbo.MyDailyFeedSavedList{(locked ? " WITH (UPDLOCK, HOLDLOCK)" : "")} WHERE Id = 1"
        : $"SELECT Articles FROM dbo.MyDailyFeedProfileSaved{(locked ? " WITH (UPDLOCK, HOLDLOCK)" : "")} WHERE ProfileId = @profile";

    private static void AddProfile(System.Data.Common.DbCommand command, int? profileId)
    {
        if (profileId is null) return;
        var p = command.CreateParameter(); p.ParameterName = "@profile"; p.Value = profileId.Value; command.Parameters.Add(p);
    }

    public async Task<IReadOnlyList<SavedArticle>> Read(int? profileId, CancellationToken ct = default)
    {
        await using var db = await factory.CreateDbContextAsync(ct);
        await db.Database.OpenConnectionAsync(ct);
        await using var command = db.Database.GetDbConnection().CreateCommand();
        command.CommandText = Select(profileId, locked: false);
        AddProfile(command, profileId);
        return await command.ExecuteScalarAsync(ct) is string json ? JsonSerializer.Deserialize<SavedArticle[]>(json, Json)! : [];
    }

    public Task<IReadOnlyList<SavedArticle>> Add(int? profileId, SavedArticle article, CancellationToken ct = default) =>
        Change(profileId, [Normalize(article) with { SavedAt = DateTimeOffset.UtcNow }], null, null, ct);
    public Task<IReadOnlyList<SavedArticle>> Remove(int? profileId, string url, CancellationToken ct = default) =>
        Change(profileId, [], NormalizeUrl(url), null, ct);
    public Task<IReadOnlyList<SavedArticle>> Import(int? profileId, SavedImport import, CancellationToken ct = default)
    {
        if (import.ClientId == Guid.Empty || import.Articles is null || import.Articles.Length > Maximum)
            throw new ArgumentException("Invalid bookmark import.");
        return Change(profileId, import.Articles.Select(Normalize).ToArray(), null, import.ClientId, ct);
    }

    private async Task<IReadOnlyList<SavedArticle>> Change(int? profileId, SavedArticle[] additions, string? remove, Guid? clientId, CancellationToken ct)
    {
        await using var db = await factory.CreateDbContextAsync(ct);
        await db.Database.OpenConnectionAsync(ct);
        var connection = db.Database.GetDbConnection();
        await using var transaction = await connection.BeginTransactionAsync(IsolationLevel.Serializable, ct);
        await using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = Select(profileId, locked: true);
        AddProfile(command, profileId);
        // HOLDLOCK also locks the missing row's key range, so two first saves can't both insert.
        var stored = await command.ExecuteScalarAsync(ct) as string;
        var list = stored is null ? new List<SavedArticle>() : JsonSerializer.Deserialize<List<SavedArticle>>(stored, Json)!;
        if (clientId.HasValue)
        {
            command.CommandText = "SELECT COUNT(*) FROM dbo.MyDailyFeedSavedImports WHERE ClientId = @client";
            var p = command.CreateParameter(); p.ParameterName = "@client"; p.Value = clientId.Value; command.Parameters.Add(p);
            if ((int)(await command.ExecuteScalarAsync(ct))! > 0) { await transaction.CommitAsync(ct); return list; }
        }
        list.RemoveAll(a => a.Url == remove);
        var seen = list.Select(a => a.Url).ToHashSet(StringComparer.Ordinal);
        foreach (var article in additions) if (seen.Add(article.Url)) list.Add(article);
        if (list.Count > Maximum) throw new SavedListFullException();
        list = list.OrderByDescending(a => a.SavedAt).ThenBy(a => a.Url, StringComparer.Ordinal).ToList();
        command.CommandText = profileId is null ? "UPDATE dbo.MyDailyFeedSavedList SET Articles = @articles WHERE Id = 1"
            : stored is null ? "INSERT dbo.MyDailyFeedProfileSaved (ProfileId, Articles) VALUES (@profile, @articles)"
            : "UPDATE dbo.MyDailyFeedProfileSaved SET Articles = @articles WHERE ProfileId = @profile";
        var data = command.CreateParameter(); data.ParameterName = "@articles"; data.DbType = DbType.String;
        data.Value = JsonSerializer.Serialize(list, Json); command.Parameters.Add(data);
        await command.ExecuteNonQueryAsync(ct);
        if (clientId.HasValue)
        {
            command.CommandText = "INSERT dbo.MyDailyFeedSavedImports(ClientId) VALUES (@client)";
            await command.ExecuteNonQueryAsync(ct);
        }
        await transaction.CommitAsync(ct);
        return list;
    }
}
