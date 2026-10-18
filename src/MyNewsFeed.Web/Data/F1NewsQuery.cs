using Microsoft.EntityFrameworkCore;

namespace MyNewsFeed.Web.Data;

public sealed record F1NewsPage(int? CategoryId, IReadOnlyList<FeedSource> Sources, int? SourceId, FeedPage Feed, DateTime? Updated);

public static class F1NewsQuery
{
    public const string CategoryName = "F1 News";
    public static Task<int?> CategoryIdAsync(WebScraperContext db) => db.SourceCategories.AsNoTracking()
        .Where(c => c.CategoryName == CategoryName && db.Sources.Any(s => s.Enabled && s.SourceCategoryId == c.Id))
        .Select(c => (int?)c.Id).FirstOrDefaultAsync();

    public static async Task<F1NewsPage> GetAsync(WebScraperContext db, int? requestedSource, string? search, FeedCursor? after)
    {
        var category = await CategoryIdAsync(db);
        // Never pass a missing fixed category to the general feed query: null means all categories there.
        if (category is null) return new(null, [], null, new([], 0, 0, null), null);
        var sources = await FeedQuery.GetSourcesAsync(db, category);
        var source = sources.Any(s => s.Id == requestedSource) ? requestedSource : null;
        var feed = await FeedQuery.GetFeedAsync(db, category, search, after,
            after is null ? FeedQuery.FirstBatch : FeedQuery.MoreBatch, source);
        var updated = await db.Sources.AsNoTracking().Where(s => s.Enabled && s.SourceCategoryId == category
            && (source == null || s.Id == source) && s.LastError == null).MaxAsync(s => s.LastScrapedAt);
        return new(category, sources, source, feed, updated);
    }
}
