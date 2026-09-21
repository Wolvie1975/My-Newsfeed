using Microsoft.EntityFrameworkCore;

namespace MyNewsFeed.Web.Data;

public sealed record FeedItem(
    int Id,
    string Title,
    string Url,
    string? Description,
    string? ImageUrl,
    string? SourceLabel,
    string SourceUrl,
    int? CategoryId,
    string? CategoryName,
    DateTime Date,
    bool Undated = false);   // Date is the sort date (the publish date, or the scrape date when there is none)

public sealed record FeedCategory(int Id, string Name, int Count);

/// <summary>
/// A position in the feed: the sort date and id of the last article already shown. Paging by position (not by page
/// number) means articles the scraper adds while someone is scrolling cannot cause duplicates or skips.
/// </summary>
public sealed record FeedCursor(DateTime Date, int Id, bool Undated = false)
{
    public string ToToken() => $"{(Undated ? 1 : 0)}.{Date.Ticks}.{Id}";

    public static FeedCursor? Parse(string? token)
    {
        var parts = token?.Split('.');

        // "flag.ticks.id"; the older "ticks.id" form (no flag) means a dated article.
        if (parts is { Length: 2 or 3 })
        {
            var offset = parts.Length - 2;
            var flag = offset == 1 ? parts[0] : "0";
            if ((flag is "0" or "1")
                && long.TryParse(parts[offset], out var ticks) && ticks is >= 0 and <= 3155378975999999999
                && int.TryParse(parts[offset + 1], out var id))
            {
                return new FeedCursor(new DateTime(ticks), id, flag == "1");
            }
        }

        return null;
    }
}

/// <param name="Total">Every article matching the filters.</param>
/// <param name="Remaining">Matching articles older than the last one in <paramref name="Items"/>.</param>
/// <param name="Next">Cursor for the next batch, or null when this batch reaches the end.</param>
public sealed record FeedPage(IReadOnlyList<FeedItem> Items, int Total, int Remaining, FeedCursor? Next)
{
    public int Shown => Total - Remaining;
}

/// <summary>Read side of the public news feed.</summary>
public static class FeedQuery
{
    /// <summary>First batch: the lead article plus twelve, which fills the three-column grid.</summary>
    public const int FirstBatch = 13;

    public const int MoreBatch = 12;

    // Descriptions are cut in SQL so one huge scraped description cannot bloat the page.
    private const int DescriptionFetchLimit = 600;

    // A page has no category of its own: it belongs to whatever category its source is in.

    /// <summary>Only pages from enabled sources are public; disabling a source hides what it collected.</summary>
    private static IQueryable<Models.Page> Visible(WebScraperContext db) =>
        db.Pages.AsNoTracking().Where(p => p.Source.Enabled);

    public static async Task<IReadOnlyList<FeedCategory>> GetCategoriesAsync(WebScraperContext db) =>
        await db.SourceCategories.AsNoTracking()
            .Select(c => new
            {
                c.Id,
                c.CategoryName,
                Count = db.Pages.Count(p => p.Source.Enabled && p.Source.SourceCategoryId == c.Id),
            })
            .Where(c => c.Count > 0)
            .OrderBy(c => c.CategoryName)
            .Select(c => new FeedCategory(c.Id, c.CategoryName, c.Count))
            .ToListAsync();

    /// <summary>
    /// When the feed was last refreshed: the most recent successful scrape of an enabled source. Sources whose last
    /// attempt failed are ignored, so a broken scraper shows an old time instead of a falsely fresh one.
    /// Null when nothing has been scraped yet.
    /// </summary>
    public static async Task<DateTime?> GetLastUpdatedAsync(WebScraperContext db) =>
        await db.Sources.AsNoTracking()
            .Where(s => s.Enabled && s.LastError == null)
            .MaxAsync(s => s.LastScrapedAt);

    /// <summary>
    /// Newest first by publish date. Articles with no publish date sort last, among themselves by scrape date (that is
    /// only the tiebreak; no date is shown for them). Returns up to
    /// <paramref name="take"/> articles that come after <paramref name="after"/> (or from the start when it is null).
    /// </summary>
    public static async Task<FeedPage> GetFeedAsync(
        WebScraperContext db, int? categoryId, string? search, FeedCursor? after, int take = FirstBatch)
    {
        var query = Visible(db);

        if (categoryId is int cid)
        {
            query = query.Where(p => p.Source.SourceCategoryId == cid);
        }

        var term = search?.Trim();
        if (!string.IsNullOrEmpty(term))
        {
            query = query.Where(p => p.Title.Contains(term)
                || (p.Description != null && p.Description.Contains(term)));
        }

        var total = await query.CountAsync();

        var older = query;
        var olderCount = total;
        if (after is { } cursor)
        {
            // Order is: dated articles newest first, then undated ones (newest scrape first). "After" means later in
            // that order, so an undated article comes after every dated one.
            var undated = cursor.Undated ? 1 : 0;
            var date = cursor.Date;
            var id = cursor.Id;
            older = query.Where(p => (p.Published == null ? 1 : 0) > undated
                || ((p.Published == null ? 1 : 0) == undated
                    && ((p.Published ?? p.ScrapedAt) < date
                        || ((p.Published ?? p.ScrapedAt) == date && p.Id < id))));
            olderCount = await older.CountAsync();
        }

        var items = await older
            .OrderBy(p => p.Published == null ? 1 : 0)
            .ThenByDescending(p => p.Published ?? p.ScrapedAt)
            .ThenByDescending(p => p.Id)
            .Take(take)
            .Select(p => new FeedItem(
                p.Id,
                p.Title,
                p.Url,
                p.Description != null && p.Description.Length > DescriptionFetchLimit
                    ? p.Description.Substring(0, DescriptionFetchLimit)
                    : p.Description,
                p.ImageUrl,
                p.Source.Label,
                p.Source.Url,
                p.Source.SourceCategoryId,
                p.Source.SourceCategory != null ? p.Source.SourceCategory.CategoryName : null,
                p.Published ?? p.ScrapedAt,
                p.Published == null))
            .ToListAsync();

        var remaining = olderCount - items.Count;
        var next = remaining > 0 && items.Count > 0 ? new FeedCursor(items[^1].Date, items[^1].Id, items[^1].Undated) : null;
        return new FeedPage(items, total, remaining, next);
    }

    /// <summary>
    /// Scraped URLs are untrusted: only http(s) may be used as a link target or image source, so a stored
    /// <c>javascript:</c> or <c>data:</c> URL can never become a clickable link.
    /// </summary>
    public static string? SafeHref(string? url) =>
        Uri.TryCreate(url, UriKind.Absolute, out var uri)
        && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps)
            ? uri.AbsoluteUri
            : null;

    /// <summary>Builds the query string that keeps the current filters. Empty when nothing is set.</summary>
    public static string QueryString(int? categoryId, string? search, string? after)
    {
        var parts = new List<string>();
        if (categoryId is int id)
        {
            parts.Add($"category={id}");
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            parts.Add($"q={Uri.EscapeDataString(search.Trim())}");
        }

        if (after is not null)
        {
            parts.Add($"after={after}");
        }

        return parts.Count == 0 ? "" : "?" + string.Join("&", parts);
    }

    /// <summary>Shortens text to at most about <paramref name="max"/> characters, preferring a word boundary.</summary>
    public static string? Excerpt(string? text, int max = 280)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return null;
        }

        text = text.Trim();
        if (text.Length <= max)
        {
            return text;
        }

        var cut = text.LastIndexOf(' ', max);
        return text[..(cut > max / 2 ? cut : max)].TrimEnd() + "…";
    }
}
