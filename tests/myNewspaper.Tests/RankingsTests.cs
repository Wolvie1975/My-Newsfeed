using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using myNewspaper.Web.Data;
using myNewspaper.Web.Data.Models;
namespace myNewspaper.Tests;

public class RankingsTests
{
    [Theory]
    [InlineData(5, 8, false, "8", "Up 3")]
    [InlineData(8, 5, false, "5", "Down 3")]
    [InlineData(5, 5, false, "5", "Unchanged")]
    [InlineData(5, null, true, "NR", "New")]
    [InlineData(5, null, false, "—", "—")]
    public void Movement_distinguishes_known_new_and_unknown(int rank, int? previous, bool unranked, string label, string change)
    {
        var row = new SportsRankingTeam { Rank = rank, PreviousRank = previous, PreviouslyUnranked = unranked };
        Assert.Equal(label, RankingsQuery.Previous(row)); Assert.Equal(change, RankingsQuery.Movement(row));
    }
    [Fact]
    public void Old_poll_age_is_separate_from_recent_collection_and_errors_are_not_public()
    {
        var now = new DateTime(2026, 10, 11, 12, 0, 0);
        Assert.True(RankingsQuery.OldPoll(new SportsRankingPoll { PublishedAtUtc = new DateTime(2026, 4, 1) }, now));
        Assert.Null(RankingsQuery.Freshness(new SportsRankingSource { LastSuccessAt = now }, now));
        Assert.Equal("Latest refresh failed; showing last available rankings.", RankingsQuery.Freshness(new SportsRankingSource { LastError = "private details" }, now));
    }
    [Fact]
    public void Disabled_ids_fall_back_to_first_enabled_source()
    {
        var first = new SportsRankingSource { Id = 1, DisplayName = "Football", Enabled = true };
        var disabled = new SportsRankingSource { Id = 2, DisplayName = "Basketball", Enabled = false };
        Assert.Same(first, RankingsQuery.SelectSource([disabled, first], 2));
        Assert.Null(RankingsQuery.SelectSource([], null));
    }
    [Fact]
    public void Mls_uses_soccer_columns_without_calculating_a_league_rank() =>
        Assert.Equal(new[] { "gamesPlayed", "wins", "ties", "losses", "pointsFor", "pointsAgainst", "pointDifferential", "points" }, StandingsQuery.Columns("espn-mls", []).Select(c => c.Name));

    private sealed class FixtureContext(DbContextOptions<WebScraperContext> options) : WebScraperContext(options)
    {
        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);
            builder.Entity<SportsRankingSource>().ToTable("RankingSourceFixture");
            builder.Entity<SportsRankingPoll>().ToTable("RankingPollFixture");
            builder.Entity<SportsRankingTeam>().ToTable("RankingTeamFixture");
        }
    }
    [Fact]
    public async Task Query_keeps_ties_and_filters_old_polls_correction_rows_and_disabled_sources()
    {
        await using var db = new FixtureContext(new DbContextOptionsBuilder<WebScraperContext>().UseSqlServer(TestDatabase.ConnectionString).Options);
        await using var transaction = await db.Database.BeginTransactionAsync();
        await db.Database.ExecuteSqlRawAsync("""
        CREATE TABLE dbo.RankingSourceFixture(ID int PRIMARY KEY,ProviderKey nvarchar(100),Sport nvarchar(100),LeagueId nvarchar(50),PollId nvarchar(50),DisplayName nvarchar(200),Url nvarchar(2048),Enabled bit,LastAttemptAt datetime2 NULL,LastSuccessAt datetime2 NULL,LastError nvarchar(max) NULL);
        CREATE TABLE dbo.RankingPollFixture(ID bigint PRIMARY KEY,SourceId int,PollId nvarchar(50),PollName nvarchar(200),Season int,SeasonLabel nvarchar(100),PublishedAtUtc datetime2,ProviderUpdatedAtUtc datetime2 NULL,Period nvarchar(100) NULL,SeasonPhase nvarchar(100) NULL,SourceUrl nvarchar(2048),IsCurrent bit,FetchedAt datetime2);
        CREATE TABLE dbo.RankingTeamFixture(ID bigint PRIMARY KEY,PollRowId bigint,TeamId nvarchar(100),TeamName nvarchar(200),Rank int NULL,PreviousRank int NULL,PreviouslyUnranked bit,Points float NULL,FirstPlaceVotes int NULL,Record nvarchar(100) NULL,LogoUrl nvarchar(2048) NULL,TeamUrl nvarchar(2048) NULL,Big12SchoolId int NULL,IsBig12 bit NULL,IsCurrent bit);
        INSERT dbo.RankingSourceFixture(ID,ProviderKey,Sport,LeagueId,PollId,DisplayName,Url,Enabled) VALUES
        (1,'espn-rankings','Football','23','1','Football','https://test.invalid',1),(2,'espn-rankings','Soccer','41','1','Men','https://test.invalid',1),(3,'espn-rankings','Football','23','1','Disabled','https://test.invalid',0);
        INSERT dbo.RankingPollFixture(ID,SourceId,PollId,PollName,Season,SeasonLabel,PublishedAtUtc,SourceUrl,IsCurrent,FetchedAt) VALUES
        (3000000001,1,'1','AP Top 25',2026,'2026','2026-10-04','https://test.invalid',1,'2026-10-11'),(3000000002,1,'1','Old',2025,'2025','2025-10-04','https://test.invalid',0,'2026-10-11');
        WITH n AS (SELECT 1 AS id UNION ALL SELECT id+1 FROM n WHERE id<26)
        INSERT dbo.RankingTeamFixture(ID,PollRowId,TeamId,TeamName,Rank,PreviouslyUnranked,IsBig12,IsCurrent)
        SELECT 4000000000+CAST(id AS bigint),3000000001,CONVERT(nvarchar(50),id),'Team '+CONVERT(nvarchar(50),id),CASE WHEN id=26 THEN 25 ELSE id END,0,0,1 FROM n;
        INSERT dbo.RankingTeamFixture(ID,PollRowId,TeamId,TeamName,Rank,PreviouslyUnranked,IsBig12,IsCurrent) VALUES
        (4000000100,3000000001,'removed','Correction removed',1,0,0,0),(4000000101,3000000002,'old','Old poll team',1,0,0,1);
        """);
        var page = await RankingsQuery.GetAsync(db, 1, NullLogger.Instance);
        var scoped = await RankingsQuery.GetAsync(db, 1, NullLogger.Instance, collegeSport: "Soccer");
        Assert.Single(scoped.Sources); Assert.Equal(2, scoped.Selected!.Id); Assert.Empty(scoped.Teams);
        Assert.Empty((await RankingsQuery.GetAsync(db, 1, NullLogger.Instance, collegeSport: "Volleyball")).Sources);
        Assert.Equal(2, page.Sources.Count); Assert.Equal(26, page.Teams.Count);
        Assert.Equal(2, page.Teams.Count(t => t.Rank == 25));
        Assert.DoesNotContain(page.Teams, t => t.TeamName.Contains("removed") || t.TeamName.Contains("Old poll"));
        Assert.Equal(3000000001L, page.Poll!.Id);
        Assert.Equal(1, (await RankingsQuery.GetAsync(db, 3, NullLogger.Instance)).Selected!.Id);
        Assert.Null((await RankingsQuery.GetAsync(db, 2, NullLogger.Instance)).Poll);
    }
    [Fact]
    public async Task Basketball_poll_stays_hidden_until_opening_day_and_current_season_publication()
    {
        await using var db = new WebScraperContext(new DbContextOptionsBuilder<WebScraperContext>().UseSqlServer(TestDatabase.ConnectionString).Options);
        await using var transaction = await db.Database.BeginTransactionAsync();
        var tag = Guid.NewGuid().ToString("N");
        var source = new SportsRankingSource
        {
            ProviderKey = "test-" + tag,
            Sport = "Men's Basketball",
            LeagueId = "41",
            PollId = "1",
            DisplayName = tag,
            Url = "https://test.invalid",
            Enabled = true
        };
        db.Add(source); await db.SaveChangesAsync();
        var day = new DateOnly(2031, 11, 2);
        var poll = new SportsRankingPoll
        {
            SourceId = source.Id,
            PollId = "1",
            PollName = "Test",
            Season = 2032,
            SeasonLabel = "2031-32",
            PublishedAtUtc = day.ToDateTime(TimeOnly.MinValue),
            SourceUrl = "https://test.invalid",
            IsCurrent = true,
            FetchedAt = DateTime.UtcNow
        };
        db.Add(poll); await db.SaveChangesAsync();
        var game = new SportsEvent
        {
            Url = "https://test.invalid/" + tag,
            Title = tag,
            ProviderKey = "big12",
            Sport = "Men's Basketball",
            EventDate = day.AddDays(1),
            TimeTbd = true
        };
        db.Add(game); await db.SaveChangesAsync();
        Assert.DoesNotContain((await RankingsQuery.GetAsync(db, source.Id, NullLogger.Instance, day)).Sources, s => s.Id == source.Id);
        game.EventDate = day; await db.SaveChangesAsync();
        Assert.Equal(source.Id, (await RankingsQuery.GetAsync(db, source.Id, NullLogger.Instance, day)).Selected!.Id);
        poll.SeasonLabel = "2030-31"; await db.SaveChangesAsync();
        Assert.DoesNotContain((await RankingsQuery.GetAsync(db, source.Id, NullLogger.Instance, day)).Sources, s => s.Id == source.Id);
    }

}
