using myNewspaper.Web.Data;

namespace myNewspaper.Web.Components;

/// <summary>One show's daily episodes for the Entertainment summaries; individual calendar rows stay intact.</summary>
public sealed record EntertainmentEpisodeBatch(IReadOnlyList<TvEpisodeItem> Episodes)
{
    public TvEpisodeItem First => Episodes[0];
    public int Count => Episodes.Count;

    public string? Range
    {
        get
        {
            var numbered = Episodes.Where(e => e.Season is not null && e.Number is not null)
                .OrderBy(e => e.Season).ThenBy(e => e.Number).ToArray();
            if (numbered.Length == Count && numbered.Select(e => e.Season).Distinct().Count() == 1 &&
                numbered.Select(e => e.Number).Distinct().Count() == Count &&
                numbered[^1].Number - numbered[0].Number == Count - 1)
                return Count == 1 ? Code(numbered[0]) : $"{Code(numbered[0])}–E{numbered[^1].Number:00}";
            var codes = Episodes.Select(Code).Where(c => c is not null).Distinct().ToArray();
            return codes.Length == 0 ? null : string.Join(", ", codes);
        }
    }

    public static string? Code(TvEpisodeItem episode) => episode.Season is { } season
        ? $"S{season:00}" + (episode.Number is { } number ? $" E{number:00}" : "") : null;

    public static IReadOnlyList<EntertainmentEpisodeBatch> Group(IEnumerable<TvEpisodeItem> episodes) =>
        episodes.GroupBy(e => e.ShowId != 0 ? $"id:{e.ShowId}" : $"title:{e.SeriesTitle}", StringComparer.OrdinalIgnoreCase)
            .Select(show => new EntertainmentEpisodeBatch(show.ToArray())).ToArray();
}
