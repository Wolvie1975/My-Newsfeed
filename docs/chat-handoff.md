# Chat handoff — October 19, 2026

Read this file when continuing work from the previous chat. These are dated
observations; check current repository and hosting state before acting.

## Latest deployment — NHL period detail, October 19, 2026

User authorized implementation and deployment of the frontend period-label
handoff. Live: `mynewsfeed-web:nhl-periods-20261019` (`03359e7c594c`), healthy.
Rollback: `mynewsfeed-web:before-nhl-periods-20261019` (`3570ee3785ab`), retained
container `mynewsfeed-web-before-nhl-periods-20261019`. Private deployment files:
`/tmp/mynewsfeed-nhl-periods-deploy/`. Runtime settings preserved; no commit/push.

`NhlBoxScoreVisibility` gates fallback NHL cache reads by enabled source, latest
valid stored season, valid final scores and source team restriction. There is no
dedicated NHL season-results query in this checkout despite the backend handoff's
reference to one; the helper uses the MLS/Championship stored-season convention.
Ordinary followed results remain an independent existing visibility path.
Baseball differing/sparse period sequences use per-team labeled tables instead of
positional shared columns. Repaired strings and older numeric/null labels remain
supported, and no result totals are recomputed.

16 focused tests passed. Live seven-sport/league example routes, four NHL theme/
width layouts, eight baseball layouts and modal regression checks passed. Private
HTTPS `/healthz` and NHL `401892525` returned 200. Men's college basketball has no
production cached acceptance example in the backend handoff; do not claim one.
This release supersedes the earlier deployment records below.

## Scraper period-label handoff — October 19, 2026

Created [sports period-label scraper handoff](../new%20development/sports-period-labels-scraper-handoff.md)
for MLB, NFL/college football, NHL, men's college basketball and existing soccer
leagues. It specifies compatible `lineScores[].period` display strings, evidence-
based mappings, overtime/shootout handling, historical backfill and acceptance
checks. This is documentation only; no collector repair or deployment is implied.

## Latest deployment — October 19, 2026

The user authorized deployment of all three sports templates. Live release:
`mynewsfeed-web:sports-templates-20261019` (`3570ee3785ab`), healthy with private
HTTPS checks passing. Runtime settings were preserved. Rollback image:
`mynewsfeed-web:before-sports-templates-20261019` (`40defb4164bd`), retained
container `mynewsfeed-web-before-sports-templates-20261019`. Private files:
`/tmp/mynewsfeed-sports-templates-deploy/`. Live baseball (8 layouts), soccer (6),
football (6) and general modal regression checks passed. Fourteen focused .NET
tests passed before release. No commit/push was performed. This supersedes the
local-only status below; baseball, soccer and football templates are now live.

## Latest local update

Football now uses `FootballBoxScore.razor` and `FootballPlayerTable.razor` for
category-first paired team tables, stacked on mobile. NFL and college football
share it; all cached categories/columns/totals remain intact. Fourteen focused
tests and six NFL modal width/theme checks passed. The ESPN URL was verification-
blocked, so do not claim an exact replica. This update is local, uncommitted and
undeployed, alongside the baseball and soccer template changes below.


Soccer now has a dedicated `SoccerTeamStats.razor` comparison component: visible
team values flank shared labels, grouped by source category. Existing period and
player detail is retained. The ESPN example was verification-blocked; do not claim
an exact replica. Thirteen focused tests and six soccer modal width/theme checks
passed. This soccer update is local, uncommitted and undeployed.


Baseball statistics now follow the user's screenshot reference: R/H/E summary,
paired hitting/pitching panels, mobile stacking and expandable extra statistics.
See `Components/BoxScores/BaseballBoxScore.razor` and `PlayerStatsTable.razor`.
Twelve focused tests, eight baseball layouts and existing modal regression checks
passed. No deployment or commit was made for this update. The live release remains
`mynewsfeed-web:boxscores-20261019`. Missing inning labels are not inferred; the
cache needs new fields for decisions, photos or scoring summaries.

## Project and preferences

- **Standing product direction:** “This is a tap to link site and not a typing one.”
  Public features should prioritize links, cards, tabs, dropdowns and preset
  selections. Normal browsing must not depend on typing. Do not reintroduce global
  Search or propose text-entry-driven public features unless explicitly requested.
  Keep keyboard accessibility and necessary admin/sign-in fields. See
  `docs/interface.md` → Standing product guideline: tap to link. This documentation
  request did not authorize a separate sweep removing existing text fields.

- Project: `/workspaces/DotNet_Projects/My-Newsfeed`, ASP.NET Core / Blazor,
  .NET 10, SQL Server, Docker. The Rust collector is a separate project.
- User calls the assistant Cody. Prefer direct implementation, concise updates,
  and avoid unnecessary confirmation questions.
- User authorized deployment after completing updates **for the previous session**.
  This authorization was explicitly session-scoped; establish deployment scope in
  a new chat rather than assuming that permission is permanent.
- Keep documentation current, especially `docs/release-history.md`, plus README,
  `docs/interface.md`, `docs/sports.md`, and DEPLOY.md when relevant.
- Do not infer permission to commit/push GitHub. Prior user requests distinguished
  documentation updates from later GitHub work. Preserve existing user changes.
- User-facing dates and times use America/Chicago.

## Implemented behavior

- Home hides completed sports events after matching final results are stored.
- Optional automatic light/dark theme uses Kansas City, Missouri sunrise/sunset;
  manual preferences remain available. Settings has an improved gear icon.
- News defaults to Top Headlines, first below All categories, with scoped publisher
  filters. Explicit All categories remains supported.
- Home, schedules and results show latest available team records when available.
  College shorthand is `12-4 (5-2)` for overall (conference), including draws for
  soccer. Tooltips explain notation and freshness. Home omits the long records note.
- Records disappear for the **entire league during playoffs**, including older
  results. Background ESPN phase checks run every ten minutes; unknown/failed or
  expired confirmation hides records. Regular-season confirmation expires after
  twenty minutes. No phase-provider requests occur in page rendering.
- College standings coverage is Big 12. Existing women's basketball exclusion
  remains intentional.
- Sports → F1 has Schedule, Results, Drivers/Constructors standings and News.
  Home has a separate F1 card outside Today & Tonight. Structured data is periodic,
  latest collected season, from read-only Jolpica-backed database views. F1 News
  uses the normal article feed system and is independent of structured enablement.
- Sports → College Sports has a shared sport selector: football, men's basketball,
  women's volleyball and women's soccer. Tabs: Schedule, Results, Standings,
  Rankings, News. Routes: `/college`, `/college/results`, `/college/standings`,
  `/college/rankings`, `/college/news`.
- College sport query keys: `football`, `mens-basketball`, `volleyball`, `soccer`.
  Sport persists across tabs; unknown/missing values default to football.
  Schedule covers today through seven days ahead with pagination; results use
  today/yesterday in site timezone and enabled NCAA follows. Standings are scoped
  to Big 12; rankings use available national polls and existing preseason gates.
- Embedded college standings/rankings omit redundant source dropdowns. Standalone
  Standings and Top 25 retain them. Sport label is vertically centered.
- College schedule uses the shared `.groups` wrapper to keep desktop column widths
  aligned across headings and rows. Mobile/tablet retain stacked cards.
- Table personal-team highlights use inset cell strokes, including sticky team
  cells; adjacent selected rows share an edge without shifting column widths.
  Existing red light/dark colors are retained.
- College News is **conference-wide**, not filtered by selected sport. Its publisher
  filter is intentional and separate from the sport selector. Five sources are
  discovered dynamically by category name `Big 12 News`: official All Sports,
  Heartland, Football, Men's Basketball and Baseball. No dedicated women's
  basketball source, but conference-wide stories can cover women's sports.
  Preserve existing global article URL ownership across overlapping feeds.
- `/feed/more?view=big12-news` enforces the category server-side and retains college
  news/sport URLs. F1 uses `view=f1-news`. Missing category must never leak general
  news. Existing cards, saving, retired-ID recovery and no-JavaScript paging apply.
- **Latest change:** YouTube is its own desktop main-navigation link and mobile
  Browse section, removed from Entertainment. Videos remain at `/videos`, with
  channel filters and playback unchanged. Mobile current-section label is YouTube.

## Current deployment recorded at handoff

- Container: `mynewsfeed-web`.
- Release: `mynewsfeed-web:youtube-section-20261019` (`a80561dc6724`).
- Rollback: `mynewsfeed-web:before-youtube-section-20261019` (`fd3d399c44d7`).
- Previous stopped container: `mynewsfeed-web-before-youtube-section-20261019`.
- Production URL: `https://mynewsfeed-prod.tailbf31bc.ts.net` (private Tailscale).
- Workspace DNS may require curl `--resolve mynewsfeed-prod.tailbf31bc.ts.net:443:100.98.148.14`.
- Internal HTTP: `http://mynewsfeed-web:8080`; SQL runtime is sql2025 / WebScraper.
- Last production browser checks passed desktop/mobile placement and active states,
  no overflow at 390/768/1180/1440px, and existing routes. HTTPS health returned 200;
  Docker health was healthy. Preview container was removed.
- At handoff `git status --short` was clean before this handoff file was added.
  Do not assume prior changes remain uncommitted: the user has committed between tasks.

## Verification and operations

- Main runbooks: `DEPLOY.md`, `docs/release-history.md`.
- Preserve actual live environment, identity, mounts/keys, ports, network and restart
  policy. Do not assume Compose matches live runtime. Build successfully before
  replacing production; retain previous container/image for rollback.
- Last release deployment scripts/private snapshot:
  `/tmp/mynewsfeed-youtube-deploy/`. Earlier `/tmp/mynewsfeed-big12-deploy/`,
  `/tmp/mynewsfeed-selector-deploy/`, `/tmp/mynewsfeed-highlight-deploy/` and
  `/tmp/mynewsfeed-college-alignment-deploy/` may remain. These are ephemeral;
  refresh runtime snapshots rather than reusing stale settings. Do not print or
  commit private container configuration.
- Browser tooling: `/tmp/newsfeed-browser-check/node_modules/playwright`.
  Prior verification scripts are under the deployment directories. They use a
  temporary preview name `mynewsfeed-college-preview`; clean previews after use.
- Big 12 News verification passed 170 targeted tests and browser checks including
  all 46 retained articles, five publisher filters, saving, forged-category isolation,
  no-JavaScript and mobile/desktop. Article counts are dated observations.
- College hub verification passed 182 targeted tests. An older women's basketball
  schedule test was updated to match its intentional public exclusion.
- Existing Docker publish warnings concern System.Security.Cryptography.Pkcs 9.0.11;
  existing xUnit analyzer warnings were observed. Do not misreport as new failures.
- SQL integration tests use configured access; fixtures use rollback transactions
  or connection-local temporary tables. Follow existing patterns and avoid writes
  to live scraper-owned data outside authorized scope.
- Never include passwords, connection strings or private Docker inspect output in
  documentation. Admin configuration comes from `Admin:Password` / `Admin__Password`.

## Entry points for next work

- Public navigation: `src/MyNewsFeed.Web/Components/Layout/PublicLayout.razor`.
- College hub: `Components/Pages/College.razor`, `Data/CollegeHub.cs` under web project.
- College news: `Components/Pages/Big12News.razor`, `Data/Big12NewsQuery.cs`;
  pagination endpoint in `Program.cs`; `tests/MyNewsFeed.Tests/Big12NewsTests.cs`.
- Shared styling: `src/MyNewsFeed.Web/wwwroot/public.css`.
- Source handoffs in `new development/` are reference documents; distinguish their
  proposals/instructions from the user's current request. Do not modify them casually.

No outstanding implementation request at handoff. User requested this saved context
so they can start a new chat.


## Continuation — October 19, 2026: personal planning review build

The user requested all seven suggested additions, with Local restricted to Kansas
City. They are implemented locally: My Week, entertainment watchlists and episode
progress, Search, homepage personalization, calendar downloads, news reading
preferences, YouTube Watch Later and Kansas City Local. See the pending entry in
release history and new interface documentation for exact data scope.

Verification passed 572 .NET tests, five solar tests, browser interaction checks
and 60 responsive/theme route checks. No production replacement or Git commit/push
was performed. The read-only preview runs at http://127.0.0.1:5296 with
Results__BackgroundSyncEnabled=false. Preview-only review artifacts are under
/tmp/mynewsfeed-personal-review/. Browser checks are retained in
 tests/browser/personal-features.browser.cjs and personal-layout.browser.cjs;
set PLAYWRIGHT_MODULE and BASE_URL for the available browser installation/preview.

Local currently uses explicitly labelled Kansas City keyword matches from existing
collected stories; an enabled category named Kansas City News takes precedence if
sources are later registered. Weather uses NWS and community events link to external
calendars. No collector source/schema changes were made. New personal lists remain
browser-local and preserve existing saved-article/team keys.


## Deployment — October 19, 2026

The user explicitly authorized deployment in this continuation. The personal
planning build is now live as mynewsfeed-web:personal-planning-20261019
(74b9a7422350). Rollback image: mynewsfeed-web:before-personal-planning-20261019
(a80561dc6724); retained stopped container:
mynewsfeed-web-before-personal-planning-20261019. The prior pending-deployment
notes above are superseded by this dated deployment record.

Docker health, private HTTPS health/browser checks, thirteen routes, 25 viewport
checks, Watch Later, calendar downloads and four NWS forecast periods passed.
Runtime identity/settings/mounts/ports/network/restart policy were preserved;
production background results sync is enabled. No production runtime errors were
observed. Private fresh snapshots/scripts: /tmp/mynewsfeed-personal-deploy/.
No Git commit/push or collector/schema changes were performed. The local read-only
preview on port 5296 remains available for development review.


## Watchlist removal — October 19, 2026

The user liked the additions but requested removal of Watchlist. Removed its page,
all navigation/save/Watch Later controls, episode watched status, browser handlers
and styles. The retired /watchlist route returns the standard 404. Old watchlist
storage is unused; Saved articles, teams and other preferences are preserved.
My Week now offers Only my teams in Sports; TV, Movies and F1 remain visible.
Tracked-series browsing/search and normal movie/video actions remain.

Deployed as mynewsfeed-web:without-watchlist-20261019 (6908e06c9777).
Rollback image: mynewsfeed-web:before-without-watchlist-20261019 (74b9a7422350).
Retained stopped container: mynewsfeed-web-before-without-watchlist-20261019.
Runtime settings were preserved. All 572 .NET tests, local browser interaction
checks and 60 responsive/theme checks passed; production browser removal and
retained-feature checks plus private HTTPS health/removal checks passed.
See release history for verification details. Private scripts/snapshots live in
/tmp/mynewsfeed-no-watchlist-deploy/. No Git commit/push or scraper changes.


## Weather icons — October 19, 2026

Added and deployed locally authored SVG weather icons on Kansas City forecast
cards. NWS isDaytime distinguishes sun/moon; summary classification covers common
conditions, with neutral thermometer fallback. Written descriptions remain; icons
are decorative and no external image requests are required. See weather-icons.js,
personal.js, LocalWeather.cs and weather-icons.browser.cjs.

Release: mynewsfeed-web:weather-icons-20261019 (a16338cf3354).
Rollback: mynewsfeed-web:before-weather-icons-20261019 (6908e06c9777).
Retained container: mynewsfeed-web-before-weather-icons-20261019.
Build, three weather tests, local/production responsive/theme/fallback browser
checks and private HTTPS real-forecast checks (four icons) passed. Production
healthy, runtime settings preserved, no observed runtime errors. Private deployment
files: /tmp/mynewsfeed-weather-icons-deploy/. No scraper/schema or Git changes.


## Site-wide Search removal — October 19, 2026

The user requested removal of site Search. Removed /search, its global header and
mobile links and cross-content SearchQuery implementation. The retired route
returns 404. Scoped listing filters remain; their bounded term helper is QueryText.
All other retained additions, including weather icons, remain available.

Deployed mynewsfeed-web:without-search-20261019 (e6c63fbe9b35).
Rollback: mynewsfeed-web:before-without-search-20261019 (a16338cf3354).
Retained stopped container: mynewsfeed-web-before-without-search-20261019.
Build, 571 .NET tests, local browser interactions and 60 theme/viewport checks
passed. Production interaction and private HTTPS navigation/removal, real weather
icons, retired route and health checks passed. Live runtime settings preserved.
No scraper/schema or Git commit/push changes. Deployment snapshots/scripts:
/tmp/mynewsfeed-no-search-deploy/. Local read-only preview remains on port 5296.


## Plan My Week removal — October 19, 2026

The user requested removal of Plan My Week. Removed /week, its Home and desktop/
mobile links, Local's weekly sports link, the combined agenda queries/browser
filter/styles and /calendar/week.ics. Both retired routes return 404. Individual
sports, F1, college, TV and movie schedules remain, along with Home customization,
team highlighting/Home filtering, reading controls and Kansas City/weather icons.

Deployed mynewsfeed-web:without-week-20261019 (d6789294eec0).
Rollback: mynewsfeed-web:before-without-week-20261019 (e6c63fbe9b35).
Retained container: mynewsfeed-web-before-without-week-20261019.
Build, 564 .NET tests, local interactions and 60 responsive/theme checks passed.
Production interactions and private HTTPS retained-page, weather-icon, retired-route
and health checks passed. Live runtime settings preserved. No collector/schema or
Git commit/push changes. Private deployment files: /tmp/mynewsfeed-no-week-deploy/.
Read-only local preview remains on port 5296. Continue following the standing
“tap to link site and not a typing one” direction recorded near the top of this file.


## Customize Home mobile alignment — October 19, 2026

Fixed Move up/Move down wrapping independently in Settings → Your homepage.
Rows use grid: on phones, a full-width section label above equal-width paired
buttons; on larger screens, label plus two fixed button columns. Minimum 44px
buttons, aligned columns, and dimmed disabled endpoints. Only public.css changed
for the implementation. Local and production browser checks passed 14 light/dark
viewport combinations (320/375/390/430/600/768/1440), reorder persistence and private
HTTPS mobile alignment/health. No additional unit tests were needed for this CSS fix.

Release: mynewsfeed-web:home-controls-20261019 (beae679b6be7).
Rollback: mynewsfeed-web:before-home-controls-20261019 (d6789294eec0).
Retained container: mynewsfeed-web-before-home-controls-20261019.
Private files: /tmp/mynewsfeed-home-controls-deploy/. Live runtime settings preserved;
no collector/schema or Git commit/push changes. Production healthy.


## Sports box-score modal update — October 19, 2026

The user supplied new development/sports-boxscores-frontend-handoff.md and asked for
box-score output in a modal, then explicitly asked to continue the update. It is
implemented and deployed. Daily Results, College Results, MLS and Championship
results offer Box score (player detail) / Game stats (team only) when cached usable
v1 JSON exists. A native modal loads a regular same-origin detail route; no-JS and
modified-click fallback work. Original result scores, records and provider links
remain. No schema/cache writes, scraper changes or provider requests were added.

Key implementation: Data/BoxScores.cs; Components/BoxScores/*;
Components/Pages/BoxScore.razor; wwwroot/box-scores.js. The cache identity is
Sport/League/EventId. One bounded availability batch validates payloads so malformed
or empty rows do not advertise actions; detail HTML is loaded only on request.
Team stats align by group/key; player columns retain indices; unknown/repeated
period labels preserve every entry without inferred numbering. Results refresh
pauses during open dialogs. Runtime SELECT access to the view was verified.

Release: mynewsfeed-web:boxscores-20261019 (40defb4164bd).
Rollback: mynewsfeed-web:before-boxscores-20261019 (beae679b6be7).
Retained container: mynewsfeed-web-before-boxscores-20261019.
Private deployment files: /tmp/mynewsfeed-boxscores-deploy/ and
/tmp/mynewsfeed-boxscores-review/. All 575 .NET tests and final focused 11 tests
passed. Live-data probes rendered NFL/MLB/WNBA/college-football/MLS/NWSL details.
Local/production and private HTTPS browser checks passed modal/focus/mobile/error/
no-JS behaviour; runtime healthy with no observed errors. No Git commit/push.

Browser checks: tests/browser/box-scores.browser.cjs. Set BASE_URL and
PLAYWRIGHT_MODULE as appropriate. RESULTS_PATH can select a daily results route;
by default the test uses yesterday in America/Chicago and requires retained cached
player detail in that window. The linked backend operations report in the source
handoff was not present in this frontend checkout; current read access/coverage
was checked directly rather than assuming historical counts.
