using MyNewsFeed.Web.Data;
using MyNewsFeed.Web.Data.Models;

namespace MyNewsFeed.Tests;

public class PersonalFeaturesTests
{
    [Theory]
    [InlineData("Kansas", "Kansas Jayhawks", true)]
    [InlineData("Kansas", "Kansas State Wildcats", false)]
    [InlineData("Kansas", "Kansas City Current", false)]
    [InlineData("Iowa State", "Iowa State Cyclones", true)]
    [InlineData("Atlanta Dream", "atlanta dream", true)]
    public void Favorites_match_exact_teams_and_known_college_aliases(string first, string second, bool match) =>
        Assert.Equal(match, TeamNames.Matches(first, second));

    [Fact]
    public void Timeline_orders_confirmed_starts_then_date_only_items_and_preserves_status()
    {
        var game = new EventItem(1, "https://test.invalid/game", "Game", "Soccer", "Away", false, null,
            new DateOnly(2026, 10, 10), new DateTime(2026, 10, 10, 23, 0, 0), false,
            null, null, null, null, null, "Home");
        var episode = new TvEpisodeItem(1, "Series", null, null, "Episode", 1, 1, false,
            game.EventDate, game.StartsAtUtc!.Value.AddHours(-1), "https://test.invalid/tv");
        var postponed = game with { Id = 2, SchoolName = "Postponed team", ScheduleStatus = "STATUS_POSTPONED" };
        var rows = TodayTonight.Build([postponed, game], [episode], [], FeedDates.Create("America/Chicago"));
        Assert.Equal("TV & Streaming", rows[0].Kind);
        Assert.Equal("6:00 P.M. CT", rows[1].TimeLabel);
        Assert.Null(rows[2].StartsUtc);
        Assert.Equal("Postponed · Time TBD", rows[2].TimeLabel);
    }

    [Fact]
    public void Timeline_excludes_vod_releases_but_keeps_other_movie_releases()
    {
        var types = new[] { "digital", "digital_rental", "digital_purchase", "theatrical", "subscription", "disc" };
        var movies = types.Select((type, id) => new MovieReleaseItem(id, type, null, null, null,
            null, null, new DateOnly(2026, 10, 13), "confirmed", type, null, null, false,
            "Provider", "https://test.invalid/movie")).ToList();
        var rows = TodayTonight.Build([], [], movies, FeedDates.Create("America/Chicago"));
        Assert.Equal(new[] { "disc", "subscription", "theatrical" }, rows.Select(r => r.Title));
    }

    [Theory]
    [InlineData("espn-wnba", "Basketball", "basketball", "wnba", null)]
    [InlineData("espn-mlb", "Baseball", "baseball", "mlb", null)]
    [InlineData("big12", "Women's Basketball", "basketball", "womens-college-basketball", "8")]
    [InlineData("big12", "Football", "football", "college-football", "4")]
    public void Coverage_checks_the_correct_sport_league_and_conference(string provider, string sport,
        string espnSport, string league, string? group)
    {
        var feed = new SportsEventsType { ProviderKey = provider, SchoolName = "Kansas" };
        var follow = new ResultFollow { Enabled = true, Kind = group is null ? "league" : "conference",
            Sport = espnSport, League = league, GroupId = group };
        Assert.True(ScheduleCoverage.Covers(feed, sport, follow));
        follow.Enabled = false; Assert.False(ScheduleCoverage.Covers(feed, sport, follow));
        follow.Enabled = true; follow.League = "other"; Assert.False(ScheduleCoverage.Covers(feed, sport, follow));
    }

    [Fact]
    public void A_single_wnba_team_does_not_cover_a_league_wide_schedule()
    {
        var follow = new ResultFollow { Enabled = true, Kind = "team", Sport = "basketball", League = "wnba", TeamId = "18" };
        Assert.False(ScheduleCoverage.Covers(new SportsEventsType { ProviderKey = "espn-wnba" }, "Basketball", follow));
    }
    [Fact]
    public void A_successfully_checked_empty_provider_is_not_a_failure()
    {
        var now = new DateTime(2026, 10, 11, 12, 0, 0);
        Assert.Equal((HealthLevel.Warning, "No upcoming games"), ScraperHealth.ClassifyScheduleFeed(true, now, 0, 0, now, TimeSpan.FromHours(3)));
        Assert.Equal(HealthLevel.Problem, ScraperHealth.ClassifyScheduleFeed(true, null, 0, 0, now, TimeSpan.FromHours(3)).Item1);
    }

    [Fact]
    public void Schedule_groups_providers_by_league_with_confirmed_starts_first()
    {
        var game = new EventItem(1, "https://test.invalid/game", "Game", "Soccer", "Away", false, null,
            new DateOnly(2026, 10, 11), new DateTime(2026, 10, 11, 23, 0, 0), false,
            null, null, null, null, null, "Home", ProviderKey: "espn-nwsl");
        var early = game with { Id = 2, StartsAtUtc = game.StartsAtUtc!.Value.AddHours(-2) };
        var tbd = early with { Id = 3, TimeTbd = true };
        var college = game with { Id = 4, ProviderKey = "big12" };
        var groups = GameGroups.Schedule([tbd, game, college, early]);
        Assert.Equal(new[] { "NWSL", "Women's College Soccer" }, groups.Select(g => g.Name));
        Assert.Equal(new[] { 2, 1, 3 }, groups[0].Games.Select(g => g.Id));
    }

    [Fact]
    public void Result_groups_sort_games_by_start_time_within_each_league()
    {
        var game = new ResultItem("2", "MLB", "MLB", new DateTime(2026, 10, 11, 23, 0, 0), "Final",
            "Home", null, 1, null, "Away", null, 0, null, "home", null);
        var early = game with { EventId = "1", StartsAtUtc = game.StartsAtUtc.AddHours(-2) };
        var groups = GameGroups.Results([game, game with { EventId = "3", LeagueName = "WNBA" }, early]);
        Assert.Equal(new[] { "MLB", "WNBA" }, groups.Select(g => g.Name));
        Assert.Equal(new[] { "1", "2" }, groups[0].Games.Select(g => g.EventId));
    }

    [Theory]
    [InlineData("2025-26", "Men's Basketball", "2026-10-11", false)]
    [InlineData("2026-27", "Men's Basketball", "2026-10-11", true)]
    [InlineData("2026–27", "Women's Basketball", "2027-01-10", true)]
    [InlineData("2026", "Football", "2026-10-11", true)]
    [InlineData("2025", "Soccer", "2026-10-11", false)]
    public void College_standings_use_the_current_published_season(string season, string sport, string day, bool expected) =>
        Assert.Equal(expected, CollegeSeasons.Matches(season, sport, DateOnly.Parse(day)));

}
