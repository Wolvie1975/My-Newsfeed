using myNewspaper.Web.Data;
using System.Text.RegularExpressions;

namespace myNewspaper.Web.Components.Phone;

/// <summary>Plain-text lines for the phone fronts: matchups, scores and clocks.</summary>
public static class PhoneText
{
    /// <summary>"Away at Home", or "A vs B" at a neutral site.</summary>
    public static string Matchup(ScoreboardGame game) =>
        $"{game.AwayName}{(game.NeutralSite == true ? " vs " : " at ")}{game.HomeName}";

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

    /// <summary>Compact quarter names for score tiles; leave other provider statuses intact.</summary>
    public static string CompactClock(ScoreboardGame game, FeedDates dates)
    {
        var status = Clock(game, dates);
        if (!game.Active) return status;
        var compact = Regex.Replace(status, @"\b([1-4])(?:st|nd|rd|th)\s+Quarter\b", "$1Q", RegexOptions.IgnoreCase);
        return compact == status ? status : Regex.Replace(compact, @"\s+[-–—]\s+", " · ");
    }

    /// <summary>The first network of a broadcast list ("ESPN, ESPN+" reads as "ESPN").</summary>
    public static string? Network(string? broadcasts) =>
        string.IsNullOrWhiteSpace(broadcasts) ? null : broadcasts.Split([',', '/', ';'], 2)[0].Trim();
}
