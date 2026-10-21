# Release history and verification

Historical evidence consolidated October 13, 2026 (Central), from the former individual
handoffs and deployment reports. Counts are observations for their original checks,
not permanent acceptance totals. Recorded rollback tags describe those deployments;
verify an image still exists before using it. Runtime configuration and persistent
keys were preserved by the documented releases unless otherwise stated.

The most recent verified web release is `mydailyfeed-web:digital-provider-20261021`,
with rollback recorded as `mynewsfeed-web:before-digital-provider-20261021`.
Its container was healthy on October 21, 2026 (Central). This is a dated
observation, not live hosting status.


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
