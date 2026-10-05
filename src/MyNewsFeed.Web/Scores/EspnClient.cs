using System.Globalization;
using System.Text.Json;
using Microsoft.Extensions.Options;

namespace MyNewsFeed.Web.Scores;

/// <summary>Settings for the results sync (section "Results").</summary>
public sealed class ScoresOptions
{
    public const string Section = "Results";

    /// <summary>ESPN's public site API. Unofficial and undocumented, so it is configurable in case it moves.</summary>
    public string BaseUrl { get; set; } = "https://site.api.espn.com/apis/site/v2/sports/";

    /// <summary>How long a follow stays fresh before the background sync fetches it again.</summary>
    public double SyncIntervalHours { get; set; } = 3;

    /// <summary>How many past days of scoreboards a newly followed league fetches (one request per day).</summary>
    public int BackfillDays { get; set; } = 7;
}

/// <summary>
/// A league ESPN covers that the admin can follow: its sport and league slugs as ESPN's addresses use them, its name, a
/// short label that tells a team's follows apart ("Kansas Jayhawks · Volleyball"), and the scoreboard tag shown on each
/// result ("NCAAF"), short enough to stay on one line in the results table.
/// </summary>
public sealed record EspnLeague(string Sport, string League, string Name, string Short, string Tag);

/// <summary>The leagues offered on the admin page. Any other ESPN sport/league pair can be added here.</summary>
public static class EspnLeagues
{
    public static readonly IReadOnlyList<EspnLeague> All =
    [
        new("football", "nfl", "NFL", "NFL", "NFL"),
        new("football", "college-football", "College Football", "Football", "NCAAF"),
        new("baseball", "mlb", "MLB", "MLB", "MLB"),
        new("basketball", "nba", "NBA", "NBA", "NBA"),
        new("basketball", "wnba", "WNBA", "WNBA", "WNBA"),
        new("basketball", "mens-college-basketball", "Men's College Basketball", "Men's Basketball", "NCAAM"),
        new("basketball", "womens-college-basketball", "Women's College Basketball", "Women's Basketball", "NCAAW"),
        new("volleyball", "womens-college-volleyball", "Women's College Volleyball", "Volleyball", "NCAA VB"),
        new("hockey", "nhl", "NHL", "NHL", "NHL"),
        new("soccer", "usa.1", "MLS", "MLS", "MLS"),
        new("soccer", "usa.nwsl", "NWSL", "NWSL", "NWSL"),
        new("soccer", "eng.1", "Premier League", "Premier League", "EPL"),
    ];

    public static EspnLeague? Find(string sport, string league) =>
        All.FirstOrDefault(l => l.Sport == sport && l.League == league);

    /// <summary>The display name of a league, falling back to its slug for one no longer in the list.</summary>
    public static string NameOf(string sport, string league) => Find(sport, league)?.Name ?? league;

    /// <summary>The scoreboard tag of a league ("NCAAF"), falling back to its slug for one no longer in the list.</summary>
    public static string TagOf(string sport, string league) => Find(sport, league)?.Tag ?? league;

    /// <summary>
    /// Whether a team name matches the admin's filter: every word typed must start a word of the name, ignoring case.
    /// "Kansas" finds Kansas Jayhawks and Kansas State Wildcats but not Arkansas Razorbacks; "kan st" finds Kansas State.
    /// </summary>
    public static bool TeamMatches(string name, string? filter)
    {
        var wanted = (filter ?? "").Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (wanted.Length == 0)
        {
            return true;
        }

        var words = name.Split([' ', '-', '(', ')', '.', '\''], StringSplitOptions.RemoveEmptyEntries);
        return wanted.All(w => words.Any(word => word.StartsWith(w, StringComparison.OrdinalIgnoreCase)));
    }

    /// <summary>
    /// How a follow is named on the results page: a league by its name, a team with its league's short label, so the
    /// same school followed in three sports reads "Kansas Jayhawks · Football", "· Men's Basketball", "· Volleyball".
    /// </summary>
    public static string FollowLabel(string kind, string name, string sport, string league) =>
        kind == FollowKinds.Team ? $"{name} · {Find(sport, league)?.Short ?? league}" : name;
}

/// <summary>One game as ESPN reports it, reduced to what the site stores.</summary>
public sealed record EspnGame(
    string EventId,
    DateTime StartsAtUtc,
    string State,
    bool Completed,
    string? StatusText,
    EspnSide Home,
    EspnSide Away,
    string? WinnerSide,
    string? Venue);

public sealed record EspnSide(string TeamId, string Name, string? LogoUrl, int? Score, int? Shootout);

public sealed record EspnTeam(string Id, string Name, string? LogoUrl);

/// <summary>The outcome of one request: a value, or an error to show on the admin page.</summary>
public sealed record EspnResult<T>(T? Value, string? Error)
{
    public bool Ok => Error is null;
}

public static class FollowKinds
{
    public const string Team = "team";
    public const string League = "league";
}

/// <summary>
/// Reads ESPN's public scoreboard data. ESPN does not document or support it and it needs no key; it has been stable
/// for years, but could change without notice, so every response is parsed defensively and a failure becomes an
/// error message on the follow rather than an exception. Pages never call it: the background sync stores games and
/// pages read the database.
/// </summary>
public sealed class EspnClient
{
    private readonly HttpClient http;

    public EspnClient(HttpClient http, IOptions<ScoresOptions> options)
    {
        this.http = http;
        http.BaseAddress = new Uri(options.Value.BaseUrl.TrimEnd('/') + "/");
    }

    /// <summary>ESPN season types: 2 is the regular season, 3 the postseason (bowls, tournaments, playoffs).</summary>
    public static readonly IReadOnlyList<int> ScheduleSeasonTypes = [2, 3];

    /// <summary>
    /// One part of a team's current-season schedule, played games with their scores included. The season type is always
    /// given: without it ESPN returns only the part of the season under way, which before a season starts is the
    /// empty preseason (college basketball in October).
    /// </summary>
    public Task<EspnResult<IReadOnlyList<EspnGame>>> GetTeamScheduleAsync(
        string sport, string league, string teamId, int seasonType, CancellationToken ct = default) =>
        GetAsync($"{sport}/{league}/teams/{Uri.EscapeDataString(teamId)}/schedule?seasontype={seasonType}", ParseGames, ct);

    /// <summary>
    /// Every game in a league on one day (ESPN's own calendar day, US Eastern). ESPN rejects date ranges, so a league is
    /// fetched a day at a time.
    /// </summary>
    public Task<EspnResult<IReadOnlyList<EspnGame>>> GetScoreboardAsync(string sport, string league, DateOnly day, CancellationToken ct = default) =>
        GetAsync($"{sport}/{league}/scoreboard?dates={day.ToString("yyyyMMdd", CultureInfo.InvariantCulture)}&limit=500", ParseGames, ct);

    /// <summary>Every team in a league, for the admin's team picker.</summary>
    public Task<EspnResult<IReadOnlyList<EspnTeam>>> GetTeamsAsync(string sport, string league, CancellationToken ct = default) =>
        GetAsync($"{sport}/{league}/teams?limit=1000", ParseTeams, ct);

    private async Task<EspnResult<IReadOnlyList<T>>> GetAsync<T>(string path, Func<JsonElement, IReadOnlyList<T>> parse, CancellationToken ct)
    {
        try
        {
            using var response = await http.GetAsync(path, ct);
            if (!response.IsSuccessStatusCode)
            {
                return new(null, $"ESPN answered HTTP {(int)response.StatusCode} for {path}.");
            }

            await using var body = await response.Content.ReadAsStreamAsync(ct);
            using var json = await JsonDocument.ParseAsync(body, cancellationToken: ct);
            return new(parse(json.RootElement), null);
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException && !ct.IsCancellationRequested)
        {
            return new(null, $"Could not reach ESPN: {ex.Message}");
        }
        catch (Exception ex) when (ex is JsonException or KeyNotFoundException or InvalidOperationException or FormatException)
        {
            // ESPN changed its data: report it rather than store half a game.
            return new(null, $"Unexpected response from ESPN for {path}: {ex.Message}");
        }
    }

    /// <summary>
    /// Games from a team schedule or a scoreboard; both have "events", each with one competition of two competitors.
    /// The two differ in detail: a schedule gives a score as {"value", "displayValue"} and logos as a list, a scoreboard
    /// gives a score as a string and a single "logo". Events without exactly a home and an away side (a tournament
    /// placeholder) are skipped.
    /// </summary>
    public static IReadOnlyList<EspnGame> ParseGames(JsonElement root)
    {
        var games = new List<EspnGame>();
        if (!root.TryGetProperty("events", out var events) || events.ValueKind != JsonValueKind.Array)
        {
            return games;
        }

        foreach (var ev in events.EnumerateArray())
        {
            var competition = ev.GetProperty("competitions").EnumerateArray().First();
            var sides = competition.GetProperty("competitors").EnumerateArray().ToList();
            var home = sides.FirstOrDefault(s => Str(s, "homeAway") == "home");
            var away = sides.FirstOrDefault(s => Str(s, "homeAway") == "away");
            if (home.ValueKind != JsonValueKind.Object || away.ValueKind != JsonValueKind.Object)
            {
                continue;
            }

            var status = competition.TryGetProperty("status", out var st) && st.TryGetProperty("type", out var type) ? type : default;
            var state = status.ValueKind == JsonValueKind.Object ? Str(status, "state") ?? "pre" : "pre";
            var completed = status.ValueKind == JsonValueKind.Object
                && status.TryGetProperty("completed", out var done) && done.ValueKind == JsonValueKind.True;

            var date = Str(competition, "date") ?? Str(ev, "date")
                ?? throw new FormatException($"event {Str(ev, "id")} has no date");

            games.Add(new EspnGame(
                Str(ev, "id") ?? throw new FormatException("an event has no id"),
                DateTimeOffset.Parse(date, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal).UtcDateTime,
                state,
                completed,
                status.ValueKind == JsonValueKind.Object ? Str(status, "shortDetail") ?? Str(status, "description") : null,
                Side(home),
                Side(away),
                IsWinner(home) ? "home" : IsWinner(away) ? "away" : null,
                competition.TryGetProperty("venue", out var venue) ? Venue(venue) : null));
        }

        return games;
    }

    /// <summary>The teams of a league: sports[0].leagues[0].teams[].team.</summary>
    public static IReadOnlyList<EspnTeam> ParseTeams(JsonElement root)
    {
        var teams = root.GetProperty("sports").EnumerateArray().First()
            .GetProperty("leagues").EnumerateArray().First()
            .GetProperty("teams");

        return teams.EnumerateArray()
            .Select(t => t.GetProperty("team"))
            .Select(t => new EspnTeam(Str(t, "id")!, Str(t, "displayName") ?? Str(t, "name") ?? "?", Logo(t)))
            .Where(t => t.Id is not null)
            .OrderBy(t => t.Name, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private static EspnSide Side(JsonElement competitor)
    {
        var team = competitor.GetProperty("team");
        return new EspnSide(
            Str(team, "id") ?? Str(competitor, "id") ?? throw new FormatException("a competitor has no team id"),
            Str(team, "displayName") ?? Str(team, "name") ?? "?",
            Logo(team),
            Score(competitor, "score"),
            Score(competitor, "shootoutScore"));
    }

    private static bool IsWinner(JsonElement competitor) =>
        competitor.TryGetProperty("winner", out var w) && w.ValueKind == JsonValueKind.True;

    /// <summary>A score given as a number, a numeric string, or {"value": 3.0, "displayValue": "3"}.</summary>
    private static int? Score(JsonElement competitor, string name)
    {
        if (!competitor.TryGetProperty(name, out var s))
        {
            return null;
        }

        if (s.ValueKind == JsonValueKind.Object)
        {
            s = s.TryGetProperty("value", out var v) ? v : default;
        }

        return s.ValueKind switch
        {
            JsonValueKind.Number when s.TryGetDouble(out var d) => (int)Math.Round(d),
            JsonValueKind.String when int.TryParse(s.GetString(), NumberStyles.Integer, CultureInfo.InvariantCulture, out var n) => n,
            _ => null,
        };
    }

    /// <summary>A scoreboard team has "logo"; a schedule or team-list team has "logos": [{"href"}].</summary>
    private static string? Logo(JsonElement team)
    {
        if (Str(team, "logo") is { } logo)
        {
            return logo;
        }

        return team.TryGetProperty("logos", out var logos) && logos.ValueKind == JsonValueKind.Array
            ? logos.EnumerateArray().Select(l => Str(l, "href")).FirstOrDefault(h => h is not null)
            : null;
    }

    /// <summary>
    /// A small version of an ESPN logo, for the 32px team badges. ESPN's logo files are large (a 4096px, 280 KB PNG for
    /// an NFL scoreboard logo), so a page of results would load megabytes; its image resizer serves the same logo at
    /// 64px (sharp on high-density screens) in a few KB. Anything not on ESPN's image host is returned unchanged.
    /// </summary>
    public static string? SmallLogo(string? url)
    {
        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri)
            || !uri.Host.Equals("a.espncdn.com", StringComparison.OrdinalIgnoreCase)
            || !uri.AbsolutePath.StartsWith("/i/", StringComparison.Ordinal)
            || uri.Query.Length > 0)
        {
            return url;
        }

        return $"https://a.espncdn.com/combiner/i?img={uri.AbsolutePath}&w=64&h=64";
    }

    /// <summary>"Pittsburgh, Pennsylvania / Petersen Events Center", the events page's "City / Venue" shape.</summary>
    private static string? Venue(JsonElement venue)
    {
        var name = Str(venue, "fullName");
        string? city = null;
        if (venue.TryGetProperty("address", out var address))
        {
            var parts = new[] { Str(address, "city"), Str(address, "state") }.Where(p => p is not null).ToList();
            city = parts.Count == 0 ? null : string.Join(", ", parts.Distinct());
        }

        return (city, name) switch
        {
            (null, null) => null,
            (null, _) => name,
            (_, null) => city,
            _ => $"{city} / {name}",
        };
    }

    private static string? Str(JsonElement e, string name) =>
        e.ValueKind == JsonValueKind.Object && e.TryGetProperty(name, out var v)
        && v.ValueKind == JsonValueKind.String && !string.IsNullOrWhiteSpace(v.GetString())
            ? v.GetString()!.Trim()
            : null;
}
