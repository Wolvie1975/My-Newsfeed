# NHL schedules, standings and results

The NHL collector uses ESPN league `90`, provider key `espn-nhl`, and sport
`Hockey`. The existing hourly `--events-from-db --standings-from-db` job collects
NHL after the updated binary is deployed and both sources are enabled.

Upcoming games use `dbo.SportsEvents`: one row per game, home-team perspective,
UTC start time, venue, broadcasts and both team logos. Canonical identity is
`espn:nhl:<gameId>`. Completed games reconcile existing schedule rows without
being inserted as upcoming fixtures. Postponements and cancellations retain their
status for reconciliation, and existing schedule retention still applies.

Final scores use `dbo.SportsMatchResults` independently of schedule retention.
Identity is `(ProviderKey, GameId)`; repeated imports update corrections while
preserving `ID` and `FirstSeenAt`. Regulation, overtime and shootout finishes use
`STATUS_FINAL`, `STATUS_FINAL_OT` and `STATUS_FINAL_SO`. Scores are ESPN's final
scoreboard totals, including the deciding goal in a shootout; they are not
shootout attempt counts. Separate shootout fields are only populated when supplied
by the provider. Live or tied scores are never imported as final NHL results.

Standings use the existing `dbo.SportsStandingsSources` and `dbo.SportsStandings`
tables. The division feed must contain 32 distinct teams. `StatsJson` retains all
provider statistics, including points, games played, wins, losses, overtime
losses, regulation wins, shootout records, goals and goal differential. Partial
or malformed snapshots fail before replacing standings.

ESPN identifies seasons by their ending year: `2027` means **2026–27**. Collection
requests July through June and rolls to the next season in July. Each game's
season must match; NHL preseason league headers can lag by one year, so game
season fields are authoritative. Empty responses do not delete previous-season
results. Prior-season results are removed only after a successful nonempty
new-season result import. Regular season, preseason and playoffs are included
when published in the season's scoreboard feeds.

## Setup and activation

Deploy the updated scraper using the existing Docker host procedure, preserving
its environment, certificate mount, network, volume and scheduler arguments.
Setup requires migration credentials; ordinary imports use runtime credentials.
From the repository root:

```sh
( set -a; . ./.env.migrations; set +a; cargo run --locked -- --configure-nhl )
```

This creates any missing shared results/standings tables and registers just the
NHL schedule and division standings sources **disabled**. Repeating setup
preserves existing enabled flags, URLs and limits. On a fresh database, first
run the documented schema initialization and runtime permission provisioning.

Validate the schedule/results feed without writing imports:

```sh
cargo run --locked -- --events-feed https://site.api.espn.com/apis/site/v2/sports/hockey/nhl/scoreboard --dry-run
```

Enable the sources in SQL when ready to collect:

```sql
BEGIN TRANSACTION;
UPDATE dbo.SportsEventsType SET Enabled=1
WHERE ProviderKey='espn-nhl' AND ProviderSchoolId=0;
UPDATE dbo.SportsStandingsSources SET Enabled=1
WHERE ProviderKey='espn-nhl' AND Sport='Hockey';
COMMIT;
```

The deployed hourly collector will then collect both sources on its next slot.
The repository change alone does not enable sources or restart the service.

## Reading data

```sql
-- Upcoming fixtures; join enabled sources as in existing front-end queries.
SELECT e.* FROM dbo.SportsEvents e
JOIN dbo.SportsEventsType s ON s.ID=e.SportsEventsTypeId
WHERE s.Enabled=1 AND e.ProviderKey='espn-nhl'
ORDER BY e.EventDate,e.StartsAtUtc;

-- Current standings retain division and published order.
SELECT t.* FROM dbo.SportsStandings t
JOIN dbo.SportsStandingsSources s ON s.ID=t.SourceId
WHERE s.Enabled=1 AND s.ProviderKey='espn-nhl' AND t.IsCurrent=1
ORDER BY t.GroupName,t.Position;

-- Use the ending year for the season selector.
SELECT r.* FROM dbo.SportsMatchResults r
JOIN dbo.SportsEventsType s ON s.ID=r.SourceId
WHERE s.Enabled=1 AND r.ProviderKey='espn-nhl' AND r.Season=2027
ORDER BY r.StartsAtUtc DESC;
```

## Validation

Offline tests cover season rollover, regulation/OT/SO scores, home/away identity,
malformed scores, season mismatches, postponements, full division standings,
missing overtime stats and incomplete/duplicate teams. A read-only live feed
check can be run explicitly:

```sh
cargo test --locked nhl::tests::live_nhl_feeds -- --ignored --nocapture
```

The SQL test is opt-in and requires migration credentials and an isolated
`NHL_TEST_DATABASE` named `WebScraper_NHL_Validation_*`. It verifies repeated
setup, result corrections, stable identity, disabled sources and results surviving
schedule removal. Never point it at production.
