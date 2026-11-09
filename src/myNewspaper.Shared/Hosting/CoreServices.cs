using System.Net;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection.Extensions;
using myNewspaper.Shared.Data;
using myNewspaper.Shared.Data.Models;
using myNewspaper.Shared.Profiles;
using myNewspaper.Shared.Requests;
using myNewspaper.Shared.Scores;
using myNewspaper.Shared.Tv;
using myNewspaper.Shared.Watching;

namespace myNewspaper.Shared.Hosting;

/// <summary>
/// How a host describes itself to outside services, in the User-Agent of its request-check and ESPN calls.
/// </summary>
public sealed record CoreHostLabels(string RequestCheck, string Results)
{
    public static readonly CoreHostLabels Public = new("household feed request check", "personal results page");
    public static readonly CoreHostLabels Admin = new("admin request check", "admin results");
}

/// <summary>
/// Services both the public and the admin host register, in one place so the two can't drift apart. Each host's
/// Program.cs adds only what is specific to it (render modes, cookies, data protection, auth, background jobs).
/// </summary>
public static class CoreServices
{
    public static IServiceCollection AddMyNewspaperCore(this IServiceCollection services, IConfiguration config,
        CoreHostLabels labels)
    {
        // Database: short-lived contexts from a factory (Blazor circuits and singletons outlive a request).
        services.AddDbContextFactory<WebScraperContext>(options => options.UseSqlServer(config.GetConnectionString("DefaultConnection")));
        services.AddHealthChecks().AddDbContextCheck<WebScraperContext>();

        // The date shown on each article is the calendar day in one configured time zone (Display:TimeZone).
        services.AddSingleton(sp => FeedDates.Create(sp.GetRequiredService<IConfiguration>()["Display:TimeZone"],
            sp.GetRequiredService<TimeProvider>()));
        services.TryAddSingleton(TimeProvider.System);
        services.AddMemoryCache();

        services.Configure<SiteLinkOptions>(config.GetSection("Sites"));
        services.AddSingleton<SiteLinks>();

        services.Configure<ProfilesOptions>(config.GetSection(ProfilesOptions.Section));
        services.AddSingleton<PasswordHasher<Profile>>();
        services.AddSingleton<ProfilePins>();

        // Request links are fetched through a guarded handler: public addresses only, checked at connect time, no redirects.
        services.AddSingleton<RequestChecks>();
        services.AddHttpClient(RequestChecks.ClientName, client =>
            {
                client.Timeout = TimeSpan.FromSeconds(10);
                client.DefaultRequestHeaders.UserAgent.ParseAdd($"myNewspaper/1.0 ({labels.RequestCheck})");
            })
            .ConfigurePrimaryHttpMessageHandler(SsrfGuard.CreateHandler);

        services.AddSingleton<WatchCatalog>();
        services.AddHttpClient("watch-catalog", client =>
        {
            client.BaseAddress = new Uri("https://api.themoviedb.org/3/");
            client.Timeout = TimeSpan.FromSeconds(10);
        });

        // Game results from ESPN's public scoreboard data. Scheduled syncing is the public host's job; admin syncs on request.
        services.Configure<ScoresOptions>(config.GetSection(ScoresOptions.Section));
        services.AddHttpClient<EspnClient>(client =>
            {
                client.Timeout = TimeSpan.FromSeconds(30);
                client.DefaultRequestHeaders.UserAgent.ParseAdd($"myNewspaper/1.0 ({labels.Results})");
            })
            .ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler { AutomaticDecompression = DecompressionMethods.All });
        services.AddSingleton<LeaguePhases>();
        services.AddTransient<ScoresSync>();

        services.AddHttpClient<TvmazeClient>(client =>
        {
            client.BaseAddress = new Uri("https://api.tvmaze.com/");
            client.Timeout = TimeSpan.FromSeconds(15);
            client.DefaultRequestHeaders.UserAgent.ParseAdd("myNewspaper/1.0");
        });

        // Only explicitly configured reverse proxies may supply the HTTPS scheme and client address.
        services.Configure<ForwardedHeadersOptions>(options =>
        {
            options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
            foreach (var proxy in config.GetSection("ReverseProxy:KnownProxies").GetChildren())
                if (IPAddress.TryParse(proxy.Value, out var address)) options.KnownProxies.Add(address);
        });

        return services;
    }
}
