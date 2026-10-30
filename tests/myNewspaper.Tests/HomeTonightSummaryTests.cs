using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using myNewspaper.Web.Components;
using myNewspaper.Web.Data;

namespace myNewspaper.Tests;

public class HomeTonightSummaryTests
{
    private static TvEpisodeItem Episode(int show, string name = "Avatar", string platform = "Paramount+") =>
        new(1, name, null, platform, "Episode", 1, 1, false, new(2026, 10, 30), null, "https://example.test/episode", show);

    private static async Task<string> Render(params TvEpisodeItem[] episodes)
    {
        await using var services = new ServiceCollection().AddLogging().AddSingleton(FeedDates.Create("America/Chicago")).BuildServiceProvider();
        await using var renderer = new HtmlRenderer(services, services.GetRequiredService<ILoggerFactory>());
        return await renderer.Dispatcher.InvokeAsync(async () => (await renderer.RenderComponentAsync<HomeTonightSummary>(
            ParameterView.FromDictionary(new Dictionary<string, object?> { ["Episodes"] = episodes }))).ToHtmlString());
    }

    [Fact]
    public async Task No_episodes_produce_no_link()
    { Assert.DoesNotContain("<a", await Render()); }

    [Fact]
    public async Task One_show_keeps_platform_and_counts_its_episodes()
    {
        var single = await Render(Episode(7));
        Assert.Contains("Paramount", single);
        Assert.DoesNotContain("bs-live__more", single);
        var batch = await Render(Enumerable.Repeat(Episode(7), 4).ToArray());
        Assert.Contains("4 episodes", batch);
        Assert.DoesNotContain("+ 3 more", batch);
        Assert.Contains("href=\"tv\"", batch);
    }

    [Fact]
    public async Task Multiple_shows_count_shows_and_omit_the_first_platform()
    {
        var html = await Render(Episode(7), Episode(7), Episode(8, "Second show"), Episode(9, "Third show"));
        Assert.Contains("+ 2 more", System.Net.WebUtility.HtmlDecode(html));
        Assert.DoesNotContain("Paramount", html);
        Assert.DoesNotContain("4 episodes", html);
    }

    [Fact]
    public async Task Unknown_show_ids_use_the_series_name_and_titles_are_encoded()
    {
        var html = await Render(Episode(0, "<script>"), Episode(0, "Other"));
        Assert.Contains("+ 1 more", System.Net.WebUtility.HtmlDecode(html));
        Assert.DoesNotContain("<script>", html);
        Assert.Contains("&lt;script&gt;", html);
    }
}
