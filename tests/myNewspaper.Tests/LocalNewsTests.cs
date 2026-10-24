using Microsoft.EntityFrameworkCore;
using myNewspaper.Web.Data;

namespace myNewspaper.Tests;

public class LocalNewsTests
{
    [Fact]
    public async Task Local_fallback_never_leaks_unrelated_or_disabled_articles_and_dedicated_category_takes_precedence()
    {
        await using var db = await Big12NewsTests.Fixture();
        var empty = await LocalNewsQuery.GetAsync(db, null);
        Assert.False(empty.Dedicated);
        Assert.Empty(empty.Feed.Items);
        await db.Database.ExecuteSqlRawAsync("UPDATE #Big12NewsPages SET Title=N'Kansas City update' WHERE ID IN (1,17)");
        var fallback = await LocalNewsQuery.GetAsync(db,null);
        Assert.Single(fallback.Feed.Items);
        Assert.Equal(1,fallback.Feed.Items[0].Id);
        await db.Database.ExecuteSqlRawAsync("""
            INSERT INTO #Big12NewsCategories VALUES (3,N'Kansas City News','20261019');
            UPDATE #Big12NewsSources SET SourceCategoryId=3 WHERE ID=102;
            """);
        var dedicated = await LocalNewsQuery.GetAsync(db,null);
        Assert.True(dedicated.Dedicated);
        Assert.Equal(new[]{15,16},dedicated.Feed.Items.Select(i=>i.Id));
        await db.Database.ExecuteSqlRawAsync("UPDATE #Big12NewsSources SET Enabled=0 WHERE ID=102");
        var disabled = await LocalNewsQuery.GetAsync(db,null);
        Assert.False(disabled.Dedicated);
        Assert.Equal(1,Assert.Single(disabled.Feed.Items).Id);
    }
}
