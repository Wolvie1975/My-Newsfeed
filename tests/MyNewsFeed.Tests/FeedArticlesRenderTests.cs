using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using MyNewsFeed.Web.Components.Feed;
using MyNewsFeed.Web.Data;

namespace MyNewsFeed.Tests;

// Renders the article component to HTML (the same way the "Load more" endpoint does) and checks the markup.
public class FeedArticlesRenderTests
{
    private sealed class FixedClock(DateTimeOffset now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => now;
    }

    private static FeedItem Item(int id, string title, DateTime date, string? description = "A summary.",
        string? image = "https://img.example.com/a.jpg", string? label = "ESPN", string url = "https://www.espn.com/a",
        string? category = "Sports", string? sourceUrl = "https://www.espn.com/espn/rss/news") =>
        new(id, title, url, description, image, label, sourceUrl ?? "", category is null ? null : 8, category, date);

    private static async Task<string> RenderAsync(IReadOnlyList<FeedItem> items, bool lead = false, string? previousDay = null,
        int? total = null)
    {
        var services = new ServiceCollection()
            .AddLogging()
            .AddSingleton(FeedDates.Create("UTC", new FixedClock(new DateTimeOffset(2026, 9, 20, 14, 0, 0, TimeSpan.Zero))))
            .BuildServiceProvider();

        await using var renderer = new HtmlRenderer(services, services.GetRequiredService<ILoggerFactory>());
        return await renderer.Dispatcher.InvokeAsync(async () =>
        {
            var output = await renderer.RenderComponentAsync<FeedArticles>(ParameterView.FromDictionary(
                new Dictionary<string, object?>
                {
                    [nameof(FeedArticles.Items)] = items,
                    [nameof(FeedArticles.LeadFirst)] = lead,
                    [nameof(FeedArticles.PreviousDay)] = previousDay,
                    [nameof(FeedArticles.Total)] = total,
                }));
            return output.ToHtmlString();
        });
    }

    private static readonly DateTime Today = new(2026, 9, 20, 9, 0, 0);
    private static readonly DateTime Yesterday = new(2026, 9, 19, 9, 0, 0);

    [Fact]
    public async Task Only_the_first_article_is_the_lead_and_only_when_asked()
    {
        var items = new[] { Item(1, "One", Today), Item(2, "Two", Today) };

        var withLead = await RenderAsync(items, lead: true);
        Assert.Single(System.Text.RegularExpressions.Regex.Matches(withLead, "article--lead"));
        Assert.Contains("Read on ESPN", withLead);

        var withoutLead = await RenderAsync(items, lead: false);
        Assert.DoesNotContain("article--lead", withoutLead);
        Assert.DoesNotContain("Read on ESPN", withoutLead);
    }

    [Fact]
    public async Task Emoji_are_removed_from_titles_and_summaries()
    {
        var html = System.Net.WebUtility.HtmlDecode(await RenderAsync([Item(1, "😈 Kentucky’s jab", Today, description: "🔥 Hot take")]));

        Assert.Contains("Kentucky’s jab", html);
        Assert.Contains("Hot take", html);
        Assert.DoesNotContain("😈", html);
        Assert.DoesNotContain("🔥", html);
    }

    [Fact]
    public async Task Unsafe_article_links_are_not_rendered_as_links_but_the_title_still_shows()
    {
        var html = await RenderAsync([Item(1, "Sneaky title", Today, url: "javascript:alert(1)")]);

        Assert.Contains("Sneaky title", html);
        Assert.DoesNotContain("javascript:", html);
        Assert.DoesNotContain("<a href=\"javascript", html);
    }

    [Fact]
    public async Task Unsafe_image_urls_are_dropped()
    {
        var html = await RenderAsync([Item(1, "T", Today, image: "javascript:alert(1)")]);
        Assert.DoesNotContain("<img", html);
        Assert.DoesNotContain("javascript:", html);
    }

    [Fact]
    public async Task Images_are_lazy_no_referrer_and_fall_back_to_the_default()
    {
        var html = await RenderAsync([Item(1, "Lead", Today), Item(2, "Card", Today)], lead: true);

        Assert.Contains("referrerpolicy=\"no-referrer\"", html);
        Assert.Contains("onerror=\"this.remove()\"", html);
        Assert.Contains("loading=\"eager\"", html);   // the lead image is above the fold
        Assert.Contains("loading=\"lazy\"", html);    // everything else is deferred
    }

    [Fact]
    public async Task An_article_without_an_image_still_has_the_thumb_so_the_default_image_shows()
    {
        var html = await RenderAsync([Item(1, "No picture", Today, image: null)]);

        Assert.Contains("class=\"thumb\"", html);
        Assert.DoesNotContain("<img", html);
    }

    [Fact]
    public async Task The_source_shows_its_label_with_a_host_fallback_and_a_badge_letter()
    {
        var labelled = await RenderAsync([Item(1, "A", Today, label: "ESPN")]);
        Assert.Contains("<span class=\"src__name\">ESPN</span>", labelled);
        Assert.Contains(">E</span>", labelled);

        var unlabelled = await RenderAsync([Item(2, "B", Today, label: null, sourceUrl: "https://www.cbssports.com/rss/headlines/")]);
        Assert.Contains("<span class=\"src__name\">cbssports.com</span>", unlabelled);
    }

    [Fact]
    public async Task Each_article_shows_the_date_it_was_published()
    {
        var html = await RenderAsync([Item(1, "New", Today), Item(2, "Old", Yesterday), Item(3, "Older", new DateTime(2026, 9, 10, 9, 0, 0))]);

        Assert.Contains(">Today</time>", html);
        Assert.Contains(">Yesterday</time>", html);
        Assert.Contains(">Sep 10</time>", html);
        Assert.Contains("datetime=\"2026-09-20T09:00:00Z\"", html);
    }

    [Fact]
    public async Task Day_headings_appear_once_per_day_and_carry_the_total_on_the_first()
    {
        var items = new[] { Item(1, "A", Today), Item(2, "B", Today), Item(3, "C", Yesterday) };
        var html = System.Net.WebUtility.HtmlDecode(await RenderAsync(items, total: 130));

        Assert.Single(System.Text.RegularExpressions.Regex.Matches(html, "Today · Sun Sep 20"));
        Assert.Single(System.Text.RegularExpressions.Regex.Matches(html, "Yesterday · Sat Sep 19"));
        Assert.Contains("130 articles", html);
    }

    [Fact]
    public async Task A_continuing_batch_does_not_repeat_the_heading_for_the_day_already_shown()
    {
        var items = new[] { Item(1, "A", Today), Item(2, "B", Yesterday) };
        var html = System.Net.WebUtility.HtmlDecode(await RenderAsync(items, previousDay: "2026-09-20"));

        Assert.DoesNotContain("Today · Sun Sep 20", html);
        Assert.Contains("Yesterday · Sat Sep 19", html);
    }

    [Fact]
    public async Task The_category_label_links_to_that_category_and_is_omitted_when_there_is_none()
    {
        var withCategory = await RenderAsync([Item(1, "A", Today, category: "Sports")]);
        Assert.Contains("<a class=\"cat\" href=\"?category=8\">Sports</a>", withCategory);

        var without = await RenderAsync([Item(2, "B", Today, category: null)]);
        Assert.DoesNotContain("class=\"cat\"", without);
    }

    [Fact]
    public async Task Article_links_open_in_a_new_tab_without_leaking_the_opener()
    {
        var html = await RenderAsync([Item(1, "A", Today)]);
        Assert.Contains("target=\"_blank\"", html);
        Assert.Contains("rel=\"noopener noreferrer\"", html);
    }

    [Fact]
    public async Task Titles_and_summaries_are_html_encoded()
    {
        var html = await RenderAsync([Item(1, "<script>alert(1)</script>", Today, description: "<img src=x onerror=alert(1)>")]);

        Assert.DoesNotContain("<script>alert", html);
        Assert.DoesNotContain("<img src=x", html);
        Assert.Contains("&lt;script&gt;", html);
    }
}
