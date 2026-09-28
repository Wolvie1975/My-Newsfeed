using Microsoft.EntityFrameworkCore;
using MyNewsFeed.Web.Data;
using MyNewsFeed.Web.Data.Models;

namespace MyNewsFeed.Tests;

public class ScraperHealthTests
{
    private static readonly DateTime Now = new(2026, 9, 28, 17, 0, 0, DateTimeKind.Utc);
    private static readonly TimeSpan Threshold = TimeSpan.FromHours(3);

    // ---- pure logic: no database ------------------------------------------------------------------------------------

    [Theory]
    [InlineData(null, 3)]
    [InlineData("", 3)]
    [InlineData("abc", 3)]
    [InlineData("0", 3)]
    [InlineData("-2", 3)]
    [InlineData("6", 6)]
    [InlineData("0.5", 0.5)]
    public void The_threshold_comes_from_configuration_with_a_safe_default(string? configured, double hours) =>
        Assert.Equal(TimeSpan.FromHours(hours), ScraperHealth.StaleAfter(configured));

    [Theory]
    [InlineData(0, "just now")]
    [InlineData(59, "just now")]
    [InlineData(60, "1 minute")]
    [InlineData(12 * 60, "12 minutes")]
    [InlineData(60 * 60, "1 hour")]
    [InlineData(5 * 3600, "5 hours")]
    [InlineData(47 * 3600, "47 hours")]
    [InlineData(7 * 86400 + 4 * 3600, "7 days")]
    public void Ages_are_rough_and_readable(int seconds, string expected) =>
        Assert.Equal(expected, ScraperHealth.Age(TimeSpan.FromSeconds(seconds)));

    [Fact]
    public void Staleness_is_strictly_past_the_threshold_and_nothing_is_stale()
    {
        Assert.False(ScraperHealth.IsStale(Now - Threshold, Now, Threshold));
        Assert.True(ScraperHealth.IsStale(Now - Threshold - TimeSpan.FromSeconds(1), Now, Threshold));
        Assert.True(ScraperHealth.IsStale(null, Now, Threshold));
    }

    private static (HealthLevel Level, string? Note) Source(
        bool enabled = true, bool hasLabel = true, double? hoursAgo = 0.5, string? error = null, int pages = 10) =>
        ScraperHealth.ClassifySource(enabled, hasLabel, hoursAgo is { } h ? Now.AddHours(-h) : null, error, pages, Now, Threshold);

    [Fact]
    public void A_fresh_labelled_source_with_articles_is_ok() => Assert.Equal((HealthLevel.Ok, null), Source());

    [Fact]
    public void A_disabled_source_is_never_a_problem() =>
        Assert.Equal((HealthLevel.Ok, "Disabled"), Source(enabled: false, error: "boom", hoursAgo: null, pages: 0));

    [Fact]
    public void An_error_beats_every_other_signal() =>
        Assert.Equal((HealthLevel.Problem, "Last scrape failed"), Source(error: "HTTP 500", hoursAgo: 100, pages: 0));

    [Fact]
    public void Source_problems_and_warnings()
    {
        Assert.Equal((HealthLevel.Problem, "Never scraped"), Source(hoursAgo: null));
        Assert.Equal((HealthLevel.Problem, "Not refreshed for 7 days"), Source(hoursAgo: 7 * 24 + 4));
        Assert.Equal((HealthLevel.Warning, "Scraped, but no articles"), Source(pages: 0));
        Assert.Equal(HealthLevel.Warning, Source(hasLabel: false).Level);
    }

    [Fact]
    public void Feed_problems_and_warnings()
    {
        Assert.Equal((HealthLevel.Ok, null), ScraperHealth.ClassifyFeed(Now.AddHours(-1), 5, null, Now, Threshold));
        Assert.Equal((HealthLevel.Ok, null), ScraperHealth.ClassifyFeed(Now.AddHours(-1), 5, 3, Now, Threshold));
        Assert.Equal((HealthLevel.Problem, "Nothing collected yet"), ScraperHealth.ClassifyFeed(null, 0, null, Now, Threshold));
        Assert.Equal((HealthLevel.Problem, "Not refreshed for 4 hours"), ScraperHealth.ClassifyFeed(Now.AddHours(-4), 5, 2, Now, Threshold));
        Assert.Equal((HealthLevel.Warning, "No upcoming games"), ScraperHealth.ClassifyFeed(Now.AddHours(-1), 5, 0, Now, Threshold));
    }

    // ---- against the database (each test rolls back) -----------------------------------------------------------------

    private static WebScraperContext Create() => new(
        new DbContextOptionsBuilder<WebScraperContext>()
            .UseSqlServer("Server=sql2025,1433;Database=WebScraper;User Id=sa;Password=Fedora_Dev_2025!;Encrypt=False;TrustServerCertificate=True;")
            .Options);

    private static string Tag() => Guid.NewGuid().ToString("N")[..10];

    [Fact]
    public async Task The_report_covers_every_pipeline_and_puts_problems_first()
    {
        await using var db = Create();
        await using var tx = await db.Database.BeginTransactionAsync();
        var tag = Tag();
        var now = DateTime.UtcNow;

        var failing = new Source { Url = $"https://{tag}.test.invalid/a", Label = $"zz {tag} failing", Enabled = true, LastScrapedAt = now, LastError = "HTTP 500" };
        var off = new Source { Url = $"https://{tag}.test.invalid/b", Label = $"zz {tag} off", Enabled = false, LastError = "old error" };
        var channel = new YoutubeVideoFeed { ChannelId = $"UC{tag}", ChannelName = $"zz {tag}", Url = $"https://www.youtube.com/feeds/videos.xml?channel_id=UC{tag}" };
        var type = new SportsEventsType { EventsTypeName = $"zz {tag}", RssUrl = $"https://{tag}.test.invalid/cal.rss" };
        db.Sources.AddRange(failing, off);
        db.YoutubeVideoFeeds.Add(channel);
        db.SportsEventsTypes.Add(type);
        // The video is not linked to its channel row (as the scraper writes it): it must still count, by channel id.
        db.YouTubeVideos.Add(new YouTubeVideo
        {
            VideoId = tag[..10] + "x", ChannelId = channel.ChannelId, Title = "zz", Url = "https://www.youtube.com/watch?v=x",
            PublishedAt = now, FirstSeenAt = now, LastSeenAt = now,
        });
        db.SportsEvents.Add(new SportsEvent
        {
            Url = $"https://{tag}.test.invalid/g/1", Title = "zz game", EventDate = new DateOnly(2020, 1, 1), TimeTbd = true,
            FirstSeenAt = now, LastSeenAt = now, SportsEventsType = type,
        });
        await db.SaveChangesAsync();

        var report = await ScraperHealth.GetReportAsync(db, now, DateOnly.FromDateTime(now), Threshold);

        Assert.False(report.Stale);   // this test's own rows were written just now
        Assert.Equal(HealthLevel.Problem, report.Sources[0].Level);

        var failingRow = report.Sources.Single(r => r.Id == failing.Id);
        Assert.Equal((HealthLevel.Problem, "HTTP 500"), (failingRow.Level, failingRow.Error));

        var offRow = report.Sources.Single(r => r.Id == off.Id);
        Assert.Equal((HealthLevel.Ok, null, false), (offRow.Level, offRow.Error, offRow.Enabled));   // a disabled source's old error is hidden

        var channelRow = report.Channels.Single(r => r.Id == channel.Id);
        Assert.Equal((HealthLevel.Ok, 1), (channelRow.Level, channelRow.ItemCount));
        Assert.True(report.UnlinkedVideos >= 1);

        var feedRow = report.EventFeeds.Single(r => r.Id == type.Id);
        Assert.Equal((HealthLevel.Warning, 1, 0), (feedRow.Level, feedRow.ItemCount, feedRow.Upcoming));   // only a past game
    }
}
