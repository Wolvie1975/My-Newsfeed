using System.Net;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using MyDailyFeed.Web.Watching;
namespace MyDailyFeed.Tests;
public class WatchPagingTests
{
    private sealed class Provider : HttpMessageHandler,IHttpClientFactory
    {
        public readonly List<int> Batches=[];
        public bool Empty,Fail;
        public int Total=3;
        public HttpClient CreateClient(string name)=>new(this){BaseAddress=new("https://api.themoviedb.org/3/")};
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,CancellationToken token){
            var uri=request.RequestUri!;string json;
            if(uri.AbsolutePath.Contains("/discover/")){
                var page=int.Parse(uri.Query.TrimStart('?').Split('&').Single(s=>s.StartsWith("page=")).Split('=')[1]);Batches.Add(page);
                var ids=Enumerable.Range((page-1)*20+1,20).Select(id=>page==2&&id==21?20:id);
                json=JsonSerializer.Serialize(new {total_pages=Total,results=ids.Select(id=>new {id,name="Show "+id,first_air_date="2020-01-01"})});
            }else{var id=int.Parse(uri.AbsolutePath.Split('/')[3]);json=Empty||id%4==0?"""{"results":{}}""":"""{"results":{"US":{"rent":[{"provider_id":7,"provider_name":"Fandango At Home"}]}}}""";}
            return Task.FromResult(new HttpResponseMessage(Fail?HttpStatusCode.ServiceUnavailable:HttpStatusCode.OK){Content=new StringContent(json)});
        }
    }
    private static WatchCatalog Catalog(Provider p)=>new(p,new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string,string?>{{"Movies:AccessToken","test"}}).Build());
    [Fact]public async Task Fills_pages_from_eligible_prefix_without_repeats_skips_or_extra_refetches(){
        var provider=new Provider();var catalog=Catalog(provider);
        var first=await catalog.FilledPage("tv",null);Assert.Equal(20,first.Items.Count);Assert.Equal(new[]{1,2},provider.Batches);
        var second=await catalog.FilledPage("tv",null,page:2);Assert.Equal(20,second.Items.Count);
        var previous=await catalog.FilledPage("tv",null);Assert.Equal(first.Items.Select(t=>t.Id),previous.Items.Select(t=>t.Id));
        var last=await catalog.FilledPage("tv",null,page:3);Assert.Equal(4,last.Items.Count);Assert.False(last.CanContinue);Assert.Equal(3,last.Pages);
        Assert.Equal(Enumerable.Range(1,60).Where(id=>id%4!=0&&id!=21),first.Items.Concat(second.Items).Concat(last.Items).Select(t=>t.Id));
        Assert.Equal(new[]{1,2,3},provider.Batches);
    }
    [Fact]public async Task Concurrent_readers_share_the_same_membership_window(){
        var provider=new Provider();var catalog=Catalog(provider);var pages=await Task.WhenAll(catalog.FilledPage("tv",null),catalog.FilledPage("tv",null));Assert.Equal(pages[0].Items.Select(t=>t.Id),pages[1].Items.Select(t=>t.Id));Assert.Equal(new[]{1,2},provider.Batches);
    }
    [Fact]public async Task Scan_budget_leaves_retry_on_same_page_without_inventing_next_page(){
        var provider=new Provider{Empty=true,Total=50};var result=await Catalog(provider).FilledPage("tv",null);Assert.Empty(result.Items);Assert.True(result.CanContinue);Assert.Equal(1,result.Pages);Assert.Equal(10,provider.Batches.Count);
    }
    [Fact]public async Task Upstream_failure_remains_distinct_from_no_matching_titles(){var result=await Catalog(new Provider{Fail=true}).FilledPage("tv",null);Assert.True(result.Failed);Assert.True(result.CanContinue);}
}
