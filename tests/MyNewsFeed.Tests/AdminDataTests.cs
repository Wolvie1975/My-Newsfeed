using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using MyNewsFeed.Web.Data;
using MyNewsFeed.Web.Data.Models;

namespace MyNewsFeed.Tests;

// Integration tests against the live WebScraper database. Every test works inside a transaction that is
// never committed, so the real data is left untouched.
public class AdminDataTests
{
    private static WebScraperContext Create() => new(
        new DbContextOptionsBuilder<WebScraperContext>()
            .UseSqlServer("Server=sql2025,1433;Database=WebScraper;User Id=sa;Password=Fedora_Dev_2025!;Encrypt=False;TrustServerCertificate=True;")
            .Options);

    private static Source NewSource(string url, bool enabled = true) => new() { Url = url, Enabled = enabled };

    [Fact]
    public async Task Duplicate_source_url_is_detected()
    {
        await using var db = Create();
        await using var tx = await db.Database.BeginTransactionAsync();
        const string existingUrl = "https://test.invalid/duplicate";
        db.Sources.Add(NewSource(existingUrl));
        await db.SaveChangesAsync();


        db.Sources.Add(NewSource(existingUrl));
        var ex = await Assert.ThrowsAsync<DbUpdateException>(() => db.SaveChangesAsync());

        Assert.True(AdminData.IsUniqueViolation(ex));
    }

    [Fact]
    public async Task A_source_can_be_added_disabled()
    {
        await using var db = Create();
        await using var tx = await db.Database.BeginTransactionAsync();

        var source = NewSource("https://test.invalid/disabled", enabled: false);
        db.Sources.Add(source);
        await db.SaveChangesAsync();
        db.ChangeTracker.Clear();

        Assert.False((await db.Sources.SingleAsync(s => s.Id == source.Id)).Enabled);
    }

    [Fact]
    public async Task A_new_source_defaults_to_enabled_with_a_created_timestamp()
    {
        await using var db = Create();
        await using var tx = await db.Database.BeginTransactionAsync();

        var source = NewSource("https://test.invalid/enabled");
        db.Sources.Add(source);
        await db.SaveChangesAsync();
        db.ChangeTracker.Clear();

        var saved = await db.Sources.SingleAsync(s => s.Id == source.Id);
        Assert.True(saved.Enabled);
        Assert.True(saved.CreatedAt > DateTime.UtcNow.AddMinutes(-5));
    }

    [Fact]
    public async Task Deleting_a_source_with_pages_is_blocked_unless_pages_are_included()
    {
        await using var db = Create();
        await using var tx = await db.Database.BeginTransactionAsync();

        var source = NewSource("https://test.invalid/with-pages");
        source.Pages.Add(new Page { Url = "https://test.invalid/with-pages/a", Title = "A" });
        source.Pages.Add(new Page { Url = "https://test.invalid/with-pages/b", Title = "B" });
        db.Sources.Add(source);
        await db.SaveChangesAsync();

        var blocked = await Assert.ThrowsAnyAsync<Exception>(() => AdminData.DeleteSourceAsync(db, source.Id, includePages: false));
        Assert.True(AdminData.IsForeignKeyViolation(blocked));
        Assert.Equal(2, await db.Pages.CountAsync(p => p.SourceId == source.Id));

        Assert.True(await AdminData.DeleteSourceAsync(db, source.Id, includePages: true));
        Assert.Equal(0, await db.Pages.CountAsync(p => p.SourceId == source.Id));
        Assert.False(await db.Sources.AnyAsync(s => s.Id == source.Id));
    }

    [Fact]
    public async Task Deleting_a_source_without_pages_needs_no_page_removal()
    {
        await using var db = Create();
        await using var tx = await db.Database.BeginTransactionAsync();

        var source = NewSource("https://test.invalid/empty");
        db.Sources.Add(source);
        await db.SaveChangesAsync();

        Assert.True(await AdminData.DeleteSourceAsync(db, source.Id, includePages: false));
        Assert.False(await AdminData.DeleteSourceAsync(db, source.Id, includePages: false));
    }

    [Fact]
    public async Task Real_data_is_intact_after_the_test_run()
    {
        await using var db = Create();
        Assert.False(await db.Sources.AnyAsync(s => s.Url.StartsWith("https://test.invalid")));
    }
}
