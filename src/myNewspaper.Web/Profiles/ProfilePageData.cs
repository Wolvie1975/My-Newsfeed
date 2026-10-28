using System.Text.Json;
using System.Text.Json.Nodes;

namespace myNewspaper.Web.Profiles;

/// <summary>
/// What the page tells its scripts (profiles handoff 2.5): the site defaults, and the open profile's settings when there
/// is one. Rendered into <c>&lt;script id="mnp-profile" type="application/json"&gt;</c> with System.Text.Json's default
/// encoder, which escapes &lt; and &gt;, so a stored value can't close the tag.
/// </summary>
public static class ProfilePageData
{
    private static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web);

    public static string Json(ProfileContext reader)
    {
        var profile = reader.Enabled ? reader.Profile : null;
        return JsonSerializer.Serialize(new
        {
            defaults = new
            {
                theme = SiteDefaults.Theme,
                colorScheme = SiteDefaults.ColorScheme,
                colorSchemes = SiteDefaults.ColorSchemes,
                homeOrder = SiteDefaults.HomeOrder,
                hideable = SiteDefaults.HideableHomeSections,
                settingsOrder = SiteDefaults.SettingsOrder,
                maxServices = SiteDefaults.MaxServices,
            },
            profile = profile is null ? null : new
            {
                name = profile.Name,
                settings = ProfileSettings.Parse(profile.Settings),
                browserImportAt = profile.BrowserImportAt,
            },
        }, Options);
    }

    /// <summary>The open profile's light/dark mode for <c>data-theme-mode</c> on &lt;html&gt;; null without one.</summary>
    public static string? ThemeMode(ProfileContext reader) => Setting(reader, ProfileSettings.Theme, SiteDefaults.Themes, SiteDefaults.Theme);

    /// <summary>The open profile's color scheme for <c>data-color-scheme</c> on &lt;html&gt;; null without one.</summary>
    public static string? ColorScheme(ProfileContext reader) =>
        Setting(reader, ProfileSettings.ColorScheme, SiteDefaults.ColorSchemes, SiteDefaults.ColorScheme);

    private static string? Setting(ProfileContext reader, string key, IReadOnlyList<string> allowed, string fallback)
    {
        if (!reader.Enabled || reader.Profile is not { } profile) return null;
        return ProfileSettings.Parse(profile.Settings)[key] is JsonValue v && v.TryGetValue<string>(out var value) && allowed.Contains(value)
            ? value : fallback;
    }
}
