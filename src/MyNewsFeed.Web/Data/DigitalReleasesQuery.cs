using Microsoft.EntityFrameworkCore;
using MyNewsFeed.Web.Data.Models;

namespace MyNewsFeed.Web.Data;

public sealed record DigitalReleasePage(IReadOnlyList<MovieReleaseSource> Sources, MovieReleaseSource? Selected,
    DateOnly StartDate, IReadOnlyList<MovieReleaseItem> Items);

public static class DigitalReleasesQuery
{
    public static MovieReleaseSource? SelectSource(IEnumerable<MovieReleaseSource> sources, string? key)
    {
        var enabled = sources.Where(s => s.Enabled && s.SourceKey is "movieinsider" or "whentostream" or "dvdsreleasedates")
            .OrderBy(s => s.Name, StringComparer.OrdinalIgnoreCase).ThenBy(s => s.ID).ToList();
        return enabled.FirstOrDefault(s => s.SourceKey == key)
            ?? enabled.FirstOrDefault(s => s.SourceKey == "movieinsider") ?? enabled.FirstOrDefault();
    }
    public static async Task<DigitalReleasePage> GetAsync(WebScraperContext db, DateOnly today, string? sourceKey, int? take = null)
    {
        var sources = await db.MovieReleaseSources.AsNoTracking().Where(s => s.Enabled
            && (s.SourceKey == "movieinsider" || s.SourceKey == "whentostream" || s.SourceKey == "dvdsreleasedates"))
            .OrderBy(s => s.Name).ThenBy(s => s.ID).ToListAsync();
        var selected = SelectSource(sources, sourceKey);
        if (selected is null) return new(sources, null, today, []);
        var query = (from r in db.MovieReleases.AsNoTracking()
            join link in db.MovieSourceLinks on r.MovieSourceLinkId equals link.ID
            join movie in db.Movies on link.MovieId equals movie.ID
            join source in db.MovieReleaseSources on link.MovieReleaseSourceId equals source.ID
            where source.Enabled && source.SourceKey == selected.SourceKey && r.CountryCode == "US"
                && (r.ReleaseType == "digital" || r.ReleaseType == "digital_rental" || r.ReleaseType == "digital_purchase")
                && r.DateStatus == "announced" && r.ReleaseStatus != "cancelled" && r.ReleaseStatus != "withdrawn"
                && r.ReleaseDate >= today
            orderby r.ReleaseDate, movie.Title, r.ID
            select new MovieReleaseItem(r.ID, movie.Title, movie.OriginalYear, movie.Description, movie.PosterUrl,
                movie.Certification, movie.RuntimeMinutes, r.ReleaseDate, r.DateStatus, r.ReleaseType,
                r.Platform, r.Format, r.IsReRelease, source.Name, r.SourceUrl));
        if (take is { } limit) query = query.Take(Math.Max(0, limit));
        var items = await query.ToListAsync();
        return new(sources, selected, today, items);
    }
}
