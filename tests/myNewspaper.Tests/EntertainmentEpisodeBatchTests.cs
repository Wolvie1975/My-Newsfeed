using myNewspaper.Web.Components;
using myNewspaper.Web.Data;

namespace myNewspaper.Tests;

public class EntertainmentEpisodeBatchTests
{
    private static TvEpisodeItem Episode(int number = 10, int show = 42, string title = "Avatar", int? season = 1) =>
        new(number, title, null, "Paramount+", "Episode", season, number, false, new DateOnly(2026, 10, 30), null, "https://example.com", show);

    [Fact]
    public void A_four_episode_release_is_one_show_with_an_ordered_range()
    {
        var group = Assert.Single(EntertainmentEpisodeBatch.Group([Episode(13), Episode(10), Episode(12), Episode(11)]));
        Assert.Equal(4, group.Count);
        Assert.Equal("S01 E10–E13", group.Range);
    }

    [Fact]
    public void Show_ids_distinguish_remakes_and_unknown_ids_fall_back_to_title()
    {
        var groups = EntertainmentEpisodeBatch.Group([Episode(show: 1), Episode(show: 2),
            Episode(show: 0, title: "Other"), Episode(11, show: 0, title: "Other"), Episode(show: 0, title: "Third")]);
        Assert.Equal(4, groups.Count);
        Assert.Equal(2, groups[2].Count);
    }

    [Fact]
    public void Nonconsecutive_or_cross_season_episodes_do_not_claim_a_contiguous_range()
    {
        Assert.Equal("S01 E10, S01 E12", Assert.Single(EntertainmentEpisodeBatch.Group([Episode(), Episode(12)])).Range);
        Assert.Equal("S01 E10, S02 E01", Assert.Single(EntertainmentEpisodeBatch.Group([Episode(), Episode(1, season: 2)])).Range);
    }

    [Fact]
    public void Missing_numbers_do_not_produce_an_invented_range()
    {
        var episode = Episode() with { Number = null };
        Assert.Equal("S01", Assert.Single(EntertainmentEpisodeBatch.Group([episode])).Range);
        Assert.Null(Assert.Single(EntertainmentEpisodeBatch.Group([episode with { Season = null }])).Range);
        Assert.Empty(EntertainmentEpisodeBatch.Group([]));
    }
}
