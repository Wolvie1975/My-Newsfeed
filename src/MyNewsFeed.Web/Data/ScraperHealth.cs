using System.Globalization;
using Microsoft.EntityFrameworkCore;

namespace MyNewsFeed.Web.Data;

public enum HealthLevel { Ok, Warning, Problem }

/// <summary>One thing the scraper reads (a news source, a YouTube channel, an events feed) and how it is doing.</summary>
/// <param name="LastSeen">The scraper's last successful touch: the source's LastScrapedAt, or the newest LastSeenAt.</param>
/// <param name="Upcoming">Events feeds only: games from today onward.</param>
public sealed record HealthRow(
    int Id, string Name, bool Enabled, DateTime? LastSeen, int ItemCount, int? Upcoming, string? Error,
    HealthLevel Level, string? Note);

/// <param name="LastRun">The newest write the scraper made to any table, or null when it has written nothing.</param>
/// <param name="UnlinkedVideos">Videos with no YoutubeVideoFeedId (the scraper does not set it yet).</param>
/// <param name="UnlinkedEvents">Events with no SportsEventsTypeId (the scraper does not set it yet).</param>
public sealed record ScraperHealthReport(
    DateTime CheckedAt, TimeSpan StaleAfter, DateTime? LastRun,
    IReadOnlyList<HealthRow> Sources, IReadOnlyList<HealthRow> Channels, IReadOnlyList<HealthRow> EventFeeds,
    int UnlinkedVideos, int UnlinkedEvents)
{
    /// <summary>The scraper as a whole has not written anything for longer than the threshold.</summary>
    public bool Stale => ScraperHealth.IsStale(LastRun, CheckedAt, StaleAfter);

    public IEnumerable<HealthRow> All => Sources.Concat(Channels).Concat(EventFeeds);

    public int Problems => All.Count(r => r.Level == HealthLevel.Problem);

    public int Warnings => All.Count(r => r.Level == HealthLevel.Warning);
}

/// <summary>
/// Whether the scraper is keeping the database fresh. Every table it fills is checked on its own, because one pipeline
/// (news, videos or events) can stop while the others carry on. Everything here is read-only.
/// </summary>
public static class ScraperHealth
{
    /// <summary>The scraper runs hourly, so three hours without a write means at least two missed runs.</summary>
    public static readonly TimeSpan DefaultStaleAfter = TimeSpan.FromHours(3);

    /// <summary>Reads Scraper:StaleAfterHours. A missing, unreadable or non-positive value gives the default.</summary>
    public static TimeSpan StaleAfter(string? configuredHours) =>
        double.TryParse(configuredHours, NumberStyles.Float, CultureInfo.InvariantCulture, out var hours) && hours > 0
            ? TimeSpan.FromHours(hours)
            : DefaultStaleAfter;

    public static bool IsStale(DateTime? lastSeen, DateTime nowUtc, TimeSpan staleAfter) =>
        lastSeen is null || nowUtc - lastSeen.Value > staleAfter;

    /// <summary>A rough age for people: "just now", "12 minutes", "5 hours", "7 days".</summary>
    public static string Age(TimeSpan age)
    {
        if (age < TimeSpan.FromMinutes(1))
        {
            return "just now";
        }

        static string Plural(int n, string unit) => $"{n} {unit}{(n == 1 ? "" : "s")}";
        return age.TotalHours < 1 ? Plural((int)age.TotalMinutes, "minute")
            : age.TotalDays < 2 ? Plural((int)age.TotalHours, "hour")
            : Plural((int)age.TotalDays, "day");
    }

    private static string NotRefreshed(DateTime lastSeen, DateTime nowUtc) => $"Not refreshed for {Age(nowUtc - lastSeen)}";

    /// <summary>
    /// A news source. Disabled sources are never a problem. An error from the last attempt beats everything else, then
    /// staleness; a source that is fetched but has no articles, or has no label (it shows as its address), is a warning.
    /// </summary>
    public static (HealthLevel, string?) ClassifySource(
        bool enabled, bool hasLabel, DateTime? lastScraped, string? lastError, int pages, DateTime nowUtc, TimeSpan staleAfter)
    {
        if (!enabled)
        {
            return (HealthLevel.Ok, "Disabled");
        }

        if (!string.IsNullOrWhiteSpace(lastError))
        {
            return (HealthLevel.Problem, "Last scrape failed");
        }

        if (lastScraped is null)
        {
            return (HealthLevel.Problem, "Never scraped");
        }

        if (IsStale(lastScraped, nowUtc, staleAfter))
        {
            return (HealthLevel.Problem, NotRefreshed(lastScraped.Value, nowUtc));
        }

        if (pages == 0)
        {
            return (HealthLevel.Warning, "Scraped, but no articles");
        }

        return hasLabel ? (HealthLevel.Ok, null) : (HealthLevel.Warning, "No label (shown as its address)");
    }

    /// <summary>
    /// A YouTube channel or an events feed. Neither has an error column, so the only signals are whether the scraper has
    /// collected anything and how long ago it last saw an item. <paramref name="upcoming"/> (events only) of zero is a
    /// warning, not a problem: an off-season feed is normal.
    /// </summary>
    public static (HealthLevel, string?) ClassifyFeed(DateTime? lastSeen, int items, int? upcoming, DateTime nowUtc, TimeSpan staleAfter)
    {
        if (items == 0 || lastSeen is null)
        {
            return (HealthLevel.Problem, "Nothing collected yet");
        }

        if (IsStale(lastSeen, nowUtc, staleAfter))
        {
            return (HealthLevel.Problem, NotRefreshed(lastSeen.Value, nowUtc));
        }

        return upcoming == 0 ? (HealthLevel.Warning, "No upcoming games") : (HealthLevel.Ok, null);
    }

    private static int Rank(HealthLevel level) => level switch { HealthLevel.Problem => 0, HealthLevel.Warning => 1, _ => 2 };

    private static IReadOnlyList<HealthRow> Ordered(IEnumerable<HealthRow> rows) =>
        rows.OrderBy(r => Rank(r.Level)).ThenBy(r => !r.Enabled).ThenBy(r => r.Name, StringComparer.OrdinalIgnoreCase).ToList();

    /// <param name="today">Today's date in the display zone, compared with EventDate directly (never converted).</param>
    public static async Task<ScraperHealthReport> GetReportAsync(
        WebScraperContext db, DateTime nowUtc, DateOnly today, TimeSpan staleAfter)
    {
        // The queries run one after another: one DbContext cannot run two at once.
        var sources = await db.Sources.AsNoTracking()
            .Select(s => new { s.Id, s.Label, s.Url, s.Enabled, s.LastScrapedAt, s.LastError, Pages = s.Pages.Count })
            .ToListAsync();

        // Videos are matched to their channel by ChannelId, so videos the scraper has not linked yet still count.
        var channels = await db.YoutubeVideoFeeds.AsNoTracking()
            .Select(f => new
            {
                f.Id,
                f.ChannelId,
                f.ChannelName,
                Videos = db.YouTubeVideos.Count(v => v.ChannelId == f.ChannelId),
                LastSeen = db.YouTubeVideos.Where(v => v.ChannelId == f.ChannelId).Max(v => (DateTime?)v.LastSeenAt),
            })
            .ToListAsync();

        var feeds = await db.SportsEventsTypes.AsNoTracking()
            .Select(t => new
            {
                t.Id,
                t.EventsTypeName,
                Events = t.SportsEvents.Count,
                Upcoming = t.SportsEvents.Count(e => e.EventDate >= today),
                LastSeen = t.SportsEvents.Max(e => (DateTime?)e.LastSeenAt),
            })
            .ToListAsync();

        var unlinkedVideos = await db.YouTubeVideos.CountAsync(v => v.YoutubeVideoFeedId == null);
        var unlinkedEvents = await db.SportsEvents.CountAsync(e => e.SportsEventsTypeId == null);

        // The overall "last run" looks at every table, linked or not, so it never depends on the link columns.
        var lastRun = new[]
        {
            await db.Sources.MaxAsync(s => s.LastScrapedAt),
            await db.YouTubeVideos.MaxAsync(v => (DateTime?)v.LastSeenAt),
            await db.SportsEvents.MaxAsync(e => (DateTime?)e.LastSeenAt),
        }.Max();

        var sourceRows = sources.Select(s =>
        {
            var (level, note) = ClassifySource(
                s.Enabled, !string.IsNullOrWhiteSpace(s.Label), s.LastScrapedAt, s.LastError, s.Pages, nowUtc, staleAfter);
            return new HealthRow(s.Id, FeedText.SourceName(s.Label, s.Url), s.Enabled, s.LastScrapedAt, s.Pages, null,
                s.Enabled ? s.LastError : null, level, note);
        });

        var channelRows = channels.Select(c =>
        {
            var (level, note) = ClassifyFeed(c.LastSeen, c.Videos, null, nowUtc, staleAfter);
            var name = string.IsNullOrWhiteSpace(c.ChannelName) ? c.ChannelId : c.ChannelName.Trim();
            return new HealthRow(c.Id, name, true, c.LastSeen, c.Videos, null, null, level, note);
        });

        var feedRows = feeds.Select(f =>
        {
            var (level, note) = ClassifyFeed(f.LastSeen, f.Events, f.Upcoming, nowUtc, staleAfter);
            return new HealthRow(f.Id, f.EventsTypeName, true, f.LastSeen, f.Events, f.Upcoming, null, level, note);
        });

        return new ScraperHealthReport(nowUtc, staleAfter, lastRun,
            Ordered(sourceRows), Ordered(channelRows), Ordered(feedRows), unlinkedVideos, unlinkedEvents);
    }
}
