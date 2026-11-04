using Microsoft.EntityFrameworkCore;
using myNewspaper.Shared.Data;

namespace myNewspaper.Tests;

public class TechDesksTests
{
    [Fact]
    public void Matches_whole_word_ai_but_not_substring_said()
    {
        var aiTopic = TechDesks.Technology.Topics.First(t => t.Slug == "ai");

        Assert.True(TechDesks.Matches(aiTopic, "New AI tools for Linux developers"));
        Assert.True(TechDesks.Matches(aiTopic, "OpenAI launches a new model"));
        Assert.False(TechDesks.Matches(aiTopic, "The company said the update arrives tomorrow"));
        Assert.False(TechDesks.Matches(aiTopic, "Paid subscribers get early access"));
    }

    [Fact]
    public void Matches_multi_word_phrase_and_punctuation_words()
    {
        var steamDeckTopic = TechDesks.VideoGames.Topics.First(t => t.Slug == "steam-deck-linux");
        var dealsTopic = TechDesks.Technology.Topics.First(t => t.Slug == "deals");
        var distrosTopic = TechDesks.Linux.Topics.First(t => t.Slug == "distros");

        Assert.True(TechDesks.Matches(steamDeckTopic, "Valve updates the Steam Deck OLED firmware"));
        Assert.True(TechDesks.Matches(dealsTopic, "Save 35% off this mechanical keyboard today"));
        Assert.True(TechDesks.Matches(distrosTopic, "System76 previews Pop!_OS COSMIC desktop"));
    }

    [Fact]
    public void Matches_checks_summary_when_present()
    {
        var kernelTopic = TechDesks.Linux.Topics.First(t => t.Slug == "kernel");

        Assert.False(TechDesks.Matches(kernelTopic, "New release candidate announced", null));
        Assert.True(TechDesks.Matches(kernelTopic, "New release candidate announced", "Linus Torvalds tagged rc1 this afternoon."));
    }

    [Theory]
    [InlineData("How to speed up your home Wi-Fi", "How-to")]
    [InlineData("How I replaced my laptop with a mini PC", "How-to")]
    [InlineData("I tested five budget mechanical keyboards", "Tested")]
    [InlineData("What is Matter and how does it work?", "Explainer")]
    [InlineData("Wi-Fi 7 mesh networking explained", "Explainer")]
    [InlineData("Best Linux laptops for developers in 2026", "Guide")]
    [InlineData("Samsung announces new Galaxy phone", null)]
    public void HowToTag_classifies_practical_headlines(string title, string? expected) =>
        Assert.Equal(expected, TechDesks.HowToTag(title));

    [Fact]
    public void LatestWeekStart_rolls_over_at_Sunday_6am_Central()
    {
        var dates = new FeedDates(TimeZoneInfo.FindSystemTimeZoneById("America/Chicago"));
        // Saturday Oct 31, 2026 18:00 UTC (13:00 CDT) -> completed week is Oct 18–24 (starts Oct 18)
        Assert.Equal(new DateOnly(2026, 10, 18), TechQuery.LatestWeekStart(new DateTime(2026, 10, 31, 18, 0, 0, DateTimeKind.Utc), dates));

        // Sunday Nov 1, 2026 09:00 UTC (04:00 CDT, before 6 AM Central) -> still Oct 18
        Assert.Equal(new DateOnly(2026, 10, 18), TechQuery.LatestWeekStart(new DateTime(2026, 11, 1, 9, 0, 0, DateTimeKind.Utc), dates));

        // Sunday Nov 1, 2026 13:00 UTC (07:00 CST, after 6 AM Central) -> rolls to Oct 25–31 (starts Oct 25)
        Assert.Equal(new DateOnly(2026, 10, 25), TechQuery.LatestWeekStart(new DateTime(2026, 11, 1, 13, 0, 0, DateTimeKind.Utc), dates));
    }

    [Fact]
    public void TryResolveWeekStart_validates_Sunday_and_12_week_window()
    {
        var dates = new FeedDates(TimeZoneInfo.FindSystemTimeZoneById("America/Chicago"));
        var nowUtc = new DateTime(2026, 11, 1, 15, 0, 0, DateTimeKind.Utc); // latestStart = 2026-10-25

        Assert.True(TechQuery.TryResolveWeekStart(null, nowUtc, dates, out var defWeek));
        Assert.Equal(new DateOnly(2026, 10, 25), defWeek);

        Assert.True(TechQuery.TryResolveWeekStart("2026-10-18", nowUtc, dates, out var prevWeek));
        Assert.Equal(new DateOnly(2026, 10, 18), prevWeek);

        // Not a Sunday -> false
        Assert.False(TechQuery.TryResolveWeekStart("2026-10-19", nowUtc, dates, out _));

        // Future week -> false
        Assert.False(TechQuery.TryResolveWeekStart("2026-11-01", nowUtc, dates, out _));

        // Older than 12 weeks -> false
        Assert.False(TechQuery.TryResolveWeekStart("2026-07-19", nowUtc, dates, out _));
    }

    [Theory]
    [InlineData("kernel_org_json", "https://www.kernel.org/releases.json", "kernel.org")]
    [InlineData("github_releases", "https://api.github.com/repos/ValveSoftware/Proton/releases", "GitHub releases")]
    [InlineData("rss", "https://kde.org/feed", "kde.org")]
    public void ReleaseSourceLabel_formats_upstream_source(string sourceKind, string? sourceRef, string expected) =>
        Assert.Equal(expected, TechQuery.ReleaseSourceLabel(sourceKind, sourceRef));

    [Fact]
    public async Task WeekAsync_computes_days_busiest_day_and_biggest_stories_and_gracefully_handles_missing_live_tables()
    {
        await using var db = new Microsoft.EntityFrameworkCore.DbContextOptionsBuilder<WebScraperContext>()
            .UseSqlServer(TestDatabase.ConnectionString)
            .Options is var opts ? new WebScraperContext(opts) : throw new InvalidOperationException();

        var dates = new FeedDates(TimeZoneInfo.FindSystemTimeZoneById("America/Chicago"));
        var latestWeek = TechQuery.LatestWeekStart(DateTime.UtcNow, dates);
        var summary = await TechQuery.WeekAsync(db, null, dates, latestWeek, "all");

        Assert.Equal(7, summary.Days.Count);
        Assert.Equal(summary.Stories, summary.Days.Sum(d => d.Count));
        Assert.Equal(summary.Days.Max(d => d.Count), summary.BusiestDayCount);

        // The Part 4 readers never throw: with the scraper's tables missing they return empty collections, and with the
        // tables present (as in the test copy of WebScraper) they return what is stored. Missing tables are covered by
        // TechQuery.LiveTablesAsync returning false, checked here only when that is the database's actual state.
        var tracker = await TechQuery.TrackerAsync(db);
        var released = await TechQuery.JustReleasedAsync(db, TimeSpan.FromHours(48), 4);
        var reading = await TechQuery.DevReadingAsync(db, "hn", 5);
        var services = await TechQuery.ServiceStatesAsync(db);
        if (!await TechQuery.LiveTablesAsync(db, "releases"))
        {
            Assert.Empty(tracker);
            Assert.Empty(released);
        }
        Assert.True(released.Count <= 4 && reading.Count <= 5);
        Assert.NotNull(services);
    }
}
