using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using myNewspaper.Web.Data;
using myNewspaper.Web.Saved;

namespace myNewspaper.Tests;

public class SharedArticlesTests
{
    [Theory]
    [InlineData("javascript:alert(1)")]
    [InlineData("file:///tmp/example")]
    [InlineData("https://user:password@example.com/")]
    [InlineData("/relative")]
    public void Rejects_unsafe_links(string url) => Assert.Throws<ArgumentException>(() => SharedArticles.NormalizeUrl(url));

    [Fact]
    public void Copies_text_and_limits_payload_without_interpreting_html()
    {
        var article = SharedArticles.Normalize(new("https://EXAMPLE.com", "<script>alert(1)</script>"));
        Assert.Equal("https://example.com/", article.Url);
        Assert.Equal("<script>alert(1)</script>", article.Title);
        Assert.NotNull(article.SavedAt);
        Assert.Throws<ArgumentException>(() => SharedArticles.Normalize(article with { Summary = new string('a', 4001) }));
    }

    [Fact]
    public void Video_copies_preserve_type_and_old_articles_default_to_article()
    {
        var video = SharedArticles.Normalize(new("https://www.youtube.com/watch?v=test", "Video", "Channel", Kind: "video"));
        Assert.Equal("video", video.Kind);
        var legacy = System.Text.Json.JsonSerializer.Deserialize<SavedArticle>("{\"Url\":\"https://example.com/\",\"Title\":\"Old article\"}");
        Assert.Equal("article", legacy!.Kind);
        Assert.Throws<ArgumentException>(() => SharedArticles.Normalize(video with { Kind = "unsupported" }));
    }

    // Profiles off: the legacy shared list. Profiles on: the profile's own row, created on first save, which leaves the
    // legacy list and every other profile's list alone.
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Sql_saved_list_merges_concurrent_saves_and_import_receipts_prevent_resurrection(bool perProfile)
    {
        var builder = new SqlConnectionStringBuilder(TestDatabase.ConnectionString);
        var database = "myNewspaperSavedTest_" + Guid.NewGuid().ToString("N");
        builder.InitialCatalog = "master";
        await using var admin = new SqlConnection(builder.ConnectionString);
        await admin.OpenAsync();
        await using var command = admin.CreateCommand();
        command.CommandText = $"CREATE DATABASE [{database}]";
        await command.ExecuteNonQueryAsync();
        try
        {
            builder.InitialCatalog = database;
            var factory = new Factory(builder.ConnectionString);
            int? owner = null, other = null;
            await using (var db = factory.CreateDbContext())
            {
                await db.Database.ExecuteSqlRawAsync(await File.ReadAllTextAsync(Path.GetFullPath("../../../../../db/018_shared_saved_articles.sql", AppContext.BaseDirectory)));
                if (perProfile)
                {
                    // 020 needs the two scraper tables it references; stand-ins with just their keys are enough here.
                    await db.Database.ExecuteSqlRawAsync("CREATE TABLE dbo.TvTrackedSeries (ID int NOT NULL PRIMARY KEY); CREATE TABLE dbo.YoutubeVideoFeed (ID int NOT NULL PRIMARY KEY);");
                    await db.Database.ExecuteSqlRawAsync((await File.ReadAllTextAsync(Path.GetFullPath("../../../../../db/020_profiles.sql", AppContext.BaseDirectory)))
                        .Replace("{", "{{").Replace("}", "}}"));   // raw SQL treats braces as placeholders
                    owner = (await db.Database.SqlQueryRaw<int>("INSERT dbo.MyDailyFeedProfiles (Name) OUTPUT inserted.Id AS Value VALUES (N'A')").ToListAsync()).Single();
                    other = (await db.Database.SqlQueryRaw<int>("INSERT dbo.MyDailyFeedProfiles (Name) OUTPUT inserted.Id AS Value VALUES (N'B')").ToListAsync()).Single();
                }
            }
            var first = new SharedArticles(factory);
            var second = new SharedArticles(factory);
            await Task.WhenAll(first.Add(owner, new("https://example.com/a", "A")), second.Add(owner, new("https://example.com/b", "B")));
            Assert.Equal(2, (await first.Read(owner)).Count);
            await first.Add(owner, new("https://example.com/a", "Duplicate"));
            Assert.Equal(2, (await second.Read(owner)).Count);
            var import = new SavedImport(Guid.NewGuid(), [new("https://example.com/a", "A"), new("https://example.com/legacy", "Legacy")]);
            Assert.Equal(3, (await first.Import(owner, import)).Count);
            await second.Remove(owner, "https://example.com/legacy");
            await first.Import(owner, import);
            Assert.DoesNotContain(await second.Read(owner), a => a.Url.EndsWith("legacy"));
            await second.Remove(owner, "https://example.com/a");
            await first.Remove(owner, "https://example.com/a");
            Assert.Single(await first.Read(owner));
            // Another service instance (as after restart) sees persisted copies.
            Assert.Single(await new SharedArticles(factory).Read(owner));
            var batch = Enumerable.Range(0, 499).Select(i => new SavedArticle($"https://example.com/{i}", "Saved")).ToArray();
            await first.Import(owner, new(Guid.NewGuid(), batch));
            await Assert.ThrowsAsync<SavedListFullException>(() => first.Add(owner, new("https://example.com/overflow", "Full")));
            Assert.Equal(500, (await second.Read(owner)).Count);
            if (perProfile)
            {
                Assert.Empty(await first.Read(other));
                Assert.Empty(await first.Read(null));
            }
        }
        finally
        {
            SqlConnection.ClearAllPools();
            command.CommandText = $"ALTER DATABASE [{database}] SET SINGLE_USER WITH ROLLBACK IMMEDIATE; DROP DATABASE [{database}]";
            await command.ExecuteNonQueryAsync();
        }
    }
    private sealed class Factory(string connection) : IDbContextFactory<WebScraperContext>
    {
        public WebScraperContext CreateDbContext() => new(new DbContextOptionsBuilder<WebScraperContext>().UseSqlServer(connection).Options);
    }
}
