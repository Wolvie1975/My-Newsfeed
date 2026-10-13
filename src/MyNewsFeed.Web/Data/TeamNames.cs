namespace MyNewsFeed.Web.Data;

public static class TeamNames
{
    private static readonly (string School, string Espn)[] Schools =
    [
        ("Arizona", "Arizona Wildcats"), ("Arizona State", "Arizona State Sun Devils"),
        ("Baylor", "Baylor Bears"), ("BYU", "BYU Cougars"), ("Cincinnati", "Cincinnati Bearcats"),
        ("Colorado", "Colorado Buffaloes"), ("Houston", "Houston Cougars"), ("Iowa State", "Iowa State Cyclones"),
        ("Kansas", "Kansas Jayhawks"), ("Kansas State", "Kansas State Wildcats"),
        ("Oklahoma State", "Oklahoma State Cowboys"), ("TCU", "TCU Horned Frogs"),
        ("Texas Tech", "Texas Tech Red Raiders"), ("UCF", "UCF Knights"),
        ("Utah", "Utah Utes"), ("West Virginia", "West Virginia Mountaineers")
    ];
    public static string Key(string? name)
    {
        var trimmed = name?.Trim() ?? "";
        foreach (var (school, espn) in Schools)
            if (trimmed.Equals(school, StringComparison.OrdinalIgnoreCase) || trimmed.Equals(espn, StringComparison.OrdinalIgnoreCase))
                return school.ToLowerInvariant();
        return trimmed.ToLowerInvariant();
    }
    public static string[] Aliases(string name)
    {
        var key = Key(name);
        var school = Schools.FirstOrDefault(s => Key(s.School) == key);
        return school.School is null ? [name.Trim()] : [school.School, school.Espn];
    }
    public static bool Matches(string? first, string? second) => Key(first) != "" && Key(first) == Key(second);
}
