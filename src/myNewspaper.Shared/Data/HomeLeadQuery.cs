namespace myNewspaper.Web.Data;

public sealed record HomeLeadSnapshot(FeedItem? Article, long Hour, int RefreshAfterMs, bool Failed = false);

public static class HomeLeadQuery
{
    public static async Task<HomeLeadSnapshot> GetAsync(WebScraperContext db, DateTime utcNow)
    {
        var candidates = await FeedQuery.GetLatestPerSourceAsync(db, int.MaxValue, hub: "headlines");
        return Snapshot(candidates, utcNow);
    }

    public static HomeLeadSnapshot Snapshot(IEnumerable<FeedItem> candidates, DateTime utcNow)
    {
        var hour = utcNow.Ticks / TimeSpan.TicksPerHour;
        var next = new DateTime((hour + 1) * TimeSpan.TicksPerHour, DateTimeKind.Utc);
        var sources = candidates
            .Where(item => item.CategoryName == "Top Headlines")
            .GroupBy(item => item.SourceUrl, StringComparer.Ordinal)
            .Select(group => group.OrderBy(item => item.Undated)
                .ThenByDescending(item => item.Date)
                .ThenByDescending(item => item.Id)
                .First())
            .OrderBy(item => item.SourceUrl, StringComparer.Ordinal)
            .ToArray();
        var article = sources.Length == 0 ? null : sources[(int)(hour % sources.Length)];
        return new(article, hour, (int)Math.Ceiling((next - utcNow).TotalMilliseconds));
    }
}
