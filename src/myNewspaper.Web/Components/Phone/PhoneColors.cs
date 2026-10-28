namespace myNewspaper.Web.Components.Phone;

/// <summary>The .mx-c-* color for a news category, by name; the name is always shown beside it.</summary>
public static class PhoneColors
{
    public static string ForCategory(string? name) => (name ?? "").ToLowerInvariant() switch
    {
        var n when n.Contains("kansas") || n.Contains("local") => "mx-c-local",
        var n when n.Contains("u.s.") || n == "us" || n.Contains("politic") || n.Contains("nation") => "mx-c-us",
        var n when n.Contains("business") || n.Contains("money") || n.Contains("econom") => "mx-c-business",
        var n when n.Contains("tech") || n.Contains("science") || n.Contains("linux") || n.Contains("game") => "mx-c-tech",
        var n when n.Contains("opinion") => "mx-c-ink",
        _ => "mx-c-world",
    };

    /// <summary>The .mx-c-* color for a sport (a scoreboard sport or a Today &amp; Tonight kind); the league is shown beside it.</summary>
    public static string ForSport(string? sport) => (sport ?? "").ToLowerInvariant() switch
    {
        var s when s.Contains("soccer") || s.Contains("nwsl") || s.Contains("mls") || s.Contains("league") => "mx-c-local",
        var s when s.Contains("volleyball") => "mx-c-tech",
        var s when s.Contains("football") || s.Contains("nfl") => "mx-c-business",
        var s when s.Contains("basketball") => "mx-c-us",
        var s when s.Contains("baseball") || s.Contains("softball") => "mx-c-gain",
        _ => "mx-c-world",
    };

    /// <summary>Folds list local news first and opinion last, as in the approved design.</summary>
    public static int Order(string? name) => ForCategory(name) switch
    {
        "mx-c-local" => 0,
        "mx-c-world" => 1,
        "mx-c-us" => 2,
        "mx-c-business" => 3,
        "mx-c-tech" => 4,
        "mx-c-ink" => 6,
        _ => 5,
    };
}
