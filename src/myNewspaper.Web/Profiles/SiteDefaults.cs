namespace myNewspaper.Web.Profiles;

/// <summary>
/// The site's built-in settings, in one place (profiles handoff, "Profile settings JSON"). A profile key that's missing
/// means the value here. The scripts still carry their own copies until stage U2 renders these into the page; keep
/// them in step until then: theme-init.js, personal.js and settings-order.js.
/// </summary>
public static class SiteDefaults
{
    public const string Theme = "system";
    public static readonly IReadOnlyList<string> Themes = ["system", "sun", "light", "dark"];

    public const string ColorScheme = "ribbon";
    public static readonly IReadOnlyList<string> ColorSchemes =
    [
        "ribbon", "monochrome", "kansas", "cardinals", "city", "christmas", "halloween", "st-patricks", "eighties",
        "nineties", "y2k", "desert", "coffee", "coastal", "olive", "golden-hour",
    ];

    /// <summary>Home page sections in their default order (personal.js).</summary>
    public static readonly IReadOnlyList<string> HomeOrder =
    [
        "lead", "watch", "opinion", "news", "sports-news", "entertainment-news", "sports", "scoreboard", "tv", "videos",
        "rankings", "forecasts", "movies", "digital",
    ];

    /// <summary>Sections that may be hidden: every home section, plus the F1 card inside Sports.</summary>
    public static readonly IReadOnlyList<string> HideableHomeSections = [.. HomeOrder, "f1"];

    /// <summary>Settings page sections in their default order (settings-order.js).</summary>
    public static readonly IReadOnlyList<string> SettingsOrder = ["appearance", "colors", "services", "teams"];

    public const int MaxTeams = 1000;
    public const int MaxTeamKeyLength = 200;
    public const int MaxServices = 20;
}
