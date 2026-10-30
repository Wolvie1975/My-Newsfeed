using myNewspaper.Web.Data;

namespace myNewspaper.Tests;

public class SportsFrontRotationTests
{
    [Fact]
    public void Cycle_is_stable_within_the_hour_independent_of_query_order_and_visits_every_source()
    {
        var hour = new DateTime(2026, 10, 30, 12, 0, 0, DateTimeKind.Utc);
        var sources = new[] { 3, 11, 42, 57 };
        var order = SportsFrontRotation.Order(sources, x => x, hour, "standings");
        Assert.Equal(order, SportsFrontRotation.Order(sources.Reverse(), x => x, hour.AddMinutes(59).AddSeconds(59), "standings"));
        var selected = Enumerable.Range(0, sources.Length).Select(i => SportsFrontRotation.Order(sources, x => x, hour.AddHours(i), "standings")[0]);
        Assert.Equal(sources.Order(), selected.Order());
        Assert.NotEqual(order[0], SportsFrontRotation.Order(sources, x => x, hour.AddHours(1), "standings")[0]);
    }

    [Fact]
    public void Empty_and_single_source_are_supported()
    {
        Assert.Empty(SportsFrontRotation.Order(Array.Empty<int>(), x => x, DateTime.UtcNow, "rankings"));
        Assert.Equal([7], SportsFrontRotation.Order(new[] { 7 }, x => x, DateTime.UtcNow, "rankings"));
    }
}
