using System.Globalization;
using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using myNewspaper.Shared.Profiles;

namespace myNewspaper.Shared.Data;

public sealed record TechBriefing(int Technology, int VideoGames, int Linux, int Videos);

public sealed record TrackedRelease(
    string Project,
    string Kind,
    string Version,
    DateTime ReleasedAt,
    string? NotesUrl,
    string NotesLabel,
    string Source,
    string ProjectSlug = "",
    DateTime? LastCheckedAt = null);

public sealed record DevStory(
    int Rank,
    string Title,
    string? Url,
    string? Domain,
    int Points,
    int Comments,
    string DiscussionUrl,
    DateTime? SnapshotAt = null);

public sealed record ServiceState(
    string Name,
    string Indicator,
    string? Incident,
    string? Component,
    DateTime? Since,
    string PageUrl,
    string? IncidentUrl = null);

public sealed record WeekSummary(
    DateOnly Start,
    DateOnly End,
    string Desk,
    int Stories,
    int Sources,
    int Releases,
    string? BusiestSource,
    int BusiestSourceCount,
    DayOfWeek BusiestDay,
    int BusiestDayCount,
    IReadOnlyList<CoveredStory> Biggest,
    IReadOnlyList<(DateOnly Day, int Count, FeedItem? Top)> Days,
    IReadOnlyList<TrackedRelease> ReleasedThisWeek,
    IReadOnlyList<(string Topic, int Count, string Hub)> Topics);

/// <summary>Read side of the Tech front (/tech) and Tech desks.</summary>
public static class TechQuery
{
    /// <summary>The tracked upstream projects of the Linux release tracker, for the admin status page.</summary>
    public static async Task<IReadOnlyList<TechReleaseProjectRow>> ReleaseProjectsAsync(WebScraperContext db, CancellationToken ct = default) =>
        await db.TechReleaseProjects.AsNoTracking().OrderBy(p => p.SortOrder).ThenBy(p => p.Name)
            .Select(p => new TechReleaseProjectRow(p.Name, p.Kind, p.LastCheckedAt, p.LastError)).ToListAsync(ct);

    /// <summary>How many enabled sources feed the Tech hub's categories.</summary>
    public static Task<int> SourceCountAsync(WebScraperContext db, CancellationToken ct = default) =>
        db.Sources.AsNoTracking().CountAsync(s => s.Enabled && s.SourceCategory != null && s.SourceCategory.NewsHub == "tech", ct);

    public static readonly string[] VideoCategories = ["Tech", "Video Games"];

    private static readonly IReadOnlyList<VideoCategory> VideoCategoryTabs =
        VideoCategories.Select(n => new VideoCategory(VideosQuery.Slug(n), n)).ToList();

    private static readonly MemoryCache FallbackCache = new(new MemoryCacheOptions());

    private static readonly HashSet<string> StopWords = new(StringComparer.Ordinal)
    {
        "the", "a", "an", "and", "of", "to", "in", "for", "on", "with", "is", "are",
        "how", "why", "what", "your", "you", "this", "that", "it", "its", "new", "now",
        "after", "over", "from", "by", "at", "as", "be", "will",
    };

    public static async Task<TechBriefing> BriefingAsync(
        WebScraperContext db, FeedDates dates, CancellationToken ct = default)
    {
        var today = dates.Today;
        var startUtc = dates.StartOfDayUtc(today);
        var endUtc = dates.StartOfDayUtc(today.AddDays(1));

        var storyCounts = await FeedQuery.VisibleInHub(db, "tech")
            .Where(p => (p.Published ?? p.ScrapedAt) >= startUtc && (p.Published ?? p.ScrapedAt) < endUtc)
            .GroupBy(p => p.Source.SourceCategory != null ? p.Source.SourceCategory.CategoryName : null)
            .Select(g => new { Category = g.Key, Count = g.Count() })
            .ToListAsync(ct);

        var tech = storyCounts.FirstOrDefault(c => c.Category == NewsSections.Technology)?.Count ?? 0;
        var games = storyCounts.FirstOrDefault(c => c.Category == NewsSections.VideoGames)?.Count ?? 0;
        var linux = storyCounts.FirstOrDefault(c => c.Category == NewsSections.Linux)?.Count ?? 0;

        var videos = await db.YouTubeVideos.AsNoTracking()
            .Where(v => v.PublishedAt >= startUtc && v.PublishedAt < endUtc
                && v.YoutubeVideoFeed != null && v.YoutubeVideoFeed.YoutubeCategory != null
                && VideoCategories.Contains(v.YoutubeVideoFeed.YoutubeCategory.CategoryName))
            .CountAsync(ct);

        return new TechBriefing(tech, games, linux, videos);
    }

    public static Task<IReadOnlyList<FeedItem>> LatestAsync(
        WebScraperContext db, int take, string? category = null, CancellationToken ct = default) =>
        FeedQuery.GetLatestInHubAsync(db, "tech", take, category, ct);

    public static Task<IReadOnlyList<CoveredStory>> MostCoveredAsync(
        WebScraperContext db, DateTime nowUtc, TimeSpan window, int minSources, int take, string? category = null,
        CancellationToken ct = default) =>
        CoveredQuery.MostCoveredAsync(db, FallbackCache, "tech", nowUtc - window, minSources, take, category, ct);

    public static Task<IReadOnlyList<CoveredStory>> MostCoveredAsync(
        WebScraperContext db, IMemoryCache? cache, DateTime nowUtc, TimeSpan window, int minSources, int take,
        string? category = null, CancellationToken ct = default) =>
        CoveredQuery.MostCoveredAsync(db, cache, "tech", nowUtc - window, minSources, take, category, ct);

    public static async Task<IReadOnlyList<VideoItem>> VideosAsync(
        WebScraperContext db, int take, CancellationToken ct = default)
    {
        var page = await VideosQuery.GetVideosAsync(
            db, Viewer.Household, channelId: null, search: null, limit: take, offset: 0,
            categories: VideoCategoryTabs, ct);
        return page.Items;
    }

    public static async Task<IReadOnlyList<VideoItem>> VideosByCategoryAsync(
        WebScraperContext db, string categoryName, int take, CancellationToken ct = default)
    {
        var page = await VideosQuery.GetVideosAsync(
            db, Viewer.Household, channelId: null, search: null, limit: take, offset: 0,
            categories: [new VideoCategory(VideosQuery.Slug(categoryName), categoryName)], ct);
        return page.Items;
    }

    public static async Task<IReadOnlyList<FeedItem>> DeskItemsSinceAsync(
        WebScraperContext db, DateTime sinceUtc, string? category = null, int take = 300, CancellationToken ct = default) =>
        await FeedQuery.VisibleSince(db, sinceUtc, "tech", category)
            .OrderBy(p => p.Published == null ? 1 : 0)
            .ThenByDescending(p => p.Published ?? p.ScrapedAt)
            .ThenByDescending(p => p.Id)
            .Take(take)
            .Select(FeedQuery.ToFeedItem)
            .ToListAsync(ct);

    /// <summary>
    /// Groups headlines from the same window that cover the same story across at least <paramref name="minSources"/>
    /// distinct sources. Pure function over in-memory items for direct unit testing.
    /// </summary>
    public static IReadOnlyList<CoveredStory> GroupMostCovered(
        IEnumerable<FeedItem> items, int minSources, int take = int.MaxValue) =>
        CoveredQuery.Group(items, minSources, take);

    // ---------- Part 4: Live data (releases, dev reading, service status) ----------

    public static async Task<bool> LiveTablesAsync(
        WebScraperContext db, string group, CancellationToken ct = default)
    {
        var key = $"tech-tables:{group}";
        if (FallbackCache.TryGetValue(key, out bool exists)) return exists;

        FormattableString? sql = group switch
        {
            "releases" => $"SELECT CASE WHEN OBJECT_ID(N'dbo.TechReleaseProjects', N'U') IS NOT NULL AND OBJECT_ID(N'dbo.TechReleases', N'U') IS NOT NULL THEN 1 ELSE 0 END AS [Value]",
            "devreading" => $"SELECT CASE WHEN OBJECT_ID(N'dbo.TechDevReadingItems', N'U') IS NOT NULL THEN 1 ELSE 0 END AS [Value]",
            "status" => $"SELECT CASE WHEN OBJECT_ID(N'dbo.TechStatusServices', N'U') IS NOT NULL AND OBJECT_ID(N'dbo.TechServiceStatus', N'U') IS NOT NULL THEN 1 ELSE 0 END AS [Value]",
            _ => null,
        };
        if (sql is null) return false;

        try
        {
            var result = await db.Database.SqlQuery<int>(sql).FirstOrDefaultAsync(ct);
            exists = result == 1;
        }
        catch (Exception ex) when (DataFailures.IsReadFailure(ex))
        {
            // A failed check says nothing about the tables: caching it hid the Tech sections for 10 minutes after one
            // database blip. Only answers are cached; the next request asks again.
            return false;
        }

        FallbackCache.Set(key, exists, TimeSpan.FromMinutes(10));
        return exists;
    }

    public static void InvalidateLiveTableCache()
    {
        FallbackCache.Remove("tech-tables:releases");
        FallbackCache.Remove("tech-tables:devreading");
        FallbackCache.Remove("tech-tables:status");
    }

    public static string ReleaseSourceLabel(string? sourceKind, string? sourceUrl)
    {
        var kind = sourceKind?.Trim().ToLowerInvariant() ?? "";
        if (kind.StartsWith("kernel", StringComparison.Ordinal) || sourceUrl?.Contains("kernel.org", StringComparison.OrdinalIgnoreCase) == true)
            return "kernel.org";
        if (kind.StartsWith("github", StringComparison.Ordinal) || sourceUrl?.Contains("github.com", StringComparison.OrdinalIgnoreCase) == true)
            return "GitHub releases";
        if (kind.StartsWith("gitlab", StringComparison.Ordinal) || sourceUrl?.Contains("gitlab", StringComparison.OrdinalIgnoreCase) == true)
            return "GitLab";

        if (Uri.TryCreate(sourceUrl, UriKind.Absolute, out var uri) && !string.IsNullOrWhiteSpace(uri.Host))
        {
            var host = uri.Host.ToLowerInvariant();
            return host.StartsWith("www.", StringComparison.Ordinal) ? host[4..] : host;
        }

        return !string.IsNullOrWhiteSpace(sourceKind) ? sourceKind : "Release feed";
    }

    public static async Task<IReadOnlyList<TrackedRelease>> TrackerAsync(
        WebScraperContext db, CancellationToken ct = default)
    {
        if (!await LiveTablesAsync(db, "releases", ct)) return [];
        try
        {
            var projects = await db.TechReleaseProjects.AsNoTracking()
                .Where(p => p.Enabled)
                .OrderBy(p => p.SortOrder).ThenBy(p => p.Name)
                .ToListAsync(ct);
            if (projects.Count == 0) return [];

            // Only each project's latest stable release, chosen in SQL (ROW_NUMBER), not every release ever recorded.
            var projectIds = projects.Select(p => p.Id).ToList();
            var latestByProject = (await db.TechReleases.AsNoTracking()
                .Where(r => !r.IsPrerelease && projectIds.Contains(r.ProjectId))
                .GroupBy(r => r.ProjectId)
                .Select(g => g.OrderByDescending(r => r.ReleasedAt).ThenByDescending(r => r.Id).First())
                .ToListAsync(ct))
                .ToDictionary(r => r.ProjectId);

            var list = new List<TrackedRelease>();
            foreach (var p in projects)
            {
                if (!latestByProject.TryGetValue(p.Id, out var r)) continue;
                list.Add(new TrackedRelease(
                    p.Name,
                    p.Kind,
                    r.Version,
                    r.ReleasedAt,
                    r.NotesUrl,
                    string.IsNullOrWhiteSpace(p.NotesLabel) ? "Release notes" : p.NotesLabel,
                    ReleaseSourceLabel(p.SourceKind, p.SourceUrl),
                    p.Slug,
                    p.LastCheckedAt));
            }
            return list;
        }
        catch (Exception ex) when (DataFailures.IsReadFailure(ex))
        {
            return [];
        }
    }

    public static Task<IReadOnlyList<TrackedRelease>> JustReleasedAsync(
        WebScraperContext db, DateTime nowUtc, TimeSpan window, int take, CancellationToken ct = default) =>
        ReleasesBetweenAsync(db, nowUtc - window, nowUtc.AddHours(1), take, ct);

    public static async Task<IReadOnlyList<TrackedRelease>> ReleasesBetweenAsync(
        WebScraperContext db, DateTime startUtc, DateTime endUtc, int take = 100, CancellationToken ct = default)
    {
        if (!await LiveTablesAsync(db, "releases", ct)) return [];
        try
        {
            var rows = await db.TechReleases.AsNoTracking()
                .Where(r => !r.IsPrerelease && r.Project.Enabled && r.ReleasedAt >= startUtc && r.ReleasedAt < endUtc)
                .OrderByDescending(r => r.ReleasedAt).ThenByDescending(r => r.Id)
                .Take(take)
                .Select(r => new
                {
                    r.Project.Name,
                    r.Project.Kind,
                    r.Project.Slug,
                    r.Project.NotesLabel,
                    r.Project.SourceKind,
                    r.Project.SourceUrl,
                    r.Project.LastCheckedAt,
                    r.Version,
                    r.ReleasedAt,
                    r.NotesUrl,
                })
                .ToListAsync(ct);

            return rows.Select(r => new TrackedRelease(
                r.Name,
                r.Kind,
                r.Version,
                r.ReleasedAt,
                r.NotesUrl,
                string.IsNullOrWhiteSpace(r.NotesLabel) ? "Release notes" : r.NotesLabel,
                ReleaseSourceLabel(r.SourceKind, r.SourceUrl),
                r.Slug,
                r.LastCheckedAt)).ToList();
        }
        catch (Exception ex) when (DataFailures.IsReadFailure(ex))
        {
            return [];
        }
    }

    public static async Task<(IReadOnlyList<(string Slug, string Name)> Projects, IReadOnlyList<TrackedRelease> Releases)> ReleaseHistoryAsync(
        WebScraperContext db, DateTime nowUtc, string? projectSlug = null, CancellationToken ct = default)
    {
        if (!await LiveTablesAsync(db, "releases", ct)) return ([], []);
        try
        {
            var projects = await db.TechReleaseProjects.AsNoTracking()
                .Where(p => p.Enabled)
                .OrderBy(p => p.SortOrder).ThenBy(p => p.Name)
                .Select(p => new { p.Slug, p.Name })
                .ToListAsync(ct);

            var since = nowUtc.AddMonths(-12);
            var query = db.TechReleases.AsNoTracking()
                .Where(r => !r.IsPrerelease && r.Project.Enabled && r.ReleasedAt >= since);
            if (!string.IsNullOrWhiteSpace(projectSlug))
            {
                var slug = projectSlug.Trim();
                query = query.Where(r => r.Project.Slug == slug);
            }

            var rows = await query
                .OrderByDescending(r => r.ReleasedAt).ThenByDescending(r => r.Id)
                .Select(r => new
                {
                    r.Project.Name,
                    r.Project.Kind,
                    r.Project.Slug,
                    r.Project.NotesLabel,
                    r.Project.SourceKind,
                    r.Project.SourceUrl,
                    r.Project.LastCheckedAt,
                    r.Version,
                    r.ReleasedAt,
                    r.NotesUrl,
                })
                .ToListAsync(ct);

            var releases = rows.Select(r => new TrackedRelease(
                r.Name,
                r.Kind,
                r.Version,
                r.ReleasedAt,
                r.NotesUrl,
                string.IsNullOrWhiteSpace(r.NotesLabel) ? "Release notes" : r.NotesLabel,
                ReleaseSourceLabel(r.SourceKind, r.SourceUrl),
                r.Slug,
                r.LastCheckedAt)).ToList();

            return (projects.Select(p => (p.Slug, p.Name)).ToList(), releases);
        }
        catch (Exception ex) when (DataFailures.IsReadFailure(ex))
        {
            return ([], []);
        }
    }

    public static async Task<IReadOnlyList<DevStory>> DevReadingAsync(
        WebScraperContext db, string site, int take, CancellationToken ct = default)
    {
        if (!await LiveTablesAsync(db, "devreading", ct)) return [];
        var normalized = string.Equals(site, "lobsters", StringComparison.OrdinalIgnoreCase) ? "lobsters" : "hn";
        try
        {
            var rows = await db.TechDevReadingItems.AsNoTracking()
                .Where(i => i.Site == normalized)
                .OrderBy(i => i.Rank)
                .Take(take)
                .ToListAsync(ct);

            return rows.Select(i => new DevStory(
                i.Rank, i.Title, i.Url, i.Domain, i.Points, i.Comments, i.DiscussionUrl, i.SnapshotAt)).ToList();
        }
        catch (Exception ex) when (DataFailures.IsReadFailure(ex))
        {
            return [];
        }
    }

    public static async Task<IReadOnlyList<ServiceState>> ServiceStatesAsync(
        WebScraperContext db, CancellationToken ct = default)
    {
        if (!await LiveTablesAsync(db, "status", ct)) return [];
        try
        {
            var services = await db.TechStatusServices.AsNoTracking()
                .Where(s => s.Enabled)
                .OrderBy(s => s.SortOrder).ThenBy(s => s.Name)
                .ToListAsync(ct);
            if (services.Count == 0) return [];

            var statuses = await db.TechServiceStatus.AsNoTracking()
                .ToDictionaryAsync(s => s.ServiceId, ct);

            var list = new List<ServiceState>();
            foreach (var svc in services)
            {
                if (statuses.TryGetValue(svc.Id, out var st))
                {
                    list.Add(new ServiceState(
                        svc.Name,
                        st.Indicator ?? "unknown",
                        st.IncidentName ?? st.Description,
                        st.IncidentComponent,
                        st.IncidentStartedAt,
                        svc.PageUrl,
                        st.IncidentUrl));
                }
                else
                {
                    list.Add(new ServiceState(svc.Name, "unknown", null, null, null, svc.PageUrl, null));
                }
            }
            return list;
        }
        catch (Exception ex) when (DataFailures.IsReadFailure(ex))
        {
            return [];
        }
    }

    // ---------- Part 5: Weekly roundup (/tech/week) ----------

    /// <summary>
    /// The latest roundup covers the last complete Sunday–Saturday week in America/Chicago.
    /// It publishes at 6:00 AM Sunday Central; before then, the previous week is still the latest.
    /// </summary>
    public static DateOnly LatestWeekStart(DateTime nowUtc, FeedDates dates)
    {
        var local = TimeZoneInfo.ConvertTimeFromUtc(DateTime.SpecifyKind(nowUtc, DateTimeKind.Utc), dates.Zone);
        var today = DateOnly.FromDateTime(local);
        var thisWeekSunday = today.AddDays(-(int)today.DayOfWeek);
        return today.DayOfWeek == DayOfWeek.Sunday && local.TimeOfDay < TimeSpan.FromHours(6)
            ? thisWeekSunday.AddDays(-14)
            : thisWeekSunday.AddDays(-7);
    }

    public static bool TryResolveWeekStart(string? weekQuery, DateTime nowUtc, FeedDates dates, out DateOnly weekStart)
    {
        var latest = LatestWeekStart(nowUtc, dates);
        if (string.IsNullOrWhiteSpace(weekQuery))
        {
            weekStart = latest;
            return true;
        }

        if (DateOnly.TryParseExact(weekQuery.Trim(), "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsed)
            && parsed.DayOfWeek == DayOfWeek.Sunday
            && parsed <= latest
            && parsed >= latest.AddDays(-12 * 7))
        {
            weekStart = parsed;
            return true;
        }

        weekStart = default;
        return false;
    }

    public static async Task<WeekSummary> WeekAsync(
        WebScraperContext db, IMemoryCache? cache, FeedDates dates, DateOnly weekStart, string desk = "all",
        int? profileId = null, CancellationToken ct = default)
    {
        var normalizedDesk = string.IsNullOrWhiteSpace(desk) ? "all" : desk.Trim().ToLowerInvariant();
        var cacheKey = $"tech-week:{normalizedDesk}:{weekStart:yyyy-MM-dd}";
        var store = cache ?? FallbackCache;

        if (!store.TryGetValue(cacheKey, out WeekSummary? cached) || cached is null)
        {
            var startUtc = dates.StartOfDayUtc(weekStart);
            var endUtc = dates.StartOfDayUtc(weekStart.AddDays(7));
            var category = normalizedDesk == "all" ? null : TechDesks.For(normalizedDesk)?.Category;

            var items = await FeedQuery.VisibleInHub(db, "tech", category)
                .Where(p => (p.Published ?? p.ScrapedAt) >= startUtc && (p.Published ?? p.ScrapedAt) < endUtc)
                .OrderByDescending(p => p.Published ?? p.ScrapedAt)
                .ThenByDescending(p => p.Id)
                .Select(FeedQuery.ToFeedItem)
                .ToListAsync(ct);

            var storiesCount = items.Count;
            var sourcesByCount = items
                .GroupBy(SourceKey, StringComparer.OrdinalIgnoreCase)
                .Select(g => (Name: g.Key, Count: g.Count()))
                .OrderByDescending(g => g.Count)
                .ThenBy(g => g.Name)
                .ToList();
            var sourcesCount = sourcesByCount.Count;
            var busiestSource = sourcesByCount.FirstOrDefault().Name;
            var busiestSourceCount = sourcesByCount.FirstOrDefault().Count;

            var days = new List<(DateOnly Day, int Count, FeedItem? Top)>(7);
            for (var i = 0; i < 7; i++)
            {
                var d = weekStart.AddDays(i);
                var dayItems = items.Where(item => dates.TodayAt(item.Date) == d).ToList();
                var topCovered = GroupMostCovered(dayItems, minSources: 2, take: 1);
                var topStory = topCovered.Count > 0 ? topCovered[0].Lead : (dayItems.Count > 0 ? dayItems[0] : null);
                days.Add((d, dayItems.Count, topStory));
            }

            var busiestDayEntry = days.OrderByDescending(d => d.Count).First();
            var biggest = GroupMostCovered(items, minSources: 2, take: 5);
            var releases = normalizedDesk is "all" or "linux"
                ? await ReleasesBetweenAsync(db, startUtc, endUtc, take: 50, ct)
                : [];

            cached = new WeekSummary(
                weekStart,
                weekStart.AddDays(6),
                normalizedDesk,
                storiesCount,
                sourcesCount,
                releases.Count,
                busiestSource,
                busiestSourceCount,
                busiestDayEntry.Day.DayOfWeek,
                busiestDayEntry.Count,
                biggest,
                days,
                releases,
                []);

            store.Set(cacheKey, cached, TimeSpan.FromHours(24));
        }

        return cached;
    }

    public static async Task<IReadOnlyDictionary<string, int>> WeekDeskCountsAsync(
        WebScraperContext db, FeedDates dates, DateOnly weekStart, CancellationToken ct = default)
    {
        var startUtc = dates.StartOfDayUtc(weekStart);
        var endUtc = dates.StartOfDayUtc(weekStart.AddDays(7));
        var rows = await FeedQuery.VisibleInHub(db, "tech")
            .Where(p => (p.Published ?? p.ScrapedAt) >= startUtc && (p.Published ?? p.ScrapedAt) < endUtc)
            .GroupBy(p => p.Source.SourceCategory != null ? p.Source.SourceCategory.CategoryName : null)
            .Select(g => new { Category = g.Key, Count = g.Count() })
            .ToListAsync(ct);

        var tech = rows.FirstOrDefault(r => r.Category == NewsSections.Technology)?.Count ?? 0;
        var games = rows.FirstOrDefault(r => r.Category == NewsSections.VideoGames)?.Count ?? 0;
        var linux = rows.FirstOrDefault(r => r.Category == NewsSections.Linux)?.Count ?? 0;
        return new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase)
        {
            ["all"] = tech + games + linux,
            ["technology"] = tech,
            ["video-games"] = games,
            ["linux"] = linux,
        };
    }

    public static async Task<IReadOnlyList<(DateOnly Start, DateOnly End, int Stories)>> EarlierWeeksAsync(
        WebScraperContext db, FeedDates dates, DateOnly currentWeekStart, string desk, int count = 3, CancellationToken ct = default)
    {
        var category = desk == "all" ? null : TechDesks.For(desk)?.Category;
        var results = new List<(DateOnly Start, DateOnly End, int Stories)>();
        for (var i = 1; i <= count; i++)
        {
            var start = currentWeekStart.AddDays(-7 * i);
            var startUtc = dates.StartOfDayUtc(start);
            var endUtc = dates.StartOfDayUtc(start.AddDays(7));
            var stories = await FeedQuery.VisibleInHub(db, "tech", category)
                .CountAsync(p => (p.Published ?? p.ScrapedAt) >= startUtc && (p.Published ?? p.ScrapedAt) < endUtc, ct);
            results.Add((start, start.AddDays(6), stories));
        }
        return results;
    }

    internal static HashSet<string> KeySet(string? title) => CoveredQuery.KeySet(title);
    internal static bool Matches(HashSet<string> a, HashSet<string> b) => CoveredQuery.Matches(a, b);
    internal static string SourceKey(FeedItem item) => CoveredQuery.SourceKey(item);
}

/// <summary>A tracked release project as the admin status page lists it.</summary>
public sealed record TechReleaseProjectRow(string Name, string Kind, DateTime? LastCheckedAt, string? LastError);
