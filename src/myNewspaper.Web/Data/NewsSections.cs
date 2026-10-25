namespace myNewspaper.Web.Data;

public static class NewsSections
{
    public const string Entertainment = "Entertainment";
    public const string TvFilm = "TV/Film";
    public const string Sports = "Sports";
    public const string MensBasketball = "Men's College Basketball News";
    public static string? RouteFor(string? name) => name switch
    {
        Entertainment => "entertainment/news",
        TvFilm => "entertainment/news/tv-film",
        Sports => "sports/news",
        MensBasketball => "sports/news/basketball",
        Big12NewsQuery.CategoryName => "sports/news/big12",
        F1NewsQuery.CategoryName => "sports/news/f1",
        _ => null
    };
    public static readonly string[] EntertainmentCategories = [Entertainment, TvFilm];
    public static readonly string[] SportsCategories = [Sports, MensBasketball, Big12NewsQuery.CategoryName, F1NewsQuery.CategoryName];
    public static string LabelFor(string? name) => name switch
    {
        Entertainment or Sports => "General",
        TvFilm => "TV/Film",
        MensBasketball => "Men’s college basketball",
        Big12NewsQuery.CategoryName => "Big 12",
        F1NewsQuery.CategoryName => "F1",
        _ => name ?? "Headlines"
    };
    public static bool ValidHub(string? hub) => hub is "headlines" or "sports" or "entertainment";
    public static string DefaultHub(string? name) => EntertainmentCategories.Contains(name) ? "entertainment" : SportsCategories.Contains(name) ? "sports" : "headlines";
    public static string? RouteFor(FeedCategory category) => category.Hub == "headlines" ? null
        : DefaultHub(category.Name) == category.Hub && RouteFor(category.Name) is { } known ? known
        : $"{category.Hub}/news/category-{category.Id}";
    public static string? HubForView(string? view) => view switch
    {
        "sports-hub-news" or "sports-news" or "mens-college-basketball-news" or "sports-big12-news" or "sports-f1-news" => "sports",
        "entertainment-hub-news" or "entertainment-news" or "tv-film-news" => "entertainment",
        _ => null
    };
    public static int Order(FeedCategory category)
    {
        var names = category.Hub == "sports" ? SportsCategories : EntertainmentCategories;
        var index = Array.IndexOf(names, category.Name);
        return index < 0 ? names.Length : index;
    }
}
