using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using MyNewsFeed.Web.Data.Models;

namespace MyNewsFeed.Web.Data;

public sealed record StandingStatistic(string Name, string DisplayName, string DisplayValue, double? Value = null);
public sealed record StandingColumn(string Name, string Label);
public sealed record StandingTeam(SportsStanding Row, IReadOnlyList<StandingStatistic> Statistics);
public sealed record StandingGroup(string Id, string Name, IReadOnlyList<StandingTeam> Teams, IReadOnlyList<StandingColumn> Columns);
public sealed record StandingsPage(IReadOnlyList<SportsStandingsSource> Sources, SportsStandingsSource? Selected,
    IReadOnlyList<StandingGroup> Groups, string? AvailabilityError = null);

public static class StandingsQuery
{
    public static int? ParseSourceId(string? value) => int.TryParse(value, out var id) && id > 0 ? id : null;

    public static SportsStandingsSource? SelectSource(IEnumerable<SportsStandingsSource> sources, int? requested)
    {
        var enabled = sources.Where(s => s.Enabled).OrderBy(s => s.DisplayName, StringComparer.OrdinalIgnoreCase).ThenBy(s => s.Id).ToList();
        return enabled.FirstOrDefault(s => s.Id == requested) ?? enabled.FirstOrDefault();
    }

    public static string? Freshness(SportsStandingsSource source, DateTime utcNow)
    {
        if (!string.IsNullOrWhiteSpace(source.LastError)) return "Latest refresh failed; showing last available standings.";
        return source.LastSuccessAt is not { } last || utcNow - DateTime.SpecifyKind(last, DateTimeKind.Utc) > TimeSpan.FromHours(2)
            ? "Standings may be out of date." : null;
    }

    public static IReadOnlyList<StandingStatistic> ParseStatistics(string? json, long rowId, ILogger logger)
    {
        try
        {
            using var document = JsonDocument.Parse(json ?? "");
            if (document.RootElement.ValueKind != JsonValueKind.Array) throw new JsonException("Expected a statistic array.");
            var result = new List<StandingStatistic>();
            foreach (var stat in document.RootElement.EnumerateArray())
            {
                if (stat.ValueKind != JsonValueKind.Object) throw new JsonException("Expected a statistic object.");
                static string Text(JsonElement element, string name) => element.TryGetProperty(name, out var value)
                    && value.ValueKind == JsonValueKind.String ? value.GetString() ?? "" : "";
                var name = Text(stat, "name");
                if (name.Length == 0) continue;
                double? number = stat.TryGetProperty("value", out var numeric) && numeric.ValueKind == JsonValueKind.Number
                    && numeric.TryGetDouble(out var value) && double.IsFinite(value) ? value : null;
                result.Add(new(name, Text(stat, "displayName"), Text(stat, "displayValue"), number));
            }
            return result;
        }
        catch (JsonException)
        {
            logger.LogWarning("Invalid standings statistics for row {RowId}.", rowId);
            return [];
        }
    }

    public static string StatisticValue(StandingTeam team, string name) =>
        team.Statistics.FirstOrDefault(s => s.Name == name)?.DisplayValue is { Length: > 0 } value ? value : "—";

    public static IReadOnlyList<StandingColumn> Columns(string provider, IReadOnlyList<StandingTeam> teams)
    {
        if (provider == "espn-nfl")
            return [new("wins", "W"), new("losses", "L"), new("ties", "T"),
                new("winPercent", "Pct"), new("pointsFor", "PF"),
                new("pointsAgainst", "PA"), new("pointDifferential", "Diff"),
                new("streak", "Streak")];
        if (provider is not ("espn-mlb" or "espn-wnba" or "espn-nwsl" or "espn-mls"))
            return teams.SelectMany(t => t.Statistics).DistinctBy(s => s.Name)
                .Select(s => new StandingColumn(s.Name, string.IsNullOrWhiteSpace(s.DisplayName) ? s.Name : s.DisplayName)).ToList();
        return provider is "espn-nwsl" or "espn-mls"
            ? [new("gamesPlayed", "GP"), new("wins", "W"), new("ties", "D"), new("losses", "L"),
                new("pointsFor", "GF"), new("pointsAgainst", "GA"), new("pointDifferential", "GD"), new("points", "Pts")]
            : [new("wins", "W"), new("losses", "L"), new("winPercent", "Pct"), new("gamesBehind", "GB"), new("streak", "Streak")];
    }

    public static IReadOnlyList<StandingGroup> BuildGroups(IEnumerable<SportsStanding> rows, string provider, ILogger logger) =>
        rows.Where(r => r.IsCurrent).OrderBy(r => r.GroupId, StringComparer.Ordinal).ThenBy(r => r.Position)
            .ThenBy(r => r.TeamName, StringComparer.OrdinalIgnoreCase).ThenBy(r => r.Id)
            .GroupBy(r => r.GroupId)
            .Select(group =>
            {
                var teams = group.Select(row => new StandingTeam(row, ParseStatistics(row.StatsJson, row.Id, logger))).ToList();
                if (provider == "espn-mls")
                    teams = teams.OrderByDescending(Points).ThenBy(t => t.Row.Position)
                        .ThenBy(t => t.Row.TeamName, StringComparer.OrdinalIgnoreCase).ThenBy(t => t.Row.Id).ToList();
                return new StandingGroup(group.Key, group.First().GroupName ?? "Standings", teams, Columns(provider, teams));
            }).ToList();

    private static double? Points(StandingTeam team)
    {
        var points = team.Statistics.FirstOrDefault(s => s.Name == "points");
        if (points?.Value is { } numeric) return numeric;
        return double.TryParse(points?.DisplayValue, System.Globalization.NumberStyles.Number,
            System.Globalization.CultureInfo.InvariantCulture, out var display) && double.IsFinite(display) ? display : null;
    }

    public static async Task<StandingsPage> GetAsync(WebScraperContext db, int? sourceId, ILogger logger, DateOnly? today = null, IEnumerable<string>? hiddenProviders = null)
    {
        var hidden = hiddenProviders?.ToArray() ?? [];
        var sources = await db.SportsStandingsSources.AsNoTracking().Where(s => s.Enabled && !hidden.Contains(s.ProviderKey))
            .OrderBy(s => s.DisplayName).ThenBy(s => s.Id).ToListAsync();
        var day = today ?? FeedDates.Create("America/Chicago").Today;
        var starts = await db.SportsEvents.AsNoTracking().Where(e => e.ProviderKey == "big12" && e.EventDate <= day
            && (e.ScheduleStatus == null || (e.ScheduleStatus != "STATUS_CANCELED" && e.ScheduleStatus != "STATUS_CANCELLED")))
            .Select(e => new { e.Sport, e.EventDate }).ToListAsync();
        sources = sources.Where(s => s.ProviderKey != "big12" ||
            (CollegeSeasons.Matches(s.LatestSeason, s.Sport, day)
                && starts.Any(e => e.Sport == s.Sport && e.EventDate >= CollegeSeasons.Start(s.Sport, day)))).ToList();
        var selected = SelectSource(sources, sourceId);
        if (selected is null) return new(sources, null, []);
        try
        {
            var rows = await db.SportsStandings.AsNoTracking()
                .Where(r => r.SourceId == selected.Id && r.IsCurrent
                    && db.SportsStandingsSources.Any(s => s.Id == r.SourceId && s.Enabled))
                .OrderBy(r => r.GroupId).ThenBy(r => r.Position).ThenBy(r => r.TeamName).ThenBy(r => r.Id).ToListAsync();
            return new(sources, selected, BuildGroups(rows.Where(r => selected.ProviderKey != "big12" || CollegeSeasons.Matches(r.Season, selected.Sport, day)), selected.ProviderKey, logger));
        }
        catch (System.Data.Common.DbException ex)
        {
            logger.LogError(ex, "Could not read standings for source {SourceId}.", selected.Id);
            return new(sources, selected, [], "Standings are temporarily unavailable. Please try again.");
        }
    }
}
