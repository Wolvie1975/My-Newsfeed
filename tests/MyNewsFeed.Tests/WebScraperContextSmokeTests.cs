using Microsoft.EntityFrameworkCore;
using MyNewsFeed.Web.Data;
using MyNewsFeed.Web.Data.Models;

namespace MyNewsFeed.Tests;

// Checks against the live WebScraper database (sql2025 must be reachable). Each test creates its own rows inside a
// transaction that is never committed, so it does not depend on what is currently stored.
public class WebScraperContextSmokeTests
{
    private static WebScraperContext Create() => new(
        new DbContextOptionsBuilder<WebScraperContext>()
            .UseSqlServer("Server=sql2025,1433;Database=WebScraper;User Id=sa;Password=Fedora_Dev_2025!;Encrypt=False;TrustServerCertificate=True;")
            .Options);

    [Fact]
    public async Task Pages_load_with_their_source()
    {
        await using var db = Create();
        await using var tx = await db.Database.BeginTransactionAsync();

        var source = new Source { Url = "https://test.invalid/smoke" };
        source.Pages.Add(new Page { Url = "https://test.invalid/smoke/1", Title = "Smoke" });
        db.Sources.Add(source);
        await db.SaveChangesAsync();
        db.ChangeTracker.Clear();

        var page = await db.Pages.Include(p => p.Source).SingleAsync(p => p.SourceId == source.Id);
        Assert.Equal(source.Url, page.Source.Url);
    }

    [Fact]
    public async Task Computed_url_hash_is_populated()
    {
        await using var db = Create();
        await using var tx = await db.Database.BeginTransactionAsync();

        var source = new Source { Url = "https://test.invalid/hash" };
        db.Sources.Add(source);
        await db.SaveChangesAsync();
        db.ChangeTracker.Clear();

        var saved = await db.Sources.SingleAsync(s => s.Id == source.Id);
        Assert.Equal(32, saved.UrlHash!.Length);
    }
}
