using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using MyNewsFeed.Web.Components.BoxScores;
using MyNewsFeed.Web.Data;
namespace MyNewsFeed.Tests;

public class BasketballBoxScoreTests
{
    [Fact]
    public async Task Championship_sample_retains_team_identity_columns_players_and_supplied_totals()
    {
        var root=Path.GetFullPath("../../../../../",AppContext.BaseDirectory);
        var payload=await File.ReadAllTextAsync(Path.Combine(root,"new development/mens-college-basketball-boxscore-example.json"));
        var data=Assert.IsType<BoxData>(BoxData.Parse(payload,new("basketball","mens-college-basketball","401856600")));
        Assert.Equal(new[]{"41","130"},data.Teams.Select(t=>t.Id));
        Assert.Equal(new[]{"29","34"},data.Teams[0].Lines.Select(l=>l.DisplayValue));
        Assert.Equal(new[]{"33","36"},data.Teams[1].Lines.Select(l=>l.DisplayValue));
        var labels=new[]{"MIN","PTS","FG","3PT","FT","REB","AST","TO","STL","BLK","OREB","DREB","PF"};
        Assert.Equal(9,data.Players.Single(g=>g.TeamId=="41").Athletes.Count);
        Assert.Equal(8,data.Players.Single(g=>g.TeamId=="130").Athletes.Count);
        using var services=new ServiceCollection().AddLogging().AddSingleton(FeedDates.Create("America/Chicago")).BuildServiceProvider();
        await using var renderer=new HtmlRenderer(services,services.GetRequiredService<ILoggerFactory>());
        // Reverse group order to prove that team matching uses identity rather than array position.
        var detail=new BoxDetail(data with {Players=data.Players.Reverse().ToList()},null,null);
        var html=await renderer.Dispatcher.InvokeAsync(async()=> (await renderer.RenderComponentAsync<BoxScoreContent>(ParameterView.FromDictionary(new Dictionary<string,object?>{["Detail"]=detail}))).ToHtmlString());
        Assert.Contains("basketball-team-grid",html);
        foreach(var label in labels)Assert.Contains(">"+label+"</th>",html);
        foreach(var group in data.Players){
            Assert.Equal(labels,group.Columns.Select(c=>c.Label));
            Assert.Null(group.Totals[0]);
            foreach(var athlete in group.Athletes)Assert.Contains(HtmlEncoder.Default.Encode(athlete.Name),html);
            Assert.Contains("<td>"+string.Join("</td><td>",group.Totals.Select(v=>HtmlEncoder.Default.Encode(v ?? "—")))+"</td>",html);
        }
        Assert.Contains("1st half",html);Assert.Contains("2nd half",html);
        Assert.Contains("21-68",html);Assert.Contains("<td>0</td>",html);
        Assert.Contains("Starter",html);Assert.Contains("Bench",html);
        if(Environment.GetEnvironmentVariable("BASKETBALL_PREVIEW_FILE") is {} preview){
            var css=await File.ReadAllTextAsync(Path.Combine(root,"src/MyNewsFeed.Web/wwwroot/public.css"));
            await File.WriteAllTextAsync(preview,"<!doctype html><html lang=\"en\"><head><meta name=\"viewport\" content=\"width=device-width,initial-scale=1\"><title>Development fixture: college basketball</title><style>"+css+"</style></head><body class=\"pub\"><main style=\"padding:1rem;max-width:1100px;margin:auto\">"+html+"</main></body></html>");
        }
    }
}
