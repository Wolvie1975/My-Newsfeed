using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using myNewspaper.Shared.Data;

namespace myNewspaper.Tests;

public class ScoreboardReadTests
{
    [Fact]
    public async Task Enabled_views_materialize_and_competition_filters_preserve_canonical_identity()
    {
        await using var db = TestDatabase.CreateContext();
        var dates = FeedDates.Create(FeedDates.SiteTimeZoneId);
        Task<ScoreboardSnapshot> Read(DateOnly day, bool carryOver, string? competition = null, bool finalsOnly = false, string? show = null) =>
            ScoreboardQuery.ReadAsync(db, dates, NullLogger.Instance, day, carryOver, competition, null, finalsOnly, show);
        var today = FeedDates.Create("America/Chicago").Today;
        var snapshot = await Read(today, true);
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
        var finals = await Read(today, true, finalsOnly: true);
        Assert.True(finals.Available);
        Assert.Equal(snapshot.Games.Where(g => g.Completed && g.Status == "final").Select(g => g.Key).Order(), finals.Games.Select(g => g.Key).Order());
        foreach (var follow in snapshot.Follows ?? [])
        {
            var filtered = await Read(today, true, show: "f:" + follow.Id);
            Assert.True(filtered.Available);
            Assert.Equal(snapshot.Games.Where(follow.Matches).Select(g => g.Key).Order(), filtered.Games.Select(g => g.Key).Order());
        }
        var big12Group = await Read(today, true, show: "g:Big 12");
        Assert.True(big12Group.Available);
        var big12Follows = (snapshot.Follows ?? []).Where(f => f.Kind == "conference" && string.Equals(f.Name, "Big 12", StringComparison.OrdinalIgnoreCase)).ToList();
        Assert.Equal(snapshot.Games.Where(g => big12Follows.Any(f => f.Matches(g))).Select(g => g.Key).Order(), big12Group.Games.Select(g => g.Key).Order());
        Assert.DoesNotContain(big12Group.Games, g => g.League == "womens-college-basketball");
        foreach (var competition in snapshot.Coverage.DistinctBy(c => c.Key).Take(3))
        {
            var filtered = await Read(today, true, competition.Key);
            Assert.True(filtered.Available);
            Assert.All(filtered.Games, g => { Assert.Equal(competition.Sport, g.Sport); Assert.Equal(competition.League, g.League); });
        }
        var top25Snapshot = await Read(today, true, show: "top25");
        Assert.True(top25Snapshot.Available);
        Assert.NotNull(top25Snapshot.Top25Week);
        Assert.All(top25Snapshot.Games, g =>
        {
            Assert.Contains(g.League, new[] { "college-football", "mens-college-basketball" });
            Assert.True(g.HomeRank is not null || g.AwayRank is not null);
        });
        Assert.All(top25Snapshot.Top25Week, g =>
        {
            Assert.Contains(g.League, new[] { "college-football", "mens-college-basketball" });
            Assert.True(g.HomeRank is not null || g.AwayRank is not null);
        });
    }
}
