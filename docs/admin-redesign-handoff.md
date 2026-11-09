# Admin redesign handoff

Written October 26, 2026 (America/Chicago) against `develop` @ `588809d`. Verify current repository state before
making changes; line numbers below are approximate.

**Approved mockups:** the myNewspaper canvas, **Admin** page (https://claude.ai/artifact/JVQ8mU7R63Z1cziNpeW9Z5):

| Artboard                                  | Shows                                                                                                               |
| ----------------------------------------- | ------------------------------------------------------------------------------------------------------------------- |
| Admin · Scraper status — desktop / phone  | Status home: health line, area tiles, Needs attention, Catch-up tasks, news sources table                           |
| Admin · Sources — desktop / phone         | List pattern: toolbar (search, category, status filter), status pills, switches, ⋯ menu, pager; cards on phones     |
| Admin · Edit source — desktop / phone     | Editor pattern: breadcrumb, labelled fields with help, enabled switch, save bar with unsaved state, health panel, Remove panel |

**Goal:** the admin area looks like the rest of myNewspaper (Newsreader + Schibsted Grotesk, `#0051ba` blue, `#ffd34d`
yellow, double-ruled headings) instead of the stock Blazor template, and works at phone widths without sideways
scrolling.

**Scope rule:** everything new is scoped under one `.adm` root class, so public pages are untouched. Bootstrap stays
loaded; `admin.css` restyles the Bootstrap classes the admin pages already use (`.btn-*`, `.form-control`, `.table`,
`.alert`), so pages that haven't been converted yet still improve.

The handoff's full code listings were applied as written unless the implementation notes at the end say otherwise;
the source of truth is now the code (`wwwroot/admin.css`, `Components/AdminIcon.razor`, `Data/AdminNavCounts.cs`,
`Components/Layout/MainLayout.razor`, `Components/Layout/NavMenu.razor`, `Components/Pages/Admin/AdminContent.razor`,
`AdminPageHeader.razor`, `StatusBadge.razor`, `AdminSwitch.razor`, `Components/AdminPager.razor`).

## Phases

1. **Shell:** `admin.css` (tokens for light and dark, sidebar, main column, cards, tiles, pills, toolbar, tables,
   `<details>` row menu, switch, forms and save bar, confirm panel, Bootstrap restyles, phone rules below 900px with a
   CSS-only menu toggle and table-to-card swap). Remove the template CSS (`MainLayout.razor.css` except
   `#blazor-error-ui`, `NavMenu.razor.css`, and the admin block in `app.css`). `AdminIcon` (named stroke icons),
   `AdminNavCounts` (problem counts from the same `ScraperHealth` report as the status page), a rewritten `NavMenu`
   (fixes the duplicate Home / Scraper status entry; "Sports event types" becomes **Event feeds**), a rewritten
   `MainLayout`, and `AdminContent` (an `ErrorBoundary` inside each interactive page; a layout-level boundary would
   never see page event-handler errors because the layout renders statically).
2. **Shared pieces:** `AdminPageHeader` (crumbs, eyebrow, title, badge, description, actions), `StatusBadge` as an
   `.adm-pill`, `AdminSwitch` (a `<button role="switch">`), and `AdminPager` restyled (with a noun) and used on Pages,
   YouTube videos and Sports events.
3. **Pattern pages:** Scraper status (health line, area tiles, Needs attention, Catch-up tasks, per-area tables through
   `HealthTable`), Sources (search, category incl. Uncategorized, All / Enabled / Disabled / Errors with counts,
   filters in the address, switches, ⋯ menu, Disable-instead confirm, phone cards) and the source editor
   (`EditContext` save bar with the changed fields, Save disabled until dirty, health aside, Remove panel).
4. **Roll out:** every admin page wrapped in `AdminContent` with `AdminPageHeader`, then the list or editor pattern
   (see the table in the original handoff: Schedule coverage, Categories, Pages, Page editor, YouTube feeds, Feed
   editor, YouTube videos, Video editor, Event feeds, Events, Event editor, Results follows, Follow a league or team,
   TV series, Login).
5. **Verify** (AGENTS.md): 320, 390, 430, 744, 820, 1024, 1440 in light and dark; shell (menu without JavaScript,
   current page, counts agree with the status page, View site, Sign out); status states; Sources filters, Back keeps
   them, switches, ⋯ menu, Disable instead / Delete; editor save bar and errors; no horizontal scroll at 320px;
   contrast 4.5:1 for pills, help text and sidebar labels in both themes; public pages unchanged.

## Notes carried from the handoff

- Phone menu dot (red dot on the menu button when something needs attention) is optional; it needs the counts in a
  scoped service the layout can read.
- "6 runs in a row" on the editor's health panel needs a failure counter the scraper doesn't store; left out.
- Check the "Uncategorized sources appear in Headlines" help text against how the site treats uncategorized sources.
- Fold in the review's bug fixes while touching pages: unhandled `InvalidOperationException` on event-type delete,
  sync-after-follow failure, scraper status load errors.

## Implementation status (October 26, 2026, release `admin-20261026`)

Done:

- **Phase 1:** `admin.css` (also: sign-in card, dark alerts, dark select arrow, inline code, the switch's off state
  darkened to `#6f7a8f` for 3:1 against the surface, `#blazor-error-ui` moved in), template CSS removed,
  `AdminIcon`, `AdminNavCounts` (unit-tested), `NavMenu`, `MainLayout` (the admin menu shows only when signed in, so
  the sign-in page is a centered card), `AdminContent`.
- **Phase 2:** `AdminPageHeader`, `StatusBadge`, `AdminSwitch`, `AdminPager` (with `Noun`; used on Sources, Pages,
  YouTube videos and Events).
- **Phase 3:** Scraper status, Sources list and source editor as specified. Uncategorized help text checked: such
  sources appear in Headlines under All categories. Deleting from the editor returns to `admin/sources?deleted=…`.
- **Phase 4 (partly):** every admin page is wrapped in `AdminContent` with `AdminPageHeader` (eyebrows from the table;
  "Sports event types" is now **Event feeds** in the menu, heading and title); the other editors sit in a card; TV
  series following is an `AdminSwitch`; the TV follow form shows its own heading only on the public page; older
  Bootstrap tables keep a 760px minimum and scroll inside their card on phones; long feed URLs ellipsize. Bug fixes:
  deleting an event feed with shared attribution reports why (and Delete is hidden for such feeds); a failed first sync
  after following a league no longer errors the page (`admin/results?syncPending=1`); scraper status load and catch-up
  errors show a message.
- **Phase 5:** `tests/browser/admin-layout.browser.cjs` (needs `ADMIN_PASSWORD`; read-only): 21 pages at
  320/390/430/744/820/1024/1440 light and dark without sideways scroll or script errors, sidebar only above 900px,
  phone menu opens and closes, active link, WCAG AA text contrast (shared `contrast-audit.cjs`), menu count equals the
  status page's problems plus stale, Sources filters in the address and kept by Back, editor save bar.

Not done yet (next batch):

- The full list pattern (toolbar, status pills, switches, ⋯ menu, phone cards) on Categories, Pages, YouTube feeds,
  YouTube videos, Event feeds, Events and Results follows; they use the restyled Bootstrap tables meanwhile.
- The editor pattern (save bar, health aside) on the page, feed, video and event editors.
- Phone menu red dot; Tab-away still leaves a ⋯ menu open.

## TV series Delete (October 27, 2026, deployed as `tvdelete-20261027`)

- TV series rows have a ⋯ menu with **Delete…** and a confirm panel (Stop following instead / Delete series /
  Cancel). Delete removes only the `TvTrackedSeries` row via `TvQuery.RemoveAsync`; series and episodes stay as history
  and reattach if the show is added again. A row the scraper holds a lease on is not deleted ("importing right now").
  Note docs/entertainment.md still prefers stopping over deleting; re-seeding with `--configure-tv` may re-add a deleted
  seeded show.
- Shared fixes: ⋯ button and menu items 44px; ⋯ menus close on choosing an item, outside click or Escape (`public.js`);
  `.table-responsive` contains `.visually-hidden` labels and makes room for a ⋯ menu opened on the bottom row.
- Verified locally: 734 .NET tests (incl. `Remove_drops_tracking_but_not_while_the_scraper_holds_a_lease`);
  `tests/browser/tv-series-delete.browser.cjs` (needs `ADMIN_PASSWORD` and `TV_FIXTURE_TITLE`, a throwaway followed
  series) at 320/390/430/744/1024/1440 light and dark; `admin-layout` and `section-hubs` suites; screenshots reviewed.
  `mixed-search-nav` failed only on Where to watch title search, unavailable locally (no provider key in this container).
  The local database is shared with the running scraper. Not checked on a real device or against the live build.
