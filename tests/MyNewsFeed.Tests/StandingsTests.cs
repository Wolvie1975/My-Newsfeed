using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using MyNewsFeed.Web.Components.Standings;
using MyNewsFeed.Web.Data;
using MyNewsFeed.Web.Data.Models;

namespace MyNewsFeed.Tests;

public class StandingsTests
{
    private static readonly ILogger Logger = NullLogger.Instance;
    private static SportsStanding Row(long id, string group = "east", int position = 1, string? stats = null) => new()
    {
        Id = id, SourceId = 1, IsCurrent = true, GroupId = group, GroupName = group,
        Position = position, TeamId = id.ToString(), TeamName = $"Team {id}", Season = "2025-26",
        Title = "2025-26 Big 12", StatsJson = stats ?? "[]"
    };

    [Theory]
    [InlineData("abc", null)] [InlineData("-1", null)] [InlineData("0", null)]
    [InlineData("7", 7)] [InlineData("2147483648", null)]
    public void Source_ids_are_parsed_without_throwing(string raw, int? expected) => Assert.Equal(expected, StandingsQuery.ParseSourceId(raw));

    [Fact]
    public void Selection_uses_enabled_sources_and_keeps_empty_sources_selectable()
    {
        var sources = new[] { new SportsStandingsSource { Id = 1, DisplayName = "WNBA", Enabled = true },
            new SportsStandingsSource { Id = 2, DisplayName = "MLB", Enabled = true },
            new SportsStandingsSource { Id = 3, DisplayName = "AAA", Enabled = false } };
        Assert.Equal(2, StandingsQuery.SelectSource(sources, null)!.Id);
        Assert.Equal(2, StandingsQuery.SelectSource(sources, 3)!.Id);
        Assert.Equal(2, StandingsQuery.SelectSource(sources, 999)!.Id);
        Assert.Equal(1, StandingsQuery.SelectSource(sources, 1)!.Id);
        Assert.Null(StandingsQuery.SelectSource([], null));
    }

    [Theory]
    [InlineData(null)] [InlineData("not json")] [InlineData("{}")] [InlineData("[null]")]
    public void Invalid_statistics_are_safe(string? json) => Assert.Empty(StandingsQuery.ParseStatistics(json, 3000000000L, Logger));

    [Fact]
    public void Statistic_display_values_and_unknown_metadata_are_preserved()
    {
        var stats = StandingsQuery.ParseStatistics("""
            [{"name":"winPercent","displayName":"Pct","displayValue":".625","value":0.625,"extra":true},
             {"name":"pointDifferential","displayName":"GD","displayValue":"+12"},
             {"name":"streak","displayName":"Streak","displayValue":"W3"}]
            """, 1, Logger);
        Assert.Equal(".625", stats[0].DisplayValue); Assert.Equal(0.625, stats[0].Value);
        Assert.Equal("+12", stats[1].DisplayValue);
        Assert.Equal("—", StandingsQuery.StatisticValue(new(Row(1), stats), "points"));
    }

    [Fact]
    public void Professional_columns_are_sport_specific()
    {
        Assert.Equal(new[] { "wins", "losses", "winPercent", "gamesBehind", "streak" }, StandingsQuery.Columns("espn-mlb", []).Select(c => c.Name));
        Assert.Equal(new[] { "gamesPlayed", "wins", "ties", "losses", "pointsFor", "pointsAgainst", "pointDifferential", "points" }, StandingsQuery.Columns("espn-nwsl", []).Select(c => c.Name));
    }

    [Fact]
    public async Task Nfl_tables_keep_conference_order_and_display_values_without_extra_columns_or_rank()
    {
        var rows = Enumerable.Range(1, 32).Select(id =>
        {
            var row = Row(id, id <= 16 ? "AFC" : "NFC", (id - 1) % 16 + 1, """
                [{"name":"wins","displayValue":"4"},{"name":"losses","displayValue":"2"},
                 {"name":"winPercent","displayValue":".667","value":0.666666},
                 {"name":"pointsFor","displayValue":"150"},{"name":"pointsAgainst","displayValue":"138"},
                 {"name":"pointDifferential","displayValue":"+12"},{"name":"streak","displayValue":"W3"},
                 {"name":"Home","displayValue":"2-1"},{"name":"Ties","displayValue":"99"}]
                """);
            row.GroupName = id <= 16 ? "American Football Conference" : "National Football Conference";
            row.Season = "2026"; row.Title = "2026 NFL"; row.Rank = id;
            return row;
        }).Reverse().ToList();
        var groups = StandingsQuery.BuildGroups(rows, "espn-nfl", Logger);
        Assert.Equal(2, groups.Count);
        Assert.All(groups, group =>
        {
            Assert.Equal(16, group.Teams.Count);
            Assert.Equal(Enumerable.Range(1, 16), group.Teams.Select(t => t.Row.Position));
            Assert.Equal(new[] { "W", "L", "T", "Pct", "PF", "PA", "Diff", "Streak" }, group.Columns.Select(c => c.Label));
        });
        Assert.Equal("—", StandingsQuery.StatisticValue(groups[0].Teams[0], "ties"));
        using var services = new ServiceCollection().AddLogging().BuildServiceProvider();
        await using var renderer = new HtmlRenderer(services, services.GetRequiredService<ILoggerFactory>());
        var html = System.Net.WebUtility.HtmlDecode(await renderer.Dispatcher.InvokeAsync(async () =>
            (await renderer.RenderComponentAsync<StandingsTable>(ParameterView.FromDictionary(new Dictionary<string, object?>
            { ["Group"] = groups[0], ["Provider"] = "espn-nfl", ["SourceName"] = "NFL", ["HeadingId"] = "afc" }))).ToHtmlString()));
        Assert.Contains("2026 NFL · American Football Conference", html);
        Assert.Contains("<td>.667</td>", html); Assert.Contains(">+12</td>", html);
        Assert.Contains(">W3</td>", html); Assert.Contains("<td>—</td>", html);
        Assert.DoesNotContain(">Rank</th>", html); Assert.DoesNotContain(">Home</th>", html);
        Assert.DoesNotContain("99</td>", html);
    }

    [Fact]
    public void Big12_duplicate_headers_keep_their_positional_keys_and_provider_order()
    {
        var json = """[{"name":"column1","displayName":"Home","displayValue":"3-1"},{"name":"column2","displayName":"Home","displayValue":"6-2"}]""";
        var rows = new[] { Row(3000000001L, position: 2, stats: json), Row(3000000002L, position: 1, stats: json), Row(3, "west", 1) };
        var groups = StandingsQuery.BuildGroups(rows, "big12", Logger);
        Assert.Equal(2, groups.Count); Assert.Equal(3000000002L, groups[0].Teams[0].Row.Id);
        Assert.Equal(new[] { "Home", "Home" }, groups[0].Columns.Select(c => c.Label));
        Assert.Equal("6-2", StandingsQuery.StatisticValue(groups[0].Teams[0], "column2"));
        Assert.Equal("2025-26", groups[0].Teams[0].Row.Season);
        var old = Row(5); old.IsCurrent = false;
        Assert.Empty(StandingsQuery.BuildGroups([old], "big12", Logger));
    }

    [Fact]
    public void Freshness_retains_failed_or_stale_data_without_exposing_raw_errors()
    {
        var now = new DateTime(2026, 10, 11, 12, 0, 0, DateTimeKind.Utc);
        var source = new SportsStandingsSource { LastSuccessAt = now.AddHours(-1) };
        Assert.Null(StandingsQuery.Freshness(source, now));
        source.LastSuccessAt = now.AddHours(-3);
        Assert.Equal("Standings may be out of date.", StandingsQuery.Freshness(source, now));
        source.LastError = "private provider error";
        Assert.Equal("Latest refresh failed; showing last available standings.", StandingsQuery.Freshness(source, now));
    }

    [Fact]
    public async Task Tables_encode_values_reject_unsafe_urls_and_keep_full_nwsl_visible()
    {
        var current = Row(1); current.TeamId = "20907"; current.TeamName = "Kansas City Current"; current.Rank = 3;
        current.LogoUrl = "javascript:alert(1)"; current.TeamUrl = "javascript:alert(2)"; current.Note = "<script>bad</script>";
        var other = Row(2); other.TeamName = "Other club";
        var group = StandingsQuery.BuildGroups([current, other], "espn-nwsl", Logger)[0];
        using var services = new ServiceCollection().AddLogging().BuildServiceProvider();
        await using var renderer = new HtmlRenderer(services, services.GetRequiredService<ILoggerFactory>());
        var html = await renderer.Dispatcher.InvokeAsync(async () =>
            (await renderer.RenderComponentAsync<StandingsTable>(ParameterView.FromDictionary(new Dictionary<string, object?>
            { ["Group"] = group, ["Provider"] = "espn-nwsl", ["SourceName"] = "NWSL", ["HeadingId"] = "group-1" }))).ToHtmlString());
        Assert.Contains("Other club", html); Assert.Contains("standing-highlight", html);
        Assert.DoesNotContain("javascript:", html); Assert.DoesNotContain("<script>", html);
        Assert.Contains("&lt;script&gt;", html); Assert.Contains("scope=\"row\"", html); Assert.Contains("<caption>", html);
        Assert.Contains("Rank", html);
    }

    // Unique, rollback-only fixture tables: no writes to the scraper-owned standings tables.
    private sealed class FixtureContext(DbContextOptions<WebScraperContext> options) : WebScraperContext(options)
    {
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            modelBuilder.Entity<SportsStandingsSource>().ToTable("StandingsSourceFixture");
            modelBuilder.Entity<SportsStanding>().ToTable("StandingsRowFixture");
        }
    }

    [Fact]
    public async Task Query_filters_disabled_historical_and_unstarted_college_sources_but_retains_empty_professional_choices()
    {
        var config = new ConfigurationBuilder().AddJsonFile(Path.GetFullPath("../../../../../src/MyNewsFeed.Web/appsettings.json", AppContext.BaseDirectory))
            .AddUserSecrets("c579816a-020e-445a-92f6-efdd7002106c").AddEnvironmentVariables().Build();
        await using var db = new FixtureContext(new DbContextOptionsBuilder<WebScraperContext>().UseSqlServer(config.GetConnectionString("DefaultConnection")).Options);
        await using var transaction = await db.Database.BeginTransactionAsync();
        await db.Database.ExecuteSqlRawAsync("""
            CREATE TABLE dbo.StandingsSourceFixture (ID int NOT NULL PRIMARY KEY, ProviderKey nvarchar(100) NOT NULL,
             Sport nvarchar(100) NOT NULL, DisplayName nvarchar(200) NOT NULL, Enabled bit NOT NULL, LastAttemptAt datetime2 NULL,
             LastSuccessAt datetime2 NULL, LastError nvarchar(max) NULL, LatestSeason nvarchar(100) NULL, LastTeamCount int NULL);
            CREATE TABLE dbo.StandingsRowFixture (ID bigint NOT NULL PRIMARY KEY, SourceId int NOT NULL, Season nvarchar(100) NOT NULL,
             SeasonType nvarchar(100) NULL, Title nvarchar(500) NULL, SourceUrl nvarchar(2048) NULL, GroupId nvarchar(100) NOT NULL,
             GroupName nvarchar(200) NULL, TeamId nvarchar(100) NOT NULL, TeamName nvarchar(200) NOT NULL, LogoUrl nvarchar(2048) NULL,
             TeamUrl nvarchar(2048) NULL, Position int NOT NULL, Rank int NULL, StatsJson nvarchar(max) NULL, Note nvarchar(max) NULL,
             FetchedAt datetime2 NULL, IsCurrent bit NOT NULL);
            """);
        await db.Database.ExecuteSqlRawAsync("""
            INSERT dbo.StandingsSourceFixture (ID,ProviderKey,Sport,DisplayName,Enabled)
            VALUES (1,'espn-mlb','baseball','MLB',1),(2,'espn-wnba','basketball','WNBA',1),(3,'espn-nwsl','soccer','NWSL',1),
             (4,'big12','football','Big 12 Football',1),(5,'big12','basketball','Big 12 Men',1),(6,'big12','basketball','Big 12 Women',1),
             (7,'big12','soccer','Big 12 Soccer',1),(8,'big12','volleyball','Big 12 Volleyball',1),(9,'espn-mlb','baseball','Disabled',0);
            INSERT dbo.StandingsRowFixture (ID,SourceId,Season,GroupId,TeamId,TeamName,Position,IsCurrent,StatsJson)
            VALUES (3000000001,1,'2026','AL','a','First',1,1,'[]'),(3000000002,1,'2026','NL','b','Second',1,1,'[]'),
             (3000000003,1,'2025','AL','a','Old season',1,0,'[]'),(3000000004,9,'2026','AL','c','Hidden',1,1,'[]');
            """);
        var page = await StandingsQuery.GetAsync(db, 1, Logger, new DateOnly(2026, 10, 11));
        Assert.Equal(3, page.Sources.Count); Assert.Equal(2, page.Groups.Count);
        Assert.Equal(2, page.Groups.Sum(g => g.Teams.Count));
        Assert.DoesNotContain(page.Groups.SelectMany(g => g.Teams), t => t.Row.TeamName is "Hidden" or "Old season");
        var empty = await StandingsQuery.GetAsync(db, 2, Logger);
        Assert.Equal(2, empty.Selected!.Id); Assert.Empty(empty.Groups);
        var disabled = await StandingsQuery.GetAsync(db, 9, Logger);
        Assert.Equal(1, disabled.Selected!.Id); Assert.Equal(2, disabled.Groups.Count);
        foreach (var hiddenId in new[] { 1, 2 })
        {
            var playoffs = await StandingsQuery.GetAsync(db, hiddenId, Logger, new DateOnly(2026, 10, 11), ["espn-mlb", "espn-wnba"]);
            Assert.Equal(3, playoffs.Selected!.Id);
            Assert.DoesNotContain(playoffs.Sources, source => source.Id is 1 or 2);
            Assert.Empty(playoffs.Groups);
        }
    }
    [Fact]
    public void Mls_sorts_points_descending_per_conference_with_provider_order_for_ties()
    {
        var low = Row(1, "east", 1, """[{"name":"points","displayValue":"20","value":20}]""");
        var high = Row(2, "east", 3, """[{"name":"points","displayValue":"50","value":50}]""");
        var tie = Row(3, "east", 2, """[{"name":"points","displayValue":"50"}]""");
        var unknown = Row(4, "east", 0);
        var west = Row(5, "west", 1, """[{"name":"points","displayValue":"60","value":60}]""");
        var groups = StandingsQuery.BuildGroups([low, high, tie, unknown, west], "espn-mls", Logger);
        Assert.Equal(2, groups.Count);
        Assert.Equal(new long[] { 3, 2, 1, 4 }, groups[0].Teams.Select(t => t.Row.Id));
        Assert.Equal(5, Assert.Single(groups[1].Teams).Row.Id);
        Assert.Equal(new long[] { 4, 1, 3, 2 }, StandingsQuery.BuildGroups([low, high, tie, unknown], "espn-nwsl", Logger)[0].Teams.Select(t => t.Row.Id));
    }

}
