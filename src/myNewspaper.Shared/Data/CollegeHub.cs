namespace myNewspaper.Web.Data;

public sealed record CollegeSport(string Key, string Name, string StandingsSport, string League);

public static class CollegeHub
{
    public static IReadOnlyList<CollegeSport> Sports { get; } = Array.AsReadOnly(new[]
    {
        new CollegeSport("football", "Football", "Football", "college-football"),
        new CollegeSport("mens-basketball", "Men's Basketball", "Men's Basketball", "mens-college-basketball"),
        new CollegeSport("volleyball", "Women's Volleyball", "Volleyball", "womens-college-volleyball"),
        new CollegeSport("soccer", "Women's Soccer", "Soccer", "usa.ncaa.w.1")
    });

    public static CollegeSport Resolve(string? value) => Sports.FirstOrDefault(s =>
        string.Equals(s.Key, value, StringComparison.OrdinalIgnoreCase)
        || string.Equals(s.StandingsSport, value, StringComparison.OrdinalIgnoreCase)) ?? Sports[0];

    public static string Href(string view, CollegeSport sport) =>
        (view == "schedule" ? "college" : "college/" + view) + "?sport=" + sport.Key;
}
