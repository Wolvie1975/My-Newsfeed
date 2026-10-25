using System.ComponentModel.DataAnnotations.Schema;
using System.Reflection;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using myNewspaper.Web.Components.F1;
using myNewspaper.Web.Data;
using myNewspaper.Web.Data.Models;

namespace myNewspaper.Tests;

public class F1Tests
{
    [Theory]
    [InlineData("04:00:00Z", "2026-10-18T04:00:00Z")]
    [InlineData(null, null)]
    [InlineData("", null)]
    [InlineData("garbage", null)]
    public void Race_time_requires_a_confirmed_utc_clock(string? time, string? expected)
    {
        var utc = F1Query.StartsUtc(new DateOnly(2026, 10, 18), time);
        Assert.Equal(expected, utc?.ToString("yyyy-MM-dd'T'HH:mm:ss'Z'"));
        if (utc != null) Assert.Equal(DateTimeKind.Utc, utc.Value.Kind);
    }

    [Fact]
    public void Sprint_weekends_only_show_available_sessions_without_qualifying_duplicates()
    {
        var race = new F1Race
        {
            RaceDate = new(2026, 10, 18),
            RaceTimeUtc = "04:00:00Z",
            FirstPractice = """{"date":"2026-10-16","time":"10:00:00Z"}""",
            SprintQualifying = """{"date":"2026-10-16","time":"14:00:00Z"}""",
            SprintShootout = """{"date":"2026-10-16","time":"14:00:00Z"}""",
            Sprint = """{"date":"2026-10-17"}""",
            ThirdPractice = "not-json"
        };
        var sessions = F1Query.Sessions(race);
        Assert.Equal(new[] { "Practice 1", "Sprint qualifying", "Sprint", "Grand Prix" }, sessions.Select(s => s.Name));
        Assert.Null(sessions[2].StartsUtc);
        var dates = FeedDates.Create("America/Chicago");
        Assert.Equal(new DateOnly(2026, 10, 17), F1Query.LocalDate(sessions[^1], dates));
        Assert.Equal(new DateOnly(2026, 10, 17), F1Query.LocalDate(sessions[2], dates));
        Assert.True(F1Query.Upcoming(race, dates, new(2026, 10, 17)));
        Assert.False(F1Query.Upcoming(race, dates, new(2026, 10, 19)));
    }

    [Fact]
    public void Classification_selection_never_combines_race_and_sprint_or_accepts_unknown_values()
    {
        F1ResultChoice[] choices = [new(1, "Race"), new(2, "Sprint"), new(2, "Race")];
        Assert.Equal(new(2, "Race"), F1Query.Select(choices, null));
        Assert.Equal(new(2, "Sprint"), F1Query.Select(choices, "2:Sprint"));
        Assert.Equal(new(2, "Race"), F1Query.Select(choices, "-100:Unknown"));
        Assert.Null(F1Query.Select([], null));
    }

    [Fact]
    public void Provider_points_positions_and_multiple_constructors_are_preserved()
    {
        Assert.Equal("12.5", F1Query.Points(12.500m)); Assert.Equal("0.125", F1Query.Points(0.125m));
        Assert.Equal("R", F1Query.Position(20, "R"));
        Assert.Equal("Pit lane / unavailable", F1Query.Grid(0));
        Assert.Equal("Team A, Team B", F1Query.Constructors("""[{"name":"Team A"},{"name":"Team B"},{"name":"Team A"}]"""));
        Assert.Equal("—", F1Query.Constructors("not-json"));
    }

    [Fact]
    public async Task Date_only_rendering_does_not_shift_the_day_and_confirmed_time_uses_display_zone()
    {
        using var services = new ServiceCollection().AddLogging().AddSingleton(FeedDates.Create("America/Chicago")).BuildServiceProvider();
        await using var renderer = new HtmlRenderer(services, services.GetRequiredService<ILoggerFactory>());
        async Task<string> Render(F1Session session) => await renderer.Dispatcher.InvokeAsync(async () =>
            (await renderer.RenderComponentAsync<SessionTime>(ParameterView.FromDictionary(new Dictionary<string, object?> { ["Session"] = session }))).ToHtmlString());
        var dateOnly = await Render(new("Sprint", new(2026, 10, 18), null));
        Assert.Contains("October 18, 2026", dateOnly); Assert.Contains("Time TBD", dateOnly);
        var timed = await Render(new("Race", new(2026, 10, 18), new DateTime(2026, 10, 18, 4, 0, 0, DateTimeKind.Utc)));
        Assert.Contains("Oct 17, 2026", timed); Assert.Contains("11:00 PM", timed);
    }

    [Fact]
    public void Refresh_failure_never_exposes_operational_errors()
    {
        var page = F1Page.Empty with { Source = new F1Source { Enabled = true, LastError = "private operational detail" } };
        Assert.DoesNotContain("private", F1Query.Freshness(page, DateTime.UtcNow));
        Assert.Contains("failed", F1Query.Freshness(page, DateTime.UtcNow));
    }

    private static readonly Type[] Types = [typeof(F1Source), typeof(F1Race), typeof(F1Classification), typeof(F1DriverStanding), typeof(F1ConstructorStanding)];
    private sealed class FixtureContext(DbContextOptions<WebScraperContext> options) : WebScraperContext(options)
    {
        protected override void OnModelCreating(ModelBuilder model)
        {
            base.OnModelCreating(model);
            foreach (var type in Types) model.Entity(type).HasNoKey().ToView("#F1Test_" + type.Name);
        }
    }
    private static async Task<FixtureContext> Fixture()
    {
        var db = new FixtureContext(new DbContextOptionsBuilder<WebScraperContext>().UseSqlServer(TestDatabase.ConnectionString).Options);
        await db.Database.OpenConnectionAsync();
        foreach (var type in Types)
        {
            var columns = type.GetProperties().Select(p =>
            {
                var t = Nullable.GetUnderlyingType(p.PropertyType) ?? p.PropertyType;
                var sql = t == typeof(int) ? "int" : t == typeof(long) ? "bigint" : t == typeof(bool) ? "bit"
                    : t == typeof(DateOnly) ? "date" : t == typeof(DateTime) ? "datetime2" : t == typeof(decimal) ? "decimal(12,3)" : "nvarchar(max)";
                return $"[{p.GetCustomAttribute<ColumnAttribute>()?.Name ?? p.Name}] {sql} NULL";
            });
            // Identifiers/types come only from the fixed model types above; values are parameterized below.
#pragma warning disable EF1002
            await db.Database.ExecuteSqlRawAsync($"CREATE TABLE [#F1Test_{type.Name}] ({string.Join(',', columns)})");
#pragma warning restore EF1002
        }
        return db;
    }
    private static async Task Insert(WebScraperContext db, object row)
    {
        await using var cmd = db.Database.GetDbConnection().CreateCommand();
        var props = row.GetType().GetProperties();
        cmd.CommandText = $"INSERT INTO [#F1Test_{row.GetType().Name}] ({string.Join(',', props.Select(p => "[" + (p.GetCustomAttribute<ColumnAttribute>()?.Name ?? p.Name) + "]"))}) VALUES ({string.Join(',', props.Select((_, i) => "@p" + i))})";
        for (var i = 0; i < props.Length; i++)
        {
            var p = cmd.CreateParameter(); p.ParameterName = "@p" + i;
            var value = props[i].GetValue(row); p.Value = value is DateOnly date ? date.ToDateTime(TimeOnly.MinValue) : value ?? DBNull.Value;
            cmd.Parameters.Add(p);
        }
        await cmd.ExecuteNonQueryAsync();
    }

    [Fact]
    public async Task Sql_views_keep_sessions_separate_decimal_points_rankings_and_snapshot_health()
    {
        await using var db = await Fixture();
        var fetched = new DateTime(2026, 10, 18, 5, 0, 0);
        await Insert(db, new F1Source { Id = 1, Enabled = true, LastError = "collection failed", LastSuccessAt = fetched });
        await Insert(db, new F1Race { Season = 2026, Round = 1, RaceName = "First GP", RaceDate = new(2026, 10, 1), FetchedAt = fetched });
        await Insert(db, new F1Race { Season = 2026, Round = 2, RaceName = "Next GP", RaceDate = new(2026, 10, 25), FetchedAt = fetched });
        await Insert(db, new F1Classification { Season = 2026, Round = 1, SessionType = "Race", Position = 2, PositionText = "R", DriverId = "b", Points = 0.5m, Status = "Retired", FetchedAt = fetched });
        await Insert(db, new F1Classification { Season = 2026, Round = 1, SessionType = "Race", Position = 1, DriverId = "a", Points = 12.5m, TimeDisplay = "+5.123", FetchedAt = fetched });
        await Insert(db, new F1Classification { Season = 2026, Round = 1, SessionType = "Sprint", Position = 1, DriverId = "b", Points = 8m, FetchedAt = fetched });
        await Insert(db, new F1DriverStanding { Season = 2026, StandingsRound = 1, DriverId = "a", Position = 1, Points = 12.5m, FetchedAt = fetched });
        await Insert(db, new F1ConstructorStanding { Season = 2026, StandingsRound = 1, ConstructorId = "x", Position = 1, Points = 20.5m, FetchedAt = fetched });
        var race = await F1Query.GetAsync(db, "results");
        Assert.Equal(2026, race.Season); Assert.Equal(2, race.Races.Count); Assert.Equal(2, race.Choices.Count);
        Assert.Equal(new[] { "a", "b" }, race.Results.Select(r => r.DriverId));
        Assert.Equal(12.5m, race.Results[0].Points); Assert.Equal("+5.123", race.Results[0].TimeDisplay);
        Assert.Equal("Retired", race.Results[1].Status); Assert.Equal(fetched, race.FetchedAt);
        Assert.Contains("failed", F1Query.Freshness(race, fetched));
        var sprint = await F1Query.GetAsync(db, "results", "1:Sprint");
        Assert.Equal(8m, Assert.Single(sprint.Results).Points);
        var standings = await F1Query.GetAsync(db, "standings");
        Assert.Equal(1, Assert.Single(standings.Drivers).StandingsRound); Assert.Equal(20.5m, Assert.Single(standings.Constructors).Points);
        await db.Database.ExecuteSqlRawAsync("UPDATE #F1Test_F1Source SET Enabled=0");
        Assert.Empty((await F1Query.GetAsync(db, "schedule")).Races);
    }

    [Fact]
    public async Task Sql_calendar_without_results_or_standings_is_a_valid_preseason_snapshot()
    {
        await using var db = await Fixture();
        await Insert(db, new F1Source { Id = 1, Enabled = true });
        await Insert(db, new F1Race { Season = 2027, Round = 1, RaceName = "Opening GP", RaceDate = new(2027, 3, 1), FetchedAt = DateTime.UtcNow });
        var data = await F1Query.GetAsync(db, "results");
        Assert.Single(data.Races); Assert.Empty(data.Results); Assert.Empty(data.Choices);
        Assert.Empty((await F1Query.GetAsync(db, "standings")).Drivers);
        await db.Database.ExecuteSqlRawAsync("DROP TABLE #F1Test_F1Race");
        await Assert.ThrowsAnyAsync<System.Data.Common.DbException>(() => F1Query.GetAsync(db, "schedule"));
    }
}
