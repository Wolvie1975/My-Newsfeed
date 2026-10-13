# NFL standings — frontend handoff

## Deployed data

NFL standings were deployed October 13, 2026. Production source ID is **10**.
The first import stored **32 teams for 2026**, with 16 AFC and 16 NFC teams.
The existing hourly scraper refreshes this source automatically.

| Setting | Value |
| --- | --- |
| ProviderKey | `espn-nfl` |
| Sport | `Football` |
| DisplayName | `NFL` |
| MinimumTeams | `32` |
| Enabled | `1` |
| Provider URL | `https://site.api.espn.com/apis/v2/sports/football/nfl/standings` |
| Existing frontend route | `/standings?sourceId=10` |

Resolve the source by ProviderKey when configuring another environment; its
numeric ID may differ. The source follows ESPN's published current season.
Display stored Season/Title instead of substituting the calendar year.

Use the existing contract in [the standings frontend guide](my-newsfeed-standings-frontend.md).
No database migration or new read permissions are needed for an application that
already reads SportsStandingsSources and SportsStandings.

## Frontend changes

1. Include NFL in the Standings source selector. Read enabled sources dynamically
   rather than keeping a fixed list of supported leagues.
2. Render two conference tables using the stored GroupName: American Football
   Conference and National Football Conference. Each contains 16 teams.
3. Use the compact NFL columns below instead of the generic list of every statistic.
4. Preserve source selection in the URL, season labels, refresh status and the
   existing last-good, empty and error states.

The deployed My-Newsfeed page already discovers NFL and renders its generic
statistic columns. The recommended adjustment is a compact NFL column mapping;
no web-app rebuild was required for initial availability. NFL standings are
independent of schedule coverage and must include teams without upcoming games.

## Queries and model

Source selector:

```sql
SELECT ID, ProviderKey, Sport, DisplayName, LatestSeason,
       LastSuccessAt, LastError, LastTeamCount
FROM dbo.SportsStandingsSources
WHERE Enabled = 1
ORDER BY DisplayName, ID;
```

Current selected-source standings (parameterize @SourceId):

```sql
SELECT r.*
FROM dbo.SportsStandings r
JOIN dbo.SportsStandingsSources s ON s.ID = r.SourceId
WHERE s.Enabled = 1 AND r.IsCurrent = 1 AND r.SourceId = @SourceId
ORDER BY r.GroupName, r.Position, r.TeamName;
```

Group rows by GroupId and display GroupName. Use TeamName, LogoUrl and TeamUrl
from the row; ESPN team IDs are scoped to the provider. Keep Position as provider
order within a conference. Do not infer a league-wide rank or playoff qualification.
ESPN currently supplies conference groups, not eight separate division tables.
Division records are retained as statistics; they do not identify division groups.

## NFL columns

StatsJson is an array of statistics. Match the case-sensitive `name` and render
`displayValue`. Ignore additional properties and show an em dash for unavailable
values. Do not calculate missing records, percentages or point differentials.

| Column | Statistic name |
| --- | --- |
| W | `wins` |
| L | `losses` |
| T | `ties` |
| Pct | `winPercent` |
| PF | `pointsFor` |
| PA | `pointsAgainst` |
| Diff | `pointDifferential` |
| Streak | `streak` |

Optional columns: Home=`Home`, Away=`Road`, Division=`divisionRecord`.
Start with the Team column and its optional logo. Preserve supplied formatting
for percentages, streaks, signs and record strings.

For the existing My-Newsfeed code, add this branch before the generic fallback in
`src/MyNewsFeed.Web/Data/StandingsQuery.cs`, inside `Columns`:

```csharp
if (provider == "espn-nfl")
    return [new("wins", "W"), new("losses", "L"), new("ties", "T"),
        new("winPercent", "Pct"), new("pointsFor", "PF"),
        new("pointsAgainst", "PA"), new("pointDifferential", "Diff"),
        new("streak", "Streak")];
```

The existing `StandingColumn` and `StatisticValue` helpers support this mapping.
Keep the table usable on mobile with horizontal scrolling and accessible headers.

## Refresh states

Use LastSuccessAt for the last successful refresh and convert UTC timestamps to
America/Chicago for display. If LastError is populated, retain the last-good
standings and show the existing stale/error indication. Do not replace records
with zeros. If the enabled source has no current rows, show an empty state.
Do not offer disabled sources or display historical rows with IsCurrent=0.

## Verification checklist

- NFL appears in the selector and direct source URLs preserve selection.
- The 2026 snapshot shows 32 teams: 16 AFC and 16 NFC, including Kansas City.
- W/L/T, percentages and points match the stored displayValue fields.
- Conference order follows Position; no invented overall ranking is shown.
- Missing statistics, logos and links are handled gracefully.
- Disabled, empty, stale and failed-refresh states preserve existing behavior.
- Existing MLB, WNBA, soccer and Big 12 column mappings still work.

Deployment details: [NFL deployment report](../reports/nfl-standings-deployment-2026-10-13.md).

## Frontend deployment verified (2026-10-13)

Added the espn-nfl column mapping and deployed the rebuilt application as
mynewsfeed-web:nfl-standings-20261013. Runtime environment, the key volume,
network and port mapping were preserved. The previous release is retained
as mynewsfeed-web:before-nfl-standings; latest points to the new release.
This deployment rebuilt the current application source, including the
previous news-card and admin updates, rather than applying a static-asset patch.
No scraper settings, standings records or database schema were changed.

All 418 tests passed, including a new NFL regression checking conference
order, compact headers, missing/case-sensitive statistics, display formatting
and omission of extra columns and ranks. Live browser checks verified NFL
selection by source ID 10, 32 teams in two groups of 16, Kansas City, source
selection after reload, mobile overflow containment and keyboard scrolling.
All 256 displayed statistic values match the preceding live generic tables,
and each conference retains its previous team order. Public HTTP checks and
the seven-page admin responsive/navigation checks passed after deployment.
