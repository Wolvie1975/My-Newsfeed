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
public sealed class AdminApp(bool enabled) : WebApplicationFactory<myNewspaper.Admin.AdminProgram>
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

public class AdminHttpTests
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

    private static async Task<HttpResponseMessage> SendJsonAsync(HttpClient client, HttpMethod method, string url, object? body, string? token)
    {
        var request = new HttpRequestMessage(method, url) { Content = body is null ? null : JsonContent.Create(body) };
        if (token is not null) request.Headers.Add("X-Saved-CSRF", token);
        return await client.SendAsync(request);
    }

    [Fact]
    public async Task The_admin_apis_need_the_admin_and_never_show_a_profiles_follows_settings_or_saved_items()
    {
        await using var app = new AdminApp(enabled: true);
        var a = await app.AddProfileAsync();
        var marker = "zzmarker" + Guid.NewGuid().ToString("N")[..8];
        var client = app.Browser();
        try
        {
            await using (var db = app.Db())
            {
                await ProfileStore.PatchSettingsAsync(db, a.Id, new System.Text.Json.Nodes.JsonObject { ["teams"] = new System.Text.Json.Nodes.JsonArray(marker) });
                var feed = await db.YoutubeVideoFeeds.OrderBy(f => f.Id).FirstOrDefaultAsync();
                if (feed is not null && !await db.ProfileChannelFollows.AnyAsync(f => f.ProfileId == a.Id && f.YoutubeVideoFeedId == feed.Id))
                    db.ProfileChannelFollows.Add(new ProfileChannelFollow { ProfileId = a.Id, YoutubeVideoFeedId = feed.Id });
                await db.SaveChangesAsync();
            }

            Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("/admin/api/profiles")).StatusCode);
            Assert.Equal(HttpStatusCode.Unauthorized, (await client.PostAsync($"/admin/api/profiles/{a.Id}/unlock", null)).StatusCode);

            var login = await PostFormAsync(client, "/account/login", new()
            {
                ["password"] = AdminApp.AdminPassword,
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

            // The admin pages render the same limited view: name, color, lock, devices and requests only.
            foreach (var path in new[] { "/admin/profiles", $"/admin/profiles/{a.Id}", "/admin/profiles/base", "/admin/requests?status=all" })
            {
                var response = await client.SendAsync(Page(path));
                Assert.Equal(HttpStatusCode.OK, response.StatusCode);
                var html = await response.Content.ReadAsStringAsync();
                Assert.Contains(path switch { "/admin/profiles/base" => "Base edition", "/admin/requests?status=all" => "Requests", _ => a.Name }, html);
                Assert.DoesNotContain(marker, html);
                Assert.DoesNotContain("Open as", html);
            }

            // Admin can't open a profile without its PIN: the admin cookie gives no profile.
            Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync("/api/me")).StatusCode);
        }
        finally { await app.CleanUpAsync(a); }
    }

}
