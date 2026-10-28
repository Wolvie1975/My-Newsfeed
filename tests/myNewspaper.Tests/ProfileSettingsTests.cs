using System.Text.Json.Nodes;
using myNewspaper.Web.Profiles;

namespace myNewspaper.Tests;

public class ProfileSettingsTests
{
    private static JsonObject Json(string json) => JsonNode.Parse(json)!.AsObject();

    [Fact]
    public void Scoreboard_stars_are_their_own_setting_and_are_checked_like_teams()
    {
        var merged = ProfileSettings.Merge([], Json("""{"scoreboardTeams":["kansas","kansas","nhl:blues"]}"""));
        Assert.Equal("""["kansas","nhl:blues"]""", merged["scoreboardTeams"]!.ToJsonString());
        Assert.Throws<ArgumentException>(() => ProfileSettings.Merge([], Json("""{"scoreboardTeams":"kansas"}""")));
        Assert.Throws<ArgumentException>(() => ProfileSettings.Merge([], Json("""{"scoreboardTeams":["a\u0000b"]}""")));
        Assert.DoesNotContain("scoreboardTeams", ProfileSettings.FillMissing([], Json("""{"scoreboardTeams":[""]}""")).Select(p => p.Key));
    }

    [Fact]
    public void The_page_data_escapes_stored_text_so_it_cannot_close_its_script_tag()
    {
        var reader = new ProfileContext(Microsoft.Extensions.Options.Options.Create(new ProfilesOptions { Enabled = true }));
        var profile = new myNewspaper.Web.Data.Models.Profile { Id = 1, Name = "</script><b>x", Settings = """{"theme":"dark","colorScheme":"kansas","teams":["</script>"]}""" };
        // The gate middleware sets the open profile; its setter is internal to the app.
        typeof(ProfileContext).GetProperty(nameof(ProfileContext.Profile))!.SetValue(reader, profile);
        var json = ProfilePageData.Json(reader);
        Assert.DoesNotContain("</script>", json, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("<b>", json);
        var data = JsonNode.Parse(json)!;
        Assert.Equal("</script><b>x", data["profile"]!["name"]!.GetValue<string>());
        Assert.Equal("ribbon", data["defaults"]!["colorScheme"]!.GetValue<string>());
        Assert.Equal(("dark", "kansas"), (ProfilePageData.ThemeMode(reader), ProfilePageData.ColorScheme(reader)));

        // No profile open (the picker, or profiles off): defaults only, and no attributes on <html>.
        var nobody = new ProfileContext(Microsoft.Extensions.Options.Options.Create(new ProfilesOptions { Enabled = true }));
        Assert.Null(JsonNode.Parse(ProfilePageData.Json(nobody))!["profile"]);
        Assert.Null(ProfilePageData.ThemeMode(nobody));
        // A damaged stored value falls back to the site default rather than reaching the page.
        profile.Settings = """{"theme":"neon","colorScheme":7}""";
        Assert.Equal(("system", "ribbon"), (ProfilePageData.ThemeMode(reader), ProfilePageData.ColorScheme(reader)));
    }

    [Fact]
    public void Values_keep_the_scripts_shapes_and_drop_duplicates()
    {
        var settings = ProfileSettings.Normalize(Json("""
            {"theme":"sun","colorScheme":"kansas","home":{"order":["tv","lead","tv"],"hidden":["f1","rankings"],"teamsOnly":true},
             "teams":["big12:kansas","big12:kansas"],"services":[8,15,8],"settingsOrder":["teams","appearance"]}
            """));
        Assert.Equal("""
            {"theme":"sun","colorScheme":"kansas","home":{"order":["tv","lead"],"hidden":["f1","rankings"],"teamsOnly":true},"teams":["big12:kansas"],"services":[8,15],"settingsOrder":["teams","appearance"]}
            """.Trim(), settings.ToJsonString());
    }

    [Theory]
    [InlineData("""{"theme":"purple"}""")]
    [InlineData("""{"theme":1}""")]
    [InlineData("""{"colorScheme":"neon"}""")]
    [InlineData("""{"home":{"order":["lead","not-a-section"]}}""")]
    [InlineData("""{"home":{"hidden":["nope"]}}""")]
    [InlineData("""{"home":{"teamsOnly":"yes"}}""")]
    [InlineData("""{"home":{"other":true}}""")]
    [InlineData("""{"home":[]}""")]
    [InlineData("""{"teams":"big12:kansas"}""")]
    [InlineData("""{"teams":[""]}""")]
    [InlineData("""{"teams":["a\u0000b"]}""")]
    [InlineData("""{"teams":[5]}""")]
    [InlineData("""{"services":[0]}""")]
    [InlineData("""{"services":[1.5]}""")]
    [InlineData("""{"services":["8"]}""")]
    [InlineData("""{"services":[1,2,3,4,5,6,7,8,9,10,11,12,13,14,15,16,17,18,19,20,21]}""")]
    [InlineData("""{"settingsOrder":["sidebar"]}""")]
    [InlineData("""{"weatherCity":"Topeka"}""")]
    public void Bad_values_and_unknown_keys_are_rejected(string json) =>
        Assert.Throws<ArgumentException>(() => ProfileSettings.Normalize(Json(json)));

    [Fact]
    public void Patches_replace_top_level_keys_and_null_returns_to_the_site_default()
    {
        var current = Json("""{"theme":"dark","teams":["a"],"services":[8]}""");
        var merged = ProfileSettings.Merge(current, Json("""{"theme":null,"teams":["b"]}"""));
        Assert.Equal("""{"teams":["b"],"services":[8]}""", merged.ToJsonString());
        Assert.Equal("""{"theme":"dark","teams":["a"],"services":[8]}""", current.ToJsonString());
        Assert.Throws<ArgumentException>(() => ProfileSettings.Merge(current, Json("""{"unknown":null}""")));
    }

    [Fact]
    public void The_browser_import_fills_only_missing_keys_and_skips_bad_values()
    {
        var filled = ProfileSettings.FillMissing(Json("""{"theme":"dark"}"""),
            Json("""{"theme":"light","colorScheme":"neon","teams":["a"],"services":[8],"unknown":1}"""));
        Assert.Equal("""{"theme":"dark","teams":["a"],"services":[8]}""", filled.ToJsonString());
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("not json")]
    [InlineData("[1,2]")]
    public void Damaged_stored_documents_read_as_empty(string? json) => Assert.Empty(ProfileSettings.Parse(json));

    [Theory]
    [InlineData("not json")]
    [InlineData("[1,2]")]
    [InlineData("\"text\"")]
    public void Input_must_be_an_object(string json) => Assert.Throws<ArgumentException>(() => ProfileSettings.ParseInput(json));

    [Fact]
    public void Oversized_input_is_rejected() =>
        Assert.Throws<ArgumentException>(() => ProfileSettings.ParseInput("{\"teams\":[\"" + new string('a', ProfileSettings.MaxLength) + "\"]}"));

    [Fact]
    public void Site_defaults_match_the_scripts_built_in_values()
    {
        var root = Path.GetFullPath("../../../../../src/myNewspaper.Web/wwwroot/", AppContext.BaseDirectory);
        var personal = File.ReadAllText(root + "personal.js");
        var order = File.ReadAllText(root + "settings-order.js");
        var theme = File.ReadAllText(root + "theme-init.js");
        // The scripts read SiteDefaults from the page and keep a copy only as a fallback; this keeps the copies in step.
        Assert.Contains(string.Join(",\n        ", SiteDefaults.HomeOrder.Select(s => $"'{s}'")), personal.Replace("\r", ""));
        Assert.Contains($"defaults?.settingsOrder || [{string.Join(", ", SiteDefaults.SettingsOrder.Select(s => $"'{s}'"))}];", order);
        foreach (var scheme in SiteDefaults.ColorSchemes) Assert.Contains($"'{scheme}'", theme);
        Assert.Contains($": '{SiteDefaults.ColorScheme}'", theme);
        Assert.Contains($": '{SiteDefaults.Theme}'", theme);
    }
}
