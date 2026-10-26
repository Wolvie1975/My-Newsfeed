using System.Globalization;
using Microsoft.EntityFrameworkCore;
using myNewspaper.Web.Data.Models;

namespace myNewspaper.Web.Data;

public sealed record VideoItem(
    int Id,
    string VideoId,
    string ChannelId,
    string ChannelName,
    string Title,
    string Url,
    DateTime PublishedAt,
    string? ThumbnailUrl,
    long? ViewCount,
    string? Category = null);

public sealed record VideoChannel(string ChannelId, string Name);

/// <param name="Slug">The address form of the name ("news-politics"); "other" for channels without a category.</param>
public sealed record VideoCategory(string Slug, string Name);

/// <param name="Total">Every video matching the filters (the list itself is capped).</param>
public sealed record VideosPage(IReadOnlyList<VideoItem> Items, int Total);

/// <summary>Read side of the public videos page.</summary>
public static class VideosQuery
{
    /// <summary>A safety cap on one page. A channel feed only carries its latest videos, so this is generous.</summary>
    public const int Limit = 300;

    // The channel's name is the lookup table's when there is one; a video row's own copy is the fallback.
    private static string Name(string? feedName, string? videoName, string channelId) =>
        !string.IsNullOrWhiteSpace(feedName) ? feedName.Trim()
        : !string.IsNullOrWhiteSpace(videoName) ? videoName.Trim()
        : channelId;

    /// <summary>The tab for channels the scraper has not given a category yet.</summary>
    public static readonly VideoCategory Other = new("other", "Other");

    /// <summary>"News & Politics" → "news-politics": lower-case letters and digits joined by single hyphens.</summary>
    public static string Slug(string name)
    {
        var parts = new List<string>();
        var current = new System.Text.StringBuilder();
        foreach (var c in name.Trim().ToLowerInvariant())
        {
            if (char.IsAsciiLetterOrDigit(c)) current.Append(c);
            else if (current.Length > 0) { parts.Add(current.ToString()); current.Clear(); }
        }
        if (current.Length > 0) parts.Add(current.ToString());
        return string.Join('-', parts);
    }

    /// <summary>
    /// The YouTube category tabs: categories with at least one video, alphabetically, then "Other" when some videos
    /// have no category (a channel the scraper added but nobody has categorized yet). Never an empty tab.
    /// </summary>
    public static async Task<IReadOnlyList<VideoCategory>> GetCategoriesAsync(WebScraperContext db)
    {
        var names = await db.YoutubeCategories.AsNoTracking()
            .Where(c => c.YoutubeVideoFeeds.Any(f => f.YouTubeVideos.Count > 0))
            .Select(c => c.CategoryName)
            .ToListAsync();
        var list = names.Select(n => new VideoCategory(Slug(n), n.Trim()))
            .Where(c => c.Slug.Length > 0 && c.Slug != Other.Slug)
            .DistinctBy(c => c.Slug)
            .OrderBy(c => c.Name, StringComparer.OrdinalIgnoreCase)
            .ToList();
        if (await Uncategorized(db.YouTubeVideos.AsNoTracking()).AnyAsync()) list.Add(Other);
        return list;
    }

    private static IQueryable<YouTubeVideo> Uncategorized(IQueryable<YouTubeVideo> videos) =>
        videos.Where(v => v.YoutubeVideoFeed == null || v.YoutubeVideoFeed.YoutubeCategoryId == null);

    /// <summary>Keeps the videos in one tab; an unknown or empty tab keeps everything.</summary>
    private static IQueryable<YouTubeVideo> InCategory(IQueryable<YouTubeVideo> videos, VideoCategory? category) =>
        category is null ? videos
        : category == Other ? Uncategorized(videos)
        : videos.Where(v => v.YoutubeVideoFeed != null && v.YoutubeVideoFeed.YoutubeCategory != null
            && v.YoutubeVideoFeed.YoutubeCategory.CategoryName == category.Name);

    /// <summary>The channels that have videos (optionally in one category tab), alphabetically by their displayed name.</summary>
    public static async Task<IReadOnlyList<VideoChannel>> GetChannelsAsync(WebScraperContext db, VideoCategory? category = null)
    {
        var rows = await InCategory(db.YouTubeVideos.AsNoTracking(), category)
            .GroupBy(v => v.ChannelId)
            .Select(g => new
            {
                ChannelId = g.Key,
                Latest = g.Max(v => v.PublishedAt),
                FeedName = g.Select(v => v.YoutubeVideoFeed != null ? v.YoutubeVideoFeed.ChannelName : null).Max(),
                VideoName = g.Select(v => v.ChannelName).Max(),
            })
            .OrderByDescending(r => r.Latest).ThenBy(r => r.ChannelId)
            .ToListAsync();

        return rows.Select(r => new VideoChannel(r.ChannelId, Name(r.FeedName, r.VideoName, r.ChannelId)))
            .OrderBy(c => c.Name, StringComparer.OrdinalIgnoreCase).ThenBy(c => c.ChannelId, StringComparer.Ordinal).ToList();
    }

    /// <summary>When the videos were last refreshed by the scraper, or null when there are none.</summary>
    public static async Task<DateTime?> GetLastUpdatedAsync(WebScraperContext db) =>
        await db.YouTubeVideos.AsNoTracking().MaxAsync(v => (DateTime?)v.LastSeenAt);

    /// <summary>Newest first. Filters by category tab, channel id and a search over the title and channel name.</summary>
    public static async Task<VideosPage> GetVideosAsync(
        WebScraperContext db, string? channelId, string? search, int limit = Limit, int offset = 0,
        VideoCategory? category = null)
    {
        var query = InCategory(db.YouTubeVideos.AsNoTracking(), category);

        if (!string.IsNullOrWhiteSpace(channelId))
        {
            var wanted = channelId.Trim();
            query = query.Where(v => v.ChannelId == wanted);
        }

        var term = search?.Trim();
        if (!string.IsNullOrEmpty(term))
        {
            query = query.Where(v => v.Title.Contains(term)
                || (v.ChannelName != null && v.ChannelName.Contains(term))
                || (v.YoutubeVideoFeed != null && v.YoutubeVideoFeed.ChannelName != null
                    && v.YoutubeVideoFeed.ChannelName.Contains(term)));
        }

        var total = await query.CountAsync();
        var rows = await query
            .OrderByDescending(v => v.PublishedAt).ThenByDescending(v => v.Id)
            .Skip(Math.Max(0, offset)).Take(limit)
            .Select(v => new
            {
                v.Id,
                v.VideoId,
                v.ChannelId,
                v.ChannelName,
                v.Title,
                v.Url,
                v.PublishedAt,
                v.ThumbnailUrl,
                v.ViewCount,
                FeedName = v.YoutubeVideoFeed != null ? v.YoutubeVideoFeed.ChannelName : null,
                Category = v.YoutubeVideoFeed != null && v.YoutubeVideoFeed.YoutubeCategory != null
                    ? v.YoutubeVideoFeed.YoutubeCategory.CategoryName : null,
            })
            .ToListAsync();

        var items = rows.Select(r => new VideoItem(
            r.Id, r.VideoId, r.ChannelId, Name(r.FeedName, r.ChannelName, r.ChannelId), r.Title, r.Url, r.PublishedAt,
            r.ThumbnailUrl, r.ViewCount, r.Category?.Trim())).ToList();
        return new VideosPage(items, total);
    }

    /// <summary>Rotate up to ten channels when video collection advances to another UTC hour.</summary>
    public static async Task<IReadOnlyList<VideoItem>> GetHomeVideosAsync(WebScraperContext db, int take = 10)
    {
        var updated = await GetLastUpdatedAsync(db);
        var candidates = await GetLatestPerChannelAsync(db);
        return SelectHomeVideos(candidates, updated ?? DateTime.UnixEpoch, take);
    }

    public static IReadOnlyList<VideoItem> SelectHomeVideos(
        IEnumerable<VideoItem> candidates, DateTime lastCollectedAt, int take = 10)
    {
        var hour = lastCollectedAt.Ticks / TimeSpan.TicksPerHour;
        return candidates
            .GroupBy(item => item.ChannelId)
            .Select(group => group.OrderByDescending(item => item.PublishedAt).ThenByDescending(item => item.Id).First())
            .OrderBy(item => Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(
                System.Text.Encoding.UTF8.GetBytes(FormattableString.Invariant($"{hour}:{item.ChannelId}")))),
                StringComparer.Ordinal)
            .ThenBy(item => item.ChannelId, StringComparer.Ordinal)
            .Take(Math.Max(0, take))
            .OrderByDescending(item => item.PublishedAt)
            .ThenByDescending(item => item.Id)
            .ToList();
    }

    /// <summary>Each channel's newest video, newest first: one row per channel, for the home page.</summary>
    public static async Task<IReadOnlyList<VideoItem>> GetLatestPerChannelAsync(WebScraperContext db)
    {
        var rows = await db.YouTubeVideos.AsNoTracking()
            .Where(v => v.PublishedAt == db.YouTubeVideos.Where(o => o.ChannelId == v.ChannelId).Max(o => o.PublishedAt))
            .Select(v => new
            {
                v.Id,
                v.VideoId,
                v.ChannelId,
                v.ChannelName,
                v.Title,
                v.Url,
                v.PublishedAt,
                v.ThumbnailUrl,
                v.ViewCount,
                FeedName = v.YoutubeVideoFeed != null ? v.YoutubeVideoFeed.ChannelName : null,
            })
            .ToListAsync();

        // Two videos published at the same instant tie; keep the higher id so each channel appears once.
        return rows
            .GroupBy(r => r.ChannelId)
            .Select(g => g.OrderByDescending(r => r.Id).First())
            .OrderByDescending(r => r.PublishedAt).ThenBy(r => r.ChannelId)
            .Select(r => new VideoItem(
                r.Id, r.VideoId, r.ChannelId, Name(r.FeedName, r.ChannelName, r.ChannelId), r.Title, r.Url, r.PublishedAt,
                r.ThumbnailUrl, r.ViewCount))
            .ToList();
    }

    /// <summary>Builds the query string that keeps the current filters. Empty when nothing is set.</summary>
    public static string QueryString(string? channel, string? search)
    {
        var parts = new List<string>();
        if (!string.IsNullOrWhiteSpace(channel))
        {
            parts.Add($"channel={Uri.EscapeDataString(channel.Trim())}");
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            parts.Add($"q={Uri.EscapeDataString(search.Trim())}");
        }

        return parts.Count == 0 ? "" : "?" + string.Join("&", parts);
    }
}

/// <summary>Display-time text helpers for the videos page. The stored data is never changed.</summary>
public static class VideosText
{
    /// <summary>
    /// The picture for a video: YouTube's standard thumbnail built from the video id, which every video has, else the
    /// stored one. Stored addresses (hq720 and custom crops) can point at a grey "no image" placeholder that YouTube
    /// serves without an error, so they are only a fallback. <paramref name="size"/> is "mqdefault" (320 × 180, the
    /// grid) or "hqdefault" (480 × 360, the featured video).
    /// </summary>
    public static string? Thumbnail(string? stored, string? videoId, string size = "mqdefault") =>
        !string.IsNullOrWhiteSpace(videoId) && videoId.All(c => char.IsAsciiLetterOrDigit(c) || c is '-' or '_')
            ? $"https://i.ytimg.com/vi/{videoId}/{size}.jpg"
            : EventsText.LogoUrl(stored);

    /// <summary>"1,234 views", "12.3K views", "1.2M views"; null when the count is unknown.</summary>
    public static string? Views(long? count)
    {
        if (count is null or < 0)
        {
            return null;
        }

        var n = count.Value;
        string text = n switch
        {
            < 1_000 => n.ToString(CultureInfo.InvariantCulture),
            < 10_000 => n.ToString("N0", CultureInfo.InvariantCulture),
            < 1_000_000 => Trim(n / 1_000d) + "K",
            < 1_000_000_000 => Trim(n / 1_000_000d) + "M",
            _ => Trim(n / 1_000_000_000d) + "B",
        };
        return $"{text} {(n == 1 ? "view" : "views")}";

        // One decimal below 100 of the unit, none above; "12.0K" becomes "12K".
        static string Trim(double v) =>
            (v < 100 ? Math.Floor(v * 10) / 10 : Math.Floor(v)).ToString("0.#", CultureInfo.InvariantCulture);
    }

    /// <summary>The channel page for an id ("UC..."); the stored feed address is the RSS one.</summary>
    public static string? ChannelUrl(string channelId) =>
        !string.IsNullOrWhiteSpace(channelId) && channelId.All(c => char.IsAsciiLetterOrDigit(c) || c is '-' or '_')
            ? $"https://www.youtube.com/channel/{channelId}"
            : null;

    public static string Initial(string name)
    {
        var t = name.Trim();
        return t.Length == 0 ? "?" : char.ToUpperInvariant(t[0]).ToString();
    }
}

/// <summary>The YouTube page's featured carousel.</summary>
public static class VideoFeatured
{
    /// <summary>
    /// Up to <paramref name="count"/> videos for the carousel: the most watched from the newest two days of the list,
    /// at most one per channel, so a busy channel cannot fill it. Videos without a view count rank last; ties go to
    /// the newer video. The rest of the page lists the others newest first.
    /// </summary>
    public static IReadOnlyList<VideoItem> Pick(IReadOnlyList<VideoItem> items, int count = 5)
    {
        if (items.Count == 0) return [];
        var since = items.Max(v => v.PublishedAt).AddDays(-2);
        return items.Where(v => v.PublishedAt >= since)
            .OrderByDescending(v => v.ViewCount ?? -1).ThenByDescending(v => v.PublishedAt)
            .DistinctBy(v => v.ChannelId)
            .Take(count)
            .ToList();
    }
}
