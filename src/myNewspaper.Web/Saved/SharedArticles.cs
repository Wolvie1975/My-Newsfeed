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
        return article with { Url = NormalizeUrl(article.Url), Title = Text(article.Title, 1000),
            Source = Text(article.Source, 500), Summary = Text(article.Summary, 4000),
            Image = string.IsNullOrEmpty(article.Image) ? null : NormalizeUrl(article.Image),
            Date = Text(article.Date, 100), DateLabel = Text(article.DateLabel, 200),
            SavedAt = article.SavedAt ?? DateTimeOffset.UtcNow };
    }
    public static string NormalizeUrl(string? value)
    {
        if (value is null || value.Length > 2048 || !Uri.TryCreate(value, UriKind.Absolute, out var uri)
            || uri.Scheme is not ("http" or "https") || !string.IsNullOrEmpty(uri.UserInfo))
            throw new ArgumentException("A valid HTTP or HTTPS article URL is required.");
        return uri.AbsoluteUri;
    }

    public async Task<IReadOnlyList<SavedArticle>> Read(CancellationToken ct = default)
    {
        await using var db = await factory.CreateDbContextAsync(ct);
        await db.Database.OpenConnectionAsync(ct);
        await using var command = db.Database.GetDbConnection().CreateCommand();
        command.CommandText = "SELECT Articles FROM dbo.MyDailyFeedSavedList WHERE Id = 1";
        return JsonSerializer.Deserialize<SavedArticle[]>((string)(await command.ExecuteScalarAsync(ct))!, Json)!;
    }

    public Task<IReadOnlyList<SavedArticle>> Add(SavedArticle article, CancellationToken ct = default) =>
        Change([Normalize(article) with { SavedAt = DateTimeOffset.UtcNow }], null, null, ct);
    public Task<IReadOnlyList<SavedArticle>> Remove(string url, CancellationToken ct = default) =>
        Change([], NormalizeUrl(url), null, ct);
    public Task<IReadOnlyList<SavedArticle>> Import(SavedImport import, CancellationToken ct = default)
    {
        if (import.ClientId == Guid.Empty || import.Articles is null || import.Articles.Length > Maximum)
            throw new ArgumentException("Invalid bookmark import.");
        return Change(import.Articles.Select(Normalize).ToArray(), null, import.ClientId, ct);
    }

    private async Task<IReadOnlyList<SavedArticle>> Change(SavedArticle[] additions, string? remove, Guid? clientId, CancellationToken ct)
    {
        await using var db = await factory.CreateDbContextAsync(ct);
        await db.Database.OpenConnectionAsync(ct);
        var connection = db.Database.GetDbConnection();
        await using var transaction = await connection.BeginTransactionAsync(IsolationLevel.Serializable, ct);
        await using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = "SELECT Articles FROM dbo.MyDailyFeedSavedList WITH (UPDLOCK, HOLDLOCK) WHERE Id = 1";
        var list = JsonSerializer.Deserialize<List<SavedArticle>>((string)(await command.ExecuteScalarAsync(ct))!, Json)!;
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
        command.CommandText = "UPDATE dbo.MyDailyFeedSavedList SET Articles = @articles WHERE Id = 1";
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
