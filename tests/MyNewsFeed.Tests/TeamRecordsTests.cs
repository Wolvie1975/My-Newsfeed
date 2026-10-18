using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using MyNewsFeed.Web.Components;
using MyNewsFeed.Web.Components.Events;
using MyNewsFeed.Web.Data;
using MyNewsFeed.Web.Data.Models;

namespace MyNewsFeed.Tests;

public class TeamRecordsTests
{
    private static readonly DateTime Now = new(2026, 10, 18, 12, 0, 0);
    private static readonly DateOnly Today = new(2026, 10, 18);
    private static SportsStandingsSource Source(string provider = "big12", string sport = "Football") =>
        new() { Id = 1, Enabled = true, ProviderKey = provider, Sport = sport, LatestSeason = "2026", LastSuccessAt = Now };
    private static SportsStanding Row(string json, string name = "Kansas", string id = "13", string season = "2026") =>
        new() { SourceId = 1, TeamName = name, TeamId = id, Season = season, IsCurrent = true, FetchedAt = Now, StatsJson = json };
    private const string College = """[{"displayName":"Overall","name":"column3","displayValue":"5-2"},{"displayName":"Conf","name":"column1","displayValue":"2-1"}]""";
    private const string Pro = """[{"name":"wins","displayValue":"8"},{"name":"losses","value":2},{"name":"ties","displayValue":"3"},{"name":"otLosses","displayValue":"1"}]""";

    [Theory]
    [InlineData("espn-mlb", "Baseball", "8–2 W–L")]
    [InlineData("espn-wnba", "Women's Basketball", "8–2 W–L")]
    [InlineData("espn-mls", "Soccer", "8–2–3 W–L–D")]
    [InlineData("espn-nhl", "Hockey", "8–2–1 W–L–OT")]
    [InlineData("espn-nfl", "Football", "8–2–3 W–L–T")]
    public void Professional_records_have_sport_specific_order(string provider, string sport, string expected)
    {
        var records = TeamRecords.Build([Source(provider, sport)], [Row(Pro)], Today, Now);
        Assert.Equal(expected, records.Find(provider, sport, "Kansas", "13")?.Text);
        Assert.Null(records.Find(provider, sport, "Kansas", "99"));
        Assert.Null(records.Find("other-provider", sport, "Kansas", "13"));
    }

    [Fact]
    public void Wnba_schedule_basketball_label_resolves_to_womens_standings()
    {
        var records = TeamRecords.Build([Source("espn-wnba", "Women's Basketball")], [Row(Pro)], Today, Now);
        Assert.Equal("8–2 W–L", records.Find("espn-wnba", "Basketball", "Kansas", "13")?.Text);
    }

    [Fact]
    public void College_aliases_match_by_name_and_sport_without_reusing_espn_ids()
    {
        var records = TeamRecords.Build([Source()], [Row(College)], Today, Now);
        Assert.Equal("5-2 (2-1)", records.Find("big12", "Football", "Kansas Jayhawks", "2305")?.Text);
        Assert.Null(records.Find("big12", "Soccer", "Kansas"));
        Assert.Null(records.Find("big12", "Football", "Houston", "13"));
    }

    [Fact]
    public void College_soccer_includes_draws_and_conference_record()
    {
        var json = """[{"displayName":"Overall Record","name":"a","displayValue":"8-2-3"},{"displayName":"Big 12 Record","name":"b","displayValue":"3-1-1"}]""";
        var records = TeamRecords.Build([Source(sport: "Soccer")], [Row(json)], Today, Now);
        Assert.Equal("8-2-3 (3-1-1)", records.Find("big12", "Soccer", "Kansas")?.Text);
    }

    [Fact]
    public void Old_college_seasons_disabled_noncurrent_missing_and_malformed_records_are_omitted()
    {
        var source = Source(sport: "Men's Basketball");
        Assert.Null(TeamRecords.Build([source], [Row(College, season: "2025-26")], Today, Now).Find("big12", source.Sport, "Kansas"));
        var row = Row(College, season: "2026-27");
        Assert.NotNull(TeamRecords.Build([source], [row], Today, Now).Find("big12", source.Sport, "Kansas"));
        source.Enabled = false;
        Assert.Null(TeamRecords.Build([source], [row], Today, Now).Find("big12", source.Sport, "Kansas"));
        source.Enabled = true; row.IsCurrent = false;
        Assert.Null(TeamRecords.Build([source], [row], Today, Now).Find("big12", source.Sport, "Kansas"));
        row.IsCurrent = true; row.StatsJson = "invalid json";
        Assert.Null(TeamRecords.Build([source], [row], Today, Now).Find("big12", source.Sport, "Kansas"));
    }

    [Fact]
    public void Professional_latest_season_and_stale_refresh_are_respected()
    {
        var source = Source("espn-nhl", "Hockey"); source.LatestSeason = "2027"; source.LastError = "Failed";
        var records = TeamRecords.Build([source], [Row(Pro, season: "2026"), Row(Pro, "Current", "99", "2027")], Today, Now);
        Assert.Null(records.Find(source.ProviderKey, source.Sport, "Kansas", "13"));
        Assert.True(records.Find(source.ProviderKey, source.Sport, "Current", "99")!.Stale);
        source.LastError = null;
        var old = Row(Pro, "Current", "99", "2027"); old.FetchedAt = Now.AddHours(-3);
        Assert.True(TeamRecords.Build([source], [old], Today, Now).Find(source.ProviderKey, source.Sport, "Current", "99")!.Stale);
    }

    [Fact]
    public void Missing_soccer_draw_count_is_not_invented()
    {
        var row = Row("""[{"name":"wins","displayValue":"2"},{"name":"losses","displayValue":"1"}]""");
        Assert.Null(TeamRecords.Build([Source("espn-mls", "Soccer")], [row], Today, Now).Find("espn-mls", "Soccer", "Kansas"));
    }

    [Fact]
    public async Task Home_and_game_rows_keep_records_with_the_correct_team_when_away()
    {
        var records = TeamRecords.Build([Source()], [Row(College)], Today, Now);
        var services = new ServiceCollection().AddLogging().AddSingleton(FeedDates.Create("America/Chicago")).BuildServiceProvider();
        await using var renderer = new HtmlRenderer(services, services.GetRequiredService<Microsoft.Extensions.Logging.ILoggerFactory>());
        var game = new EventItem(1, "https://example.test", "Kansas at Houston", "Football", "Houston", true,
            null, Today, Now, false, null, null, null, null, null, "Kansas", ProviderKey: "big12");
        var html = await renderer.Dispatcher.InvokeAsync(async () => (await renderer.RenderComponentAsync<GameRow>(ParameterView.FromDictionary(
            new Dictionary<string, object?> { ["Item"] = game, ["Records"] = records }))).ToHtmlString());
        var away = html[html.IndexOf("td-away")..html.IndexOf("td-home")];
        Assert.Contains("5-2 (2-1)", away);
        Assert.DoesNotContain("5-2 (2-1)", html[html.IndexOf("td-home")..]);
        var tonight = TodayTonight.Build([game], FeedDates.Create("America/Chicago")).Single();
        var homeHtml = await renderer.Dispatcher.InvokeAsync(async () => (await renderer.RenderComponentAsync<TonightMatchup>(ParameterView.FromDictionary(
            new Dictionary<string, object?> { ["Item"] = tonight, ["Records"] = records }))).ToHtmlString());
        Assert.Contains("5-2 (2-1)", homeHtml);
    }
}
