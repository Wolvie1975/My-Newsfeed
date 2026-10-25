using System.Net;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using myNewspaper.Web.Watching;
namespace myNewspaper.Tests;
public class WatchShelfTests
{
    private sealed class Provider : HttpMessageHandler,IHttpClientFactory
    {
        public readonly List<string> Paths=[];public bool FailTrendingTv;
        public HttpClient CreateClient(string name)=>new(this){BaseAddress=new("https://api.themoviedb.org/3/")};
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,CancellationToken token){
            var uri=request.RequestUri!;lock(Paths)Paths.Add(uri.PathAndQuery);string json;
            if(uri.AbsolutePath.EndsWith("/watch/providers")){
                // Even IDs stream on Netflix (8); multiples of five have no US options; the rest are rentals.
                var id=int.Parse(uri.AbsolutePath.Split('/')[3]);
                json=id%5==0?"""{"results":{}}""":id%2==0?"""{"results":{"US":{"flatrate":[{"provider_id":8,"provider_name":"Netflix","logo_path":"/n.png"}]}}}""":"""{"results":{"US":{"rent":[{"provider_id":2,"provider_name":"Apple TV"}]}}}""";
            }else{
                if(FailTrendingTv&&uri.AbsolutePath.Contains("trending/tv"))return Task.FromResult(new HttpResponseMessage(HttpStatusCode.ServiceUnavailable));
                var page=int.Parse(uri.Query.TrimStart('?').Split('&').Single(s=>s.StartsWith("page=")).Split('=')[1]);
                var offset=uri.AbsolutePath.Contains("/tv")?1000:0;
                json=JsonSerializer.Serialize(new {total_pages=5,results=Enumerable.Range((page-1)*10+1+offset,10).Select(id=>new {id,title="Movie "+id,name="Show "+id,release_date="2026-10-01",first_air_date="2026-01-01"})});
            }
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK){Content=new StringContent(json)});
        }
    }
    private static WatchCatalog Catalog(Provider p)=>new(p,new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string,string?>{{"Movies:AccessToken","test"}}).Build());

    [Fact]public async Task Trending_row_keeps_upstream_order_fills_twelve_eligible_titles_and_caches(){
        var provider=new Provider();var catalog=Catalog(provider);
        var row=await catalog.TrendingShelf("movie","8");
        Assert.Equal(WatchCatalog.ShelfSize,row.Items.Count);Assert.False(row.Failed);
        Assert.DoesNotContain(row.Items,t=>t.Id%5==0);
        Assert.Equal(row.Items.Select(t=>t.Id).Order(),row.Items.Select(t=>t.Id));
        Assert.All(row.Items.Where(t=>t.Id%2==0),t=>Assert.Equal("Netflix",Assert.Single(t.MatchingProviders!).Name));
        Assert.Contains(provider.Paths,p=>p.StartsWith("/3/trending/movie/week?page=2"));Assert.DoesNotContain(provider.Paths,p=>p.Contains("page=3"));
        var calls=provider.Paths.Count;var again=await catalog.TrendingShelf("movie",null);
        Assert.Equal(row.Items.Select(t=>t.Id),again.Items.Select(t=>t.Id));Assert.Equal(calls,provider.Paths.Count);Assert.All(again.Items,t=>Assert.Empty(t.MatchingProviders!));
    }
    [Fact]public async Task Services_row_mixes_movies_and_tv_and_keeps_only_titles_on_chosen_services(){
        var provider=new Provider();var row=await Catalog(provider).ServicesShelf("8");
        Assert.Equal(WatchCatalog.ShelfSize,row.Items.Count);
        Assert.Equal(Enumerable.Range(0,12).Select(i=>i%2==0?"movie":"tv"),row.Items.Select(t=>t.Kind));
        Assert.All(row.Items,t=>Assert.Equal("Netflix",Assert.Single(t.MatchingServices!)));
        Assert.All(provider.Paths.Where(p=>p.Contains("/discover/")),p=>Assert.Contains("with_watch_providers=8",p));
    }
    [Fact]public async Task Services_row_without_selection_is_empty_without_requests(){
        var provider=new Provider();var row=await Catalog(provider).ServicesShelf("");
        Assert.Empty(row.Items);Assert.False(row.Failed);Assert.Empty(provider.Paths);
    }
    [Fact]public async Task New_movies_row_requests_the_last_sixty_days_with_us_viewing_options(){
        var provider=new Provider();await Catalog(provider).NewMoviesShelf(new DateOnly(2026,10,25),null);
        var path=provider.Paths.First(p=>p.Contains("/discover/movie"));
        Assert.Contains("primary_release_date.gte=2026-08-26",path);Assert.Contains("primary_release_date.lte=2026-10-25",path);
        Assert.Contains("watch_region=US",path);Assert.Contains("with_watch_monetization_types=flatrate%7Cfree%7Cads%7Crent%7Cbuy",path);
    }
    [Fact]public async Task One_failed_row_does_not_affect_another(){
        var catalog=Catalog(new Provider{FailTrendingTv=true});
        var rows=await Task.WhenAll(catalog.TrendingShelf("tv",null),catalog.TrendingShelf("movie",null));
        Assert.True(rows[0].Failed);Assert.Empty(rows[0].Items);Assert.False(rows[1].Failed);Assert.Equal(12,rows[1].Items.Count);
    }
}
