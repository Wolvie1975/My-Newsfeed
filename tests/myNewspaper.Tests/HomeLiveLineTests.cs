using myNewspaper.Web.Components.Pages;
using myNewspaper.Web.Data;

namespace myNewspaper.Tests;

public class HomeLiveLineTests
{
    private static ScoreboardGame Game(string status, decimal? away = null, decimal? home = null, bool? neutral = null) => new()
    {
        AwayName = "Boston Bruins",
        HomeName = "Carolina Hurricanes",
        Status = status,
        AwayScore = away,
        HomeScore = home,
        NeutralSite = neutral,
    };

    [Fact]
    public void Live_games_read_away_at_home_with_scores()
    {
        Assert.Equal("Boston Bruins 3 @ Carolina Hurricanes 1", Home.LiveLine(Game("live", 3, 1)));
    }

    [Fact]
    public void Games_not_started_read_away_at_home()
    {
        Assert.Equal("Boston Bruins @ Carolina Hurricanes", Home.LiveLine(Game("upcoming")));
    }

    [Fact]
    public void Neutral_sites_use_vs_because_neither_team_is_home()
    {
        Assert.Equal("Boston Bruins 2 vs Carolina Hurricanes 2", Home.LiveLine(Game("live", 2, 2, neutral: true)));
        Assert.Equal("Boston Bruins vs Carolina Hurricanes", Home.LiveLine(Game("upcoming", neutral: true)));
    }

    [Fact]
    public void Live_games_without_scores_yet_show_names_only()
    {
        Assert.Equal("Boston Bruins @ Carolina Hurricanes", Home.LiveLine(Game("live")));
    }
}
