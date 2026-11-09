# Session handoff — November 4, 2026 (America/Chicago)

Written against `develop` at `23c8eb1`, which matches `origin/develop` (everything is pushed). Verify the current
repository state, `git log` and the running containers before acting; the facts below describe 12:00 PM Central.

Read first: [AGENTS.md](../AGENTS.md) (mobile checks, commit rules, browser-test cleanup, coding standards),
[docs/dotnet-coding-standard.md](../docs/dotnet-coding-standard.md) and
[docs/coding-standards.md](../docs/coding-standards.md) (the "Guardrails against past drift" table is the clean-up
tracker), [DEPLOY.md](../DEPLOY.md), and the two November 4 entries at the top of
[docs/release-history.md](../docs/release-history.md).

The user gave standing permission to **deploy and commit** for the rest of the earlier session. Confirm that still
applies in a new chat before deploying.

## Where things stand

| Item | State |
| --- | --- |
| Bootstrap subset on the public site (`dad7e2e`) | Deployed and verified live (72/72 page checks) |
| .NET coding standard + addendum (`bd0eb89`) | Committed; AGENTS.md requires both |
| Security quick fixes (`6121778`) and picker error banner (`3e97b9d`) | Deployed and verified live, public and admin |
| Clean-up step 1: test database and credentials out of tests (`c0fcdae`) | Committed; tests/docs only, nothing to deploy |
| Clean-up step 2: shared host setup, `AddMyNewspaperCore()` (`23c8eb1`) | Committed, all 1096 tests pass, **images built but NOT deployed** |

Live containers right now: `mynewsfeed-web` on the security release image (`412a0843967d`), `mynewspaper-admin` on
`mynewspaper-admin:security-20261104` (`d0b61c5f99ba`).

## Next steps (in order)

- [x] **Deploy step 2.** Done 1:12 PM Central; live checks passed (see release history). Images are already built and tagged:
  - `mynewspaper-web:shared-setup-20261104` (`4f0ab5370963`), rollback `mynewspaper-web:before-shared-setup-20261104` (`412a0843967d`)
  - `mynewspaper-admin:shared-setup-20261104` (`d2fdd8417a3e`), rollback `mynewspaper-admin:before-shared-setup-20261104` (`d0b61c5f99ba`)
  - If anything was committed since `23c8eb1`, rebuild instead of reusing these images.
  - Public: `docker compose up -d --no-build web` (web is Compose-managed; settings match `.env`).
  - Admin: **not** Compose-managed, and has a setting Compose lacks (`Results__SyncIntervalHours`). Use the
    container-swap procedure below.
  - Live checks: `/healthz` on both; key public pages; admin login over HTTPS with the Secure session cookie; one
    outbound feature per shared client if practical (Where to watch search, TV search, request checker in admin).
- [x] Add a release-history entry for step 2 and commit it.
- [x] **Clean-up step 3**, committed (local), one commit per concern: `ad70d35` is null (95 lines; 158 EF
  expression-tree lines must keep `== null` because of CS8122, ratcheted by `RepositoryHygieneTests`), `707a6c4`
  namespaces (`myNewspaper.Shared.*`, `IDE0130` at warning, CA1716 skips namespaces), `4ad6b1a` class renames
  (`BoxScoreQuery`, `ScoreboardQuery`, `AdminStore`, `FollowStore`, `ProfilePageSettings`, plus a naming guard),
  `2eace14` using order. Build 0 warnings; 1098 tests pass.
  - [x] **Deploy step 3.** First attempt was denied by the auto-mode check; deployed at about 1:40 PM Central after
    the user re-confirmed, live checks passed (release history). Images are built and tagged:
    `mynewspaper-web:cleanup3-20261104` (`28fe86970c4d`), rollback `mynewspaper-web:before-cleanup3-20261104`
    (`4f0ab5370963`); `mynewspaper-admin:cleanup3-20261104` (`4de9de53f996`), rollback
    `mynewspaper-admin:before-cleanup3-20261104` (`d2fdd8417a3e`). Web: `docker image tag
    mynewspaper-web:cleanup3-20261104 mynewspaper-web:latest && docker compose up -d --no-build web`; admin: container
    swap with `T=cleanup3-20261104`. Then the same live checks as step 2 and a release-history entry.
  - Prettier reports 7 browser-test files (`tests/browser/*.cjs`) with existing style issues, untouched this session.
- [x] **Step 4 (data access)**, committed (local), not yet deployed: `13ce9da` interpolated SQL only + BannedApiAnalyzers
  (`BannedSymbols.txt`, app projects; Dockerfiles copy it); `8b0a46a` Web inline queries; `285b027`, `1ac3b1b`,
  `aadf942`, `a1db369` Admin inline queries by area (new `AdminQuery`, records not entities; writes in `AdminStore`);
  `1e844c0` static `BoxScoreQuery`/`ScoreboardQuery` + hygiene test against `db.<DbSet>` in components. Build 0
  warnings; 1099 tests pass. The `== null` ratchet is now 160 (TV series projection).
  - Verification used, reusable: old and new builds run side by side against `WebScraper_Test` (Web on 5401/5402 with
    `BackgroundJobs__Enabled=false`, Profiles off; Admin on 5411/5412 over HTTPS with a throwaway self-signed cert
    and a local-only `Admin__Password`), rendered HTML compared after normalising tokens, Blazor state/element ids,
    the server port and countdown attributes. Public: ~250 renders identical. Admin: every list/edit page identical,
    with a seeded `zz` profile, two requests and a lock for the profile/request pages (deleted afterwards). Admin
    writes exercised on the new build: category add/duplicate/rename/delete, source toggle/save/duplicate URL, page
    save, feed save, video feed save, event type save, Results follow toggle and duplicate follow; each restored.
    The test copy matched live for sources afterwards. Scripts are in the session scratchpad (not kept).
  - [x] **Deploy step 4.** Deployed as `data-access-20261104` after the user asked to retry; live checks passed (see
    release history).
  - Step 5 note: CA2016 is already enforced where a method takes a token; the work is adding `ct` parameters.
- [ ] Steps 5–8 from the plan: cancellation tokens through the data layer (`CA2016`), clock via `TimeProvider` with one
  `nowUtc` name, shared browser-test helper `tests/browser/lib.cjs`, CSS (breakpoints 744/1024/1920/2560, one
  `.sr-only`, `admin-` → `adm-`, plus a `tools/check-css.cjs` guard). Update the guardrails table status as each lands.
- [ ] Then the reliability findings from the review: Home page fails entirely if one of ~10 unguarded queries fails
  (`Home.razor`); `LocalWeather` slow-path adds ~3 s and failures aren't cached; Admin "Sync now" vs background
  `ScoresSync` can collide on `UQ_ResultGames_Event` and abort the run; `TechQuery.TrackerAsync` loads all releases;
  `FeedQuery.GetCategoriesAsync` counts the whole Pages table for pages that don't use the counts; one DB blip hides
  Tech sections for 10 minutes. Migration notes: 024 re-applies its category move on re-run; 008 claims a
  transaction it doesn't have.

## Waiting on the user

- ~~Drop ten old databases~~: done November 4 at about 1:20 PM Central (user approved). Remaining: `BoxofficeChallenge`,
  `WebScraper`, `WebScraper_Test` and the system databases.
- **SQL credentials:** rotate the `sa` password (it is in Git history until October 25) and give the web and admin
  apps their own least-privilege logins. Touches `.env`, both app containers, user secrets, and the scraper and
  scoreboard containers in the separate Rss-Rust-Scrape project. Prepare scripts and a rollout plan for review
  before changing anything. **Drafted** in `new development/sql-credentials/` (README with findings, phases and four
  questions; scripts parse-checked only, nothing run). Key facts: the live `sa` password is still a leaked value;
  the scraper and scoreboard already run as `webscraper_ingest`, not `sa`.
- **Permissions:** the auto-mode check sometimes errors (no verdict) for a few minutes; retrying later works. The user
  may add allow rules in `.claude/settings.json` for `dotnet build/test`, `git add/commit` and `docker compose`.

## How-tos learned this session

**Environment.** This dev container is on the production Docker host (socket at `/var/run/docker.sock`). The Docker
CLI, Compose and Buildx were installed from Ubuntu packages, and Playwright's Chromium plus its system libraries were
installed. `sqlcmd` and `docker exec` into SQL aren't needed: a tiny .NET file-based script with
`#:package Microsoft.Data.SqlClient@6.1.6` that reads `SQL_CONNECTION` from `.env` (strip the quotes) and splits on
`GO` worked well. Never print the connection string or passwords.

**Reaching the live site from here.** `http://mynewsfeed-web:8080` and `http://mynewspaper-admin:8080` on
`dev-bridge`. For HTTPS (needed for the admin Secure cookie), map the tailnet hostname to the Tailscale IP:
`docker exec tailscale tailscale ip -4` and `docker exec tailscale tailscale serve status`, then
`curl --resolve host:443:IP` or Chromium `--host-resolver-rules=MAP host IP`. Playwright's `APIRequestContext` ignores
that mapping; send POSTs from the page with `fetch` instead.

**Profiles are on in production.** Every page redirects to the picker without a profile. For live checks create a
temporary `zz …` profile through `/editions/new` (wait for the redirect before reading cookies), and afterwards delete
it and every unassigned device created since a recorded start time (AGENTS.md). Any `curl` with
`Accept: text/html` also creates a device row. Since November 5: `tools/sql/cleanup-zz.sh <start-utc> --apply` (and
`tools/sql/household-check.sh`). Clean-up used before that:

```sql
-- record first: SELECT CONVERT(varchar(33), SYSUTCDATETIME(), 126);
SET XACT_ABORT ON; BEGIN TRANSACTION;
DECLARE @start datetime2 = '<recorded start>';
DELETE FROM dbo.MyDailyFeedProfileDevices WHERE ProfileId IN (SELECT Id FROM dbo.MyDailyFeedProfiles WHERE Name LIKE N'zz %');
DELETE FROM dbo.MyDailyFeedProfiles WHERE Name LIKE N'zz %';
DELETE FROM dbo.MyDailyFeedProfileDevices WHERE ProfileId IS NULL AND CreatedAt >= @start;
COMMIT TRANSACTION;
```

Expected household afterwards: 1 profile (Jason, Id 219, household admin), 6 devices, 1 unassigned (a real iPhone).

**Admin container swap: retired November 5, 2026.** Admin runs under Compose; deploy both sites with
`deployment/deploy.sh` and roll back with `deployment/rollback.sh <sha>` (DEPLOY.md). Kept for history:

```bash
T=<release-tag>; S=<private temp dir>; umask 077
hc=$(docker inspect mynewspaper-admin --format '{{index .Config.Healthcheck.Test 1}}')
docker inspect mynewspaper-admin --format '{{range .Config.Env}}{{println .}}{{end}}' \
  | grep -v -E '^(PATH|DOTNET_VERSION|ASPNET_VERSION|APP_UID|DOTNET_RUNNING_IN_CONTAINER|ASPNETCORE_HTTP_PORTS)=' | grep -v '^$' > $S/admin.env
docker stop mynewspaper-admin && docker rename mynewspaper-admin mynewspaper-admin-before-$T
docker run -d --name mynewspaper-admin --restart unless-stopped -p 8086:8080 --network dev-bridge --network-alias admin \
  -v mynewsfeed_mynewspaper-admin-keys:/keys --env-file $S/admin.env \
  --health-cmd "$hc" --health-interval 30s --health-timeout 5s --health-start-period 20s --health-retries 3 mynewspaper-admin:$T
rm -f $S/admin.env   # then wait for healthy; compare env hashes old vs new; on failure rename the old one back and start it
```

**Tests.** Integration tests now always use `WebScraper_Test` (refuse `WebScraper`); run them with
`ConnectionStrings__DefaultConnection` set from `.env`. Rebuild the copy with
`deployment/create-test-database.sql` (then delete the `.bak` it leaves in `/var/opt/mssql/data`).
`RepositoryHygieneTests` fails on any committed `Password=<value>`, including in comments.

**Gotchas.** `pkill -f` with a pattern that appears in your own command line kills your shell (exit 144); kill by
PID. With profiles off, the local site has no `/editions`, `/requests` or `/news/topics/follow`. `SecurePolicy.Always`
on the admin **antiforgery** cookie breaks the login page behind the proxy; only the session cookie is Secure.

## Smaller known issues (not started)

- Admin · YouTube video edit overflows 55px at 390px for video 36379 (long description); the previous build does
  the same, so it predates step 4. Probably an unbroken string in the description block.

- `SeriesFollowForm` (shown in admin and with profiles off): Follow buttons off-screen at 320px, white table in dark
  mode, near-invisible TVmaze credit in dark, low-contrast "Back to tracked series" in light.
- `public.js:127` listens for `enhancedload` on `document`; Blazor raises it on `Blazor.addEventListener`. Unverified.
- Admin login has no rate limiting; security headers (CSP, nosniff, framing) are missing on both sites; profile
  lockout can be triggered by anyone on the tailnet; the SSRF guard misses some uncommon IPv6 forms.
- Docs drift (from the audit): two date-stamp styles, duplicated handoffs, `docs/tech` number collision,
  `chat-handoff.md` overlapping `release-history.md`, links into ignored `new development/`.
