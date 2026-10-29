using Microsoft.EntityFrameworkCore;

namespace myNewspaper.Web.Data;

public sealed record MovieReleaseItem(int Id, string Title, short? Year, string? Description, string? Poster,
    string? Certification, short? Runtime, DateOnly? Date, string DateStatus, string Type, string? Platform,
    string? Format, bool ReRelease, string Source, string SourceUrl,
    string? MovieActionUrl = null, string? MovieActionLabel = null, string? MovieActionKind = null);
public sealed record MoviesPage(IReadOnlyList<MovieReleaseItem> Items, int Total, IReadOnlyList<string> Platforms, DateTime? Updated);

public static class MoviesQuery
{
    public static readonly IReadOnlyDictionary<string, string> Types = new Dictionary<string, string>
    {
        ["theatrical"] = "In theaters",
        ["subscription"] = "Streaming",
        ["digital"] = "Digital",
        ["digital_rental"] = "Digital rental",
        ["digital_purchase"] = "Digital purchase",
        ["disc"] = "On disc"
    };

    public static Task<MoviesPage> GetAsync(WebScraperContext db, DateOnly today, string? search,
        string? type, string? platform, string? period, int offset = 0, int take = 50, DateOnly? onDate = null, DateOnly? through = null) =>
        GetCoreAsync(db, today, search, type, platform, period, offset, take, onDate, false, through);

    public static async Task<IReadOnlyList<MovieReleaseItem>> GetTodayTimelineAsync(WebScraperContext db, DateOnly today) =>
        (await GetCoreAsync(db, today, null, null, null, null, 0, 200, today, true)).Items;

    private static async Task<MoviesPage> GetCoreAsync(WebScraperContext db, DateOnly today, string? search,
        string? type, string? platform, string? period, int offset, int take, DateOnly? onDate, bool timelineOnly, DateOnly? through = null)
    {
        var releases = from r in db.MovieReleases.AsNoTracking()
                       join link in db.MovieSourceLinks on r.MovieSourceLinkId equals link.ID
                       join movie in db.Movies on link.MovieId equals movie.ID
                       join source in db.MovieReleaseSources on link.MovieReleaseSourceId equals source.ID
                       where source.Enabled && r.CountryCode == "US" && r.ReleaseStatus == "scheduled"
                       select new { r, movie, source };
        if (timelineOnly) releases = releases.Where(x => x.r.ReleaseType != "digital"
            && x.r.ReleaseType != "digital_rental" && x.r.ReleaseType != "digital_purchase");
        var platforms = timelineOnly ? new List<string>() : await releases.Where(x => x.r.Platform != null && x.r.Platform != "")
            .Select(x => x.r.Platform!).Distinct().OrderBy(x => x).ToListAsync();
        var updated = timelineOnly ? (DateTime?)null : await releases.MaxAsync(x => (DateTime?)x.r.LastSeenAt);
        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            releases = releases.Where(x => x.movie.Title.Contains(term));
        }
        if (!string.IsNullOrWhiteSpace(type)) releases = releases.Where(x => x.r.ReleaseType == type.Trim());
        if (!string.IsNullOrWhiteSpace(platform)) releases = releases.Where(x => x.r.Platform == platform.Trim());
        if (period == "month")
        {
            var monthEnd = new DateOnly(today.Year, today.Month, DateTime.DaysInMonth(today.Year, today.Month));
            releases = releases.Where(x => x.r.ReleaseDate >= today && x.r.ReleaseDate <= monthEnd);
        }
        else if (period == "tbd") releases = releases.Where(x => x.r.ReleaseDate == null);
        else if (period == "recent")
        {
            var start = today.AddDays(-30);
            releases = releases.Where(x => x.r.ReleaseDate >= start && x.r.ReleaseDate < today);
        }
        else if (period != "all") releases = releases.Where(x => x.r.ReleaseDate >= today || x.r.ReleaseDate == null);
        if (through is { } last) releases = releases.Where(x => x.r.ReleaseDate <= last);
        if (onDate is { } date) releases = releases.Where(x => x.r.ReleaseDate == date);
        // Combine format/source observations before counting and paging, rather than hiding
        // duplicate cards after Take(), which would leave incomplete pages and inflated totals.
        var representatives = releases.GroupBy(x => new { MovieId = x.movie.ID, x.r.ReleaseDate, x.r.ReleaseType, x.r.IsReRelease })
            .Select(g => g.Min(x => x.r.ID));
        var total = timelineOnly ? 0 : await representatives.CountAsync();
        var rows = await releases.Where(x => representatives.Contains(x.r.ID))
            .OrderBy(x => x.r.ReleaseDate == null).ThenBy(x => x.r.ReleaseDate)
            .ThenBy(x => x.movie.Title).ThenBy(x => x.r.ID).Skip(Math.Max(0, offset)).Take(take)
            .Select(x => new
            {
                MovieId = x.movie.ID,
                Item = new MovieReleaseItem(x.r.ID, x.movie.Title, x.movie.OriginalYear, x.movie.Description,
                x.movie.PosterUrl, x.movie.Certification, x.movie.RuntimeMinutes, x.r.ReleaseDate, x.r.DateStatus,
                x.r.ReleaseType, x.r.Platform, x.r.Format, x.r.IsReRelease, x.source.Name, x.r.SourceUrl)
            }).ToListAsync();
        var movieIds = rows.Select(x => x.MovieId).Distinct().ToArray();
        var variants = await releases.Where(x => movieIds.Contains(x.movie.ID))
            .Select(x => new { MovieId = x.movie.ID, x.r.ReleaseDate, x.r.ReleaseType, x.r.IsReRelease, x.r.Format, x.r.Platform, x.r.DateStatus })
            .ToListAsync();
        var byRelease = variants.ToLookup(x => (x.MovieId, x.ReleaseDate, x.ReleaseType, x.IsReRelease));
        var items = rows.Select(row =>
        {
            var matches = byRelease[(row.MovieId, row.Item.Date, row.Item.Type, row.Item.ReRelease)];
            return row.Item with
            {
                Format = CombineDetails(matches.Select(x => x.Format)),
                Platform = CombineDetails(matches.Select(x => x.Platform)),
                DateStatus = matches.Any(x => x.DateStatus == "announced") ? "announced" : row.Item.DateStatus
            };
        }).ToList();
        return new(await MovieCommerceQuery.EnrichAsync(db, items), total, platforms, updated);
    }
    private static string? CombineDetails(IEnumerable<string?> values)
    {
        var details = values.Where(v => !string.IsNullOrWhiteSpace(v)).Select(v => v!.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase).OrderBy(v => v, StringComparer.OrdinalIgnoreCase).ToArray();
        return details.Length == 0 ? null : string.Join(" · ", details);
    }

    public static DateOnly? HomeTheatricalFriday(DateOnly today) => today.DayOfWeek switch
    {
        DayOfWeek.Thursday => today.AddDays(1),
        DayOfWeek.Friday => today,
        DayOfWeek.Saturday => today.AddDays(-1),
        DayOfWeek.Sunday => today.AddDays(-2),
        _ => null
    };

    /// <summary>Current Friday releases during Thursday–Sunday, one entry per movie across enabled sources.</summary>
    public static async Task<IReadOnlyList<MovieReleaseItem>> GetFridayTheatricalAsync(
        WebScraperContext db, DateOnly today, int take = 5)
    {
        if (HomeTheatricalFriday(today) is not { } friday) return [];
        var eligible = from r in db.MovieReleases.AsNoTracking()
                       join link in db.MovieSourceLinks on r.MovieSourceLinkId equals link.ID
                       join movie in db.Movies on link.MovieId equals movie.ID
                       join source in db.MovieReleaseSources on link.MovieReleaseSourceId equals source.ID
                       where source.Enabled && r.CountryCode == "US" && r.ReleaseStatus == "scheduled"
                           && r.ReleaseType == "theatrical" && r.ReleaseDate == friday
                       select new { r, link.MovieId, movie, source };
        var uniqueIds = eligible.GroupBy(x => new { x.MovieId, x.r.ReleaseDate }).Select(g => g.Min(x => x.r.ID));
        var items = await eligible.Where(x => uniqueIds.Contains(x.r.ID))
            .OrderBy(x => x.r.ReleaseDate).ThenBy(x => x.movie.Title).ThenBy(x => x.r.ID).Take(take)
            .Select(x => new MovieReleaseItem(x.r.ID, x.movie.Title, x.movie.OriginalYear, x.movie.Description,
                x.movie.PosterUrl, x.movie.Certification, x.movie.RuntimeMinutes, x.r.ReleaseDate, x.r.DateStatus,
                x.r.ReleaseType, x.r.Platform, x.r.Format, x.r.IsReRelease, x.source.Name, x.r.SourceUrl))
            .ToListAsync();
        return await MovieCommerceQuery.EnrichAsync(db, items);
    }

    /// <summary>Upcoming dated digital releases, one entry per movie and date across enabled sources.</summary>
    public static async Task<IReadOnlyList<MovieReleaseItem>> GetUpcomingDigitalAsync(
        WebScraperContext db, DateOnly today, int take = 5)
    {
        var eligible = from r in db.MovieReleases.AsNoTracking()
                       join link in db.MovieSourceLinks on r.MovieSourceLinkId equals link.ID
                       join movie in db.Movies on link.MovieId equals movie.ID
                       join source in db.MovieReleaseSources on link.MovieReleaseSourceId equals source.ID
                       where source.Enabled && r.CountryCode == "US" && r.ReleaseStatus == "scheduled"
                           && (r.ReleaseType == "digital" || r.ReleaseType == "digital_rental" || r.ReleaseType == "digital_purchase") && r.ReleaseDate >= today

                       select new { r, link.MovieId, movie, source };
        var uniqueIds = eligible.GroupBy(x => new { x.MovieId, x.r.ReleaseDate }).Select(g => g.Min(x => x.r.ID));
        var items = await eligible.Where(x => uniqueIds.Contains(x.r.ID))
            .OrderBy(x => x.r.ReleaseDate).ThenBy(x => x.movie.Title).ThenBy(x => x.r.ID).Take(take)
            .Select(x => new MovieReleaseItem(x.r.ID, x.movie.Title, x.movie.OriginalYear, x.movie.Description,
                x.movie.PosterUrl, x.movie.Certification, x.movie.RuntimeMinutes, x.r.ReleaseDate, x.r.DateStatus,
                x.r.ReleaseType, x.r.Platform, x.r.Format, x.r.IsReRelease, x.source.Name, x.r.SourceUrl))
            .ToListAsync();
        return await MovieCommerceQuery.EnrichAsync(db, items);
    }

}
