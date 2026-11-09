using Microsoft.EntityFrameworkCore;

namespace myNewspaper.Shared.Data;

public sealed record NewsPublisher(int Id, string Label, DateTime? LastChecked, bool Failed);
public sealed record BoxOfficePage(int? CategoryId, IReadOnlyList<NewsPublisher> Publishers, int? SelectedSource,
    FeedPage Feed);

public static class BoxOfficeQuery
{
    public static Task<int?> CategoryIdAsync(WebScraperContext db, CancellationToken ct = default) => db.SourceCategories.AsNoTracking()
        .Where(c => c.CategoryName == "Box Office").OrderBy(c => c.Id).Select(c => (int?)c.Id).FirstOrDefaultAsync(ct);
    public static string? Health(NewsPublisher source, DateTime nowUtc) => source.Failed
        ? "Latest collection failed; showing previously collected stories."
        : source.LastChecked is not { } last || nowUtc - DateTime.SpecifyKind(last, DateTimeKind.Utc) > TimeSpan.FromHours(2)
            ? "Story collection may be delayed." : null;
    public static async Task<BoxOfficePage> GetAsync(WebScraperContext db, int? sourceId, FeedCursor? after, CancellationToken ct = default)
    {
        var category = await CategoryIdAsync(db, ct);
        if (category is null) return new(null, [], null, new([], 0, 0, null));
        var sources = await db.Sources.AsNoTracking().Where(s => s.Enabled && s.SourceCategoryId == category)
            .OrderBy(s => s.Label).ThenBy(s => s.Id).Select(s => new { s.Id, s.Label, s.Url, s.LastScrapedAt, s.LastError }).ToListAsync(ct);
        var publishers = sources.Select(s => new NewsPublisher(s.Id, FeedText.SourceName(s.Label, s.Url),
            s.LastScrapedAt, !string.IsNullOrWhiteSpace(s.LastError))).ToList();
        var selected = publishers.Any(p => p.Id == sourceId) ? sourceId : null;
        var feed = await FeedQuery.GetFeedAsync(db, category, null, after,
            after is null ? FeedQuery.FirstBatch : FeedQuery.MoreBatch, selected, ct: ct);
        return new(category, publishers, selected, feed);
    }
}
