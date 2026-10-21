using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using MyDailyFeed.Web.Components;
using MyDailyFeed.Web.Data;
using MyDailyFeed.Web.Data.Models;

namespace MyDailyFeed.Tests;

public class HomeRankingsTests
{
    private static async Task<string> Render(RankingsPage page)
    {
        await using var services = new ServiceCollection().AddLogging().AddSingleton(FeedDates.Create("America/Chicago")).BuildServiceProvider();
        await using var renderer = new HtmlRenderer(services, services.GetRequiredService<ILoggerFactory>());
        return await renderer.Dispatcher.InvokeAsync(async () => (await renderer.RenderComponentAsync<HomeRankings>(ParameterView.FromDictionary(new Dictionary<string, object?> { ["Data"] = page }))).ToHtmlString());
    }

    [Fact]
    public async Task Shows_selected_poll_top_25_only_and_links_to_matching_rankings()
    {
        var source = new SportsRankingSource { Id = 12, Sport = "Football", DisplayName = "Football AP", Enabled = true, LastSuccessAt = DateTime.UtcNow };
        var alternate = new SportsRankingSource { Id = 15, Sport = "Volleyball", DisplayName = "Volleyball", Enabled = true };
        var teams = Enumerable.Range(1, 26).Select(i => new SportsRankingTeam { Rank = i, TeamName = "Team-" + i + "-name", Record = "5-0", IsBig12 = i == 1 }).Append(new() { TeamName = "Unranked team" }).ToList();
        var html = await Render(new([source, alternate], source, new() { PollName = "AP Top 25", SeasonLabel = "2026", PublishedAtUtc = DateTime.UtcNow }, teams));
        Assert.Contains("Team-25-name", html); Assert.DoesNotContain("Team-26-name", html); Assert.DoesNotContain("Unranked team", html);
        Assert.Contains("rankings?sourceId=12", html); Assert.Contains("rankingSourceId", html);
        Assert.Contains("Football AP", html); Assert.Contains("Volleyball", html);
        Assert.Contains("ranking-big12", html); Assert.Contains("5-0", html);
        Assert.Contains("Poll dated", html); Assert.Contains("<noscript>", html);
    }

    [Fact]
    public async Task Empty_and_error_states_do_not_fabricate_rankings()
    {
        Assert.Contains("No ranking sources", await Render(new([], null, null, [])));
        var source = new SportsRankingSource { Id = 1, DisplayName = "Football" };
        Assert.Contains("Rankings not available yet", await Render(new([source], source, null, [])));
        Assert.Contains("temporarily unavailable", await Render(new([], null, null, [], "Rankings are temporarily unavailable.")));
    }
}
