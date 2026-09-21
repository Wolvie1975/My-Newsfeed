using Microsoft.EntityFrameworkCore;
using MyNewsFeed.Web.Data;
using MyNewsFeed.Web.Data.Models;

namespace MyNewsFeed.Tests;

public class EventsTests
{
    // ---- text helpers: no database ---------------------------------------------------------------------------------

    [Theory]
    [InlineData("https://big12sports.com/images/logos/houston.png", "https://big12sports.com/images/logos/houston.png")]
    [InlineData("http://big12sports.com/images/logos/site/site.png", "https://big12sports.com/images/logos/site/site.png")]
    [InlineData("HTTP://Example.test/a.png", "https://example.test/a.png")]
    [InlineData("javascript:alert(1)", null)]
    [InlineData("data:image/png;base64,AAAA", null)]
    [InlineData("/images/logos/ksu.png", null)]
    [InlineData("", null)]
    [InlineData(null, null)]
    public void Logo_urls_are_web_addresses_and_http_is_upgraded_to_https(string? url, string? expected) =>
        Assert.Equal(expected, EventsText.LogoUrl(url));

    [Theory]
    [InlineData("Lawrence, Kan.", "Lawrence, Kan.", null)]
    [InlineData("Lawrence, Kan. / David Booth Kansas Memorial Stadium", "Lawrence, Kan.", "David Booth Kansas Memorial Stadium")]
    [InlineData("Manhattan, Kan. / Bill Snyder Family Stadium", "Manhattan, Kan.", "Bill Snyder Family Stadium")]
    [InlineData("  Boulder, Colo.  ", "Boulder, Colo.", null)]
    [InlineData("", null, null)]
    [InlineData(null, null, null)]
    public void Locations_split_into_city_and_venue(string? location, string? city, string? venue) =>
        Assert.Equal((city, venue), EventsText.SplitLocation(location));

    [Theory]
    [InlineData("Grand Canyon", "GC")]
    [InlineData("Arizona State", "AS")]
    [InlineData("Texas Tech University", "TTU")]
    [InlineData("Kansas", "KAN")]
    [InlineData("BYU", "BYU")]
    [InlineData("Salt Lake City State College", "SLC")]
    [InlineData("  ", "?")]
    [InlineData("", "?")]
    [InlineData(null, "?")]
    public void Initials_stand_in_for_a_missing_logo(string? name, string expected) =>
        Assert.Equal(expected, EventsText.Initials(name));

    [Theory]
    [InlineData(null, null, "")]
    [InlineData("Volleyball", null, "?sport=Volleyball")]
    [InlineData(null, "kansas state", "?q=kansas%20state")]
    [InlineData("Women's Soccer", "a&b", "?sport=Women%27s%20Soccer&q=a%26b")]
    [InlineData("  ", "  ", "")]
    public void Query_strings_keep_only_the_filters_that_are_set(string? sport, string? search, string expected) =>
        Assert.Equal(expected, EventsQuery.QueryString(sport, search));

    // ---- times and dates ---------------------------------------------------------------------------------------------

    [Theory]
    [InlineData("2026-09-20 19:00", "2:00 P.M. CT")]
    [InlineData("2026-09-24 23:00", "6:00 P.M. CT")]
    [InlineData("2026-09-20 17:05", "12:05 P.M. CT")]
    [InlineData("2026-09-20 05:30", "12:30 A.M. CT")]
    [InlineData("2026-09-20 14:00", "9:00 A.M. CT")]
    [InlineData("2026-11-05 00:00", "6:00 P.M. CT")]   // after daylight saving ends: UTC-6
    public void Game_times_are_shown_in_central_in_schedule_style(string utc, string expected) =>
        Assert.Equal(expected, FeedDates.Create("America/Chicago").EventTime(DateTime.Parse(utc)));

    [Fact]
    public void The_time_label_names_the_display_zone()
    {
        var utc = new DateTime(2026, 9, 20, 19, 0, 0);
        Assert.Equal("7:00 P.M. UTC", FeedDates.Create("UTC").EventTime(utc));
        Assert.Equal("3:00 P.M. ET", FeedDates.Create("America/New_York").EventTime(utc));
        Assert.Equal("12:00 P.M. PT", FeedDates.Create("America/Los_Angeles").EventTime(utc));
        Assert.Equal("12:30 A.M. UTC+5:30", FeedDates.Create("Asia/Kolkata").EventTime(utc));   // no everyday abbreviation
    }

    [Theory]
    [InlineData("2026-09-25", "Friday, September 25, 2026")]
    [InlineData("2026-10-01", "Thursday, October 1, 2026")]
    [InlineData("2026-11-28", "Saturday, November 28, 2026")]
    public void Date_bars_write_the_date_out(string date, string expected) =>
        Assert.Equal(expected, FeedDates.LongDate(DateOnly.Parse(date)));

    // ---- the query, against the database (each test rolls back) --------------------------------------------------

    private static WebScraperContext Create() => new(
        new DbContextOptionsBuilder<WebScraperContext>()
            .UseSqlServer("Server=sql2025,1433;Database=WebScraper;User Id=sa;Password=Fedora_Dev_2025!;Encrypt=False;TrustServerCertificate=True;")
            .Options);

    private static string Tag() => Guid.NewGuid().ToString("N")[..10];

    private static SportsEvent Game(string token, string title, DateOnly date, DateTime? startsUtc = null, bool tbd = false,
        string? sport = "Volleyball", string? opponent = "Test U", string? location = "Lawrence, Kan.") => new()
    {
        Url = $"http://{token}.test.invalid/calendar.aspx?id={Guid.NewGuid():N}",
        Title = $"zz {token} {title}",
        Sport = sport,
        Opponent = opponent,
        Location = location,
        EventDate = date,
        StartsAtUtc = startsUtc,
        TimeTbd = tbd,
    };

    [Fact]
    public async Task Upcoming_starts_today_and_includes_todays_finished_games_but_not_yesterdays()
    {
        await using var db = Create();
        await using var tx = await db.Database.BeginTransactionAsync();
        var token = Tag();
        var today = new DateOnly(2026, 6, 15);
        db.SportsEvents.AddRange(
            Game(token, "yesterday", today.AddDays(-1)), Game(token, "today", today), Game(token, "tomorrow", today.AddDays(1)));
        await db.SaveChangesAsync();

        var page = await EventsQuery.GetUpcomingAsync(db, today, null, token);

        Assert.Equal(new[] { $"zz {token} today", $"zz {token} tomorrow" }, page.Items.Select(i => i.Title));
        Assert.Equal(2, page.Total);
    }

    [Fact]
    public async Task Games_are_ordered_by_day_then_start_time_with_time_tbd_last()
    {
        await using var db = Create();
        await using var tx = await db.Database.BeginTransactionAsync();
        var token = Tag();
        var day = new DateOnly(2026, 6, 15);
        db.SportsEvents.AddRange(
            Game(token, "b tbd", day, tbd: true, sport: "Football"),
            Game(token, "c evening", day, new DateTime(2026, 6, 16, 0, 0, 0)),
            Game(token, "a afternoon", day, new DateTime(2026, 6, 15, 19, 0, 0)),
            Game(token, "d next day", day.AddDays(1), new DateTime(2026, 6, 15, 20, 0, 0)));
        await db.SaveChangesAsync();

        var titles = (await EventsQuery.GetUpcomingAsync(db, day, null, token)).Items.Select(i => i.Title.Replace($"zz {token} ", ""));

        Assert.Equal(new[] { "a afternoon", "c evening", "b tbd", "d next day" }, titles);
    }

    [Fact]
    public async Task The_sport_filter_and_search_narrow_the_list()
    {
        await using var db = Create();
        await using var tx = await db.Database.BeginTransactionAsync();
        var token = Tag();
        var day = new DateOnly(2026, 6, 15);
        var type = new SportsEventsType { EventsTypeName = "Type", RssUrl = $"https://{token}.test.invalid/rss", SchoolName = $"School{token}" };
        var soccer = Game(token, "soccer game", day, sport: "Soccer", opponent: "Arizona", location: "Tucson, Ariz.");
        soccer.SportsEventsType = type;
        db.SportsEvents.AddRange(soccer, Game(token, "volleyball game", day, sport: "Volleyball", opponent: "Houston"));
        await db.SaveChangesAsync();

        Assert.Equal(2, (await EventsQuery.GetUpcomingAsync(db, day, null, token)).Total);
        Assert.Equal("soccer game", Assert.Single((await EventsQuery.GetUpcomingAsync(db, day, "Soccer", token)).Items).Title.Replace($"zz {token} ", ""));
        Assert.Single((await EventsQuery.GetUpcomingAsync(db, day, "soccer", token)).Items);                // case-insensitive
        Assert.Empty((await EventsQuery.GetUpcomingAsync(db, day, "Football", token)).Items);
        Assert.Single((await EventsQuery.GetUpcomingAsync(db, day, null, "Houston")).Items.Where(i => i.Title.Contains(token)));   // opponent
        Assert.Single((await EventsQuery.GetUpcomingAsync(db, day, null, "Tucson")).Items.Where(i => i.Title.Contains(token)));     // location
        Assert.Single((await EventsQuery.GetUpcomingAsync(db, day, null, $"School{token}")).Items);                                  // school name
    }

    [Fact]
    public async Task Each_event_carries_its_schools_name_from_its_type()
    {
        await using var db = Create();
        await using var tx = await db.Database.BeginTransactionAsync();
        var token = Tag();
        var day = new DateOnly(2026, 6, 15);
        var type = new SportsEventsType { EventsTypeName = "Type", RssUrl = $"https://{token}.test.invalid/rss", SchoolName = "Testville" };
        var linked = Game(token, "linked", day);
        linked.SportsEventsType = type;
        db.SportsEvents.AddRange(linked, Game(token, "unlinked", day));
        await db.SaveChangesAsync();

        var items = (await EventsQuery.GetUpcomingAsync(db, day, null, token)).Items;

        Assert.Equal("Testville", items.Single(i => i.Title.EndsWith("linked") && !i.Title.EndsWith("unlinked")).SchoolName);
        Assert.Null(items.Single(i => i.Title.EndsWith("unlinked")).SchoolName);
    }

    [Fact]
    public async Task The_list_is_capped_but_the_total_is_not()
    {
        await using var db = Create();
        await using var tx = await db.Database.BeginTransactionAsync();
        var token = Tag();
        var day = new DateOnly(2026, 6, 15);
        db.SportsEvents.AddRange(Game(token, "1", day), Game(token, "2", day.AddDays(1)), Game(token, "3", day.AddDays(2)));
        await db.SaveChangesAsync();

        var page = await EventsQuery.GetUpcomingAsync(db, day, null, token, limit: 2);

        Assert.Equal(2, page.Items.Count);
        Assert.Equal(3, page.Total);
    }

    [Fact]
    public async Task The_sport_filter_lists_only_sports_with_upcoming_games_and_last_updated_follows_the_scraper()
    {
        await using var db = Create();
        await using var tx = await db.Database.BeginTransactionAsync();
        var token = Tag();
        var day = new DateOnly(2026, 6, 15);
        var upcoming = Game(token, "upcoming", day, sport: $"ZZUp{token}");
        var past = Game(token, "past", day.AddDays(-5), sport: $"ZZPast{token}");
        upcoming.LastSeenAt = new DateTime(2099, 1, 1, 12, 0, 0);
        db.SportsEvents.AddRange(upcoming, past);
        await db.SaveChangesAsync();

        var sports = await EventsQuery.GetSportsAsync(db, day);

        Assert.Contains($"ZZUp{token}", sports);
        Assert.DoesNotContain($"ZZPast{token}", sports);
        Assert.Equal(new DateTime(2099, 1, 1, 12, 0, 0), await EventsQuery.GetLastUpdatedAsync(db));
    }

    [Fact]
    public async Task No_test_events_are_left_in_the_database()
    {
        await using var db = Create();
        Assert.False(await db.SportsEvents.AnyAsync(e => e.Url.Contains("test.invalid")));
    }

    [Theory]
    [InlineData("Kansas", "Houston", true, "Kansas at Houston")]
    [InlineData("Kansas", "Houston", false, "Kansas vs Houston")]
    [InlineData("Kansas", "Houston", null, "Kansas / Houston")]
    [InlineData(null, " Houston ", false, "Our team vs Houston")]
    [InlineData("Kansas", null, true, "Kansas at Opponent to be announced")]
    public void Matchups_put_the_school_first(string? school, string? opponent, bool? isAway, string expected) =>
        Assert.Equal(expected, EventsText.Matchup(school, opponent, isAway));

    [Fact]
    public async Task Todays_games_are_only_that_date_in_start_order_with_time_tbd_last()
    {
        await using var db = Create();
        await using var tx = await db.Database.BeginTransactionAsync();
        var token = Tag();
        var day = new DateOnly(2031, 3, 4);
        db.SportsEvents.AddRange(
            Game(token, "yesterday", day.AddDays(-1), new DateTime(2031, 3, 3, 20, 0, 0)),
            Game(token, "b tbd", day, tbd: true),
            Game(token, "c evening", day, new DateTime(2031, 3, 5, 0, 0, 0)),
            Game(token, "a afternoon", day, new DateTime(2031, 3, 4, 19, 0, 0)),
            Game(token, "tomorrow", day.AddDays(1), new DateTime(2031, 3, 5, 20, 0, 0)));
        await db.SaveChangesAsync();

        var games = (await EventsQuery.GetOnDateAsync(db, day)).Where(g => g.Title.Contains(token));

        Assert.Equal(new[] { "a afternoon", "c evening", "b tbd" }, games.Select(g => g.Title.Replace($"zz {token} ", "")));
    }
}
