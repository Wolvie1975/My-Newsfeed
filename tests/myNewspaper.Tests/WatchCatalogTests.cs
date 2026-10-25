using System.Net;
using Microsoft.Extensions.Configuration;
using myNewspaper.Web.Watching;
namespace myNewspaper.Tests;
public class WatchCatalogTests
{
    private sealed class Provider : HttpMessageHandler,IHttpClientFactory
    {
        public int Calls;public string? LastPath;public bool Broken;
        public HttpClient CreateClient(string name)=>new(this){BaseAddress=new("https://api.themoviedb.org/3/")};
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage r,CancellationToken token){
            Calls++;LastPath=r.RequestUri!.PathAndQuery;
            Assert.Equal("Bearer",r.Headers.Authorization?.Scheme);
            var json=LastPath.Contains("providers") ? """{"results":{"US":{"link":"https://www.themoviedb.org/movie/1/watch?locale=US","flatrate":[{"provider_id":8,"provider_name":"Service <one>","logo_path":"/service.png"}],"rent":[{"provider_name":"Rental","logo_path":"https://evil.invalid/logo.png"}]},"CA":{"buy":[{"provider_name":"Canada only"}]}}}""" : LastPath.Contains("search/") ? """{"total_pages":99,"results":[{"id":1,"title":"Example","release_date":"2020-01-01","popularity":13.5,"poster_path":"/poster.jpg"},{"id":2,"title":"Hidden","adult":true}]}""" : """{"id":1,"title":"Example","name":"Example TV","release_date":"2020-01-01","first_air_date":"2021-01-01","poster_path":"https://evil.invalid/a.jpg"}""";
            if(LastPath.Contains("/discover/"))json="""{"total_pages":2,"results":[{"id":3,"title":"Example movie","name":"Ongoing series","first_air_date":"2020-01-01"}]}""";
            if(LastPath.Contains("/2/watch/providers"))json="""{"results":{"US":{"rent":[{"provider_id":8,"provider_name":"Rental only"}]}}}""";
            if(LastPath.Contains("/3/watch/providers"))json="""{"results":{"CA":{"flatrate":[{"provider_id":8,"provider_name":"Canada only"}]}}}""";
            if(LastPath.Contains("/4/watch/providers"))json="""{"results":{"US":{"flatrate":[],"rent":[],"link":"https://www.themoviedb.org/movie/4/watch"}}}""";
            if(LastPath.Contains("/5/watch/providers"))json="""{"results":{"US":{"buy":[{"provider_id":60,"provider_name":"Fandango"}]}}}""";
            if(LastPath.Contains("/7/watch/providers"))json="""{"results":null}""";
            return Task.FromResult(new HttpResponseMessage(Broken||LastPath.Contains("/6/watch/providers")?HttpStatusCode.ServiceUnavailable:HttpStatusCode.OK){Content=new StringContent(json)});
        }
    }
    private static WatchCatalog Catalog(Provider provider)=>new(provider,new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string,string?>{{"Movies:AccessToken","test-token"}}).Build());
    [Fact]public async Task Search_encodes_query_bounds_pages_filters_adults_and_caches(){
        var provider=new Provider();var catalog=Catalog(provider);
        var result=await catalog.List("movie","  A & B  ",0,999);
        Assert.Single(result.Items);Assert.Equal(new DateOnly(2020,1,1),result.Items[0].ReleaseDate);Assert.Equal(13.5,result.Items[0].Popularity);Assert.Equal(10,result.Pages);Assert.Contains("query=A%20%26%20B",provider.LastPath);Assert.Contains("page=10",provider.LastPath);
        Assert.Equal("https://image.tmdb.org/t/p/w342/poster.jpg",result.Items[0].Poster);
        await catalog.List("movie","A & B",0,10);Assert.Equal(1,provider.Calls);
        Assert.True((await catalog.List("person","test")).Failed);Assert.Equal(1,provider.Calls);
    }
    [Fact]public async Task Availability_is_US_only_preserves_categories_and_cache_check_time(){
        var provider=new Provider();var catalog=Catalog(provider);var result=await catalog.Detail("movie",1);
        Assert.NotNull(result);Assert.Null(result.Title.Poster);Assert.Equal(new[]{"Subscription","Rent"},result.Offers.Select(o=>o.Category));
        Assert.DoesNotContain(result.Offers.SelectMany(o=>o.Providers),n=>n=="Canada only");
        Assert.StartsWith("https://www.themoviedb.org/",result.Link);
        var again=await catalog.Detail("movie",1);Assert.Equal(result.Checked,again!.Checked);Assert.Equal(2,provider.Calls);
        Assert.Null(await catalog.Detail("person",1));Assert.Null(await catalog.Detail("tv",-1));Assert.Equal(2,provider.Calls);
    }
    [Fact]public async Task Services_rank_real_access_before_rental_only_without_mutating_shared_listing(){
        var provider=new Provider();var catalog=Catalog(provider);
        var original=new WatchListing(new[]{new WatchTitle(2,"movie","Rental",null,null,""),new WatchTitle(1,"movie","Included",null,null,"")},1);
        var ranked=await catalog.Prioritize(original,"8,8,-1,garbage");
        Assert.Equal(new[]{1,2},ranked.Items.Select(t=>t.Id));
        Assert.Equal(new[]{"Service <one>"},ranked.Items[0].MatchingServices);
        Assert.Empty(ranked.Items[1].MatchingServices!);
        Assert.Equal(new[]{2,1},original.Items.Select(t=>t.Id));
        Assert.All(original.Items,t=>Assert.Null(t.MatchingServices));
        await catalog.Prioritize(original,"8");Assert.Equal(2,provider.Calls);
        Assert.Equal(new[]{3,8},WatchCatalog.ServiceIds("8,3,8,0,-1,invalid"));
    }
    [Fact]public async Task Result_rows_include_grouped_US_providers_safe_logos_and_cached_links_without_selected_services(){
        var provider=new Provider();var catalog=Catalog(provider);
        var original=new WatchListing(new[]{new WatchTitle(1,"movie","Example",null,null,""),new WatchTitle(6,"tv","Unknown",null,null,"")},1);
        var listing=await catalog.Prioritize(original,null,filterUnavailable:false);
        var title=listing.Items[0];
        Assert.Equal(new[]{"Subscription","Rent"},title.Offers!.Select(o=>o.Category));
        var service=Assert.Single(title.Offers[0].Options!);
        Assert.Equal(8,service.Id);Assert.Equal("Service <one>",service.Name);
        Assert.Equal("https://image.tmdb.org/t/p/w92/service.png",service.Logo);
        Assert.Null(Assert.Single(title.Offers[1].Options!).Logo);
        Assert.StartsWith("https://www.themoviedb.org/",title.WatchLink);
        Assert.DoesNotContain(title.Offers.SelectMany(o=>o.Providers),n=>n=="Canada only");
        Assert.False(title.OffersUnavailable);Assert.True(listing.Items[1].OffersUnavailable);
        Assert.Null(original.Items[0].Offers);
        await catalog.Prioritize(original,null,filterUnavailable:false);Assert.Equal(2,provider.Calls);
    }
    [Theory]
    [InlineData(60,"Fandango",false)]
    [InlineData(999,"AMC Theatres",false)]
    [InlineData(7,"Fandango At Home",true)]
    [InlineData(332,"Fandango at Home Free",true)]
    [InlineData(80,"AMC",true)]
    [InlineData(526,"AMC+",true)]
    [InlineData(289,"Cinemax Amazon Channel",true)]
    public void Ticketing_is_excluded_without_removing_home_services(int id,string name,bool allowed)=>Assert.Equal(allowed,WatchCatalog.HomeViewingService(id,name));
    [Fact]public void Old_ticketing_selections_are_ignored()=>Assert.Equal(new[]{7,526},WatchCatalog.ServiceIds("60,7,526"));
    [Theory]
    [InlineData("tv",null,true)]
    [InlineData("tv","Breaking Bad",false)]
    [InlineData("movie",null,false)]
    public async Task Active_status_filter_applies_only_to_tv_browsing(string kind,string? query,bool filtered){
        var provider=new Provider();var result=await Catalog(provider).List(kind,query,18,2);
        Assert.False(result.Failed);Assert.Contains("page=2",provider.LastPath);
        if(filtered){Assert.Contains("with_status=0%7C1%7C2%7C5",provider.LastPath);Assert.Contains("with_genres=18",provider.LastPath);}
        else Assert.DoesNotContain("with_status",provider.LastPath);
        if(!string.IsNullOrWhiteSpace(query))Assert.Contains("/search/tv",provider.LastPath);
    }
    [Fact]public async Task Filters_confirmed_missing_US_options_even_without_service_preferences_but_retains_rentals_and_unknowns(){
        var provider=new Provider();var catalog=Catalog(provider);
        var original=new WatchListing(Enumerable.Range(1,7).Select(id=>new WatchTitle(id,"movie","Title "+id,null,null,"")).ToList(),3);
        var result=await catalog.Prioritize(original,null);
        Assert.Equal(new[]{1,2,6,7},result.Items.Select(t=>t.Id));
        Assert.Equal(3,result.Pages);Assert.Equal(7,original.Items.Count);
        Assert.NotNull(result.Items[0].MatchingServices);Assert.NotNull(result.Items[1].MatchingServices);
        Assert.Null(result.Items[2].MatchingServices);Assert.Null(result.Items[3].MatchingServices);
        await catalog.Prioritize(original,null);Assert.Equal(7,provider.Calls);
    }
    [Fact]public async Task Provider_failure_is_distinct_from_empty_results(){var provider=new Provider{Broken=true};var catalog=Catalog(provider);Assert.True((await catalog.List("tv","title")).Failed);Assert.Null(await catalog.Detail("tv",1));}
}
