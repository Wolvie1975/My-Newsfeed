using MyDailyFeed.Web.Watching;
namespace MyDailyFeed.Tests;
public class WatchSortTests
{
    private static WatchListing Listing()=>new(new[]{
        new WatchTitle(1,"tv","Zulu",null,null,"",new[]{"Service"},new(2020,1,1),4),
        new WatchTitle(2,"tv","alpha",null,null,"",[],new(2024,1,1),9),
        new WatchTitle(3,"tv","Bravo",null,null,"",new[]{"Service"},new(2024,2,1),2),
        new WatchTitle(4,"tv","Delta",null,null,"")},2);
    [Theory]
    [InlineData("services",3,1,2,4)]
    [InlineData("title",2,3,4,1)]
    [InlineData("newest",3,2,1,4)]
    [InlineData("popular",2,1,3,4)]
    [InlineData("invalid",3,1,2,4)]
    public void Sorts_preserve_shared_listing_and_put_unknown_metadata_last(string mode,int a,int b,int c,int d){var original=Listing();var sorted=WatchCatalog.Sort(original,mode);Assert.Equal(new[]{a,b,c,d},sorted.Items.Select(t=>t.Id));Assert.Equal(new[]{1,2,3,4},original.Items.Select(t=>t.Id));Assert.Equal(2,sorted.Pages);}
}
