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

    public sealed record SchemeChoice(string Key, string Name, string Primary, string Secondary);

    /// <summary>The color schemes, as Settings lists them (name and swatch colors).</summary>
    public static readonly IReadOnlyList<SchemeChoice> ColorSchemeChoices =
    [
        new("ribbon", "Daily Ribbon · Default", "#0869D8", "#FFD34E"),
        new("monochrome", "Monochromatic", "#262626", "#737373"),
        new("kansas", "Kansas Jayhawks", "#0051BA", "#E8000D"),
        new("cardinals", "St. Louis Cardinals", "#C41E3A", "#0C2340"),
        new("city", "St. Louis CITY SC", "#001837", "#DD004A"),
        new("christmas", "Christmas", "#14532D", "#B91C1C"),
        new("halloween", "Halloween", "#C2410C", "#581C87"),
        new("st-patricks", "St. Patrick’s Day", "#15803D", "#D4AF37"),
        new("eighties", "’80s Neon", "#A21CAF", "#075985"),
        new("nineties", "’90s Retro", "#0F766E", "#7E22CE"),
        new("y2k", "2000s Y2K", "#1D4ED8", "#BEF264"),
        new("desert", "Desert Sunset", "#A45A52", "#F2B33D"),
        new("coffee", "Coffee", "#5D4037", "#795548"),
        new("coastal", "Coastal", "#0E7490", "#FB7185"),
        new("olive", "Olive Grove", "#5F6B2A", "#FDE68A"),
        new("golden-hour", "Golden Hour", "#854D0E", "#C4B5FD"),
    ];

    public static readonly IReadOnlyList<string> ColorSchemes = [.. ColorSchemeChoices.Select(c => c.Key)];

    /// <summary>The light and dark mode choices, as Settings words them.</summary>
    public static readonly IReadOnlyList<(string Key, string Name)> ThemeChoices =
    [
        ("system", "Follow device"), ("sun", "Automatic · Kansas City sunrise/sunset"), ("light", "Light"), ("dark", "Dark"),
    ];

    /// <summary>Home page sections in their default order (personal.js).</summary>
    public static readonly IReadOnlyList<string> HomeOrder =
    [
        "lead", "watch", "opinion", "news", "sports-news", "entertainment-news", "sports", "scoreboard", "tv", "videos",
        "rankings", "forecasts", "movies", "digital",
    ];

    /// <summary>Sections that may be hidden: every home section, plus the F1 card inside Sports.</summary>
    public static readonly IReadOnlyList<string> HideableHomeSections = [.. HomeOrder, "f1"];

    /// <summary>Each hideable section's name, as its heading reads on the home page.</summary>
    public static readonly IReadOnlyDictionary<string, string> HomeSectionNames = new Dictionary<string, string>
    {
        ["lead"] = "Lead story",
        ["watch"] = "Where to watch",
        ["opinion"] = "Opinion",
        ["news"] = "Headlines",
        ["sports-news"] = "Sports headlines",
        ["entertainment-news"] = "Entertainment headlines",
        ["sports"] = "Today & Tonight",
        ["scoreboard"] = "Scoreboard",
        ["tv"] = "Today’s episodes",
        ["videos"] = "Latest videos",
        ["rankings"] = "Top 25",
        ["forecasts"] = "Box office",
        ["movies"] = "Friday theater releases",
        ["digital"] = "Upcoming Digital / VOD",
        ["f1"] = "Formula 1",
    };

    /// <summary>The six profile colors offered in setup and admin; each takes white text at 4.5:1 or better.</summary>
    public static readonly IReadOnlyList<(string Hex, string Name)> ProfileColors =
    [
        ("#0051BA", "Blue"), ("#B4235A", "Raspberry"), ("#0F766E", "Teal"), ("#B45309", "Amber"), ("#6D28D9", "Violet"),
        ("#B0001E", "Red"),
    ];

    /// <summary>Settings page sections in their default order (settings-order.js).</summary>
    public static readonly IReadOnlyList<string> SettingsOrder = ["appearance", "colors", "services", "teams"];

    public const int MaxTeams = 1000;
    public const int MaxTeamKeyLength = 200;
    public const int MaxServices = 20;
}
