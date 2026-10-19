using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using MyNewsFeed.Web.Components.BoxScores;
using MyNewsFeed.Web.Data;
namespace MyNewsFeed.Tests;

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
}
