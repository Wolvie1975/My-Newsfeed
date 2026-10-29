using System.Text.Json;
using System.Text.Json.Nodes;

namespace myNewspaper.Web.Profiles;

/// <summary>
/// The settings document stored in <c>Profiles.Settings</c> and <c>Household.BaseSettings</c>. Values keep the shapes the
/// scripts store in localStorage today. A missing key means <see cref="SiteDefaults"/>. Every write goes through
/// <see cref="Normalize(string, JsonNode?)"/>, so unknown keys and bad values never reach the database.
/// </summary>
public static class ProfileSettings
{
    public const int MaxLength = 64_000;

    public const string Theme = "theme";
    public const string ColorScheme = "colorScheme";
    public const string Home = "home";
    public const string Teams = "teams";
    public const string Services = "services";
    public const string SettingsOrder = "settingsOrder";
    /// <summary>The scoreboard's starred teams (scoreboard.js), separate from My Teams.</summary>
    public const string ScoreboardTeams = "scoreboardTeams";

    public static readonly IReadOnlyList<string> Keys = [Theme, ColorScheme, Home, Teams, Services, SettingsOrder, ScoreboardTeams];

    /// <summary>A stored document. Anything unreadable counts as empty, so an old or damaged row never breaks reading.</summary>
    public static JsonObject Parse(string? json)
    {
        try { return JsonNode.Parse(json ?? "{}") as JsonObject ?? []; }
        catch (JsonException) { return []; }
    }

    /// <summary>A document from outside (an API body or a form field). Throws <see cref="ArgumentException"/>.</summary>
    public static JsonObject ParseInput(string? json)
    {
        if (string.IsNullOrWhiteSpace(json)) return [];
        if (json.Length > MaxLength) throw new ArgumentException("Settings are too large.");
        try { return JsonNode.Parse(json) as JsonObject ?? throw new ArgumentException("Settings must be a JSON object."); }
        catch (JsonException) { throw new ArgumentException("Settings must be a JSON object."); }
    }

    public static string Serialize(JsonObject settings)
    {
        var json = settings.ToJsonString();
        return json.Length <= MaxLength ? json : throw new ArgumentException("Settings are too large.");
    }

    /// <summary>A whole document, every key checked. Used for the base edition and new profiles.</summary>
    public static JsonObject Normalize(JsonObject settings)
    {
        var result = new JsonObject();
        foreach (var (key, value) in settings)
        {
            if (value is null) continue;
            result[key] = Normalize(key, value);
        }
        return result;
    }

    /// <summary>Merge-patch on top-level keys: a value replaces the key, null removes it (back to the site default).</summary>
    public static JsonObject Merge(JsonObject current, JsonObject patch)
    {
        var result = current.DeepClone().AsObject();
        foreach (var (key, value) in patch)
        {
            if (!Keys.Contains(key)) throw new ArgumentException($"Unknown setting \"{key}\".");
            if (value is null) result.Remove(key);
            else result[key] = Normalize(key, value);
        }
        return result;
    }

    /// <summary>The one-time browser import: only keys the profile doesn't have yet are filled, and bad values are skipped.</summary>
    public static JsonObject FillMissing(JsonObject current, JsonObject values)
    {
        var result = current.DeepClone().AsObject();
        foreach (var (key, value) in values)
        {
            if (value is null || result.ContainsKey(key) || !Keys.Contains(key)) continue;
            try { result[key] = Normalize(key, value); }
            catch (ArgumentException) { }
        }
        return result;
    }

    /// <summary>One value, checked and tidied (duplicates removed). Throws <see cref="ArgumentException"/>.</summary>
    /// <summary>
    /// Setup steps 2 to 4 (handoff 6.2) as plain form fields: the teams and services ticked, and the home sections in the
    /// order shown, with the unticked ones hidden. Sections setup doesn't list (the F1 card) and the rest of the base
    /// edition's home (teamsOnly) keep the base edition's values. Validated like every other settings write.
    /// </summary>
    public static JsonObject FromSetup(JsonObject baseEdition, IEnumerable<string>? teams, IEnumerable<int>? services,
        IEnumerable<string>? shown, IEnumerable<string>? order)
    {
        var home = baseEdition[Home] is JsonObject current ? (JsonObject)current.DeepClone() : new JsonObject();
        var listed = (order ?? []).Where(SiteDefaults.HomeOrder.Contains).Distinct().ToList();
        if (listed.Count > 0)
        {
            var on = (shown ?? []).ToHashSet(StringComparer.Ordinal);
            var keptHidden = (home["hidden"] as JsonArray ?? []).Select(n => n?.GetValue<string>()).OfType<string>().Where(s => !listed.Contains(s));
            home["order"] = new JsonArray([.. listed.Select(s => (JsonNode)s)]);
            home["hidden"] = new JsonArray([.. keptHidden.Concat(listed.Where(s => !on.Contains(s))).Distinct().Select(s => (JsonNode)s)]);
        }
        return Normalize(new JsonObject
        {
            [Teams] = new JsonArray([.. (teams ?? []).Select(t => (JsonNode)t)]),
            [Services] = new JsonArray([.. (services ?? []).Select(s => (JsonNode)s)]),
            [Home] = home,
        });
    }

    public static JsonNode Normalize(string key, JsonNode? value) => key switch
    {
        Theme => OneOf(value, SiteDefaults.Themes, key),
        ColorScheme => OneOf(value, SiteDefaults.ColorSchemes, key),
        Home => HomeValue(value),
        Teams => new JsonArray([.. Strings(value, key, SiteDefaults.MaxTeams, team =>
            team.Length is > 0 and <= SiteDefaults.MaxTeamKeyLength && !team.Any(char.IsControl)).Select(t => (JsonNode)t)]),
        ScoreboardTeams => new JsonArray([.. Strings(value, key, SiteDefaults.MaxTeams, team =>
            team.Length is > 0 and < 300 && !team.Any(char.IsControl)).Select(t => (JsonNode)t)]),
        Services => ServicesValue(value),
        SettingsOrder => new JsonArray([.. Strings(value, key, SiteDefaults.SettingsOrder.Count, SiteDefaults.SettingsOrder.Contains)
            .Select(s => (JsonNode)s)]),
        _ => throw new ArgumentException($"Unknown setting \"{key}\"."),
    };

    private static JsonNode OneOf(JsonNode? value, IReadOnlyList<string> allowed, string key) =>
        value is JsonValue v && v.TryGetValue<string>(out var text) && allowed.Contains(text)
            ? JsonValue.Create(text) : throw new ArgumentException($"Invalid {key}.");

    private static List<string> Strings(JsonNode? value, string key, int max, Func<string, bool> valid)
    {
        if (value is not JsonArray array || array.Count > max) throw new ArgumentException($"Invalid {key}.");
        var result = new List<string>();
        foreach (var item in array)
        {
            if (item is not JsonValue v || !v.TryGetValue<string>(out var text) || !valid(text)) throw new ArgumentException($"Invalid {key}.");
            if (!result.Contains(text, StringComparer.Ordinal)) result.Add(text);
        }
        return result;
    }

    private static JsonNode ServicesValue(JsonNode? value)
    {
        if (value is not JsonArray array) throw new ArgumentException("Invalid services.");
        var ids = new List<int>();
        foreach (var item in array)
        {
            if (item is not JsonValue v || v.GetValueKind() != JsonValueKind.Number || !v.TryGetValue<int>(out var id) || id <= 0)
                throw new ArgumentException("Invalid services.");
            if (!ids.Contains(id)) ids.Add(id);
        }
        return ids.Count <= SiteDefaults.MaxServices
            ? new JsonArray([.. ids.Select(id => (JsonNode)id)]) : throw new ArgumentException("Choose up to 20 services.");
    }

    private static JsonNode HomeValue(JsonNode? value)
    {
        if (value is not JsonObject home) throw new ArgumentException("Invalid home.");
        var result = new JsonObject();
        foreach (var (key, item) in home)
        {
            result[key] = key switch
            {
                "order" => new JsonArray([.. Strings(item, "home order", SiteDefaults.HomeOrder.Count, SiteDefaults.HomeOrder.Contains)
                    .Select(s => (JsonNode)s)]),
                "hidden" => new JsonArray([.. Strings(item, "hidden sections", SiteDefaults.HideableHomeSections.Count,
                    SiteDefaults.HideableHomeSections.Contains).Select(s => (JsonNode)s)]),
                "teamsOnly" => item is JsonValue v && v.GetValueKind() is JsonValueKind.True or JsonValueKind.False
                    ? JsonValue.Create(v.GetValue<bool>()) : throw new ArgumentException("Invalid teamsOnly."),
                _ => throw new ArgumentException($"Unknown home setting \"{key}\"."),
            };
        }
        return result;
    }
}
