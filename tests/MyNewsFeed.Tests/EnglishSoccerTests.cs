using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using MyNewsFeed.Web.Data;
using MyNewsFeed.Web.Data.Models;
namespace MyNewsFeed.Tests;

public class EnglishSoccerTests
{
    [Theory]
    [InlineData(true, 2, 0, "STATUS_FULL_TIME", "Wrexham win")]
    [InlineData(false, 0, 2, "STATUS_FINAL", "Wrexham win")]
    [InlineData(false, 2, 0, "STATUS_FINAL", "Wrexham loss")]
    [InlineData(true, 1, 1, "STATUS_FULL_TIME", "Draw")]
    [InlineData(true, 1, 1, "STATUS_FINAL_PEN", "Penalties")]
    [InlineData(true, 0, 0, "STATUS_POSTPONED", null)]
    [InlineData(true, null, 0, "STATUS_FINAL", null)]
    public void Outcome_respects_home_away_missing_goals_and_penalties(bool home, int? homeScore, int? awayScore, string status, string? expected) =>
        Assert.Equal(expected, ChampionshipResultsQuery.WrexhamOutcome(new SportsMatchResult
        { HomeTeamId = home ? 352 : 1, AwayTeamId = home ? 1 : 352, HomeScore = homeScore, AwayScore = awayScore, ScheduleStatus = status }));

    [Theory]
    [InlineData("espn-championship")]
    [InlineData("espn-epl")]
    public void English_tables_use_soccer_columns_and_keep_provider_order(string provider)
    {
        var rows = new[] { new SportsStanding { Id = 1, IsCurrent = true, GroupId = "league", Position = 2, TeamName = "A", StatsJson = "[]" },
            new SportsStanding { Id = 2, IsCurrent = true, GroupId = "league", Position = 1, TeamName = "B", StatsJson = "[]" } };
        var group = Assert.Single(StandingsQuery.BuildGroups(rows, provider, NullLogger.Instance));
        Assert.Equal(new long[] { 2, 1 }, group.Teams.Select(t => t.Row.Id));
        Assert.Equal(new[] { "P", "W", "D", "L", "GF", "GA", "GD", "Pts" }, group.Columns.Select(c => c.Label));
        Assert.Equal("—", StandingsQuery.StatisticValue(group.Teams[0], "points"));
    }

    private sealed class FixtureContext(DbContextOptions<WebScraperContext> options) : WebScraperContext(options)
    {
        protected override void OnModelCreating(ModelBuilder model)
        {
            base.OnModelCreating(model);
            model.Entity<SportsEventsType>().ToTable("EnglishFeedFixture");
            model.Entity<SportsMatchResult>().ToTable("EnglishResultFixture");
        }
    }
    [Fact]
    public async Task Results_filter_disabled_nonfinal_missing_and_old_seasons_then_preserve_last_good_on_feed_error()
    {
        var config = new ConfigurationBuilder().AddJsonFile(Path.GetFullPath("../../../../../src/MyNewsFeed.Web/appsettings.json", AppContext.BaseDirectory))
            .AddUserSecrets("c579816a-020e-445a-92f6-efdd7002106c").AddEnvironmentVariables().Build();
        await using var db = new FixtureContext(new DbContextOptionsBuilder<WebScraperContext>().UseSqlServer(config.GetConnectionString("DefaultConnection")).Options);
        await using var transaction = await db.Database.BeginTransactionAsync();
        await db.Database.ExecuteSqlRawAsync("""
            CREATE TABLE dbo.EnglishFeedFixture (ID int PRIMARY KEY, RssUrl nvarchar(2048) NOT NULL, EventsTypeName nvarchar(200) NOT NULL,
             SchoolName nvarchar(100) NULL, DateAdded datetime2 NOT NULL, ProviderKey varchar(50) NULL, ProviderSchoolId int NULL, Enabled bit NOT NULL,
             LastAttemptAt datetime2 NULL, LastSuccessAt datetime2 NULL, LastError nvarchar(1000) NULL, LastEventCount int NULL, LastIssueCount int NULL);
            CREATE TABLE dbo.EnglishResultFixture (ID bigint PRIMARY KEY, SourceId int NOT NULL, ProviderKey nvarchar(50) NOT NULL,
             GameId int NOT NULL, Season int NOT NULL, MatchDate date NOT NULL, StartsAtUtc datetime2 NOT NULL,
             HomeTeamId int NOT NULL, AwayTeamId int NOT NULL, HomeTeamName nvarchar(200) NOT NULL, AwayTeamName nvarchar(200) NOT NULL,
             HomeScore int NULL, AwayScore int NULL, HomeLogoUrl nvarchar(2048) NULL, AwayLogoUrl nvarchar(2048) NULL,
             ScheduleStatus nvarchar(100) NOT NULL, SourceUrl nvarchar(2048) NULL, FirstSeenAt datetime2 NOT NULL, LastSeenAt datetime2 NOT NULL);
            INSERT dbo.EnglishFeedFixture (ID,RssUrl,EventsTypeName,DateAdded,ProviderKey,Enabled)
             VALUES (1,'https://test.invalid','Championship',GETUTCDATE(),'espn-championship',1),
                    (2,'https://test.invalid/disabled','Disabled',GETUTCDATE(),'espn-championship',0);
            """);
        SportsMatchResult Row(int id, int season = 2026, string status = "STATUS_FULL_TIME", int source = 1) => new()
        {
            Id = id, SourceId = source, ProviderKey = "espn-championship", GameId = id, Season = season,
            MatchDate = new(2026, 10, 1), StartsAtUtc = new DateTime(2026, 10, 1, 12, 0, 0).AddMinutes(id),
            HomeTeamId = 352, AwayTeamId = 1, HomeTeamName = "Wrexham", AwayTeamName = "Opponent",
            HomeScore = 2, AwayScore = 1, ScheduleStatus = status, FirstSeenAt = DateTime.UtcNow, LastSeenAt = DateTime.UtcNow
        };
        var away = Row(2); away.HomeTeamId = 1; away.AwayTeamId = 352;
        var other = Row(3); other.HomeTeamId = 2;
        var missing = Row(7); missing.HomeScore = null;
        db.AddRange(Row(1), away, other, Row(4, 2025), Row(5, status: "STATUS_POSTPONED"), Row(6, source: 2), missing);
        await db.SaveChangesAsync();
        var page = await ChampionshipResultsQuery.GetAsync(db, false, 999);
        Assert.Equal(2026, page.Season); Assert.Equal(3, page.Total); Assert.Equal(0, page.PageIndex);
        Assert.Equal(new[] { 3, 2, 1 }, page.Items.Select(r => r.GameId));
        Assert.Equal(new[] { 2, 1 }, (await ChampionshipResultsQuery.GetAsync(db, true)).Items.Select(r => r.GameId));
        await db.Database.ExecuteSqlRawAsync("UPDATE dbo.EnglishFeedFixture SET LastError='Private provider error' WHERE ID=1");
        Assert.Equal(3, (await ChampionshipResultsQuery.GetAsync(db, false)).Total);
        db.Add(Row(8, 2027)); await db.SaveChangesAsync();
        Assert.Equal(8, Assert.Single((await ChampionshipResultsQuery.GetAsync(db, false)).Items).GameId);
        await db.Database.ExecuteSqlRawAsync("UPDATE dbo.EnglishFeedFixture SET Enabled=0 WHERE ID=1");
        Assert.Null((await ChampionshipResultsQuery.GetAsync(db, false)).Source);
    }
}
