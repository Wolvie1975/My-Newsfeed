using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using MyDailyFeed.Web.Components;
using MyDailyFeed.Web.Data;

namespace MyDailyFeed.Tests;

public class MovieCommerceTests
{
    [Theory]
    [InlineData("theatrical", "https://www.fandango.com/film-123/movie-overview", "Get tickets on Fandango")]
    [InlineData("digital", "https://athome.fandango.com/content/browse/details/Film/123", "View on Fandango at Home")]
    [InlineData("digital_purchase", "https://athome.fandango.com/content/browse/details/Film/123", "Buy on Fandango at Home")]
    [InlineData("digital_rental", "https://athome.fandango.com/content/browse/details/Film/123", "View on Fandango at Home")]
    public void Verified_direct_actions_retain_exact_labels(string type, string url, string label)
    {
        Assert.Equal(new MovieAction(url, label, "direct"), MovieCommerceQuery.Resolve("Film", type, url, label, "direct"));
    }

    [Theory]
    [InlineData("javascript:alert(1)")]
    [InlineData("http://www.fandango.com/film-123/movie-overview")]
    [InlineData("https://www.fandango.com.evil.test/film-123/movie-overview")]
    [InlineData("https://user@www.fandango.com/film-123/movie-overview")]
    [InlineData("https://www.fandango.com:444/film-123/movie-overview")]
    [InlineData("https://athome.fandango.com/content/browse/details/Film/123")]
    [InlineData("https://www.fandango.com/checkout")]
    public void Unsafe_or_wrong_provider_destinations_fall_back(string url)
    {
        var action = MovieCommerceQuery.Resolve("Film", "theatrical", url, "Get tickets on Fandango", "direct");
        Assert.Equal(new MovieAction("https://www.fandango.com/search?q=Film", "Search Fandango", "search"), action);
    }

    [Theory]
    [InlineData("theatrical")]
    [InlineData("digital")]
    public void Missing_cache_encodes_punctuation_and_search_round_trips(string type)
    {
        const string title = "L'été & A+B? #2";
        var action = MovieCommerceQuery.Resolve(title, type)!;
        Assert.EndsWith(Uri.EscapeDataString(title), action.Url);
        Assert.Equal("search", action.Kind);
        Assert.Equal(action, MovieCommerceQuery.Resolve(title, type, action.Url, action.Label, action.Kind));
    }

    [Theory]
    [InlineData("subscription")]
    [InlineData("disc")]
    public void Noncommerce_releases_have_no_action(string type) => Assert.Null(MovieCommerceQuery.Resolve("Film", type));

    [Fact]
    public void Unverified_buy_label_is_not_inferred()
    {
        var action = MovieCommerceQuery.Resolve("Film", "digital", "https://athome.fandango.com/content/browse/details/Film/123", "Buy now", "direct");
        Assert.Equal("Search Fandango at Home", action!.Label);
    }

    [Fact]
    public async Task Rendered_action_encodes_title_and_protects_new_tab()
    {
        using var services = new ServiceCollection().AddLogging().BuildServiceProvider();
        await using var renderer = new HtmlRenderer(services, services.GetRequiredService<ILoggerFactory>());
        var item = new MovieReleaseItem(1, "Film <script>", null, null, null, null, null, null, "dated", "theatrical", null, null, false, "Source", "https://example.com");
        var html = await renderer.Dispatcher.InvokeAsync(async () => (await renderer.RenderComponentAsync<MovieCommerceAction>(ParameterView.FromDictionary(
            new Dictionary<string, object?> { ["Item"] = item }))).ToHtmlString());
        Assert.Contains("Search Fandango", html);
        Assert.Contains("rel=\"noopener noreferrer\"", html);
        Assert.Contains("target=\"_blank\"", html);
        Assert.DoesNotContain("<script>", html);
        Assert.Contains("Film%20%3Cscript%3E", html);
    }
}
