using System.Net;
using myNewspaper.Web.Components;
using myNewspaper.Web.Data;

namespace myNewspaper.Tests;

/// <summary>The News section (docs/news-section-handoff.md): its tabs and the addresses that moved under it.</summary>
public class NewsSectionTests
{
    [Fact]
    public void Tabs_follow_the_design_order_and_kansas_city_has_its_own_page()
    {
        var tabs = SectionChoices.News([
            new FeedCategory(8, "Linux", 0), new FeedCategory(2627, "U.S. News", 0), new FeedCategory(5950, "Kansas City News", 0),
            new FeedCategory(6444, "Business", 0), new FeedCategory(5877, "Top Headlines", 0), new FeedCategory(6022, "Opinion", 0),
        ]);
        Assert.Equal(["News home", "All headlines", "Top Headlines", "Kansas City", "Opinion", "Linux", "Business", "U.S. News"],
            tabs.Select(t => t.Label));
        Assert.Equal(["news", "news/all", "news/all?category=5877", "news/kansas-city", "news/all?category=6022"],
            tabs.Take(5).Select(t => t.Href));
    }

    [Theory]
    [InlineData("/local", "/news/kansas-city")]
    [InlineData("/local/?section=events", "/news/kansas-city?section=events")]
    [InlineData("/news?category=5877", "/news/all?category=5877")]
    [InlineData("/news?category=all&source=3", "/news/all?category=all&source=3")]
    [InlineData("/news?q=election", "/news/all?q=election")]
    [InlineData("/news?after=abc", "/news/all?after=abc")]
    public async Task Old_headlines_and_local_addresses_move_permanently_with_their_query(string from, string to)
    {
        await using var app = new ProfileApp(enabled: false);
        var response = await app.Browser().GetAsync(from);
        Assert.Equal(HttpStatusCode.MovedPermanently, response.StatusCode);
        Assert.Equal(to, response.Headers.Location!.OriginalString);
    }

    [Theory]
    [InlineData("/news")]
    [InlineData("/news?view=x")]
    [InlineData("/news/all?category=5877")]
    [InlineData("/local/weather")]
    public async Task Other_addresses_are_not_redirected(string path)
    {
        await using var app = new ProfileApp(enabled: false);
        var response = await app.Browser().GetAsync(path);
        Assert.NotEqual(HttpStatusCode.MovedPermanently, response.StatusCode);
    }
}
