using Microsoft.AspNetCore.DataProtection;
using myNewspaper.Shared.Data;
using myNewspaper.Shared.Hosting;
using myNewspaper.Shared.Profiles;
using myNewspaper.Shared.Requests;
using myNewspaper.Shared.Saved;
using myNewspaper.Shared.Scores;
using myNewspaper.Shared.Watching;
using myNewspaper.Web.Components;
using myNewspaper.Web.Endpoints;
using myNewspaper.Web.Saved;

var builder = WebApplication.CreateBuilder(args);

// Shared with the admin host: database, clock, display dates, profiles' PIN hashing, site links, the request checker,
// Where to watch, ESPN results, TVmaze and forwarded headers.
builder.Services.AddMyNewspaperCore(builder.Configuration, CoreHostLabels.Public);



builder.Services.AddSingleton<SharedArticles>();
builder.Services.AddAntiforgery(options => options.HeaderName = "X-Saved-CSRF");
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

// Household profiles stay off (Profiles:Enabled = false) until switch-on; see new development/profiles-handoff.md.
builder.Services.AddScoped<ProfileContext>();
builder.Services.AddSingleton<ProfileCookies>();
builder.Services.AddSingleton<RequestBacklog>();
// The request checker fills in details for the admin; the rollout turns new household channels on for everyone.
if (builder.Configuration.GetValue("BackgroundJobs:Enabled", true))
    builder.Services.AddHostedService<RequestCheckerService>();
builder.Services.AddSingleton<ChannelRolloutService>();
if (builder.Configuration.GetValue("BackgroundJobs:Enabled", true))
    builder.Services.AddHostedService(sp => sp.GetRequiredService<ChannelRolloutService>());

// A background sync stores followed leagues' and teams' games from ESPN; pages only read the database.
builder.Services.AddHostedService<LeaguePhaseService>();
if (builder.Configuration.GetValue("BackgroundJobs:Enabled", true) && builder.Configuration.GetValue("Results:BackgroundSyncEnabled", true))
    builder.Services.AddHostedService<ScoresSyncService>();

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
// The commit and build time of the running build, so a deploy can check it (Shared/Hosting/BuildInfo.cs).
app.MapVersion();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();

// Lets the integration tests start the app with WebApplicationFactory.
public partial class Program;
