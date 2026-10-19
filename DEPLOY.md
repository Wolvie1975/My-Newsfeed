# Deployment and operations

Maintained runbook consolidated October 13, 2026 (Central). This replaces the
commit-specific Results rollout and separate movie/PWA instructions. Historical
image tags and validation belong in [Release history](docs/release-history.md).
Sports and entertainment contracts are in [Sports](docs/sports.md) and
[Entertainment](docs/entertainment.md).

## Scope and runtime configuration

The web app runs as `mynewsfeed-web`, listens on container port 8080 and normally
publishes host port 8085 on the shared `dev-bridge` network. SQL Server is maintained
separately. The reviewed shared environment uses sql2025 / WebScraper; another server
may use a different database. Always use that deployment's actual runtime connection,
not an assumed development database or copied numeric source IDs.

Compose reads private `.env` settings: SQL_CONNECTION, ADMIN_PASSWORD, optional
WEB_PORT, REVERSE_PROXY_IP and DISPLAY_TIMEZONE. Direct container deployments may
have runtime overrides absent from `.env`; inspect and preserve them privately.
Keep the persistent `/keys` data-protection volume, network, ports, restart policy,
health check and any existing operational settings. Never print passwords or a full
connection string. Display:TimeZone defaults to America/Chicago. Scraper:StaleAfterHours
controls admin freshness; Results:SyncIntervalHours defaults to a ten-minute interval.

The web app requires outbound HTTPS to ESPN and its image CDN for result synchronization.
Team-record eligibility also uses a background ESPN league-phase check at startup
and every ten minutes. Records remain hidden until regular season is confirmed;
unknown or failed checks hide them, and confirmations expire after twenty minutes.
Check `League phase` log entries after deployment. This needs no database migration
and runs independently of the results sync interval.
Rust scraping jobs run separately. A web deployment does not automatically rebuild
or configure the scraper, register new providers or enable feeds.

## Prepare source and database

Inspect `git status --short`; preserve local changes and use the intended release
source. Fetch/fast-forward only a clean, appropriate branch when needed. Do not reset
server changes or blindly reuse the original Results release's fixed commit IDs.

Confirm a recent backup before schema changes. If backup status is unknown, stop
and obtain approval before changing schema. Run the existing scripts as written,
in dependency order, with error-on-failure mode. Do not edit migrations to work
around failures or re-run a partially completed nonrepeatable migration.

Use installed sqlcmd, or SQL Server container tooling
`/opt/mssql-tools18/bin/sqlcmd`. Supply SQLCMDPASSWORD privately through the environment,
not in command arguments/output. Every invocation uses `-S`, `-d`, `-U`, `-C`, `-b`.
Inside the SQL container use localhost. First run a read-only identity check:

```sql
SELECT DB_NAME() AS db, @@SERVERNAME AS server;
```

Verify the database matches runtime configuration. Stop if it does not, required base
scraper tables are missing, or a schema is partially initialized. Original application
migrations 001–010 prepare Pages/source categories/YouTube/sports configuration;
inspect existing columns and objects before applying earlier data-moving migrations.
Keep scraper-owned `sql/` migrations in their separate repository.

| Application script | Purpose / application rule |
| --- | --- |
| [011](db/011_add_result_follows_and_games.sql) | ResultFollows and ResultGames; run once only when both absent; partial presence requires investigation |
| [012](db/012_seed_result_follows.sql) | NFL/MLB/MLS/NWSL and Kansas follows; repeatable, preserves existing choices |
| [013](db/013_add_result_conference_follows.sql) | Conference follows / ResultFollowTeams; run once after 011 |
| [014](db/014_seed_big12_follows.sql) | Big 12 football/basketball/volleyball follows; after 013, preserves existing choices |
| [015](db/015_seed_big12_womens_soccer.sql) | Big 12 women's soccer; after 013, repeatable |
| [016](db/016_add_schedule_status.sql) | Nullable ScheduleStatus; apply before web deployment if scraper has not added it; repeatable |
| [017](db/017_seed_schedule_result_coverage.sql) | WNBA and Big 12 women's basketball coverage; requires conference schema, preserves existing choices |

Feature prerequisites include SELECT access to scraper-owned standings, ranking,
movie, TV, box-office and Championship result tables. Apply scraper schema/runtime
grants and validated initial collection before deploying dependent views. Do not
invent web migrations for missing scraper tables or give the web app an ingestion
role solely to read snapshots. Successful empty enabled feeds are valid.

Before deploying the MLS season-results model, run the scraper’s `--configure-mls`
with migration credentials to provision nullable HomeShootoutScore/AwayShootoutScore
on SportsMatchResults. Both columns must exist before the updated web app reads
that shared table, including Championship queries. Setup registers MLS disabled
and preserves existing settings; explicitly enable the intended MLS feed and
verify `--events-from-db` collection. No additional hourly scheduler flag is needed.

Before deploying movie-section posters, apply the scraper’s additive
`--configure-boxoffice` migration so BoxOfficeWeekend and BoxOfficeForecasts
both expose nullable PosterUrl and PosterLookupStatus. The web queries require
these columns; movie releases continue reading Movies.PosterUrl. The scraper
owns CinemaPosterCache and poster lookup/backfill, not the web app.

For movie releases run [check-movie-schema.sql](deployment/check-movie-schema.sql)
against the target database in read-only, error-on-failure mode. Stop on failed schema
checks; provision missing scraper schema in the scraper project first.

## F1 read prerequisites

Before deploying F1, the scraper owner must provision and populate F1Source and
the F1Schedule, F1Results, F1DriverStandings and F1ConstructorStandings views. Verify
the existing frontend runtime identity can SELECT all five. No new credentials,
write grants or runtime DDL are needed by the web app. The collector owns
enablement, schema and atomic snapshot refreshes. Verify `/f1`, `/f1/results`,
`/f1/standings` and the Constructors view after rollout, including timestamps,
Grand Prix/Sprint selection and mobile table scrolling.

For the College Sports hub, no additional database objects or collector rollout
are required. Verify `/college` and its Results, Standings and Rankings tabs for
all four sports, sport persistence, empty/preseason states and mobile navigation.
Also verify `/college/news`, its publisher filter, article saving and complete
pagination. It reads the existing Big 12 News category; no collector restart or
registration scripts are needed for the frontend rollout.

## Build and deploy

Run relevant application checks; the full suite is:

```bash
dotnet test MyNewsFeed.sln
```

Tests need configured SQL access; use development/staging for integration fixtures.
Record the current image ID for rollback. For a Compose-managed deployment whose
private settings reflect all current runtime overrides:

```bash
docker image tag "$(docker inspect mynewsfeed-web --format '{{.Image}}')" mynewsfeed-web:before-release
docker compose up -d --build web
docker compose ps
```

The Dockerfile publishes the app and verifies `_framework/blazor.web.js` is in the
static-asset manifest. A failed build must not replace the running service. Preserve
logs and report the build failure. If Compose settings do not match the live runtime,
recreate from its preserved configuration through the established deployment process
rather than discarding settings. Never include runtime secrets in a report.

## Validate and troubleshoot

The container should become healthy within about a minute; `/healthz` checks database
reachability. Verify `/healthz`, `/news`, `/events`, `/results`, `/standings`, `/rankings`,
`/movies`, `/movies/digital`, `/box-office` and affected admin views. Unauthenticated
admin access should redirect to login. Inspect theme/responsive layout, selected-source
URLs, paging and no-JavaScript controls relevant to the release. Inspect application
logs without publishing credentials or connection strings.

A missing required table, column or SELECT grant requires database preparation, not
a fabricated empty success. Failed provider imports should keep last-good data with
a generic public warning and diagnostic details restricted to logs/admin. Empty results
can be normal outside game dates or before finals synchronize. Date-limited results
and full-season Championship results are independent.

For score sync, inspect recent `Results sync`/ESPN log entries after the service starts.
A healthy run can skip follows not yet due; verify actual status before forcing refresh.
Read-only checks:

```sql
SELECT Name,League,Enabled,LastSyncedAt,LastError
FROM dbo.ResultFollows ORDER BY Kind DESC,League;
SELECT League,COUNT(*) AS games,
       SUM(CASE WHEN Completed=1 THEN 1 ELSE 0 END) AS finals
FROM dbo.ResultGames GROUP BY League ORDER BY League;
```

Reporting should identify the target database, scripts actually run, health and HTTP
checks, relevant sync status, release/rollback image and material skipped steps.
Historical row counts and timestamps are evidence for their original run only.

## Rollback

For the Compose path, restore the recorded image without rebuilding:

```bash
docker image tag mynewsfeed-web:before-release mynewsfeed-web:latest
docker compose up -d --no-build --pull never --force-recreate web
```

A preserved previous container can instead be restored using its matching runtime
configuration. Verify health and affected routes after rollback. Leave additive
schemas and stored data intact; dropping results/fixtures destroys data and is not
part of an application image rollback. Check recorded rollback tags still exist
before relying on them. `before-release` is a runbook example, not a guaranteed tag.

## Private HTTPS and Android installation

Tailscale Serve proxies host HTTP port 8085 through the server's HTTPS `*.ts.net`
name. It is tailnet-only; Funnel is not used. Serve configuration lives in the
Tailscale container's persistent state volume. Review existing Serve configuration
before replacing it:

```bash
docker exec tailscale tailscale serve status
docker exec tailscale tailscale serve --bg http://127.0.0.1:8085
```

On Android, connect Tailscale, open the HTTPS address in Chrome, and use Install app /
Add to Home screen. The manifest launches Home standalone. The service worker caches
identity assets and a reconnect screen, not articles, schedules, logins or admin pages;
current content still needs the server.

Production REVERSE_PROXY_IP identifies the trusted host proxy's Docker gateway.
Compose maps it to ReverseProxy:KnownProxies; forwarded scheme/client headers are
accepted only from trusted proxies and framework loopback defaults. This supports
secure authentication cookies through HTTPS while LAN HTTP remains available. Verify
the gateway after recreating the network:

```bash
docker inspect mynewsfeed-web --format '{{range .NetworkSettings.Networks}}{{.Gateway}}{{end}}'
```

Check HTTPS certificate, manifest, icons, service worker/offline screen and admin
login. Service-worker updates remove only prior `mynewsfeed-pwa-*` caches; change
cache version when bundled offline assets change.

The original movie deployment report targeted `jcarpio@mynewsfeed-prod` at
`/home/jcarpio/Projects/My-Newsfeed`. Treat that as historical environment context;
confirm the current target and runtime configuration before operating on another host.
