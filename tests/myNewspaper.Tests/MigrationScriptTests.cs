using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace myNewspaper.Tests;

/// <summary>
/// Migration scripts behave as their headers say (dotnet-coding-standard §13): one transaction, and a re-run never
/// redoes or overrides anything. 008 runs in a scratch database built in 007's layout; 024 runs on the test database
/// inside a transaction that is rolled back.
/// </summary>
public class MigrationScriptTests
{
    private static string Script(string name) =>
        File.ReadAllText(Path.GetFullPath("../../../../../db/" + name, AppContext.BaseDirectory));

    private static async Task<object?> ScalarAsync(SqlConnection connection, string sql)
    {
        await using var command = new SqlCommand(sql, connection);
        return await command.ExecuteScalarAsync();
    }

    private static async Task ExecuteAsync(SqlConnection connection, string sql)
    {
        await using var command = new SqlCommand(sql, connection);
        await command.ExecuteNonQueryAsync();
    }

    /// <summary>A scratch database with dbo.YoutubeVideoFeed as 007 left it (Url after DateAdded) and a linked video.</summary>
    private static async Task WithScratchDatabaseAsync(Func<SqlConnection, Task> test, string extraColumn = "")
    {
        var database = "myNewspaper_migrations_" + Guid.NewGuid().ToString("N")[..8];
        var builder = new SqlConnectionStringBuilder(TestDatabase.ConnectionString) { InitialCatalog = "master" };
        await using var master = new SqlConnection(builder.ConnectionString);
        await master.OpenAsync();
        await ExecuteAsync(master, $"CREATE DATABASE [{database}]");
        try
        {
            builder.InitialCatalog = database;
            await using var connection = new SqlConnection(builder.ConnectionString);
            await connection.OpenAsync();
            await ExecuteAsync(connection, $"""
                CREATE TABLE dbo.YoutubeVideoFeed (
                    ID int IDENTITY(1,1) NOT NULL CONSTRAINT PK_YoutubeVideoFeed PRIMARY KEY,
                    ChannelId nvarchar(40) NOT NULL,
                    ChannelName nvarchar(200) NULL,
                    DateAdded datetime2 NOT NULL CONSTRAINT DF_YoutubeVideoFeed_DateAdded DEFAULT (sysutcdatetime()),
                    Url nvarchar(2048) NOT NULL {extraColumn},
                    CONSTRAINT UQ_YoutubeVideoFeed_ChannelId UNIQUE (ChannelId));
                CREATE TABLE dbo.YouTubeVideos (
                    ID int IDENTITY(1,1) NOT NULL PRIMARY KEY,
                    YoutubeVideoFeedId int NULL CONSTRAINT FK_YouTubeVideos_YoutubeVideoFeed REFERENCES dbo.YoutubeVideoFeed (ID));
                SET IDENTITY_INSERT dbo.YoutubeVideoFeed ON;
                INSERT dbo.YoutubeVideoFeed (ID, ChannelId, ChannelName, DateAdded, Url)
                VALUES (7, N'UC7', N'Seven', '2026-01-01', N'https://www.youtube.com/channel/UC7'),
                       (9, N'UC9', NULL, '2026-01-02', N'https://www.youtube.com/channel/UC9');
                SET IDENTITY_INSERT dbo.YoutubeVideoFeed OFF;
                INSERT dbo.YouTubeVideos (YoutubeVideoFeedId) VALUES (9);
                """);
            await test(connection);
        }
        finally
        {
            SqlConnection.ClearAllPools();
            await ExecuteAsync(master, $"ALTER DATABASE [{database}] SET SINGLE_USER WITH ROLLBACK IMMEDIATE; DROP DATABASE [{database}]");
        }
    }

    private static Task<object?> ColumnsAsync(SqlConnection connection) => ScalarAsync(connection,
        "SELECT STRING_AGG(name, ',') WITHIN GROUP (ORDER BY column_id) FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.YoutubeVideoFeed')");

    private static Task<object?> LinkedFeedAsync(SqlConnection connection) => ScalarAsync(connection,
        "SELECT f.ChannelId FROM dbo.YouTubeVideos v JOIN dbo.YoutubeVideoFeed f ON f.ID = v.YoutubeVideoFeedId");

    private static Task<object?> ForeignKeyAsync(SqlConnection connection) => ScalarAsync(connection,
        "SELECT COUNT(*) FROM sys.foreign_keys WHERE name = N'FK_YouTubeVideos_YoutubeVideoFeed'");

    [Fact]
    public Task Migration_008_reorders_the_columns_keeping_ids_and_links_and_a_rerun_does_nothing() =>
        WithScratchDatabaseAsync(async connection =>
        {
            await ExecuteAsync(connection, Script("008_youtube_video_feed_column_order.sql"));
            Assert.Equal("ID,ChannelId,ChannelName,Url,DateAdded", await ColumnsAsync(connection));
            Assert.Equal("UC9", await LinkedFeedAsync(connection));
            Assert.Equal(1, await ForeignKeyAsync(connection));
            Assert.Equal(16, await ScalarAsync(connection, "SELECT SUM(ID) FROM dbo.YoutubeVideoFeed"));

            await ExecuteAsync(connection, "INSERT dbo.YoutubeVideoFeed (ChannelId, Url) VALUES (N'UC10', N'https://www.youtube.com/channel/UC10')");
            await ExecuteAsync(connection, Script("008_youtube_video_feed_column_order.sql"));
            Assert.Equal(3, await ScalarAsync(connection, "SELECT COUNT(*) FROM dbo.YoutubeVideoFeed"));
            Assert.Equal(DBNull.Value, await ScalarAsync(connection, "SELECT OBJECT_ID(N'dbo.YoutubeVideoFeed_old')"));
        });

    [Fact]
    public Task Migration_008_failing_part_way_leaves_the_table_as_it_was() =>
        WithScratchDatabaseAsync(async connection =>
        {
            // The rename to YoutubeVideoFeed_old fails after the foreign key is dropped and three renames succeeded.
            await ExecuteAsync(connection, "CREATE TABLE dbo.YoutubeVideoFeed_old (ID int)");

            await Assert.ThrowsAsync<SqlException>(() => ExecuteAsync(connection, Script("008_youtube_video_feed_column_order.sql")));

            Assert.Equal("ID,ChannelId,ChannelName,DateAdded,Url", await ColumnsAsync(connection));
            Assert.Equal(1, await ForeignKeyAsync(connection));
            Assert.Equal("UC9", await LinkedFeedAsync(connection));
            Assert.Equal(1, await ScalarAsync(connection, "SELECT COUNT(*) FROM sys.key_constraints WHERE name = N'PK_YoutubeVideoFeed'"));
        });

    [Fact]
    public Task Migration_008_never_rebuilds_a_table_later_migrations_added_columns_to() =>
        WithScratchDatabaseAsync(async connection =>
        {
            await ExecuteAsync(connection, Script("008_youtube_video_feed_column_order.sql"));
            Assert.Equal("ID,ChannelId,ChannelName,DateAdded,Url,IsHouseholdPick", await ColumnsAsync(connection));
        }, extraColumn: ", IsHouseholdPick bit NOT NULL CONSTRAINT DF_scratch_pick DEFAULT (0)");

    private const string TechCategory = "Linux";

    private static async Task<string> HubAsync(DbContext db) =>
        (await db.Database.SqlQuery<string>($"SELECT NewsHub AS Value FROM dbo.SourceCategories WHERE Category_Name = {TechCategory}").ToListAsync()).Single();

    private static async Task EnsureCategoryAsync(DbContext db) =>
        await db.Database.ExecuteSqlAsync($"""
            IF NOT EXISTS (SELECT 1 FROM dbo.SourceCategories WHERE Category_Name = {TechCategory})
                INSERT dbo.SourceCategories (Category_Name, DateAdded, NewsHub) VALUES ({TechCategory}, SYSUTCDATETIME(), N'tech');
            """);

    [Fact]
    public async Task Migration_024_rerun_keeps_a_tech_category_an_admin_moved_back_to_headlines()
    {
        await using var db = TestDatabase.CreateContext();
        await using var tx = await db.Database.BeginTransactionAsync();
        await EnsureCategoryAsync(db);
        await db.Database.ExecuteSqlAsync($"UPDATE dbo.SourceCategories SET NewsHub = N'headlines' WHERE Category_Name = {TechCategory}");

        await db.Database.ExecuteSqlRawAsync(Script("024_tech_hub.sql"));

        Assert.Equal("headlines", await HubAsync(db));
    }

    [Fact]
    public async Task Migration_024_first_run_still_moves_the_tech_categories()
    {
        await using var db = TestDatabase.CreateContext();
        await using var tx = await db.Database.BeginTransactionAsync();
        await EnsureCategoryAsync(db);
        await db.Database.ExecuteSqlAsync($"UPDATE dbo.SourceCategories SET NewsHub = N'headlines' WHERE Category_Name = {TechCategory}");
        // The database as it was before 024: the hub constraint doesn't allow 'tech' yet.
        await db.Database.ExecuteSqlRawAsync("""
            ALTER TABLE dbo.SourceCategories DROP CONSTRAINT CK_SourceCategories_NewsHub;
            ALTER TABLE dbo.SourceCategories WITH NOCHECK ADD CONSTRAINT CK_SourceCategories_NewsHub
                CHECK (NewsHub IN (N'headlines', N'sports', N'entertainment'));
            """);

        await db.Database.ExecuteSqlRawAsync(Script("024_tech_hub.sql"));

        Assert.Equal("tech", await HubAsync(db));
    }
}
