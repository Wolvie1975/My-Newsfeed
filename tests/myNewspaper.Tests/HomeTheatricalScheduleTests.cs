using myNewspaper.Web.Data;

namespace myNewspaper.Tests;

public class HomeTheatricalScheduleTests
{
    [Theory]
    [InlineData(19, null)]
    [InlineData(20, null)]
    [InlineData(21, null)]
    [InlineData(22, 23)]
    [InlineData(23, 23)]
    [InlineData(24, 23)]
    [InlineData(25, 23)]
    [InlineData(26, null)]
    [InlineData(29, 30)]
    public void Friday_release_window_is_Thursday_through_Sunday(int day, int? fridayDay)
    {
        var expected = fridayDay is { } value ? new DateOnly(2026, 10, value) : (DateOnly?)null;
        Assert.Equal(expected, MoviesQuery.HomeTheatricalFriday(new DateOnly(2026, 10, day)));
    }

    [Fact]
    public void Window_handles_year_boundary()
    {
        Assert.Equal(new DateOnly(2027, 1, 1), MoviesQuery.HomeTheatricalFriday(new DateOnly(2026, 12, 31)));
        Assert.Equal(new DateOnly(2027, 1, 1), MoviesQuery.HomeTheatricalFriday(new DateOnly(2027, 1, 3)));
    }
}
