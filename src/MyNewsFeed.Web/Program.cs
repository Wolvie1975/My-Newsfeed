using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MyNewsFeed.Web.Auth;
using MyNewsFeed.Web.Components;
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

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
