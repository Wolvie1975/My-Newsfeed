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
