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
}
