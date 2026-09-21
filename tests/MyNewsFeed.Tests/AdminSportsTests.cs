using Microsoft.EntityFrameworkCore;
using MyNewsFeed.Web.Data;
using MyNewsFeed.Web.Data.Models;

namespace MyNewsFeed.Tests;

public class AdminSportsTests
{
    // ---- pure logic: no database ------------------------------------------------------------------------------------

    [Theory]
    [InlineData("https://big12sports.com/services/x.ashx/calendar.rss?sport_id=0", "big12sports.com")]
    [InlineData("http://www.Big12Sports.com/calendar.aspx?id=182068", "big12sports.com")]
    [InlineData("https://sub.big12sports.com/a", "sub.big12sports.com")]
    [InlineData("ftp://files.example.test/x", "files.example.test")]
    [InlineData("not a url", null)]
    [InlineData("/relative/path", null)]
    [InlineData("", null)]
    [InlineData(null, null)]
    public void HostOf_gives_the_website_without_www(string? url, string? expected) =>
        Assert.Equal(expected, AdminData.HostOf(url));

    [Fact]
    public void An_event_is_linked_when_exactly_one_type_is_on_its_website()
    {
        var plan = AdminData.PlanSportsEventLinks(
            unlinkedEvents: [(1, "http://big12sports.com/calendar.aspx?id=1"), (2, "https://www.big12sports.com/calendar.aspx?id=2")],
            types: [(10, "https://big12sports.com/services/calendar.rss?sport_id=0")]);

        Assert.Equal(new Dictionary<int, int> { [1] = 10, [2] = 10 }, plan);   // http vs https and www make no difference
    }

    [Fact]
    public void A_website_with_several_feeds_is_never_guessed()
    {
        var plan = AdminData.PlanSportsEventLinks(
            unlinkedEvents: [(1, "http://big12sports.com/calendar.aspx?id=1")],
            types: [(10, "https://big12sports.com/calendar.rss?sport_id=1"), (11, "https://big12sports.com/calendar.rss?sport_id=2")]);

        Assert.Empty(plan);
    }

    [Fact]
    public void Events_on_other_websites_and_unreadable_addresses_are_left_alone()
    {
        var plan = AdminData.PlanSportsEventLinks(
            unlinkedEvents: [(1, "http://other.example.test/game/1"), (2, "garbage"), (3, "http://big12sports.com/g/3")],
            types: [(10, "https://big12sports.com/calendar.rss"), (11, "also garbage")]);

        Assert.Equal(new Dictionary<int, int> { [3] = 10 }, plan);
    }

    [Fact]
    public void Different_websites_each_get_their_own_type()
    {
        var plan = AdminData.PlanSportsEventLinks(
            [(1, "http://a.example.test/1"), (2, "http://b.example.test/2")],
            [(10, "https://a.example.test/rss"), (11, "https://b.example.test/rss")]);

        Assert.Equal(new Dictionary<int, int> { [1] = 10, [2] = 11 }, plan);
    }

    [Fact]
    public void Nothing_to_link_gives_an_empty_plan()
    {
        Assert.Empty(AdminData.PlanSportsEventLinks([], [(10, "https://a.example.test/rss")]));
        Assert.Empty(AdminData.PlanSportsEventLinks([(1, "http://a.example.test/1")], []));
    }

    // ---- against the database (each test rolls back) -----------------------------------------------------------------

    private static WebScraperContext Create() => new(
        new DbContextOptionsBuilder<WebScraperContext>()
            .UseSqlServer("Server=sql2025,1433;Database=WebScraper;User Id=sa;Password=Fedora_Dev_2025!;Encrypt=False;TrustServerCertificate=True;")
            .Options);

    private static string Tag() => Guid.NewGuid().ToString("N")[..10];

    private static SportsEvent Event(string host, int n, SportsEventsType? type = null) => new()
    {
        Url = $"http://{host}/calendar.aspx?id={n}",
        Title = $"zz test game {n}",
        Sport = "Volleyball",
        EventDate = new DateOnly(2026, 12, 1).AddDays(n),
        TimeTbd = true,
        SportsEventsType = type,
    };

    private static SportsEventsType Type(string host, string name = "Test type") =>
        new() { EventsTypeName = name, RssUrl = $"https://{host}/services/calendar.rss?sport_id=0" };

    [Fact]
    public async Task Linking_by_website_attaches_matching_events_and_repeats_safely()
    {
        await using var db = Create();
        await using var tx = await db.Database.BeginTransactionAsync();
        var host = $"e{Tag()}.test.invalid";
        var type = Type(host);
        var mine = new[] { Event(host, 1), Event(host, 2) };
        var elsewhere = Event($"other{Tag()}.test.invalid", 3);
        var alreadyLinked = Event(host, 4, Type($"x{Tag()}.test.invalid", "Chosen by hand"));
        db.SportsEventsTypes.Add(type);
        db.SportsEvents.AddRange(mine);
        db.SportsEvents.AddRange(elsewhere, alreadyLinked);
        await db.SaveChangesAsync();

        var linked = await AdminData.LinkSportsEventsBySiteAsync(db);

        Assert.True(linked >= 2);
        Assert.All(mine, e => Assert.Equal(type.Id, db.SportsEvents.AsNoTracking().Single(x => x.Id == e.Id).SportsEventsTypeId));
        Assert.Null(db.SportsEvents.AsNoTracking().Single(x => x.Id == elsewhere.Id).SportsEventsTypeId);
        Assert.Equal(alreadyLinked.SportsEventsTypeId, db.SportsEvents.AsNoTracking().Single(x => x.Id == alreadyLinked.Id).SportsEventsTypeId);

        Assert.Equal(0, await AdminData.LinkSportsEventsBySiteAsync(db));   // nothing left to do
    }

    [Fact]
    public async Task Linking_skips_a_website_that_has_two_types()
    {
        await using var db = Create();
        await using var tx = await db.Database.BeginTransactionAsync();
        var host = $"e{Tag()}.test.invalid";
        var evt = Event(host, 1);
        db.SportsEventsTypes.AddRange(Type(host, "One"), new SportsEventsType { EventsTypeName = "Two", RssUrl = $"https://{host}/other.rss" });
        db.SportsEvents.Add(evt);
        await db.SaveChangesAsync();

        await AdminData.LinkSportsEventsBySiteAsync(db);

        Assert.Null(db.SportsEvents.AsNoTracking().Single(x => x.Id == evt.Id).SportsEventsTypeId);
    }

    [Fact]
    public async Task A_type_without_events_can_be_deleted()
    {
        await using var db = Create();
        await using var tx = await db.Database.BeginTransactionAsync();
        var type = Type($"e{Tag()}.test.invalid");
        db.SportsEventsTypes.Add(type);
        await db.SaveChangesAsync();

        Assert.True(await AdminData.DeleteSportsEventsTypeAsync(db, type.Id, includeEvents: false));
        Assert.False(await AdminData.DeleteSportsEventsTypeAsync(db, type.Id, includeEvents: false));   // already gone
    }

    [Fact]
    public async Task A_type_with_events_is_protected_unless_its_events_are_deleted_too()
    {
        await using var db = Create();
        await using var tx = await db.Database.BeginTransactionAsync();
        var host = $"e{Tag()}.test.invalid";
        var type = Type(host);
        var mine = new[] { Event(host, 1, type), Event(host, 2, type) };
        var bystander = Event($"b{Tag()}.test.invalid", 3);
        db.SportsEvents.AddRange(mine);
        db.SportsEvents.Add(bystander);
        await db.SaveChangesAsync();

        var blocked = await Assert.ThrowsAnyAsync<Exception>(() => AdminData.DeleteSportsEventsTypeAsync(db, type.Id, includeEvents: false));
        Assert.True(AdminData.IsForeignKeyViolation(blocked));
        Assert.Equal(2, await db.SportsEvents.CountAsync(e => e.SportsEventsTypeId == type.Id));

        Assert.True(await AdminData.DeleteSportsEventsTypeAsync(db, type.Id, includeEvents: true));
        Assert.False(await db.SportsEventsTypes.AnyAsync(t => t.Id == type.Id));
        Assert.Equal(0, await db.SportsEvents.CountAsync(e => e.SportsEventsTypeId == type.Id));
        Assert.True(await db.SportsEvents.AnyAsync(e => e.Id == bystander.Id));
    }

    [Fact]
    public async Task The_same_feed_cannot_be_added_twice()
    {
        await using var db = Create();
        await using var tx = await db.Database.BeginTransactionAsync();
        var host = $"e{Tag()}.test.invalid";
        db.SportsEventsTypes.Add(Type(host));
        await db.SaveChangesAsync();

        db.SportsEventsTypes.Add(Type(host, "A second name for the same feed"));
        var ex = await Assert.ThrowsAsync<DbUpdateException>(() => db.SaveChangesAsync());
        Assert.True(AdminData.IsUniqueViolation(ex));
    }

    [Fact]
    public async Task No_test_rows_are_left_in_the_sports_tables()
    {
        await using var db = Create();
        Assert.False(await db.SportsEvents.AnyAsync(e => e.Url.Contains("test.invalid")));
        Assert.False(await db.SportsEventsTypes.AnyAsync(t => t.RssUrl.Contains("test.invalid")));
    }
}
