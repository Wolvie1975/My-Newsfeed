using MyNewsFeed.Web.Data;

namespace MyNewsFeed.Tests;

public class ListingFilterTests
{
    [Fact]
    public void Listing_filter_terms_are_trimmed_and_bounded()
    {
        Assert.Equal("Kansas", QueryText.Term("  Kansas  "));
        Assert.Equal("", QueryText.Term(null));
        Assert.Equal(120, QueryText.Term(new string('x', 300)).Length);
    }
}
