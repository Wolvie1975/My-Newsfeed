using Microsoft.EntityFrameworkCore;
using myNewspaper.Shared.Scores;

namespace myNewspaper.Shared.Data;

/// <summary>A category in a pick list: its id, name and the hub it belongs to.</summary>
public sealed record CategoryOption(int Id, string Name, string Hub);

/// <summary>A source in a pick list.</summary>
public sealed record SourceOption(int Id, string Name);

/// <summary>A row of Admin · Sources.</summary>
public sealed record AdminSourceRow(
    int Id, string Url, string? Label, bool Enabled, DateTime? LastScrapedAt, string? LastError, int PageCount, string? CategoryName)
{
    public string Name => string.IsNullOrWhiteSpace(Label) ? Url : Label;
    public bool HasProblem => Enabled && !string.IsNullOrEmpty(LastError);
}

/// <summary>One page of Admin · Sources, with the status counts for the current search and category.</summary>
public sealed record AdminSourcePage(
    IReadOnlyList<AdminSourceRow> Rows, int Total, int PageIndex, int All, int Enabled, int Disabled, int Errors);

/// <summary>A source as its edit page shows it.</summary>
public sealed record AdminSourceDetail(
    int Id, string Url, string? Label, bool Enabled, int? SourceCategoryId, DateTime CreatedAt, DateTime? LastScrapedAt,
    string? LastError, int PageCount, DateTime? NewestPublished);

/// <summary>A row of Admin · Categories.</summary>
public sealed record AdminCategoryRow(int Id, string Name, DateTime DateAdded, int SourceCount, string Hub);

/// <summary>A row of Admin · Pages.</summary>
public sealed record AdminPageRow(int Id, string Title, string Url, string SourceName, int SourceId, DateTime? Published, DateTime ScrapedAt);

/// <summary>One page of Admin · Pages.</summary>
public sealed record AdminPageList(IReadOnlyList<AdminPageRow> Rows, int Total, int PageIndex);

/// <summary>A scraped page as its edit page shows it.</summary>
public sealed record AdminPageDetail(int Id, string Url, string Title, string? Description, DateTime? Published, DateTime ScrapedAt, string SourceName);

/// <summary>A YouTube category in a pick list.</summary>
public sealed record YoutubeCategoryOption(int Id, string Name);

/// <summary>A YouTube feed in a pick list: its id, channel and display name (channel name, else channel id).</summary>
public sealed record YoutubeFeedOption(int Id, string ChannelId, string Name);

/// <summary>A row of Admin · YouTube feeds.</summary>
public sealed record AdminYoutubeFeedRow(int Id, string ChannelId, string? ChannelName, string Url, DateTime DateAdded, int VideoCount, string? Category = null)
{
    public string Name => string.IsNullOrWhiteSpace(ChannelName) ? ChannelId : ChannelName;
}

/// <summary>One page of Admin · YouTube feeds, with what a feed sync would catch up.</summary>
public sealed record AdminYoutubeFeedList(IReadOnlyList<AdminYoutubeFeedRow> Rows, int Total, int PageIndex, int UnlinkedVideos, int ChannelsWithoutFeed);

/// <summary>A YouTube feed as its edit page shows it.</summary>
public sealed record AdminYoutubeFeedDetail(int Id, string ChannelId, string? ChannelName, string Url, int? YoutubeCategoryId,
    bool IsHouseholdPick, DateTime DateAdded, int VideoCount);

/// <summary>A row of Admin · YouTube videos.</summary>
public sealed record AdminYoutubeVideoRow(
    int Id, string VideoId, string Title, string Url, string? ThumbnailUrl, string? ChannelName,
    int? FeedId, string? FeedName, DateTime PublishedAt, long? Views, decimal? RatingAverage, int? RatingCount);

/// <summary>One page of Admin · YouTube videos.</summary>
public sealed record AdminYoutubeVideoList(IReadOnlyList<AdminYoutubeVideoRow> Rows, int Total, int PageIndex);

/// <summary>A YouTube video as its edit page shows it.</summary>
public sealed record AdminYoutubeVideoDetail(int Id, string VideoId, string ChannelId, string? ChannelName, string Title, string Url,
    DateTime PublishedAt, DateTime? UpdatedAt, string? ThumbnailUrl, string? Description, long? ViewCount, int? RatingCount,
    decimal? RatingAverage, DateTime FirstSeenAt, DateTime LastSeenAt, int? YoutubeVideoFeedId);

/// <summary>A schedule (sports event type) in a pick list.</summary>
public sealed record EventTypeOption(int Id, string Name);

/// <summary>A row of Admin · Event types.</summary>
public sealed record AdminEventTypeRow(int Id, string RssUrl, string EventsTypeName, string? SchoolName, DateTime DateAdded, int EventCount,
    bool Enabled, DateTime? LastSuccessAt, string? ProviderKey, bool HasSharedEvents = false)
{
    // Feeds from a provider, or with events shared with other feeds, can only be disabled (AdminStore refuses to delete them).
    public bool CanDelete => ProviderKey is null && !HasSharedEvents;
}

/// <summary>One page of Admin · Event types, with the count of events that have no type.</summary>
public sealed record AdminEventTypeList(IReadOnlyList<AdminEventTypeRow> Rows, int Total, int PageIndex, int UnlinkedEvents);

/// <summary>An event type as its edit page shows it.</summary>
public sealed record AdminEventTypeDetail(int Id, string RssUrl, string EventsTypeName, string? SchoolName, DateTime DateAdded, bool Enabled,
    string? ProviderKey, int? ProviderSchoolId, DateTime? LastAttemptAt, DateTime? LastSuccessAt, string? LastError, int? LastEventCount,
    int? LastIssueCount, int EventCount);

/// <summary>A row of Admin · Events.</summary>
public sealed record AdminEventRow(
    int Id, string Url, string Title, string? Sport, string? Opponent, bool? IsAway, string? Location,
    DateOnly EventDate, DateTime? StartsAtUtc, bool TimeTbd, string? Tv, int? TypeId, string? TypeName, bool AttributionLocked);

/// <summary>One page of Admin · Events.</summary>
public sealed record AdminEventList(IReadOnlyList<AdminEventRow> Rows, int Total, int PageIndex);

/// <summary>An event as its edit page shows it; attribution is locked for provider and shared-calendar events.</summary>
public sealed record AdminEventDetail(int Id, string Url, string Title, string? Sport, string? Opponent, bool? IsAway, string? Location,
    DateOnly EventDate, DateTime? StartsAtUtc, bool TimeTbd, string? Tv, int? SportsEventsTypeId, string? ProviderKey, DateTime FirstSeenAt,
    DateTime LastSeenAt, bool AttributionLocked);

/// <summary>A row of Admin · Results follows.</summary>
public sealed record AdminResultFollowRow(int Id, string Kind, string Sport, string League, string Name, string? LogoUrl, bool Enabled,
    DateTime? LastSyncedAt, string? LastError);

/// <summary>What the admin header alert counts: locked profiles and requests waiting for an answer.</summary>
public sealed record AdminAlertCounts(int LockedProfiles, int WaitingRequests);

/// <summary>A row of Admin · TV series: a tracked TVmaze show, titled from the matched series once there is one.</summary>
public sealed record AdminTvSeriesRow(int Id, int TvmazeShowId, string Title, bool Enabled, int? EpisodeCount, DateTime? LastSuccessAt,
    string? LastError, string? NetworkName, string? WebChannelName);

/// <summary>One page of Admin · TV series.</summary>
public sealed record AdminTvSeriesList(IReadOnlyList<AdminTvSeriesRow> Rows, int Total, int PageIndex);

/// <summary>
/// Reads for the admin site's pages. Writes are in <see cref="AdminStore"/>. Every list is bounded by a page size or by
/// the size of a configuration table (categories, sources).
/// </summary>
public static class AdminQuery
{
    // ---- Categories and sources ----------------------------------------------------------------------------------

    public static async Task<IReadOnlyList<CategoryOption>> CategoryOptionsAsync(WebScraperContext db, CancellationToken ct = default) =>
        await db.SourceCategories.AsNoTracking().OrderBy(c => c.CategoryName)
            .Select(c => new CategoryOption(c.Id, c.CategoryName, c.NewsHub)).ToListAsync(ct);

    public static async Task<IReadOnlyList<AdminCategoryRow>> CategoriesAsync(WebScraperContext db, CancellationToken ct = default) =>
        await db.SourceCategories.AsNoTracking().OrderBy(c => c.CategoryName)
            .Select(c => new AdminCategoryRow(c.Id, c.CategoryName, c.DateAdded, c.Sources.Count, c.NewsHub)).ToListAsync(ct);

    public static async Task<IReadOnlyList<SourceOption>> SourceOptionsAsync(WebScraperContext db, CancellationToken ct = default) =>
        await db.Sources.AsNoTracking().OrderBy(s => s.Label ?? s.Url).Select(s => new SourceOption(s.Id, s.Label ?? s.Url)).ToListAsync(ct);

    /// <summary>
    /// Admin · Sources. <paramref name="category"/> is "none" (uncategorized), a category id, or empty; <paramref name="status"/>
    /// is "enabled", "disabled", "errors" or empty. The page index is clamped to the last page.
    /// </summary>
    public static async Task<AdminSourcePage> SourcesAsync(WebScraperContext db, string? category, string? term, string? status,
        int pageIndex, int pageSize, CancellationToken ct = default)
    {
        IQueryable<Models.Source> query = db.Sources.AsNoTracking();
        if (category == "none") query = query.Where(s => s.SourceCategoryId == null);
        else if (int.TryParse(category, out var categoryId)) query = query.Where(s => s.SourceCategoryId == categoryId);
        term = term?.Trim();
        if (!string.IsNullOrEmpty(term)) query = query.Where(s => s.Url.Contains(term) || (s.Label != null && s.Label.Contains(term)));

        // Counts for the status buttons, within the search and category.
        var all = await query.CountAsync(ct);
        var enabled = await query.CountAsync(s => s.Enabled, ct);
        var errors = await query.CountAsync(s => s.Enabled && s.LastError != null && s.LastError != "", ct);

        query = status switch
        {
            "enabled" => query.Where(s => s.Enabled),
            "disabled" => query.Where(s => !s.Enabled),
            "errors" => query.Where(s => s.Enabled && s.LastError != null && s.LastError != ""),
            _ => query,
        };
        var total = await query.CountAsync(ct);
        pageIndex = Math.Min(Math.Max(0, pageIndex), Math.Max(0, (total - 1) / pageSize));
        var rows = await query
            .OrderBy(s => s.Label ?? s.Url).ThenBy(s => s.Id)
            .Skip(pageIndex * pageSize).Take(pageSize)
            .Select(s => new AdminSourceRow(s.Id, s.Url, s.Label, s.Enabled, s.LastScrapedAt, s.LastError, s.Pages.Count,
                s.SourceCategory != null ? s.SourceCategory.CategoryName : null))
            .ToListAsync(ct);
        return new(rows, total, pageIndex, all, enabled, all - enabled, errors);
    }

    /// <summary>A source with its page count and newest page date, or null when it doesn't exist.</summary>
    public static async Task<AdminSourceDetail?> SourceAsync(WebScraperContext db, int id, CancellationToken ct = default)
    {
        var source = await db.Sources.AsNoTracking().Where(s => s.Id == id)
            .Select(s => new { s.Id, s.Url, s.Label, s.Enabled, s.SourceCategoryId, s.CreatedAt, s.LastScrapedAt, s.LastError })
            .FirstOrDefaultAsync(ct);
        if (source is null) return null;
        var pageCount = await db.Pages.CountAsync(p => p.SourceId == id, ct);
        var newest = await db.Pages.Where(p => p.SourceId == id).MaxAsync(p => p.Published, ct);
        return new(source.Id, source.Url, source.Label, source.Enabled, source.SourceCategoryId, source.CreatedAt, source.LastScrapedAt,
            source.LastError, pageCount, newest);
    }

    // ---- Pages ---------------------------------------------------------------------------------------------------

    /// <summary>
    /// Admin · Pages, sorted by "title", "published" or (default) "scraped". Id breaks ties so paging is stable when
    /// the sort column has duplicates or nulls. The page index is clamped to the last page.
    /// </summary>
    public static async Task<AdminPageList> PagesAsync(WebScraperContext db, int? sourceId, string? search, string sortColumn,
        bool descending, int pageIndex, int pageSize, CancellationToken ct = default)
    {
        IQueryable<Models.Page> query = db.Pages.AsNoTracking();
        if (sourceId is { } id) query = query.Where(p => p.SourceId == id);
        if (!string.IsNullOrEmpty(search))
            query = query.Where(p => p.Title.Contains(search) || p.Url.Contains(search) || (p.Description != null && p.Description.Contains(search)));

        var total = await query.CountAsync(ct);
        var ordered = (sortColumn, descending) switch
        {
            ("title", false) => query.OrderBy(p => p.Title).ThenBy(p => p.Id),
            ("title", true) => query.OrderByDescending(p => p.Title).ThenBy(p => p.Id),
            ("published", false) => query.OrderBy(p => p.Published).ThenBy(p => p.Id),
            ("published", true) => query.OrderByDescending(p => p.Published).ThenBy(p => p.Id),
            (_, false) => query.OrderBy(p => p.ScrapedAt).ThenBy(p => p.Id),
            _ => query.OrderByDescending(p => p.ScrapedAt).ThenBy(p => p.Id),
        };
        pageIndex = Math.Min(Math.Max(0, pageIndex), Math.Max(0, (total - 1) / pageSize));
        var rows = await ordered.Skip(pageIndex * pageSize).Take(pageSize)
            .Select(p => new AdminPageRow(p.Id, p.Title, p.Url, p.Source.Label ?? p.Source.Url, p.SourceId, p.Published, p.ScrapedAt))
            .ToListAsync(ct);
        return new(rows, total, pageIndex);
    }

    public static Task<AdminPageDetail?> PageAsync(WebScraperContext db, int id, CancellationToken ct = default) =>
        db.Pages.AsNoTracking().Where(p => p.Id == id)
            .Select(p => new AdminPageDetail(p.Id, p.Url, p.Title, p.Description, p.Published, p.ScrapedAt, p.Source.Label ?? p.Source.Url))
            .FirstOrDefaultAsync(ct);

    // ---- YouTube -------------------------------------------------------------------------------------------------

    public static async Task<IReadOnlyList<YoutubeCategoryOption>> YoutubeCategoryOptionsAsync(WebScraperContext db, CancellationToken ct = default) =>
        await db.YoutubeCategories.AsNoTracking().OrderBy(c => c.CategoryName).Select(c => new YoutubeCategoryOption(c.Id, c.CategoryName)).ToListAsync(ct);

    /// <summary>Every feed, or only a channel's feeds when <paramref name="channelId"/> is given, by display name.</summary>
    public static async Task<IReadOnlyList<YoutubeFeedOption>> YoutubeFeedOptionsAsync(WebScraperContext db, string? channelId = null,
        CancellationToken ct = default)
    {
        var query = db.YoutubeVideoFeeds.AsNoTracking();
        if (channelId is not null) query = query.Where(f => f.ChannelId == channelId);
        return await query.OrderBy(f => f.ChannelName ?? f.ChannelId)
            .Select(f => new YoutubeFeedOption(f.Id, f.ChannelId, f.ChannelName ?? f.ChannelId)).ToListAsync(ct);
    }

    /// <summary>Admin · YouTube feeds; the page index is clamped to the last page.</summary>
    public static async Task<AdminYoutubeFeedList> YoutubeFeedsAsync(WebScraperContext db, int pageIndex, int pageSize, CancellationToken ct = default)
    {
        var total = await db.YoutubeVideoFeeds.CountAsync(ct);
        pageIndex = Math.Min(Math.Max(0, pageIndex), Math.Max(0, (total - 1) / pageSize));
        var rows = await db.YoutubeVideoFeeds.AsNoTracking()
            .OrderBy(f => f.ChannelName ?? f.ChannelId).ThenBy(f => f.Id)
            .Skip(pageIndex * pageSize).Take(pageSize)
            .Select(f => new AdminYoutubeFeedRow(f.Id, f.ChannelId, f.ChannelName, f.Url, f.DateAdded, f.YouTubeVideos.Count,
                f.YoutubeCategory != null ? f.YoutubeCategory.CategoryName : null))
            .ToListAsync(ct);
        var unlinked = await db.YouTubeVideos.CountAsync(v => v.YoutubeVideoFeedId == null, ct);
        var withoutFeed = await db.YouTubeVideos.Where(v => !db.YoutubeVideoFeeds.Any(f => f.ChannelId == v.ChannelId))
            .Select(v => v.ChannelId).Distinct().CountAsync(ct);
        return new(rows, total, pageIndex, unlinked, withoutFeed);
    }

    public static async Task<AdminYoutubeFeedDetail?> YoutubeFeedAsync(WebScraperContext db, int id, CancellationToken ct = default) =>
        await db.YoutubeVideoFeeds.AsNoTracking().Where(f => f.Id == id)
            .Select(f => new AdminYoutubeFeedDetail(f.Id, f.ChannelId, f.ChannelName, f.Url, f.YoutubeCategoryId, f.IsHouseholdPick, f.DateAdded,
                db.YouTubeVideos.Count(v => v.YoutubeVideoFeedId == f.Id)))
            .FirstOrDefaultAsync(ct);

    /// <summary>
    /// Admin · YouTube videos. <paramref name="feed"/> is "none" (no feed), a feed id, or empty; sort is "title",
    /// "channel", "views" or (default) "published". Id breaks ties; the page index is clamped to the last page.
    /// </summary>
    public static async Task<AdminYoutubeVideoList> YoutubeVideosAsync(WebScraperContext db, string? feed, string? search, string sortColumn,
        bool descending, int pageIndex, int pageSize, CancellationToken ct = default)
    {
        IQueryable<Models.YouTubeVideo> query = db.YouTubeVideos.AsNoTracking();
        if (feed == "none") query = query.Where(v => v.YoutubeVideoFeedId == null);
        else if (int.TryParse(feed, out var feedId)) query = query.Where(v => v.YoutubeVideoFeedId == feedId);
        if (!string.IsNullOrEmpty(search))
        {
            query = query.Where(v => v.Title.Contains(search) || v.VideoId.Contains(search)
                || (v.ChannelName != null && v.ChannelName.Contains(search))
                || (v.YoutubeVideoFeed != null && v.YoutubeVideoFeed.ChannelName != null && v.YoutubeVideoFeed.ChannelName.Contains(search)));
        }
        var total = await query.CountAsync(ct);
        var ordered = (sortColumn, descending) switch
        {
            ("title", false) => query.OrderBy(v => v.Title).ThenBy(v => v.Id),
            ("title", true) => query.OrderByDescending(v => v.Title).ThenBy(v => v.Id),
            ("channel", false) => query.OrderBy(v => v.YoutubeVideoFeed != null ? (v.YoutubeVideoFeed.ChannelName ?? v.YoutubeVideoFeed.ChannelId) : v.ChannelName).ThenBy(v => v.Id),
            ("channel", true) => query.OrderByDescending(v => v.YoutubeVideoFeed != null ? (v.YoutubeVideoFeed.ChannelName ?? v.YoutubeVideoFeed.ChannelId) : v.ChannelName).ThenBy(v => v.Id),
            ("views", false) => query.OrderBy(v => v.ViewCount).ThenBy(v => v.Id),
            ("views", true) => query.OrderByDescending(v => v.ViewCount).ThenBy(v => v.Id),
            (_, false) => query.OrderBy(v => v.PublishedAt).ThenBy(v => v.Id),
            _ => query.OrderByDescending(v => v.PublishedAt).ThenBy(v => v.Id),
        };
        pageIndex = Math.Min(Math.Max(0, pageIndex), Math.Max(0, (total - 1) / pageSize));
        var rows = await ordered.Skip(pageIndex * pageSize).Take(pageSize)
            .Select(v => new AdminYoutubeVideoRow(v.Id, v.VideoId, v.Title, v.Url, v.ThumbnailUrl, v.ChannelName, v.YoutubeVideoFeedId,
                v.YoutubeVideoFeed != null ? (v.YoutubeVideoFeed.ChannelName ?? v.YoutubeVideoFeed.ChannelId) : null,
                v.PublishedAt, v.ViewCount, v.RatingAverage, v.RatingCount))
            .ToListAsync(ct);
        return new(rows, total, pageIndex);
    }

    public static Task<AdminYoutubeVideoDetail?> YoutubeVideoAsync(WebScraperContext db, int id, CancellationToken ct = default) =>
        db.YouTubeVideos.AsNoTracking().Where(v => v.Id == id)
            .Select(v => new AdminYoutubeVideoDetail(v.Id, v.VideoId, v.ChannelId, v.ChannelName, v.Title, v.Url, v.PublishedAt, v.UpdatedAt,
                v.ThumbnailUrl, v.Description, v.ViewCount, v.RatingCount, v.RatingAverage, v.FirstSeenAt, v.LastSeenAt, v.YoutubeVideoFeedId))
            .FirstOrDefaultAsync(ct);

    // ---- Sports events and Results follows -----------------------------------------------------------------------

    /// <summary>Admin · Event types; an event counts for a type when it is assigned or shared through an event source.</summary>
    public static async Task<AdminEventTypeList> EventTypesAsync(WebScraperContext db, int pageIndex, int pageSize, CancellationToken ct = default)
    {
        var total = await db.SportsEventsTypes.CountAsync(ct);
        pageIndex = Math.Min(Math.Max(0, pageIndex), Math.Max(0, (total - 1) / pageSize));
        var rows = await db.SportsEventsTypes.AsNoTracking()
            .OrderBy(t => t.EventsTypeName).ThenBy(t => t.Id)
            .Skip(pageIndex * pageSize).Take(pageSize)
            .Select(t => new AdminEventTypeRow(t.Id, t.RssUrl, t.EventsTypeName, t.SchoolName, t.DateAdded,
                db.SportsEvents.Count(e => e.SportsEventsTypeId == t.Id || e.EventSources.Any(s => s.SportsEventsTypeId == t.Id)),
                t.Enabled, t.LastSuccessAt, t.ProviderKey, db.SportsEventSources.Any(s => s.SportsEventsTypeId == t.Id)))
            .ToListAsync(ct);
        var unlinked = await db.SportsEvents.CountAsync(e => e.SportsEventsTypeId == null, ct);
        return new(rows, total, pageIndex, unlinked);
    }

    public static async Task<AdminEventTypeDetail?> EventTypeAsync(WebScraperContext db, int id, CancellationToken ct = default) =>
        await db.SportsEventsTypes.AsNoTracking().Where(t => t.Id == id)
            .Select(t => new AdminEventTypeDetail(t.Id, t.RssUrl, t.EventsTypeName, t.SchoolName, t.DateAdded, t.Enabled, t.ProviderKey,
                t.ProviderSchoolId, t.LastAttemptAt, t.LastSuccessAt, t.LastError, t.LastEventCount, t.LastIssueCount,
                db.SportsEvents.Count(e => e.SportsEventsTypeId == t.Id || e.EventSources.Any(s => s.SportsEventsTypeId == t.Id))))
            .FirstOrDefaultAsync(ct);

    /// <summary>Every event type, or (<paramref name="legacyOnlyExcept"/> given) only the legacy ones plus that type.</summary>
    public static async Task<IReadOnlyList<EventTypeOption>> EventTypeOptionsAsync(WebScraperContext db, bool legacyOnly = false,
        int? legacyOnlyExcept = null, CancellationToken ct = default)
    {
        var query = db.SportsEventsTypes.AsNoTracking();
        if (legacyOnly) query = query.Where(t => t.ProviderKey == null || t.Id == legacyOnlyExcept);
        return await query.OrderBy(t => t.EventsTypeName).Select(t => new EventTypeOption(t.Id, t.EventsTypeName)).ToListAsync(ct);
    }

    /// <summary>The distinct sports named on events, for the filter list.</summary>
    public static async Task<IReadOnlyList<string>> EventSportsAsync(WebScraperContext db, CancellationToken ct = default) =>
        await db.SportsEvents.AsNoTracking().Where(e => e.Sport != null).Select(e => e.Sport ?? "").Distinct().OrderBy(s => s).ToListAsync(ct);

    /// <summary>
    /// Admin · Events. <paramref name="type"/> is "none", a type id (assigned or shared) or empty; <paramref name="when"/> is
    /// "upcoming" or "past" relative to <paramref name="today"/> (the display zone's date); sort is "sport", "opponent" or
    /// (default) "date". Id breaks ties; the page index is clamped to the last page.
    /// </summary>
    public static async Task<AdminEventList> EventsAsync(WebScraperContext db, string? type, string? sport, string? when, DateOnly today,
        string? search, string sortColumn, bool descending, int pageIndex, int pageSize, CancellationToken ct = default)
    {
        IQueryable<Models.SportsEvent> query = db.SportsEvents.AsNoTracking();
        if (type == "none") query = query.Where(e => e.SportsEventsTypeId == null);
        else if (int.TryParse(type, out var typeId)) query = query.Where(e => e.SportsEventsTypeId == typeId || e.EventSources.Any(s => s.SportsEventsTypeId == typeId));
        if (!string.IsNullOrEmpty(sport)) query = query.Where(e => e.Sport == sport);
        if (when == "upcoming") query = query.Where(e => e.EventDate >= today);
        else if (when == "past") query = query.Where(e => e.EventDate < today);
        if (!string.IsNullOrEmpty(search))
            query = query.Where(e => e.Title.Contains(search) || (e.Opponent != null && e.Opponent.Contains(search)) || (e.Location != null && e.Location.Contains(search)));

        var total = await query.CountAsync(ct);
        var ordered = (sortColumn, descending) switch
        {
            ("sport", false) => query.OrderBy(e => e.Sport).ThenBy(e => e.EventDate).ThenBy(e => e.Id),
            ("sport", true) => query.OrderByDescending(e => e.Sport).ThenBy(e => e.EventDate).ThenBy(e => e.Id),
            ("opponent", false) => query.OrderBy(e => e.Opponent).ThenBy(e => e.EventDate).ThenBy(e => e.Id),
            ("opponent", true) => query.OrderByDescending(e => e.Opponent).ThenBy(e => e.EventDate).ThenBy(e => e.Id),
            (_, false) => query.OrderBy(e => e.EventDate).ThenBy(e => e.StartsAtUtc).ThenBy(e => e.Id),
            _ => query.OrderByDescending(e => e.EventDate).ThenByDescending(e => e.StartsAtUtc).ThenBy(e => e.Id),
        };
        pageIndex = Math.Min(Math.Max(0, pageIndex), Math.Max(0, (total - 1) / pageSize));
        var rows = await ordered.Skip(pageIndex * pageSize).Take(pageSize)
            .Select(e => new AdminEventRow(e.Id, e.Url, e.Title, e.Sport, e.Opponent, e.IsAway, e.Location, e.EventDate, e.StartsAtUtc, e.TimeTbd, e.Tv,
                e.SportsEventsTypeId, e.SportsEventsType != null ? e.SportsEventsType.EventsTypeName : null, e.ProviderKey != null || e.EventSources.Any()))
            .ToListAsync(ct);
        return new(rows, total, pageIndex);
    }

    public static Task<AdminEventDetail?> EventAsync(WebScraperContext db, int id, CancellationToken ct = default) =>
        db.SportsEvents.AsNoTracking().Where(e => e.Id == id)
            .Select(e => new AdminEventDetail(e.Id, e.Url, e.Title, e.Sport, e.Opponent, e.IsAway, e.Location, e.EventDate, e.StartsAtUtc, e.TimeTbd, e.Tv,
                e.SportsEventsTypeId, e.ProviderKey, e.FirstSeenAt, e.LastSeenAt,
                e.ProviderKey != null || db.SportsEventSources.Any(s => s.SportsEventId == e.Id)))
            .FirstOrDefaultAsync(ct);

    /// <summary>Every Results follow, in the order Admin lists them: by kind, then name, then league.</summary>
    public static async Task<IReadOnlyList<AdminResultFollowRow>> ResultFollowsAsync(WebScraperContext db, CancellationToken ct = default) =>
        (await db.ResultFollows.AsNoTracking()
            .Select(f => new AdminResultFollowRow(f.Id, f.Kind, f.Sport, f.League, f.Name, f.LogoUrl, f.Enabled, f.LastSyncedAt, f.LastError))
            .ToListAsync(ct))
        .OrderBy(f => FollowKinds.Order(f.Kind))
        .ThenBy(f => f.Name, StringComparer.OrdinalIgnoreCase)
        .ThenBy(f => EspnLeagues.NameOf(f.Sport, f.League))
        .ToList();

    // ---- Header alert and TV -------------------------------------------------------------------------------------

    public static async Task<AdminAlertCounts> AlertCountsAsync(WebScraperContext db, CancellationToken ct = default) =>
        new(await db.Profiles.CountAsync(p => p.LockedAt != null, ct), await db.ContentRequests.CountAsync(r => r.Status == "waiting", ct));

    /// <summary>Admin · TV series: followed shows first, then by expected title. The page index is clamped to the last page.</summary>
    public static async Task<AdminTvSeriesList> TvSeriesAsync(WebScraperContext db, int pageIndex, int pageSize, CancellationToken ct = default)
    {
        var total = await db.TvTrackedSeries.CountAsync(ct);
        pageIndex = Math.Min(Math.Max(0, pageIndex), Math.Max(0, (total - 1) / pageSize));
        var rows = await db.TvTrackedSeries.AsNoTracking()
            .OrderByDescending(t => t.Enabled).ThenBy(t => t.ExpectedTitle).ThenBy(t => t.Id)
            .Skip(pageIndex * pageSize).Take(pageSize)
            .Select(t => new AdminTvSeriesRow(t.Id, t.TvmazeShowId, t.Series != null ? t.Series.Title : t.ExpectedTitle, t.Enabled, t.EpisodeCount,
                t.LastSuccessAt, t.LastError, t.Series != null ? t.Series.NetworkName : null, t.Series != null ? t.Series.WebChannelName : null))
            .ToListAsync(ct);
        return new(rows, total, pageIndex);
    }
}
