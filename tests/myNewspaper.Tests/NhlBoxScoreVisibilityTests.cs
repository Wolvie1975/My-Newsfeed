using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using myNewspaper.Web.Components.Events;
using myNewspaper.Web.Data;
using myNewspaper.Web.Data.Models;
namespace myNewspaper.Tests;

public class NhlBoxScoreVisibilityTests
{
    private sealed class FixtureContext(DbContextOptions<WebScraperContext> options) : WebScraperContext(options)
    {
        protected override void OnModelCreating(ModelBuilder model)
        {
            base.OnModelCreating(model); model.Entity<SportsEventsType>().ToTable("NhlVisibilityFeedFixture"); model.Entity<SportsMatchResult>().ToTable("NhlVisibilityResultFixture");
        }
    }
    [Fact]
    public async Task Nhl_detail_requires_enabled_source_latest_valid_season_and_source_team()
    {
        await using var db = new FixtureContext(new DbContextOptionsBuilder<WebScraperContext>().UseSqlServer(TestDatabase.ConnectionString).Options);
        await using var transaction = await db.Database.BeginTransactionAsync();
        await db.Database.ExecuteSqlRawAsync("""
            CREATE TABLE dbo.NhlVisibilityFeedFixture (ID int PRIMARY KEY, RssUrl nvarchar(2048) NOT NULL, EventsTypeName nvarchar(200) NOT NULL,
             SchoolName nvarchar(100) NULL, DateAdded datetime2 NOT NULL, ProviderKey varchar(50) NULL, ProviderSchoolId int NULL, Enabled bit NOT NULL,
             LastAttemptAt datetime2 NULL, LastSuccessAt datetime2 NULL, LastError nvarchar(1000) NULL, LastEventCount int NULL, LastIssueCount int NULL);
            CREATE TABLE dbo.NhlVisibilityResultFixture (ID bigint PRIMARY KEY, SourceId int NOT NULL, ProviderKey nvarchar(50) NOT NULL,
             GameId int NOT NULL, Season int NOT NULL, MatchDate date NOT NULL, StartsAtUtc datetime2 NOT NULL,
             HomeTeamId int NOT NULL, AwayTeamId int NOT NULL, HomeTeamName nvarchar(200) NOT NULL, AwayTeamName nvarchar(200) NOT NULL,
             HomeScore int NULL, AwayScore int NULL, HomeShootoutScore int NULL, AwayShootoutScore int NULL,
             HomeLogoUrl nvarchar(2048) NULL, AwayLogoUrl nvarchar(2048) NULL,
             ScheduleStatus nvarchar(100) NOT NULL, SourceUrl nvarchar(2048) NULL, FirstSeenAt datetime2 NOT NULL, LastSeenAt datetime2 NOT NULL);
            INSERT dbo.NhlVisibilityFeedFixture (ID,RssUrl,EventsTypeName,DateAdded,ProviderKey,Enabled)
             VALUES (1,'https://test.invalid/mls','MLS',GETUTCDATE(),'espn-nhl',1),
                    (2,'https://test.invalid/disabled','Disabled',GETUTCDATE(),'espn-nhl',0);
            """);
        SportsMatchResult Row(int id) => new()
        {
            Id = id,
            SourceId = 1,
            ProviderKey = "espn-nhl",
            GameId = id,
            Season = 2026,
            MatchDate = new(2026, 10, 1),
            StartsAtUtc = new DateTime(2026, 10, 1, 12, 0, 0),
            HomeTeamId = 1,
            AwayTeamId = 2,
            HomeTeamName = "Home",
            AwayTeamName = "Away",
            HomeScore = 2,
            AwayScore = 1,
            ScheduleStatus = "STATUS_FINAL",
            FirstSeenAt = DateTime.UtcNow,
            LastSeenAt = DateTime.UtcNow
        };
        var valid = Row(1); var old = Row(2); old.Season = 2025;
        var disabled = Row(3); disabled.SourceId = 2;
        var wrong = Row(4); wrong.ProviderKey = "espn-mls";
        var live = Row(5); live.ScheduleStatus = "STATUS_IN_PROGRESS";
        var missing = Row(6); missing.HomeScore = null;
        var same = Row(7); same.AwayTeamId = 1;
        var overtime = Row(8); overtime.ScheduleStatus = "STATUS_FINAL_OT";
        var shootout = Row(9); shootout.ScheduleStatus = "STATUS_FINAL_SO";
        db.AddRange(valid, old, disabled, wrong, live, missing, same, overtime, shootout); await db.SaveChangesAsync();
        foreach (var id in new[] { 1, 8, 9 }) Assert.True(await NhlBoxScoreVisibility.ContainsAsync(db, id));
        foreach (var id in new[] { 2, 3, 4, 5, 6, 7, 999 }) Assert.False(await NhlBoxScoreVisibility.ContainsAsync(db, id));
        await db.Database.ExecuteSqlRawAsync("UPDATE dbo.NhlVisibilityFeedFixture SET ProviderSchoolId=99 WHERE ID=1");
        Assert.False(await NhlBoxScoreVisibility.ContainsAsync(db, 1));
        await db.Database.ExecuteSqlRawAsync("UPDATE dbo.NhlVisibilityFeedFixture SET ProviderSchoolId=1 WHERE ID=1");
        Assert.True(await NhlBoxScoreVisibility.ContainsAsync(db, 1));
        var newer = Row(10); newer.Season = 2027; db.Add(newer); await db.SaveChangesAsync();
        Assert.False(await NhlBoxScoreVisibility.ContainsAsync(db, 1));
        Assert.True(await NhlBoxScoreVisibility.ContainsAsync(db, 10));
        await db.Database.ExecuteSqlRawAsync("UPDATE dbo.NhlVisibilityFeedFixture SET Enabled=0 WHERE ID=1");
        Assert.False(await NhlBoxScoreVisibility.ContainsAsync(db, 10));
    }
}
