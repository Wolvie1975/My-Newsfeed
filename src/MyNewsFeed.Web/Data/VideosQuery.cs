using System.Globalization;
using Microsoft.EntityFrameworkCore;

namespace MyNewsFeed.Web.Data;

public sealed record VideoItem(
    int Id,
    string VideoId,
    string ChannelId,
    string ChannelName,
    string Title,
    string Url,
    DateTime PublishedAt,
    string? ThumbnailUrl,
    long? ViewCount);

public sealed record VideoChannel(string ChannelId, string Name);

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

    /// <summary>The channels that have videos, most recently published first.</summary>
    public static async Task<IReadOnlyList<VideoChannel>> GetChannelsAsync(WebScraperContext db)
    {
        var rows = await db.YouTubeVideos.AsNoTracking()
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

        return rows.Select(r => new VideoChannel(r.ChannelId, Name(r.FeedName, r.VideoName, r.ChannelId))).ToList();
    }

    /// <summary>When the videos were last refreshed by the scraper, or null when there are none.</summary>
    public static async Task<DateTime?> GetLastUpdatedAsync(WebScraperContext db) =>
        await db.YouTubeVideos.AsNoTracking().MaxAsync(v => (DateTime?)v.LastSeenAt);

    /// <summary>Newest first. Filters by channel id and by a search over the title and channel name.</summary>
    public static async Task<VideosPage> GetVideosAsync(
        WebScraperContext db, string? channelId, string? search, int limit = Limit)
    {
        var query = db.YouTubeVideos.AsNoTracking().AsQueryable();

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
            .Take(limit)
            .Select(v => new
            {
                v.Id, v.VideoId, v.ChannelId, v.ChannelName, v.Title, v.Url, v.PublishedAt, v.ThumbnailUrl, v.ViewCount,
                FeedName = v.YoutubeVideoFeed != null ? v.YoutubeVideoFeed.ChannelName : null,
            })
            .ToListAsync();

        var items = rows.Select(r => new VideoItem(
            r.Id, r.VideoId, r.ChannelId, Name(r.FeedName, r.ChannelName, r.ChannelId), r.Title, r.Url, r.PublishedAt,
            r.ThumbnailUrl, r.ViewCount)).ToList();
        return new VideosPage(items, total);
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
    /// <summary>The picture for a video: the stored thumbnail, else YouTube's standard one built from the video id.</summary>
    public static string? Thumbnail(string? stored, string? videoId)
    {
        var safe = EventsText.LogoUrl(stored);
        if (safe is not null)
        {
            return safe;
        }

        return !string.IsNullOrWhiteSpace(videoId) && videoId.All(c => char.IsAsciiLetterOrDigit(c) || c is '-' or '_')
            ? $"https://i.ytimg.com/vi/{videoId}/mqdefault.jpg"
            : null;
    }

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
