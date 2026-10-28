using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.StaticAssets;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using myNewspaper.Web.Data;
using myNewspaper.Web.Data.Models;
using myNewspaper.Web.Profiles;

namespace myNewspaper.Tests;

/// <summary>Starts the real app against the test database with profiles switched on or off.</summary>
public sealed class ProfileApp(bool enabled) : WebApplicationFactory<Program>
{
    public const string AdminPassword = "profiles-test-admin";

    /// <summary>Every device these tests create carries this user agent, so cleanup can find it.</summary>
    public string UserAgent { get; } = "myNewspaper-tests/" + Guid.NewGuid().ToString("N");

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseSetting("ConnectionStrings:DefaultConnection", TestDatabase.ConnectionString);
        builder.UseSetting("Profiles:Enabled", enabled ? "true" : "false");
        builder.UseSetting("Admin:Password", AdminPassword);
        builder.UseSetting("Results:BackgroundSyncEnabled", "false");
    }

    public HttpClient Browser()
    {
        var client = CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false, HandleCookies = true });
        client.DefaultRequestHeaders.UserAgent.ParseAdd(UserAgent);
        return client;
    }

    public WebScraperContext Db() => Services.GetRequiredService<IDbContextFactory<WebScraperContext>>().CreateDbContext();

    public async Task<Profile> AddProfileAsync(string pin = "1234")
    {
        await using var db = Db();
        return await ProfileStore.CreateAsync(db, new ProfilePins(new PasswordHasher<Profile>()), "zz " + Guid.NewGuid().ToString("N")[..10], "#0051BA", pin, null);
    }

    public async Task CleanUpAsync(params Profile[] profiles)
    {
        await using var db = Db();
        var ids = profiles.Select(p => p.Id).ToList();
        await db.Profiles.Where(p => ids.Contains(p.Id)).ExecuteDeleteAsync();
        await db.ProfileDevices.Where(d => d.UserAgent == UserAgent).ExecuteDeleteAsync();
    }
}

public class ProfileHttpTests
{
    private static async Task<string> TokenFromLoginPageAsync(HttpClient client)
    {
        var html = await (await client.GetAsync("/login")).Content.ReadAsStringAsync();
        return Regex.Match(html, @"name=""__RequestVerificationToken""[^>]*value=""([^""]+)""").Groups[1].Value;
    }

    private static HttpRequestMessage Page(string url) => new(HttpMethod.Get, url) { Headers = { { "Accept", "text/html,application/xhtml+xml" } } };

    private static async Task<HttpResponseMessage> PostFormAsync(HttpClient client, string url, Dictionary<string, string> form, bool json = false)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, url) { Content = new FormUrlEncodedContent(form) };
        if (json) request.Headers.Add("Accept", "application/json");
        return await client.SendAsync(request);
    }

    private static async Task<HttpResponseMessage> OpenAsync(HttpClient client, Profile profile, string pin, bool json = true) =>
        await PostFormAsync(client, $"/profiles/{profile.Id}/open",
            new() { ["pin"] = pin, ["returnUrl"] = "/tv", ["__RequestVerificationToken"] = await TokenFromLoginPageAsync(client) }, json);

    private static async Task<HttpResponseMessage> SendJsonAsync(HttpClient client, HttpMethod method, string url, object? body, string? token)
    {
        var request = new HttpRequestMessage(method, url) { Content = body is null ? null : JsonContent.Create(body) };
        if (token is not null) request.Headers.Add("X-Saved-CSRF", token);
        return await client.SendAsync(request);
    }

    [Fact]
    public async Task Without_a_profile_every_public_route_sends_you_to_the_picker()
    {
        await using var app = new ProfileApp(enabled: true);
        var client = app.Browser();
        try
        {
            // Built from the endpoint data source, so new routes are covered automatically (Phase 7).
            var endpoints = app.Services.GetRequiredService<EndpointDataSource>().Endpoints.OfType<RouteEndpoint>()
                .Where(e => e.Metadata.GetMetadata<StaticAssetDescriptor>() is null)
                .ToList();
            var routes = endpoints
                .Select(e => (Path: "/" + Regex.Replace(e.RoutePattern.RawText!.Trim('/'), @"\{[^}]+\}", "1"),
                    Methods: e.Metadata.GetMetadata<HttpMethodMetadata>()?.HttpMethods ?? ["GET"]))
                .Where(r => !ProfileGate.IsExempt(r.Path) && !ProfileGate.IsPassThrough(r.Path))
                .Distinct().ToList();
            Assert.Contains(routes, r => r.Path == "/api/saved");
            Assert.Contains(routes, r => r.Path == "/feed/more");
            Assert.Contains(routes, r => r.Path == "/");

            foreach (var (path, methods) in routes)
            {
                if (methods.Contains("GET"))
                {
                    var page = await client.SendAsync(Page(path));
                    Assert.True(page.StatusCode == HttpStatusCode.Redirect, $"GET {path} page: {page.StatusCode}");
                    Assert.StartsWith("/editions?returnUrl=", page.Headers.Location!.OriginalString);
                }
                foreach (var method in methods)
                {
                    var api = await client.SendAsync(new HttpRequestMessage(new HttpMethod(method), path) { Headers = { { "Accept", "application/json" } } });
                    Assert.True(api.StatusCode == HttpStatusCode.Unauthorized, $"{method} {path}: {api.StatusCode}");
                    Assert.Equal("/editions", (await api.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("gate").GetString());
                }
            }
        }
        finally { await app.CleanUpAsync(); }
    }

    [Fact]
    public async Task Admin_sign_in_health_and_static_files_are_never_sent_to_the_picker()
    {
        await using var app = new ProfileApp(enabled: true);
        var client = app.Browser();
        try
        {
            Assert.Equal(HttpStatusCode.OK, (await client.SendAsync(Page("/login"))).StatusCode);
            var admin = await client.SendAsync(Page("/admin"));
            Assert.Equal(HttpStatusCode.Redirect, admin.StatusCode);
            Assert.StartsWith("http://localhost/login", admin.Headers.Location!.ToString());
            Assert.NotEqual(HttpStatusCode.Redirect, (await client.GetAsync("/healthz")).StatusCode);
            Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/manifest.webmanifest")).StatusCode);
            Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/theme-init.js")).StatusCode);
        }
        finally { await app.CleanUpAsync(); }
    }

    [Fact]
    public async Task Api_calls_and_probes_without_a_cookie_create_no_device()
    {
        await using var app = new ProfileApp(enabled: true);
        var client = app.Browser();
        try
        {
            foreach (var path in new[] { "/api/saved", "/feed/more", "/healthz", "/local/weather" })
            {
                var response = await client.GetAsync(path);
                Assert.False(response.Headers.TryGetValues("Set-Cookie", out var cookies) && cookies.Any(c => c.StartsWith(ProfileCookies.DeviceCookie)), path);
            }
            await using var db = app.Db();
            Assert.False(await db.ProfileDevices.AnyAsync(d => d.UserAgent == app.UserAgent));
        }
        finally { await app.CleanUpAsync(); }
    }

    [Fact]
    public async Task With_profiles_off_the_profile_routes_do_not_exist_and_nothing_is_gated()
    {
        await using var app = new ProfileApp(enabled: false);
        var client = app.Browser();
        Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync("/api/me")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await client.PostAsync("/profiles/1/open", new FormUrlEncodedContent([]))).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await client.PostAsync("/api/me/requests", JsonContent.Create(new { }))).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/api/saved")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await client.SendAsync(Page("/"))).StatusCode);
        // Admin APIs work either way, behind the admin sign-in.
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("/admin/api/profiles")).StatusCode);
    }

    [Fact]
    public async Task Opening_a_profile_checks_the_pin_and_antiforgery_and_gives_it_its_own_saved_list()
    {
        await using var app = new ProfileApp(enabled: true);
        var a = await app.AddProfileAsync("1234");
        var b = await app.AddProfileAsync("4321");
        var alex = app.Browser();
        var blair = app.Browser();
        try
        {
            Assert.Equal(HttpStatusCode.Redirect, (await alex.SendAsync(Page("/"))).StatusCode);   // device cookie set here

            // Antiforgery: a form post without the token is refused before any PIN is checked.
            var noToken = await PostFormAsync(alex, $"/profiles/{a.Id}/open", new() { ["pin"] = "1234" }, json: true);
            Assert.Equal(HttpStatusCode.BadRequest, noToken.StatusCode);

            var wrong = await OpenAsync(alex, a, "0000");
            Assert.Equal(HttpStatusCode.Unauthorized, wrong.StatusCode);
            Assert.Equal(4, (await wrong.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("triesLeft").GetInt32());
            var formWrong = await OpenAsync(alex, a, "0000", json: false);
            Assert.Equal($"/editions?profile={a.Id}&error=wrong-pin&triesLeft=3&returnUrl=%2Ftv", formWrong.Headers.Location!.OriginalString);
            var opened = await OpenAsync(alex, a, "1234", json: false);
            Assert.Equal("/tv", opened.Headers.Location!.OriginalString);

            var me = await (await alex.GetAsync("/api/me")).Content.ReadFromJsonAsync<JsonElement>();
            Assert.Equal(a.Name, me.GetProperty("name").GetString());
            var token = me.GetProperty("token").GetString();

            Assert.Equal(HttpStatusCode.BadRequest, (await SendJsonAsync(alex, HttpMethod.Put, "/api/me/settings", new { theme = "dark" }, null)).StatusCode);
            Assert.Equal(HttpStatusCode.OK, (await SendJsonAsync(alex, HttpMethod.Put, "/api/me/settings", new { theme = "dark" }, token)).StatusCode);
            Assert.Equal(HttpStatusCode.BadRequest, (await SendJsonAsync(alex, HttpMethod.Put, "/api/me/settings", new { theme = "neon" }, token)).StatusCode);
            Assert.Equal(HttpStatusCode.OK, (await SendJsonAsync(alex, HttpMethod.Post, "/api/saved/add",
                new { url = "https://example.com/profile-test", title = "Alex's" }, token)).StatusCode);

            await blair.SendAsync(Page("/"));
            Assert.Equal(HttpStatusCode.OK, (await OpenAsync(blair, b, "4321")).StatusCode);
            var blairSaved = await (await blair.GetAsync("/api/saved")).Content.ReadFromJsonAsync<JsonElement>();
            Assert.Empty(blairSaved.GetProperty("articles").EnumerateArray());
            var alexSaved = await (await alex.GetAsync("/api/saved")).Content.ReadFromJsonAsync<JsonElement>();
            Assert.Single(alexSaved.GetProperty("articles").EnumerateArray());
            var blairMe = await (await blair.GetAsync("/api/me")).Content.ReadFromJsonAsync<JsonElement>();
            Assert.Empty(blairMe.GetProperty("settings").EnumerateObject());

            // Sign out: the picker is back.
            var signOut = await PostFormAsync(alex, "/profiles/signout", new() { ["__RequestVerificationToken"] = await TokenFromLoginPageAsync(alex) });
            Assert.Equal("/editions", signOut.Headers.Location!.OriginalString);
            Assert.Equal(HttpStatusCode.Unauthorized, (await alex.GetAsync("/api/me")).StatusCode);
        }
        finally { await app.CleanUpAsync(a, b); }
    }

    [Fact]
    public async Task The_admin_apis_need_the_admin_and_never_show_a_profiles_follows_settings_or_saved_items()
    {
        await using var app = new ProfileApp(enabled: true);
        var a = await app.AddProfileAsync();
        var marker = "zzmarker" + Guid.NewGuid().ToString("N")[..8];
        var client = app.Browser();
        try
        {
            await using (var db = app.Db())
            {
                await ProfileStore.PatchSettingsAsync(db, a.Id, new System.Text.Json.Nodes.JsonObject { ["teams"] = new System.Text.Json.Nodes.JsonArray(marker) });
                var feed = await db.YoutubeVideoFeeds.OrderBy(f => f.Id).FirstOrDefaultAsync();
                if (feed is not null) db.ProfileChannelFollows.Add(new ProfileChannelFollow { ProfileId = a.Id, YoutubeVideoFeedId = feed.Id });
                await db.SaveChangesAsync();
            }

            Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("/admin/api/profiles")).StatusCode);
            Assert.Equal(HttpStatusCode.Unauthorized, (await client.PostAsync($"/admin/api/profiles/{a.Id}/unlock", null)).StatusCode);

            var login = await PostFormAsync(client, "/account/login", new()
            {
                ["password"] = ProfileApp.AdminPassword,
                ["returnUrl"] = "/admin",
                ["__RequestVerificationToken"] = await TokenFromLoginPageAsync(client),
            });
            Assert.Equal("/admin", login.Headers.Location!.OriginalString);
            var token = (await (await client.GetAsync("/admin/api/token")).Content.ReadFromJsonAsync<JsonElement>()).GetProperty("token").GetString();
            Assert.Equal(HttpStatusCode.BadRequest, (await client.PostAsync($"/admin/api/profiles/{a.Id}/unlock", null)).StatusCode);
            Assert.Equal(HttpStatusCode.OK, (await SendJsonAsync(client, HttpMethod.Post, $"/admin/api/profiles/{a.Id}/unlock", null, token)).StatusCode);

            foreach (var path in new[] { "/admin/api/profiles", $"/admin/api/profiles/{a.Id}/devices", "/admin/api/household",
                         "/admin/api/household/base-edition", "/admin/api/requests", $"/admin/api/requests?profileId={a.Id}", "/admin/api/requests/summary" })
            {
                var response = await client.GetAsync(path);
                Assert.Equal(HttpStatusCode.OK, response.StatusCode);
                var body = await response.Content.ReadAsStringAsync();
                Assert.DoesNotContain(marker, body);
                Assert.DoesNotContain("lastReadAt", body, StringComparison.OrdinalIgnoreCase);
                Assert.DoesNotContain("lastSeenAt", body, StringComparison.OrdinalIgnoreCase);
                Assert.DoesNotContain("YoutubeVideoFeedId", body, StringComparison.OrdinalIgnoreCase);
                Assert.DoesNotContain("TvTrackedSeriesId", body, StringComparison.OrdinalIgnoreCase);
            }

            // Admin can't open a profile without its PIN: the admin cookie gives no profile.
            Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("/api/me")).StatusCode);
        }
        finally { await app.CleanUpAsync(a); }
    }
}
