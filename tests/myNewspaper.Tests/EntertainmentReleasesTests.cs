using myNewspaper.Shared.Data;
using Xunit;

namespace myNewspaper.Tests;

public class EntertainmentReleasesTests
{
    private sealed record TestRelease(string Title, DateOnly? Day, string? Poster, bool Tonight = false);

    [Fact]
    public void Deduplicate_merges_year_and_no_year_pair_on_same_day()
    {
        var nov10 = new DateOnly(2026, 11, 10);
        var item1 = new TestRelease("Crawlers", nov10, null);
        var item2 = new TestRelease("Crawlers (2026)", nov10, null);

        var deduplicated = EntertainmentReleases.Deduplicate(
            new[] { item1, item2 },
            x => x.Title,
            x => x.Day,
            x => x.Poster,
            x => x.Tonight);

        Assert.Single(deduplicated);
        Assert.Equal("Crawlers (2026)", deduplicated[0].Title);
    }

    [Fact]
    public void Deduplicate_prefers_poster_over_year()
    {
        var nov10 = new DateOnly(2026, 11, 10);
        var item1 = new TestRelease("Crawlers", nov10, "poster.jpg");
        var item2 = new TestRelease("Crawlers (2026)", nov10, null);

        var deduplicated = EntertainmentReleases.Deduplicate(
            new[] { item1, item2 },
            x => x.Title,
            x => x.Day,
            x => x.Poster,
            x => x.Tonight);

        Assert.Single(deduplicated);
        Assert.Equal("Crawlers", deduplicated[0].Title);
        Assert.Equal("poster.jpg", deduplicated[0].Poster);
    }

    [Fact]
    public void Deduplicate_keeps_two_different_films_on_same_day()
    {
        var nov10 = new DateOnly(2026, 11, 10);
        var item1 = new TestRelease("Film A", nov10, "a.jpg");
        var item2 = new TestRelease("Film B", nov10, "b.jpg");

        var deduplicated = EntertainmentReleases.Deduplicate(
            new[] { item1, item2 },
            x => x.Title,
            x => x.Day,
            x => x.Poster,
            x => x.Tonight);

        Assert.Equal(2, deduplicated.Count);
        Assert.Equal("Film A", deduplicated[0].Title);
        Assert.Equal("Film B", deduplicated[1].Title);
    }

    [Fact]
    public void Deduplicate_keeps_same_title_on_different_days()
    {
        var nov10 = new DateOnly(2026, 11, 10);
        var nov11 = new DateOnly(2026, 11, 11);
        var item1 = new TestRelease("Crawlers", nov10, "a.jpg");
        var item2 = new TestRelease("Crawlers", nov11, "a.jpg");

        var deduplicated = EntertainmentReleases.Deduplicate(
            new[] { item1, item2 },
            x => x.Title,
            x => x.Day,
            x => x.Poster,
            x => x.Tonight);

        Assert.Equal(2, deduplicated.Count);
        Assert.Equal(nov10, deduplicated[0].Day);
        Assert.Equal(nov11, deduplicated[1].Day);
    }
}
