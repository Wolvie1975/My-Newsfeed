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

public class MlsResultsTests
{
    [Theory]
    [InlineData("STATUS_FINAL_PEN", 1, 1, 2, 4, "1–1 (2–4 on penalties)", "Away win")]
    [InlineData("STATUS_FINAL_PEN", 0, 0, 5, 3, "0–0 (5–3 on penalties)", "Home win")]
    [InlineData("STATUS_FINAL_AET", 2, 1, null, null, "2–1", "Home win")]
    [InlineData("STATUS_FULL_TIME", 0, 0, null, null, "0–0", "Draw")]
    public void Match_goals_remain_separate_from_penalties_and_winner_uses_shootout(string status, int home, int away, int? homePen, int? awayPen, string score, string winner)
    {
        var game = new SportsMatchResult { HomeTeamName = "Home", AwayTeamName = "Away", HomeScore = home, AwayScore = away,
            HomeShootoutScore = homePen, AwayShootoutScore = awayPen, ScheduleStatus = status };
        Assert.Equal(score, MlsResultsQuery.Score(game)); Assert.Equal(winner, MlsResultsQuery.Winner(game));
    }
    [Fact]
    public async Task Penalty_row_renders_tallies_winner_safe_links_and_encoded_names()
    {
        using var services = new ServiceCollection().AddLogging().AddSingleton(FeedDates.Create("America/Chicago")).BuildServiceProvider();
        await using var renderer = new HtmlRenderer(services, services.GetRequiredService<ILoggerFactory>());
        var game = new SportsMatchResult { HomeTeamName = "Home <club>", AwayTeamName = "Away", HomeScore = 1, AwayScore = 1,
            HomeShootoutScore = 2, AwayShootoutScore = 4, ScheduleStatus = "STATUS_FINAL_PEN", SourceUrl = "javascript:alert(1)",
            StartsAtUtc = new DateTime(2026,10,14,1,0,0) };
        var html = await renderer.Dispatcher.InvokeAsync(async () => (await renderer.RenderComponentAsync<MlsResultRow>(ParameterView.FromDictionary(
            new Dictionary<string,object?> { ["Game"] = game, ["Dates"] = FeedDates.Create("America/Chicago") }))).ToHtmlString());
        Assert.Contains("Home &lt;club&gt;", html); Assert.Contains("on penalties", html); Assert.Contains("Away win", html);
        Assert.DoesNotContain("Draw", html); Assert.DoesNotContain("javascript:", html);
    }
    private sealed class FixtureContext(DbContextOptions<WebScraperContext> options) : WebScraperContext(options)
    {
        protected override void OnModelCreating(ModelBuilder model)
        {
            base.OnModelCreating(model); model.Entity<SportsEventsType>().ToTable("MlsFeedFixture"); model.Entity<SportsMatchResult>().ToTable("MlsResultFixture");
        }
    }
    [Fact]
    public async Task Results_scope_provider_season_valid_finals_and_page_without_losing_last_good_data()
    {
        var config = new ConfigurationBuilder().AddJsonFile(Path.GetFullPath("../../../../../src/myNewspaper.Web/appsettings.json", AppContext.BaseDirectory))
            .AddUserSecrets("c579816a-020e-445a-92f6-efdd7002106c").AddEnvironmentVariables().Build();
        await using var db = new FixtureContext(new DbContextOptionsBuilder<WebScraperContext>().UseSqlServer(config.GetConnectionString("DefaultConnection")).Options);
        await using var transaction = await db.Database.BeginTransactionAsync();
        await db.Database.ExecuteSqlRawAsync("""
            CREATE TABLE dbo.MlsFeedFixture (ID int PRIMARY KEY, RssUrl nvarchar(2048) NOT NULL, EventsTypeName nvarchar(200) NOT NULL,
             SchoolName nvarchar(100) NULL, DateAdded datetime2 NOT NULL, ProviderKey varchar(50) NULL, ProviderSchoolId int NULL, Enabled bit NOT NULL,
             LastAttemptAt datetime2 NULL, LastSuccessAt datetime2 NULL, LastError nvarchar(1000) NULL, LastEventCount int NULL, LastIssueCount int NULL);
            CREATE TABLE dbo.MlsResultFixture (ID bigint PRIMARY KEY, SourceId int NOT NULL, ProviderKey nvarchar(50) NOT NULL,
             GameId int NOT NULL, Season int NOT NULL, MatchDate date NOT NULL, StartsAtUtc datetime2 NOT NULL,
             HomeTeamId int NOT NULL, AwayTeamId int NOT NULL, HomeTeamName nvarchar(200) NOT NULL, AwayTeamName nvarchar(200) NOT NULL,
             HomeScore int NULL, AwayScore int NULL, HomeShootoutScore int NULL, AwayShootoutScore int NULL,
             HomeLogoUrl nvarchar(2048) NULL, AwayLogoUrl nvarchar(2048) NULL,
             ScheduleStatus nvarchar(100) NOT NULL, SourceUrl nvarchar(2048) NULL, FirstSeenAt datetime2 NOT NULL, LastSeenAt datetime2 NOT NULL);
            INSERT dbo.MlsFeedFixture (ID,RssUrl,EventsTypeName,DateAdded,ProviderKey,Enabled)
             VALUES (1,'https://test.invalid/mls','MLS',GETUTCDATE(),'espn-mls',1),
                    (2,'https://test.invalid/disabled','Disabled',GETUTCDATE(),'espn-mls',0);
            """);
        SportsMatchResult Row(int id) => new() { Id=id,SourceId=1,ProviderKey="espn-mls",GameId=id,Season=2026,
            MatchDate=new(2026,10,1),StartsAtUtc=new DateTime(2026,10,1,12,0,0),HomeTeamId=1,AwayTeamId=2,
            HomeTeamName="Home",AwayTeamName="Away",HomeScore=2,AwayScore=1,ScheduleStatus="STATUS_FINAL",FirstSeenAt=DateTime.UtcNow,LastSeenAt=DateTime.UtcNow };
        db.AddRange(Enumerable.Range(1,30).Select(Row));
        var old=Row(31);old.Season=2025;
        var disabled=Row(32);disabled.SourceId=2;
        var wrong=Row(33);wrong.ProviderKey="espn-championship";
        var live=Row(34);live.ScheduleStatus="STATUS_IN_PROGRESS";
        var missing=Row(35);missing.HomeScore=null;
        var invalid=Row(36);invalid.HomeScore=-1;
        var same=Row(37);same.AwayTeamId=1;
        var pen=Row(38);pen.HomeScore=pen.AwayScore=1;pen.ScheduleStatus="STATUS_FINAL_PEN";pen.HomeShootoutScore=2;pen.AwayShootoutScore=4;
        var noPen=Row(39);noPen.ScheduleStatus="STATUS_FINAL_PEN";
        var tiedPen=Row(40);tiedPen.ScheduleStatus="STATUS_FINAL_PEN";tiedPen.HomeShootoutScore=tiedPen.AwayShootoutScore=3;
        db.AddRange(old,disabled,wrong,live,missing,invalid,same,pen,noPen,tiedPen);await db.SaveChangesAsync();
        var first=await MlsResultsQuery.GetAsync(db);var last=await MlsResultsQuery.GetAsync(db,999);
        Assert.Equal(2026,first.Season);Assert.Equal(31,first.Total);Assert.Equal(25,first.Items.Count);Assert.Equal(1,last.PageIndex);
        Assert.Equal(6,last.Items.Count);Assert.Equal(31,first.Items.Concat(last.Items).Select(r=>r.Id).Distinct().Count());
        Assert.Equal(38,first.Items[0].Id);
        await db.Database.ExecuteSqlRawAsync("UPDATE dbo.MlsFeedFixture SET LastError='Private provider diagnostic' WHERE ID=1");
        Assert.Equal(31,(await MlsResultsQuery.GetAsync(db)).Total);
        var newer=Row(41);newer.Season=2027;db.Add(newer);await db.SaveChangesAsync();
        Assert.Equal(41,Assert.Single((await MlsResultsQuery.GetAsync(db)).Items).Id);
        await db.Database.ExecuteSqlRawAsync("UPDATE dbo.MlsFeedFixture SET Enabled=0 WHERE ID=1");
        Assert.Null((await MlsResultsQuery.GetAsync(db)).Source);
    }
}
