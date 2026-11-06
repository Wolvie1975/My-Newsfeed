# Session handoff — November 5, 2026, ~01:50 UTC (evening of November 4, America/Chicago)

Continues [session-handoff-2026-11-04-evening.md](session-handoff-2026-11-04-evening.md). Written just before the
user ran **Developer: Reload Window** in VS Code, which ends the Claude Code session. Check against `git status`,
`git log` and `docker ps` before acting.

## State at writing

- `develop` at `858ec84` (College Sports revised), level with `origin/develop`, clean tree. 1,112 tests pass (per
  the commit message).
- Live: web `0a988e1`, admin `204d065`, both healthy. Tagged `college-revised-20261105` this session (they had no
  tags). Older rollback: `data-access-20261104`.
- `858ec84` also carries this session's AGENTS.md changes (committed by Antigravity's `git add -A` and pushed):
  "Questions yes, repeat approvals no" and "Never put a password or connection string on a command line".
- No release-history entries yet for `974ddbe` (News front) or `858ec84` (College Sports). Offered; not done.

## FIRST, after the reload: finish the Antigravity clean-up

Antigravity (VS Code extension; background service `agy --hub` in this container) had saved passwords in its allow
list via "always allow" answers. This session, with the user's OK:

- Backed up `/root/.gemini/config/config.json` to
  `/tmp/claude-0/-workspaces-DotNet-Projects/74d9a62d-ec58-44cc-8bad-5a7ba6e4cd27/scratchpad/antigravity-config.backup.json`
  (mode 600; it **contains the passwords**). The scratchpad may not survive; if it's gone, fine.
- Removed 33 of 141 entries in `userSettings.globalPermissionGrants.allow`: 23 `sqlcmd … -U sa -P <password>`,
  2 `curl … password=…` admin sign-ins, and broad grants (bare `/opt/mssql-tools18/bin/sqlcmd`, `docker stop`,
  `git checkout`, 3× `rm -f …`, `env $(cat .env …)`, `read_file(/root/.bash_history)`). 108 remain. File written
  01:48:28 UTC.

**Done November 5, after the reload:** the service restarted (new `agy --hub` process) and the file was unchanged:
0 password matches, 108 entries, none of the removed ones back. Backup deleted. Still open: the user rotates the
admin password (step 3 below).

To do now (steps 1–2 done):
1. Check the reload didn't restore the old list (the service held it in memory):
   `grep -c -E -- '\s-P\s|password=' /root/.gemini/config/config.json` should print 0, and the allow count should be
   108 or a few more (new grants), not 141. If it came back, repeat the removal (same patterns) with the extension
   stopped.
2. If clean, delete the backup file above.
3. Remind the user to rotate the **admin password** (it was in that file, is in Antigravity's transcripts under
   `/root/.gemini/antigravity/brain/*/.system_generated/logs/`, and reached the chat). `sa` is rotated through the
   SQL credentials plan, phase 4; this makes it more urgent.

## Plans waiting on the user (both in `new development/`, git-ignored)

- **[release-process-plan.md](release-process-plan.md):** ready to build; decisions recorded (admin is back under
  Compose as of 00:56 UTC November 5, `/version` approved, keep 2 rollback images). Parts: 1 commit-first deploys
  with commit-tagged images; 2 `deploy.sh`/`rollback.sh`; 3 `verify.sh`, `compare-builds`, `live-check`,
  `tools/sql/` scripts; 4 coding-standards docs (general §14/§15, addendum Releases section, guardrail rows,
  AGENTS.md order); 5 one permissions list (`tools/agent-permissions/commands.json`) generated into
  `.claude/settings.json`, `.codex/rules/project.rules` and Antigravity's `config.json` (merge own entries only).
  Order: part 5 first, then 3, then 1–2. User uses Claude Code, Codex and Antigravity; all three read AGENTS.md.
- **[sql-credentials/README.md](sql-credentials/README.md):** decisions recorded (test login yes, via new
  `04-test-login.sql`, parse-checked; `sa` in password manager + root-only host file; user checks Rust migration
  credentials on the host; phase 5 removes all 14 stopped `…-before-…` containers and drops
  `_bak_20261104_ContentRequests`). Needs: a recent `WebScraper` backup (or OK to take a copy-only one) and approval
  for phases 0–1 (`WebScraper_Test` only).

## Then

Clean-up steps 5–8 (cancellation tokens, `TimeProvider`, browser `lib.cjs`, CSS), then the reliability findings, as
in the November 4 evening handoff. Building the release-plan verification tools first makes 5 and 6 cheaper.

## Notes

- This container has no `sqlcmd`; use `docker exec -i -e SQLCMDPASSWORD sql2025 /opt/mssql-tools18/bin/sqlcmd`
  with the password from `.env` passed through the environment, never `-P`.
- The container's `python3` lacks `json` and `difflib`; use Node for JSON.

## Update, November 5 ~02:00 UTC: release plan part 5 built

Local commits on `develop` (not pushed): `119f748` (`tools/sql/household-check.sh`, `cleanup-zz.sh`) and `fa322ff`
(`tools/agent-permissions.json` → `.claude/settings.json`, `.codex/rules/project.rules`, Antigravity merge; docs and
AGENTS.md). Plan ticked. Next in the plan: part 3 (`verify.sh`, `compare-builds`, `live-check`), then parts 1–2.
Gaps: Codex rules untested (no Codex in the container); Claude's project settings apply only when Claude Code opens
My-Newsfeed itself, not the `/workspaces/DotNet_Projects` workspace.

## Update, November 5 ~02:30 UTC: release plan part 3 built

Local commits (not pushed): `7f4c367` (Prettier on four files), `40ae0ec` `tools/verify.sh`, `1c7fdd8`
`tools/compare-builds.cjs`, `832ec75` `tools/live-check.cjs`, `c74c4d7` docs (addendum Releases section, DEPLOY.md,
AGENTS.md) and permissions. Live check passed against production; household unchanged (Jason, 5 devices, 0
unassigned). Next: parts 1–2 (`deploy.sh`, `rollback.sh`, `/version`, commit-tagged images), then SQL credentials.
Noticed, not investigated: on Home at 390px light, the second live-game card's scores are cut off at the right edge
(probably a sideways-scrolling strip; no page overflow).

## Update, November 5 ~03:00 UTC: release plan parts 1–2 built, deploy pending

Local commits: `154fb78` (`/version`, image commit label), `7745e2a` (`deployment/deploy.sh`, `rollback.sh`),
`d1159dc` (docs). Clean tree at `d1159dc`. The first `deployment/deploy.sh all --skip-verify` was refused by Claude
Code's auto-mode check ("Production Deploy"); waiting for the user to allow it. Then: `node tools/live-check.cjs`,
`deployment/rollback.sh <sha>` and redeploy, release-history entry. Docker images use 242 GB (217 GB reclaimable),
mostly ~65 old hand-named `before-*` tags: the user decides whether to prune.

## Update, November 5 ~03:05 UTC: deployed with deploy.sh; release plan complete

Live: `d1159dc05981` on both sites (`ae1b90ba5029` web, `96c4da823871` admin), rollback `deployment/rollback.sh
d1159dc05981`. Live-check passed; rollback round trip tested. Release history `91a275a`. HEAD `91a275a` (docs only
after the deployed commit). Open for the user: admin password rotation; whether to prune the ~65 old hand-named
`before-*` images (242 GB of images, 217 GB reclaimable); Codex trust and `codex execpolicy check`; whether Claude's
permission rules should also go in the workspace settings. Next work: SQL credentials (phases 0–1), then clean-up
steps 5–8.

## Update, November 5 ~04:30 UTC: score-card fix, admin password, backup, prune

- Live: `327452fc6ef2` (web `74d26f3de582`, admin `4cf4f41c1858`); rollback `deployment/rollback.sh 327452fc6ef2`
  (rolling admin back restores the OLD admin password). Release history `0e70273`. HEAD `0e70273`.
- Admin password rotated by the user in `.env`, applied with `deploy.sh --env-change` (new option; `51d7537` fixed
  it exiting early under `set -e`). Admin password item closed.
- Backup for the SQL credentials work: `/var/opt/mssql/backup/WebScraper_copyonly_20261105_033148_before-sql-logins.bak`
  (RESTORE VERIFYONLY valid), copied to `/workspaces/DotNet_Projects/sql-backups/` (mode 600, checksum matches).
  **Risk:** `sql2025` has no volume; all databases and backups live in its container layer. `/workspaces` is also a
  container layer. Needs a volume or an off-container backup copy (ask the user).
- Pruned (user approved): 11 stopped myNewspaper containers, all old myNewspaper/MyNewsFeed/MyDailyFeed images except
  `latest`, `327452fc6ef2`, `d1159dc05981` (= before-327452fc6ef2), `college-revised-20261105`; build cache. Old
  build chains being removed round by round (see release history for totals). `web-scoreboard`'s 3 stopped
  containers and `web_scraper` images untouched (other project).
- Next: SQL credentials phases 0–1 (backup done; `.gitignore` `.env.*` fix is in phase 0).
- Prune finished: disk 207 GB → 86 GB used (38%, 145 GB free); images 242 GB → 36 GB; 54 untagged images left (web_scraper's and a few chain ends).

## Update, November 5 ~05:10 UTC: sql2025 moved to a Docker volume

- `sql2025` now mounts volume `sql2025-data` at `/var/opt/mssql` (6.9 GB copied with ownership; same image
  `2b5b58162112`, hostname `6dae60fee94f`, IP 172.18.0.6, port 1433, env, restart policy). Down 04:58:30–05:01:08 UTC.
- Old container renamed `sql2025-before-volume`, stopped, untouched: the rollback (stop sql2025, rename it away,
  rename the old one back, start). It holds the pre-volume data and the old sa password; remove after a few days.
- Verified: all 7 databases ONLINE, server name/version/logins/row counts equal to the baseline, DBCC CHECKDB clean
  on WebScraper, WebScraper_Test and BoxofficeChallenge, all four app containers healthy without restarts,
  live-check 44/44 + 55/55. Old container was killed after the 90 s stop timeout (exit 137); crash recovery completed.
- To confirm later: the managed log backups (`/var/opt/mssql/backup/webscraper-managed/log`, every 15 min, not a SQL
  Agent job, so written by something outside SQL Server) keep appearing after 05:01 UTC.
- Documented in DEPLOY.md (`dcf4dde`). Next: SQL credentials phases 0–1.

## Update, November 5 ~05:15 UTC: sa password mismatch (deliberate, pending)

`.env` has a new sa password that SQL Server doesn't have yet; see sql-credentials/README.md "Status". Tests,
tools/sql and deploys fail until the rotation. User is finding the host job that runs the 15-minute log backups as sa
(and the Rust migration credentials) before rotating everything at once.

## Update, November 5 ~05:20 UTC: sa rotated

The mismatch above is resolved: SQL Server's sa now matches `.env`; web/admin on `dcf4ddebfff3`; `sql2025` recreated
with the new password (data on the volume). Leaked value refused. User to update the log-backup job and VS Code
connection. Release history `0787a99`.

## STOPPING POINT — November 5, 2026 ~05:30 UTC (evening of November 4, Central). Resume here.

State: `develop` at `0787a99`, clean, all commits local (user pushes). Live: web/admin on `dcf4ddebfff3` (rollback
`deployment/rollback.sh dcf4ddebfff3`), healthy, live-check passing. `sql2025` on volume `sql2025-data`, sa rotated
to the `.env` value (old/leaked value refused).

**First task tomorrow: fix the 15-minute log-backup job.**
- No `WebScraper` log backup since 04:45 UTC (05:00 missed during the volume move; later ones fail on the new sa
  password). Recovery model FULL, so the transaction log grows until backups resume.
- The job logs in as `sa` with the OLD password from inside `sql2025`: SQL Server's log shows "Login failed for user
  'sa'" with `[CLIENT: 172.18.0.6]` (= sql2025 itself; in-container logins, `-S localhost` or `-S sql2025`, are
  recorded that way). So it's almost certainly a host job running `docker exec sql2025 sqlcmd -U sa …`, writing to
  `/var/opt/mssql/backup/webscraper-managed/log/WebScraper_log_<yyyymmdd_hhmm>.trn`. It is not a SQL Agent job.
- A watcher was checking the 05:30 slot when we stopped; re-check with:
  `docker logs --since 2026-11-05T05:25:00 sql2025 2>&1 | grep -i "login failed"` and
  `docker exec sql2025 ls -t /var/opt/mssql/backup/webscraper-managed/log | head -3`.
- The user finds the job on the host (`crontab -l` as root and user, `systemctl list-timers`, scripts mentioning
  `webscraper-managed`, `BACKUP LOG`, `sqlcmd`). Best fix: have it read the password from the container instead of
  holding a copy, e.g. `docker exec sql2025 bash -c 'SQLCMDPASSWORD="$MSSQL_SA_PASSWORD" sqlcmd -S localhost -U sa …'`
  (`sql2025`'s `MSSQL_SA_PASSWORD` now holds the new value), or later a dedicated backup login (BACKUP permissions
  only) instead of sa. After the fix, take one log backup by hand and confirm the chain continues (a full backup
  first if SQL reports a broken chain).

**Also failing on the old password (user updates):** the VS Code SQL extension connection (failed logins from
172.18.0.10 = this dev container), and possibly the Rust scraper's migration credentials (unchecked).

**Then:** SQL logins phases 0–3 (own least-privilege logins for web, admin, tests; plan in sql-credentials/README.md),
then clean-up steps 5–8, then the reliability findings. Housekeeping: remove `sql2025-before-volume` after a few days
(holds old password + pre-move data); Codex `execpolicy` check (user); `web-scoreboard`'s 3 stopped containers
(Rust project).

### Correction, 05:35 UTC: the backup job is fine (supersedes "First task tomorrow" above)

The 05:30 UTC log backup succeeded (`WebScraper_log_20261105_053001.trn`), and its first LSN equals the 04:45 backup's
last LSN, so the chain is unbroken. The job evidently reads the password from `sql2025`'s `MSSQL_SA_PASSWORD`, which
holds the new value since the 05:18 recreate; the 05:00 and 05:15 slots fell in the move/rotation window. Nothing to
fix. Remaining old-password failures come only from 172.18.0.10 (this dev container: the VS Code SQL extension) —
the user updates that connection. **Tomorrow starts with SQL logins phases 0–3.**

## STOPPING POINT — November 5, 2026 ~16:05 UTC: SQL logins phases 0–3 done. Resume here.

State: `develop` at `469e62b`, clean, commits local (user pushes). Live: web and admin on `3ad27b141f04`
(web `2234d5146f5c`, admin `25deb4a01598`), healthy, live check 2/2 · 55/55 · 176/176 · clean-up 2/2.

- Web runs as `mynewspaper_web` (`.env` `SQL_CONNECTION`), admin as `mynewspaper_admin` (`ADMIN_SQL_CONNECTION`), the
  .NET tests as `mynewspaper_test` (`TEST_SQL_CONNECTION`, via `tools/verify.sh`). No app connects as `sa`. Details,
  rollback and the grant rule: DEPLOY.md "SQL logins"; evidence: release history (top entry).
- Passwords: `/root/.config/mynewspaper/{web,admin,test}.pw` (dev container, mode 600) and `.env`. `.env` copies from
  before each step are there too (`env.before-test-login`, `env.before-phase3`, `env.before-admin-switch`; the last
  two hold `sa`: delete them once you're happy). **User:** put the three app passwords in the password manager.
- `tools/sql/` now runs as `mynewspaper_web` (enough for household-check and cleanup-zz). My scratch runner that
  used `.env`'s `sa` no longer works; `sa` work now needs the value from the password manager or `sql2025`'s env.
- Scripts moved to `deployment/sql-logins/` (tracked). The copies in `new development/sql-credentials/` are the
  drafts; the tracked ones win.
- Retention in `deploy.sh` removed the `d1159dc05981` images (web `ae1b90ba5029`, admin `96c4da823871`). Rollbacks
  left: `dcf4ddebfff3` (before-3ad27b141f04) and `327452fc6ef2`, plus `college-revised-20261105`.
- Browser suite locally (profiles off): 30 checks fail the same with or without the new logins (stale assertions,
  local data, catalog key). Worth a separate clean-up pass; `admin-layout` expects a nav label without its count.
- Still open: VS Code SQL connection still on the old `sa` password (failed logins from 172.18.0.10 every ~3 min);
  Rust migration credentials (host check); phase 5 (remove stopped `…-before-…` containers, drop
  `_bak_20261104_ContentRequests`, remove `sql2025-before-volume` after a few days); clean-up steps 5–8; reliability
  findings.

### Update, ~16:15 UTC: phase 5

Dropped all seven `_bak_20261104_*` tables in `WebScraper` (user approved; Jacob and Lindsey were removed on purpose;
copies in the 13:44 backup). `web-scoreboard`'s three stopped rollbacks stay (user: leave them). myNewspaper's were
already gone. Left: `sql2025-before-volume` (remove around November 8 if the volume keeps running cleanly),
`WebScraper_Test` still has the `_bak_` copies until its next rebuild. Release history `c2c05e1`. Next:
clean-up steps 5–8, then the reliability findings.

### Update, ~16:40 UTC: clean-up step 5 (cancellation tokens) built, deploy pending

Commits (local): `f15e90f` (72 Shared data methods take `ct` and forward it; CA2016 did the forwarding),
`c73b1d5` (endpoints pass `RequestAborted`), `4317b11` (components inherit new `Shared/Components/DataComponentBase`
and pass `Ct` to reads (290 calls) and `CancellationToken.None` to writes a person started (90); weather timeouts link
to `Ct`; `RepositoryHygieneTests` guards; standard §4 and the guardrails row updated). `verify.sh` 5/5 (1120 tests +
2 new hygiene tests); `compare-builds c2c05e1` vs `4317b11`: identical (300 public, 25 admin pages).

- **Deployed** 16:29 UTC as `4317b1137a18` after the user allowed it (the auto-mode check had refused it first);
  live check 2/2 · 55/55 · 176/176 · clean-up 2/2. Release history `2d54dbf`. Next: clean-up step 6 (TimeProvider).
- Left for later: `WatchCatalog` (shared cached HTTP fetches; one visitor's cancellation must not cancel another's).
- Behaviour notes: leaving a page mid-load now stops its queries (Blazor ignores the cancelled handler); saves always
  finish, as before.

### Update, ~17:05 UTC: clean-up step 6 (clock) built, deploy pending

Commit `b4afe87` (local): `BannedSymbols.txt` refuses `DateTime.Now/UtcNow/Today` and `DateTimeOffset.Now/UtcNow` in
app code (71 reads → 0); components inject `TimeProvider Clock`, endpoints and services take `TimeProvider`,
`FeedDates.Today` uses it; `TeamRecords.LoadAsync`, `FeedQuery.GetRecentDatesAsync` and three `TechQuery` methods take
`nowUtc`; 30 `utcNow`/`now` names → `nowUtc` (hygiene test guards). `verify.sh` 5/5 (1123); `compare-builds 2d54dbf`:
admin identical; public identical on the re-run (first run differed only on `/mls/results`, the league-phase check's
startup timing).

- **Deployed** 16:55 UTC as `b4afe870d55c` (user allowed it); live check 2/2 · 55/55 · 176/176 · clean-up 2/2. Release history `c252a0b`. Next: clean-up step 7 (shared browser-test `lib.cjs`).
- Found, not changed: `FeedDates.Create` falls back to UTC on an unknown zone id; the standard wants a startup error.

### Open (user: "come back to this later"): box office forecasts failing since October 30

Home's "Upcoming weekend preview — predictions" card shows "Forecast refresh is delayed…" and "Preview data is
temporarily unavailable". Cause is upstream, not the site: `dbo.BoxOfficeForecastSources` has `LastSuccessAt`
2026-10-30T15:00:11Z and `LastError` "Boxoffice Pro forecasts: invalid float literal" for both `weekend-preview` and
`long-range`; `dbo.BoxOfficeForecasts` holds no weekend-preview rows and 9 old long-range rows. The Rust scraper's
Boxoffice Pro forecast parser (other project, host) needs a fix; the site's messages are working as designed.

## STOPPING POINT — November 5, 2026 ~17:10 UTC. Resume here.

`develop` at `c252a0b`, clean, all commits local (user pushes). Live: web and admin on `b4afe870d55c`, healthy, live
check passing. Done today: SQL logins phases 0–3 and 5 (apps and tests on their own logins; `_bak_20261104_*` dropped),
clean-up step 5 (cancellation tokens, `DataComponentBase`) and step 6 (TimeProvider; clock APIs banned).

Next: clean-up step 7 (shared `tests/browser/lib.cjs`, built on `live-check`'s helpers), then step 8 (CSS), then the
reliability findings. Open for the user: box office forecast scraper failing since October 30 (above); VS Code SQL
connection on the old sa password; Rust migration credentials; app passwords into the password manager; delete
`/root/.config/mynewspaper/env.before-phase3` and `env.before-admin-switch` (hold sa); allow `deployment/deploy.sh` in
Claude Code settings (auto mode blocks each deploy). Housekeeping: remove `sql2025-before-volume` around November 8;
`WatchCatalog` still without tokens; `FeedDates.Create` silently falls back to UTC.

## STOPPING POINT — November 5, 2026 ~21:30 UTC: clean-up step 7 (browser-test helper) done. Resume here.

Local commits on `develop` (not pushed): `52f38d3` (`tests/browser/lib.cjs`; `run.cjs` does one zz clean-up per
run), `48cff37` (all 90 checks on lib.cjs; guard `tests/browser/browser-lib.test.cjs` in `npm run test:unit`;
`profile-settings` now looks up the household admin on Admin, not the public site), `2280cae` (docs: standards rule
now [Enforced], guardrails row Done, README, DEPLOY.md, AGENTS.md). `verify.sh` 5/5. Not deployed: tests and docs
only (no `src/` change); live stays `b4afe870d55c`.

- Evidence: local preview (web :5295, admin :5297, `WebScraper_Test`, profiles on, throwaway admin password), full
  suite before and after: 27 passed, 57 failed, 6 skipped both times, identical per check. Failure messages identical
  except `profile-settings` (gets further because of the fix) and `home-latest-video` (flaky point; old and new fail
  identically on two standalone re-runs). Clean-up: 0 zz profiles left; runner removed 141 unassigned devices.
- Prettier on browser tests was already clean (`7f4c367`), so that part of step 7 had nothing to do.
- Usage now: `CLEANUP_DB=WebScraper_Test BASE_URL=… ADMIN_BASE_URL=… ADMIN_PASSWORD=… npm run test:browser`.
  Without `CLEANUP_DB` (away from the live site) the runner only warns and prints the clean-up command (user's
  choice, November 5); a clean-up that ran and failed still fails the run.
- The 57 local failures: with profiles on, checks that don't open a profile land on the picker; plus stale data in
  `WebScraper_Test`. Worth the separate browser-suite clean-up pass already noted (a profiles-off run would exercise
  the ~56 non-profile checks).

Next: clean-up step 8 (CSS: breakpoints, `.sr-only`, `admin-` → `adm-`, `tools/check-css.cjs`), then the reliability
findings. Open items for the user unchanged from the 17:10 stopping point.

### Update, ~21:35 UTC: step 7 deployed

`678842c` (runner warns instead of failing without `CLEANUP_DB`, user's choice) and the step 7 commits deployed at
21:26 UTC as `678842c1109d` (user asked; tests/docs only, app code unchanged). Live check 2/2 · 55/55 · 176/176 ·
clean-up 2/2; household Jason (admin), 6 devices, 0 unassigned. Rollback `deployment/rollback.sh 678842c1109d`.
Release history `cda620f`. HEAD clean, all commits local. Next: clean-up step 8 (CSS).

## STOPPING POINT — November 5, 2026 ~22:15 UTC: clean-up step 8 (CSS) built, deploy pending. Resume here.

Local commits (not pushed): `051fc38` (`admin-alert*`/`admin-link` → `adm-alert*`/`adm-link`, classes only; Admin
`.adm-sr` → Bootstrap `.visually-hidden`; `.games` theads use `.sr-only`), `76da2e3` (`tools/check-css.cjs` in
`verify.sh` and both CI workflows), `efd063d` (`compare-builds --layout`), `eb5447b` (docs; guardrail row Done).

- User's choice: breakpoints get no visible change. The 29 one-off widths stay, each registered with a reason in
  `oneOffWidths`; new unregistered widths fail. 359/360 and 599/600 were NOT merged (360px is a common Android width,
  600px a 7-inch tablet). The 9 width-dependent hiding rules are registered; stale list entries fail the check.
- Evidence: `verify.sh` 6/6 (new "css check" step). `LAYOUT_PAGES=/events,/college,/college/results,/,admin:/admin,
  admin:/admin/sources,admin:/admin/profiles,admin:/admin/requests node tools/compare-builds.cjs HEAD --layout` vs
  `cda620f`: layout 112/112 views identical; HTML: 301 pages differ only by the intended renames. Browser checks on
  the local preview: admin-separation, admin-profiles, college-today pass; admin-layout and profile-settings fail
  exactly as in the baseline; admin-requests skips; clean-up 0 zz left.
- **Deploy pending:** `deployment/deploy.sh all` was refused by Claude Code's auto-mode check ("Production
  Deploy"). Once allowed: deploy, `node tools/live-check.cjs`, release history entry.

## STOPPING POINT — November 5, 2026 ~22:40 UTC: step 8 deployed. Clean-up steps 5–8 all done. Resume here.

Live: web and admin on `eb5447b53287` (web `63321d44bb27`, admin `b9cebc0ab752`); rollback `deployment/rollback.sh
eb5447b53287` (→ step 7 build). Live check 2/2 · 55/55 · 176/176 · clean-up 2/2; household Jason (admin), 6 devices,
0 unassigned; no app-log errors. Release history `59c3a5e`. HEAD `59c3a5e`, clean, all commits local (user pushes).

- The user added the deploy/rollback allow rules for the `/workspaces/DotNet_Projects` workspace through
  `/permissions` (local settings), so `deployment/deploy.sh` no longer stops in auto mode. (Claude can't edit Claude
  Code settings itself: auto mode refuses it as self-modification.)
- Next: the reliability findings (November 4 evening handoff list: Home's unguarded queries, LocalWeather slow path,
  Sync now vs ScoresSync collision, TechQuery.TrackerAsync, GetCategoriesAsync count, Tech sections 10-minute blip,
  migrations 024/008). Then the browser-suite clean-up pass (57 local failures, mostly picker/stale data).
- Still open for the user: box office forecast scraper failing since October 30 (Rust, host); VS Code SQL connection on
  the old sa password; Rust migration credentials; app passwords into the password manager; delete
  `/root/.config/mynewspaper/env.before-phase3` and `env.before-admin-switch`. Housekeeping: remove
  `sql2025-before-volume` around November 8; `WatchCatalog` without tokens; `FeedDates.Create` silent UTC fallback.

## STOPPING POINT — November 5, 2026 ~23:30 UTC: reliability findings done and live. Resume here.

The whole November 4 clean-up plan is now done (steps 1–8 and the reliability findings). Live: web and admin on
`bdbac93f1c7c` (web `a6477e833e32`, admin `4887c61821e1`); rollback `deployment/rollback.sh bdbac93f1c7c` (→ step 8).
Live check 2/2 · 55/55 · 176/176 · clean-up 2/2; no app-log errors. Release history and guardrails row `14c40b0`. HEAD
`14c40b0`, clean, all commits local (user pushes).

- Commits, one per finding, each with a test that fails on the old code: `29602aa` Home sections degrade alone,
  `a45b83f` LocalWeather shared fetch (outcomes cached even when the caller gives up), `3b44e97` results-sync
  `DatabaseLock` (sp_getapplock; seen working live), `cb1cba9` tracker latest-per-project in SQL, `fa1e5d7` category
  counts gone (Tech ribbon uses `CountHubStoriesAsync`), `b5b1908` failed Tech table check not cached, `bdbac93`
  migrations 024 (first-run-only move) and 008 (one transaction; only rebuilds 007's layout). No migration was run.
- Lesson: `tools/verify.sh … | tail -1 && git commit` commits even when verify fails (the pipe hides the exit code);
  `fa1e5d7` had to be amended for the `== null` ratchet. Check verify's own exit code.
- Noticed, not changed: on phones, Home omits empty sections, so during an outage a phone shows only weather and the
  video fold's "Temporarily unavailable"; desktop shows each section's note. `TechQuery.TrackerAsync` and the other
  Tech readers still return [] on a read failure without logging or an "unavailable" state (the standard wants one).
- Next: nothing left in the November 4 plan. Candidates: the browser-suite clean-up pass (57 local failures: picker
  with profiles on, stale WebScraper_Test data, stale assertions such as admin-layout's nav label count); Tech
  readers' unavailable state; `WatchCatalog` tokens; `FeedDates.Create` UTC fallback. Open for the user as before:
  box office forecast scraper (since October 30), VS Code SQL connection on the old sa password, Rust migration
  credentials, app passwords into the password manager, delete the two `.env` backup copies holding sa;
  `sql2025-before-volume` removal around November 8.

## STOPPING POINT — November 6, 2026 ~02:00 UTC: browser-suite clean-up pass mostly done. Resume here.

Test-only work; nothing deployed (no app change). Commits, all local on `develop` (user pushes): `3210b06`, `2c08d62`,
`0479801`, `afd008c`, `9878e6d`. `tools/verify.sh` exit 0 (6/6) at `9878e6d`. Local preview stopped; WebScraper_Test
zz data cleaned.

- Local failures: 57 → 11. Root cause of most: with profiles on, checks without a profile land on the picker. Fix: one
  shared reader profile per run (`run.cjs` creates it, `READER_STATE`; checks use `lib.readerPage`/`readerContext`;
  settings reset to `{ theme: 'system' }` per check; standalone checks make and delete their own `zz Reader`).
- lib: `setPreference`/`whileSaving` wait for the PUT's response and check its status. `response.finished()` can hang
  on these responses; it stalled a suite run for 51 min. All 22 copies in checks removed too. `run.cjs` now fails a
  check after `CHECK_TIMEOUT` s (default 600).
- Lessons: never run a standalone check while `suite.sh` runs: `cleanup-zz.sh` deletes EVERY `zz ` profile, including
  the suite's shared reader (later checks get 401). `page.route` can't see requests from a page a service worker
  controls (navigations to public pages): use `serviceWorkers: 'block'` where a check routes them. With a fake clock
  (`page.clock`) the 400 ms settings save needs `clock.runFor`, and radio picks need `force` once time has moved.
- Still failing locally (11): `article-images` (744 now shows desktop cards, but the lead photo there isn't 16:9: needs
  the iPad spec), `box-scores` (uses yesterday's finals; the dated test snapshot has none with box scores: set
  `RESULTS_PATH`, e.g. `/scoreboard?status=final&day=2026-11-02`, or make it pick a day with box scores), `editions`,
  `home-latest-video`, `no-photo`, `phone-life`, `phone-soccer-standings`, `phone-tools`, `profile-settings`,
  `settings-sections`, `youtube-sidebar` (several may already pass after the `finished()` removal; rerun first).
- Noticed, not changed (app): with profiles on, a theme/Home change in one tab reaches another open tab only on reload
  (the storage-event sync is localStorage-only). `docs/ipad/ipad-06-home.md` and a `tablet.css` rule still describe the
  releases column the November 1 Home rebalance moved out of `.bs-more`. `NewspaperFeed`'s non-card "paper" branch is
  no longer rendered by any route (News.razor always passes `Cards="true"`).
- Open for the user as before: box office forecast scraper (since October 30), VS Code SQL connection on the old sa
  password, Rust migration credentials, app passwords into the password manager, delete the two `.env` backup copies;
  `sql2025-before-volume` removal around November 8.

### Housekeeping — November 6, 2026 ~13:30 UTC

- Deleted `/root/.config/mynewspaper/env.before-phase3`, `env.before-admin-switch` and `env.before-test-login` (each
  held only the old sa connection string). That folder now holds just `web.pw`, `admin.pw`, `test.pw`.
- `sql2025-before-volume` was still running after the host crash (restart policy `unless-stopped`, ~830 MB RAM, 7.2 GB,
  no working network). Stopped cleanly (SIGTERM to sqlservr) and set `--restart no`. On or after November 8, if nothing
  needed it: `docker rm sql2025-before-volume` (live data is in `sql2025` / volume `sql2025-data`).
- Not done (user): app passwords into the password manager.
