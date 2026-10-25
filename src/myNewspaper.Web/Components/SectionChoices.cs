namespace myNewspaper.Web.Components;

/// <summary>The section lists shown under Sports and Entertainment page headings (tabs on desktop, a dropdown on phones).</summary>
public static class SectionChoices
{
    public static readonly IReadOnlyList<NavigationChoice> Sports =
    [
        new("sports", "Sports home"), new("sports/news", "All headlines"), new("scoreboard", "Scoreboard"), new("events", "Schedule"),
        new("standings", "Standings"), new("rankings", "Top 25"), new("college", "College Sports"), new("f1", "F1"),
    ];

    public static readonly IReadOnlyList<NavigationChoice> Entertainment =
    [
        new("entertainment", "Entertainment home"), new("entertainment/news", "All headlines"), new("movies", "Movies"), new("tv", "TV & streaming"),
        new("movies/digital", "Digital releases"), new("box-office", "Box office"), new("watch", "Where to watch"),
    ];

    public static IReadOnlyList<NavigationChoice>? For(string? section) => section switch
    {
        "sports" => Sports,
        "entertainment" => Entertainment,
        _ => null,
    };
}
