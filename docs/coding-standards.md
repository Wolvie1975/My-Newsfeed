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
- **[Review]** Components inject `IDbContextFactory<WebScraperContext>` as `Factory` and call `XxxQuery`/`XxxStore`
  methods. Admin pages currently run LINQ inline (about 18 files); move it into query/store classes when a page is
  next changed.
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

## Guardrails against past drift

The November 4, 2026 audit found the problems below. Each has a rule above, a clean-up step, and a guard so it can't
quietly come back. Update the **Status** column as each guard lands, and don't remove a guard without replacing it.

| Problem found | Rule | Guard that stops it recurring | Status |
| --- | --- | --- | --- |
| The same data job done four ways: inline LINQ in about 25 components; `SqlQueryRaw`, `ExecuteSqlRaw`, `DbCommand` and interpolated SQL side by side | Data section above; general §6 | Banned-API list blocks the `*Raw` APIs. A check script fails on `db.<Table>` LINQ inside `.razor` files. Remaining `DbCommand` uses each carry a "why" comment. | Planned (clean-up step 4) |
| About 60 read methods with no `CancellationToken`; `CreateDbContextAsync()` without a token | General §4 | `CA2016` at `warning`; review checklist | Planned (step 5) |
| Clock read three ways inside queries; `nowUtc`/`utcNow`/`now` parameter names | General §4 | Banned-API list blocks `DateTime.Now`, `DateTime.UtcNow`, `DateTimeOffset.UtcNow` outside the edge | Planned (step 6) |
| Shared code in `myNewspaper.Web.*` with `RootNamespace` `myNewspaper.Common` | General §2; Solution shape above | `IDE0130` at `warning`; `RootNamespace` matches the assembly | Done November 4, 2026: Shared is `myNewspaper.Shared.*` (and `myNewspaper.Hosting` became `myNewspaper.Shared.Hosting`), its `RootNamespace` is `myNewspaper.Shared`, and Admin's endpoints and tests moved out of `myNewspaper.Web.Endpoints`/`myNewspaper.Tests` |
| `Web` and `Admin` `Program.cs` duplicating about 60 lines that had drifted | Solution shape above; general §5 | One `AddMyNewspaperCore()` in Shared (`Hosting/CoreServices.cs`); reviewers reject copied registrations | Done November 4, 2026: both hosts' registrations and outbound client settings compared identical before and after, apart from Admin's duplicate `TimeProvider` registration now removed |
| 35 breakpoint widths, an `admin-`/`adm-` split, three visually-hidden patterns | CSS section above | `tools/check-css.cjs` (run by CI) fails on unregistered widths, unregistered prefixes and copies of the clip pattern | Planned (step 8) |
| `sa` password hard-coded in 10 test files; integration tests writing to the live database | Tests section above; general §12 | `RepositoryHygieneTests` fails on a literal `Password=`/`Pwd=` in any tracked file; `TestDatabase` always targets `WebScraper_Test` and refuses `WebScraper`; [create-test-database.sql](../deployment/create-test-database.sql) rebuilds the copy | Done November 4, 2026 for the tests. Still open: rotate the `sa` password (in Git history until October 25) and give the apps their own least-privilege SQL logins |
| 89 browser checks with 10 viewport sets, several base-URL defaults (one production), and copied clean-up code | Tests section above; AGENTS.md | Shared `tests/browser/lib.cjs`; the check script fails on any `*.browser.cjs` that doesn't use it | Planned (step 7) |
| CI ran only 4 of about 81 test classes and no lint or format checks | General §1 | Both workflows run the full suite (with a SQL service container), `dotnet format --verify-no-changes`, `npm run lint`, `npm run format:check` and the check script | Planned |
| `== null` and `is null` mixed | General §3 | `IDE0041` and the `is null` preference at `warning` (catches `ReferenceEquals` and object casts); `RepositoryHygieneTests` ratchets the remaining `== null` lines, which are all in EF expression trees where `is null` doesn't compile (CS8122) | Done November 4, 2026: 95 lines converted; 158 expression-tree lines remain, and the test fails if the total rises |

When the audit is repeated, add any new repeated deviation to this table with its rule and guard.
