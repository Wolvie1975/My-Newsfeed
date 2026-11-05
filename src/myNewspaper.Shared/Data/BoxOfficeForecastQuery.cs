using System.Globalization;
using Microsoft.EntityFrameworkCore;

namespace myNewspaper.Shared.Data;

public sealed record BoxOfficeForecast(string Kind, DateOnly TargetDate, DateOnly PeriodEnd,
    string Title, string? Distributor, int? PredictedRank, long Low, long High,
    DateOnly AsOf, string? SourceUrl, DateTime FetchedAt, string? PosterUrl = null, string? PosterLookupStatus = null);
public sealed record BoxOfficeForecastSource(string Kind, DateTime? LastSuccess, bool Failed);
public sealed record BoxOfficeForecastSnapshot(IReadOnlyList<BoxOfficeForecast> Movies,
    IReadOnlyList<BoxOfficeForecastSource> Sources);

public static class BoxOfficeForecastQuery
{
    public const string Preview = "weekend-preview";
    public const string LongRange = "long-range";
    // Hourly collection can miss a few runs without misrepresenting a publication gap.
    public static bool IsRecent(BoxOfficeForecastSource? source, DateTime nowUtc) =>
        source is { Failed: false, LastSuccess: { } success } && success <= nowUtc
        && nowUtc - success <= TimeSpan.FromHours(24);
    public static string EmptyMessage(string kind, BoxOfficeForecastSource? source, DateTime nowUtc) =>
        IsRecent(source, nowUtc)
            ? kind == Preview ? "The next weekend preview has not been published yet" : "The next future-film forecasts have not been published yet"
            : kind == Preview ? "Preview data is temporarily unavailable" : "Future-film forecast data is temporarily unavailable";
    public static string Range(long low, long high) => $"{Amount(low)}–{Amount(high)}";
    private static string Amount(long value) => value >= 1_000_000
        ? "$" + (value / 1_000_000m).ToString("0.######", CultureInfo.GetCultureInfo("en-US")) + "M"
        : BoxOfficeChartQuery.Money(value);
    public static string DateLabel(DateOnly date) => date.ToString("MMMM d, yyyy", CultureInfo.GetCultureInfo("en-US"));

    /// <summary>
    /// The weekend-preview predictions for one weekend, keyed by a normalised title, so reported results can show what
    /// was predicted. Only the current preview is stored, so older weekends simply have no predictions.
    /// </summary>
    public static async Task<IReadOnlyDictionary<string, BoxOfficeForecast>> PredictionsAsync(WebScraperContext db, DateOnly friday, CancellationToken ct = default)
    {
        var rows = await db.Database.SqlQuery<PredictionRow>($"""
            SELECT MovieTitle, ForecastLow, ForecastHigh, PredictedRank
            FROM dbo.BoxOfficeForecasts WHERE Kind = {Preview} AND TargetDate = {friday.ToDateTime(TimeOnly.MinValue)}
            """).ToListAsync(ct);
        return rows.GroupBy(r => TitleKey(r.MovieTitle)).ToDictionary(g => g.Key, g =>
        {
            var r = g.First();
            return new BoxOfficeForecast(Preview, friday, friday.AddDays(2), r.MovieTitle, null, r.PredictedRank,
                r.ForecastLow, r.ForecastHigh, friday, null, DateTime.MinValue);
        });
    }

    /// <summary>Case- and punctuation-insensitive title key for matching predictions to reported results.</summary>
    public static string TitleKey(string title) =>
        new string(title.Where(char.IsLetterOrDigit).Select(char.ToLowerInvariant).ToArray());

    private sealed record PredictionRow(string MovieTitle, long ForecastLow, long ForecastHigh, int? PredictedRank);

    public static async Task<BoxOfficeForecastSnapshot> GetAsync(WebScraperContext db, DateOnly today, CancellationToken ct = default)
    {
        var rows = await db.Database.SqlQuery<ForecastRow>($"""
            SELECT Kind, TargetDate, PeriodEnd, MovieTitle, Distributor, PredictedRank, ForecastLow, ForecastHigh, ForecastAsOf,
                   SourceUrl, FetchedAtUtc, PosterUrl, PosterLookupStatus
            FROM dbo.BoxOfficeForecasts
            """)
            .Where(r => r.PeriodEnd >= today)
            .OrderBy(r => r.Kind).ThenBy(r => r.TargetDate).ThenBy(r => r.PredictedRank).ThenBy(r => r.MovieTitle)
            .ToListAsync(ct);
        var sources = await db.Database.SqlQuery<SourceRow>($"SELECT Kind, LastSuccessAt, LastError FROM dbo.BoxOfficeForecastSources")
            .ToListAsync(ct);
        return new(
            rows.Select(r => new BoxOfficeForecast(r.Kind, r.TargetDate, r.PeriodEnd, r.MovieTitle, r.Distributor, r.PredictedRank,
                r.ForecastLow, r.ForecastHigh, r.ForecastAsOf, BoxOfficeChartQuery.SafeUrl(r.SourceUrl),
                DateTime.SpecifyKind(r.FetchedAtUtc, DateTimeKind.Utc), BoxOfficeChartQuery.SafeUrl(r.PosterUrl), r.PosterLookupStatus)).ToList(),
            sources.Select(s => new BoxOfficeForecastSource(s.Kind,
                s.LastSuccessAt is { } at ? DateTime.SpecifyKind(at, DateTimeKind.Utc) : null, s.LastError is not null)).ToList());
    }

    /// <summary>A row of the scraper-owned <c>dbo.BoxOfficeForecasts</c> table, as stored.</summary>
    private sealed class ForecastRow
    {
        public string Kind { get; set; } = "";
        public DateOnly TargetDate { get; set; }
        public DateOnly PeriodEnd { get; set; }
        public string MovieTitle { get; set; } = "";
        public string? Distributor { get; set; }
        public int? PredictedRank { get; set; }
        public long ForecastLow { get; set; }
        public long ForecastHigh { get; set; }
        public DateOnly ForecastAsOf { get; set; }
        public string? SourceUrl { get; set; }
        public DateTime FetchedAtUtc { get; set; }
        public string? PosterUrl { get; set; }
        public string? PosterLookupStatus { get; set; }
    }

    /// <summary>A row of <c>dbo.BoxOfficeForecastSources</c>.</summary>
    private sealed class SourceRow
    {
        public string Kind { get; set; } = "";
        public DateTime? LastSuccessAt { get; set; }
        public string? LastError { get; set; }
    }
}
