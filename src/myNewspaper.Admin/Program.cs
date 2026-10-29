using System.Net;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using myNewspaper.Admin.Components;
using myNewspaper.Hosting;
using myNewspaper.Web.Auth;
using myNewspaper.Web.Data;
using myNewspaper.Web.Data.Models;
using myNewspaper.Web.Endpoints;
using myNewspaper.Web.Profiles;
using myNewspaper.Web.Requests;
using myNewspaper.Web.Scores;
using myNewspaper.Web.Tv;
using myNewspaper.Web.Watching;

namespace myNewspaper.Admin;

public partial class AdminProgram
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);
        builder.Services.AddRazorComponents().AddInteractiveServerComponents();
        builder.Services.AddAntiforgery(options =>
        {
            options.HeaderName = "X-Saved-CSRF";
            options.Cookie.Name = "myNewspaper.Admin.Csrf";
        });
        builder.Services.AddAuthentication(AdminAuth.Scheme).AddCookie(AdminAuth.Scheme, options =>
        {
            options.Cookie.Name = "myNewspaper.Admin.Session";
            options.Cookie.HttpOnly = true;
            options.LoginPath = "/login";
            options.AccessDeniedPath = "/login";
            options.ExpireTimeSpan = TimeSpan.FromHours(8);
            options.Events.OnRedirectToLogin = context => AdminStatus(context, StatusCodes.Status401Unauthorized);
            options.Events.OnRedirectToAccessDenied = context => AdminStatus(context, StatusCodes.Status403Forbidden);
        });
        builder.Services.AddAuthorization();
        builder.Services.AddCascadingAuthenticationState();
        var protection = builder.Services.AddDataProtection().SetApplicationName("myNewspaper.Admin");
        if (builder.Configuration["DataProtection:KeysPath"] is { Length: > 0 } keys)
            protection.PersistKeysToFileSystem(new DirectoryInfo(keys));
        builder.Services.AddDbContextFactory<WebScraperContext>(options =>
            options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));
        builder.Services.AddHealthChecks().AddDbContextCheck<WebScraperContext>();
        builder.Services.AddSingleton(sp => FeedDates.Create(sp.GetRequiredService<IConfiguration>()["Display:TimeZone"]));
        builder.Services.AddSingleton(TimeProvider.System);
        builder.Services.AddMemoryCache();
        builder.Services.Configure<ProfilesOptions>(builder.Configuration.GetSection(ProfilesOptions.Section));
        builder.Services.AddSingleton<PasswordHasher<Profile>>();
        builder.Services.AddSingleton<ProfilePins>();
        builder.Services.Configure<SiteLinkOptions>(builder.Configuration.GetSection("Sites"));
        builder.Services.AddSingleton<SiteLinks>();
        builder.Services.AddSingleton<RequestChecks>();
        builder.Services.AddHttpClient(RequestChecks.ClientName, client =>
        {
            client.Timeout = TimeSpan.FromSeconds(10);
            client.DefaultRequestHeaders.UserAgent.ParseAdd("myNewspaper/1.0 (admin request check)");
        }).ConfigurePrimaryHttpMessageHandler(SsrfGuard.CreateHandler);
        builder.Services.AddSingleton<WatchCatalog>();
        builder.Services.AddHttpClient("watch-catalog", client =>
        {
            client.BaseAddress = new Uri("https://api.themoviedb.org/3/");
            client.Timeout = TimeSpan.FromSeconds(10);
        });
        builder.Services.Configure<ScoresOptions>(builder.Configuration.GetSection(ScoresOptions.Section));
        builder.Services.AddHttpClient<EspnClient>(client =>
        {
            client.Timeout = TimeSpan.FromSeconds(30);
            client.DefaultRequestHeaders.UserAgent.ParseAdd("myNewspaper/1.0 (admin results)");
        }).ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler { AutomaticDecompression = DecompressionMethods.All });
        builder.Services.AddSingleton<LeaguePhases>();
        builder.Services.AddTransient<ScoresSync>();
        builder.Services.AddHttpClient<TvmazeClient>(client =>
        {
            client.BaseAddress = new Uri("https://api.tvmaze.com/");
            client.Timeout = TimeSpan.FromSeconds(15);
            client.DefaultRequestHeaders.UserAgent.ParseAdd("myNewspaper/1.0");
        });
        // Only explicit admin actions use these services; scheduled workers remain in the public host.
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
        app.Use(async (http, next) =>
        {
            if (!Path.HasExtension(http.Request.Path)) http.Response.Headers.CacheControl = "no-store";
            await next();
        });
        app.UseAuthentication();
        app.UseAuthorization();
        app.UseAntiforgery();
        app.MapStaticAssets();
        app.MapAccount();
        app.MapAdminProfiles();
        app.MapGet("/", () => Results.LocalRedirect("/admin"));
        app.MapHealthChecks("/healthz");
        app.MapRazorComponents<App>().AddInteractiveServerRenderMode();
        app.Run();
    }

    private static Task AdminStatus(Microsoft.AspNetCore.Authentication.RedirectContext<Microsoft.AspNetCore.Authentication.Cookies.CookieAuthenticationOptions> context, int status)
    {
        if (context.Request.Path.StartsWithSegments("/admin/api"))
        {
            context.HttpContext.Features.Get<Microsoft.AspNetCore.Diagnostics.IStatusCodePagesFeature>()?.Enabled = false;
            context.Response.StatusCode = status;
        }
        else context.Response.Redirect(context.RedirectUri);
        return Task.CompletedTask;
    }
}
