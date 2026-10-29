using System.Net;
using System.Text;
using Microsoft.EntityFrameworkCore;
using myNewspaper.Web.Data;
using myNewspaper.Web.Requests;

namespace myNewspaper.Tests;

public class RequestCheckTests
{
    [Theory]
    [InlineData("127.0.0.1")]
    [InlineData("10.1.2.3")]
    [InlineData("100.64.0.1")]       // Tailscale
    [InlineData("100.127.255.254")]
    [InlineData("172.16.0.1")]
    [InlineData("172.18.0.4")]       // the Docker network the database is on
    [InlineData("192.168.1.10")]
    [InlineData("169.254.169.254")]  // cloud metadata
    [InlineData("0.0.0.0")]
    [InlineData("224.0.0.1")]
    [InlineData("::1")]
    [InlineData("::")]
    [InlineData("fd7a:115c:a1e0::1")]  // Tailscale IPv6
    [InlineData("fe80::1")]
    [InlineData("::ffff:10.0.0.1")]    // IPv4-mapped
    [InlineData("64:ff9b::a00:1")]     // NAT64 of 10.0.0.1
    public void Private_loopback_link_local_and_tailscale_addresses_are_refused(string address) =>
        Assert.False(SsrfGuard.IsAllowedAddress(IPAddress.Parse(address)));

    [Theory]
    [InlineData("8.8.8.8")]
    [InlineData("100.63.255.255")]
    [InlineData("100.128.0.1")]
    [InlineData("172.32.0.1")]
    [InlineData("2606:4700:4700::1111")]
    [InlineData("64:ff9b::808:808")]   // NAT64 of 8.8.8.8
    public void Public_addresses_are_allowed(string address) => Assert.True(SsrfGuard.IsAllowedAddress(IPAddress.Parse(address)));

    [Theory]
    [InlineData("file:///etc/passwd")]
    [InlineData("ftp://example.com/feed")]
    [InlineData("gopher://example.com/")]
    [InlineData("https://user:pass@example.com/")]
    [InlineData("/relative")]
    public void Only_http_and_https_links_are_followed(string url)
    {
        Uri.TryCreate(url, UriKind.RelativeOrAbsolute, out var uri);
        Assert.False(SsrfGuard.IsAllowedUri(uri));
    }

    [Theory]
    [InlineData("http://localhost:1/")]
    [InlineData("http://127.0.0.1:1/")]
    [InlineData("http://10.0.0.1:1/")]
    [InlineData("http://100.64.0.1:1/")]
    [InlineData("http://[::1]:1/")]
    public async Task The_guarded_client_refuses_to_connect_inside_the_network(string url)
    {
        using var client = new HttpClient(SsrfGuard.CreateHandler());
        var ex = await Assert.ThrowsAsync<HttpRequestException>(() => client.GetAsync(url));
        Assert.Contains("isn't a public address", ex.Message);
    }

    [Theory]
    [InlineData("http://127.0.0.1:1/admin")]
    [InlineData("http://10.0.0.5/")]
    [InlineData("http://100.100.100.100/")]
    [InlineData("file:///etc/passwd")]
    public async Task A_redirect_from_a_public_address_into_the_network_is_refused(string target)
    {
        // The first hop answers with a redirect; every later hop goes through the real guarded handler.
        var checks = new RequestChecks(new Clients(new RedirectOnce(new Uri(target), SsrfGuard.CreateHandler())));
        await Assert.ThrowsAsync<HttpRequestException>(() => checks.FetchAsync(new Uri("https://public.example/feed")));
    }

    [Fact]
    public async Task Redirects_stop_after_five()
    {
        var checks = new RequestChecks(new Clients(new Fake(_ => Redirect("https://loop.example/again"))));
        var ex = await Assert.ThrowsAsync<HttpRequestException>(() => checks.FetchAsync(new Uri("https://loop.example/")));
        Assert.Contains("redirects", ex.Message);
    }

    [Theory]
    [InlineData("https://www.youtube.com/channel/UCabcdefghijklmnopqrstuv", "UCabcdefghijklmnopqrstuv")]
    [InlineData("youtube.com/channel/UCabcdefghijklmnopqrstuv/videos", "UCabcdefghijklmnopqrstuv")]
    [InlineData("UCabcdefghijklmnopqrstuv", "UCabcdefghijklmnopqrstuv")]
    public async Task Channel_links_resolve_without_fetching(string link, string id)
    {
        var checks = new RequestChecks(new Clients(new Fake(_ => throw new InvalidOperationException("No fetch expected."))));
        Assert.Equal(id, await checks.ResolveChannelAsync(link));
    }

    [Theory]
    [InlineData("https://www.youtube.com/@somehandle", "/@somehandle", """<link rel="canonical" href="https://www.youtube.com/channel/UC0123456789abcdefghijkl">""")]
    [InlineData("https://www.youtube.com/c/OldName", "/c/OldName", """<meta itemprop="identifier" content="UC0123456789abcdefghijkl">""")]
    [InlineData("https://www.youtube.com/watch?v=dQw4w9WgXcQ", "/watch?v=dQw4w9WgXcQ", """{"videoDetails":{"channelId":"UC0123456789abcdefghijkl"}}""")]
    [InlineData("https://youtu.be/dQw4w9WgXcQ", "/watch?v=dQw4w9WgXcQ", """{"videoDetails":{"channelId":"UC0123456789abcdefghijkl"}}""")]
    public async Task Handles_custom_names_and_video_links_resolve_from_the_page(string link, string path, string html)
    {
        var checks = new RequestChecks(new Clients(new Fake(request =>
            request.RequestUri!.Host == "www.youtube.com" && request.RequestUri.PathAndQuery == path ? Ok(html) : new HttpResponseMessage(HttpStatusCode.NotFound))));
        Assert.Equal("UC0123456789abcdefghijkl", await checks.ResolveChannelAsync(link));
    }

    [Theory]
    [InlineData("https://example.com/channel/UC0123456789abcdefghijkl")]
    [InlineData("https://www.youtube.com/@changed-page")]
    public async Task Anything_unrecognized_falls_back_to_needing_a_channel_id(string link)
    {
        var checks = new RequestChecks(new Clients(new Fake(_ => Ok("<html>no ids here</html>"))));
        Assert.Null(await checks.ResolveChannelAsync(link));
    }

    [Fact]
    public async Task News_discovery_finds_advertised_feeds_and_the_usual_paths()
    {
        const string page = """
            <html><head>
              <link rel="alternate" type="application/rss+xml" title="News" href="/news.rss">
              <link type='application/atom+xml' href='https://example.com/atom.xml' rel='alternate'>
              <link rel="stylesheet" href="/site.css">
            </head></html>
            """;
        var checks = new RequestChecks(new Clients(new Fake(request => request.RequestUri!.AbsolutePath switch
        {
            "/" => Ok(page),
            "/news.rss" => Ok(Rss),
            "/atom.xml" => Ok(Atom),
            "/feed" => Ok(Rss),
            _ => new HttpResponseMessage(HttpStatusCode.NotFound),
        })));
        Assert.Equal(["https://example.com/news.rss", "https://example.com/atom.xml", "https://example.com/feed"],
            await checks.FindFeedsAsync(new Uri("https://example.com/")));
    }

    [Fact]
    public async Task A_feed_link_is_used_directly()
    {
        var checks = new RequestChecks(new Clients(new Fake(_ => Ok(Rss))));
        Assert.Equal(["https://example.com/rss"], await checks.FindFeedsAsync(new Uri("https://example.com/rss")));
    }

    private const string StarSearch = "https://news.google.com/rss/search?q=site%3Akansascity.com+when%3A30d&hl=en-US&gl=US&ceid=US%3Aen";

    private static string GoogleNews(params string[] sources) =>
        """<?xml version="1.0"?><rss version="2.0"><channel><title>Google News</title>"""
        + string.Concat(sources.Select((s, i) => $"""<item><title>Story {i} - Paper</title><source url="{s}">Paper</source></item>"""))
        + "</channel></rss>";

    [Fact]
    public async Task Google_news_is_offered_only_when_its_headlines_come_from_the_site()
    {
        var body = GoogleNews("https://www.kansascity.com", "https://amp.kansascity.com", "https://www.kansascity.com");
        var checks = new RequestChecks(new Clients(new Fake(request =>
            request.RequestUri!.AbsoluteUri == StarSearch ? Ok(body) : new HttpResponseMessage(HttpStatusCode.NotFound))));
        Assert.Equal((StarSearch, 3), await checks.GoogleNewsFeedAsync(new Uri("https://www.kansascity.com/")));

        // A section link searches that section, in the same form as the USA Today sources.
        string? asked = null;
        var section = new RequestChecks(new Clients(new Fake(request => { asked = request.RequestUri!.AbsoluteUri; return Ok(GoogleNews()); })));
        Assert.Null(await section.GoogleNewsFeedAsync(new Uri("https://www.usatoday.com/story/news")));
        Assert.Equal("https://news.google.com/rss/search?q=site%3Ausatoday.com%2Fstory%2Fnews%2F+when%3A30d&hl=en-US&gl=US&ceid=US%3Aen", asked);

        // Mostly other publishers (Google matched loosely), no headlines, or no answer: nothing is offered.
        var mixed = new RequestChecks(new Clients(new Fake(_ => Ok(GoogleNews("https://www.kansascity.com", "https://www.kshb.com")))));
        Assert.Null(await mixed.GoogleNewsFeedAsync(new Uri("https://kansascity.com")));
        var down = new RequestChecks(new Clients(new Fake(_ => throw new HttpRequestException("blocked"))));
        Assert.Null(await down.GoogleNewsFeedAsync(new Uri("https://kansascity.com")));
    }

    [Fact]
    public async Task A_site_that_blocks_the_checker_is_offered_through_google_news()
    {
        // A made-up paper, so the answer doesn't depend on which sources the household has added.
        var checks = new RequestChecks(new Clients(new Fake(request => request.RequestUri!.Host == "news.google.com"
            ? Ok(GoogleNews("https://www.blocked-paper.example", "https://www.blocked-paper.example"))
            : throw new HttpRequestException("Connection reset"))));
        await using var db = Database();
        var result = await checks.CheckNewsAsync(db, "https://www.blocked-paper.example/");
        Assert.True(result.Ok);
        Assert.Equal("The site didn't respond · Google News feed found (2 headlines)", result.Summary);
        Assert.Equal(["https://news.google.com/rss/search?q=site%3Ablocked-paper.example+when%3A30d&hl=en-US&gl=US&ceid=US%3Aen"], result.FeedUrls);

        var nothing = new RequestChecks(new Clients(new Fake(_ => Ok("<html>no feeds</html>"))));
        var none = await nothing.CheckNewsAsync(db, "https://example.com/");
        Assert.False(none.Ok);
        Assert.Equal("No feed found on example.com · needs a feed address", none.Summary);
    }

    [Fact]
    public async Task A_youtube_channel_is_ready_once_its_id_is_found_even_if_its_feed_is_down()
    {
        // YouTube's feeds can answer 404 while the scraper still collects from the channel page.
        var checks = new RequestChecks(new Clients(new Fake(_ => new HttpResponseMessage(HttpStatusCode.NotFound))));
        await using var db = Database();
        var result = await checks.CheckYoutubeAsync(db, "https://www.youtube.com/channel/UCzzzzzzzzzzzzzzzzzzzzzz");
        Assert.True(result.Ok);
        Assert.Equal("Channel ID found from the link", result.Summary);
        Assert.Equal("UCzzzzzzzzzzzzzzzzzzzzzz", result.ChannelId);
        Assert.Equal([RequestChecks.FeedUrl("UCzzzzzzzzzzzzzzzzzzzzzz")], result.FeedUrls);
    }

    private static WebScraperContext Database() =>
        new(new DbContextOptionsBuilder<WebScraperContext>().UseSqlServer(TestDatabase.ConnectionString).Options);

    private const string Rss = """<?xml version="1.0"?><rss version="2.0"><channel><title>T</title><item><title>Newest story</title></item><item><title>Older</title></item></channel></rss>""";
    private const string Atom = """<feed xmlns="http://www.w3.org/2005/Atom"><title>T</title><entry><title> Atom entry </title></entry></feed>""";

    [Fact]
    public void Feeds_parse_as_rss_atom_or_rdf()
    {
        Assert.Equal((2, "Newest story"), RequestChecks.ParseFeed(Rss)!.Value);
        Assert.Equal((1, "Atom entry"), RequestChecks.ParseFeed(Atom)!.Value);
        Assert.Equal((1, "R"), RequestChecks.ParseFeed("""<rdf:RDF xmlns:rdf="http://www.w3.org/1999/02/22-rdf-syntax-ns#" xmlns="http://purl.org/rss/1.0/"><item><title>R</title></item></rdf:RDF>""")!.Value);
        Assert.Null(RequestChecks.ParseFeed("<html><body>Not a feed</body></html>"));
        Assert.Null(RequestChecks.ParseFeed("not xml"));
    }

    [Fact]
    public void Feeds_with_a_dtd_are_refused_so_entities_never_expand() =>
        Assert.Null(RequestChecks.ParseFeed("""<?xml version="1.0"?><!DOCTYPE rss [<!ENTITY x SYSTEM "file:///etc/passwd">]><rss><channel><item><title>&x;</title></item></channel></rss>"""));

    [Fact]
    public async Task Test_feed_reports_the_count_and_newest_title()
    {
        var checks = new RequestChecks(new Clients(new Fake(_ => Ok(Rss))));
        var result = await checks.TestFeedAsync("https://example.com/rss");
        Assert.True(result.Ok);
        Assert.Equal(2, result.Entries);
        Assert.Equal("Newest story", result.NewestTitle);
        Assert.False((await checks.TestFeedAsync("file:///etc/passwd")).Ok);
    }

    [Fact]
    public async Task Oversized_responses_are_refused()
    {
        var checks = new RequestChecks(new Clients(new Fake(_ => Ok(new string('a', RequestChecks.MaxBytes + 1)))));
        await Assert.ThrowsAsync<HttpRequestException>(() => checks.FetchAsync(new Uri("https://example.com/")));
    }

    [Fact]
    public async Task A_youtube_sized_channel_page_is_read()
    {
        // YouTube's channel pages are about 2.6 MB, with the canonical link well past the first megabyte.
        var page = new string(' ', 2_600_000) + "<link rel=\"canonical\" href=\"https://www.youtube.com/channel/UCXuqSBlHAE6Xw-yeJA0Tunw\">";
        var checks = new RequestChecks(new Clients(new Fake(_ => Ok(page))));
        Assert.Equal("UCXuqSBlHAE6Xw-yeJA0Tunw", await checks.ResolveChannelAsync("https://www.youtube.com/@LinusTechTips"));
    }

    private static HttpResponseMessage Ok(string body) => new(HttpStatusCode.OK) { Content = new StringContent(body, Encoding.UTF8, "text/html") };

    private static HttpResponseMessage Redirect(string to) => new(HttpStatusCode.Found) { Headers = { Location = new Uri(to) } };

    private sealed class Fake(Func<HttpRequestMessage, HttpResponseMessage> respond) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) =>
            Task.FromResult(respond(request));
    }

    private sealed class RedirectOnce(Uri target, HttpMessageHandler inner) : DelegatingHandler(inner)
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) =>
            request.RequestUri!.Host == "public.example" ? Task.FromResult(Redirect(target.OriginalString)) : base.SendAsync(request, cancellationToken);
    }

    private sealed class Clients(HttpMessageHandler handler) : IHttpClientFactory
    {
        public HttpClient CreateClient(string name) => new(handler, disposeHandler: false);
    }
}
