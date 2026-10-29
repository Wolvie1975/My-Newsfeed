using myNewspaper.Web.Data;

namespace myNewspaper.Tests;

/// <summary>Hour and day headings for the All headlines cards (docs/headline-cards-handoff.md), in Central time.</summary>
public class HeadlineCardGroupTests
{
    private static readonly FeedDates Dates = FeedDates.Create("America/Chicago");
    // 9:30 AM CDT on Thursday, October 29, 2026.
    private static readonly DateTime Now = new(2026, 10, 29, 14, 30, 0, DateTimeKind.Utc);

    private static FeedItem At(DateTime utc, bool undated = false) =>
        new(1, "Story", "https://example.com", null, null, null, "https://example.com", null, null, utc, undated);

    [Fact]
    public void Hour_mode_groups_today_by_hour_and_earlier_days_by_day()
    {
        Assert.Equal("h-2026-10-29-09", Dates.CardGroupKey(At(Now.AddMinutes(-20)), "hour", Now));
        Assert.Equal("9 AM hour", Dates.CardGroupLabel(At(Now.AddMinutes(-20)), "hour", Now));
        Assert.Equal("12 AM hour", Dates.CardGroupLabel(At(new DateTime(2026, 10, 29, 5, 10, 0, DateTimeKind.Utc)), "hour", Now));
        // 11:50 PM on the 28th, Central: yesterday's day group, not an hour.
        var lateYesterday = At(new DateTime(2026, 10, 29, 4, 50, 0, DateTimeKind.Utc));
        Assert.Equal("d-2026-10-28", Dates.CardGroupKey(lateYesterday, "hour", Now));
        Assert.Equal("Yesterday · Wed, Oct 28", Dates.CardGroupLabel(lateYesterday, "hour", Now));
    }

    [Fact]
    public void Day_mode_labels_today_yesterday_and_older_days()
    {
        Assert.Equal("Today", Dates.CardGroupLabel(At(Now.AddHours(-1)), "day", Now));
        Assert.Equal("d-2026-10-29", Dates.CardGroupKey(At(Now.AddHours(-1)), "day", Now));
        Assert.Equal("Mon, Oct 26", Dates.CardGroupLabel(At(Now.AddDays(-3)), "day", Now));
        Assert.Equal("Sat, Dec 27, 2025", Dates.CardGroupLabel(At(new DateTime(2025, 12, 27, 18, 0, 0, DateTimeKind.Utc)), "day", Now));
    }

    [Fact]
    public void Undated_stories_share_one_group()
    {
        Assert.Equal("d-undated", Dates.CardGroupKey(At(Now, undated: true), "hour", Now));
        Assert.Equal("Date unknown", Dates.CardGroupLabel(At(Now, undated: true), "day", Now));
    }
}
