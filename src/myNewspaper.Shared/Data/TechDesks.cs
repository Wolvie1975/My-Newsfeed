using System.Text.RegularExpressions;

namespace myNewspaper.Web.Data;

public sealed record DeskTopic(string Slug, string Label, string[] Words);
public sealed record DeskDefinition(string Slug, string Category, string Title, string Description, string ColorClass, IReadOnlyList<DeskTopic> Topics);

public static class TechDesks
{
    public static readonly DeskDefinition Technology = new("technology", NewsSections.Technology, "Technology",
        "Phones, PCs, AI and apps from Engadget, How-To Geek and XDA.", "mx-c-tech",
        [
            new("phones", "Phones", ["phone", "iphone", "pixel", "galaxy", "android", "ios", "smartphone", "foldable"]),
            new("pcs", "PCs & laptops", ["pc", "laptop", "windows", "macbook", "chromebook", "cpu", "gpu", "nvidia", "amd", "intel", "ryzen"]),
            new("ai", "AI", ["ai", "chatgpt", "openai", "claude", "gemini", "copilot", "llm"]),
            new("apps", "Apps & software", ["app", "apps", "update", "browser", "chrome", "firefox", "office", "software"]),
            new("smart-home", "Smart home", ["smart home", "alexa", "google home", "homekit", "matter", "thermostat", "doorbell"]),
            new("deals", "Deals", ["deal", "deals", "sale", "% off", "discount", "price drop"]),
        ]);

    public static readonly DeskDefinition VideoGames = new("video-games", NewsSections.VideoGames, "Video Games",
        "Platforms, news and deals from IGN, Polygon and GamingOnLinux.", "mx-c-games",
        [
            new("pc", "PC", ["pc", "steam", "epic games store", "gog"]),
            new("xbox", "Xbox", ["xbox", "game pass"]),
            new("playstation", "PlayStation", ["playstation", "ps5", "ps4", "psn"]),
            new("switch", "Switch", ["switch", "nintendo"]),
            new("steam-deck-linux", "Steam Deck & Linux", ["steam deck", "proton", "linux", "steamos"]),
        ]);

    public static readonly DeskDefinition Linux = new("linux", NewsSections.Linux, "Linux",
        "Kernel, distros, desktops and Linux gaming from Phoronix, 9to5Linux, Linuxiac, Linux Today and GamingOnLinux.", "mx-c-linux",
        [
            new("kernel", "Kernel", ["kernel", "linux 6.", "linux 7.", "torvalds", "rc1", "driver"]),
            new("distros", "Distros", ["fedora", "ubuntu", "debian", "arch", "mint", "opensuse", "pop!_os", "distro", "nixos"]),
            new("desktops", "Desktops", ["gnome", "kde", "plasma", "xfce", "cosmic", "wayland", "x11"]),
            new("gaming", "Gaming", ["proton", "steam", "steam deck", "gaming", "vulkan", "wine"]),
            new("hardware", "Hardware", ["benchmark", "cpu", "gpu", "amd", "intel", "nvidia", "arm", "risc-v"]),
        ]);

    public static readonly IReadOnlyList<DeskDefinition> All = [Technology, VideoGames, Linux];

    public static DeskDefinition? For(string? slug) =>
        slug is null ? null : All.FirstOrDefault(d => string.Equals(d.Slug, slug, StringComparison.OrdinalIgnoreCase));

    private static readonly DeskTopic GamingDealsTopic = new("deals", "Deals", ["sale", "deal", "deals", "% off", "discount"]);

    public static bool IsGamingDeal(string title, string? summary = null) =>
        Matches(GamingDealsTopic, title, summary);

    /// <summary>Whole-word, case-insensitive match against the title (and summary when present).</summary>
    public static bool Matches(DeskTopic topic, string title, string? summary = null)
    {
        foreach (var word in topic.Words)
        {
            if (MatchesWord(title, word) || (!string.IsNullOrWhiteSpace(summary) && MatchesWord(summary, word)))
            {
                return true;
            }
        }

        return false;
    }

    private static bool MatchesWord(string text, string word)
    {
        if (string.IsNullOrWhiteSpace(text) || string.IsNullOrWhiteSpace(word)) return false;

        // Words with punctuation ("% off", "pop!_os", "linux 6.", "risc-v") use plain case-insensitive Contains.
        foreach (var ch in word)
        {
            if (!char.IsLetterOrDigit(ch) && ch != ' ')
            {
                return text.Contains(word, StringComparison.OrdinalIgnoreCase);
            }
        }

        return Regex.IsMatch(text, $@"\b{Regex.Escape(word)}\b", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
    }

    private static readonly Regex BestForRegex = new(@"\bbest\b.+\bfor\b", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Compiled);

    /// <summary>
    /// Classifies a Technology headline for the "How-to &amp; guides" block:
    /// returns "How-to", "Guide", "Explainer", "Tested", or null when it doesn't match.
    /// </summary>
    public static string? HowToTag(string? title)
    {
        if (string.IsNullOrWhiteSpace(title)) return null;
        if (MatchesWord(title, "how to") || MatchesWord(title, "how i"))
            return "How-to";
        if (MatchesWord(title, "i tried") || MatchesWord(title, "i tested"))
            return "Tested";
        if (MatchesWord(title, "what is") || MatchesWord(title, "explained"))
            return "Explainer";
        if (BestForRegex.IsMatch(title))
            return "Guide";
        return null;
    }
}

