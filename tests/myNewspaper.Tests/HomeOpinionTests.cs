using myNewspaper.Web.Data;
namespace myNewspaper.Tests;
public class HomeOpinionTests
{
    private static readonly DateTime Now = new(2026,10,24,12,0,0,DateTimeKind.Utc);
    private static FeedItem Item(int id,string source,int minutes=0,bool undated=false) => new(id,"Opinion story "+id,"https://example.com/"+id,null,null,null,source,100,"Opinion",Now.AddMinutes(minutes),undated);
    [Fact]
    public void Every_publisher_gets_one_slot_before_cycle_repeats() {
        var items=Enumerable.Range(0,5).Select(i=>Item(i,"https://publisher.test/"+i)).ToArray();
        var cycle=Enumerable.Range(0,5).Select(h=>HomeOpinionQuery.Snapshot(100,items,Now.AddHours(h)).Article!.SourceUrl).ToArray();
        Assert.Equal(5,cycle.Distinct().Count());
        Assert.Equal(cycle[0],HomeOpinionQuery.Snapshot(100,items,Now.AddHours(5)).Article!.SourceUrl);
        Assert.NotEqual(cycle[0],cycle[1]);
    }
    [Fact]
    public void Selection_is_stable_within_hour_and_independent_of_input_order() {
        var items=new[]{Item(1,"https://b.test"),Item(2,"https://a.test")};
        Assert.Equal(HomeOpinionQuery.Snapshot(100,items,Now).Article,HomeOpinionQuery.Snapshot(100,items.Reverse(),Now.AddMinutes(59)).Article);
        Assert.Equal(3600000,HomeOpinionQuery.Snapshot(100,items,Now).RefreshAfterMs);
        Assert.Equal(1000,HomeOpinionQuery.Snapshot(100,items,Now.AddMinutes(59).AddSeconds(59)).RefreshAfterMs);
    }
    [Fact]
    public void Only_newest_story_per_publisher_competes_with_deterministic_ties() {
        var selected=HomeOpinionQuery.Snapshot(100,new[]{Item(1,"https://a.test"),Item(2,"https://a.test"),Item(3,"https://a.test",60,true)},Now);
        Assert.Equal(2,selected.Article!.Id);
    }
    [Fact]
    public void Empty_category_and_single_source_are_valid() {
        Assert.Null(HomeOpinionQuery.Snapshot(null,[],Now).Article);
        var item=Item(1,"https://a.test");
        Assert.Equal(item,HomeOpinionQuery.Snapshot(100,[item],Now.AddHours(123)).Article);
    }
}
