using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using myNewspaper.Web.Data;
using myNewspaper.Web.Data.Models;
namespace myNewspaper.Tests;

public class BoxOfficeTests
{
    [Fact]
    public void Collection_health_reports_attempt_failures_without_publisher_status_inference()
    {
        var now = new DateTime(2026, 10, 12, 12, 0, 0);
        Assert.Null(BoxOfficeQuery.Health(new(1, "Deadline", now, false), now));
        Assert.Equal("Story collection may be delayed.", BoxOfficeQuery.Health(new(1, "Deadline", now.AddHours(-3), false), now));
        Assert.Equal("Latest collection failed; showing previously collected stories.", BoxOfficeQuery.Health(new(1, "Variety", now, true), now));
    }
    [Fact]
    public async Task Publishers_stay_separate_and_empty_sources_remain_available_with_stable_paging()
    {
        await using var db = new WebScraperContext(new DbContextOptionsBuilder<WebScraperContext>().UseSqlServer(TestDatabase.ConnectionString).Options);
        await using var transaction = await db.Database.BeginTransactionAsync();
        var tag = Guid.NewGuid().ToString("N");
        var category = await db.SourceCategories.SingleOrDefaultAsync(c => c.CategoryName == "Box Office") ?? new SourceCategory { CategoryName = "Box Office" };
        var publisher = new Source { Url = $"https://test.invalid/{tag}/feed", Label = tag, Enabled = true, SourceCategory = category };
        var empty = new Source { Url = $"https://test.invalid/{tag}/empty", Label = "Empty " + tag, Enabled = true, SourceCategory = category };
        var hidden = new Source { Url = $"https://test.invalid/{tag}/disabled", Label = "Hidden " + tag, Enabled = false, SourceCategory = category };
        var foreign = new Source { Url = $"https://test.invalid/{tag}/other", Label = "Other " + tag, Enabled = true };
        for (var i = 0; i < 14; i++) publisher.Pages.Add(new Page { Title = $"{tag} {i}", Url = $"https://test.invalid/{tag}/story/{i}", Published = new DateTime(2026, 10, 12).AddMinutes(i) });
        hidden.Pages.Add(new Page { Title = tag + " hidden", Url = $"https://test.invalid/{tag}/hidden" });
        db.AddRange(publisher, empty, hidden, foreign); await db.SaveChangesAsync();
        var first = await BoxOfficeQuery.GetAsync(db, publisher.Id, null);
        Assert.Equal(category.Id, first.CategoryId); Assert.Equal(14, first.Feed.Total);
        Assert.Equal(13, first.Feed.Items.Count); Assert.Equal(publisher.Id, first.SelectedSource);
        var second = await BoxOfficeQuery.GetAsync(db, publisher.Id, first.Feed.Next);
        Assert.Single(second.Feed.Items); Assert.Null(second.Feed.Next);
        Assert.Equal(14, first.Feed.Items.Concat(second.Feed.Items).Select(p => p.Id).Distinct().Count());
        Assert.Contains(first.Publishers, p => p.Id == empty.Id);
        Assert.DoesNotContain(first.Publishers, p => p.Id == hidden.Id);
        Assert.Empty((await BoxOfficeQuery.GetAsync(db, empty.Id, null)).Feed.Items);
        Assert.Null((await BoxOfficeQuery.GetAsync(db, hidden.Id, null)).SelectedSource);
        Assert.Null((await BoxOfficeQuery.GetAsync(db, foreign.Id, null)).SelectedSource);
        Assert.Contains(await FeedQuery.GetCategoriesAsync(db), c => c.Id == category.Id);
    }
    [Fact]
    public void Paging_links_keep_source_and_category() => Assert.Equal("?category=5&source=8&after=0.123.9", FeedQuery.QueryString(5, null, "0.123.9", 8));
}
