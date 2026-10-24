using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using myNewspaper.Web.Components.BoxScores;
using myNewspaper.Web.Data;
namespace myNewspaper.Tests;

public class BoxScoreTests
{
    private static readonly BoxIdentity Id = new("football","nfl","123");
    private const string Payload = """
    {"version":1,"sport":"football","league":"nfl","eventId":"123","teams":[
      {"id":"a","name":"Away <club>","homeAway":"away","lineScores":[{"period":1,"displayValue":"0"}],"statistics":[{"group":"offense","key":"yards","label":"Yards","value":"0"},{"group":null,"key":"possession","label":"Possession","value":"50.0%"}]},
      {"id":"h","name":"Home","homeAway":"home","statistics":[{"group":null,"key":"possession","label":"Possession","value":"49.9%"},{"group":"offense","key":"yards","label":"Yards","value":"123.50"}]}],
      "playerGroups":[{"teamId":"a","name":"Passing","columns":[{"label":null},{"key":"yards","label":"YDS","description":"Yards"},{"label":"TD"}],
      "athletes":[{"name":"Player <script>","starter":null,"stats":["ignore","0","2"]}],"totals":["ignore","10.25"]}]}
    """;
    [Fact]
    public async Task Sparse_baseball_periods_keep_labels_with_their_values()
    {
        var data=new BoxData(new("baseball","mlb","123"),new BoxTeam[]{
            new("a","Away","away",Array.Empty<BoxStat>(),new BoxLine[]{new("1st inning","0"),new("3rd inning","2")}),
            new("h","Home","home",Array.Empty<BoxStat>(),new BoxLine[]{new("1st inning","1"),new("2nd inning","0"),new("3rd inning","X")})
        },Array.Empty<BoxPlayers>());
        using var services=new ServiceCollection().AddLogging().BuildServiceProvider();
        await using var renderer=new HtmlRenderer(services,services.GetRequiredService<ILoggerFactory>());
        var html=await renderer.Dispatcher.InvokeAsync(async()=> (await renderer.RenderComponentAsync<BaseballBoxScore>(ParameterView.FromDictionary(new Dictionary<string,object?>{["Data"]=data}))).ToHtmlString());
        Assert.Contains("<th scope=\"row\">3rd inning</th><td>2</td>",html);
        Assert.Contains("<th scope=\"row\">2nd inning</th><td>0</td>",html);
        Assert.Contains("<th scope=\"row\">3rd inning</th><td>X</td>",html);
        Assert.DoesNotContain("<th scope=\"col\" title=\"2nd inning\">",html);
    }
    [Fact]
    public async Task Football_categories_pair_teams_preserving_extra_groups_and_source_columns()
    {
        var parsed=BoxData.Parse(Payload,Id)!;
        var passing=parsed.Players[0];
        var data=parsed with {Players=new BoxPlayers[]{passing with{Name="punting"},passing with{Name="passing"},passing with{Name="passing",TeamId="h"},passing with{Name="Additional <detail>"}}};
        using var services=new ServiceCollection().AddLogging().BuildServiceProvider();
        await using var renderer=new HtmlRenderer(services,services.GetRequiredService<ILoggerFactory>());
        var html=await renderer.Dispatcher.InvokeAsync(async()=> (await renderer.RenderComponentAsync<FootballBoxScore>(ParameterView.FromDictionary(new Dictionary<string,object?>{["Data"]=data}))).ToHtmlString());
        Assert.True(html.IndexOf("<h3>Passing</h3>",StringComparison.Ordinal)<html.IndexOf("<h3>Punting</h3>",StringComparison.Ordinal));
        Assert.Contains("<h4>Home</h4>",html);Assert.Contains("Away &lt;club&gt;",html);
        Assert.Contains("Additional &lt;detail&gt;",html);Assert.Contains("Player &lt;script&gt;",html);
        Assert.Contains("<td>0</td><td>2</td>",html);Assert.Contains("10.25",html);
        Assert.Contains("role=\"region\"",html);
    }
    [Fact]
    public async Task Soccer_comparison_aligns_group_keys_and_preserves_missing_values()
    {
        var data=new BoxData(new("soccer","usa.1","123"),new BoxTeam[]{
            new("a","Away <club>","away",new BoxStat[]{new(null,"possession","Possession","50.0%"),new(null,"shots","Shots","0"),new("passing","shots","Passes","123.50")},Array.Empty<BoxLine>()),
            new("h","Home","home",new BoxStat[]{new(null,"shots","Shots","9"),new(null,"possession","Possession","49.9%")},Array.Empty<BoxLine>())
        },Array.Empty<BoxPlayers>());
        using var services=new ServiceCollection().AddLogging().BuildServiceProvider();
        await using var renderer=new HtmlRenderer(services,services.GetRequiredService<ILoggerFactory>());
        var html=await renderer.Dispatcher.InvokeAsync(async()=> (await renderer.RenderComponentAsync<SoccerTeamStats>(ParameterView.FromDictionary(new Dictionary<string,object?>{["Data"]=data}))).ToHtmlString());
        Assert.Contains("Away &lt;club&gt;",html);
        Assert.Contains("<td>50.0%</td><th scope=\"row\">Possession</th><td>49.9%</td>",html);
        Assert.Contains("<td>0</td><th scope=\"row\">Shots</th><td>9</td>",html);
        Assert.Contains("<td>123.50</td><th scope=\"row\">Passes</th><td>&#x2014;</td>",html);
    }
    [Fact]
    public void Parser_preserves_zero_precision_periods_nullable_metadata_and_column_indices()
    {
        var data=Assert.IsType<BoxData>(BoxData.Parse(Payload,Id));
        Assert.Equal("Box score",data.Label);Assert.Equal("0",data.Teams[0].Statistics[0].Value);
        Assert.Equal("1",Assert.Single(data.Teams[0].Lines).Period);
        var group=Assert.Single(data.Players);Assert.Equal(new[]{"0","2"},Assert.Single(group.Athletes).Stats);
        Assert.Null(group.Athletes[0].Starter);Assert.Equal(new string?[]{"10.25",null},group.Totals);
        Assert.Equal("Game stats",BoxData.Parse(Payload.Replace("\"playerGroups\"","\"ignored\""),Id)!.Label);
    }
    [Theory]
    [InlineData("garbage")]
    [InlineData("[]")]
    [InlineData("{\"version\":2}")]
    [InlineData("{\"version\":\"one\"}")]
    public void Malformed_or_unsupported_payloads_are_unavailable(string payload)=>Assert.Null(BoxData.Parse(payload,Id));
    [Fact]
    public void Composite_identity_and_empty_payloads_do_not_leak_stats()
    {
        Assert.Null(BoxData.Parse(Payload,new("football","college-football","123")));
        Assert.Null(BoxData.Parse(Payload,new("soccer","nfl","123")));
        Assert.Null(BoxData.Parse(Payload,new("football","nfl","124")));
        Assert.Null(BoxData.Parse("""{"version":1,"sport":"football","league":"nfl","eventId":"123","teams":[]} """,Id));
        Assert.False(new BoxIdentity("basketball","womens-college-basketball","123").Valid);
    }
    [Theory]
    [InlineData("javascript:alert(1)",false)]
    [InlineData("https://evil.invalid/game/123",false)]
    [InlineData("https://www.espn.com/nfl/boxscore/_/gameId/124",false)]
    [InlineData("https://www.espn.com/nfl/boxscore/_/gameId/123",true)]
    public void Provider_links_require_https_espn_and_matching_event(string url,bool valid)=>Assert.Equal(valid,BoxScoresRepository.ProviderUrl(url,Id)!=null);
    [Fact]
    public async Task Rendered_tables_encode_provider_text_and_preserve_values()
    {
        using var services=new ServiceCollection().AddLogging().AddSingleton(FeedDates.Create("America/Chicago")).BuildServiceProvider();
        await using var renderer=new HtmlRenderer(services,services.GetRequiredService<ILoggerFactory>());
        var html=await renderer.Dispatcher.InvokeAsync(async()=> (await renderer.RenderComponentAsync<BoxScoreContent>(ParameterView.FromDictionary(new Dictionary<string,object?>{["Detail"]=new BoxDetail(BoxData.Parse(Payload,Id)!,null,null)}))).ToHtmlString());
        Assert.Contains("Away &lt;club&gt;",html);Assert.Contains("Player &lt;script&gt;",html);
        Assert.DoesNotContain("<script>",html);Assert.Contains("123.50",html);Assert.Contains("50.0%",html);
        Assert.Contains("role=\"region\"",html);Assert.Contains("scope=\"col\"",html);
    }
    [Fact]
    public async Task Baseball_reference_layout_preserves_unknown_periods_and_original_column_values()
    {
        const string payload = """
        {"version":1,"sport":"baseball","league":"mlb","eventId":"123","teams":[
        {"id":"a","name":"Away","homeAway":"away","lineScores":[{"period":null,"displayValue":"1"},{"period":null,"displayValue":"0"}],"statistics":[{"group":"batting","key":"runs","label":"Runs","value":"3"},{"group":"batting","key":"hits","label":"Hits","value":"12"},{"group":"fielding","key":"errors","label":"Errors","value":"0"}]},
        {"id":"h","name":"Home","homeAway":"home","statistics":[{"group":"pitching","key":"hits","label":"Hits allowed","value":"99"}]}],
        "playerGroups":[{"teamId":"a","name":"batting","columns":[{"key":"hits-atBats","label":"H-AB"},{"key":"hits","label":"H"},{"key":"atBats","label":"AB"},{"key":"avg","label":"AVG"}],"athletes":[{"name":"Player","stats":["3-5","3","5",".314"]}],"totals":["12-35","12","35"]}]}
        """;
        var data=BoxData.Parse(payload,new("baseball","mlb","123"))!;
        Assert.Equal(new[]{2,1,3},BaseballBoxScore.Columns(data.Players[0],"batting"));
        using var services=new ServiceCollection().AddLogging().AddSingleton(FeedDates.Create("America/Chicago")).BuildServiceProvider();
        await using var renderer=new HtmlRenderer(services,services.GetRequiredService<ILoggerFactory>());
        var html=await renderer.Dispatcher.InvokeAsync(async()=> (await renderer.RenderComponentAsync<BoxScoreContent>(ParameterView.FromDictionary(new Dictionary<string,object?>{["Detail"]=new BoxDetail(data,null,null)}))).ToHtmlString());
        Assert.Contains("baseball-team-grid",html);Assert.Contains("Inning labels were not supplied",html);
        Assert.Contains(".314",html);Assert.Contains("3-5",html);Assert.Contains("Additional hitting statistics",html);
        Assert.Contains("Team batting details",html);Assert.Contains("Hits allowed",html);Assert.Contains("99",html);Assert.DoesNotContain("Scoring Summary",html);
        Assert.Contains("Collected scoring",html);Assert.Contains("Inning not specified",html);
    }
}
