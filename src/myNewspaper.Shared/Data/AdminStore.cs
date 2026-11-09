using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace myNewspaper.Shared.Data;

public static class AdminStore
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
    public static async Task<bool> DeleteSourceAsync(WebScraperContext db, int id, bool includePages, CancellationToken ct = default)
    {
        var ownsTransaction = db.Database.CurrentTransaction is null;
        await using var tx = ownsTransaction ? await db.Database.BeginTransactionAsync(ct) : null;

        if (includePages)
        {
            await db.Pages.Where(p => p.SourceId == id).ExecuteDeleteAsync(ct);
        }

        var deleted = await db.Sources.Where(s => s.Id == id).ExecuteDeleteAsync(ct);

        if (tx is not null)
        {
            await tx.CommitAsync(ct);
        }

        return deleted > 0;
    }

    // ---- Sources, categories and pages -------------------------------------------------------------------------------

    /// <summary>
    /// Adds a source (<paramref name="id"/> null) or updates one. Returns false when the source to update no longer
    /// exists. A duplicate URL or a deleted category surfaces as <see cref="DbUpdateException"/>
    /// (<see cref="IsUniqueViolation"/>, <see cref="IsForeignKeyViolation"/>).
    /// </summary>
    public static async Task<bool> SaveSourceAsync(WebScraperContext db, int? id, string url, string? label, bool enabled, int? categoryId,
        CancellationToken ct = default)
    {
        if (id is null)
        {
            db.Sources.Add(new Models.Source { Url = url, Label = label, Enabled = enabled, SourceCategoryId = categoryId });
        }
        else
        {
            var source = await db.Sources.FindAsync([id.Value], ct);
            if (source is null) return false;
            source.Url = url;
            source.Label = label;
            source.Enabled = enabled;
            source.SourceCategoryId = categoryId;
        }
        await db.SaveChangesAsync(ct);
        return true;
    }

    /// <summary>Sets a source's Enabled flag, or flips it when <paramref name="enabled"/> is null. False when it doesn't exist.</summary>
    public static async Task<bool> SetSourceEnabledAsync(WebScraperContext db, int id, bool? enabled, CancellationToken ct = default)
    {
        var source = await db.Sources.FindAsync([id], ct);
        if (source is null) return false;
        source.Enabled = enabled ?? !source.Enabled;
        await db.SaveChangesAsync(ct);
        return true;
    }

    /// <summary>Adds a category. A duplicate name surfaces as <see cref="DbUpdateException"/> (<see cref="IsUniqueViolation"/>).</summary>
    public static async Task AddCategoryAsync(WebScraperContext db, string name, string hub, CancellationToken ct = default)
    {
        db.SourceCategories.Add(new Models.SourceCategory { CategoryName = name, NewsHub = hub });
        await db.SaveChangesAsync(ct);
    }

    /// <summary>Renames a category and sets its hub. False when it no longer exists.</summary>
    public static async Task<bool> UpdateCategoryAsync(WebScraperContext db, int id, string name, string hub, CancellationToken ct = default)
    {
        var category = await db.SourceCategories.FindAsync([id], ct);
        if (category is null) return false;
        category.CategoryName = name;
        category.NewsHub = hub;
        await db.SaveChangesAsync(ct);
        return true;
    }

    /// <summary>Deletes a category; false when it was already gone. In use by a source: <see cref="IsForeignKeyViolation"/>.</summary>
    public static async Task<bool> DeleteCategoryAsync(WebScraperContext db, int id, CancellationToken ct = default) =>
        await db.SourceCategories.Where(c => c.Id == id).ExecuteDeleteAsync(ct) > 0;

    /// <summary>Updates a scraped page's title, description and published date. False when it no longer exists.</summary>
    public static async Task<bool> UpdatePageAsync(WebScraperContext db, int id, string title, string? description, DateTime? published,
        CancellationToken ct = default)
    {
        var page = await db.Pages.FindAsync([id], ct);
        if (page is null) return false;
        page.Title = title;
        page.Description = description;
        page.Published = published;
        await db.SaveChangesAsync(ct);
        return true;
    }

    public static async Task DeletePageAsync(WebScraperContext db, int id, CancellationToken ct = default) =>
        await db.Pages.Where(p => p.Id == id).ExecuteDeleteAsync(ct);

    // ---- YouTube feeds ------------------------------------------------------------------------------------------------

    private const string YoutubeFeedUrlPrefix = "https://www.youtube.com/feeds/videos.xml?channel_id=";

    /// <summary>The standard Atom feed address for a channel: what the scraper's --youtube-feed option takes.</summary>
    public static string YoutubeFeedUrl(string channelId) => YoutubeFeedUrlPrefix + Uri.EscapeDataString(channelId.Trim());

    /// <summary>
    /// Deletes a YouTube feed. Videos reference feeds with ON DELETE NO ACTION, so unless <paramref name="includeVideos"/>
    /// is set, the database rejects the delete while any video still points at the feed.
    /// Returns false when the feed no longer exists.
    /// </summary>
    public static async Task<bool> DeleteYoutubeFeedAsync(WebScraperContext db, int id, bool includeVideos, CancellationToken ct = default)
    {
        var ownsTransaction = db.Database.CurrentTransaction is null;
        await using var tx = ownsTransaction ? await db.Database.BeginTransactionAsync(ct) : null;

        if (includeVideos)
        {
            await db.YouTubeVideos.Where(v => v.YoutubeVideoFeedId == id).ExecuteDeleteAsync(ct);
        }

        var deleted = await db.YoutubeVideoFeeds.Where(f => f.Id == id).ExecuteDeleteAsync(ct);

        if (tx is not null)
        {
            await tx.CommitAsync(ct);
        }

        return deleted > 0;
    }

    public sealed record YoutubeSyncResult(int FeedsCreated, int VideosLinked);

    /// <summary>
    /// Brings the feed list in line with the videos: creates a feed for every channel that has videos but no feed,
    /// then links every unlinked video to its channel's feed. Additive and safe to repeat. The scraper inserts videos
    /// without a feed link until it is taught to set one, so this catches up whatever it has added.
    /// </summary>
    public static async Task<YoutubeSyncResult> SyncYoutubeFeedsAsync(WebScraperContext db, CancellationToken ct = default)
    {
        var ownsTransaction = db.Database.CurrentTransaction is null;
        await using var tx = ownsTransaction ? await db.Database.BeginTransactionAsync(ct) : null;

        var created = await db.Database.ExecuteSqlAsync($"""
            INSERT INTO dbo.YoutubeVideoFeed (ChannelId, ChannelName, Url)
            SELECT v.ChannelId, MAX(v.ChannelName), {YoutubeFeedUrlPrefix} + v.ChannelId
            FROM dbo.YouTubeVideos v
            WHERE NOT EXISTS (SELECT 1 FROM dbo.YoutubeVideoFeed f WHERE f.ChannelId = v.ChannelId)
            GROUP BY v.ChannelId
            """, ct);
        var linked = await db.Database.ExecuteSqlAsync($"""
            UPDATE v SET v.YoutubeVideoFeedId = f.ID
            FROM dbo.YouTubeVideos v JOIN dbo.YoutubeVideoFeed f ON f.ChannelId = v.ChannelId
            WHERE v.YoutubeVideoFeedId IS NULL
            """, ct);

        if (tx is not null)
        {
            await tx.CommitAsync(ct);
        }

        return new YoutubeSyncResult(created, linked);
    }

    /// <summary>
    /// Adds a feed (<paramref name="id"/> null) or updates one; <paramref name="keepChannelId"/> leaves an existing
    /// feed's channel unchanged (it already has videos). False when the feed to update no longer exists. A duplicate
    /// channel surfaces as <see cref="DbUpdateException"/> (<see cref="IsUniqueViolation"/>).
    /// </summary>
    public static async Task<bool> SaveYoutubeFeedAsync(WebScraperContext db, int? id, string channelId, string? name, string url,
        int? categoryId, bool householdPick, bool keepChannelId, CancellationToken ct = default)
    {
        if (id is null)
        {
            db.YoutubeVideoFeeds.Add(new Models.YoutubeVideoFeed
            {
                ChannelId = channelId,
                ChannelName = name,
                Url = url,
                YoutubeCategoryId = categoryId,
                IsHouseholdPick = householdPick,
            });
        }
        else
        {
            var feed = await db.YoutubeVideoFeeds.FindAsync([id.Value], ct);
            if (feed is null) return false;
            if (!keepChannelId) feed.ChannelId = channelId;
            feed.ChannelName = name;
            feed.Url = url;
            feed.YoutubeCategoryId = categoryId;
            feed.IsHouseholdPick = householdPick;
        }
        await db.SaveChangesAsync(ct);
        return true;
    }

    public static async Task DeleteYoutubeVideoAsync(WebScraperContext db, int id, CancellationToken ct = default) =>
        await db.YouTubeVideos.Where(v => v.Id == id).ExecuteDeleteAsync(ct);

    // ---- Sports event types -------------------------------------------------------------------------------------------

    /// <summary>
    /// Adds a legacy RSS event type (<paramref name="id"/> null) or updates one. False when the type to update no longer
    /// exists. A duplicate feed address surfaces as <see cref="DbUpdateException"/> (<see cref="IsUniqueViolation"/>).
    /// </summary>
    public static async Task<bool> SaveSportsEventsTypeAsync(WebScraperContext db, int? id, string name, string rssUrl, string? school,
        bool enabled, CancellationToken ct = default)
    {
        if (id is null)
        {
            db.SportsEventsTypes.Add(new Models.SportsEventsType { EventsTypeName = name, RssUrl = rssUrl, SchoolName = school, Enabled = enabled });
        }
        else
        {
            var type = await db.SportsEventsTypes.FindAsync([id.Value], ct);
            if (type is null) return false;
            type.EventsTypeName = name;
            type.RssUrl = rssUrl;
            type.SchoolName = school;
            type.Enabled = enabled;
        }
        await db.SaveChangesAsync(ct);
        return true;
    }

    // ---- Results follows ---------------------------------------------------------------------------------------------

    /// <summary>
    /// Follows a league (<paramref name="teamId"/> null) or a team, enabled, and returns the new follow's id. Already
    /// followed: <see cref="DbUpdateException"/> (<see cref="IsUniqueViolation"/>).
    /// </summary>
    public static async Task<int> AddResultFollowAsync(WebScraperContext db, string kind, string sport, string league, string? teamId, string name,
        string? logoUrl, CancellationToken ct = default)
    {
        var follow = new Models.ResultFollow
        {
            Kind = kind,
            Sport = sport,
            League = league,
            TeamId = teamId,
            Name = name,
            LogoUrl = logoUrl,
            Enabled = true,
        };
        db.ResultFollows.Add(follow);
        await db.SaveChangesAsync(ct);
        return follow.Id;
    }

    public static async Task SetResultFollowEnabledAsync(WebScraperContext db, int id, bool enabled, CancellationToken ct = default) =>
        await db.ResultFollows.Where(f => f.Id == id).ExecuteUpdateAsync(s => s.SetProperty(f => f.Enabled, enabled), ct);

    /// <summary>Stops following; false when it was already removed.</summary>
    public static async Task<bool> DeleteResultFollowAsync(WebScraperContext db, int id, CancellationToken ct = default) =>
        await db.ResultFollows.Where(f => f.Id == id).ExecuteDeleteAsync(ct) > 0;


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
    public static async Task<Dictionary<int, int>> PlanSportsEventLinksAsync(WebScraperContext db, CancellationToken ct = default)
    {
        var types = await db.SportsEventsTypes.AsNoTracking().Select(t => new { t.Id, t.RssUrl }).ToListAsync(ct);
        var unlinked = await db.SportsEvents.AsNoTracking()
            .Where(e => e.SportsEventsTypeId == null)
            .Select(e => new { e.Id, e.Url })
            .ToListAsync(ct);

        return PlanSportsEventLinks(
            unlinked.Select(e => (e.Id, e.Url)), types.Select(t => (t.Id, t.RssUrl)));
    }

    /// <summary>
    /// Links every unlinked event whose website matches exactly one type. Additive and safe to repeat. The scraper adds
    /// events without a type until it is taught to set one, so this catches up whatever it has added.
    /// Returns how many events were linked.
    /// </summary>
    public static async Task<int> LinkSportsEventsBySiteAsync(WebScraperContext db, CancellationToken ct = default)
    {
        var plan = await PlanSportsEventLinksAsync(db, ct);
        if (plan.Count == 0)
        {
            return 0;
        }

        var ownsTransaction = db.Database.CurrentTransaction is null;
        await using var tx = ownsTransaction ? await db.Database.BeginTransactionAsync(ct) : null;

        var linked = 0;
        foreach (var byType in plan.GroupBy(p => p.Value))
        {
            var typeId = byType.Key;
            foreach (var chunk in byType.Select(p => p.Key).Chunk(500))
            {
                var ids = chunk;
                linked += await db.SportsEvents
                    .Where(e => ids.Contains(e.Id) && e.SportsEventsTypeId == null)
                    .ExecuteUpdateAsync(s => s.SetProperty(e => e.SportsEventsTypeId, typeId), ct);
            }
        }

        if (tx is not null)
        {
            await tx.CommitAsync(ct);
        }

        return linked;
    }

    /// <summary>
    /// Deletes an events type. Events reference types with ON DELETE NO ACTION, so unless <paramref name="includeEvents"/>
    /// is set, the database rejects the delete while any event still points at the type.
    /// Returns false when the type no longer exists.
    /// </summary>
    public static async Task<bool> DeleteSportsEventsTypeAsync(WebScraperContext db, int id, bool includeEvents, CancellationToken ct = default)
    {
        if (await db.SportsEventSources.AnyAsync(s => s.SportsEventsTypeId == id, ct)
            || await db.SportsEventsTypes.AnyAsync(t => t.Id == id && t.ProviderKey != null, ct))
            throw new InvalidOperationException("This feed has shared event attribution. Disable it instead of deleting it.");
        var ownsTransaction = db.Database.CurrentTransaction is null;
        await using var tx = ownsTransaction ? await db.Database.BeginTransactionAsync(ct) : null;

        if (includeEvents)
        {
            await db.SportsEvents.Where(e => e.SportsEventsTypeId == id).ExecuteDeleteAsync(ct);
        }

        var deleted = await db.SportsEventsTypes.Where(t => t.Id == id).ExecuteDeleteAsync(ct);

        if (tx is not null)
        {
            await tx.CommitAsync(ct);
        }

        return deleted > 0;
    }

    /// <summary>Only legacy events without shared attribution can be deleted manually.</summary>
    public static async Task<bool> DeleteSportsEventAsync(WebScraperContext db, int id, CancellationToken ct = default)
    {
        var deleted = await db.SportsEvents
            .Where(e => e.Id == id && e.ProviderKey == null && !e.EventSources.Any())
            .ExecuteDeleteAsync(ct);
        if (deleted == 0 && await db.SportsEvents.AnyAsync(e => e.Id == id, ct))
            throw new InvalidOperationException("This event is maintained by a provider or shared calendar. Disable its feed instead of deleting it.");
        return deleted > 0;
    }

    /// <summary>Provider and shared-source attribution is owned by the scraper.</summary>
    public static async Task<bool> AssignSportsEventTypeAsync(WebScraperContext db, int id, int? typeId, CancellationToken ct = default)
    {
        var updated = await db.SportsEvents.Where(e => e.Id == id && e.ProviderKey == null
                && !e.EventSources.Any()
                && (typeId == null || db.SportsEventsTypes.Any(t => t.Id == typeId && t.ProviderKey == null)))
            .ExecuteUpdateAsync(s => s.SetProperty(e => e.SportsEventsTypeId, typeId), ct);
        if (updated == 0 && await db.SportsEvents.AnyAsync(e => e.Id == id, ct))
            throw new InvalidOperationException("Only legacy events can be assigned to a legacy type. Provider and shared-calendar attribution is read-only.");
        return updated > 0;
    }

    /// <summary>A feed link must refer to the video's own channel.</summary>
    public static async Task<bool> AssignYoutubeVideoFeedAsync(WebScraperContext db, int id, int? feedId, CancellationToken ct = default)
    {
        var updated = await db.YouTubeVideos.Where(v => v.Id == id
                && (feedId == null || db.YoutubeVideoFeeds.Any(f => f.Id == feedId && f.ChannelId == v.ChannelId)))
            .ExecuteUpdateAsync(s => s.SetProperty(v => v.YoutubeVideoFeedId, feedId), ct);
        if (updated == 0 && await db.YouTubeVideos.AnyAsync(v => v.Id == id, ct))
            throw new InvalidOperationException("Choose a feed for this video's channel. The selected feed is missing or belongs to another channel.");
        return updated > 0;
    }

}
