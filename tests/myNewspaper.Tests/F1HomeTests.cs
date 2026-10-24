using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using myNewspaper.Web.Components.F1;
using myNewspaper.Web.Data;
using myNewspaper.Web.Data.Models;

namespace myNewspaper.Tests;

public class F1HomeTests
{
    private static readonly FeedDates Dates = FeedDates.Create("America/Chicago");
    private static readonly F1Race Race = new() { Season = 2026, Round = 18, RaceName = "United States Grand Prix", RaceDate = new(2026, 10, 25), RaceTimeUtc = "20:00:00Z", FirstPractice = "{\"date\":\"2026-10-23\",\"time\":\"17:30:00Z\"}" };
    private static F1Page Page => F1Page.Empty with { Source = new F1Source { Enabled = true }, Races = [Race] };

    [Theory]
    [InlineData(22, false)]
    [InlineData(23, true)]
    [InlineData(24, true)]
    [InlineData(25, true)]
    [InlineData(26, false)]
    public void Friday_through_race_day_is_inclusive(int day, bool embedded)
    {
        Assert.Equal(embedded, F1Query.HomeWeekendRace(Page, Dates, new(2026, 10, day)) is not null);
        Assert.Equal(embedded, F1Query.HomeWeekendRace(Page with { Choices = [new(18, "Race")] }, Dates, new(2026, 10, day)) is not null);
    }

    [Fact]
    public void Window_uses_local_race_day_and_handles_unknown_time_and_disabled_data()
    {
        var saturday = new F1Race { RaceDate = new(2026, 10, 25), RaceTimeUtc = "04:00:00Z" }; // Saturday 11pm Central.
        var page = Page with { Races = [saturday] };
        Assert.NotNull(F1Query.HomeWeekendRace(page, Dates, new(2026, 10, 23)));
        Assert.NotNull(F1Query.HomeWeekendRace(page, Dates, new(2026, 10, 24)));
        Assert.Null(F1Query.HomeWeekendRace(page, Dates, new(2026, 10, 25)));
        saturday.RaceTimeUtc = null;
        Assert.NotNull(F1Query.HomeWeekendRace(page, Dates, new(2026, 10, 25)));
        Assert.Null(F1Query.HomeWeekendRace(page with { Source = new F1Source { Enabled = false } }, Dates, new(2026, 10, 25)));
        Assert.Null(F1Query.HomeWeekendRace(F1Page.Empty, Dates, new(2026, 10, 25)));
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task Summary_renders_race_next_session_and_links_in_both_placements(bool embedded)
    {
        await using var services = new ServiceCollection().AddLogging().AddSingleton(Dates).BuildServiceProvider();
        await using var renderer = new HtmlRenderer(services, services.GetRequiredService<ILoggerFactory>());
        var html = await renderer.Dispatcher.InvokeAsync(async () => (await renderer.RenderComponentAsync<F1Home>(ParameterView.FromDictionary(new Dictionary<string, object?>
        {
            ["Data"] = Page, ["Embedded"] = embedded, ["NowUtc"] = new DateTime(2026, 10, 23, 12, 0, 0, DateTimeKind.Utc)
        }))).ToHtmlString());
        Assert.Contains("United States Grand Prix", html);
        Assert.Contains("Practice 1", html);
        Assert.Contains("f1/standings", html);
        Assert.Contains("3:00 PM", html);
        Assert.Equal(!embedded, html.Contains("data-home-section=\"f1\""));
        Assert.Equal(embedded, html.Contains("F1 &#xB7; Race weekend") || html.Contains("F1 · Race weekend"));
    }
}
