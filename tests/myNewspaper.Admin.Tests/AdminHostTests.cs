using System.Net;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using myNewspaper.Admin;
using myNewspaper.Web.Profiles;
using myNewspaper.Web.Requests;
using myNewspaper.Web.Scores;

namespace myNewspaper.Tests;

public class AdminHostTests
{
    private sealed class App : WebApplicationFactory<AdminProgram>
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseSetting("ConnectionStrings:DefaultConnection", "Server=unused;Database=unused;Integrated Security=true;TrustServerCertificate=true");
            builder.UseSetting("Admin:Password", "admin-host-tests-only");
        }
    }

    [Fact]
    public async Task Admin_has_its_own_manifest_requires_login_and_has_no_public_routes_or_scheduled_workers()
    {
        await using var app = new App();
        using var client = app.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        var login = await client.GetAsync("/login");
        Assert.Equal(HttpStatusCode.OK, login.StatusCode);
        Assert.Contains("myNewspaper.Admin.Csrf", string.Join(";", login.Headers.GetValues("Set-Cookie")));
        var response = await client.GetAsync("/admin");
        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.StartsWith("http://localhost/login", response.Headers.Location!.ToString());
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("/admin/api/profiles")).StatusCode);
        var routes = app.Services.GetRequiredService<EndpointDataSource>().Endpoints.OfType<RouteEndpoint>()
            .Select(e => e.RoutePattern.RawText).ToList();
        Assert.DoesNotContain("/news/all", routes);
        Assert.DoesNotContain("/api/me", routes);
        Assert.DoesNotContain("/feed/more", routes);
        var workers = app.Services.GetServices<IHostedService>();
        Assert.DoesNotContain(workers, w => w is RequestCheckerService or ChannelRolloutService or ScoresSyncService or LeaguePhaseService);
        var manifest = JsonDocument.Parse(await client.GetStringAsync("/manifest.webmanifest")).RootElement;
        Assert.Equal("/mynewspaper-admin", manifest.GetProperty("id").GetString());
        Assert.Equal("/admin", manifest.GetProperty("start_url").GetString());
        Assert.Equal("myNewspaper Admin", manifest.GetProperty("name").GetString());
        foreach (var icon in manifest.GetProperty("icons").EnumerateArray())
            Assert.Equal(HttpStatusCode.OK, (await client.GetAsync(icon.GetProperty("src").GetString())).StatusCode);
    }
    [Fact]
    public async Task Shared_assets_on_the_login_page_serve_real_bytes_including_fingerprinted_urls()
    {
        await using var app = new App();
        using var client = app.CreateClient();
        var html = await client.GetStringAsync("/login");
        var assets = Regex.Matches(html, "(?:href|src)=\"([^\"]+\\.(?:css|js))\"")
            .Select(match => match.Groups[1].Value).Distinct().ToArray();
        Assert.NotEmpty(assets);
        foreach (var asset in assets)
        {
            var response = await client.GetAsync("/" + asset.TrimStart('/'));
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.True((await response.Content.ReadAsByteArrayAsync()).Length > 100, $"Empty static asset: {asset}");
        }
        foreach (var path in new[] { "/theme-init.js", "/public.css", "/icons/newspaper-page-192.png" })
            Assert.True((await client.GetByteArrayAsync(path)).Length > 100, $"Empty static asset: {path}");
    }

}
