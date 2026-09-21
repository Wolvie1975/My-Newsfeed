using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace MyNewsFeed.Web.Data;

public static class AdminData
{
    private const int UniqueIndexViolation = 2601;
    private const int UniqueConstraintViolation = 2627;
    private const int ForeignKeyViolation = 547;

    /// <summary>True when the save failed on a unique index (for example a source URL or category name that already exists).</summary>
    public static bool IsUniqueViolation(DbUpdateException ex) =>
        ex.InnerException is SqlException { Number: UniqueIndexViolation or UniqueConstraintViolation };

    public static bool IsForeignKeyViolation(Exception ex) =>
        ex is SqlException { Number: ForeignKeyViolation } || ex.InnerException is SqlException { Number: ForeignKeyViolation };

    /// <summary>
    /// Deletes a source. Pages reference sources with ON DELETE NO ACTION, so unless <paramref name="includePages"/>
    /// is set, the database rejects the delete while any page still points at the source.
    /// Returns false when the source no longer exists.
    /// </summary>
    public static async Task<bool> DeleteSourceAsync(WebScraperContext db, int id, bool includePages)
    {
        var ownsTransaction = db.Database.CurrentTransaction is null;
        await using var tx = ownsTransaction ? await db.Database.BeginTransactionAsync() : null;

        if (includePages)
        {
            await db.Pages.Where(p => p.SourceId == id).ExecuteDeleteAsync();
        }

        var deleted = await db.Sources.Where(s => s.Id == id).ExecuteDeleteAsync();

        if (tx is not null)
        {
            await tx.CommitAsync();
        }

        return deleted > 0;
    }

    // ---- YouTube feeds ------------------------------------------------------------------------------------------------

    private const string YoutubeFeedUrlPrefix = "https://www.youtube.com/feeds/videos.xml?channel_id=";

    /// <summary>The standard Atom feed address for a channel: what the scraper's --youtube-feed option takes.</summary>
    public static string YoutubeFeedUrl(string channelId) => YoutubeFeedUrlPrefix + Uri.EscapeDataString(channelId.Trim());

    /// <summary>
    /// Deletes a YouTube feed. Videos reference feeds with ON DELETE NO ACTION, so unless <paramref name="includeVideos"/>
    /// is set, the database rejects the delete while any video still points at the feed.
    /// Returns false when the feed no longer exists.
    /// </summary>
    public static async Task<bool> DeleteYoutubeFeedAsync(WebScraperContext db, int id, bool includeVideos)
    {
        var ownsTransaction = db.Database.CurrentTransaction is null;
        await using var tx = ownsTransaction ? await db.Database.BeginTransactionAsync() : null;

        if (includeVideos)
        {
            await db.YouTubeVideos.Where(v => v.YoutubeVideoFeedId == id).ExecuteDeleteAsync();
        }

        var deleted = await db.YoutubeVideoFeeds.Where(f => f.Id == id).ExecuteDeleteAsync();

        if (tx is not null)
        {
            await tx.CommitAsync();
        }

        return deleted > 0;
    }

    public sealed record YoutubeSyncResult(int FeedsCreated, int VideosLinked);

    // Constants only (no user input), so plain SQL text is safe here.
    private const string CreateMissingFeedsSql =
        "INSERT INTO dbo.YoutubeVideoFeed (ChannelId, ChannelName, Url) " +
        "SELECT v.ChannelId, MAX(v.ChannelName), N'" + YoutubeFeedUrlPrefix + "' + v.ChannelId " +
        "FROM dbo.YouTubeVideos v " +
        "WHERE NOT EXISTS (SELECT 1 FROM dbo.YoutubeVideoFeed f WHERE f.ChannelId = v.ChannelId) " +
        "GROUP BY v.ChannelId";

    private const string LinkVideosSql =
        "UPDATE v SET v.YoutubeVideoFeedId = f.ID " +
        "FROM dbo.YouTubeVideos v JOIN dbo.YoutubeVideoFeed f ON f.ChannelId = v.ChannelId " +
        "WHERE v.YoutubeVideoFeedId IS NULL";

    /// <summary>
    /// Brings the feed list in line with the videos: creates a feed for every channel that has videos but no feed,
    /// then links every unlinked video to its channel's feed. Additive and safe to repeat. The scraper inserts videos
    /// without a feed link until it is taught to set one, so this catches up whatever it has added.
    /// </summary>
    public static async Task<YoutubeSyncResult> SyncYoutubeFeedsAsync(WebScraperContext db)
    {
        var ownsTransaction = db.Database.CurrentTransaction is null;
        await using var tx = ownsTransaction ? await db.Database.BeginTransactionAsync() : null;

        var created = await db.Database.ExecuteSqlRawAsync(CreateMissingFeedsSql);
        var linked = await db.Database.ExecuteSqlRawAsync(LinkVideosSql);

        if (tx is not null)
        {
            await tx.CommitAsync();
        }

        return new YoutubeSyncResult(created, linked);
    }

    // ---- Sports event types -------------------------------------------------------------------------------------------

    /// <summary>The website part of an address, lower-cased and without a leading "www.", or null when it isn't a URL.</summary>
    public static string? HostOf(string? url)
    {
        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri) || string.IsNullOrEmpty(uri.Host))
        {
            return null;
        }

        var host = uri.Host.ToLowerInvariant();
        return host.StartsWith("www.", StringComparison.Ordinal) ? host[4..] : host;
    }

    /// <summary>
    /// Decides which unlinked events can be attached to a type by website. An event is matched only when exactly one
    /// type's feed address is on the same site, so a site with several feeds (for example one per sport) is never guessed.
    /// Returns event ID to type ID.
    /// </summary>
    public static Dictionary<int, int> PlanSportsEventLinks(
        IEnumerable<(int Id, string Url)> unlinkedEvents, IEnumerable<(int Id, string RssUrl)> types)
    {
        var typeByHost = types
            .Select(t => (t.Id, Host: HostOf(t.RssUrl)))
            .Where(t => t.Host is not null)
            .GroupBy(t => t.Host!)
            .Where(g => g.Count() == 1)
            .ToDictionary(g => g.Key, g => g.Single().Id);

        var plan = new Dictionary<int, int>();
        foreach (var (id, url) in unlinkedEvents)
        {
            if (HostOf(url) is { } host && typeByHost.TryGetValue(host, out var typeId))
            {
                plan[id] = typeId;
            }
        }

        return plan;
    }

    /// <summary>The links <see cref="LinkSportsEventsBySiteAsync"/> would make, without making them.</summary>
    public static async Task<Dictionary<int, int>> PlanSportsEventLinksAsync(WebScraperContext db)
    {
        var types = await db.SportsEventsTypes.AsNoTracking().Select(t => new { t.Id, t.RssUrl }).ToListAsync();
        var unlinked = await db.SportsEvents.AsNoTracking()
            .Where(e => e.SportsEventsTypeId == null)
            .Select(e => new { e.Id, e.Url })
            .ToListAsync();

        return PlanSportsEventLinks(
            unlinked.Select(e => (e.Id, e.Url)), types.Select(t => (t.Id, t.RssUrl)));
    }

    /// <summary>
    /// Links every unlinked event whose website matches exactly one type. Additive and safe to repeat. The scraper adds
    /// events without a type until it is taught to set one, so this catches up whatever it has added.
    /// Returns how many events were linked.
    /// </summary>
    public static async Task<int> LinkSportsEventsBySiteAsync(WebScraperContext db)
    {
        var plan = await PlanSportsEventLinksAsync(db);
        if (plan.Count == 0)
        {
            return 0;
        }

        var ownsTransaction = db.Database.CurrentTransaction is null;
        await using var tx = ownsTransaction ? await db.Database.BeginTransactionAsync() : null;

        var linked = 0;
        foreach (var byType in plan.GroupBy(p => p.Value))
        {
            var typeId = byType.Key;
            foreach (var chunk in byType.Select(p => p.Key).Chunk(500))
            {
                var ids = chunk;
                linked += await db.SportsEvents
                    .Where(e => ids.Contains(e.Id) && e.SportsEventsTypeId == null)
                    .ExecuteUpdateAsync(s => s.SetProperty(e => e.SportsEventsTypeId, typeId));
            }
        }

        if (tx is not null)
        {
            await tx.CommitAsync();
        }

        return linked;
    }

    /// <summary>
    /// Deletes an events type. Events reference types with ON DELETE NO ACTION, so unless <paramref name="includeEvents"/>
    /// is set, the database rejects the delete while any event still points at the type.
    /// Returns false when the type no longer exists.
    /// </summary>
    public static async Task<bool> DeleteSportsEventsTypeAsync(WebScraperContext db, int id, bool includeEvents)
    {
        var ownsTransaction = db.Database.CurrentTransaction is null;
        await using var tx = ownsTransaction ? await db.Database.BeginTransactionAsync() : null;

        if (includeEvents)
        {
            await db.SportsEvents.Where(e => e.SportsEventsTypeId == id).ExecuteDeleteAsync();
        }

        var deleted = await db.SportsEventsTypes.Where(t => t.Id == id).ExecuteDeleteAsync();

        if (tx is not null)
        {
            await tx.CommitAsync();
        }

        return deleted > 0;
    }

}
