using System.Globalization;

namespace MyNewsFeed.Web.Data;

/// <summary>
/// Turns stored UTC publish dates into the labels the feed shows ("Today", "Yesterday", "Sep 18"). "Today" is
/// decided in one configured time zone (Display:TimeZone), so every visitor sees the same labels.
/// </summary>
public sealed class FeedDates(TimeZoneInfo zone, TimeProvider clock)
{
    private static readonly CultureInfo Culture = CultureInfo.InvariantCulture;

    public TimeZoneInfo Zone => zone;

    /// <summary>Reads the zone from configuration. An unknown or empty id falls back to UTC.</summary>
    public static FeedDates Create(string? timeZoneId, TimeProvider clock)
    {
        var zone = TimeZoneInfo.Utc;
        if (!string.IsNullOrWhiteSpace(timeZoneId))
        {
            try { zone = TimeZoneInfo.FindSystemTimeZoneById(timeZoneId.Trim()); }
            catch (Exception ex) when (ex is TimeZoneNotFoundException or InvalidTimeZoneException) { }
        }

        return new FeedDates(zone, clock);
    }

    private DateOnly Local(DateTime utc) =>
        DateOnly.FromDateTime(TimeZoneInfo.ConvertTimeFromUtc(DateTime.SpecifyKind(utc, DateTimeKind.Utc), zone));

    public DateOnly Today => DateOnly.FromDateTime(TimeZoneInfo.ConvertTime(clock.GetUtcNow(), zone).DateTime);

    /// <summary>The calendar day (in the display zone) an article belongs to, as yyyy-MM-dd.</summary>
    public string DayKey(DateTime utc) => Local(utc).ToString("yyyy-MM-dd", Culture);

    /// <summary>"Today", "Yesterday", or a short date such as "Sep 18" (with the year when it isn't this year).</summary>
    public string Short(DateTime utc)
    {
        var day = Local(utc);
        var today = Today;
        if (day == today) return "Today";
        if (day == today.AddDays(-1)) return "Yesterday";
        return day.ToString(day.Year == today.Year ? "MMM d" : "MMM d, yyyy", Culture);
    }

    /// <summary>The heading over a group of articles: "Today · Sun Sep 20", "Yesterday · Sat Sep 19", "Fri Sep 18".</summary>
    public string GroupLabel(string dayKey)
    {
        if (!DateOnly.TryParseExact(dayKey, "yyyy-MM-dd", Culture, DateTimeStyles.None, out var day))
        {
            return dayKey;
        }

        var today = Today;
        var text = day.ToString(day.Year == today.Year ? "ddd MMM d" : "ddd MMM d, yyyy", Culture);
        if (day == today) return $"Today · {text}";
        if (day == today.AddDays(-1)) return $"Yesterday · {text}";
        return text;
    }
}
