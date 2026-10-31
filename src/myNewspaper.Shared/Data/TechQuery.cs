using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using myNewspaper.Web.Profiles;

namespace myNewspaper.Web.Data;

public sealed record TechBriefing(int Technology, int VideoGames, int Linux, int Videos);

public sealed record SourceBadge(string Name, string Mark, string? Category);

public sealed record CoveredStory(FeedItem Lead, IReadOnlyList<FeedItem> Others, int Sources)
{
    private IReadOnlyList<SourceBadge>? badges;

    public IReadOnlyList<SourceBadge> SourceMarks => badges ??= new[] { Lead }.Concat(Others)
        .Select(i => (Name: TechQuery.SourceKey(i), Category: i.CategoryName))
        .DistinctBy(x => x.Name, StringComparer.OrdinalIgnoreCase)
        .Select(x => new SourceBadge(x.Name, FeedText.SourceMark(x.Name, 2), x.Category))
        .ToList();

    public IReadOnlyList<string> SourceNames => SourceMarks.Select(s => s.Name).ToList();
}

/// <summary>Read side of the Tech front (/tech) and Tech desks.</summary>
public static class TechQuery
{
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
        WebScraperContext db, TimeSpan window, int minSources, int take, string? category = null,
        CancellationToken ct = default) =>
        MostCoveredAsync(db, FallbackCache, window, minSources, take, category, ct);

    public static async Task<IReadOnlyList<CoveredStory>> MostCoveredAsync(
        WebScraperContext db, IMemoryCache? cache, TimeSpan window, int minSources, int take,
        string? category = null, CancellationToken ct = default)
    {
        var key = $"tech-covered:{category}:{window}";
        var store = cache ?? FallbackCache;
        if (!store.TryGetValue(key, out IReadOnlyList<CoveredStory>? all) || all is null)
        {
            var since = DateTime.UtcNow - window;
            var items = await FeedQuery.VisibleSince(db, since, "tech", category)
                .OrderByDescending(p => p.Published ?? p.ScrapedAt)
                .ThenByDescending(p => p.Id)
                .Take(400)
                .Select(FeedQuery.ToFeedItem)
                .ToListAsync(ct);

            all = GroupMostCovered(items, minSources: 2, take: int.MaxValue);
            store.Set(key, all, TimeSpan.FromMinutes(10));
        }

        return all.Where(c => c.Sources >= minSources).Take(Math.Max(0, take)).ToList();
    }

    public static async Task<IReadOnlyList<VideoItem>> VideosAsync(
        WebScraperContext db, int take, CancellationToken ct = default)
    {
        var page = await VideosQuery.GetVideosAsync(
            db, Viewer.Household, channelId: null, search: null, limit: take, offset: 0,
            categories: VideoCategoryTabs);
        return page.Items;
    }

    /// <summary>
    /// Groups headlines from the same window that cover the same story across at least <paramref name="minSources"/>
    /// distinct sources. Pure function over in-memory items for direct unit testing.
    /// </summary>
    public static IReadOnlyList<CoveredStory> GroupMostCovered(
        IEnumerable<FeedItem> items, int minSources, int take = int.MaxValue)
    {
        var list = items.ToList();
        if (list.Count == 0 || take <= 0) return [];

        var keys = list.Select(i => KeySet(i.Title)).ToArray();
        var sources = list.Select(SourceKey).ToArray();
        var parent = Enumerable.Range(0, list.Count).ToArray();

        int Find(int x)
        {
            while (parent[x] != x)
            {
                parent[x] = parent[parent[x]];
                x = parent[x];
            }
            return x;
        }

        void Union(int a, int b)
        {
            var ra = Find(a);
            var rb = Find(b);
            if (ra != rb) parent[rb] = ra;
        }

        for (var i = 0; i < list.Count; i++)
        {
            if (keys[i].Count == 0) continue;
            for (var j = i + 1; j < list.Count; j++)
            {
                if (keys[j].Count == 0) continue;
                if (string.Equals(sources[i], sources[j], StringComparison.OrdinalIgnoreCase)) continue;
                if (Matches(keys[i], keys[j])) Union(i, j);
            }
        }

        var results = new List<(CoveredStory Story, DateTime Newest)>();
        foreach (var group in Enumerable.Range(0, list.Count).GroupBy(Find))
        {
            var members = group.Select(idx => list[idx]).ToList();
            var distinctSources = members.Select(SourceKey).Distinct(StringComparer.OrdinalIgnoreCase).Count();
            if (distinctSources < minSources) continue;

            var lead = members
                .Where(m => !string.IsNullOrWhiteSpace(m.Description))
                .OrderBy(m => m.Undated ? 1 : 0).ThenBy(m => m.Date).ThenBy(m => m.Id)
                .FirstOrDefault()
                ?? members
                    .OrderBy(m => m.Undated ? 1 : 0).ThenBy(m => m.Date).ThenBy(m => m.Id)
                    .First();

            var others = members
                .Where(m => m != lead)
                .OrderByDescending(m => m.Date).ThenByDescending(m => m.Id)
                .ToList();

            var newest = members.Max(m => m.Date);
            results.Add((new CoveredStory(lead, others, distinctSources), newest));
        }

        return results
            .OrderByDescending(r => r.Story.Sources)
            .ThenByDescending(r => r.Newest)
            .ThenByDescending(r => r.Story.Lead.Id)
            .Take(take)
            .Select(r => r.Story)
            .ToList();
    }

    internal static HashSet<string> KeySet(string? title)
    {
        var set = new HashSet<string>(StringComparer.Ordinal);
        if (string.IsNullOrWhiteSpace(title)) return set;

        var cleaned = Regex.Replace(title.ToLowerInvariant(), @"['’]", "");
        foreach (var raw in Regex.Split(cleaned, @"[^a-z0-9]+"))
        {
            if (raw.Length == 0 || StopWords.Contains(raw)) continue;
            var hasDigit = raw.Any(char.IsAsciiDigit);
            var letterCount = raw.Count(char.IsAsciiLetter);
            if (!hasDigit && letterCount < 3) continue;

            var token = raw.Length > 4 && raw.EndsWith('s') && !hasDigit ? raw[..^1] : raw;
            if (!StopWords.Contains(token)) set.Add(token);
        }

        return set;
    }

    internal static bool Matches(HashSet<string> a, HashSet<string> b)
    {
        var shared = 0;
        var sharedDigits = 0;
        var sharedWords = 0;

        foreach (var token in a)
        {
            if (!b.Contains(token)) continue;
            shared++;
            if (token.Any(char.IsAsciiDigit)) sharedDigits++;
            else sharedWords++;
        }

        if (shared == 0) return false;
        var union = a.Count + b.Count - shared;
        var jaccard = (double)shared / union;
        return jaccard >= 0.34 || (sharedDigits >= 2 && sharedWords >= 1);
    }

    internal static string SourceKey(FeedItem item)
    {
        var cleaned = FeedText.SourceName(item.SourceLabel, item.SourceUrl);
        return !string.IsNullOrWhiteSpace(cleaned) ? cleaned
            : !string.IsNullOrWhiteSpace(item.SourceLabel) ? item.SourceLabel.Trim()
            : item.SourceUrl.Trim();
    }
}
