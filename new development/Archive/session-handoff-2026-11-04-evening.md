# Session handoff — November 4, 2026, evening (America/Chicago)

This continues [session-handoff-2026-11-04.md](session-handoff-2026-11-04.md), the midday handoff. Its how-tos still
apply: the environment, reaching the live site over HTTPS, temporary `zz` profiles and cleanup SQL, the admin
container swap, tests and gotchas. Read that first, then this.

The facts below were written against `develop` at `7370fbf`, at about 2:45 PM Central. Before acting, check them
against `git status`, `git log` and `docker ps`.

**Commits:** all local, nothing pushed. The user pushes to GitHub themselves (AGENTS.md).

**Permissions:** the user allowed deploy and commit in the earlier session. Confirm that still applies in a new chat.
The auto-mode check refused production deploys ("Production Deploy") until the user said to retry, then let them
through.

## Where things stand

Clean-up steps 1–4 are done and live:

| Step | Commits | Live image tag | State |
| --- | --- | --- | --- |
| 2. Shared host setup (`AddMyNewspaperCore`) | `23c8eb1`, record `7c298a1` | `shared-setup-20261104` | Deployed, verified |
| 3. `is null`, `myNewspaper.Shared.*` namespaces, class renames | `ad70d35`, `707a6c4`, `4ad6b1a`, `2eace14`, record `c025a8d` | `cleanup3-20261104` | Deployed, verified |
| 4. Data access | `13ce9da`, `8b0a46a`, `285b027`, `1ac3b1b`, `aadf942`, `a1db369`, `1e844c0`, record `7370fbf` | `data-access-20261104` | Deployed, verified (currently live) |

- **Rollback for step 4:** images `mynewspaper-web:before-data-access-20261104` (`28fe86970c4d`) and
  `mynewspaper-admin:before-data-access-20261104` (`4de9de53f996`), plus the stopped container
  `mynewspaper-admin-before-data-access-20261104`.
- **Release evidence:** each step's evidence is in [docs/release-history.md](../docs/release-history.md).
- **Guardrails table:** the one in [docs/coding-standards.md](../docs/coding-standards.md) is up to date.
- **Build and tests:** the build has 0 warnings and all 1099 tests pass against `WebScraper_Test`.
- **Household after the last live check:** 1 profile (Jason, Id 219, household admin), 6 devices, 1 unassigned.

Also done this session:
- **Old databases:** the ten old validation databases were dropped (user approved). What remains is `BoxofficeChallenge`,
  `WebScraper`, `WebScraper_Test` and the system databases.
- **SQL credentials:** a rotation and least-privilege login plan is drafted for review. Nothing in it has run; see below.

### Guards added in steps 3 and 4 (a future change can trip these)

- **`RepositoryHygieneTests`, three checks:**
  - It allows at most 160 `== null` / `!= null` lines. These must be inside EF expression trees, where `is null` gives
    CS8122. If a new EF predicate needs one, raise the constant in the same change and say why.
  - It rejects classes named `…Repository`, `…Manager`, `…Helper(s)`, `…Data` or `…Util(s)`.
  - It fails on `db.<DbSet>` in any `.razor` file or in Web/Admin host code. It keys on the variable name `db`, so
    keep that name.
- **Analyzer rules:** `IDE0041` and `IDE0130` (namespace = assembly + folder) are at warning. CA1716 skips
  namespaces, because `Shared` is reserved only in Visual Basic.
- **`BannedSymbols.txt`:** BannedApiAnalyzers (RS0030) blocks every `ExecuteSqlRaw`, `SqlQueryRaw` and `FromSqlRaw`
  overload in the application projects. Test projects are exempt. Both Dockerfiles copy the file.
- **Data access pattern:** components create `db` and call `XxxQuery`/`XxxStore` methods. Admin reads go in
  `AdminQuery` and return records, not entities; admin writes go in `AdminStore`.

## Next steps (in order)

- [ ] **Step 5: cancellation tokens through the data layer.** `CA2016` is already enforced wherever a method takes a
  token. The work is adding `CancellationToken ct = default` as the last parameter of the roughly 60 read methods
  that lack it, passing it to `CreateDbContextAsync(ct)` and EF calls, and having endpoints and components pass the
  framework's token. Update the guardrails row "About 60 read methods with no `CancellationToken`".
- [ ] **Step 6: clock.** Inject `TimeProvider` at the edge and use one `DateTime nowUtc` parameter name. Ban
  `DateTime.Now`, `DateTime.UtcNow` and `DateTimeOffset.UtcNow` outside the edge by adding them to
  `BannedSymbols.txt`.
- [x] **Step 7: browser tests.** Done November 5, 2026 (`52f38d3`, `48cff37`, `2280cae`; see the 11-05 handoff). Add a shared `tests/browser/lib.cjs` covering BASE_URL, viewports (320/390/430, plus
  768/1440), themes, `SCREENSHOT_DIR` and `zz` cleanup in teardown. Add a check that fails on any `*.browser.cjs`
  that doesn't use it. Prettier already flags 7 existing browser-test files; fix those here.
- [x] **Step 8: CSS.** Built November 5, 2026 (`051fc38`, `76da2e3`, `efd063d`, `eb5447b`); breakpoints kept as they are (user: no visible change), each registered. See the 11-05 handoff. Use breakpoints 744/1024/1920/2560, one `.sr-only`, and `admin-` → `adm-`. Add a
  `tools/check-css.cjs` guard.
- [x] **Then the reliability findings:** Done and live November 5, 2026 (`bdbac93f1c7c`; one commit each, see the 11-05 handoff).
  - Home fails entirely if one of ~10 unguarded queries fails.
  - `LocalWeather`'s slow path adds ~3 s, and its failures aren't cached.
  - Admin "Sync now" and the background `ScoresSync` can collide on `UQ_ResultGames_Event`.
  - `TechQuery.TrackerAsync` loads every release.
  - `FeedQuery.GetCategoriesAsync` counts the whole Pages table.
  - One DB blip hides the Tech sections for 10 minutes.
  - Migration 024 re-applies its category move on re-run, and 008 claims a transaction it doesn't have.

For each step: one commit per concern, verify, deploy, run the live checks, add a release-history entry, and update
the guardrails table.

## Waiting on the user

- **SQL credentials:** the plan is in [sql-credentials/README.md](sql-credentials/README.md), with four scripts that
  were parse-checked only (nothing run). The README ends with four questions. Key facts:
  - The live `sa` password is still one of the values leaked in Git history.
  - `web-scraper` and `web-scoreboard` already run as `webscraper_ingest`.
  - Twelve stopped rollback containers hold the old `sa` connection string. This session added three more
    `…-before-…` admin containers (`shared-setup`, `cleanup3`, `data-access`), which also hold it.

## How to verify a no-behaviour-change step without deploying (used for steps 3 and 4)

Since November 5: `node tools/compare-builds.cjs <old-ref> [<new-ref>]` (new defaults to the working tree). It runs
both builds against `WebScraper_Test` (admin over HTTPS with a throwaway certificate), compares about 300 public
pages and every admin list and edit page after normalising volatile markup, and cleans up after itself. Writes are
still exercised by hand on the new build against the test copy, and restored.

## Deploy procedure used today

Since November 5, 2026: `deployment/deploy.sh` and `deployment/rollback.sh <sha>` do all of this (DEPLOY.md). Kept
for history:

1. Tag the running images as `before-<tag>`.
2. `docker compose build web admin`, then tag both `latest` images as `<tag>`.
3. Run `docker compose up -d --no-build web` and compare environment hashes before and after.
4. Swap the admin container with the procedure in the midday handoff, using `mynewspaper-admin:<tag>`.
5. Wait for both to report healthy.

**Live checks:** since November 5, `node tools/live-check.cjs` does the standard set below (except the flows in the
third bullet) and its own clean-up. Previously by hand:
- `/healthz` on both sites.
- About 19 public pages at 320/390/430/1440px in light and dark: no overflow, no script errors, no error banner.
- Where to watch search, TV search, an admin request (file one as the `zz` profile, then Check again and Test feed),
  admin HTTPS sign-in with the Secure cookie.
- Every admin route at 390 and 1440px.
- Delete the profile through Admin, then remove unassigned devices created since the recorded start time.

## Known issues added this session

- **Video edit overflow:** Admin · YouTube video edit overflows 55px at 390px for video 36379 (long description).
  The pre-step-4 build does the same.
- **`dotnet format --verify-no-changes` exits 2:** it reports CS8618 on `WebScraperContext` DbSets because it
  compiles without EF's suppressor. The build has none, so treat that as noise; any other finding is real.
