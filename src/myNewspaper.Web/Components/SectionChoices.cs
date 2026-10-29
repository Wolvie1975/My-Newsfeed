using myNewspaper.Web.Data;

namespace myNewspaper.Web.Components;

/// <summary>The section lists shown under News, Sports and Entertainment page headings (tabs on desktop, a dropdown on phones).</summary>
public static class SectionChoices
{
    public static readonly IReadOnlyList<NavigationChoice> Sports =
    [
        new("sports", "Sports home"), new("sports/news", "Headlines"), new("scoreboard", "Scoreboard"), new("events", "Competitions"),
        new("standings", "Standings"), new("rankings", "Top 25"), new("college", "College Sports"), new("f1", "F1"),
    ];

    public static readonly IReadOnlyList<NavigationChoice> Entertainment =
    [
        new("entertainment", "Entertainment home"), new("entertainment/news", "Headlines"), new("movies", "Movies"), new("tv", "TV & streaming"),
        new("movies/digital", "Digital releases"), new("box-office", "Box office"), new("watch", "Where to watch"),
    ];

    public const string NewsKansasCityCategory = "Kansas City News";

    // Named categories keep the design's order; any other headlines-hub category follows alphabetically.
    private static readonly string[] NewsOrder = ["Top Headlines", NewsKansasCityCategory, "Opinion", "Technology", "Video Games", "Linux"];

    /// <summary>News tabs: home, all, then each headlines-hub category; Kansas City goes to its own page.</summary>
    public static IReadOnlyList<NavigationChoice> News(IEnumerable<FeedCategory> headlinesCategories) =>
    [
        new("news", "News home"),
        new("news/all", "All headlines"),
        .. headlinesCategories
            .OrderBy(c => Array.IndexOf(NewsOrder, c.Name) is var i && i >= 0 ? i : NewsOrder.Length).ThenBy(c => c.Name)
            .Select(c => c.Name == NewsKansasCityCategory
                ? new NavigationChoice("news/kansas-city", "Kansas City")
                : new NavigationChoice($"news/all?category={c.Id}", c.Name)),
    ];

    /// <summary>The News tabs for a page that hasn't loaded categories itself.</summary>
    public static async Task<IReadOnlyList<NavigationChoice>> NewsAsync(WebScraperContext db) =>
        News((await FeedQuery.GetCategoriesAsync(db)).Where(c => c.Hub == "headlines"));

    public static IReadOnlyList<NavigationChoice>? For(string? section) => section switch
    {
        "sports" => Sports,
        "entertainment" => Entertainment,
        _ => null,
    };
}
