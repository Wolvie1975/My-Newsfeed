using System.Data;
using System.Globalization;
using Microsoft.EntityFrameworkCore;

namespace myNewspaper.Web.Data;

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
    public static bool IsRecent(BoxOfficeForecastSource? source, DateTime utcNow) =>
        source is { Failed: false, LastSuccess: { } success } && success <= utcNow
        && utcNow - success <= TimeSpan.FromHours(24);
    public static string EmptyMessage(string kind, BoxOfficeForecastSource? source, DateTime utcNow) =>
        IsRecent(source, utcNow)
            ? kind == Preview ? "The next weekend preview has not been published yet" : "The next future-film forecasts have not been published yet"
            : kind == Preview ? "Preview data is temporarily unavailable" : "Future-film forecast data is temporarily unavailable";
    public static string Range(long low, long high) => $"{Amount(low)}–{Amount(high)}";
    private static string Amount(long value) => value >= 1_000_000
        ? "$" + (value / 1_000_000m).ToString("0.######", CultureInfo.GetCultureInfo("en-US")) + "M"
        : BoxOfficeChartQuery.Money(value);
    public static string DateLabel(DateOnly date) => date.ToString("MMMM d, yyyy", CultureInfo.GetCultureInfo("en-US"));

    public static async Task<BoxOfficeForecastSnapshot> GetAsync(WebScraperContext db, DateOnly today)
    {
        await db.Database.OpenConnectionAsync();
        try
        {
            await using var command = db.Database.GetDbConnection().CreateCommand();
            command.CommandText = "SELECT Kind, TargetDate, PeriodEnd, MovieTitle, Distributor, PredictedRank, ForecastLow, ForecastHigh, ForecastAsOf, SourceUrl, FetchedAtUtc, PosterUrl, PosterLookupStatus FROM dbo.BoxOfficeForecasts WHERE PeriodEnd >= @Today ORDER BY Kind, TargetDate, PredictedRank, MovieTitle; SELECT Kind, LastSuccessAt, LastError FROM dbo.BoxOfficeForecastSources;";
            var parameter = command.CreateParameter();
            parameter.ParameterName = "@Today"; parameter.DbType = DbType.Date;
            parameter.Value = today.ToDateTime(TimeOnly.MinValue); command.Parameters.Add(parameter);
            await using var reader = await command.ExecuteReaderAsync();
            var movies = new List<BoxOfficeForecast>();
            while (await reader.ReadAsync())
                movies.Add(new(reader.GetString(0), DateOnly.FromDateTime(reader.GetDateTime(1)),
                    DateOnly.FromDateTime(reader.GetDateTime(2)), reader.GetString(3),
                    reader.IsDBNull(4) ? null : reader.GetString(4),
                    reader.IsDBNull(5) ? null : Convert.ToInt32(reader.GetValue(5), CultureInfo.InvariantCulture),
                    Convert.ToInt64(reader.GetValue(6), CultureInfo.InvariantCulture), Convert.ToInt64(reader.GetValue(7), CultureInfo.InvariantCulture),
                    DateOnly.FromDateTime(reader.GetDateTime(8)),
                    reader.IsDBNull(9) ? null : BoxOfficeChartQuery.SafeUrl(reader.GetString(9)),
                    DateTime.SpecifyKind(reader.GetDateTime(10), DateTimeKind.Utc),
                    reader.IsDBNull(11) ? null : BoxOfficeChartQuery.SafeUrl(reader.GetString(11)),
                    reader.IsDBNull(12) ? null : reader.GetString(12)));
            await reader.NextResultAsync();
            var sources = new List<BoxOfficeForecastSource>();
            while (await reader.ReadAsync())
                sources.Add(new(reader.GetString(0), reader.IsDBNull(1) ? null :
                    DateTime.SpecifyKind(reader.GetDateTime(1), DateTimeKind.Utc), !reader.IsDBNull(2)));
            return new(movies, sources);
        }
        finally { await db.Database.CloseConnectionAsync(); }
    }
}
