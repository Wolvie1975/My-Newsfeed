using Microsoft.EntityFrameworkCore;
using MyNewsFeed.Web.Data;
using MyNewsFeed.Web.Data.Models;

namespace MyNewsFeed.Tests;

// Integration tests against the live WebScraper database. Each test works in a transaction that is never
// committed, and tags its rows with a unique token so the assertions only see its own data whatever else is stored.
public class FeedQueryTests
{
    private static WebScraperContext Create() => new(
        new DbContextOptionsBuilder<WebScraperContext>()
            .UseSqlServer("Server=sql2025,1433;Database=WebScraper;User Id=sa;Password=Fedora_Dev_2025!;Encrypt=False;TrustServerCertificate=True;")
            .Options);

    private static Page NewPage(string token, string title, DateTime scraped, DateTime? published = null,
        string? description = null, string? imageUrl = null) => new()
    {
        Url = $"https://test.invalid/{token}/{Guid.NewGuid():N}",
        Title = $"{title} {token}",
        Description = description,
        ImageUrl = imageUrl,
        ScrapedAt = scraped,
        Published = published,
    };

    [Fact]
    public async Task Feed_shows_only_pages_from_enabled_sources_newest_first()
    {
        await using var db = Create();
        await using var tx = await db.Database.BeginTransactionAsync();
        var token = $"feed{Guid.NewGuid():N}";

        var enabled = new Source { Url = $"https://test.invalid/{token}/on", Label = "On", Enabled = true };
        var disabled = new Source { Url = $"https://test.invalid/{token}/off", Enabled = false };
        // Newest by publish date, oldest publish date, and one with no publish date that was scraped in between.
        enabled.Pages.Add(NewPage(token, "Newest", scraped: new(2026, 1, 1), published: new(2026, 6, 1)));
        enabled.Pages.Add(NewPage(token, "Oldest", scraped: new(2026, 9, 1), published: new(2026, 2, 1)));
        enabled.Pages.Add(NewPage(token, "Unpublished", scraped: new(2026, 4, 1)));
        disabled.Pages.Add(NewPage(token, "Hidden", scraped: new(2026, 12, 1), published: new(2026, 12, 1)));
        db.Sources.AddRange(enabled, disabled);
        await db.SaveChangesAsync();

        var feed = await FeedQuery.GetFeedAsync(db, categoryId: null, search: token, after: null);

        Assert.Equal(3, feed.Total);
        Assert.Equal(new[] { $"Newest {token}", $"Unpublished {token}", $"Oldest {token}" }, feed.Items.Select(i => i.Title));
        Assert.All(feed.Items, i => Assert.Equal("On", i.SourceLabel));
        Assert.DoesNotContain(feed.Items, i => i.Title.StartsWith("Hidden"));
        // The date shown is the publish date when known, else the scrape date.
        Assert.Equal(new DateTime(2026, 4, 1), feed.Items.Single(i => i.Title.StartsWith("Unpublished")).Date);
        Assert.Equal(new DateTime(2026, 6, 1), feed.Items.Single(i => i.Title.StartsWith("Newest")).Date);
        Assert.Null(feed.Next);
        Assert.Equal(3, feed.Shown);
    }

    [Fact]
    public async Task Feed_carries_the_image_and_source_details_the_design_needs()
    {
        await using var db = Create();
        await using var tx = await db.Database.BeginTransactionAsync();
        var token = $"img{Guid.NewGuid():N}";

        var category = new SourceCategory { CategoryName = $"Sports {token}" };
        var source = new Source { Url = $"https://test.invalid/{token}/rss", Label = "ESPN", Enabled = true, SourceCategory = category };
        source.Pages.Add(NewPage(token, "With image", new(2026, 1, 2), imageUrl: "https://img.example.com/a.jpg"));
        source.Pages.Add(NewPage(token, "Without image", new(2026, 1, 1)));
        db.Sources.Add(source);
        await db.SaveChangesAsync();

        var feed = await FeedQuery.GetFeedAsync(db, null, token, null);

        var with = feed.Items.Single(i => i.Title.StartsWith("With image"));
        Assert.Equal("https://img.example.com/a.jpg", with.ImageUrl);
        Assert.Equal("ESPN", with.SourceLabel);
        Assert.Equal(source.Url, with.SourceUrl);
        Assert.Equal(category.Id, with.CategoryId);
        Assert.Equal(category.CategoryName, with.CategoryName);
        Assert.Null(feed.Items.Single(i => i.Title.StartsWith("Without image")).ImageUrl);
    }

    [Fact]
    public async Task Feed_filters_by_category_and_lists_only_categories_with_visible_pages()
    {
        await using var db = Create();
        await using var tx = await db.Database.BeginTransactionAsync();
        var token = $"cat{Guid.NewGuid():N}";

        var used = new SourceCategory { CategoryName = $"Used {token}" };
        var onlyDisabled = new SourceCategory { CategoryName = $"Disabled {token}" };
        var empty = new SourceCategory { CategoryName = $"Empty {token}" };
        var inUse = new Source { Url = $"https://test.invalid/{token}/used", Enabled = true, SourceCategory = used };
        var plain = new Source { Url = $"https://test.invalid/{token}/plain", Enabled = true };
        var disabled = new Source { Url = $"https://test.invalid/{token}/off", Enabled = false, SourceCategory = onlyDisabled };
        inUse.Pages.Add(NewPage(token, "In category", new(2026, 1, 1)));
        plain.Pages.Add(NewPage(token, "Uncategorized", new(2026, 1, 2)));
        disabled.Pages.Add(NewPage(token, "Disabled page", new(2026, 1, 3)));
        db.Sources.AddRange(inUse, plain, disabled);
        db.SourceCategories.Add(empty);
        await db.SaveChangesAsync();

        var filtered = await FeedQuery.GetFeedAsync(db, used.Id, token, null);
        Assert.Equal($"In category {token}", Assert.Single(filtered.Items).Title);
        Assert.Equal(used.CategoryName, filtered.Items[0].CategoryName);

        var mine = (await FeedQuery.GetCategoriesAsync(db)).Where(c => c.Name.EndsWith(token)).ToList();
        Assert.Equal(used.CategoryName, Assert.Single(mine).Name);
        Assert.Equal(1, mine[0].Count);
    }

    [Fact]
    public async Task A_category_covers_the_pages_of_all_its_sources_and_follows_a_source_that_moves()
    {
        await using var db = Create();
        await using var tx = await db.Database.BeginTransactionAsync();
        var token = $"move{Guid.NewGuid():N}";

        var news = new SourceCategory { CategoryName = $"News {token}" };
        var tech = new SourceCategory { CategoryName = $"Tech {token}" };
        var first = new Source { Url = $"https://test.invalid/{token}/1", Enabled = true, SourceCategory = news };
        var second = new Source { Url = $"https://test.invalid/{token}/2", Enabled = true, SourceCategory = news };
        first.Pages.Add(NewPage(token, "A", new(2026, 1, 1)));
        first.Pages.Add(NewPage(token, "B", new(2026, 1, 2)));
        second.Pages.Add(NewPage(token, "C", new(2026, 1, 3)));
        db.Sources.AddRange(first, second);
        db.SourceCategories.Add(tech);
        await db.SaveChangesAsync();

        var inNews = await FeedQuery.GetFeedAsync(db, news.Id, token, null);
        Assert.Equal(3, inNews.Total);
        Assert.All(inNews.Items, i => Assert.Equal(news.CategoryName, i.CategoryName));

        // Changing the source's category is all it takes: its pages move with it.
        first.SourceCategoryId = tech.Id;
        await db.SaveChangesAsync();

        Assert.Equal(1, (await FeedQuery.GetFeedAsync(db, news.Id, token, null)).Total);
        var inTech = await FeedQuery.GetFeedAsync(db, tech.Id, token, null);
        Assert.Equal(2, inTech.Total);
        Assert.All(inTech.Items, i => Assert.Equal(tech.CategoryName, i.CategoryName));

        var counts = (await FeedQuery.GetCategoriesAsync(db)).Where(c => c.Name.EndsWith(token)).ToDictionary(c => c.Name, c => c.Count);
        Assert.Equal(1, counts[news.CategoryName]);
        Assert.Equal(2, counts[tech.CategoryName]);
    }

    [Fact]
    public async Task Feed_search_matches_title_or_description_and_treats_wildcards_literally()
    {
        await using var db = Create();
        await using var tx = await db.Database.BeginTransactionAsync();
        var token = $"srch{Guid.NewGuid():N}";

        var source = new Source { Url = $"https://test.invalid/{token}", Enabled = true };
        source.Pages.Add(NewPage(token, "Rust release", new(2026, 1, 1)));
        source.Pages.Add(NewPage(token, "Other", new(2026, 1, 2), description: "Mentions kubernetes here"));
        source.Pages.Add(NewPage(token, "Percent 100%", new(2026, 1, 3)));
        db.Sources.Add(source);
        await db.SaveChangesAsync();

        Assert.Equal(1, (await FeedQuery.GetFeedAsync(db, null, $"rust release {token}", null)).Total);
        Assert.Equal(1, (await FeedQuery.GetFeedAsync(db, null, "kubernetes", null)).Items.Count(i => i.Title.EndsWith(token)));
        // A literal '%' must not act as a wildcard that matches everything.
        var percent = await FeedQuery.GetFeedAsync(db, null, "100%", null);
        Assert.Contains(percent.Items, i => i.Title.StartsWith("Percent"));
        Assert.DoesNotContain(percent.Items, i => i.Title.StartsWith("Rust"));
    }

    [Fact]
    public async Task Paging_by_cursor_walks_the_whole_feed_once()
    {
        await using var db = Create();
        await using var tx = await db.Database.BeginTransactionAsync();
        var token = $"page{Guid.NewGuid():N}";

        var source = new Source { Url = $"https://test.invalid/{token}", Enabled = true };
        for (var i = 1; i <= 5; i++)
        {
            source.Pages.Add(NewPage(token, $"Item{i}", new(2026, 1, i)));
        }

        db.Sources.Add(source);
        await db.SaveChangesAsync();

        var first = await FeedQuery.GetFeedAsync(db, null, token, null, take: 2);
        Assert.Equal((5, 3, 2), (first.Total, first.Remaining, first.Items.Count));
        Assert.Equal(2, first.Shown);
        Assert.Equal($"Item5 {token}", first.Items[0].Title);
        Assert.NotNull(first.Next);

        var second = await FeedQuery.GetFeedAsync(db, null, token, first.Next, take: 2);
        Assert.Equal(new[] { $"Item3 {token}", $"Item2 {token}" }, second.Items.Select(i => i.Title));
        Assert.Equal((5, 1, 4), (second.Total, second.Remaining, second.Shown));

        var last = await FeedQuery.GetFeedAsync(db, null, token, second.Next, take: 2);
        Assert.Equal($"Item1 {token}", Assert.Single(last.Items).Title);
        Assert.Equal((0, 5), (last.Remaining, last.Shown));
        Assert.Null(last.Next);
    }

    [Fact]
    public async Task Articles_arriving_between_batches_cause_no_duplicates_or_gaps()
    {
        await using var db = Create();
        await using var tx = await db.Database.BeginTransactionAsync();
        var token = $"live{Guid.NewGuid():N}";

        var source = new Source { Url = $"https://test.invalid/{token}", Enabled = true };
        for (var i = 1; i <= 4; i++)
        {
            source.Pages.Add(NewPage(token, $"Item{i}", new(2026, 1, i)));
        }

        db.Sources.Add(source);
        await db.SaveChangesAsync();

        var first = await FeedQuery.GetFeedAsync(db, null, token, null, take: 2);        // Item4, Item3

        // The scraper adds two newer articles while the visitor is looking at the first batch.
        source.Pages.Add(NewPage(token, "Item5", new(2026, 1, 5)));
        source.Pages.Add(NewPage(token, "Item6", new(2026, 1, 6)));
        await db.SaveChangesAsync();

        var second = await FeedQuery.GetFeedAsync(db, null, token, first.Next, take: 5);

        // With page numbers the visitor would now see Item4 and Item3 again. With a cursor they get exactly the rest.
        Assert.Equal(new[] { $"Item2 {token}", $"Item1 {token}" }, second.Items.Select(i => i.Title));
        Assert.Equal(6, second.Total);
        Assert.Null(second.Next);
    }

    [Fact]
    public async Task Articles_with_the_same_date_are_ordered_by_id_and_paged_without_loss()
    {
        await using var db = Create();
        await using var tx = await db.Database.BeginTransactionAsync();
        var token = $"tie{Guid.NewGuid():N}";

        var when = new DateTime(2026, 3, 3, 12, 0, 0);
        var source = new Source { Url = $"https://test.invalid/{token}", Enabled = true };
        for (var i = 1; i <= 4; i++)
        {
            source.Pages.Add(NewPage(token, $"Same{i}", scraped: when, published: when));
        }

        db.Sources.Add(source);
        await db.SaveChangesAsync();

        var seen = new List<int>();
        FeedCursor? cursor = null;
        do
        {
            var batch = await FeedQuery.GetFeedAsync(db, null, token, cursor, take: 1);
            seen.AddRange(batch.Items.Select(i => i.Id));
            cursor = batch.Next;
        }
        while (cursor is not null);

        Assert.Equal(4, seen.Count);
        Assert.Equal(seen.Distinct().Count(), seen.Count);
        Assert.Equal(seen.OrderByDescending(x => x), seen);
    }

    [Fact]
    public async Task Feed_cuts_very_long_descriptions_in_the_query()
    {
        await using var db = Create();
        await using var tx = await db.Database.BeginTransactionAsync();
        var token = $"long{Guid.NewGuid():N}";

        var source = new Source { Url = $"https://test.invalid/{token}", Enabled = true };
        source.Pages.Add(NewPage(token, "Long", new(2026, 1, 1), description: new string('a', 5000)));
        db.Sources.Add(source);
        await db.SaveChangesAsync();

        var item = Assert.Single((await FeedQuery.GetFeedAsync(db, null, token, null)).Items);
        Assert.Equal(600, item.Description!.Length);
    }
}
