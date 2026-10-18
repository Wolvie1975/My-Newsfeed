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

    // The scraper normally initializes this additive column. Exercise the same migration inside
    // each rollback transaction so tests also work against a pre-deployment development database.
    private static async Task<Microsoft.EntityFrameworkCore.Storage.IDbContextTransaction> BeginScheduleTransactionAsync(WebScraperContext db)
    {
        var transaction = await db.Database.BeginTransactionAsync();
        await db.Database.ExecuteSqlRawAsync(
            "IF COL_LENGTH('dbo.SportsEvents', 'ScheduleStatus') IS NULL ALTER TABLE dbo.SportsEvents ADD ScheduleStatus nvarchar(100) NULL;");
        return transaction;
    }

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
    public async Task Upcoming_starts_today_and_preserves_legacy_games_with_unknown_status()
    {
        await using var db = Create();
        await using var tx = await BeginScheduleTransactionAsync(db);
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
        await using var tx = await BeginScheduleTransactionAsync(db);
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
        await using var tx = await BeginScheduleTransactionAsync(db);
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
        await using var tx = await BeginScheduleTransactionAsync(db);
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
        await using var tx = await BeginScheduleTransactionAsync(db);
        var token = Tag();
        var day = new DateOnly(2026, 6, 15);
        db.SportsEvents.AddRange(Game(token, "1", day), Game(token, "2", day.AddDays(1)), Game(token, "3", day.AddDays(2)));
        await db.SaveChangesAsync();

        var page = await EventsQuery.GetUpcomingAsync(db, day, null, token, limit: 2);

        Assert.Equal(2, page.Items.Count);
        Assert.Equal(3, page.Total);
        Assert.Single((await EventsQuery.GetUpcomingAsync(db, day, null, token, limit: 2, offset: 2)).Items);
    }

    [Fact]
    public async Task The_sport_filter_lists_only_sports_with_upcoming_games_and_last_updated_follows_the_scraper()
    {
        await using var db = Create();
        await using var tx = await BeginScheduleTransactionAsync(db);
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
        await using var tx = await BeginScheduleTransactionAsync(db);
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

    [Fact]
    public async Task Homepage_hides_a_college_final_while_preserving_schedule_and_in_progress_games()
    {
        await using var db = Create();
        await using var tx = await BeginScheduleTransactionAsync(db);
        var token = Tag();
        var day = new DateOnly(2031, 3, 4);
        var start = new DateTime(2031, 3, 4, 19, 0, 0);
        var final = Game(token, "final", day, start, sport: "Football", opponent: "Houston");
        final.SchoolName = "Kansas";
        final.ProviderKey = "big12";
        var live = Game(token, "live", day, start.AddHours(3), sport: "Football", opponent: "Houston");
        live.SchoolName = "Kansas";
        live.ProviderKey = "big12";
        var espn = Game(token, "espn final", day, start.AddHours(6), sport: "Hockey");
        espn.ProviderKey = "espn-nhl";
        espn.GameId = 987654;
        espn.CanonicalKey = "espn:nhl:987654";
        db.SportsEvents.AddRange(final, live, espn);
        db.ResultGames.Add(new ResultGame { Sport = "hockey", League = "nhl", EventId = "987654",
            StartsAtUtc = start.AddHours(6), State = "post", Completed = true, HomeTeamId = "1", AwayTeamId = "2",
            HomeName = "Home", AwayName = "Away", HomeScore = 1, AwayScore = 0,
            FirstSeenAt = start, LastSeenAt = start });
        db.ResultGames.Add(new ResultGame { Sport = "football", League = "college-football", EventId = token,
            StartsAtUtc = start, State = "post", Completed = true, HomeTeamId = "2305", AwayTeamId = "248",
            HomeName = "Kansas Jayhawks", AwayName = "Houston Cougars", HomeScore = 21, AwayScore = 7,
            FirstSeenAt = start, LastSeenAt = start });
        await db.SaveChangesAsync();
        var scheduled = await EventsQuery.GetOnDateAsync(db, day);
        var homepage = await HomeEventResults.WithoutFinalResultsAsync(db, scheduled);
        Assert.DoesNotContain(homepage, g => g.Id == final.Id);
        Assert.DoesNotContain(homepage, g => g.Id == espn.Id);
        Assert.Contains(homepage, g => g.Id == live.Id);
        Assert.Contains(scheduled, g => g.Id == final.Id);
    }

    [Fact]
    public async Task Expanded_calendars_match_both_schools_without_duplicate_events_and_respect_disabled_feeds()
    {
        await using var db = Create();
        await using var tx = await BeginScheduleTransactionAsync(db);
        var tag = Guid.NewGuid().ToString("N");
        var first = new SportsEventsType { EventsTypeName = tag, SchoolName = "First", RssUrl = $"https://test.invalid/{tag}/1", Enabled = true };
        var second = new SportsEventsType { EventsTypeName = tag, SchoolName = "Second", RssUrl = $"https://test.invalid/{tag}/2", Enabled = true };
        db.AddRange(first, second);
        await db.SaveChangesAsync();
        var game = new SportsEvent { Title = tag, Url = $"https://test.invalid/{tag}/game", EventDate = new DateOnly(2030, 1, 1),
            SchoolName = "Canonical school", Sport = "Basketball", Opponent = "Second", IsNeutral = true,
            TimeTbd = true, SportsEventsTypeId = first.Id, FirstSeenAt = DateTime.UtcNow, LastSeenAt = DateTime.UtcNow };
        db.Add(game);
        await db.SaveChangesAsync();
        db.SportsEventSources.AddRange(
            new SportsEventSource { SportsEventId = game.Id, SportsEventsTypeId = first.Id, ProviderGameId = 1, SourceUrl = game.Url, FirstSeenAt = DateTime.UtcNow, LastSeenAt = DateTime.UtcNow },
            new SportsEventSource { SportsEventId = game.Id, SportsEventsTypeId = second.Id, ProviderGameId = 2, SourceUrl = game.Url, FirstSeenAt = DateTime.UtcNow, LastSeenAt = DateTime.UtcNow });
        await db.SaveChangesAsync();
        var today = game.EventDate;
        var combined = await EventsQuery.GetUpcomingAsync(db, today, null, tag);
        Assert.Equal(game.Id, Assert.Single(combined.Items).Id);
        Assert.Equal("Canonical school", combined.Items[0].SchoolName);
        Assert.True(combined.Items[0].IsNeutral);
        foreach (var school in new[] { first.Id, second.Id })
            Assert.Equal(game.Id, Assert.Single((await EventsQuery.GetUpcomingAsync(db, today, null, tag, school: school)).Items).Id);
        first.ProviderKey = "big12";
        first.LastSuccessAt = DateTime.UtcNow;
        first.LastAttemptAt = first.LastSuccessAt.Value.AddMinutes(-1);
        first.LastIssueCount = 1;
        first.LastError = "RSS/JSON timing disagreement";
        await db.SaveChangesAsync();
        var health = await ScraperHealth.GetReportAsync(db, DateTime.UtcNow, today, TimeSpan.FromHours(3));
        Assert.Equal(HealthLevel.Warning, health.EventFeeds.Single(f => f.Id == first.Id).Level);
        first.LastAttemptAt = first.LastSuccessAt.Value.AddMinutes(1);
        await db.SaveChangesAsync();
        health = await ScraperHealth.GetReportAsync(db, DateTime.UtcNow, today, TimeSpan.FromHours(3));
        Assert.Equal(HealthLevel.Problem, health.EventFeeds.Single(f => f.Id == first.Id).Level);
        second.Enabled = false;
        await db.SaveChangesAsync();
        Assert.Empty((await EventsQuery.GetUpcomingAsync(db, today, null, tag, school: second.Id)).Items);
        Assert.Single((await EventsQuery.GetUpcomingAsync(db, today, null, tag)).Items);
        await Assert.ThrowsAsync<InvalidOperationException>(() => AdminData.DeleteSportsEventsTypeAsync(db, first.Id, true));
        first.Enabled = false;
        await db.SaveChangesAsync();
        Assert.Empty((await EventsQuery.GetUpcomingAsync(db, today, null, tag)).Items);
    }
    [Fact]
    public async Task English_schedules_scope_team_ids_and_hide_final_disabled_and_past_games()
    {
        await using var db = Create();
        await using var tx = await BeginScheduleTransactionAsync(db);
        var token = Tag(); var day = new DateOnly(2031, 3, 4);
        var championship = new SportsEventsType { EventsTypeName = token, RssUrl = $"https://test.invalid/{token}/championship", ProviderKey = "espn-championship", Enabled = true };
        var epl = new SportsEventsType { EventsTypeName = token, RssUrl = $"https://test.invalid/{token}/epl", ProviderKey = "espn-epl", Enabled = true };
        SportsEvent Match(string name, int home, int away, string? status = null)
        {
            var game = Game(token, name, day); game.SportsEventsType = championship; game.ProviderKey = "espn-championship";
            game.SchoolId = home; game.OpponentSchoolId = away; game.SchoolName = name; game.Opponent = "Away";
            game.ScheduleStatus = status; game.IsAway = true; return game;
        }
        var home = Match("Wrexham home", 352, 1); home.TimeTbd = true;
        var away = Match("Wrexham away", 1, 352);
        var other = Match("Other league", 352, 2); other.ProviderKey = "espn-epl"; other.SportsEventsType = epl;
        var past = Match("Past", 352, 1); past.EventDate = day.AddDays(-1);
        db.AddRange(home, away, other, past, Match("Final", 352, 1, "STATUS_FINAL_PEN"), Match("Canceled", 352, 1, "STATUS_CANCELED"), Match("Unnecessary", 352, 1, "STATUS_UNNECESSARY"));
        await db.SaveChangesAsync();
        var focused = await EventsQuery.GetUpcomingAsync(db, day, null, token, providerTeamId: 352);
        Assert.Equal(2, focused.Total); Assert.All(focused.Items, item => Assert.False(item.IsAway));
        Assert.Equal(day, focused.Items.Single(item => item.Id == home.Id).EventDate);
        Assert.Equal("Time TBD", EventsText.TimeLabel(focused.Items.Single(item => item.Id == home.Id), FeedDates.Create("America/Chicago")));
        Assert.Equal(3, (await EventsQuery.GetUpcomingAsync(db, day, null, token)).Total);
        var selectors = await EventsQuery.GetSchoolsAsync(db);
        Assert.Contains(selectors, source => source.Id == championship.Id && source.Name == "EFL Championship");
        Assert.Contains(selectors, source => source.Id == epl.Id && source.Name == "Premier League");
        championship.Enabled = false; await db.SaveChangesAsync();
        Assert.Empty((await EventsQuery.GetUpcomingAsync(db, day, null, token, providerTeamId: 352)).Items);
    }

    [Fact]
    public async Task Mls_schedule_is_selectable_provider_scoped_and_home_perspective_including_today()
    {
        await using var db = Create();
        await using var tx = await BeginScheduleTransactionAsync(db);
        var token = Tag(); var day = new DateOnly(2031, 3, 4);
        var feed = await db.SportsEventsTypes.SingleOrDefaultAsync(f => f.ProviderKey == "espn-mls" && f.ProviderSchoolId == 0)
            ?? new SportsEventsType { EventsTypeName = token, RssUrl = $"https://test.invalid/{token}/mls", ProviderKey = "espn-mls", ProviderSchoolId = 0, Enabled = true };
        feed.Enabled = true;
        SportsEvent Match(string name, string? status = null) {
            var game = Game(token, name, day, sport: "Soccer"); game.ProviderKey = "espn-mls"; game.SportsEventsType = feed;
            game.ScheduleStatus = status; game.SchoolName = "MLS home"; game.Opponent = "MLS away"; game.IsAway = true; return game;
        }
        var future = Match("future"); future.EventDate = day.AddDays(1);
        var today = Match("today"); today.TimeTbd = true;
        var past = Match("past"); past.EventDate = day.AddDays(-1);
        var mismatch = Match("wrong provider"); mismatch.ProviderKey = "espn-epl";
        db.AddRange(today,future,past,mismatch,Match("final","STATUS_FINAL_PEN"),Match("canceled","STATUS_CANCELED")); await db.SaveChangesAsync();
        Assert.Contains(await EventsQuery.GetSchoolsAsync(db),f=>f.Id==feed.Id && f.Name=="MLS");
        var page=await EventsQuery.GetUpcomingAsync(db,day,null,token,school:feed.Id);
        Assert.Equal(2,page.Total); Assert.All(page.Items,item=> { Assert.False(item.IsAway); Assert.Equal("MLS",GameGroups.ScheduleLeague(item)); });
        var onDay=Assert.Single(await EventsQuery.GetOnDateAsync(db,day), e=>e.Title.Contains(token));
        Assert.False(onDay.IsAway);Assert.Equal("espn-mls",onDay.ProviderKey);
        feed.Enabled=false;await db.SaveChangesAsync();
        Assert.Empty((await EventsQuery.GetUpcomingAsync(db,day,null,token,school:feed.Id)).Items);
    }

    [Fact]
    public async Task Professional_schedules_filter_status_dates_and_provider_identity()
    {
        await using var db = Create();
        await using var tx = await BeginScheduleTransactionAsync(db);
        var token = Tag();
        var day = new DateOnly(2031, 3, 4);
        var nwsl = await db.SportsEventsTypes.SingleOrDefaultAsync(f => f.ProviderKey == "espn-nwsl" && f.ProviderSchoolId == 20907) ?? new SportsEventsType { EventsTypeName = token, RssUrl = $"https://test.invalid/{token}/nwsl",
            ProviderKey = "espn-nwsl", ProviderSchoolId = 20907, Enabled = true };
        var wnba = await db.SportsEventsTypes.SingleOrDefaultAsync(f => f.ProviderKey == "espn-wnba" && f.ProviderSchoolId == 0) ?? new SportsEventsType { EventsTypeName = token, RssUrl = $"https://test.invalid/{token}/wnba",
            ProviderKey = "espn-wnba", ProviderSchoolId = 0, Enabled = true };
        foreach (var status in new[] { "STATUS_SCHEDULED", "STATUS_POSTPONED", "STATUS_DELAYED", "STATUS_SUSPENDED",
            "STATUS_CANCELED", "STATUS_CANCELLED", "STATUS_FULL_TIME", "STATUS_FINAL", "STATUS_FINAL_PEN" })
        {
            var game = Game(token, status, day);
            game.ProviderKey = "espn-nwsl";
            game.SportsEventsType = nwsl;
            game.ScheduleStatus = status;
            game.SchoolName = "Kansas City Current";
            db.Add(game);
        }
        var leagueGame = Game(token, "wnba", day.AddDays(1), new DateTime(2031, 3, 6, 1, 0, 0));
        leagueGame.ProviderKey = "espn-wnba";
        leagueGame.SportsEventsType = wnba;
        leagueGame.SchoolName = "Home team";
        leagueGame.Opponent = "Away team";
        leagueGame.IsAway = false;
        leagueGame.SchoolId = 3; // An ESPN ID must not resolve to a Big 12 school.
        db.Add(leagueGame);
        db.AddRange(Game(token, "yesterday", day.AddDays(-1)), Game(token, "outside range", day.AddDays(2)));
        await db.SaveChangesAsync();

        var page = await EventsQuery.GetUpcomingAsync(db, day, null, token, from: day.AddDays(-1), to: day.AddDays(1));
        Assert.Equal(5, page.Total);
        Assert.DoesNotContain(page.Items, e => e.ScheduleStatus is not null &&
            (e.ScheduleStatus.Contains("CANCEL") || e.ScheduleStatus.Contains("FINAL") || e.ScheduleStatus == "STATUS_FULL_TIME"));
        Assert.Equal("Home team", Assert.Single(page.Items, e => e.Id == leagueGame.Id).SchoolName);
        Assert.Single((await EventsQuery.GetUpcomingAsync(db, day, null, token, school: wnba.Id)).Items);
        Assert.Equal(4, (await EventsQuery.GetOnDateAsync(db, day)).Count(e => e.Title.Contains(token)));
        var sources = await EventsQuery.GetSchoolsAsync(db);
        Assert.Equal("WNBA", sources.Single(e => e.Id == wnba.Id).Name);
        Assert.Equal("Kansas City Current", sources.Single(e => e.Id == nwsl.Id).Name);
        Assert.Empty((await EventsQuery.GetUpcomingAsync(db, day, null, token, from: day.AddDays(3), to: day)).Items);
        wnba.Enabled = false;
        nwsl.ProviderSchoolId = 3;
        await db.SaveChangesAsync();
        Assert.Empty((await EventsQuery.GetUpcomingAsync(db, day, null, token, to: day.AddDays(1))).Items);
    }

    [Theory]
    [InlineData("2026-10-10T00:30:00", "2026-10-09")]
    [InlineData("2026-11-01T05:30:00", "2026-11-01")]
    [InlineData("2026-11-02T05:30:00", "2026-11-01")]
    public void Central_date_boundaries_respect_midnight_and_dst(string utc, string expected) =>
        Assert.Equal(DateOnly.Parse(expected), FeedDates.Create("America/Chicago").TodayAt(DateTime.Parse(utc)));

    [Fact]
    public void Schedule_links_preserve_the_selected_date_range() =>
        Assert.Equal("?sport=Basketball&from=2026-10-09&to=2026-10-16&school=7",
            EventsQuery.QueryString("Basketball", null, 7, new DateOnly(2026, 10, 9), new DateOnly(2026, 10, 16)));

    [Fact]
    public async Task Mlb_has_a_schedule_choice_and_filters_to_its_enabled_league_feed()
    {
        await using var db = Create();
        await using var tx = await BeginScheduleTransactionAsync(db);
        var token = Tag();
        var day = new DateOnly(2031, 3, 4);
        var feed = await db.SportsEventsTypes.SingleOrDefaultAsync(f => f.ProviderKey == "espn-mlb" && f.ProviderSchoolId == 0) ?? new SportsEventsType { EventsTypeName = "MLB", RssUrl = $"https://test.invalid/{token}/mlb",
            ProviderKey = "espn-mlb", ProviderSchoolId = 0, Enabled = true };
        var game = Game(token, "mlb", day, sport: "Baseball");
        game.ProviderKey = "espn-mlb";
        game.SportsEventsType = feed;
        game.SchoolName = "Home baseball team";
        game.Opponent = "Away baseball team";
        game.IsAway = false;
        db.Add(game);
        db.Add(Game(token, "school", day));
        await db.SaveChangesAsync();
        Assert.Equal("MLB", (await EventsQuery.GetSchoolsAsync(db)).Single(f => f.Id == feed.Id).Name);
        var selected = await EventsQuery.GetUpcomingAsync(db, day, null, token, school: feed.Id);
        Assert.Equal(game.Id, Assert.Single(selected.Items).Id);
        feed.Enabled = false;
        await db.SaveChangesAsync();
        Assert.DoesNotContain(await EventsQuery.GetSchoolsAsync(db), f => f.Id == feed.Id);
        Assert.Empty((await EventsQuery.GetUpcomingAsync(db, day, null, token, school: feed.Id)).Items);
    }

    [Theory]
    [InlineData("Football")]
    [InlineData("Soccer")]
    [InlineData("Volleyball")]
    [InlineData("Men's Basketball")]
    [InlineData("Women's Basketball")]
    public async Task Upcoming_college_games_and_sport_options_survive_past_fixture_retention(string sport)
    {
        await using var db = Create();
        await using var tx = await BeginScheduleTransactionAsync(db);
        var token = Tag(); var day = new DateOnly(2031, 10, 11);
        var feed = new SportsEventsType { EventsTypeName = token, RssUrl = $"https://{token}.test.invalid/rss", SchoolName = token, ProviderKey = "big12", Enabled = true };
        var first = Game(token, "opening", day.AddDays(1), sport: sport);
        var later = Game(token, "later", day.AddDays(8), sport: sport);
        var old = Game(token, "past", day.AddDays(-1), sport: sport);
        foreach (var game in new[] { first, later, old }) { game.ProviderKey = "big12"; game.SportsEventsType = feed; }
        db.AddRange(first, later, old); await db.SaveChangesAsync();
        // Women's basketball is intentionally hidden across public schedules.
        if (sport == "Women's Basketball")
        {
            Assert.Empty((await EventsQuery.GetUpcomingAsync(db, day, sport, token, school: feed.Id)).Items);
            Assert.DoesNotContain(sport, await EventsQuery.GetSportsAsync(db, day, feed.Id));
            return;
        }
        Assert.Equal(2, (await EventsQuery.GetUpcomingAsync(db, day, null, token, school: feed.Id)).Total);
        db.Remove(old); await db.SaveChangesAsync();
        var page = await EventsQuery.GetUpcomingAsync(db, day, sport, token, school: feed.Id);
        Assert.Equal(new[] { first.Id, later.Id }, page.Items.Select(i => i.Id));
        Assert.Contains(sport, await EventsQuery.GetSportsAsync(db, day, feed.Id));
        feed.Enabled = false; await db.SaveChangesAsync();
        Assert.Empty((await EventsQuery.GetUpcomingAsync(db, day, sport, token, school: feed.Id)).Items);
        Assert.Empty(await EventsQuery.GetSportsAsync(db, day, feed.Id));
    }

}
