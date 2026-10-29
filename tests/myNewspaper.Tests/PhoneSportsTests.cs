using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using myNewspaper.Web.Components.Phone;
using myNewspaper.Web.Data;
using myNewspaper.Web.Data.Models;

namespace myNewspaper.Tests;

public class PhoneSportsTests
{
    private static EventItem Game(int id, string school, string opponent, int days = 0, bool? away = true) =>
        new(id, "https://example.test/" + id, "Game", "Football", opponent, away, null, new DateOnly(2026, 10, 28).AddDays(days),
            new DateTime(2026, 10, 28, 19, 0, 0, DateTimeKind.Utc).AddDays(days), false, null, null, null, null, null, school);
    private static SportsRankingTeam Ranked(string name, int rank) => new() { TeamName = name, Rank = rank };

    [Fact]
    public void Featured_prefers_two_ranked_teams_with_best_combined_rank_then_highest_ranked_participant()
    {
        var single = Game(1, "Kansas", "Unranked");
        var pair = Game(2, "BYU Cougars", "Utah Utes", 2);
        var weakerPair = Game(3, "Kansas", "BYU", 1);
        var poll = new[] { Ranked("Kansas", 15), Ranked("BYU", 6), Ranked("Utah", 7) };
        Assert.Equal(pair, PhoneSports.Featured([single, weakerPair, pair], poll));
        Assert.Equal(pair, PhoneSports.Featured([single, pair], [Ranked("BYU", 6), Ranked("Kansas", 15)]));
    }

    [Fact]
    public void Featured_without_ranks_uses_chronological_order_and_handles_no_games()
    {
        var later = Game(2, "Kansas", "TCU", 2);
        var first = Game(1, "BYU", "Utah");
        Assert.Equal(first, PhoneSports.Featured([later, first], []));
        Assert.Null(PhoneSports.Featured([], []));
        Assert.Contains("BYU at Utah", PhoneSports.Matchup(first));
        Assert.Equal("Utah at BYU", PhoneSports.Matchup(first with { IsAway = false }));
        Assert.Equal("BYU vs Utah", PhoneSports.Matchup(first with { IsNeutral = true }));
    }

    [Theory]
    [InlineData("4-0", 1d)]
    [InlineData("3-1", .75)]
    [InlineData("2-2-2", 1d / 3)]
    [InlineData("0-0", null)]
    [InlineData("—", null)]
    [InlineData("-1-4", null)]
    public void Win_bars_use_real_records_and_leave_unknown_values_absent(string record, double? expected) =>
        Assert.Equal(expected, PhoneSports.WinFraction(record));

    [Fact]
    public void Rank_moves_do_not_invent_previous_rank_or_label_missing_rank_as_new()
    {
        Assert.Null(PhoneSports.Movement(new() { Rank = 1 }));
        Assert.Equal("—", PhoneSports.MoveLabel(new() { Rank = 1 }));
        Assert.Equal("NEW", PhoneSports.MoveLabel(new() { Rank = 15, PreviouslyUnranked = true }));
        Assert.Equal("▲ 2", PhoneSports.MoveLabel(new() { Rank = 1, PreviousRank = 3 }));
        Assert.Equal("▼ 3", PhoneSports.MoveLabel(new() { Rank = 8, PreviousRank = 5 }));
        Assert.Equal("0%", PhoneSports.Percent(0, 0));
        Assert.Equal("50%", PhoneSports.Percent(50, 100));
    }

    private static async Task<string> RenderRankings(RankingsPage page)
    {
        await using var services = new ServiceCollection().AddLogging().AddSingleton(FeedDates.Create("America/Chicago")).BuildServiceProvider();
        await using var renderer = new HtmlRenderer(services, services.GetRequiredService<ILoggerFactory>());
        return await renderer.Dispatcher.InvokeAsync(async () => (await renderer.RenderComponentAsync<PhoneRankings>(ParameterView.FromDictionary(new Dictionary<string, object?> { ["Data"] = page }))).ToHtmlString());
    }

    [Fact]
    public async Task Poll_without_previous_ranks_has_no_moves_tiles_or_empty_folds()
    {
        var source = new SportsRankingSource { Id = 1, Enabled = true, DisplayName = "Test poll", LastSuccessAt = DateTime.UtcNow };
        var html = await RenderRankings(new([source], source, new() { PollName = "AP", PublishedAtUtc = DateTime.UtcNow }, [Ranked("Kansas", 1), Ranked("BYU", 2)]));
        Assert.DoesNotContain("Biggest moves", html);
        Assert.DoesNotContain("<details", html);
        Assert.Contains("ranking-source-m", html);
        Assert.Contains("<noscript>", html);
    }

    [Fact]
    public async Task Empty_and_failed_polls_keep_honest_states()
    {
        Assert.Contains("No ranking sources", await RenderRankings(new([], null, null, [])));
        Assert.Contains("Rankings not available yet", await RenderRankings(new([], new() { Id = 1 }, null, [])));
        Assert.Contains("temporarily unavailable", await RenderRankings(new([], null, null, [], "Rankings are temporarily unavailable.")));
    }

    private sealed class TestNavigation : NavigationManager
    {
        public TestNavigation() => Initialize("http://localhost/", "http://localhost/f1");
        protected override void NavigateToCore(string uri, bool forceLoad) { }
    }

    private static async Task<string> RenderSports<T>(Dictionary<string, object?> parameters) where T : IComponent
    {
        await using var services = new ServiceCollection().AddLogging().AddSingleton(FeedDates.Create("America/Chicago"))
            .AddSingleton<NavigationManager>(new TestNavigation())
            .AddSingleton(new myNewspaper.Web.Profiles.ProfileContext(Microsoft.Extensions.Options.Options.Create(new myNewspaper.Web.Profiles.ProfilesOptions())))
            .BuildServiceProvider();
        await using var renderer = new HtmlRenderer(services, services.GetRequiredService<ILoggerFactory>());
        return await renderer.Dispatcher.InvokeAsync(async () => (await renderer.RenderComponentAsync<T>(ParameterView.FromDictionary(parameters))).ToHtmlString());
    }

    [Fact]
    public async Task F1_date_only_race_keeps_TBD_and_does_not_invent_championship_rows()
    {
        var data = F1Page.Empty with
        {
            Source = new() { Enabled = true, LastSuccessAt = DateTime.UtcNow },
            Season = 2026,
            Races = [new() { Round = 1, RaceDate = DateOnly.FromDateTime(DateTime.UtcNow).AddDays(30), RaceName = "Fixture Grand Prix" }]
        };
        var html = await RenderSports<PhoneF1>(new() { ["Data"] = data });
        Assert.Contains("Fixture Grand Prix", html);
        Assert.Contains("Time TBD", html);
        Assert.DoesNotContain("Championship top 3", html);
        Assert.DoesNotContain("Drivers&#x27; standings", html);
        Assert.DoesNotContain(">error<", html);
    }

    [Fact]
    public async Task College_with_one_game_and_no_poll_or_standings_has_no_fabricated_data_folds()
    {
        var game = Game(1, "Kansas", "TCU");
        var html = await RenderSports<PhoneCollege>(new() { ["Events"] = new EventsPage([game], 1) });
        Assert.Contains("Kansas at TCU", html);
        Assert.Contains("Game of the week", html);
        Assert.DoesNotContain("Big 12 standings", html);
        Assert.DoesNotContain("Big 12 in the Top 25", html);
    }

    [Fact]
    public async Task Empty_college_and_disabled_F1_do_not_show_featured_content()
    {
        var college = await RenderSports<PhoneCollege>([]);
        Assert.Contains("No upcoming games", college);
        Assert.DoesNotContain("Game of the week", college);
        var f1 = await RenderSports<PhoneF1>([]);
        Assert.Contains("F1 data is not enabled", f1);
        Assert.DoesNotContain("Next race weekend", f1);
    }

    private static StandingTeam SoccerTeam() => new(new SportsStanding { TeamName = "Gotham FC", Position = 1, Rank = 1 },
        [new("gamesPlayed", "Games played", "29", 29), new("wins", "Wins", "17", 17), new("ties", "Draws", "7", 7),
         new("losses", "Losses", "5", 5), new("points", "Points", "58", 58)]);

    [Theory]
    [InlineData("espn-nwsl")]
    [InlineData("espn-mls")]
    [InlineData("espn-championship")]
    [InlineData("espn-epl")]
    public void Soccer_highlights_published_points_then_wins_draws_losses(string provider)
    {
        var team = SoccerTeam();
        var group = new StandingGroup("soccer", "Regular season", [team], StandingsQuery.Columns(provider, [team], "Soccer"));
        Assert.True(PhoneSports.IsSoccer(provider, "Soccer"));
        Assert.Equal("PTS", PhoneSports.PrimaryLabel(group, true));
        Assert.Equal("58", PhoneSports.Primary(group, team, true));
        Assert.Equal("W 17 · D 7 · L 5", PhoneSports.Secondary(group, team, true));
        Assert.Equal("gamesPlayed", group.Columns[0].Name);
    }

    [Fact]
    public void Missing_points_are_unknown_instead_of_games_played_or_calculated_points()
    {
        var team = SoccerTeam();
        var group = new StandingGroup("soccer", "Regular season", [team], StandingsQuery.Columns("espn-nwsl", [team], "Soccer"));
        var missing = team with { Statistics = team.Statistics.Where(s => s.Name != "points").ToArray() };
        Assert.Equal("—", PhoneSports.Primary(group, missing, true));
        Assert.Equal("W 17 · D 7 · L 5", PhoneSports.Secondary(group, missing, true));
    }

    [Fact]
    public void College_soccer_uses_conference_points_and_the_correct_draws_position()
    {
        var team = new StandingTeam(new() { TeamName = "Kansas" }, [new("column1", "Big 12 Record", "5-1-2"), new("column3", "Points", "17")]);
        var group = new StandingGroup("big12", "Big 12", [team], StandingsQuery.Columns("big12", [team], "Soccer"));
        Assert.True(PhoneSports.IsSoccer("big12", "Soccer"));
        Assert.Equal("17", PhoneSports.Primary(group, team, true));
        Assert.Equal("W 5 · D 2 · L 1", PhoneSports.Secondary(group, team, true));
        Assert.False(PhoneSports.IsSoccer("big12", "Football"));
        Assert.False(PhoneSports.IsSoccer("espn-nhl", "Hockey"));
    }

    [Fact]
    public async Task Soccer_leader_personal_tile_and_rows_all_render_points_and_complete_record()
    {
        var team = SoccerTeam();
        var source = new SportsStandingsSource { Id = 1, Enabled = true, ProviderKey = "espn-nwsl", Sport = "Soccer", DisplayName = "NWSL", LastSuccessAt = DateTime.UtcNow };
        var teams = Enumerable.Range(1, 7).Select(i => new StandingTeam(new SportsStanding { TeamName = i == 1 ? "Gotham FC" : "Team " + i, Position = i, Rank = i }, team.Statistics)).ToArray();
        var group = new StandingGroup("soccer", "Regular season", teams, StandingsQuery.Columns(source.ProviderKey, teams, source.Sport));
        var html = System.Net.WebUtility.HtmlDecode(await RenderSports<PhoneStandings>(new() { ["Data"] = new StandingsPage([source], source, [group]) }));
        Assert.Contains("mx-number mx-number--record\">58</div>", html);
        Assert.Contains("mx-tile__value\">58 pts</b>", html);
        Assert.Contains("W 17 · D 7 · L 5", html);
        Assert.Contains(">PTS</span>", html);
        Assert.DoesNotContain(">29<", html);
        var rows = System.Net.WebUtility.HtmlDecode(await RenderSports<PhoneStandingRows>(new() { ["Group"] = group, ["Teams"] = new[] { team }, ["Soccer"] = true }));
        Assert.Contains("<strong>58</strong>", rows);
        Assert.Contains("W 17 · D 7 · L 5", rows);
    }

    [Theory]
    [InlineData("espn-nwsl", "Soccer", true)]
    [InlineData("espn-epl", "Soccer", true)]
    [InlineData("espn-championship", "Soccer", true)]
    [InlineData("big12", "Soccer", true)]
    [InlineData("espn-mls", "Soccer", false)]
    [InlineData("big12", "Football", false)]
    public async Task Single_table_soccer_leagues_list_every_place_after_the_leader_in_one_section(string provider, string sport, bool single)
    {
        var stats = SoccerTeam().Statistics;
        var source = new SportsStandingsSource { Id = 1, Enabled = true, ProviderKey = provider, Sport = sport, DisplayName = "League", LastSuccessAt = DateTime.UtcNow };
        var teams = Enumerable.Range(1, 9).Select(i => new StandingTeam(new SportsStanding { TeamName = "Team " + i, Position = i, Rank = i }, stats)).ToArray();
        var group = new StandingGroup("soccer", "Regular season", teams, StandingsQuery.Columns(provider, teams, sport));
        var html = System.Net.WebUtility.HtmlDecode(await RenderSports<PhoneStandings>(new() { ["Data"] = new StandingsPage([source], source, [group]) }));
        Assert.Equal(single, html.Contains("The rest of the table"));
        Assert.Equal(!single, html.Contains("Places 7–9"));
    }
}
