using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace myNewspaper.Shared.Data;

public sealed record SourceBadge(string Name, string Mark, string? Category);

public sealed record CoveredStory(FeedItem Lead, IReadOnlyList<FeedItem> Others, int Sources)
{
    private IReadOnlyList<SourceBadge>? badges;

    public IReadOnlyList<SourceBadge> SourceMarks => badges ??= new[] { Lead }.Concat(Others)
        .Select(i => (Name: CoveredQuery.SourceKey(i), Category: i.CategoryName))
        .DistinctBy(x => x.Name, StringComparer.OrdinalIgnoreCase)
        .Select(x => new SourceBadge(x.Name, FeedText.SourceMark(x.Name, 2), x.Category))
        .ToList();

    public IReadOnlyList<string> SourceNames => SourceMarks.Select(s => s.Name).ToList();
}

/// <summary>Groups and queries stories covered by multiple independent sources across a hub.</summary>
public static partial class CoveredQuery
{
    private static readonly MemoryCache FallbackCache = new(new MemoryCacheOptions());

    private static readonly HashSet<string> StopWords = new(StringComparer.Ordinal)
    {
        "the", "a", "an", "and", "of", "to", "in", "for", "on", "with", "is", "are",
        "how", "why", "what", "your", "you", "this", "that", "it", "its", "new", "now",
        "after", "over", "from", "by", "at", "as", "be", "will",
    };

    [GeneratedRegex(@"['’]")]
    private static partial Regex ApostropheRegex();

    [GeneratedRegex(@"[^a-z0-9]+")]
    private static partial Regex NonAlphaNumericRegex();

    /// <summary>Reads recent stories in the hub and groups them by shared coverage.</summary>
    public static Task<IReadOnlyList<CoveredStory>> MostCoveredAsync(
        WebScraperContext db, IMemoryCache? cache, string hub, DateTime sinceUtc, int minSources, int take,
        string? category = null, CancellationToken ct = default)
    {
        var bucket = sinceUtc.Ticks / TimeSpan.FromMinutes(5).Ticks;
        var key = $"covered:{hub}:{category}:{bucket}";
        return MostCoveredCachedAsync(db, cache ?? FallbackCache, key, hub, sinceUtc, minSources, take, category, ct);
    }

    private static async Task<IReadOnlyList<CoveredStory>> MostCoveredCachedAsync(
        WebScraperContext db, IMemoryCache store, string key, string hub, DateTime sinceUtc, int minSources, int take,
        string? category, CancellationToken ct)
    {
        if (!store.TryGetValue(key, out IReadOnlyList<CoveredStory>? all) || all is null)
        {
            var items = await FeedQuery.VisibleSince(db, sinceUtc, hub, category)
                .OrderByDescending(p => p.Published ?? p.ScrapedAt)
                .ThenByDescending(p => p.Id)
                .Take(400)
                .Select(FeedQuery.ToFeedItem)
                .ToListAsync(ct);

            all = Group(items, minSources: 2, take: int.MaxValue);
            store.Set(key, all, TimeSpan.FromMinutes(10));
        }

        return all.Where(c => c.Sources >= minSources).Take(Math.Max(0, take)).ToList();
    }

    /// <summary>
    /// Groups headlines from the same window that cover the same story across at least <paramref name="minSources"/>
    /// distinct sources. Pure function over in-memory items for direct unit testing.
    /// </summary>
    public static IReadOnlyList<CoveredStory> Group(
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
            .ThenByDescending(r => 1 + r.Story.Others.Count)
            .ThenByDescending(r => r.Newest)
            .ThenByDescending(r => r.Story.Lead.Id)
            .Take(take)
            .Select(r => r.Story)
            .ToList();
    }

    public static HashSet<string> KeySet(string? title)
    {
        var set = new HashSet<string>(StringComparer.Ordinal);
        if (string.IsNullOrWhiteSpace(title)) return set;

        var cleaned = ApostropheRegex().Replace(title.ToLowerInvariant(), "");
        foreach (var raw in NonAlphaNumericRegex().Split(cleaned))
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

    public static bool Matches(HashSet<string> a, HashSet<string> b)
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

    public static string SourceKey(FeedItem item)
    {
        var cleaned = FeedText.SourceName(item.SourceLabel, item.SourceUrl);
        return !string.IsNullOrWhiteSpace(cleaned) ? cleaned
            : !string.IsNullOrWhiteSpace(item.SourceLabel) ? item.SourceLabel.Trim()
            : item.SourceUrl.Trim();
    }
}

