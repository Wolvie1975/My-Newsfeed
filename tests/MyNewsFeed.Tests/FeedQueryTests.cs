using Microsoft.EntityFrameworkCore;
using MyNewsFeed.Web.Data;
using MyNewsFeed.Web.Data.Models;

namespace MyNewsFeed.Tests;

public class FeedQueryTests
{
    private static WebScraperContext Create() => new(
        new DbContextOptionsBuilder<WebScraperContext>()
            .UseSqlServer("Server=sql2025,1433;Database=WebScraper;User Id=sa;Password=Fedora_Dev_2025!;Encrypt=False;TrustServerCertificate=True;")
            .Options);

    // ---- pure helpers -------------------------------------------------------------------------------------------

    [Theory]
    [InlineData("https://example.com/a", "https://example.com/a")]
    [InlineData("http://example.com/", "http://example.com/")]
    [InlineData("javascript:alert(1)", null)]
    [InlineData("JaVaScRiPt:alert(1)", null)]
    [InlineData("data:text/html,<script>alert(1)</script>", null)]
    [InlineData("ftp://example.com/file", null)]
    [InlineData("/relative/path", null)]
    [InlineData("", null)]
    [InlineData(null, null)]
    public void SafeHref_only_allows_http_and_https(string? url, string? expected) =>
        Assert.Equal(expected, FeedQuery.SafeHref(url));

    [Fact]
    public void Excerpt_returns_short_text_unchanged_and_null_for_blank()
    {
        Assert.Equal("Short text", FeedQuery.Excerpt("  Short text  "));
        Assert.Null(FeedQuery.Excerpt(null));
        Assert.Null(FeedQuery.Excerpt("   "));
    }

    [Fact]
    public void Excerpt_cuts_long_text_at_a_word_boundary()
    {
        var text = string.Join(' ', Enumerable.Repeat("word", 100));
        var excerpt = FeedQuery.Excerpt(text, 50)!;

        Assert.EndsWith("…", excerpt);
        Assert.True(excerpt.Length <= 51);
        Assert.DoesNotContain("wor…", excerpt);
    }

    [Fact]
    public void Excerpt_hard_cuts_text_without_spaces()
    {
        var excerpt = FeedQuery.Excerpt(new string('x', 500), 100)!;
        Assert.Equal(101, excerpt.Length);
    }

    // ---- database ---------------------------------------------------------------------------------------------------
    // Each test works in a transaction that is never committed, and tags its rows with a unique token so the
    // assertions only see its own data whatever else is stored.

    private static Page NewPage(string token, string title, DateTime scraped, DateTime? published = null,
        string? description = null) => new()
    {
        Url = $"https://test.invalid/{token}/{Guid.NewGuid():N}",
        Title = $"{title} {token}",
        Description = description,
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

        var feed = await FeedQuery.GetFeedAsync(db, categoryId: null, search: token, page: 1);

        Assert.Equal(3, feed.Total);
        Assert.Equal(new[] { $"Newest {token}", $"Unpublished {token}", $"Oldest {token}" },
            feed.Items.Select(i => i.Title));
        Assert.All(feed.Items, i => Assert.Equal("On", i.SourceName));
        Assert.DoesNotContain(feed.Items, i => i.Title.StartsWith("Hidden"));
        // The date shown is the publish date when known, else the scrape date.
        Assert.Equal(new DateTime(2026, 4, 1), feed.Items.Single(i => i.Title.StartsWith("Unpublished")).Date);
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

        var filtered = await FeedQuery.GetFeedAsync(db, used.Id, token, 1);
        Assert.Equal($"In category {token}", Assert.Single(filtered.Items).Title);
        Assert.Equal(used.CategoryName, filtered.Items[0].CategoryName);

        var categories = await FeedQuery.GetCategoriesAsync(db);
        var mine = categories.Where(c => c.Name.EndsWith(token)).ToList();
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

        var inNews = await FeedQuery.GetFeedAsync(db, news.Id, token, 1);
        Assert.Equal(3, inNews.Total);
        Assert.All(inNews.Items, i => Assert.Equal(news.CategoryName, i.CategoryName));

        // Changing the source's category is all it takes: its pages move with it.
        first.SourceCategoryId = tech.Id;
        await db.SaveChangesAsync();

        Assert.Equal(1, (await FeedQuery.GetFeedAsync(db, news.Id, token, 1)).Total);
        var inTech = await FeedQuery.GetFeedAsync(db, tech.Id, token, 1);
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

        Assert.Equal(1, (await FeedQuery.GetFeedAsync(db, null, $"rust release {token}", 1)).Total);
        Assert.Equal(1, (await FeedQuery.GetFeedAsync(db, null, "kubernetes", 1)).Items.Count(i => i.Title.EndsWith(token)));
        // A literal '%' must not act as a wildcard that matches everything.
        var percent = await FeedQuery.GetFeedAsync(db, null, "100%", 1);
        Assert.Contains(percent.Items, i => i.Title.StartsWith("Percent"));
        Assert.DoesNotContain(percent.Items, i => i.Title.StartsWith("Rust"));
    }

    [Fact]
    public async Task Feed_paging_clamps_out_of_range_pages()
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

        var first = await FeedQuery.GetFeedAsync(db, null, token, page: 1, pageSize: 2);
        Assert.Equal((5, 3, 2), (first.Total, first.TotalPages, first.Items.Count));
        Assert.Equal($"Item5 {token}", first.Items[0].Title);

        var last = await FeedQuery.GetFeedAsync(db, null, token, page: 3, pageSize: 2);
        Assert.Equal($"Item1 {token}", Assert.Single(last.Items).Title);

        var tooFar = await FeedQuery.GetFeedAsync(db, null, token, page: 99, pageSize: 2);
        Assert.Equal(3, tooFar.Page);
        var tooLow = await FeedQuery.GetFeedAsync(db, null, token, page: -4, pageSize: 2);
        Assert.Equal(1, tooLow.Page);
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

        var item = Assert.Single((await FeedQuery.GetFeedAsync(db, null, token, 1)).Items);
        Assert.Equal(600, item.Description!.Length);
    }
}
