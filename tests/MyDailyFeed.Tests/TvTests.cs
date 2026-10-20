using System.Net;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using MyDailyFeed.Web.Data;
using MyDailyFeed.Web.Data.Models;
using MyDailyFeed.Web.Tv;

namespace MyDailyFeed.Tests;

public class TvTests
{
    private static WebScraperContext Create()
    {
        var config = new ConfigurationBuilder()
            .AddJsonFile(Path.GetFullPath("../../../../../src/MyDailyFeed.Web/appsettings.json", AppContext.BaseDirectory))
            .AddJsonFile(Path.GetFullPath("../../../../../src/MyDailyFeed.Web/appsettings.Development.json", AppContext.BaseDirectory), true)
            .AddUserSecrets("c579816a-020e-445a-92f6-efdd7002106c").AddEnvironmentVariables().Build();
        return new(new DbContextOptionsBuilder<WebScraperContext>().UseSqlServer(config.GetConnectionString("DefaultConnection")).Options);
    }

    [Fact]
    public async Task Calendar_preserves_batches_specials_date_only_and_local_time_boundaries()
    {
        await using var db = Create();
        await using var tx = await db.Database.BeginTransactionAsync();
        var id = Random.Shared.Next(1_000_000_000, 1_500_000_000);
        var today = new DateOnly(2030, 1, 10);
        var dates = FeedDates.Create("America/Chicago");
        var series = new TvSeries { TvmazeShowId = id, Title = "Test series", SourceUrl = "https://www.tvmaze.com/shows/1",
            RawMetadata = "{}", WebChannelName = "Test platform", FirstSeenAt = DateTime.UtcNow, LastSeenAt = DateTime.UtcNow };
        db.Add(series);
        await db.SaveChangesAsync();
        await TvQuery.FollowAsync(db, id, series.Title);
        TvEpisode Episode(int offset, DateOnly? date, bool special = false) => new()
        {
            TvmazeEpisodeId = id + offset, SeriesId = series.Id, SeasonNumber = 1, EpisodeNumber = special ? null : offset,
            IsSpecial = special, Airdate = date, IsDateOnly = true, RawAirstamp = date + "T12:00:00Z",
            SourceUrl = "https://www.tvmaze.com/episodes/1", RawMetadata = "{}", FirstSeenAt = DateTime.UtcNow, LastSeenAt = DateTime.UtcNow
        };
        db.AddRange(Episode(1, today), Episode(2, today), Episode(3, today, true), Episode(4, today.AddDays(7)), Episode(5, null));
        var timed = Episode(6, today.AddDays(1));
        timed.IsDateOnly = false; timed.Airtime = new TimeOnly(20, 0);
        timed.StartsAtUtc = ResultsWindow.ForDay(today, dates.Zone).EndUtc.AddMinutes(-30);
        db.Add(timed);
        await db.SaveChangesAsync();
        var calendar = await TvQuery.GetAsync(db, dates, today, id);
        Assert.Equal(4, calendar.Total);
        Assert.Equal(4, calendar.Items.Select(e => e.ProviderId).Distinct().Count());
        Assert.All(calendar.Items, e => Assert.Equal(today, e.Date));
        Assert.Equal(3, calendar.Items.Count(e => e.StartsUtc == null));
        Assert.Single(calendar.Items, e => e.Special && e.Number == null);
        db.Add(Episode(7, today.AddDays(1)));
        await db.SaveChangesAsync();
        var todays = (await TvQuery.GetOnDateAsync(db, dates, today)).Where(e => e.ShowId == id).ToList();
        Assert.Equal(4, todays.Count);
        Assert.All(todays, e => Assert.Equal(today, e.Date));
        var tracking = await db.TvTrackedSeries.SingleAsync(t => t.TvmazeShowId == id);
        tracking.LastSuccessAt = DateTime.UtcNow; tracking.CachedShow = "{}"; tracking.CachedEpisodes = "[]";
        tracking.LeaseToken = Guid.NewGuid();
        await db.SaveChangesAsync();
        var lease = tracking.LeaseToken;
        await TvQuery.SetEnabledAsync(db, tracking.Id, false);
        Assert.Empty((await TvQuery.GetAsync(db, dates, today, id)).Items);
        Assert.Equal(7, await db.TvEpisodes.CountAsync(e => e.SeriesId == series.Id));
        db.ChangeTracker.Clear();
        await TvQuery.FollowAsync(db, id, series.Title);
        tracking = await db.TvTrackedSeries.SingleAsync(t => t.TvmazeShowId == id);
        Assert.True(tracking.Enabled);
        Assert.Equal(lease, tracking.LeaseToken);
        Assert.Equal("[]", tracking.CachedEpisodes);
        Assert.NotNull(tracking.LastSuccessAt);
        Assert.Single(await db.TvTrackedSeries.Where(t => t.TvmazeShowId == id).ToListAsync());
        var health = await ScraperHealth.GetReportAsync(db, DateTime.UtcNow, today, TimeSpan.FromHours(3));
        Assert.Equal(HealthLevel.Ok, health.TvSeries.Single(t => t.Id == tracking.Id).Level);
        Assert.Contains(health.All, r => r.Id == tracking.Id && r.Name == series.Title);
    }

    [Fact]
    public async Task Follow_can_wait_for_first_import_and_keeps_unknown_dates_out_of_calendar()
    {
        await using var db = Create();
        await using var tx = await db.Database.BeginTransactionAsync();
        var id = Random.Shared.Next(1_500_000_000, 1_900_000_000);
        await TvQuery.FollowAsync(db, id, "Pending series");
        var tracking = await db.TvTrackedSeries.SingleAsync(t => t.TvmazeShowId == id);
        Assert.Null(tracking.SeriesId);
        var dates = FeedDates.Create("America/Chicago");
        var calendar = await TvQuery.GetAsync(db, dates, dates.Today, id);
        Assert.Empty(calendar.Items);
        Assert.Contains("Pending series", calendar.Unannounced);
        await TvQuery.SetEnabledAsync(db, tracking.Id, false);
        Assert.DoesNotContain((await TvQuery.GetAsync(db, dates, dates.Today, null)).Shows, s => s.Id == id);
    }

    [Fact]
    public async Task Search_keeps_same_named_shows_distinct_and_handles_rate_limits()
    {
        using var http = new HttpClient(new Handler(HttpStatusCode.OK, """
            [{"show":{"id":90632,"name":"Line of Fire","premiered":"2026-09-21","network":{"name":"NBC"},"image":null}},
             {"show":{"id":1961,"name":"Line of Fire","premiered":"2003-12-02","network":{"name":"ABC"}}}]
            """)) { BaseAddress = new Uri("https://api.tvmaze.com/") };
        var result = await new TvmazeClient(http).SearchAsync("Line of Fire");
        Assert.Null(result.Error);
        Assert.Equal(new[] { 90632, 1961 }, result.Shows.Select(s => s.Id));
        Assert.Equal("2026-09-21", result.Shows[0].Premiere);
        using var throttled = new HttpClient(new Handler(HttpStatusCode.TooManyRequests, "")) { BaseAddress = http.BaseAddress };
        Assert.NotNull((await new TvmazeClient(throttled).SearchAsync("test")).Error);
    }

    private sealed class Handler(HttpStatusCode status, string body) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct) =>
            Task.FromResult(new HttpResponseMessage(status) { Content = new StringContent(body) });
    }
}
