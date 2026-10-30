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

No schema, queries, carousel behavior, channel tabs or saved-video sidebar were
added. The absent Jump to component has no speculative CSS. Content height remains
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

Release outcomes, image IDs and outstanding verification limits are recorded in
[release history](release-history.md).
