using System.Collections.Concurrent;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using myNewspaper.Web.Data;
using myNewspaper.Web.Data.Models;

namespace myNewspaper.Web.Scores;

public enum LeaguePhase { Unknown, RegularSeason, Postseason }

/// <summary>Current league-wide phase, maintained in the background, never inferred from standings season type.</summary>
public sealed class LeaguePhases
{
    private readonly ConcurrentDictionary<string, (LeaguePhase Phase, DateTime Checked)> phases = new();
    public void Update(SportsStandingsSource source, LeaguePhase phase, DateTime checkedAt) =>
        phases[source.ProviderKey + "|" + source.Sport] = (phase, checkedAt);

    public bool IsRegular(SportsStandingsSource source, DateTime now) =>
        phases.TryGetValue(source.ProviderKey + "|" + source.Sport, out var entry)
        && entry.Phase == LeaguePhase.RegularSeason && now >= entry.Checked
        && now - entry.Checked <= TimeSpan.FromMinutes(20);

    public static (string Sport, string League)? Competition(SportsStandingsSource source) => source.ProviderKey switch
    {
        "espn-mlb" => ("baseball", "mlb"),
        "espn-wnba" => ("basketball", "wnba"),
        "espn-nhl" => ("hockey", "nhl"),
        "espn-nfl" => ("football", "nfl"),
        "espn-nwsl" => ("soccer", "usa.nwsl"),
        "espn-mls" => ("soccer", "usa.1"),
        "espn-epl" => ("soccer", "eng.1"),
        "espn-championship" => ("soccer", "eng.2"),
        "big12" => source.Sport switch
        {
            "Football" => ("football", "college-football"),
            "Men's Basketball" => ("basketball", "mens-college-basketball"),
            "Women's Basketball" => ("basketball", "womens-college-basketball"),
            "Volleyball" => ("volleyball", "womens-college-volleyball"),
            "Soccer" => ("soccer", "usa.ncaa.w.1"),
            _ => null
        },
        _ => null
    };

    public static LeaguePhase Parse(JsonElement root, string sport, string league)
    {
        if (!root.TryGetProperty("leagues", out var leagues) || leagues.ValueKind != JsonValueKind.Array) return LeaguePhase.Unknown;
        foreach (var item in leagues.EnumerateArray())
        {
            if (!item.TryGetProperty("slug", out var slug) || slug.GetString() != league) continue;
            if (!item.TryGetProperty("season", out var season) || !season.TryGetProperty("type", out var type)
                || type.ValueKind != JsonValueKind.Object) return LeaguePhase.Unknown;
            var name = type.TryGetProperty("name", out var n) && n.ValueKind == JsonValueKind.String ? n.GetString()?.ToLowerInvariant() ?? "" : "";
            if (new[] { "postseason", "post-season", "playoff", "play-off", "knockout", "tournament" }.Any(name.Contains))
                return LeaguePhase.Postseason;
            // Soccer's numeric type is a competition-specific ID. Its stage id=1 denotes league play;
            // basketball/football/baseball/hockey instead use id=2 for regular season, id=3 postseason.
            var id = type.TryGetProperty("id", out var i) ? i.ToString() : "";
            if (sport != "soccer" && id == "3") return LeaguePhase.Postseason;
            return id == (sport == "soccer" ? "1" : "2") ? LeaguePhase.RegularSeason : LeaguePhase.Unknown;
        }
        return LeaguePhase.Unknown;
    }
}

public sealed class LeaguePhaseService(IServiceScopeFactory scopes, LeaguePhases phases, ILogger<LeaguePhaseService> log) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = scopes.CreateScope();
                var factory = scope.ServiceProvider.GetRequiredService<IDbContextFactory<WebScraperContext>>();
                var espn = scope.ServiceProvider.GetRequiredService<EspnClient>();
                await using var db = await factory.CreateDbContextAsync(stoppingToken);
                var sources = await db.SportsStandingsSources.AsNoTracking().Where(s => s.Enabled).ToListAsync(stoppingToken);
                foreach (var source in sources)
                {
                    if (LeaguePhases.Competition(source) is not { } competition) continue;
                    var result = await espn.GetLeaguePhaseAsync(competition.Sport, competition.League, stoppingToken);
                    var phase = result.Ok ? result.Value : LeaguePhase.Unknown;
                    phases.Update(source, phase, DateTime.UtcNow);
                    if (!result.Ok) log.LogWarning("League phase unavailable for {Provider}/{Sport}: {Error}", source.ProviderKey, source.Sport, result.Error);
                    else log.LogInformation("League phase {Provider}/{Sport}: {Phase}", source.ProviderKey, source.Sport, phase);
                    await Task.Delay(TimeSpan.FromMilliseconds(300), stoppingToken);
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { break; }
            catch (Exception ex) { log.LogError(ex, "Could not refresh league phases."); }
            try { await Task.Delay(TimeSpan.FromMinutes(10), stoppingToken); }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { break; }
        }
    }
}
