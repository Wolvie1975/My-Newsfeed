# Wide screens and 4K

Implemented October 30, 2026 (America/Chicago), from the wide-screens handoff in
`new development/`. This document describes the shipped layout rather than the
optional mockup features.

At 1920 CSS px, the reader page fills the viewport with 56px side padding. Home
uses four columns followed by the More from today band and two wide desks. At
2560px, all six Home columns share one row and the band disappears. Entertainment
headlines use one list inside the narrow fifth column. News/Sports side sections
and Entertainment box office/TV sit beside each other. Headline cards use four,
then five columns; video cards use five, then six. At 3200px the reader shell uses
CSS `zoom: 1.25`, retaining the 2560px breakpoint rules. Other public pages inherit
the wider container. Admin uses its separate `.adm` shell and is unchanged.

The existing desktop header has a later, more specific fixed-width padding rule;
`broadsheet.css` overrides that rule at 1920px to align it with the page. YouTube
card counts and the featured-card span also need the existing theme selector's
specificity. The Home wrapper uses `display: contents` for its two original grids
only at wide widths; DOM and keyboard order remain the original order, even when
the Scoreboard column moves into the first row at 1920px.

The initial wide-screen release changed layout only. The follow-up YouTube sidebar
adds a third column at 2560px on the first-page featured front. By channel lists the
existing category links with counts of videos published today in the site's display
zone. One grouped query applies the viewer's followed-channel filter and the UTC
start/end of the site-local day, including daylight-saving transitions. Counts
cover the viewer's categories independently of the selected listing filter.

Watch later shows the current profile's three most recently saved videos from the
same SharedArticles store as Saved and VideoSaveButton. Copies survive feed expiry.
The initial links are server-rendered; public.js reuses Saved's existing cache,
save/remove responses and refresh to keep the list current without a new poller.
The section hides when empty. Below 2560px the sidebar is hidden and the existing
category controls remain. No schema, carousel behavior or channel-tab changes were
needed. The absent Jump to component has no speculative CSS. Content height remains
variable; six columns do not guarantee that every section fits above the fold.

## Verification

The reusable check is `tests/browser/wide-screens.browser.cjs`. It accepts BASE_URL,
ADMIN_BASE_URL, ADMIN_PASSWORD (for a temporary live reader profile), STORAGE_STATE,
SCREENSHOT_DIR and optional comma-separated WIDTHS. The default widths are 320,
390, 430, 768, 1024, 1280, 1440, 1920, 2560, 3200 and 3840px in light/dark. It checks
six fronts, page/header padding, column counts and overflow, plus wide secondary
pages, phone menus, 4K sticky positioning/share dialog and two additional schemes.
Temporary live profiles are deleted in cleanup. Existing phone/front/headline and
YouTube suites cover controls, persistence, touch targets and no-JavaScript paths.

The sidebar check is tests/browser/youtube-sidebar.browser.cjs. It requires a
profiles-enabled site and ADMIN_BASE_URL/ADMIN_PASSWORD to create and delete its
own disposable profile, plus BASE_URL and optional SCREENSHOT_DIR/WIDTHS. It checks
real save/remove updates, latest-three ordering, category links, plain-text saved
titles, mobile controls, themes, server-rendered no-JavaScript bookmarks and the
no-JavaScript category form. No household Saved list is changed by this check.

Release outcomes, image IDs and outstanding verification limits are recorded in
[release history](release-history.md).

## Complete Sports tables and hourly selection

The Sports front now renders every team and every standings group for its selected
competition on desktop, plus all teams ranked 1–25 in the selected poll. Tables keep
provider/rank order. Both full-view links retain the selected source. Phone folds
keep their five-row summaries and links to the complete views.

Each panel uses a deterministic shuffled cycle over enabled sources with nonempty
eligible snapshots. Existing hidden-provider and college-season rules still apply.
Selection advances on the hour and remains consistent for readers within that hour
while the available sources stay the same. The web clock drives selection; it does
not start the scraper or depend on a successful scrape. Existing snapshots remain
the data source. A visible desktop Sports page fetches fresh server-rendered panels
at the hour, retains the last good tables on network failure, and retries. Hidden
pages resume when visible; focused links/dialogs defer replacement. Phones and
no-JavaScript readers pick up the selection on navigation/reload.

Preview verification covered 320/390/430/1024/1440/1920/2560/3840px in light and dark,
with screenshots reviewed. Checks compared complete standings counts with the
matching full page, asserted 25 rank-ordered rows, exercised phone folds/full-view
links, preference persistence, no overflow, automatic panel refresh and the
no-JavaScript poll link. Rotation tests cover the hour boundary, query-order
independence, a complete cycle, and empty/single-source inputs. Live evidence is
recorded with the release in `docs/release-history.md`. No schema/collector changes
were required; physical devices and non-Chromium browsers were unavailable.

## Compact Home masthead follow-up

From 1920 CSS px, Home shows its existing tile/wordmark in the navigation and hides
the large centered masthead logo. The date/greeting move to the left, followed by
a boxless four-period weather row and the existing Live now/Coming up box. The
heading remains an h1 and Refresh retains its normal navigation behavior. Below
1920px, the prior masthead and header rules continue to apply.

The revised handoff's unconstrained auto weather track overlapped the greeting
at 1920px with long summaries. The implementation bounds the three grid tracks,
reserves enough space for the real site font and Refresh button, scales the greeting
from about 54px to 72px, and gives each weather period a shrinkable text track.
The Kansas City link sits below the four periods. Weather aligns with the greeting's
bottom edge; long summaries wrap without widening the page. Existing weather icons,
selection logic and data are unchanged. No queries, schema or collector changes
are required. The 3200px CSS zoom rules remain in effect.

The focused browser test exercises 320/390/430/768/1024/1280/1440/1920/2560/3200/3840px,
light/dark, additional Monochrome/Cardinals schemes, normal Refresh, profile theme
persistence, navigation branding, original smaller-screen layouts, phone folds and
no-JavaScript Refresh/folds. Desktop checks stress a longer greeting and four long
forecast summaries by changing only the test DOM. Live and preview results and
limits are recorded in release history; screenshots/logs remain in ignored
`test-results/browser/home-masthead-*` directories.
