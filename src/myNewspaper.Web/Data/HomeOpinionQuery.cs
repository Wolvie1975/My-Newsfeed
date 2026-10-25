using Microsoft.EntityFrameworkCore;
namespace myNewspaper.Web.Data;

public sealed record HomeOpinion(int? CategoryId, FeedItem? Article, long Hour, int RefreshAfterMs, bool Failed = false);
public static class HomeOpinionQuery
{
    public static async Task<HomeOpinion> GetAsync(WebScraperContext db, DateTime utcNow)
    {
        var categoryId = await db.SourceCategories.Where(c => c.CategoryName == "Opinion")
            .Select(c => (int?)c.Id).SingleOrDefaultAsync();
        var candidates = categoryId == null ? [] : await FeedQuery.GetLatestPerSourceAsync(db, int.MaxValue, categoryId);
        return Snapshot(categoryId, candidates, utcNow);
    }
    public static HomeOpinion Snapshot(int? categoryId, IEnumerable<FeedItem> candidates, DateTime utcNow)
    {
        var hour = utcNow.Ticks / TimeSpan.TicksPerHour;
        var next = new DateTime((hour + 1) * TimeSpan.TicksPerHour, DateTimeKind.Utc);
        // Round-robin through stable source URLs: no repeated publisher until the cycle completes.
        var sources = candidates.GroupBy(c => c.SourceUrl, StringComparer.Ordinal)
            .Select(g => g.OrderBy(c => c.Undated).ThenByDescending(c => c.Date).ThenByDescending(c => c.Id).First())
            .OrderBy(c => c.SourceUrl, StringComparer.Ordinal).ToArray();
        var article = sources.Length == 0 ? null : sources[(int)(hour % sources.Length)];
        return new(categoryId, article, hour, (int)Math.Ceiling((next - utcNow).TotalMilliseconds));
    }
}
