using myNewspaper.Web.Data;
namespace myNewspaper.Tests;

public class CategoryHubRoutingTests
{
    [Theory]
    [InlineData("headlines", null)]
    [InlineData("sports", "sports/news/category-42")]
    [InlineData("entertainment", "entertainment/news/category-42")]
    public void Arbitrary_categories_route_by_saved_hub_and_stable_id(string hub, string? route) =>
        Assert.Equal(route, NewsSections.RouteFor(new FeedCategory(42, "Any future category", 0, hub)));
    [Fact]
    public void Assigned_hub_overrides_legacy_name() => Assert.Equal("entertainment/news/category-7", NewsSections.RouteFor(new FeedCategory(7, "Sports", 0, "entertainment")));
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("local")]
    [InlineData("Sports")]
    public void Unsupported_assignments_are_rejected(string? hub) => Assert.False(NewsSections.ValidHub(hub));
}
