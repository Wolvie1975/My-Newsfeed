namespace myNewspaper.Web.Data;

/// <summary>
/// The admin menu's red "needs attention" counts. Built from the same report as the Scraper status page, so the two
/// always agree. Only enabled rows with a Problem count; a stale or never-run scraper adds one to the total.
/// </summary>
public sealed record AdminNavCounts(int Total, int Sources, int Channels, int EventFeeds, int TvSeries)
{
    public static readonly AdminNavCounts None = new(0, 0, 0, 0, 0);

    public static AdminNavCounts From(ScraperHealthReport report)
    {
        static int Bad(IEnumerable<HealthRow> rows) => rows.Count(r => r.Enabled && r.Level == HealthLevel.Problem);
        var sources = Bad(report.Sources);
        var channels = Bad(report.Channels);
        var feeds = Bad(report.EventFeeds);
        var tv = Bad(report.TvSeries);
        var stale = report.LastRun is null || report.Stale ? 1 : 0;
        return new(sources + channels + feeds + tv + Bad(report.MovieSources) + stale, sources, channels, feeds, tv);
    }
}
