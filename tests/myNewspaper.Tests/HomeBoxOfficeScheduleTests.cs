using myNewspaper.Web.Data;

namespace myNewspaper.Tests;

public class HomeBoxOfficeScheduleTests
{
    [Theory]
    [InlineData(18, true)]
    [InlineData(19, true)]
    [InlineData(20, true)]
    [InlineData(21, false)]
    [InlineData(22, false)]
    [InlineData(23, false)]
    [InlineData(24, false)]
    public void Uses_results_only_Sunday_through_Tuesday(int day, bool results) =>
        Assert.Equal(results, HomeBoxOfficeSchedule.ShowResults(new DateOnly(2026, 10, day)));

    [Theory]
    [InlineData(2026, 10, 18, 2026, 10, 16)]
    [InlineData(2026, 10, 20, 2026, 10, 16)]
    [InlineData(2027, 1, 3, 2027, 1, 1)]
    public void Results_belong_to_the_current_weekend(int year, int month, int day, int fy, int fm, int fd) =>
        Assert.Equal(new DateOnly(fy, fm, fd), HomeBoxOfficeSchedule.ResultsWeekend(new DateOnly(year, month, day)));

    [Fact]
    public void Switching_uses_Central_midnight_instead_of_UTC()
    {
        var dates = FeedDates.Create("America/Chicago");
        Assert.True(HomeBoxOfficeSchedule.ShowResults(dates.TodayAt(new DateTime(2026, 10, 21, 4, 59, 0, DateTimeKind.Utc))));
        Assert.False(HomeBoxOfficeSchedule.ShowResults(dates.TodayAt(new DateTime(2026, 10, 21, 5, 0, 0, DateTimeKind.Utc))));
    }
}
