using System.Data;
using System.Globalization;
using Microsoft.EntityFrameworkCore;

namespace myNewspaper.Web.Data;

public sealed record BoxOfficeMovie(int Rank, DateOnly WeekendStart, string Title, long WeekendGross,
    long TotalDomesticGross, decimal? WeeklyChangePercent, int? Theaters, string? MovieUrl,
    string? SourceUrl, DateTime FetchedAt, string? PosterUrl = null, string? PosterLookupStatus = null,
    string? ResultStatus = null, string? StatusEvidenceUrl = null, DateTime? StatusAsOfUtc = null);

public static class BoxOfficeChartQuery
{
    private static readonly CultureInfo Us = CultureInfo.GetCultureInfo("en-US");
    public static string StatusLabel(string? status) => status switch
    {
        "estimated" => "Estimated",
        "final" => "Final",
        _ => "Reported totals"
    };
    public static string Money(long value) => value.ToString("C0", Us);
    /// <summary>Short money for agate tables: "$19.5M", "$718K", "$950".</summary>
    public static string Compact(long value) => value switch
    {
        >= 1_000_000 => "$" + (value / 1_000_000m).ToString("0.#", Us) + "M",
        >= 1_000 => "$" + (value / 1_000m).ToString("0", Us) + "K",
        _ => value.ToString("C0", Us),
    };
    public static string Change(decimal? value) => value is { } change ? change.ToString("+0.##;-0.##;0", Us) + "%" : "—";
    public static bool IsStale(DateOnly friday, DateOnly today) => today.DayNumber - friday.DayNumber > 10;
    public static string WeekendLabel(DateOnly friday) => friday.Year != friday.AddDays(2).Year
        ? $"{friday.ToString("MMMM d, yyyy", Us)}–{friday.AddDays(2).ToString("MMMM d, yyyy", Us)}"
        : friday.Month == friday.AddDays(2).Month
        ? $"{friday.ToString("MMMM d", Us)}–{friday.AddDays(2).ToString("d, yyyy", Us)}"
        : $"{friday.ToString("MMMM d", Us)}–{friday.AddDays(2).ToString("MMMM d, yyyy", Us)}";
    public static string? SafeUrl(string? value) => Uri.TryCreate(value, UriKind.Absolute, out var uri)
        && uri.Scheme is "http" or "https" ? uri.AbsoluteUri : null;

    public static async Task<IReadOnlyList<BoxOfficeMovie>> GetAsync(WebScraperContext db, DateOnly? expectedFriday = null)
    {
        await db.Database.OpenConnectionAsync();
        try
        {
            await using var command = db.Database.GetDbConnection().CreateCommand();
            command.CommandText = "SELECT TOP (10) Rank, WeekendStart, MovieTitle, WeekendGross, TotalDomesticGross, WeeklyChangePercent, Theaters, MovieUrl, SourceUrl, FetchedAt, PosterUrl, PosterLookupStatus, ResultStatus, COALESCE(StatusSourceUrl, SourceUrl), StatusAsOfUtc FROM dbo.BoxOfficeWeekend"
                + (expectedFriday.HasValue ? " WHERE WeekendStart = @ExpectedFriday" : "") + " ORDER BY Rank";
            if (expectedFriday is { } friday)
            {
                var parameter = command.CreateParameter();
                parameter.ParameterName = "@ExpectedFriday"; parameter.DbType = DbType.Date;
                parameter.Value = friday.ToDateTime(TimeOnly.MinValue); command.Parameters.Add(parameter);
            }
            await using var reader = await command.ExecuteReaderAsync();
            var rows = new List<BoxOfficeMovie>();
            while (await reader.ReadAsync())
            {
                rows.Add(new(Convert.ToInt32(reader.GetValue(0), CultureInfo.InvariantCulture), DateOnly.FromDateTime(reader.GetDateTime(1)),
                    reader.GetString(2), Convert.ToInt64(reader.GetValue(3), CultureInfo.InvariantCulture), Convert.ToInt64(reader.GetValue(4), CultureInfo.InvariantCulture),
                    reader.IsDBNull(5) ? null : Convert.ToDecimal(reader.GetValue(5), CultureInfo.InvariantCulture),
                    reader.IsDBNull(6) ? null : Convert.ToInt32(reader.GetValue(6), CultureInfo.InvariantCulture),
                    reader.IsDBNull(7) ? null : SafeUrl(reader.GetString(7)),
                    reader.IsDBNull(8) ? null : SafeUrl(reader.GetString(8)),
                    DateTime.SpecifyKind(reader.GetDateTime(9), DateTimeKind.Utc),
                    reader.IsDBNull(10) ? null : SafeUrl(reader.GetString(10)),
                    reader.IsDBNull(11) ? null : reader.GetString(11),
                    reader.IsDBNull(12) ? null : reader.GetString(12),
                    reader.IsDBNull(13) ? null : SafeUrl(reader.GetString(13)),
                    reader.IsDBNull(14) ? null : DateTime.SpecifyKind(reader.GetDateTime(14), DateTimeKind.Utc)));
            }
            return rows;
        }
        finally { await db.Database.CloseConnectionAsync(); }
    }
}
