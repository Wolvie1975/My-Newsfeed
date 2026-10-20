using System.Text.Json;
using MyDailyFeed.Web.Data.Models;
using MyDailyFeed.Web.Scores;

namespace MyDailyFeed.Tests;

public class LeaguePhasesTests
{
    [Theory]
    [InlineData("baseball", "mlb", "3", "Postseason", LeaguePhase.Postseason)]
    [InlineData("basketball", "wnba", "3", "Postseason", LeaguePhase.Postseason)]
    [InlineData("hockey", "nhl", "2", "Regular Season", LeaguePhase.RegularSeason)]
    [InlineData("football", "college-football", "3", "Bowls", LeaguePhase.Postseason)]
    [InlineData("soccer", "usa.1", "1", "Regular Season", LeaguePhase.RegularSeason)]
    [InlineData("soccer", "eng.1", "1", "2026-27 English Premier League", LeaguePhase.RegularSeason)]
    [InlineData("soccer", "usa.1", "1", "MLS Cup Playoffs", LeaguePhase.Postseason)]
    [InlineData("soccer", "eng.2", "2", "Promotion Play-Offs", LeaguePhase.Postseason)]
    [InlineData("soccer", "usa.ncaa.w.1", "1", "NCAA Tournament", LeaguePhase.Postseason)]
    [InlineData("basketball", "nba", "1", "Preseason", LeaguePhase.Unknown)]
    [InlineData("football", "nfl", "4", "Offseason", LeaguePhase.Unknown)]
    public void Current_league_metadata_classifies_phase_without_using_event_or_standings_phase(
        string sport, string league, string id, string name, LeaguePhase expected)
    {
        using var json = JsonDocument.Parse(JsonSerializer.Serialize(new {
            leagues = new[] { new { slug = league, season = new { type = new { id, name, type = 14308 } } } },
            events = new[] { new { season = new { type = 2 } } }
        }));
        Assert.Equal(expected, LeaguePhases.Parse(json.RootElement, sport, league));
    }

    [Fact]
    public void Missing_and_other_league_metadata_never_enables_records()
    {
        using var empty = JsonDocument.Parse("{}");
        Assert.Equal(LeaguePhase.Unknown, LeaguePhases.Parse(empty.RootElement, "baseball", "mlb"));
        using var other = JsonDocument.Parse("""{"leagues":[{"slug":"other","season":{"type":{"id":"2"}}}]}""");
        Assert.Equal(LeaguePhase.Unknown, LeaguePhases.Parse(other.RootElement, "baseball", "mlb"));
    }

    [Fact]
    public void Entire_league_gate_hides_all_teams_and_past_results_until_next_regular_phase()
    {
        var source = new SportsStandingsSource { ProviderKey = "espn-mlb", Sport = "Baseball" };
        var now = new DateTime(2026, 10, 18);
        var phases = new LeaguePhases();
        Assert.False(phases.IsRegular(source, now));
        phases.Update(source, LeaguePhase.RegularSeason, now);
        Assert.True(phases.IsRegular(source, now.AddMinutes(10)));
        Assert.False(phases.IsRegular(source, now.AddMinutes(21)));
        phases.Update(source, LeaguePhase.Postseason, now.AddMinutes(1));
        Assert.False(phases.IsRegular(source, now.AddMinutes(2)));
        phases.Update(source, LeaguePhase.Unknown, now.AddMinutes(3));
        Assert.False(phases.IsRegular(source, now.AddMinutes(4)));
        phases.Update(source, LeaguePhase.RegularSeason, now.AddMinutes(5));
        Assert.True(phases.IsRegular(source, now.AddMinutes(6)));
    }

    [Fact]
    public void College_sports_and_professional_leagues_have_independent_gates()
    {
        var football = new SportsStandingsSource { ProviderKey = "big12", Sport = "Football" };
        var basketball = new SportsStandingsSource { ProviderKey = "big12", Sport = "Men's Basketball" };
        var phases = new LeaguePhases(); var now = DateTime.UtcNow;
        phases.Update(football, LeaguePhase.RegularSeason, now);
        phases.Update(basketball, LeaguePhase.Postseason, now);
        Assert.True(phases.IsRegular(football, now));
        Assert.False(phases.IsRegular(basketball, now));
        Assert.Equal(("football", "college-football"), LeaguePhases.Competition(football));
        Assert.Equal(("basketball", "mens-college-basketball"), LeaguePhases.Competition(basketball));
    }
}
