using Microsoft.EntityFrameworkCore;

namespace MyNewsFeed.Web.Data;

public sealed record EventItem(
    int Id,
    string Url,
    string Title,
    string? Sport,
    string? Opponent,
    bool? IsAway,
    string? Location,
    DateOnly EventDate,
    DateTime? StartsAtUtc,
    bool TimeTbd,
    string? Tv,
    string? StreamUrl,
    string? LiveStatsUrl,
    string? TeamLogoUrl,
    string? OpponentLogoUrl,
    string? SchoolName);

/// <param name="Total">Every upcoming event matching the filters (the list itself is capped).</param>
public sealed record EventsPage(IReadOnlyList<EventItem> Items, int Total);

/// <summary>Read side of the public sports events page.</summary>
public static class EventsQuery
{
    /// <summary>A safety cap on one page; a season is far smaller than this.</summary>
    public const int Limit = 200;

    /// <summary>
    /// The sports that have games from <paramref name="today"/> onward, for the filter. Games are grouped by
    /// <see cref="SportsEvent.EventDate"/>, which is already the host school's calendar date, so "today" is compared
    /// with it directly and never converted.
    /// </summary>
    public static async Task<IReadOnlyList<string>> GetSportsAsync(WebScraperContext db, DateOnly today) =>
        await db.SportsEvents.AsNoTracking()
            .Where(e => e.EventDate >= today && e.Sport != null)
            .Select(e => e.Sport!)
            .Distinct()
            .OrderBy(s => s)
            .ToListAsync();

    /// <summary>When the schedule was last refreshed by the scraper, or null when there are no events.</summary>
    public static async Task<DateTime?> GetLastUpdatedAsync(WebScraperContext db) =>
        await db.SportsEvents.AsNoTracking().MaxAsync(e => (DateTime?)e.LastSeenAt);

    /// <summary>
    /// Games from <paramref name="today"/> onward (today's finished games included), soonest first; within a day, games
    /// with a known start time come before games whose time is still to be announced.
    /// </summary>
    public static async Task<EventsPage> GetUpcomingAsync(
        WebScraperContext db, DateOnly today, string? sport, string? search, int limit = Limit)
    {
        var query = db.SportsEvents.AsNoTracking().Where(e => e.EventDate >= today);

        if (!string.IsNullOrWhiteSpace(sport))
        {
            var wanted = sport.Trim();
            query = query.Where(e => e.Sport == wanted);
        }

        var term = search?.Trim();
        if (!string.IsNullOrEmpty(term))
        {
            query = query.Where(e => e.Title.Contains(term)
                || (e.Opponent != null && e.Opponent.Contains(term))
                || (e.Location != null && e.Location.Contains(term))
                || (e.Sport != null && e.Sport.Contains(term))
                || (e.SportsEventsType != null && e.SportsEventsType.SchoolName != null
                    && e.SportsEventsType.SchoolName.Contains(term)));
        }

        var total = await query.CountAsync();
        var items = await query
            .OrderBy(e => e.EventDate)
            .ThenBy(e => e.StartsAtUtc == null ? 1 : 0)
            .ThenBy(e => e.StartsAtUtc)
            .ThenBy(e => e.Sport)
            .ThenBy(e => e.Id)
            .Take(limit)
            .Select(e => new EventItem(
                e.Id, e.Url, e.Title, e.Sport, e.Opponent, e.IsAway, e.Location, e.EventDate, e.StartsAtUtc, e.TimeTbd,
                e.Tv, e.StreamUrl, e.LiveStatsUrl, e.TeamLogoUrl, e.OpponentLogoUrl,
                e.SportsEventsType != null ? e.SportsEventsType.SchoolName : null))
            .ToListAsync();

        return new EventsPage(items, total);
    }

    /// <summary>Builds the query string that keeps the current filters. Empty when nothing is set.</summary>
    public static string QueryString(string? sport, string? search)
    {
        var parts = new List<string>();
        if (!string.IsNullOrWhiteSpace(sport))
        {
            parts.Add($"sport={Uri.EscapeDataString(sport.Trim())}");
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            parts.Add($"q={Uri.EscapeDataString(search.Trim())}");
        }

        return parts.Count == 0 ? "" : "?" + string.Join("&", parts);
    }
}

/// <summary>Display-time text helpers for the events page. The stored data is never changed.</summary>
public static class EventsText
{
    /// <summary>
    /// A logo address safe to load: http(s) only, and http upgraded to https (some logos are stored as http, which
    /// browsers block on an https site). A logo that then fails to load falls back to the initials badge.
    /// </summary>
    public static string? LogoUrl(string? url)
    {
        var safe = FeedQuery.SafeHref(url);
        return safe is not null && safe.StartsWith("http://", StringComparison.OrdinalIgnoreCase)
            ? "https://" + safe["http://".Length..]
            : safe;
    }

    /// <summary>
    /// Splits "Lawrence, Kan. / David Booth Kansas Memorial Stadium" into the city and the venue. The venue is null when
    /// the location has no " / ".
    /// </summary>
    public static (string? City, string? Venue) SplitLocation(string? location)
    {
        if (string.IsNullOrWhiteSpace(location))
        {
            return (null, null);
        }

        var parts = location.Split(" / ", 2, StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
        return parts.Length == 2 ? (parts[0], parts[1]) : (parts.Length == 1 ? parts[0] : null, null);
    }

    /// <summary>Two or three letters for a team badge when it has no logo: "Grand Canyon" gives "GC", "Kansas" gives "KAN".</summary>
    public static string Initials(string? name)
    {
        var words = (name ?? "").Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(w => w.Where(char.IsLetterOrDigit).ToArray())
            .Where(w => w.Length > 0)
            .ToList();

        if (words.Count == 0)
        {
            return "?";
        }

        var text = words.Count == 1
            ? new string(words[0].Take(3).ToArray())
            : new string(words.Take(3).Select(w => w[0]).ToArray());
        return text.ToUpperInvariant();
    }
}
