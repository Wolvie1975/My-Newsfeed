# Chat handoff

Latest Where to watch work: [Where to watch handoff](where-to-watch-handoff.md)
(October 25: flow redesign, Featured rows and search results tidy deployed;
compact layout October 24). Includes validation, rollback, pending Git changes and
the deferred service hyperlink decision.

Read this file when continuing work from the previous chat. These are dated
observations; check current repository and hosting state before acting.

## Concept header deployed; section pages next — October 25, 2026 (Central)

The user compared live pages with the canvas (https://claude.ai/artifact/JVQ8mU7R63Z1cziNpeW9Z5) and found only Home
had been rebuilt; section pages were only restyled and the header was unchanged. Agreed plan, one batch at a time
with a deploy for the user to test: (1) header everywhere — done, `header-20261025`; (2) Entertainment, Movies, TV,
Digital releases, Box office, Where to watch; (3) Sports, Scoreboard, Standings, Top 25, College, F1; (4) Headlines,
Local, YouTube, Saved, Settings. Match each page to its desktop and phone boards side by side, including large
system text (the user's phone behaves like about 317px). The desktop header keeps a light/dark icon and an Admin
link that the concepts omit; the user may want them removed. Details in [release history](release-history.md).
Committed locally on `develop`. Batch 2 (Entertainment pages) deployed as `entertainment-20261026`; see release history. Batch 3 (Sports pages) deployed as `sports-20261026`. Batch 4 (Headlines, Local, YouTube, Saved, Settings) deployed as `pages-20261026`; every page now follows its concept board. The user is drafting new Sports and Entertainment concepts separately. Work in the light resource mode (see release history). Follow-up `youtube-20261026`: YouTube v2 carousel and the new Sports/Entertainment headline boards. Follow-up `contrast-20261026`: every scheme's text passes a WCAG AA contrast check (`scheme-contrast.browser.cjs`); run it whenever a scheme or colour changes. Follow-up `settings-20261026`: Reading preferences removed from Settings at the user's request. Pending: the user is testing.

## Broadsheet redesign deployed — October 25, 2026 (Central)

User asked to build the approved redesign (canvas https://claude.ai/artifact/JVQ8mU7R63Z1cziNpeW9Z5)
and deploy. Three redesign commits already existed on `develop` from an earlier session that left
no handoff; this session verified them, fixed phone Today & Tonight (compact rows, 44px targets) and
the desktop side-column release grid, and deployed `mynewspaper-web:broadsheet-20261025`. Details,
checks and rollback in [release history](release-history.md#broadsheet-redesign--deployed-october-25-2026-central).
Committed locally on `develop` (user pushes). Not yet done from the proposed extras: YouTube topic
chips, league chips in place of typed team search, grouped duplicate stories. The phone scoreboard
strip on /sports still scrolls sideways (existing component). No pending request.

## Code standards cleanup deployed — October 25, 2026 (Central)

User requested a code review for modern .NET/Blazor/JS standards, approved all phases and
deployment. Deployed `mynewspaper-web:code-standards-20261025`; details in release history
and [maintenance](maintenance.md#code-standards-review--october-25-2026). Committed locally
on `develop` (user pushes). VS Code crashed mid-session; a second session resumed after
confirming the first had ended without deploying.

New working conventions: connection string in user secrets (`appsettings.json` has none);
`npm ci` then `npm run lint`, `npm run format:check`, `npm run test:browser` (serial, against
BASE_URL); builds fail on warnings. Open follow-ups for the user: rotate the `sa` password
(it stays in Git history) and use a least-privilege login; optional scraper index on
`SportsBoxScores`; login rate limiting before any public exposure. No pending request.

## GitHub checkpoint — October 25, 2026 (Central)

User requested a check-in of the overnight Where to watch work and a fresh start.
This commit covers the October 24 compact layout and the October 25 flow redesign,
Featured rows and search results tidy, with tests and documentation. All three
October 25 releases were verified live before this checkpoint; the current release
is `mynewspaper-web:watch-search-20261025`. Fetched origin first: develop matched
origin/develop at `c4a2cbe`. Documentation-only addition; no new mobile check needed.
Check the push result and current Git state when resuming. No pending request.

## Where to watch Featured rows and search tidy deployed — October 25, 2026 (Central)

User approved the rows mockup and chose Featured as default, four rows, a mixed
services row and 12 per row. From the results-page review they picked Best match
default + tidier links/heading. Deployed `watch-featured-20261025` then
`watch-search-20261025` (current; `5b88f6d4b6fb`). 715 .NET tests passed; preview
and live 20-combination browser matrix (320/390/430/768/1440, light/dark, JS on/off)
and all Where to watch suites passed live with screenshot review. Real device
unavailable. Not committed or pushed. No pending request.

## Where to watch flow deployed — October 25, 2026 (Central)

User approved deployment for this session. The search results layout is unchanged.
Genre was removed. Movies/TV and My services toggles, poster service badges,
selected-service logos and instant sort were added, with a smaller phone header.
Deployed `mynewspaper-web:watch-flow-20261025` (`a85358c2c77e`); rollback
`before-mynewspaper-watch-flow-20261025`. 703 .NET tests passed. Preview and live
browser checks passed 20 combinations (320/390/430/768/1440px, light/dark, JavaScript
on/off) with screenshot review. Real device unavailable. Not committed or pushed.
See [where-to-watch-handoff.md](where-to-watch-handoff.md).

## Homepage hourly lead and article actions — deployed October 24, 2026

Follow-up request: make the homepage Top story rotate hourly as well as Opinion.
Added HomeLeadQuery: it round-robins the newest headline per enabled source across
UTC hours, excluding Opinion so the separate Opinion spotlight stays distinct.
HomeLeadStory exposes the next-boundary delay; `/home/lead` serves a no-store card
fragment, and home-lead.js refreshes only this card, retains the last good story on
failure, and pauses while hidden. No News page or database changes.

Build passed. Published preview behavior passed with a simulated clock, including
one failed refresh retaining the story and a later successful retry. Mobile browser
checks and screenshot review passed at 320/390/430px in light/dark; desktop at
1440px in both themes passed. Real device not available.

Follow-up layout request: place Where to watch immediately below Top story on both
mobile and desktop. Homepage News, Sports and Entertainment shelf articles include
share and save controls with 44px touch targets. Deployed as
`mynewspaper-web:home-lead-actions-20261024` (image
`df4b96088426`); production container is healthy. Rollback image:
`mynewsfeed-web:before-home-lead-actions-20261024`; previous container retained.

Live browser checks passed at 320/390/430/1440px in light and dark themes for
section order, overflow, and share/save visibility and touch targets. Share dialog
and mocked save interaction passed; `/healthz`, `/` and `/home/lead` returned 200;
runtime error markers: zero. Screenshots are under
`/tmp/mynewspaper-home-lead-review/`. No real-device check available. Tailscale
Serve still maps the tailnet URL to host port 8085, but this runner could not resolve
the tailnet hostname for an HTTPS browser check.

## Homepage editorial layout deployed — October 24, 2026

Deployed `mynewspaper-web:homepage-reorg-20261024` (image `ef323ab7626c`).
Container healthy; runtime configuration, persistent mounts and restart settings
were preserved. Rollback image: `mynewsfeed-web:before-mynewspaper-homepage-reorg-20261024`;
previous container retained. No schema or feed-collector changes.

Homepage now leads with the newest available headline, then shows separate News,
Sports and Entertainment shelves and the existing rotating Opinion card. Added
hub-scoped story queries, preference controls for the new homepage sections, and a
full-width desktop lead. News/Sports/Entertainment routes are existing hub URLs.

Before deployment, published preview passed 320/390/430px light/dark and 1440px
light/dark checks for section order, lead link, touch controls and horizontal
overflow; screenshots reviewed. The same checks passed against production after
deployment. 12 health/public routes and Tailscale HTTPS passed; runtime error
markers: zero. No real-device check available. Evidence: `/tmp/mynewspaper-homepage-reorg-deploy/`.

## Home Opinion spotlight deployed — October 24, 2026

Deployed `mynewspaper-web:home-opinion-20261024` (image `8ed93c383a5f`).
Container is healthy; runtime configuration and persistent mounts were preserved.
Rollback image: `mynewsfeed-web:before-mynewspaper-home-opinion-20261024`;
previous container retained. No database or News page changes.

Live checks passed: 12 health/public routes, Tailscale HTTPS, Opinion card at
320/390/430px in light/dark with screenshot review, tablet/desktop, hourly refresh
and failed-refresh retention, mocked save writes/share, reading preferences and
no-JavaScript link. Homepage columns/resizing/hidden-section checks passed after
correcting the test to compare rounded grid spans instead of unrounded card
bottoms; no runtime layout change was needed. Runtime error markers: zero.
No real-device check available. Evidence: `/tmp/mynewspaper-home-opinion-deploy/`.
User subsequently requested GitHub check-in; this change set includes the
implementation, tests and deployment evidence. No runtime changes during check-in;
mobile verification remains the live evidence recorded above.

## Home Opinion publisher spotlight — local update, October 24, 2026

User requested an Opinion homepage card alternating sources hourly, explicitly
leaving the News page alone. Added HomeOpinionCard after Latest news, with one
publisher's newest article, attribution/image/date/summary, existing share/save
controls and a View all link to the existing Opinion category. Membership resolves
by category name, never fixed IDs. Latest-per-source query accepts an optional
category filter; all existing callers keep their previous behavior.

HomeOpinionQuery uses the actual UTC hour and a stable round-robin of eligible
source URLs. Each publisher gets one slot before the cycle repeats; selection is
stable within an hour. Only enabled Opinion feeds with retained articles compete;
empty/retired feeds are skipped. Missing data has a normal empty state, while a
query failure is isolated from other homepage cards. No News page changes.

/home/opinion serves a no-store card fragment. home-opinion.js refreshes just the
card at the next hour boundary, catches up on focus/visibility return and preserves
the outer card, layout, reading/hidden preferences and controls. Failed requests
retain the last good story and retry after a minute. Without JavaScript the initial
card and existing feed link render; reloading updates the hourly selection.
Registered Opinion in the existing personal section list; saved reading/visibility
preferences remain honored. No database, collector or source configuration changes.

Build/publish and 165 focused tests passed, including complete publisher cycles,
hour stability, deterministic newest/tie selection, empty/single-source cases and
connection-local SQL fixtures proving enabled/category scoping. Browser checks
passed at 320/390/430px light/dark with screenshot review, plus 768/1440 regression:
card metadata/links, share/save (mocked writes), muted/hidden preferences, no-JS
Opinion link, clock-driven refresh and failed-refresh retention. Existing homepage
cascading-column/mobile/no-JS suite passed. No real-device check available.
Evidence/read-only preview: `/tmp/mynewspaper-home-opinion-review/`, port 5326.

This local review was subsequently deployed; see the deployment entry above.
Implementation is included in the subsequent GitHub check-in.

## Section dropdown navigation check-in — October 24, 2026

User requested GitHub check-in after deployment. Local commit on develop includes
shared section dropdowns, College/F1/league/Watch/Local conversions, browser
regressions and release evidence. All three live browser suites passed in the
preceding deployment; no implementation changes during this check-in. GitHub HTTPS
push preflight still cannot authenticate (could not read Username). Remote push
remains unconfirmed; restore GitHub login and run `git push origin develop`.
Documentation-only addition needs no further mobile check; live verification is
recorded below.

## Remaining section navigation dropdowns deployed — October 24, 2026

User supplied a College Sports screenshot showing Schedule/Results/Standings/
Rankings/News bubbles and requested implementation/deployment of dropdowns.
Replaced College view bubbles and remaining section-choice groups: F1 views,
calendar scope and championship tables; soccer league Schedule/Table/Season
results; Watch content type/genres; Kansas City section anchors. Existing news hub
category dropdowns stay intact. Previous/Next and date paging remain links.

Shared SectionSelect renders a labeled native select, automatic GET submit and
no-JavaScript Go fallback with 44px minimum height. Local-only /navigate redirects
preserve query parameters and fragments; external/protocol-relative/backslash
or control-character targets fall back to Home. College selections preserve sport;
Watch URL helper preserves sort/services. No data/schema/collector changes.

Live image `mynewspaper-web:section-select-20261024` (`323c6b2645da`), container
`mynewsfeed-web`, healthy. Rollback image
`mynewsfeed-web:before-mynewspaper-section-select-20261024`; previous container
`mynewsfeed-web-before-mynewspaper-section-select-20261024` retained. Environment,
renamed entrypoint, mounts/keys, network, ports, restart policy and healthcheck
verified preserved. Docker build, health/public-route smoke and private HTTPS passed.

Local and live section-select browser suites passed: 320/390/430px light/dark,
768/1440 regressions, screenshots reviewed, all five College views across all four
sports, sport switching while retaining view, F1 championship selector, Watch
content/genre/sort, native no-JS Go on College/F1/Local, and local-only redirects.
Live F1 full/upcoming calendar and all MLS league destination selections passed.
Both updated Watch sort and mixed-search/navigation regression suites passed locally
and live. Runtime smoke had zero failure/unhandled-exception markers. No real-device
check available. Evidence: `/tmp/mynewspaper-section-select-deploy/`.

Source/docs/tests are not committed or pushed as part of this deployment-only
request. Earlier category dropdown release is superseded by this release.

## Hub category dropdowns deployed — October 24, 2026 (Central)

User requested deployment and GitHub check-in. Live image:
`mynewspaper-web:category-dropdown-20261024` (`20e8cab3cd79`), container
`mynewsfeed-web`, healthy. Rollback image:
`mynewsfeed-web:before-mynewspaper-category-dropdown-20261024`; prior container
retained as `mynewsfeed-web-before-mynewspaper-category-dropdown-20261024`.
Runtime environment, renamed entrypoint, mounts/keys, network, ports, restart
policy and healthcheck verified preserved. No database/collector changes.

Sports/Entertainment headline category buttons are now a native dropdown, applying
on selection. Without JavaScript, Apply submits the same GET selection. Saved hub
assignments still supply options; canonical links, source resets and paging remain.
Final Docker build, twelve health/public-route smoke checks and private HTTPS
passed. Both live hub browser suites passed: actual dropdown selection, source
filters, redirects, cross-hub API rejection and JS/no-JS selection/pagination.
Phone 320/390/430px light/dark with screenshot review and 44px selector targets,
plus 768/1440 regressions passed. No real-device check. Runtime smoke log had zero
failure/unhandled-exception markers. Evidence:
`/tmp/mynewspaper-category-dropdown-deploy/`.

Local check-in follows on develop. GitHub authentication preflight still failed
(could not read HTTPS Username); remote push is not confirmed. The recorded
origin/develop already includes `1369f64`; the dropdown commit remains local.
Restore GitHub authentication and run
`git push origin develop` if the final push attempt remains blocked.
Earlier local-only dropdown notes below are superseded by this deployment.

## Hub category dropdowns — local update, October 24, 2026

User requested replacing wrapping category bubbles on Sports/Entertainment headline
pages with a dropdown. Both now use a visible Category label and native select,
44px minimum height, responsive width, automatic GET submission and an Apply
fallback without JavaScript. Options still come from saved eligible hub assignments.
Selection uses the existing category redirect so canonical section URLs, source
reset and scoped paging remain. Removed obsolete category-button styles.

Publish and both adapted hub browser suites passed: actual category selection,
source filters, redirects, scoped API/JS/no-JS pagination and no-JS selector Apply.
320/390/430px light/dark screenshots reviewed, with 768/1440 regressions; no root
overflow. No real-device check. Evidence/read-only preview:
`/tmp/mynewspaper-category-dropdown-review/`, port 5324.
No database changes. Not deployed/committed/pushed; latest verified live release
remains `mynewspaper-web:category-hubs-20261024`.

## Category hub assignments deployed; GitHub check-in — October 24, 2026

User requested production deployment and GitHub check-in. Live image:
`mynewspaper-web:category-hubs-20261024` (`34ec4e8dca9e`), container
`mynewsfeed-web`, healthy. Rollback image:
`mynewsfeed-web:before-mynewspaper-category-hubs-20261024`; prior container retained
as `mynewsfeed-web-before-mynewspaper-category-hubs-20261024`. Runtime environment,
renamed entrypoint, healthcheck, mounts/keys, ports, network and restart policy
verified preserved. Private Tailscale HTTPS passed.

Saved Headlines/Sports/Entertainment assignments and dynamic category tabs are now
live. Admin Categories offers Hub on add/edit, with phone cards and 44px controls.
Enabled sources make future categories available in the assigned hub without code
registration. Migration 019 was already applied to WebScraper after same-day full
backup confirmation and repeated safely; no additional database scripts or
collector changes in this rollout. Initial assignments preserve existing feeds.

Final-source Docker build passed; 161 focused predeployment tests passed. Live
admin controls/cancel, Sports and Entertainment browser suites passed at
320/390/430px light/dark and 768/1440px. Category editor and public screenshots
reviewed, including touch targets and no overflow; source filters, wrong-hub API
rejection, legacy links and JS/no-JS paging passed. Admin browser checks submitted
no category changes; persisted assignments tested in isolated SQL fixtures.
No real-device check available. Twelve health/public route smoke checks passed;
runtime smoke logs had zero failure/unhandled-exception markers. Evidence:
`/tmp/mynewspaper-category-hubs-deploy/`.

Check-in includes accumulated scoreboard consolidation/Show filtering, news hub
organization, myNewspaper project/branding rename and configurable category hubs.
Branch `develop` matched origin before the local commit. GitHub push authentication
preflight failed: HTTPS could not read Username and no usable alternate GitHub
login was available. Local commit can be pushed with `git push origin develop`
after GitHub authentication is restored. No remote push is confirmed.

Earlier local-only category-hub notes below are superseded by this deployment.

## Saved category hub assignments — local web update, October 24, 2026

User requested routing future news categories to their proper hubs and explicitly
chose Headlines/Sports/Entertainment only (no Local). Added Hub selection to Admin
Categories add/edit, defaulting to Headlines. Existing six section categories are
initially assigned to Sports/Entertainment. New eligible categories automatically
appear as tabs in the assigned hub; no code registration/redeploy is needed for
later category additions/assignment changes. Enabled sources are required before a
category appears publicly. No name guessing. ID-based new-category links survive
renames. Headlines filtering/sources/timestamps/paging use saved assignments;
section API reads reject categories assigned to another hub. Existing short aliases,
College/F1 readers and Home aggregate are retained.

Database setup APPLIED: `db/019_news_category_hubs.sql` adds non-null constrained
SourceCategories.NewsHub with Headlines default, initially routes existing names
without renaming categories or changing sources/articles. Verified target WebScraper
on sql2025 against the preserved runtime connection. Confirmed same-day full backup
(msdb backup_finish_date 2026-10-24 07:35:01, recorded server value). Migration
succeeded and repeat application preserved assignments (4 sports, 2 entertainment,
7 headlines). Old live app remains healthy. No collector changes.

Build/publish and 161 focused .NET tests passed, including SQL temp-table fixtures
for arbitrary future categories, persisted hub moves/renames and Headlines/source
exclusion. Public Sports/Entertainment browser suites passed: generic ID routes,
cross-hub API rejection, redirects, source filters, JS/no-JS pagination, mobile
320/390/430 light/dark and 768/1440 regressions. Admin add/edit/cancel controls were
exercised without submitting category data; persistence covered by isolated SQL
fixtures. Phone category list now uses cards; all category editor buttons/selects have
verified 44px minimum targets, with no internal phone overflow. Final admin suite
passed at 320/390/430/768/1440 in both emulated color schemes. Screenshots reviewed; no real-device
check. Evidence: `/tmp/mynewspaper-category-hubs-review/`; final preview port 5323.

Web changes are NOT deployed/committed/pushed. Database prerequisite is already
applied; latest live web remains `mynewspaper-web:brand-20261024`. Next deployment
must include updated model/queries/admin/hub page together. Later category edits do
not require another migration or deployment.

## myNewspaper rename and final logo deployed — October 24, 2026 (Central)

User authorized deployment. Live image `mynewspaper-web:brand-20261024`
(`e3db9a7eea2a`), container `mynewsfeed-web`, healthy. Rollback image:
`mynewsfeed-web:before-mynewspaper-brand-20261024`; prior container retained as
`mynewsfeed-web-before-mynewspaper-brand-20261024`. Entrypoint deliberately changed
to `dotnet myNewspaper.Web.dll`. Environment, health check, mounts/keys, ports,
network and restart policy verified preserved. Existing WebScraper runtime database
retained; no database scripts, schema changes or collector/source changes.

App/project branding is now exactly `myNewspaper`; the final newspaper “n” logo
includes the requested flowing ribbons and sunshine. Header/favicon, manifest,
192px/512px/maskable icons and offline screen use the revised artwork. PWA identity
and browser storage keys remain stable; worker cache is `mynewsfeed-pwa-v4`.
SQL saved-list/import tables and legacy authentication/data-protection identifiers
remain unchanged. Hostname, repository and checkout folder retain existing names.

Final-source Docker build passed; predeployment clean build/publish, 153 focused
.NET tests, five solar-time JS tests and theme-mode regressions passed. Live
branding, HTTPS PWA, Sports and Entertainment browser suites passed: logo/style
assets, preserved preferences, category/source filters, JS/no-JS paging, active
navigation, manifest dimensions, Chromium installability, old app-cache removal
with unrelated cache retained, and offline screen. Mobile at 320/390/430px light/
dark plus 768/1440px checked; screenshots reviewed and 44px brand/navigation
links exercised. No real-device check available. Health/public-route smoke and
private Tailscale HTTPS passed; smoke runtime logs had zero failure/unhandled
exception markers. Evidence: `/tmp/mynewspaper-brand-deploy/`.

Not committed/pushed as part of this request. Earlier local-only rename/logo notes
below are superseded by this release.

## myNewspaper rename and ribbon/sun logo — local update, October 24, 2026

User requested app/project name `myNewspaper` and a new logo, then asked to retain
the former logo's ribbons and sunshine. Renamed solution to `myNewspaper.sln`,
projects/directories to `src/myNewspaper.Web` and `tests/myNewspaper.Tests`, with
matching namespaces, assembly/style references, page/admin titles and user agents.
Dockerfile/Compose image naming and maintained build/deploy commands updated.
The repository/checkout folder, hostname and runtime container are not renamed.

Built-in image generation produced a folded-newspaper “n” logo; final edit adds
flowing gold/light-blue paper ribbons and a golden sunrise, using the prior logo
as reference. Final source: `docs/assets/mynewspaper-ribbon-icon-source.png`.
Prompts/provenance: `docs/assets/mynewspaper-logo.md`. Browser canvas exports:
`wwwroot/icons/newspaper-ribbon-192.png`, `newspaper-ribbon-512.png`, and inset
`newspaper-ribbon-maskable-512.png`. Header, favicon, manifest and offline screen
use the revised icons. Earlier source/logo assets are preserved.

Manifest name/short_name are exactly `myNewspaper`; id/start URL/scope remain `/`.
Worker cache advances to `mynewsfeed-pwa-v4`, purging only older app caches.
Persistent contracts remain unchanged: SQL MyDailyFeedSavedList/Imports, legacy
browser storage keys, MyNewsFeed.Admin cookie, MyNewsFeed data-protection name,
UserSecretsId, key volume and container identity. No SQL/source/collector writes.

Clean solution build/final publish passed with existing dependency/analyzer
warnings. 153 focused .NET tests and five solar-time JS tests passed. Final-logo
branding/PWA/Sports/Entertainment browser suites passed: saved preference keys,
manifest/icon dimensions, Chromium installability, old-cache cleanup with unrelated
cache preserved, offline branding, category links/filtering and JS/no-JS paging.
Phone 320/390/430px light/dark and tablet/desktop 768/1440px checked with screenshots
reviewed; 44px header link and mobile navigation tested. No real-device check.
Theme-mode browser checks also passed (automatic boundaries, persistence, manual
overrides, navigation, cross-tab changes and blocked storage). Seventeen public/health
routes and unauthenticated admin redirect passed. A stale CSS-isolation scope after renaming was fixed by moving generated bin/obj
outputs aside and rebuilding; HTML/CSS scopes now match. A Docker preview build
validated renamed paths/entrypoint before the final artwork revision; it is not a
final deployment image. Final read-only published preview: http://127.0.0.1:5319.
Evidence: `/tmp/mynewspaper-brand-review/`.

Local only, not deployed/committed/pushed. Latest verified live release remains
`mydailyfeed-web:headline-menu-20261024`. Future deployment must use
`dotnet myNewspaper.Web.dll` as Entrypoint when cloning the old runtime config.

## Headlines menu order deployed — October 24, 2026 (Central)

User requested deployment. Live image `mydailyfeed-web:headline-menu-20261024`
(`989829a7c0f4`), container `mynewsfeed-web`, healthy. Sports headlines and
Entertainment headlines are first in their respective desktop/mobile menus.
Rollback image `mynewsfeed-web:before-headline-menu-20261024`; previous container
retained as `mynewsfeed-web-before-headline-menu-20261024`. Environment, entrypoint,
healthcheck, mounts/keys, ports, network and restart policy verified preserved.
No database or collector changes.

Docker build, live health/public-route smoke checks and private HTTPS passed.
Focused live browser checks passed at 320/390/430/768/1440px in light/dark,
including order, active states, mobile link clicks, 44px targets, overflow and
screenshot review. No real-device check available. Runtime smoke logs showed zero
failure/unhandled-exception markers. Evidence: `/tmp/mydailyfeed-headline-menu-deploy/`.
Not committed/pushed as part of this request. Earlier local-only menu-order notes
below are superseded by this release.

## Headlines first in section menus — local update, October 24, 2026

Moved Sports headlines and Entertainment headlines to the first position in their
respective desktop/mobile menus. Publish and focused browser checks passed at
320/390/430/768/1440px light/dark, including link order, active states, mobile
44px targets, real link clicks, overflow and screenshot review. No real-device
check. Evidence/read-only preview: `/tmp/mydailyfeed-headline-menu-review/`,
port 5316. Local only; not deployed/committed/pushed. Latest verified live release
remains `mydailyfeed-web:news-hubs-20261024`.

## Headline hubs deployed — October 24, 2026 (Central)

User requested deployment. Live image: `mydailyfeed-web:news-hubs-20261024`
(`49522a036460`), container `mynewsfeed-web`, healthy. Rollback image:
`mynewsfeed-web:before-news-hubs-20261024`; previous container retained as
`mynewsfeed-web-before-news-hubs-20261024`. Runtime environment, entrypoint,
healthcheck, persistent mounts/keys, ports, network and restart policy verified
preserved. No database migration or collector/source changes.

Sports and Entertainment each have one Headlines menu entry with category tabs:
Sports General/basketball/Big 12/F1, Entertainment General/TV-Film. Legacy URLs and
College/F1 news contexts remain available. Category sources and paging stay scoped.

Docker build passed; eight focused predeployment tests passed. All three live
Sports/Entertainment/Box Office browser suites passed, including tabs, legacy links,
source filters, JS/no-JS paging, active navigation and Headlines exclusion. Phone
checks at 320/390/430px light/dark included screenshot review and 44px targets;
tablet/desktop regressions passed. No real-device check available. Health/public
route smoke checks and private Tailscale HTTPS passed; runtime logs showed zero
failure/unhandled-exception markers in the smoke check. Evidence:
`/tmp/mydailyfeed-news-hubs-deploy/`. Not committed/pushed as part of this request.
Earlier local-only hub notes below are superseded by this deployment.

## Headline hubs — local update, October 24, 2026 (Central)

User requested similar hub pages instead of separate headline menu links. Sports
now has one Sports headlines menu link (`/sports/news`) with General, Men's
college basketball, Big 12 and F1 switching tabs. Entertainment has one
Entertainment headlines link (`/entertainment/news`) with General and TV/Film tabs.
Hub titles and mobile Browse context stay consistent while the selected category
heading/tab and feed change. Categories scope sources, counts, timestamps and
JS/no-JS pagination. Switching tabs starts the selected feed at its newest items.

Canonical child routes: `/sports/news/basketball`, `/sports/news/big12`,
`/sports/news/f1`, `/entertainment/news/tv-film`. Old basketball/TV-Film paths remain
working aliases; old enabled numeric category bookmarks redirect to hub tabs.
Existing `/college/news` and `/f1/news` remain in their College/F1 hubs. No data,
source/collector configuration or schema changes. Headlines exclusions remain.

Build/publish and eight focused tests passed. Updated Sports/Entertainment browser
suites and Box Office regression passed at 320/390/430/768/1440px light/dark (Box
Office at phone widths/1440), including screenshot review, 44px targets, active
navigation, tab clicks, scoped filtering/paging and no-JS navigation. No real-device
check. Evidence/read-only preview: `/tmp/mydailyfeed-news-hubs-review/`, port 5315.
Local only, not deployed/committed/pushed. Latest verified live release remains
`mydailyfeed-web:section-news-20261024`.

## Section headline reorganization deployed — October 24, 2026 (Central)

User requested deployment of the pending Sports, Entertainment/TV-Film and Box
Office reorganization. Live image: `mydailyfeed-web:section-news-20261024`
(`c20581f1a473`), container `mynewsfeed-web`, healthy. Rollback image:
`mynewsfeed-web:before-section-news-20261024`; previous container retained as
`mynewsfeed-web-before-section-news-20261024`. Runtime environment, entrypoint,
health check, mounts/keys, ports, network and restart policy verified preserved.
No database migrations or collector/source changes were made.

Sports now contains all four sports headline categories; Entertainment contains
Entertainment/TV-Film headlines and Box Office. Headlines excludes relocated
categories and sources, with legacy category redirects and scoped paging retained.
Existing Big 12/F1 and Box Office routes/readers remain available.

Docker build passed after the eight focused predeployment tests. Live Sports,
Entertainment and Box Office browser suites passed: source filters, legacy links,
JS/no-JS pagination, section exclusion, navigation/active states, 44px targets,
320/390/430px light/dark with screenshot review, and tablet/desktop regressions.
No real-device check was available. Health/public-route smoke checks and private
Tailscale HTTPS passed; runtime logs had zero failure/unhandled-exception markers.
Evidence: `/tmp/mydailyfeed-section-news-deploy/`. Changes are deployed but not
committed or pushed as part of this request. Earlier pending-local entries below
are historical and superseded by this release.

## All sports headline categories moved — local update, October 24, 2026

User clarified that the move includes all sports headline categories. Sports now
contains Sports headlines (`/sports/news`), Men's college basketball headlines
(`/sports/mens-college-basketball/news`), Big 12 News (`/college/news`) and F1 News
(`/f1/news`) on desktop/mobile. Existing Big 12/F1 pages and readers are reused.
Headlines excludes all four categories and their sources/articles, alongside the
two pending Entertainment categories. Old enabled category bookmarks redirect to
the appropriate section, retaining source/search/valid cursor values. New shared
sports feeds have scoped source filters, switching tabs and JS/no-JS pagination.
No article/source/schema/collector writes; Home's aggregate remains unchanged.

Build/clean publish and eight focused tests passed, including connection-local SQL
fixtures. Sports, Entertainment and Box Office browser suites passed. All four
sports destinations checked at 320/390/430/768/1440px in light/dark; mobile
screenshots reviewed, active navigation/44px targets/no overflow checked, plus
source filtering, legacy links and JS/no-JS paging. No real-device check.
Evidence: `/tmp/mydailyfeed-sports-news-review/`; read-only preview port 5314.

Sports and the preceding Entertainment/Box Office work are local, not deployed,
committed or pushed. Latest verified live release remains
`mydailyfeed-web:scoreboard-show-20261024`. Next: user review/deployment request.

## Box Office included in Entertainment move — local addition, October 24, 2026

User added Box Office to the requested reorganization. Its desktop/mobile menu
entry and active parent now belong to Entertainment; removed News entry and
Headlines' Weekend box office shortcut. Results, weekend preview and forecast
routes/data remain unchanged. Publish and both Entertainment browser suites passed;
Box Office tested at 320/390/430/1440px light/dark and without JavaScript, all three
routes, 44px links, active states and no overflow/errors. Mobile screenshots
reviewed; no real-device check. Evidence/read-only preview remains under
`/tmp/mydailyfeed-entertainment-news-review/` (port 5313).

Entertainment headline relocation and this Box Office addition are local, not
deployed/committed/pushed. Live remains `mydailyfeed-web:scoreboard-show-20261024`.
Next: user's review/deployment request.

## Entertainment headline relocation — local update, October 24, 2026 (Central)

User requested moving Entertainment and TV/Film headline areas into Entertainment
and out of Headlines. Added dedicated routes `/entertainment/news` and
`/entertainment/tv-film` under Entertainment navigation on desktop/mobile. Headlines
removes both categories and their sources from its controls/All categories queries
and paging. Existing enabled category links redirect to the appropriate new feed;
source/search/cursor values are retained. Shared cards/save/share, source filtering,
44px switching tabs and JS/no-JS pagination remain. No article/source/schema or
collector writes. Home's aggregate and other feed readers remain unchanged.

Build/clean publish and four focused tests (including connection-local SQL fixtures)
passed. Browser checks cover links/redirects, source filters, API/paging scope,
no-JS navigation, active states and mobile touch targets. Required 320/390/430px
light/dark checks and screenshot review were completed; 768/1440px checked too.
No real-device check. Evidence and local read-only preview:
`/tmp/mydailyfeed-entertainment-news-review/`, http://127.0.0.1:5313.
This change is not deployed, committed or pushed. Live remains
`mydailyfeed-web:scoreboard-show-20261024`. Next: user review/deployment request.

## Combined Show filter deployed — October 24, 2026 (Central)

User requested combining redundant Sports/Following controls and deploying.
Live release: `mydailyfeed-web:scoreboard-show-20261024` (049ec734719a), healthy.
Show groups All sports, Leagues, Conferences and Teams; league follows are not
duplicated. Games remains separate. Team/conference selections imply their league,
preventing contradictory filters. Auto-submit/no-JS fallback remain. Legacy
competition/follow links work (valid follow wins); new links carry one show value.

Build/clean publish, 35 focused tests (including read-only SQL) and four local/live
browser suites passed. Required 320/390/430px light/dark screenshots were captured
and reviewed before and after deployment; responsive automated checks also cover
375/767/768/1440px. Real-device verification was not run. Twelve live public/health
routes and private HTTPS Scoreboard returned 200; no runtime error markers.
Runtime settings/mounts preserved. No schema/collector/credential changes.

Rollback image/container use `before-scoreboard-show-20261024`. Live evidence and
screenshots: `/tmp/mydailyfeed-scoreboard-show-deploy/`; local review evidence:
`/tmp/mydailyfeed-scoreboard-show-review/` (preview stopped). Guides updated.
Changes remain uncommitted; no push attempted. Next: user feedback, no pending task.

## Closing GitHub checkpoint — October 24, 2026 (Central)

User requested a GitHub check-in and to end work for now. This checkpoint includes
the final Results consolidation polish/docs, deployed mobile controls and Home
ordering fixes, browser checks, and the standing mandatory-mobile requirement in
AGENTS.md and the maintained guides. Latest verified deployment remains
`mydailyfeed-web:scoreboard-mobile-20261024` (11296ffaa696); rollback uses
`before-scoreboard-mobile-20261024`. Deployment and live test evidence is below.

Fetched origin before committing: develop matched origin/develop at `3b6e960`.
Earlier notes about these changes being uncommitted are superseded by this
checkpoint. Closing diff checks passed. This check-in adds only documentation;
mobile verification is not applicable to that addition, and the deployed feature
checks already passed. No new deployment or additional implementation was needed.
Push failed because this workspace has no GitHub HTTPS credentials. The checkpoint
is committed locally; after authenticating, run `git push origin develop`.
Verify the current Git/push state when resuming. No outstanding implementation task remains;
follow the user's next request and retain mandatory mobile acceptance checks.

## Standing mobile acceptance requirement — October 24, 2026 (Central)

User explicitly requested documenting mobile as a necessary check for anything we
develop. This requirement is now in root AGENTS.md, README, the interface acceptance
checklist and DEPLOY.md. Every feature/fix, including changes affecting rendered
data, requires affected mobile flows at 320/390/430px minimum in light/dark mode,
interaction and screenshot checks before completion, plus live verification after
deployment. Record evidence/gaps; desktop-only checks are insufficient. Future
sessions must retain this requirement.

This is a documentation-only update with no rendered/interactive impact, so no
new mobile/browser test or deployment was needed. Diff checks passed. No commit
or push performed.

## Scoreboard mobile/order fixes deployed — October 24, 2026 (Central)

User explicitly requested deployment when ready. Live release:
`mydailyfeed-web:scoreboard-mobile-20261024` (11296ffaa696), healthy. Today & Tonight
now precedes Scoreboard, including saved reversed layouts. Sports / All sports
replaces Competition. All three scoreboard selects submit on change; noscript
Apply remains. Phone date navigation uses a centered date above matched Previous/
Next buttons, with Today on its own line. Date limits, filters and unrelated
preferences remain effective.

Clean publish, Docker/static-asset guard and three live browser suites passed.
Targeted widths 320/375/390/430/767/768/1440px verified real light/dark themes,
no-JS fallback, matched 44px controls, no overflow and Home default/legacy order.
Scoreboard/consolidation suites passed filters, links and refresh behavior.
Twelve public/health routes and private HTTPS Scoreboard returned 200; no runtime
error markers observed; live dark phone screenshot reviewed. Runtime settings and
mounts preserved. No schema/collector/credential changes.

Rollback image/container use `before-scoreboard-mobile-20261024`. Release history
and `/tmp/mydailyfeed-scoreboard-mobile-deploy/` hold evidence; local review
artifacts remain under `/tmp/mydailyfeed-scoreboard-mobile-review/`, with its
preview stopped. No commit/push in this turn; changes remain uncommitted.
Next: user feedback; no pending implementation task.

## Results consolidation deployed — October 24, 2026 (Central)

User explicitly requested deployment after local consolidation was finished.
Live release: `mydailyfeed-web:scoreboard-consolidation-20261024` (10d84111ec48),
healthy. Scoreboard now includes current team records, Following and Final scores
filters and enabled MLS/Championship season links. `/results` redirects to the
final-score view, preserving day/follow. Duplicate Results navigation removed;
box-score/admin links point to Scoreboard. College/F1/season result pages and
existing result collection/storage remain intact. Phone filters are aligned.

Local build/clean publish and 34 focused tests (including read-only SQL) passed.
Docker/static-asset guard and three live browser suites passed consolidated
navigation/redirects, filter/date/refresh persistence, responsive JS/no-JS views,
scoreboard recovery/polling/disclosures and cached box-score modal/fallback checks.
Twelve live health/public routes and private HTTPS Results redirect passed;
mobile screenshot reviewed; no runtime error markers observed. Runtime settings
and persistent mounts preserved. No schema, collector or credential changes.

Rollback image/container use `before-scoreboard-consolidation-20261024`.
Release history and `/tmp/mydailyfeed-scoreboard-consolidation-deploy/` contain
live evidence; earlier local review artifacts remain under
`/tmp/mydailyfeed-scoreboard-consolidation/`. Core consolidation was already in
user-created commit `3b6e960`; final polish/docs remain uncommitted. No commit or
push performed in this deployment turn. Next: user feedback; no pending task.

## Scoreboard past-day limit deployed — October 23, 2026 (Central)

User requested yesterday as the earliest past scoreboard day and explicitly
requested deployment. Live release: `mydailyfeed-web:scoreboard-history-20261023`
(f8694fbf28ee), healthy. Both public page and refresh-fragment validation reject
older dates by falling back to today; Previous day navigation stops at yesterday
(Central). Seven days ahead and older active-game carry-over on today remain.
No scraper retention setting or stored rows were changed.

Solution build and Docker/static-asset guard passed with existing warnings. Live
JS/no-JS checks at 320/390/1440px verified yesterday's lower limit, older page and
fragment fallback, seven-day future limit, no overflow/errors and five healthy
routes. Private HTTPS Scoreboard returned 200 with a hostname resolution override.
Runtime settings/mounts compared and preserved. Rollback image/container use
`before-scoreboard-history-20261023`; evidence is under
`/tmp/mydailyfeed-scoreboard-history-deploy/`. Documentation and browser assertion
updated. Changes remain uncommitted; no push attempted. No pending implementation
request remains.

## End-of-night GitHub checkpoint — October 23, 2026 (Central)

The user approved the deployed scoreboard styling and requested a GitHub check-in
to close the session. This checkpoint includes the complete scoreboard feature,
styling revision, browser/SQL tests and updated guides/release evidence. Latest
verified deployment remains `mydailyfeed-web:scoreboard-style-20261023`, healthy;
its rollback image/container use `before-scoreboard-style-20261023`.

Fetched origin before committing: `develop` and `origin/develop` both pointed to
`c027826`, so the earlier theme checkpoint is now present on the remote. Historical
notes below about uncommitted scoreboard work are superseded by this checkpoint.
Build, focused tests and deployment/browser verification are recorded below and
in release history; closing diff whitespace checks passed. No additional code or
deployment was needed for this check-in. The closing push failed because GitHub
HTTPS credentials are unavailable in this workspace; the checkpoint is committed
locally. After authenticating, run `git push origin develop`. Verify Git state when
resuming. No outstanding implementation request remains; follow the user's next
brief. Prior deployment permissions remain session-scoped.

## Scoreboard styling deployed — October 23, 2026 (Central)

After reviewing the local styling and phone checks, the user explicitly requested
deployment. Now live: `mydailyfeed-web:scoreboard-style-20261023` (c52b23d52a89),
healthy. Shared sports badges, fixed score/star columns, consistent Live/Final
pills/actions and aligned footers unify the cards. Full-page metadata lives under
native Game info disclosures; Home uses compact divided rows. Expanded disclosures
survive refresh; stale and collection warnings remain visible.

Clean publish, Docker/static-asset guard, live scoreboard behavior suite and ten
viewport/theme combinations passed. Live phone/desktop checks covered expanded
info, footer alignment, no overflow/JavaScript errors, eight routes and private
HTTPS Scoreboard. Runtime settings/mounts preserved; no schema/collector changes.
Rollback image/container use `before-scoreboard-style-20261023`. Release history
and `/tmp/mydailyfeed-scoreboard-style-deploy/` contain evidence/live screenshots.
The temporary local preview was stopped; review screenshots remain available.
Changes remain uncommitted, with no new push attempt. Next: user's visual review
of the deployed styling.

## Live scoreboard — October 23, 2026 (Central)

User explicitly authorized implementation and deployment in this session.
Implemented and deployed `mydailyfeed-web:scoreboard-20261023` (0585962b33b5),
healthy. Home has a separate six-game card; Sports → Scoreboard offers enabled
competition filters and Central dates from three days back through seven ahead.
Today includes overnight active/paused games. Existing Today & Tonight, schedules,
results and F1 remain independent; no speculative calendar/ESPN identity joining.

Reads use the existing frontend principal against `SportsScoreboard` and
`SportsScoreboardCoverage`. No schema, collector, credentials or write grants were
changed. Five-minute visible-only refresh retains displayed samples on failure;
clocks never tick locally. Separate stable-ID star preferences preserve existing
name-based team choices. Existing box-score detail visibility now also recognizes
public scoreboard games. See sports/interface guides for behavior and limits.

Build, 37 focused tests (including read-only SQL), Docker/static-asset guard,
preview/image/live browser checks, 14 live routes and private HTTPS Scoreboard
passed. Runtime configuration/mounts preserved; no runtime error markers observed.
Rollback image/container use `before-scoreboard-20261023`. Details and evidence
are in release history and `/tmp/mydailyfeed-scoreboard-deploy/`. Temporary previews
were stopped/removed. Changes remain uncommitted; no push attempted for this work.

Earlier GitHub checkpoint committed as `c027826`; push failed because this
workspace has no GitHub HTTPS credentials. `develop` remained one commit ahead
of `origin/develop` before scoreboard implementation. Next: user's review of the
live feature; no additional implementation request pending.

## GitHub checkpoint — October 23, 2026 (Central)

The user requested a GitHub check-in before starting the next larger project.
This checkpoint includes the Daily Ribbon default palette and finishing details,
selected-team row fills, full-width standings striping and selection contrast,
removal of the Selected badge, and the homepage Refresh control. Deployment
evidence is recorded in release history; the latest recorded release is
`mydailyfeed-web:home-refresh-20261023`. Live hosting was not rechecked here.

Reviewed the existing changes and fetched `origin`; `develop` matched its remote
before this checkpoint. Solution build, JavaScript syntax and diff whitespace
checks passed; the build retained six existing dependency/analyzer warnings.
No new implementation or deployment was requested. Preserve the standing
tap-to-link product direction and treat older deployment permissions as historical.
Next: follow the user's larger-project brief.

## Session handoff — October 22, 2026 (Central)

### Latest release: separate Digital / VOD homepage card

Deployed `mydailyfeed-web:digital-card-20261022` (761fae8e3900), healthy.
Upcoming Digital / VOD is now a separate cascading card, available all week.
The theatrical card retains its Thursday–Sunday window. Legacy hidden Movies &
Digital preferences still hide both. Runtime settings/mounts preserved; no schema
changes. Build, Docker static-asset guard and responsive browser checks passed.
Rollback image/container use `before-digital-card-20261022`; see release history
and `/tmp/mydailyfeed-digital-card-deploy/`. Changes remain uncommitted.

### Deployment permission for this session

The user explicitly granted permission to deploy during this October 22, 2026
session. Deployment may proceed within the work authorized in this session,
following the deployment runbook and preserving runtime configuration and data.
This permission applies only to this session; future sessions must not infer
deployment authorization from this historical note. Recording this permission
does not mean a deployment has occurred.

### Scraper coordination: theatrical coverage

Completed frontend review of the scraper handoff on October 22. Scraper restored
the provider with CollectDisc=0 and withdrew retained discs. Read-only checks found
six theatrical, six digital, five subscription scheduled rows and 24 withdrawn
discs. No frontend provider setting/schema changes were needed. Home's theatrical
View all now selects the theatrical Movies view; digital/admin help clarifies
provider-wide enablement versus disc collection policy.

Deployed `mydailyfeed-web:theatrical-completion-20261022` (bb5d0487fb60), healthy;
rollback image/container use `before-theatrical-completion-20261022`. Runtime settings
and mounts preserved. Twenty-eight focused tests passed, including SQL temporary
fixtures; live phone/desktop checks confirmed five Home films, six theatrical
Movies films, provider wording, keyboard navigation and no discs/overflow/JS errors.
See release history for evidence and limits. These updates are uncommitted; earlier
session changes were committed as `3cb517b`. The original request is preserved in
[scraper-theatrical-handoff.md](scraper-theatrical-handoff.md), now historical.

### Homepage cascading layout — deployed

Subsequent release: `mydailyfeed-web:theatrical-window-20261022` (6ce5d5c9c5d8)
shows the current Friday's theatrical releases Thursday–Sunday (display zone),
retaining Friday on Saturday/Sunday and hiding that subsection Monday–Wednesday.
Digital / VOD remains visible all week. Ten date-window tests and live Thursday
responsive/route checks passed; SQL integration tests were updated but not run.
Runtime settings/mounts were preserved; no schema changes. Rollback image and
container use `before-theatrical-window-20261022`. Evidence and limitations are in
release history and `/tmp/mydailyfeed-theatrical-window-deploy/`. Uncommitted.

- Restored shorter-column placement on desktop, with Where to watch and Latest
  news leading. Mobile shows Where to watch first and Latest news second.
  Updated document order, browser preference defaults, CSS and the layout script;
  existing hidden-section and team preferences remain effective.
- Updated the interface guide and consolidated the two homepage layout browser
  check entry points around the current layout. Build passed with six existing
  dependency/analyzer warnings. JavaScript syntax and diff checks passed.
- Browser checks passed at 320, 390, 767, 768, 1023, 1024, 1440 and 1920px,
  including resizing, shorter-column selection, overlap/overflow, dynamic heights,
  hidden cards and no-JavaScript fallback. Initial checks used representative
  cards; the same checks subsequently passed against the live container.
- Deployed `mydailyfeed-web:cascade-priority-20261022` (6eac62c942c8), healthy.
  Runtime settings and persistent mounts were preserved. Rollback image and
  retained container use `before-cascade-priority-20261022`. Checked public routes
  returned 200 over container HTTP; private HTTPS was not separately checked.
  See release history and `/tmp/mydailyfeed-cascade-check/` for evidence.
  No schema changes, commit or push for this release.
- The earlier handoff was committed as `7501336`; its push failed because this
  environment had no GitHub HTTPS credentials. Remote state was not rechecked.

### Initial orientation

- Reviewed the README, topic-guide structure, maintenance notes, release history,
  application startup, homepage, feed queries, shared saved-content implementation,
  and test layout to establish context. No application changes, tests, deployment,
  commits or pushes were performed in this session.
- Working tree was clean before this handoff update. Current HEAD was `92bc9a7`
  (`Add rotating box-office homepage card and mobile content limits`).
- Release history records `mydailyfeed-web:mobile-home-limits-20261021` as the
  latest verified web release. Hosting status was not checked this session.
  Consult release history before the older deployment snapshots below.
- The user agreed to reuse this file for session continuity. At the end of a work
  session, update it with changes, commit/deployment state, checks, unresolved
  work, decisions and a concrete next step. Do not store secrets here or treat
  historical session permissions as current authorization.
- No implementation task is currently pending. Next session: read this note,
  README and current release history, inspect Git state, then follow the user's
  next request. The maintenance guide contains previously documented follow-ups;
  they were not revalidated during this orientation.

## Navigation logo — October 20, 2026

Replaced the MyDailyFeed navigation text with the existing ribbon logo, linked to
Home with an accessible link name. The logo is 44px square on desktop and mobile.

Deployed `mydailyfeed-web:header-logo-20261020` (2f8b334e4277), healthy.
Rollback image `mynewsfeed-web:before-header-logo-20261020` (8f1c56409f05), retained
container `mynewsfeed-web-before-header-logo-20261020`. Runtime settings, mounts,
ports and keys preserved; no schema changes. Local build and Docker publish/static-
asset guard passed with existing PKCS warnings. Live browser checks passed image
loading, dimensions, accessible Home link and navigation, no horizontal overflow
at 390/768/1440px and no JavaScript errors. Private HTTPS health returned 200.
Private deployment files: `/tmp/mydailyfeed-header-logo-deploy/`.
No commit or push for this release.

## Homepage watch link and simplified Settings/TV — October 20, 2026

Replaced the homepage Customize Home link with Where to watch beside Kansas City.
Removed Your homepage from Settings and its section-order controls. Existing browser
homepage preferences still apply; an all-sections-hidden homepage offers a restore
button instead of the removed Settings destination. Removed Find a tracked series
from TV & Streaming and disconnected its q parameter from filtering/navigation.

Deployed `mydailyfeed-web:home-links-20261020` (8f1c56409f05), healthy.
Rollback image `mynewsfeed-web:before-home-links-20261020` (5209576be142), retained
container `mynewsfeed-web-before-home-links-20261020`. Runtime settings, mounts,
ports and keys preserved; no schema changes. Local build, Docker publish/static-asset
guard and diff checks passed, with existing PKCS warnings. Live phone/desktop browser
checks passed the watch link/navigation, removed customization link and Settings
section/order row, removed TV search, calendar navigation and no JavaScript errors.
Public route checks and private HTTPS health returned 200. Private deployment files:
`/tmp/mydailyfeed-home-links-deploy/`. No commit or push for this release.

## Home source note and video rotation — October 20, 2026

Moved the digital-release source/date note beside its heading using the Latest News
note style; heading text wraps on narrow screens. Home Latest Videos selects up to
10 channels, one newest video per channel, with a stable random selection that
rotates when successful video collection advances to another UTC hour. Selected
videos display newest first; the full Videos page is unchanged.

Deployed `mydailyfeed-web:home-rotation-20261020` (5209576be142), healthy.
Rollback image `mynewsfeed-web:before-home-rotation-20261020` (52c918dae944),
retained container `mynewsfeed-web-before-home-rotation-20261020`. Runtime
settings, mounts, ports and persistent keys preserved. No schema changes.
Docker publish/static-asset guard and focused rotation test passed (existing PKCS
warnings). Live browser checks passed the inline source note, exactly 10 distinct
video channels, rotation label and no horizontal overflow at 390/768/1440/1920px;
health and public route checks passed. Private HTTPS health returned 200 using
an explicit hostname resolution override from the workspace. Private deployment
files: `/tmp/mydailyfeed-home-rotation-deploy/`. Source and release documentation
committed for push to `origin/develop` after deployment.

## Cascading desktop homepage — October 20, 2026

Home now uses two cascading desktop columns rather than fixed rows with large
empty areas beside taller tiles. home-layout.js measures natural heights and sets
grid spans; observers handle image/content resizing, hidden cards, reordering and
enhanced navigation. Settings DOM order is retained, including customized layouts.
Card internals fit the narrower columns. Below 1024px existing stacked layout is
retained; without JavaScript desktop falls back to a normal two-column grid.
Different final column heights can still leave bottom whitespace.

Deployed mydailyfeed-web:cascade-20261020 (52c918dae944), healthy. Rollback image
mynewsfeed-web:before-cascade-20261020 (46dd954c61ef), retained container
mynewsfeed-web-before-cascade-20261020. Private deployment files/screenshots:
/tmp/mydailyfeed-cascade-deploy/. Runtime settings/mounts/ports preserved; private
HTTPS health returned 200. Local build and Docker publish/static-asset guard passed
with existing PKCS warnings. Preview checks passed two-column packing, no overlap,
small inter-card gaps, dynamic height, hidden/reordered sections, enhanced Home
navigation, desktop/mobile resizing and no-JavaScript fallback. Desktop screenshot
reviewed. Live browser verification also passed the full layout matrix and no-JavaScript
fallback (external images blocked in that fallback check to avoid image-load delays).
No schema changes, commit or push.

## Direct YouTube and Saved mobile links — October 20, 2026

Mobile Browse now has a direct YouTube link to /videos and a direct Saved articles
link to /saved. Removed the YouTube and Your links headings so neither destination
appears as a child navigation item. Sports and Entertainment retain their grouped
subnavigation. Settings/admin remain direct links.

Deployed mydailyfeed-web:mobile-links-20261020 (46dd954c61ef), healthy. Rollback
image mynewsfeed-web:before-mobile-links-20261020 (1687dfffcfb2), retained container
mynewsfeed-web-before-mobile-links-20261020. Private deployment snapshots/scripts:
/tmp/mydailyfeed-mobile-links-deploy/. Runtime settings/mounts/ports preserved;
private HTTPS health returned 200. Docker publish/static-asset guard passed with
existing PKCS warnings. Live browser checks passed direct-link structure/destinations, no redundant headings,
Sports/Entertainment grouping, theme/phone/tablet layouts, menu closing and mixed
search/sort/detail/clear flows without JavaScript errors. No schema changes,
commit or push for this release.

## Combined title search and clearer mobile subnavigation — October 20, 2026

Where to watch title queries now use TMDB search/multi, showing movies and TV
together and excluding people/adult results. Cards retain media labels and the
correct detail/provider routes. Search hides type tabs; clearing returns to the
originating movie/TV browse mode. Browse tabs, genres, active-TV filtering, services,
sorting and filled logical pages remain. Deduplication uses (kind, ID), preserving
a movie and TV title that share the same numeric provider ID.

Mobile Browse gives Sports and Entertainment named groups, stronger parent labels,
borders and indented sublinks with a vertical guide. Existing destinations, touch
targets, current-page indicators, menu scrolling and closing behavior remain;
desktop dropdowns are unchanged.

Deployed mydailyfeed-web:mixed-nav-20261020 (1687dfffcfb2), healthy. Rollback image
mynewsfeed-web:before-mixed-nav-20261020 (85cb568ecbfd), retained container
mynewsfeed-web-before-mixed-nav-20261020. Private snapshots/scripts/screenshots:
/tmp/mydailyfeed-mixed-nav-deploy/. Runtime settings/mounts/ports preserved;
private HTTPS health returned 200. Docker publish/static-asset guard passed with
existing PKCS warnings. All 26 focused catalog/sort/paging/mixed-search tests passed.
Preview mixed-search/navigation, existing watch and sort browser suites passed
(including no-JavaScript). Mobile screenshot reviewed. Live browser verification
also passed mixed search, sort/detail/clear flows and grouped navigation at phone
and tablet widths in light/dark themes, without overflow or JavaScript errors. No schema changes, commit or push for this release.

## Simplify Saved page — October 20, 2026

Removed the Reading list / All saved selector and saved-article total shown in the
user's screenshot. Saved always displays the full shared list; imported read
metadata does not hide cards. Removed obsolete filter/count markup, handlers,
styles and Show all saved fallback. Header bookmark badge and sync/error status
remain. Rendering signature includes readiness, fixing initial empty-list guidance.

Deployed mydailyfeed-web:saved-toolbar-20261020 (85cb568ecbfd), healthy. Rollback
image mynewsfeed-web:before-saved-toolbar-20261020 (bea9cc17cbdf), retained
container mynewsfeed-web-before-saved-toolbar-20261020. Private snapshots/scripts:
/tmp/mydailyfeed-saved-toolbar-deploy/. Runtime settings/mounts/ports preserved;
private HTTPS health returned 200. Docker publish/static-asset guard passed with
existing PKCS warnings. Shared-Saved browser checks include initial empty guidance,
removed toolbar/count, imported read-marked bookmarks visible, independent-device
save/remove/polling, migration replay protection, CSRF/unsafe links, blocked storage,
failure/retry, offline recovery and phone/desktop layout. No schema changes for
this UI simplification. No commit or push made for this release.

## Shared Saved articles across devices — October 20, 2026

Saved now uses a single server-backed list shared by everyone who can access the
site, with no new login screen. Article copies survive scraper retention and
redeployments. Devices refresh every 15 seconds while visible, on focus/reconnect
and enhanced navigation. Saving/removing updates the shared list; other personal
settings remain local. Saved page copy explains the shared list.

Applied idempotent db/018_shared_saved_articles.sql: app-owned singleton JSON list
and UUID import-receipt tables. Serializable SQL writes prevent lost updates.
Explicit add/remove is idempotent; duplicate URLs merge. Existing browser bookmarks
import once, retaining local backup and server receipts to prevent resurrection
after removal/retries. Visit each original browser/address to import its local list
(HTTP/IP and HTTPS have separate storage). New saves work with storage blocked.
Offline writes are not queued; errors preserve confirmed state and retry on sync.
Capacity is 500 articles, with visible errors rather than silent eviction.

GET /api/saved is no-store and supplies antiforgery tokens; JSON write routes
validate X-Saved-CSRF, HTTP(S) links, bounded text and request sizes. All saved
text remains rendered through DOM textContent. Schema is independent of scraper
rows. Keep the added tables on rollback and include them in SQL backups.

Deployed mydailyfeed-web:shared-saved-20261020 (bea9cc17cbdf), healthy. Rollback
image mynewsfeed-web:before-shared-saved-20261020 (2db42b038e39), retained container
mynewsfeed-web-before-shared-saved-20261020. Private snapshots/scripts/logs:
/tmp/mydailyfeed-shared-saved-deploy/. Runtime settings, mounts, ports and networks
preserved; private HTTPS health returned 200. Docker publish/static-asset guard
passed with existing PKCS dependency warnings. Six focused tests passed, including
a temporary isolated SQL database covering concurrent writers, deduplication, import
replay, persistence and capacity rollback. Preview personal-features checks passed.
Preview and live shared-Saved browser suites cover independent devices, real polling,
legacy import including UUID fallback, removal/replay, CSRF/unsafe links, blocked
storage, failure/retry, offline recovery and phone/desktop layout. Test bookmarks
are removed after checks. No commit or push made for this release.

## Remove article read and mute actions — October 20, 2026

Removed Mark read/Mark unread and Mute publisher from Home and shared article
cards, and removed Mark read/Mark unread from Saved. Deleted ReadingControls,
its styles and the corresponding click handlers. Bookmark and Share remain.
Settings copy now describes preferred category and previously stored preferences;
legacy muted publishers/read filters can still be reset there. This supersedes
the previous toolbar release's statement that article read/mute actions remain.

Deployed mydailyfeed-web:reading-actions-20261020 (2db42b038e39), healthy. Rollback
image mynewsfeed-web:before-reading-actions-20261020 (3242ba1a923b), retained
container mynewsfeed-web-before-reading-actions-20261020. Private deployment
snapshots/scripts: /tmp/mydailyfeed-reading-actions-deploy/. Docker publish and
static-asset guard passed, with existing PKCS dependency warnings. JavaScript
syntax and diff whitespace checks passed. Live personal-features browser suite
passed saving, homepage/customization, category, cross-tab, blocked-storage and
no-JavaScript checks. Additional checks confirmed removed actions absent on Home,
News, Local, F1 News, college news and Saved at 390px/1440px without overflow.
Private HTTPS health returned 200; runtime settings/mounts/ports preserved.
No commit or push made for this release.

## Remove inline reading toolbar — October 20, 2026

Removed the screenshot's Hide read stories checkbox, reading-preferences shortcut
and loaded-story status hint from News, Local, F1 News and college news. Deleted
the unused shared component, toolbar CSS and counter rendering. Settings reading
controls and article actions remain available. Updated the existing personal
features browser check to adjust Hide read stories in Settings.

Deployed mydailyfeed-web:reading-bar-20261020 (3242ba1a923b), healthy. Rollback:
mynewsfeed-web:before-reading-bar-20261020 (114e537b3da9), retained container
mynewsfeed-web-before-reading-bar-20261020; private snapshots/scripts under
/tmp/mydailyfeed-reading-bar-deploy/. Docker publish/static-asset guard passed
with existing PKCS warnings. Live browser checks passed all four feeds at 390px
and 1440px, no toolbar/overflow, Settings hide-read behavior and no JS errors.
Private HTTPS health returned 200; runtime settings/mounts/ports preserved.
No commit or push made for this release.

## MyDailyFeed ribbon app icon — October 20, 2026

Deployed user-selected concept 4: white ribbon d, feed cards and sunrise on blue.
Source preserved in docs/assets/mydailyfeed-icon-source.png (built-in imagegen);
192/512px exports and an inset maskable 512px export use daily-ribbon filenames.
Manifest, favicon and offline screen updated; service worker cache advanced to v3.
App identity/scope and legacy files retained. Installed icon refresh is controlled
by the browser/OS; reinstall if necessary. No application data/schema changes.

Live image mydailyfeed-web:icon-20261020 (114e537b3da9), healthy; rollback image
mynewsfeed-web:before-icon-20261020 (57976dba3956), retained container
mynewsfeed-web-before-icon-20261020. Private deployment snapshots/scripts:
/tmp/mydailyfeed-icon-deploy/. Runtime settings, mounts, ports and networks
compared and preserved. Docker publish/static-asset guard passed, with existing
PKCS dependency warnings. Live HTTPS health returned 200. Browser checks passed
icon dimensions, maskable entry, stable manifest identity, Chromium installability,
v3 worker/cache and offline navigation. Device installation was confirmed by the
user before this icon change; on-device icon refresh still needs user observation.
No commit or push made for this release.

## Latest deployment — filled viewing pages, October 20, 2026

Live mydailyfeed-web:fill-pages-20261020 (57976dba3956), healthy. Rollback image:
mynewsfeed-web:before-fill-pages-20261020 (18cf4b587629), retained container
mynewsfeed-web-before-fill-pages-20261020. Private files:
/tmp/mydailyfeed-fill-pages-deploy/. Runtime preserved; private HTTPS health 200.

WatchCatalog.FilledPage builds stable logical pages of up to 20 eligible titles
from a 15-minute query window (32-window cache). It deduplicates provider IDs and
retains surplus entries for Next/Previous. Sorting and selected-service badges
are applied after membership selection; do not re-filter cached membership for
each user, which would shift boundaries. List remains a raw-batch compatibility
wrapper; FilledPage uses ListBatch for up to 50 upstream pages, ten new batches
per request and ten visible pages. Incomplete scans/source failures show a same-
page Continue action. Unknown availability retains the existing failure behavior.

25 focused tests plus live paging/watch/sort/US-options checks passed: full TV/
movie pages, 40 unique TV titles across first two pages, stable back navigation,
ended-title search and 60 actual viewing-option details. No commit/push was made;
user manually handles pushes when asked. Session deployment permission remains
active. This supersedes earlier release records and the no-refill limitation.

## Latest deployment — US availability filtering, October 20, 2026

Live mydailyfeed-web:us-options-20261020 (18cf4b587629), healthy. Rollback image:
mynewsfeed-web:before-us-options-20261020 (ef6f7f3e7cdb), retained container
mynewsfeed-web-before-us-options-20261020. Private files:
/tmp/mydailyfeed-us-options-deploy/. Runtime preserved, HTTPS health 200.

WatchCatalog.Prioritize now also filters confirmed no-US-option titles from every
listing, including title searches and sessions with no selected services. Access
metadata checks all five offer categories; selected-service matches still use
only subscription/free/ads. Malformed/failing checks remain unknown and visible.
Pages can shrink; pagination remains available on empty filtered pages. Detail
fallback is preserved for saved links or availability changes. No base cached
listing mutation. 21 focused tests and live US-options/watch/sort suites passed;
36 retained cards checked for actual US offers. No commit/push was made. Session
deployment permission remains active. This supersedes earlier release records.

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
