using Microsoft.EntityFrameworkCore;

namespace MyNewsFeed.Web.Data;

public sealed record FeedItem(
    int Id, string Title, string Url, string? Description, string SourceName, string? CategoryName, DateTime Date);

public sealed record FeedCategory(int Id, string Name, int Count);

public sealed record FeedPage(IReadOnlyList<FeedItem> Items, int Total, int Page, int PageSize)
{
    public int TotalPages => Math.Max(1, (int)Math.Ceiling(Total / (double)PageSize));
}

/// <summary>Read side of the public news feed.</summary>
public static class FeedQuery
{
    public const int DefaultPageSize = 20;

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

    /// <summary>Newest first, by publish date when known and otherwise by when the page was scraped.</summary>
    public static async Task<FeedPage> GetFeedAsync(
        WebScraperContext db, int? categoryId, string? search, int page, int pageSize = DefaultPageSize)
    {
        var query = Visible(db);

        if (categoryId is int id)
        {
            query = query.Where(p => p.Source.SourceCategoryId == id);
        }

        var term = search?.Trim();
        if (!string.IsNullOrEmpty(term))
        {
            query = query.Where(p => p.Title.Contains(term)
                || (p.Description != null && p.Description.Contains(term)));
        }

        var total = await query.CountAsync();
        var lastPage = Math.Max(1, (int)Math.Ceiling(total / (double)pageSize));
        page = Math.Clamp(page, 1, lastPage);

        var items = await query
            .OrderByDescending(p => p.Published ?? p.ScrapedAt)
            .ThenByDescending(p => p.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(p => new FeedItem(
                p.Id,
                p.Title,
                p.Url,
                p.Description != null && p.Description.Length > DescriptionFetchLimit
                    ? p.Description.Substring(0, DescriptionFetchLimit)
                    : p.Description,
                p.Source.Label ?? p.Source.Url,
                p.Source.SourceCategory != null ? p.Source.SourceCategory.CategoryName : null,
                p.Published ?? p.ScrapedAt))
            .ToListAsync();

        return new FeedPage(items, total, page, pageSize);
    }

    /// <summary>
    /// Scraped URLs are untrusted: only http(s) may be used as a link target, so a stored
    /// <c>javascript:</c> or <c>data:</c> URL can never become a clickable link.
    /// </summary>
    public static string? SafeHref(string? url) =>
        Uri.TryCreate(url, UriKind.Absolute, out var uri)
        && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps)
            ? uri.AbsoluteUri
            : null;

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
