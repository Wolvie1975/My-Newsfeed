using myNewspaper.Web.Components;
using myNewspaper.Web.Data;

namespace myNewspaper.Tests;

public class NewsLeadTests
{
    private static readonly DateTime Now = new(2026, 10, 29, 12, 0, 0, DateTimeKind.Utc);

    private static FeedItem Item(int id, string? category, string? image = "https://img.test/a.jpg") =>
        new(id, "Story " + id, "https://example.com/" + id, null, image, null, "https://publisher.test/" + id, null, category, Now.AddMinutes(id));

    [Fact]
    public void Newer_stories_from_other_desks_never_lead()
    {
        var us = Item(1, "U.S. News");
        Assert.Equal(us, NewsLeadQuery.Select([us, Item(2, "Video Games"), Item(3, "Opinion"), Item(4, "Technology"), Item(5, null)]));
    }

    [Theory]
    [InlineData("Top Headlines")]
    [InlineData("U.S. News")]
    [InlineData(SectionChoices.NewsKansasCityCategory)]
    public void Each_hard_news_feed_can_lead(string category)
    {
        var newest = Item(3, category);
        Assert.Equal(newest, NewsLeadQuery.Select([Item(1, "Top Headlines"), Item(2, "U.S. News"), newest]));
    }

    [Fact]
    public void The_newest_story_leads_with_or_without_a_photo()
    {
        var text = Item(2, "Kansas City News", image: null);
        Assert.Equal(text, NewsLeadQuery.Select([Item(1, "Top Headlines"), text]));
    }

    [Fact]
    public void No_hard_news_means_no_chosen_lead()
    {
        Assert.Null(NewsLeadQuery.Select([Item(1, "Technology"), Item(2, "Opinion")]));
        Assert.Null(NewsLeadQuery.Select([]));
    }
}
