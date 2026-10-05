# Deploying the Results page

Instructions for the agent deploying MyNewsFeed on a server. This release adds the public `/results` page (final scores
from ESPN) and the `/admin/results` follows page. The server has **its own SQL Server database**, separate from the
development one, so the database has to be prepared here before the site will work.

Work through the steps in order. Each step has a check; do not go on to the next step until its check passes. When this
document says **stop**, stop and report to the user what you found. Do not improvise around it.

## Ground rules

- Do not change any code, scripts or `docker-compose.yml`. If something here does not match what you find, stop and ask.
- Never print, log or paste the database password or the admin password. Show server and database names only.
- Run SQL only against the database named in this server's `.env` (`SQL_CONNECTION`). If you are unsure which
  database that is, stop and ask.
- Ask the user before step 4 whether the database has a recent backup. If the answer is no or unknown, do not continue
  until the user explicitly says to go ahead.
- Run each SQL script once, as written. Do not edit a script to work around an error.

## What this release contains

| Commit | Change |
|---|---|
| `5d03d69` | Results page, ESPN sync, admin follows page, `db/011` |
| `fddb456` | Layout fixes from review |
| `833e4f9` | Polish from review notes |
| `f923198` | `db/012`, the seed of the seven follows |

Database changes, both in `db/`:

| Script | What it does | Safe to run twice? |
|---|---|---|
| `011_add_result_follows_and_games.sql` | Creates tables `dbo.ResultFollows` and `dbo.ResultGames` | **No.** It fails if the tables already exist. Run it only when step 3 says they are missing. |
| `012_seed_result_follows.sql` | Adds 7 follows: NFL, MLB, MLS, NWSL, and Kansas football, men's basketball and volleyball | Yes. It skips follows that already exist. |

New runtime requirement: the site needs outbound HTTPS to `site.api.espn.com` and `a.espncdn.com`. There are **no new
environment variables** and no API key.

## Step 1: Get the code

From the repository folder on the server:

```bash
git fetch origin
git checkout develop
git pull --ff-only origin develop
git log --oneline -5
```

**Check:** the log includes `f923198 Seed the Results page's follows for new databases`, and both `db/011_add_result_follows_and_games.sql`
and `db/012_seed_result_follows.sql` exist. If `git pull --ff-only` refuses because the server's copy has local commits
or changes, **stop**.

## Step 2: Find the database and a way to run SQL

1. Read `SQL_CONNECTION` from `.env`. It has the form
   `Server=<host>,<port>;Database=<name>;User Id=<user>;Password=<password>;...`. Note the host, port, database and user.
   Report the host and database name to the user; never the password.
2. Choose a way to run `sqlcmd`, in this order:
   - **`sqlcmd` installed on the server** (`which sqlcmd`).
   - **The SQL Server container's own tools**, when SQL Server runs in Docker on this server (`docker ps` shows it,
     for example `sql2025`): `docker exec -i <container> /opt/mssql-tools18/bin/sqlcmd ...`. With this option, use
     `-S localhost` instead of the host name, because the command runs inside the SQL container.
   - If neither is available, **stop** and ask the user how SQL scripts are normally run on this server.
3. Pass the password through the `SQLCMDPASSWORD` environment variable, never on the command line. Use these flags on
   every call: `-S <host>,<port> -d <database> -U <user> -C -b`. `-C` trusts the server certificate, matching
   `TrustServerCertificate=True`. `-b` stops at the first error.

Example, with `sqlcmd` installed (substitute the values from `.env`):

```bash
export SQLCMDPASSWORD='<password from .env>'   # do not echo this
sqlcmd -S <host>,<port> -d <database> -U <user> -C -b -Q "SELECT DB_NAME() AS db, @@SERVERNAME AS server"
```

**Check:** the query returns the database name from `.env`. If it connects to a different database, **stop**.

## Step 3: Check what the database already has

Run this read-only query. It changes nothing.

```sql
SELECT 'base tables (scraper)' AS item,
       CASE WHEN OBJECT_ID('dbo.Pages') IS NOT NULL AND OBJECT_ID('dbo.Sources') IS NOT NULL
             AND OBJECT_ID('dbo.SportsEvents') IS NOT NULL AND OBJECT_ID('dbo.YouTubeVideos') IS NOT NULL
            THEN 'ok' ELSE 'MISSING' END AS state
UNION ALL SELECT '001 Pages.SourceId',            CASE WHEN COL_LENGTH('dbo.Pages', 'SourceId') IS NOT NULL THEN 'ok' ELSE 'MISSING' END
UNION ALL SELECT '002 SourceCategories',          CASE WHEN OBJECT_ID('dbo.SourceCategories') IS NOT NULL THEN 'ok' ELSE 'MISSING' END
UNION ALL SELECT '003+004 Pages.SourceCategoryId dropped', CASE WHEN COL_LENGTH('dbo.Pages', 'SourceCategoryId') IS NULL THEN 'ok' ELSE 'MISSING' END
UNION ALL SELECT '006 YoutubeVideoFeed',          CASE WHEN OBJECT_ID('dbo.YoutubeVideoFeed') IS NOT NULL THEN 'ok' ELSE 'MISSING' END
UNION ALL SELECT '007 YoutubeVideoFeed.Url',      CASE WHEN COL_LENGTH('dbo.YoutubeVideoFeed', 'Url') IS NOT NULL AND COL_LENGTH('dbo.YoutubeVideoFeed', 'FeedUrl') IS NULL THEN 'ok' ELSE 'MISSING' END
UNION ALL SELECT '008 YoutubeVideoFeed rebuilt',  CASE WHEN OBJECT_ID('dbo.PK_YoutubeVideoFeed_old') IS NULL AND OBJECT_ID('dbo.PK_YoutubeVideoFeed') IS NOT NULL THEN 'ok' ELSE 'MISSING' END
UNION ALL SELECT '009 SportsEventsType',          CASE WHEN OBJECT_ID('dbo.SportsEventsType') IS NOT NULL THEN 'ok' ELSE 'MISSING' END
UNION ALL SELECT '010 SportsEventsType.SchoolName', CASE WHEN COL_LENGTH('dbo.SportsEventsType', 'SchoolName') IS NOT NULL THEN 'ok' ELSE 'MISSING' END
UNION ALL SELECT '011 ResultFollows + ResultGames',
       CASE WHEN OBJECT_ID('dbo.ResultFollows') IS NOT NULL AND OBJECT_ID('dbo.ResultGames') IS NOT NULL THEN 'present'
            WHEN OBJECT_ID('dbo.ResultFollows') IS NULL AND OBJECT_ID('dbo.ResultGames') IS NULL THEN 'not yet'
            ELSE 'PARTIAL' END
UNION ALL SELECT '012 follows seeded',
       CASE WHEN OBJECT_ID('dbo.ResultFollows') IS NULL THEN 'not yet'
            ELSE CAST((SELECT COUNT(*) FROM dbo.ResultFollows) AS varchar(10)) + ' follows' END;
```

`005` only fills in display labels and cannot be checked by structure. It is not needed for this release.

How to read the result:

| Result | What to do |
|---|---|
| Base tables `MISSING` | **Stop.** The scraper has not created its tables on this database, so this is not a working MyNewsFeed database. |
| Any of 001–010 `MISSING` | **Stop.** Report which ones. Earlier scripts must be applied in order and some move data; that is the user's call, not part of this release. |
| 011 `not yet` | Go on to step 4. |
| 011 `present` | Skip step 4 and go to step 5. |
| 011 `PARTIAL` | **Stop.** Only one of the two tables exists. Report it and do not run 011. |

## Step 4: Create the tables (`011`)

Only when step 3 showed 011 `not yet`, and after the user has confirmed a backup (see the ground rules):

```bash
sqlcmd -S <host>,<port> -d <database> -U <user> -C -b -i db/011_add_result_follows_and_games.sql
```

**Check:** run step 3's query again. 011 must now say `present`. If the script errored, **stop** and report the error
and step 3's output. Do not run 011 again.

## Step 5: Add the follows (`012`)

```bash
sqlcmd -S <host>,<port> -d <database> -U <user> -C -b -i db/012_seed_result_follows.sql
```

**Check:**

```sql
SELECT Kind, Sport, League, TeamId, Name, Enabled FROM dbo.ResultFollows ORDER BY Kind DESC, League;
```

There should be at least these 7 rows, all `Enabled = 1` unless the user disabled some on purpose:

| Kind | Sport | League | TeamId | Name |
|---|---|---|---|---|
| team | football | college-football | 2305 | Kansas Jayhawks |
| team | basketball | mens-college-basketball | 2305 | Kansas Jayhawks |
| team | volleyball | womens-college-volleyball | 2305 | Kansas Jayhawks |
| league | baseball | mlb | | MLB |
| league | football | nfl | | NFL |
| league | soccer | usa.1 | | MLS |
| league | soccer | usa.nwsl | | NWSL |

## Step 6: Deploy the site

From the repository folder:

```bash
docker compose up -d --build
docker compose ps
```

The build runs `dotnet publish` and fails on purpose if the Blazor script is missing from the output. If the build fails,
**stop** and report the last 40 lines of the build output.

**Check:** the `web` service reaches `healthy` within about a minute. Its health check calls `/healthz`, which only passes
when the database is reachable. Then:

```bash
curl -s -o /dev/null -w "%{http_code}\n" "http://localhost:${WEB_PORT:-8085}/healthz"    # expect 200
curl -s -o /dev/null -w "%{http_code}\n" "http://localhost:${WEB_PORT:-8085}/results"    # expect 200
```

## Step 7: Confirm the results sync

The background sync starts about one minute after the site starts, then checks every 30 minutes and refreshes each follow
every 3 hours. The first run makes about 38 requests to ESPN: two per Kansas follow, and one per day for a 7-day backfill
for each league. Wait 3 minutes after step 6, then:

```bash
docker compose logs web --since 10m | grep -E "Results sync|ESPN"
```

Expect a line like `Results sync: 7 synced, 0 failed, 120 games changed, 38 ESPN requests.` The numbers vary with the
schedule. Then check the database:

```sql
SELECT Name, League, LastSyncedAt, LastError FROM dbo.ResultFollows ORDER BY Kind DESC, League;
SELECT League, COUNT(*) AS games, SUM(CASE WHEN Completed = 1 THEN 1 ELSE 0 END) AS finals
FROM dbo.ResultGames GROUP BY League ORDER BY League;
```

**Check:** every follow has a `LastSyncedAt` and no `LastError`. Most leagues have finals. Low or zero counts can be
normal: a league in its off-season or an international break, or Kansas men's basketball before November.

## Troubleshooting

| Symptom | Likely cause | What to do |
|---|---|---|
| `/results` or `/admin/results` returns 500; logs mention `Invalid object name 'dbo.ResultFollows'` | 011 was not run on this database | Go back to step 3. |
| Every follow has `LastError` like `Could not reach ESPN` | The server cannot make outbound HTTPS requests | Report to the user. From the host, `curl -sI https://site.api.espn.com` shows whether it is the network. |
| A follow has `LastError` like `ESPN answered HTTP 4xx` or `Unexpected response from ESPN` | ESPN changed its unofficial API | Report the exact error to the user. The page keeps the scores it already has. |
| No `Results sync` line after 5 minutes | Nothing was due (every follow synced in the last 3 hours), or the service hit an error | Check `docker compose logs web | grep -i "results sync run failed"`. If nothing shows and `LastSyncedAt` values are recent, the sync is working. |
| `/results` says "No teams or leagues are followed yet" | 012 not run, or all follows disabled | Run step 5's check. |

## Rolling back

The tables added by 011 are only used by the new code, so rolling back the site does not require touching the database:

```bash
git checkout cc888ee          # the commit before this release
docker compose up -d --build
```

Leave `dbo.ResultFollows` and `dbo.ResultGames` in place; the previous version ignores them. Dropping them deletes the
follows and stored scores, so only do that if the user asks for it explicitly.

## Report back

When finished, or when you stop, tell the user:

1. The database host and name you used. Not the password.
2. Step 3's output, before and after.
3. Which scripts you ran, and their output or errors.
4. The `docker compose ps` state, and the `/healthz` and `/results` status codes.
5. The `Results sync` log line, and each follow's `LastSyncedAt` and `LastError`.
6. Anything you skipped or stopped on, and why.
