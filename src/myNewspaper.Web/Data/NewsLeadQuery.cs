namespace myNewspaper.Web.Data;

/// <summary>
/// The News front's lead: the newest hard-news story, so a deal post or opinion piece never leads the page because
/// its category happened to publish last.
/// </summary>
public static class NewsLeadQuery
{
    // "Kansas City News" is SectionChoices.NewsKansasCityCategory (kept literal: Data doesn't reference Components).
    public static readonly string[] Categories = ["Top Headlines", "U.S. News", "Kansas City News"];

    public static FeedItem? Select(IEnumerable<FeedItem> candidates) => candidates
        .Where(item => item.CategoryName is { } category && Categories.Contains(category) && FeedQuery.SafeHref(item.Url) is not null)
        .OrderBy(item => item.Undated)
        .ThenByDescending(item => item.Date)
        .ThenByDescending(item => item.Id)
        .FirstOrDefault();
}
