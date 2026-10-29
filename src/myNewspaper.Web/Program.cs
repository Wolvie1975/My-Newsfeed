using System.Net;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection.Extensions;
using myNewspaper.Web.Auth;
using myNewspaper.Web.Components;
using myNewspaper.Web.Data;
using myNewspaper.Web.Data.Models;
using myNewspaper.Web.Endpoints;
using myNewspaper.Web.Profiles;
using myNewspaper.Web.Requests;
using myNewspaper.Web.Saved;
using myNewspaper.Web.Scores;
using myNewspaper.Web.Tv;
using myNewspaper.Web.Watching;

var builder = WebApplication.CreateBuilder(args);
builder.Services.Configure<myNewspaper.Hosting.SiteLinkOptions>(builder.Configuration.GetSection("Sites"));
builder.Services.AddSingleton<myNewspaper.Hosting.SiteLinks>();

builder.Services.AddScoped<BoxScoresRepository>();
builder.Services.AddScoped<ScoreboardRepository>();
builder.Services.AddSingleton<SharedArticles>();
builder.Services.AddAntiforgery(options => options.HeaderName = "X-Saved-CSRF");
builder.Services.AddSingleton<WatchCatalog>();
builder.Services.AddHttpClient("watch-catalog", client =>
{
    client.BaseAddress = new Uri("https://api.themoviedb.org/3/");
    client.Timeout = TimeSpan.FromSeconds(10);
});
builder.Services.AddMemoryCache();
builder.Services.AddSingleton<LocalWeather>();
builder.Services.AddHttpClient("local-weather", client =>
{
    client.Timeout = TimeSpan.FromSeconds(8);
    client.DefaultRequestHeaders.UserAgent.ParseAdd("myNewspaper/1.0 (+https://www.weather.gov/documentation/services-web-api)");
    client.DefaultRequestHeaders.Accept.ParseAdd("application/geo+json");
});

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

// In a container the default key ring lives in the container's own filesystem and is lost on every rebuild,
// which would sign everyone out. DataProtection:KeysPath points it at a persistent volume instead.
var keysPath = builder.Configuration["DataProtection:KeysPath"];
if (!string.IsNullOrEmpty(keysPath))
{
    builder.Services.AddDataProtection()
        // Keep the existing data-protection discriminator for cookie compatibility.
        .SetApplicationName("MyNewsFeed")
        .PersistKeysToFileSystem(new DirectoryInfo(keysPath));
}

// The date shown on each article is the calendar day in one configured time zone (Display:TimeZone).
builder.Services.AddSingleton(sp => FeedDates.Create(sp.GetRequiredService<IConfiguration>()["Display:TimeZone"]));

// Blazor Server circuits are long-lived, so components create short-lived contexts from a factory.
builder.Services.AddDbContextFactory<WebScraperContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));
builder.Services.AddHealthChecks().AddDbContextCheck<WebScraperContext>();

// Household profiles stay off (Profiles:Enabled = false) until switch-on; see new development/profiles-handoff.md.
builder.Services.Configure<ProfilesOptions>(builder.Configuration.GetSection(ProfilesOptions.Section));
builder.Services.TryAddSingleton(TimeProvider.System);
builder.Services.AddScoped<ProfileContext>();
builder.Services.AddSingleton<ProfileCookies>();
builder.Services.AddSingleton<PasswordHasher<Profile>>();
builder.Services.AddSingleton<ProfilePins>();
// Request links are fetched through a guarded handler: public addresses only, checked at connect time, no redirects.
builder.Services.AddHttpClient(RequestChecks.ClientName, client =>
    {
        client.Timeout = TimeSpan.FromSeconds(10);
        client.DefaultRequestHeaders.UserAgent.ParseAdd("myNewspaper/1.0 (household feed request check)");
    })
    .ConfigurePrimaryHttpMessageHandler(SsrfGuard.CreateHandler);
builder.Services.AddSingleton<RequestChecks>();
builder.Services.AddSingleton<RequestBacklog>();
// The request checker fills in details for the admin; the rollout turns new household channels on for everyone.
if (builder.Configuration.GetValue("BackgroundJobs:Enabled", true))
    builder.Services.AddHostedService<RequestCheckerService>();
builder.Services.AddSingleton<ChannelRolloutService>();
if (builder.Configuration.GetValue("BackgroundJobs:Enabled", true))
    builder.Services.AddHostedService(sp => sp.GetRequiredService<ChannelRolloutService>());

// Game results from ESPN's public scoreboard data: a background sync stores followed leagues' and teams' games; pages
// only read the database.
builder.Services.Configure<ScoresOptions>(builder.Configuration.GetSection(ScoresOptions.Section));
builder.Services.AddHttpClient<EspnClient>(http =>
    {
        http.Timeout = TimeSpan.FromSeconds(30);
        http.DefaultRequestHeaders.UserAgent.ParseAdd("myNewspaper/1.0 (personal results page)");
    })
    .ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler { AutomaticDecompression = DecompressionMethods.All });
builder.Services.AddSingleton<LeaguePhases>();
builder.Services.AddHostedService<LeaguePhaseService>();
builder.Services.AddTransient<ScoresSync>();
if (builder.Configuration.GetValue("BackgroundJobs:Enabled", true) && builder.Configuration.GetValue("Results:BackgroundSyncEnabled", true))
    builder.Services.AddHostedService<ScoresSyncService>();

builder.Services.AddHttpClient<TvmazeClient>(http =>
{
    http.BaseAddress = new Uri("https://api.tvmaze.com/");
    http.Timeout = TimeSpan.FromSeconds(15);
    http.DefaultRequestHeaders.UserAgent.ParseAdd("myNewspaper/1.0");
});

// Only explicitly configured reverse proxies may supply the HTTPS scheme.
builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
    foreach (var proxy in builder.Configuration.GetSection("ReverseProxy:KnownProxies").GetChildren())
        if (IPAddress.TryParse(proxy.Value, out var address)) options.KnownProxies.Add(address);
});

var app = builder.Build();
app.UseForwardedHeaders();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}
app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
app.UseHttpsRedirection();

// Old Headlines and Local addresses (bookmarks, the installed app, Saved links) move under News, query intact.
app.Use(async (context, next) =>
{
    var path = context.Request.Path.Value?.TrimEnd('/') ?? "";
    var to = path.Equals("/local", StringComparison.OrdinalIgnoreCase) ? "/news/kansas-city"
        // /news with a list query is the old Headlines list.
        : path.Equals("/news", StringComparison.OrdinalIgnoreCase)
          && context.Request.Query.Keys.Any(k => k is "category" or "source" or "q" or "after") ? "/news/all"
        : null;
    if (to is null) { await next(); return; }
    context.Response.Redirect(context.Request.PathBase + to + context.Request.QueryString, permanent: true);
});

// After routing (which WebApplication adds first) and authentication, so it can read endpoint metadata.
app.UseProfileGate();
app.UseAntiforgery();

app.MapStaticAssets();
app.MapSharedArticles();
app.MapPublicEndpoints();
app.MapProfiles();
app.MapMe();
app.MapFeed();

// Used by the container health check: healthy only when the database is reachable.
app.MapHealthChecks("/healthz");

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();

// Lets the integration tests start the app with WebApplicationFactory.
public partial class Program;
