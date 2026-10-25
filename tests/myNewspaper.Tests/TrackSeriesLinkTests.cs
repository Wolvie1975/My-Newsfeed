using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using myNewspaper.Web.Components.Watching;
using myNewspaper.Web.Watching;
namespace myNewspaper.Tests;

public class TrackSeriesLinkTests
{
    private static async Task<string> Render(string kind)
    {
        using var services = new ServiceCollection().AddLogging().BuildServiceProvider();
        await using var renderer = new HtmlRenderer(services, services.GetRequiredService<ILoggerFactory>());
        return await renderer.Dispatcher.InvokeAsync(async () => (await renderer.RenderComponentAsync<TrackSeriesLink>(ParameterView.FromDictionary(new Dictionary<string, object?> { ["Title"] = new WatchTitle(1, kind, "A & B <series>", "2026", null, "") }))).ToHtmlString());
    }
    [Fact] public async Task Tv_link_prefills_public_tracking_route_and_preserves_identity_context() { var html = await Render("tv"); Assert.Contains("Add to tracked series", html); Assert.Contains("tv/track?q=A%20%26%20B%20%3Cseries%3E", html); Assert.Contains("year=2026", html); Assert.DoesNotContain("Admin sign-in", html); Assert.Contains("shared tracked-series list", html); Assert.DoesNotContain("<series>", html); }
    [Fact] public async Task Movies_do_not_offer_tracking() => Assert.Equal("", await Render("movie"));
}
