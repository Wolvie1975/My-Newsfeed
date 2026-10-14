using System.Globalization;

namespace MyNewsFeed.Web.Data;

/// <summary>
/// Turns stored UTC times into what people see. Every stored time is UTC and is converted here, at display time, into
/// one configured zone (Display:TimeZone, America/Chicago for this site); nothing converted is ever written back.
/// The calendar day an article falls on is decided in that zone, so every visitor sees the same date.
/// </summary>
public sealed class FeedDates(TimeZoneInfo zone)
{
    private static readonly CultureInfo Culture = CultureInfo.InvariantCulture;

    /// <summary>Day key for articles that have no publish date; they are grouped together, at the end.</summary>
    public const string UndatedKey = "undated";

    public TimeZoneInfo Zone => zone;

    /// <summary>Reads the zone from configuration. An unknown or empty id falls back to UTC.</summary>
    public static FeedDates Create(string? timeZoneId)
    {
        var zone = TimeZoneInfo.Utc;
        if (!string.IsNullOrWhiteSpace(timeZoneId))
        {
            try { zone = TimeZoneInfo.FindSystemTimeZoneById(timeZoneId.Trim()); }
            catch (Exception ex) when (ex is TimeZoneNotFoundException or InvalidTimeZoneException) { }
        }

        return new FeedDates(zone);
    }

    // SQL Server hands back DateTimeKind.Unspecified, which .NET would treat as local time and shift wrongly,
    // so every value is marked as UTC before it is converted.
    private static DateTime AsUtc(DateTime value) => DateTime.SpecifyKind(value, DateTimeKind.Utc);

    private DateTime Local(DateTime utc) => TimeZoneInfo.ConvertTimeFromUtc(AsUtc(utc), zone);

    private string OffsetText(DateTime utc)
    {
        var offset = zone.GetUtcOffset(AsUtc(utc));
        var sign = offset < TimeSpan.Zero ? "-" : "+";
        var hours = Math.Abs(offset.Hours);
        var minutes = Math.Abs(offset.Minutes);
        return minutes == 0 ? $"UTC{sign}{hours}" : $"UTC{sign}{hours}:{minutes:00}";
    }

    /// <summary>Today's calendar date in the display zone, given the current UTC time.</summary>
    public DateOnly TodayAt(DateTime utcNow) => DateOnly.FromDateTime(Local(utcNow));

    public DateOnly Today => TodayAt(DateTime.UtcNow);

    /// <summary>The calendar day (in the display zone) an article belongs to, as yyyy-MM-dd.</summary>
    public string DayKey(DateTime utc) => Local(utc).ToString("yyyy-MM-dd", Culture);

    /// <summary>The day an article is grouped under; articles with no publish date share <see cref="UndatedKey"/>.</summary>
    public string DayKey(FeedItem item) => item.Undated ? UndatedKey : DayKey(item.Date);

    /// <summary>The date an article was published, for example "Sep 20, 2026".</summary>
    public string Published(DateTime utc) => Local(utc).ToString("MMM d, yyyy", Culture);

    /// <summary>The time of day an article was published, for example "9:52 AM".</summary>
    public string Time(DateTime utc) => Local(utc).ToString("h:mm tt", Culture);

    /// <summary>The time of day with its UTC offset, for example "2:00 PM (UTC-5)". Not the venue's clock.</summary>
    public string TimeWithOffset(DateTime utc) => $"{Time(utc)} ({OffsetText(utc)})";

    /// <summary>The full moment with its UTC offset, for tooltips and the "last updated" line: "Sep 20, 2026, 9:52 AM (UTC-5)".</summary>
    public string PublishedFull(DateTime utc) => $"{Local(utc).ToString("MMM d, yyyy, h:mm tt", Culture)} ({OffsetText(utc)})";

    /// <summary>Short label for the display zone ("CT", "ET", "MT", "PT", "UTC"), or null when it has no everyday abbreviation.</summary>
    public string? ZoneLabel => zone.Id switch
    {
        "America/Chicago" or "Central Standard Time" => "CT",
        "America/New_York" or "Eastern Standard Time" => "ET",
        "America/Denver" or "Mountain Standard Time" => "MT",
        "America/Los_Angeles" or "Pacific Standard Time" => "PT",
        "UTC" or "Etc/UTC" => "UTC",
        _ => null,
    };

    /// <summary>
    /// A game's start time in the display zone, in the schedule style "6:00 P.M. CT". The label names the zone the time
    /// is shown in (yours), not the venue's clock.
    /// </summary>
    public string EventTime(DateTime utc)
    {
        var local = Local(utc);
        var hour = local.Hour % 12 == 0 ? 12 : local.Hour % 12;
        var half = local.Hour < 12 ? "A.M." : "P.M.";
        return $"{hour}:{local.Minute:00} {half} {ZoneLabel ?? OffsetText(utc)}";
    }

    /// <summary>"Good morning", "Good afternoon" or "Good evening" by the hour in the display zone.</summary>
    public string Greeting(DateTime utcNow) => Local(utcNow).Hour switch
    {
        < 12 => "Good morning",
        < 18 => "Good afternoon",
        _ => "Good evening",
    };

    /// <summary>Today's date for the home page heading, in the display zone: "Monday, September 21".</summary>
    public string TodayLabel(DateTime utcNow) => Local(utcNow).ToString("dddd, MMMM d", Culture);

    /// <summary>A calendar date written out for a heading, for example "Friday, September 25, 2026".</summary>
    public static string LongDate(DateOnly date) => date.ToString("dddd, MMMM d, yyyy", Culture);

    /// <summary>A timestamp for admin screens: "2026-09-20 09:52 (UTC-5)", or a dash when there is no value.</summary>
    public string Stamp(DateTime? utc) =>
        utc is null ? "—" : $"{Local(utc.Value).ToString("yyyy-MM-dd HH:mm", Culture)} ({OffsetText(utc.Value)})";

    /// <summary>The heading over a group of articles from one day, for example "Sun Sep 20, 2026".</summary>
    public string GroupLabel(string dayKey)
    {
        if (dayKey == UndatedKey)
        {
            return "Date unknown";
        }

        return DateOnly.TryParseExact(dayKey, "yyyy-MM-dd", Culture, DateTimeStyles.None, out var day)
            ? day.ToString("ddd MMM d, yyyy", Culture)
            : dayKey;
    }
}
