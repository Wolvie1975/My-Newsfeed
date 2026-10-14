# Public interface and administration

Current interface notes consolidated October 13, 2026 (Central). Data contracts
are in [Sports](sports.md) and [Entertainment](entertainment.md); historical releases
are in [Release history](release-history.md).

## Navigation and layout

Desktop navigation groups Sports and Entertainment with native disclosures. The
mobile menu has News/Sports/Entertainment/Your links groups, including Saved and
Administration. The small public header omits admin sign-in. Native navigation and
metadata disclosures work without JavaScript; enhanced menus dismiss on selection
or Escape. Administration Home/brand goes to `/admin`; View newsfeed goes to the
public site. Admin heading/navigation markup is rendered in Razor, not rewritten by
a JavaScript DOM observer.

Mobile introductions and metadata are compact enough to bring tables higher.
Toolbar and inline counts are mutually exclusive by breakpoint. Programmatically
focused page headings do not show the large interactive focus rectangle; links,
buttons, forms and disclosures retain visible keyboard focus. Compact standings and
polls reveal full statistics via native checkboxes; secondary source/refresh metadata
uses native disclosures while season, poll date and warnings remain visible.

Wide tables scroll within labeled, keyboard-accessible containers without page-wide
overflow. Names remain readable without logos; mobile names wrap and do not require
a sticky column. Use semantic headings, captions and column/row headers. Keep provider
text HTML-encoded and public links/images restricted to HTTP(S); new-tab links use
`rel="noopener noreferrer"`. Do not expose raw metadata, diagnostic errors or scraper
configuration as public UI.

Admin layout CSS targets MainLayout's `div.page` wrapper. PublicLayout's `main.page`
must not inherit admin background/text overrides, especially in dark mode. At widths
up to 900 pixels the admin sidebar becomes collapsible; tables scroll internally.

## News, saved articles and sharing

Article cards show publication date/time directly below the headline, before the
summary. Source labels and actions occupy the footer; long source names wrap. Saved
cards use the same presentation. Publication time is not substituted with collection
time. News keeps source/category selection, deterministic cursors, enhanced Load more
and usable no-JavaScript paging.

Saved articles and read/unread status use browser localStorage, shared across tabs
but not synchronized across devices. Clearing site storage removes these preferences;
blocked storage leaves server-rendered pages usable. Old saved records begin unread.
An empty library explains bookmarking and offers Browse news. A nonempty library with
no filter matches offers Show all saved. Hide the filter/count when the library is
empty. Render signatures include library size so empty-state guidance updates correctly.

The public script initializes newly added controls, caches saved articles in memory,
and invalidates on cross-tab changes/clearing. Mutations clone before writing so failed
writes don't alter the cache; explicit Save/read actions update affected controls.
Saved text uses textContent rather than parsing arbitrary HTML.

Share is the single article sharing control; its dialog includes Copy link, phone-only
SMS, and native Share via apps when supported. Share/Save hit areas are 44 × 44 pixels.
Copy link includes article data and uses a fallback textarea inside the open dialog
when clipboard APIs are unavailable, including ordinary HTTP contexts. SMS appears
for phone user agents (iPhone/iPod/Android Mobile or mobile UA hints), not merely a
narrow desktop/iPad viewport. Encode article title and safe URL in the platform's SMS
URI body format. No message is sent automatically. Native sharing needs a secure
context and navigator.share; installed app availability is device-controlled.
The removed Messenger-specific copy action must not be reintroduced as a second copy
control. Modal dismissal restores focus. Browser tests use mocked native/share APIs;
physical messaging app behavior was not verified.

Team favorites, the My teams page and favorites-only controls have been removed.
Public sports views show followed leagues and use schedule/sport/follow dropdowns.
Results supports today/yesterday; its earlier today-only revision is superseded.

## Movies and homepage

Subnavigation distinguishes all releases, theaters, subscription streaming and digital
rental/purchase. Movie type selections survive paging. Digital has its own mobile
section label and current-page marker; selected-provider scope remains explicit.
Home's Today & Tonight omits digital rental/purchase entries but keeps its separate
upcoming digital section. Empty Results offers a direct yesterday link.

## Administration and health

Sources, YouTube feeds, sports event feeds and tracked television series use SQL
count/Skip/Take paging, 25 rows per page with stable ID tie breakers. Changing source
category resets paging; reload/deletion clamps to the last available page. Page
changes clear pending deletions. Movies/Videos use 50-row public batches.

Provider-owned/shared sports events have read-only attribution and protected delete
controls. Handlers recheck ownership and recover expected foreign-key rejections;
compatible legacy events remain editable/deletable. Video assignment offers only
matching-channel feeds and validates the current ChannelId on save. Category forms
prevent native browser submission while asynchronous handlers execute.

A successful empty provider schedule is healthy, not Nothing collected yet.
Upcoming health counts exclude the same completed/canceled statuses as public queries.
Schedule coverage reports missing result follows and sync errors without silently
enabling intentionally disabled feeds. All admin pages require authorization.

## Verification and remaining polish

Check public and authenticated admin routes at phone/tablet/desktop widths in both
themes. Exercise navigation, keyboard focus, source selection, paging, no-JavaScript
fallbacks, Save/read/filter/remove recovery, cross-tab updates and share dismissal.
Contain horizontal scrolling and show counts once. Use physical-device checks for
SMS/app behavior when needed; responsive emulation does not establish delivery.

Remaining review suggestions: use clearer Time not announced wording for date-only
items, clarify entry points between daily and Championship season results, refine
missing-artwork placeholders, and standardize secondary metadata across views.
The seven higher-priority October 13 UI review recommendations were implemented.
