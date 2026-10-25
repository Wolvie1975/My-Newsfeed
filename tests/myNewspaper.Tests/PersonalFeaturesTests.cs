using myNewspaper.Web.Data;
using myNewspaper.Web.Data.Models;

namespace myNewspaper.Tests;

public class PersonalFeaturesTests
{
    [Fact]
    public void Personal_highlight_keys_share_college_aliases_and_keep_professional_teams_distinct()
    {
        var keys = System.Text.Json.JsonSerializer.Deserialize<string[]>(
            SportsTeamPreferences.Keys("Kansas", "Kansas Jayhawks", "Kansas State Wildcats", "Kansas City Current", null));
        Assert.Equal(new[] { "kansas", "kansas state", "kansas city current" }, keys);
        Assert.Equal("[]", SportsTeamPreferences.Keys(null, " "));
    }

    [Theory]
    [InlineData("espn-epl", "Arsenal", "Chelsea", false)]
    [InlineData("espn-nhl", "St. Louis Blues", "Dallas Stars", true)]
    [InlineData("espn-nhl", "Dallas Stars", "St. Louis Blues", true)]
    [InlineData("espn-nhl", "Montreal Canadiens", "Washington Capitals", true)]
    [InlineData("espn-nhl", "Washington Capitals", "Montreal Canadiens", true)]
    [InlineData("espn-nhl", "St. Louis Blues", "Montreal Canadiens", true)]
    [InlineData("espn-nhl", "Colorado Avalanche", "Dallas Stars", false)]
    [InlineData("espn-nhl", "St. Louis CITY SC", "Dallas Stars", false)]
    [InlineData("espn-championship", "Wrexham", "Bristol City", true)]
    [InlineData("espn-championship", "Bristol City", "Wrexham", true)]
    [InlineData("espn-championship", "Wrexham AFC", "Bristol City", true)]
    [InlineData("espn-championship", "Coventry City", "Bristol City", false)]
    [InlineData("espn-mls", "Sporting Kansas City", "Inter Miami CF", true)]
    [InlineData("espn-mls", "Inter Miami CF", "Sporting KC", true)]
    [InlineData("espn-mls", "St. Louis CITY SC", "Seattle Sounders FC", true)]
    [InlineData("espn-mls", "Seattle Sounders FC", "St Louis City", true)]
    [InlineData("espn-mls", "Seattle Sounders FC", "Inter Miami CF", false)]
    [InlineData("espn-mls", "Kansas City Current", "Inter Miami CF", false)]
    [InlineData("espn-nwsl", "Kansas City Current", "Orlando Pride", true)]
    [InlineData("big12", "Kansas", "Iowa State", true)]
    [InlineData(null, "Kansas", "Iowa State", true)]
    public void Timeline_limits_english_and_mls_soccer_to_requested_clubs(
        string? provider, string home, string away, bool included)
    {
        var game = new EventItem(1, "https://test.invalid/game", "Game", "Soccer", away, false, null,
            new DateOnly(2026, 10, 14), null, true, null, null, null, null, null, home,
            ProviderKey: provider);
        var rows = TodayTonight.Build([game], FeedDates.Create("America/Chicago"));
        Assert.Equal(included ? 1 : 0, rows.Count);
    }

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
        var postponed = game with { Id = 2, SchoolName = "Postponed team", ScheduleStatus = "STATUS_POSTPONED" };
        var rows = TodayTonight.Build([postponed, game], FeedDates.Create("America/Chicago"));
        Assert.Equal(2, rows.Count);
        Assert.All(rows, row => Assert.Equal("Soccer", row.Kind));
        Assert.Equal("6:00 P.M. CT", rows[0].TimeLabel);
        Assert.Null(rows[1].StartsUtc);
        Assert.Equal("Postponed · Time TBD", rows[1].TimeLabel);
    }

    [Fact]
    public void Timeline_is_empty_without_games() =>
        Assert.Empty(TodayTonight.Build([], FeedDates.Create("America/Chicago")));

    [Theory]
    [InlineData("espn-wnba", "Basketball", "basketball", "wnba", null)]
    [InlineData("espn-mlb", "Baseball", "baseball", "mlb", null)]
    [InlineData("big12", "Women's Basketball", "basketball", "womens-college-basketball", "8")]
    [InlineData("big12", "Football", "football", "college-football", "4")]
    public void Coverage_checks_the_correct_sport_league_and_conference(string provider, string sport,
        string espnSport, string league, string? group)
    {
        var feed = new SportsEventsType { ProviderKey = provider, SchoolName = "Kansas" };
        var follow = new ResultFollow
        {
            Enabled = true,
            Kind = group is null ? "league" : "conference",
            Sport = espnSport,
            League = league,
            GroupId = group
        };
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
