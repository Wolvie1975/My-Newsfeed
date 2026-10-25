using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using myNewspaper.Web.Data;
using myNewspaper.Web.Data.Models;

namespace myNewspaper.Tests;

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
        var disabled = new MovieReleaseSource { ID = 3, SourceKey = "dvdsreleasedates", Name = "DVDs Release Dates", Enabled = false, LastError = "Old provider failure" };
        insider.Enabled = true;
        Assert.Same(insider, DigitalReleasesQuery.SelectSource([disabled, other, insider], "dvdsreleasedates"));
        insider.Enabled = other.Enabled = false;
        Assert.Null(DigitalReleasesQuery.SelectSource([disabled, other, insider], "dvdsreleasedates"));
    }

    [Fact]
    public async Task Digital_view_includes_today_and_future_dates_but_excludes_past_and_unconfirmed_releases()
    {
        await using var db = new WebScraperContext(new DbContextOptionsBuilder<WebScraperContext>().UseSqlServer(TestDatabase.ConnectionString).Options);
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
        var link = new MovieSourceLink
        {
            MovieId = movie.ID,
            MovieReleaseSourceId = insider.ID,
            ExternalMovieId = tag,
            Url = "https://test.invalid",
            FirstSeenAt = DateTime.UtcNow,
            LastSeenAt = DateTime.UtcNow
        };
        var otherLink = new MovieSourceLink
        {
            MovieId = movie.ID,
            MovieReleaseSourceId = other.ID,
            ExternalMovieId = tag,
            Url = "https://test.invalid",
            FirstSeenAt = DateTime.UtcNow,
            LastSeenAt = DateTime.UtcNow
        };
        db.AddRange(link, otherLink); await db.SaveChangesAsync();
        var monday = new DateOnly(2026, 10, 5);
        MovieRelease Release(string key, DateOnly day, string type = "digital", string dateStatus = "announced", string status = "scheduled") => new()
        {
            MovieSourceLinkId = link.ID,
            ReleaseKey = key,
            ReleaseDate = day,
            CountryCode = "US",
            ReleaseType = type,
            DateStatus = dateStatus,
            ReleaseStatus = status,
            Platform = type == "subscription" ? "Test streaming" : null,
            SourceUrl = "https://test.invalid",
            Format = "VOD / Digital",
            FirstSeenAt = DateTime.UtcNow,
            LastSeenAt = DateTime.UtcNow
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
        var preview = await DigitalReleasesQuery.GetAsync(db, monday.AddDays(6), null, take: 2);
        Assert.Equal(page.Items.Take(2).Select(i => i.Id), preview.Items.Select(i => i.Id));
        Assert.Empty((await DigitalReleasesQuery.GetAsync(db, monday, null, take: 0)).Items);
        var own = page.Items.Where(i => i.Title == tag).ToList();
        Assert.Equal(new[] { sunday.ID, next.ID }, own.Select(i => i.Id));
        Assert.All(own, i => Assert.Null(i.Year));
        var alternate = await DigitalReleasesQuery.GetAsync(db, monday.AddDays(6), "whentostream");
        Assert.Equal(separate.ID, Assert.Single(alternate.Items, i => i.Title == tag).Id);
        // Disabled providers cannot appear in choices, leak releases or contribute warnings.
        other.Enabled = false; other.LastError = "Alternate failed"; insider.LastError = null;
        await db.SaveChangesAsync();
        var fallback = await DigitalReleasesQuery.GetAsync(db, monday, "whentostream");
        Assert.Equal(insider.ID, fallback.Selected!.ID);
        Assert.Null(fallback.Selected.LastError);
        Assert.DoesNotContain(fallback.Sources, s => s.ID == other.ID);
        Assert.DoesNotContain(fallback.Items, i => i.Id == separate.ID);
        // An enabled provider with no upcoming rows must not borrow another source's data.
        var future = new DateOnly(9998, 1, 1);
        var futureAlternate = Release("future-alternate", future); futureAlternate.MovieSourceLinkId = otherLink.ID;
        db.Add(futureAlternate); await db.SaveChangesAsync();
        Assert.DoesNotContain((await DigitalReleasesQuery.GetAsync(db, future, "movieinsider")).Items, i => i.Title == tag);
        await db.MovieReleaseSources.Where(s => s.Enabled).ExecuteUpdateAsync(set => set.SetProperty(s => s.Enabled, false));
        var none = await DigitalReleasesQuery.GetAsync(db, monday, "dvdsreleasedates");
        Assert.Null(none.Selected); Assert.Empty(none.Sources); Assert.Empty(none.Items);
        await db.MovieReleaseSources.Where(s => s.ID == insider.ID).ExecuteUpdateAsync(set => set.SetProperty(s => s.Enabled, true));
        early.ReleaseDate = monday.AddDays(8); await db.SaveChangesAsync();
        var changed = await DigitalReleasesQuery.GetAsync(db, monday.AddDays(6), "movieinsider");
        Assert.Equal(monday.AddDays(8), Assert.Single(changed.Items, i => i.Id == early.ID).Date);
    }
}
