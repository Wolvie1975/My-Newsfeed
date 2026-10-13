using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using MyNewsFeed.Web.Data;
using MyNewsFeed.Web.Data.Models;

namespace MyNewsFeed.Tests;

public class DigitalReleasesTests
{
    [Fact]
    public void Default_prefers_enabled_movieinsider_and_never_selects_disabled_sources()
    {
        var insider = new MovieReleaseSource { ID = 1, SourceKey = "movieinsider", Name = "Movie Insider", Enabled = true };
        var other = new MovieReleaseSource { ID = 2, SourceKey = "whentostream", Name = "When To Stream", Enabled = true };
        Assert.Same(insider, DigitalReleasesQuery.SelectSource([other, insider], null));
        Assert.Same(other, DigitalReleasesQuery.SelectSource([other, insider], "whentostream"));
        Assert.Same(insider, DigitalReleasesQuery.SelectSource([other, insider], "unknown"));
        insider.Enabled = false;
        Assert.Same(other, DigitalReleasesQuery.SelectSource([other, insider], "movieinsider"));
        Assert.Null(DigitalReleasesQuery.SelectSource([], null));
    }

    [Fact]
    public async Task Digital_view_includes_today_and_future_dates_but_excludes_past_and_unconfirmed_releases()
    {
        var config = new ConfigurationBuilder().AddJsonFile(Path.GetFullPath("../../../../../src/MyNewsFeed.Web/appsettings.json", AppContext.BaseDirectory))
            .AddUserSecrets("c579816a-020e-445a-92f6-efdd7002106c").AddEnvironmentVariables().Build();
        await using var db = new WebScraperContext(new DbContextOptionsBuilder<WebScraperContext>().UseSqlServer(config.GetConnectionString("DefaultConnection")).Options);
        await using var transaction = await db.Database.BeginTransactionAsync();
        var tag = Guid.NewGuid().ToString("N");
        var insider = await db.MovieReleaseSources.SingleOrDefaultAsync(s => s.SourceKey == "movieinsider")
            ?? new MovieReleaseSource { SourceKey = "movieinsider", Name = "Movie Insider", Url = "https://test.invalid", CreatedAt = DateTime.UtcNow };
        var other = await db.MovieReleaseSources.SingleOrDefaultAsync(s => s.SourceKey == "whentostream")
            ?? new MovieReleaseSource { SourceKey = "whentostream", Name = "When To Stream", Url = "https://test.invalid", CreatedAt = DateTime.UtcNow };
        insider.Enabled = other.Enabled = true;
        if (db.Entry(insider).State == EntityState.Detached) db.Add(insider);
        if (db.Entry(other).State == EntityState.Detached) db.Add(other);
        var movie = new Movie { Title = tag, FirstSeenAt = DateTime.UtcNow, LastSeenAt = DateTime.UtcNow };
        db.Add(movie); await db.SaveChangesAsync();
        var link = new MovieSourceLink { MovieId = movie.ID, MovieReleaseSourceId = insider.ID, ExternalMovieId = tag,
            Url = "https://test.invalid", FirstSeenAt = DateTime.UtcNow, LastSeenAt = DateTime.UtcNow };
        var otherLink = new MovieSourceLink { MovieId = movie.ID, MovieReleaseSourceId = other.ID, ExternalMovieId = tag,
            Url = "https://test.invalid", FirstSeenAt = DateTime.UtcNow, LastSeenAt = DateTime.UtcNow };
        db.AddRange(link, otherLink); await db.SaveChangesAsync();
        var monday = new DateOnly(2026, 10, 5);
        MovieRelease Release(string key, DateOnly day, string type = "digital", string dateStatus = "announced", string status = "scheduled") => new()
        {
            MovieSourceLinkId = link.ID, ReleaseKey = key, ReleaseDate = day, CountryCode = "US", ReleaseType = type,
            DateStatus = dateStatus, ReleaseStatus = status, Platform = type == "subscription" ? "Test streaming" : null, SourceUrl = "https://test.invalid", Format = "VOD / Digital",
            FirstSeenAt = DateTime.UtcNow, LastSeenAt = DateTime.UtcNow
        };
        var early = Release("early", monday); var sunday = Release("sunday", monday.AddDays(6));
        var next = Release("next", monday.AddDays(7));
        var separate = Release("other", monday.AddDays(6)); separate.MovieSourceLinkId = otherLink.ID;
        db.AddRange(early, sunday, separate, Release("old", monday.AddDays(-1)), next,
            Release("estimated", monday, dateStatus: "estimated"), Release("disc", monday, "disc"),
            Release("subscription", monday, "subscription"), Release("cancelled", monday, status: "cancelled"));
        await db.SaveChangesAsync();
        var page = await DigitalReleasesQuery.GetAsync(db, monday.AddDays(6), null);
        Assert.Equal(insider.ID, page.Selected!.ID);
        var own = page.Items.Where(i => i.Title == tag).ToList();
        Assert.Equal(new[] { sunday.ID, next.ID }, own.Select(i => i.Id));
        Assert.All(own, i => Assert.Null(i.Year));
        var alternate = await DigitalReleasesQuery.GetAsync(db, monday.AddDays(6), "whentostream");
        Assert.Equal(separate.ID, Assert.Single(alternate.Items, i => i.Title == tag).Id);
        early.ReleaseDate = monday.AddDays(8); await db.SaveChangesAsync();
        var changed = await DigitalReleasesQuery.GetAsync(db, monday.AddDays(6), "movieinsider");
        Assert.Equal(monday.AddDays(8), Assert.Single(changed.Items, i => i.Id == early.ID).Date);
    }
}
