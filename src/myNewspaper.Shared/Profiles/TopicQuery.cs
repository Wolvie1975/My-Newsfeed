using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using myNewspaper.Web.Data;
using myNewspaper.Web.Data.Models;

namespace myNewspaper.Web.Profiles;

/// <summary>One part of a title for display: plain text, or a matched term to wrap in &lt;mark&gt;.</summary>
public sealed record TopicSpan(string Text, bool Match);

/// <summary>The follow form's live preview: past-week matches, per day (oldest first), and the three verdicts.</summary>
public sealed record TopicPreview(int Total, IReadOnlyList<(DateOnly Day, int Count)> PerDay, string Verdict,
    IReadOnlyList<FeedItem> Latest, int HiddenByExcept);

/// <summary>A followed topic with how many matches are new since it was last marked read.</summary>
public sealed record TopicSummary(ProfileTopic Topic, string Name, int New);

/// <summary>
/// Your topics (docs/news-section-handoff.md, phase 5): words or phrases a profile follows. Every method takes the
/// open profile's ID and only ever reads or changes that profile's rows; like FollowQuery, there is deliberately no way
/// to read another profile's topics. The admin sees counts only.
/// </summary>
public static class TopicQuery
{
    public const int Limit = 20;
    public const int MaxTerms = 5;
    public const int MaxExcept = 5;
    public const int PreviewDays = 7;
    public const int PageDays = 30;
    public const int PageSize = 20;
    public static readonly string[] Hubs = ["headlines", "sports", "entertainment"];

    public sealed class TooManyTopicsException() : Exception($"A profile can follow {Limit} topics.");

    /// <summary>Comma-separated alternatives, each 2–60 characters, without repeats (any capitals).</summary>
    public static string[] Split(string? text, int max) =>
        (text ?? "").Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)
            .Select(t => Regex.Replace(t, @"\s+", " "))
            .Where(t => t.Length is >= 2 and <= 60).Distinct(StringComparer.OrdinalIgnoreCase).Take(max).ToArray();

    /// <summary>
    /// Whole words, any capitals; a phrase must appear as written. "Whole" means no letter or digit just before or after,
    /// so terms that start or end with a symbol (C++, .NET) match too, which \b alone wouldn't allow.
    /// </summary>
    private static Regex Words(IEnumerable<string> terms) =>
        new(@"(?<![\w])(" + string.Join("|", terms.OrderByDescending(t => t.Length).Select(t => Regex.Escape(t).Replace(@"\ ", @"\s+"))) + @")(?![\w])",
            RegexOptions.IgnoreCase | RegexOptions.CultureInvariant, TimeSpan.FromMilliseconds(200));

    public static string NameOf(ProfileTopic topic) => string.Join(", ", Split(topic.Terms, MaxTerms));

    /// <summary>Matching stories since <paramref name="since"/>, newest first. Candidates come from SQL; whole-word and Except run here.</summary>
    public static async Task<(List<FeedItem> Items, int HiddenByExcept)> MatchAsync(
        WebScraperContext db, string? terms, string? except, string? hub, DateTime since, int cap = 2000, CancellationToken ct = default)
    {
        var include = Split(terms, MaxTerms);
        if (include.Length == 0) return ([], 0);
        var exclude = Split(except, MaxExcept);

        var query = FeedQuery.VisibleSince(db, since, hub);
        var candidates = new Dictionary<int, FeedItem>();
        // At most five small queries (SQL Server's collation already ignores capitals); this avoids building an OR.
        foreach (var term in include)
            foreach (var item in await query.Where(p => p.Title.Contains(term) || (p.Description != null && p.Description.Contains(term)))
                         .OrderByDescending(p => p.Published ?? p.ScrapedAt).Take(cap).Select(FeedQuery.ToFeedItem).ToListAsync(ct))
                candidates[item.Id] = item;

        var yes = Words(include);
        var no = exclude.Length > 0 ? Words(exclude) : null;
        var matched = candidates.Values.Where(i => yes.IsMatch(i.Title) || yes.IsMatch(i.Description ?? "")).ToList();
        var kept = no is null ? matched : matched.Where(i => !no.IsMatch(i.Title) && !no.IsMatch(i.Description ?? "")).ToList();
        return (kept.OrderByDescending(i => i.Date).ThenByDescending(i => i.Id).ToList(), matched.Count - kept.Count);
    }

    /// <summary>
    /// The verdict for a week's matches: none (grey, a spelling tip), broad (amber, suggestions; still followable) or
    /// good (green).
    /// </summary>
    public static string VerdictFor(int perWeek) => perWeek switch
    {
        0 => "none",
        > 100 => "broad",
        _ => "good",
    };

    public static async Task<TopicPreview> PreviewAsync(WebScraperContext db, FeedDates dates, string? terms, string? except, string? hub,
        DateTime nowUtc, CancellationToken ct = default)
    {
        var today = dates.TodayAt(nowUtc);
        var first = today.AddDays(1 - PreviewDays);
        var since = dates.StartOfDayUtc(first);
        var (items, hidden) = await MatchAsync(db, terms, except, ValidHub(hub), since, ct: ct);
        var perDay = Enumerable.Range(0, PreviewDays).Select(i => first.AddDays(i))
            .Select(day => (day, items.Count(item => dates.TodayAt(item.Date) == day))).ToList();
        return new(items.Count, perDay, VerdictFor(items.Count), items.Take(3).ToList(), hidden);
    }

    /// <summary>A title split around the topic's terms, for &lt;mark&gt;.</summary>
    public static IReadOnlyList<TopicSpan> Highlight(string text, string? terms)
    {
        var include = Split(terms, MaxTerms);
        if (include.Length == 0 || string.IsNullOrEmpty(text)) return [new(text, false)];
        var spans = new List<TopicSpan>();
        var at = 0;
        foreach (Match m in Words(include).Matches(text))
        {
            if (m.Index > at) spans.Add(new(text[at..m.Index], false));
            spans.Add(new(m.Value, true));
            at = m.Index + m.Length;
        }
        if (at < text.Length) spans.Add(new(text[at..], false));
        return spans;
    }

    public static string? ValidHub(string? hub) => Hubs.Contains(hub) ? hub : null;

    // ---------- The open profile's topics ----------

    public static Task<List<ProfileTopic>> ListAsync(WebScraperContext db, int profileId, CancellationToken ct = default) =>
        db.ProfileTopics.AsNoTracking().Where(t => t.ProfileId == profileId).OrderBy(t => t.CreatedAt).ThenBy(t => t.Id).ToListAsync(ct);

    public static Task<ProfileTopic?> GetAsync(WebScraperContext db, int profileId, int id, CancellationToken ct = default) =>
        db.ProfileTopics.AsNoTracking().FirstOrDefaultAsync(t => t.Id == id && t.ProfileId == profileId, ct);

    /// <summary>Where "new" starts for a topic: when it was last marked read, else when it was followed.</summary>
    public static DateTime NewSince(ProfileTopic topic) => topic.LastViewedAt ?? topic.CreatedAt;

    /// <summary>Each topic with its count of new matches (from the last 30 days at most).</summary>
    public static async Task<List<TopicSummary>> SummariesAsync(WebScraperContext db, int profileId, DateTime nowUtc, CancellationToken ct = default)
    {
        var floor = nowUtc.AddDays(-PageDays);
        var list = new List<TopicSummary>();
        foreach (var topic in await ListAsync(db, profileId, ct))
        {
            var since = NewSince(topic) > floor ? NewSince(topic) : floor;
            var (items, _) = await MatchAsync(db, topic.Terms, topic.ExceptTerms, topic.Hub, since, cap: 200, ct: ct);
            list.Add(new(topic, NameOf(topic), items.Count(i => i.Date > NewSince(topic))));
        }
        return list;
    }

    /// <summary>Checks the form's fields: at least one alternative, the Except words, and the section.</summary>
    public static (string Terms, string? Except, string? Hub) Normalize(string? terms, string? except, string? hub)
    {
        if ((terms?.Length ?? 0) > 300 || (except?.Length ?? 0) > 200) throw new ArgumentException("Too long.");
        var include = Split(terms, MaxTerms);
        if (include.Length == 0) throw new ArgumentException("Enter a word or phrase of 2 to 60 characters.");
        var exclude = Split(except, MaxExcept);
        if (!string.IsNullOrEmpty(hub) && ValidHub(hub) is null) throw new ArgumentException("Choose where to look.");
        return (string.Join(", ", include), exclude.Length > 0 ? string.Join(", ", exclude) : null, ValidHub(hub));
    }

    public static async Task<ProfileTopic> CreateAsync(WebScraperContext db, int profileId, string? terms, string? except, string? hub,
        DateTime nowUtc, CancellationToken ct = default)
    {
        var (t, x, h) = Normalize(terms, except, hub);
        if (await db.ProfileTopics.CountAsync(p => p.ProfileId == profileId, ct) >= Limit) throw new TooManyTopicsException();
        var topic = new ProfileTopic { ProfileId = profileId, Terms = t, ExceptTerms = x, Hub = h, CreatedAt = nowUtc };
        db.ProfileTopics.Add(topic);
        await db.SaveChangesAsync(ct);
        db.Entry(topic).State = EntityState.Detached;
        return topic;
    }

    public static async Task<bool> UpdateAsync(WebScraperContext db, int profileId, int id, string? terms, string? except, string? hub,
        CancellationToken ct = default)
    {
        var (t, x, h) = Normalize(terms, except, hub);
        return await db.ProfileTopics.Where(p => p.Id == id && p.ProfileId == profileId)
            .ExecuteUpdateAsync(s => s.SetProperty(p => p.Terms, t).SetProperty(p => p.ExceptTerms, x).SetProperty(p => p.Hub, h), ct) > 0;
    }

    public static async Task<bool> MarkReadAsync(WebScraperContext db, int profileId, int id, DateTime nowUtc, CancellationToken ct = default) =>
        await db.ProfileTopics.Where(p => p.Id == id && p.ProfileId == profileId)
            .ExecuteUpdateAsync(s => s.SetProperty(p => p.LastViewedAt, nowUtc), ct) > 0;

    public static async Task<bool> DeleteAsync(WebScraperContext db, int profileId, int id, CancellationToken ct = default) =>
        await db.ProfileTopics.Where(p => p.Id == id && p.ProfileId == profileId).ExecuteDeleteAsync(ct) > 0;

    /// <summary>How many topics a profile follows: the only thing the admin may see.</summary>
    public static Task<int> CountAsync(WebScraperContext db, int profileId, CancellationToken ct = default) =>
        db.ProfileTopics.CountAsync(p => p.ProfileId == profileId, ct);
}
