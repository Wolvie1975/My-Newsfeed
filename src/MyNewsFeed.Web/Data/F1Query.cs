using System.Data;
using System.Globalization;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using MyNewsFeed.Web.Data.Models;

namespace MyNewsFeed.Web.Data;

public sealed record F1Session(string Name, DateOnly Date, DateTime? StartsUtc);
public sealed record F1ResultChoice(int Round, string SessionType)
{
    public string Value => $"{Round}:{SessionType}";
}
public sealed record F1Page(F1Source? Source, int? Season, DateTime? FetchedAt,
    IReadOnlyList<F1Race> Races, IReadOnlyList<F1ResultChoice> Choices, F1ResultChoice? Selected,
    IReadOnlyList<F1Classification> Results, IReadOnlyList<F1DriverStanding> Drivers,
    IReadOnlyList<F1ConstructorStanding> Constructors)
{
    public static F1Page Empty => new(null, null, null, [], [], null, [], [], []);
}

public static class F1Query
{
    public static async Task<F1Page> GetAsync(WebScraperContext db, string view, string? selection = null)
    {
        // All views expand one atomically replaced snapshot. Keep that row stable across these reads.
        await using var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.RepeatableRead);
        var source = await db.Set<F1Source>().AsNoTracking().FirstOrDefaultAsync();
        if (source?.Enabled != true) return F1Page.Empty with { Source = source };
        var races = await db.Set<F1Race>().AsNoTracking().OrderBy(r => r.Round).ToListAsync();
        var season = races.Select(r => (int?)r.Season).FirstOrDefault();
        if (season is null) return F1Page.Empty with { Source = source };
        races = races.Where(r => r.Season == season).ToList();
        var choices = await db.Set<F1Classification>().AsNoTracking().Where(r => r.Season == season && (r.SessionType == "Race" || r.SessionType == "Sprint"))
            .Select(r => new F1ResultChoice(r.Round, r.SessionType)).Distinct().ToListAsync();
        choices = choices.Where(c => races.Any(r => r.Round == c.Round)).OrderByDescending(c => c.Round)
            .ThenBy(c => c.SessionType == "Race" ? 0 : 1).ToList();
        var selected = Select(choices, selection);
        var results = view == "results" && selected != null
            ? await db.Set<F1Classification>().AsNoTracking().Where(r => r.Season == season && r.Round == selected.Round && r.SessionType == selected.SessionType)
                .OrderBy(r => r.Position).ThenBy(r => r.DriverId).ToListAsync() : [];
        var drivers = view == "standings"
            ? await db.Set<F1DriverStanding>().AsNoTracking().Where(r => r.Season == season).OrderBy(r => r.Position).ThenBy(r => r.DriverId).ToListAsync() : [];
        var constructors = view == "standings"
            ? await db.Set<F1ConstructorStanding>().AsNoTracking().Where(r => r.Season == season).OrderBy(r => r.Position).ThenBy(r => r.ConstructorId).ToListAsync() : [];
        await transaction.CommitAsync();
        return new(source, season, races.Min(r => r.FetchedAt), races, choices, selected, results, drivers, constructors);
    }

    public static F1ResultChoice? Select(IEnumerable<F1ResultChoice> choices, string? selected) =>
        choices.FirstOrDefault(c => c.Value == selected) ?? choices.OrderByDescending(c => c.Round).ThenBy(c => c.SessionType == "Race" ? 0 : 1).FirstOrDefault();

    public static DateTime? StartsUtc(DateOnly date, string? time) =>
        TimeOnly.TryParseExact(time, ["HH:mm:ss'Z'", "HH:mm'Z'"], CultureInfo.InvariantCulture, DateTimeStyles.None, out var clock)
            ? DateTime.SpecifyKind(date.ToDateTime(clock), DateTimeKind.Utc) : null;

    public static IReadOnlyList<F1Session> Sessions(F1Race race)
    {
        var sessions = new List<F1Session>();
        void Add(string name, string? json)
        {
            if (string.IsNullOrWhiteSpace(json)) return;
            try
            {
                using var doc = JsonDocument.Parse(json);
                if (doc.RootElement.ValueKind != JsonValueKind.Object || !doc.RootElement.TryGetProperty("date", out var d)
                    || d.ValueKind != JsonValueKind.String || !DateOnly.TryParseExact(d.GetString(), "yyyy-MM-dd", out var date)) return;
                var time = doc.RootElement.TryGetProperty("time", out var t) && t.ValueKind == JsonValueKind.String ? t.GetString() : null;
                sessions.Add(new(name, date, StartsUtc(date, time)));
            }
            catch (JsonException) { }
        }
        Add("Practice 1", race.FirstPractice); Add("Practice 2", race.SecondPractice); Add("Practice 3", race.ThirdPractice);
        Add("Qualifying", race.Qualifying); Add("Sprint qualifying", race.SprintQualifying); Add("Sprint qualifying", race.SprintShootout); Add("Sprint", race.Sprint);
        sessions.Add(new("Grand Prix", race.RaceDate, StartsUtc(race.RaceDate, race.RaceTimeUtc)));
        return sessions.DistinctBy(s => (s.Name, s.Date, s.StartsUtc)).OrderBy(s => s.Date).ThenBy(s => s.StartsUtc is null)
            .ThenBy(s => s.StartsUtc).ThenBy(s => s.Name, StringComparer.Ordinal).ToList();
    }

    public static DateOnly LocalDate(F1Session session, FeedDates dates) => session.StartsUtc is { } utc
        ? DateOnly.ParseExact(dates.DayKey(utc), "yyyy-MM-dd") : session.Date;
    public static bool Upcoming(F1Race race, FeedDates dates, DateOnly today) => Sessions(race).Any(s => LocalDate(s, dates) >= today);
    public static string Points(decimal points) => points.ToString("0.###", CultureInfo.InvariantCulture);
    public static string Position(int position, string? text) => string.IsNullOrWhiteSpace(text) ? position.ToString(CultureInfo.InvariantCulture) : text;
    public static string Grid(int grid) => grid > 0 ? grid.ToString(CultureInfo.InvariantCulture) : "Pit lane / unavailable";
    public static string? Freshness(F1Page page, DateTime now) => !string.IsNullOrWhiteSpace(page.Source?.LastError)
        ? "Latest refresh failed; showing the last available snapshot when present."
        : page.FetchedAt is { } fetched && now - DateTime.SpecifyKind(fetched, DateTimeKind.Utc) > TimeSpan.FromHours(2)
            ? "F1 data may be out of date." : null;
    public static string Constructors(string? json)
    {
        try
        {
            using var doc = JsonDocument.Parse(json ?? "[]");
            if (doc.RootElement.ValueKind != JsonValueKind.Array) return "—";
            var names = doc.RootElement.EnumerateArray().Where(c => c.ValueKind == JsonValueKind.Object)
                .Select(c => c.TryGetProperty("name", out var n) && n.ValueKind == JsonValueKind.String ? n.GetString() : null)
                .Where(n => !string.IsNullOrWhiteSpace(n)).Distinct().ToArray();
            return names.Length == 0 ? "—" : string.Join(", ", names);
        }
        catch (JsonException) { return "—"; }
    }
}
