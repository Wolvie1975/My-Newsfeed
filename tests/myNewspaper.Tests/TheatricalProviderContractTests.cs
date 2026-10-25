using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using myNewspaper.Web.Data;
using myNewspaper.Web.Data.Models;

namespace myNewspaper.Tests;

public class TheatricalProviderContractTests
{
    private sealed class FixtureContext(DbContextOptions<WebScraperContext> options) : WebScraperContext(options)
    {
        protected override void OnModelCreating(ModelBuilder model)
        {
            base.OnModelCreating(model);
            model.Entity<Movie>().ToTable("#TheatricalMovies");
            model.Entity<MovieRelease>().ToTable("#TheatricalReleases");
            model.Entity<MovieSourceLink>().ToTable("#TheatricalLinks");
            model.Entity<MovieReleaseSource>().ToTable("#TheatricalSources");
            model.Entity<MovieCommerceRow>().ToView("#TheatricalCommerce");
        }
    }

    [Fact]
    public async Task Non_disc_provider_preserves_theatrical_digital_and_subscription_contracts()
    {
        await using var db = new FixtureContext(new DbContextOptionsBuilder<WebScraperContext>()
            .UseSqlServer(TestDatabase.ConnectionString).Options);
        await db.Database.OpenConnectionAsync();
        // Connection-local tables keep every fixture write separate from production rows.
        await db.Database.ExecuteSqlRawAsync("""
            CREATE TABLE #TheatricalMovies (ID int, Title nvarchar(500), OriginalYear smallint,
                Description nvarchar(max), PosterUrl nvarchar(2048), Certification nvarchar(20), RuntimeMinutes smallint);
            CREATE TABLE #TheatricalLinks (ID int, MovieId int, MovieReleaseSourceId int);
            CREATE TABLE #TheatricalSources (ID int, SourceKey varchar(50), Name nvarchar(200), Url nvarchar(2048),
                Enabled bit, CollectDisc bit, LastScrapedAt datetime2, LastError nvarchar(1000), CreatedAt datetime2);
            CREATE TABLE #TheatricalReleases (ID int, MovieSourceLinkId int, CountryCode char(2),
                ReleaseType varchar(30), ReleaseDate date, ReleaseStatus varchar(12), DateStatus varchar(12),
                Platform nvarchar(100), Format nvarchar(100), IsReRelease bit, SourceUrl nvarchar(2048), LastSeenAt datetime2);
            CREATE TABLE #TheatricalCommerce (MovieId int, Provider nvarchar(50), ActionUrl nvarchar(2048),
                ActionLabel nvarchar(200), LinkKind nvarchar(50));
            INSERT #TheatricalSources VALUES
                (1,'dvdsreleasedates','DVDs Release Dates','https://test.invalid',1,0,NULL,NULL,'20261022'),
                (2,'movieinsider','Movie Insider','https://test.invalid',1,1,NULL,NULL,'20261022'),
                (3,'whentostream','When To Stream','https://test.invalid',1,1,NULL,NULL,'20261022');
            INSERT #TheatricalMovies (ID,Title) VALUES (1,'Film A'),(2,'Film B'),(3,'Film C'),
                (4,'Film D'),(5,'Film E'),(6,'Film F'),(7,'Next Friday'),(8,'Foreign'),(9,'Cancelled'),(10,'November');
            INSERT #TheatricalLinks SELECT ID,ID,1 FROM #TheatricalMovies;
            INSERT #TheatricalLinks VALUES (11,1,2),(12,1,3);
            INSERT #TheatricalReleases (ID,MovieSourceLinkId,CountryCode,ReleaseType,ReleaseDate,ReleaseStatus,
                DateStatus,IsReRelease,SourceUrl,LastSeenAt)
            SELECT ID,ID,'US','theatrical','20261023','scheduled','announced',0,'https://test.invalid','20261022'
            FROM #TheatricalMovies WHERE ID<=6;
            INSERT #TheatricalReleases (ID,MovieSourceLinkId,CountryCode,ReleaseType,ReleaseDate,ReleaseStatus,
                DateStatus,IsReRelease,SourceUrl,LastSeenAt) VALUES
                (11,1,'US','theatrical','20261023','scheduled','announced',0,'https://test.invalid','20261022'),
                (12,7,'US','theatrical','20261030','scheduled','announced',0,'https://test.invalid','20261022'),
                (13,8,'GB','theatrical','20261023','scheduled','announced',0,'https://test.invalid','20261022'),
                (14,9,'US','theatrical','20261023','cancelled','announced',0,'https://test.invalid','20261022'),
                (15,1,'US','disc','20261023','withdrawn','announced',0,'https://test.invalid','20261022'),
                (16,1,'US','digital','20261023','scheduled','announced',0,'https://test.invalid','20261022'),
                (17,11,'US','digital_purchase','20261023','scheduled','announced',0,'https://test.invalid','20261022'),
                (18,12,'US','subscription','20261023','scheduled','announced',0,'https://test.invalid','20261022'),
                (19,10,'US','theatrical','20261106','scheduled','announced',0,'https://test.invalid','20261022');
            """);
        var dates = FeedDates.Create("America/Chicago");
        var thursday = dates.TodayAt(new DateTime(2026, 10, 23, 0, 30, 0, DateTimeKind.Utc));
        Assert.Equal(new DateOnly(2026, 10, 22), thursday);
        for (int day = 0; day < 4; day++)
        {
            var home = await MoviesQuery.GetFridayTheatricalAsync(db, thursday.AddDays(day));
            Assert.Equal(5, home.Count);
            Assert.Equal(5, home.Select(x => x.Title).Distinct().Count());
            Assert.All(home, x => Assert.Equal(new DateOnly(2026, 10, 23), x.Date));
            Assert.All(home, x => { Assert.Null(x.Poster); Assert.NotNull(x.MovieActionUrl); });
            Assert.Equal(6, (await MoviesQuery.GetFridayTheatricalAsync(db, thursday.AddDays(day), 10)).Count);
        }
        foreach (var day in new[] { -3, -2, -1, 4 })
            Assert.Empty(await MoviesQuery.GetFridayTheatricalAsync(db, thursday.AddDays(day)));
        var theatrical = await MoviesQuery.GetAsync(db, thursday, null, "theatrical", null, "month");
        Assert.Equal(7, theatrical.Total); // Six this Friday, one next Friday; November excluded.
        Assert.Equal(6, theatrical.Items.Count(x => x.Date == new DateOnly(2026, 10, 23)));
        var saturday = await MoviesQuery.GetAsync(db, thursday.AddDays(2), null, "theatrical", null, "month");
        Assert.Equal("Next Friday", Assert.Single(saturday.Items).Title);
        var all = await MoviesQuery.GetAsync(db, thursday, null, null, null, "month");
        Assert.DoesNotContain(all.Items, x => x.Type == "disc");
        Assert.Single(all.Items, x => x.Type == "subscription");
        Assert.Equal("movieinsider", (await DigitalReleasesQuery.GetAsync(db, thursday, null)).Selected!.SourceKey);
        var digital = await DigitalReleasesQuery.GetAsync(db, thursday, "dvdsreleasedates");
        Assert.Equal("digital", Assert.Single(digital.Items).Type);
        await db.Database.ExecuteSqlRawAsync("UPDATE #TheatricalSources SET Enabled=0 WHERE SourceKey='dvdsreleasedates'");
        Assert.Empty(await MoviesQuery.GetFridayTheatricalAsync(db, thursday));
        Assert.Empty((await MoviesQuery.GetAsync(db, thursday, null, "theatrical", null, "month")).Items);
        var fallback = await DigitalReleasesQuery.GetAsync(db, thursday, "dvdsreleasedates");
        Assert.Equal("movieinsider", fallback.Selected!.SourceKey);
        Assert.DoesNotContain(fallback.Sources, x => x.SourceKey == "dvdsreleasedates");
        Assert.Equal("digital_purchase", Assert.Single(fallback.Items).Type);
    }
}
