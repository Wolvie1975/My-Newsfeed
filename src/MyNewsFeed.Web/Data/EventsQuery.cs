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
    string? SchoolName,
    bool? IsNeutral = null, string? Radio = null, string? AudioUrl = null, string? ScheduleStatus = null, string? ProviderKey = null);

/// <param name="Total">Every upcoming event matching the filters (the list itself is capped).</param>
public sealed record EventSchool(int Id, string Name);

public sealed record EventsPage(IReadOnlyList<EventItem> Items, int Total);

/// <summary>Read side of the public sports events page.</summary>
public static class EventsQuery
{
    private static IQueryable<Models.SportsEvent> Visible(WebScraperContext db) => db.SportsEvents.AsNoTracking()
        .Where(e => e.ScheduleStatus == null ||
            (e.ScheduleStatus != "STATUS_CANCELED" && e.ScheduleStatus != "STATUS_CANCELLED"
             && e.ScheduleStatus != "STATUS_FULL_TIME" && !e.ScheduleStatus.StartsWith("STATUS_FINAL")))
        .Where(e => e.ProviderKey == "espn-nwsl" || e.ProviderKey == "espn-wnba" || e.ProviderKey == "espn-mlb"
            ? e.SportsEventsType != null && e.SportsEventsType.Enabled
                && e.SportsEventsType.ProviderKey == e.ProviderKey
                && (e.ProviderKey == "espn-nwsl" ? e.SportsEventsType.ProviderSchoolId == 20907
                    : e.SportsEventsType.ProviderSchoolId == 0)
            : e.EventSources.Any(s => s.Feed.Enabled)
                || (!e.EventSources.Any() && (e.SportsEventsType == null || e.SportsEventsType.Enabled)));

    private static IQueryable<Models.SportsEvent> InStartedSeasons(WebScraperContext db, DateOnly today)
    {
        var calendar = CollegeSeasons.Start(null, today);
        var basketball = CollegeSeasons.Start("Basketball", today);
        return Visible(db).Where(e => e.ProviderKey != "big12" || db.SportsEvents.Any(first =>
            first.ProviderKey == "big12" && first.Sport == e.Sport && first.EventDate <= today
            && first.EventDate >= ((e.Sport == "Men's Basketball" || e.Sport == "Women's Basketball") ? basketball : calendar)
            && (first.ScheduleStatus == null || (first.ScheduleStatus != "STATUS_CANCELED" && first.ScheduleStatus != "STATUS_CANCELLED"))));
    }

    public static async Task<IReadOnlyList<EventSchool>> GetSchoolsAsync(WebScraperContext db) =>
        await db.SportsEventsTypes.AsNoTracking().Where(t => t.Enabled && (t.SchoolName != null || t.ProviderKey == "espn-nwsl" || t.ProviderKey == "espn-wnba" || t.ProviderKey == "espn-mlb"))
            .OrderBy(t => t.SchoolName).Select(t => new EventSchool(t.Id, t.ProviderKey == "espn-mlb" ? "MLB" : t.ProviderKey == "espn-wnba" ? "WNBA" : t.ProviderKey == "espn-nwsl" ? "Kansas City Current" : t.SchoolName!)).ToListAsync();

    /// <summary>A safety cap on one page; a season is far smaller than this.</summary>
    public const int Limit = 200;

    /// <summary>
    /// The sports that have games from <paramref name="today"/> onward, for the filter. Games are grouped by
    /// <see cref="SportsEvent.EventDate"/>, which is already the host school's calendar date, so "today" is compared
    /// with it directly and never converted.
    /// </summary>
    public static async Task<IReadOnlyList<string>> GetSportsAsync(WebScraperContext db, DateOnly today, int? school = null, DateOnly? from = null, DateOnly? to = null) =>
        await InStartedSeasons(db, today)
            .Where(e => e.EventDate >= today && (from == null || e.EventDate >= from) && (to == null || e.EventDate <= to) && e.Sport != null
                && (school == null || e.EventSources.Any(s => s.SportsEventsTypeId == school && s.Feed.Enabled)
                    || (!e.EventSources.Any() && e.SportsEventsTypeId == school)))
            .Select(e => e.Sport!)
            .Distinct()
            .OrderBy(s => s)
            .ToListAsync();

    /// <summary>When the schedule was last refreshed by the scraper, or null when there are no events.</summary>
    public static async Task<DateTime?> GetLastUpdatedAsync(WebScraperContext db) =>
        await db.SportsEvents.AsNoTracking().MaxAsync(e => (DateTime?)e.LastSeenAt);

    /// <summary>
    /// Unfinished, non-canceled games from <paramref name="today"/> onward, soonest first; within a day, games
    /// with a known start time come before games whose time is still to be announced.
    /// </summary>
    public static async Task<EventsPage> GetUpcomingAsync(
        WebScraperContext db, DateOnly today, string? sport, string? search, int limit = Limit, int? school = null, DateOnly? from = null, DateOnly? to = null, string? team = null)
    {
        var query = InStartedSeasons(db, today).Where(e => e.EventDate >= today && (from == null || e.EventDate >= from) && (to == null || e.EventDate <= to));
        if (school is { } schoolId)
            query = query.Where(e => e.EventSources.Any(s => s.SportsEventsTypeId == schoolId && s.Feed.Enabled)
                || (!e.EventSources.Any() && e.SportsEventsTypeId == schoolId));

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
                || (e.SchoolName != null && e.SchoolName.Contains(term))
                || (e.SportsEventsType != null && e.SportsEventsType.SchoolName != null
                    && e.SportsEventsType.SchoolName.Contains(term)));
        }

        if (!string.IsNullOrWhiteSpace(team))
        {
            var aliases = TeamNames.Aliases(team);
            query = query.Where(e => aliases.Contains(e.SchoolName!) || aliases.Contains(e.Opponent!)
                || (e.SchoolName == null && e.SportsEventsType != null && aliases.Contains(e.SportsEventsType.SchoolName!)));
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
                e.SchoolName ?? (e.SportsEventsType != null ? e.SportsEventsType.SchoolName : null), e.IsNeutral, e.Radio, e.AudioUrl, e.ScheduleStatus, e.ProviderKey ?? (e.SportsEventsType != null ? e.SportsEventsType.ProviderKey : null)))
            .ToListAsync();

        return new EventsPage(items, total);
    }

    /// <summary>
    /// Every game on <paramref name="date"/> (the host school's calendar date, never converted), in start order; games
    /// whose time is still to be announced come last.
    /// </summary>
    public static async Task<IReadOnlyList<EventItem>> GetOnDateAsync(WebScraperContext db, DateOnly date) =>
        await InStartedSeasons(db, date)
            .Where(e => e.EventDate == date)
            .OrderBy(e => e.StartsAtUtc == null ? 1 : 0)
            .ThenBy(e => e.StartsAtUtc)
            .ThenBy(e => e.Sport)
            .ThenBy(e => e.Id)
            .Take(Limit)
            .Select(e => new EventItem(
                e.Id, e.Url, e.Title, e.Sport, e.Opponent, e.IsAway, e.Location, e.EventDate, e.StartsAtUtc, e.TimeTbd,
                e.Tv, e.StreamUrl, e.LiveStatsUrl, e.TeamLogoUrl, e.OpponentLogoUrl,
                e.SchoolName ?? (e.SportsEventsType != null ? e.SportsEventsType.SchoolName : null), e.IsNeutral, e.Radio, e.AudioUrl, e.ScheduleStatus))
            .ToListAsync();

    /// <summary>Builds the query string that keeps the current filters. Empty when nothing is set.</summary>
    public static string QueryString(string? sport, string? search, int? school = null, DateOnly? from = null, DateOnly? to = null)
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

        if (from is { } start) parts.Add($"from={start:yyyy-MM-dd}");
        if (to is { } end) parts.Add($"to={end:yyyy-MM-dd}");
        if (school is { } id) parts.Add($"school={id}");

        return parts.Count == 0 ? "" : "?" + string.Join("&", parts);
    }
}

/// <summary>Display-time text helpers for the events page. The stored data is never changed.</summary>
public static class EventsText
{
    public static string? StatusLabel(string? status) => status switch
    {
        "STATUS_POSTPONED" => "Postponed",
        "STATUS_DELAYED" => "Delayed",
        "STATUS_SUSPENDED" => "Suspended",
        _ => null
    };

    public static string TimeLabel(EventItem item, FeedDates dates) =>
        StatusLabel(item.ScheduleStatus) is { } status ? $"{status} · Time TBD"
        : item.TimeTbd || item.StartsAtUtc is null ? "Time TBD" : dates.EventTime(item.StartsAtUtc.Value);

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

    /// <summary>
    /// "Kansas at Houston" for an away game, "Kansas vs Houston" for a home game, "Kansas / Houston" when it is not known
    /// which. The school always comes first, as on its own calendar.
    /// </summary>
    public static string Matchup(string? school, string? opponent, bool? isAway)
    {
        var us = string.IsNullOrWhiteSpace(school) ? "Our team" : school.Trim();
        var them = string.IsNullOrWhiteSpace(opponent) ? "Opponent to be announced" : opponent.Trim();
        return $"{us} {(isAway is null ? "/" : isAway.Value ? "at" : "vs")} {them}";
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
