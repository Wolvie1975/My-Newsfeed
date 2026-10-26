using myNewspaper.Web.Data;

namespace myNewspaper.Tests;

public class AdminNavCountsTests
{
    private static HealthRow Row(HealthLevel level, bool enabled = true) =>
        new(1, "x", enabled, DateTime.UtcNow, 1, null, null, level, null);

    private static ScraperHealthReport Report(DateTime? lastRun, IReadOnlyList<HealthRow> sources, IReadOnlyList<HealthRow> channels,
        IReadOnlyList<HealthRow> feeds, IReadOnlyList<HealthRow>? tv = null, IReadOnlyList<HealthRow>? movies = null) =>
        new(DateTime.UtcNow, TimeSpan.FromHours(3), lastRun, sources, channels, feeds, 0, 0)
        {
            TvSeries = tv ?? [],
            MovieSources = movies ?? [],
        };

    [Fact]
    public void Counts_only_enabled_problems_per_area_and_add_one_for_a_stale_scraper()
    {
        var fresh = Report(DateTime.UtcNow.AddMinutes(-10),
            [Row(HealthLevel.Problem), Row(HealthLevel.Problem, enabled: false), Row(HealthLevel.Warning), Row(HealthLevel.Ok)],
            [Row(HealthLevel.Problem)], [], [Row(HealthLevel.Problem), Row(HealthLevel.Warning)], [Row(HealthLevel.Problem)]);
        Assert.Equal(new AdminNavCounts(4, 1, 1, 0, 1), AdminNavCounts.From(fresh));

        var stale = Report(DateTime.UtcNow.AddHours(-5), [], [], []);
        Assert.Equal(1, AdminNavCounts.From(stale).Total);
        Assert.Equal(1, AdminNavCounts.From(Report(null, [], [], [])).Total);
        Assert.Equal(AdminNavCounts.None, AdminNavCounts.From(Report(DateTime.UtcNow, [Row(HealthLevel.Ok)], [], [])));
    }
}
