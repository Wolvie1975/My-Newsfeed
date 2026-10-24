using Microsoft.EntityFrameworkCore;

namespace myNewspaper.Web.Data;

public static class LocalNewsQuery
{
    // A dedicated enabled category takes precedence. Until registered, explicitly labelled
    // city-name matches reuse collected content; neither path falls back to an unfiltered feed.
    public static async Task<(FeedPage Feed, bool Dedicated)> GetAsync(WebScraperContext db, FeedCursor? after)
    {
        var category = await db.SourceCategories.AsNoTracking()
            .Where(c => c.CategoryName == "Kansas City News" && c.Sources.Any(s => s.Enabled))
            .Select(c => (int?)c.Id).FirstOrDefaultAsync();
        return (await FeedQuery.GetFeedAsync(db, category, category == null ? "Kansas City" : null, after, take: 12), category != null);
    }
}
