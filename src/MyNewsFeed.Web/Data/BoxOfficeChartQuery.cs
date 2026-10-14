using System.Globalization;
using Microsoft.EntityFrameworkCore;

namespace MyNewsFeed.Web.Data;

public sealed record BoxOfficeMovie(int Rank, DateOnly WeekendStart, string Title, long WeekendGross,
    long TotalDomesticGross, decimal? WeeklyChangePercent, int? Theaters, string? MovieUrl,
    string? SourceUrl, DateTime FetchedAt, string? PosterUrl = null, string? PosterLookupStatus = null);

public static class BoxOfficeChartQuery
{
    private static readonly CultureInfo Us = CultureInfo.GetCultureInfo("en-US");
    public static string Money(long value) => value.ToString("C0", Us);
    public static string Change(decimal? value) => value is { } change ? change.ToString("+0.##;-0.##;0", Us) + "%" : "—";
    public static bool IsStale(DateOnly friday, DateOnly today) => today.DayNumber - friday.DayNumber > 10;
    public static string WeekendLabel(DateOnly friday) => friday.Year != friday.AddDays(2).Year
        ? $"{friday.ToString("MMMM d, yyyy", Us)}–{friday.AddDays(2).ToString("MMMM d, yyyy", Us)}"
        : friday.Month == friday.AddDays(2).Month
        ? $"{friday.ToString("MMMM d", Us)}–{friday.AddDays(2).ToString("d, yyyy", Us)}"
        : $"{friday.ToString("MMMM d", Us)}–{friday.AddDays(2).ToString("MMMM d, yyyy", Us)}";
    public static string? SafeUrl(string? value) => Uri.TryCreate(value, UriKind.Absolute, out var uri)
        && uri.Scheme is "http" or "https" ? uri.AbsoluteUri : null;

    public static async Task<IReadOnlyList<BoxOfficeMovie>> GetAsync(WebScraperContext db)
    {
        await db.Database.OpenConnectionAsync();
        try
        {
            await using var command = db.Database.GetDbConnection().CreateCommand();
            command.CommandText = "SELECT TOP (10) Rank, WeekendStart, MovieTitle, WeekendGross, TotalDomesticGross, WeeklyChangePercent, Theaters, MovieUrl, SourceUrl, FetchedAt, PosterUrl, PosterLookupStatus FROM dbo.BoxOfficeWeekend ORDER BY Rank";
            await using var reader = await command.ExecuteReaderAsync();
            var rows = new List<BoxOfficeMovie>();
            while (await reader.ReadAsync())
            {
                rows.Add(new(Convert.ToInt32(reader.GetValue(0)), DateOnly.FromDateTime(reader.GetDateTime(1)),
                    reader.GetString(2), Convert.ToInt64(reader.GetValue(3)), Convert.ToInt64(reader.GetValue(4)),
                    reader.IsDBNull(5) ? null : Convert.ToDecimal(reader.GetValue(5)),
                    reader.IsDBNull(6) ? null : Convert.ToInt32(reader.GetValue(6)),
                    reader.IsDBNull(7) ? null : SafeUrl(reader.GetString(7)),
                    reader.IsDBNull(8) ? null : SafeUrl(reader.GetString(8)),
                    DateTime.SpecifyKind(reader.GetDateTime(9), DateTimeKind.Utc),
                    reader.IsDBNull(10) ? null : SafeUrl(reader.GetString(10)),
                    reader.IsDBNull(11) ? null : reader.GetString(11)));
            }
            return rows;
        }
        finally { await db.Database.CloseConnectionAsync(); }
    }
}
