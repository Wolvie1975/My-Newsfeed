using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using MyNewsFeed.Web.Components.Events;
using MyNewsFeed.Web.Data;

namespace MyNewsFeed.Tests;

// Renders one game row to HTML (the same way the page does) and checks the markup.
public class EventsRenderTests
{
    private static EventItem Game(bool? isAway = false, string? opponent = "Houston", string? school = "Kansas",
        string? sport = "Volleyball", string? location = "Lawrence, Kan.", DateTime? startsUtc = null, bool tbd = false,
        string? tv = "ESPN+", string? stream = "https://www.espn.com/watch/x", string? stats = "https://stats.example.test/1",
        string url = "http://big12sports.com/calendar.aspx?id=1",
        string? teamLogo = "http://big12sports.com/images/logos/site/site.png",
        string? opponentLogo = "https://big12sports.com/images/logos/houston.png") =>
        new(1, url, "title", sport, opponent, isAway, location, new DateOnly(2026, 9, 25),
            startsUtc ?? new DateTime(2026, 9, 25, 23, 0, 0), tbd, tv, stream, stats, teamLogo, opponentLogo, school);

    private static async Task<string> RenderAsync(EventItem item, bool decode = true)
    {
        var services = new ServiceCollection()
            .AddLogging()
            .AddSingleton(FeedDates.Create("America/Chicago"))
            .BuildServiceProvider();

        await using var renderer = new HtmlRenderer(services, services.GetRequiredService<ILoggerFactory>());
        return await renderer.Dispatcher.InvokeAsync(async () =>
        {
            var output = await renderer.RenderComponentAsync<GameRow>(ParameterView.FromDictionary(
                new Dictionary<string, object?> { [nameof(GameRow.Item)] = item }));
            var html = output.ToHtmlString();
            return decode ? System.Net.WebUtility.HtmlDecode(html) : html;
        });
    }

    /// <summary>The text inside the cell with this class.</summary>
    private static string Cell(string html, string cellClass)
    {
        var start = html.IndexOf($"<td class=\"{cellClass}", StringComparison.Ordinal);
        Assert.True(start >= 0, $"no {cellClass} cell in: {html}");
        var end = html.IndexOf("</td>", start, StringComparison.Ordinal);
        return html[start..end];
    }

    [Fact]
    public async Task A_home_game_puts_the_opponent_in_the_away_column_and_the_school_in_home()
    {
        var html = await RenderAsync(Game(isAway: false));

        Assert.Contains("Houston", Cell(html, "td-away"));
        Assert.Contains("Kansas", Cell(html, "td-home"));
        Assert.Contains(">Away<", Cell(html, "td-away"));
        Assert.Contains(">Home<", Cell(html, "td-home"));
    }

    [Fact]
    public async Task An_away_game_puts_the_school_in_the_away_column()
    {
        var html = await RenderAsync(Game(isAway: true));

        Assert.Contains("Kansas", Cell(html, "td-away"));
        Assert.Contains("Houston", Cell(html, "td-home"));
    }

    [Fact]
    public async Task When_home_or_away_is_unknown_nothing_is_guessed_in_the_labels()
    {
        var html = await RenderAsync(Game(isAway: null));

        Assert.DoesNotContain("team__label", html);
        Assert.Contains("Kansas", Cell(html, "td-home"));
        Assert.Contains("Houston", Cell(html, "td-away"));
    }

    [Fact]
    public async Task A_missing_school_or_opponent_gets_a_neutral_label()
    {
        var html = await RenderAsync(Game(school: null, opponent: null));

        Assert.Contains("Our team", html);
        Assert.Contains("Opponent to be announced", html);
    }

    [Fact]
    public async Task Team_logos_are_https_lazy_no_referrer_and_fall_back_to_initials()
    {
        var html = await RenderAsync(Game(isAway: false, teamLogo: "http://big12sports.com/images/logos/site/site.png", opponentLogo: null));

        Assert.Contains("src=\"https://big12sports.com/images/logos/site/site.png\"", html);   // http upgraded
        Assert.DoesNotContain("src=\"http://", html);
        Assert.Contains("referrerpolicy=\"no-referrer\"", html);
        Assert.Contains("loading=\"lazy\"", html);
        Assert.Contains("onerror=\"this.parentNode.classList.add('is-broken');this.remove()\"", html);
        Assert.Contains("no-logo", Cell(html, "td-away"));               // the opponent has no logo
        Assert.Contains(">HOU<", Cell(html, "td-away"));                 // so its badge shows initials
    }

    [Fact]
    public async Task Unsafe_logo_addresses_are_dropped()
    {
        var html = await RenderAsync(Game(opponentLogo: "javascript:alert(1)"));

        Assert.DoesNotContain("javascript:", html);
        Assert.Contains("no-logo", Cell(html, "td-away"));
    }

    [Fact]
    public async Task The_start_time_is_shown_in_central_and_tbd_says_so()
    {
        var timed = await RenderAsync(Game(startsUtc: new DateTime(2026, 9, 25, 23, 0, 0)));
        Assert.Contains("6:00 P.M. CT", Cell(timed, "td-time"));
        Assert.DoesNotContain("is-tbd", timed);

        var tbd = await RenderAsync(Game(tbd: true));
        Assert.Contains("Time TBD", Cell(tbd, "td-time"));
        Assert.Contains("is-tbd", tbd);
    }

    [Fact]
    public async Task A_stadium_is_shown_under_the_city()
    {
        var html = await RenderAsync(Game(location: "Lawrence, Kan. / David Booth Kansas Memorial Stadium"));

        Assert.Contains("<span>Lawrence, Kan.</span>", html);
        Assert.Contains("<span class=\"loc__venue\">David Booth Kansas Memorial Stadium</span>", html);
    }

    [Fact]
    public async Task Links_show_tv_stats_video_and_the_game_page_and_open_in_a_new_tab_safely()
    {
        var html = await RenderAsync(Game());
        var links = Cell(html, "td-links");

        Assert.Contains("ESPN+", links);
        Assert.Contains(">Stats</a>", links);
        Assert.Contains(">Video</a>", links);
        Assert.Contains(">Details</a>", links);
        Assert.Contains("target=\"_blank\"", links);
        Assert.Contains("rel=\"noopener noreferrer\"", links);
    }

    [Fact]
    public async Task Missing_and_unsafe_links_are_left_out()
    {
        var html = await RenderAsync(Game(tv: null, stream: "javascript:alert(1)", stats: null, url: "javascript:alert(2)"));

        Assert.DoesNotContain("javascript:", html);
        Assert.DoesNotContain(">Video</a>", html);
        Assert.Contains("td-links--none", html);   // nothing left to show, so the cell collapses on small screens
    }

    [Fact]
    public async Task The_sport_shows_above_the_teams_and_collapses_when_unknown()
    {
        Assert.Contains("Volleyball", Cell(await RenderAsync(Game(sport: "Volleyball")), "td-sport"));
        Assert.Contains("td-sport--none", await RenderAsync(Game(sport: null)));
    }

    [Fact]
    public async Task Names_are_html_encoded()
    {
        var html = await RenderAsync(Game(opponent: "<script>alert(1)</script>"), decode: false);
        Assert.DoesNotContain("<script>alert", html);
        Assert.Contains("&lt;script&gt;", html);
    }
}
