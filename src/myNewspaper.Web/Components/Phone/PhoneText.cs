using System.Text.RegularExpressions;
using myNewspaper.Shared.Data;

namespace myNewspaper.Web.Components.Phone;

/// <summary>Plain-text lines for the phone fronts: matchups, scores and clocks.</summary>
public static class PhoneText
{
    /// <summary>"Away at Home", or "A vs B" at a neutral site, including #rank for ranked teams.</summary>
    public static string Matchup(ScoreboardGame game) =>
        $"{FormatRank(game.AwayRank)}{game.AwayName}{(game.NeutralSite == true ? " vs " : " at ")}{FormatRank(game.HomeRank)}{game.HomeName}";

    private static string FormatRank(byte? rank) => rank is >= 1 and <= 25 ? $"#{rank} " : "";

    /// <summary>The same line for a Today &amp; Tonight item (as TonightMatchup, without records).</summary>
    public static string Matchup(TonightItem item)
    {
        var known = !item.IsNeutral && item.IsAway is not null;
        return !known ? $"{item.SchoolName} vs {item.OpponentName}"
            : item.IsAway == true ? $"{item.SchoolName} at {item.OpponentName}" : $"{item.OpponentName} at {item.SchoolName}";
    }

    /// <summary>"13–10" (away first) once a game has a score; null before.</summary>
    public static string? Score(ScoreboardGame game) =>
        game.Active || game.Status == "final" || game.AwayScore is not null
            ? $"{ScoreboardGame.Score(game.AwayScore)}–{ScoreboardGame.Score(game.HomeScore)}" : null;

    /// <summary>The short status a tile shows: the live clock ("Half", "51′"), "Final", or the start time.</summary>
    public static string Clock(ScoreboardGame game, FeedDates dates) => game.Status switch
    {
        "final" => game.Label,
        "upcoming" => game.TimeTbd || game.StartsAtUtc is null ? "TBD" : dates.Time(game.StartsAtUtc.Value),
        _ when game.Active => game.StatusText ?? game.DisplayClock ?? "Live",
        _ => game.Label,
    };

    /// <summary>A live tile's clock, period first: "4Q 8:00", "3P 11:35", "2H 63′", "Half", "OT 2:10". Not live: as Clock.</summary>
    public static string CompactClock(ScoreboardGame game, FeedDates dates)
    {
        var status = Clock(game, dates);
        if (!game.Active) return status;

        // Providers send "8:00 - 4th Quarter", "11:35 - 3rd Period", "63' - 2nd Half", or just "Halftime" / "Top 5th".
        var split = Regex.Match(status, @"^\s*(?<time>[^-–—]+?)\s*[-–—]\s*(?<period>.+?)\s*$");
        var time = split.Success ? split.Groups["time"].Value.Trim() : null;
        var period = ShortPeriod(split.Success ? split.Groups["period"].Value : status);
        return string.IsNullOrEmpty(time) ? period : $"{period} {time.Replace('\'', '′')}";
    }

    /// <summary>"4th Quarter" → "4Q", "3rd Period" → "3P", "2nd Half" → "2H", "Halftime" → "Half", "Overtime" → "OT",
    /// "Top 5th" → "Top 5", "End of 3rd Quarter" → "End 3Q". Anything else is returned as it came.</summary>
    private static string ShortPeriod(string period)
    {
        var p = period.Trim();
        p = Regex.Replace(p, @"\bEnd of\b", "End", RegexOptions.IgnoreCase);
        p = Regex.Replace(p, @"\b([1-9])(?:st|nd|rd|th)\s+(Quarter|Period|Half)\b",
            m => m.Groups[1].Value + char.ToUpperInvariant(m.Groups[2].Value[0]), RegexOptions.IgnoreCase);
        p = Regex.Replace(p, @"\b(Top|Bot(?:tom)?|Mid(?:dle)?)\s+([1-9][0-9]?)(?:st|nd|rd|th)\b",
            m => $"{m.Groups[1].Value[..3]} {m.Groups[2].Value}", RegexOptions.IgnoreCase);
        p = Regex.Replace(p, @"^Half\s*time$", "Half", RegexOptions.IgnoreCase);
        p = Regex.Replace(p, @"\bOvertime\b", "OT", RegexOptions.IgnoreCase);
        return p;
    }

    /// <summary>A league name short enough for a 150px tile. The full CompetitionLabel stays in the aria-label.</summary>
    public static string TileLeague(string sport, string league) => league switch
    {
        "college-football" => "CFB",
        "mens-college-basketball" => "NCAAM",
        "womens-college-basketball" => "NCAAW",
        "eng.1" => "EPL",
        "eng.2" => "EFL",
        "usa.ncaa.w.1" => "NCAAW Soccer",
        "womens-college-volleyball" => "NCAAW Volleyball",
        _ => ScoreboardPanel.CompetitionLabel(sport, league),
    };

    /// <summary>The first network of a broadcast list ("ESPN, ESPN+" reads as "ESPN").</summary>
    public static string? Network(string? broadcasts) =>
        string.IsNullOrWhiteSpace(broadcasts) ? null : broadcasts.Split([',', '/', ';'], 2)[0].Trim();
}
