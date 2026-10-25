using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using myNewspaper.Web.Data;

namespace myNewspaper.Tests;

public class ScoreboardReadTests
{
    [Fact]
    public async Task Enabled_views_materialize_and_competition_filters_preserve_canonical_identity()
    {
        var factory = new PooledDbContextFactory<WebScraperContext>(new DbContextOptionsBuilder<WebScraperContext>()
            .UseSqlServer(TestDatabase.ConnectionString).Options);
        var repository = new ScoreboardRepository(factory, NullLogger<ScoreboardRepository>.Instance);
        var today = FeedDates.Create("America/Chicago").Today;
        var snapshot = await repository.ReadAsync(today, true);
        Assert.True(snapshot.Available);
        Assert.DoesNotContain(snapshot.Coverage, c => c.League == "womens-college-basketball");
        Assert.DoesNotContain(snapshot.Follows ?? [], f => f.League == "womens-college-basketball");
        Assert.DoesNotContain(snapshot.Games, g => g.League == "womens-college-basketball");
        Assert.Equal(snapshot.Games.Count, snapshot.Games.Select(g => g.Key).Distinct().Count());
        Assert.All(snapshot.Games, g =>
        {
            Assert.Contains(snapshot.Coverage, c => c.Sport == g.Sport && c.League == g.League);
            Assert.True(g.ScoreboardDate == today || g.ScoreboardDate < today && g.Active);
        });
        var finals = await repository.ReadAsync(today, true, finalsOnly: true);
        Assert.True(finals.Available);
        Assert.Equal(snapshot.Games.Where(g => g.Completed && g.Status == "final").Select(g => g.Key).Order(), finals.Games.Select(g => g.Key).Order());
        foreach (var follow in snapshot.Follows ?? [])
        {
            var filtered = await repository.ReadAsync(today, true, show: "f:" + follow.Id);
            Assert.True(filtered.Available);
            Assert.Equal(snapshot.Games.Where(follow.Matches).Select(g => g.Key).Order(), filtered.Games.Select(g => g.Key).Order());
        }
        foreach (var competition in snapshot.Coverage.DistinctBy(c => c.Key).Take(3))
        {
            var filtered = await repository.ReadAsync(today, true, competition.Key);
            Assert.True(filtered.Available);
            Assert.All(filtered.Games, g => { Assert.Equal(competition.Sport, g.Sport); Assert.Equal(competition.League, g.League); });
        }
    }
}
