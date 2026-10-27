using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using myNewspaper.Web.Components;
using myNewspaper.Web.Data;
using myNewspaper.Web.Data.Models;

namespace myNewspaper.Tests;

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
        var teams = Enumerable.Range(1, 26).Select(i => new SportsRankingTeam { Rank = i, TeamName = "Team-" + i + "-name", Record = "5-0", IsBig12 = i == 1, TeamId = i.ToString(), LogoUrl = i == 1 ? "https://a.espncdn.com/i/teamlogos/ncaa/500/2305.png" : null, PreviousRank = i == 1 ? 3 : null, PreviouslyUnranked = i == 2 }).Append(new() { TeamName = "Unranked team" }).ToList();
        var html = await Render(new([source, alternate], source, new() { PollName = "AP Top 25", SeasonLabel = "2026", PublishedAtUtc = DateTime.UtcNow }, teams));
        Assert.Contains("Team-25-name", html); Assert.DoesNotContain("Team-26-name", html); Assert.DoesNotContain("Unranked team", html);
        Assert.Contains("rankings?sourceId=12", html); Assert.Contains("rankingSourceId", html);
        Assert.Contains("Football AP", html); Assert.Contains("Volleyball", html);
        Assert.Contains("ranking-big12", html); Assert.Contains("5-0", html);
        Assert.Contains(">School</th>", html); Assert.DoesNotContain(">Conference</th>", html); Assert.Contains(">Previous</th>", html);
        Assert.Contains(">NR</td>", html);
        Assert.Contains("<img", html); Assert.Contains("width=\"22\"", html); Assert.DoesNotContain("big12-badge", html);
        Assert.Contains("Poll dated", html); Assert.Contains("<noscript>", html);
    }

    [Fact]
    public async Task Desktop_pills_use_short_sport_names_unless_two_polls_share_a_sport()
    {
        var football = new SportsRankingSource { Id = 1, Sport = "Football", DisplayName = "Football — AP Top 25" };
        var soccer = new SportsRankingSource { Id = 4, Sport = "Soccer", DisplayName = "Women's Soccer — United Soccer Coaches" };
        var html = await Render(new([football, soccer], football, null, []));
        Assert.Contains("d-rankings__pills", html);
        Assert.Contains("href=\"?rankingSourceId=1\" data-ranking-source=\"1\" aria-current=\"page\">Football</a>", html);
        Assert.Contains("href=\"?rankingSourceId=4\" data-ranking-source=\"4\">Soccer</a>", html);

        var coaches = new SportsRankingSource { Id = 6, Sport = "Football", DisplayName = "Football — Coaches Poll" };
        html = await Render(new([football, coaches], football, null, []));
        Assert.Contains(">Football &#x2014; AP Top 25</a>", html);
        Assert.Contains(">Football &#x2014; Coaches Poll</a>", html);
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
