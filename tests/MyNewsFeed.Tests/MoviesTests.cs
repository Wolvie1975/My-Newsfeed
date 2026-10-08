using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using MyNewsFeed.Web.Data;
using MyNewsFeed.Web.Data.Models;

namespace MyNewsFeed.Tests;

public class MoviesTests
{
    [Fact]
    public async Task Releases_filter_dates_platforms_and_sources_without_shifting_calendar_dates()
    {
        var config = new ConfigurationBuilder()
            .AddJsonFile(Path.GetFullPath("../../../../../src/MyNewsFeed.Web/appsettings.json", AppContext.BaseDirectory))
            .AddJsonFile(Path.GetFullPath("../../../../../src/MyNewsFeed.Web/appsettings.Development.json", AppContext.BaseDirectory), true)
            .AddUserSecrets("c579816a-020e-445a-92f6-efdd7002106c").AddEnvironmentVariables().Build();
        await using var db = new WebScraperContext(new DbContextOptionsBuilder<WebScraperContext>()
            .UseSqlServer(config.GetConnectionString("DefaultConnection")).Options);
        await using var transaction = await db.Database.BeginTransactionAsync();
        var tag = Guid.NewGuid().ToString("N");
        var today = new DateOnly(2026, 10, 8);
        var source = new MovieReleaseSource { SourceKey = tag, Name = "Test", Url = "https://test.invalid", Enabled = true, CreatedAt = DateTime.UtcNow };
        var movie = new Movie { Title = tag, FirstSeenAt = DateTime.UtcNow, LastSeenAt = DateTime.UtcNow };
        db.AddRange(source, movie);
        await db.SaveChangesAsync();
        var link = new MovieSourceLink { MovieId = movie.ID, MovieReleaseSourceId = source.ID, ExternalMovieId = tag,
            Url = "https://test.invalid", FirstSeenAt = DateTime.UtcNow, LastSeenAt = DateTime.UtcNow };
        db.Add(link);
        await db.SaveChangesAsync();
        MovieRelease Release(string key, DateOnly? date, string status = "scheduled") => new()
        {
            MovieSourceLinkId = link.ID, ReleaseKey = key, CountryCode = "US", ReleaseType = "subscription",
            ReleaseDate = date, DateStatus = date is null ? "tbd" : "announced", ReleaseStatus = status, Platform = "Test streaming",
            SourceUrl = "https://test.invalid", FirstSeenAt = DateTime.UtcNow, LastSeenAt = DateTime.UtcNow
        };
        db.AddRange(Release("today", today), Release("yesterday", today.AddDays(-1)), Release("unknown", null),
            Release("old", today.AddDays(-31)), Release("cancelled", today, "cancelled"));
        await db.SaveChangesAsync();
        var upcoming = await MoviesQuery.GetAsync(db, today, tag, null, null, null);
        Assert.Equal(2, upcoming.Total);
        Assert.Equal(today, upcoming.Items[0].Date);
        Assert.Null(upcoming.Items[1].Date);
        Assert.Single((await MoviesQuery.GetAsync(db, today, tag, "subscription", "Test streaming", "recent")).Items);
        Assert.Single((await MoviesQuery.GetAsync(db, today, tag, null, null, "tbd")).Items);
        Assert.Equal(4, (await MoviesQuery.GetAsync(db, today, tag, null, null, "all")).Total);
        Assert.Empty((await MoviesQuery.GetAsync(db, today, tag, "disc", null, "all")).Items);
        Assert.Empty((await MoviesQuery.GetAsync(db, today, tag, null, "Another platform", "all")).Items);
        source.LastScrapedAt = DateTime.UtcNow;
        await db.SaveChangesAsync();
        var now = DateTime.UtcNow;
        var report = await ScraperHealth.GetReportAsync(db, now, today, TimeSpan.FromHours(3));
        var movieHealth = Assert.Single(report.MovieSources, x => x.Id == source.ID);
        Assert.Equal(HealthLevel.Ok, movieHealth.Level);
        Assert.Equal(5, movieHealth.ItemCount);
        Assert.Equal(1, movieHealth.Upcoming);
        Assert.Contains(movieHealth, report.All);
        Assert.True(report.LastRun >= source.LastScrapedAt);
        source.LastError = "Movie feed HTTP 500";
        await db.SaveChangesAsync();
        report = await ScraperHealth.GetReportAsync(db, now, today, TimeSpan.FromHours(3));
        movieHealth = Assert.Single(report.MovieSources, x => x.Id == source.ID);
        Assert.Equal(HealthLevel.Problem, movieHealth.Level);
        Assert.Equal(source.LastError, movieHealth.Error);
        source.Enabled = false;
        await db.SaveChangesAsync();
        Assert.Empty((await MoviesQuery.GetAsync(db, today, tag, null, null, "all")).Items);
        report = await ScraperHealth.GetReportAsync(db, now, today, TimeSpan.FromHours(3));
        movieHealth = Assert.Single(report.MovieSources, x => x.Id == source.ID);
        Assert.Equal(HealthLevel.Ok, movieHealth.Level);
        Assert.Equal("Disabled", movieHealth.Note);
        Assert.Null(movieHealth.Error);
    }
}
