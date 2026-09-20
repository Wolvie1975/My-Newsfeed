using System.Globalization;

namespace MyNewsFeed.Web.Data;

/// <summary>
/// Turns stored UTC publish dates into the dates the feed shows ("Sep 20, 2026"). Which calendar day an article
/// falls on is decided in one configured time zone (Display:TimeZone), so every visitor sees the same date.
/// </summary>
public sealed class FeedDates(TimeZoneInfo zone)
{
    private static readonly CultureInfo Culture = CultureInfo.InvariantCulture;

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

    private static DateTime AsUtc(DateTime value) => DateTime.SpecifyKind(value, DateTimeKind.Utc);

    private DateTime Local(DateTime utc) => TimeZoneInfo.ConvertTimeFromUtc(AsUtc(utc), zone);

    /// <summary>The calendar day (in the display zone) an article belongs to, as yyyy-MM-dd.</summary>
    public string DayKey(DateTime utc) => Local(utc).ToString("yyyy-MM-dd", Culture);

    /// <summary>The date an article was published, for example "Sep 20, 2026".</summary>
    public string Published(DateTime utc) => Local(utc).ToString("MMM d, yyyy", Culture);

    /// <summary>The time of day an article was published, for example "9:52 AM".</summary>
    public string Time(DateTime utc) => Local(utc).ToString("h:mm tt", Culture);

    /// <summary>The full moment with its UTC offset, for tooltips and the "last updated" line: "Sep 20, 2026, 9:52 AM (UTC-5)".</summary>
    public string PublishedFull(DateTime utc)
    {
        var offset = zone.GetUtcOffset(AsUtc(utc));
        var sign = offset < TimeSpan.Zero ? "-" : "+";
        var hours = Math.Abs(offset.Hours);
        var minutes = Math.Abs(offset.Minutes);
        var text = minutes == 0 ? $"UTC{sign}{hours}" : $"UTC{sign}{hours}:{minutes:00}";
        return $"{Local(utc).ToString("MMM d, yyyy, h:mm tt", Culture)} ({text})";
    }

    /// <summary>The heading over a group of articles from one day, for example "Sun Sep 20, 2026".</summary>
    public string GroupLabel(string dayKey) =>
        DateOnly.TryParseExact(dayKey, "yyyy-MM-dd", Culture, DateTimeStyles.None, out var day)
            ? day.ToString("ddd MMM d, yyyy", Culture)
            : dayKey;
}
