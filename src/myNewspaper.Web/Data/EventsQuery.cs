using Microsoft.EntityFrameworkCore;

namespace myNewspaper.Web.Data;

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
    bool? IsNeutral = null, string? Radio = null, string? AudioUrl = null, string? ScheduleStatus = null, string? ProviderKey = null, string? CanonicalKey = null, int? GameId = null, int? SchoolId = null, int? OpponentSchoolId = null);

/// <param name="Total">Every upcoming event matching the filters (the list itself is capped).</param>
public sealed record EventSchool(int Id, string Name);
public sealed record ScheduleChoice(string Value, string Name, string Provider, string? Sport);

public sealed record EventsPage(IReadOnlyList<EventItem> Items, int Total);

/// <summary>Read side of the public sports events page.</summary>
public static class EventsQuery
{
    private static IQueryable<Models.SportsEvent> Visible(WebScraperContext db) => db.SportsEvents.AsNoTracking()
        .Where(e => e.Sport != "Women's Basketball")
        .Where(e => e.ScheduleStatus == null ||
            (e.ScheduleStatus != "STATUS_CANCELED" && e.ScheduleStatus != "STATUS_CANCELLED"
             && e.ScheduleStatus != "STATUS_UNNECESSARY" && e.ScheduleStatus != "STATUS_FULL_TIME" && !e.ScheduleStatus.StartsWith("STATUS_FINAL")))
        .Where(e => e.ProviderKey == "espn-nwsl" || e.ProviderKey == "espn-wnba" || e.ProviderKey == "espn-mlb" || e.ProviderKey == "espn-championship" || e.ProviderKey == "espn-epl" || e.ProviderKey == "espn-mls"
            ? e.SportsEventsType != null && e.SportsEventsType.Enabled
                && e.SportsEventsType.ProviderKey == e.ProviderKey
                && (e.ProviderKey == "espn-nwsl" ? e.SportsEventsType.ProviderSchoolId == 20907
                    : e.ProviderKey == "espn-championship" || e.ProviderKey == "espn-epl" || e.ProviderKey == "espn-mls" || e.SportsEventsType.ProviderSchoolId == 0)
            : e.EventSources.Any(s => s.Feed.Enabled)
                || (!e.EventSources.Any() && (e.SportsEventsType == null || e.SportsEventsType.Enabled)));

    public static async Task<IReadOnlyList<EventSchool>> GetSchoolsAsync(WebScraperContext db) =>
        await db.SportsEventsTypes.AsNoTracking().Where(t => t.Enabled && (t.SchoolName != null || t.ProviderKey == "espn-nwsl" || t.ProviderKey == "espn-wnba" || t.ProviderKey == "espn-mlb" || t.ProviderKey == "espn-championship" || t.ProviderKey == "espn-epl" || t.ProviderKey == "espn-mls"))
            .OrderBy(t => t.SchoolName).Select(t => new EventSchool(t.Id, t.ProviderKey == "espn-championship" ? "EFL Championship" : t.ProviderKey == "espn-epl" ? "Premier League" : t.ProviderKey == "espn-mls" ? "MLS" : t.ProviderKey == "espn-mlb" ? "MLB" : t.ProviderKey == "espn-wnba" ? "WNBA" : t.ProviderKey == "espn-nwsl" ? "Kansas City Current" : t.SchoolName!)).ToListAsync();

    public static async Task<IReadOnlyList<ScheduleChoice>> GetScheduleChoicesAsync(WebScraperContext db, DateOnly today)
    {
        var rows = await Visible(db).Where(e => e.EventDate >= today)
            .Select(e => new { Provider = e.ProviderKey ?? (e.SportsEventsType != null ? e.SportsEventsType.ProviderKey : null), e.Sport })
            .Distinct().ToListAsync();
        return rows.Select(r => new ScheduleChoice((r.Provider ?? "") + "|" + r.Sport,
            r.Provider switch
            {
                "espn-epl" => "Premier League", "espn-championship" => "EFL Championship",
                "espn-mls" => "MLS", "espn-nwsl" => "NWSL", "espn-mlb" => "MLB",
                "espn-wnba" => "WNBA", "espn-nhl" => "NHL",
                "big12" => "Big 12 " + r.Sport,
                _ => r.Sport ?? "Other events"
            }, r.Provider ?? "", r.Sport))
            .OrderBy(c => c.Name, StringComparer.OrdinalIgnoreCase).ThenBy(c => c.Value, StringComparer.Ordinal).ToList();
    }

    /// <summary>A safety cap on one page; a season is far smaller than this.</summary>
    public const int Limit = 200;

    /// <summary>
    /// The sports that have games from <paramref name="today"/> onward, for the filter. Games are grouped by
    /// <see cref="SportsEvent.EventDate"/>, which is already the host school's calendar date, so "today" is compared
    /// with it directly and never converted.
    /// </summary>
    public static async Task<IReadOnlyList<string>> GetSportsAsync(WebScraperContext db, DateOnly today, int? school = null, DateOnly? from = null, DateOnly? to = null) =>
        await Visible(db)
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
        WebScraperContext db, DateOnly today, string? sport, string? search, int limit = Limit, int? school = null, DateOnly? from = null, DateOnly? to = null, string? team = null, int? providerTeamId = null, int offset = 0, string? provider = null)
    {
        var query = Visible(db).Where(e => e.EventDate >= today && (from == null || e.EventDate >= from) && (to == null || e.EventDate <= to));
        if (provider is not null)
            query = query.Where(e => (e.ProviderKey ?? (e.SportsEventsType != null ? e.SportsEventsType.ProviderKey : null) ?? "") == provider);
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
        if (providerTeamId == 352)
            query = query.Where(e => e.ProviderKey == "espn-championship" && (e.SchoolId == 352 || e.OpponentSchoolId == 352));
        var total = await query.CountAsync();
        offset = limit > 0 ? Math.Clamp(offset, 0, Math.Max(0, (total - 1) / limit) * limit) : 0;
        var items = await query
            .OrderBy(e => e.EventDate)
            .ThenBy(e => e.StartsAtUtc == null ? 1 : 0)
            .ThenBy(e => e.StartsAtUtc)
            .ThenBy(e => e.Sport)
            .ThenBy(e => e.Id)
            .Skip(offset).Take(limit)
            .Select(e => new EventItem(
                e.Id, e.Url, e.Title, e.Sport, e.Opponent, e.ProviderKey == "espn-championship" || e.ProviderKey == "espn-epl" || e.ProviderKey == "espn-mls" ? false : e.IsAway, e.Location, e.EventDate, e.StartsAtUtc, e.TimeTbd,
                e.Tv, e.StreamUrl, e.LiveStatsUrl, e.TeamLogoUrl, e.OpponentLogoUrl,
                e.SchoolName ?? (e.SportsEventsType != null ? e.SportsEventsType.SchoolName : null), e.IsNeutral, e.Radio, e.AudioUrl, e.ScheduleStatus, e.ProviderKey ?? (e.SportsEventsType != null ? e.SportsEventsType.ProviderKey : null), e.CanonicalKey, e.GameId, e.SchoolId, e.OpponentSchoolId))
            .ToListAsync();

        return new EventsPage(items, total);
    }

    /// <summary>
    /// Every game on <paramref name="date"/> (the host school's calendar date, never converted), in start order; games
    /// whose time is still to be announced come last.
    /// </summary>
    public static async Task<IReadOnlyList<EventItem>> GetOnDateAsync(WebScraperContext db, DateOnly date) =>
        await Visible(db)
            .Where(e => e.EventDate == date)
            .OrderBy(e => e.StartsAtUtc == null ? 1 : 0)
            .ThenBy(e => e.StartsAtUtc)
            .ThenBy(e => e.Sport)
            .ThenBy(e => e.Id)
            .Take(Limit)
            .Select(e => new EventItem(
                e.Id, e.Url, e.Title, e.Sport, e.Opponent, e.ProviderKey == "espn-mls" ? false : e.IsAway, e.Location, e.EventDate, e.StartsAtUtc, e.TimeTbd,
                e.Tv, e.StreamUrl, e.LiveStatsUrl, e.TeamLogoUrl, e.OpponentLogoUrl,
                e.SchoolName ?? (e.SportsEventsType != null ? e.SportsEventsType.SchoolName : null), e.IsNeutral, e.Radio, e.AudioUrl, e.ScheduleStatus, e.ProviderKey ?? (e.SportsEventsType != null ? e.SportsEventsType.ProviderKey : null), e.CanonicalKey, e.GameId, e.SchoolId, e.OpponentSchoolId))
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
    /// <summary>Provider radio labels can contain HTML, including truncated image tags.</summary>
    public static string? RadioLabel(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return null;
        var decoded = System.Net.WebUtility.HtmlDecode(value);
        var text = new System.Text.StringBuilder();
        var inTag = false;
        char quote = '\0';
        for (var i = 0; i < decoded.Length; i++)
        {
            var ch = decoded[i];
            if (!inTag && ch == '<' && i + 1 < decoded.Length
                && (char.IsLetter(decoded[i + 1]) || decoded[i + 1] is '/' or '!' or '?'))
            { inTag = true; text.Append(' '); continue; }
            if (inTag)
            {
                if (quote != '\0') { if (ch == quote) quote = '\0'; }
                else if (ch is '\'' or '"') quote = ch;
                else if (ch == '>') inTag = false;
                continue;
            }
            text.Append(ch);
        }
        var label = string.Join(" ", text.ToString().Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));
        return label.Length == 0 ? null : label;
    }

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
