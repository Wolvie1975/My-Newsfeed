# Sports data and public views

Current behavior, reviewed against the web source on October 13, 2026 (Central).
For rollout and credentials see [Deployment](../DEPLOY.md); for dated evidence see
[Release history](release-history.md). Provider setup commands below belong to the
Rust scraper repository and are operational reference, not automatic setup steps.

## Ownership and coverage

| Feature | Owner / tables | Public route |
| --- | --- | --- |
| Upcoming fixtures | Rust scraper: SportsEvents, SportsEventsType, SportsEventSources | `/events` |
| Recent final scores | Web app ESPN score sync: ResultFollows, ResultFollowTeams, ResultGames | `/results` |
| Championship / MLS season scores | Rust scraper: SportsMatchResults | `/championship/results`, `/mls/results` |
| League/conference tables | Rust scraper: SportsStandingsSources, SportsStandings | `/standings` |
| National polls | Rust scraper: SportsRankingSources, SportsRankingPolls, SportsRankingTeams | `/rankings` |

Standings and polls are snapshots, not records reconstructed from fixtures or scores.
Web page requests read stored data; they do not scrape sports providers. The separate
score service calls ESPN. Provider keys scope team/game IDs: never resolve an ESPN
participant ID through Big 12 metadata or merge games by names and dates alone.

The reviewed schedule supports all 16 Big 12 member feeds (football, soccer,
volleyball and men's/women's basketball), Kansas City Current, league-wide WNBA/MLB,
EFL Championship, Premier League and league-wide MLS. MLS also has independently
collected season results. NFL has standings and recent-score support. Subsequent
scraper additions need matching frontend provider rules and coverage verification.

## Upcoming schedule

`/events` lists enabled, unfinished fixtures from today forward. The Schedule and
sport dropdowns narrow the list; `school`, `sport` and `page` selections survive
paging. Retired `from`, `to` and `q` filters are ignored. No favorites-only view exists.
Pages contain up to 200 games, with Earlier/Later controls. Games group by calendar
date, then league; leagues sort alphabetically and confirmed starts precede TBD.

The college schedule no longer requires a stored past opening fixture. Retention
can remove past games without hiding upcoming football, soccer, volleyball or
basketball. Basketball fixtures may appear before their November opening games.
This change applies to sport options and date-specific schedule reads too.

Exclude canceled/cancelled, unnecessary, full-time and `STATUS_FINAL*` fixtures.
Legacy NULL statuses remain valid. Postponed/delayed/suspended games show their
status and Time TBD. Unknown starts use stored EventDate, not a timezone conversion
of a placeholder. Confirmed StartsAtUtc values are marked UTC before Central display.
Big 12 dates retain the source school's calendar day; ESPN providers normally use
Central dates, with provider-date preservation for unconfirmed Eastern/London
midnight placeholders. Do not fabricate venue, time, logo or broadcast fields.

Big 12 mirrored games have one canonical SportsEvents row with multiple source
observations. Enabled school filtering uses SportsEventSources, not just the primary
SportsEventsTypeId. Respect primary perspective and shared-record ownership.

Professional fixtures are read directly from SportsEvents rather than the Big 12
school-calendar view. Validate matching enabled provider configuration:

| ProviderKey | Configuration identity | Stored perspective / canonical identity |
| --- | --- | --- |
| `espn-nwsl` | ProviderSchoolId `20907` | Kansas City Current; `espn:usa.nwsl:<event ID>` |
| `espn-wnba` | ProviderSchoolId `0` | Home team; `espn:wnba:<event ID>` |
| `espn-mlb` | ProviderSchoolId `0` | Home team |
| `espn-mls` | ProviderSchoolId `0`; league `usa.1`, league ID `770` | Home team; `espn:usa.1:<event ID>` |
| `espn-championship` | League `eng.2`; Wrexham team `352` | Home team; `espn:eng.2:<event ID>` |
| `espn-epl` | League `eng.1`, league ID `700` | Home team; `espn:eng.1:<event ID>` |

WNBA SchoolId/SchoolName/TeamLogoUrl identify home, and OpponentSchoolId/Opponent/
OpponentLogoUrl identify away. Neutral sites retain supplied identities with
IsNeutral and nullable IsAway. Kansas City's IsAway is from its own perspective.
Use `LiveStatsUrl ?? Url` for game navigation and actual supplied watch links only.

Representative professional read contract (parameterize provider/day/team):

```sql
SELECT e.*
FROM dbo.SportsEvents e
JOIN dbo.SportsEventsType f ON f.ID=e.SportsEventsTypeId
WHERE f.Enabled=1 AND f.ProviderKey=e.ProviderKey
  AND e.ProviderKey=@ProviderKey AND e.EventDate>=@TodayCentral
  AND (e.ScheduleStatus IS NULL OR
       (e.ScheduleStatus NOT IN ('STATUS_CANCELED','STATUS_CANCELLED',
                                'STATUS_UNNECESSARY','STATUS_FULL_TIME')
        AND e.ScheduleStatus NOT LIKE 'STATUS_FINAL%'))
ORDER BY e.EventDate,e.StartsAtUtc,e.ID;
```

For Championship, `team=352` matches SchoolId OR OpponentSchoolId. Resolve enabled
feed IDs by ProviderKey; production IDs recorded in old reports are not portable.

## Home-page completed events

Added October 17, 2026 (Central).

The home page's Today & Tonight panel also hides games with a stored completed
result, even while schedule status awaits its next refresh. ESPN fixtures match
by league/event identity; Big 12 fixtures match the college competition, exact
confirmed start and both team names (including known ESPN aliases). A date or
team name alone is insufficient. The panel checks both recent results and season
results when loading the page; it does not delete fixtures or scores. When none
remain, it says “No remaining games today.”

Recent results require `ResultGames.Completed`; season results require a matching
ESPN provider/game ID, nonnegative scores and `STATUS_FULL_TIME` or `STATUS_FINAL*`.
A live score alone does not hide a game. This result-based filter runs only for
Home; Events continues using its existing schedule-status rules. Home does not
poll for results, so an already open page needs a reload after collection. If
collection has not stored a matching final, the fixture can remain visible until
either a final result or finished schedule status arrives.

The targeted regression checks cover both college and ESPN fixtures:

```bash
dotnet test MyDailyFeed.sln --filter 'FullyQualifiedName~HomeEventResultsTests|FullyQualifiedName~Homepage_hides_a_college_final'
```

These include a transactional SQL integration fixture; use a development/test
database. Deployment verification is recorded in
[release history](release-history.md#hide-completed-home-page-games--october-17-2026).

## Provider collection and prerequisites

Apply scraper schema and runtime grants before enabling collectors. The web principal
needs appropriate SELECT access. `db/016_add_schedule_status.sql` adds nullable
ScheduleStatus when the scraper has not already supplied it. Result coverage seed
`db/017_seed_schedule_result_coverage.sql` adds WNBA and Big 12 women's basketball
without changing existing enabled choices; earlier seeds cover other supported follows.

Scraper configuration: `--configure-big12`, `--configure-nwsl`, `--configure-wnba`,
`--configure-mlb`, `--configure-championship`, `--configure-epl`, `--configure-mls`. Configuration normally
registers feeds disabled and preserves edited URLs/settings. Enable intended feeds
through administration, then preview and import:

```bash
cargo run --locked -- --events-from-db --dry-run -o /tmp/schedule-preview.xml
cargo run --locked -- --events-from-db -o /tmp/schedule-preview.xml
```

Keep `--events-from-db` in hourly collection. WNBA's default scoreboard URL is
`https://site.api.espn.com/apis/site/v2/sports/basketball/wnba/scoreboard`.
Kansas City's is the soccer `usa.nwsl/scoreboard?team=20907` endpoint. Monthly
`dates=YYYYMM` requests cover current month through next calendar year; long date
ranges and the Current team-schedule endpoint proved unsuitable for future coverage.
NWSL uses the provider-specific `My-Newsfeed/1.0` user agent.

Optional `season=YYYY` is a validation restriction, not an ongoing collection default.
WNBA `team=18,20` filters either participant using ESPN IDs. It does not remove older
stored observations; narrowing existing coverage needs matching frontend filtering.
Use one registered league row, not a separate row for every team/year. League
ProviderSchoolId `0` is not a participant ID.

Stable IDs/FirstSeenAt survive re-imports and rescheduling. Explicit changed statuses
update the same row; missing observations do not themselves infer cancellation.
Fetch/validation failures preserve last-good schedules. Routine retention and public
date filters remain separate from that missing-observation rule. End times are not guessed.

## Recent results

`/results` defaults to today's finished games in the configured Central display zone,
with navigation limited to today and yesterday. It selects by the game's start date;
a final after midnight stays with its start day. Retired/out-of-window dates fall back
to today. Enabled follows without a final in the selected date window are omitted
from the dropdown unless explicitly selected. This can make football/college options
absent even though their follows are enabled and healthy.

Results appear only after ESPN reports a final and synchronization succeeds. The
background service checks about every ten minutes; production uses
`Results__SyncIntervalHours=0.16666666666666666`. Today's visible page refreshes
approximately every two minutes; background tabs do not reload. A process-wide gate
serializes background/admin syncs in one instance, not across multiple instances.
Postponed or canceled games do not have final scores. Provider coverage and reporting
latency can still leave gaps. Raw errors remain in administration/logs.

Admin → Schedule coverage reports enabled follows, missing coverage, unmapped sports
and sync errors without enabling anything automatically. Schedule and results
registration are separate: adding a schedule provider does not create score coverage.

## Championship season results and English soccer

Premier League has Schedule/Table views and 20 teams. Historical EPL match results
are not imported by this collector. Championship has Schedule/Table/Season results,
all 24 table teams, and a Wrexham focus (`team=352`) on schedule/results only.
Wrexham is highlighted in the full table; the table is never reduced to one club.

`SportsMatchResults` is independent of ResultGames/Follows and past schedule cleanup.
Its ID is a .NET `long`; SourceId/GameId/Season/team IDs are integers. MatchDate is a
calendar date; StartsAtUtc/FirstSeenAt/LastSeenAt are UTC. Names, logos, final goals,
ScheduleStatus and SourceUrl are provider values. Select the latest validated stored
season and page 25 matches at a time:

```sql
SELECT r.*
FROM dbo.SportsMatchResults r
JOIN dbo.SportsEventsType f ON f.ID=r.SourceId
WHERE f.Enabled=1 AND r.ProviderKey='espn-championship'
  AND r.Season=@SeasonStartYear
  AND (@WrexhamOnly=0 OR r.HomeTeamId=352 OR r.AwayTeamId=352)
ORDER BY r.StartsAtUtc DESC,r.GameId DESC;
```

Show only validated final scores; missing goals are not zero. W/D/L uses Wrexham's
home/away perspective. Penalty finals are labeled without inventing a shootout winner
from tied goal totals. Corrections preserve row identities. A validated new-season
import replaces older-season results; failed/empty replacement retains last-good data.
Provision the scraper-owned table and SELECT permission before deploying its web view.

## Standings

`/standings?sourceId=<ID>` discovers enabled sources dynamically. Invalid/disabled IDs
fall back to the first visible source in display-name order; visible empty sources
remain selectable. Source visibility additionally respects
`Standings:HiddenProviders`: MLB/WNBA were temporarily hidden during playoffs without
disabling collection. Enabled Big 12 standings remain selectable independently of
retained schedule fixtures. Only current-season rows render; preseason or stale-season
feeds show a current-season unavailable message until the provider publishes new data.

Read source status separately from rows:

```sql
SELECT ID,ProviderKey,Sport,DisplayName,LastAttemptAt,LastSuccessAt,
       LastError,LatestSeason,LastTeamCount
FROM dbo.SportsStandingsSources WHERE Enabled=1 ORDER BY DisplayName,ID;

SELECT t.*
FROM dbo.SportsStandings t
JOIN dbo.SportsStandingsSources s ON s.ID=t.SourceId
WHERE s.Enabled=1 AND s.ID=@SourceId AND t.IsCurrent=1
ORDER BY t.GroupId,t.Position,t.TeamName,t.ID;
```

BIGINT row IDs map to `long`; TeamId/GroupId are strings, Rank and timestamps nullable.
Group by GroupId and display GroupName. Preserve all teams and source Season/Title;
never apply schedule today-forward bounds to tables or derive rank from Position.
MLS sorts points descending within each East/West conference, breaking ties by
provider Position and sorting unavailable points last. Other tables retain Position.
Highlight Kansas City Current in full NWSL and Wrexham in full Championship.

StatsJson is an array of name/displayName/displayValue and optional numeric value.
Ignore extra fields, log invalid JSON by row ID, and show missing values as an em dash.
Use supplied displayValue; don't calculate absent percentages, points or records.
Big 12 uses ordered `column1`, `column2`, … keys; match by key and preserve duplicate
header labels and source record strings. ESPN team IDs never become Big 12 lookups.

| Provider | Full statistic names, in display order |
| --- | --- |
| NFL | wins, losses, ties, winPercent, pointsFor, pointsAgainst, pointDifferential, streak |
| MLB / WNBA | wins, losses, winPercent, gamesBehind, streak |
| NWSL / MLS / Championship / EPL | gamesPlayed, wins, ties, losses, pointsFor, pointsAgainst, pointDifferential, points |
| Big 12 | Published positional keys and headers, preserving conference/overall sections |

Compact defaults: NFL W/L/T/Pct; MLB/WNBA W/L/Pct; soccer W/D/L/Pts; other sources
first three provider columns. Show all statistics reveals the rest and supplied
soccer ranks. About these standings contains refresh/source/order metadata; notes
use native disclosures. Team names wrap and expanded tables scroll inside the page.

Freshness older than two hours warns while keeping last-good data. LastError shows a
generic failed-refresh message, never diagnostic text. Source timestamps are UTC
collection times, not provider publication dates. Preserve official season labels
and notes/clincher markers without inventing qualifications.

Schema/setup in the scraper: `sql/standings.sql`, `sql/standings_seed.sql`,
`--configure-standings`, hourly `--standings-from-db`. NFL has 32 teams in AFC/NFC;
MLS has 30 across Eastern/Western; Championship/EPL have 24/20. These recorded
verification counts do not create inferred ranks or permanent IDs.

## National Top 25

Rankings are separate from standings. The configured polls are:

| Sport | ESPN path | League ID | Poll ID / name |
| --- | --- | --- | --- |
| Football | football/college-football | 23 | 1, AP Top 25 (FBS) |
| Men's basketball | basketball/mens-college-basketball | 41 | 1, AP Top 25 |
| Women's basketball | basketball/womens-college-basketball | 54 | 1, AP Top 25 |
| Women's soccer | soccer/usa.ncaa.w.1 | 5499 | 86, United Soccer Coaches/IWSOC |
| Women's volleyball | volleyball/womens-college-volleyball | 402 | 89, AVCA Division I |

`/rankings?sourceId=<ID>` selects enabled visible sources. Basketball choices additionally
require a current-season poll and stored opening fixture; last season's polls are not
reachable by sourceId. Other polls retain full national tables and age labels.

```sql
SELECT ID,DisplayName,Sport,LastSuccessAt,LastError
FROM dbo.SportsRankingSources WHERE Enabled=1 ORDER BY DisplayName,ID;

SELECT p.ID AS PollRowId,p.PollName,p.Season,p.SeasonLabel,p.PublishedAtUtc,
       p.ProviderUpdatedAtUtc,p.Period,p.SeasonPhase,p.SourceUrl,p.FetchedAt,
       t.ID AS RankingRowId,t.TeamId,t.TeamName,t.Rank,t.PreviousRank,
       t.PreviouslyUnranked,t.Points,t.FirstPlaceVotes,t.Record,
       t.LogoUrl,t.TeamUrl,t.Big12SchoolId,t.IsBig12
FROM dbo.SportsRankingSources s
JOIN dbo.SportsRankingPolls p ON p.SourceId=s.ID AND p.IsCurrent=1
JOIN dbo.SportsRankingTeams t ON t.PollRowId=p.ID AND t.IsCurrent=1
WHERE s.Enabled=1 AND s.ID=@SourceId
ORDER BY t.Rank,t.TeamName,t.ID;
```

Both IsCurrent filters are required. Snapshot identity is
`(SourceId, PollId, Season, PublishedAtUtc)`; teams key by snapshot and ESPN TeamId.
Complete corrections commit atomically and preserve IDs; older versions, wrong polls,
incomplete payloads or failed writes retain last-good data. Schema/seed/grants are
scraper-owned: `sql/rankings.sql`, `sql/rankings_seed.sql`, `sql/runtime_permissions.sql`.
Configure with `--configure-rankings`, explicitly enable intended sources, then preview
`--rankings-from-db --dry-run` and add `--rankings-from-db` to hourly collection.
Grant the web principal SELECT on all three tables; do not give it ingestion permissions
solely to read. Optional pinned seasons must match returned provider seasons.

Use supplied tied ranks without renumbering or enforcing exactly 25 rows. IsBig12/
Big12SchoolId comes from verified active full-member metadata, never numeric ESPN ID
conversion. Keep every national team, mark Big 12 visibly and accessibly, and don't
highlight former members based on history. Preserve provider poll/season/phase/period,
record strings and supplied zeros; unavailable points/votes remain unavailable.
Previous rank → movement is previous minus current; previously unranked → NR/New;
unknown previous → em dash. Soccer/volleyball polls are not labeled AP.

Default columns are Rank/Team/Record/Change. Show full poll details reveals Previous,
Points and First-place votes when available. About this poll reveals source/refresh
metadata; poll date, season and warnings stay visible. Poll age over 14 days is
Latest available poll — dated; collection age over two hours has a separate warning.
A refreshed old poll never becomes a new poll.

## MLS schedule and season-results contract

MLS now has league-wide fixtures and current-season final results from ESPN
competition `usa.1` (league ID `770`). Existing MLS standings remain unchanged.
Production schedule/results source ID: **5525**, ProviderKey `espn-mls`.
Standings source ID: **9**. Resolve IDs by ProviderKey in other environments.

### Upcoming schedule

```sql
SELECT e.* FROM dbo.SportsEvents e
JOIN dbo.SportsEventsType f ON f.ID=e.SportsEventsTypeId
WHERE f.Enabled=1 AND e.ProviderKey='espn-mls'
 AND e.EventDate>=@TodayCentral
 AND (e.ScheduleStatus IS NULL OR
      (e.ScheduleStatus NOT IN ('STATUS_CANCELED','STATUS_CANCELLED','STATUS_UNNECESSARY','STATUS_FULL_TIME')
       AND e.ScheduleStatus NOT LIKE 'STATUS_FINAL%'))
ORDER BY e.EventDate,e.StartsAtUtc,e.ID;
```

Use existing schedule UI: home team is SchoolId, away team is OpponentSchoolId.
Show kickoff, venue, team logos and provider-supplied watch links. Convert known
StartsAtUtc values to America/Chicago; preserve TBD when a kickoff is not known.
Canonical game key is `espn:usa.1:<gameId>`. Optional team filters should match
both SchoolId and OpponentSchoolId; do not add separate team feeds.

### Results

```sql
SELECT r.* FROM dbo.SportsMatchResults r
JOIN dbo.SportsEventsType f ON f.ID=r.SourceId
WHERE f.Enabled=1 AND r.ProviderKey='espn-mls' AND r.Season=@Season
ORDER BY r.MatchDate DESC,r.StartsAtUtc DESC,r.ID DESC;
```

Use the current provider season (initially 2026). Results include regular-season
and playoff games when ESPN publishes them. They are independent of the pruned
SportsEvents table and do not populate existing ResultGames/ResultFollows.

Map these fields:

| Field | Meaning |
| --- | --- |
| ID | bigint / .NET long; stable local result ID |
| SourceId / GameId | int; source and stable ESPN game ID |
| Season | Provider season year; initial season 2026 |
| MatchDate / StartsAtUtc | Central date / UTC kickoff |
| HomeTeamId / AwayTeamId | ESPN team IDs |
| HomeTeamName / AwayTeamName | Provider names |
| HomeScore / AwayScore | Final match goals, excluding shootout penalties |
| HomeShootoutScore / AwayShootoutScore | Nullable int; separate penalty shootout tallies |
| HomeLogoUrl / AwayLogoUrl | Nullable provider logos |
| ScheduleStatus | Provider final status |
| SourceUrl | Original ESPN match page |
| FirstSeenAt / LastSeenAt | UTC first import / most recent observation |

For STATUS_FINAL_PEN, display match goals plus the penalty tally, e.g.
**1–1 (2–4 on penalties)**. Use penalties to identify the winner; do not show a
shootout match as a draw or add penalty tallies to match goals. For other final
statuses, show the match score and supplied status (including extra time). The web
query excludes nonfinal, missing/negative-goal, same-team and malformed penalty rows.
Do not infer playoff-series aggregate winners from a single match.

### Standings and collection

Keep the existing MLS conference standings and route `/standings?sourceId=9`.
The source's Eastern/Western conference groups provide membership; fixture rows
are league-wide and do not carry a conference label.

`--configure-mls` registers the schedule disabled and adds nullable shootout
columns to the shared results table using migration credentials. Enable its
SportsEventsType row to collect. Existing `--events-from-db` hourly collection
then discovers MLS automatically; no extra scheduler argument is required.

Current calendar-season months are fetched to reconcile score corrections and
reschedules. Results require an explicit final status, valid nonnegative goals,
and distinct teams. Final penalty games require valid unequal shootout scores.
Repeated imports/corrections preserve IDs and FirstSeenAt. Disabled sources are
protected by the writer; provider/game identity keeps MLS separate from other
leagues, including Championship results using the same table.

### Retention and empty states

Upcoming schedules follow existing rules: past dates and canceled/unnecessary
fixtures are removed. Current-season final results remain available for the
Results view. Prior-season results are removed after a successful nonempty
new-season results import; this avoids deleting the last good season during the
off-season or after a failed request. The front-end filters Season explicitly.

Show an empty schedule when no future MLS fixtures have been published. Do not
invent playoff matchups or next-season fixtures. Show source import errors via
the existing sports source status UI; retain the last good data on fetch/parse
failure. The frontend is deployed: select MLS in `/events`, or open `/mls/results` for
the latest validated stored season, paged 25 matches at a time. Schedule/Table/Season
results links resolve enabled source IDs by ProviderKey. The ordinary Results page
links to MLS season results while its existing today/yesterday score view remains
independent. Source freshness older than two hours warns without dropping data.

October 13, 2026 verification: 90 upcoming fixtures and 421 completed 2026 results;
no past or canceled fixtures remained after retention.

## Validation

Use existing EventsTests, ResultsTests, EnglishSoccerTests,
StandingsTests and RankingsTests as applicable. Check enabled/disabled selection,
retention, past/final/canceled status exclusion, TBD dates, provider identity, home/away,
paging, last-good behavior, unknown statistics and no invented ranks/scores. Check
phone/desktop layouts, themes, keyboard scrolling and no-JavaScript selectors.
Rust provider fixtures and isolated tests live in the scraper repository. For SQL
validation use explicitly named staging databases, never production; configure
NWSL_TEST_DATABASE, WNBA_TEST_DATABASE or RANKINGS_TEST_DATABASE as appropriate.

## Team records beside matchups

Added October 18, 2026 (Central). Home, Events and Results show current season
records beside teams when the enabled standings feeds provide them. MLS and
Championship season-results pages use the same records. These are the latest
standings records, including on past results; they are not reconstructed records
as of each game's final whistle. The normal standings collector supplies updates.

Professional labels use W–L for baseball/basketball, W–L–D for soccer, W–L–OT for
hockey and W–L–T for NFL. College labels show overall and conference records,
including draws for soccer. College shorthand is overall (conference), such as
`12-4 (5-2)` or `8-2-3 (3-1-1)` for soccer. Missing portions are omitted rather
than replaced with zeroes. College coverage is limited to the Big 12 standings;
uncovered opponents and unavailable records remain blank rather than showing
zeroes. Previous-season college basketball records are excluded until current
academic-season standings are collected.

Each page loads one shared snapshot from enabled SportsStandingsSources and
current SportsStandings rows. Professional records match within provider/sport
using ESPN team IDs where supplied, otherwise the scoped team name. Professional
rows must match the source's LatestSeason. College records use sport and known
team-name aliases, never ESPN IDs as Big 12 IDs, and pass CollegeSeasons validation.
Conflicting duplicate records and missing/malformed statistics are omitted.

Record tooltips include the standings update time in the configured display
timezone. Records older than two hours, without an update time or from a source
with a refresh error carry a visible “last available” label. No tables, seeds,
scraper settings or browser provider calls were added.

Targeted validation includes TeamRecordsTests plus the existing standings, result,
matchup rendering, home-event and personal-feature checks.

### League-wide postseason exclusion

Records are displayed only while the league is confirmed in regular season. Once
a league enters playoffs/postseason, records are hidden for every team in that
league on Home, Events, Results and the season-results pages, including past
results. Games, scores, standings tables and schedules themselves remain visible.

A web background service checks current ESPN scoreboard league metadata on
startup and every ten minutes. It does not infer phase from the standings
SeasonType (which can remain regular season during playoffs), a historical event,
a game title or calendar dates. Soccer stage IDs are interpreted separately from
other sports; explicit playoff/postseason stage names override regular-stage IDs.
Big 12 sports are checked independently using their corresponding NCAA league.
No provider requests run in page rendering or browser code.

The in-memory phase snapshot must confirm regular season within the last twenty
minutes. Startup, unsupported/unknown phases, failed checks and expired snapshots
hide records until a successful regular-season check. No database migration is
required. LeaguePhasesTests covers classification, soccer stage IDs, independent
league/sport gates, postseason transitions and expiry.

## F1 section

F1 is a separate entry under Sports on desktop and mobile. It has its own pages,
including a separate RSS News tab:

| Route | View |
| --- | --- |
| `/f1` | Upcoming/current-weekend calendar |
| `/f1?calendar=all` | Full collected season calendar |
| `/f1/results` | Latest published Grand Prix classification |
| `/f1/news` | F1 publisher articles, source filtering and cursor pagination |
| `/f1/results?race=<round>:Sprint` | A published sprint classification |
| `/f1/standings` | Drivers' championship |
| `/f1/standings?table=constructors` | Constructors' championship |

The existing reader accesses dbo.F1Schedule, F1Results, F1DriverStandings,
F1ConstructorStandings and F1Source through read-only, keyless EF mappings. F1
remains separate from home/away match events and team W/L records. No browser or
web-request provider fetches, ingestion writes or frontend schema migrations are
needed. A repeatable-read transaction keeps each page's reads on one coherent
backend snapshot while the collector can replace the snapshot atomically.

Schedule cards show the round, Grand Prix, circuit, location and race date/time,
with a native expandable timetable for supplied practice/qualifying/sprint
sessions. Equivalent SprintQualifying/SprintShootout entries are deduplicated.
Confirmed UTC clocks convert through the configured display timezone; date-only
sessions keep their original date with Time TBD. RaceUrl is labelled Race
reference and validated as an outbound link, never treated as a stream. Past
starts without Race classifications show Results pending/unavailable; scheduled
start alone does not establish a final or cancellation.

The result selector lists only collected Race/Sprint classifications, defaults
to the latest round with Grand Prix preferred, and preserves explicit session
selection through reload. Numeric Position determines order; PositionText,
provider points (including fractions), time/gap strings and status are preserved.
Grid zero is shown as Pit lane / unavailable. Optional timing details do not
award or recalculate points. Standings preserve provider ranks and as-of round,
show all supplied entries, and list every supplied constructor for a driver.

Disabling Jolpica F1Source hides the structured sports datasets; preseason
results/standings can validly be empty. Failed refreshes retain the backend's last-good snapshot and display a
generic notice with its original fetched time. Database failures show an error.
The source attribution is Data: Jolpica F1, with an explicit periodic/non-live
notice. Only the latest collected season is available; no historical selector,
live timing, driver portraits or constructor logos are supplied.

F1Tests covers parsing, timezone/date-only behavior, sprint weekend sessions,
classification choice, decimals, multiple constructors, health and read queries.
SQL fixtures use connection-local temporary tables and leave production F1 data
untouched. Run `dotnet test MyDailyFeed.sln --filter FullyQualifiedName~F1Tests`
with the existing development/test connection configuration.

The dedicated F1 home summary reuses the same read contract in its own card,
separate from Today & Tonight. The next Grand Prix excludes rounds with published
Race classifications; the latest published result may be Race or Sprint. It uses
the same timezone/TBD, last-good freshness and error handling as the F1 section.

F1 News uses Pages, Sources and SourceCategories through the standard feed
repository. It does not read Jolpica classifications or depend on F1Source.
Only enabled F1 News sources are included, with no numeric ID list. No source
registration scripts or scraper deployment are needed for frontend rollout. See
[F1 News behavior](interface.md#f1-news).

F1NewsTests covers category isolation, source filtering, cursor pagination and
article rendering. Run `dotnet test MyDailyFeed.sln --filter FullyQualifiedName~F1NewsTests`
with the same development/test connection configuration.

## College Sports hub

Sports → College Sports opens a dedicated hub with a shared sport selector:
Football, Men's Basketball, Women's Volleyball and Women's Soccer. The existing
intentional exclusion of women's basketball is retained.

| Route | View |
| --- | --- |
| `/college` | Big 12 schedule from today through the next seven days |
| `/college/results` | Finished games from enabled college follows, today or yesterday |
| `/college/standings` | Big 12 standings for the selected sport |
| `/college/rankings` | Available national polls for the selected sport |
| `/college/news` | Conference-wide Big 12 publisher articles |

All views accept `sport=football|mens-basketball|volleyball|soccer`. An absent or
unrecognized sport defaults to football. Tabs, schedule pagination and result-date
links preserve the selected sport. The hub uses one sport selector; embedded
standings and rankings omit the redundant source dropdown. Choosing a different
sport resets view-specific source, date and page filters. GET forms include
no-JavaScript Apply controls. The existing standalone sports routes remain available.

Schedules use the existing enabled-feed, final/cancellation and timezone rules;
seven-day listings paginate at the shared schedule limit. Results use the existing
follow membership and site-timezone day windows, scoped to the selected NCAA
league before counting or limiting. Standings scope sources to Big 12 and the
selected sport; rankings scope sources to the sport. An unrelated source ID cannot
switch the hub into another sport or professional league. Current-season and
preseason gates remain in effect. Missing sources and empty listings are shown
explicitly, with no fallback to a different sport.

Game rows reuse the existing team records, conference shorthand, freshness
tooltips and league-wide playoff suppression. Schedule/result update times are
scoped to the sport. No new scraper, source registrations, credentials or database
schema changes are required. The News tab is conference-wide rather than filtered by the selected sport;
it retains the sport for navigation back to structured coverage. See
[Big 12 News behavior](interface.md#big-12-news).

## Individual schedules

The combined My Week agenda and its calendar downloads were removed at the user's
request. Sports Events, Results, College Sports and F1 retain their existing
schedules, filters and navigation. Personal team highlights and Home's optional
selected-team filter remain available through Settings.

## Cached box scores and game-stat modals

Results (including College Results), MLS season results and Championship season
results offer Box score when cached player statistics exist, or Game stats when
only team statistics exist. Actions appear only for supported, valid normalized
snapshots in `dbo.SportsBoxScores`, keyed by `(Sport, League, EventId)`.
Existing score sources, team records, filters, pagination, shootouts and provider
score links remain unchanged. Schedules retain their original Stats/Details links;
F1 classifications are independent.

A native dialog opens on tap, with Close/Escape, focus containment and focus return
to the originating link. The Results auto-refresh pauses while a dialog is open.
A normal `/box-score/{sport}/{league}/{eventId}` page is the no-JavaScript and
modified-click fallback. Invalid, missing, hidden or unavailable identities return
404 with generic unavailable text. A failed modal load leaves a normal detail link.
No typed input or embedded external website is required.

Statistics come from the existing optional read-only cache, with no request-time
provider scraping. One bounded batch per visible listing validates availability,
composite identity, supported version and usable JSON before rendering actions.
This validation deliberately reads cached payloads in the batch to avoid offering
buttons for malformed/empty snapshots; payloads are not embedded in list HTML.
Detail HTML is fetched only when requested. Failed availability reads leave existing
results usable. A valid retained last-good snapshot remains available after a
refresh error; raw diagnostics and retry times are never displayed.

Team values align by `(group,key)`, not array position. Player values and provided
totals retain the original column indices, including when an unusable column is
skipped. Missing values show an em dash; zeroes, percentages and precision remain
unchanged. Team-stat sections expand on tap; team-only games initially open their
first section. Available player tables are visible directly. Period scores retain
every supplied entry per team, including repeated unknown periods, without inferred
inning/quarter/set numbering. Provider team scores do not replace result-card scores.

Tables scroll within labelled keyboard-focusable regions on narrow screens. Provider
text is Razor-encoded; optional provider URLs require HTTPS, an ESPN host and a path
segment matching the event ID. Timestamps use the configured display timezone.
No cache writes, schema migration, collector restart or new provider credentials
are required. The frontend runtime must retain SELECT access to SportsBoxScores.

Baseball uses a reference-style R/H/E scoreboard followed by paired hitting and
pitching panels (two columns on wide screens, stacked on mobile). Primary columns
follow conventional box-score ordering while additional cached columns remain in
expandable tables. R/H/E comes from batting runs/hits and fielding errors, never
pitching totals. Missing inning labels remain unspecified rather than guessed.
The current cache does not contain pitcher decisions, headshots or scoring plays;
those reference elements require a collector contract update before rendering.

Soccer team statistics render as immediately visible comparison tables, with club
values flanking a shared statistic label. Each available group remains separate;
values align by group/key rather than source array order. Period sections and
available player tables remain accessible. The user's ESPN reference is
https://www.espn.com/soccer/team-stats/_/gameId/761864/league/usa.1 (verification
screen prevented exact visual inspection). No new provider data is fetched.

Football player statistics use category-first sections with paired team panels on
wide screens and stacked panels on phones. Conventional passing/rushing/receiving,
defense and special-teams ordering applies to available groups; unknown categories
are retained afterward. Source columns, totals, positions and starter flags are
preserved. This applies to NFL and college football. Existing period sections and
team-stat comparisons remain available. Reference supplied by the user:
https://www.espn.com/nfl/boxscore/_/gameId/401872995 (verification screen prevented
exact visual inspection). No additional provider data is requested at runtime.

### Repaired scoring-period labels and NHL detail visibility

The period-label collector repair retains payload version 1 and emits descriptive
strings in `lineScores[].period`. Numeric/null values remain compatible. Render
source labels and values without deduplicating unknown entries or calculating
final results. NHL SO may be a deciding 0/1 indicator, not shootout attempts/goals.
Baseball shows labeled per-team inning tables when the teams' period sequences
differ, avoiding positional misalignment in sparse data.

NHL detail has an additional `SportsMatchResults` visibility path: resolve the
first enabled `espn-nhl` source, respect any configured team scope, and require a
valid final in its latest validated stored season. It does not hardcode a season
or allow arbitrary cache identities. Existing ordinary followed-result visibility
continues independently. No dedicated NHL season-results page/query is added.
See [backend frontend handoff](../new%20development/sports-period-labels-frontend-handoff.md).

### Basketball player template

Basketball player tables use paired team panels on desktop and stacked panels on
mobile, with dynamic provider columns and internally scrolling tables. Player
names remain visible during horizontal scrolling. Positions and known starter/
bench flags are shown; missing flags remain unspecified. Supplied totals, zeroes,
shooting strings and missing values retain their meaning. Existing period/team
sections and production visibility rules remain unchanged.

The championship sample in new development/mens-college-basketball-boxscore-example.json
is used only for offline parsing/rendering and browser fixture checks. It does not
create a production result or cached detail route. The component also supports
other basketball leagues through the same dynamic contract.

## Live scoreboard

`/scoreboard` and a separate homepage card read the scraper-owned
`dbo.SportsScoreboard` and `dbo.SportsScoreboardCoverage` views using existing
frontend SELECT access. These reads never trigger collection. Sports navigation
links to Scoreboard on desktop and mobile. Existing schedules, results and F1
remain independent; Big 12 calendar IDs have no validated ESPN bridge, so those
fixtures are not merged with scoreboard games.

The full page offers enabled competition choices and Central dates from three
days back through seven ahead. Today includes older live/delayed/suspended games,
with their assigned date shown. Home shows up to six games with live/paused games
first and links to the full page. Each Central midnight is converted separately
for daylight saving changes. Coverage comes from enabled sources and does not
promise national college coverage or a complete historical archive.

Scores update approximately every ten minutes. Visible pages read stored HTML
again every five minutes and immediately when returning to a visible tab. Failed
reads retain the displayed sample with a retry message. Open box-score dialogs
and focused scoreboard controls prevent background replacement. SQL sample times,
stale/error/partial warnings, status labels and fixed sampled clocks distinguish
freshness from active play. NULL scores stay unavailable; actual zero stays zero.
Shootouts remain separate, and only explicit final winner flags receive emphasis.
Baseball counts/bases, per-period/set scores, venue and broadcasts appear under
Game info on the full page when supplied. Native disclosures work without
JavaScript and stay expanded across automatic refresh. Team badges match other
sports views; full-page cards use aligned footers and up to three desktop columns.
Home uses compact divided rows within its existing card. Short update times have
full date/time tooltips; stale and collection warnings remain visible.

Stars save highlights in `mydailyfeed-scoreboard-teams-v1`, keyed by
Sport/League/TeamId. Unknown IDs have no star; existing name-based team preferences
are preserved separately. No login or server write is needed. Without JavaScript,
server-rendered scores, date links, competition forms and cached box-score detail
links work; automatic refresh and stars require JavaScript.

Cached box scores reuse existing validated payloads and modal links. The detail
reader also accepts games still visible through enabled ESPN scoreboard coverage.
Missing or unusable cache entries never hide games. The frontend adds no schema,
collection jobs, write grants or upstream statistics requests.

Checks: `ScoreboardTests`, the read-only SQL `ScoreboardReadTests`, and
`tests/browser/scoreboard.browser.cjs` (BASE_URL and PLAYWRIGHT_MODULE optional).
