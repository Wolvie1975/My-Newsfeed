namespace MyNewsFeed.Web.Data;

public static class CollegeSeasons
{
    // Basketball spans academic years. This identifies the season label, not a guessed opening-game date.
    public static DateOnly Start(string? sport, DateOnly today) => sport?.Contains("Basketball", StringComparison.OrdinalIgnoreCase) == true
        ? new DateOnly(today.Month >= 7 ? today.Year : today.Year - 1, 7, 1)
        : new DateOnly(today.Year, 1, 1);

    public static bool Matches(string? season, string? sport, DateOnly today)
    {
        if (string.IsNullOrWhiteSpace(season)) return false;
        var text = season.Trim().Replace('–', '-').Replace('—', '-');
        var parts = text.Split('-');
        if (!int.TryParse(parts[0], out var year) || year != Start(sport, today).Year) return false;
        if (sport?.Contains("Basketball", StringComparison.OrdinalIgnoreCase) != true) return parts.Length == 1;
        return parts.Length == 2 && int.TryParse(parts[1], out var end) && (end == year + 1 || end == (year + 1) % 100);
    }
}
