using myNewspaper.Web.Data;
using myNewspaper.Web.Data.Models;

namespace myNewspaper.Tests;

public class HomeEventResultsTests
{
    private static EventItem Game => new(1, "https://example.test/game", "Kansas vs Houston", "Football", "Houston",
        false, null, new DateOnly(2026, 10, 17), new DateTime(2026, 10, 17, 19, 0, 0), false,
        null, null, null, null, null, "Kansas", ProviderKey: "big12");

    [Fact]
    public void College_final_matches_both_perspectives_and_aliases_but_preserves_other_games()
    {
        var result = new ResultGame { Completed = true, Sport = "football", League = "college-football",
            StartsAtUtc = Game.StartsAtUtc!.Value, HomeName = "Houston Cougars", AwayName = "Kansas Jayhawks" };
        Assert.True(HomeEventResults.Matches(Game, result));
        result.Completed = false;
        Assert.False(HomeEventResults.Matches(Game, result));
        result.Completed = true;
        Assert.False(HomeEventResults.Matches(Game with { StartsAtUtc = Game.StartsAtUtc.Value.AddHours(3) }, result));
        Assert.False(HomeEventResults.Matches(Game with { Sport = "Basketball" }, result));
        Assert.False(HomeEventResults.Matches(Game with { Opponent = "Baylor" }, result));
        Assert.False(HomeEventResults.Matches(Game with { TimeTbd = true }, result));
    }

    [Theory]
    [InlineData("Soccer", "soccer", "usa.ncaa.w.1")]
    [InlineData("Men's Basketball", "basketball", "mens-college-basketball")]
    [InlineData("Volleyball", "volleyball", "womens-college-volleyball")]
    public void College_sports_use_the_configured_results_competition(string sport, string resultSport, string league)
    {
        var result = new ResultGame { Completed = true, Sport = resultSport, League = league,
            StartsAtUtc = Game.StartsAtUtc!.Value, HomeName = "Kansas Jayhawks", AwayName = "Houston Cougars" };
        Assert.True(HomeEventResults.Matches(Game with { Sport = sport }, result));
    }

    [Fact]
    public void Espn_identity_requires_same_league_event_and_completed_result()
    {
        var game = Game with { ProviderKey = "espn-mlb", GameId = 123, CanonicalKey = "espn:mlb:123" };
        var result = new ResultGame { Completed = true, League = "mlb", EventId = "123" };
        Assert.True(HomeEventResults.Matches(game, result));
        Assert.True(HomeEventResults.Matches(game with { CanonicalKey = null }, result));
        result.League = "wnba";
        Assert.False(HomeEventResults.Matches(game, result));
        result.League = "mlb";
        result.EventId = "124";
        Assert.False(HomeEventResults.Matches(game, result));
        result.EventId = "123";
        result.Completed = false;
        Assert.False(HomeEventResults.Matches(game, result));
    }

    [Theory]
    [InlineData("STATUS_FINAL", 1, true)]
    [InlineData("STATUS_FULL_TIME", 0, true)]
    [InlineData("STATUS_FINAL_AET", 2, true)]
    [InlineData("STATUS_IN_PROGRESS", 1, false)]
    [InlineData("STATUS_FINAL", null, false)]
    public void Season_results_require_final_scores_and_provider_identity(string status, int? score, bool expected)
    {
        var game = Game with { ProviderKey = "espn-mls", GameId = 123 };
        var result = new SportsMatchResult { ProviderKey = "espn-mls", GameId = 123,
            ScheduleStatus = status, HomeScore = score, AwayScore = 0 };
        Assert.Equal(expected, HomeEventResults.Matches(game, result));
        result.ProviderKey = "espn-championship";
        Assert.False(HomeEventResults.Matches(game, result));
    }
}
