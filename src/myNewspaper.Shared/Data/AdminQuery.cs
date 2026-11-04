using Microsoft.EntityFrameworkCore;

namespace myNewspaper.Shared.Data;

/// <summary>A category in a pick list: its id, name and the hub it belongs to.</summary>
public sealed record CategoryOption(int Id, string Name, string Hub);

/// <summary>A source in a pick list.</summary>
public sealed record SourceOption(int Id, string Name);

/// <summary>A row of Admin · Sources.</summary>
public sealed record AdminSourceRow(
    int Id, string Url, string? Label, bool Enabled, DateTime? LastScrapedAt, string? LastError, int PageCount, string? CategoryName)
{
    public string Name => string.IsNullOrWhiteSpace(Label) ? Url : Label;
    public bool HasProblem => Enabled && !string.IsNullOrEmpty(LastError);
}

/// <summary>One page of Admin · Sources, with the status counts for the current search and category.</summary>
public sealed record AdminSourcePage(
    IReadOnlyList<AdminSourceRow> Rows, int Total, int PageIndex, int All, int Enabled, int Disabled, int Errors);

/// <summary>A source as its edit page shows it.</summary>
public sealed record AdminSourceDetail(
    int Id, string Url, string? Label, bool Enabled, int? SourceCategoryId, DateTime CreatedAt, DateTime? LastScrapedAt,
    string? LastError, int PageCount, DateTime? NewestPublished);

/// <summary>A row of Admin · Categories.</summary>
public sealed record AdminCategoryRow(int Id, string Name, DateTime DateAdded, int SourceCount, string Hub);

/// <summary>A row of Admin · Pages.</summary>
public sealed record AdminPageRow(int Id, string Title, string Url, string SourceName, int SourceId, DateTime? Published, DateTime ScrapedAt);

/// <summary>One page of Admin · Pages.</summary>
public sealed record AdminPageList(IReadOnlyList<AdminPageRow> Rows, int Total, int PageIndex);

/// <summary>A scraped page as its edit page shows it.</summary>
public sealed record AdminPageDetail(int Id, string Url, string Title, string? Description, DateTime? Published, DateTime ScrapedAt, string SourceName);

/// <summary>
/// Reads for the admin site's pages. Writes are in <see cref="AdminStore"/>. Every list is bounded by a page size or by
/// the size of a configuration table (categories, sources).
/// </summary>
public static class AdminQuery
{
    // ---- Categories and sources ----------------------------------------------------------------------------------

    public static async Task<IReadOnlyList<CategoryOption>> CategoryOptionsAsync(WebScraperContext db, CancellationToken ct = default) =>
        await db.SourceCategories.AsNoTracking().OrderBy(c => c.CategoryName)
            .Select(c => new CategoryOption(c.Id, c.CategoryName, c.NewsHub)).ToListAsync(ct);

    public static async Task<IReadOnlyList<AdminCategoryRow>> CategoriesAsync(WebScraperContext db, CancellationToken ct = default) =>
        await db.SourceCategories.AsNoTracking().OrderBy(c => c.CategoryName)
            .Select(c => new AdminCategoryRow(c.Id, c.CategoryName, c.DateAdded, c.Sources.Count, c.NewsHub)).ToListAsync(ct);

    public static async Task<IReadOnlyList<SourceOption>> SourceOptionsAsync(WebScraperContext db, CancellationToken ct = default) =>
        await db.Sources.AsNoTracking().OrderBy(s => s.Label ?? s.Url).Select(s => new SourceOption(s.Id, s.Label ?? s.Url)).ToListAsync(ct);

    /// <summary>
    /// Admin · Sources. <paramref name="category"/> is "none" (uncategorized), a category id, or empty; <paramref name="status"/>
    /// is "enabled", "disabled", "errors" or empty. The page index is clamped to the last page.
    /// </summary>
    public static async Task<AdminSourcePage> SourcesAsync(WebScraperContext db, string? category, string? term, string? status,
        int pageIndex, int pageSize, CancellationToken ct = default)
    {
        IQueryable<Models.Source> query = db.Sources.AsNoTracking();
        if (category == "none") query = query.Where(s => s.SourceCategoryId == null);
        else if (int.TryParse(category, out var categoryId)) query = query.Where(s => s.SourceCategoryId == categoryId);
        term = term?.Trim();
        if (!string.IsNullOrEmpty(term)) query = query.Where(s => s.Url.Contains(term) || (s.Label != null && s.Label.Contains(term)));

        // Counts for the status buttons, within the search and category.
        var all = await query.CountAsync(ct);
        var enabled = await query.CountAsync(s => s.Enabled, ct);
        var errors = await query.CountAsync(s => s.Enabled && s.LastError != null && s.LastError != "", ct);

        query = status switch
        {
            "enabled" => query.Where(s => s.Enabled),
            "disabled" => query.Where(s => !s.Enabled),
            "errors" => query.Where(s => s.Enabled && s.LastError != null && s.LastError != ""),
            _ => query,
        };
        var total = await query.CountAsync(ct);
        pageIndex = Math.Min(Math.Max(0, pageIndex), Math.Max(0, (total - 1) / pageSize));
        var rows = await query
            .OrderBy(s => s.Label ?? s.Url).ThenBy(s => s.Id)
            .Skip(pageIndex * pageSize).Take(pageSize)
            .Select(s => new AdminSourceRow(s.Id, s.Url, s.Label, s.Enabled, s.LastScrapedAt, s.LastError, s.Pages.Count,
                s.SourceCategory != null ? s.SourceCategory.CategoryName : null))
            .ToListAsync(ct);
        return new(rows, total, pageIndex, all, enabled, all - enabled, errors);
    }

    /// <summary>A source with its page count and newest page date, or null when it doesn't exist.</summary>
    public static async Task<AdminSourceDetail?> SourceAsync(WebScraperContext db, int id, CancellationToken ct = default)
    {
        var source = await db.Sources.AsNoTracking().Where(s => s.Id == id)
            .Select(s => new { s.Id, s.Url, s.Label, s.Enabled, s.SourceCategoryId, s.CreatedAt, s.LastScrapedAt, s.LastError })
            .FirstOrDefaultAsync(ct);
        if (source is null) return null;
        var pageCount = await db.Pages.CountAsync(p => p.SourceId == id, ct);
        var newest = await db.Pages.Where(p => p.SourceId == id).MaxAsync(p => p.Published, ct);
        return new(source.Id, source.Url, source.Label, source.Enabled, source.SourceCategoryId, source.CreatedAt, source.LastScrapedAt,
            source.LastError, pageCount, newest);
    }

    // ---- Pages ---------------------------------------------------------------------------------------------------

    /// <summary>
    /// Admin · Pages, sorted by "title", "published" or (default) "scraped". Id breaks ties so paging is stable when
    /// the sort column has duplicates or nulls. The page index is clamped to the last page.
    /// </summary>
    public static async Task<AdminPageList> PagesAsync(WebScraperContext db, int? sourceId, string? search, string sortColumn,
        bool descending, int pageIndex, int pageSize, CancellationToken ct = default)
    {
        IQueryable<Models.Page> query = db.Pages.AsNoTracking();
        if (sourceId is { } id) query = query.Where(p => p.SourceId == id);
        if (!string.IsNullOrEmpty(search))
            query = query.Where(p => p.Title.Contains(search) || p.Url.Contains(search) || (p.Description != null && p.Description.Contains(search)));

        var total = await query.CountAsync(ct);
        var ordered = (sortColumn, descending) switch
        {
            ("title", false) => query.OrderBy(p => p.Title).ThenBy(p => p.Id),
            ("title", true) => query.OrderByDescending(p => p.Title).ThenBy(p => p.Id),
            ("published", false) => query.OrderBy(p => p.Published).ThenBy(p => p.Id),
            ("published", true) => query.OrderByDescending(p => p.Published).ThenBy(p => p.Id),
            (_, false) => query.OrderBy(p => p.ScrapedAt).ThenBy(p => p.Id),
            _ => query.OrderByDescending(p => p.ScrapedAt).ThenBy(p => p.Id),
        };
        pageIndex = Math.Min(Math.Max(0, pageIndex), Math.Max(0, (total - 1) / pageSize));
        var rows = await ordered.Skip(pageIndex * pageSize).Take(pageSize)
            .Select(p => new AdminPageRow(p.Id, p.Title, p.Url, p.Source.Label ?? p.Source.Url, p.SourceId, p.Published, p.ScrapedAt))
            .ToListAsync(ct);
        return new(rows, total, pageIndex);
    }

    public static Task<AdminPageDetail?> PageAsync(WebScraperContext db, int id, CancellationToken ct = default) =>
        db.Pages.AsNoTracking().Where(p => p.Id == id)
            .Select(p => new AdminPageDetail(p.Id, p.Url, p.Title, p.Description, p.Published, p.ScrapedAt, p.Source.Label ?? p.Source.Url))
            .FirstOrDefaultAsync(ct);
}
