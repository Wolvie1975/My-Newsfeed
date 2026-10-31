# Release history and verification

Historical evidence consolidated October 13, 2026 (Central), from the former individual
handoffs and deployment reports. Counts are observations for their original checks,
not permanent acceptance totals. Recorded rollback tags describe those deployments;
verify an image still exists before using it. Runtime configuration and persistent
keys were preserved by the documented releases unless otherwise stated.

## Tablet paper background through gutters — October 31, 2026 (Central)

The user’s iPad/Brave screenshot showed the lighter content paper stopping at the
24px gutters, leaving a darker band behind the edge-to-edge poster shelves. The
744–1023px shell now uses `--surface` when its page contains `tab-desktop`, matching
the content background across the viewport. The existing shell minimum height also
covers short pages. The fix applies to Watch and YouTube, and future pages inherit
it when they opt in. Content spacing, unconverted fronts, phones and desktop retain
their existing styles. No markup, query, schema, collector or admin changes.

Solution build passed with zero warnings/errors; formatting and diff checks passed.
Preview/live Chromium verification covered Watch and YouTube at 320/390/430/743/
744/820/834/1023/1024/1440/2560px in light/dark. Assertions compare computed shell
colors with the expected surface/background, check layout visibility, overflow,
saved themes and Refresh. Phone menus, YouTube category navigation, Watch title
search and no-JavaScript search/navigation were exercised. Monochrome/Cardinals
were checked at 820px, as were unchanged Home/News/Sports/Entertainment/Settings
backgrounds and phone content. Screenshots were inspected. Physical iPad/Brave,
Safari and Firefox follow-up remains unverified; the original screenshot is user
provided, and the fix checks use browser emulation.

Evidence and disposable-profile verification script:
`test-results/browser/tablet-paper-{preview,live}/`,
`test-results/browser/tablet-paper-release/` and `test-results/tablet-paper.cjs`.
Public image `mynewspaper-web:tablet-paper-20261031` (`ceda4735dc35`) preserves runtime
settings, persistent keys, ports, network aliases and restart policy. Deployed CSS
matches source; public/admin health returned Healthy and admin was unchanged.
Rollback container `mynewsfeed-web-before-tablet-paper-20261031` and image
`mynewspaper-web:before-tablet-paper-20261031` (`2f39c4695ac3`) were retained.

## iPad YouTube layout — October 30, 2026 (Central)

YouTube now opts in to the 744–1023px tablet layout. Its category strip scrolls beside
the channel selector, with the count on the next row. Featured spans the page above
Latest and the By channel / Watch later sidebar; five picks retain their existing
progress/title presentation. More videos uses three columns. Two desktop-only CSS
blocks start at 744px after checking phone components, and sidebar link styles are
copied into tablet scope. The shared browse wrapper uses `display: contents` outside
tablet widths, including the existing heading spacing when there are no categories.
Other fronts retain their phone presentation; Watch retains its completed tablet
layout. No query, schema, collector, carousel-script or admin changes were required.

Solution build passed with zero warnings/errors; browser-test ESLint, repository
formatting and diff checks passed. Preview/live Chromium checks covered 320/390/430/
743/744/820/834/1023/1024/1280/1440/1920/2560/3840px in light/dark. Checks covered
wrapping/overflow, the tablet grid and sidebar, 44px carousel/actions, five picks,
three tablet card columns, existing desktop column counts, Refresh, saved themes,
phone menus/folds and no-aside fallback. Monochrome/Cardinals were checked at 820px.
Interactions exercised arrows/picks, no rotation before Play, Play/Pause rotation,
a real emulated touch swipe on the category strip at 744px, category/channel filters,
paging, row/card Watch later persistence/removal and Share with a test clipboard.
No-JavaScript filtering and phone navigation passed. Watch regression and isolation
of the remaining fronts were checked. Screenshots were inspected. Tests use their
own temporary profiles; household Saved lists and preferences were untouched.
Physical iPads, Safari and Firefox were unavailable; width changes cover orientation
breakpoints without claiming physical-device rotation.

Evidence: `test-results/browser/ipad-youtube-{preview,live}/` and
`test-results/browser/ipad-youtube-release/`. Updated [iPad handoff](ipad/README.md);
Guide 03 remains separate.

Public image `mynewspaper-web:ipad-youtube-20261031` (`2f39c4695ac3`) preserves the
actual runtime environment, key mount, ports, network aliases and restart policy.
Both changed deployed stylesheets match source. Public/admin health returned Healthy;
admin image was unchanged. Rollback container `mynewsfeed-web-before-ipad-youtube-20261031`
and image `mynewspaper-web:before-ipad-youtube-20261031` (`805c90d1b4b0`) were retained.

## iPad foundation and Where to watch — October 30, 2026 (Central)

Added the 744–1023px tablet stylesheet and per-page opt-in wrappers. Every public
page receives a two-row tablet header with all six main sections, a search shortcut,
Settings icon and initial-only edition switch. Settings anchor offsets account for
the taller header. Watch is the first opted-in page: 48px heading, full-width search,
compact services/browse row and 150px sideways-scrolling poster shelves with a fade.
Tablet arrows are hidden; desktop arrows and phone Watch remain available. Other
pages retain their phone content at tablet widths. The foundation was checked before
Watch opted in. No database, catalog-query, scraper or admin changes were required.

Solution build passed with zero warnings/errors, browser-test ESLint, repository
formatting and diff checks passed. Chromium preview/live checks covered 320/390/430/
743/744/820/834/1023/1024/1280/1440/2560px in light/dark, plus Monochrome and
Cardinals at 744px. Assertions and screenshots cover header rows, 44px targets,
page overflow, page isolation, theme persistence, Refresh and breakpoint transitions.
A DOM-only six-logo/+12 fixture checks the crowded services bar. A touch gesture
scrolls a poster shelf; desktop arrow navigation, Movies/TV tabs, My services,
Arrival search, automatic sorting and Manage were exercised. No-JavaScript tablet
search/sort and phone search also passed. Tests create/delete disposable reader
profiles. Physical iPads, Safari and Firefox were unavailable; viewport changes
exercise orientation breakpoints without claiming physical rotation.

Evidence: `test-results/browser/ipad-foundation-preview/`,
`test-results/browser/ipad-{preview,live}/` and `test-results/browser/ipad-release/`.
Maintained handoff: [iPad rollout](ipad/README.md). Guides 02 onward remain separate.

Public image `mynewspaper-web:ipad-watch-20261031` (`805c90d1b4b0`) preserves the
actual runtime environment, persistent keys, ports, network aliases and restart
policy. Both deployed CSS files match source. Public/admin health returned Healthy;
admin image was unchanged. Rollback container `mynewsfeed-web-before-ipad-watch-20261031`
and image `mynewspaper-web:before-ipad-watch-20261031` (`a07e6904abab`) were retained.

## Entertainment navigation cleanup — October 30, 2026 (Central)

Removed Where to watch from the shared Entertainment section choices, including
its desktop tabs and phone dropdown. The main navigation retains the standalone
Where to watch page. No query, schema, scraper, CSS or admin changes were needed.

Solution build passed with zero warnings/errors; diff checks passed. Preview/live
Chromium verification used a disposable reader profile at 320/390/430/768/1440/
1920/2560px in light/dark. Checked absence from tabs/options, remaining Movies
navigation, 44px phone select, no horizontal overflow, persisted themes, Refresh,
the main Watch link and the no-JavaScript phone select/Go fallback. Representative
phone/desktop screenshots were inspected. Physical devices, Firefox and Safari
were unavailable. Evidence and verification script are in
`test-results/browser/entertainment-nav-{preview,live}/` and
`test-results/entertainment-nav.cjs`.

Public image `mynewspaper-web:entertainment-nav-20261031` (`a07e6904abab`) preserves
runtime settings, persistent keys, ports, network aliases and restart policy.
Public/admin health returned Healthy; admin was unchanged. Retained rollback
container `mynewsfeed-web-before-entertainment-nav-20261031` and image
`mynewspaper-web:before-entertainment-nav-20261031` (`4151d3b9a324`).

## Home sports-only score area — October 30, 2026 (Central)

Removed New tonight from Home's Live now/Coming up area at every width, leaving
that area for sports scores and the Scoreboard link. Today’s episodes and the TV
calendar remain available. Deleted the unused summary component, its rendering
tests and its CSS. No data, schema, scraper or admin changes were required.

Solution build passed with zero warnings/errors; changed browser-test ESLint,
repository formatting and diff checks passed. Preview/live Chromium checks covered
320/390/430/768/1024/1280/1440/1920/2560/3200/3840px in light/dark, plus wide
Monochrome/Cardinals themes. Checks covered absence of the TV summary/link, score
row geometry, overflow, Refresh, saved themes, phone folds and 44px summary targets,
header search, other public routes and no-JavaScript controls. Representative phone
and wide screenshots were inspected. No physical-device, Firefox or Safari checks
were available. Evidence: `test-results/browser/sports-only-{preview,live}/`.

Public image `mynewspaper-web:sports-only-20261031` (`4151d3b9a324`) preserves the
actual runtime environment, key mount, ports, network aliases and restart policy.
Public/admin health checks returned Healthy; the admin image was unchanged.
Rollback container `mynewsfeed-web-before-sports-only-20261031` and image
`mynewspaper-web:before-sports-only-20261031` (`ad8e9d750baa`) were retained.

## Home episode-card spacing — October 30, 2026 (Central)

Fixed the sidebar's inherited two-column show grid, which left the episode text
squeezed beside a poster in half of an already narrow card. From 1024px, each show
occupies a full row. Its poster/platform/title share a compact header, with every
episode and its metadata spanning the full card width below. Multiple shows stack,
series titles remain readable, and episode links have 44px targets. Phone fronts
and the full TV calendar keep their existing layouts. This is a scoped CSS fix;
no markup, query, schema or collector changes were needed on sql2025 / WebScraper.

Solution build passed with zero warnings/errors, browser-test ESLint, repository
formatting and diff checks passed. No .NET/JS unit or full repository-lint rerun was
needed for the CSS change. Preview and live Chromium checks covered 320/390/430/
768/1024/1280/1440/1920/2560/3200/3840px in light/dark. A test-only DOM fixture uses
the reported four-episode batch plus a second series, matching the actual nested
show-list markup. Assertions check one full-width show per row, episode details
below the poster, full-width lists, unwrapped date-only metadata, stacked series,
44px episode links, calendar navigation, Refresh, theme persistence, no horizontal
overflow and no-JavaScript calendar/fold controls. Screenshots were inspected.
Production episode data and existing readers' preferences were untouched; each run
removed its disposable reader profile. Evidence: `test-results/browser/home-episodes-{preview,live}/` and `test-results/browser/episode-spacing-release/`. Physical devices,
Safari and Firefox were unavailable; no checks on those are claimed.

Deployed public image `mynewspaper-web:episodes-spacing-20261030` (`ad8e9d750baa`) as
`mynewsfeed-web`. Public/admin HTTPS health returned 200; runtime environment, key
mounts, ports, restart policy and aliases were preserved. Deployed broadsheet CSS
matches source; startup logs had no failure/critical entries. Admin was unchanged.
Rollback: stopped container `mynewsfeed-web-before-episodes-spacing-20261030` and
image `mynewspaper-web:before-episodes-spacing-20261030` (`c5ed3f806c1b`).

## Wide Home polish and public-header search — October 30, 2026 (Central)

Reviewed the second revised wide-screens handoff against the deployed build. The
wide Home masthead now uses a 56px greeting, date-line Refresh, right-aligned weather
with its link alongside, and a bounded Live now/Coming up line below the double rule.
It shows at most three games plus a show summary. Normal GET Where to watch search
appears in the public header from 1920px, except on Watch; Home's wide search band is
hidden. The lead photo/P8 frame is 16:9 above a 46px headline, the latest video title
is 20px, and the six-column grid has a 2.4fr lead and side columns at least 340px.
Primary labels fit one line; the episode count remains visible below its heading.
Series titles wrap in full. Narrower layouts retain their existing search/masthead.

Show summaries group episodes by show ID, with a title fallback for unknown IDs.
One show retains its platform and adds a batch count; multiple shows show `+ N more`
and omit a misleading single platform; no episodes produce no link. Four rendering
tests passed, covering these branches and encoded titles. The implementation also
accounts for the existing lead classifier's higher-specificity rules, the ticker's
inherited column direction, long ticker text, actual 44px controls, and CSS zoom.
No queries, schema migrations or collector changes were needed on sql2025 / WebScraper.

Solution build passed with zero warnings/errors; changed browser-test ESLint,
repository formatting and diff checks passed. Full .NET/JS-unit/repository-lint
suites were not repeated; their previously recorded unrelated failures remain
outside this change. Preview and live Chromium matrices covered 320/390/430/768/
1024/1280/1440/1920/2560/3200/3840px in light/dark, plus Monochrome/Cardinals at 2560.
Checks covered real Refresh, phone folds, theme persistence, header GET submission
and absence on Watch, header search/overflow on Sports, All headlines, YouTube,
Entertainment, Settings and Saved, no-JavaScript search/Refresh/folds, three-game
single-line geometry, long forecast/show fixtures, six primary single-line labels,
lead image order/ratio and classified headline size. A focused preview at 2560 also
verified an unclamped long TV series title; the live matrix included that fixture.
A final placeholder-contrast adjustment uses the theme text color at full opacity;
its preview/live follow-up covered 320/390/430/1920/2560/3840px in both themes and
repeated the search/navigation/no-JavaScript flows.
Fixtures change only test DOM; no production episodes/forecasts were modified.
Screenshots were inspected and each disposable profile was removed. Evidence:
`test-results/browser/home-polish-{preview,live}/` and
`test-results/browser/home-polish-release/`. Physical devices, Safari and Firefox
were unavailable; no claim is made for them.

Public image `mynewspaper-web:home-polish-release-20261030` (`c5ed3f806c1b`) deployed
as `mynewsfeed-web`. Public/admin HTTPS health returned 200. Environment, key mounts,
ports, restart policy and network aliases were preserved; public/broadsheet CSS
hashes match source. Startup logs had no failure/critical entries. Admin was not
rebuilt. Rollback: stopped container `mynewsfeed-web-before-home-polish-20261030`
and image `mynewspaper-web:before-home-polish-20261030` (`f976307bda8d`).
The intermediate pre-contrast release is also retained as stopped container
`mynewsfeed-web-before-home-polish-contrast-20261030` and image
`mynewspaper-web:before-home-polish-contrast-20261030` (`2aa90031f401`).

## Compact wide-screen Home masthead — October 30, 2026 (Central)

From 1920 CSS px, Home uses its existing navigation tile/wordmark, a left-aligned
date and greeting, a boxless four-period weather strip, and the existing Live now/
Coming up box. The large centered logo is hidden. The greeting scales from about
54px to 72px. Grid tracks reserve room for the actual site font and Refresh button;
weather text can shrink/wrap, with the Kansas City link below the four periods.
This corrects the revised handoff's 1920px long-forecast overlap. All changes are
scoped to the wide-screen breakpoint; smaller Home layouts remain in place.
Weather icons/data and scraper behavior are unchanged. No queries, migrations or
collector changes were needed on sql2025 / WebScraper. Admin was not rebuilt.

Solution build passed with zero warnings/errors. Browser-test ESLint, repository
formatting and diff checks passed. Full .NET/unit/repository-lint suites were not
rerun for this CSS-only change; prior unrelated suite failures remain as recorded.
Preview and live Chromium checks covered 320/390/430/768/1024/1280/1440/1920/2560/
3200/3840px in light/dark, plus Monochrome and Cardinals at 2560. Assertions covered
navigation branding, single-row navigation, no horizontal overflow, greeting and
Refresh clearance, long forecast summaries, theme persistence, unchanged narrower
mastheads, actual Refresh and phone fold interactions, and no-JavaScript Refresh/
phone folds. Test-only DOM changes stressed the longer "Good afternoon" greeting
and four long forecast summaries; production forecasts were not modified.
Screenshots were inspected. Disposable reader profiles were deleted after each
run. Evidence is under `test-results/browser/home-masthead-{preview,live}/` and
`test-results/browser/home-masthead-release/`. Physical devices, Safari and Firefox
were unavailable; those checks are not claimed.

Deployed public image `mynewspaper-web:home-masthead-20261030` (`f976307bda8d`) as
`mynewsfeed-web`. Public/admin HTTPS health returned 200. Runtime environment,
persistent key mounts, ports, restart policy and network aliases were preserved;
the deployed broadsheet CSS hash matches source. Startup logs had no failure or
critical entries. Rollback is the stopped container
`mynewsfeed-web-before-home-masthead-20261030` and image
`mynewspaper-web:before-home-masthead-20261030` (`4f856d3d0b42`).

## Complete Sports tables and hourly rotation — October 30, 2026 (Central)

The Sports front's desktop panels now include every standings team/group and all
ranked Top 25 teams, retaining provider/rank order. Their selected competition and
poll advance through shuffled hourly cycles over enabled, nonempty eligible feeds.
Full-view links retain the selected source. Visible desktop panels refresh at the
hour without reloading the page; failures retain the last display and retry. Phone
folds retain five-row summaries and pick up selection on navigation/reload. Selection
uses the web clock, aligned with hourly collection; no scraper trigger or collector
change is introduced. No schema migration was needed on sql2025 / WebScraper.

Solution build passed with zero warnings/errors, two rotation tests and five JS unit
tests passed, and changed-JS/browser-test ESLint, repository formatting and diff
checks passed. The rotation tests cover stable selection within an hour, the next
hour, a complete cycle, query-order independence, and empty/single sources. The full
.NET suite and repository-wide lint were not repeated; their previously recorded
unrelated failures remain outside this change.

Preview and live Chromium checks covered 320/390/430/1024/1440/1920/2560/3840px in
light and dark. Assertions compared all standings rows with the matching full page,
checked all 25 ranks in order, stable selection, phone fold/full-view navigation,
44px phone fold/link targets, persisted themes, no horizontal overflow, automatic
panel refresh with an accelerated initial delay, and no-JavaScript full-poll links.
Screenshots were inspected. Each run used and removed its own temporary reader
profile. Evidence: `test-results/browser/sports-panels-{preview,live}/` screenshots and
`test-results/browser/sports-panels-release/{preview,live}.log` full-matrix logs. Physical devices, Safari and
Firefox were unavailable; no claim is made for those checks.

Public image `mynewspaper-web:sports-panels-20261030` (`4f856d3d0b42`) was deployed as
`mynewsfeed-web`. Public/admin HTTPS health returned 200; public Docker health is
healthy. Runtime environment, key mounts, ports, restart policy and network aliases
were preserved; deployed CSS/JS hashes match source. Startup logs had no failure or
critical entries. Admin was unchanged. Rollback: stopped container
`mynewsfeed-web-before-sports-panels-20261030` and image
`mynewspaper-web:before-sports-panels-20261030` (`40a9579e1b76`).

## YouTube sidebar — October 30, 2026 (Central)

The YouTube featured front now adds a third column from 2560 CSS px. By channel
shows today's video counts for the current reader's followed categories and links
to each category. Counts use one grouped query bounded by the site's calendar day,
independent of the active category/channel filter. Watch later shows the profile's
three newest saved videos, links to the full Saved list, and updates after saves,
removals and Saved refreshes. It is server-rendered for no-JavaScript readers and
hidden when empty. Narrower layouts and the existing category controls are retained.
No schema migration, carousel or collector changes were needed.

Solution build passed with zero warnings/errors. All 36 targeted video tests and
all five JavaScript unit tests passed. The new SQL test checks a 25-hour Chicago
DST day, exclusive day boundaries, uncategorized videos, unfollowed feeds and a
viewer with no profile. Changed JavaScript/browser-test ESLint, repository-wide
formatting and diff checks passed. The previous release's unrelated full-suite
profile-settings and repository-lint failures were not changed by this follow-up.

Preview browser checks passed 320/390/430/768/1024/1280/1440/1920/2560/3840px in
light/dark, plus Monochrome/Cardinals at 2560. Real temporary-profile saves/removals
verified newest-first order, the three-item cap, empty hiding, refill after removal,
live updates, reload persistence and safe plain-text saved titles. Category links
and counts independent of filtering passed. Sidebar target sizes, phone menus/save
controls, no horizontal overflow, breakpoint visibility, server-rendered bookmarks
and no-JavaScript category form navigation passed. Screenshots were reviewed.
The live layout matrix passed the same widths/themes and the real-save/category
checks. A focused live rerun passed server-rendered bookmarks and no-JavaScript
category navigation after the test was corrected to wait for CSS loading before
asserting media-query layout. Test fixtures were removed through Admin cleanup; a final check found no sidebar
test profiles remaining.

Deployed mynewspaper-web:youtube-sidebar-20261030 (40a9579e1b76), public container
1b4f0609bb74. The live environment, key volume, port mapping, restart policy,
health check, network and aliases were preserved. Database remains sql2025 /
WebScraper. Public/admin HTTPS health endpoints returned 200; Admin was unchanged.
The deployed public.js/sections.css hashes match the working tree; no fail/critical
startup entries were observed. Rollback image
mynewspaper-web:before-youtube-sidebar-20261030 and stopped container
mynewsfeed-web-before-youtube-sidebar-20261030 retain c34ccf4053df.
Evidence: test-results/browser/youtube-sidebar-preview/, youtube-sidebar-live/
and validation logs under test-results/browser/youtube-sidebar-release/.
Checks use Chromium emulation; physical devices, Firefox and Safari were unavailable.

## Wide screens and 4K — October 30, 2026 (Central)

The public reader fills the viewport from 1920 CSS px with 56px edges. Home adds
its fourth column at 1920px and puts all six columns together at 2560px. News,
Sports and Entertainment side sections share the extra width; headline/video
grids add columns. At 3200px and above the reader shell scales by 1.25. The header
padding override, YouTube selector specificity and narrow Home Entertainment list
were adjusted after source/screenshot review. Optional YouTube sidebar/channel-tab
work and the absent Jump to component are excluded. Admin's separate shell is
unchanged. See [implementation notes](wide-screens-4k-handoff.md).

Solution build passed with zero warnings/errors, all five JavaScript unit tests
passed, repository-wide formatting and changed-browser-test ESLint passed. The
full .NET run passed 1,038 of 1,040 tests. Two unchanged profile tests expect empty
settings, but new profiles inherit the configured database's nonempty base edition:
ProfileHttpTests.Opening_a_profile_checks_the_pin_and_antiforgery_and_gives_it_its_own_saved_list
and ProfileDataTests.Settings_patch_by_key_and_the_browser_import_happens_once.
No household preferences were cleared to satisfy these assertions. Repository-wide
ESLint already reported 258 errors in unchanged JavaScript before implementation;
those remain outside this CSS/Razor release.

Preview checks passed Home columns, phone headlines, YouTube categories, phone
fronts and phone life interactions. Preview/live wide-screen checks covered Home,
News, Sports, Entertainment, All headlines and YouTube at 320/390/430/768/1024/1280/
1440/1920/2560/3200/3840px, light/dark, with no horizontal overflow. Wide Home column
placement/band visibility, header/page edges, headline/video column counts and 4K
sticky header/share dialog geometry passed. Wide secondary-page overflow checks
passed for Where to watch, Scoreboard, Standings, Movies, TV, Saved and Settings.
Monochrome/Cardinals at 2560px were also checked through saved preferences; reviewed
screenshots show visible column rules. Live headline interactions passed three
schemes, phone widths, saved preferences and no-JavaScript/tablet/desktop checks.
Corrected live phone-front/life reruns passed 320/390/430px in Kansas light/dark,
including 44px targets, contrast, folds/menus, date/filter controls, save/share,
watch dialogs/search/sort, refresh and saved-preference persistence, empty states,
no-JavaScript paths and retained 1024/1280/1440 layouts. Their preview runs covered
all three schemes. Live headline checks covered all three schemes separately.

The first live phone-front interaction run exposed a test race: its fixed 900ms
wait could reload before the settings PUT completed. The phone-front/life helpers
now await the actual successful save response. Their temporary-profile setup also
uses the existing Admin hydration retry for creation and deletion. Initial runs
needed manual removal of their own temporary profiles; corrected cleanup was
verified separately. The superseded pre-fix run was stopped. These are test-harness
corrections; no application code was changed after the release image was built.
All temporary profiles from this release were removed; the final Admin check
found none remaining. The older YouTube
category suite requires a profiles-disabled preview; live YouTube is covered by
the profile-aware wide-screen and phone-life checks.

Deployed public image mynewspaper-web:wide-20261030 (c34ccf4053df), container
ba5c65ffed3e. The original environment, key mount, ports, restart policy, health
check, network and aliases were preserved from the actual runtime configuration.
Database remains sql2025 / WebScraper; no migrations or collector changes were
needed. Both public/admin HTTPS health endpoints returned 200; both containers
are healthy. No fail/critical startup entries were observed; league-phase checks
reported regular-season confirmations. All four deployed stylesheet hashes match
the working tree. Admin image remains 4e1a75444c26.
Rollback image mynewspaper-web:before-wide-20261030 and stopped container
mynewsfeed-web-before-wide-20261030 retain the previous public image 118925ff2fe6.

Evidence: test-results/browser/wide-preview-wide/, wide-live-wide/,
wide-live-schemes/, wide-live-phone/, wide-live-fronts/ and wide-live-life/.
Validation logs are retained under test-results/browser/wide-release/.
Screenshots were reviewed. Verification uses Chromium emulation; physical devices,
Safari and Firefox were unavailable. Content height is variable; six columns do
not guarantee that every section fits above the fold.

## College daily featured game — October 30, 2026 (Central)

Replaced the phone/tablet Game of the week selection with Today's featured game.
Only events on the site's configured calendar date are eligible; ranked-matchup
priority is then applied within that day. No future fallback: an empty day shows
No games today for the selected sport. Weekly schedule remains below. Later weekly
pages load daily candidates independently. Desktop weekly schedule is unchanged.

77 targeted PhoneSports/College tests passed, including future/past exclusion,
rank priority, Chicago/UTC boundaries, empty-day rendering and later-page daily
candidates. Preview/live Chromium passed 320, 390, 430, 820, 1024 and 1440px,
light/dark, JS on/off; all four sports checked at 390px. Verified hero date against
actual schedule groups, real basketball empty day, retained weekly content, 44px
sport selector and no horizontal overflow. Phone/empty-state/desktop screenshots
inspected; temporary profiles deleted. Release build, formatting, changed-test lint
and diff checks passed. Physical devices/Safari unavailable.

Deployed `mynewspaper-web:college-today-20261030` (prefix `118925ff2fe6`), preserving
runtime configuration and keys. Rollback tag
`mynewspaper-web:before-college-today-20261030` and stopped container
`mynewsfeed-web-before-college-today-20261030` retain image `1fac6e35369f`.
Both sites healthy; public HTTPS health 200; no fail/critical startup entries observed.
Admin unchanged; no schema/collector changes. Evidence: test-results/browser/college-today/live.

## Sports Media headline category — October 30, 2026 (Central)

Admin configuration only: created Sports Media (category 7628) in the Sports hub
and reassigned the existing enabled Awful Announcing source from General Sports
(category 9). Its URL and collection settings were preserved. Existing collected
headlines are immediately available at /sports/news/category-7628, via desktop
tabs and the mobile dropdown. No application build, deployment or schema change
was needed; both hosts read the shared category/source data.

Live Chromium passed 320, 390, 430, 820, 1024 and 1440px, light/dark, JS on/off:
category navigation/redirect, active category, Awful Announcing source filter,
correct article categories/publishers and no horizontal overflow. Ten articles
were observed during verification. Phone/desktop screenshots inspected; temporary
verification profiles deleted. Evidence: test-results/browser/sports-media/live.
Physical devices/Safari unavailable. Application unit tests are not applicable to
this configuration-only change. To reverse, reassign the source to Sports before
removing the then-unused category; do not delete the source or its articles.

## Latest video action alignment — October 30, 2026 (Central)

Reset inherited Save margins and center the latest-video card's action row so
Share and Watch later sit together on the right with an 8px gap. Both controls
retain 44px touch targets. Added gap/alignment browser assertions.

Preview/live Chromium passed 320, 390, 430, 820, 1024 and 1440px in light/dark;
live also verified save/unsave and JS on/off. Phone/desktop screenshots inspected,
temporary profile deleted. Formatting, changed-test lint, diff and release build
passed. Physical devices/Safari unavailable.

Deployed `mynewspaper-web:video-actions-20261030` (prefix `1fac6e35369f`), preserving
runtime/keys. Rollback tag `mynewspaper-web:before-video-actions-20261030` and stopped
container `mynewsfeed-web-before-video-actions-20261030` retain `fa00b619f79b`.
Both sites healthy; public HTTPS health 200; no fail/critical startup entries observed.
Admin unchanged. Evidence: test-results/browser/video-actions/live.

## Latest followed YouTube video on Home — October 30, 2026 (Central)

Replaced the desktop Opinion box and phone Opinion fold with the newest collected
video from the current profile's followed channels across all categories. Includes
thumbnail, title, channel/category, date/time, external YouTube link, watch-later
and share controls. Empty state offers Follow channels; failures show a retry
message. Refresh reads the newest video. The separate rotating video shelf remains.
The `opinion` settings key retains saved order/visibility; its public Settings label
now reads Latest YouTube video. Opinion news remains under News. Removed the old
Opinion polling script from the page and replaced its browser test.

Solution build passed with zero warnings/errors; 36 targeted video/isolation tests
passed, including cross-category newest selection, unfollowed exclusion and ties.
Broader video/ProfileData tests were 52/53: the existing browser-import test assumes
empty household defaults, unlike this database. Changed-JS lint, formatting and diff
checks passed. Repository lint passed excluding ignored browser artifacts and the
existing no-unsafe-finally finding in no-photo.browser.cjs. No household settings
were changed for tests.

Preview and live Chromium passed 320, 390, 430, 820, 1024 and 1440px, light/dark,
JS on/off, empty state, follow changes, profile-scoped selection, phone-fold opening,
links, date/channel text, no overflow, 44px actions and save/unsave. Phone/desktop
screenshots inspected; temporary profiles deleted. Physical devices/Safari unavailable.

Deployed `mynewspaper-web:latest-video-20261030` (image prefix `fa00b619f79b`),
preserving runtime settings and keys. Rollback tag
`mynewspaper-web:before-latest-video-20261030` and stopped container
`mynewsfeed-web-before-latest-video-20261030` retain image `d93da38f3063`.
Public/admin healthy, public HTTPS health 200, no fail/critical startup entries
observed. Admin not replaced; no schema or collector changes.

## Mobile feature story width — October 30, 2026 (Central)

Compact feature thumbnails now share a row with the category below 1024px;
headline and summary use the full card width. Publisher, date and action controls
wrap into readable rows. Wide-photo and no-JavaScript feature bodies also use a
single text column. Desktop styling is unchanged.

Preview Chromium passed 320, 390, 430, 820 and 1440px in light/dark with compact
and landscape fixtures and JavaScript disabled. Live checks repeated those widths
and themes, actual Big 12 story content, compact thumbnail fixtures, and the
no-JavaScript fallback. Confirmed full-width text/publisher, no horizontal overflow,
44px actions and saving/unsaving. Phone/desktop screenshots inspected; temporary
profile deleted. CSS formatting and diff checks passed. Physical devices/Safari
unavailable.

Deployed `mynewspaper-web:feature-card-20261030` (image prefix `d93da38f3063`),
preserving runtime configuration and keys. Rollback image
`mynewspaper-web:before-feature-card-20261030` (prefix `e51c997555cf`) and stopped
container `mynewsfeed-web-before-feature-card-20261030` retained. Both sites healthy;
public HTTPS health returned 200. No fail/critical startup entries observed. Admin
was not replaced. Browser evidence: test-results/browser/feature-card/live.

## Home Top story spacing — October 30, 2026 (Central)

Added 16px top padding to the desktop lead story so its image/placeholder clears
the section divider. Phone layout is unchanged. CSS formatting and diff checks
passed. Preview Chromium checks passed 320, 390, 430, 820, 1024 and 1440px in
light/dark with JavaScript on/off; live checks repeated all six widths and both
themes with JavaScript enabled. Desktop geometry confirmed the gap; phone and
desktop screenshots were inspected. Physical devices/Safari were unavailable.

Deployed public image `mynewspaper-web:lead-spacing-20261030` (ID prefix
`e51c997555cf`), preserving runtime settings and persistent keys. Rollback image
`mynewspaper-web:before-lead-spacing-20261030` (ID prefix `48accb3ec847`) and stopped
container `mynewsfeed-web-before-lead-spacing-20261030` retained. Both public and
admin containers healthy; public HTTPS health returned 200, with no fail/critical
startup log entries observed. Admin was not replaced. Temporary test profile removed.

## Header color-scheme modal — October 30, 2026 (Central)

Added a palette shortcut beside the light/dark toggle on mobile and desktop.
It opens a native modal with the same 16 named swatches from SiteDefaults as
Settings. Choices apply immediately and save automatically, preserve light/dark
mode, and synchronize with Settings. The modal supports Tab/Shift+Tab, Escape,
backdrop/Close/Done dismissal and focus restoration. Sticky controls keep save
feedback visible on small screens. A failed save exposes a retry action;
profile acknowledgements identify the preference patch so stale/unrelated saves
do not report success for the current selection. Without JavaScript/native modal
support, the shortcut links to Settings. Narrow headers use a separate clock row
to keep four controls (or five with an admin Requests alert) usable.

Deployed public image `mynewspaper-web:scheme-picker-20261030` (`48accb3ec847`)
to `mynewsfeed-web`. Runtime environment, health check, ports, network, restart
policy and persistent keys were preserved. Admin was not rebuilt; no migration
or collector change ran. Rollback: `mynewspaper-web:before-scheme-picker-20261030`
(`ba56cac5113b`) and `mynewsfeed-web-before-scheme-picker-20261030`.

Public build and Docker publish/static-asset guard passed with no build warnings
or errors. All 139 profile-related .NET tests passed. Targeted ESLint, changed
CSS/JavaScript Prettier and whitespace checks passed. Preview Chromium checks
covered 320/390/430/820/1024/1280/1440px light/dark: modal fits, 44px shortcut,
all choices, focus containment/restoration, three dismissal paths, no page
navigation on opening, Settings synchronization, storage-failure retry,
refresh/enhanced-navigation persistence, admin notification fixture and native
no-JavaScript Settings fallback. Representative screenshots were inspected.

Final HTTPS checks passed all seven widths in light/dark on a disposable profile.
All 16 schemes saved, retained light/dark mode, synchronized with Settings and
survived refresh/enhanced navigation. Tab/Shift+Tab, focus restoration, backdrop/
Escape/Done, background scroll lock, viewport fit, 44px shortcut and no page
navigation on opening passed. A controlled 503 response displayed failure
feedback; retry saved successfully. The extra admin notification fixture passed
320/390/430px light/dark after awaiting completed navigation before measuring the
new header. Native no-JavaScript Settings fallback worked. Representative live
phone/desktop screenshots were inspected. Both HTTPS health endpoints returned
200; startup league-phase checks ran without application errors. Test profiles
and the temporary workspace hostname mapping were removed after verification.

Evidence: `test-results/browser/scheme-picker/` and `/tmp/scheme-modal-*.log`.
Physical-device and Safari checks were unavailable. Commit remains local on
`develop`.

## Mobile header light/dark toggle — October 30, 2026 (Central)

The existing sun/moon theme button now appears immediately beside Saved in the
mobile header, with the same outlined 44px treatment as Saved/Menu. It uses the
existing theme behavior and profile persistence. Narrow-phone spacing fits long
dates/times; an admin Requests notification moves the clock to a second row at
narrow widths. The menu sheet fits below the actual header height. Desktop action
order and theme behavior are unchanged.

Deployed public image `mynewspaper-web:mobile-theme-20261030` (`ba56cac5113b`)
to `mynewsfeed-web`. Runtime environment, health check, ports, network, restart
policy and persistent keys were preserved. Admin was not rebuilt. No migration
or collector change ran. Rollback image/container:
`mynewspaper-web:before-mobile-theme-20261030` (`d375ffdbd724`) /
`mynewsfeed-web-before-mobile-theme-20261030`.

Public build and Docker publish/static-asset guard passed; changed CSS formatting
and whitespace checks passed. Preview checks covered 320/390/430/820/1024/1440px
light/dark: switching both directions, sun/moon icons, accessible labels/pressed
state, 44px mobile targets, Saved and Menu/Escape, refresh/navigation persistence,
long Wednesday/date/time fit and no overflow. An injected admin Requests fixture
also passed 320/390/430px light/dark without overlapping controls and with the
menu aligned to the viewport. Native Saved/Menu work without JavaScript; theme
switching requires JavaScript, as before. Screenshots were inspected.

Final HTTPS checks passed at 320/390/430/820/1024/1440px in light/dark.
Every toggle saved successfully to the temporary profile; icons, labels and
pressed state matched. Preferences survived refresh and navigation, and toggling
still worked after enhanced navigation. Long clock labels, Saved/Menu/Escape,
44px mobile controls and no overflow passed. The injected Requests fixture also
passed 320/390/430px light/dark, including menu geometry. Native no-JavaScript
Menu/Saved navigation worked. Representative live screenshots were inspected.
Both HTTPS health endpoints returned 200, the public container is healthy and
startup league-phase checks ran without application errors. The temporary profile
and workspace hostname mapping were removed after verification.

Evidence: `test-results/browser/mobile-theme/` and `/tmp/mobile-theme-*.log`.
Physical-device and Safari checks were unavailable. Commit remains local on
`develop`.

## Homepage New Tonight in Coming up — October 30, 2026 (Central)

Moved the masthead's New Tonight episode teaser below the games in the Coming up /
Live now box. The weather box contains only the forecast and no longer renders
when forecast data is absent. Show title, time/platform and the TV link remain;
the phone Coming to screens fold retains its existing presentation.

Deployed `mynewspaper-web:tonight-box-20261030` (`d375ffdbd724`) to
`mynewsfeed-web`. Runtime environment, health check, ports, network, restart policy
and persistent keys were preserved. Admin was not rebuilt. No migration or
collector change ran. Rollback: `mynewspaper-web:before-tonight-box-20261030`
(`890ff293ddff`) and `mynewsfeed-web-before-tonight-box-20261030`.

Public build and Docker publish/static-asset guard passed; changed CSS formatting
and diff whitespace checks passed. Preview checks covered 320/390/430/820/1024/
1440/1870px light/dark, correct placement below games, containment/no overflow,
44px episode link, TV navigation and theme persistence after refresh. The phone
TV fold and no-JavaScript TV links at 390/1440px worked. Existing homepage-column
regression passed with and without JavaScript. Screenshots were inspected.

Final HTTPS verification passed at all seven widths in light/dark: the teaser
appeared only inside Coming up, below the game list, with no overflow and a 44px
link. Mobile's Coming to screens fold and TV navigation worked. Refresh retained
the selected theme, and TV links worked without JavaScript at 390/1440px.
Representative live screenshots were inspected. A disposable profile followed
the existing Avatar series for representative data and was removed after checks.
Both HTTPS health endpoints returned 200, the public container is healthy and
league-phase startup checks ran without application errors. The temporary
workspace hostname mapping was removed after verification.

Evidence: `test-results/browser/tonight-move/`, `/tmp/tonight-move-*.log` and
`/tmp/tonight-box-*.log`. Physical devices and Safari were unavailable. Commit
remains local on `develop`.

## P8 no-photo frames — October 29, 2026 (Central)

Implemented the approved [P8 handoff](no-photo-placeholder-handoff.md): decorative
category-colour frames with the cropped page mark, gold fold, publisher initials
and category label. Labels disappear below 120px; dark backgrounds mix the category
colour into navy. The existing category helper now maps Top Headlines to red.
Headline cards retain their media area for missing, failed, tiny and compact photos;
wide photos remain visible. The top always uses lead + two supporting stories +
Latest, whose first row alone has media. Home lead and missing-photo Opinion
thumbnails use P8. Shared phone lists use 96px thumbnails, with publisher names and
44px actions on separate lines; their first feature keeps the full P8 frame.
No-JavaScript featured cards also keep P8. The dedicated folded phone News front
retains its existing photo/text behavior.

Deployed public image `mynewspaper-web:p8-placeholders-final-20261029`
(`890ff293ddff`) to `mynewsfeed-web`. Runtime environment, health check, ports,
network, restart policy and persistent keys were preserved. Admin was not rebuilt
or replaced. No database migration or collector change ran.

The pre-P8 release remains in `mynewsfeed-web-before-p8-placeholders-20261029`
and image `mynewspaper-web:before-p8-placeholders-20261029` (`50431d7b01c5`).
The first P8 build was briefly deployed before live screenshot review prompted
more room for phone publisher names. That build is separately retained in
`mynewsfeed-web-before-p8-placeholders-final-20261029` (`627b8ee21114`).

Validation: public build and Docker publish/static-asset guard passed. All 1,032
.NET tests and five JavaScript unit tests passed; targeted ESLint, changed CSS/JavaScript
Prettier and diff whitespace checks passed. Browser checks covered 320/390/430/820px
light/dark plus 1024/1280/1440px with Kansas, Cardinals and monochrome schemes.
News, Sports and Entertainment headline pages retained their frame geometry and
unique visible story order. Controlled wide, compact, tiny and broken photos tested
initial rendering, late failures and dynamic replacement; Home lead and Opinion,
small-label suppression, decorative accessibility, share dialogs, 44px actions,
saved-story persistence and theme persistence after refresh were checked.
The updated article-image regression also passed 320/390/430/744/1440px light/dark,
including square/portrait photos, BBC resolution retry, enhanced navigation,
Load more, front pages and no-JavaScript fallbacks. Screenshots were inspected.

After deployment, the final HTTPS build passed 320/390/430/820px light/dark
and 1024/1280/1440px desktop checks. All three headline hubs were checked on
desktop, with Kansas, Cardinals and monochrome schemes; fixtures, late image
failures, layout/order, Home placeholders, share actions and no-JavaScript P8
passed. The 390px save/refresh/unsave flow and saved theme survived refresh.
Representative phone, iPad and desktop screenshots were inspected. The served
fingerprinted CSS matched the final source; the real placeholder SVG decoded.
Both HTTPS health endpoints returned 200, the public container is healthy, and
league-phase startup checks ran without application errors. Temporary test
profiles were removed. Admin test setup needed retries while its interactive
circuit initialized; final verification completed successfully. The temporary
workspace hostname mapping was removed after HTTPS checks.

Artifacts: `test-results/browser/p8*/` and `test-results/browser/live-p8*/`
(ignored); browser logs are in `/tmp/p8-*.log`. Physical devices and Safari were
not available. The commit remains local on `develop`.

## Complete box-office chart without bars — October 29, 2026 (Central)

Deployed public image `mynewspaper-web:full-boxoffice-20261029`
(`50431d7b01c5`) to `mynewsfeed-web`. Mobile/iPad weekend results now show
all collected chart entries in one ordered list, without blue gross bars or a
separate 6–10 disclosure. Weekend grosses, new-release badges and change labels
remain, with extra rank-column space for two-digit places. Biggest totals and
Next weekend remain supplemental sections; the desktop results table is unchanged.
No database migrations or schema scripts ran. Runtime environment, ports, network,
restart policy, health check and persistent key volume were preserved. The
connection remains sql2025 / WebScraper. Admin was not rebuilt or replaced.

Rollback: image `mynewspaper-web:before-full-boxoffice-20261029`
(`3e5afc6aa2ea`) and stopped container
`mynewsfeed-web-before-full-boxoffice-20261029` preserve the preceding release.

Public build passed with zero warnings/errors. Docker publish and its static-asset
guard passed. ESLint, changed-file Prettier and diff whitespace checks passed.
Preview checks covered 320/390/430/768/820/834/1024/1440px light/dark with ten real
entries: matching mobile/desktop ranks and counts, rows outside disclosures,
absence of bars and the 6–10 fold, two-digit rank fit, no title/gross overlap,
no horizontal overflow and desktop-table visibility. Refresh retained the theme;
View navigated to Next weekend and back. Without JavaScript, the full chart stayed
visible and the View form opened Forecasts. Screenshots were inspected.

After deployment, all checks passed against the production HTTPS site using a
disposable signed-in profile at all eight widths in light/dark. All ten chart
entries were in one continuous list, matching desktop order; no bars or 6–10
fold remained. Phone/iPad screenshots were inspected, including rank 10 spacing.
Refresh retained the saved theme, View navigation worked to Next weekend and
back, and the full list and Forecasts form worked without JavaScript. Authenticated
smoke checks returned 200 for health, News, Events, Results (following its redirect),
Standings, Rankings, Movies, digital Movies and box office. Both services were
healthy, both production HTTPS health endpoints returned 200, and league-phase
startup checks were observed.

Artifacts: `test-results/browser/live-full-boxoffice/` (ignored). The temporary
profile was removed, and the workspace hostname mapping used for certificate-verified
HTTPS checks was removed afterwards. Physical-device checks and full unrelated
application suites were not run. The commit remains local on `develop`; no push
was made.

## Standings team logos and simpler rows — October 29, 2026 (Central)

Deployed public image `mynewspaper-web:standings-logos-final-20261029`
(`3e5afc6aa2ea`) to `mynewsfeed-web`. Mobile/iPad standings no longer show
win-percentage meters. Team logos appear beside names in the rows, featured leader
and Your teams tiles, with a small white backing to keep dark logos readable.
Decorative images reuse the existing validated logo URLs and lazy-loading behavior;
missing/failed images leave the name and numeric record intact. Desktop standings
retain their existing logo/table layout. No database migration or schema script
ran. Runtime environment, ports, network, restart policy, health check and persistent
key volume were preserved; the connection remains sql2025 / WebScraper. Admin was
not rebuilt or replaced.

Rollback to the preceding release: image
`mynewspaper-web:before-standings-logos-20261029` (`41e524538773`) and stopped
container `mynewsfeed-web-before-standings-logos-20261029`. The first candidate
without logo backings was briefly deployed; screenshot review prompted the final
contrast improvement. That candidate is retained separately as
`mynewsfeed-web-before-standings-logos-final-20261029` (`cec87bb2226a`).

Validation: all 41 PhoneSportsTests passed, including rendered standings names and
records, absence of meters, safe/missing/unsafe logo URLs and empty decorative alt
text. Docker publish and its static-asset guard passed. CSS/docs formatting and
diff whitespace checks passed. Actual preview checks passed at
320/390/430/768/820/834/1024/1440px light/dark with real provider logos loading,
no meters, no page overflow, records clear of team names, followed-team/leader
logos, 44px team links and desktop logo regressions. League switching, selected
league/theme persistence after refresh, failed-image removal with readable team
name and the no-JavaScript League form passed. Backing-color screenshots were
checked after the contrast adjustment. Local artifacts are under
`test-results/browser/standings-logos-local/` (ignored).

After deployment, the same checks passed on the actual production HTTPS site with
a disposable profile following BYU and Kansas. Real team logos loaded at all eight
widths in both themes; screenshots of rows, the featured team, Your teams and the
desktop layout were inspected. League switching and selected-league/theme persistence
survived refresh. A deliberately failed logo was removed without losing the team
name. The native League form worked at 820px with JavaScript disabled. Authenticated
smoke checks returned 200 for health, News, Events, Results (following its redirect),
Standings, Rankings, Movies, digital Movies and box office. Both services were
healthy, both production HTTPS health endpoints returned 200, and league-phase
startup checks were present.

Artifacts: `test-results/browser/standings-logos-live/` (ignored). The disposable
profile was deleted after checks; no real profile settings were changed. Temporary
workspace hostname mapping used for certificate-verified production HTTPS checks
was removed. Physical-device and full unrelated test suites were not run. The
commit remains local on `develop`; no push was made.

## Homepage masthead link placement — October 29, 2026 (Central)

Public image `mynewspaper-web:masthead-links-20261029` (`41e524538773`)
is deployed to `mynewsfeed-web`. Kansas City now appears immediately below the
weather heading; Scoreboard appears below Live now/Coming up. The forecast's parent
grid uses one full-width column, retaining the two-by-two desktop periods without
a side column reserved for the link. Mobile/iPad content remains the existing
mobile version below 1024px. This release also includes committed ranking-source
exclusion `d8d5236`, whose 13 focused ranking tests passed before its commit.

Runtime environment, port bindings, network, restart policy, health check and the
persistent key volume were preserved. The database connection remains sql2025 /
WebScraper. No schema scripts or migrations ran. Admin was not rebuilt or replaced.
Rollback: `mynewspaper-web:before-masthead-links-20261029` (`0e7f428fb1ad`)
and stopped container `mynewsfeed-web-before-masthead-links-20261029` retain the
preceding public release.

Local public build passed with zero warnings/errors. Changed CSS/docs passed
Prettier and whitespace checks. Docker publish and its Blazor static-asset guard
passed. Preview checks passed at 320/390/430/768/820/834/1024/1280/1440/1817/1920px
light/dark for overflow, mobile content visibility, desktop heading/link/content
order, left alignment, 44px link targets and full forecast width. Both links
navigated with and without JavaScript; refresh retained the theme. Desktop masthead
and representative phone screenshots were inspected.

Post-deployment checks passed against the actual HTTPS site with a disposable
signed-in profile at all 11 widths in light/dark. Assertions checked the links
below their headings and above their content, left alignment, 44px targets, full
forecast width, mobile/iPad rendering and no horizontal overflow. Screenshots of
the masthead and phone layouts were inspected. Refresh preserved the saved theme;
both links navigated correctly with and without JavaScript. Ranking selectors
excluded Women's Basketball. Authenticated smoke checks returned 200 for health,
News, Events, Results (following its redirect), Standings, Rankings, Movies,
digital Movies and box office. Both services were healthy, both production HTTPS
health endpoints returned 200, and league-phase startup checks were observed.

Evidence: `test-results/browser/live-masthead-links/` (ignored). The temporary
profile was deleted; the workspace hostname mapping used for trusted production
HTTPS checks was removed afterwards. Browser checks blocked third-party images,
while local fonts/scripts loaded. Physical-device tests and full unrelated suites
were not run. The commit remains local on `develop`; no push was made.

## iPad portrait uses the mobile version — October 29, 2026 (Central)

Deployed public image `mynewspaper-web:ipad-mobile-20261029`
(`0e7f428fb1ad`) to `mynewsfeed-web`. Mobile layouts now cover all widths below
1024px, including common 768/820/834px iPad portrait sizes. Desktop remains at
1024px and wider, including the 1024px-wide large iPad portrait. The former tablet
desktop stylesheet was removed; public CSS, article image loading and browser
expectations now share the 1024px boundary. Long live-score statuses wrap inside
their cards instead of extending the page at 320px.

Built from an isolated source copy containing only this task's changes. Unrelated
local edits to RankingsQuery.cs and RankingsTests.cs were excluded from the image
and commit; their source copies were checked against HEAD. Runtime environment,
port bindings, network, restart policy, health check and persistent key volume were
preserved. The existing database connection remains sql2025 / WebScraper. No schema
scripts or migrations ran. Admin remains on its existing image and configuration.
Rollback: image `mynewspaper-web:before-ipad-mobile-20261029`
(`0c896cbfef6b`) and stopped container
`mynewsfeed-web-before-ipad-mobile-20261029` preserve the preceding public release.

Local validation: zero-warning public build; ESLint, formatting and whitespace
checks passed. Docker publish and the Blazor static-asset guard passed. Preview
layout checks covered 17 pages at 320/390/430/744/768/820/834/1023/1024/1280/1440px
light/dark; menu/fold interactions, mobile/desktop visibility and page overflow
were checked. A focused Home run completed refresh, saved theme and no-JavaScript
navigation after the broad local harness stopped following its page checks.
Screenshots were inspected. Details: [mobile handoff](mobile-refresh-handoff.md).

Production verification used a disposable signed-in profile over the actual HTTPS
origin. Across the initial run and a shorter resumed batch, all 17 public pages
passed at all 11 widths in light/dark: mobile content below 1024px, desktop at
1024px and wider, no horizontal overflow, mobile menu visibility and opening/closing,
available folds and 44px menu targets. Home, News, Scoreboard and Settings screenshots
were inspected, including phone/iPad/desktop views in both themes. Saved themes
survived refresh; navigation from the mobile menu worked with JavaScript disabled
at 820px. Main pages returned 200, as did the additional health, Events, Results
(following its redirect) and digital Movies smoke checks. Both containers were
healthy, both HTTPS health endpoints returned 200, and league-phase startup checks
were observed.

The initial live harness exited 143 before all dark-mode checks completed. The
remaining pages and final navigation checks passed in a shorter batch. Both
verification profiles were removed, including manual cleanup of the interrupted
run's profile. Logs and screenshots: `test-results/browser/live-ipad-mobile/`
(ignored). Temporary stored authentication state and the workspace hostname mapping
used for certificate-verified Tailscale HTTPS checks were removed after validation.
Remote images were blocked during browser checks; local assets still loaded.
Physical-device testing and full unrelated application test suites were not run.
The commit is local on `develop`; no push was made. Unrelated ranking edits remain
uncommitted in the workspace.

## Requests form choice alignment — October 29, 2026 (Central)

Deployed public image `mynewspaper-web:request-format-20261029`
(`0c896cbfef6b`) to `mynewsfeed-web`. The request-type legend now sits above
a separate choices grid. Options stack below 360px and share an equal-width row
at larger sizes. The group aligns with the form inputs and keeps the native
fieldset/legend association. Runtime environment, ports, network, restart policy,
health check and key volume were preserved. The existing database connection remains
sql2025 / WebScraper. No schema scripts or migrations ran; Admin was not rebuilt
or replaced.

Rollback: image `mynewspaper-web:before-request-format-20261029`
(`c7957f2eefdb`) and stopped container
`mynewsfeed-web-before-request-format-20261029` preserve the preceding public build.

Validation: public build passed with zero warnings/errors, changed CSS and docs
passed Prettier, and diff whitespace checks passed. Docker publish and its asset
manifest guard passed. Actual preview Settings checks passed at
320/390/430/768/1440px light/dark, including legend placement, option alignment,
both radio selections, 44px minimum touch targets, horizontal overflow, refresh,
saved theme and no-JavaScript selection. Screenshots were inspected.

After deployment, the same checks passed against the production HTTPS Settings
page at all five widths in both themes. Phone screenshots and a desktop screenshot
were inspected. Both services were healthy, both HTTPS health endpoints returned
200, and league-phase startup checks were present. Authenticated HTTP smoke checks
returned 200 for News, Events, Results (following its redirect), Standings, Rankings,
Movies, digital movies and box office. Artifacts are under
`test-results/browser/live-request-format/` (ignored). No requests were submitted;
the temporary verification profile was deleted. A temporary workspace hostname
mapping enabled certificate-verified production HTTPS checks and was removed after
verification.

Limits: checks used Chromium emulation; physical-device testing was unavailable.
The full unrelated application suites were not rerun for this markup/CSS change.
The commit remains local on `develop`; no push was made.

## Mobile live-score quarter shorthand — October 29, 2026 (Central)

Public image `mynewspaper-web:mobile-live-quarter-20261029`
(`c7957f2eefdb`) replaces `mynewsfeed-web`. Phone score tiles now display
`1:56 · 4Q` instead of `1:56 - 4th Quarter`; the accessible link label retains
the full provider status. Other statuses and desktop markup are unchanged.
The current runtime environment, ports, health check, restart policy, network and
persistent key volume were preserved; Admin was not rebuilt or replaced. No schema
scripts or migrations ran. The existing connection remains sql2025 / WebScraper.
Rollback image: `mynewspaper-web:before-mobile-live-quarter-20261029`
(`0729f739cdcf`), with stopped container
`mynewsfeed-web-before-mobile-live-quarter-20261029` retaining its configuration.

The 38 PhoneSportsTests passed, including quarter conversions, unchanged non-quarter
statuses and full accessible status in rendered component markup. Docker publish
and its static-asset guard passed. Local homepage checks covered 320/390/430px in
light/dark, plus 768/1440px layout regressions, refresh, saved theme, score-card
navigation and no-JavaScript navigation. Screenshots were inspected. The example
quarter card used actual component-rendered test markup inserted into the homepage;
no live games were present. It stayed within the card at all required phone widths,
retained its full accessible label, and exceeded the 44px touch-target minimum.
Local artifacts: `test-results/browser/local-quarter/` (ignored).

After rollout, both containers were healthy and both production HTTPS health
endpoints returned 200. League-phase startup checks were present. Authenticated
production homepage checks repeated 320/390/430px light/dark and 768/1440px layout
checks, with saved theme persistence, refresh and score-card navigation. The
no-JavaScript navigation check passed. All six phone screenshots were inspected
under `test-results/browser/live-quarter/` (ignored). No live games were present,
so the quarter-specific layout check used the same component-rendered example
inserted into the deployed homepage, without changing stored game data.

The first HTTP smoke run encountered an Events 500: logs identified SQL Server
error 1205, a deadlock victim in the existing schedule-choices query. A focused
retry returned 200 for health, News, Events, Results (following its redirect),
Standings, Rankings, Movies, digital movies and box office. No unrelated database
or query changes were made. Disposable profiles were deleted after both runs.
The temporary workspace hostname mapping used for trusted production HTTPS checks
was removed afterwards.

Limits: quarter-specific data was a rendered test example, rather than a currently
live game; real-device and screen-reader testing were unavailable. Full unrelated
application suites were not rerun. The commit remains local on `develop`; no push
was made.

## Homepage weather icons — October 29, 2026 (Central)

Deployed public image `mynewspaper-web:home-weather-20261029`
(`0729f739cdcf`) to `mynewsfeed-web`. Each homepage forecast period now
includes a server-rendered condition icon, including day/night variants, on both
phone and desktop layouts. Artwork matches the existing local weather icons;
forecast text remains the accessible description. No database scripts or migrations
ran. The existing sql2025 / WebScraper connection, runtime environment, ports,
network, restart policy, health check and persistent key volume were preserved.
Admin remains on its existing image and configuration.

Rollback: image `mynewspaper-web:before-home-weather-20261029`
(`f057f45e0be1`) and stopped container
`mynewsfeed-web-before-home-weather-20261029` retain the preceding public release.

Validation: local public build passed with zero warnings/errors; changed CSS passed
Prettier and the diff passed whitespace checks. Local Chromium checks covered
320/390/430/768/1440px light/dark, with screenshot inspection and no horizontal
overflow. Refresh, forecast navigation and no-JavaScript rendering were checked.
The Docker publish and static-asset manifest guard passed.

After rollout, the public and admin containers were healthy and both production
HTTPS health endpoints returned 200. Authenticated HTTPS checks passed for News,
Events, Results (following its redirect), Standings, Rankings, Movies, digital
movies and box office. League-phase startup checks were observed. Actual homepage
checks at 320/390/430/768/1440px light/dark verified four visible icons, no horizontal
overflow, saved theme persistence after refresh and decorative SVG accessibility.
The phone forecast link exceeds 44px and opens Kansas City news. A no-JavaScript
phone session also rendered four icons and followed the link. Screenshots at all
widths/themes were inspected under `test-results/browser/live-home-weather/`
(ignored). A disposable profile used for verification was removed afterwards.
A temporary workspace hostname mapping enabled the production Tailscale HTTPS
address with certificate verification enabled; it was removed after the checks.

Limits: phone checks used Chromium emulation; physical-device testing was
unavailable. The full unrelated test suites were not rerun for this presentation
change. The commit remains local on `develop`; no push was made.

## Separate public and admin PWAs — October 29, 2026 (Central)

Deployed the split websites from local `develop`, with shared code in
`myNewspaper.Shared` and independent public/admin projects, images and PWA identities.
Public remains at `https://mynewsfeed-prod.tailbf31bc.ts.net` on port 443; Admin is
`https://mynewsfeed-prod.tailbf31bc.ts.net:8443/admin`, as selected by the user.
Both are tailnet-only and use trusted HTTPS. `admin.mynewspaper` remains a future
hostname. Details and rollback steps: [admin separation](admin-separation.md).

- Public: `mynewsfeed-web`, image `mynewspaper-web:split-20261029` (`e48d70bae523`).
- Admin: `mynewspaper-admin`, image `mynewspaper-admin:split-20261029` (`4e1a75444c26`).
- Combined-app rollback: stopped container `mynewsfeed-web-before-split-20261029`
  and image `mynewspaper-web:before-split-20261029` (`d899ee3c639d`).

Preserved sql2025 / WebScraper, existing public key volume, profile cookie purposes,
operational overrides and background worker ownership. Admin has its own key volume
and authentication cookies; the public container no longer receives the admin
password. No schema scripts or database migrations ran. A profile opened before the
public switch remained signed in afterwards; its temporary profile/devices were
removed. Both containers are healthy and both HTTPS `/healthz` endpoints return 200.
League-phase startup checks were observed. Private, ignored Compose settings were
prepared from actual runtime configuration; no secrets are committed.

Validation: zero-warning builds; 1,006 public/shared plus three admin .NET tests;
five JavaScript unit tests; ESLint; full Prettier check. Both Docker images publish
and serve nonempty shared assets, including fingerprinted resources and Blazor.
The local 24-page admin layout suite passed at 320/390/430/744/1440px light/dark,
including contrast, navigation, filters and dirty editor state. Local profile,
separation, public headline and PWA checks passed.

After deployment, admin profile, separation, public headline and PWA suites passed
over the actual HTTPS origins. Phone verification used 320/390/430px light/dark
with Kansas, monochrome and Cardinals schemes for public headlines; admin checks
also covered 744/1440px, both themes, loaded Requests data, panel bounds, menu and
Escape, API/authentication separation, independent caches/storage and offline
recovery. Screenshots were inspected under `test-results/browser/live-split/`.
The Requests toggle opts out of enhanced navigation so Blazor cannot overwrite
loaded panel content. The theme check waits for the debounced save to finish before
reloading. Shared-asset byte checks guard against HTTP 200 responses with empty
bodies, which the first extraction attempt exposed.

Limits: physical-device installation was unavailable; the seeded request
approval/decline browser suite skipped because no disposable fixtures were present.
Those domain operations remain covered by .NET tests. GitHub workflows are local
and have not run on GitHub. No push was made.

## Headline top arrangements — October 29, 2026 (Central)

Latest web release: `mynewspaper-web:headline-top-20261029`, image `d899ee3c639d`.
Deployed the local working tree to `mynewsfeed-web` on the existing production host;
no commit or push was made. Runtime environment, port 8085, dev-bridge network,
restart policy, health check and persistent key volume were preserved. The runtime
connection remains sql2025 / WebScraper. No database migration was required or run.
Rollback image: `mynewspaper-web:before-headline-top-20261029` (`28f0349b1fab`);
previous stopped container: `mynewsfeed-web-before-headline-top-20261029`.

The lead's measured photo now selects a photo layout with two supporting text cards
and a Latest column, or a full-width text banner followed by three cards. Tablet
portrait retains all three cards rather than hiding story 4. Stories 5–8 have only
one visible copy, and group headings continue across the grid seam. Desktop card
photos are loaded eagerly so hidden, unmeasured media cannot stall lazy loading.

Validation: solution build passed with zero warnings; all 1,007 application tests
passed. ESLint and formatting for changed JS/CSS/tests passed. The repository-wide
format check still flags six unchanged browser test files (local-weather-nav,
phone-life, phone-screens, phone-sports, section-select and tv-series-delete).
Local article-images, headline-top and publisher-layout checks passed, including
BBC fallback, dynamic images, enhanced navigation and Load more; the phone suite passed at
320/390/430px, light/dark, Kansas/monochrome/Cardinals, including folds, category
selection, no-JS fallbacks and tablet/desktop regressions.

After rollout, `/healthz` returned 200 Healthy and the container became healthy.
Authenticated checks returned 200 for News, Events, the Results destination,
Standings, Rankings, Movies, digital movies, box office and sports/entertainment
headlines. League-phase startup checks were present. Live headline-top tests passed
at 320/390/430/744/1024/1280/1440px with wide, compact and broken image fixtures,
visible-story uniqueness, Share dialogs and no-JS banner. The live phone suite
passed all required phone widths/themes and three colour schemes. Additional live
desktop checks verified all six theme/scheme combinations with real photos.
Screenshots were inspected locally and on production; artifacts are under
`test-results/browser/` and `test-results/browser/live-headline/` (not versioned).
Temporary verification profiles and stored authentication state were removed.
Real-device checks were unavailable; phone verification used Chromium emulation.
The Tailscale HTTPS hostname was unreachable from this workspace (curl status 000),
so deployed browser checks used the private container HTTP address. HTTPS/TLS
verification from a tailnet device remains outstanding. Unauthenticated admin access
redirected to login (302).

Previous recorded release follows.

The previously recorded web release is `mynewspaper-web:article-image-nav-fix-20261029`
(image `756ae425b587`), deployed October 29, 2026 (Central), from `7ff365d`.
Rollback: image `mynewspaper-web:before-image-nav-fix-20261029` and container
`mynewsfeed-web-before-image-nav-fix-20261029`.
This is a dated deployment record, not live hosting status.

## Article images after navigation — deployed October 29, 2026 (Central)

Blazor enhanced navigation activated the no-JavaScript fallback stylesheet,
hiding loaded lead images. The fallback now checks a JavaScript capability marker
restored on each image scan. No scraper or database changes. Local and live
regression checks covered News → Home → Sports → Entertainment → News at
320/390/430/744/1440px in light/dark, plus no-JavaScript fallbacks. Images remained
visible and no horizontal overflow occurred. Actual Home NYT and Sports ESPN
images were visually inspected after navigation, along with phone fixtures.
Temporary profiles removed; health and runtime preservation checks passed.
No physical-device or Safari verification. Evidence and scoped-test details:
[article-images.md](article-images.md), `/tmp/article-images-deploy/fix-live.log`.

## News section, phases 4–5: Since your last visit and Your topics — deployed October 29, 2026 (Central)

Following docs/news-section-handoff.md. Migration `db/023_news_section.sql` (additive, repeatable; applied to
`sql2025`/`WebScraper` before the build, run twice to confirm) adds `MyDailyFeedProfiles.PreviousVisitAt` and the
app-owned `MyDailyFeedProfileTopics` (cascades with its profile). Since your last visit: `ProfileStore.TouchAsync`
moves the old `LastReadAt` into `PreviousVisitAt` when a profile returns after 30 minutes; the News front's first
briefing tile counts headlines-hub stories since then and how many are Kansas City's (none with no profile or on a
first visit). Your topics: a profile follows up to 20 words or phrases (5 alternatives, 5 Except words, one section
or all), matched as whole words in any capitals (terms may start or end with a symbol, such as C++). The follow form
(`/news/topics/follow`) is a plain page that works without JavaScript; `topics.js` adds the live preview from
`GET /me/topics/preview` (verdict, 7-day chart, latest matches with the phrase marked, the Except count). It is a page
rather than the mockup's dialog. Topic pages (`/news/topics/{id}`): new since Mark as read, then 30 days of earlier
matches 20 at a time, each with Share and Save; Edit topic and Stop following. The front has a Your topics box
(desktop) and fold (phones). Every topic read and write is limited to the open profile: another profile's topic answers
404 and the admin sees no topic text. With profiles off, `/news/topics` answers 404. Commit `acaaf6c` on `develop`
(local).

Deployed `mynewspaper-web:news-topics-20261029`, image
`sha256:a774e1b385eca8a464ec6f67205f4b9972418f90e2be3b4818abded8629ba449`, from a clean worktree of `acaaf6c`. Rollback
`mynewsfeed-web:before-news-topics-20261029` (`dfe15f64b102`); a rollback build ignores the new column and table. Port
8085, `dev-bridge`, restart policy and runtime environment hash unchanged; healthy, `/healthz` 200.

Verification: 983 .NET tests (new: the visit rule; topic splitting, whole-word and symbol matching, verdicts,
validation, the 20 limit, profile isolation and deletion with the profile). Locally (profiles on) and live: `topics`
(nonsense, broad and normal phrases with their verdicts, chart, marks, Except; follow, topic page with Share and Save,
Mark as read, the front's box and fold, edit, another profile 404, no-JS follow and its error, stop following;
320/390/430/1440 light and dark overflow, contrast, ids, 44px targets), `news-front` (now also no Since tile on a first
visit) and `headlines-phone` passed. Since your last visit seen at 320px on a throwaway profile with its previous
visit set 3 hours back (live: 61 new, 8 local; three tiles fit). Screenshots inspected: follow form 390 light/dark and
1440 dark, topic page 390 dark, the Since tiles at 320. Throwaway profiles deleted; no topic rows left.

## News section, phases 1–3: News front, News tabs, Kansas City under News — deployed October 29, 2026 (Central)

Headlines becomes a section called News (docs/news-section-handoff.md, saved from `new development/`). The top
navigation is Home · News · Sports · Entertainment · Where to watch · YouTube (Local folded in). News tabs:
News home, All headlines, then each headlines-hub category (the design's order first, others alphabetically), with
Kansas City going to its own page; phones get the Section dropdown. `/news` is the News front (`NewsFront`): a
briefing strip (Kansas City now from the NWS forecast, rendered on the server; Election Day from
`News:ElectionDay`, hidden from the day after), the day so far, the lead and six Top headlines, Kansas City and
Opinion beside them, In pictures (`FeedQuery.GetPhotosAsync`, one photo per source from 36 hours, not the lead's; four
on tablets) and desks for Technology, Linux and Video Games; Share and Save on every row. Phones: the folded front with
three Top headlines (Save with Share stacked), the day so far and More News folds. The headlines list moved to
`/news/all`, where no category means every category; Local moved to `/news/kansas-city` with desktop jump links.
`/local` and `/news` with a list query answer 301 with the query intact; links site-wide use the new addresses; the
service worker caches `v7`. Commits `8df2d7f` and `096f47e` on `develop` (local).

Deployed `mynewspaper-web:news-section-20261029`, image
`sha256:dfe15f64b102672198858825844587df67c13e8b2330db5da58f1a544c8c3195`, from a clean worktree of `096f47e`. Rollback
`mynewsfeed-web:before-news-section-20261029` (`99d260e5e283`, the phone story share release). Port 8085, `dev-bridge`,
restart policy and runtime environment hash unchanged; healthy, `/healthz` 200; `/local` and `/news?category=…` 301.

Verification: 971 .NET tests (new: tab order and Kansas City's page, the six redirects and four non-redirects).
New `news-front` passed locally and live: the front at 320/390/430 × Kansas/Monochrome/Cardinals × light/dark and
744/1024/1440 light/dark (overflow, contrast, ids, 44px targets, briefing tiles inside the screen, Save and Share on
every row, first fold open, Share dialog, phone menu, no-JS Section select, 6-link nav, Kansas City tabs and jump
links). Browser checks moved to the new addresses; locally 19 of 22 passed: `phone-life` (the Watch rail needs the
streaming-catalog key) and `theme-mode` (Settings' theme radios now sit in a collapsed panel since `e23a1d7`) fail for
reasons outside this release, and `headlines-phone` was fixed and passes. Live, the profile-aware checks
(`news-front`, `headlines-phone`, `phone-fronts`, `phone-life`) passed; the others don't support profiles and stop at
the picker. Screenshots inspected at 320/390 and 744/1440, light and dark. `pwa` now matches the current cache and
icons.

## Phone story lists: Share under Save — deployed October 28, 2026 (Central)

The phone refresh gave the Top stories rows one 44px column with only Save, so Share was left on the lead story
alone. Share now sits under Save in that column on every phone top list (Headlines, Home, Sports, Entertainment and
Kansas City news), so headlines keep their width at 320px; the "More nearby" local stories get Save and Share in a
row, as video rows do. `headlines-phone` now checks that every top story has both buttons, Share under Save and inside
its row, and that Share opens the dialog for that story. The Google News request test uses a made-up paper (the Kansas
City Star feed it used is now a real source). Commit `ace1a71` on `develop` (local), which includes `62f43bb`/`a7ea7d2`
(article images).

Deployed `mynewspaper-web:story-share-20261029`, image
`sha256:99d260e5e283ed32caa32a24b95078ac724e137e9de851812102c3ff8a511e5a`, built from a clean worktree of `ace1a71`.
The previous container (article images, started with `docker run`) is kept stopped as
`mynewsfeed-web-before-story-share-20261029`, tag `mynewsfeed-web:before-story-share-20261029`. Port 8085,
`dev-bridge`, restart policy and runtime environment hash unchanged; healthy, `/healthz` 200.

Verification: 960 .NET tests. Locally and live (profiles on): `headlines-phone` (320/390/430 × Kansas/Monochrome/
Cardinals × light/dark, the new Share assertions, Share dialog at 390), `phone-fronts` and `phone-life` passed;
`phone-screens` passed locally. Screenshots inspected: Headlines 320 dark (local) and Kansas City 320 light (live).
Not from this release: `video-share` fails on the pre-change build too (it waits for a visible video Share button);
`phone-life`'s Watch rail needs the streaming-catalog key, so it fails locally only.

## Article image sizing and fallback — deployed October 28, 2026 (Central)

Small images now remain thumbnails; suitable large images use capped feature
layouts. Tiny or failed images collapse to text. Supported BBC thumbnails request
the same asset at higher resolution with a single fallback to the original URL.
No schema or collector changes. The image includes the preceding request checks.

Runtime environment, IPv4/IPv6 ports, network, restart policy, log rotation and
persistent keys volume/access were preserved. Container health and direct/public
HTTPS health endpoints passed. The complete live Chromium suite covered
320/390/430/744/1440px, light/dark, 50 Headlines cases and 36 front-page cases,
source-size limits, dynamic updates, BBC success/fallback, enhanced navigation,
Load more and no-JavaScript behavior. Representative screenshots were inspected,
including the reported BBC photograph at 1024 × 575px on phone and desktop.
The temporary profile was removed. No physical-device or Safari check was
available. Evidence: `/tmp/article-images-deploy/`; implementation and limits:
[article-images.md](article-images.md).

## Request checks: YouTube by channel ID, news through Google News — deployed October 29, 2026 (Central)

A YouTube request is ready once the checker finds its channel ID ("Channel ID found from the link", plus "Already
collected"); the channel's feed is no longer tested, because YouTube's `feeds/videos.xml` answers 404 from this
server while the scraper still collects from the channel page. A news link with no feed of its own, or a site that
blocks the checker, is offered through its Google News search feed (`site:<domain>[/path/] when:30d`, US English,
the form the USA Today sources use), only when it has headlines and at least 90% of them name that site as their
source: "The site didn't respond · Google News feed found (100 headlines)". Admin's Find feeds uses the same check.
The admin still approves every request. Commit `ab7241e` on `develop` (local).

Deployed `mynewspaper-web:request-checks-20261029`, image
`sha256:70c4ed2149807352797bc967b4ad3048ee78e2b516ff8ea84071ab275b9a3487`, built from a clean worktree of `ab7241e`.
Rollback retained as `mynewsfeed-web:before-request-checks-20261029` (`dc23d626d211`, the U4 image). Port 8085,
`dev-bridge`, restart policy and runtime environment hash unchanged; healthy, `/healthz` 200. No database change.

Verification: 960 .NET tests (three new: the Google News lookup and its 90% source rule, a blocked site offered
through Google News, a YouTube channel ready with its feed down). The real checker against the real sites:
kansascity.com and www2.ljworld.com get a 100-headline Google News feed; `youtube.com/@mkbhd` is ready; an unknown
handle still needs a channel ID; `kcstar.com` still needs a feed address (it only redirects to kansascity.com, and
the block stops the check before the redirect lands; enter `kansascity.com` in Find feeds instead). Live, with a
throwaway profile: a kansascity.com news request and a YouTube request sent from Settings at 390px, both checked
("Checked"), and read in Admin · Requests at 320/390/430/1440px light and dark: no sideways scroll, contrast audit
clean, the summary and the Google News feed address inside the screen; screenshots inspected at 320 and 390. The
profile and its requests were deleted. Seen, not from this release: admin pages show an unstyled "Saved articles
could not sync" line from `public.js` when no profile is open.

## Profiles U4: new edition setup and full profile deletion — deployed October 29, 2026 (Central)

The picker's **+ Start a new edition** opens `/editions/new`: one form, prefilled from the base edition, that
`profiles.js` steps through four screens (name, color and PIN; teams with a search; services, up to 20; home
sections with show/hide and move-up), posting as JSON so a refused name returns to step 1 with every choice kept.
Without JavaScript it is one long form that posts to `POST /profiles/new` (`setup=1`; `ProfileSettings.FromSetup`
lays the fields over the base edition). Deleting a profile (`ProfileAdmin.DeleteAsync`) now runs one transaction
under the channel-rollout lock: devices, the profile (cascading Saved, follows and requests), then any YouTube
channel only its requests added and that channel's stored videos. Commit `afba7db` on `develop` (local); built
together with `e23a1d7` (Settings sections independently collapsible).

Deployed `mynewspaper-web:profiles-u4-20261029`, image
`sha256:dc23d626d2115e1f1e0752e24f35b76350f5f113b32fcf2d6d7ebce67c34792a`, built from a clean worktree of `e23a1d7`.
The previous container (`settings-panels-20261028`, started with `docker run`) is kept stopped as
`mynewsfeed-web-before-profiles-u4-20261029`, with rollback tag `mynewsfeed-web:before-profiles-u4-20261029`
(`b579fd2c0382`). Port 8085, `dev-bridge` network, restart policy and runtime environment hash unchanged;
container healthy. No database migration.

Verification: 957 .NET tests passed, including the new setup-to-settings and full-deletion tests. Live (profiles
on): `profile-setup` passed 320/390/430/768/1440px light/dark (overflow, contrast, unique IDs, 44px targets), the
full phone flow from the picker, the taken-name return to step 1, and the no-JavaScript form and its error;
`editions`, `admin-profiles` and `settings-panels` passed. Known failures, not from this release:
`profile-settings` (phone release 2's folded home duplicates `[data-home-section]`) and `settings-order` (its
`a[href="settings"]` selector no longer matches; fails on the pre-U4 build too). Only the household's two profiles
remain afterwards.

## Phone refresh, release 4: Entertainment, Movies, TV, Digital releases and Box office — deployed October 28, 2026 (Central)

Below 744px these pages get folded phone fronts beside their unchanged desktop markup: a scheme ribbon with one
native select (Section, Release type, Week, Source, View; each with a no-JavaScript Go), a dark focal block, a middle
list and exclusive native folds. Entertainment: lead, On screen timeline, top three, box office/TV/releases/Friday/
headline folds. Movies: week strip, next release, the week's timeline, AMC link under In theaters, Looking ahead and
In theaters folds; title searches keep the full layout. TV: tonight's or the next episode (batch drops labelled as
batches), the week's timeline, Your shows, TVmaze attribution. Digital releases: next release or the empty state,
every remaining release, Still in theaters. Box office Results: No. 1 with openers/best hold/biggest drop, top five
with gross bars, 6–10/biggest totals/next weekend folds; forecast views keep their section under the ribbon. Trending
folds are left out (they need the live catalog API). Dark-block and top-list title links are now 44px targets on
every phone page. Fixed: `/movies?type=digital` (the Digital option of the Movies select) returned a 500 because
static rendering ran the week math on an unset date after `NavigateTo`; it now redirects to `/movies/digital`.
Commit `ec95073` on `develop` (local).

Deployed `mynewspaper-web:phone-screens-20261029` (tag uses the UTC date), image
`sha256:2cf78ecff635198b5318f61442dd32349b9ccccea105ad550c3a4993a9d30092`.
Rollback retained as `mynewsfeed-web:before-mynewspaper-phone-screens-20261029` (`ce1907bbd201`).
Runtime environment hash unchanged; container healthy, `/healthz` 200. No database migration or collector changes.

Predeploy (local, profiles off): build with zero warnings/errors, 955 .NET tests, eslint, prettier and
`git diff --check` passed. New `phone-screens` passed 320/390/430px × Kansas/Monochrome/Cardinals × light/dark on
Entertainment, Movies, TV, Digital releases and all three Box office views: one ribbon, hidden desktop heading,
contrast audit, overflow, unique IDs, unsplit titles, first fold open, exclusive/keyboard folds, 64px summaries, 44px
targets; lead save/share, every ribbon select, the week strip, Movies search, Digital source, TV week and Box office
chart/View; no-JS folds and Go paths at each phone width; 744/1024/1440px light/dark unchanged. Full browser suite:
43 passed, 10 skipped (profiles-only), 13 failed; 11 are the known local-only ones without a streaming-catalog key,
and `section-hubs` and `theatrical-provider` were fixed (Entertainment's phone Section select; the phone Movies page
had lost the AMC Theatres link) and pass. Local evidence: `/tmp/release4/local/`.

Live (profiles on, throwaway profile): `phone-screens` passed the same matrix. The first two live runs timed out
because third-party poster hosts (`dvdsreleasedates.com`) throttled the run's fresh fetches; without JavaScript
Chrome also ignores `loading="lazy"`. The check now blocks external images and checks the pages' no-poster
fallbacks. The throwaway profiles (including two diagnostic probes) were deleted, and 14 headless test-device
records without a profile were removed; Jason and Jacob remain. Live screenshots reviewed, including the TV empty
state for a profile with no shows. Evidence: `/tmp/release4/deploy/`. Not checked: the Digital releases empty state
with real data (every source has dates), a real device, VoiceOver/TalkBack.

## Phone refresh, release 6: All headlines, Saved and Settings — deployed October 28, 2026 (Central)

Below 744px, the Sports and Entertainment headlines pages, Saved and Settings keep their existing lists and
forms under a scheme ribbon with one native select: Section, a counted Show (All/Articles/Videos) and Jump to.
Each select has a no-JavaScript Go path; Jump to uses the local `/navigate` redirect, which keeps the
`#section`. The first article or saved item takes the dark feature styling; Load more adds none, and
removing the featured saved item moves it on. Profile-only Settings anchors appear only with a profile open.
Ribbon titles step down to 36px below 360px so long titles wrap between words, and the ribbon `h1` shows
no focus ring after navigation. Wider layouts are unchanged. Commit `63748dc` on `develop` (local).

Deployed `mynewspaper-web:phone-tools-20261028`, image
`sha256:ce1907bbd20102f4002a4dd6c3e5fc4a02fda273a1028c3f6c6f511c93a5f69b`.
Rollback retained as `mynewsfeed-web:before-mynewspaper-phone-tools-20261028` (`7176ce3f5fa0`).
Runtime environment hash unchanged; container healthy, `/healthz` 200. No database migration or collector
changes.

Predeploy (local, profiles off): build with zero warnings/errors, 955 .NET tests, eslint, prettier and
`git diff --check` passed. New `phone-tools` passed 320/390/430px × Kansas/Monochrome/Cardinals ×
light/dark: ribbon and hidden desktop heading, contrast, overflow, unique IDs, 44px targets, one first-item
feature, no split title words or title focus ring, counted Show options, Settings anchors; Section select,
Load more, feature Share/Escape, Saved removal/Show → Videos, Jump to scrolling; empty and unreachable (503)
Saved; no-JS Go paths at each phone width; retained 744/1024/1440px layouts in both themes. Full browser
suite: 40 passed, 10 skipped (profiles-only), 15 failed. 11 are the known local-only ones without a
streaming-catalog key (watch*, active-tv, us-options, mixed-search-nav, track-series, phone-life's Watch
rail). `sports-news`, `entertainment-news`, `watch-later` and `section-select` were updated to the visible
copy and then passed. Local evidence: `/tmp/release6/local/`.

Live (profiles on, throwaway profile): `phone-tools` passed the same matrix, including the profile-only
Jump to anchors. The throwaway profile was deleted and two headless test-device records without a profile
were removed; Jason and Jacob remain. Live screenshots reviewed. Evidence: `/tmp/release6/deploy/`.
Not checked: a real device, VoiceOver/TalkBack.

## Phone refresh, release 5: Where to watch, Local and YouTube — deployed October 28, 2026 (Central)

Below 744px, Watch gains a scheme ribbon with search/Browse, numbered trending-movie shelf and folds
for Trending TV, new movies and services. Search/provider results, sorting, paging and detail dialogs remain.
Local highlights the labelled first forecast period, wind and next three periods, then three news stories
and folds for further news/full forecast/event links. One request populates both layouts; location/reset,
NWS attribution, stale/error fallbacks and feed coverage context remain. YouTube features one video with
Watch later/Share, three latest videos and short category/channel folds. Its combined native selector keeps
category/query context. Complete phone news/video lists preserve access to all loaded rows and pagination.
Native disclosures are exclusive, and controls retain no-JavaScript fallbacks. Wider layouts are retained,
including corrected full-width Watch shelves through the new display:contents wrapper.

Deployed `mynewspaper-web:phone-life-20261028`, image
`sha256:7176ce3f5fa02019ab52f8ee66e89e6f4cdf5506f3199ff097b88ce7ea0a2a3c`.
Rollback retained as `mynewsfeed-web:before-mynewspaper-phone-life-20261028` (`c1e3fa5259c3`).
Runtime environment values, entrypoint, ports, keys/mounts, restart policy, health check and network
verified preserved. No database migration or collector changes. Source/docs included in the release 5 local check-in on `develop`.

Predeploy: solution build (zero warnings/errors), production publish/static-asset guard, lint and
changed-code formatting passed. `phone-life` passed 320/390/430px in Kansas/Monochrome/Cardinals,
light/dark: contrast, overflow, unique IDs, 44px targets, native/keyboard folds, weather, save/share,
Watch modal/search/sort, category/query/reload, complete lists and empty video search. No-JavaScript
controls passed at each phone width in default fallback rendering. Retained 744/1024/1440px layouts
passed, with a final targeted Watch width/alignment/arrow check after the wrapper correction.
Existing share/category/location/weather-icon/Watch featured/sort/results/paging/detail suites passed.
`phone-local-weather` checked single/stale/empty/503 forecasts and one request across layout resizing.
No full .NET test-suite rerun for this UI-only change. Local evidence: `/tmp/release5/`.

Live: the same phone widths, three schemes and light/dark matrix passed with profiles enabled, including
contrast/touch targets, folds/keyboard/no-JS, save/unsave through an isolated mocked Saved API, sharing,
modal/search/sort, category/query/reload, complete lists, empty videos and retained wider layouts.
Weather fixtures isolate the matrix; a separate real forecast returned 200 and populated all four periods.
Authenticated main routes passed; `/healthz` 200 Healthy and container healthy. Recent application logs
had zero error markers and included league-phase checks. Representative screenshots across all phone
widths/themes, wider Watch and the real weather rendering reviewed. Temporary profile deleted.
Two orphaned headless test-device records were removed; Jason and Jacob remain. Live evidence: `/tmp/release5-deploy/`.
Limits: no real device or VoiceOver/TalkBack; geolocation was checked on the local secure preview,
not over the live container's plain-HTTP test address. Release 4 remains pending.

## Scheme-colored phone ribbons — deployed October 28, 2026 (Central)

Shared phone title bands now follow the selected scheme's secondary color and paired text color.
Daily Ribbon stays yellow in light mode; dark mode uses a muted blend with the scheme surface and
its normal text color. The live dot uses the ribbon text color and the search button has a contrasting
outline. Applies to Home, News and the released sports ribbons; tablet/desktop layouts are retained.

Deployed `mynewspaper-web:scheme-ribbons-20261028`, image
`sha256:c1e3fa5259c338a2a1b2ed24a88c60d7b79f610d2131119a8ce07af29a24379e`.
Rollback image retained as `mynewsfeed-web:before-mynewspaper-scheme-ribbons-20261028`
(`79a593092535`). Environment values, entrypoint, health check, ports, keys/mounts,
restart policy and network verified preserved. Container healthy; `/healthz` returns 200 Healthy.
No database or collector changes. Profiles remain enabled, so anonymous content requests require
an open profile; admin redirects to sign-in.

Build/publish/static-asset guard, lint and changed-code formatting passed. Local checks passed all
16 schemes in light/dark at 320/390/430px, including contrast over blended weather backgrounds and
overflow; search, saved preference reload, 44px controls, no-JS and tablet/desktop checks passed.
Seven other shared-ribbon routes passed phone visibility/overflow checks in both modes.
Evidence: `/tmp/ribbon-schemes/` and `/tmp/ribbon-deploy/`.
Live verification passed with profiles enabled: all 16 schemes × light/dark × 320/390/430px, including
blended ribbon/weather contrast, overflow, saved preference reload and 44px input/button targets.
Search navigation and seven other shared-ribbon routes passed in both modes at each phone width.
Phone ribbons stay hidden at 768/1440px; no-JavaScript Home passed at 320px. Live Kansas light,
City dark and Coastal light screenshots inspected. The temporary test profile and two orphaned
headless test devices were removed; Jason and Jacob remain.
Real-device and VoiceOver/TalkBack checks unavailable.

## Phone soccer standings: points, then W–D–L — deployed October 28, 2026 (Central)

Release 3 had promoted the first published column, games played, for soccer. The phone leader, My Teams tiles,
top-six rows and folded remaining rows now highlight published **points** with **W–D–L** underneath. The column
label is PTS and the leader explicitly says Points. Applies to NWSL, MLS, Championship, Premier League and Big 12
soccer, including the College standings fold. Missing points remain unknown rather than being calculated from
results or substituted with games played. Other sports and desktop table columns/order keep their existing behavior.

Deployed `mynewspaper-web:soccer-points-20261028` (`79a593092535`) through Compose; rollback retained as
`mynewsfeed-web:before-mynewspaper-soccer-points-20261028` (`df265789b18e`). Environment, ports, persistent keys,
mounts, restart policy, health check and network preserved. No database or collector changes. Included in the subsequent scheme-ribbon local check-in.

Predeploy: 955 .NET tests passed, including published/missing soccer points, correct W–D–L for College soccer,
and a seven-team fixture checking the leader, tiles and folded rows. Production publish/static-asset guard and
JavaScript lint passed. `phone-soccer-standings` compares every phone row against the published desktop table
for NWSL/MLS/Championship/Premier League, and checks NWSL at 320/390/430px light/dark, personal tiles, folds,
reload, contrast, overflow, no-JavaScript rendering and unchanged desktop columns at 744/1440px. Representative
phone screenshots reviewed. The same checks passed live with profiles on, including published values for all four
professional soccer leagues, six NWSL width/theme cases, 44px targets, personal tiles, folds/reload and no-JS.
Live phone screenshots reviewed; health is 200 Healthy. Temporary profile and two orphaned headless device rows
removed; Jason and Jacob remain. Evidence: `/tmp/soccer-points/`.
Not checked: a real device or VoiceOver/TalkBack.

## Phone refresh, release 3: Top 25, Standings, College Sports and F1 — deployed October 28, 2026 (Central)

Below 744px, the four sports pages adopt the shared ribbon, dark focal block, short lists/timelines and exclusive
native folds. Top 25 shows No. 1, real rank changes, 2–10 and the remaining/Big 12/new/dropping teams. Unknown
previous ranks produce no invented movement tiles. Standings preserves provider groups and order, shows each
group's first six rows, folds the remaining places, and adds My Teams tiles, upcoming games and the matching
Big 12 poll when available. Only valid records get win bars; missing values remain unknown. College's featured
game follows the combined-rank/highest-ranked-participant/first-scheduled rule. The overview adds a weekly
timeline, Your teams, Friday games, standings, poll, further games and Explore links. Other College views keep
their existing content with a Sport ribbon and plain view links. F1 has its next weekend and timetable, top-three
points bars/gaps, upcoming rounds, full driver/constructor folds and calendar; Results/Standings retain their
tables with native phone selectors. The standings snapshot is reused rather than fetched twice for the phone.

College's Friday fold does not assume every sport plays at night; other sports link to their real schedules instead
of fabricating unqueried data. F1 headlines link to Sports' F1 category rather than an unloaded headlines fold.
My Teams uses the open profile, including no-JavaScript rendering, or browser preferences with profiles off.
Empty sections are omitted and availability/freshness/source context remains visible. Desktop/iPad layouts are
retained from 744px. No schema or collector changes.

Deployed `mynewspaper-web:phone-sports-20261028` (`df265789b18e`) through Compose, healthy; rollback
`mynewsfeed-web:before-mynewspaper-phone-sports-20261028` (`0949497c41b3`). Environment, ports, persistent keys,
mounts, restart policy, health check and network match the previous container.

Predeploy: 948 .NET tests passed, including featured-game ranking/fallbacks, unknown/empty records and polls,
one-game College and date-only F1. Production publish/static-asset guard, JavaScript lint and changed-file formatting
passed. `phone-sports` passed at 320/390/430px in Kansas/Monochrome/Cardinals, light/dark: contrast, overflow,
44px targets, 64px fold summaries, unique IDs, one initially open fold, exclusive/keyboard folds and selects without
JavaScript, JS selectors and reload persistence, My Teams and F1 calendar. Retained layouts checked at
744/1024/1440px. Scoped tablet checks passed all four pages at 430/743/744/768/820/834/1023/1024/1440px in both
themes; scoped contrast passed three schemes at 390/1440px. Final-image `section-select` passed all College
sports/views, F1 selectors and no-JS navigation. `phone-fronts` and `personal-features` passed as regressions.
Live `phone-sports` passed the same matrix and interactions with profiles on, including no-JavaScript timetable
opening, native selectors and My Teams, plus retained tablet/desktop layouts. Representative screenshots at each
phone width and in both themes reviewed. The temporary profile was deleted. The initial image (`ca8534226742`)
was refined to give Standings' personal tiles two readable columns; the final image's tiles passed all three phone
widths, light/dark, in preview and live. Final live desktop smoke, no-JavaScript personal tiles and health also passed
(`final-live.log`); final Standings screenshots reviewed. Both temporary profiles and four orphaned headless device
rows were removed; Jason and Jacob remain. Startup league-phase checks succeeded. Five JavaScript unit tests
passed. Evidence: `/tmp/release3/` (live screenshots under `live/`).
Not checked: a real device or VoiceOver/TalkBack. Included in the subsequent scheme-ribbon local check-in.

## Home lead uses Top Headlines only — deployed October 28, 2026 (Central)

Home's main story on desktop and phones now comes only from **Top Headlines**, rather than all non-Opinion
Headlines categories. The existing hourly publisher rotation and newest-story ordering remain; an empty category
does not fall back to another category. The lead refresh endpoint and edition-picker preview share the same query.

Deployed `mynewspaper-web:top-headlines-lead-20261028` (`0949497c41b3`) through Compose. Rollback:
`mynewsfeed-web:before-mynewspaper-top-headlines-lead-20261028` (`e1ea2d9dc6c1`). Runtime environment, ports,
mounts, restart policy, health check and networks preserved. No schema or collector changes.

Predeploy: 934 .NET tests passed, including mixed categories, empty Top Headlines and hourly publisher rotation;
production publish and static-asset guard passed. Local profiles-off Home checks at 320/390/430/768/1440px in
light/dark confirmed Top Headlines attribution, identical phone/desktop story links, Share opening/closing,
reload keeping the selected story and no overflow. All six phone screenshots reviewed; no-JavaScript phone
and `/home/lead` passed. Live, with profiles on, the same widths/themes passed: Top Headlines attribution,
matching desktop/phone story, Share, reload, overflow, no-JavaScript phone, refresh endpoint and health (200).
All six phone screenshots reviewed. Temporary profile and two orphaned headless test-device rows deleted;
Jason and Jacob remain. Real-device verification unavailable.
Evidence: `/tmp/home-top-headlines/` (live/build evidence under `deploy/`).

## Phone refresh, release 2: Home, Sports and Scoreboard — deployed October 28, 2026 (Central)

Below 744px, Home, Sports and Scoreboard use the folded phone layout: a title ribbon, a dark lead block where
appropriate, short timelines and headline lists, and exclusive native folds. Home includes weather and Where to
watch search, respects hidden sections, and links into the other sections. Sports includes headlines, today's games,
Big 12 standings, AP Top 25 and F1. Its College Sports fold is deferred because the front loads no college schedule.
Scoreboard includes a date stepper, Sport select, live-game tiles when available, an upcoming timeline and league
folds; refresh preserves the open fold. Adds PhoneScoreTiles and PhoneText. The new wrapper required the retained
scoreboard grid's direct-child selectors to follow it; this fixes the tablet overflow found during verification.
Commit `9e96beb` (committed locally after deployment, at the user's request).

Deployed `mynewspaper-web:phone-fronts-20261028` (`e1ea2d9dc6c1`) through Compose, with the previous image retained
as `mynewsfeed-web:before-mynewspaper-phone-fronts-20261028` (`6818b90cff80`). Environment, ports, mounts and restart
policy match the previous container. No schema or collector changes. Health endpoint returns Healthy.

Validation: 931 .NET tests passed; production publish/static-asset guard and JavaScript lint passed. Changed JS/CSS
and browser checks pass formatting; the repository-wide formatting check flags the existing, unchanged
`tv-series-delete.browser.cjs`. Local `phone-fronts` passed at 320/390/430px, Kansas/Monochrome/Cardinals in light/dark,
with screenshot inspection, contrast, overflow, touch targets, duplicate IDs, first/exclusive folds without JavaScript,
date navigation, Sport/Section selects, open-fold preservation on refresh and a hidden Home section. Retained layouts
checked at 744/1024/1440px; scoreboard regression also checks 768px. Older browser checks now target the retained
controls and leave the replaced phone layouts to `phone-fronts`. Live `phone-fronts` passed the same widths, themes,
schemes and flows with profiles on; screenshots reviewed. Its temporary profile and two orphaned headless test device
rows were removed; Jason and Jacob remain. Live health is 200, public routes enforce the profile gate and admin
redirects to sign-in; startup league-phase checks succeeded. All 16 schemes passed contrast on Home, Sports,
Where to watch and Settings at 390/1440px, light/dark; five JavaScript unit tests passed. The full 60-check browser suite
finished in two batches (the first shell session ended during the long contrast check): 50 passed, zero failed,
10 skipped (admin/profile prerequisites or unavailable sport fixtures). The second batch reran contrast and completed
all remaining checks. Tablet regression passed 17 pages at 430/743/744/768/820/834/1023/1024/1440px, light/dark.
Not checked: a real device or VoiceOver/TalkBack; no in-progress games were present during the phone checks.
Evidence: `/tmp/release2/`.

## Phone refresh, release 1: the folded Headlines front — deployed October 28, 2026 (Central)

First release of [the mobile refresh](mobile-refresh-handoff.md). Below 744px, `/news` shows the approved folded front:
a yellow ribbon ("Updated … · N stories · N sources", **The day so far** or the category's name, one **Category**
select with a no-JavaScript Go button), the lead story in a dark block with labelled Save and Share ("Just in" on an
image when it is under an hour old), **Top stories** (feed order; the site doesn't count reads, so not "most read"), a
**Latest** timeline with **More stories**, and **By category**: one fold per other headline category with its three
newest stories, local first and opinion last, the first one open, one open at a time (native `<details name>`). A
search, a source filter or a later page keeps the old layout on every width; iPad and desktop are unchanged. Adds
`mobile.css` (`.mx-` prefix), `Components/Phone`, `SectionSelect`'s `AlwaysSelect` (not yet used), labelled Save/Share
buttons and `FeedQuery.GetLatestByCategoryAsync`. Commit `47ab9ca`.

Deployed `mynewspaper-web:phone-headlines-20261028` (`6818b90cff80`) through Compose with `--pull`, healthy; rollback
`mynewsfeed-web:before-mynewspaper-phone-headlines-20261028` (`6b769189dc48`). Environment identical (sorted hash).
Predeploy: 931 .NET tests (the new per-category query: newest three, enabled sources only, undated last, empty
categories empty); locally with profiles off and on, `headlines-phone.browser.cjs` at 320, 390 and 430px in light and
dark under the kansas, monochrome and cardinals schemes (no sideways scroll, the select inside the edge at 16px, no
repeated ids, fold summaries 64px and every other target 44px, first fold open, the dark block distinct from the page,
contrast), with JavaScript off (opening one fold closes the other, Enter and Space toggle), the Category select
(title follows; no fold for the open category), More stories and a search falling back to the old layout, and 744,
1024 and 1440px unchanged; screenshots reviewed against the canvas. With profiles off the full browser suite ran:
`compact-stories`, `frontpages`, `publisher-layout` and `video-share` had tested the old phone cards on `/news` and now
leave that to `headlines-phone` (the cards are still checked on Sports and Entertainment headlines) and pass; the other
failures were the Watch checks, `active-tv`, `us-options`, `mixed-search-nav` and `track-series`, which need the
streaming-catalog key locally. Live: `headlines-phone` passed with a throwaway profile at the same widths, themes and
schemes (screenshots reviewed); the profile was deleted and the 4 headless device rows removed (Jason and Jacob
remain). Not checked: a real device, VoiceOver/TalkBack, and the other pages' live layout (unchanged code).

## Settings: your profile, TV shows, YouTube channels and requests — deployed October 28, 2026 (Central)

Stage U3. Settings gains **Your profile** (name, color, Change PIN, this device's picker, sign out), **What's yours
and what's shared**, **TV shows** (follow from a TVmaze search and unfollow, for the open profile; the last unfollow
stops collection, history kept), **YouTube channels** (household channel switches in a collapsible list, your other
channels, household picks) and **Requests** (the form and Your requests with the admin's reply). All are plain forms
to `/me/*`, which the gate covers and antiforgery protects, coming back with fixed message codes (no names in the
address). **Track this show** (`/tv/track`) now follows for the open profile; before this, a show added there since
switch-on reached nobody's calendar. The household channel rollout service now runs every 15 minutes, and saving a
feed in admin triggers it. `TvQuery.FollowAsync` re-enables a tracked show in SQL. Commit `200b540`.

Deployed `mynewspaper-web:make-it-yours-20261028` (`6b769189dc48`) through Compose with `--pull`, healthy; rollback
`mynewsfeed-web:before-mynewspaper-make-it-yours-20261028` (`6d0b593a9586`). Environment identical (sorted hash).
Predeploy: 930 .NET tests (another profile's requested channel can't be followed or listed and answers like an
unknown ID; the last unfollow stops collection and following again restarts it; `/me` forms need an open edition and
antiforgery and redirect with codes; `/me` is 404 with profiles off); locally with profiles on,
`settings-sections.browser.cjs` at 320, 390, 430, 744, 820, 1024 and 1440px in light and dark (name and color, the
device picker, Change PIN wrong then right, follow and unfollow from Settings and from Track this show using a show the
household already tracks, household channel off and on, a request listed as Waiting, sideways scroll, contrast, 44px
targets; screenshots reviewed), plus `profile-settings` and `editions`; with profiles off, `personal-layout`,
`settings-order`, `theme-mode` and `personal-features` passed and `/tv/track` still renders the old form
(`track-series` needs the streaming-catalog key). Live: `settings-sections`, `profile-settings` and `editions` passed
at the same widths and themes. The profile-settings check had cleared the household admin mark (it marks its own
throwaway profile); Jason was marked again, and the check now restores the previous mark itself (verified live).
Throwaway profiles, their request, and the device rows the checks created were removed; tracked TV series (10 enabled),
follows and the 32 rollouts are unchanged. Not checked: a real device.

## Settings saved to each profile, and the household admin's sign-in link — deployed October 28, 2026 (Central)

Stage U2: light/dark mode, color scheme, home sections, My Teams, services, Settings order and the scoreboard's stars
are saved to the open profile instead of the browser (`profiles.js`'s `window.myNewspaperPrefs`, PUT
`/api/me/settings`), so they follow a person to every device. A browser's old settings are imported once, into the
first profile opened there, and then removed. The profile's theme and scheme are on `<html>` from the server, so pages
don't flash. Scripts read the site defaults from the page. The picker shows **Your teams**. `db/022` (applied earlier
the same day) adds the household admin profile: with profiles on, only that profile's pages show **Admin sign in**;
Jason's profile was marked as the household admin after the deploy. Commits `2871b34` and `0eac3c4` (with
`9fcb03f`, already live).

Deployed `mynewspaper-web:profile-settings-20261028` (`6d0b593a9586`) through Compose with `--pull`, healthy; rollback
`mynewsfeed-web:before-mynewspaper-profile-settings-20261028` (`0035a6d8fb4d`). Environment identical (sorted hash).
Predeploy: 927 .NET tests (scoreboard stars, page-data escaping, damaged stored values); locally with profiles on,
`profile-settings.browser.cjs` at 320, 390, 430, 744, 820, 1024 and 1440px in light and dark (import once and the old
keys removed, theme and scheme on `<html>` from the server, the imported hidden home section applied on another
device, a theme change on one device shown on the other, a second profile on the same device starting clean, the Admin
sign in link only for the household admin's profile, Settings wording, no sideways scroll, contrast; screenshots
reviewed) and `editions.browser.cjs`; with profiles off, 14 public checks passed (`watch-services` needs the
streaming-catalog key, which the local run doesn't have). Live: `profile-settings.browser.cjs` and
`editions.browser.cjs` passed at the same widths and themes; their throwaway profiles and the device rows the checks
created were removed. Not checked: a real device.

## Standings My Teams highlight — deployed October 28, 2026 (Central)

From 744px the standings page's ruled-paper reset cleared every cell's background, which also cleared the selected
team's fill and left the selection text color on the page: a faint edge only in light mode, unreadable dark text in
dark mode with Daily Ribbon. `sections.css` now keeps the selection fill and text color on selected rows there, as on
phones. Commit `9fcb03f`. Built from the live code (`edb17a6`, the U1 picker release) plus only that change, in a
temporary detached worktree, so the not-yet-checked settings work (`2871b34`) was not deployed.

Deployed `mynewspaper-web:standings-highlight-20261028` (`0035a6d8fb4d`) through Compose with `--pull`, healthy;
rollback `mynewsfeed-web:before-mynewspaper-standings-highlight-20261028` (`1b52180fbe9c`). Environment identical
(sorted hash), profiles still on. Locally: BYU and Kansas selected in Daily Ribbon, Monochrome, Kansas and Halloween,
light and dark, at 390 and 1440px: filled rows, text readable, contrast audit clean, screenshots reviewed. Live, with
a throwaway profile (deleted afterwards, with its devices): 390 and 1440px in light and dark, yellow fill with dark
text, contrast clean, no sideways scroll. Not checked: a real device.

## Household profiles switched on — October 28, 2026 (Central)

Stage U1's switch-on, on the same image as the entry below (`mynewspaper-web:profiles-picker-20261028`,
`1b52180fbe9c`). The admin created the first profile (Jason, with its PIN) in Admin · Profiles and confirmed a fresh
`WebScraper` backup. `db/021_seed_first_profiles.sql` ran and then ran again with no further changes: the profile
follows all 10 enabled TV series and all 32 YouTube channels, 32 channels are recorded as rolled out, and its Saved
list is identical to the shared list (6 articles); `MyDailyFeedSavedList` is kept for rollback. The private override
gained `Profiles__Enabled: "true"` and the container was recreated: healthy, and every other setting identical
(sorted hash with that one line left out). Every public page now redirects a device without an open profile to
`/editions?returnUrl=…` and scripts get 401 `{ "gate": "/editions" }`; `/healthz` and the admin sign-in are unchanged.
`editions.browser.cjs` passed on the live build at 320, 390, 430, 744, 820, 1024 and 1440px in light and dark (gate and
return, PIN pad as bottom sheet and dialog, wrong PIN, lockout, no-JavaScript form, edition chip, phone menu, script
gate, sign-out, sideways scroll, contrast, 44px targets); screenshots reviewed. Its two throwaway profiles were deleted,
and the device rows the checks and probes created (15 from the browser, 12 from `curl`) were removed; one profile and
two unrelated devices remain. Rollback: remove the `Profiles__Enabled` line and recreate the container (or run the
previous image), and the shared Saved list and household TV/YouTube views come back. Not checked: the admin opening
Jason's edition with its PIN (the admin does this), a real device. The existing public browser checks now land on the
picker and need a test profile before they can run against this site again.

## Edition picker built, profiles still off — deployed October 28, 2026 (Central)

Stage U1's code, deployed ahead of switch-on: the edition picker (`/editions`, counts-only previews, PIN pad), the
first-PIN page (`/editions/{id}/new-pin`), `profiles.js` (PIN pad and the scripts' gate redirect), the header's
edition chip and the phone menu's Switch edition, and `db/021_seed_first_profiles.sql` (not run). `Profiles:Enabled`
stays `false`, so readers see no change: none of the new markup renders without an open profile, and `profiles.js`
only wraps `fetch` to watch for the gate's 401. Commit `fb9d61e`.

Deployed `mynewspaper-web:profiles-picker-20261028` (`1b52180fbe9c`) through Compose with `--pull`, healthy; rollback
`mynewsfeed-web:before-mynewspaper-profiles-picker-20261028` (`d3a35f20d238`, the A2 release). The container's
environment, sorted and hashed, is identical to the previous container's. Predeploy: 925 .NET tests (including the
picker never printing a followed show or channel name, and the first-PIN page); `editions.browser.cjs` locally with
`Profiles__Enabled=true` at 320, 390, 430, 744, 820, 1024 and 1440px in light and dark (gate, PIN pad as sheet and
dialog, wrong PIN, lockout, no-JavaScript form, edition chip, phone menu, script gate, sign-out, sideways scroll,
contrast, 44px targets; screenshots reviewed); with profiles off locally, `personal-layout`, `tablet-layout`,
`scheme-contrast`, `theme-mode`, `shared-saved` and `branding` passed (`mixed-search-nav` needs the streaming-catalog
key, which the local run doesn't have). `021` dry-run in a rolled-back transaction. Live: `/`, `/tv`, `/videos`,
`/saved`, `/entertainment`, `/settings` 200 with no chip or alert markup for a reader; `/editions`, `/profiles` and
`/api/me` 404; `/admin/profiles` redirects to login; `personal-layout`, `branding` and `theme-mode` passed on the live
build (`editions` skipped itself, as profiles are off). Logs showed no errors. Mobile checks of the picker on the live
build wait for switch-on, when it first becomes reachable. Not checked: a real device.

## Admin requests and the header alert, profiles still off — deployed October 28, 2026 (Central)

Stage A2 of the household profiles plan: Admin · Requests (`/admin/requests`: Waiting / Added / Declined / All, the
queue beside the selected request from 1024px, Check again, Test feed, Add channel or source and approve, approve and
follow for an already-collected channel, Decline with a reason), **Requests** in the admin menu, the admin-only
Requests alert in the public header (bottom sheet on phones, popover from 744px; locked profiles first with Unlock and
Reset PIN; Mark all seen), **Suggest to everyone** on the YouTube feed editor, and the request checker now running.
The checker reads pages up to 5 MB (was 2 MB), because a YouTube channel page is about 2.6 MB and every `@handle` link
failed before. `Profiles:Enabled` stays `false`; readers see no change apart from one small script, `admin-alert.js`,
that does nothing without the admin's header markup. Commit `dd8a434`.

Deployed `mynewspaper-web:profiles-requests-20261028` (`d3a35f20d238`) through Compose with `--pull`, healthy; rollback
`mynewsfeed-web:before-mynewspaper-profiles-requests-20261028` (`0b3b7503c1b7`, the A1 release). The container's
environment, sorted and hashed, is identical to the previous container's; port 8085 unchanged. Predeploy: 923 .NET
tests (including news-source approval, a YouTube-sized page, the Requests page in the admin privacy test and the
admin-only alert), ESLint and Prettier clean for the changed files, `admin-layout.browser.cjs` on 24 pages and
`admin-profiles.browser.cjs` at 320–1440px in light and dark. Live: `/`, `/tv`, `/videos`, `/saved`, `/entertainment`,
`/settings` 200 with no alert markup for an anonymous visitor; `/profiles` 404; `/admin/requests` redirects to login;
`/admin/api/requests/summary` 401. With seeded rows (a throwaway profile with five waiting requests and one declined,
and a locked profile; deleted afterwards), `admin-requests.browser.cjs` passed on the live build at 320, 390, 430,
744, 820, 1024 and 1440px in light and dark: no sideways scroll, contrast, 44px targets, queue and editor layout, the
live checker resolving `youtube.com/@LinusTechTips`, Test feed (15 videos), Decline, the already-collected approve,
the header alert as sheet and popover, Escape, Unlock and Mark all seen, and no alert for a reader.
`admin-profiles.browser.cjs` passed again on the live build. Screenshots reviewed. The checks pressed Mark all seen,
which set the household's seen time; no real requests existed. Not checked: Add channel / Add source and approve in a
browser (they'd add a real feed or source; the .NET tests cover both in rolled-back transactions) and a real device.

## Admin profiles pages, profiles still off — deployed October 28, 2026 (Central)

Stage A1 of the household profiles plan: Admin · Profiles (`/admin/profiles`: add with name, color and PIN, lock
banners with Unlock and Reset PIN, picker order, hide, delete, sign out devices, household picker and forget-devices
settings), Manage profile (`/admin/profiles/{id}`) and the base edition editor (`/admin/profiles/base`), with
**Household → Profiles** in the admin menu. Every admin switch gained a 44px tap area; the TV series delete text now
says anyone following it will lose it. `Profiles:Enabled` stays `false`, so readers see no change. Commits `e08b87a`
and the services-search fix below.

Deployed `mynewspaper-web:profiles-admin-20261028` through Compose with `--pull`, healthy; rollback
`mynewsfeed-web:before-mynewspaper-profiles-admin-20261028` (`6897db98b04b`, the foundation release). The six app
settings all match the private override generated from the previous container; port 8085 unchanged; the image
environment is identical to the previous image's. The first A1 image (`6c763348a189`) showed all 338 streaming
services on the live base edition, making it 20,331px tall on a phone (the local run has no catalog key, so this
wasn't visible before). The editor now lists only chosen services plus a search showing up to 24 matches; rebuilt as
`0b3b7503c1b7` (3,457px at 390px) and redeployed. Predeploy: 920 .NET tests, Prettier clean. Live: `/`, `/tv`,
`/videos`, `/saved`, `/entertainment`, `/settings` 200; `/profiles` and `/profiles/pick` 404; `/admin/profiles*`
redirect to login; `/admin/api/profiles` 401. `admin-profiles.browser.cjs` passed on the final build at 320, 390, 430,
744, 820, 1024 and 1440px in light and dark (no sideways scroll, contrast, 44px targets, add with PIN checks, Reset
PIN, save bars, service search, phone cards and ⋯ menu, delete; its throwaway profile was deleted), with screenshots
reviewed. `admin-layout.browser.cjs` passed on 23 admin pages at the same widths and themes on the first A1 image.
Not checked: the lock banner with a real lock (PINs can't be tried while profiles are off) and a real device.

## Household profiles foundation, switched off — deployed October 28, 2026 (Central)

Stages F1 to F3 of the household profiles plan (`new development/profiles-handoff.md`, kept out of the repository),
behind `Profiles:Enabled`, which stays `false`, so readers see no change. Adds `db/020_profiles.sql` (applied to
sql2025 / WebScraper earlier on October 28 after a verified copy-only backup, and rerun cleanly), the profile gate,
PINs, settings validation, the viewer and follow filters on `TvQuery` and `VideosQuery`, per-profile Saved storage,
the request checker with its SSRF guard and the household channel rollout (both built, neither started), the
`/profiles/*` and `/api/me*` endpoints (404 while off), and admin APIs under `/admin/api` (admin sign-in required).
Commit `4798ad8`, on branch `profiles-foundation` (not yet merged into `develop`).

Deployed `mynewspaper-web:profiles-20261028` (`6897db98b04b`) through Compose, healthy; rollback
`mynewsfeed-web:before-mynewspaper-profiles-20261028` (`31950bbc9fa1`). The dev container had been rebuilt again, so the
Docker CLI and Compose were reinstalled and the private override was regenerated from the live container's own settings:
six app settings identical to live; port 8085, dev-bridge, `mynewsfeed_mynewsfeed-keys`, restart policy and health
check matched. A first build (`530ef8408519`) used a stale cached base image (.NET 10.0.12 instead of 10.0.14) and ran
briefly; it was rebuilt with `--pull` and replaced. The final image's environment matches the previous release's.
Predeploy: solution build with no warnings, 920 .NET tests including the new live-database and test-host checks (the
no-profile route sweep with the switch on, switch-off 404s, antiforgery, PIN lock and reset, the privacy test, admin
API privacy, rollout and per-profile Saved); the test rows were confirmed gone afterwards. No JavaScript or styles
changed, so lint and the browser checks weren't rerun. Live: /healthz and 19 public routes returned 200 (the /results
and /movies/digital redirects unchanged); /editions, /api/me, POST /profiles/1/open and POST /api/me/requests returned
404; /admin redirected to login; /admin/api/* returned 401 without the sign-in; no device cookie was set. The previous
image, run alongside without a port and with result syncing off, served byte-identical /tv, /videos, /saved,
/entertainment and /settings, and the same Saved list. Home and /sports differed only in the host name, the refresh
countdown and encrypted component state. Logs showed no errors. The scraper's hourly runs since `020` collected 269 of
270 sources, with YouTube and TV stages succeeding; the one failure is ESPN WNBA standings ("mixed standings seasons"),
unrelated. Mobile checks don't apply: nothing rendered changed (the compared pages above are identical). Real device not
checked. The admin password is still the `CHANGE_ME` placeholder.

## Homepage Live now: "@" between away and home — deployed October 28, 2026 (Central)

The desktop masthead's Live now box read "Boston Bruins 3, Carolina Hurricanes 1", which doesn't say which team is
home. It now reads away @ home: "Boston Bruins 3 @ Carolina Hurricanes 1" while live, and "Kings @ Rangers" before the
start (previously "at"). Neutral-site games keep "vs", since neither team is home. The line moved into
`Home.LiveLine` with four unit tests. Commit `90078a7`.

Deployed `mynewspaper-web:liveat-20261028` (`31950bbc9fa1`) through Compose, healthy; rollback
`mynewsfeed-web:before-mynewspaper-liveat-20261028` (`3da6a580017e`). Compose settings regenerated from and matched the
live container. Predeploy: solution build with no warnings, 739 .NET tests, lint clean (format:check flags only the
earlier `tv-series-delete.browser.cjs`); home-columns and tablet-layout passed; the box at 1440 light and 820 dark
reviewed. Live: /healthz and 12 public routes returned 200, unauthenticated /admin redirected to login; the live box
read "Columbus Blue Jackets 3 @ Philadelphia Flyers 1", "Milwaukee Brewers 1 @ Tampa Bay Rays 1" and "Boston Bruins 1 @
Carolina Hurricanes 1"; home-columns, home-scoreboard-sport and home-top25 passed; the box at 820/1024/1440 had no
overflow and wraps long names cleanly. The box shows only from 744px, so phones are unaffected. Real device not checked.

## Scoreboard Sport dropdown limited to the homepage — deployed October 28, 2026 (Central)

Fixes a regression from the previous release, reported from a phone: the Sports page's "Today's scores" box shares the
compact scoreboard component, so it also showed the new Sport dropdown, unstyled (running to the edge, with its focus
outline overlapping the game cards) and without the parameter that keeps a choice across refresh. The previous release
was verified on the homepage only. The box now shows the filter only when the page opts in (`SportFilter`); the
homepage does, and the five-minute refresh sends `sportFilter=true` only from a panel that has it. The Sports page is
back to its earlier layout. Commit `f71a7a0`.

Deployed `mynewspaper-web:sportsfix-20261028` (`3da6a580017e`) through Compose, healthy; rollback
`mynewsfeed-web:before-mynewspaper-sportsfix-20261028` (`ba61e366cef2`). Compose settings regenerated from and matched
the live container. Predeploy: solution build with no warnings, 735 .NET tests, lint clean (format:check flags only the
earlier `tv-series-delete.browser.cjs`); home-scoreboard-sport now also asserts /sports has no filter, ignores
`?scoreboardShow=` and stays unfiltered after its own refresh (it failed against the previous live build and passed on
the fix); scoreboard, scoreboard-consolidation, scoreboard-mobile, scoreboard-show, home-top25 and home-columns passed;
/sports at 320/390/1440 light/dark showed no filter and no overflow. One local home-top25 run timed out loading the page
on the dev server; three reruns passed. Live: /healthz, 13 routes including /sports returned 200, unauthenticated
/admin redirected to login; the dropdown renders on / and not on /sports; home-scoreboard-sport, all four scoreboard
checks, home-top25 and home-columns passed; /sports at 320/390/430 light/dark showed no filter and no overflow;
screenshots reviewed. Real device not rechecked.

## Homepage scoreboard Sport dropdown — deployed October 27, 2026 (Central)

The homepage scoreboard box gets a Sport dropdown at every width (no pills): All sports, then only the leagues with games
today (leagues only, no conferences or teams). It is hidden when fewer than two leagues have games. Choosing a league
swaps the box in place (no reload, scroll kept), shows up to six of that league's games, points "See all games" at the
full scoreboard filtered to it, and is kept in the address (`?scoreboardShow=c:{sport}|{league}`) for bookmarks,
refresh and the five-minute auto-refresh; unknown values fall back to All sports. It is independent of profiles and of
the "only my teams" setting (which filters only Today & Tonight). Without JavaScript, the dropdown and Apply load the
filtered page. The full `/scoreboard` page is unchanged. Deployed from the working tree, then committed as `f29b393`.

Deployed `mynewspaper-web:scoreboardsport-20261027` (`ba61e366cef2`) through Compose, healthy; rollback
`mynewsfeed-web:before-mynewspaper-scoreboardsport-20261027` (`130057f4f448`). Compose settings regenerated from and
matched the live container. Predeploy: solution build with no warnings, 735 .NET tests, lint clean (format:check flags
only the earlier `tv-series-delete.browser.cjs`); new home-scoreboard-sport check (MLB and NHL that day) and
home-columns, home-opinion, home-top25, home-watch-search, personal-features, personal-layout, scoreboard,
scoreboard-consolidation, scoreboard-mobile, scoreboard-show and tablet-layout passed; screenshots at 320/390/430/820/
1024/1440 light/dark reviewed (dropdown at least 44px, inside the box, no overflow). home-top25 needed a one-line fix
for the page's second dropdown. Live: /healthz, 13 public routes and fragments returned 200, unauthenticated /admin
redirected to login; home-scoreboard-sport, home-columns, personal-layout and all four scoreboard checks passed.
**Open:** home-top25 failed intermittently against the live build (about 4 of 80 runs, always on the first pill's
scroll check within two seconds of load). It never reproduced in 45 instrumented runs or 24 further runs, and pill
clicks never moved the card in direct measurement. The test now waits for the page to settle, also checks the card's
on-screen position, and reports the values, so a recurrence will show whether anything visible moved. Real device not
checked.

## Homepage Top 25 sport pills on desktop — deployed October 27, 2026 (Central)

At 1024px and wider the homepage Top 25 card shows one pill per sport instead of the "Sport and poll" dropdown. Pills use
the source's short sport name (today Football, Soccer, Volleyball; Men's and Women's Basketball appear when the card
lists them), falling back to the full poll name when two polls share a sport. Phones and iPad portrait (below 1024px)
keep the dropdown, at the user's request. Pill clicks swap the card in place like the dropdown; without JavaScript they
are plain links. The pills are slightly tighter than page pills so three fit one row in the 256px column at 1024px.
Commit `2b64eb2`.

Deployed `mynewspaper-web:toppills-20261027` (`130057f4f448`) through Compose, healthy; rollback
`mynewsfeed-web:before-mynewspaper-toppills-20261027` (`a917e2807bd8`). Compose settings regenerated from and matched the
live container. Predeploy: solution build with no warnings, 735 .NET tests (new pill-label test), lint clean
(format:check flags only the earlier `tv-series-delete.browser.cjs`); home-top25 (pills at 1440, dropdown at 390 and
820, both switching in place, no-JS pill links), home-opinion, home-columns, home-watch-search, personal-features and
personal-layout passed; at 320/390/430/820/1024/1440 light/dark: pills only at 1024 and 1440, one row, 44px tall, page
kept its scroll position on a pill click, no overflow or script errors. Live: /healthz, 12 public routes and the
homepage fragments returned 200 (/results and /movies/digital after their usual redirects), unauthenticated /admin
redirected to login; home-top25, home-opinion, home-columns and personal-layout passed; the same six-width light/dark
sweep passed; screenshots reviewed. Noted, not changed: at 1024px the existing Top 25 table breaks long school names
mid-word ("Nebr aska"), as it did before this release. Real device not checked.

## Homepage Top 25 poll switch in place — deployed October 27, 2026 (Central)

Choosing a poll in the homepage Top 25 card used to submit its GET form and reload the whole page, jumping back to the
top. A new `/home/rankings?rankingSourceId=` fragment endpoint returns only the card, and `home-rankings.js` swaps it in
place, updates the address with `replaceState` (bookmarks and refresh keep the poll) and keeps focus on the picker. The
GET form stays as the no-JavaScript and failure fallback. The back button no longer steps through polls. Commit `00b5b9c`.

Deployed `mynewspaper-web:top25-20261027` (`a917e2807bd8`) through Compose, healthy; rollback
`mynewsfeed-web:before-mynewspaper-top25-20261027` (`b9f58d6f8006`). The previous session's private Compose override was
gone, so it was regenerated from the live container's own settings (nine app settings identical to live; port 8085,
dev-bridge, `mynewsfeed_mynewsfeed-keys`, restart policy and health check matched). Predeploy: solution build with no
warnings, 734 .NET tests, lint clean (format:check flags only the earlier `tv-series-delete.browser.cjs`); home-top25
(now asserting no reload and unchanged scroll), home-opinion, home-columns, home-watch-search, personal-features and
personal-layout passed; at 320/390/430 light/dark the poll switch kept the exact scroll position with no overflow or
script errors, and with the new script blocked the old reload-to-top reproduced (3300px to 0). Live: /healthz and 12
public routes plus the three homepage fragments returned 200, unauthenticated /admin redirected to login; home-top25,
home-opinion and home-columns passed; the 320/390/430 light/dark switch check passed; screenshots reviewed (team logos
finish loading a moment after the swap, as on a full load); logs showed only the usual startup warnings. Tablet and
desktop were covered by home-top25's width sweep only; real device not checked.

## TV series Delete — deployed October 27, 2026 (Central)

Admin TV series rows get a ⋯ menu with Delete… and a confirm panel (Stop following instead / Delete series / Cancel);
Delete removes only the tracking row, keeping series and episode history, and skips a row the scraper has leased.
Shared admin fixes: 44px ⋯ targets, menus close on choice/outside click/Escape, scrolling tables contain hidden labels
and make room for a bottom-row menu. Details in docs/admin-redesign-handoff.md. Also includes the Categories tidy-up
(`fa03f75`), which was already live as `mynewspaper-web:admincats-20261026` (`27612cfd928c`, unrecorded until now).

Deployed `mynewspaper-web:tvdelete-20261027` (`b9f58d6f8006`) through Compose, healthy; rollback
`mynewsfeed-web:before-mynewspaper-tvdelete-20261027` (`27612cfd928c`). The dev container had been rebuilt, so the
Docker CLI/Compose were reinstalled and the lost `/tmp` override was regenerated from the live container's own settings
(nine app settings identical to live; port 8085, dev-bridge, `mynewsfeed_mynewsfeed-keys`, restart policy and health check
matched). Predeploy: 734 .NET tests; tv-series-delete at 320/390/430/744/1024/1440 light/dark, admin-layout (all widths)
and section-hubs passed; mixed-search-nav failed only on Where to watch title search (no provider key locally). Live:
/healthz and 13 public routes, admin sign-in redirect, tv-series-delete at all six widths light/dark, admin-layout at
390/1440 light/dark passed; screenshots reviewed; logs showed only the usual HTTPS-port warning. The checks deleted only
their own throwaway series. Real device not checked.

## Admin redesign, phases 1–4 — deployed October 26, 2026 (Central)

From `docs/admin-redesign-handoff.md` (approved Admin mockups on the canvas). The admin area moves from the Blazor
template to myNewspaper's look: `admin.css` scoped to `.adm` (light and dark, phones below 900px), a new sidebar menu
with icons and red problem counts, Scraper status (health line, tiles, Needs attention, Catch-up tasks), Sources (search,
category, status filter with counts, filters in the address, switches, ⋯ menu, Disable-instead confirm, phone cards),
the source editor (save bar naming changed fields, health and Remove panels), and every other admin page in the new
header and error boundary. The handoff's implementation status section lists what is done and the next batch.
Found while checking: the yellow Admin tag on the white sign-in card (1.43:1) and Bootstrap's pink inline code in dark
mode (3.73:1) were unreadable; both fixed.

Deployed `mynewspaper-web:admin-20261026` (`3d007990c29e`), healthy; rollback
`mynewsfeed-web:before-mynewspaper-admin-20261026`. 733 .NET tests (new AdminNavCounts test). Preview:
admin-layout at all seven widths light/dark (21 pages), frontpages, branding, home-columns, compact-stories,
settings-order, theme-mode, tablet-layout on four public pages and contrast for three schemes on / and /settings
passed (public site unchanged). Live: admin-layout at 390 and 1440 light/dark, frontpages and branding passed. Nothing
was saved or deleted by the checks. Real device not checked.

## Share buttons after in-site navigation — deployed October 26, 2026 (Central)

The user reported share icons appearing only after a refresh. Cause: share buttons are rendered `hidden` and revealed
by public.js; between pages with the same layout (reproduced Entertainment headlines → Headlines: 13 of 13 hidden),
Blazor's enhanced navigation updates the existing markup in place, restoring `hidden`. No nodes are added, so the
MutationObserver that reveals new buttons never fired. public.js now also re-syncs share buttons, save states and theme
toggles on Blazor's `enhancedload`. entertainment-news gained a check for that navigation (failed on the old live
build, passes now).

Deployed `mynewspaper-web:share-20261026` (`2d3a6f80ab61`), healthy; rollback
`mynewsfeed-web:before-mynewspaper-share-20261026`. Preview: a ten-step walk through News, Sports, Entertainment, Home,
YouTube and Saved left no share button hidden; entertainment-news, video-share, shared-saved and theme-mode passed.
Live: entertainment-news and video-share passed.

## Saved page tidy-up — deployed October 26, 2026 (Central)

From the user's screenshot. Both Saved lists lay cards out with `repeat(auto-fill, minmax(min(240px, 100%), 1fr))`
(the site's fixed four-across story grid had squeezed the Videos column to about 120px, stacking "Bulwark" a letter
per line). The status line clears once synced (the heading already explains sharing) and hides when empty. Tab
counts keep their space ("Everything · 2", a 0.3em gap; flex dropped the leading space). The placeholder mark hides
whenever a picture is present (`:has(> img)`), so transparent PNGs no longer show it; it returns if the picture fails.
watch-later now picks a visible unsaved video (a saved featured video had moved its first match into a hidden slide).

Deployed `mynewspaper-web:saved-20261026` (`22fa89151c9c`), healthy; rollback
`mynewsfeed-web:before-mynewspaper-saved-20261026`. Preview: Saved at 1920/1440/820/390 (no overflow, no mark over
pictures), saved-sections (caught a 768px overflow, fixed with `min(240px, 100%)`), shared-saved, watch-later,
video-share, compact-stories and tablet-layout on /saved passed. Live: saved-sections and video-share passed.

## YouTube channel categories — deployed October 26, 2026 (Central)

From the scraper's frontend handoff (`dbo.YoutubeCategories`, `YoutubeVideoFeed.YoutubeCategoryId`, separate from
news `SourceCategories`). Model: `YoutubeCategory` entity and the nullable FK. `VideosQuery`: `GetCategoriesAsync`
(categories with at least one video, by name, then `Other` while any video has no category or no channel row),
`Slug` ("News & Politics" → `news-politics`), and an optional category on `GetVideosAsync`/`GetChannelsAsync`;
`VideoItem.Category`. YouTube page: pills on desktop (`.yt-cats`, as on the headline pages) and a Category dropdown on
phones; `videos?category=<slug>`; unknown slugs show All; a channel from another category is dropped. The featured
carousel, Latest videos, channel list and search stay in the category; a single featured video renders no controls
(youtube.js skips carousels without them). In All the kicker reads "Category · Channel". Admin: the channel editor has
a Category dropdown (writes `YoutubeCategoryId`; agreed with the user, a change from the handoff's read-only note) and
the feed list shows a Category column ("Other" when unset). Categories are still created by the scraper. The site's
SQL login is `sa`, so no extra grant was needed; the user considers this acceptable for an in-network site.

Deployed `mynewspaper-web:ytcats-20261026` (`7ae34ff5099d`), healthy; rollback
`mynewsfeed-web:before-mynewspaper-ytcats-20261026`. 732 .NET tests (new: slugs; tabs filter videos and channels,
Other keeps uncategorized and feed-less videos, empty categories hidden). New `youtube-categories.browser.cjs`:
4 tabs (News & Politics, Sports, Tech, Video Games) sum to All (320), per-tab channels, kickers, carousel controls,
phone dropdown, 44px controls, 320–1440px. Found and fixed before release: a `Featured@if` Razor slip printed code into
the heading (320px overflow, caught by frontpages) and the carousel script errored on one-video carousels. Preview:
frontpages, video-share, watch-later, tablet-layout and contrast for 16 schemes on /videos passed; admin list and
editor checked without saving. Live: youtube-categories, frontpages and video-share passed.

## F1 › News retired — deployed October 26, 2026 (Central)

At the user's request: the F1 page's News tab duplicated Sports headlines › F1 (same "F1 News" category, sources and
articles), so `F1News.razor`, the `f1-news` Load more view, `F1NewsQuery.GetAsync`/`CategoryIdAsync`/`F1NewsPage` and
`F1NewsTests` were removed (`F1NewsQuery.CategoryName` stays). `/f1/news` redirects (302) to `/sports/news/f1`,
keeping a numeric `source`. The F1 tabs are Schedule, Results and Standings; the next-race banner links to "F1
headlines →"; the homepage F1 card's News link now says Headlines and points to `/sports/news/f1`. Banner links gained
a 4px gap so the arrow keeps its space.

Deployed `mynewspaper-web:f1news-20261026` (`20728419f2f7`), healthy; rollback
`mynewsfeed-web:before-mynewspaper-f1news-20261026`. 727 .NET tests passed (run the test host directly:
`dotnet tests/myNewspaper.Tests/bin/Debug/net10.0/myNewspaper.Tests.dll`; `dotnet test` reported "Zero tests ran").
Preview: redirect (with and without source), sports-news (now checks the redirect and that /f1 has no f1/news link),
section-select, section-hubs and tablet-layout on /f1 and /sports/news/f1 passed. Live: redirect, sports-news and
section-select passed.

## F1 News on the Headlines layout; picture cards on phones — deployed October 26, 2026 (Central)

At the user's request: desktop F1 › News (`/f1/news`) renders through `NewspaperFeed` inside `news-front`, like the
Headlines pages (lead with Latest headlines, support stories, More stories rows). Phones: Headlines, Sports and
Entertainment headlines (all topics) and Local's Kansas City news show every story as the standard article card F1
News used (picture on top, category, serif title, date, summary, publisher and actions). How: the newspaper feed's
base rules and its publisher-line rules in public.css now sit in `@media (min-width: 744px)`; the phone-only compact
rules (public.css ≤699px blocks, sections.css phone Headlines and Local blocks, an unused `news-front--hub` block)
were removed; a sections.css ≤743px block sets the groupings to `display: contents`, hides the section labels, gives
card category links a 44px tap area without moving them, and drops Local's outer news box. A computed-style
comparison found no difference from the F1 card on any story kind on all four pages. compact-stories,
publisher-layout and the Load more check were updated to expect picture cards.

Deployed `mynewspaper-web:cards-20261026` (`0b74fa9d5b5f`), healthy; rollback
`mynewsfeed-web:before-mynewspaper-cards-20261026`. Preview: compact-stories, frontpages, publisher-layout,
entertainment-news, sports-news, section-hubs, watch-later, shared-saved, tablet-layout (5 pages) and contrast for 16
schemes on /news, /local and /f1/news passed. Live: compact-stories, frontpages and publisher-layout passed. Real
device not re-checked.

## "Headlines" and "Competitions" labels — deployed October 26, 2026 (Central)

At the user's request the Sports and Entertainment section tabs and the phone menu's Sports and Entertainment groups
say "Headlines" (was "All headlines", "Sports headlines", "Entertainment headlines"). The events page is now
"Competitions": page title, heading, phone menu link and its Sports tab (was "Schedule"). Unchanged: the headline
pages' own headings, the "All sports/entertainment headlines →" links on the fronts, the Sports front's
"Schedule →" link beside Upcoming games and the admin menu. entertainment-news and sports-news now choose TV/Film and
F1 by name (the new Box Office topic follows TV/Film).

Deployed `mynewspaper-web:labels-20261026` (`cef2c981f245`), healthy; rollback
`mynewsfeed-web:before-mynewspaper-labels-20261026`. Preview: sports-news, entertainment-news, section-hubs,
section-select, scoreboard-consolidation, mixed-search-nav and branding passed. Live: sports-news, entertainment-news
and section-select passed.

## Headline categories tidied (data only) — October 26, 2026 (Central)

From a review of each category's sources against its headlines, approved by the user; made in the admin pages, no
deployment. Sports › Big 12: the USA TODAY Google News source (9420) searched any sports story mentioning "Big 12" and
mostly returned national college football; it now requires "Big 12" in the title
(`site:www.usatoday.com/story/sports/ intitle:"Big 12" when:30d`). A version listing team names was tried and dropped:
Google ignored `when:30d` with it and returned stories from 2024. Entertainment: new **Box Office** category
(Entertainment hub, id 6443) holding the Deadline, IndieWire, THR and Variety box-office feeds (7094, 9036, 9035, 9034),
so General is Nerdist and USA TODAY Entertainment. Headlines: **More News** renamed **U.S. News** (2627); new
**Business** category (Headlines, id 6444) holding USA TODAY Money (9413). Technology: 9to5 Toys (9951, all deals)
disabled, which hides its stories. Opinion was left mixed on purpose (the user likes the variety). Note:
`BoxOfficeQuery` (stories for a category named "Box Office") is not used by any page since the Box office redesign.
Verified by reading each source back in admin and on the public category menus and pages.

## Story placeholders and support stories on tablets — deployed October 26, 2026 (Central)

At the user's request the story placeholder (a fixed light-blue `img/branded-article-v2.svg` with the old logo) is
replaced: `.thumb`, the home lead image and the Opinion card thumb show the current page mark (`img/placeholder-mark.svg`,
the "n" and fold cut out) as a CSS mask in `--ph-fg` at 45% on `--ph-bg`, so it follows every scheme in light and dark.
The old SVGs (`branded-article-v1/v2`, `default-article`) are no longer referenced but were left in place. Found while
checking: the three stories under the Headlines lead (also Sports and Entertainment headlines) put a 96px thumbnail
beside the title in a third of the page, leaving titles a word or two wide on iPads; from 744 to 1279px the picture
now sits above the title (Entertainment's 72px posters too).

Deployed `mynewspaper-web:placeholder-20261026` (`6dfa28d14b5f`), healthy; rollback
`mynewsfeed-web:before-mynewspaper-placeholder-20261026`. Preview (pictures blocked to show placeholders): Ribbon light,
Halloween dark and Coastal light reviewed; compact-stories, frontpages, entertainment-news, sports-news, section-hubs,
home-opinion, home-columns, publisher-layout and tablet-layout (/, /news, /sports/news, /entertainment/news) passed.
Live: placeholder-mark.svg served; compact-stories and frontpages passed. Real device not re-checked.

## Forecast and Where to watch swapped on the home masthead — deployed October 26, 2026 (Central)

At the user's request, from 744px the Kansas City forecast (with New tonight) is the masthead's left ear, outlined like
Coming up with the periods two by two; the ears match in height. Where to watch is a full-width band in the scheme's
button colour under the masthead, title beside a search that fills the band. Markup moved (the forecast section into
the masthead, Watch after it); phones keep Watch first then the forecast through CSS order and look unchanged.
home-columns and home-watch-search follow the new positions.

Deployed `mynewspaper-web:swap-20261026` (`ae76fe97094d`), healthy; rollback
`mynewsfeed-web:before-mynewspaper-swap-20261026`. Preview: home-columns, home-watch-search, home-opinion, home-top25,
frontpages, branding, weather-icons, tablet-layout on / and contrast for all 16 schemes on / passed; screens at 1440
(Olive light, Ribbon dark), 820 and 390 reviewed. Live: home-columns, home-watch-search and tablet-layout on / passed.
Real device not re-checked.

## iPad portrait gets the desktop page layout — deployed October 26, 2026 (Central)

From the user's plan (`ipad-portrait-desktop-layout.md`), reviewed and amended. Page-layout media queries in
public.css, sections.css and broadsheet.css moved from 1024/1023px to 744/743px, and public.css's and
broadsheet.css's 768/767 splits moved to 744/743 too, so iPad mini (744) is not half phone, half desktop. The header
keeps the phone bar and Menu sheet below 1024px (broadsheet.css 1374/1594/1722/1745/1752 and public.css 903/2524
stay at 1024): seven section links and the actions do not fit one row on an iPad. New `tablet.css` (744–1023,
loaded last) tightens the desktop design: 24px page padding; home masthead title across the top with Where to watch
and Coming up beneath; weather title above its periods with New tonight below; front columns main + side with Today &
Tonight below; "More from today" side lists side by side under the headlines; Sports front lead + standings with
upcoming below (standings records nowrap); slimmer side columns on section fronts; YouTube Latest videos in two
columns under the featured video and More videos three across; Local forecast temperature 38px. The plan's
`(pointer: coarse)` 44px rule for all buttons was left out (it would also change phones). The service worker needed
no change (it caches only the offline page and icons; stylesheets are fingerprinted).

Deployed `mynewspaper-web:ipad-20261026` (`77fa07e55a61`), healthy; rollback
`mynewsfeed-web:before-mynewspaper-ipad-20261026`. New check `tablet-layout.browser.cjs`: 17 pages at
430/743/744/768/820/834/1023/1024/1440 light/dark — no sideways scroll, no script errors, desktop columns from 744,
Menu bar below 1024 — passed on preview and live. Nine checks updated from a 1024 (or 768) desktop threshold to 744;
home-columns gained a tablet branch. Preview: the suite up to scheme-contrast ran (the run was stopped to deploy at
the user's request); five threshold failures were fixed and passed. Live: the remaining 25 checks passed after
scoreboard-mobile's threshold update. Screens reviewed at 744 and 820. Real iPad not checked.

## Coastal, Olive Grove and Golden Hour schemes — deployed October 26, 2026 (Central)

Three schemes chosen for header colours at least about 15 CIEDE2000 from every existing header: Coastal (`coastal`,
ocean cyan `#0e7490`, coral highlight `#fb7185`), Olive Grove (`olive`, `#5f6b2a`, butter cream `#fde68a`) and Golden
Hour (`golden-hour`, ochre `#854d0e`, lavender `#c4b5fd`). White text on headers, dark text on highlights, light and
dark variants. Added to public.css, theme-init.js, public.js, the Settings list and the contrast check (16 schemes).

Deployed `mynewspaper-web:newschemes-20261026` (`d49280da3892`), healthy; rollback
`mynewsfeed-web:before-mynewspaper-newschemes-20261026`. Preview: contrast passed for the three on all 17 pages,
light/dark, 390/1440; home at 320/390/430 light/dark applies the scheme with no overflow or script errors; choosing
Golden Hour in Settings persists across reload. Live: contrast on /, /settings and /watch passed and Settings lists
all three. Real device not re-checked.

## St. Louis CITY SC home kit colours — deployed October 26, 2026 (Central)

At the user's request the CITY scheme drops the Tina Turner gold for the regular kit: River Blue navy `#001837`
header, City Red `#dd004a` buttons, chips and marks, navy highlight (so the Search button stands out on the red
Where to watch panel), links `#b8003f` light / `#ff8fb1` dark. Kit hex values are approximations (the club does not
publish them). Navy leads because a red header with navy would nearly duplicate Cardinals. Renamed "St. Louis CITY
SC".

Deployed `mynewspaper-web:city-20261026` (`f77d0b3924b5`), healthy; rollback
`mynewsfeed-web:before-mynewspaper-city-20261026`. Preview: contrast passed for City on all 17 pages, light/dark,
390/1440; home at 320/390/430 light/dark has no overflow or script errors. Live: contrast on /, /settings and /watch
passed. Real device not re-checked.

## Desert Sunset and St. Patrick's Day colours — deployed October 26, 2026 (Central)

An overlap review (CIEDE2000 between header colours) found near-duplicates: Ribbon/Kansas (same blue), Ribbon/Kansas
vs Y2K, Monochrome vs City (near-black headers), Christmas vs St. Patrick's (dark greens), Halloween vs Desert (burnt
orange), and Desert's two rusts reading as one colour. The user chose to keep Kansas, Y2K and City as they are and
change two: Desert Sunset is now clay terracotta `#a45a52` (links `#8c463f`) with a sunset gold highlight `#f2b33d`
(dark text); St. Patrick's Day is kelly green `#15803d` (dark-mode accent `#86efac`). Swatches follow. The contrast
check gained `SCHEMES` to narrow a run.

Deployed `mynewspaper-web:schemes-20261026` (`3da3702d48ef`), healthy; rollback
`mynewsfeed-web:before-mynewspaper-schemes-20261026`. Preview: contrast passed for both schemes on all 17 pages,
light/dark, 390/1440; home at 320/390/430 light/dark has no overflow or script errors. Live: contrast on /, /settings
and /f1 and settings-order passed. Real device not re-checked.

## Reading preferences removed from Settings — deployed October 26, 2026 (Central)

The user doesn't use it. Removed the Settings section (hide read stories, preferred news category, muted publishers,
reset), its index link and Arrange entry, and the personal.js code behind it, so values saved earlier in a browser no
longer hide stories, show the Opinion "hidden by your reading preferences" note or redirect Headlines. Settings now
has four sections.

Deployed `mynewspaper-web:settings-20261026` (`88d7fde91cd9`), healthy; rollback
`mynewsfeed-web:before-mynewspaper-settings-20261026`. Preview: personal-features, home-opinion, branding and
settings-order (updated to four sections) passed; scheme contrast passed on /settings; Settings at 320/390/430
light/dark has no overflow or script errors. Live: settings-order, personal-features and home-opinion passed. Real
device not re-checked.

## Readable text under every colour scheme — deployed October 26, 2026 (Central)

The user found dark text on Halloween's purple Search button. New check `tests/browser/scheme-contrast.browser.cjs`
audits every visible text element against the colour actually behind it (WCAG AA: 4.5:1, or 3:1 for large text;
text over pictures and visually hidden labels skipped) for all 13 schemes, light and dark, at 390 and 1440px.
`PAGES` picks pages (default `/,/watch,/sports,/settings`); `REPORT=1` lists every failure. Live before the fix: 27
failures. Fixes: the Where to watch Search button uses `--selection-fg` (Ribbon now defines `#141b2b` for its gold);
the ear title and label use `--btn-fg` (dark on City's gold); Kansas dark `--pill-bg` `#344c70` → `#2c4264` (muted
toggle text and streaming badges); the F1 banner's gold kicker and pale-blue place line apply only on Ribbon, other
schemes use `--header-fg`.

Deployed `mynewspaper-web:contrast-20261026` (`59d3b6f825c9`), healthy; rollback
`mynewsfeed-web:before-mynewspaper-contrast-20261026`. Audit passed on preview for 17 pages (home, watch, sports,
settings, news, local, entertainment, movies, tv, box office, YouTube, saved, scoreboard, standings, rankings,
college, F1) and on live for home, watch, F1 and sports. Where to watch checked at 320/390/430 under Halloween light
and dark, City light and Kansas dark: readable, no horizontal overflow. Built in the light resource mode. Real device
not re-checked. Evidence: `/tmp/mynewspaper-broadsheet-deploy/b5`.

## YouTube v2 and section headlines — deployed October 26, 2026 (Central)

Built from boards the user added to the canvas: YouTube v2 replaces the earlier YouTube design (featured carousel of
five videos — most watched in the newest two days, one per channel — still until Play, seven-second rotation with a
progress bar, arrows and title tabs, pauses while hidden, announces changes only when not rotating; Latest videos
beside it; More videos grid; youtube.js; later pages keep the grid; no-JS shows the first video). No phone board:
on phones the tabs become bars and the controls stay 44px. Sports and Entertainment headlines follow their new boards
(masthead, Sport/Topic pills on desktop and dropdown on phones, poster thumbnails for Entertainment, overview link
dropped). Thumbnails are now built from the video id first (stored hq720/custom addresses could show YouTube's grey
placeholder), which also fixes grey boxes elsewhere on the site.

Deployed `mynewspaper-web:youtube-20261026` (`9ba81c595479`), healthy; rollback
`mynewsfeed-web:before-mynewspaper-youtube-20261026`. 729 .NET tests. Preview suite: four checks expected the old
headline layouts (updated) and then passed. Carousel exercised at 1440 and 390 (next, pick, play advancing at 7s,
pause holding, 44px controls; no-JS one slide). Four-page matrix 317–1440 light/dark: 72 views clean on preview
and live; six related checks passed live; live featured image loaded (480×360). Built in the light resource mode.
Real device not re-checked. Follow-up (same tag, rollback
`mynewsfeed-web:before-mynewspaper-youtube2-20261026`): the homepage Coming up ear reads "Away at Home" for games
not yet started instead of the live-score comma format; home checks passed on preview and the live text was
confirmed. Evidence: `/tmp/mynewspaper-broadsheet-deploy/b5`.

## Headlines, Local, YouTube, Saved and Settings to concept boards — deployed October 25, 2026 (Central)

Batch 4, the last of the concept rebuild. Headlines (category tabs on desktop, compact Latest column, More stories as
small-thumbnail rows, lead card with picture on phones; section headline pages share it), Local (Overview/News/
Weather/Events tabs, large-temperature forecast, news beside Find something to do), YouTube (renamed from Videos;
one even grid, no oversized first video), Saved (Everything/Articles/Videos tabs with counts and a ?list= filter,
lists side by side, browse links in empty lists) and Settings (section index, card sections, two-across choice
buttons, each My Teams league collapsible and opened by search; phone page 14,761px → 3,753px).

First batch in the light resource mode after the October 25 out-of-memory event: no local app, one memory-capped image
build at a time (1.5 GB, 2 GB with swap), browser checks one at a time with no headless Chrome left running.
Deployed `mynewspaper-web:pages-20261026` (`e67bcdcb0ce2`), healthy; rollback image
`mynewsfeed-web:before-mynewspaper-pages-20261026`; runtime configuration preserved. 726 .NET tests; lint and format
clean. Preview suite: 40 passed, three failures were checks expecting old layouts (updated), and the matrix found
Saved overflowing at 317/320px (status line did not wrap; fixed). Five-page matrix at 317–1440, light/dark,
JavaScript off at 390/1440: 90 views clean on preview and live; six related checks passed live. Proposed extras
still open: YouTube topic chips (needs a channel-to-topic mapping) and grouped duplicate stories. Real device not
re-checked. A follow-up the same night lined up the Saved Articles and Videos headings on desktop (same tag,
image `2d2ac46bc5b3`; rollback `mynewsfeed-web:before-mynewspaper-pages2-20261026`). Evidence:
`/tmp/mynewspaper-broadsheet-deploy/b4`.

## Sports pages to concept boards — deployed October 25, 2026 (Central)

Batch 3 of the concept rebuild. Sports (new SportsFront: score cards, upcoming games agate, lead and headlines, Big 12
football standings and AP Top 5), Scoreboard (games grouped by competition as agate rows with one status line; pager
"← Sat / date / Mon →"; script hooks unchanged), Standings (league pills, ruled tables), Top 25 (poll pills, two
columns of agate rows with coloured movement; College rankings share it), College Sports (sport pills, ruled day
labels, agate schedule rows, Big 12 poll and conference-leader side column) and F1 (next race banner, upcoming rounds,
drivers' championship). SectionHub now only routes to the two section fronts. Kept from before: the scoreboard's
All/Final dropdown (the board's Live filter has no data behind it) and College phone rows with venue and links.

Deployed `mynewspaper-web:sports-20261026` (`c9d390ff1a3e`), healthy; rollback image
`mynewsfeed-web:before-mynewspaper-sports-20261026`; runtime configuration preserved. 726 .NET tests; lint and
format clean. The preview suite's six failures were checks that expected the old Sports markup (updated: pager by
class, one-row phone pager, hub and compact-story checks, share focus on the lead) and all passed on the rebuilt
preview. Seven-view matrix (Sports, Scoreboard, Standings, Top 25, College, College rankings, F1) at
317/320/390/430/768/1024/1440, light/dark, JavaScript off at 390/1440: 126 views without overflow or script errors.
A VS Code crash interrupted the session after the batch commit; the pending fixes were intact on disk and were
committed before deployment. Live: eight Sports checks passed, the same 126-view matrix was clean and the
application log showed no errors. Real device not re-checked. Evidence: `/tmp/mynewspaper-broadsheet-deploy/b3`.

## Entertainment pages to concept boards — deployed October 25, 2026 (Central)

Batch 2 of the concept rebuild. Entertainment (new EntertainmentFront: On screen, lead, headline grid, box office /
Tonight on TV / Friday in theaters), Movies (Sunday–Saturday release calendar with week paging and Looking ahead),
TV & Streaming (week of episodes by day, tracked series with next air day), Digital releases (source pills and an
empty state offering other sources), Box office (view pills; agate results table with Predicted and New) and the
Where to watch frame (blue search panel, white-on-grey browse toggle; rows and results unchanged). Styles live in
`sections.css`. Sports keeps its hub until batch 3; the two box office forecast views have no board and keep their
cards.

Deployed `mynewspaper-web:entertainment-20261026` (`bbd88ca31a96`), healthy; rollback image
`mynewsfeed-web:before-mynewspaper-entertainment-20261026`; runtime configuration preserved. 726 .NET tests; lint
and format clean. Preview suite: failures were the Where to watch overflow at 320px (fixed: border-box panel), old
hub/compact-story expectations (updated) and intermittent home load timeouts (passed on rerun). Seven-page matrix at
317 (large-text stand-in)/320/390/430/768/1024/1440, light/dark, JavaScript off at 390/1440: 126 views without
overflow or script errors on preview and live; nine Entertainment and Where to watch checks passed live. Real device
not re-checked. Evidence: `/tmp/mynewspaper-broadsheet-deploy/b2`.

## Broadsheet follow-ups and concept header — deployed October 25, 2026 (Central)

After the first broadsheet release the user compared the live site with the canvas and with their Android phone
(412px, large system text, installed app). Four further releases, each verified on a preview and then live:

| Image | Change |
| --- | --- |
| `broadsheet-v2-20261025` (`f092f65d0a3b`) | Desktop front page matched to option 4: agate Today & Tonight by part of day, headline-first lead, two-column headlines, text-only Opinion, top-3 box office, sports briefs in the side column |
| `broadsheet-v3-20261025` (`c99c61549b63`) | "More from today" in ruled-column style: agate scoreboard, column-edge alignment, Top 25 fits, empty release sections left out |
| `broadsheet-v4-20261025` (`6328de622c50`) | Phone top: greeting and Refresh on one row, compact Where to watch, 2 × 2 forecast under 600px (four across on tablets) |
| `header-20261025` (`b2bc2db84360`) | Header rebuilt to the concepts: phone bar with date and time, Saved and a Menu button opening a full-height section sheet (no "Browse" dropdown); desktop plain masthead row of text links (no dropdowns) with Saved, Settings, light/dark and Admin |

Each rollback image is `mynewsfeed-web:before-mynewspaper-<tag>`; runtime environment, port 8085, `dev-bridge`,
`/keys` and restart policy were preserved. 721 .NET tests passed; lint and format clean. The browser suite on each
preview passed apart from intermittent `page.goto` load timeouts on home checks (external images; all passed on
rerun). Screen matrix (7 pages at 320/390/430/768/1440, light/dark, JavaScript off at 390/1440): no overflow or
script errors on preview or live. Large system text was approximated at 317px. The phone fixes came from the
user's Android screenshots; the device was not re-checked after the header release. Section pages other than Home
still use their old layouts; rebuilding them to their concept boards is planned in batches.
Evidence: `/tmp/mynewspaper-broadsheet-deploy/v2` to `v5`.

## Broadsheet redesign — deployed October 25, 2026 (Central)

User approved the redesign concepts (canvas, Oct 25) and asked to build and deploy. Ships the
folded-page logo and broadsheet design system, the broadsheet homepage, broadsheet section pages,
box office Predicted ranges and TV batch drops (commits `89937fb`, `33f687a`, `c8f8704`), plus two
fixes found in pre-release review: phone/tablet Today & Tonight rows are compact with 44px targets
(the network badge is the stream link; records hidden under 768px) (`ac6f06a`), and the desktop
side column lists digital/theater releases with thumbnails instead of a squeezed poster grid (`07e2a5d`).

Deployed `mynewspaper-web:broadsheet-20261025` (`14f6a33cdfd8`), healthy; rollback image
`mynewsfeed-web:before-mynewspaper-broadsheet-20261025` (= code-standards) and previous container
retained; runtime environment, port 8085, `dev-bridge`, `/keys` volume and restart policy preserved.
No database changes.

Checks: build 0 warnings; 720 .NET tests passed; ESLint and Prettier clean. Preview: 26 routes as
expected, zero error log lines; browser suite 43 passed, 4 skipped (pwa and location-weather need a
secure context; two data/opt-in skips). Matrix of 7 pages (home, sports, entertainment, box office,
TV, watch, news) at 320/390/430/768/1440px, light/dark, JS off at 390/1440: no horizontal overflow
or script errors on preview or live; screenshots reviewed. Live: routes 200, zero error log lines;
browser suite 40 passed with home-top25, home-watch-search and scoreboard-mobile timing out on
`page.goto` (load event); all three passed on rerun and live home loads measured 0.3–0.5s, so these
are recorded as intermittent external-asset stalls in this runner. Real device and Tailscale HTTPS
not checked. Evidence: `/tmp/mynewspaper-broadsheet-deploy/`.

## Code standards cleanup — deployed October 25, 2026 (Central)

User asked for a review against modern .NET/Blazor/JavaScript standards, approved the
proposed phases, invited better approaches and authorized deployment. Scope and what was
deliberately left are in [maintenance](maintenance.md#code-standards-review--october-25-2026).
One visible change: Home renders the lead before Where to watch, so desktop without
JavaScript shows the lead full width first (previously Watch, with the lead half-width).

Deployed `mynewspaper-web:code-standards-20261025` (`a389d7441916`), healthy; rollback image
`mynewsfeed-web:before-mynewspaper-code-standards-20261025` (= watch-search) and previous
container retained; runtime environment, port 8085, `dev-bridge`, `/keys` volume and restart
policy preserved. Before the swap, the image ran as a throwaway container (no port, sync off):
healthy, 26 routes as expected, zero error log lines.

Checks: build 0 warnings/0 errors (warnings now fail builds); 715 .NET tests passed; ESLint,
Prettier and dotnet format clean; Node unit tests 5/5. Browser suite (47 checks): the live
baseline before the work was 33 passed/14 failed, all failures being drift or environment.
Preview after: 44 passed, 3 opt-in/data skips. Live after: 42 passed, 3 skips, and pwa and
location-weather needing a secure context (identical on the old build; both pass on the
localhost preview; they now skip on plain-HTTP hosts). Computed styles of 45,652 elements on
16 pages at 390/1440px light/dark were identical before and after the CSS merge. Phone
screenshots (320/390 light/dark, JS on/off) and 1440px reviewed on preview and live. Real
device unavailable. Evidence: `/tmp/mynewspaper-code-standards-deploy/`.


## Where to watch search results tidy — deployed October 25, 2026 (Central)

From a review of the results page, the user picked two changes and declined folding
Rent/Buy and merging ad tiers. (1) **Best match** is the default sort for searches:
TMDB relevance order, kept through the paging window (FilledPage no longer reorders by
services; "Your services first" remains a sort option). New searches no longer carry
the browse sort. "the office" now lists The Office first, where it previously sank below
titles on the viewer's services. (2) One-line heading "Results for “term”" with count,
page and Clear search beside it. The sort shares that row on wider screens.
Each result keeps only Viewing details (the dialog/page still has Open viewing options).
The TV seasons note appears once below the results. Provider rows are unchanged.

Deployed `mynewspaper-web:watch-search-20261025` (`5b88f6d4b6fb`), healthy; rollback
image `mynewsfeed-web:before-mynewspaper-watch-search-20261025` and previous container
retained; runtime configuration preserved. All 715 .NET tests passed (Best match sort
tests added). Preview and live matrix passed 20 combinations including Best match
selection, heading, single details link, no per-row seasons note and 44px Clear search;
all Where to watch browser suites passed live; screenshots reviewed. track-series passed
on a rerun after one preview failure (external TVmaze dependency). Real device
unavailable. home-watch-search still fails as on earlier builds (homepage assertion).
Evidence: `/tmp/mynewspaper-watch-search-deploy/`.

## Where to watch Featured rows — deployed October 25, 2026 (Central)

User approved the streaming-rows mockup: Featured as the default view, four rows,
a mixed movie/TV services row and 12 titles per row. `/watch` (no `kind`, or
`kind=featured`) now shows Popular on your services (only with services selected;
TMDB discover with the viewer's providers, alternating movies and TV), Trending
movies this week and Trending TV this week (`trending/{kind}/week`), and New movies
you can watch (discover, released in the last 60 days in the display time zone,
US monetization types). Each row keeps up to 12 titles with US viewing options,
scanning at most three upstream pages per kind. Rows are cached for 15 minutes,
keep upstream order and personalize badges per viewer. A failed row shows its own
status and doesn't affect the others. See all opens the matching grid (My services, popular or
newest). The toggle is Featured | Movies | TV shows. Movies/TV keep the grid, sort,
My services filter and paging. Rows swipe on phones; at 768px and wider JavaScript
adds 44px arrow buttons. Row cards hide the services text visually (kept for screen
readers) since poster badges show it. New `WatchPosterCard` and `WatchRow` components.

Deployed `mynewspaper-web:watch-featured-20261025` (`9c16d8f0b3ab`), healthy; rollback
image `mynewsfeed-web:before-mynewspaper-watch-featured-20261025` and previous container
retained. Runtime settings/mounts/network/restart policy preserved. No database changes.

All 708 .NET tests passed (5 new row tests). Preview and live browser matrix passed
20 combinations (320/390/430/768/1440px, light/dark, JS on/off): row order and counts,
no page overflow, row scrolling, desktop arrows, row details modal, See all into the
grid, Featured without services (three rows), plus the existing grid/search flows.
New `tests/browser/watch-featured.browser.cjs` and the Where to watch suites passed live.
Screenshots reviewed on preview and live. Tailnet HTTPS `/watch` returned 200. Real
device unavailable. The matrix helper now waits for network idle after in-page
navigation: Blazor updates the URL before the DOM, which caused intermittent
first-run test timeouts (not page failures; uncached TV/My services page measured 0.24s).
Evidence: `/tmp/mynewspaper-watch-featured-deploy/`.

## Where to watch flow — deployed October 25, 2026 (Central)

User request: improve Where to watch's look and flow while keeping the search results
layout, and remove genre browsing entirely. Implemented:

- Movies | TV shows toggle links replace the Content type dropdown. The Genre
  dropdown is gone; old `genre=` links are ignored.
- All services | My services toggle (shown once services are selected) filters
  browsing via TMDB discover `with_watch_providers` + flatrate/free/ads, then
  rechecks each title against cached US offers. Query: `only=1`. Browse only;
  title search is unaffected. Not saved between visits (URL only).
- Selected service logos sit beside a Manage/Choose services link to Settings.
- Browse posters carry up to three logo badges for the viewer's subscription/free
  services, plus +N.
- Sort applies on change; Apply sort remains for no-JavaScript browsers.
- Toolbar note shortened ("Movies with US viewing options · Page 1"); the intro
  sentence is hidden at 600px and below so posters reach the first phone screen.
- Desktop puts search and both toggles on one row.

Deployed `mynewspaper-web:watch-flow-20261025` (image `a85358c2c77e`), container
healthy. Rollback image `mynewsfeed-web:before-mynewspaper-watch-flow-20261025`;
previous container retained under the same suffix. Runtime environment, mounts,
ports, network, health check and restart policy verified preserved. No database,
collector or credential changes.

Build, Docker publish/static-asset guard and all 703 .NET tests passed (three new
tests cover the service filter, poster badge logos and the separate filtered paging
window). Preview and live browser checks passed 20 combinations: 320/390/430/768/1440px,
light/dark, JavaScript on/off. They covered genre removal, the Movies/TV and My services
toggles, instant sort plus the no-JS Apply button, sort/kind/only preservation through
sorting, refresh and paging, poster badges shown exactly when a title is on the
viewer's services, posters on the first phone screen, the details modal with Escape,
search results unchanged with toggles hidden, no horizontal overflow and 44px
controls. Phone, tablet and desktop screenshots were inspected on preview and live.
Updated Where to watch browser suites passed on live (watch, watch-sort,
watch-results, watch-services, watch-paging, us-options, active-tv, mixed-search-nav,
section-select, track-series). Main routes returned 200 with the expected Results,
digital and admin redirects; tailnet HTTPS `/healthz` and `/watch` returned 200.
Real-device checks unavailable. Known limits: the first live matrix run timed out once
waiting for modal content on a cold TMDB cache (the immediate rerun passed 20/20), and
watch-sort timed out twice in 12 preview runs under concurrent load. home-watch-search
fails identically on the prior live build (homepage layout assertion, unrelated).
Existing Pkcs advisories remain. Evidence: `/tmp/mynewspaper-watch-flow/` (preview)
and `/tmp/mynewspaper-watch-flow-deploy/` (live; runtime JSON there is private).

## Homepage hourly lead and article actions — deployed October 24, 2026

Follow-up request: make the homepage Top story rotate hourly as well as Opinion.
Added HomeLeadQuery: it round-robins the newest headline per enabled source across
UTC hours, excluding Opinion so the separate Opinion spotlight stays distinct.
HomeLeadStory exposes the next-boundary delay; `/home/lead` serves a no-store card
fragment, and home-lead.js refreshes only this card, retains the last good story on
failure, and pauses while hidden. No News page or database changes.

Build passed with existing NU1902/NU1903 warnings for
`System.Security.Cryptography.Pkcs` 9.0.11. Published preview behavior passed with
a simulated clock, including failed-refresh retention and successful retry. The
Where to watch card now follows Top story on mobile and desktop. News, Sports and
Entertainment homepage stories have share/save actions with 44px targets.

Deployed as `mynewspaper-web:home-lead-actions-20261024` (image
`df4b96088426`); production is healthy. Runtime environment, port 8085, `dev-bridge`
network, persistent `/keys` volume and restart policy were preserved. Rollback image:
`mynewsfeed-web:before-home-lead-actions-20261024`; previous container retained.
Live browser checks passed at 320/390/430/1440px in light/dark for section order,
overflow, share/save visibility and touch targets. Share dialog and mocked save
interaction passed. `/healthz`, `/` and `/home/lead` returned 200; runtime error
markers: zero. Screenshots: `/tmp/mynewspaper-home-lead-review/`. No real-device check
available. Tailscale Serve still points to port 8085, but the deployment runner could
not resolve the tailnet hostname for an HTTPS browser check.

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

## All sports headline categories — pending local update, October 24, 2026

Moved Sports, Men's College Basketball News, Big 12 News and F1 News out of
Headlines and into Sports navigation on desktop/mobile. Added `/sports/news` and
`/sports/mens-college-basketball/news`; reused existing `/college/news` and
`/f1/news`. Headlines category/source controls and paging exclude these categories.
Enabled legacy category links redirect with filters preserved; fixed section
feeds retain scoped JS/no-JS paging. Data and Home's aggregate are unchanged.

Build/clean publish and eight focused tests passed. Sports browser coverage and
both Entertainment regression suites passed. All four sports feeds checked at
320/390/430/768/1440px light/dark, with phone screenshot review, navigation/touch
target checks, source filters and no-JS paging. No real-device check. Evidence:
`/tmp/mydailyfeed-sports-news-review/` (local read-only preview port 5314).
Not deployed, committed or pushed; the pending Entertainment/Box Office work is
also local. Latest verified live release remains `mydailyfeed-web:scoreboard-show-20261024`.

## Box Office navigation — pending local addition, October 24, 2026

Moved Box Office from News into Entertainment on desktop/mobile, including parent
active states for results, weekend preview and movie forecasts. Removed the
Headlines shortcut. Existing routes, charts, forecasts and data are unchanged.
This is part of the pending Entertainment headline reorganization, not deployed.

Publish/diff checks and both Entertainment browser suites passed. Box Office
checks covered all three routes at 320/390/430/1440px, light/dark with JavaScript,
no-JavaScript layouts, correct parent/selected link, 44px mobile links and no
overflow/errors. Mobile menu screenshots reviewed; no real-device check. Evidence:
`/tmp/mydailyfeed-entertainment-news-review/box-office.log` and menu screenshots.
No commit/push or deployment; live remains `mydailyfeed-web:scoreboard-show-20261024`.

## Entertainment/TV-Film headline placement — pending local update, October 24, 2026

Added Entertainment navigation destinations `/entertainment/news` and
`/entertainment/tv-film`, reusing the shared news page/cards with fixed category
scope. Headlines excludes both moved categories from its dropdown, sources,
All categories queries and pagination. Old enabled category links redirect with
source/search/cursor preservation. New feed titles, navigation states, source
filters, scoped JS/no-JS pagination and 44px section tabs are implemented.
No schema, source-enablement, collector or stored-article changes. Home's aggregate
and other category readers remain unchanged.

Build/clean publish and four focused tests passed, including connection-local SQL
fixtures for source/category separation, uncategorised content, disabled sources,
counts and cursor paging. Published-preview browser checks passed old redirects,
source controls, API scope, duplicate-free paging, no-JS links, nav active states,
44px controls and no horizontal overflow. Required 320/390/430px light/dark cases
were checked with screenshots; 768/1440px regression checks passed too. No real-device
check was performed. Evidence/screenshots and read-only preview (port 5313):
`/tmp/mydailyfeed-entertainment-news-review/`. No deployment, commit or push.
Latest live release remains `mydailyfeed-web:scoreboard-show-20261024`.

## Combined scoreboard Show filter — October 24, 2026

Deployed `mydailyfeed-web:scoreboard-show-20261024` (049ec734719a), healthy.
Show replaces Sports/Following with grouped All sports, Leagues, Conferences and
Teams choices, eliminating duplicate league follows and conflicting selections.
Games remains separate. GET auto-submit and no-JS Apply fallback remain. Date links
and refresh retain the selection. Old Results/competition/follow bookmarks remain
usable; valid follows take precedence over incompatible legacy league filters.
No game, record, freshness, date-limit or source-enablement semantics changed.

Build/clean publish and 35 focused tests passed, including read-only SQL filtering
and selection-resolution tests. Docker/static-asset guard passed with existing
PKCS warnings. Four local and live browser suites passed groups/unique choices,
league/conference/team/all selection, legacy bookmarks, canonical date links,
filter/refresh persistence, failure recovery, no-JS fallback and Home ordering.
Required mobile verification ran before and after deployment at 320/390/430px in
light/dark mode; all six screenshots were reviewed at each stage. Automated
responsive checks also covered 375/767/768/1440px. No real-device check was run.
Twelve live routes and private HTTPS Scoreboard returned 200; hostname resolution
override used for HTTPS. No runtime error markers observed.

Runtime settings, entrypoint, health check, mounts, ports, restart policy and
network mode compared and preserved. No schema, collector or credential changes.
Rollback image: `mynewsfeed-web:before-scoreboard-show-20261024`; previous container:
`mynewsfeed-web-before-scoreboard-show-20261024`. Evidence/screenshots:
`/tmp/mydailyfeed-scoreboard-show-deploy/`; local evidence under
`/tmp/mydailyfeed-scoreboard-show-review/`. Preview stopped. No commit or push.

## Scoreboard mobile controls and Home order — October 24, 2026

Deployed `mydailyfeed-web:scoreboard-mobile-20261024` (11296ffaa696), healthy.
Today & Tonight precedes Scoreboard in homepage document/default order and saved
layouts with the reversed pair; other preferences remain effective. Competition is
now labelled Sports, defaulting to All sports. Sports/Following/Games submit GET
forms on selection, with Apply available without JavaScript. Phone date controls
show a centered date above equal-width Previous/Next buttons and a separate Today
shortcut. Existing query parameters, date limits and filters remain compatible.

Clean publish and Docker/static-asset guard passed with existing dependency
warnings. Three live browser suites passed automatic/fallback submission, default/
legacy Home ordering, matched 44px mobile date buttons, responsive/no-overflow
layouts, consolidated navigation/filters, refresh persistence/recovery, polling
and no-JavaScript behavior. Targeted widths: 320/375/390/430/767/768/1440px, with
real light/dark themes under JavaScript. Twelve live health/public routes and
private HTTPS Scoreboard returned 200 (hostname resolution override used). Live
dark phone screenshot reviewed; no runtime error markers observed.

Runtime environment, entrypoint, health check, mounts, ports, restart policy and
network mode compared and preserved. No schema/collector/credential changes.
Rollback image: `mynewsfeed-web:before-scoreboard-mobile-20261024`; retained previous
container: `mynewsfeed-web-before-scoreboard-mobile-20261024`. Evidence and live
screenshot: `/tmp/mydailyfeed-scoreboard-mobile-deploy/`. Temporary local review
preview stopped; earlier screenshots remain under
`/tmp/mydailyfeed-scoreboard-mobile-review/`. No commit or push for this release.

## Daily Results consolidated into Scoreboard — October 24, 2026

Deployed `mydailyfeed-web:scoreboard-consolidation-20261024` (10d84111ec48), healthy.
Scoreboard now includes current team records, enabled league/team/conference
Following choices, final-only filtering and links to enabled MLS/Championship
season results. `/results` redirects temporarily to Scoreboard's final-score view,
preserving day/follow queries. The duplicate Results navigation item is removed;
box-score/admin links point to Scoreboard. College/season results, F1, existing
result storage and synchronization remain intact. Filters persist through date
links and stored-score refresh. Phone controls use aligned label/select columns.
Records retain regular-season confirmation, playoff suppression and stale labels.

Local build/clean publish and 34 focused scoreboard/team-record tests passed,
including read-only SQL follow/final checks. Docker publish/static-asset guard
passed with existing dependency warnings. Live consolidation, scoreboard and
cached box-score browser suites passed redirects/bookmarks, filters, date/refresh
persistence, JS/no-JS responsive layouts, failed-read recovery, hidden-tab polling,
disclosure retention and modal/keyboard/fallback behavior. Twelve health/public
routes returned 200; legacy Results over private HTTPS redirected successfully,
preserving its follow parameter (hostname resolution override used). Live mobile
screenshot reviewed; no runtime error markers observed.

Runtime environment, entrypoint, health check, persistent mounts, ports, restart
policy and network mode were compared and preserved. No schema, collector or
credential changes. Previous container retained as
`mynewsfeed-web-before-scoreboard-consolidation-20261024`; rollback image:
`mynewsfeed-web:before-scoreboard-consolidation-20261024`. Deployment evidence:
`/tmp/mydailyfeed-scoreboard-consolidation-deploy/`; local test/review artifacts:
`/tmp/mydailyfeed-scoreboard-consolidation/`. Core consolidation was already in
`3b6e960`; remaining polish/documentation changes are uncommitted. No push attempted.

## Scoreboard history limited to yesterday — October 23, 2026

Deployed `mydailyfeed-web:scoreboard-history-20261023` (f8694fbf28ee), healthy.
Previous day navigation stops at yesterday in Central time. Older date parameters
fall back to today in both the full page and refresh fragment. Seven days ahead
and prior-day active/paused carry-over remain. Stored games and scraper retention
were not changed.

Solution build and Docker publish/static-asset guard passed with existing warnings.
Live JavaScript/no-JavaScript checks at 320/390/1440px passed yesterday's boundary,
older page/fragment fallback, future limit, no overflow/errors and five public/health
routes. Private HTTPS Scoreboard returned 200 using a hostname resolution override.
Runtime settings, entrypoint, health check, mounts, ports, network mode and restart
policy were compared and preserved. Previous container retained as
`mynewsfeed-web-before-scoreboard-history-20261023`; rollback image:
`mynewsfeed-web:before-scoreboard-history-20261023`. No schema or collection changes.
Evidence: `/tmp/mydailyfeed-scoreboard-history-deploy/`. No commit or push.

## Consistent scoreboard styling — October 23, 2026

Deployed `mydailyfeed-web:scoreboard-style-20261023` (c52b23d52a89), healthy.
Shared sports team badges, aligned score/star columns, consistent Live/Final pills
and action links, and aligned footers replace uneven matchup cards. The full page
uses up to three desktop columns; Game info disclosures contain venue/broadcast/
period details. Home uses compact divided rows. Short update times retain full
UTC-backed date/time tooltips. Stale and collection warnings remain visible.
Expanded disclosures survive automatic refresh; keyboard focus still prevents
replacement while controls are in use.

Clean publish and Docker/static-asset guard passed with existing dependency
warnings. Local phone checks passed 320/375/390/430px light/dark. Live tests passed
320/390/768/1440/1884px light/dark for Home and Scoreboard, no overflow, expanded
details, three-column footer alignment, eight health/public routes and no JavaScript
errors. The existing scoreboard suite passed filters, preferences, failed-read
retention/recovery, five-minute hidden-tab polling, disclosure retention and
no-JavaScript controls against the deployed image. Live mobile screenshot reviewed;
private HTTPS Scoreboard returned 200 using a hostname resolution override.

Runtime environment, entrypoint, health check, mounts, ports, restart policy and
network mode were compared and preserved. No schema or collector changes.
Rollback image: `mynewsfeed-web:before-scoreboard-style-20261023`; retained container:
`mynewsfeed-web-before-scoreboard-style-20261023`. Evidence and live screenshots:
`/tmp/mydailyfeed-scoreboard-style-deploy/`. Earlier local review artifacts remain
in `/tmp/mydailyfeed-scoreboard-style-review/`. No commit or push for this release.

## Live scoreboard — October 23, 2026

Deployed `mydailyfeed-web:scoreboard-20261023` (0585962b33b5), healthy.
Home has a separate six-game Scoreboard card; Sports navigation links to the new
`/scoreboard` page with enabled competition filtering and Central date navigation
(three days back through seven ahead). Today includes prior-day active/paused
games. Existing schedules/results and F1 remain separate. Browser reads use
scraper-owned views only, with no new collection, schema or write permissions.

Five-minute visible-page refresh and return-to-tab reads retain the last displayed
sample on failure. Fixed sampled clocks, stale/error/partial warnings, optional
period/count/base details and separate shootout scores preserve source semantics.
Stars use separate browser preferences keyed by sport/league/team ID. Cached
box-score actions reuse existing modals; detail visibility now recognizes enabled
scoreboard coverage as well as existing result sources.

Solution build and 37 focused scoreboard/box-score tests passed, including a
read-only SQL view/materialization/filter check. Docker publish/static-asset guard
passed with existing PKCS dependency warnings. Preview, built-image preview and
live browser checks passed 320/390/768/1440px, light/dark, competition/date forms,
canonical deduplication, star persistence, failed-read retention/recovery,
five-minute/hidden-tab refresh and no-JavaScript controls. All three advertised
box-score modals opened in the image preview; mobile screenshot reviewed. Fourteen
live public/health routes returned 200, no runtime error markers were observed,
and private HTTPS `/scoreboard` returned 200 using a hostname resolution override.
Live data is dated evidence, not a guarantee of every status or sport appearing.
Status/NULL/zero/stale/JSON/DST cases also have deterministic tests; no production
sources were disabled or fixture games inserted for testing.

Runtime environment, entrypoint, health check, mounts, ports, restart policy and
network mode were compared and preserved. Previous container retained as
`mynewsfeed-web-before-scoreboard-20261023`; rollback image:
`mynewsfeed-web:before-scoreboard-20261023`. Temporary image preview removed.
Evidence: `/tmp/mydailyfeed-scoreboard-deploy/`. No commit or push for this release.

## Homepage Refresh control — October 23, 2026

Deployed `mydailyfeed-web:home-refresh-20261023` (7b982425121e). A Refresh link
styled as a button sits beside the homepage greeting and performs a full GET
reload of the current URL, preserving source/poll query selections. It works
without JavaScript and reads latest available content without triggering
collection jobs. Docker build/static-asset guard passed. Live checks at 320,
390 and 1440px with JavaScript on/off verified visibility, successful full
reload, query preservation and no horizontal overflow. Health returned 200;
runtime settings/mounts preserved. Previous container retained as
`mynewsfeed-web-before-home-refresh-20261023`, rollback image
`mynewsfeed-web:before-home-refresh-20261023`. No schema changes.
Evidence: `/tmp/mydailyfeed-home-refresh-deploy/`.

## Remove Selected badge — October 23, 2026

Deployed `mydailyfeed-web:no-selected-badge-20261023` (29777354a9fe). Removed
the generated Selected badge beside standings team names; solid selected-row
colors, contrasting text and edge remain unchanged. Docker build passed. Live
checks across 13 palettes, light/dark and 390/1440px (52 combinations) passed
row fill/text contrast, absence of badges and no horizontal document overflow.
Health returned 200; runtime configuration and mounts preserved. Previous
container retained as `mynewsfeed-web-before-no-selected-badge-20261023`,
rollback image `mynewsfeed-web:before-no-selected-badge-20261023`.
Evidence: `/tmp/mydailyfeed-no-selected-badge-deploy/`.

## Strong selected standings rows — October 23, 2026

Deployed `mydailyfeed-web:selected-standings-20261023` (3a4a4f6bb7fa). Selected
standings rows now use solid theme selection colors, contrasting text/edge and
a visible Selected label. Unselected rows retain alternating stripes. Docker
publish/static-asset guard passed. Live checks across all 13 palettes, light
and dark at 390 and 1440px (52 combinations) verified consistent cell fills,
text contrast of at least 4.5:1, labels and no horizontal document overflow.
Tests applied the existing selected class to real standings rows. Health returned
200; runtime configuration and mounts preserved. Previous container retained as
`mynewsfeed-web-before-selected-standings-20261023`, rollback image
`mynewsfeed-web:before-selected-standings-20261023`. No schema changes.
Evidence: `/tmp/mydailyfeed-selected-standings-deploy/`.

## Full-width standings row striping — October 23, 2026

Deployed `mydailyfeed-web:standings-stripes-20261023` (d432d17a21a5). Standings
rows alternate surface and accent-tinted backgrounds across all cells, including
sticky team names and percentage columns. Selected teams use a stronger fill.
Docker publish/static-asset guard passed. Live College football standings checks
at 390 and 1440px in light/dark verified uniform cell backgrounds, alternating
rows, distinct selected-row fills and no horizontal document overflow. Health
returned 200; runtime configuration and mounts preserved. Previous container
retained as `mynewsfeed-web-before-standings-stripes-20261023`, rollback image
`mynewsfeed-web:before-standings-stripes-20261023`. No schema changes.
Evidence: `/tmp/mydailyfeed-standings-stripes-deploy/`.

## Selected-team row fill — October 23, 2026

Deployed `mydailyfeed-web:team-highlight-20261023` (ea55ea3cb6c9). Selected
team rows use an accent-tinted full-row background instead of a red outline,
including sticky cells. Daily Ribbon uses a yellow edge; other palettes use
their accent. Shared Results/Events/standings selection styling is updated.
Docker publish/static-asset guard passed. Live yesterday Results
(`/results?day=2026-10-22`) verified selected-row fill, no outline, yellow edge,
light/dark layouts at 390, 768 and 1440px without horizontal overflow or page
errors. The test applied the existing selected-row class to a real result row.
Health returned 200; runtime settings and mounts compared and preserved.
Previous container retained as `mynewsfeed-web-before-team-highlight-20261023`;
rollback image `mynewsfeed-web:before-team-highlight-20261023`. No schema changes.
Evidence: `/tmp/mydailyfeed-team-highlight-deploy/`.

## Daily Ribbon finishing details — October 22, 2026

Deployed `mydailyfeed-web:ribbon-finish-20261022` (e59120dff86d). Daily Ribbon
now includes a subtle curved header ribbon, yellow curved heading accents,
blue secondary controls and pale blue Big 12 rows with yellow edges and badges.
Other schemes retain their styling and red full-page ranking highlights. Mobile
team badges remain hidden with the legend visible.

Docker build/static-asset guard passed. Live checks passed all 13 palette
choices/persistence, light/dark Home/rankings/Settings at 390 and 1440px,
consistent highlighted row backgrounds, yellow edges and mobile badge behavior.
Desktop rankings screenshot reviewed. Health returned 200; runtime configuration
and mounts preserved. Previous container retained as
`mynewsfeed-web-before-ribbon-finish-20261022`, rollback image
`mynewsfeed-web:before-ribbon-finish-20261022`. Evidence:
`/tmp/mydailyfeed-ribbon-finish-deploy/`.

## Daily Ribbon default palette — October 22, 2026

Deployed `mydailyfeed-web:ribbon-theme-20261022` (7f536e007dbc). New browsers
use the logo-inspired Daily Ribbon palette: blue gradient header, pale blue
surfaces, warm yellow selection accents and softer cards/buttons. Dark mode
uses navy surfaces. Existing saved palettes retain their selection and styling;
Settings offers Daily Ribbon plus all twelve previous palettes. Existing red
Big 12 row highlights remain unchanged.

Docker publish/static-asset guard and JavaScript syntax checks passed. Live
checks verified default selection, all 13 palette controls, persistence after
reload, and Home/rankings/Settings at 390 and 1440px in light/dark mode without
horizontal overflow or JavaScript errors. Desktop light and mobile dark
screenshots reviewed. Health returned 200; runtime settings and mounts preserved.
Previous container retained as `mynewsfeed-web-before-ribbon-theme-20261022`;
rollback image `mynewsfeed-web:before-ribbon-theme-20261022`. No schema changes.
Pre-theme source tag: `design-before-logo-theme-20261022`; source archive and
reference screenshots are in `new development/design-backup-before-logo-theme/`.
Evidence: `/tmp/mydailyfeed-ribbon-theme-deploy/`.

## Big 12 full-row highlighting — October 22, 2026

Deployed `mydailyfeed-web:big12-rows-20261022` (5c0e31e7f9d5). Full rankings
tables highlight every cell of Big 12 rows with a light red background and red
border. Below 1024px team badges are hidden; the legend above the table remains.
Desktop team badges remain visible. Docker publish/static-asset guard passed;
live light/dark checks at 390, 768, 1024 and 1440px passed consistent cell color,
badge visibility, retained legend and no document overflow. Health returned 200.
Runtime configuration and mounts preserved; previous container retained as
`mynewsfeed-web-before-big12-rows-20261022`, rollback image
`mynewsfeed-web:before-big12-rows-20261022`. No schema changes.
Evidence: `/tmp/mydailyfeed-big12-rows-deploy/`.

## Uniform full rankings tables — October 22, 2026

Deployed `mydailyfeed-web:uniform-rankings-20261022` (f33ec770ba51). All sports
on full rankings pages now share Rank, Team, Record, Points, Previous and Trend
columns. First-place votes appear beside team names with an explanatory legend.
Full columns display automatically at 1024px and wider; smaller screens retain
the opt-in detail toggle. Homepage rankings remain compact.

Docker publish/static-asset guard passed. Live checks passed all three populated
poll sources at 390, 768, 1023, 1024 and 1440px, including column consistency,
detail visibility, mobile toggle, no document overflow or JavaScript errors.
Home, College rankings and health returned HTTP 200; desktop screenshot reviewed.
Runtime configuration and persistent mounts compared and preserved. Previous
container retained as `mynewsfeed-web-before-uniform-rankings-20261022`, rollback
image `mynewsfeed-web:before-uniform-rankings-20261022`. No schema changes.
Evidence: `/tmp/mydailyfeed-uniform-rankings-deploy/`.

## Mobile Tools group — October 22, 2026

Deployed `mydailyfeed-web:mobile-tools-20261022` (4e29e3f0e6ab). Mobile Saved,
Settings and Admin sign in (Administration when authorized) now share a Tools
group using existing mobile group styling. Desktop navigation is unchanged.
Docker publish/static-asset guard passed. Live anonymous checks at 320, 390,
768 and 1440px verified group labels and targets, Saved active state, menu
closure, mobile menu bounds and desktop controls. Screenshot reviewed at 390px.
Health returned 200; runtime settings and mounts preserved. Saved content had
365px document width at a 320px viewport; the navigation itself fit, and wider
checked viewports had no document overflow. Authenticated label verified in
source only. Previous container retained as
`mynewsfeed-web-before-mobile-tools-20261022`, rollback image
`mynewsfeed-web:before-mobile-tools-20261022`. No schema changes.
Evidence: `/tmp/mydailyfeed-mobile-tools-deploy/`.

## Watch and YouTube after Entertainment — October 22, 2026

Deployed `mydailyfeed-web:nav-after-entertainment-20261022` (e012d18cd66f).
Desktop and mobile begin Home, News, Sports, Entertainment, Where to watch,
YouTube. Docker publish/static-asset guard passed. Live checks at 390, 768 and
1440px verified order, no horizontal overflow and health HTTP 200. Runtime
settings and mounts preserved; prior container retained as
`mynewsfeed-web-before-nav-after-entertainment-20261022`, with rollback image
`mynewsfeed-web:before-nav-after-entertainment-20261022`. No schema changes.
Evidence: `/tmp/mydailyfeed-nav-after-entertainment-deploy/`.

## Navigation link order — October 22, 2026

Deployed `mydailyfeed-web:nav-order-20261022` (b0251e72ab5d). Desktop and mobile
now begin Home, Where to watch, YouTube, News, Sports, Entertainment. Build and
Docker static-asset guard passed with existing dependency warnings. Live browser
checks at 390, 768 and 1440px verified order and no horizontal overflow; health
returned 200. Runtime settings and mounts preserved; prior container retained as
`mynewsfeed-web-before-nav-order-20261022`, rollback image
`mynewsfeed-web:before-nav-order-20261022`. No schema changes.
Evidence: `/tmp/mydailyfeed-nav-order-deploy/`.

## News navigation group — October 22, 2026

Deployed `mydailyfeed-web:news-nav-20261022` (65afd3dddc7c), healthy.
News now groups Headlines, Local and Box Office on desktop and mobile; the
News page heading and title are Headlines. Desktop News highlights all three
destinations. Runtime configuration and persistent mounts were compared and
preserved; no schema changes. Previous container retained as
`mynewsfeed-web-before-news-nav-20261022`, with rollback image
`mynewsfeed-web:before-news-nav-20261022`.

Build and Docker static-asset guard passed with existing dependency warnings.
Live browser checks at 390, 768 and 1440px passed grouping, all three active
states, link navigation, heading, menu close on selection, no horizontal
overflow or JavaScript errors. Health returned 200 and container was healthy.
Private HTTPS was not separately checked. Evidence:
`/tmp/mydailyfeed-news-nav-deploy/`.

## Separate homepage Digital / VOD card — October 22, 2026

Upcoming Digital / VOD now has its own homepage card and cascades independently
of Friday theater releases. The theatrical card is absent Monday–Wednesday;
Digital remains available all week. Source/date selection, poster and retailer
behavior are preserved. Legacy hidden Movies & Digital preferences hide both.

Deployed `mydailyfeed-web:digital-card-20261022` (761fae8e3900), healthy.
Rollback image `mynewsfeed-web:before-digital-card-20261022` (bb5d0487fb60),
retained container `mynewsfeed-web-before-digital-card-20261022`. Runtime settings
and persistent mounts compared and preserved; no schema changes. Solution build,
Docker publish/static-asset guard, JavaScript syntax and diff checks passed with
existing dependency warnings. Live checks at 320, 390, 768, 1024, 1440 and 1920px
passed independent card markup, links, no overlap/overflow or JavaScript errors,
legacy hidden preference and health. Digital card screenshot reviewed. Evidence:
`/tmp/mydailyfeed-digital-card-deploy/`. No commit or push for this release.

## Theatrical scraper handoff completion — October 22, 2026

Reviewed the scraper's theatrical/no-disc completion handoff. The frontend keeps
its enabled-provider, country, status and view-specific date filters; CollectDisc
remains a scraper collection policy, with no new frontend mapping or write flow.
Home's theatrical View all now links to `/movies?type=theatrical`. Digital provider
help explains that DVDs Release Dates also supplies theater/digital dates; admin
status help explains that disabling a provider hides all its release types.

Deployed `mydailyfeed-web:theatrical-completion-20261022` (bb5d0487fb60), healthy.
Rollback image `mynewsfeed-web:before-theatrical-completion-20261022` (6ce5d5c9c5d8),
retained container `mynewsfeed-web-before-theatrical-completion-20261022`. Runtime
environment, entrypoint, health check, mounts, ports, restart policy and network
mode compared and preserved. No frontend schema or provider setting changes.

Twenty-eight focused tests passed, including SQL connection-local temporary-table
fixtures for CollectDisc=0 theatrical/digital coverage, withdrawn discs, disabled
providers, deduplication before Home's cap, Central-time dates and distinct
Home/Movies date windows, plus schedule and commerce tests. Production rows were
not modified by these fixtures. Docker publish/static-asset guard and diff checks
passed with existing dependency/analyzer warnings. Live browser checks at 390 and
1440px confirmed five films on Home, six on theatrical Movies, keyboard navigation
through View all, provider wording, no disc badges in All releases, no overflow or
JavaScript errors. These counts are dated observations. Read-only diagnostics
confirmed six scheduled theatrical, six digital and five subscription releases,
24 withdrawn discs and no scheduled discs. Health and checked public routes
returned 200 (Digital follows its canonical-source redirect); admin redirects to
login. Private HTTPS was not separately checked. Evidence:
`/tmp/mydailyfeed-theatrical-completion-deploy/` and
`/tmp/mydailyfeed-movies-diagnosis/contract-tests.log`. Not committed or pushed.

## Homepage theatrical window — October 22, 2026

The homepage shows the current Friday's US theatrical releases Thursday through
Sunday inclusive, using the configured display time zone. Saturday/Sunday retain
that Friday; Monday–Wednesday hide the theatrical subsection. No future Friday is
substituted. Digital / VOD remains visible all week; the Movies page is unchanged.

Deployed `mydailyfeed-web:theatrical-window-20261022` (6ce5d5c9c5d8), healthy.
Rollback image `mynewsfeed-web:before-theatrical-window-20261022` (6eac62c942c8),
retained container `mynewsfeed-web-before-theatrical-window-20261022`. Runtime
configuration and persistent mounts compared and preserved; no schema changes.
Ten date-window tests passed, including all weekdays and a year boundary.
SQL integration expectations were updated but not run. Docker publish/static-asset
guard passed with existing warnings. Live Thursday checks at 320, 390, 768, 1024,
1440 and 1920px passed theatrical/digital visibility, Friday dates when present,
overflow and JavaScript checks. Health, Home, Movies, Digital/VOD, News, Where to
watch, Events, Results and Box office returned 200 over container HTTP. Other
weekdays were verified through date-window tests, not live clock changes.
The live cascading-layout regression also passed priorities, resizing, dynamic
heights, hidden cards and no-JavaScript fallback.
Private HTTPS was not separately checked. Evidence:
`/tmp/mydailyfeed-theatrical-window-deploy/`. No commit or push for this release.

## Cascading homepage with Watch and news priority — October 22, 2026

Restored shorter-column placement on desktop. Where to watch and Latest news
lead the columns; subsequent cards use the shorter column. Mobile shows Where
to watch first and Latest news second. Hidden-section and team preferences remain
effective; legacy saved ordering does not override these priorities.

Deployed `mydailyfeed-web:cascade-priority-20261022` (6eac62c942c8), healthy.
Rollback image `mynewsfeed-web:before-cascade-priority-20261022` (6b6cbf9d3ab6),
retained container `mynewsfeed-web-before-cascade-priority-20261022`. Environment,
entrypoint, health check, persistent mounts, ports, restart policy and network mode
were compared and preserved. No schema changes. Solution build, Docker publish/
static-asset guard, JavaScript syntax and diff checks passed with existing warnings.
Live browser checks at 320, 390, 767, 768, 1023, 1024, 1440 and 1920px passed
priority, shorter-column placement, resizing, dynamic heights, hidden cards,
overlap/overflow, JavaScript errors and no-JavaScript desktop/mobile fallback.
Health, Home, News, Videos, Events, Results, Standings, Rankings, Movies,
Digital/VOD, both box-office views and Where to watch returned 200 over container
HTTP. Private HTTPS was not separately checked. Screenshots and verification
evidence: `/tmp/mydailyfeed-cascade-check/`. No commit or push for this release.

## Mobile homepage news and video limits — October 21, 2026

At phone widths through 767px, homepage Latest news shows up to 8 source stories
and Latest videos up to 5 channels. Headings reflect the responsive limits. Larger
screens retain 15 news sources and 10 video channels; View all and hourly rotation
remain. CSS hides excess homepage rows without requiring JavaScript. Browser CSS
preview checks passed at 320, 412, 767, 768 and 1280px, including resizing back to
the larger lists.

Deployed `mydailyfeed-web:mobile-home-limits-20261021` (6b6cbf9d3ab6), healthy.
Rollback image `mynewsfeed-web:before-mobile-home-limits-20261021` (dddf64c4dfdc),
retained container `mynewsfeed-web-before-mobile-home-limits-20261021`. Runtime
environment, key volume, ports, restart policy and network preserved; no schema
changes. Build, Docker publish/static-asset guard and diff checks passed. Live browser
checks at 320, 412, 767, 768, 1280 and 1920px verified counts, responsive headings,
no horizontal overflow and no JavaScript errors. Health, /news, /videos, /box-office,
/box-office/weekend-preview and /movies returned 200. Evidence:
`/tmp/mydailyfeed-header-clock-deploy/limits-*`.

## Box-office estimate and final labels — October 21, 2026

Integrated the scraper status contract into box-office results and the homepage
Sunday–Tuesday results card. Each weekend gross displays Estimated, Final or
Reported totals (NULL/unrecognized statuses fall back to Reported totals).
Cumulative domestic totals have no estimate/final label. Safe status-evidence links
fall back to chart attribution; provider update time appears only when supplied,
and collection time remains separately labeled. Final means provider-reported
actuals; later corrections are possible. Current-weekend Home reads bind the
expected Friday as a SQL date; no older chart is substituted. Weekday rotation,
previews, posters and empty/error handling remain.

Deployed `mydailyfeed-web:boxoffice-status-20261021` (dddf64c4dfdc), healthy.
Rollback image `mynewsfeed-web:before-boxoffice-status-20261021` (126256560c42),
retained container `mynewsfeed-web-before-boxoffice-status-20261021`. Runtime
environment, key volume, ports, restart policy and network preserved. No frontend
DDL or collector changes. Existing reader successfully read the new fields before
replacement. Twenty-one status/schedule/forecast tests passed, including mixed
mock statuses and absent provider timestamps. Docker publish/static-asset guard
and diff checks passed with existing warnings. Live browser checks at 320, 412,
1280 and 1920px passed estimated/unknown labels, separate domestic totals, missing
provider timestamps, source links and overflow. Predictions remain visible on the
current day. Final was tested with mock data only; no live Final row was verified.
Health, Home, all box-office views, /news, /movies, /events, /results, /standings
and /rankings returned 200. Evidence:
`/tmp/mydailyfeed-header-clock-deploy/status-*`.

## Homepage Latest news expanded to 15 sources — October 21, 2026

Increased the Latest news card limit from 10 to 15 distinct sources, retaining
one latest story per source and hourly rotation. The heading uses the same count
as the query limit.

Deployed `mydailyfeed-web:home-news15-20261021` (126256560c42), healthy.
Rollback image `mynewsfeed-web:before-home-news15-20261021` (f2b6f3283649),
retained container `mynewsfeed-web-before-home-news15-20261021`. Runtime
environment, key volume, ports, restart policy and network preserved; no schema
changes. Build, Docker publish/static-asset guard and diff checks passed. Live
browser checks at 320, 412, 1280 and 1920px confirmed 15 news items, the updated
heading and no horizontal overflow. Health, /news, /box-office and /movies
returned 200. Evidence: `/tmp/mydailyfeed-header-clock-deploy/news15-*`.

## Homepage box-office card spacing — October 21, 2026

Removed the standalone forecast section’s 40px top margin from homepage cards so
the card grid controls both surrounding gaps. Applies to predictions and weekend
results. Set the mobile card order to match Friday releases, keeping box office
immediately above releases. Browser CSS preview checks at 320, 412, 1280 and 1920px
verified surrounding gaps match the grid spacing (including desktop row rounding).

Deployed `mydailyfeed-web:boxoffice-spacing-20261021` (f2b6f3283649), healthy.
Rollback image `mynewsfeed-web:before-boxoffice-spacing-20261021` (7a8be93ea555),
retained container `mynewsfeed-web-before-boxoffice-spacing-20261021`. Runtime
environment, key volume, ports, restart policy and network preserved. Docker publish
and static-asset guard passed. Live browser spacing checks passed at 320, 412,
1280 and 1920px; mobile gaps are 12px on both sides and desktop gaps follow the
existing grid row rounding. Health, Home, box-office results/preview and Movies
returned 200. No schema changes. Evidence:
`/tmp/mydailyfeed-header-clock-deploy/spacing-*`.

## Homepage box-office weekly rotation — October 21, 2026

The homepage box-office card shows predictions Wednesday through Saturday and
reported weekend results Sunday through Tuesday, using the configured display
zone (Central in production). Both variants share the same homepage visibility
setting, now named Weekend box office, and stay above Friday theater releases.
Results show the current weekend only; missing current-weekend rows show a waiting
message instead of an older chart. Read failures leave the rest of Home available.

The existing results feed has no estimate/final indicator. Totals refresh from
The Numbers and carry a revision notice; they are not labeled final by calendar day.
Explicit estimate-to-final labeling requires scraper-supplied publication status.
No schema changes were made. Fifteen schedule/forecast tests passed, covering every
weekday, weekend selection, year boundary and Central midnight. Build succeeded
with existing package/analyzer warnings; diff checks passed.

Deployed `mydailyfeed-web:boxoffice-rotation-20261021` (7a8be93ea555), healthy.
Rollback image `mynewsfeed-web:before-boxoffice-rotation-20261021` (e2a913b6fbad),
retained container `mynewsfeed-web-before-boxoffice-rotation-20261021`. Runtime
environment, key volume, ports, restart policy and network preserved; no schema
changes. Docker publish/static-asset guard passed. Live predictions-card checks
at 320, 412, 1280 and 1920px passed placement, link, overflow and visibility checks.
Sunday–Tuesday selection was verified by automated tests, not by changing the
production clock. Health, Home, all three box-office views, /movies, /news, /events,
/results, /standings and /rankings returned 200. Evidence:
`/tmp/mydailyfeed-header-clock-deploy/rotation-*`.

## Homepage weekend predictions card — October 21, 2026

Added Upcoming weekend preview — Predictions as a separate homepage card above
Friday theater releases. Reuses the box-office forecast section, including ranks,
posters, predicted gross ranges, dates, source links and stale/empty messaging.
View all links to /box-office/weekend-preview. Added a Weekend predictions visibility
choice to homepage settings. Forecast read failures leave the rest of Home available.

Validation: solution build, JavaScript syntax and diff checks passed; four existing
forecast tests passed. Local browser checks at 320, 412, 1280 and 1920px verified
card placement, View all link, no horizontal overflow and independent visibility.

Deployed `mydailyfeed-web:home-forecasts-20261021` (e2a913b6fbad), healthy.
Rollback image `mynewsfeed-web:before-home-forecasts-20261021` (75b5eb21198b),
retained container `mynewsfeed-web-before-home-forecasts-20261021`. Runtime
environment, key volume, ports, restart policy and network preserved; no schema
changes. Docker publish/static-asset guard passed. Live browser checks repeated
at 320, 412, 1280 and 1920px passed placement, link, overflow and visibility checks.
Health, Home, all three box-office views, /movies, /news, /events, /results,
/standings and /rankings returned 200. Private deployment evidence:
`/tmp/mydailyfeed-header-clock-deploy/forecasts-*`.

## Remove redundant mobile News label — October 21, 2026

Removed the non-linked News group label above Home in the mobile Browse menu.
The News navigation link and Sports/Entertainment group labels remain. Verified
the intended markup change and diff formatting.

Deployed `mydailyfeed-web:mobile-menu-label-20261021` (75b5eb21198b), healthy.
Rollback image `mynewsfeed-web:before-mobile-menu-label-20261021` (3d8a6f89f8f0),
retained container `mynewsfeed-web-before-mobile-menu-label-20261021`. Runtime
environment, key volume, ports, restart policy and network preserved; no schema
changes. Docker publish/static-asset guard passed. Live browser checks at 320,
412 and 768px verified the label is absent, Home/News links and Sports/Entertainment
labels remain, navigation works and no horizontal overflow or JavaScript errors
occur. Clock remains visible. Health and /news, /events, /results, /standings,
/rankings, /movies, /movies/digital and /box-office returned 200. Evidence:
`/tmp/mydailyfeed-header-clock-deploy/menu-*`.

## Mobile clock beside logo — October 21, 2026

Moved the mobile date and clock beside the logo in two lines. Mobile uses a shorter
date without the weekday (for example, Oct 21, 2026); time retains AM/PM and timezone
without seconds. Browser preview checks passed at 320, 360, 375, 412, 767, 768 and
1600px, including header alignment and horizontal overflow checks. Visually inspected
the 320px preview. JavaScript syntax and diff checks passed.

Deployed `mydailyfeed-web:mobile-clock-20261021` (3d8a6f89f8f0).
Rollback image `mynewsfeed-web:before-mobile-clock-20261021` (baf8259acf3c),
retained container `mynewsfeed-web-before-mobile-clock-20261021`. Runtime environment,
mounts, ports, restart policy and network preserved; no schema changes. Docker publish
and static-asset guard passed. Live browser checks passed at 320, 375, 768, 1280,
1600 and 1920px, including mobile logo/clock/button alignment, AM/PM without seconds,
no horizontal overflow, enhanced navigation and no JavaScript errors. Health and
/news, /events, /results, /standings, /rankings, /movies, /movies/digital and
/box-office returned 200. Evidence: `/tmp/mydailyfeed-header-clock-deploy/`.

## Header date and live clock — October 21, 2026

Added a browser-local date and 12-hour time with AM/PM and timezone (without seconds) to the public
header. The clock uses a compact separate row on mobile and narrower screens, and
sits inline on wide desktops. It updates each second, after tab visibility changes
and enhanced navigation; it stays hidden when JavaScript is unavailable. No live
screen-reader announcements are emitted for clock updates.

Validation: solution build, JavaScript syntax and diff checks passed. Isolated clock
checks passed initial rendering, one-second scheduling, midnight rollover and enhanced
navigation. Existing package security and xUnit analyzer warnings remain.

Deployed `mydailyfeed-web:header-clock-20261021` (baf8259acf3c), healthy.
Rollback image `mynewsfeed-web:before-header-clock-20261021` (713fdefd1110),
retained container `mynewsfeed-web-before-header-clock-20261021`. Runtime environment,
mounts, ports and restart policy preserved; no schema changes. Docker publish and
static-asset guard passed. Live browser checks passed at 320, 375, 768, 1280, 1600
and 1920px: local date, 12-hour AM/PM without seconds, no horizontal overflow,
enhanced navigation and no JavaScript errors. Visually inspected mobile and desktop
screenshots. Health and /news, /events, /results, /standings, /rankings, /movies,
/movies/digital and /box-office returned 200. Private deployment evidence:
`/tmp/mydailyfeed-header-clock-deploy/`.

## Device-location forecast with Kansas City fallback — October 21, 2026

Added Use my location and Use Kansas City controls to the Local forecast. Location
permission is requested only after a click on a secure origin. Coordinates are
rounded to two decimal places in the browser and server; the selection stays in
memory, not persistent storage. The forecast title uses the NWS nearby city/state,
and its official link follows the selected point. Local news/events remain KC.
The API validates coordinate pairs, finite values and ranges, caches per rounded
location, and sets custom forecast responses private/no-store. Existing Kansas City
caching and stale-data behavior remain. Denial, timeout, unavailable location and
unsupported/failed NWS locations fall back to KC with an explanation. Reset cancels
pending selection and superseded requests cannot overwrite newer results.

Deployed `mydailyfeed-web:location-weather-20261021` (713fdefd1110), healthy.
Rollback image `mynewsfeed-web:before-location-weather-20261021` (a663937b4932),
retained container `mynewsfeed-web-before-location-weather-20261021`. Runtime settings,
ports, mounts and keys preserved; no schema changes. LocalWeather tests passed (6):
cache isolation, rounded/validated coordinates, city labels, stale fallback and
untrusted forecast-host rejection. Docker publish/static-asset guard and diff checks
passed with existing PKCS warnings. Browser tests passed explicit geolocation,
rounding, labels/official links, reset, denied/timeout/unavailable/unsupported fallback,
cancellation, mobile/desktop, no-JavaScript fallback and API validation/privacy.
Existing weather icon/theme regression passed. Tested permission flow on actual
private HTTPS with simulated browser coordinates and confirmed real NWS New York
forecast rendering (4 periods). Real KC and NY API responses succeeded; private HTTPS
health 200. HTTP location control correctly explains HTTPS and falls back to KC.
Production device location was not obtained; browser tests used controlled coordinates.
Visually inspected the location forecast. Private files:
`/tmp/mydailyfeed-location-weather-deploy/`.

## Rename homepage poll column to Previous — October 21, 2026

Relabeled Previous rank as Previous in homepage Top 25. Values, logos and Big 12
highlighting are unchanged. Updated existing label assertions.

Deployed `mydailyfeed-web:top25-previous-20261021` (a663937b4932), healthy.
Rollback image `mynewsfeed-web:before-top25-previous-20261021` (975a2217e423), retained
container `mynewsfeed-web-before-top25-previous-20261021`. Runtime settings, ports,
mounts and keys preserved; no data/schema changes. Docker publish/static-asset guard
and diff checks passed with existing PKCS warnings. Live browser verified headings
Rank, School, Record, Previous; health returned 200.
Private deployment files: `/tmp/mydailyfeed-top25-previous-deploy/`.

## Remove homepage Kansas City shortcut — October 21, 2026

Removed the Kansas City link below the homepage greeting and its now-empty daily-
links navigation row. Local remains available through the main navigation.

Deployed `mydailyfeed-web:home-local-link-20261021` (975a2217e423), healthy.
Rollback image `mynewsfeed-web:before-home-local-link-20261021` (484aaad7e785), retained
container `mynewsfeed-web-before-home-local-link-20261021`. Runtime settings, ports,
mounts and keys preserved; no schema/data changes. Docker publish/static-asset guard
and diff checks passed with existing PKCS warnings. Live checks verified the link
and empty row are absent at 390/1440px, the homepage search remains visible, and
no overflow or JavaScript errors. Home, Local and health returned 200.
Private deployment files: `/tmp/mydailyfeed-home-local-link-deploy/`.

## Restore homepage Top 25 school logos — October 21, 2026

Restored safe source logos at 22px beside school names, with consistent spacing
and centered alignment. Missing or failed images leave the school name visible.
Rank, School, Record and Previous rank remain the only columns; Big 12 highlighting,
full desktop list and compact mobile scrolling are retained.

Deployed `mydailyfeed-web:ranking-logos-20261021` (484aaad7e785), healthy.
Rollback image `mynewsfeed-web:before-ranking-logos-20261021` (dcddade43463), retained
container `mynewsfeed-web-before-ranking-logos-20261021`. Runtime settings, ports,
mounts and keys preserved; no schema/data changes. HomeRankings tests passed (2).
Docker publish/static-asset guard and diff checks passed with existing PKCS warnings.
Live browser regression passed logos/dimensions, four-column headers, Big 12 markers,
all available polls, full desktop list/mobile height cap, no-JavaScript Apply and
health 200. Verified an actual logo image loaded at 22px and visually inspected
the table; no overflow or JavaScript errors. Private deployment files:
`/tmp/mydailyfeed-ranking-logos-deploy/`.

## Remove Conference from homepage Top 25 — October 21, 2026

Homepage Top 25 now shows Rank, School, Record and Previous rank. Removed Conference,
its column-specific styling, and the unused conference HTTP lookup/cache and related
registration/tests. Narrowed the table's minimum width to suit four columns. Big 12
row tint and left markers remain driven by stored IsBig12, independent of conference
data. Full desktop list, mobile scrolling and poll selection remain intact.

Deployed `mydailyfeed-web:top25-no-conference-20261021` (dcddade43463), healthy.
Rollback image `mynewsfeed-web:before-top25-no-conference-20261021` (c54b1a6215f0),
retained container `mynewsfeed-web-before-top25-no-conference-20261021`. Runtime
settings, ports, mounts and keys preserved; no schema/data changes. HomeRankings
tests passed (2). Docker publish/static-asset guard and diff checks passed with
existing PKCS warnings. Live browser checks passed exact four-column headers,
retained Big 12 markers, all available poll selections, full desktop list/mobile
height cap across 390–1920px and no-JavaScript Apply. Visually inspected the table;
no overflow or JavaScript errors; health 200. Private deployment files:
`/tmp/mydailyfeed-top25-no-conference-deploy/`.

## Show full homepage Top 25 on desktop — October 21, 2026

Removed the homepage ranking table's vertical height cap at desktop widths of
1024px and above, showing all available ranked schools without an internal vertical
scrollbar. Smaller screens retain the 440px-high scrolling list. Five columns,
Big 12 highlighting, poll selection and homepage column layout remain intact.

Deployed `mydailyfeed-web:desktop-top25-20261021` (c54b1a6215f0), healthy.
Rollback image `mynewsfeed-web:before-desktop-top25-20261021` (0da7bd047096), retained
container `mynewsfeed-web-before-desktop-top25-20261021`. Runtime settings, ports,
mounts and keys preserved; no data/schema changes. Docker publish/static-asset
guard and diff checks passed with existing PKCS warnings. Live browser checks passed
available poll selections, last-row visibility without internal vertical scrolling
on desktop, retained mobile height cap, responsive widths 390–1920px, no-JavaScript
Apply and homepage column regression with the taller card. No overflow or JavaScript
errors; health 200. Private files: `/tmp/mydailyfeed-desktop-top25-deploy/`.

## Where to watch search on Home — October 21, 2026

Replaced the small Where to watch link beside Kansas City with a search card above
Today & Tonight in the right desktop column and first in the mobile stack. Its
labeled movie/TV title field submits q to the existing /watch results page. The
field is required with a 120-character limit; normal GET submission works with
Enter/Search and without JavaScript. Existing primary navigation remains available.
Updated homepage ordering and column regression to include the new card.

Deployed `mydailyfeed-web:home-watch-search-20261021` (0da7bd047096), healthy.
Rollback image `mynewsfeed-web:before-home-watch-search-20261021` (cbdeec42d02d),
retained container `mynewsfeed-web-before-home-watch-search-20261021`. Runtime
settings, ports, mounts, keys and watch credentials preserved; no schema changes.
Docker publish/static-asset guard and diff checks passed with existing PKCS warnings.
Live browser checks passed search-card placement at 360/390/1440px, removal of the
inline link, required/length attributes and special-character title submission to
/watch with the query retained, with and without JavaScript. Homepage column,
resize, hidden-section, enhanced navigation and no-JavaScript regression passed.
Visually inspected desktop; no overflow or JavaScript errors; health 200.
Private files: `/tmp/mydailyfeed-home-watch-search-deploy/`.

## Clear Big 12 highlighting and compact homepage poll — October 21, 2026

Homepage Top 25 Big 12 rows now have a full-row accent tint, a left marker and
stronger school text. Previously they shared the standard background and differed
only by a thin border. Scoped homepage table styles now use fixed column proportions,
consistent alignment/padding, tabular numerals and compact typography. School and
Conference align left; numeric columns center. Poll metadata wraps with consistent
spacing; the redundant visual table caption is now screen-reader-only. All five
requested columns remain, with internal scrolling on narrow screens.

Deployed `mydailyfeed-web:poll-style-20261021` (cbdeec42d02d), healthy.
Rollback image `mynewsfeed-web:before-poll-style-20261021` (c2da4328dfda), retained
container `mynewsfeed-web-before-poll-style-20261021`. Runtime settings, ports,
mounts and keys preserved; no data/schema changes. Docker publish/static-asset guard
and diff checks passed with existing PKCS warnings. Live style checks verified
full-row tint, marker, column geometry/alignment and no overflow at
390/768/1024/1440/1920px in both light and dark themes using the real theme toggle.
Visually inspected both themes. Selector, five-column content, ranked-only rows,
scrolling and no-JavaScript Apply regression passed; no JavaScript errors; health 200.
Private files: `/tmp/mydailyfeed-poll-style-deploy/`.

## Homepage Top 25 five-column table — October 21, 2026

Homepage Top 25 now shows exactly Rank, School, Record, Conference and Previous
rank. Removed logos and inline Big 12 badges from School; row highlighting remains.
Previous rank uses stored values, NR for previously unranked schools and an em dash
when unknown. Conference names are supplemented from ESPN season-specific standings
through a six-hour memory cache, with a five-second HTTP timeout, prior-data fallback
and five-minute failure retry. Stored Big 12 membership takes precedence over provider
conference names; unresolved conferences show an em dash. No schema changes or
rankings writes. Dedicated rankings pages are unchanged.

Deployed `mydailyfeed-web:ranking-columns-v2-20261021` (c2da4328dfda), healthy.
Pre-change rollback `mynewsfeed-web:before-ranking-columns-20261021` (2fc0cc192635),
retained container `mynewsfeed-web-before-ranking-columns-20261021`. Intermediate
build (ccf3b8cd07e5) retained as `mynewsfeed-web:before-ranking-columns-v2-20261021`;
final build resolves an older ESPN conference label conflicting with stored Big 12
membership. Runtime settings, ports, mounts and keys preserved. Focused tests passed
(6), covering columns, previous-rank display, Big 12 precedence, conference nesting,
season mismatch and missing metadata. Docker publish/static-asset guard passed with
existing PKCS warnings. Live browser checks passed exact table headers, no logos or
inline badges, populated football conferences, all available selections, ranks 1–25,
internal scrolling, mobile/desktop layouts, no-JavaScript Apply and health 200.
Visually inspected the final table; no overflow or JavaScript errors.
Private files: `/tmp/mydailyfeed-ranking-columns-v2-deploy/` and
`/tmp/mydailyfeed-ranking-columns-deploy/`.

## Homepage Top 25 and standalone F1 removal — October 21, 2026

Removed the standalone homepage F1 card. F1 now appears on Home only inside Today
& Tonight during the existing Friday-through-race-day window. Added Top 25 after
Today’s episodes in the right column, before movies. Its Sport and poll selector
uses the rankings page's enabled/season-eligible sources and carries the selection
in rankingSourceId. View all opens the selected poll on /rankings. The bounded
scrollable table displays ranks 1–25, logos, records and Big 12 highlighting, with
poll dates, freshness warnings, attribution and honest empty/error states. Apply
works without JavaScript. Hidden-section ordering now includes rankings while
preserving the F1 visibility preference for its embedded summary.

Deployed `mydailyfeed-web:home-top25-20261021` (2fc0cc192635), healthy.
Rollback image `mynewsfeed-web:before-home-top25-20261021` (abc5bae5d421), retained
container `mynewsfeed-web-before-home-top25-20261021`. Runtime settings, ports,
mounts and keys preserved; no schema or collector changes. Focused HomeRankings,
Rankings and F1Home tests passed (20). Docker publish/static-asset guard and diff
checks passed with existing PKCS warnings. Live checks passed all available source
selections, matching full-page links, ranked-only rows, bounded scrolling, absence
of standalone F1, no-JavaScript Apply and 390/768/1024/1440/1920px layouts. Homepage
column regression passed dynamic heights, hidden sections, enhanced navigation,
mobile order and no-JavaScript fallback. No overflow or JavaScript errors; visually
inspected Top 25; health 200. Private files: `/tmp/mydailyfeed-home-top25-deploy/`.

## F1 inside Today & Tonight on race weekends — October 21, 2026

F1 moves into Today & Tonight from the Friday preceding the Grand Prix through
race day, inclusive, and the standalone homepage F1 card is omitted during that
window. Outside it, the separate F1 card remains. Placement uses the site's local
calendar day (America/Chicago), including Saturday races and UTC date rollovers;
unknown starts use the published race date. Published results do not end the window
early. Race time, next session, latest result, links, attribution and freshness
warnings remain. F1 summary data is loaded once by Home for either placement, and
F1 read errors retain the existing public fallback. Existing F1 visibility preferences
apply to both placements. No schema or collector changes.

Deployed `mydailyfeed-web:f1-weekend-20261021` (abc5bae5d421), healthy.
Rollback image `mynewsfeed-web:before-f1-weekend-20261021` (f86cdbe1fbfe), retained
container `mynewsfeed-web-before-f1-weekend-20261021`. Runtime settings, ports,
mounts and keys preserved. Focused F1 tests passed (19), including Friday/race-day
boundaries, completed results, Saturday UTC rollovers, unknown times, disabled data
and both component placements. Final Docker publish/static-asset guard and diff
checks passed with existing PKCS warnings. Live checks verified today's single
standalone F1 summary, desktop/mobile race details, no overflow/JavaScript errors
and health 200. Homepage column regression passed across 390–1920px, dynamic heights,
hidden sections, enhanced navigation and no-JavaScript fallback. Weekend placement
was verified with deterministic fixtures; production date/data were not changed.
Private deployment files: `/tmp/mydailyfeed-f1-weekend-deploy/`.

## Smaller homepage movie posters — October 21, 2026

Capped homepage theater and Digital / VOD posters at 120px wide while preserving
the 2:3 portrait ratio (180px high), including missing-image fallbacks. The cap
scales down if its card is narrower. Dedicated movie-page poster styles are unchanged.

Deployed `mydailyfeed-web:home-posters-20261021` (f86cdbe1fbfe), healthy.
Rollback image `mynewsfeed-web:before-home-posters-20261021` (24e4e1037c8f), retained
container `mynewsfeed-web-before-home-posters-20261021`. Runtime settings, ports,
mounts and keys preserved; no schema changes. Docker publish/static-asset guard
and diff checks passed with existing PKCS warnings. Live browser checks verified
poster dimensions and aspect ratio at 390/768/1024/1440/1920px, no overflow or
JavaScript errors and health 200. Visually inspected the homepage movie section.
The theater list was empty during the check; both lists use the same poster class.
Private deployment files: `/tmp/mydailyfeed-home-posters-deploy/`.

## Fixed homepage columns and episode order — October 21, 2026

Latest news and Latest videos now occupy the left desktop column. The right column
shows Today & Tonight, Today’s episodes, F1, then movies/digital releases. Explicit
column positions and independently calculated row spans prevent cards switching
columns. Mobile order is Today & Tonight, episodes, news, videos, F1, movies.
This arrangement supersedes stored ordering while retaining hidden-section and
team preferences. Updated server markup, mobile CSS order and browser ordering.

Deployed `mydailyfeed-web:home-columns-v2-20261021` (24e4e1037c8f), healthy.
Pre-change rollback image `mynewsfeed-web:before-home-columns-20261021`
(7cc3c35d5749), retained container `mynewsfeed-web-before-home-columns-20261021`.
An interim desktop release (232c3f5f0b7c) is retained as
`mynewsfeed-web:before-home-columns-v2-20261021`; final build also corrects legacy
mobile CSS order found during verification. Runtime settings, ports, mounts and
keys preserved; no schema changes. Docker publish/static-asset guard passed with
existing PKCS warnings. Live browser checks passed fixed columns and order at
390/768/1024/1440/1920px, resize transitions, dynamic card height, enhanced Home
navigation, stored-order override, hidden-section collapse and no-JavaScript column
fallback. No overflow or JavaScript errors; visually inspected desktop; health 200.
Private deployment files: `/tmp/mydailyfeed-home-columns-v2-deploy/` and
`/tmp/mydailyfeed-home-columns-deploy/`.

## Enabled Digital / VOD providers and canonical selection — October 21, 2026

Digital / VOD now redirects default, invalid and disabled source selections to the
resolved enabled provider's URL, preferring Movie Insider. With no enabled provider,
a stale source query is removed and the existing empty state is shown. Existing
queries already restrict choices/results to enabled sources, confirmed US digital,
digital_purchase and digital_rental releases from today onward. Existing selected-
provider warnings remain isolated. Production options verified as Movie Insider and
When To Stream; disabled DVDs Release Dates is absent. No provider configuration,
historical data, date-window or schema changes were made by this frontend release.

Deployed `mydailyfeed-web:digital-provider-20261021` (7cc3c35d5749), healthy.
Rollback image `mynewsfeed-web:before-digital-provider-20261021` (9315a715c39c),
retained container `mynewsfeed-web-before-digital-provider-20261021`. Runtime settings,
ports, mounts and keys preserved. DigitalReleases tests passed (2), including
transactional SQL cases for disabled-source isolation, selected-source warning
metadata, empty selected-provider results, no enabled providers and date boundaries.
SQL fixture changes were rolled back. Docker publish/static-asset guard passed with
existing PKCS dependency warnings. Live browser checks passed default/disabled/unknown
URL redirects, both enabled choices, alternate-provider labels and selection, layouts
at 390/1440px, no JavaScript errors and health 200. Scraper collection was not rerun.
Private deployment files: `/tmp/mydailyfeed-digital-provider-deploy/`.

## Align video Share and Watch later — October 21, 2026

Grouped Share and Watch later together on Home and Videos, right aligned with a
4px gap. Removed the save button's independent auto/negative margins inside this
group. Buttons share a centered baseline and stay together when metadata wraps.

Deployed `mydailyfeed-web:video-actions-20261021` (9315a715c39c), healthy.
Rollback image `mynewsfeed-web:before-video-actions-20261021` (1a74d818d4f2), retained
container `mynewsfeed-web-before-video-actions-20261021`. Runtime configuration,
ports, mounts and keys preserved; no schema changes. Docker publish/static-asset
guard and diff checks passed with existing PKCS warnings. Live Home/Videos browser
checks verified adjacent controls, 4px spacing, equal heights and centered alignment
at 390/768/1440px with no overflow or JavaScript errors. Visually inspected the
Home video section. Private deployment files: `/tmp/mydailyfeed-video-actions-deploy/`.

## Share YouTube videos — October 21, 2026

Added the existing article Share control to YouTube cards on Home and Videos.
Video dialogs, including those in Saved, now say Share video. Copy link, mobile
SMS and native app sharing use the existing implementation; native sharing remains
conditional on browser support and a secure context. Article labels are unchanged.

Deployed `mydailyfeed-web:video-share-20261021` (1a74d818d4f2), healthy.
Rollback image `mynewsfeed-web:before-video-share-20261021` (16ae005bec41), retained
container `mynewsfeed-web-before-video-share-20261021`. Runtime configuration,
ports, mounts and keys preserved; no schema changes. Docker publish/static-asset
guard and diff checks passed with existing PKCS warnings. Live desktop/mobile
browser checks passed Home/Videos/Saved video sharing and news article regression:
correct dialog heading/title, copied URL, SMS URL, Escape/focus return and no
horizontal overflow or JavaScript errors. Copy transport was stubbed for assertions;
native OS share sheets were not exercised. Private deployment files:
`/tmp/mydailyfeed-video-share-deploy/`.

## Separate Articles and Videos in Saved — October 21, 2026

Saved now displays Articles followed by Videos as separate sections on one page.
Each keeps newest saves first and shows its own empty message. Legacy bookmarks
without Kind belong to Articles. Existing save/remove and cross-device behavior
are unchanged; no database migration.

Deployed `mydailyfeed-web:saved-sections-20261021` (16ae005bec41), healthy.
Rollback image `mynewsfeed-web:before-saved-sections-20261021` (54748a4fd601),
retained container `mynewsfeed-web-before-saved-sections-20261021`. Runtime settings,
ports, mounts and keys preserved. Docker publish/static-asset guard passed with
existing PKCS dependency warnings. Browser checks passed mixed, article-only,
video-only, empty and legacy lists at 390/768/1440px. Live Watch later regression
passed save, independent-device visibility, reload, removal and button-state sync;
no JavaScript errors, health 200. Test video removed after verification.
Private deployment files: `/tmp/mydailyfeed-saved-sections-deploy/`.

## YouTube Watch later in Saved — October 21, 2026

Added Watch later to YouTube cards on Home and Videos. The existing shared Saved
list now combines articles and videos; video cards show a YouTube label, channel,
thumbnail and publication date, and open YouTube in a new tab. Saved navigation
and empty-state copy describe both content types. Existing article bookmarks remain
compatible; optional Kind metadata defaults to article. The existing 500-item limit,
CSRF protection, cross-device polling and persistence apply to both types. This is
independent of the YouTube account's Watch Later list. No schema changes.

Deployed `mydailyfeed-web:watch-later-20261021` (54748a4fd601), healthy.
Rollback image `mynewsfeed-web:before-watch-later-20261021` (2f8b334e4277), retained
container `mynewsfeed-web-before-watch-later-20261021`. Runtime configuration,
mounts, ports and keys preserved. Focused SharedArticles/Videos tests passed (36);
Docker publish/static-asset guard passed with existing PKCS dependency warnings.
Live browser checks passed actual video saving, channel/thumbnail/type display,
independent-device visibility, reload persistence, removal and button-state sync.
Home/Videos/Saved fit 390/768/1440px without overflow or JavaScript errors.
Shared Saved browser regression also passed import/replay protection, polling, CSRF,
unsafe links, blocked storage, failure/retry and offline recovery; test saves removed.
Health and public runbook routes returned 200; unauthenticated admin returned 302.
Private deployment files: `/tmp/mydailyfeed-watch-later-deploy/`.
Source committed locally after deployment, together with the previously
deployed navigation logo and homepage/Settings/TV updates. GitHub push is pending
because this workspace currently has no working GitHub authentication.

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

## Refill filtered viewing pages — October 20, 2026

Where to watch now fills logical pages with up to 20 eligible titles from
successive provider batches instead of leaving holes after US availability
filtering. A bounded 15-minute query window retains eligible membership and
provider order, deduplicates IDs and carries surplus titles into subsequent
pages. Next/Previous slice this shared sequence; sorts and service preferences
apply afterward without changing page membership or mutating the shared window.
Short final pages remain valid. Active-TV browse filtering and all-status title
search remain unchanged.

The cache holds up to 32 query windows. Collection scans at most ten new provider
batches per request, 50 per window, and retains the existing ten display-page cap
and four-request upstream concurrency limit. Large/sparse searches or source
failures expose Continue loading this page rather than skipping unseen titles.
Next is offered only when another eligible title is known; provider failures
remain distinct from empty results. Direct links and expired/evicted windows may
rebuild against current provider data. Service badges are re-evaluated separately
without silently dropping cached page members mid-window.

Verification: 25 focused tests passed, covering refill, no repeats/skips,
concurrent readers, stable previous pages, short final pages, source failure and
bounded continuation. Live TV/movie pages contained 20 titles; two TV pages had
40 distinct titles with identical contents on back navigation. Ended-title search,
mobile layout, full watch/sort suites and US-options checks passed; all 60 checked
result cards had US offers. Runtime settings preserved; container/private HTTPS
health passed. Existing PKCS warnings remain. No database/collector change or Git
commit/push was performed.

Release: mydailyfeed-web:fill-pages-20261020 (57976dba3956).
Rollback: mynewsfeed-web:before-fill-pages-20261020 (18cf4b587629).
Retained container: mynewsfeed-web-before-fill-pages-20261020.
Private deployment files: /tmp/mydailyfeed-fill-pages-deploy/.

## Hide titles without US viewing options — October 20, 2026

Where to watch browsing and search now omit titles whose successful provider
response confirms no usable US subscription/free/ads/rental/purchase offers.
Filtering runs even without service preferences, reusing bounded cached provider
checks. Rentals/purchases qualify for visibility but still do not count as included
access on selected services. Theater-only entries do not qualify. Failed or
malformed availability checks remain visible as unknown, with a retry-oriented
availability note, rather than being treated as no options. Shared cached listing
order remains unchanged. Empty filtered pages retain pagination; pages explain
that fewer titles may remain. Direct detail routes keep their honest no-options
fallback for saved links or changed catalogs.

Verification: 21 focused service/sort tests passed, including missing US regions,
empty lists, theater-only results, rental-only access, malformed/provider failure,
cache reuse and untouched shared listings. Local and live checks verified US
options in 36 retained movie/TV/search cards. Full watch and sort browser suites
passed six modal layouts, error/fallback, keyboard/focus, state preservation and
no-JavaScript forms/detail. Container/private HTTPS health passed; runtime settings
preserved. Existing PKCS build warnings remain. No schema/collector change or Git
commit/push was performed.

Release: mydailyfeed-web:us-options-20261020 (18cf4b587629).
Rollback: mynewsfeed-web:before-us-options-20261020 (ef6f7f3e7cdb).
Retained container: mynewsfeed-web-before-us-options-20261020.
Private deployment files: /tmp/mydailyfeed-us-options-deploy/.

## Where to watch sorting — October 20, 2026

Added a Sort this page selector: Your services first (default), Title A–Z,
Newest first and Most popular. Default service groups are alphabetized internally;
without service selections the default is alphabetical for the loaded page.
Other modes sort all loaded titles independently of service badges. Newest uses
full movie release/TV premiere dates, popularity uses TMDB's numeric value, and
unknown metadata sorts last. Ties are deterministic. Shared cached listings are
not mutated. Tabs, genre/page links, search and Clear search preserve the sort;
plain GET forms support sorting without JavaScript. This is per-page sorting,
not a global catalog-wide ordering or a new filter.

Verification: 20 focused provider/sort tests passed, covering date/popularity
parsing, all sort modes, service-group alphabetization, unknown metadata and
unchanged cached order. Local and live sorting checks passed A–Z/group order,
state preservation, mobile and no-JavaScript forms. Full live watch regression
checks passed six modal layouts, search, empty/error/fallback and keyboard/focus.
Container/private HTTPS health passed; runtime settings preserved. Existing PKCS
build warnings remain. No schema/collector change or Git commit/push was made.

Release: mydailyfeed-web:watch-sort-20261020 (ef6f7f3e7cdb).
Rollback: mynewsfeed-web:before-watch-sort-20261020 (d04df3e91825).
Retained container: mynewsfeed-web-before-watch-sort-20261020.
Private deployment files: /tmp/mydailyfeed-watch-sort-deploy/.

## Active TV browsing — October 20, 2026

Where to watch TV popular/genre browsing now requests TMDB statuses returning,
planned, in production and pilot (with_status=0|1|2|5). Ended/canceled series are
excluded upstream before pagination and service prioritization. No date-based
heuristic is used; older ongoing shows remain eligible. Nonblank title search
retains ended/canceled shows. Movie browsing, direct details and tracked-series
records are unchanged. The TV browse page explains the distinction.

Verification: 15 focused provider tests passed, including filter application to
TV browse/genres/pages and exclusion from title search/movie requests. Local and
live browser checks validated 40 displayed TV statuses against TMDB across two
browse/genre pages, confirmed The Mentalist remains searchable and movie browsing
is unaffected. The full live watch suite passed search/error/fallback states,
six modal layouts, keyboard/focus and no-JavaScript detail. Runtime settings were
preserved; container/private HTTPS health passed. Existing PKCS warnings remain.
No database/collector changes or Git commit/push were performed.

Release: mydailyfeed-web:active-tv-20261020 (d04df3e91825).
Rollback: mynewsfeed-web:before-active-tv-20261020 (f138f628aeac).
Retained container: mynewsfeed-web-before-active-tv-20261020.
Private deployment files: /tmp/mydailyfeed-active-tv-deploy/.

## Public tracked-series additions — October 20, 2026

At the user's explicit request, adding a TV title no longer requires admin sign-in.
Where to watch links to /tv/track with title/year context. This public interactive
page confirms the TVmaze match and adds it to the shared site-wide tracked list.
The shared SeriesFollowForm preserves provider verification, membership in the
search results, existing duplicate/re-enable handling and an explicit Follow click.
Success stays on the public page with a confirmation. GET/search alone does not
write tracking data. Movies remain excluded. Existing /admin routes, including
tracking removal/management, retain their authorization.

Five focused link/TV tests passed. Local and live browser checks verified anonymous
modal-to-track navigation, prefilled TVmaze results, movie exclusion and mobile
layout; the admin list still requires authentication. Browser checks did not add
any production series. The full live watch suite passed six layouts and existing
search/error/keyboard/no-JS detail behavior. Actual following remains an interactive
server action. Container/private HTTPS health passed; runtime settings preserved.
Existing PKCS build warnings remain. No Git commit/push was made.

Release: mydailyfeed-web:public-track-20261020 (f138f628aeac).
Rollback: mynewsfeed-web:before-public-track-20261020 (31b922fe465b).
Retained container: mynewsfeed-web-before-public-track-20261020.
Private deployment files: /tmp/mydailyfeed-public-track-deploy/.
This supersedes the prior sign-in requirement for adding series, not admin access.

## Track TV titles from Where to watch — October 20, 2026

TV title detail/modal now offers Add to tracked series. It opens the existing
protected tracking page with a URL-encoded title and premiere-year context. The
admin page automatically searches TVmaze; the reader confirms the correct series
and taps Follow. Existing Following/duplicate/re-enable behavior is retained.
Movies do not offer tracking. A TMDB ID is never used as a TVmaze identity, and
opening the link does not write a tracking row. Existing admin sign-in and return
URLs remain required; episode collection starts on the scraper's usual next run.

Verification: five focused link/TV tests passed, including title encoding,
movie exclusion and existing tracking behavior. Local and live browser checks
passed TV modal navigation, protected sign-in return, prefilled query/year and
automatic TVmaze result rendering. The browser checks did not follow a production
series. The full live watch suite also passed six layouts, search, error/fallback,
keyboard/focus and no-JavaScript detail. Container/private HTTPS health passed;
runtime settings were preserved. Existing PKCS build warnings remain.

Release: mydailyfeed-web:track-series-20261020 (31b922fe465b).
Rollback image: mynewsfeed-web:before-track-series-20261020 (7f5ed6f6fc38).
Retained container: mynewsfeed-web-before-track-series-20261020.
Private deployment files: /tmp/mydailyfeed-track-series-deploy/.
No schema/collector change or Git commit/push was performed.

## MyDailyFeed project rename — October 20, 2026

Renamed the application, .NET solution, web/test project folders and files,
namespaces, page titles, navigation brand, manifest and offline screen to
MyDailyFeed. New DF icons replace the NF icons in current manifest/favicon/offline
references. Solution: MyDailyFeed.sln; projects: src/MyDailyFeed.Web and
tests/MyDailyFeed.Tests. Docker publishes/starts MyDailyFeed.Web.dll. Current
project-path references in documentation and tests were updated.

Compatibility: existing browser preference keys, saved articles, theme/team/service
choices, Settings order, admin cookie name, data-protection application name and
user-secrets ID are retained. Manifest id/scope/start URL are unchanged. Existing
workspace folder/GitHub remote, private hostname, operational container/network
identity and persistent key volume are retained. No remote repository rename,
database/schema change, Git commit or push was performed. Historical deployment
identities and prior-name source handoffs remain historical evidence.

Verification: all 593 .NET tests passed. Live browser suites passed branding on
Home/Settings/Watch/Movies/TV, new manifest/icons/offline page, legacy preferences,
Settings navigation/order/reset, watch search/modal six layouts and sports-modal
regressions. Private HTTPS health returned 200. Runtime settings were preserved,
with the entrypoint intentionally updated to dotnet MyDailyFeed.Web.dll. Existing
PKCS build warnings remain. Installed-app name/icon updates follow browser refresh
behavior; existing tabs may need a full refresh to load the new branding.

Release: mydailyfeed-web:rename-20261020 (7f5ed6f6fc38).
Rollback image: mynewsfeed-web:before-rename-20261020 (dc9adef11982).
Retained container: mynewsfeed-web-before-rename-20261020.
Private deployment files: /tmp/mydailyfeed-rename-deploy/.

## Settings controls after in-site navigation — October 20, 2026

Fixed the empty Arrange Settings sections panel reported in the user's screenshot.
Reproduced zero move buttons after navigating Home → Settings, while direct-load
checks had passed. Document enhancedload listeners were not the Blazor navigation
hook. Settings ordering now uses Blazor.addEventListener plus an idempotent DOM
observer for replacement/late content. The same hook repair restores service
preferences after enhanced navigation. No preference keys or values were changed.

Regression tests now enter Settings from Home and Where to watch, verify all 12
move buttons, perform moves, reload/reset, handle malformed storage and check
mobile overflow. Live service checks also navigate Settings → Where to watch →
Settings and verify restored selections. Both suites passed locally and live.
Container/private HTTPS health passed; runtime configuration was preserved.
No Git commit/push was performed. Existing PKCS build warnings remain.

Release: mynewsfeed-web:settings-nav-20261020 (dc9adef11982).
Rollback: mynewsfeed-web:before-settings-nav-20261020 (896acc615bae).
Retained container: mynewsfeed-web-before-settings-nav-20261020.
Private deployment files: /tmp/mynewsfeed-settings-nav-deploy/.

## Settings services and section order deployment — October 20, 2026

Deployed mynewsfeed-web:settings-services-20261020 (896acc615bae) under the user's
explicit session-wide deployment authorization. My services now lives in Settings
with a management shortcut from Where to watch. Appearance, color scheme, home,
reading, services and teams can be moved up/down independently of homepage order;
the order persists in this browser with a restore-default action. Services save
without leaving Settings, and a GET/link fallback remains available without JS.
The prior theater/ticketing exclusion is included; home-viewing services remain.

Verification: 12 focused provider tests passed before release. Live service tests
passed persistence/ranking, TV navigation, clearing, mobile and no-JS selection;
Settings tests passed reorder/reload/reset, malformed storage and phone overflow.
The full live watch suite passed search/genres, error/invalid/empty states, six
modal width/theme layouts, credits, keyboard/focus and no-JavaScript detail.
Explicit live checks confirmed Fandango ticketing excluded while Fandango At Home
and AMC+ remain. Container/private HTTPS health passed; environment, user, command,
health check, mounts, ports, restart policy and networks were preserved. Existing
PKCS build warnings remain. No Git commit/push was performed.

Rollback image: mynewsfeed-web:before-settings-services-20261020 (9ac98a4e5365).
Retained container: mynewsfeed-web-before-settings-services-20261020.
Private snapshots/scripts: /tmp/mynewsfeed-settings-services-deploy/.
This supersedes pending/local-only statuses below.

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

## Main navigation and service preferences deployment — October 20, 2026

User lifted the feedback deployment hold. Deployed the main-navigation link and
My services selectors as mynewsfeed-web:watch-services-20261020 (9ac98a4e5365).
Where to watch is outside the Entertainment group on desktop and in the primary
mobile links. Preferences prioritize verified subscription/free/ads matches on
each results page without hiding other titles or counting rentals as included.

Four focused service tests passed before release. Live browser checks passed the
main-nav placement, selection and matching badges, browser persistence, Movie/TV
navigation, clearing, phone overflow and no-JavaScript forms. The full live watch
suite also passed search/genres, empty/error/invalid states, six modal width/theme
layouts, credits, keyboard/focus and no-JavaScript detail fallback. Container and
private HTTPS health passed; runtime settings were preserved. Existing PKCS build
warnings remain. No Git commit/push was performed for this deployment.

Rollback image: mynewsfeed-web:before-watch-services-20261020 (9d863862bcc2).
Retained container: mynewsfeed-web-before-watch-services-20261020.
Private deployment files: /tmp/mynewsfeed-watch-services-deploy/.
This supersedes the local-only/hold status in the feedback entry below.

## Where to watch feedback — October 20, 2026 (local only)

Moved Where to watch out of Entertainment into the main desktop/mobile navigation.
Added remembered service checkboxes and per-page prioritization of verified
subscription/free/ad-supported matches, with service badges and unchanged access
to other titles. Selections persist through movie/TV/search/genre/page navigation;
rental/purchase-only availability is excluded from matches. Shared cached listings
retain their base ordering. Four focused service tests and local browser checks
passed, including real badges, persistence, clearing, mobile and no-JavaScript
selection. User explicitly requested no deployment while providing feedback;
these changes remain local and do not change the latest live release below.

## Where to watch — October 20, 2026

Implemented and deployed the user-authorized movie/TV streaming lookup at /watch,
linked from Entertainment on desktop/mobile and from movie release cards. Typed
title search is an explicit scoped exception to the tap-to-link direction; Movies/
TV tabs, popular titles and common genres support browsing without typing.
Titles open in a native modal with keyboard/focus return and a normal detail-page
fallback. US availability separates subscription, free, ads, rental and purchase
services, with checked time and series-level availability caveats for TV.

Uses the existing server-only Movies:AccessToken for TMDB, with bounded memory
caching and four concurrent upstream requests. The supplied TMDB viewing-options
link is used because provider deep links are not supplied. No service destinations
or prices are fabricated. TMDB logo/API notice and JustWatch attribution appear
in Data credits. Provider errors and absent offers have distinct messages. No
scraper, database or schema changes were made. The Blazor link-handling interaction
was fixed by intercepting only watch-title clicks in capture phase.

Verification: three provider-client tests passed (encoded/bounded search, cache,
adult filtering, US categories, invalid identities and provider failure). Live
browser checks passed movie/TV search, genres, empty/error/invalid states, six
width/theme modal layouts, credits, Escape/focus and no-JavaScript fallback. The
existing sports-modal regression suite also passed. Private HTTPS /healthz and
search returned 200; runtime environment, user, commands, health check, mounts,
ports, restart policy and networks were preserved. Existing PKCS build warnings
remain. No Git commit/push was requested for this update.

Release: mynewsfeed-web:watch-20261020 (9d863862bcc2).
Rollback: mynewsfeed-web:before-watch-20261020 (7099cb8225a4).
Retained container: mynewsfeed-web-before-watch-20261020.
Private deployment snapshots/scripts: /tmp/mynewsfeed-watch-deploy/.

## Basketball template deployment — October 19, 2026

Deployed as mynewsfeed-web:basketball-20261019 (7099cb8225a4). Container health
and private HTTPS /healthz passed. Environment, user, commands, health check,
mounts, ports, restart policy and networks match the prior release.

Verification: 17 focused tests and six championship fixture width/theme layouts
passed before release. Four live WNBA width/theme layouts passed using an eligible
cached result, plus the general Results/MLS/Championship modal regression suite
covering keyboard/focus, six layouts, error handling and no-JavaScript fallback.
The development championship route remains HTTP 404; no historical game/cache row
or visibility exception was created. Existing PKCS build warnings remain.

Rollback image: mynewsfeed-web:before-basketball-20261019 (03359e7c594c).
Retained container: mynewsfeed-web-before-basketball-20261019.
Private snapshot/scripts: /tmp/mynewsfeed-basketball-deploy/.
This supersedes the local-only status below. User requested Git check-in after
release; see Git history for the resulting commit.

## Basketball player template — October 19, 2026 (local update)

Added paired basketball team player tables on desktop, stacked on mobile, using
source columns and team identities. All player groups, positions, starter/bench
flags and supplied totals are retained. Missing totals show an em dash; shooting
strings and real zeroes remain unchanged. Wide tables scroll internally, with
player names held in view. Existing period and team-stat sections remain intact;
team-only snapshots do not show an empty player panel.

The supplied championship JSON is a development fixture only. No production row,
detail visibility exception, provider request or cache write was added. Tests
parse the actual sample, reverse player-group order to verify team association,
and validate 17 players, 13 columns, half labels and complete supplied totals.
Verification: 17 focused box-score tests and six fixture width/theme layouts passed.
No deployment or Git commit/push was performed for this update.

## NHL detail visibility and repaired period labels — October 19, 2026

Deployed `mynewsfeed-web:nhl-periods-20261019` (`03359e7c594c`). NHL cached detail
now permits valid final games from the enabled NHL source's latest validated
stored season, respecting a configured source team restriction. No source IDs or
season numbers are hardcoded. This checkout has no dedicated NHL season-results
query, so the new bounded visibility helper follows the existing MLS/Championship
stored-season approach. Ordinary followed results and soccer paths are preserved;
cache existence alone does not grant access.

The repaired version-1 string labels render without a contract change. Baseball
now falls back to labeled per-team inning tables when sequences differ, preserving
sparse or unknown entries without shifting values beneath shared column labels.
No final scores are calculated from periods; NHL SO values remain provider values.

Verification: 16 focused box-score/visibility tests passed, including disabled
sources, stale seasons, source team scope, invalid/non-final games, OT/SO finals,
and sparse innings with zero/unplayed values. Seven repaired production detail
routes passed browser checks, including NHL `401892525`; four NHL width/theme
layouts, eight baseball layouts and the general modal regression suite passed.
Private HTTPS health and NHL detail returned 200. Runtime environment, user,
command, mounts, ports, restart policy and networks were preserved. Existing PKCS
build warnings remain. No collector/schema change or Git commit/push was made.

Rollback image: `mynewsfeed-web:before-nhl-periods-20261019` (`3570ee3785ab`);
retained container: `mynewsfeed-web-before-nhl-periods-20261019`.
Private deployment files: `/tmp/mynewsfeed-nhl-periods-deploy/`.

## Scraper period-label handoff — October 19, 2026 (documentation only)

Added [period-label repair instructions](../new%20development/sports-period-labels-scraper-handoff.md)
covering MLB, NFL/college football, NHL, men's college basketball and existing
soccer leagues. Defines the compatible JSON field, source investigation, safe
period mapping, overtime/shootout distinctions, historical backfill and return
verification requirements. No application, scraper or deployment change was made.

## Sports template deployment — October 19, 2026

Deployed the baseball, soccer and football templates together as
`mynewsfeed-web:sports-templates-20261019` (`3570ee3785ab`). Container health and
private HTTPS home/health checks passed. Existing environment, user, command,
health check, mounts, network, ports and restart policy were preserved.

Live verification passed eight baseball layouts, six soccer modal layouts, six
football modal layouts, and the general Results/MLS/Championship modal regression
suite (six width/theme layouts, keyboard/focus, error/invalid identity and
no-JavaScript fallback). Fourteen focused .NET box-score tests passed before
release. The build retains the existing PKCS dependency warnings.

Rollback image: `mynewsfeed-web:before-sports-templates-20261019`
(`40defb4164bd`); retained container:
`mynewsfeed-web-before-sports-templates-20261019`.
Private deployment snapshot/scripts: `/tmp/mynewsfeed-sports-templates-deploy/`.
This deployment supersedes the local-only status in the three template entries
below. No Git commit or push was performed.

## Football category template — October 19, 2026 (local update)

Added category-first football player statistics: passing, rushing, receiving,
fumbles, defense, interceptions, returns, kicking and punting when collected.
Teams appear side by side on wide screens and stack on phones. Additional cached
categories, source column order, supplied totals, positions and starter metadata
remain intact. Existing team-stat comparisons and period sections are retained.
The supplied NFL ESPN URL returned a verification screen, so this adapts the
requested category structure without claiming an exact visual replica. The layout
also supports cached college-football groups without NFL-specific assumptions.

Verification: 14 focused box-score tests passed, including category ordering,
paired teams, extra groups, source values and encoded provider text. Six NFL modal
width/theme checks passed for desktop pairing, mobile stacking, overflow and
Escape/focus return. This update has not been deployed or committed.

## Soccer team-comparison template — October 19, 2026 (local update)

Added a soccer-specific team comparison to the existing modal and fallback page:
club values flank shared statistic labels, with all available statistic groups
visible immediately. Values align by group/key, preserving zeroes and precision;
missing values remain em dashes. Existing period and player detail is retained.
The supplied ESPN reference URL returned a verification screen during inspection;
this is a comparison layout inspired by the request, not a verified exact replica.

Verification: 13 focused box-score tests passed, including reordered statistics,
group-key collisions, missing values and encoded club names. Six soccer modal
width/theme checks passed, covering visible comparisons, row alignment, overflow,
Escape dismissal and focus return. This update has not been deployed or committed.

## Baseball box-score reference template — October 19, 2026 (local update)

Adapted the supplied baseball reference for the existing statistics modal and its
standalone fallback page. An R/H/E scoreboard leads paired hitting and pitching
tables on desktop, stacked on mobile. Primary columns follow the reference;
additional collected columns and team details expand on tap. Other sports retain
their existing layouts. All values and supplied totals retain their source indices.
Unknown inning labels remain explicit; decisions, headshots and scoring summaries
are omitted because the normalized cache does not supply them.

Verification: 12 focused box-score tests passed, eight baseball width/theme layouts
passed, and the existing modal regression checks passed across Results, MLS and
Championship, including keyboard/focus, six width/theme layouts, error handling and
no-JavaScript fallback. This update has not been deployed or committed.

## Product interaction direction — October 19, 2026 (documentation only)

Recorded the user's standing direction: **“This is a tap to link site and not a
typing one.”** Public browsing should use direct links, cards, tabs, dropdowns and
preset selections, without requiring typed queries. Future work must not reintroduce
global Search or text-entry-driven public features unless explicitly requested.
The guideline is recorded in README, the interface guide and the chat handoff.
Keyboard accessibility and necessary administration/sign-in input remain supported.
No application change or deployment was required for this documentation update.

## Cached sports box-score modals — October 19, 2026

Implemented the sports box-score frontend handoff using the read-only
`dbo.SportsBoxScores` view. Daily Results, College Results, MLS and Championship
season results now offer Box score for usable cached player detail and Game stats
for team-only detail. Validated composite identities use Sport/League/EventId;
missing, corrupt, unsupported or empty snapshots retain the existing result.
Availability is read in one bounded batch per listing, never per rendered row.

Tapping opens a native modal with persistent Close, Escape, focus containment and
restoration, expandable team/period sections and internally scrolling player
tables. A regular detail route supports modified clicks and no JavaScript.
Repeated unspecified periods remain separate; no inning/quarter/set numbers or
missing statistics are inferred. Provider text is encoded; optional ESPN links
require a matching event identity. Existing score/record sources, shootouts,
provider destinations, filters and pagination are preserved. Results auto-refresh
waits while a dialog is open. No new request-time provider scraping is performed.

Verification: all 575 .NET tests passed, followed by 11 focused parser/identity/
URL/render checks after final layout refinements. Read-only runtime-identity
probes confirmed SELECT access in WebScraper and rendered cached NFL, MLB, WNBA,
college-football, MLS and NWSL samples. Predeployment comparison preserved 21 daily
scores and 25 each MLS/Championship scores/provider destinations (dated samples).
Local and production browser checks passed the three result entry points, modal
open/close, keyboard/focus restoration, six light/dark layouts at 390/768/1440px,
internal table scrolling, error handling, invalid/missing IDs and no-JavaScript
navigation. Private HTTPS phone checks passed Results, MLS and Championship modals
and health HTTP 200. Docker is healthy; no runtime errors or optional box-score
read failures were observed. Publish/static-asset guard passed; existing package
and analyzer warnings remain.

Live runtime environment, identity, mounts/keys, ports, networks and restart
settings were preserved. No SQL writes/migrations, scraper changes, new credentials
or Git commit/push were performed. Existing cache coverage varies by game; backend
handoff counts are historical observations, not hardcoded UI coverage.

Release: `mynewsfeed-web:boxscores-20261019` (`40defb4164bd`).
Rollback: `mynewsfeed-web:before-boxscores-20261019` (`beae679b6be7`).
Previous stopped container: `mynewsfeed-web-before-boxscores-20261019`.
Private snapshots/scripts/screenshots: `/tmp/mynewsfeed-boxscores-deploy/` and
`/tmp/mynewsfeed-boxscores-review/`.

## Customize Home mobile button alignment — October 19, 2026

Fixed independent flex wrapping that placed Move up beside a section label while
Move down fell onto the next line. Customize Home now uses consistent grid columns:
phone labels span the full row, with equal-width Move up/Move down buttons beneath;
larger screens keep labels and two fixed button columns together. Touch targets
remain at least 44px; disabled first/last movement buttons have a clear dimmed state.

Local and production browser checks passed 14 viewport/theme combinations at
320/375/390/430/600/768/1440px in light/dark mode. Checks verified paired buttons,
matching widths and column positions across all six rows, touch-target size, no
overflow, reorder persistence and disabled ordering endpoints. Private HTTPS
mobile alignment and health HTTP 200 passed. Docker is healthy; release publish
and static-asset guard passed. This was a CSS-only implementation change; no new
unit tests were added. Live runtime settings were preserved, and the prior
container/image remains available for rollback. No database, collector or Git
commit/push changes.

Release: `mynewsfeed-web:home-controls-20261019` (`beae679b6be7`).
Rollback: `mynewsfeed-web:before-home-controls-20261019` (`d6789294eec0`).
Previous stopped container: `mynewsfeed-web-before-home-controls-20261019`.
Private scripts/screenshots/snapshots: `/tmp/mynewsfeed-home-controls-deploy/`.

## Remove Plan My Week — October 19, 2026

Removed the combined My Week agenda at the user's request, including Home's
“Plan My Week” action, desktop/mobile My Week navigation, Local's weekly sports
link, agenda-only browser preferences/styles and calendar downloads. `/week` and
`/calendar/week.ics` now return the standard 404 page. Individual sports, F1,
college, TV and movie schedules remain available in their existing sections.
Home customization, personal team highlights/filtering, news reading controls,
Kansas City Local and weather icons remain available.

Verification: solution build and all 564 remaining .NET tests passed; obsolete
agenda/calendar tests were removed while bounded listing-filter checks remain.
Local browser interaction and 60 light/dark route/viewport checks passed at
390/768/1180/1280/1440px. Production interaction checks passed retired routes,
removed navigation, Home preferences, reading/Saved state, cross-tab updates,
blocked storage and no-JavaScript browsing. Private HTTPS checks passed retained
routes, weather icons, retired route 404s and health HTTP 200. Docker is healthy;
release publish/static-asset guard passed. Live runtime environment, identity,
mounts/keys, ports, networks and restart settings were preserved. No schema,
collector or Git commit/push changes.

Release: `mynewsfeed-web:without-week-20261019` (`d6789294eec0`).
Rollback: `mynewsfeed-web:before-without-week-20261019` (`e6c63fbe9b35`).
Previous stopped container: `mynewsfeed-web-before-without-week-20261019`.
Private snapshots/scripts: `/tmp/mynewsfeed-no-week-deploy/`.

## Remove site-wide Search — October 19, 2026

Removed the global Search page, header magnifying-glass link, mobile Search link
and cross-content query implementation at the user's request. `/search`, including
bookmarked query URLs, now returns the standard 404 page. Existing section-level
News, YouTube, movie and tracked-series filtering remains available; its bounded
text helper now lives in `QueryText`. My Week, calendar downloads, personal Home,
reading controls and Kansas City Local/weather icons remain available.

Verification: solution build and all 571 remaining .NET tests passed (the obsolete
global-search integration test was removed). Local browser interaction and 60
light/dark responsive route checks passed at 390/768/1180/1280/1440px. Production
interaction checks passed removed Search/Watchlist entry points, retired-route
404s, team-filtered agenda, calendars, Home preferences, reading/Saved consistency,
cross-tab state, blocked storage and no-JavaScript browsing. Private HTTPS checks
passed six existing routes without Search links, real weather icons, Search 404
and health HTTP 200. Docker reports healthy. Release publish/static-asset guard
passed; runtime environment, identity, keys/mounts, ports, networks and restart
settings were preserved. No schema, scraper or Git commit/push changes.

Release: `mynewsfeed-web:without-search-20261019` (`e6c63fbe9b35`).
Rollback: `mynewsfeed-web:before-without-search-20261019` (`a16338cf3354`).
Previous stopped container: `mynewsfeed-web-before-without-search-20261019`.
Private runtime snapshots/scripts: `/tmp/mynewsfeed-no-search-deploy/`.

## Kansas City weather icons — October 19, 2026

Forecast cards now show local SVG condition artwork beside the temperature,
including day/night clear and partly cloudy icons, clouds, rain, thunderstorms,
snow, freezing conditions, mixed precipitation, fog and wind. The NWS day/night
flag is retained in the cached API response. Unknown conditions use a thermometer;
written forecasts remain visible and decorative icons are hidden from assistive
technology. Constant local SVG paths avoid external icon downloads and provider
text is never interpreted as SVG markup. Light/dark palettes are supported.

Verification: solution build and three weather API/cache/fallback tests passed.
Local and production browser checks passed day/night mapping, storm priority,
unknown-condition text safety, light/dark layouts at 390/768/1440px and unavailable
weather fallback. Private HTTPS checks confirmed four icons with the real NWS
forecast and health HTTP 200. Docker is healthy with no runtime error entries
observed. Publish/static-asset guard passed; runtime environment, identity, keys,
mounts, ports, networks and restart settings were preserved. Existing publish
package warnings remain. No schema, scraper or Git commit/push changes.

Release: `mynewsfeed-web:weather-icons-20261019` (`a16338cf3354`).
Rollback: `mynewsfeed-web:before-weather-icons-20261019` (`6908e06c9777`).
Previous stopped container: `mynewsfeed-web-before-weather-icons-20261019`.
Private snapshots/scripts: `/tmp/mynewsfeed-weather-icons-deploy/`.

## Remove Watchlist — October 19, 2026

Removed the Watchlist page, desktop/mobile/Home navigation links, movie/TV/video
save buttons, YouTube Watch Later and episode watched/progress controls at the
user's request. Existing browser-local watchlist data is unused; no saved articles
or team selections are cleared.

My Week's optional filter is now **Only my teams in Sports**. It filters sports
fixtures by existing team aliases while keeping TV, Movies and F1 available.
Calendar downloads, Search, homepage preferences, news reading controls, Kansas
City Local, tracked-series browsing and normal movie/video destinations remain.
Removed watchlist-only browser handlers, styles and agenda metadata; ordinary
`/watchlist` requests now return the standard 404 page.

Verification: all 572 .NET tests passed. Browser checks passed removal from all
entry points, the retired route, team filtering with other content retained,
calendar downloads, homepage settings, read/Saved consistency, cross-tab changes,
blocked storage and no-JavaScript browsing.

Docker publish/static-asset guard passed; production is healthy and private HTTPS
`/healthz` returned 200. Production browser checks passed removed entry points,
retired-route 404, sports-only team filtering, calendars, Home preferences,
reading/Saved consistency, cross-tab updates, blocked storage and no-JavaScript.
All 60 production responsive/theme route checks passed at
390/768/1180/1280/1440px in light/dark mode. No runtime error entries were
observed. Private HTTPS checks passed Home, My Week, YouTube and TV without removed controls.
Live environment, identity, mounts/keys, ports, networks and restart settings were
preserved. No schema, collector or Git commit/push changes were made.

Release: `mynewsfeed-web:without-watchlist-20261019` (`6908e06c9777`).
Rollback: `mynewsfeed-web:before-without-watchlist-20261019` (`74b9a7422350`).
Previous stopped container: `mynewsfeed-web-before-without-watchlist-20261019`.
Private runtime snapshots/scripts: `/tmp/mynewsfeed-no-watchlist-deploy/`.

## Personal planning and discovery — October 19, 2026

Implemented a connected first pass of all seven requested additions:

- My Week (`/week`): seven-day sports/F1/TV/movie agenda, native expandable days,
  content filters and optional selected-teams/watchlist scope.
- Movie/TV watchlists, individual episode watched status and YouTube Watch Later
  (`/watchlist`), with browser-local saved copies and progress controls.
- Search (`/search`) across collected news, videos, movie catalogs and tracked TV.
- Homepage section visibility/order and optional selected-team-only games.
- Single-item and full-week `.ics` calendar downloads, retaining date-only entries
  and excluding estimated releases.
- News read/unread controls shared with Saved, Hide read, publisher muting and a
  remembered preferred category, managed in Settings.
- Kansas City Local (`/local`): labelled city keyword matches until an enabled
  `Kansas City News` category exists, cached NWS forecasts and external Visit KC /
  city event calendars. No source registrations or scraper schema changes.

Verification: solution build passed; all **572 .NET tests** and **5 solar tests**
passed. Browser interaction checks passed watchlist saving/progress/removal, episode
status, personal agenda filtering, calendar download, homepage order/visibility,
reading/muting/preferred category, Saved status consistency, cross-tab updates,
unsafe stored-content handling, blocked storage and no-JavaScript browsing.
**60 route/theme/viewport checks** passed across six routes, light/dark and
390/768/1180/1280/1440px without horizontal overflow or JavaScript errors.
Existing xUnit analyzer warnings remain. A transient preview read deadlock occurred
while rollback-based SQL integration fixtures ran concurrently; all tests passed,
and a subsequent Settings read succeeded after the fixtures completed.

Deployed with a successful Docker release publish and static-asset guard.
Production checks passed thirteen routes, 25 viewport checks across
390/768/1180/1280/1440px, Watch Later, `.ics` downloads and the weather endpoint
(returning four forecast periods). Private HTTPS browser checks passed My Week,
Local and Search; HTTPS `/healthz` returned 200. Docker reports healthy; no runtime
error entries were observed. League-phase checks ran. Live environment, runtime
identity, mounts/keys, ports, network and restart settings were preserved; production
background result synchronization remains enabled. No schema/collector changes or
Git commit/push were performed.

Release: `mynewsfeed-web:personal-planning-20261019` (`74b9a7422350`).
Rollback: `mynewsfeed-web:before-personal-planning-20261019` (`a80561dc6724`).
Previous stopped container: `mynewsfeed-web-before-personal-planning-20261019`.
Private runtime snapshots and scripts: `/tmp/mynewsfeed-personal-deploy/`.
The local read-only preview uses `Results__BackgroundSyncEnabled=false`.
See [personal planning behavior](interface.md#personal-planning-and-discovery--october-19-2026).

## Separate YouTube navigation — October 19, 2026

Moved Videos out of Entertainment into its own desktop YouTube navigation link
and dedicated YouTube section in mobile Browse. The current-section label reads
YouTube on video pages; Entertainment is no longer marked active there. Existing
video routes, channel filters, paging and playback destinations are unchanged.

Production publish and Blazor static-asset guard passed. Preview and live browser
checks verified desktop/mobile placement and active states, no overflow at
390/768/1180/1440px and existing entertainment/sports/health routes.

Release: `mynewsfeed-web:youtube-section-20261019` (`a80561dc6724`).
Rollback: `mynewsfeed-web:before-youtube-section-20261019` (`fd3d399c44d7`).
Previous stopped container: `mynewsfeed-web-before-youtube-section-20261019`.

## Big 12 News in College Sports — October 18, 2026

Added the College Sports News tab at `/college/news`, using the existing article
cards, source selector, safe article/image links, date formatting, cursor paging,
saving and retired-ID recovery. Coverage is conference-wide; the selected sport
is retained for hub navigation, source forms and pagination. Five enabled
publishers are discovered dynamically from the Big 12 News category by name.
Existing URL ownership is preserved across overlapping official feeds.

Enhanced Load more uses `view=big12-news` and enforces the category server-side,
including when a request supplies an unrelated category or source. Missing or
disabled coverage remains empty, query errors remain distinct from empty data,
and source options include enabled feeds with no articles. Dedicated women's
basketball remains unregistered; conference-wide feeds can include women's sports.
No schema, source registration, collector restart or new credentials were needed.

All 170 targeted feed/query/helper/rendering/personal-feature/Box Office checks
passed, including temporary-table Big 12 category/source isolation and pagination
fixtures. Production publish and Blazor static-asset guard passed with existing
Pkcs dependency warnings. Preview and production browser verification covered all
46 retained articles without duplicate IDs, all five publisher filters/reload,
article destinations and saving, forged-category isolation, empty search, sport
persistence, 390/768/1440px layouts, no-JavaScript forms/paging and existing routes.
Article counts are dated observations. Production HTTPS News and health returned
200; the container was healthy. Runtime settings, identity, mounts, ports,
restart policy and network were verified preserved.

Release: `mynewsfeed-web:big12-news-20261018` (`fd3d399c44d7`).
Rollback: `mynewsfeed-web:before-big12-news-20261018` (`d61f9d5de5d4`).
Previous stopped container: `mynewsfeed-web-before-big12-news-20261018`.

## Single college sport selector — October 18, 2026

Removed redundant source dropdowns from the college hub's embedded Standings and
Rankings views. The shared Sport selector remains and its label is now vertically
centered alongside the control. Standalone Standings and Top 25 retain their
source selectors.

Production publish and the static-asset guard passed. Preview and production
browser checks verified exactly one college selector, centered label/control
alignment and no overflow across all four sports at 390/768/1440px. Standalone
source selectors were also checked. Production health was healthy and HTTPS
health returned 200.

Release: `mynewsfeed-web:college-selector-20261018` (`d61f9d5de5d4`).
Rollback: `mynewsfeed-web:before-college-selector-20261018` (`0ef5235fa2db`).
Previous stopped container: `mynewsfeed-web-before-college-selector-20261018`.

## Uniform table team highlights — October 18, 2026

Personal team highlights in standings/rankings now draw inset strokes on each
cell, including the sticky team column. This prevents the pinned column from
covering the row outline. Adjacent selected rows share one horizontal edge;
selection no longer adds a layout-changing left border. Existing light/dark red
colors and schedule/result card highlights are retained.

Production publish and the static-asset guard passed. Preview and live browser
checks verified light/dark themes, adjacent selections, sticky cells while
scrolling, unchanged column widths and no overflow at 390/1440/1815px. Desktop
standings were visually reviewed. Container health was healthy and HTTPS health
returned 200.

Release: `mynewsfeed-web:highlight-alignment-20261018` (`0ef5235fa2db`).
Rollback: `mynewsfeed-web:before-highlight-alignment-20261018` (`77cb2aef74cd`).
Previous stopped container: `mynewsfeed-web-before-highlight-alignment-20261018`.

## College schedule column alignment — October 18, 2026

Restored the shared schedule layout wrapper on the College Sports hub. Desktop
rows now inherit the same fixed column widths as the headings instead of sizing
independently around each game's content. Mobile/tablet game cards retain their
existing layout.

Production publish and the static-asset guard passed. Preview and live browser
checks verified matching row/header column positions at 1440/1815px and no page
overflow at 390/768/1440/1815px. The desktop schedule was visually reviewed.
Production container health was healthy and HTTPS health returned 200.

Release: `mynewsfeed-web:college-alignment-20261018` (`77cb2aef74cd`).
Rollback: `mynewsfeed-web:before-college-alignment-20261018` (`047abc5d402f`).
Previous stopped container: `mynewsfeed-web-before-college-alignment-20261018`.

## College Sports hub — October 18, 2026

Added Sports → College Sports on desktop and mobile, with Schedule, Results,
Standings and Rankings tabs and a shared sport selector. Football, men's
basketball, women's volleyball and women's soccer use existing feeds. URLs retain
the sport across tabs, pagination, result dates and standings/poll forms. Missing
or invalid sport values default to football. Existing public sports routes remain
available.

Schedules cover the next seven days and paginate; results use today/yesterday's
site-timezone windows and enabled college follows. Big 12 standings and national
polls remain scoped to the selected sport even with an unrelated source ID.
Existing current-season/preseason gates, compact records, freshness tooltips,
league-wide playoff suppression and intentional women's basketball exclusion
remain in effect. Empty coverage does not fall back to another sport. No schema,
collector, source registration or credential changes were required.

All 182 targeted schedule/result/standings/rankings/team-record checks passed,
including new query isolation assertions. Corrected an older schedule test to
expect the application's existing women's basketball exclusion. Production
publish and the Blazor static-asset guard passed; existing Pkcs dependency and
xUnit analyzer warnings remain.

Preview and production browser checks passed all sixteen sport/tab combinations,
390/768/1440px overflow checks, sport/tab/date persistence, source isolation,
unknown-sport fallback, no-JavaScript forms and existing-page smoke checks.
Mobile standings were visually reviewed. Production HTTPS College Sports and
health returned 200; the container was healthy with no error-level log entries
observed. Runtime configuration, identity, ports, restart policy, mounts and
networks were verified preserved.

Release: `mynewsfeed-web:college-hub-20261018` (`047abc5d402f`).
Rollback: `mynewsfeed-web:before-college-hub-20261018` (`cb4466d3c168`).
Previous stopped container: `mynewsfeed-web-before-college-hub-20261018`.

## Compact college records and Home text cleanup — October 18, 2026

College records now use overall (conference) shorthand, for example 12-4 (5-2).
Soccer retains draws in both portions. Missing portions remain omitted, and
record tooltips explain the parentheses along with freshness. Professional record
formats and league-wide playoff suppression remain unchanged. Removed the long
team-record explanation from Home's Today & Tonight panel; records and their
tooltips remain visible when eligible. Other sports pages retain their note.

All twelve team-record checks passed. Production publish and Blazor static-asset
guard passed, with the existing Pkcs dependency warnings. Live browser checks
verified Home text removal, compact college labels and tooltip meanings on
Results, the separate F1 card, and no mobile/desktop overflow. HTTPS Home, Results
and health returned 200. Container health was healthy, with no error-level entries
observed. Runtime settings, host configuration, network and keys were preserved.
No database scripts or scraper changes were required.

Release: `mynewsfeed-web:compact-records-20261018` (`cb4466d3c168`).
Rollback: `mynewsfeed-web:before-compact-records-20261018` (`a04785efff8f`).
Previous stopped container: `mynewsfeed-web-before-compact-records-20261018`.

## F1 News tab — October 18, 2026

Added Sports → F1 → News at `/f1/news`, sharing the existing article cards,
publication ordering, safe destinations, publisher attribution, source selector,
cursor pagination and browser-local save behavior. A shared F1 tab bar exposes
Schedule, Results, Standings and News; the dedicated home F1 card also links to
News. Category membership is resolved by F1 News name and enabled sources, without
hardcoded IDs. News remains independent of Jolpica F1Source enablement.

Enhanced Load more enforces the fixed category server-side and returns F1-route
paging links. Source filters persist through reload/pagination, invalid sources
reset within F1 scope, and a missing/disabled category cannot expose general
articles. Empty data and query errors use distinct states. Freshness is scoped to
successful category/source scrapes. No source registration scripts, schema changes,
new credentials or scraper rebuild/restart were required.

All 134 targeted F1/news query, rendering and helper checks passed. New SQL fixtures
used connection-local temporary tables to verify source/cursor scoping, disabled
sources and missing-category isolation. Application build, production publish and
Blazor static-asset guard passed. Existing Pkcs dependency warnings remain.

Read-only production checks found four enabled publishers with ten retained
articles each. Preview and live browser checks verified all 40 articles through
complete enhanced pagination without duplicate IDs, publisher selection/reload,
article destinations and saving, fixed-category isolation even with conflicting
request parameters, empty search, original News default, home link, 390/768/1440px
layouts and no-JavaScript GET forms/paging. The mobile News view was visually
reviewed. These counts are dated observations, not permanent acceptance totals.

HTTPS F1 News and health returned 200. Existing F1 Schedule/Results/both standings,
News and Home returned 200. Container health was healthy with no error-level
application entries observed. Runtime settings, host configuration, network and
persistent keys were preserved. Database remains sql2025 / WebScraper. The temporary
preview container was removed after checks.

Release: `mynewsfeed-web:f1-news-20261018` (`a04785efff8f`).
Rollback: `mynewsfeed-web:before-f1-news-20261018` (`f87b88d2d853`).
Previous stopped container: `mynewsfeed-web-before-f1-news-20261018`.

## Separate F1 home-page card — October 18, 2026

Added a dedicated F1 card outside Today & Tonight. It shows the next unclassified
Grand Prix with circuit/location and display-zone start time, the next available
weekend session, the latest published Race/Sprint result, and links to Schedule,
Results and Standings. It occupies its own full-width desktop row and appears
after Today & Tonight on mobile. It reuses the F1 read contract and preserves
snapshot freshness, source attribution and date-only/TBD behavior. F1 failures
are handled inside the card so other Home sections remain available.

Application build, production Docker publish and Blazor static-asset guard passed.
Preview and live browser checks confirmed the F1 card is a sibling outside Today &
Tonight, its next race and latest result render, links open the F1 views, and
390/768/1440px layouts have no horizontal overflow or overlap. No-JavaScript
rendering passed; the mobile card was visually reviewed. HTTPS Home, F1 and
health returned 200. Container health was healthy and no error-level application
entries were observed. Existing Pkcs build warnings remain.

Runtime settings, host configuration, network and persistent keys were preserved.
No database scripts or scraper changes were made. The temporary preview container
was removed after checks.

Release: `mynewsfeed-web:f1-home-20261018` (`f87b88d2d853`).
Rollback: `mynewsfeed-web:before-f1-home-20261018` (`285e5173584c`).
Previous stopped container: `mynewsfeed-web-before-f1-home-20261018`.

## Dedicated F1 section under Sports — October 18, 2026

Added a separate Sports → F1 entry on desktop and mobile, with Schedule, Results
and Standings tabs. Schedule offers upcoming/current weekends and the full season,
with expandable practice, qualifying, sprint and race timetables. Confirmed times
convert from UTC to the display zone; date-only sessions retain their date and
Time TBD. Equivalent sprint qualifying fields are deduplicated. Past races with
no Race classifications show Results pending/unavailable. Race reference links
are validated and are not labelled as streams.

Results separates Grand Prix and Sprint classifications, preserves provider
position text/status/time gaps/decimal points and offers optional timing details.
Grid zero is labelled Pit lane / unavailable. Drivers and Constructors standings
use provider rankings and show the published championship as-of round, independently
of the next scheduled race. Multiple constructors per driver are retained.
Mobile tables scroll within labelled keyboard-focusable regions and include a
scrolling hint. Native navigation, disclosures and GET-form Apply controls work
without JavaScript. Jolpica attribution and periodic-data limitations are visible.

The existing runtime reader successfully accessed F1Source and all four deployed
views. Read-only keyless mappings and repeatable-read page queries consume the
collector's complete latest-season snapshot; no schema migrations, seeds, new
credentials, ingestion changes or Rust scraper deployment were needed. Failed
refreshes preserve last-good freshness; disabled feeds and valid empty preseason
data are distinct from database errors. Historical season selection and live
timing remain outside this feature.

All 197 targeted tests passed, including 11 new F1 cases for timezone/date-only
handling, sprint sessions, session selection, fractional points, multiple teams,
health and SQL queries. SQL fixtures used only connection-local temporary tables.
Production image publish and Blazor static-asset guard passed. Build output
reported the existing Pkcs dependency vulnerability warnings.

Preview and production browser checks verified 23 full-calendar races, 23 available
race/sprint options, 23 driver standings rows and 11 constructor standings rows,
with championship as-of round 17. These are dated observations, not fixed grid
sizes. Navigation, direct links/reload, Grand Prix/Sprint changes, both championship
views, attribution, no-JavaScript controls and 390/768/1440px layouts passed.
Mobile Results and desktop Standings were visually reviewed; driver-code spacing
and a mobile scrolling hint were refined before the final build.

HTTPS F1 Schedule, Results, Standings and health returned 200. HTTP Home, News,
Events, Results, Standings and all F1 views returned 200; unauthenticated Admin
returned 302. Container health was healthy and no error-level application entries
were observed during verification. Runtime environment, host configuration,
network and persistent keys were preserved. The database remains sql2025 /
WebScraper. The temporary preview container was removed after checks.

Release: `mynewsfeed-web:f1-section-20261018` (`285e5173584c`).
Rollback: `mynewsfeed-web:before-f1-section-20261018` (`93554cf9532b`).
Previous stopped container: `mynewsfeed-web-before-f1-section-20261018`.
See [F1 data and routes](sports.md#f1-section).

## Hide records league-wide during playoffs — October 18, 2026

Records now appear only for leagues confirmed in regular season. Playoffs hide
records for the entire league across Home, Events, Results, MLS results and
Championship results, including older results. Games and scores remain visible.
A background service reads current ESPN scoreboard league metadata on startup
and every ten minutes. Page rendering makes no provider requests. Soccer stage
IDs are handled separately; explicit postseason/playoff names take precedence.
Unknown, failed or expired phase checks suppress records. Regular-season
confirmation expires after twenty minutes. No calendar cutoff or historical game
status is used, and standings SeasonType is not used to infer the current phase.

All 186 targeted regression checks passed. Production Docker build and Blazor
static-asset guard passed, with the existing Pkcs dependency warnings. Preview
and production phase checks identified MLB and WNBA as Postseason and all eleven
other enabled standings source/sport pairs as RegularSeason. Live browser checks
verified MLB/WNBA records absent on Home, Events and October 17 results while
regular-season and college conference records remained. All five views passed
390px/1440px overflow checks. HTTPS Home, Events, Results and health returned 200;
container health was healthy and no error-level application entries were observed.

Runtime configuration, network, host configuration and persistent keys were
preserved. No schema/seed scripts ran, and Rust scraping was not restarted. The
runtime database remains sql2025 / WebScraper. The temporary preview container
was removed after checks. Score synchronization was not forced.

Release: `mynewsfeed-web:playoff-records-20261018` (`93554cf9532b`).
Rollback: `mynewsfeed-web:before-playoff-records-20261018` (`2637a1f75888`).
Previous stopped container: `mynewsfeed-web-before-playoff-records-20261018`.
See [league-phase rules](sports.md#league-wide-postseason-exclusion).

## Current team records beside sports matchups — October 18, 2026

Home, Events, Results, MLS results and Championship results now show the latest
available season records beside covered teams. College teams show overall and
conference totals, including soccer draws. Professional records identify W–L,
W–L–D, W–L–OT or W–L–T as appropriate. Records on past results are current season
standings, not historical records as of the game. A per-record tooltip supplies
the update time; failed, unknown-age or more-than-two-hour-old snapshots show
“last available.” Missing/invalid or conflicting records are omitted.

Uses one shared standings snapshot per page. Provider/sport-scoped ESPN IDs are
used for professional teams; Big 12 records use sport and known team-name aliases
without crossing provider IDs. WNBA's schedule Basketball label is reconciled
with Women's Basketball standings. Old college seasons are excluded. College
coverage remains limited to the Big 12 standings; uncovered opponents may have
no record. Previous-season basketball totals are not carried into a new season.

All 172 targeted tests passed, including new format, identity, alias, season,
freshness, missing-data and away-team rendering checks, plus existing standings,
result, matchup and home-event regressions. Production Docker publish and the
Blazor static-asset guard passed. Preview and live browser checks passed all five
views at 390/768/1440px with no horizontal overflow, including college conference
records, soccer draws and update-time titles. Observed record-label counts were
16 on Home, 78 on Events, 55 on October 17 Results, and 50 each on MLS and
Championship results; these are dated observations, not acceptance totals.
Mobile Home and college-result cards were visually reviewed.

HTTPS Home, Events, Results and health returned 200. Container health was healthy;
no error-level application entries were observed during verification. The runtime
database remains sql2025 / WebScraper; standings coverage was checked read-only.
Runtime environment, host configuration, network and persistent keys were
preserved. No schema/seed scripts ran and the Rust scraper was not restarted.
No forced score refresh was requested. The build reported existing Pkcs dependency
vulnerability warnings. The temporary preview container was removed after checks.

Release image: `mynewsfeed-web:team-records-20261018` (`2637a1f75888`).
Rollback image: `mynewsfeed-web:before-team-records-20261018`
(`84aea0cd6733`). Previous stopped container:
`mynewsfeed-web-before-team-records-20261018`.
See [record coverage and matching](sports.md#team-records-beside-matchups).

## October 17, 2026 changes at a glance

All four feature updates and the dropdown-order/default follow-ups were deployed
to production. The latest headlines-default release includes all changes below.

| Change | Current behavior and usage | Deployment evidence |
| --- | --- | --- |
| Completed home-page sports events | Hide on the next page load after a matching final is stored; [matching rules](sports.md#home-page-completed-events) | [Home-page release](#hide-completed-home-page-games--october-17-2026) |
| Top Headlines | First named category, with enabled-source filtering; [news behavior](interface.md#top-headlines-and-news-source-filtering) | [Integration release](#top-headlines-and-news-source-selector--october-17-2026), [dropdown-order follow-up](#top-headlines-first-in-category-dropdown--october-17-2026) |
| Clearer Settings gear | Symmetrical outline gear in the header on desktop and mobile | [Gear release](#clearer-settings-gear--october-17-2026) |
| Kansas City automatic appearance | Select Automatic in Settings; [setup and manual override](interface.md#automatic-light-and-dark-mode) | [Automatic-theme release](#kansas-city-automatic-lightdark-mode--october-17-2026) |

## News defaults to Top Headlines — October 17, 2026

Opening News without a category now selects Top Headlines by its database name.
Explicit category choices remain supported. All categories uses `category=all`
so source filtering, reload, cursor links and enhanced Load more preserve that
choice. If no enabled Top Headlines category exists, News falls back to All
categories. Clear filters returns to the default News view.

All 102 targeted feed query/helper tests passed, as did the production image
build and Blazor static-asset guard. Live browser checks verified default and
reload behavior, explicit All categories, source scoping, enhanced pagination
and no-JavaScript views. HTTPS News and health returned 200; container health
was healthy with no error-level entries observed. Runtime configuration and
persistent mounts were preserved. No database scripts ran. The existing Pkcs
build warnings remain. Actual Top Headlines cards still await collected articles
as recorded for the integration release.

Release: `mynewsfeed-web:headlines-default-20261017` (`84aea0cd6733`).
Rollback: `mynewsfeed-web:before-headlines-default-20261017` (`98666225a77a`).
Previous stopped container: `mynewsfeed-web-before-headlines-default-20261017`.

## Top Headlines first in category dropdown — October 17, 2026

Placed Top Headlines immediately below All categories in News; the remaining
categories stay alphabetical. Membership and category IDs still come from the
database. Production image build and Blazor static-asset guard passed. A live
browser check verified dropdown order, selection and the six enabled source
options. HTTPS News and health returned 200; container health was healthy.
Runtime settings and persistent mounts were preserved; no database scripts ran.

Release: `mynewsfeed-web:headlines-first-20261017` (`98666225a77a`).
Rollback: `mynewsfeed-web:before-headlines-first-20261017` (`9368fbec816b`).
Previous stopped container: `mynewsfeed-web-before-headlines-first-20261017`.

## Top Headlines and news source selector — October 17, 2026

Top Headlines already appeared through database-driven category discovery. Added
an enabled-source selector to News, scoped to the selected category and available
before articles arrive. Source selection now applies on initial/direct requests,
persists through cursor pagination and enhanced Load more, and resets when the
category changes. Empty categories keep both selectors and show a normal empty
message. Existing dated-first ordering, cards, URL validation and saved-item
identity remain unchanged. No source/category IDs were hardcoded.

All 121 targeted feed query, rendering and helper checks passed. A new transactional
SQL regression covered empty enabled-source discovery, disabled-source exclusion,
source/category scoping, cursor counts and undated ordering. Production image build
and Blazor static-asset guard passed. Live browser checks covered the dynamic
category, six source options, direct navigation/reload, filter scope, empty state,
1440px/390px layouts without overflow, existing BBC cards and save behavior,
enhanced Load more without duplicate IDs, and no-JavaScript GET forms. HTTPS News
and health returned 200; container health was healthy, with no error-level
application entries observed. Runtime settings, network, host configuration and
persistent keys were preserved. Existing Pkcs dependency warnings remain.

Read-only production checks of sql2025 / WebScraper confirmed NBC News, CBS News,
The New York Times, The Guardian, Al Jazeera and Sky News enabled under Top
Headlines, with BBC News and NPR still under General. All six new feeds had zero
article rows and no recorded LastError at verification. Successful ingestion and
actual Top Headlines desktop/mobile cards remain unverified; source registration
alone is not collection evidence. No source registration scripts, schema scripts
or scraper deployments ran. Score synchronization was not forced.

Release image: `mynewsfeed-web:top-headlines-20261017` (`9368fbec816b`).
Rollback image: `mynewsfeed-web:before-top-headlines-20261017`
(`69920ea4f758`). Previous stopped container:
`mynewsfeed-web-before-top-headlines-20261017`.

## Clearer Settings gear — October 17, 2026

Replaced the irregular Settings icon with a symmetrical eight-tooth gear and
centered hub, keeping the header's existing outline style, link and accessible
label. Production build and Blazor static-asset guard passed. Live browser checks
verified the SVG matches the source, is visible at desktop (1440px) and mobile
(390px) widths, has no horizontal overflow, and opens Settings. Home, Settings,
Events, Results and health returned 200; unauthenticated Admin returned 302.
HTTPS Settings and health returned 200. Container health was healthy and no
error-level application log entries were observed during verification.

Runtime environment, host configuration, network and persistent keys were
preserved. No database scripts ran and scraper collection was not restarted.
The build reported the existing Pkcs dependency vulnerability warnings.
Release image: `mynewsfeed-web:settings-gear-20261017` (`69920ea4f758`).
Rollback image: `mynewsfeed-web:before-settings-gear-20261017`
(`804e53a096c9`). The previous stopped container is retained as
`mynewsfeed-web-before-settings-gear-20261017` with its matching configuration.

## Kansas City automatic light/dark mode — October 17, 2026

Settings now offers Automatic · Kansas City sunrise/sunset alongside Follow device,
Light and Dark. Automatic calculates approximate sunrise and sunset locally for
Kansas City, Missouri and displays today's Central times. It switches to dark at
sunset and light at sunrise, checks every 30 seconds, and refreshes after tab
resume and navigation. Existing preferences remain in effect until Automatic is
selected. The header toggle selects a manual choice; selecting Automatic resumes
the daily schedule. No geolocation permission or external API is needed.

The production Docker build and Blazor static-asset guard passed. Five calculation
regressions passed, including sunrise/sunset boundaries, city date near UTC
midnight, seasons, DST and polar handling. Browser regression checks against the
live release passed automatic mode, persistence, manual override, enhanced
navigation, tab focus, device mode, cross-tab changes, mobile overflow and blocked
storage, using a device timezone outside Central. HTTPS Settings and health
returned 200 through the existing Tailscale Serve endpoint with explicit DNS
resolution from the workspace. Home, Settings, News, Events, Results, Standings,
Rankings, Movies, Digital and Box Office returned 200; unauthenticated Admin
returned 302. Container health was healthy, with no error-level application log
entries during verification. A fresh score-sync cycle was not forced or verified.
The Docker build reported existing Pkcs dependency vulnerability warnings;
dependency upgrades were outside this release.

The runtime database identity check confirmed WebScraper on sql2025. No database
scripts ran. Runtime environment, host configuration, network and persistent keys
were preserved; scraper collection was not restarted.

Release image: `mynewsfeed-web:solar-theme-20261017` (`804e53a096c9`).
Rollback image: `mynewsfeed-web:before-solar-theme-20261017`
(`f6d490188f1a`). The previous stopped container is retained as
`mynewsfeed-web-before-solar-theme-20261017` with its matching configuration.

## Hide completed home-page games — October 17, 2026

Today & Tonight now excludes fixtures with stored completed results, including
when schedule status has not yet refreshed. ESPN fixtures match league/event
identity; Big 12 fixtures require competition, exact confirmed start and both
participants, with known team-name aliases. Scheduled and in-progress games
remain visible. The empty panel says “No remaining games today.” Filtering runs
on page load and preserves stored fixtures and results.

The release Docker build and its Blazor static-asset guard passed. Eleven targeted
unit/database regression checks passed, including college and ESPN home filtering.
Earlier broader checks passed 113 tests after excluding an existing women's
basketball retention test group; its women's basketball case conflicts with the
current schedule exclusion. Build output also reported existing Pkcs dependency
vulnerability warnings; dependency upgrades were outside this release.

Deployed to the existing `mynewsfeed-web` container target using sql2025 /
WebScraper. Preserved runtime environment, host configuration, network, health
check and persistent keys. No schema scripts ran and scraper collection was not
restarted. Container health and HTTP checks for Home, News, Events, Results,
Standings, Rankings, Movies, Digital and Box Office passed; unauthenticated Admin
returned 302. HTTPS Home and health returned 200 through the existing Tailscale
Serve endpoint, using explicit DNS resolution from the workspace. No error-level
application logs were observed during verification; a fresh score-sync cycle was
not forced or verified.

Release image: `mynewsfeed-web:home-final-results-20261017` (`f6d490188f1a`).
Rollback image: `mynewsfeed-web:before-home-final-results-20261017`
(`fcc190e3e1cd`). The previous stopped container is retained as
`mynewsfeed-web-before-home-final-results-20261017` with its matching configuration.

## Warm color schemes and BYU retirement — October 16, 2026

Added Desert Sunset (terracotta, peach and sand) and Coffee (espresso, caramel
and ivory), each with light/dark palettes and browser persistence. Removed BYU
from theme choices and palette definitions. A saved `byu` color preference now
falls back to Kansas; BYU remains eligible in My Teams and saved team choices
are preserved. Settings now offers twelve schemes.

Production build, JavaScript syntax and whitespace checks passed. Browser checks
at 390/768/1024/1440 pixels covered all twelve schemes in both modes, refresh,
navigation, team retention and no horizontal overflow. A separate legacy BYU
check verified Kansas fallback and retained BYU team selection. Eight primary
warm-palette text/background pairs exceeded 4.5:1 contrast. Health and HTTPS
Settings returned 200. Runtime configuration and keys were preserved.

## Decade color schemes — October 16, 2026

Added ’80s Neon (pink, blue and purple), ’90s Retro (teal, purple and cream),
and 2000s Y2K (silver, blue and lime) to Settings. All eleven choices support
light/dark mode and automatic browser persistence. The `eighties`, `nineties`
and `y2k` values are recognized before paint and throughout navigation.

Production build, JavaScript syntax and whitespace checks passed. Browser checks
at 390/768/1024/1440 pixels covered all eleven schemes in both modes, refresh,
navigation, team retention and no horizontal overflow. Thirteen primary decade
palette text/background pairs exceeded 4.5:1 contrast. Health and HTTPS Settings
returned 200. Runtime configuration and keys were preserved.

## St. Patrick’s Day color scheme — October 16, 2026

Added St. Patrick’s Day as an eighth optional Settings scheme: emerald green and
gold with a matching dark palette. Dark text on gold selections and darker gold
text on light surfaces preserve readability. The `st-patricks` preference is
supported by prepaint initialization and browser persistence. Existing schemes
and team selections remain supported.

Production build, JavaScript syntax and whitespace checks passed. Browser checks
at 390/768/1024/1440 pixels covered all eight schemes in light/dark mode, refresh,
navigation, team retention and no horizontal overflow. Main text and button pairs
exceeded 4.5:1 contrast. Health and HTTPS Settings returned 200. Runtime
configuration and keys were preserved.

## Halloween color scheme — October 16, 2026

Added Halloween as a seventh optional Settings scheme with pumpkin orange, purple
and black, plus a matching dark palette. The `halloween` preference is supported
by prepaint initialization and browser persistence. Existing schemes and saved
team selections remain supported.

Production build, JavaScript syntax and whitespace checks passed. Browser checks
at 390/768/1024/1440 pixels covered all seven schemes in light/dark mode, refresh,
navigation, team retention and no horizontal overflow. Main Halloween text and
button pairs exceeded 4.5:1 contrast. Health and HTTPS Settings returned 200.
Runtime configuration and keys were preserved.

## Christmas color scheme — October 16, 2026

Added Christmas as a sixth optional Settings color scheme: evergreen, cranberry
red and warm white, with a matching dark palette. The `christmas` value is accepted
by both prepaint initialization and preference handling, preserving refresh and
navigation behavior. Existing schemes and team preferences remain supported.

Production build, JavaScript syntax and whitespace checks passed. Browser checks
at 390/768/1024/1440 pixels covered all six schemes in light/dark mode, refresh,
navigation, team retention and no horizontal overflow. Primary Christmas text and
button color pairs exceeded 4.5:1 contrast. Health and HTTPS Settings returned 200.
Runtime configuration and keys were preserved.

## CITY SC Tina Turner palette — October 16, 2026

Updated the existing CITY SC scheme to gold and black inspired by the Tina Turner
kit. Settings labels this choice St. Louis CITY SC · Tina Turner; its existing
`city` preference key remains compatible. UI gold shades are adapted for readable
links, buttons and header text in light and dark mode. The other schemes and team
selections remain intact. See the [club announcement](https://www.stlcitysc.com/news/st-louis-city-sc-unveils-the-tina-turner-kit).

Production build and browser checks passed at 390/768/1024/1440 pixels for all
schemes, both modes, persistence, navigation and team retention. Header, button,
light-link and dark-link text/background pairs exceeded 4.5:1 contrast. Health
and HTTPS Settings returned 200; the container was healthy. Runtime configuration
and keys were preserved.

## Personal settings — October 16, 2026

Added Settings (`/settings`) with a gear link beside Saved articles and a link in
the mobile menu. Moved My Teams selection from the Sports menu into Settings;
`/sports/teams` remains an alias for existing bookmarks. Existing team preferences
continue using the same browser storage key, professional choices and Big 12-only
college choices; selected-team highlights remain red.

Added five instant color schemes: Monochromatic, Kansas Jayhawks (the existing
default), BYU Cougars, St. Louis Cardinals and St. Louis CITY SC. Each supports the
existing light/dark toggle. Preferences save locally to this browser/device;
initialization applies the scheme before paint and restores it across navigation.
Invalid scheme values default to Kansas, and storage failures report an unsaved
choice. No account, database or scraper changes were required.

Production build and JavaScript syntax checks passed. Browser checks at
390/768/1024/1440 pixels covered all five schemes in light/dark mode, refresh,
navigation, team retention and no horizontal overflow. Additional checks covered
team updates, cross-tab synchronization, the legacy URL, default/invalid schemes
and no-JavaScript notices. Settings HTTPS and health returned 200. Runtime
configuration and keys were preserved; the original rollback image is retained.

## Box-office section pages — October 16, 2026

Separated the combined Box Office view into weekend Top 10 results at `/box-office`,
weekend predictions at `/box-office/weekend-preview` and future-movie predictions
at `/box-office/movie-forecasts`. A shared News-style dropdown selects the page,
with automatic GET submission and a no-JavaScript Apply button. Each page retains
its section's posters, dates, source attribution, warnings and empty states; the
results page no longer queries forecasts.

Production build and three focused forecast tests passed. Browser checks at
390/1440 pixels with and without JavaScript verified all three pages, selected
options, section isolation and no horizontal overflow. All 27 card contents matched
the predeployment snapshot after whitespace normalization. Health and HTTPS preview
returned 200; runtime configuration and keys were preserved.

## Movie view dropdown — October 16, 2026

Replaced the release-view buttons on Movies and Digital with a shared News-style
GET dropdown offering All releases, In theaters, Subscription streaming and Digital
rental / purchase. Selection submits automatically with JavaScript; Apply works
without it. The digital choice redirects to the existing Digital page.

Production build and all four selection checks passed at 390/1440 pixels with
and without JavaScript. Verified the selected option, navigation, unchanged card
counts (13 all, nine theatrical, one subscription, one default-source digital) and
no horizontal overflow. HTTPS Movies and health returned 200; the container was
healthy. Runtime configuration and keys were preserved.

## Desktop movie action layout — October 16, 2026

Placed movie-card retailer buttons on their own line below the details/source
link at desktop widths (768 pixels and wider). This prevents Search Fandango at
Home from sitting beside Movie details. Mobile styles remain unchanged.
Production build and browser checks at 390/1440/1874 pixels passed: the action
remains inside its card, below details, without horizontal overflow and retains
its destination. Health and HTTPS Digital returned 200. Runtime configuration
and keys were preserved.

## Movie retailer link placement — October 16, 2026

Moved the general AMC logo link below the theatrical release summary and update
stamp, immediately above the first release-date heading. Added a matching general
Fandango at Home link and locally served logo on Digital rental / purchase, below
the selected-source summary and above its release-date list. Both links open the
retailer homepage in a new tab with noopener/noreferrer; per-film actions remain.

The production image built successfully. Browser checks at 390/1440 pixels verified
both destinations, loaded logos, placement and no horizontal overflow. The nine
theatrical cards and one default-source digital card remained visible. Health and
HTTPS Digital returned 200. Runtime configuration and keys were preserved.

## AMC Theatres link — October 16, 2026

Added a general AMC Theatres website link with a locally served logo below the
heading on `/movies?type=theatrical`. It opens https://www.amctheatres.com/ in a new
tab with noopener/noreferrer. Other release views keep their existing layout.
Production browser checks at 390/1440 pixels confirmed the logo loads, the link
has the correct destination, nine theatrical cards remain visible and no horizontal
overflow occurs. Health and HTTPS theatrical releases returned 200. Deployment
preserved runtime configuration and keys.

The current page behavior is documented in
[Entertainment data and public views](entertainment.md#movie-releases-and-digitalvod);
logo provenance is recorded in the
[brand asset notes](../src/MyDailyFeed.Web/wwwroot/images/brands/README.md).

## Movie commerce links — October 16, 2026

Added secondary Fandango ticket and Fandango at Home digital links on Movies,
Digital and homepage movie previews, using the scraper's MovieCommerce view.
Validated HTTPS destinations and exact labels; missing/invalid matches generate
encoded title searches. Subscription/disc releases receive no action. No database
writes, credential changes or scraper changes were needed.

All 20 focused commerce/poster tests passed, including direct ticket/Buy/View
fixtures, unsafe destination rejection and punctuation encoding. Production data
currently supplies search actions only. Browser checks at 390/1440 pixels verified
11 actions across 13 Movies cards, one action on the default Digital source and six
homepage actions with no horizontal overflow. Every enabled digital source was
checked; subscription cards had no retailer action. Release titles, source URLs,
available date markup and card counts matched the predeployment snapshots.
Health and HTTPS Movies returned 200; runtime configuration and keys were preserved.

## Sports releases

October 9 professional schedules provisioned nullable ScheduleStatus and enabled
Kansas City Current, WNBA and MLB on sql2025 / WebScraper. Health, schedule selection
and Results returned 200. No scraper scheduler settings changed in that web rollout.
The initial full-season provider audits (Current: 30 games; WNBA: 378 in 2026 and 705
in its earlier broader refresh) predated upcoming-only refinements and do not describe
current import counts.

Standings/Digital integration passed 394 tests and browser checks after schema/import
verification. Later Top 25/MLS work passed 411 tests with five stored polls, 125 ranking
entries, 23 Big 12 markers and 30 MLS teams in two conferences. Subsequent basketball
visibility revisions hid stale-season choices; these historical five-poll checks do
not promise five visible current choices. Compact standings and rankings passed
light/dark, mobile/desktop and no-JavaScript checks (18 standings and 10 rankings tests).

October 13 NFL mapping passed 418 tests: 32 teams in two 16-team conferences, Kansas
City included, 256 statistics matching previous stored values, provider order and
keyboard/mobile scrolling. English soccer passed 430 tests and checks of Championship's
24/EPL's 20 table teams, eight statistics per team, all 115 stored season finals,
Wrexham's 10 matches, filters, paging, both themes and no-JavaScript controls.

The college schedule retention fix passed 441 tests. Deleting past fixture evidence
no longer hides upcoming sports. Live checks showed 54 football and 94 volleyball
games, accessible basketball paging, all five sport options and Kansas football,
with and without JavaScript at 390/1440 pixels. `/healthz`, `/results` and `/box-office`
returned 200. No database migration or scraper setting changes were required.

MLS schedule and season results were subsequently deployed as
`mynewsfeed-web:mls-schedule-results-20261013`. All 448 tests passed, including
provider/status/date isolation, home perspective in schedule and Home reads,
latest-season selection, last-good retention, stable pagination, encoded/safe
rendering and shootout winner/goal separation. Production already had enabled MLS
feed 5525, standings source 9 and both nullable shootout columns, so no web migration,
grant, database write or scraper configuration changes were needed.

Live checks at 390/768/1440 pixels in both themes, with and without JavaScript,
verified 90 MLS fixtures, 421 results, source links, paging and daily-results entry.
All 421 match links were reachable across 17 pages without duplicates. MLS's 30-team
standings, Championship's 115 season finals and 54 college football fixtures remained
accessible. The MLS mobile screenshot was visually inspected. Health and affected
public routes returned 200; runtime settings, network, ports and key volume were
preserved. The previous image/container remain available for rollback. No production
MLS penalty finals were present; shootout behavior was verified with test fixtures.

## Entertainment releases

Movie Insider validation for October 5–11 found ten confirmed digital releases;
When To Stream supplied five digital and nine subscription observations. These
were provider snapshots, not fixed page totals. The former weekly bounds were later
replaced with today-forward coverage. Movie rollout used the existing scraper schema
and read-only application schema check.

Deadline/Variety news originally had 20 stored stories, publisher separation and
no-JavaScript paging checks. That `/box-office` news mode was superseded October 13
by structured The Numbers results; news stories are not weekend financial chart data.

The top-ten release passed 433 tests, including the application's read credentials,
ten ranks with one weekend, nullable changes and New Year dates. Browser checks at
390/768/1440 pixels with and without JavaScript verified links, attribution, currency,
no overflow and no JavaScript errors. The chart displayed October 9–11, 2026.

The forecasts release passed 437 tests covering source health, expired-date filtering,
precision and provider ranks. Live checks found 12 future-film forecasts, ten actuals
and a healthy unpublished-preview state across three widths, both themes and both
JavaScript modes. Every forecast had attribution/as-of information; long-range ranks
were not invented. Production health and related route checks passed.

Movie-section posters were deployed as `mynewsfeed-web:movie-section-posters-20261013`.
All 451 tests passed, including safe image URLs, encoded title alt text and missing
poster rendering. Chart/forecast database queries successfully read their added
nullable columns. Browser checks passed on Box Office, Movies, Digital and Home
at 390/768/1440 pixels in both themes: poster slots, accessible alt text, successful
HTTP responses and no horizontal overflow. The chart/forecast page had 22 portrait
slots; a sampled loaded poster was 800×1200 pixels. Forced external image failure
revealed neutral artwork and accessible unavailable text; missing release placeholders
remained visible. The mobile chart screenshot was inspected. All poster sources
remain external; lookup diagnostics are not exposed. Runtime settings and persistent
keys were preserved, and no scraper settings, schema or content were changed by the
web rollout. Rollback: `mynewsfeed-web:before-movie-section-posters`.

## Interface and audit releases

Movies calendar month-end scope was deployed as
`mynewsfeed-web:movies-current-month-20261015`. Per user clarification, the Movies
schedule includes today through the final day of the current month, excluding prior
dates and undated releases. All/theatrical/subscription views, counts and paging use
the window. Clamping an oversized page retains its release-type filter. The page
describes the month-end window and has matching empty-state text. Separate digital
and homepage queries retain their prior behavior. Build passed. Live 390/1440-pixel
checks exercised all three views, validated displayed dates, oversized-page type
preservation and no horizontal overflow. Health passed; the container was healthy.
Runtime settings and keys were preserved without scraper/schema changes.
Rollback image/container: `mynewsfeed-web:before-movies-current-month` /
`mynewsfeed-web-before-movies-current-month`.

Sports Events Links-column formatting was deployed as
`mynewsfeed-web:event-links-layout-20261015`. TV/Streaming and Radio details stack
above a distinct wrapping row of Stats/Video/Audio/Details links. Long broadcast lists
wrap independently instead of mixing with the action links. Build and 24 event tests
passed. Live 390/1440-pixel NHL checks verified broadcast/action separation, all 58
links retained in that snapshot and no page overflow. Desktop screenshot inspected.
Health passed; container healthy. Runtime settings and keys were preserved.
Rollback image/container: `mynewsfeed-web:before-event-links-layout` /
`mynewsfeed-web-before-event-links-layout`.

My Teams moved to the first Sports menu entry on desktop/mobile in
`mynewsfeed-web:myteams-menu-first-20261015`. Production build passed and live
browser checks verified both menu orders. Health passed; container healthy.
Runtime settings and keys were preserved. Rollback image/container:
`mynewsfeed-web:before-myteams-menu-first` / `mynewsfeed-web-before-myteams-menu-first`.

Personal team highlight color changed to red in
`mynewsfeed-web:red-team-highlights-20261015`. Selected rows use a red outline and
left border, with lighter red in dark mode. Selection logic and bold team names
remain. Production build passed. Live 390/1440-pixel light/dark checks verified
computed outline and border colors, persisted selection and no overflow. Health
passed; the container was healthy. Runtime settings and keys were preserved.
Rollback image/container: `mynewsfeed-web:before-red-team-highlights` /
`mynewsfeed-web-before-red-team-highlights`.

My Teams eligibility was corrected per user clarification in
`mynewsfeed-web:pro-and-big12-team-selection-20261015`. Only college choices are
restricted to the 16 Big 12 schools; professional teams from enabled NFL, NHL,
MLB, MLS, NWSL, WNBA, EPL and Championship data are available again. Existing saved
professional selections are supported again. Build, JS syntax and 42 personal-feature
tests passed. Live 390/1440-pixel checks verified exactly 16 college choices, no Alabama,
professional selections, persistence, search, cross-page highlights, clearing and
blocked-storage feedback. Health passed; the container was healthy. Runtime settings
and keys were preserved without database or scraper changes. Rollback image/container:
`mynewsfeed-web:before-pro-and-big12-team-selection` /
`mynewsfeed-web-before-pro-and-big12-team-selection`.

My Teams was limited to Big 12 schools in
`mynewsfeed-web:big12-only-team-selection-20261015`. The catalog uses the existing
16-school alias list instead of all standings/schedule participants. Existing Big 12
selections remain; non-Big 12 saved keys are ignored for personal highlights and counts.
The selection page no longer needs a database query. Build and 42 personal-feature
tests passed. Live 390/1440-pixel checks verified exactly 16 choices, search, retained
Kansas selection/highlighting, ignored non-Big 12 selections and no overflow.
Health passed; the container was healthy. Runtime settings and keys were preserved.
Rollback image/container: `mynewsfeed-web:before-big12-only-team-selection` /
`mynewsfeed-web-before-big12-only-team-selection`.

Personal sports highlights were deployed as
`mynewsfeed-web:personal-sports-highlights-20261015`. Sports navigation includes
My Teams (`/sports/teams`), with searchable team/school checkboxes grouped by league,
automatic browser-local saving, selection count, clear action and storage-failure
feedback. Choices are read from enabled standings and schedule sources. Canonical
college aliases share one key across sports. Schedule, daily result, MLS/Championship
season-result and standings rows expose encoded keys and highlight either selected
participant without filtering other rows. Navigation and cross-tab storage changes
resynchronize highlights. No account synchronization is provided.
Build, JS syntax and 91 offline standings/event-rendering/personal-feature tests passed.
Live 390/1440-pixel checks exercised search, saving/reload, highlights across pages,
clear persistence, full-table retention and no overflow. A separate check verified
daily-result highlights; blocked-storage feedback passed. The mobile selection-screen
screenshot was inspected. Health and HTTPS My Teams returned 200; container healthy.
Runtime settings and keys were preserved with no database or scraper changes.
Rollback image/container: `mynewsfeed-web:before-personal-sports-highlights` /
`mynewsfeed-web-before-personal-sports-highlights`.

Big 12 basketball source-style formatting was deployed as
`mynewsfeed-web:basketball-source-layout-20261015`. The table now has Big 12 Record,
Pct., Home, Away, Overall Record, Pct., Home, Away, Neutral and Streak, in screenshot
order. Compact left-aligned rows and an italic white-on-black header replace the
five-column grouped format. All ten source columns map directly, preserving distinct
conference and overall records. Current-season filtering and women's exclusion remain.
Build and 25 offline standings tests passed, including ten values and rendered-table
structure. Live 390/1440-pixel checks verified basketball's current-season waiting
state, no overflow and unchanged soccer columns. The populated basketball table was
verified with test fixtures because new-season data is not yet available. Health
passed and the container was healthy. Runtime settings and keys were preserved;
no scraper/schema changes were made. Rollback image/container:
`mynewsfeed-web:before-basketball-source-layout` / `mynewsfeed-web-before-basketball-source-layout`.

Big 12 soccer source-column alignment was deployed as
`mynewsfeed-web:college-soccer-source-columns-20261015`, replacing the earlier EPL
adaptation at the user's request. The linked Big 12 table was inspected directly.
The table now shows Big 12 Record/Conf. Pct./Points/Home/Away/Streak followed by
Overall Record/Pct./Home/Away/Neutral/Streak in source order. Values read the twelve
stored source columns directly, retaining separate conference and overall records.
Build and 25 offline standings tests passed. Live 390/1440-pixel checks verified
16 teams, twelve populated statistics, no overflow and a reference row matching the
linked source. The desktop screenshot was inspected. Health passed; the container
was healthy. Runtime settings and keys were preserved without scraper/schema changes.
Rollback image/container: `mynewsfeed-web:before-college-soccer-source-columns` /
`mynewsfeed-web-before-college-soccer-source-columns`.

Football AP poll reference formatting was deployed as
`mynewsfeed-web:football-poll-layout-20261015`. Per user clarification, AP only was
formatted; Coaches Poll collection was not added. Football uses compact
RK/TEAM/REC/PTS/TREND columns, team logos, inline first-place votes, striped rows,
colored movement arrows, NR indicators and a source rankings link. Big 12 highlighting
remains. Other sports retain their prior tables. Others receiving votes and dropped
teams are not stored and are omitted. Build and eight offline rankings tests passed.
Live 390/1440-pixel checks verified 25 rows, five columns, votes/trends and no overflow.
The mobile screenshot was inspected. Health passed; the container was healthy.
Runtime settings and keys were preserved without database or scraper changes.
Rollback image/container: `mynewsfeed-web:before-football-poll-layout` /
`mynewsfeed-web-before-football-poll-layout`.

NFL reference formatting was deployed as `mynewsfeed-web:nfl-division-layout-20261015`.
AFC/NFC headings contain East/North/South/West division tables in that order. All
W/L/T/PCT/HOME/AWAY/DIV/CONF/PF/PA/DIFF/STRK columns are visible by default, with
logos, striped rows, shaded percentage and colored signed point differential.
Home/road/division/conference records use stored provider statistics; row order stays
as published. Build and 25 offline standings tests passed. Live 390/1440-pixel checks
verified 32 teams, eight four-team divisions, all 12 columns and no page overflow.
The desktop screenshot was inspected. Health passed; the container was healthy.
Runtime settings and keys were preserved without schema or scraper changes.
Rollback image/container: `mynewsfeed-web:before-nfl-division-layout` /
`mynewsfeed-web-before-nfl-division-layout`.

Big 12 Volleyball's final four columns were removed in
`mynewsfeed-web:volleyball-trim-columns-20261015`: Conf. Home, Conf. Away,
Conf. Neutral and Conf. Streak. Conf/CPct./Overall/Pct./Streak remain visible by
default without the extra statistics toggle. Provider values and ordering remain.
Build and 25 offline standings tests passed. Live 390/1440-pixel checks verified
15 teams, the five requested statistics visible, no extra columns and no overflow.
Health passed; the container was healthy. Runtime settings and keys were preserved
without database or scraper changes. Rollback image/container:
`mynewsfeed-web:before-volleyball-trim-columns` / `mynewsfeed-web-before-volleyball-trim-columns`.

College soccer's unavailable F/A/GD columns were removed in
`mynewsfeed-web:soccer-remove-empty-columns-20261015`. The conference table retains
GP/W/D/L/P, logos, striped rows and shaded points; the unavailable-goals note was
replaced with a simple Conference standings label. Professional soccer columns
retain their existing behavior. Build and 25 offline standings tests passed.
Live 390/1440-pixel checks verified all 16 teams, five populated columns, record
arithmetic and no overflow. Health passed and the container was healthy.
Runtime settings and keys were preserved without database or scraper changes.
Rollback image/container: `mynewsfeed-web:before-soccer-remove-empty-columns` /
`mynewsfeed-web-before-soccer-remove-empty-columns`.

College basketball's compact grouped standings were deployed as
`mynewsfeed-web:college-basketball-table-20261015`. When current-season rows become
available, basketball uses Conference W-L/PCT and Overall W-L/PCT/STRK with the same
layout as football. Duplicate provider percentage labels map to their respective
conference/overall positions; no empty extra columns are added. Women's college
basketball remains excluded and prior-season basketball rows remain hidden.
Build and 25 offline standings tests passed, including separate percentage mapping.
Live 390/1440-pixel checks verified the current-season basketball waiting message,
continued women's exclusion, no overflow and the unchanged five-column football
table. Health passed and the container was healthy. The live basketball table is
pending new-season data; statistics mapping was tested with fixtures. Runtime settings
and keys were preserved without database or scraper changes. Rollback image/container:
`mynewsfeed-web:before-college-basketball-table` / `mynewsfeed-web-before-college-basketball-table`.

College football's empty columns were removed in
`mynewsfeed-web:football-remove-empty-columns-20261015`. Conference now contains
W-L/PCT; Overall contains W-L/PCT/STRK. Unavailable PF/PA and overall HOME/AWAY
columns and their explanatory note were removed; grouped header spans and divider
were updated. Build and 24 offline standings tests passed. Live 390/1440-pixel checks
verified all 16 teams, five populated statistics and no overflow. Health passed and
the container was healthy. Runtime settings and keys were preserved with no schema
or scraper changes. Rollback image/container:
`mynewsfeed-web:before-football-remove-empty-columns` /
`mynewsfeed-web-before-football-remove-empty-columns`.

College football reference formatting was deployed as
`mynewsfeed-web:college-football-table-20261015`. Big 12 tables use grouped Conference
and Overall headers, compact striped rows, logos and shaded conference percentage.
Columns match the reference: conference W-L/PCT/PF/PA, overall W-L/PCT/PF/PA/HOME/AWAY/STRK.
The current source supplies records, percentages and overall streak; unavailable
points and overall home/away records show em dashes with an explanatory note.
Conference home/away data is not substituted for overall records. All columns are
visible by default and provider order remains. Build and 24 offline standings tests
passed. Live 390/1440-pixel checks verified 16 teams, grouped headers, populated records,
no extra toggle and no page overflow. The desktop screenshot was inspected.
Health passed; the container was healthy. Runtime settings and keys were preserved;
no schema or scraper changes were made. Rollback image/container:
`mynewsfeed-web:before-college-football-table` / `mynewsfeed-web-before-college-football-table`.

Sports Events' three-day lookahead was deployed as
`mynewsfeed-web:events-three-days-20261015`. Listings, counts and paging are bounded
from today through today plus three days inclusive, using the configured display
timezone. League choices remain available; out-of-window selections show an empty
message. Production build passed. Live 390/1440-pixel checks exercised All schedules,
all 11 league/sport choices and an oversized page parameter; every displayed fixture
fell within October 15–18, 2026 and no horizontal overflow occurred. The all-schedules
view contained 101 fixtures in that snapshot. Health passed; the container was healthy.
Runtime settings and keys were preserved without database or scraper changes.
Rollback image/container: `mynewsfeed-web:before-events-three-days` /
`mynewsfeed-web-before-events-three-days`.

Sports Events radio-label cleanup was deployed as
`mynewsfeed-web:radio-label-cleanup-20261015`. Provider radio text is HTML-decoded,
stripped of complete/truncated tags and whitespace-normalized before rendering.
Image-only labels are omitted; separate audio links and real station text remain.
Stored data is unchanged. Build and 24 event-rendering/helper tests passed, including
the reported truncated Houston image tag, encoded markup, ordinary station text and
audio-link retention. Live mobile/desktop college schedule checks found no raw radio
markup or horizontal overflow. Health passed; the container was healthy. Existing
runtime settings and keys were preserved. Rollback image/container:
`mynewsfeed-web:before-radio-label-cleanup` / `mynewsfeed-web-before-radio-label-cleanup`.

Sports Events' streamlined dropdown was deployed as
`mynewsfeed-web:events-single-filter-20261015`. One Standings-style league/sport
selector replaces the separate school and sport dropdowns. College schedules are
grouped by Big 12 sport; professional schedules have one choice per league, plus
All schedules. Choices come from enabled visible upcoming fixtures. Selection uses
provider and sport together, persists through paging, and retains Wrexham filtering.
Existing school/sport URLs remain supported. Build and 17 event-rendering tests passed.
Live 390/1440-pixel checks exercised all 11 choices, paging URLs, Wrexham filtering,
no horizontal overflow and no-JavaScript Apply. The mobile screenshot was inspected.
Health and HTTPS Events passed; the container was healthy. Runtime settings and keys
were preserved without database or scraper changes. Rollback image/container:
`mynewsfeed-web:before-events-single-filter` / `mynewsfeed-web-before-events-single-filter`.

Women's college basketball was removed from public schedule/standings/results queries
in `mynewsfeed-web:hide-womens-college-basketball-20261015`. Schedule sport filtering,
Big 12 standings choices and results follow selection exclude the sport, including
direct URLs. Men's college basketball and other sports keep their existing behavior.
No stored data, feed flags or scraper settings were changed. Build and 23 offline
standings tests passed. Live 390/1440-pixel checks verified the four remaining college
standings choices, men's basketball schedules, no women's basketball sport choice,
empty direct women's schedule, excluded standings source and working Results.
Health and tailnet HTTPS Home passed; the container was healthy. Runtime settings
and keys were preserved. Rollback image/container:
`mynewsfeed-web:before-hide-womens-college-basketball` /
`mynewsfeed-web-before-hide-womens-college-basketball`.

Big 12 soccer EPL-style standings were deployed as
`mynewsfeed-web:college-soccer-table-20261014`. The conference table uses
GP/W/D/L/F/A/GD/P, deriving GP/W/D/L from the provider's Big 12 W-L-T record and
retaining published conference points. Goals statistics are unavailable in this
source and show em dashes with an explanatory note. The table uses the same logos,
striping, points shading and default-visible columns as professional soccer tables.
Build and 23 offline standings tests passed. Live 390/1440-pixel checks verified
all 16 teams, record arithmetic, eight columns and no page overflow. Health passed
and the container was healthy. Runtime settings and keys were preserved; no schema
or scraper changes were made. Rollback image/container:
`mynewsfeed-web:before-college-soccer-table` / `mynewsfeed-web-before-college-soccer-table`.

College standings visibility was repaired and deployed as
`mynewsfeed-web:college-standings-visible-20261014`. Enabled Big 12 sources stay
selectable without retained past opening fixtures; stale-season rows remain excluded.
Basketball feeds show a current-season unavailable message while awaiting new tables.
Read-only production checks found all five standings feeds, five college schedules
and five conference result follows already enabled with no recorded follow errors.
Live standings checks showed Football 16, Soccer 16, Volleyball 15 teams and both
basketball choices pending new-season tables. All five schedule sport filters returned
games; upcoming college data included 54 football, 94 volleyball and 40 soccer games,
plus men's/women's basketball fixtures. Stored finals included football, soccer and
volleyball; daily Results only shows today/yesterday and no college finals were present
in the checked daily windows. Build and 22 offline standings tests passed; the updated
SQL fixture test was not run against production. Health passed and the container was
healthy. No feed flags, database schema, or scraper settings changed.
Rollback image/container: `mynewsfeed-web:before-college-standings-visible` /
`mynewsfeed-web-before-college-standings-visible`.

Soccer standings reference formatting was deployed as
`mynewsfeed-web:soccer-standings-layout-20261014`. EPL, Championship, MLS and NWSL
show GP/W/D/L/F/A/GD/P in compact striped tables with logos, season in the first
header, shaded points and colored signed goal differential. All statistics are
visible by default; the extra rank column is removed. Existing team ordering,
league groupings, Wrexham/Current highlighting and notes remain.
Build, 22 offline standings tests and two English soccer column tests passed.
Live 390/1440-pixel checks verified EPL's 20, Championship's 24, MLS's 30 and NWSL's
16 teams, all eight columns visible and no page overflow. The desktop screenshot
was inspected. Health and HTTPS standings passed; the container was healthy.
Runtime settings and keys were preserved without scraper/schema changes.
Rollback image/container: `mynewsfeed-web:before-soccer-standings-layout` /
`mynewsfeed-web-before-soccer-standings-layout`.

Homepage news published dates were deployed as
`mynewsfeed-web:home-news-published-date-20261014`. Each dated story shows its
published date/time beneath the headline in the configured display timezone;
undated stories omit it. The production build passed. Live 390/1440-pixel browser
checks verified ten timestamps, placement between headlines and summaries, and
no horizontal overflow. Health and HTTPS Home passed; the container was healthy.
Existing runtime settings and keys were preserved with no scraper/database changes.
Rollback image/container: `mynewsfeed-web:before-home-news-published-date` /
`mynewsfeed-web-before-home-news-published-date`.

The YouTube mobile app-link release was reverted at the user's request after mobile
breakage. Production was restored using the preserved pre-change container running
`mynewsfeed-web:nhl-standings-layout-20261014`; latest was retagged to that release.
Only the YouTube app-link source changes were removed. Earlier homepage and NHL
standings updates remain. Video links again use standard web URLs and new-tab behavior.


YouTube mobile video links were deployed as
`mynewsfeed-web:youtube-mobile-app-links-20261014`. Android Chrome prepares a
YouTube-package intent with HTTPS fallback during a video click; iOS and other
mobile browsers keep HTTPS links in the same context for normal app-link handling.
Desktop links retain new-tab behavior. Host and video ID validation prevents
untrusted stored URLs from becoming app intents. Build and JS syntax checks passed.
Browser checks verified Android/iPhone/desktop behavior, fallback encoding and
untrusted-host rejection. Live Home/Videos checks passed in Android/iPhone profiles.
Health and HTTPS passed and the container was healthy. Physical phone app launch
was not tested. Runtime settings and keys were preserved without scraper/database
changes. Rollback image/container: `mynewsfeed-web:before-youtube-mobile-app-links` /
`mynewsfeed-web-before-youtube-mobile-app-links`.

NHL standings reference layout was deployed as
`mynewsfeed-web:nhl-standings-layout-20261014`. Eastern/Western conference headings
contain Atlantic/Metropolitan and Central/Pacific division tables respectively.
All 16 reference statistics are visible by default in compact striped tables with
logos, shaded points and signed/color-coded goal differential. Last-10 records omit
the provider's unrelated points suffix. Provider row order and stored values remain.
Build passed; all 22 offline standings tests passed, including NHL columns, conference
mapping, missing values and overtime-loss aliases. The existing 18-test standings
suite also passed before the added tests. Live checks verified 32 teams across four
divisions and all columns at 390/1440 pixels with/without JavaScript, no page overflow,
dark theme, and successful health/HTTPS. The desktop screenshot was inspected.
Runtime settings and keys were preserved. No schema or scraper changes were made.
Rollback image/container: `mynewsfeed-web:before-nhl-standings-layout` /
`mynewsfeed-web-before-nhl-standings-layout`.

Today & Tonight NHL filtering was deployed as `mynewsfeed-web:home-nhl-filter-20261014`.
The box includes NHL fixtures only when St. Louis Blues or Montreal Canadiens
participate, home or away. Full Events schedules retain their existing behavior.
Build and 130 relevant tests passed, including both clubs on either side and
unrelated-team exclusion. Live checks at 390/1440 pixels retained Capitals–Canadiens
and excluded Avalanche–Stars, with no overflow. Blues inclusion was verified in tests.
Home, health, Events and tailnet HTTPS returned 200; the container was healthy.
Runtime settings and keys were preserved with no database or scraper changes.
Rollback image/container: `mynewsfeed-web:before-home-nhl-filter` /
`mynewsfeed-web-before-home-nhl-filter`.

Today & Tonight Home/Away columns were deployed as
`mynewsfeed-web:home-away-columns-20261014`. Home appears on the left and Away on the
right, with names/logos ordered by schedule designation. Neutral/unknown games use
Team/Opponent labels. Time now joins location and TV/streaming beneath the teams.
Build and 123 relevant tests passed. Live browser checks at 320/390/768/1440 pixels
with light/dark color preferences verified column labels, horizontal alignment,
details underneath, all current logos loaded, no overflow and image failure initials.
The mobile screenshot was inspected. Health and HTTPS Home passed; the container
was healthy. Runtime settings and keys were preserved without database or scraper
changes. Rollback image/container: `mynewsfeed-web:before-home-away-columns` /
`mynewsfeed-web-before-home-away-columns`.

October 14 horizontal Today & Tonight matchups were deployed as
`mynewsfeed-web:home-horizontal-matchups-20261014`. Both teams share a horizontal
row with their logos beside their names; time appears above and location plus
TV/streaming details below. This CSS layout change preserves schedule filtering
and links. The production build passed. Browser checks at 320/390/768/1440 pixels
with light/dark color preferences verified horizontal team alignment, details below,
all six logos loaded, no overflow and initials after image failure. The mobile
screenshot was inspected. Health and HTTPS Home passed; the container was healthy.
Runtime settings and keys were preserved with no database or scraper changes.
Rollback image/container: `mynewsfeed-web:before-home-horizontal-matchups` /
`mynewsfeed-web-before-home-horizontal-matchups`.

October 14 Today & Tonight location/broadcast details were deployed as
`mynewsfeed-web:home-game-details-20261014`. Schedule location and TV/streaming text
appear beside teams on wider screens, below them on phones; valid stream URLs get
a Watch stream link. Missing details are omitted. Build and 123 relevant tests passed.
Live browser checks at 390/1440 pixels with light/dark color preferences verified
Dodger Stadium/FS1, GEODIS Park/Apple TV and Energizer Park/Apple TV, six loaded logos,
no overflow and image failure fallback. The desktop screenshot was inspected.
Health and HTTPS Home passed and the container was healthy. Runtime settings and keys
were preserved; no database or scraper changes were made. Rollback image/container:
`mynewsfeed-web:before-home-game-details` / `mynewsfeed-web-before-home-game-details`.
Publish reported existing System.Security.Cryptography.Pkcs 9.0.11 vulnerability
warnings; dependency changes were outside this layout release.

October 14 Today & Tonight team rows were deployed as
`mynewsfeed-web:home-team-rows-20261014`. Each team now has a logo beside its name
on a separate row using the schedule's badge styling, with initials for missing or
failed images. Matchup links and vs/@ relationships remain available.
Build and 123 personal-feature/feed helper tests passed. Live browser checks at
390/1440 pixels with light/dark color preferences verified six loaded logos, no
horizontal overflow and initials after forced image failures. The mobile screenshot
was inspected. Health returned ok, HTTPS Home returned 200 and the container was
healthy. Runtime settings and keys were preserved; no database or scraper changes
were made. Rollback image/container: `mynewsfeed-web:before-home-team-rows` /
`mynewsfeed-web-before-home-team-rows`.

October 14 Today & Tonight team logos were deployed as
`mynewsfeed-web:home-team-logos-20261014`. Each matchup displays available schedule
logos for both teams above its title, using safe image URLs, fixed 28-pixel sizes,
decorative alt text and image-error removal. Missing or failed images leave the
matchup readable. Build and 123 personal-feature/feed helper tests passed.
Live browser checks at 390/1440 pixels with light/dark color preferences verified
all six logos loaded for today's three fixtures, no horizontal overflow, and readable
titles after forced image failures. The mobile screenshot was visually inspected.
Health returned ok, tailnet HTTPS Home returned 200 and the container was healthy.
Runtime settings and keys were preserved; no database or scraper changes were made.
Rollback image/container: `mynewsfeed-web:before-home-team-logos` /
`mynewsfeed-web-before-home-team-logos`.

October 14 sports-only Today & Tonight was deployed as
`mynewsfeed-web:home-sports-only-20261014`. The box now builds exclusively from today's
sports fixtures, retaining the Wrexham/Sporting Kansas City/St. Louis City filters.
Movie and TV entries no longer enter this box; dedicated homepage sections remain.
The unused today-movie query was removed and the empty message now describes games.
Build and 123 personal-feature/feed helper tests passed. Live no-JavaScript browser
checks at 390/1440 pixels showed only Dodgers–Brewers, Nashville–Sporting Kansas City
and St. Louis City–Vancouver, preserved the separate TV section and found no overflow.
Health, Events, Movies, TV and Home returned 200, including tailnet HTTPS Home.
The container was healthy; runtime settings and keys were preserved with no database
or scraper configuration changes. Rollback image/container are retained as
`mynewsfeed-web:before-home-sports-only` / `mynewsfeed-web-before-home-sports-only`.

October 14 Today & Tonight soccer filtering was deployed as
`mynewsfeed-web:home-soccer-filter-20261014`. Home excludes EPL fixtures, keeps only
Wrexham Championship fixtures, and keeps MLS fixtures involving Sporting Kansas City
or St. Louis City on either side. Other timeline content and full Events schedules
retain their existing behavior. Build and 123 personal-feature/feed helper tests
passed, including home/away inclusion, exclusion and other-provider preservation.
Live no-JavaScript browser checks at 390/1440 pixels verified today's two selected
MLS fixtures, preserved baseball/movie/TV entries and no horizontal overflow.
Home, health, Events, Results and News returned 200, as did tailnet HTTPS Home.
The healthy container retains runtime configuration and persistent keys; no database
or scraper settings changed. Previous container and image are retained for rollback
as `mynewsfeed-web-before-home-soccer-filter` and
`mynewsfeed-web:before-home-soccer-filter`. Wrexham inclusion was covered by tests;
no Wrexham fixture was present in today's live homepage check.

October 14 homepage source rotation was deployed as
`mynewsfeed-web:home-source-rotation-20261014`. Home now selects up to ten enabled
sources with articles, using the latest successful news scrape's UTC hour for stable
random selection, and shows each selected source's newest story. Build and all 89
feed helper tests passed, including same-hour stability and next-hour rotation.
Live checks verified ten distinct sources and an unchanged selection on a repeated
request. Health and all ten checked public routes returned HTTP 200; tailnet HTTPS
Home also returned 200. The container was healthy. Existing runtime settings,
network, ports and persistent keys were preserved; no database or scraper settings
were changed. The previous container and rollback image were retained as
`mynewsfeed-web-before-home-source-rotation` and
`mynewsfeed-web:before-home-source-rotation` respectively.

The original codebase audit had 362 passing tests. Protected sports/video ownership
repairs raised coverage to 364. The personal-features/audit release passed 376 tests;
its favorites/My teams controls were later removed, while saved reading status,
paging and serialized result synchronization remained.

News cards initially moved dates into a footer row, then below headlines. Both
changes passed all 19 article-rendering checks. Admin CSS/navigation changes passed
seven authenticated page checks at 390/768/1024/1440 pixels. Admin Home was corrected
to `/admin`, followed by public/admin theme isolation. Later optimization replaced
DOM-rewritten admin structure with Razor markup, added 25-row SQL admin paging and
saved-state caching; all 419 tests passed. Four share open/close cycles changed
saved-storage reads from eight to zero in that focused browser measurement, not an
application-wide latency benchmark.

Sharing evolved from multiple copy actions to one Share dialog with Copy link,
phone-only SMS and supported native sharing. Desktop/iPhone/Android/iPad profiles,
focus/dismissal and mocked native APIs passed; physical app delivery was not tested.
The homepage VOD exclusion passed all 20 personal-feature tests and live timeline
comparison while keeping the separate upcoming digital preview.

The original public UI review covered 78 route/theme/width combinations. Its seven
main recommendations were implemented in the polish release, passing 430 tests and
15 public-view checks. Mobile first-table positions improved: Events 655→569 pixels,
NFL 619→498, Rankings 634→480, Wrexham results 703→547. Counts, heading focus, grouped
navigation, 44-pixel actions, Saved recovery and movie labels/filters were verified.
Lower-priority suggestions remain in [Interface](interface.md).

## Recorded images and rollback tags

The following inventory preserves image names from individual reports. “latest points
to this release” statements in those files applied at that time; they are superseded
by later rollouts. Some early updates patched static assets; subsequent full builds
included current source. Rollback must always use a compatible preserved runtime.

| Original report | Recorded image tags |
| --- | --- |
| deployment/MOVIES.md | `mynewsfeed-web:before-movies` |
| docs/admin-layout-deployment.md | `mynewsfeed-web:admin-layout-20261013`, `mynewsfeed-web:before-admin-layout`, `mynewsfeed-web:admin-home-20261013`, `mynewsfeed-web:before-admin-home`, `mynewsfeed-web:dark-scope-20261013`, `mynewsfeed-web:before-dark-scope` |
| docs/article-sharing-deployment.md | `mynewsfeed-web:article-sharing-20261013`, `mynewsfeed-web:before-article-sharing`, `mynewsfeed-web:share-menu-20261013`, `mynewsfeed-web:before-share-menu-cleanup`, `mynewsfeed-web:unified-share-20261013`, `mynewsfeed-web:before-unified-share` |
| docs/box-office-frontend.md | `mynewsfeed-web:before-box-office` |
| docs/boxoffice-forecasts-deployment.md | `mynewsfeed-web:boxoffice-forecasts-20261013`, `mynewsfeed-web:before-boxoffice-forecasts` |
| docs/boxoffice-top-10-deployment.md | `mynewsfeed-web:boxoffice-top10-20261013`, `mynewsfeed-web:before-boxoffice-top10` |
| docs/digital-vod-frontend.md | `mynewsfeed-web:before-standings-digital` |
| docs/english-soccer-deployment.md | `mynewsfeed-web:english-soccer-20261013`, `mynewsfeed-web:before-english-soccer` |
| docs/frontend-admin-optimizations.md | `mynewsfeed-web:frontend-admin-optimizations-20261013`, `mynewsfeed-web:before-frontend-admin-optimizations` |
| docs/homepage-no-vod-deployment.md | `mynewsfeed-web:homepage-no-vod-20261013`, `mynewsfeed-web:before-homepage-no-vod` |
| docs/my-newsfeed-nfl-standings.md | `mynewsfeed-web:nfl-standings-20261013`, `mynewsfeed-web:before-nfl-standings` |
| docs/news-card-formatting-deployment.md | `mynewsfeed-web:news-card-formatting-20261013`, `mynewsfeed-web:before-news-card-formatting`, `mynewsfeed-web:headline-date-20261013`, `mynewsfeed-web:before-headline-date` |
| docs/personal-features.md | `mynewsfeed-web:before-personal-features` |
| docs/professional-schedule-deployment.md | `mynewsfeed-web:before-professional-schedules`, `mynewsfeed-web:college-schedule-fix-20261013`, `mynewsfeed-web:before-college-schedule-fix` |
| docs/public-ui-polish-deployment.md | `mynewsfeed-web:public-ui-polish-20261013`, `mynewsfeed-web:before-public-ui-polish` |
| docs/rankings-layout-deployment.md | `mynewsfeed-web:rankings-layout-20261013`, `mynewsfeed-web:before-rankings-layout` |
| docs/standings-deployment.md | `mynewsfeed-web:before-standings-digital` |
| docs/standings-layout-deployment.md | `mynewsfeed-web:standings-layout-20261013`, `mynewsfeed-web:before-standings-layout` |
| MLS schedule/results update | `mynewsfeed-web:mls-schedule-results-20261013`, `mynewsfeed-web:before-mls-schedule-results` |
| Movie-section posters update | `mynewsfeed-web:movie-section-posters-20261013`, `mynewsfeed-web:before-movie-section-posters` |

## October 24, 2026 — News, Sports, Entertainment and YouTube fronts

Deployed `mynewspaper-web:frontpages-20261024` to `mynewsfeed-web`, using the
existing sql2025 / WebScraper runtime. Final image:
`sha256:77ba60512a836e02d01469c7425c93393e7bd43b2863e2e6ab3e9b669974c8a3`.
No database migrations, publisher registration or scraper deployment were required.
Environment, health check, ports, network, restart policy and persistent mounts
were compared with the previous runtime and preserved.

News has a lead/latest/supporting layout and compact story rows. Sports adds a
scrollable scoreboard and upcoming schedule; Entertainment adds releases and
tracked TV highlights. YouTube has a chronological featured-video grid. Existing
source/channel filters, article sharing/saving, Watch later and paging remain.
See the interface guide for selection and schedule scope.

Validation: application build and container publish/static-asset guard passed;
108 relevant helper/render tests passed. Preview and live browser matrices covered
320, 390, 430, 768 and 1440px in light/dark mode, representative populated articles
and videos, overflow, 44px card controls, mobile agenda order, source/channel
filtering, reload, unique cursor pagination and no-JavaScript forms/paging.
Sports/Entertainment navigation regression suites and sharing-dialog checks passed.
Phone and desktop screenshots were inspected. No physical-device verification was
available. Existing Pkcs package advisory warnings remain; the full integration
suite was not run against production.

Live health and ten affected/related public routes returned 200. Container health
was healthy and the initial runtime log had no failure/unhandled-exception markers.
Final markup and fingerprinted CSS were explicitly verified after live review caught
an intermediate image serving the older narrow-phone filter row. Final phone
filters were checked at all three required widths in both themes.

Original rollback image:
`sha256:df4b96088426241b0d59169d61784d378f99ca1ba9a4e2d7e557c4e74683bb0b`,
also tagged `mynewsfeed-web:before-mynewspaper-frontpages-20261024`.
Original container retained as
`mynewsfeed-web-before-mynewspaper-frontpages-20261024`; an intermediate container
is also retained with the `before-mynewspaper-frontpages-final-20261024` suffix.
Evidence and private runtime snapshots: `/tmp/mynewspaper-frontpages-deploy/`.

### October 24, 2026 — Publisher wrapping repair

A user screenshot exposed publisher names wrapping one letter per line in narrow
supporting cards. The shared article metadata grid had reserved columns for two
44px actions beside the publisher. Newspaper cards now give date, publisher and
actions their own rows, preserving readable names and button target sizes across
News, Sports and Entertainment.

Deployed `mynewspaper-web:source-wrap-20261024`, image
`sha256:97a325eba7c218ace8d43da86b8ee927fa8af43c507257a3d4b6d8f3d0750c77`.
The container build/static-asset guard passed, health is healthy, `/healthz` returned
200, and the final fingerprinted stylesheet was verified. Runtime settings and
persistent mounts were preserved. No database or scraper changes were needed.

Preview and live verification each passed 54 combinations: News/Sports/Entertainment
at 320, 390, 430, 700, 753, 900, 1024, 1150 and 1440px in light/dark mode. Checks
covered publisher width, single-line Nerdist labels, document overflow, 44px actions,
sharing dialog/Escape/focus return. Screenshots included every required phone width,
the reproduced narrow desktop arrangement and full desktop; representative phone
and desktop screenshots were inspected. No physical-device checks were available.

Previous image/container retained under the
`before-mynewspaper-source-wrap-20261024` rollback tag/name. Evidence resides in
`/tmp/mynewspaper-source-wrap-deploy/`. The original front-page release files and
other pre-existing local changes were preserved.
The live 40-view front-page regression matrix also passed, including source/channel
filters, reload, unique pagination and no-JavaScript forms/paging.

### October 24, 2026 — Coming up card spacing

Added 14px vertical/16px horizontal padding inside Coming up release/schedule cards
and a 10px gap between cards. This resolves titles/dates crowding bordered cards.
Deployed `mynewspaper-web:agenda-padding-20261024`, image
`sha256:a002221fb8f4e188e5ec4d7001da083edcccf708ce06f572eb0ac046c208ca22`.
Container publish/static-asset guard passed; runtime settings and mounts were
preserved; live health was healthy and `/healthz` returned 200. No database changes.

Preview and live each passed 20 page/width/theme checks: Entertainment and Sports
at 320/390/430/768/1440px in light/dark mode. Assertions covered computed inner
padding, card gaps, page overflow, 44px links and schedule navigation. Phone/desktop
screenshots were inspected, including the live movie cards. No real device was
available. Existing behavior/preferences and no-JavaScript links are unchanged by
this CSS-only update. Evidence: `/tmp/mynewspaper-agenda-padding-deploy/`.
Previous runtime/image retained under `before-mynewspaper-agenda-padding-20261024`.

### October 24, 2026 — Dedicated Sports and Entertainment hubs

Added `/sports` and `/entertainment` as bounded section overviews. Both show up to
eight source-diverse stories with the existing newspaper hierarchy. Sports leads
with scores and includes upcoming games; Entertainment includes release/TV previews
and up to three weekend box-office predictions/results. Detailed News pages now
focus on article filtering/paging and link back to the overview. Existing score,
schedule, movie, viewing-guide and category routes remain available.

Desktop Sports/Entertainment labels navigate directly to their hubs; adjacent
44px arrow controls open section dropdowns. Mobile section headings are hub links,
with correct active labels/Browse context. Reused publisher metadata and card
padding fixes remain present.

Deployed `mynewspaper-web:section-hubs-20261024`, image
`sha256:816bda113f841fcc1ef5c77b49237643ce73b5882f6a1d795d54824017aabbb8`.
Runtime environment, mounts/keys, ports, network, health check and restart policy
were preserved. Existing sql2025 / WebScraper data supplies the hubs; no database
migration, publisher registration or scraper deployment was required. Previous
container/image retained under `before-mynewspaper-section-hubs-20261024`.

Build, publish/static-asset guard and 119 relevant helper/render tests passed.
Preview checks covered both hubs at 320/390/430/768/1440px in light/dark mode,
including bounded content, score/release/TV/box-office panels, overflow, 44px links,
mobile agenda order, desktop dropdowns, hub/article navigation, reload, share-dialog
focus return and no-JavaScript links. The 1280px header boundary and saved appearance
after reload also passed. Existing Sports and Entertainment category/navigation
regressions and the 40-view News/YouTube filter/pagination/no-JavaScript matrix
passed in preview. Representative phone/desktop screenshots were inspected; no
physical-device check was available. Existing Pkcs advisory warnings remain.

Live health was healthy and twelve public/health routes returned 200, with no
initial runtime failure/unhandled-exception markers. Release evidence and private
runtime snapshots reside in `/tmp/mynewspaper-section-hubs-deploy/`.
Post-deployment verification passed the same 20 hub width/theme combinations and
the 40-view News/YouTube regression matrix, including live desktop/mobile hub links,
no-JavaScript navigation, source/channel filters, pagination and sharing. Live phone
and desktop screenshots were inspected. The deployed container remains healthy.

### October 24, 2026 — Plain hub navigation links

Removed bubble styling from Sports/Entertainment section navigation: no borders,
rounded backgrounds or filled containers; 44px vertical link targets remain. Recorded
the user's plain-section-link preference in the interface guide.
Deployed `mynewspaper-web:plain-hub-links-20261024`, image
`sha256:c75770fba969f20b7b51559f7480d91ec80f798477836b547dff6a5e9387dd41`.
Container publish/static-asset guard passed, runtime configuration/mounts were
preserved, health is healthy and `/healthz` returned 200. Previous container/image
retained under `before-mynewspaper-plain-hub-links-20261024`.

Preview and live each passed 20 Sports/Entertainment width/theme combinations at
320/390/430/768/1440px in light/dark mode. Verified transparent backgrounds, zero
borders/radius, 44px targets, no page overflow and headline-link navigation. Phone
and desktop screenshots were captured; representative preview/live phone screenshots
were inspected. No real device was available. This is a CSS-only change to existing
links. Evidence: `/tmp/mynewspaper-plain-hub-links-deploy/`.

### October 24, 2026 — Hub dropdown navigation

User clarified that hub section navigation should be dropdowns, rather than bubble
links or plain link rows. Replaced those rows with the existing native SectionSelect
component. Sports offers eight destinations and Entertainment seven, including each
hub's selected Home entry. Selecting a destination submits through the existing
local-only `/navigate` endpoint; a Go button supports JavaScript-disabled browsers.
Removed obsolete hub-link styling and corrected the maintained preference text.

Deployed `mynewspaper-web:hub-dropdown-20261024`, image
`sha256:50c613e906e8370b60a38345ed516bac74cb868a497aa8c0d4f03bd90c77ed63`.
Application build and container publish/static-asset guard passed, with existing
Pkcs package advisory warnings. Runtime settings/mounts were preserved, and
`/healthz` returned 200. Previous runtime/image retained under
`before-mynewspaper-hub-dropdown-20261024`. No database or collector changes.

Preview verification covered 20 hub/page/width/theme combinations at
320/390/430/768/1440px in light/dark mode, including 44px select height, current
selection, absence of link rows, dropdown destinations, mobile order, page overflow,
hub/article navigation, share-dialog focus and no-JavaScript select/Go navigation.
All 15 dropdown destinations also resolved successfully. Phone/desktop screenshots
were captured and representative phone screenshots inspected. No real device was
available. Evidence: `/tmp/mynewspaper-hub-dropdown-deploy/`.
The same 20-view hub matrix passed after deployment, including native dropdown
navigation and no-JavaScript Go submission. Live phone/desktop screenshots were
captured; the deployed container is healthy.

### October 24, 2026 — Hub links first inside section menus

Removed separate Sports/Entertainment hub links beside dropdown arrows. Each
section's header label now opens its complete dropdown, with Sports home or
Entertainment home first. Mobile groups use a heading followed by the same hub-first
link ordering. The on-page native hub selectors remain available. Active states and
44px menu targets are preserved; obsolete split-header styling was removed.

Deployed `mynewspaper-web:hub-menu-20261024`, image
`sha256:b88d56ffd16efed274140db45535af342cf039496f454590bbf452d551e055e3`.
Application build/publish/static-asset guard passed. Runtime configuration and mounts
were preserved; live container health was healthy and `/healthz` returned 200. No
database/collector changes. Prior container/image retained under
`before-mynewspaper-hub-menu-20261024`.

Preview menu checks passed 20 page/width/theme combinations at
320/390/430/768/1440px in light/dark mode: hub-first ordering, absence of split-header
links, active states, 44px targets, overflow, headlines navigation and no-JavaScript
hub links. The 20-view hub regression suite also passed, including native section
selectors, content/agenda order, share focus and no-JavaScript navigation. Phone and
desktop screenshots were captured and representative images inspected. No physical
device was available. Evidence: `/tmp/mynewspaper-hub-menu-deploy/`.
Both 20-view suites passed after deployment. Live mobile menu screenshots were
inspected, confirming the hub is the first link below its section heading.

### October 24, 2026 — Compact mobile story rows

User screenshot showed excessive space in mobile news cards, with all metadata
constrained beside a thumbnail and actions on a separate row. Below 700px, list and
supporting cards now pair an 80px thumbnail with category/headline, then use the
full card width for summary/date and a wrapping publisher/actions footer. Short
publishers share a row with 44px share/save controls; long names move the actions
below. Lead/latest cards reuse the compact footer. Publisher/action wrappers are
transparent to the existing desktop grids. No-summary rows no longer reserve a
large body grid area. Both initial and Load more articles use the shared markup.

Deployed `mynewspaper-web:compact-stories-20261024`, image
`sha256:763e829976ffa2e32c252001be6043dfab61528171d79a6e7a382b1fb2b8133c`.
Build/publish/static-asset guard and 108 relevant helper/render tests passed.
Runtime environment, mounts, network and port/restart settings were preserved.
Container health was healthy; `/healthz` returned 200. No database or collector
changes. Previous container/image retained under
`before-mynewspaper-compact-stories-20261024`.

Preview: 50 page/width/theme combinations covered News, Sports News, Entertainment
News and both hubs at 320/390/430/768/1440px in light/dark mode. Assertions covered
thumbnail/headline placement, full-width dates, 44px buttons, short/long publisher
footers, no overflow, sharing/Escape/focus and Load more row layout. Additional
40-view front-page/filter/pagination/no-JavaScript checks, 54 publisher wrapping
views and Home/News/Videos/Saved sharing regressions passed. Representative phone
and desktop screenshots were inspected, including a long-publisher fixture. No
physical-device check was available. Existing Pkcs advisory warnings remain.
Evidence: `/tmp/mynewspaper-compact-stories-deploy/`.
Post-deployment verification passed the same 50 compact-story combinations, the
40-view front-page regression matrix and the sharing regression suite. Live compact
and long-publisher screenshots were inspected. The deployment remains healthy.

### October 24, 2026 — More stories responsive images

Deployed `mynewspaper-web:more-stories-20261024`, image
`sha256:6e778fa0ae9799cb0c14e805fc73a8db9866e1ce623c39591f04ac2f8800dd16`.
Only More stories (including Load more) hides images below 700px and uses
168px-wide, 16:9 thumbnails on wider screens, matching homepage Top Headlines.
Build/publish/static-asset guard passed. No database/collector changes were needed.
Runtime environment, persistent mounts, ports, network and restart policy were
verified preserved. Previous container and rollback image retained under
`before-mynewspaper-more-stories-20261024`.

Live verification: healthy container and `/healthz` 200; News, both news sections,
Events, Standings, Rankings, Movies, Digital and Box Office returned 200; Results
redirected to Scoreboard and unauthenticated admin redirected to login. All 50
route/viewport/theme compact-story checks passed across News, both news sections
and both hubs at 320/390/430/768/1440px, including share/Escape/focus, publisher
wrapping, 44px actions, overflow and Load more. Twelve additional live checks at
320/390/430/700/768/1440px in both themes confirmed hidden mobile images and
168px/16:9 wider-screen thumbnails. Mobile and desktop screenshots were inspected.
No error-level entries appeared in the sampled startup logs; league-phase/results
sync completion was not observed in that sample. Real-device checks were unavailable.
Existing Pkcs dependency advisories remain. Evidence:
`/tmp/mynewspaper-more-stories-deploy/`.

### October 24, 2026 — More stories reference-image correction

The user's screenshot clarified that More stories should use large images above
cards, matching the featured story cards, rather than small images beside text.
Deployed `mynewspaper-web:more-stories-large-20261024`, image
`sha256:d188a1ed0402c0c50a27b2b4ce20ea916babf83f40038f52a5540743da1b3954`.
More stories now uses full-card-width, 200px-high images with two columns from
700px and three from 1024px. Mobile images remain hidden. Initial and loaded
articles share the layout; lead/latest/featured layouts are preserved.

Build/publish/static-asset guard passed. Container is healthy and `/healthz`
returned 200. Runtime environment, mounts, network, ports and restart settings were
verified preserved. No database or collector changes. Previous container/image
retained under `before-mynewspaper-more-stories-large-20261024` for rollback.
Preview and live: 50 route/width/theme compact-story checks passed at
320/390/430/768/1440px in light/dark modes, including share/focus, publisher
wrapping, 44px controls, overflow and mobile Load more. Additional image checks
covered 320/390/430/768/1200/1440px in both themes on the three news pages;
hubs without More stories were skipped in that image-specific check. Desktop
Load more confirmed full-width 200px images. Live desktop/mobile screenshots
were inspected. Main routes passed HTTP checks, including Results-to-Scoreboard
and admin-to-login redirects. Real-device checks were unavailable. Existing Pkcs
advisories remain. Evidence: `/tmp/mynewspaper-more-stories-large-deploy/`.

### October 24, 2026 — Hide mobile Top Headlines images

Deployed `mynewspaper-web:mobile-headlines-20261024`, image
`sha256:72846aa5d05c172f381ee7634052ca2a41fd6dbdbc25fa61afd0cffc5103b433`.
Below 700px, the Top Headlines lead/featured images are hidden, and supporting
headlines use the full card width. More stories stays text-only on mobile;
desktop images and large More stories cards retain their prior layout.

Build/publish/static-asset guard passed; container healthy and health endpoint 200.
Runtime environment, mounts, ports, network and restart settings were verified
preserved. No schema/collector changes. Rollback container/image retained under
`before-mynewspaper-mobile-headlines-20261024`.

Preview and live checks passed 50 route/width/theme cases across News, both news
sections and both hubs at 320/390/430/768/1440px in light/dark modes. Assertions
covered hidden mobile images, overflow, full-width text, 44px actions, publisher
wrapping, sharing/focus and Load more. Additional checks confirmed lead/support
image visibility before/after refresh, unchanged desktop 200px More stories
images and lead sharing/Escape/focus. Mobile screenshots at all three phone
widths were inspected. Main route smoke checks passed, including Results and
admin redirects. Real-device checks were unavailable. Existing Pkcs dependency
advisories remain. Evidence: `/tmp/mynewspaper-mobile-headlines-deploy/`.

### October 24, 2026 — Remove women's college basketball from scoreboard

Deployed `mynewspaper-web:womens-college-removal-20261024`, image
`sha256:a5fa7bd7b16882da50337510f4c3d30430c6129ee9f0e3dc858532fe6513fc40`.
The shared scoreboard reader excludes women's college basketball coverage,
follows and games, removing both its league and Big 12 Show options, including
Home and refresh fragments. College hub/schedules/standings/legacy results already
excluded it. Men’s college basketball and WNBA remain. No database or collector
changes were made.

Twenty-one scoreboard unit/integration tests passed, including assertions against
coverage/follows/games. Preview and live checks passed 32 viewport/theme/JS cases
at 320/390/430/1440px in light/dark modes, with and without JavaScript. Checked
absent league/conference choices and game cards, remaining men's/WNBA options,
filter submission, legacy removed-league links, Home and overflow; inspected
mobile screenshots at all three widths. Grouped Show selection, league/team/
conference/all submissions, legacy filter resolution and date links also passed.
Real-device checks were unavailable. Build/publish/static-asset guard passed;
container healthy, `/healthz` and affected scoreboard/College/events/results
routes returned 200. Runtime settings and mounts were verified preserved.
Rollback container/image retained under
`before-mynewspaper-womens-college-removal-20261024`. Existing Pkcs dependency
advisories remain. Evidence: `/tmp/mynewspaper-womens-college-removal-deploy/`.

### October 24, 2026 — Branded article image placeholder

Deployed `mynewspaper-web:branded-placeholder-20261024`, image
`sha256:8a8497b4e2669cb2640109c443cb11a8edc815903a8a5cd53155163af277d350`.
Replaced the generic missing-photo symbol with a shared local SVG containing the
existing myNewspaper icon and wordmark. Feed cards, homepage lead and Opinion
use the fallback for missing or failed article images. Actual photos cover it;
mobile newspaper images remain hidden. Video thumbnails retain their existing
fallback. No database/collector changes.

Build and publish/static-asset guard passed; 23 relevant feed rendering/Opinion
tests passed. Preview and live checks covered News and Home at 320/390/430/1440px
in both themes (16 combinations), including missing/broken-image fixtures,
Opinion fallback, hidden mobile feed images, asset HTTP 200 and overflow.
Phone and desktop fallback screenshots were inspected. Real-device checks were
unavailable. Container healthy; runtime settings/mounts preserved; main routes and
health endpoint passed HTTP checks. Prior container/image retained under
`before-mynewspaper-branded-placeholder-20261024`. Existing Pkcs advisories remain.
Evidence: `/tmp/mynewspaper-branded-placeholder-deploy/`.

### October 24, 2026 — Placeholder cache correction

The browser could reuse the previous generic SVG at `default-article.svg` for an
hour while loading newer CSS that enlarged it. Moved the branded asset to the
distinct `branded-article-v1.svg` URL and preserved the original generic asset for
older styles. Feed, lead and Opinion backgrounds now request the new filename.

Deployed `mynewspaper-web:branded-placeholder-cachefix-20261024`, image
`sha256:8ab27f4a52dee18dc53b4cbbadc7b93fa7a9f44fdda0aec23bd3beb9feed498e`.
Build/publish/static-asset guard passed; container healthy and health endpoint 200.
Runtime settings/mounts preserved. Previous container/image retained under
`before-mynewspaper-branded-placeholder-cachefix-20261024`. No database changes.
Preview and live checks passed 16 Home/News combinations at 320/390/430/1440px
in light/dark modes, including missing/broken-image fixtures, Opinion fallback,
hidden mobile images and overflow. An assertion confirmed the old placeholder
URL was never requested. The exact Entertainment lead from the user's screenshot
was checked on the deployed build; desktop and phone screenshots were inspected.
Real-device checks were unavailable. Evidence:
`/tmp/mynewspaper-branded-placeholder-cachefix-deploy/`.

### October 24, 2026 — Logo-only article placeholder

Removed the visible myNewspaper wordmark from the fallback and centered the
existing logo on the same background. The new `branded-article-v2.svg` filename
avoids reusing the previous cached image. Article photos and mobile image hiding
are unchanged.

Deployed `mynewspaper-web:logo-placeholder-20261024`, image
`sha256:013d08ff5da5887fea9f0a25e7196a14fbff2223aa73f6ff873fa51009de6c85`.
Build/publish/static-asset guard passed. Container healthy, health endpoint 200;
runtime settings/mounts preserved. Previous container/image retained under
`before-mynewspaper-logo-placeholder-20261024`. No database changes.
Preview and live checks passed 16 Home/News combinations at 320/390/430/1440px
in light/dark modes: missing/broken images, Opinion fallback, hidden mobile
images, overflow, new asset HTTP 200 and absence of SVG text elements. Inspected
phone screenshots and the actual Entertainment lead from the user's screenshot.
Real-device checks were unavailable. Evidence:
`/tmp/mynewspaper-logo-placeholder-deploy/`.

### October 24, 2026 — Where to watch provider rows

Title search now shows poster-and-provider rows inspired by the user's reference:
poster, title/year/type and US provider logos grouped as Stream, Free, Free with
ads, Rent and Buy where available. Mobile uses a compact poster/title row with
provider groups below. Browse pages retain the poster grid. Provider metadata
reuses cached eligibility responses; no extra detail fetch per result. Names and
initials remain available when logos fail. Existing credits, mixed search,
personal service matches, sorting, pagination and details modal/no-JS links remain.
Prices, season counts, ratings and direct provider purchase links are not inferred.

Deployed `mynewspaper-web:watch-results-20261024`, image
`sha256:f40f3baad6a9445bb1f81c659d5095f9024a1329d1ebd7e56f5bceba68e6fb82`.
Build/publish/static-asset guard and 27 watch-related unit tests passed, including
US grouping, safe/missing logos, cached links and provider-error metadata. Preview
and live checks passed 20 viewport/theme/JS combinations at 320/390/430/768/1440px
in light/dark modes, covering Arrow logos/categories, poster placement, mobile
stacking, overflow, 44px action links, sorting/search preservation, modal Escape/
focus, no-JS details and browse-grid regression. Separate sort, mixed-search/
navigation and stable 20-title pagination suites passed before/after rollout.
Phone and desktop screenshots were inspected. Real-device checks unavailable.

Container healthy and health endpoint 200. Watch search/detail and main routes
passed HTTP checks; runtime environment, mounts/network/ports/restart settings
verified preserved. No database/collector changes. Previous container/image kept
under `before-mynewspaper-watch-results-20261024`. Existing Pkcs advisories remain.
Evidence: `/tmp/mynewspaper-watch-results-deploy/`.

### October 24, 2026 — Compact Where to watch search and browse

Deployed `mynewspaper-web:watch-compact-20261024`, image
`sha256:cbe552dcd9b66754e0592ab8d9e187371ff9004d464be6954336fba057bd24dd`.
Search, service settings link and browse selectors share a compact panel; content
type and genre align side by side on desktop and stack on phones. Sorting shares
a toolbar with the results note. Existing provider result rows and browse cards
remain. Service hyperlinks are deferred. No database/collector changes.

Build/publish/static-asset guard and 27 watch-related tests passed. Preview and
live checks passed 16 combinations at 320/390/430/1440px in light/dark modes,
with and without JavaScript, covering content type, genre, title search, sort,
refresh, clear search, service query preservation, overflow and 44px controls.
Phone screenshots at all three widths and desktop screenshots were inspected.
Additional 768px light/dark checks, saved-service restoration/refresh and browse
details modal/Escape passed on preview and live. Real-device checks unavailable.
Existing Pkcs dependency advisories remain.

Container healthy; health endpoint and main routes passed HTTP checks, including
expected Results/digital/admin redirects. Runtime environment, mounts, network,
ports, health check and restart settings verified preserved. Previous
container/image retained under `before-mynewspaper-watch-compact-20261024`.
Evidence: `/tmp/mynewspaper-watch-compact-deploy/`.

## Front-page logo — October 28, 2026 (Central)

Replaced the folded-page “n” with the approved miniature front-page mark from
`new development/logo-front-page-handoff.md`. Updated phone/desktop headers,
Home and Editions mastheads, brand SVGs, placeholder mask, favicon and install
icons. Existing theme classes remain in use. Manifest/Apple icon references now
use `newspaper-front-*`; the service worker caches identity assets under
`mynewsfeed-pwa-v6`. Old icon files remain for compatibility.

Release image: `mynewspaper-web:front-page-logo-20261028`
(`sha256:f55b915561abb656c6270bb44aedbe96ee33b7a5a6c56cd93a8ad266882a0e4e`).
Rollback image: `mynewspaper-web:before-front-page-logo-20261028`; the previous
container is retained, stopped, as
`mynewsfeed-web-before-front-page-logo-20261028`. Runtime environment, key volume,
network, IPv4/IPv6 host port 8085, restart policy and log rotation were preserved.
No database changes or scraper rollout were needed.

Validation: .NET build passed with zero warnings/errors; all 955 .NET tests
passed; ESLint and formatting checks for the changed supported files passed.
Preview and deployed browser checks cover Home and Editions at 320/390/430px
in Kansas/Cardinals/monochrome, light/dark, plus 744/1024/1440px in Kansas,
light/dark (48 page/width/scheme/theme combinations per pass). Checks inspect
front-page SVG geometry, inherited ink colour, photo opacity and horizontal
overflow. The deployed checks also assert the selected theme/scheme. Temporary
verification profiles are deleted afterward. Login/administration were exercised
during setup, and SVG/PNG responses, manifest icon references and service-worker
version were checked. Representative phone, tablet/desktop and standalone
logo/placeholder screenshots were visually inspected.

The live container is healthy and `/healthz` returns 200. Unauthenticated content
requests retain the profile gate; `/admin` redirects to login. HTTPS Editions
returned 200, and a browser installed the active service worker and confirmed
`mynewsfeed-pwa-v6` in Cache Storage. This workspace required an explicit browser
DNS mapping to the existing Tailscale address for that HTTPS check; the proxy
configuration was unchanged. Evidence and private runtime snapshot:
`/tmp/logo-front-deploy/`.

Limits: Chromium emulation only; no physical phone, Safari favicon or native
home-screen icon refresh was verified. Existing iOS shortcuts may need removal
and re-adding to display the new icon.

## Settings profile spacing — October 28, 2026 (Central)

The Settings → Your profile colour legend inherited Bootstrap's float and occupied
the first grid cell, leaving an uneven first row. Reset the legend float/width,
put an 8px gap below it, and limit the choice grid to the name field's 28rem width.
The choices now start below “Color” and form aligned rows on phone and desktop.

Deployed `mynewspaper-web:settings-profile-spacing-20261028`
(`sha256:93ab6319537435cc79cb303cb6bbec5db33c9453fdc550f00f0317152504c86f`).
Built from committed source plus this CSS change, excluding unrelated working-tree
profile changes. Previous image/container retained as
`mynewspaper-web:before-settings-profile-spacing-20261028` /
`mynewsfeed-web-before-settings-profile-spacing-20261028`. Runtime environment,
key mounts, network, IPv4/IPv6 port bindings, restart and log settings match the
previous deployment. No database changes.

Validation: release image build and CSS formatting passed. Before and after
deployment, Chromium checked 320/390/430/744/1440px in light and dark: legend
above choices, grid/name alignment, 44px choice targets, no horizontal overflow,
and colour selection surviving save/reload. Representative mobile and desktop
screenshots were inspected. The deployed check also exercised colour saving with
JavaScript disabled at 390px. Verification used temporary profiles, removed after
the checks. Container health and `/healthz` passed. No physical device was available.
Evidence: `/tmp/settings-spacing/`.

## Collapsible Settings sections — October 28, 2026 (Central)

All nine Settings sections now use native details/summary panels, like Arrange
Settings sections. They start closed and open independently with pointer or
keyboard controls. Existing anchor IDs are retained on the expandable headings;
section links and the phone Jump to menu open their target, including repeated
links and page reloads. Scroll offsets keep headings below the sticky header.
Reordering retains the panel contents and open state. Saved preferences are
unchanged by opening or closing a panel.

Form success/error messages open the relevant panel on the server. TV searches
keep TV shows open, and the no-JavaScript services form keeps its submitted
selection visible. Message-to-section mapping is shared with the message component.

Deployed image: `mynewspaper-web:settings-panels-20261028`
(`sha256:b579fd2c0382542db1011227e1ff075534a3c887cbcc9dc1f313c38e0c077735`).
The image was built from `02138e9` plus this Settings implementation, separately
from the concurrent profile-setup release. Previous image/container retained as
`mynewspaper-web:before-settings-panels-20261028` /
`mynewsfeed-web-before-settings-panels-20261028`. Runtime environment, keys volume,
network, IPv4/IPv6 bindings, restart and logging settings were compared and
preserved. No schema or collector changes.

Validation: release build passed; workspace build had zero warnings/errors;
all 955 tests against the isolated release source passed. Targeted ESLint and
formatting checks passed. Chromium viewport checks covered 320/390/430/744/1440px,
light/dark, before and after rollout: all nine panels, pointer/Enter/Space controls,
44px summaries, section jumps/deep links, reordering, theme selection and profile
saves/reloads, visible feedback, heading offsets and no horizontal overflow.
No-JavaScript checks exercised profile/services submission and server-opened
profile/TV/channel/request messages. Updated the existing profile-theme and
services browser checks to open the relevant panels; added
`tests/browser/settings-panels.browser.cjs` for the new behavior. Temporary test
profiles were removed. Representative phone and desktop screenshots were
visually inspected. The live container is healthy and `/healthz` returns 200.
No physical-device or Safari check was available. Evidence: `/tmp/settings-panels/`.
