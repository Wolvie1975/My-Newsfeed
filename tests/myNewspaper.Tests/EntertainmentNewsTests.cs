using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using myNewspaper.Web.Data;
using myNewspaper.Web.Data.Models;
namespace myNewspaper.Tests;
public class EntertainmentNewsTests
{
    private sealed class FixtureContext(DbContextOptions<WebScraperContext> options) : WebScraperContext(options)
    {
        protected override void OnModelCreating(ModelBuilder model) {
            base.OnModelCreating(model);
            model.Entity<Page>().ToTable("#NewsPages");
            model.Entity<Source>().ToTable("#NewsSources");
            model.Entity<SourceCategory>().ToTable("#NewsCategories");
        }
    }
    [Theory]
    [InlineData("Entertainment", "entertainment/news")]
    [InlineData("TV/Film", "entertainment/news/tv-film")]
    [InlineData("Top Headlines", null)]
    [InlineData("Sports", "sports/news")]
    [InlineData("Men's College Basketball News", "sports/news/basketball")]
    [InlineData("Big 12 News", "sports/news/big12")]
    [InlineData("F1 News", "sports/news/f1")]
    public void Categories_have_stable_section_routes(string name, string? route) => Assert.Equal(route, NewsSections.RouteFor(name));

    [Fact]
    public async Task Headlines_exclude_moved_categories_and_sources_through_paging_without_changing_other_readers()
    {
        var config = new ConfigurationBuilder().AddJsonFile(Path.GetFullPath("../../../../../src/myNewspaper.Web/appsettings.json", AppContext.BaseDirectory))
            .AddUserSecrets("c579816a-020e-445a-92f6-efdd7002106c").AddEnvironmentVariables().Build();
        await using var db = new FixtureContext(new DbContextOptionsBuilder<WebScraperContext>().UseSqlServer(config.GetConnectionString("DefaultConnection")).Options);
        await db.Database.OpenConnectionAsync();
        await db.Database.ExecuteSqlRawAsync("""
            CREATE TABLE #NewsCategories (ID int, Category_Name nvarchar(100), DateAdded datetime2, NewsHub nvarchar(16) NOT NULL DEFAULT 'headlines');
            CREATE TABLE #NewsSources (ID int, Url nvarchar(2048), Label nvarchar(200), Enabled bit, SourceCategoryId int NULL, LastScrapedAt datetime2 NULL, LastError nvarchar(1000) NULL);
            CREATE TABLE #NewsPages (ID int, Url nvarchar(2048), Title nvarchar(500), Description nvarchar(max), ImageUrl nvarchar(2048), Published datetime2 NULL, ScrapedAt datetime2, SourceId int);
            INSERT #NewsCategories(ID,Category_Name,DateAdded) VALUES (1,'Entertainment','20261024'),(2,'TV/Film','20261024'),(3,'General','20261024'),(4,'Sports','20261024'),(5,'Big 12 News','20261024'),(6,'F1 News','20261024'),(7,'Men''s College Basketball News','20261024');
            UPDATE #NewsCategories SET NewsHub=CASE WHEN ID IN (1,2) THEN 'entertainment' WHEN ID IN (4,5,6,7) THEN 'sports' ELSE 'headlines' END;
            INSERT #NewsSources VALUES (10,'https://test.invalid/e','Entertainment',1,1,'20261024',NULL),(20,'https://test.invalid/t','Film',1,2,'20261024',NULL),
              (30,'https://test.invalid/g','General',1,3,'20261020',NULL),(40,'https://test.invalid/u','Uncategorised',1,NULL,'20261019',NULL),
              (50,'https://test.invalid/d','Disabled',0,1,'20261025',NULL),
              (60,'https://test.invalid/s','Sports',1,4,'20261026',NULL),(70,'https://test.invalid/b','Big12',1,5,'20261026',NULL),
              (80,'https://test.invalid/f','F1',1,6,'20261026',NULL),(90,'https://test.invalid/m','Basketball',1,7,'20261026',NULL);
            INSERT #NewsPages VALUES
              (1,'https://test.invalid/1','Entertainment new',NULL,NULL,'20261024','20261024',10),
              (2,'https://test.invalid/2','Entertainment old',NULL,NULL,'20261023','20261023',10),
              (3,'https://test.invalid/3','Film',NULL,NULL,'20261024','20261024',20),
              (4,'https://test.invalid/4','Headline',NULL,NULL,'20261022','20261022',30),
              (5,'https://test.invalid/5','Headline older',NULL,NULL,'20261021','20261021',30),
              (6,'https://test.invalid/6','Uncategorised',NULL,NULL,NULL,'20261020',40),
              (7,'https://test.invalid/7','Disabled',NULL,NULL,'20261025','20261025',50),
              (8,'https://test.invalid/8','Sports',NULL,NULL,'20261026','20261026',60),
              (9,'https://test.invalid/9','Big12',NULL,NULL,'20261026','20261026',70),
              (10,'https://test.invalid/10','F1',NULL,NULL,'20261026','20261026',80),
              (11,'https://test.invalid/11','Basketball',NULL,NULL,'20261026','20261026',90);
            """);
        Assert.Equal(new[] {30,40}, (await FeedQuery.GetSourcesAsync(db,null,headlinesOnly:true)).Select(s=>s.Id));
        Assert.Equal(new DateTime(2026,10,20), await FeedQuery.GetLastUpdatedAsync(db,headlinesOnly:true));
        var first = await FeedQuery.GetFeedAsync(db,null,null,null,1,headlinesOnly:true);
        Assert.Equal(3,first.Total); Assert.Equal(4,Assert.Single(first.Items).Id);
        var next = await FeedQuery.GetFeedAsync(db,null,null,first.Next,headlinesOnly:true);
        Assert.Equal(new[] {5,6},next.Items.Select(i=>i.Id));
        Assert.Empty((await FeedQuery.GetFeedAsync(db,1,null,null,headlinesOnly:true)).Items);
        Assert.Empty((await FeedQuery.GetFeedAsync(db,null,null,null,sourceId:10,headlinesOnly:true)).Items);
        var entertainment = await FeedQuery.GetFeedAsync(db,1,null,null,1);
        Assert.Equal(2,entertainment.Total);
        Assert.Equal(2,Assert.Single((await FeedQuery.GetFeedAsync(db,1,null,entertainment.Next)).Items).Id);
        Assert.Single((await FeedQuery.GetFeedAsync(db,2,null,null)).Items);
        foreach (var category in new[] {4,5,6,7}) {
            Assert.Empty((await FeedQuery.GetFeedAsync(db,category,null,null,headlinesOnly:true)).Items);
            Assert.Single((await FeedQuery.GetFeedAsync(db,category,null,null)).Items);
        }
        Assert.Single((await F1NewsQuery.GetAsync(db,null,null,null)).Feed.Items);
        Assert.Single((await Big12NewsQuery.GetAsync(db,null,null,null)).Feed.Items);
        Assert.Equal(10,(await FeedQuery.GetFeedAsync(db,null,null,null)).Total);
        // Arbitrary future categories require no name matching or code registration.
        await db.Database.ExecuteSqlRawAsync("""
            INSERT #NewsCategories VALUES (8,'Pro hoops','20261024','sports'),(9,'New streaming releases','20261024','entertainment');
            INSERT #NewsSources VALUES (100,'https://test.invalid/new-s','New sports',1,8,'20261026',NULL),(110,'https://test.invalid/new-e','New entertainment',1,9,'20261026',NULL);
            INSERT #NewsPages VALUES (12,'https://test.invalid/12','Future sports',NULL,NULL,'20261026','20261026',100),(13,'https://test.invalid/13','Future entertainment',NULL,NULL,'20261026','20261026',110);
            """);
        var future = (await FeedQuery.GetCategoriesAsync(db)).Single(c => c.Id == 8);
        Assert.Equal("sports",future.Hub);
        Assert.Equal("sports/news/category-8",NewsSections.RouteFor(future));
        Assert.Equal(3,(await FeedQuery.GetFeedAsync(db,null,null,null,headlinesOnly:true)).Total);
        Assert.Single((await FeedQuery.GetFeedAsync(db,8,null,null)).Items);
        var moved = await db.SourceCategories.SingleAsync(c => c.Id == 8);
        moved.NewsHub = "entertainment"; moved.CategoryName = "Renamed coverage";
        await db.SaveChangesAsync();
        future = (await FeedQuery.GetCategoriesAsync(db)).Single(c => c.Id == 8);
        Assert.Equal("entertainment/news/category-8",NewsSections.RouteFor(future));
        Assert.Equal("Renamed coverage",future.Name);
        moved.NewsHub = "headlines"; await db.SaveChangesAsync();
        Assert.Equal(4,(await FeedQuery.GetFeedAsync(db,null,null,null,headlinesOnly:true)).Total);
        Assert.Contains(await FeedQuery.GetSourcesAsync(db,null,headlinesOnly:true),s => s.Id == 100);

        await db.Database.ExecuteSqlRawAsync("""
            INSERT #NewsCategories VALUES (10,'Opinion','20261024','headlines');
            INSERT #NewsSources VALUES (120,'https://test.invalid/opinion-a','Opinion A',1,10,'20261026',NULL),(130,'https://test.invalid/opinion-b','Opinion B',1,10,'20261026',NULL),(140,'https://test.invalid/retired','Retired opinion',0,10,'20261026',NULL),(150,'https://test.invalid/empty','Empty opinion',1,10,'20261026',NULL);
            INSERT #NewsPages VALUES (14,'https://test.invalid/14','Active opinion A',NULL,NULL,'20261026','20261026',120),(15,'https://test.invalid/15','Active opinion B',NULL,NULL,'20261026','20261026',130),(16,'https://test.invalid/16','Retired opinion',NULL,NULL,'20261027','20261027',140);
            """);
        var opinionA=await HomeOpinionQuery.GetAsync(db,new DateTime(2026,10,24,12,0,0,DateTimeKind.Utc));
        var opinionB=await HomeOpinionQuery.GetAsync(db,new DateTime(2026,10,24,13,0,0,DateTimeKind.Utc));
        Assert.Equal(10,opinionA.CategoryId);
        Assert.NotEqual(opinionA.Article!.SourceUrl,opinionB.Article!.SourceUrl);
        Assert.Equal(new[]{14,15},new[]{opinionA.Article.Id,opinionB.Article.Id}.Order());

    }
}
