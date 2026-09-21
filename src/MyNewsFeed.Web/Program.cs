using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MyNewsFeed.Web.Auth;
using MyNewsFeed.Web.Components;
using MyNewsFeed.Web.Components.Feed;
using MyNewsFeed.Web.Data;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddAuthentication(AdminAuth.Scheme)
    .AddCookie(AdminAuth.Scheme, options =>
    {
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
        .SetApplicationName("MyNewsFeed")
        .PersistKeysToFileSystem(new DirectoryInfo(keysPath));
}

// The date shown on each article is the calendar day in one configured time zone (Display:TimeZone).
builder.Services.AddSingleton(sp => FeedDates.Create(sp.GetRequiredService<IConfiguration>()["Display:TimeZone"]));

// Blazor Server circuits are long-lived, so components create short-lived contexts from a factory.
builder.Services.AddDbContextFactory<WebScraperContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

var app = builder.Build();

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
    string? after, string? category, string? q, string? day) =>
{
    if (FeedCursor.Parse(after) is not { } cursor)
    {
        return Results.BadRequest();
    }

    int? categoryId = int.TryParse(category, out var parsedCategory) ? parsedCategory : null;
    await using var db = await factory.CreateDbContextAsync();
    var page = await FeedQuery.GetFeedAsync(db, categoryId, q, cursor, FeedQuery.MoreBatch);

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
        nextHref = page.Next is { } next ? "news" + FeedQuery.QueryString(categoryId, q, next.ToToken()) : null,
        shown = page.Shown,
        total = page.Total,
        added = page.Items.Count,
        lastDay = page.Items.Count > 0 ? dates.DayKey(page.Items[^1]) : day,
    });
});

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
