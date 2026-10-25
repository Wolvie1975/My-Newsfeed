using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using myNewspaper.Web.Components;
using myNewspaper.Web.Data;

namespace myNewspaper.Tests;

public class BoxOfficeForecastTests
{
    private static readonly DateTime Now = new(2026, 10, 14, 1, 0, 0, DateTimeKind.Utc);
    [Fact]
    public void Empty_states_require_recent_success_and_no_error()
    {
        var recent = new BoxOfficeForecastSource(BoxOfficeForecastQuery.Preview, Now.AddHours(-1), false);
        Assert.Equal("The next weekend preview has not been published yet", BoxOfficeForecastQuery.EmptyMessage(recent.Kind, recent, Now));
        foreach (var source in new BoxOfficeForecastSource?[] { null, recent with { Failed = true }, recent with { LastSuccess = null }, recent with { LastSuccess = Now.AddHours(-25) }, recent with { LastSuccess = Now.AddHours(1) } })
            Assert.Equal("Preview data is temporarily unavailable", BoxOfficeForecastQuery.EmptyMessage(recent.Kind, source, Now));
        Assert.True(BoxOfficeForecastQuery.IsRecent(recent with { LastSuccess = Now.AddHours(-24) }, Now));
        Assert.Equal("The next future-film forecasts have not been published yet", BoxOfficeForecastQuery.EmptyMessage(BoxOfficeForecastQuery.LongRange, recent, Now));
    }
    [Fact]
    public void Ranges_preserve_dollars_without_rounding_predictions()
    {
        Assert.Equal("$25M–$35M", BoxOfficeForecastQuery.Range(25_000_000, 35_000_000));
        Assert.Equal("$1.234567M–$2.5M", BoxOfficeForecastQuery.Range(1_234_567, 2_500_000));
        Assert.Equal("$0–$900,000", BoxOfficeForecastQuery.Range(0, 900_000));
    }
    [Fact]
    public async Task Preview_renders_only_provider_ranks_and_hides_diagnostic_errors()
    {
        using var services = new ServiceCollection().AddLogging().BuildServiceProvider();
        await using var renderer = new HtmlRenderer(services, services.GetRequiredService<ILoggerFactory>());
        var rows = new List<BoxOfficeForecast> {
            new(BoxOfficeForecastQuery.Preview, new(2026,10,16), new(2026,10,18), "Movie <one>", "Distributor", 3, 25_000_000, 35_000_000, new(2026,10,13), "https://example.com/analysis", Now),
            new(BoxOfficeForecastQuery.Preview, new(2026,10,16), new(2026,10,18), "Movie two", null, null, 1_000_000, 2_000_000, new(2026,10,13), null, Now)
        };
        var html = await renderer.Dispatcher.InvokeAsync(async () => (await renderer.RenderComponentAsync<BoxOfficeForecastSection>(ParameterView.FromDictionary(new Dictionary<string, object?>
        {
            ["Rows"] = rows,
            ["Kind"] = BoxOfficeForecastQuery.Preview,
            ["Source"] = new BoxOfficeForecastSource(BoxOfficeForecastQuery.Preview, Now, true),
            ["UtcNow"] = Now
        }))).ToHtmlString());
        Assert.Contains("Predicted rank 3", html);
        Assert.DoesNotContain("Predicted rank 1", html);
        Assert.Contains("Movie &lt;one&gt;", html);
        Assert.Contains("Forecast refresh is delayed", html);
        Assert.Contains("Boxoffice Pro", html);
        Assert.Contains("https://example.com/analysis", html);
    }
    [Fact]
    public async Task Application_credentials_read_forecasts_and_expired_dates_are_filtered()
    {
        await using var db = new WebScraperContext(new DbContextOptionsBuilder<WebScraperContext>().UseSqlServer(TestDatabase.ConnectionString).Options);
        var today = FeedDates.Create("America/Chicago").Today;
        var snapshot = await BoxOfficeForecastQuery.GetAsync(db, today);
        Assert.Contains(snapshot.Sources, s => s.Kind == BoxOfficeForecastQuery.Preview);
        Assert.Contains(snapshot.Sources, s => s.Kind == BoxOfficeForecastQuery.LongRange);
        Assert.All(snapshot.Movies, row =>
        {
            Assert.True(row.PeriodEnd >= today);
            Assert.True(row.Low <= row.High);
            Assert.Equal(DateTimeKind.Utc, row.FetchedAt.Kind);
        });
        var future = await BoxOfficeForecastQuery.GetAsync(db, new(9999, 12, 31));
        Assert.Empty(future.Movies);
        Assert.Equal(snapshot.Sources.Count, future.Sources.Count);
    }
}
