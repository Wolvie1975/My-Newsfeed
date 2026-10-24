using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using myNewspaper.Web.Components;
using myNewspaper.Web.Data;
namespace myNewspaper.Tests;

public class BoxOfficeResultStatusTests
{
    [Theory]
    [InlineData("estimated", "Estimated")]
    [InlineData("final", "Final")]
    [InlineData("unknown", "Reported totals")]
    [InlineData(null, "Reported totals")]
    [InlineData("unexpected", "Reported totals")]
    public void Status_mapping_is_explicit(string? status, string label) => Assert.Equal(label, BoxOfficeChartQuery.StatusLabel(status));

    [Fact]
    public async Task Mixed_mock_chart_labels_weekend_only_and_omits_missing_provider_time()
    {
        await using var services = new ServiceCollection().AddLogging().AddSingleton(FeedDates.Create("America/Chicago")).BuildServiceProvider();
        await using var renderer = new HtmlRenderer(services, services.GetRequiredService<ILoggerFactory>());
        var friday = new DateOnly(2026, 10, 23);
        var rows = new[] { "estimated", "final", "unknown" }.Select((status, i) => new BoxOfficeMovie(i+1, friday, "Mock " + status, 1000, 2000, null, null, null, "https://www.the-numbers.com/", DateTime.UtcNow, ResultStatus: status)).ToList();
        var html = await renderer.Dispatcher.InvokeAsync(async () => (await renderer.RenderComponentAsync<HomeBoxOfficeResults>(ParameterView.FromDictionary(new Dictionary<string, object?> { ["Rows"] = rows, ["Weekend"] = friday }))).ToHtmlString());
        Assert.Contains("Weekend gross · Estimated", html);
        Assert.Contains("Weekend gross · Final", html);
        Assert.Contains("Weekend gross · Reported totals", html);
        Assert.Contains("<dt>Total domestic</dt>", html);
        Assert.DoesNotContain("Provider updated", html);
        Assert.Contains("Status source:", html);
    }
}
