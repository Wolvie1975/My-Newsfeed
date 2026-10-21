using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.HttpOverrides;
using System.Net;
using Microsoft.EntityFrameworkCore;
using MyDailyFeed.Web.Auth;
using MyDailyFeed.Web.Components;
using MyDailyFeed.Web.Components.Feed;
using MyDailyFeed.Web.Data;
using MyDailyFeed.Web.Scores;
using MyDailyFeed.Web.Saved;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddScoped<BoxScoresRepository>();
builder.Services.AddSingleton<SharedArticles>();
builder.Services.AddAntiforgery(options => options.HeaderName = "X-Saved-CSRF");
builder.Services.AddSingleton<MyDailyFeed.Web.Watching.WatchCatalog>();
builder.Services.AddHttpClient("watch-catalog",client=>{client.BaseAddress=new Uri("https://api.themoviedb.org/3/");client.Timeout=TimeSpan.FromSeconds(10);});
builder.Services.AddMemoryCache();
builder.Services.AddHttpClient("ranking-conferences", client => client.Timeout = TimeSpan.FromSeconds(5));
builder.Services.AddSingleton<MyDailyFeed.Web.Data.RankingConferences>();
builder.Services.AddSingleton<LocalWeather>();
builder.Services.AddHttpClient("local-weather", client => {
    client.Timeout = TimeSpan.FromSeconds(8);
    client.DefaultRequestHeaders.UserAgent.ParseAdd("MyDailyFeed/1.0 (+https://www.weather.gov/documentation/services-web-api)");
    client.DefaultRequestHeaders.Accept.ParseAdd("application/geo+json");
});

// Add services to the container.
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

// Game results from ESPN's public scoreboard data: a background sync stores followed leagues' and teams' games; pages
// only read the database.
builder.Services.Configure<ScoresOptions>(builder.Configuration.GetSection(ScoresOptions.Section));
builder.Services.AddHttpClient<EspnClient>(http =>
    {
        http.Timeout = TimeSpan.FromSeconds(30);
        http.DefaultRequestHeaders.UserAgent.ParseAdd("MyDailyFeed/1.0 (personal results page)");
    })
    .ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler { AutomaticDecompression = System.Net.DecompressionMethods.All });
builder.Services.AddSingleton<LeaguePhases>();
builder.Services.AddHostedService<LeaguePhaseService>();
builder.Services.AddTransient<ScoresSync>();
if (builder.Configuration.GetValue("Results:BackgroundSyncEnabled", true))
    builder.Services.AddHostedService<ScoresSyncService>();

builder.Services.AddHttpClient<MyDailyFeed.Web.Tv.TvmazeClient>(http =>
{
    http.BaseAddress = new Uri("https://api.tvmaze.com/");
    http.Timeout = TimeSpan.FromSeconds(15);
    http.DefaultRequestHeaders.UserAgent.ParseAdd("MyDailyFeed/1.0");
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

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}
app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();
app.UseAntiforgery();

app.MapStaticAssets();
app.MapSharedArticles();

// Form posts from the static login page and the nav-menu logout button (antiforgery is validated for [FromForm] endpoints).
app.MapPost("/account/login", async (HttpContext http, IConfiguration config,
    [FromForm] string? password, [FromForm] string? returnUrl) =>
{
    var target = AdminAuth.SafeReturnUrl(returnUrl);
    if (!AdminAuth.PasswordMatches(config["Admin:Password"], password))
    {
        return Results.LocalRedirect($"/login?error=1&returnUrl={Uri.EscapeDataString(target)}");
    }

    var identity = new ClaimsIdentity([new Claim(ClaimTypes.Name, "admin")], AdminAuth.Scheme);
    await http.SignInAsync(AdminAuth.Scheme, new ClaimsPrincipal(identity));
    return Results.LocalRedirect(target);
});

app.MapPost("/account/logout", async (HttpContext http) =>
{
    await http.SignOutAsync(AdminAuth.Scheme);
    return Results.LocalRedirect("/login");
});

// Used by the container health check: healthy only when the database is reachable.
app.MapGet("/healthz", async (IDbContextFactory<WebScraperContext> factory) =>
{
    await using var db = await factory.CreateDbContextAsync();
    return await db.Database.CanConnectAsync() ? Results.Ok("ok") : Results.StatusCode(StatusCodes.Status503ServiceUnavailable);
});

// "Load more" on the public feed: the next batch of article cards, rendered by the same component as the page.
app.MapGet("/feed/more", async (HttpContext http, IDbContextFactory<WebScraperContext> factory, ILoggerFactory loggers,
    string? after, string? category, string? q, string? day, string? source, string? view, string? sport) =>
{
    if (FeedCursor.Parse(after) is not { } cursor)
    {
        return Results.BadRequest();
    }

    int? categoryId = int.TryParse(category, out var parsedCategory) ? parsedCategory : null;
    await using var db = await factory.CreateDbContextAsync();
    var boxOffice = view == "box-office";
    var f1News = view == "f1-news";
    var big12News = view == "big12-news";
    if (big12News)
    {
        categoryId = await Big12NewsQuery.CategoryIdAsync(db);
        if (categoryId is null) return Results.NotFound();
    }
    if (f1News)
    {
        categoryId = await F1NewsQuery.CategoryIdAsync(db);
        if (categoryId is null) return Results.NotFound();
    }
    if (boxOffice)
    {
        categoryId = await BoxOfficeQuery.CategoryIdAsync(db);
        if (categoryId is null) return Results.NotFound();
        q = null;
    }
    int? sourceId = int.TryParse(source, out var parsedSource) && parsedSource > 0 ? parsedSource : null;
    if (sourceId is not null && !await db.Sources.AnyAsync(s => s.Id == sourceId && s.Enabled
        && (categoryId == null || s.SourceCategoryId == categoryId))) sourceId = null;
    var page = await FeedQuery.GetFeedAsync(db, categoryId, q, cursor, FeedQuery.MoreBatch, sourceId);

    await using var renderer = new HtmlRenderer(http.RequestServices, loggers);
    var html = await renderer.Dispatcher.InvokeAsync(async () =>
    {
        var output = await renderer.RenderComponentAsync<FeedArticles>(ParameterView.FromDictionary(
            new Dictionary<string, object?>
            {
                [nameof(FeedArticles.Items)] = page.Items,
                [nameof(FeedArticles.PreviousDay)] = day,
            }));
        return output.ToHtmlString();
    });

    var dates = http.RequestServices.GetRequiredService<FeedDates>();
    http.Response.Headers.CacheControl = "no-store";
    return Results.Json(new
    {
        html,
        nextHref = page.Next is { } next ? big12News ? CollegeHub.Href("news", CollegeHub.Resolve(sport)) + "&" + FeedQuery.QueryString(null, q, next.ToToken(), sourceId).TrimStart('?') : (boxOffice ? "box-office" : f1News ? "f1/news" : "news") + FeedQuery.QueryString(f1News ? null : categoryId, q, next.ToToken(), sourceId, allCategories: !boxOffice && !f1News && categoryId is null) : null,
        shown = page.Shown,
        total = page.Total,
        added = page.Items.Count,
        lastDay = page.Items.Count > 0 ? dates.DayKey(page.Items[^1]) : day,
    });
});

app.MapGet("/local/weather", async (LocalWeather weather, HttpContext context) =>
{
    context.Response.Headers.CacheControl = "public,max-age=120";
    return Results.Json(await weather.GetAsync(context.RequestAborted));
});

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
