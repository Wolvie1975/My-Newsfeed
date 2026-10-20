# Chat handoff — October 19, 2026

Read this file when continuing work from the previous chat. These are dated
observations; check current repository and hosting state before acting.

## Latest deployment — watch sorting, October 20, 2026

Live mydailyfeed-web:watch-sort-20261020 (ef6f7f3e7cdb), healthy. Rollback image:
mynewsfeed-web:before-watch-sort-20261020 (d04df3e91825), retained container
mynewsfeed-web-before-watch-sort-20261020. Private deployment files:
/tmp/mydailyfeed-watch-sort-deploy/. Runtime preserved, HTTPS health 200.

Sort this page offers services/title/newest/popular. Default services groups
alphabetize titles; no selected services means alphabetical loaded titles.
Sorting is per page and copies the cached listing; full dates/popularity are
parsed from TMDB list responses. Unknown metadata sorts last. Sort query state
survives tabs, genre/page links, search and clearing. No-JS forms work. Active-TV
browse filtering and title search behavior remain intact. 20 focused tests plus
live sorting and full watch browser suites passed. No commit/push was made;
session deployment permission remains active. This supersedes earlier releases.

## Latest deployment — active TV browse, October 20, 2026

Live mydailyfeed-web:active-tv-20261020 (d04df3e91825), healthy. Rollback:
mynewsfeed-web:before-active-tv-20261020 (f138f628aeac), retained container
mynewsfeed-web-before-active-tv-20261020. Private files:
/tmp/mydailyfeed-active-tv-deploy/. Runtime preserved, HTTPS health 200.

TV non-search discovery adds with_status=0|1|2|5 so ended/canceled shows stay out
of popular/genre browsing before pagination/service ordering. Search still finds
them. No first-air-year cutoff, detail blocking or tracking-record changes.
15 focused tests and live active-TV/watch browser suites passed. 40 displayed
statuses checked against TMDB, ended-series search retained. No commit/push was
made. Session deployment permission remains active. This supersedes earlier
release records below.

## Latest deployment — public series additions, October 20, 2026

User explicitly prefers no password wall for adding tracked series. TV links now
use /tv/track, which hosts the shared SeriesFollowForm with Public=true. It searches
TVmaze, requires the reader's Follow confirmation, re-verifies provider identity
and adds/re-enables through existing TvQuery. Success stays public. Admin wrapper
still exists and all /admin authorization is retained. This is a shared site list,
not browser-local tracking; that is stated on the public page. No mutations on GET.

Live: mydailyfeed-web:public-track-20261020 (f138f628aeac), healthy. Rollback image:
mynewsfeed-web:before-public-track-20261020 (31b922fe465b), retained container
mynewsfeed-web-before-public-track-20261020. Private files:
/tmp/mydailyfeed-public-track-deploy/. Runtime preserved, HTTPS health 200.
Five focused tests and live public-tracking/watch browser suites passed, including
anonymous navigation, prefilled results, mobile and protected admin management.
Browser verification did not add a production series. No commit/push was made.
This supersedes the prior password-wall behavior; session deployment permission
remains active.

## Latest deployment — track TV series, October 20, 2026

Live mydailyfeed-web:track-series-20261020 (31b922fe465b), healthy. Rollback:
mynewsfeed-web:before-track-series-20261020 (7f5ed6f6fc38), retained container
mynewsfeed-web-before-track-series-20261020. Private files:
/tmp/mydailyfeed-track-series-deploy/. Runtime preserved; HTTPS health 200.

Watch TV detail/modal has TrackSeriesLink → admin/tv-series/new?q=title&year=year.
The protected page auto-searches TVmaze and asks the reader to confirm a matching
result before Follow. Movie links are absent. Never pass a TMDB ID to TvQuery as
if it were a TVmaze ID. No mutation on GET; existing admin protection and
idempotent Follow/re-enable behavior remain. Five focused tests plus local/live
sign-in/prefilled TVmaze result and full watch browser suites passed. No production
series was followed by browser verification. No Git commit/push was made.
This supersedes earlier release records below; session deployment permission
remains active.

## MyDailyFeed rename / latest deployment — October 20, 2026

Project is now MyDailyFeed. Solution: MyDailyFeed.sln. Web and test projects,
folders and namespaces are MyDailyFeed.Web / MyDailyFeed.Tests. Build/test commands
must use the new names. The checkout directory remains My-Newsfeed and GitHub
remote remains unchanged. Do not interpret legacy lower-case deployment identifiers
or historical reference documents as current UI branding.

Live: mydailyfeed-web:rename-20261020 (7f5ed6f6fc38), healthy. Operational container
is still mynewsfeed-web, serving the same private hostname/port. Entrypoint is now
dotnet MyDailyFeed.Web.dll. Rollback image mynewsfeed-web:before-rename-20261020
(dc9adef11982), retained container mynewsfeed-web-before-rename-20261020. Private
files: /tmp/mydailyfeed-rename-deploy/. Runtime settings preserved except the
intentional renamed assembly entrypoint. No commit/push or remote rename was made.

Keep old browser storage keys, MyNewsFeed.Admin cookie name, data-protection
SetApplicationName(MyNewsFeed), secrets ID, PWA id/scope and persistent key volume
for compatibility. New branding and DF icons are live. All 593 tests and live
branding/Settings/watch/sports-modal suites passed. User's session deployment
authorization remains active. This supersedes earlier release records below.

## Session deployment authorization — October 20, 2026

The user explicitly granted permission to deploy for this session. Proceed with
necessary verification and deployment of authorized changes without asking again.
This supersedes earlier deployment holds/local-only notes for this session; it
does not authorize Git commits or pushes.

## Latest deployment — navigation initialization fix, October 20, 2026

Live mynewsfeed-web:settings-nav-20261020 (dc9adef11982), healthy. Rollback:
mynewsfeed-web:before-settings-nav-20261020 (896acc615bae), retained container
mynewsfeed-web-before-settings-nav-20261020. Private files:
/tmp/mynewsfeed-settings-nav-deploy/. Runtime preserved, private HTTPS health 200.
No commit/push was made; session deployment permission remains active.

User screenshot showed empty Arrange Settings panel. Reproduced via Home →
Settings (zero move buttons). A direct page load was insufficient test coverage.
settings-order.js now uses Blazor.addEventListener('enhancedload') and a guarded
DOM observer; watch.js uses the correct enhanced-load hook for service restoration.
Tests now navigate via actual site links, then check controls, moves, persistence,
reset/mobile and restored services. Both suites passed locally and live. Existing
browser tabs may need a full page refresh to load the corrected global scripts.
This supersedes the earlier deployment records below.

## Latest deployment — Settings services/order, October 20, 2026

Live: mynewsfeed-web:settings-services-20261020 (896acc615bae), healthy with
private HTTPS /healthz 200 and runtime settings preserved. Rollback:
mynewsfeed-web:before-settings-services-20261020 (9ac98a4e5365), retained container
mynewsfeed-web-before-settings-services-20261020. Private files:
/tmp/mynewsfeed-settings-services-deploy/. User explicitly authorized deployments
for the rest of this session; no need to ask again. No Git commit/push was made.

My services moved to Settings (WatchServiceSettings); /watch has a shortcut.
settings-order.js lets all six Settings sections move up/down with browser-local
persistence and reset, independent of Home ordering. Theater/ticketing exclusion
is included. 12 focused tests and live services/order/watch suites passed,
including no-JS selection, persistence, malformed storage and mobile. Live checks
confirmed ticketing removed and home-viewing services retained. This supersedes
pending/local-only notes and earlier deployment records below.

## Latest deployment — service preferences, October 20, 2026

User explicitly requested deploy after feedback, lifting the earlier hold.
Live: mynewsfeed-web:watch-services-20261020 (9ac98a4e5365), healthy. Runtime
settings preserved; private HTTPS /healthz returned 200. Rollback image:
mynewsfeed-web:before-watch-services-20261020 (9d863862bcc2), retained container
mynewsfeed-web-before-watch-services-20261020. Private files:
/tmp/mynewsfeed-watch-services-deploy/. No Git commit/push was performed.

Main navigation and service selectors are now live. Matching is per page, based
on subscription/free/ads provider IDs, with stable order for other titles. Saved
browser preferences and GET parameters follow title search and browsing. Four
focused service tests and both live watch/service browser suites passed. This
supersedes local-only and deployment-hold notes below.

## Latest deployment — Where to watch, October 20, 2026

User authorized implementation/deployment of typed movie/TV title lookup as a
scoped interaction exception. Live: mynewsfeed-web:watch-20261020 (9d863862bcc2),
healthy with private HTTPS checks passing and runtime settings preserved.
Rollback: mynewsfeed-web:before-watch-20261020 (7099cb8225a4), retained container
mynewsfeed-web-before-watch-20261020. Private files: /tmp/mynewsfeed-watch-deploy/.

/watch uses TMDB with existing Movies:AccessToken kept server-side. WatchCatalog
provides capped search/discovery, bounded cache and US viewing options; native
watch.js dialog has regular detail-page fallback. Capture-phase click handling
is necessary to prevent Blazor enhanced navigation from taking over. Credits
include official TMDB logo/API notice and JustWatch attribution. No provider deep
links are available; use the returned TMDB viewing-options link. Movie-release
cards have a title lookup link. No global Search was restored.

Three focused service tests, the live watch browser suite (six themes/widths,
search, movie/TV, genres, empty/error/invalid states, focus, no-JS) and sports-modal
regressions passed. No scraper/schema changes or Git commit/push were made. See
docs/entertainment.md for caching, limitations and provider documentation.
This supersedes the earlier live release records below.

## Where to watch authorization — October 20, 2026

User explicitly approved typed movie/TV title search as an exception to the tap-to-link rule and authorized implementation/deployment. Do not reintroduce global Search. Scope: /watch, Movies/TV, genre/popular browsing, title modal/detail and movie-card lookup links.

## Latest deployment — basketball, October 19, 2026

User authorized deployment and Git check-in. Live release:
mynewsfeed-web:basketball-20261019 (7099cb8225a4), healthy; private HTTPS health
passed and runtime settings were preserved. Rollback image:
mynewsfeed-web:before-basketball-20261019 (03359e7c594c); retained container:
mynewsfeed-web-before-basketball-20261019. Private files:
/tmp/mynewsfeed-basketball-deploy/. Tests: 17 focused checks, six championship
fixture layouts, four live WNBA layouts and the general modal regression suite
passed. The championship fixture route stays unavailable in production. This
supersedes the local-only status and earlier deployment records below. Git check-in
was requested after release; inspect Git history/remote for current push status.

## Latest local update — basketball template

Added BasketballBoxScore.razor for basketball player tables, paired on desktop,
stacked on mobile, with dynamic columns and sticky player names inside scrolling
regions. Source team identity, player groups, starter/bench flags and supplied
totals are retained. Actual championship sample was added by the user; it remains
only in development/test tooling. No historical production game or route bypass
was inserted. 17 focused tests and six fixture width/theme checks passed. Local
only; not deployed or committed. Live release remains nhl-periods-20261019.

To regenerate the standalone fixture preview, set BASKETBALL_PREVIEW_FILE to an
absolute temporary HTML path when running the BoxScore test filter. Run
 tests/browser/basketball-boxscore.browser.cjs with the same path and available
PLAYWRIGHT_MODULE. The preview is generated test output, not an application route.

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

- Public navigation: `src/MyDailyFeed.Web/Components/Layout/PublicLayout.razor`.
- College hub: `Components/Pages/College.razor`, `Data/CollegeHub.cs` under web project.
- College news: `Components/Pages/Big12News.razor`, `Data/Big12NewsQuery.cs`;
  pagination endpoint in `Program.cs`; `tests/MyDailyFeed.Tests/Big12NewsTests.cs`.
- Shared styling: `src/MyDailyFeed.Web/wwwroot/public.css`.
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

## Pending feedback — hold deployment, October 20, 2026

User requested Where to watch as a main navigation item, moved out of the
Entertainment group on desktop and into the primary mobile links. This is a local
change. User explicitly said do not deploy yet; more feedback is coming. That hold
supersedes prior deployment authorization until the user asks to deploy again.

## Pending feedback — service selectors, October 20, 2026

Added My services checkboxes to /watch using the union of current US TMDB movie/
TV provider choices. Up to 20 selections apply with GET forms, persist in browser
localStorage when available and follow Movie/TV/genre/pagination/search links.
Within each results page, verified subscription/free/ads matches sort first and
show the matching services; rental/purchase-only offers do not qualify. Other
titles remain visible in stable order. This is per-page prioritization, not a
catalog-wide filter. Provider requests remain bounded to four concurrent calls;
provider access is cached six hours and service options twelve hours. Ranking
copies the shared listing rather than mutating its cached order.

Four service tests and browser checks passed (selection, actual badges, browser
persistence, TV navigation, clearing, phone overflow and no-JavaScript forms).
Main-nav movement and service selectors remain LOCAL: user's deployment hold is
still active. No deployment or commit was made.

## Pending local update — theater options, October 20, 2026

Removed ticketing/theater-only providers from Where to watch service choices,
matching and displayed offers. Fandango provider 60 and exact theater-only names
are excluded; At Home, AMC/AMC+ TV and Cinemax remain eligible. Stale provider-60
selections are ignored. Twelve focused tests passed. Not deployed yet; live
release remains watch-services-20261020.

## Settings organization — pending local update, October 20, 2026

User clarified that section arrangement applies to Settings. My services now
lives in Settings with a shortcut from Where to watch; existing browser choices
continue to apply. The six Settings sections have Move up/Move down controls,
browser-local ordering and Restore default Settings order. Homepage ordering
remains independent. Services can be saved without leaving Settings; the GET
fallback and Where to watch link work without JavaScript.

Twelve focused provider tests and local browser checks passed for service
persistence/ranking/navigation, clearing, no-JavaScript selection, Settings order
persistence/reset, malformed storage and mobile overflow. Changes remain local,
including the prior theater-option removal. No deployment or commit was made.
