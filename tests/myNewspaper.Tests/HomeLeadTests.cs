using myNewspaper.Web.Data;

namespace myNewspaper.Tests;

public class HomeLeadTests
{
    private static readonly DateTime Now = new(2026, 10, 28, 12, 0, 0, DateTimeKind.Utc);

    private static FeedItem Item(int id, string? category, string source = "https://publisher.test") =>
        new(id, "Story " + id, "https://example.com/" + id, null, null, null, source, null, category, Now.AddMinutes(id));

    [Fact]
    public void Only_top_headlines_compete_even_when_other_categories_are_newer()
    {
        var headline = Item(1, "Top Headlines");
        var candidates = new[] { headline, Item(2, "Technology"), Item(3, "World"), Item(4, "Opinion"), Item(5, null) };

        for (var hour = 0; hour < 24; hour++)
            Assert.Equal(headline, HomeLeadQuery.Snapshot(candidates, Now.AddHours(hour)).Article);
    }

    [Fact]
    public void No_top_headlines_means_no_lead_instead_of_another_category()
    {
        Assert.Null(HomeLeadQuery.Snapshot([Item(1, "Technology"), Item(2, "World")], Now).Article);
        Assert.Null(HomeLeadQuery.Snapshot([], Now).Article);
    }

    [Fact]
    public void Top_headlines_still_rotate_publishers_hourly_using_their_newest_story()
    {
        var older = Item(1, "Top Headlines", "https://a.test");
        var latest = Item(2, "Top Headlines", "https://a.test");
        var other = Item(3, "Top Headlines", "https://b.test");
        var candidates = new[] { older, latest, other, Item(4, "Technology", "https://c.test") };
        var first = HomeLeadQuery.Snapshot(candidates, Now);
        var second = HomeLeadQuery.Snapshot(candidates, Now.AddHours(1));

        Assert.Equal(new[] { latest.Id, other.Id }, new[] { first.Article!.Id, second.Article!.Id }.Order().ToArray());
        Assert.Equal(first.Article, HomeLeadQuery.Snapshot(candidates.Reverse(), Now.AddMinutes(59)).Article);
        Assert.Equal(first.Article, HomeLeadQuery.Snapshot(candidates, Now.AddHours(2)).Article);
    }
}
