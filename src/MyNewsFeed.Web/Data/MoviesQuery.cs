using Microsoft.EntityFrameworkCore;

namespace MyNewsFeed.Web.Data;

public sealed record MovieReleaseItem(int Id, string Title, short? Year, string? Description, string? Poster,
    string? Certification, short? Runtime, DateOnly? Date, string DateStatus, string Type, string? Platform,
    string? Format, bool ReRelease, string Source, string SourceUrl);
public sealed record MoviesPage(IReadOnlyList<MovieReleaseItem> Items, int Total, IReadOnlyList<string> Platforms, DateTime? Updated);

public static class MoviesQuery
{
    public static readonly IReadOnlyDictionary<string, string> Types = new Dictionary<string, string>
    {
        ["theatrical"] = "In theaters", ["subscription"] = "Streaming", ["digital"] = "Digital",
        ["digital_rental"] = "Digital rental", ["digital_purchase"] = "Digital purchase", ["disc"] = "On disc"
    };

    public static async Task<MoviesPage> GetAsync(WebScraperContext db, DateOnly today, string? search,
        string? type, string? platform, string? period)
    {
        var releases = from r in db.MovieReleases.AsNoTracking()
                       join link in db.MovieSourceLinks on r.MovieSourceLinkId equals link.ID
                       join movie in db.Movies on link.MovieId equals movie.ID
                       join source in db.MovieReleaseSources on link.MovieReleaseSourceId equals source.ID
                       where source.Enabled && r.CountryCode == "US" && r.ReleaseStatus == "scheduled"
                       select new { r, movie, source };
        var platforms = await releases.Where(x => x.r.Platform != null && x.r.Platform != "")
            .Select(x => x.r.Platform!).Distinct().OrderBy(x => x).ToListAsync();
        var updated = await releases.MaxAsync(x => (DateTime?)x.r.LastSeenAt);
        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            releases = releases.Where(x => x.movie.Title.Contains(term));
        }
        if (!string.IsNullOrWhiteSpace(type)) releases = releases.Where(x => x.r.ReleaseType == type.Trim());
        if (!string.IsNullOrWhiteSpace(platform)) releases = releases.Where(x => x.r.Platform == platform.Trim());
        if (period == "tbd") releases = releases.Where(x => x.r.ReleaseDate == null);
        else if (period == "recent")
        {
            var start = today.AddDays(-30);
            releases = releases.Where(x => x.r.ReleaseDate >= start && x.r.ReleaseDate < today);
        }
        else if (period != "all") releases = releases.Where(x => x.r.ReleaseDate >= today || x.r.ReleaseDate == null);
        var total = await releases.CountAsync();
        var items = await releases.OrderBy(x => x.r.ReleaseDate == null).ThenBy(x => x.r.ReleaseDate)
            .ThenBy(x => x.movie.Title).ThenBy(x => x.r.ID).Take(200)
            .Select(x => new MovieReleaseItem(x.r.ID, x.movie.Title, x.movie.OriginalYear, x.movie.Description,
                x.movie.PosterUrl, x.movie.Certification, x.movie.RuntimeMinutes, x.r.ReleaseDate, x.r.DateStatus,
                x.r.ReleaseType, x.r.Platform, x.r.Format, x.r.IsReRelease, x.source.Name, x.r.SourceUrl)).ToListAsync();
        return new(items, total, platforms, updated);
    }
    /// <summary>Upcoming Friday theatrical releases, one entry per movie and date across enabled sources.</summary>
    public static async Task<IReadOnlyList<MovieReleaseItem>> GetFridayTheatricalAsync(
        WebScraperContext db, DateOnly today, int take = 5)
    {
        // A fixed Friday avoids dependence on SQL Server's DATEFIRST setting.
        var friday = new DateOnly(1970, 1, 2);
        var eligible = from r in db.MovieReleases.AsNoTracking()
                       join link in db.MovieSourceLinks on r.MovieSourceLinkId equals link.ID
                       join movie in db.Movies on link.MovieId equals movie.ID
                       join source in db.MovieReleaseSources on link.MovieReleaseSourceId equals source.ID
                       where source.Enabled && r.CountryCode == "US" && r.ReleaseStatus == "scheduled"
                           && r.ReleaseType == "theatrical" && r.ReleaseDate >= today
                           && EF.Functions.DateDiffDay(friday, r.ReleaseDate) % 7 == 0
                       select new { r, link.MovieId, movie, source };
        var uniqueIds = eligible.GroupBy(x => new { x.MovieId, x.r.ReleaseDate }).Select(g => g.Min(x => x.r.ID));
        return await eligible.Where(x => uniqueIds.Contains(x.r.ID))
            .OrderBy(x => x.r.ReleaseDate).ThenBy(x => x.movie.Title).ThenBy(x => x.r.ID).Take(take)
            .Select(x => new MovieReleaseItem(x.r.ID, x.movie.Title, x.movie.OriginalYear, x.movie.Description,
                x.movie.PosterUrl, x.movie.Certification, x.movie.RuntimeMinutes, x.r.ReleaseDate, x.r.DateStatus,
                x.r.ReleaseType, x.r.Platform, x.r.Format, x.r.IsReRelease, x.source.Name, x.r.SourceUrl))
            .ToListAsync();
    }

}
