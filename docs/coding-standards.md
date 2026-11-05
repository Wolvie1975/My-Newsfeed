# My-Newsfeed coding standards

The general rules are in the [.NET coding standard](dotnet-coding-standard.md), and section numbers below (§2, §6 …)
refer to it. This addendum records what is specific to this repository. Where
the two disagree, this file wins here. Process rules (mobile verification, commits, browser-test clean-up) stay in
[AGENTS.md](../AGENTS.md).

Drafted November 4, 2026 (America/Chicago). **[Enforced]** = the build or lint fails; **[Review]** = checked by the
reviewer. Existing code is being brought into line in the clean-up steps listed under
[Guardrails against past drift](#guardrails-against-past-drift); each step is its own commit and adds its guard.

## Solution shape

| Project | Role | Rendering |
| --- | --- | --- |
| `src/myNewspaper.Web` | Public site | Static SSR. Interactivity only where a component opts in (today: `SeriesFollowForm` on `/tv/track`). |
| `src/myNewspaper.Admin` | Admin site, separate container | `@rendermode InteractiveServer` on admin pages. |
| `src/myNewspaper.Shared` | Data model, queries, stores, profiles, endpoints helpers, shared components and `wwwroot` | Used by both hosts. |

- **[Review]** Code used by both hosts goes in Shared. Host-only code stays in its host. Registrations that both
  hosts need go in `AddMyNewspaperCore()` (`src/myNewspaper.Shared/Hosting/CoreServices.cs`); each `Program.cs`
  adds only what is specific to that host. Per-host wording (outbound User-Agents) is passed in as
  `CoreHostLabels`, not copied.
- **[Enforced]** Namespaces are the project's assembly name plus the folder path (`IDE0130`): Shared code is
  `myNewspaper.Shared.<Folder>`, public-site code `myNewspaper.Web.<Folder>`, admin code `myNewspaper.Admin.<Folder>`.
  CA1716 doesn't check namespaces, because `Shared` is reserved only in Visual Basic.

## Data

- **[Review]** `WebScraperContext` covers two kinds of tables:
  - **Scraper-owned** tables (articles, sources, sports, movies, TV, YouTube): the separate Rust scraper writes
    them. This app reads them and writes only the columns documented as web-owned (for example
    `YoutubeVideoFeed.IsHouseholdPick`, `SourceCategories.NewsHub`).
  - **App-owned** tables are prefixed `MyDailyFeed` (profiles, devices, follows, topics, saved lists, requests,
    household). The app creates them in `db/` migrations.
- **[Enforced]** Components inject `IDbContextFactory<WebScraperContext>` as `Factory`, create the context as `db`
  and call `XxxQuery`/`XxxStore` methods, which return records rather than entities. Admin reads are in `AdminQuery`
  and writes in `AdminStore`. `RepositoryHygieneTests` fails on `db.<DbSet>` in any `.razor` file or in Web/Admin host
  code; it reads the DbSet names from `WebScraperContext` and keys on the `db` name, so keep that name.
- **[Enforced]** Raw SQL goes through `FromSql`, `SqlQuery<T>` or `ExecuteSql(Async)` with an interpolated string. The
  `*Raw` overloads are in [BannedSymbols.txt](../BannedSymbols.txt) (RS0030) for application projects; test fixtures
  may run SQL text. New banned APIs go in that file with a message saying what to use instead.
- **[Review]** Read failures are caught with `DataFailures.IsReadFailure(ex)`, logged, and shown as an
  "unavailable" state. Don't add `catch (DbException)` or unfiltered catches.
- **[Review]** Display dates come from the injected `FeedDates` (configured by `Display:TimeZone`). Feature code
  doesn't convert time zones itself.
- **[Review]** The current household profile is the scoped `ProfileContext`. Profile data is always filtered by
  the server-side profile id, never an id from the URL or form.
- **[Review]** Admin endpoints bind `RequestDb` (context, clock and token together). JSON APIs use the shared
  `JsonApi.Filter`, which also checks antiforgery.

## Migrations

- **[Review]** New scripts follow the template of `db/018`–`db/024` (see the general standard, section 13): header,
  `XACT_ABORT` + one transaction, `THROW` for prerequisites, a guard on every change. Scripts `001`–`013` predate
  this and are run-once only; don't copy their style.
- **[Review]** Every new script gets a row in the [DEPLOY.md](../DEPLOY.md) migration table in the same commit,
  saying when it must run relative to the web build that needs it.

## Blazor and markup

- **[Review]** Public pages work without JavaScript: navigation is links and GET forms; state changes are POST
  forms with `<AntiforgeryToken />`. Product direction is "tap to link, not typing"
  ([interface guide](interface.md#standing-product-guideline-tap-to-link)); typed input needs an approved exception.
- **[Review]** Phone and desktop layouts: pages render both halves (`mx-phone` / `mx-desktop`), and the tablet and
  mobile stylesheets switch between them. New pages follow the same split instead of inventing another.

## CSS

- **[Review]** Public styles are scoped under the `.pub` root class and live in the global stylesheets in
  `src/myNewspaper.Shared/wwwroot/public.css` and `src/myNewspaper.Web/wwwroot/*.css`. Scoped `.razor.css` only for
  layout components.
- **[Review]** Breakpoints: `max-width: 1023px` / `min-width: 1024px` (phone and tablet vs desktop),
  `(min-width: 744px) and (max-width: 1023px)` (tablet), `min-width: 1920px` (wide) and `min-width: 2560px` (4K).
  Anything else needs a comment explaining why. Today's CSS has about 26 other one-off widths; fold them into these
  when a rule is next edited.
- **[Review]** Colors come from the tokens on `:root`/`.pub` (`--text`, `--surface`, …). Dark mode and colour schemes
  are `:root[data-theme='dark']` and `:root[data-color-scheme='…']`, set before first paint by `theme-init.js`.
  Don't add `prefers-color-scheme` media queries.
- **[Review]** Visually hidden text uses `.sr-only`. Don't copy the clip pattern into new rules, and don't use
  Bootstrap's `.visually-hidden` on public pages.
- **[Review]** Touch targets are at least 44px (AGENTS.md); use `min-height: 44px`, until a shared token replaces it.
- **[Review]** Bootstrap: the **admin** site loads full Bootstrap 5 and may use its classes. The **public** site
  loads only `lib/bootstrap/bootstrap-public-subset.css` (root variables, base reset, and the classes used by
  `SeriesFollowForm` and the error page). Don't add Bootstrap classes to public markup. If a shared component needs
  one, regenerate the subset and record it there.
- **[Review]** Class prefixes: each new feature picks a new short prefix, uses BEM (`xx-block__element--modifier`)
  with `is-`/`has-` state classes, and adds itself to this table. The prefix must not collide with Bootstrap
  utilities on admin pages (`d-`, `p-`, `m-`, `g-`, `text-`, `bg-` …).

| Prefix | Meaning or main users |
| --- | --- |
| `pub` | Public root scope |
| `mx-` | Phone layout (`mobile.css`) |
| `d-` | Desktop cards and layout |
| `bs-` | Broadsheet Home (`broadsheet.css`) |
| `tab-` | Tablet layout (`tablet.css`) |
| `sb-` | Scoreboard |
| `td-`, `tc-`, `tw-`, `spf-` | Tech pages (desk, status/releases, week, front) |
| `ef-` | Section-front shared pieces (Videos, TV, Topic, Entertainment) |
| `hc-` | Headline cards and news feeds |
| `nf-` | Watch / where-to-watch |
| `yt-` | YouTube / Videos |
| `tp-` | Topics |
| `mv-` | Movies, standings and rankings tables |
| `set-` | Settings |
| `bo-` | Box office |
| `tv-`, `tvc-` | TV |
| `dg-` | Digital releases |
| `rk-` | Rankings |
| `adm-` | Admin (use this, not `admin-`, for new admin styles) |
| `cv-` | Most covered lists (News, Tech) |
| `nw-` | News front additions: trending, cascade |

## JavaScript

- **[Enforced]** ESLint and Prettier on `src/*/wwwroot/*.js`, `*.razor.js` and `tests/browser/*.cjs`.
- **[Review]** Feature scripts are classic scripts: an IIFE with `'use strict'`, loaded with `defer`, one file per
  feature. They hook markup through `data-*` attributes and re-run after enhanced navigation with
  `Blazor.addEventListener('enhancedload', …)` (not `document.addEventListener`).
- **[Review]** `localStorage` keys are `mynewsfeed-<feature>-v<N>`. Older `mydailyfeed-*`, `theme` and
  `saved-articles` keys stay readable for existing browsers until a migration step moves them.
- **[Review]** Don't build markup from text with `innerHTML`; copy server-rendered nodes or use DOM APIs.

## Tests

- **[Review]** C# integration tests take their connection string only from `TestDatabase` and run against the
  `WebScraper_Test` database, never the live `WebScraper` database. No connection strings in test files.
- **[Review]** Browser checks (`tests/browser/*.browser.cjs`) use the shared helper for `BASE_URL` (default
  `http://localhost:5295`), viewports (at least 320, 390 and 430, plus 768 and 1440 when layout changes), light and
  dark themes, screenshots under `SCREENSHOT_DIR`, and clean-up of temporary `zz ` profiles and unassigned devices
  (AGENTS.md). Until that helper exists, new checks copy the conventions from the most recent check rather than
  older ones.

## Documentation

- **[Review]** Dates are "Month D, YYYY (America/Chicago)". Release notes go in
  [release-history.md](release-history.md), newest first. Handoffs live in `docs/` and are not linked from
  `new development/`, which is ignored by git.

## Releases

AGENTS.md sets the cadence; these are the tools for each step. They live in the repository, not in a session
scratchpad, so a check is a command, not instructions for rebuilding it.

- **[Enforced]** `tools/verify.sh` before every commit: build with 0 warnings, all .NET tests against
  `WebScraper_Test`, JS unit tests, lint and format check, with one pass/fail summary (`--quick` for build, JS unit
  and hygiene tests). A change isn't committed while it fails.
- **[Review]** A step that claims no behaviour change proves it with `node tools/compare-builds.cjs <old-ref>
  [<new-ref>]`: both builds side by side against `WebScraper_Test`, about 300 public pages and every admin list and
  edit page compared after normalising volatile markup. Record the counts in release history.
- **[Review]** After every deploy, `node tools/live-check.cjs`: health, admin HTTPS sign-in, about 22 public pages at
  320/390/430/1440px in light and dark, every admin route and edit page at 390 and 1440px, then clean-up through
  Admin and `tools/sql/cleanup-zz.sh`. It is the standard live part of mobile verification; flows a change adds (a
  form, a dialog) still get their own checks. Paste `test-results/live/summary.md` into the release-history entry.
- **[Review]** A known failure goes in `live-check.cjs`'s `known` list with the reason and the date it was found, so
  it stays visible without failing every run. Remove it when fixed.

## Agents and database tools

- **[Enforced]** Coding agents (Claude Code, Codex, Antigravity) get their command permissions from one list,
  [tools/agent-permissions.json](../tools/agent-permissions.json): allow, ask or deny, by command prefix. Run
  `node tools/agent-permissions.cjs` to regenerate `.claude/settings.json` and `.codex/rules/project.rules`, and add
  `--antigravity` to merge the allow list into this machine's Antigravity (it has no project file). Never edit a
  generated file; `npm run test:unit` fails if one is out of date.
- **[Review]** Pre-approve a routine command by adding it to the list, not by answering "always allow" in one tool.
  Nothing destructive goes on the allow list, and raw `sqlcmd` stays off it: the risk is in the SQL, not the command.
- **[Review]** Routine database work goes through [tools/sql/](../tools/sql/): `household-check.sh` (read-only) and
  `cleanup-zz.sh <start-utc>` (dry run unless `--apply`). Scripts read the connection from `.env` and pass the
  password as `SQLCMDPASSWORD`. A password never appears on a command line (AGENTS.md). It can't be automated
  because agents compose commands at run time.

## Guardrails against past drift

The November 4, 2026 audit found the problems below. Each has a rule above, a clean-up step, and a guard so it can't
quietly come back. Update the **Status** column as each guard lands, and don't remove a guard without replacing it.

| Problem found | Rule | Guard that stops it recurring | Status |
| --- | --- | --- | --- |
| The same data job done four ways: inline LINQ in about 25 components; `SqlQueryRaw`, `ExecuteSqlRaw`, `DbCommand` and interpolated SQL side by side | Data section above; general §6 | Banned-API list blocks the `*Raw` APIs. A check script fails on `db.<Table>` LINQ inside `.razor` files. Remaining `DbCommand` uses each carry a "why" comment. | Done November 4, 2026: `BannedSymbols.txt` (RS0030, application projects) refuses every `ExecuteSqlRaw`, `SqlQueryRaw` and `FromSqlRaw` overload, and the seven uses became `SqlQuery`/`ExecuteSqlAsync`; the box office reads moved from `DbCommand` to `SqlQuery`, and `SharedArticles` keeps its `DbCommand` with the reason; the inline queries in 9 Web and 20 Admin files moved to query/store classes (new `AdminQuery`), and `BoxScoreQuery`/`ScoreboardQuery` became static; `RepositoryHygieneTests` fails on `db.<DbSet>` in components and host code. The check is a hygiene test rather than a script |
| About 60 read methods with no `CancellationToken`; `CreateDbContextAsync()` without a token | General §4 | `CA2016` at `warning`; review checklist | Planned (step 5) |
| Clock read three ways inside queries; `nowUtc`/`utcNow`/`now` parameter names | General §4 | Banned-API list blocks `DateTime.Now`, `DateTime.UtcNow`, `DateTimeOffset.UtcNow` outside the edge | Planned (step 6) |
| Shared code in `myNewspaper.Web.*` with `RootNamespace` `myNewspaper.Common` | General §2; Solution shape above | `IDE0130` at `warning`; `RootNamespace` matches the assembly | Done November 4, 2026: Shared is `myNewspaper.Shared.*` (and `myNewspaper.Hosting` became `myNewspaper.Shared.Hosting`), its `RootNamespace` is `myNewspaper.Shared`, and Admin's endpoints and tests moved out of `myNewspaper.Web.Endpoints`/`myNewspaper.Tests` |
| `Web` and `Admin` `Program.cs` duplicating about 60 lines that had drifted | Solution shape above; general §5 | One `AddMyNewspaperCore()` in Shared (`Hosting/CoreServices.cs`); reviewers reject copied registrations | Done November 4, 2026: both hosts' registrations and outbound client settings compared identical before and after, apart from Admin's duplicate `TimeProvider` registration now removed |
| 35 breakpoint widths, an `admin-`/`adm-` split, three visually-hidden patterns | CSS section above | `tools/check-css.cjs` (run by CI) fails on unregistered widths, unregistered prefixes and copies of the clip pattern | Planned (step 8) |
| `sa` password hard-coded in 10 test files; integration tests writing to the live database | Tests section above; general §12 | `RepositoryHygieneTests` fails on a literal `Password=`/`Pwd=` in any tracked file; `TestDatabase` always targets `WebScraper_Test` and refuses `WebScraper`; [create-test-database.sql](../deployment/create-test-database.sql) rebuilds the copy | Done November 4, 2026 for the tests. Still open: rotate the `sa` password (in Git history until October 25) and give the apps their own least-privilege SQL logins |
| 89 browser checks with 10 viewport sets, several base-URL defaults (one production), and copied clean-up code | Tests section above; AGENTS.md | Shared `tests/browser/lib.cjs`; the check script fails on any `*.browser.cjs` that doesn't use it | Planned (step 7) |
| CI ran only 4 of about 81 test classes and no lint or format checks | General §1 | Both workflows run the full suite (with a SQL service container), `dotnet format --verify-no-changes`, `npm run lint`, `npm run format:check` and the check script | Planned |
| `== null` and `is null` mixed | General §3 | `IDE0041` and the `is null` preference at `warning` (catches `ReferenceEquals` and object casts); `RepositoryHygieneTests` ratchets the remaining `== null` lines, which are all in EF expression trees where `is null` doesn't compile (CS8122) | Done November 4, 2026: 95 lines converted; 158 expression-tree lines remained (160 after step 4 moved the TV series list into a query), and the test fails if the total rises |
| Role-less class names (`BoxScoresRepository`, `ScoreboardRepository`, `AdminData`, `MyFollows`, `ProfilePageData`) | General §2 | `RepositoryHygieneTests` fails on a class or interface named `…Repository`, `…Manager`, `…Helper(s)`, `…Data` or `…Util(s)` | Done November 4, 2026: renamed `BoxScoreQuery`, `ScoreboardQuery`, `AdminStore`, `FollowStore`, `ProfilePageSettings`. `BoxScoreQuery` and `ScoreboardQuery` are still injected instances rather than static classes; step 4 reshapes them with the rest of data access |
| Agents' "always allow" answers saved about 25 commands with the `sa` or admin password in plain text (`sqlcmd -P …`), plus broad grants such as bare `sqlcmd`, `docker stop` and `rm -f`; each tool's permissions drifted on its own | Agents and database tools above; AGENTS.md | One list generated into every tool's format, with a unit test that fails on a hand-edited file and on any destructive or raw-SQL command in the allow list; `tools/sql/` scripts keep passwords off command lines | Done November 5, 2026: Antigravity's saved grants cleaned (33 removed, then `docker compose`/`docker image` narrowed to safe subcommands) and the list merged in; `.claude/settings.json` and `.codex/rules/project.rules` generated. Still open: rotate the admin password and `sa` (both were saved there); confirm the Codex rules with `codex execpolicy check` where Codex is installed |
| Verification tooling (the side-by-side build comparison, the live checks, the clean-up SQL) lived in session scratchpads and was rebuilt from handoff prose each time; `format:check` had been failing on four files | Releases section above; general §14 | `tools/verify.sh`, `tools/compare-builds.cjs`, `tools/live-check.cjs` and `tools/sql/` in the repository, documented in DEPLOY.md; `verify.sh` includes the format check | Done November 5, 2026: `compare-builds` reported step 4 (`c025a8d` → `7370fbf`) identical over 300 public and 25 admin pages and caught the College Sports navigation change; the four files were formatted (`7f4c367`). Still open: `deploy.sh`/`rollback.sh` (release plan parts 1–2) |

When the audit is repeated, add any new repeated deviation to this table with its rule and guard.
