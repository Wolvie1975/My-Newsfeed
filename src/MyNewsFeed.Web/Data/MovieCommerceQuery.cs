using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.WebUtilities;
using System.Text.RegularExpressions;

namespace MyNewsFeed.Web.Data;

[Keyless]
public sealed class MovieCommerceRow
{
    public int MovieId { get; set; }
    public string Provider { get; set; } = "";
    public string? ActionUrl { get; set; }
    public string? ActionLabel { get; set; }
    public string? LinkKind { get; set; }
}

public sealed record MovieAction(string Url, string Label, string Kind);

public static partial class MovieCommerceQuery
{
    public static string? Provider(string type) => type switch
    {
        "theatrical" => "fandango",
        "digital" or "digital_purchase" or "digital_rental" => "fandango-at-home",
        _ => null
    };

    public static MovieAction? Resolve(string title, string type, string? url = null, string? label = null, string? kind = null)
    {
        var provider = Provider(type);
        if (provider is null) return null;
        var theatrical = provider == "fandango";
        var fallback = new MovieAction(theatrical
            ? "https://www.fandango.com/search?q=" + Uri.EscapeDataString(title)
            : "https://athome.fandango.com/content/browse/search?searchString=" + Uri.EscapeDataString(title),
            theatrical ? "Search Fandango" : "Search Fandango at Home", "search");
        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri) || uri.Scheme != "https"
            || uri.UserInfo.Length != 0 || uri.Port != 443 || uri.Fragment.Length != 0) return fallback;
        var hostOk = theatrical ? uri.Host is "www.fandango.com" or "fandango.com" : uri.Host == "athome.fandango.com";
        if (!hostOk) return fallback;
        if (kind == "direct" && uri.Query.Length == 0
            && (theatrical ? TicketPath().IsMatch(uri.AbsolutePath) : DigitalPath().IsMatch(uri.AbsolutePath))
            && (theatrical ? label == "Get tickets on Fandango" : label is "Buy on Fandango at Home" or "View on Fandango at Home"))
            return new(uri.AbsoluteUri, label!, kind);
        if (kind == "search" && uri.Host == (theatrical ? "www.fandango.com" : "athome.fandango.com")
            && uri.AbsolutePath == (theatrical ? "/search" : "/content/browse/search") && label == fallback.Label)
        {
            var query = QueryHelpers.ParseQuery(uri.Query);
            var parameter = theatrical ? "q" : "searchString";
            if (query.Count == 1 && query.TryGetValue(parameter, out var value) && value.Count == 1 && value[0] == title)
                return new(uri.AbsoluteUri, label, kind);
        }
        return fallback;
    }

    [GeneratedRegex(@"^/[^/]+-[0-9]+/movie-overview/?$")]
    private static partial Regex TicketPath();
    [GeneratedRegex(@"^/content/browse/details/[^/]+/[0-9]+/?$")]
    private static partial Regex DigitalPath();

    public static async Task<IReadOnlyList<MovieReleaseItem>> EnrichAsync(WebScraperContext db, IReadOnlyList<MovieReleaseItem> items)
    {
        var ids = items.Where(i => Provider(i.Type) is not null).Select(i => i.Id).Distinct().ToArray();
        if (ids.Length == 0) return items;
        var mappings = await (from release in db.MovieReleases.AsNoTracking()
            join link in db.MovieSourceLinks on release.MovieSourceLinkId equals link.ID
            where ids.Contains(release.ID)
            select new { ReleaseId = release.ID, link.MovieId }).ToListAsync();
        var movieIds = mappings.Select(m => m.MovieId).Distinct().ToArray();
        // Query failures propagate; only an absent cache row is a normal fallback case.
        var cache = await db.Set<MovieCommerceRow>().AsNoTracking().Where(c => movieIds.Contains(c.MovieId)
            && (c.Provider == "fandango" || c.Provider == "fandango-at-home")).ToListAsync();
        var byRelease = mappings.ToDictionary(m => m.ReleaseId, m => m.MovieId);
        var byMovie = cache.ToDictionary(c => (c.MovieId, c.Provider));
        return items.Select(item =>
        {
            MovieCommerceRow? cached = null;
            if (byRelease.TryGetValue(item.Id, out var movieId) && Provider(item.Type) is { } provider)
                byMovie.TryGetValue((movieId, provider), out cached);
            var action = Resolve(item.Title, item.Type, cached?.ActionUrl, cached?.ActionLabel, cached?.LinkKind);
            return item with { MovieActionUrl = action?.Url, MovieActionLabel = action?.Label, MovieActionKind = action?.Kind };
        }).ToList();
    }
}
