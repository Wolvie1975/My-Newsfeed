using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using myNewspaper.Web.Data;
namespace myNewspaper.Tests;

public class BoxOfficeChartTests
{
    [Fact]
    public async Task Stored_chart_can_be_read_with_application_credentials()
    {
        await using var db = new WebScraperContext(new DbContextOptionsBuilder<WebScraperContext>()
            .UseSqlServer(TestDatabase.ConnectionString).Options);
        var rows = await BoxOfficeChartQuery.GetAsync(db);
        Assert.Equal(10, rows.Count);
        Assert.Equal(Enumerable.Range(1, 10), rows.Select(row => row.Rank));
        Assert.Single(rows.Select(row => row.WeekendStart).Distinct());
        Assert.All(rows, row =>
        {
            Assert.False(string.IsNullOrWhiteSpace(row.Title));
            Assert.Equal(DateTimeKind.Utc, row.FetchedAt.Kind);
            Assert.NotNull(row.SourceUrl);
        });
    }

    [Fact]
    public void Calendar_weekend_is_not_shifted_by_display_timezone()
    {
        Assert.Equal("October 9–11, 2026", BoxOfficeChartQuery.WeekendLabel(new(2026, 10, 9)));
        Assert.Equal("October 30–November 1, 2026", BoxOfficeChartQuery.WeekendLabel(new(2026, 10, 30)));
        Assert.Equal("December 31, 2027–January 2, 2028", BoxOfficeChartQuery.WeekendLabel(new(2027, 12, 31)));
        Assert.False(BoxOfficeChartQuery.IsStale(new(2026, 10, 9), new(2026, 10, 19)));
        Assert.True(BoxOfficeChartQuery.IsStale(new(2026, 10, 9), new(2026, 10, 20)));
    }
    [Fact]
    public void Missing_change_does_not_become_zero_and_money_uses_usd()
    {
        Assert.Equal("—", BoxOfficeChartQuery.Change(null));
        Assert.Equal("0%", BoxOfficeChartQuery.Change(0));
        Assert.Equal("-63%", BoxOfficeChartQuery.Change(-63));
        Assert.Equal("+65%", BoxOfficeChartQuery.Change(65));
        Assert.Equal("$27,805,825", BoxOfficeChartQuery.Money(27805825));
        Assert.Null(BoxOfficeChartQuery.SafeUrl("javascript:alert(1)"));
        Assert.Null(BoxOfficeChartQuery.SafeUrl("//example.com"));
        Assert.Equal("https://example.com/movie", BoxOfficeChartQuery.SafeUrl("https://example.com/movie"));
    }
}
