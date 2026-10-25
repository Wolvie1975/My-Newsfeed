using System.Net;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.EntityFrameworkCore;
using myNewspaper.Web.Auth;
using myNewspaper.Web.Components;
using myNewspaper.Web.Data;
using myNewspaper.Web.Endpoints;
using myNewspaper.Web.Saved;
using myNewspaper.Web.Scores;
using myNewspaper.Web.Tv;
using myNewspaper.Web.Watching;

var builder = WebApplication.CreateBuilder(args);

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

builder.Services.AddAuthentication(AdminAuth.Scheme)
    .AddCookie(AdminAuth.Scheme, options =>
    {
        // Retain the deployed cookie name so the rebrand does not sign users out.
        options.Cookie.Name = "MyNewsFeed.Admin";
        options.Cookie.HttpOnly = true;
        options.LoginPath = "/login";
        options.AccessDeniedPath = "/login";
        options.ExpireTimeSpan = TimeSpan.FromHours(8);
    });
builder.Services.AddAuthorization();
builder.Services.AddCascadingAuthenticationState();

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
if (builder.Configuration.GetValue("Results:BackgroundSyncEnabled", true))
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

app.UseAuthentication();
app.UseAuthorization();
app.UseAntiforgery();

app.MapStaticAssets();
app.MapSharedArticles();
app.MapAccount();
app.MapPublicEndpoints();
app.MapFeed();

// Used by the container health check: healthy only when the database is reachable.
app.MapHealthChecks("/healthz");

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
