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

    [Fact]
    public async Task Sql_shared_list_merges_concurrent_saves_and_import_receipts_prevent_resurrection()
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
            await using (var db = factory.CreateDbContext())
                await db.Database.ExecuteSqlRawAsync(await File.ReadAllTextAsync(Path.GetFullPath("../../../../../db/018_shared_saved_articles.sql", AppContext.BaseDirectory)));
            var first = new SharedArticles(factory);
            var second = new SharedArticles(factory);
            await Task.WhenAll(first.Add(new("https://example.com/a", "A")), second.Add(new("https://example.com/b", "B")));
            Assert.Equal(2, (await first.Read()).Count);
            await first.Add(new("https://example.com/a", "Duplicate"));
            Assert.Equal(2, (await second.Read()).Count);
            var import = new SavedImport(Guid.NewGuid(), [new("https://example.com/a", "A"), new("https://example.com/legacy", "Legacy")]);
            Assert.Equal(3, (await first.Import(import)).Count);
            await second.Remove("https://example.com/legacy");
            await first.Import(import);
            Assert.DoesNotContain(await second.Read(), a => a.Url.EndsWith("legacy"));
            await second.Remove("https://example.com/a");
            await first.Remove("https://example.com/a");
            Assert.Single(await first.Read());
            // Another service instance (as after restart) sees persisted copies.
            Assert.Single(await new SharedArticles(factory).Read());
            var batch = Enumerable.Range(0, 499).Select(i => new SavedArticle($"https://example.com/{i}", "Saved")).ToArray();
            await first.Import(new(Guid.NewGuid(), batch));
            await Assert.ThrowsAsync<SavedListFullException>(() => first.Add(new("https://example.com/overflow", "Full")));
            Assert.Equal(500, (await second.Read()).Count);
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
