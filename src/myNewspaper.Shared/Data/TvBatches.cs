namespace myNewspaper.Web.Data;

/// <summary>
/// Groups a day's episodes so a batch drop — several episodes of one season released at the same moment, as streaming
/// services often do — shows as one entry ("S01 · E10–E13 · 4 episodes") instead of a run of near-identical rows.
/// </summary>
public static class TvBatches
{
    public static IReadOnlyList<IReadOnlyList<TvEpisodeItem>> Group(IEnumerable<TvEpisodeItem> episodes)
    {
        var batches = new List<List<TvEpisodeItem>>();
        foreach (var episode in episodes)
        {
            var last = batches.Count > 0 ? batches[^1] : null;
            if (last is not null && SameDrop(last[0], episode)) last.Add(episode);
            else batches.Add([episode]);
        }
        return batches;
    }

    /// <summary>"S01 · E10–E13 · 4 episodes" for a consecutive run, otherwise "S01 · 4 episodes".</summary>
    public static string Label(IReadOnlyList<TvEpisodeItem> batch)
    {
        var first = batch[0];
        var season = first.Season is { } s ? $"S{s:00} · " : "";
        var numbers = batch.Select(e => e.Number).ToList();
        var consecutive = numbers.All(n => n is not null)
            && numbers.Zip(numbers.Skip(1), (a, b) => b - a).All(step => step == 1);
        var range = consecutive ? $"E{numbers[0]:00}–E{numbers[^1]:00} · " : "";
        return $"{season}{range}{batch.Count} episodes";
    }

    private static bool SameDrop(TvEpisodeItem a, TvEpisodeItem b) =>
        !a.Special && !b.Special
        && (a.ShowId != 0 ? a.ShowId == b.ShowId : a.SeriesTitle == b.SeriesTitle)
        && a.Season == b.Season && a.Date == b.Date && a.StartsUtc == b.StartsUtc;
}
