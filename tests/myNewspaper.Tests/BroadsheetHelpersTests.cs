using myNewspaper.Web.Data;

namespace myNewspaper.Tests;

public class BroadsheetHelpersTests
{
    private static readonly DateOnly Friday = new(2026, 10, 30);

    private static TvEpisodeItem Episode(int number, string show = "Avatar: Seven Havens", int season = 1, DateTime? starts = null, bool special = false) =>
        new(number, show, null, "Paramount+", "Title " + number, season, number, special, Friday, starts, "https://example.test/" + number, show.Length);

    [Fact]
    public void A_batch_drop_collapses_into_one_entry_with_its_episode_range()
    {
        var batches = TvBatches.Group([Episode(10), Episode(11), Episode(12), Episode(13), Episode(7, "MobLand", 2)]);
        Assert.Equal(2, batches.Count);
        Assert.Equal(4, batches[0].Count);
        Assert.Equal("S01 · E10–E13 · 4 episodes", TvBatches.Label(batches[0]));
        Assert.Single(batches[1]);
    }

    [Fact]
    public void Episodes_at_different_times_or_seasons_and_specials_stay_separate()
    {
        var nine = new DateTime(2026, 10, 31, 2, 0, 0, DateTimeKind.Utc);
        Assert.Equal(2, TvBatches.Group([Episode(1, starts: nine), Episode(2, starts: nine.AddHours(1))]).Count);
        Assert.Equal(2, TvBatches.Group([Episode(1, season: 1), Episode(2, season: 2)]).Count);
        Assert.Equal(2, TvBatches.Group([Episode(1), Episode(2, special: true)]).Count);
    }

    [Fact]
    public void A_non_consecutive_batch_reports_only_its_size()
    {
        var batch = TvBatches.Group([Episode(1), Episode(3)]).Single();
        Assert.Equal("S01 · 2 episodes", TvBatches.Label(batch));
    }

    [Theory]
    [InlineData("Spider-Man: Brand New Day", "spider man brand new day")]
    [InlineData("Your Mother, Your Mother, Your Mother", "YOUR MOTHER YOUR MOTHER YOUR MOTHER")]
    public void Prediction_titles_match_reported_titles_ignoring_case_and_punctuation(string reported, string predicted) =>
        Assert.Equal(BoxOfficeForecastQuery.TitleKey(reported), BoxOfficeForecastQuery.TitleKey(predicted));

    [Theory]
    [InlineData(19_500_000, "$19.5M")]
    [InlineData(24_000_000, "$24M")]
    [InlineData(153_681_000, "$153.7M")]
    [InlineData(718_000, "$718K")]
    [InlineData(950, "$950")]
    public void Compact_money_reads_like_an_agate_table(long value, string expected) =>
        Assert.Equal(expected, BoxOfficeChartQuery.Compact(value));

    private static VideoItem Video(int id, string channel, int hoursAgo, long? views) =>
        new(id, "v" + id, channel, channel, "Video " + id, "https://www.youtube.com/watch?v=v" + id,
            new DateTime(2026, 10, 25, 20, 0, 0).AddHours(-hoursAgo), null, views);

    [Fact]
    public void Featured_videos_are_the_most_watched_recent_ones_one_per_channel()
    {
        var picks = VideoFeatured.Pick([
            Video(1, "a", 0, 100), Video(2, "a", 1, 5000), Video(3, "b", 2, 900),
            Video(4, "c", 3, null), Video(5, "d", 72, 99999)]);
        Assert.Equal([2, 3, 4], picks.Select(v => v.Id));
    }

    [Fact]
    public void Featured_videos_stop_at_the_count() =>
        Assert.Equal(2, VideoFeatured.Pick([Video(1, "a", 0, 1), Video(2, "b", 0, 2), Video(3, "c", 0, 3)], 2).Count);
}
