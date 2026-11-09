# SQL credentials: rotate `sa`, give each app its own login

Draft for review, November 4, 2026 (America/Chicago). Nothing here has been run. The scripts in this folder only
parse-checked against the server (`SET PARSEONLY ON`; no logins, users or roles exist yet).

## What I found (read-only checks, November 4)

- **The live `sa` password is still the leaked one.** It matches one of the three password values committed to Git
  before October 25. It is `sysadmin`.
- **Who uses `sa` today:** the public site (`mynewsfeed-web`), admin (`mynewspaper-admin`) and this dev container's
  tools (the `.env` `SQL_CONNECTION`). Nothing else was connected as `sa`.
- **The Rust scraper and scoreboard don't use `sa` at runtime.** `web-scraper` and `web-scoreboard` connect as
  `webscraper_ingest`. That login is in the `WebScraperIngest` role, with 180 table- and column-level grants. Their
  `--configure-*` migrations are documented as using "migration credentials". I couldn't see the Rust project from
  here (`/home/jcarpio/Development/Environment/Rust_Projects/Rss-Rust-Scrape` isn't mounted), so where those come
  from needs checking on the host.
- There is an unused `WebScraperScoreboardReader` role (SELECT on 12 scoreboard/Tech/homelab objects, no members).
  It doesn't cover what the apps read, so the plan doesn't use it.
- **Stopped rollback containers** carry the `sa` connection string in their saved environment. There were twelve
  on November 4 morning; by November 5 there are fourteen `…-before-…` containers (web, admin and scoreboard).
  The scoreboard ones probably hold `webscraper_ingest`, not `sa`; all go in phase 5 either way. `sql2025` carries `MSSQL_SA_PASSWORD` (used only when the data
  directory is first created).
- `.gitignore` ignores `.env` exactly, not `.env.*`.
- There's a stray `_bak_20261104_ContentRequests` table in `WebScraper` (a manual backup from earlier today).

## Design

| Login | Role | Can do |
| --- | --- | --- |
| `mynewspaper_web` | `MyNewspaperWebApp` | SELECT on schema `dbo`. INSERT/UPDATE/DELETE on the 11 `MyDailyFeed*` tables, `TvTrackedSeries`, and `ResultFollows`/`ResultFollowTeams`/`ResultGames` (scores sync runs in the web host) |
| `mynewspaper_admin` | `MyNewspaperAdminApp` | SELECT on schema `dbo`. INSERT/UPDATE/DELETE on the same app tables, plus `Sources`, `SourceCategories`, `Pages`, `YoutubeVideoFeed`, `YouTubeVideos`, `SportsEvents`, `SportsEventsType` and the Results tables |
| `sa` | `sysadmin` | Rotated. Used only for migrations and maintenance, from a private file outside the repo; no app container has it |

- Neither app login gets DDL, `db_owner` or a server role. The write lists come from the code: EF `Add`/`Remove`,
  `ExecuteUpdate`/`ExecuteDelete`, tracked `SaveChanges` in the admin pages, and the raw SQL in `AdminStore`,
  `ProfileAdmin` and `SharedArticles`. Phase 1 confirms them before production.
- Reads are granted at the schema level. Both apps read across about 70 tables and 10 views, and per-object SELECT
  would break each time the scraper adds a table. The danger is writes and DDL, and those are per table.
- Rule going forward (to add to DEPLOY.md and the migration template): a migration that adds an app-owned table also
  grants the role(s) that write it.

## Scripts

| File | What it does |
| --- | --- |
| [01-app-logins.sql](01-app-logins.sql) | Creates both logins (if absent), roles, grants and users in `$(MNP_DATABASE)`. Repeatable; an existing login keeps its password |
| [02-verify-app-logins.sql](02-verify-app-logins.sql) | Read-only: probes allow/deny as each login inside rolled-back transactions; lists server-role memberships (expects none) |
| [03-rotate-sa.sql](03-rotate-sa.sql) | Refuses while any other session uses `sa`, then changes the `sa` password |
| [04-test-login.sql](04-test-login.sql) | Creates `mynewspaper_test`, `db_owner` of `WebScraper_Test` only; refuses if it has a user in `WebScraper`. Re-run after every `create-test-database.sql` (the restore drops the user) |
| [99-rollback-app-logins.sql](99-rollback-app-logins.sql) | Drops users and roles; drops the logins only with `MNP_DROP_LOGINS=yes` |

Passwords are passed as environment variables that sqlcmd reads as scripting variables. They're never on a command
line, in a file in the repo, or printed. Generate each one straight into a private file:

```bash
umask 077; openssl rand -base64 36 | tr -d '/+=\n' | cut -c1-32 > "$S/web.pw"   # $S = a private temp dir
```

## Rollout

**Phase 0: prepare (no server change).**
- Confirm a recent backup of `WebScraper`.
- Add `.env.*` (with `!.env.example`) to `.gitignore`.
- Choose where the rotated `sa` lives: a password manager plus a root-only file outside the repo (for example
  `~/.config/mynewspaper/sa.env`).

**Phase 1: rehearse on `WebScraper_Test`.**
1. Run `01` then `02` with `MNP_DATABASE=WebScraper_Test`. Every probe should PASS. The logins are server-wide, so
   this creates them; their only database user is in `WebScraper_Test`.
2. Run Web and Admin locally against `WebScraper_Test`, each with its own login in
   `ConnectionStrings__DefaultConnection`. Then run the browser suites and exercise every admin page, Sync now,
   requests, profile create/delete, Saved and the background services.
3. Search both app logs for `permission was denied` (SQL 229/230). Add any missing table to `01`, re-run it, and repeat.
4. **Test login (decided: yes).** The .NET integration tests seed scraper tables, so they need more than either
   app login. Run [04-test-login.sql](04-test-login.sql) to create `mynewspaper_test`, `db_owner` of
   `WebScraper_Test` only. That makes "tests never touch `WebScraper`" true at the server too, not just in
   `TestDatabase`. `create-test-database.sql` (a restore) stays on `sa`, and `04` is re-run after it.
   - Code change: today `TestDatabase.cs` reads the web app's `DefaultConnection` user secret, the same one the
     local web host uses. Make it prefer a new `ConnectionStrings:TestConnection` (user secret or
     `ConnectionStrings__TestConnection`), falling back to `DefaultConnection`, and set that secret to the test
     login. Run the full suite under it; it should still be 1099+ passing.

**Phase 2: production logins (additive; the apps keep running on `sa`).**
- Run `01` then `02` with `MNP_DATABASE=WebScraper`.

**Phase 3: switch the apps, one at a time.**
- **Web:** in `.env`, change `SQL_CONNECTION` to `User Id=mynewspaper_web` with its password, then
  `docker compose up -d --no-build web`. Check `/healthz`, the key pages, a profile flow, the scores-sync log lines,
  and the log for SQL 229.
- **Admin:** run the container swap from the handoff with the copied env file's `ConnectionStrings__DefaultConnection`
  changed to `mynewspaper_admin`. Check sign-in, an edit on each admin page type (then revert it), Requests and
  Sync now.
- **Rollback for either app:** put the previous connection back (`sa` still works until phase 4) and recreate the
  container, or rename the `…-before-…` container back.

**Phase 4: rotate `sa`.** Blocked until you report where the Rust migration credentials come from (step 2).
1. Move this dev container's tools to the right login: day-to-day clean-up (`zz` profiles, devices) works as
   `mynewspaper_web`; migrations use the private `sa` file.
2. Move the Rust project's migration credentials (on the host) to the private file as well. **You check this on
   the host** (the Rust project isn't mounted in the dev container): where the `--configure-*` migrations get their
   login, and whether it's `sa`.
3. Run `03`. It refuses while anything else is connected as `sa`.
4. Update the new value in the password manager and in `sql2025`'s private env file (`MSSQL_SA_PASSWORD`). Since
   November 5 the data is on the `sql2025-data` volume, so the variable only matters if the volume is ever recreated
   empty; `ALTER LOGIN` persists. Remove the stopped `sql2025-before-volume` container (it holds the old password
   and the pre-volume data) once the volume has run cleanly for a few days. The tests are already on `mynewspaper_test` by then.
5. **Where `sa` lives (decided):** your password manager, plus a root-only file on the host outside the repo, for
   example `~/.config/mynewspaper/sa.env` (`chmod 600`). Only you hold it: no app container, and not this dev
   container's default tools. When a migration needs `sa`, you run it or supply the value for that run.

**Phase 5: tidy.**
- **Decided:** remove all the stopped `…-before-…` rollback containers (fourteen on November 5; their saved
  environment holds the old password). The images stay as rollback. New rollback containers will carry only the app
  logins.
- **Decided:** drop `_bak_20261104_ContentRequests`.
- Leave Git history alone: the old value is dead once `sa` is rotated. Rewriting history would force-push over
  GitHub and break clones without adding protection.
- Record the result in DEPLOY.md (runtime identities, the grant rule), release history, and the guardrails table row
  in `docs/coding-standards.md`.

## Decisions (November 5, 2026)

1. **Test login:** yes, part of this change ([04-test-login.sql](04-test-login.sql), phase 1 step 4).
2. **Rotated `sa`:** password manager plus a root-only host file outside the repo. Only the user holds it.
3. **Rust migration credentials:** the user checks on the host. Phase 4 waits on the answer.
4. **Phase 5:** remove all the stopped rollback containers and drop `_bak_20261104_ContentRequests`.

Still to confirm before any server change: a recent backup of `WebScraper` (phase 0), and approval to start phase 1.

## Status, November 5, 2026 ~05:15 UTC

- The user put a **new** sa password in `.env` (`SQL_CONNECTION`); it is not in Git history. SQL Server's sa is
  **unchanged** (still the leaked value, last set September 25, 2026), so `.env` doesn't log in. Until the rotation:
  `tools/verify.sh`, the .NET tests, `tools/sql/*` and any deploy or recreate of web/admin fail (deploy.sh would
  roll back). The running web and admin containers still hold the old value and work.
- Who uses sa today (sessions and `msdb.dbo.backupset`): web and admin (EF Core), the VS Code SQL extension, and a
  job outside SQL Server that runs the 15-minute **log backups as sa** (285 in 3 days; not a SQL Agent job). The
  Rust scraper and scoreboard run as `webscraper_ingest`.
- Decision (user): find the backup job and the Rust migration credentials on the host first, then rotate everything
  in one go: `ALTER LOGIN [sa]` to the `.env` value → `deploy.sh --env-change` → recreate `sql2025` with the new
  `MSSQL_SA_PASSWORD` (DEPLOY.md, SQL Server container) → user updates the backup job and the VS Code connection.
- Where to look on the host: `crontab -l` (root and the user), `systemctl list-timers`, and any script mentioning
  `webscraper-managed`, `BACKUP LOG` or `sqlcmd`.

## Update, November 5, 2026 ~05:20 UTC: sa rotated (user asked)

Done: `ALTER LOGIN [sa]` to the `.env` value (05:12:23 UTC), web/admin redeployed with `--env-change`, `sql2025`
recreated with the new `MSSQL_SA_PASSWORD`; the leaked value is refused. **Still on the old value until the user
updates them:** the 15-minute log-backup job (runs as sa) and the VS Code SQL connection. Rust migration credentials
still to check. Phase 4 is therefore done except for those; phases 0–3 (own logins for web/admin/tests) and 5 remain.
`sql2025-before-volume` (stopped) still holds the old password and pre-volume data: remove after a few days.
