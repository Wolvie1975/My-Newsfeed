using System.Net;
using Microsoft.Extensions.Configuration;
using myNewspaper.Web.Watching;
namespace myNewspaper.Tests;

public class WatchMixedSearchTests
{
    private sealed class Provider : HttpMessageHandler, IHttpClientFactory
    {
        public readonly List<string> Paths = [];
        public HttpClient CreateClient(string name) => new(this) { BaseAddress = new("https://api.themoviedb.org/3/") };
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
        {
            var path = request.RequestUri!.PathAndQuery; Paths.Add(path);
            var json = path.Contains("/search/multi") ? """
                {"total_pages":1,"results":[
                {"id":7,"media_type":"movie","title":"Shared title movie","release_date":"2020-01-01"},
                {"id":7,"media_type":"tv","name":"Shared title series","first_air_date":"2021-01-01"},
                {"id":7,"media_type":"tv","name":"Duplicate series"},
                {"id":8,"media_type":"person","name":"Actor"},
                {"id":9,"media_type":"movie","title":"Adult","adult":true}]}
                """ : """{"results":{"US":{"flatrate":[{"provider_id":8,"provider_name":"Netflix"}]}}}""";
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(json) });
        }
    }
    [Fact]
    public async Task Mixed_search_keeps_overlapping_movie_tv_ids_filters_people_and_uses_correct_providers()
    {
        var provider = new Provider();
        var catalog = new WatchCatalog(provider, new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?> { { "Movies:AccessToken", "test" } }).Build());
        var result = await catalog.FilledPage("multi", "Shared title", selected: "8");
        Assert.False(result.Failed); Assert.Equal(2, result.Items.Count);
        Assert.Contains(result.Items, t => t.Kind == "movie" && t.Id == 7 && t.Year == "2020");
        Assert.Contains(result.Items, t => t.Kind == "tv" && t.Id == 7 && t.Year == "2021");
        Assert.All(result.Items, t => Assert.Contains("Netflix", t.MatchingServices!));
        Assert.Contains(provider.Paths, p => p.Contains("/search/multi?query=Shared%20title"));
        Assert.Contains(provider.Paths, p => p.Contains("/movie/7/watch/providers"));
        Assert.Contains(provider.Paths, p => p.Contains("/tv/7/watch/providers"));
        Assert.True((await catalog.FilledPage("multi", null)).Failed);
        Assert.False(WatchCatalog.Kind("multi"));
    }
}
