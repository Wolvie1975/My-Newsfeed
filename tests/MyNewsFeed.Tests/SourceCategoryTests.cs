using Microsoft.EntityFrameworkCore;
using MyNewsFeed.Web.Data;
using MyNewsFeed.Web.Data.Models;

namespace MyNewsFeed.Tests;

// Integration tests against the live WebScraper database, each inside a transaction that is never committed.
public class SourceCategoryTests
{
    private static WebScraperContext Create() => new(
        new DbContextOptionsBuilder<WebScraperContext>()
            .UseSqlServer("Server=sql2025,1433;Database=WebScraper;User Id=sa;Password=Fedora_Dev_2025!;Encrypt=False;TrustServerCertificate=True;")
            .Options);

    [Fact]
    public async Task A_new_category_gets_a_date_added()
    {
        await using var db = Create();
        await using var tx = await db.Database.BeginTransactionAsync();

        var category = new SourceCategory { CategoryName = "Test News" };
        db.SourceCategories.Add(category);
        await db.SaveChangesAsync();
        db.ChangeTracker.Clear();

        var saved = await db.SourceCategories.SingleAsync(c => c.Id == category.Id);
        Assert.Equal("Test News", saved.CategoryName);
        Assert.True(saved.DateAdded > DateTime.UtcNow.AddMinutes(-5));
    }

    [Theory]
    [InlineData("Test News")]
    [InlineData("test news")] // the database collation is case-insensitive
    public async Task Duplicate_category_names_are_rejected(string second)
    {
        await using var db = Create();
        await using var tx = await db.Database.BeginTransactionAsync();

        db.SourceCategories.Add(new SourceCategory { CategoryName = "Test News" });
        await db.SaveChangesAsync();

        db.SourceCategories.Add(new SourceCategory { CategoryName = second });
        var ex = await Assert.ThrowsAsync<DbUpdateException>(() => db.SaveChangesAsync());

        Assert.True(AdminData.IsUniqueViolation(ex));
    }

    [Fact]
    public async Task A_category_in_use_cannot_be_deleted()
    {
        await using var db = Create();
        await using var tx = await db.Database.BeginTransactionAsync();

        var category = new SourceCategory { CategoryName = "Test In Use" };
        db.Sources.Add(new Source { Url = "https://test.invalid/categorized", SourceCategory = category });
        await db.SaveChangesAsync();

        var ex = await Assert.ThrowsAnyAsync<Exception>(
            () => db.SourceCategories.Where(c => c.Id == category.Id).ExecuteDeleteAsync());
        Assert.True(AdminData.IsForeignKeyViolation(ex));
    }

    [Fact]
    public async Task An_unused_category_can_be_deleted_and_sources_can_be_uncategorized()
    {
        await using var db = Create();
        await using var tx = await db.Database.BeginTransactionAsync();

        var category = new SourceCategory { CategoryName = "Test Temporary" };
        var source = new Source { Url = "https://test.invalid/uncategorize", SourceCategory = category };
        db.Sources.Add(source);
        await db.SaveChangesAsync();

        source.SourceCategoryId = null;
        await db.SaveChangesAsync();

        Assert.Equal(1, await db.SourceCategories.Where(c => c.Id == category.Id).ExecuteDeleteAsync());
        Assert.Null((await db.Sources.AsNoTracking().SingleAsync(s => s.Id == source.Id)).SourceCategoryId);
    }

    [Fact]
    public async Task No_test_rows_are_left_in_the_database()
    {
        await using var db = Create();

        Assert.False(await db.SourceCategories.AnyAsync(c => c.CategoryName.StartsWith("Test ")));
        Assert.False(await db.Sources.AnyAsync(s => s.Url.StartsWith("https://test.invalid")));
    }
}
