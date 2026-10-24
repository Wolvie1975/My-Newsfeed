using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using myNewspaper.Web.Data;
using myNewspaper.Web.Data.Models;

namespace myNewspaper.Tests;

public class Big12NewsTests
{
    internal sealed class FixtureContext(DbContextOptions<WebScraperContext> options) : WebScraperContext(options)
    {
        protected override void OnModelCreating(ModelBuilder model)
        {
            base.OnModelCreating(model);
            model.Entity<SourceCategory>().ToTable("#Big12NewsCategories");
            model.Entity<Source>().ToTable("#Big12NewsSources");
            model.Entity<Page>().ToTable("#Big12NewsPages");
        }
    }
    internal static async Task<FixtureContext> Fixture()
    {
        var config = new ConfigurationBuilder().AddJsonFile(Path.GetFullPath("../../../../../src/myNewspaper.Web/appsettings.json", AppContext.BaseDirectory))
            .AddUserSecrets("c579816a-020e-445a-92f6-efdd7002106c").AddEnvironmentVariables().Build();
        var db = new FixtureContext(new DbContextOptionsBuilder<WebScraperContext>().UseSqlServer(config.GetConnectionString("DefaultConnection")).Options);
        await db.Database.OpenConnectionAsync();
        await db.Database.ExecuteSqlRawAsync("""
            CREATE TABLE #Big12NewsCategories (ID int, Category_Name nvarchar(100), DateAdded datetime2);
            CREATE TABLE #Big12NewsSources (ID int, Url nvarchar(2048), UrlHash varbinary(32), Label nvarchar(200), Enabled bit,
                CreatedAt datetime2, LastScrapedAt datetime2, LastError nvarchar(1000), SourceCategoryId int);
            CREATE TABLE #Big12NewsPages (ID int, Url nvarchar(2048), UrlHash varbinary(32), Title nvarchar(500), Description nvarchar(max),
                Published datetime2, ScrapedAt datetime2, SourceId int, ImageUrl nvarchar(2048));
            INSERT INTO #Big12NewsCategories VALUES (1,N'Big 12 News','20261018'),(2,N'Other','20261018');
            INSERT INTO #Big12NewsSources (ID,Url,Label,Enabled,CreatedAt,LastScrapedAt,SourceCategoryId) VALUES
                (101,'https://test.invalid/a','A',1,'20261018','20261018',1),
                (102,'https://test.invalid/b','B',1,'20261018','20261018',1),
                (103,'https://test.invalid/off','Disabled',0,'20261018','20261018',1),
                (104,'https://test.invalid/empty','Empty',1,'20261018','20261018',1),
                (105,'https://test.invalid/other','Other',1,'20261018','20261018',2);
            INSERT INTO #Big12NewsPages (ID,Url,Title,Published,ScrapedAt,SourceId)
                SELECT n,CONCAT('https://articles.test/',n),CONCAT('Article ',n),
                    CASE WHEN n=14 THEN NULL ELSE DATEADD(minute,-n,CONVERT(datetime2,'20261018')) END,
                    '20261018',CASE WHEN n<=14 THEN 101 WHEN n<=16 THEN 102 WHEN n=17 THEN 103 ELSE 105 END
                FROM (VALUES(1),(2),(3),(4),(5),(6),(7),(8),(9),(10),(11),(12),(13),(14),(15),(16),(17),(18)) numbers(n);
            """);
        return db;
    }

    [Fact]
    public async Task Fixed_category_sources_counts_and_cursor_paging_never_include_other_or_disabled_feeds()
    {
        await using var db = await Fixture();
        var data = await Big12NewsQuery.GetAsync(db, null, null, null);
        Assert.Equal(1, data.CategoryId);
        Assert.Equal(new[] { 101, 102, 104 }, data.Sources.Select(s => s.Id));
        Assert.Equal(16, data.Feed.Total); Assert.Equal(13, data.Feed.Items.Count);
        Assert.All(data.Feed.Items, p => Assert.Equal("Big 12 News", p.CategoryName));
        var next = await Big12NewsQuery.GetAsync(db, null, null, data.Feed.Next);
        Assert.Equal(16, next.Feed.Total); Assert.Equal(3, next.Feed.Items.Count); Assert.Equal(0, next.Feed.Remaining);
        Assert.True(next.Feed.Items[^1].Undated);
        Assert.Equal(16, data.Feed.Items.Concat(next.Feed.Items).Select(p => p.Id).Distinct().Count());
        var source = await Big12NewsQuery.GetAsync(db, 101, null, null);
        Assert.Equal(14, source.Feed.Total); Assert.Equal(101, source.SourceId);
        Assert.All(source.Feed.Items, p => Assert.Equal("A", p.SourceLabel));
        Assert.Equal(16, (await Big12NewsQuery.GetAsync(db, 105, null, null)).Feed.Total);
        Assert.Equal(16, (await Big12NewsQuery.GetAsync(db, 103, null, null)).Feed.Total);
        var empty = await Big12NewsQuery.GetAsync(db, 104, null, null);
        Assert.Empty(empty.Feed.Items); Assert.Equal(3, empty.Sources.Count);
    }

    [Fact]
    public async Task Missing_or_disabled_big12_category_is_empty_even_when_other_news_exists()
    {
        await using var db = await Fixture();
        await db.Database.ExecuteSqlRawAsync("UPDATE #Big12NewsSources SET Enabled=0 WHERE SourceCategoryId=1");
        var data = await Big12NewsQuery.GetAsync(db, 105, null, null);
        Assert.Null(data.CategoryId); Assert.Empty(data.Sources); Assert.Equal(0, data.Feed.Total);
        await db.Database.ExecuteSqlRawAsync("DELETE FROM #Big12NewsCategories WHERE ID=1");
        Assert.Empty((await Big12NewsQuery.GetAsync(db, null, null, null)).Feed.Items);
        Assert.True(await db.Pages.AnyAsync(p => p.SourceId == 105));
    }
}
