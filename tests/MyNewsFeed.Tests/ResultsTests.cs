using System.Net;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MyNewsFeed.Web.Components.Events;
using MyNewsFeed.Web.Data;
using MyNewsFeed.Web.Data.Models;
using MyNewsFeed.Web.Scores;

namespace MyNewsFeed.Tests;

public class ResultsTests
{
    // ---- ESPN responses: no network, no database ----------------------------------------------------------------------

    // Trimmed from a real team schedule (Kansas volleyball): scores are {"value","displayValue"}, logos a list, and the
    // venue address has a city and a state.
    private const string ScheduleJson = """
        { "team": { "id": "2305" }, "events": [
          { "id": "401884318", "date": "2026-08-28T22:30Z", "name": "Kansas Jayhawks at Pittsburgh Panthers",
            "competitions": [ { "id": "401884318", "date": "2026-08-28T22:30Z",
              "venue": { "fullName": "Petersen Events Center", "address": { "city": "Pittsburgh", "state": "Pennsylvania" } },
              "competitors": [
                { "id": "221", "homeAway": "home", "winner": true,
                  "team": { "id": "221", "displayName": "Pittsburgh Panthers", "logos": [ { "href": "https://a.espncdn.com/i/teamlogos/ncaa/500/221.png" } ] },
                  "score": { "value": 3.0, "displayValue": "3" } },
                { "id": "2305", "homeAway": "away", "winner": false,
                  "team": { "id": "2305", "displayName": "Kansas Jayhawks", "logos": [ { "href": "https://a.espncdn.com/i/teamlogos/ncaa/500/2305.png" } ] },
                  "score": { "value": 1.0, "displayValue": "1" } } ],
              "status": { "type": { "name": "STATUS_FINAL", "state": "post", "completed": true, "description": "Final", "shortDetail": "Final" } } } ] },
          { "id": "401884400", "date": "2026-11-27T23:00Z",
            "competitions": [ { "date": "2026-11-27T23:00Z",
              "competitors": [
                { "homeAway": "home", "team": { "id": "2305", "displayName": "Kansas Jayhawks" } },
                { "homeAway": "away", "team": { "id": "2628", "displayName": "TCU Horned Frogs" } } ],
              "status": { "type": { "state": "pre", "completed": false, "shortDetail": "11/27 - 5:00 PM EST" } } } ] }
        ] }
        """;

    // Trimmed from a real NWSL scoreboard: scores are strings and a team has a single "logo".
    private const string ScoreboardJson = """
        { "leagues": [], "events": [
          { "id": "401854019", "date": "2026-10-04T17:00Z", "name": "Angel City FC at Gotham FC",
            "competitions": [ { "date": "2026-10-04T17:00Z",
              "status": { "type": { "name": "STATUS_FULL_TIME", "state": "post", "completed": true, "description": "Full Time", "shortDetail": "FT" } },
              "venue": { "fullName": "Sports Illustrated Stadium", "address": { "city": "Harrison, New Jersey", "country": "USA" } },
              "competitors": [
                { "id": "15364", "homeAway": "home", "winner": false, "score": "1",
                  "team": { "id": "15364", "displayName": "Gotham FC", "logo": "https://a.espncdn.com/i/teamlogos/soccer/500/15364.png" } },
                { "id": "21422", "homeAway": "away", "winner": true, "score": "1", "shootoutScore": 4,
                  "team": { "id": "21422", "displayName": "Angel City FC" } } ] } ] }
        ] }
        """;

    private static JsonElement Json(string text) => JsonDocument.Parse(text).RootElement;

    [Fact]
    public void A_schedule_game_is_read_with_its_score_logos_winner_and_venue()
    {
        var game = EspnClient.ParseGames(Json(ScheduleJson))[0];

        Assert.Equal("401884318", game.EventId);
        Assert.Equal(new DateTime(2026, 8, 28, 22, 30, 0, DateTimeKind.Utc), game.StartsAtUtc);
        Assert.Equal(("post", true, "Final"), (game.State, game.Completed, game.StatusText));
        Assert.Equal(new EspnSide("221", "Pittsburgh Panthers", "https://a.espncdn.com/i/teamlogos/ncaa/500/221.png", 3, null), game.Home);
        Assert.Equal(new EspnSide("2305", "Kansas Jayhawks", "https://a.espncdn.com/i/teamlogos/ncaa/500/2305.png", 1, null), game.Away);
        Assert.Equal("home", game.WinnerSide);
        Assert.Equal("Pittsburgh, Pennsylvania / Petersen Events Center", game.Venue);
    }

    [Fact]
    public void An_upcoming_schedule_game_has_no_score_winner_or_venue()
    {
        var game = EspnClient.ParseGames(Json(ScheduleJson))[1];

        Assert.Equal(("pre", false), (game.State, game.Completed));
        Assert.Null(game.Home.Score);
        Assert.Null(game.Away.LogoUrl);
        Assert.Null(game.WinnerSide);
        Assert.Null(game.Venue);
    }

    [Fact]
    public void A_scoreboard_game_reads_string_scores_a_single_logo_and_a_shootout()
    {
        var game = Assert.Single(EspnClient.ParseGames(Json(ScoreboardJson)));

        Assert.Equal(new EspnSide("15364", "Gotham FC", "https://a.espncdn.com/i/teamlogos/soccer/500/15364.png", 1, null), game.Home);
        Assert.Equal(new EspnSide("21422", "Angel City FC", null, 1, 4), game.Away);
        Assert.Equal("away", game.WinnerSide);   // level after full time, won on the shoot-out
        Assert.Equal("FT", game.StatusText);
        Assert.Equal("Harrison, New Jersey / Sports Illustrated Stadium", game.Venue);
    }

    [Fact]
    public void An_event_without_both_a_home_and_an_away_side_is_skipped()
    {
        var games = EspnClient.ParseGames(Json("""
            { "events": [ { "id": "1", "date": "2026-10-01T00:00Z", "competitions": [ { "competitors": [
                { "homeAway": "home", "team": { "id": "1", "displayName": "TBD" } } ] } ] } ] }
            """));

        Assert.Empty(games);
    }

    [Fact]
    public void A_response_without_events_has_no_games() =>
        Assert.Empty(EspnClient.ParseGames(Json("""{ "leagues": [] }""")));

    [Fact]
    public void The_team_list_is_read_and_sorted_by_name()
    {
        var teams = EspnClient.ParseTeams(Json("""
            { "sports": [ { "leagues": [ { "teams": [
                { "team": { "id": "2305", "displayName": "Kansas Jayhawks", "logos": [ { "href": "https://x.test/2305.png" } ] } },
                { "team": { "id": "2000", "displayName": "Abilene Christian Wildcats" } } ] } ] } ] }
            """));

        Assert.Equal([new EspnTeam("2000", "Abilene Christian Wildcats", null), new EspnTeam("2305", "Kansas Jayhawks", "https://x.test/2305.png")], teams);
    }

    // ---- the HTTP client, against a fake handler ------------------------------------------------------------------------

    private sealed class FakeHandler(HttpStatusCode status, string body) : HttpMessageHandler
    {
        public List<Uri> Requests { get; } = [];

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
        {
            Requests.Add(request.RequestUri!);
            return Task.FromResult(new HttpResponseMessage(status) { Content = new StringContent(body, Encoding.UTF8, "application/json") });
        }
    }

    private static EspnClient Client(FakeHandler handler) =>
        new(new HttpClient(handler), Options.Create(new ScoresOptions()));

    [Fact]
    public async Task A_team_schedule_and_a_scoreboard_day_are_requested_at_the_right_addresses()
    {
        var handler = new FakeHandler(HttpStatusCode.OK, ScheduleJson);
        var client = Client(handler);

        var schedule = await client.GetTeamScheduleAsync("volleyball", "womens-college-volleyball", "2305", seasonType: 2);
        await client.GetScoreboardAsync("soccer", "usa.nwsl", new DateOnly(2026, 10, 4));

        Assert.True(schedule.Ok);
        Assert.Equal(2, schedule.Value!.Count);
        Assert.Equal(
            [
                "https://site.api.espn.com/apis/site/v2/sports/volleyball/womens-college-volleyball/teams/2305/schedule?seasontype=2",
                "https://site.api.espn.com/apis/site/v2/sports/soccer/usa.nwsl/scoreboard?dates=20261004&limit=500",
            ],
            handler.Requests.Select(u => u.ToString()));
    }

    [Fact]
    public async Task An_http_error_is_reported_not_thrown()
    {
        // What ESPN answers for a date range, which it no longer accepts.
        var handler = new FakeHandler(HttpStatusCode.BadRequest, """{"code":400,"message":"Failed to get events endpoint."}""");

        var result = await Client(handler).GetScoreboardAsync("baseball", "mlb", new DateOnly(2026, 10, 4));

        Assert.False(result.Ok);
        Assert.Contains("HTTP 400", result.Error);
    }

    [Fact]
    public async Task A_response_of_the_wrong_shape_is_an_error_not_a_crash()
    {
        var handler = new FakeHandler(HttpStatusCode.OK, """{ "events": [ { "id": "1" } ] }""");

        var result = await Client(handler).GetTeamScheduleAsync("football", "nfl", "12", seasonType: 2);

        Assert.False(result.Ok);
        Assert.StartsWith("Unexpected response", result.Error);
    }

    // ---- sync planning ---------------------------------------------------------------------------------------------------

    [Fact]
    public void A_new_league_follow_backfills_the_configured_days_through_today()
    {
        var days = ScoresSync.DaysToFetch(null, new DateOnly(2026, 10, 5), backfillDays: 3);

        Assert.Equal([new(2026, 10, 2), new(2026, 10, 3), new(2026, 10, 4), new DateOnly(2026, 10, 5)], days);
    }

    [Fact]
    public void A_synced_league_refetches_from_the_day_before_its_last_sync()
    {
        // Last synced 2026-10-05 01:00 UTC, which is still Oct 4 in New York; late games of Oct 3 can finish after midnight.
        var days = ScoresSync.DaysToFetch(new DateTime(2026, 10, 5, 1, 0, 0), new DateOnly(2026, 10, 5), backfillDays: 7);

        Assert.Equal([new(2026, 10, 3), new(2026, 10, 4), new DateOnly(2026, 10, 5)], days);
    }

    [Fact]
    public void A_league_switched_off_for_weeks_backfills_no_further_than_the_limit()
    {
        var days = ScoresSync.DaysToFetch(new DateTime(2026, 8, 1), new DateOnly(2026, 10, 5), backfillDays: 2);

        Assert.Equal([new(2026, 10, 3), new(2026, 10, 4), new DateOnly(2026, 10, 5)], days);
    }

    [Theory]
    [InlineData("team", "Kansas Jayhawks", "volleyball", "womens-college-volleyball", "Kansas Jayhawks · Volleyball")]
    [InlineData("team", "Kansas Jayhawks", "basketball", "mens-college-basketball", "Kansas Jayhawks · Men's Basketball")]
    [InlineData("league", "NFL", "football", "nfl", "NFL")]
    [InlineData("team", "Somebody", "cricket", "ipl", "Somebody · ipl")]
    public void Follows_are_labelled_so_one_school_in_several_sports_reads_clearly(string kind, string name, string sport, string league, string expected) =>
        Assert.Equal(expected, EspnLeagues.FollowLabel(kind, name, sport, league));

    // ---- display ---------------------------------------------------------------------------------------------------------

    private static ResultItem Result(int? away = 1, int? home = 3, string? winner = "home", int? awayShootout = null, int? homeShootout = null,
        string? venue = "Pittsburgh, Pennsylvania / Petersen Events Center") =>
        new("1", "Women's College Volleyball", new DateTime(2026, 8, 28, 22, 30, 0), "Final",
            "Pittsburgh Panthers", null, home, homeShootout, "Kansas Jayhawks", null, away, awayShootout, winner, venue);

    private static async Task<string> RenderAsync(ResultItem item)
    {
        var services = new ServiceCollection().AddLogging().BuildServiceProvider();
        await using var renderer = new HtmlRenderer(services, services.GetRequiredService<ILoggerFactory>());
        return await renderer.Dispatcher.InvokeAsync(async () =>
        {
            var output = await renderer.RenderComponentAsync<ResultRow>(ParameterView.FromDictionary(
                new Dictionary<string, object?> { [nameof(ResultRow.Item)] = item }));
            return WebUtility.HtmlDecode(output.ToHtmlString());
        });
    }

    [Fact]
    public async Task A_result_lists_the_away_team_first_with_the_score_in_the_same_order()
    {
        var html = await RenderAsync(Result());

        Assert.True(html.IndexOf("Kansas Jayhawks", StringComparison.Ordinal) < html.IndexOf("Pittsburgh Panthers", StringComparison.Ordinal));
        Assert.Contains(">1 – 3<", html);
        Assert.Contains(">Final<", html);
        Assert.Contains("td-rhome is-winner", html);
        Assert.DoesNotContain("td-raway is-winner", html);
        Assert.Contains("Petersen Events Center", html);
    }

    [Fact]
    public async Task A_shootout_is_shown_and_its_winner_bolded()
    {
        var html = await RenderAsync(Result(away: 1, home: 1, winner: "away", awayShootout: 4, homeShootout: 3));

        Assert.Contains("Final · Shootout 4 – 3", html);
        Assert.Contains("td-raway is-winner", html);
    }

    [Fact]
    public async Task A_draw_bolds_neither_team_and_a_missing_venue_hides_its_cell()
    {
        var html = await RenderAsync(Result(away: 0, home: 0, winner: null, venue: null));

        Assert.DoesNotContain("is-winner", html);
        Assert.Contains("td-rloc--none", html);
    }

    // ---- against the database (each test rolls back) -------------------------------------------------------------------

    private static WebScraperContext Create() => new(
        new DbContextOptionsBuilder<WebScraperContext>()
            .UseSqlServer("Server=sql2025,1433;Database=WebScraper;User Id=sa;Password=Fedora_Dev_2025!;Encrypt=False;TrustServerCertificate=True;")
            .Options);

    // Made-up leagues and ids so the tests never meet real rows.
    private static string Tag() => "t" + Guid.NewGuid().ToString("N")[..8];

    private static EspnGame Game(string id, string home, string away, bool completed = true, int? hs = 2, int? @as = 1) =>
        new(id, new DateTime(2026, 9, 20, 18, 0, 0, DateTimeKind.Utc), completed ? "post" : "pre", completed, completed ? "Final" : null,
            new EspnSide(home, $"Team {home}", null, hs, null), new EspnSide(away, $"Team {away}", null, @as, null),
            completed ? "home" : null, null);

    [Fact]
    public async Task Upserting_adds_new_games_then_updates_only_what_changed()
    {
        await using var db = Create();
        await using var tx = await db.Database.BeginTransactionAsync();
        var league = Tag();
        var now = new DateTime(2026, 10, 1, 12, 0, 0);

        Assert.Equal(2, await ScoresSync.UpsertGamesAsync(db, "sport", league,
            [Game("1", "a", "b", completed: false, hs: null, @as: null), Game("2", "c", "d")], now));
        await db.SaveChangesAsync();

        // Next sync: game 1 has been played, game 2 is unchanged.
        var changed = await ScoresSync.UpsertGamesAsync(db, "sport", league, [Game("1", "a", "b"), Game("2", "c", "d")], now.AddHours(3));
        await db.SaveChangesAsync();

        Assert.Equal(1, changed);
        var row = await db.ResultGames.AsNoTracking().SingleAsync(g => g.League == league && g.EventId == "1");
        Assert.Equal(("post", true, 2, 1, "home"), (row.State, row.Completed, row.HomeScore, row.AwayScore, row.WinnerSide));
        Assert.Equal(now, row.FirstSeenAt);
        Assert.Equal(now.AddHours(3), row.LastSeenAt);
    }

    [Fact]
    public async Task Results_show_finished_games_of_followed_leagues_and_of_followed_teams_in_their_own_league()
    {
        await using var db = Create();
        await using var tx = await db.Database.BeginTransactionAsync();
        // Start from no follows inside this transaction, so only the ones below count.
        await db.ResultFollows.ExecuteDeleteAsync();

        string pro = Tag(), college = Tag(), otherCollege = Tag();
        var leagueFollow = new ResultFollow { Kind = FollowKinds.League, Sport = "s", League = pro, Name = "Pro league", Enabled = true };
        var teamFollow = new ResultFollow { Kind = FollowKinds.Team, Sport = "s", League = college, TeamId = "2305", Name = "Kansas Jayhawks", Enabled = true };
        db.ResultFollows.AddRange(leagueFollow, teamFollow);

        await ScoresSync.UpsertGamesAsync(db, "s", pro, [Game("p1", "x", "y"), Game("p2", "x", "y", completed: false)], DateTime.UtcNow);
        await ScoresSync.UpsertGamesAsync(db, "s", college, [Game("c1", "2305", "z"), Game("c2", "z", "2305"), Game("c3", "q", "r")], DateTime.UtcNow);
        // The same team id in a different league is a different team (ESPN ids are per sport).
        await ScoresSync.UpsertGamesAsync(db, "s", otherCollege, [Game("o1", "2305", "z")], DateTime.UtcNow);
        await db.SaveChangesAsync();

        var all = await ResultsQuery.GetResultsAsync(db, followId: null);
        Assert.Equal(["c1", "c2", "p1"], all.Items.Select(r => r.EventId).Order());
        Assert.Equal(3, all.Total);

        var teamOnly = await ResultsQuery.GetResultsAsync(db, teamFollow.Id);
        Assert.Equal(["c1", "c2"], teamOnly.Items.Select(r => r.EventId).Order());

        // A disabled follow's results leave the page, and its chip goes.
        leagueFollow.Enabled = false;
        await db.SaveChangesAsync();
        Assert.Equal(["c1", "c2"], (await ResultsQuery.GetResultsAsync(db, null)).Items.Select(r => r.EventId).Order());
        Assert.Equal([$"Kansas Jayhawks · {college}"], (await ResultsQuery.GetFollowsAsync(db)).Select(f => f.Label));
    }

    [Fact]
    public async Task A_league_or_a_team_cannot_be_followed_twice()
    {
        await using var db = Create();
        await using var tx = await db.Database.BeginTransactionAsync();
        var league = Tag();
        db.ResultFollows.Add(new ResultFollow { Kind = FollowKinds.League, Sport = "s", League = league, Name = "L" });
        db.ResultFollows.Add(new ResultFollow { Kind = FollowKinds.Team, Sport = "s", League = league, TeamId = "1", Name = "T" });
        await db.SaveChangesAsync();

        db.ResultFollows.Add(new ResultFollow { Kind = FollowKinds.League, Sport = "s", League = league, Name = "L again" });
        var ex = await Assert.ThrowsAsync<DbUpdateException>(() => db.SaveChangesAsync());
        Assert.True(AdminData.IsUniqueViolation(ex));
        db.ChangeTracker.Clear();

        db.ResultFollows.Add(new ResultFollow { Kind = FollowKinds.Team, Sport = "s", League = league, TeamId = "1", Name = "T again" });
        ex = await Assert.ThrowsAsync<DbUpdateException>(() => db.SaveChangesAsync());
        Assert.True(AdminData.IsUniqueViolation(ex));
    }

    [Fact]
    public async Task A_team_follow_needs_a_team_id_and_a_league_follow_cannot_have_one()
    {
        await using var db = Create();
        await using var tx = await db.Database.BeginTransactionAsync();

        db.ResultFollows.Add(new ResultFollow { Kind = FollowKinds.Team, Sport = "s", League = Tag(), Name = "No team id" });
        await Assert.ThrowsAsync<DbUpdateException>(() => db.SaveChangesAsync());
        db.ChangeTracker.Clear();

        db.ResultFollows.Add(new ResultFollow { Kind = FollowKinds.League, Sport = "s", League = Tag(), TeamId = "1", Name = "League with a team" });
        await Assert.ThrowsAsync<DbUpdateException>(() => db.SaveChangesAsync());
    }

    [Fact]
    public async Task A_new_follow_is_enabled_unless_saved_disabled()
    {
        await using var db = Create();
        await using var tx = await db.Database.BeginTransactionAsync();
        var on = new ResultFollow { Kind = FollowKinds.League, Sport = "s", League = Tag(), Name = "On", Enabled = true };
        var off = new ResultFollow { Kind = FollowKinds.League, Sport = "s", League = Tag(), Name = "Off", Enabled = false };
        db.ResultFollows.AddRange(on, off);
        await db.SaveChangesAsync();

        Assert.True((await db.ResultFollows.AsNoTracking().SingleAsync(f => f.Id == on.Id)).Enabled);
        Assert.False((await db.ResultFollows.AsNoTracking().SingleAsync(f => f.Id == off.Id)).Enabled);
    }
}
