using System.Text.Json;
using MyDailyFeed.Web.Data;

namespace MyDailyFeed.Tests;
public class RankingConferencesTests
{
    [Fact]
    public void Maps_conference_members_through_division_children_without_using_division_as_conference()
    {
        using var json = JsonDocument.Parse("""
        {"season":{"year":2026},"name":"NCAA","children":[
          {"name":"Southeastern Conference","shortName":"SEC","isConference":true,"children":[
            {"name":"East Division","standings":{"entries":[{"team":{"id":"1"}}]}}]},
          {"name":"NCAA Division I","children":[{"name":"Atlantic Coast Conference","shortName":"ACC","standings":{"entries":[{"team":{"id":"2"}}]}}]}]}
        """);
        var names = RankingConferences.Parse(json.RootElement, 2026);
        Assert.Equal("SEC", names["1"]); Assert.Equal("ACC", names["2"]);
        Assert.Empty(RankingConferences.Parse(json.RootElement, 2025));
    }
    [Theory]
    [InlineData("{}")]
    [InlineData("[]")]
    [InlineData("{\"season\":{\"year\":2026}}")]
    public void Missing_conference_data_stays_unknown(string data)
    {
        using var json = JsonDocument.Parse(data);
        Assert.Empty(RankingConferences.Parse(json.RootElement, 2026));
    }
}
