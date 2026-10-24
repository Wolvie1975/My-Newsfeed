using System.Data.Common;
using System.Globalization;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using MyDailyFeed.Web.Scores;

namespace MyDailyFeed.Web.Data;

public sealed class ScoreboardGame
{
    public long ID { get; set; }
    public string Provider { get; set; } = "";
    public string Sport { get; set; } = "";
    public string League { get; set; } = "";
    public string EventId { get; set; } = "";
    public DateOnly ScoreboardDate { get; set; }
    public DateTime? StartsAtUtc { get; set; }
    public bool TimeTbd { get; set; }
    public bool WasRescheduled { get; set; }
    public string? HomeTeamId { get; set; }
    public string? AwayTeamId { get; set; }
    public string HomeName { get; set; } = "TBD";
    public string AwayName { get; set; } = "TBD";
    public string? HomeLogoUrl { get; set; }
    public string? AwayLogoUrl { get; set; }
    public decimal? HomeScore { get; set; }
    public decimal? AwayScore { get; set; }
    public string Status { get; set; } = "unknown";
    public bool Completed { get; set; }
    public string ProviderStatus { get; set; } = "";
    public string? StatusText { get; set; }
    public int? Period { get; set; }
    public string? DisplayClock { get; set; }
    public bool? NeutralSite { get; set; }
    public int? HomeShootout { get; set; }
    public int? AwayShootout { get; set; }
    public string? WinnerSide { get; set; }
    public string? Venue { get; set; }
    public string? Broadcasts { get; set; }
    public string? SourceUrl { get; set; }
    public string? LiveDetailsJson { get; set; }
    public DateTime LastObservedAtUtc { get; set; }
    public bool IsStale { get; set; }
    public string? RequestAvailability { get; set; }
    public bool HasBoxScore { get; set; }
    public string Key => $"{Provider}|{Sport}|{League}|{EventId}";
    public string? HomePreference => TeamKey(Sport, League, HomeTeamId);
    public string? AwayPreference => TeamKey(Sport, League, AwayTeamId);
    public static string? TeamKey(string sport, string league, string? id) => string.IsNullOrWhiteSpace(id) ? null : $"{sport}|{league}|{id}";
    public static string Score(decimal? value) => value?.ToString("0.###", CultureInfo.InvariantCulture) ?? "—";
    public string Label => Status switch {
        "upcoming" => "Upcoming", "final" => StatusText ?? "Final", "live" => StatusText ?? "Live",
        "postponed" => "Postponed", "cancelled" => "Cancelled", "suspended" => "Suspended",
        "delayed" => "Delayed", _ => "Status unavailable"
    };
    public bool Active => Status is "live" or "delayed" or "suspended";
    public bool Stale(DateTime utcNow) => IsStale || Active && utcNow - DateTime.SpecifyKind(LastObservedAtUtc, DateTimeKind.Utc) > TimeSpan.FromMinutes(25);
    public string? Details => FormatDetails(LiveDetailsJson, Sport);
    public static string? FormatDetails(string? json, string sport)
    {
        if (string.IsNullOrWhiteSpace(json)) return null;
        try {
            using var doc = JsonDocument.Parse(json);
            if (doc.RootElement.ValueKind != JsonValueKind.Object) return null;
            var root = doc.RootElement;
            var parts = new List<string>();
            if (sport == "baseball") {
                foreach (var (key, label) in new[] { ("outs", "outs"), ("balls", "balls"), ("strikes", "strikes") })
                    if (root.TryGetProperty(key, out var v) && v.ValueKind == JsonValueKind.Number && v.TryGetInt32(out var number)) parts.Add($"{number} {label}");
                foreach (var (key, label) in new[] { ("onFirst", "1st occupied"), ("onSecond", "2nd occupied"), ("onThird", "3rd occupied") })
                    if (root.TryGetProperty(key, out var v) && v.ValueKind == JsonValueKind.True) parts.Add(label);
            }
            foreach (var (key, label) in new[] { ("awayPeriods", "Away"), ("homePeriods", "Home") }) {
                if (!root.TryGetProperty(key, out var array) || array.ValueKind != JsonValueKind.Array) continue;
                var values = new List<string>();
                foreach (var period in array.EnumerateArray().Take(40)) {
                    if (period.ValueKind != JsonValueKind.Object) continue;
                    var value = period.TryGetProperty("displayValue", out var display) && display.ValueKind == JsonValueKind.String ? display.GetString()
                        : period.TryGetProperty("score", out var score) && score.ValueKind == JsonValueKind.Number ? score.GetRawText() : null;
                    if (value != null) values.Add(value);
                }
                if (values.Count > 0) parts.Add($"{label} periods: {string.Join(" · ", values)}");
            }
            return parts.Count > 0 ? string.Join(" · ", parts) : null;
        } catch (Exception ex) when (ex is JsonException or InvalidOperationException or FormatException) { return null; }
    }
}
public sealed class ScoreboardCoverage
{
    public int ID { get; set; }
    public string SourceKind { get; set; } = "";
    public int OriginId { get; set; }
    public string Sport { get; set; } = "";
    public string League { get; set; } = "";
    public string Label { get; set; } = "";
    public DateTime? LastAttemptAt { get; set; }
    public DateTime? LastSuccessAt { get; set; }
    public string? Availability { get; set; }
    public string Key => Sport + "|" + League;
}
public sealed record ScoreboardFollow(int Id, string Label, string Sport, string League, string Kind, string? TeamId, IReadOnlySet<string> Members)
{
    public bool Matches(ScoreboardGame game) => game.Provider == "espn" && game.Sport == Sport && game.League == League && Kind switch {
        "league" => true,
        "team" => !string.IsNullOrEmpty(TeamId) && (game.HomeTeamId == TeamId || game.AwayTeamId == TeamId),
        "conference" => game.HomeTeamId != null && Members.Contains(game.HomeTeamId) || game.AwayTeamId != null && Members.Contains(game.AwayTeamId),
        _ => false
    };
}
public sealed record ScoreboardSnapshot(IReadOnlyList<ScoreboardGame> Games, IReadOnlyList<ScoreboardCoverage> Coverage, bool Available, IReadOnlyList<ScoreboardFollow>? Follows = null);

public sealed class ScoreboardRepository(IDbContextFactory<WebScraperContext> factory, ILogger<ScoreboardRepository> log)
{
    public async Task<ScoreboardSnapshot> ReadAsync(DateOnly day, bool carryOver = false, string? competition = null, int? followId = null, bool finalsOnly = false)
    {
        try {
            await using var db = await factory.CreateDbContextAsync();
            var coverage = await db.Database.SqlQueryRaw<ScoreboardCoverage>("SELECT ID,SourceKind,OriginId,Sport,League,Label,LastAttemptAt,LastSuccessAt,Availability FROM dbo.SportsScoreboardCoverage").ToListAsync();
            var origins = await db.ResultFollows.AsNoTracking().Where(f => f.Enabled).ToListAsync();
            var originIds = origins.Select(f => f.Id).ToArray();
            var members = await db.ResultFollowTeams.AsNoTracking().Where(m => originIds.Contains(m.FollowId)).ToListAsync();
            var follows = origins.Where(f => coverage.Any(c => c.Sport == f.Sport && c.League == f.League))
                .OrderBy(f => FollowKinds.Order(f.Kind)).ThenBy(f => f.Name).ThenBy(f => f.League)
                .Select(f => new ScoreboardFollow(f.Id, EspnLeagues.FollowLabel(f.Kind, f.Name, f.Sport, f.League), f.Sport, f.League, f.Kind, f.TeamId,
                    members.Where(m => m.FollowId == f.Id).Select(m => m.TeamId).ToHashSet())).ToList();
            var selectedFollow = follows.FirstOrDefault(f => f.Id == followId);
            var selected = coverage.FirstOrDefault(c => c.Key == competition);
            var window = ResultsWindow.ForDay(day, TimeZoneInfo.FindSystemTimeZoneById("America/Chicago"));
            var query = db.Database.SqlQueryRaw<ScoreboardGame>("SELECT ID,Provider,Sport,League,EventId,ScoreboardDate,StartsAtUtc,TimeTbd,WasRescheduled,HomeTeamId,AwayTeamId,HomeName,AwayName,HomeLogoUrl,AwayLogoUrl,HomeScore,AwayScore,Status,Completed,ProviderStatus,StatusText,Period,DisplayClock,NeutralSite,HomeShootout,AwayShootout,WinnerSide,Venue,Broadcasts,SourceUrl,LiveDetailsJson,LastObservedAtUtc,IsStale,RequestAvailability,HasBoxScore FROM dbo.SportsScoreboard");
            query = query.Where(g => g.ScoreboardDate == day && (g.TimeTbd || g.StartsAtUtc >= window.StartUtc && g.StartsAtUtc < window.EndUtc)
                || carryOver && g.ScoreboardDate < day && (g.Status == "live" || g.Status == "delayed" || g.Status == "suspended"));
            if (selected != null) query = query.Where(g => g.Sport == selected.Sport && g.League == selected.League);
            if (finalsOnly) query = query.Where(g => g.Status == "final" && g.Completed);
            var games = await query.ToListAsync();
            if (selectedFollow != null) games = games.Where(selectedFollow.Matches).ToList();
            return new(games.DistinctBy(g => g.Key).OrderBy(g => g.Status == "live" ? 0 : g.Active ? 1 : g.Status == "upcoming" ? 2 : 3).ThenBy(g => g.StartsAtUtc).ThenBy(g => g.ID).ToList(), coverage, true, follows);
        } catch (Exception ex) when (ex is DbException or InvalidOperationException) {
            log.LogWarning(ex, "Scoreboard read unavailable");
            return new([], [], false);
        }
    }
}
