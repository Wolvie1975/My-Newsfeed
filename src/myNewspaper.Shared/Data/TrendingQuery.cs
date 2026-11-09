using System.Globalization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace myNewspaper.Shared.Data;

public sealed record TrendingPhrase(string Text, int Stories);

/// <summary>Finds the most-used names and phrases in today's news headlines.</summary>
public static class TrendingQuery
{
    private static readonly HashSet<string> StopPhrases = new(StringComparer.OrdinalIgnoreCase)
    {
        // Days
        "Monday", "Tuesday", "Wednesday", "Thursday", "Friday", "Saturday", "Sunday",
        "Mon", "Tue", "Wed", "Thu", "Fri", "Sat", "Sun",
        // Months
        "January", "February", "March", "April", "May", "June", "July", "August", "September", "October", "November", "December",
        "Jan", "Feb", "Mar", "Apr", "Jun", "Jul", "Aug", "Sep", "Sept", "Oct", "Nov", "Dec",
        // Countries / common abbreviations
        "U.S.", "US", "USA", "UK",
        // News meta words
        "New", "Watch", "Live", "Video", "Opinion", "Editorial", "Update", "Updates", "Breaking", "Latest",
        "Today", "Tonight", "Tomorrow", "Yesterday", "Exclusive", "Photos", "Report", "Reports",
        // Common network source names
        "CBS News", "BBC News", "NBC News", "ABC News", "Fox News", "Associated Press", "Reuters", "CNN", "NPR", "PBS",
    };

    private static readonly HashSet<string> Connectors = new(StringComparer.OrdinalIgnoreCase)
    {
        "of", "de", "&",
    };

    private static readonly char[] TrimPunctuation =
        ['"', '\'', '“', '”', '‘', '’', '(', ')', '[', ']', '{', '}', '<', '>', ',', '.', '!', '?', ':', ';', '—', '-'];

    /// <summary>Extracts trending capitalized names and phrases from the given feed items.</summary>
    public static IReadOnlyList<TrendingPhrase> Phrases(IEnumerable<FeedItem> items, int take = 6)
    {
        var list = items.ToList();
        if (list.Count == 0 || take <= 0) return [];

        var sourceLabels = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var item in list)
        {
            if (!string.IsNullOrWhiteSpace(item.SourceLabel))
                sourceLabels.Add(item.SourceLabel.Trim());
            var sName = FeedText.SourceName(item.SourceLabel, item.SourceUrl);
            if (!string.IsNullOrWhiteSpace(sName))
                sourceLabels.Add(sName.Trim());
        }

        // Map canonical lowercase phrase -> (Best original casing, Distinct story IDs)
        var phraseTracker = new Dictionary<string, (string Display, HashSet<int> StoryIds)>(StringComparer.OrdinalIgnoreCase);

        foreach (var item in list)
        {
            if (string.IsNullOrWhiteSpace(item.Title)) continue;

            var rawWords = item.Title.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries);
            var words = rawWords.Select(w => w.Trim(TrimPunctuation)).ToArray();
            var storyPhrases = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            for (var i = 0; i < words.Length; i++)
            {
                if (string.IsNullOrEmpty(words[i])) continue;

                // 1-word run (skip first word of the title)
                if (i > 0 && IsCapitalized(words[i]))
                {
                    TryAddPhrase(words[i], storyPhrases, sourceLabels);
                }

                // 2-word run
                if (i + 1 < words.Length && IsCapitalized(words[i]) && IsCapitalized(words[i + 1]))
                {
                    TryAddPhrase($"{words[i]} {words[i + 1]}", storyPhrases, sourceLabels);
                }

                // 3-word run (allows connectors inside: "Board of Education")
                if (i + 2 < words.Length && IsCapitalized(words[i]) && IsCapitalized(words[i + 2]))
                {
                    if (IsCapitalized(words[i + 1]) || Connectors.Contains(words[i + 1]))
                    {
                        TryAddPhrase($"{words[i]} {words[i + 1]} {words[i + 2]}", storyPhrases, sourceLabels);
                    }
                }
            }

            foreach (var phrase in storyPhrases)
            {
                if (!phraseTracker.TryGetValue(phrase, out var entry))
                {
                    entry = (phrase, new HashSet<int>());
                    phraseTracker[phrase] = entry;
                }
                entry.StoryIds.Add(item.Id);
            }
        }

        // Keep phrases in 3 or more stories
        var qualifying = phraseTracker.Values
            .Where(p => p.StoryIds.Count >= 3)
            .Select(p => new TrendingPhrase(p.Display, p.StoryIds.Count))
            .ToList();

        // When a phrase is inside a longer kept phrase with the same count ("Education" inside "Board of Education"), keep the longer one.
        var result = new List<TrendingPhrase>();
        for (var i = 0; i < qualifying.Count; i++)
        {
            var p1 = qualifying[i];
            var dominated = false;
            for (var j = 0; j < qualifying.Count; j++)
            {
                if (i == j) continue;
                var p2 = qualifying[j];
                if (p2.Stories == p1.Stories && IsSubPhrase(p1.Text, p2.Text))
                {
                    dominated = true;
                    break;
                }
            }

            if (!dominated)
            {
                result.Add(p1);
            }
        }

        return result
            .OrderByDescending(p => p.Stories)
            .ThenBy(p => p.Text, StringComparer.OrdinalIgnoreCase)
            .Take(take)
            .ToList();
    }

    private static void TryAddPhrase(string phrase, HashSet<string> storyPhrases, HashSet<string> sourceLabels)
    {
        if (StopPhrases.Contains(phrase) || sourceLabels.Contains(phrase)) return;
        storyPhrases.Add(phrase);
    }

    private static bool IsCapitalized(string word) =>
        word.Length > 0 && char.IsAsciiLetterUpper(word[0]);

    private static bool IsSubPhrase(string shorter, string longer)
    {
        if (shorter.Length >= longer.Length) return false;
        var sWords = shorter.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        var lWords = longer.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (sWords.Length >= lWords.Length) return false;

        for (var i = 0; i <= lWords.Length - sWords.Length; i++)
        {
            var match = true;
            for (var j = 0; j < sWords.Length; j++)
            {
                if (!string.Equals(lWords[i + j], sWords[j], StringComparison.OrdinalIgnoreCase))
                {
                    match = false;
                    break;
                }
            }
            if (match) return true;
        }

        return false;
    }

    /// <summary>Reads recent headlines in the headlines hub and extracts trending phrases.</summary>
    public static async Task<IReadOnlyList<TrendingPhrase>> TodayAsync(
        WebScraperContext db, IMemoryCache cache, DateTime sinceUtc, int take, CancellationToken ct = default)
    {
        var bucket = sinceUtc.Ticks / TimeSpan.FromMinutes(15).Ticks;
        var key = $"trending:headlines:{bucket}:{take}";
        if (cache.TryGetValue<IReadOnlyList<TrendingPhrase>>(key, out var cached) && cached is not null)
            return cached;

        var items = await FeedQuery.VisibleSince(db, sinceUtc, "headlines")
            .OrderByDescending(p => p.Published ?? p.ScrapedAt)
            .ThenByDescending(p => p.Id)
            .Take(500)
            .Select(FeedQuery.ToFeedItem)
            .ToListAsync(ct);

        var phrases = Phrases(items, take);
        cache.Set(key, phrases, TimeSpan.FromMinutes(15));
        return phrases;
    }
}

