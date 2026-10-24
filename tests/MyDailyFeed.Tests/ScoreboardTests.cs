using MyDailyFeed.Web.Data;

namespace MyDailyFeed.Tests;

public class ScoreboardTests
{
    [Fact]
    public void Follows_match_only_explicit_ids_in_the_same_provider_and_competition()
    {
        var game = new ScoreboardGame { Provider = "espn", Sport = "football", League = "college-football", HomeTeamId = "2305", AwayTeamId = "9", HomeName = "Kansas" };
        var team = new ScoreboardFollow(1, "Kansas", "football", "college-football", "team", "2305", new HashSet<string>());
        Assert.True(team.Matches(game));
        game.HomeTeamId = "other";
        Assert.False(team.Matches(game)); // Matching display names are not identities.
        var conference = team with { Kind = "conference", TeamId = null, Members = new HashSet<string> { "9" } };
        Assert.True(conference.Matches(game)); // Includes non-conference opponents.
        game.League = "nfl";
        Assert.False(conference.Matches(game));
        game.League = "college-football";
        game.Provider = "other";
        Assert.False(conference.Matches(game));
    }
    [Fact]
    public void Empty_membership_and_unknown_follow_types_do_not_match_games()
    {
        var game = new ScoreboardGame { Provider = "espn", Sport = "soccer", League = "usa.1" };
        var follow = new ScoreboardFollow(1, "MLS", "soccer", "usa.1", "league", null, new HashSet<string>());
        Assert.True(follow.Matches(game));
        Assert.False((follow with { Kind = "team" }).Matches(game));
        Assert.False((follow with { Kind = "conference" }).Matches(game));
        Assert.False((follow with { Kind = "unknown" }).Matches(game));
    }
    [Theory]
    [InlineData("upcoming", "Upcoming")]
    [InlineData("live", "Live")]
    [InlineData("final", "Final")]
    [InlineData("postponed", "Postponed")]
    [InlineData("cancelled", "Cancelled")]
    [InlineData("suspended", "Suspended")]
    [InlineData("delayed", "Delayed")]
    [InlineData("unknown", "Status unavailable")]
    [InlineData("unexpected", "Status unavailable")]
    public void Exceptional_statuses_override_provider_description(string status, string expected)
    {
        var game = new ScoreboardGame { Status = status, Completed = true };
        Assert.Equal(expected, game.Label);
        if (status is "postponed" or "cancelled" or "suspended" or "delayed" or "unknown") {
            game.StatusText = "Final";
            Assert.Equal(expected, game.Label);
        }
    }
    [Fact]
    public void Null_is_not_zero_and_fractional_scores_are_preserved()
    {
        Assert.Equal("—", ScoreboardGame.Score(null));
        Assert.Equal("0", ScoreboardGame.Score(0));
        Assert.Equal("97", ScoreboardGame.Score(97.000m));
        Assert.Equal("1.125", ScoreboardGame.Score(1.125m));
    }
    [Fact]
    public void Team_identity_is_namespaced_and_unknown_ids_are_not_preferences()
    {
        Assert.NotEqual(ScoreboardGame.TeamKey("soccer", "usa.1", "1"), ScoreboardGame.TeamKey("soccer", "eng.1", "1"));
        Assert.Null(ScoreboardGame.TeamKey("soccer", "usa.1", null));
        Assert.Null(ScoreboardGame.TeamKey("soccer", "usa.1", " "));
    }
    [Theory]
    [InlineData("2026-11-01", 25)]
    [InlineData("2026-03-08", 23)]
    [InlineData("2026-10-23", 24)]
    public void Central_day_boundaries_follow_dst(string date, int hours)
    {
        var window = ResultsWindow.ForDay(DateOnly.Parse(date), TimeZoneInfo.FindSystemTimeZoneById("America/Chicago"));
        Assert.Equal(hours, (window.EndUtc-window.StartUtc).TotalHours);
    }
    [Fact]
    public void Only_active_or_paused_samples_age_into_stale_state()
    {
        var now = new DateTime(2026,10,24,3,0,0,DateTimeKind.Utc);
        foreach (var status in new[] { "live", "delayed", "suspended", "final", "upcoming", "cancelled" }) {
            var game = new ScoreboardGame { Status = status, LastObservedAtUtc = DateTime.SpecifyKind(now.AddMinutes(-26), DateTimeKind.Unspecified) };
            Assert.Equal(status is "live" or "delayed" or "suspended", game.Stale(now));
        }
    }
    [Fact]
    public void Details_handle_missing_and_malformed_optional_json()
    {
        Assert.Null(ScoreboardGame.FormatDetails("{", "baseball"));
        Assert.Null(ScoreboardGame.FormatDetails("null", "baseball"));
        Assert.Null(ScoreboardGame.FormatDetails("{}", "baseball"));
        Assert.Null(ScoreboardGame.FormatDetails("{\"outs\":null}", "baseball"));
    }
    [Fact]
    public void Baseball_counts_and_extra_periods_preserve_provider_values()
    {
        var text = ScoreboardGame.FormatDetails("""{"outs":0,"balls":1,"strikes":2,"onFirst":true,"onSecond":false,"homePeriods":[{"period":10,"score":2,"displayValue":"2"}]}""", "baseball");
        Assert.Contains("0 outs", text);
        Assert.Contains("1st occupied", text);
        Assert.DoesNotContain("2nd occupied", text);
        Assert.Contains("Home periods: 2", text);
    }
}
