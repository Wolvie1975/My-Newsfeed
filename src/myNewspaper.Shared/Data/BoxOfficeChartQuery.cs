using System.Globalization;
using Microsoft.EntityFrameworkCore;

namespace myNewspaper.Shared.Data;

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

    public static async Task<IReadOnlyList<BoxOfficeMovie>> GetAsync(WebScraperContext db, DateOnly? expectedFriday = null,
        CancellationToken ct = default)
    {
        var query = db.Database.SqlQuery<WeekendRow>($"""
            SELECT Rank, WeekendStart, MovieTitle, WeekendGross, TotalDomesticGross, WeeklyChangePercent, Theaters, MovieUrl,
                   SourceUrl, FetchedAt, PosterUrl, PosterLookupStatus, ResultStatus, StatusSourceUrl, StatusAsOfUtc
            FROM dbo.BoxOfficeWeekend
            """);
        if (expectedFriday is { } friday) query = query.Where(r => r.WeekendStart == friday);
        var rows = await query.OrderBy(r => r.Rank).Take(10).ToListAsync(ct);
        return rows.Select(r => new BoxOfficeMovie(r.Rank, r.WeekendStart, r.MovieTitle, r.WeekendGross, r.TotalDomesticGross,
            r.WeeklyChangePercent is { } change ? (decimal)change : null, r.Theaters, SafeUrl(r.MovieUrl), SafeUrl(r.SourceUrl),
            DateTime.SpecifyKind(r.FetchedAt, DateTimeKind.Utc), SafeUrl(r.PosterUrl), r.PosterLookupStatus, r.ResultStatus,
            SafeUrl(r.StatusSourceUrl ?? r.SourceUrl),
            r.StatusAsOfUtc is { } asOf ? DateTime.SpecifyKind(asOf, DateTimeKind.Utc) : null)).ToList();
    }

    /// <summary>A row of the scraper-owned <c>dbo.BoxOfficeWeekend</c> table, as stored.</summary>
    private sealed class WeekendRow
    {
        public int Rank { get; set; }
        public DateOnly WeekendStart { get; set; }
        public string MovieTitle { get; set; } = "";
        public long WeekendGross { get; set; }
        public long TotalDomesticGross { get; set; }
        public double? WeeklyChangePercent { get; set; }
        public int? Theaters { get; set; }
        public string? MovieUrl { get; set; }
        public string? SourceUrl { get; set; }
        public DateTime FetchedAt { get; set; }
        public string? PosterUrl { get; set; }
        public string? PosterLookupStatus { get; set; }
        public string? ResultStatus { get; set; }
        public string? StatusSourceUrl { get; set; }
        public DateTime? StatusAsOfUtc { get; set; }
    }
}
