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
        var firstPage = await MoviesQuery.GetAsync(db, today, tag, null, null, "all", take: 2);
        var secondPage = await MoviesQuery.GetAsync(db, today, tag, null, null, "all", offset: 2, take: 2);
        Assert.Equal(4, firstPage.Total); Assert.Equal(4, secondPage.Total);
        Assert.Equal(4, firstPage.Items.Concat(secondPage.Items).Select(i => i.Id).Distinct().Count());
        Assert.Empty((await MoviesQuery.GetAsync(db, today, tag, null, null, "all", offset: 4, take: 2)).Items);
        Assert.Single((await MoviesQuery.GetAsync(db, today, tag, null, null, null, onDate: today)).Items);
        var vod = Release("vod", today); vod.ReleaseType = "digital";
        db.Add(vod); await db.SaveChangesAsync();
        var timeline = await MoviesQuery.GetTodayTimelineAsync(db, today);
        Assert.Single(timeline, item => item.Title == tag);
        Assert.DoesNotContain(timeline, item => item.Type is "digital" or "digital_rental" or "digital_purchase");
        await db.MovieReleases.Where(r => r.ID == vod.ID).ExecuteDeleteAsync();
        Assert.Empty((await MoviesQuery.GetAsync(db, today, tag, "disc", null, "all")).Items);
        Assert.Empty((await MoviesQuery.GetAsync(db, today, tag, null, "Another platform", "all")).Items);
        var dvd = Release("dvd", today); dvd.ReleaseType = "disc"; dvd.Format = "DVD";
        var bluray = Release("bluray", today); bluray.ReleaseType = "disc"; bluray.Format = "Blu-ray + Digital";
        var repeated = Release("bluray-repeat", today); repeated.ReleaseType = "disc"; repeated.Format = "Blu-ray + Digital";
        db.AddRange(dvd, bluray, repeated);
        await db.SaveChangesAsync();
        var combined = await MoviesQuery.GetAsync(db, today, tag, null, null, "all", take: 2);
        var combinedNext = await MoviesQuery.GetAsync(db, today, tag, null, null, "all", offset: 2, take: 2);
        var combinedLast = await MoviesQuery.GetAsync(db, today, tag, null, null, "all", offset: 4, take: 2);
        Assert.Equal(5, combined.Total);
        var disc = Assert.Single(combined.Items.Concat(combinedNext.Items).Concat(combinedLast.Items), x => x.Type == "disc");
        Assert.Equal("Blu-ray + Digital · DVD", disc.Format);
        Assert.Equal(5, combined.Items.Concat(combinedNext.Items).Concat(combinedLast.Items).Select(x => x.Id).Distinct().Count());
        await db.MovieReleases.Where(r => r.ID == dvd.ID || r.ID == bluray.ID || r.ID == repeated.ID).ExecuteDeleteAsync();
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

    [Fact]
    public async Task Home_shows_only_upcoming_Friday_theatrical_releases_and_deduplicates_movies()
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

        await db.MovieReleaseSources.Where(x => x.ID != source.ID)
            .ExecuteUpdateAsync(set => set.SetProperty(x => x.Enabled, false));
        MovieRelease Release(string key, DateOnly? date, string type = "theatrical", string status = "scheduled") => new()
        {
            MovieSourceLinkId = link.ID, ReleaseKey = key, CountryCode = "US", ReleaseType = type,
            ReleaseDate = date, DateStatus = date is null ? "tbd" : "announced", ReleaseStatus = status,
            SourceUrl = "https://test.invalid", FirstSeenAt = DateTime.UtcNow, LastSeenAt = DateTime.UtcNow
        };
        var friday = today.AddDays(1);
        db.AddRange(Release("friday", friday), Release("duplicate", friday), Release("thursday", today),
            Release("past", friday.AddDays(-7)), Release("streaming", friday, "digital"),
            Release("cancelled", friday, status: "cancelled"), Release("unknown", null));
        await db.SaveChangesAsync();
        var items = await MoviesQuery.GetFridayTheatricalAsync(db, today);
        Assert.Equal(friday, Assert.Single(items).Date);
        Assert.Single(await MoviesQuery.GetFridayTheatricalAsync(db, friday));
        Assert.Empty(await MoviesQuery.GetFridayTheatricalAsync(db, friday.AddDays(1)));
        for (int i = 0; i < 6; i++)
        {
            var another = new Movie { Title = $"Movie {i} {tag}", FirstSeenAt = DateTime.UtcNow, LastSeenAt = DateTime.UtcNow };
            db.Add(another);
            await db.SaveChangesAsync();
            var anotherLink = new MovieSourceLink { MovieId = another.ID, MovieReleaseSourceId = source.ID,
                ExternalMovieId = $"movie-{i}-{tag}", Url = "https://test.invalid", FirstSeenAt = DateTime.UtcNow, LastSeenAt = DateTime.UtcNow };
            db.Add(anotherLink);
            await db.SaveChangesAsync();
            var release = Release($"more-{i}", friday.AddDays(7));
            release.MovieSourceLinkId = anotherLink.ID;
            db.Add(release);
        }
        await db.SaveChangesAsync();
        items = await MoviesQuery.GetFridayTheatricalAsync(db, today);
        Assert.Equal(5, items.Count);
        Assert.Equal(friday, items[0].Date);
        Assert.All(items, x => Assert.Equal(DayOfWeek.Friday, x.Date!.Value.DayOfWeek));
        Assert.Equal(7, (await MoviesQuery.GetFridayTheatricalAsync(db, today, 10)).Count);
        db.AddRange(Release("digital-today", today, "digital_purchase"),
            Release("digital-duplicate", friday, "digital_rental"),
            Release("digital-past", today.AddDays(-1), "digital"),
            Release("digital-cancelled", today, "digital", "cancelled"),
            Release("digital-tbd", null, "digital"));
        await db.SaveChangesAsync();
        var digital = await MoviesQuery.GetUpcomingDigitalAsync(db, today);
        Assert.Equal(new[] { today, friday }, digital.Select(x => x.Date!.Value));
        Assert.Equal("digital_purchase", digital[0].Type);
        Assert.Single(await MoviesQuery.GetUpcomingDigitalAsync(db, today, take: 1));
        source.Enabled = false;
        await db.SaveChangesAsync();
        Assert.Empty(await MoviesQuery.GetFridayTheatricalAsync(db, today));
        Assert.Empty(await MoviesQuery.GetUpcomingDigitalAsync(db, today));
    }
}
