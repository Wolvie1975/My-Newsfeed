# Public interface and administration

Current interface notes consolidated October 13, 2026 (Central). Data contracts
are in [Sports](sports.md) and [Entertainment](entertainment.md); historical releases
are in [Release history](release-history.md). Home-page completion filtering and
automatic appearance notes were updated October 17, 2026 (Central).

## Where to watch: approved typed-search exception

On October 20, 2026 the user explicitly approved typed title search for movie/TV streaming lookup and authorized implementation and deployment. This is a scoped exception to the tap-to-link guideline below. It does not authorize a global search field. Entertainment → Where to watch offers movie/TV tabs, popular titles, genres, title search and a viewing-options modal with a regular-page fallback.

## Standing product guideline: tap to link

User direction recorded October 19, 2026: **“This is a tap to link site and not a
typing one.”** Treat this as an ongoing requirement for future public features and
interface changes.

- Make content discoverable through tappable links, cards, tabs, dropdowns and
  preset choices. Normal browsing should not require typing.
- Prefer selecting a category, publisher, channel, sport, team or date over entering
  a search query. Provide direct destinations and clear labels.
- Do not reintroduce site-wide Search or add text-entry-driven public features
  unless the user explicitly requests them.
- Preserve keyboard accessibility: tap-oriented browsing must still work with
  keyboard focus and activation.

This records future design direction; it does not itself request removal of every
existing text field. Administration and sign-in may retain necessary text entry.
Existing public text inputs should not become a prerequisite for reaching content.

## Required mobile verification

User direction recorded October 24, 2026 (Central): **mobile is a necessary check
for anything we develop.** This is a standing acceptance requirement, including
backend/query changes that affect displayed data. Verify affected mobile flows
before calling a feature/fix complete or ready to deploy; repeat them on the live
build after deployment. Desktop success does not establish mobile success.

Use phone emulation at **320, 390 and 430px minimum**, in light and dark mode,
with representative content, plus real-device checks when available. Inspect
screenshots and exercise the controls, alongside automated assertions:

- No horizontal document overflow, clipped content or unreadable wrapping.
- Correct card/section order, aligned date navigation and usable filters.
- Readable text, status/selection contrast and at least 44px touch targets.
- Working mobile navigation, forms, dialogs and keyboard/focus behavior.
- Refresh, persisted preferences, long/empty/error data and no-JavaScript
  fallback where relevant to the change.

Check tablet/desktop layouts for regressions too. Record tested widths, themes,
flows, results and limits in the handoff and release evidence. If mobile checks
cannot run, report that validation as outstanding rather than claiming readiness.
For documentation-only changes with no UI/data-display impact, record why mobile
verification is not applicable. This documentation-only policy update has no
rendered or interactive impact and does not require a new browser run.

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

Saved articles use one SQL Server list shared by everyone with access to the site.
Article copies survive scraper retention and app replacement. The public script
loads the list, applies explicit add/remove actions, and refreshes every 15 seconds
while visible, on focus/reconnect and enhanced navigation. Browser storage is not
required for new bookmarks. Failed writes leave the last confirmed list unchanged;
unreachable reads disable mutations until a successful refresh. Saved text uses
textContent rather than parsing arbitrary HTML.

Legacy browser bookmarks import once per browser using a stable UUID receipt.
SQL records import receipts transactionally so retries cannot resurrect a removed
article. The original local list remains as a backup; an imported browser does not
keep uploading it. Open the site once on each existing device to import its list.
The shared list is limited to 500 entries; capacity errors are visible and do not
silently evict bookmarks. Legacy read metadata is retained, but the Saved page shows all articles.

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
Home's Latest news shows the newest story from up to ten randomly selected enabled
sources with articles. Selection stays stable within the latest successful news scrape's
UTC hour and rotates when that timestamp advances to another hour. Selected stories
remain ordered newest first; failed sources do not advance the rotation timestamp.
Homepage stories show their published date and time beneath the headline in the
configured display timezone. Undated stories omit the timestamp.

Home's Today & Tonight excludes Premier League matches, shows Championship matches
only when Wrexham participates, and shows MLS matches only when Sporting Kansas City
or St. Louis City participates (home or away). Full schedules remain available on Events.
NHL games appear only when St. Louis Blues or Montreal Canadiens participate, home or away.

Home's Today & Tonight contains only sports fixtures for today. Movies and television
episodes remain in their dedicated homepage sections. Confirmed starts precede
time-TBD fixtures. Stored final results hide completed games on the next page
load, even if the schedule status is still pending its next refresh. In-progress
games remain visible. With no eligible games remaining, the panel says
“No remaining games today.” See [matching rules](sports.md#home-page-completed-events).
Each matchup shows Home and Away columns side by side with a logo beside each name,
using the schedule page's badge style. Teams and logos follow the schedule's home/away
designation; neutral or unknown designations use Team/Opponent labels instead.
Missing or failed logos show team initials. Matchup links remain available.
Time, location and TV/streaming details appear beneath the teams.
Safe stream URLs get a Watch stream link.

## Administration and health

Big 12 football standings use Conference and Overall column groups matching the
reference layout, with compact stripes and shaded conference percentage. The source's
records/percentages/streak are mapped by statistic label. Unavailable PF/PA and overall
home/away columns are omitted. Conference home/away records are not substituted.

Events uses a single league/sport dropdown styled like Standings. It offers All
schedules, one choice per professional league and one per Big 12 sport with upcoming
visible fixtures. Changing selection submits automatically; a no-JavaScript Apply
button remains. Paging preserves selection, Wrexham filtering remains available,
and existing school/sport query links continue to work.
The Events listing covers today through three days from today in the configured
display timezone, inclusive. Counts and pagination use this window for every filter.
League choices remain available even when their next game falls outside the window.

Women's college basketball is excluded from public schedules, standings and result
queries, including direct filter URLs. Men's college basketball and WNBA are unaffected.
Stored data and feed collection settings are retained.

EPL, Championship, MLS and NWSL standings show all GP/W/D/L/F/A/GD/P columns by
default, with compact striped rows, team logos, shaded points and colored signed
goal differences. The first header shows the stored season; tables scroll on phones.
Big 12 soccer mirrors the source's twelve statistics: Big 12 Record, Conf. Pct.,
Points, Home, Away, Streak, Overall Record, Pct., Home, Away, Neutral and Streak.
Conference and overall values are kept distinct and displayed in source order.

NHL standings use conference headings with division tables and all 16 statistics
visible by default: GP, W, L, OTL, PTS, RW, ROW, SOW, SOL, HOME, AWAY, GF, GA,
DIFF, L10 and STRK. Points are shaded and goal differential keeps its signed value
and color. Tables scroll horizontally on phones, with team names retained on the left.

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

College basketball matches the Big 12 source screenshot: Big 12 Record, Pct., Home,
Away, Overall Record, Pct., Home, Away, Neutral and Streak, with a compact black
header and left-aligned values. Repeated labels retain separate conference/overall
values. Current-season filtering remains; women's college basketball stays excluded.

Big 12 Volleyball standings retain Conf, CPct., Overall, Pct. and Streak. Conference
Home/Away/Neutral/Streak columns are omitted, and all remaining statistics are visible
without the Show all statistics toggle.

NFL standings use AFC/NFC headings with East/North/South/West division tables.
All W/L/T/PCT/HOME/AWAY/DIV/CONF/PF/PA/DIFF/STRK columns are visible by default,
with shaded winning percentage, striped rows and signed/colored point differential.

Football AP rankings use compact RK/TEAM/REC/PTS/TREND columns, with first-place
votes beside the team name, colored trend arrows and NR for previously unranked
teams. Only the configured AP poll is shown; unavailable other-vote/dropped-team
summaries are omitted. Other sports keep their existing rankings layouts.

Settings → My Teams (`/settings`) lets visitors search and select teams/schools
from enabled standings and schedule data. Selections save automatically in
`mynewsfeed-sports-teams-v1` localStorage on the current browser/device. College
aliases share a key across sports. Matching schedule, daily results, MLS/Championship
season results and standings rows gain a personal outline and bold team names;
full listings and existing provider highlights remain visible. Clear selections
removes personal highlights. Navigation and cross-tab changes resynchronize state;
blocked storage reports an error. JavaScript is required for this personal feature.

My Teams includes professional teams from enabled league standings/schedules, while
college choices are limited to the 16 Big 12 schools in the shared alias list.
Saved professional and Big 12 selections remain supported.

Personal team highlights use a red row outline and red left border, with a lighter
red in dark mode. Selected team names retain their bold weight.

My Teams now lives in Settings, reached through the gear link beside Saved articles
on desktop and mobile. The previous `/sports/teams` URL opens the same Settings
page for existing bookmarks; it is no longer listed in the Sports menu.

Schedule Links cells stack labeled TV/Streaming and Radio information above a
separate wrapping row of Stats/Video/Audio/Details actions. Long broadcast lists
wrap independently so action links remain grouped.

The Movies calendar lists releases from today through the current month's final day,
using the display timezone. All/In theaters/Subscription streaming filters and paging
share that window; undated releases are omitted. Separate digital and homepage queries
retain their existing scope.


## Personal color schemes

Settings (`/settings`) offers Monochromatic, Christmas, Halloween, St. Patrick’s Day, Kansas Jayhawks (the existing default),
St. Louis Cardinals and St. Louis CITY SC (Tina Turner kit, gold and
black). The CITY palette is inspired by the [club’s kit announcement](https://www.stlcitysc.com/news/st-louis-city-sc-unveils-the-tina-turner-kit); UI shades are adapted for readability. Changes apply immediately
and save in this browser under `mynewsfeed-color-scheme`. The existing light/dark
preference remains independent and works with all twelve schemes. The initialization
script applies stored preferences before first paint; choices persist through
navigation, refresh and cross-tab updates. Invalid or absent scheme values fall
back to Kansas. Storage failures apply the scheme for the page and show a message.

Team choices continue using `mynewsfeed-sports-teams-v1`; existing selections are
preserved. Preferences require JavaScript and remain local to the browser/device.
The Settings page keeps all eligible professional teams and limits college choices
to Big 12 schools. Selected-team red highlights remain independent of color scheme.

Christmas uses evergreen, cranberry red and warm white, with a matching dark
palette. It is an optional year-round selection and saves under `christmas` using
the same browser preference key as the other schemes.

Halloween adds pumpkin orange, purple and black with a matching dark palette.
It saves as `halloween` and is available year-round alongside Christmas.

St. Patrick’s Day uses emerald green and gold with a matching dark palette.
It saves as `st-patricks` and is available year-round.

Decade-inspired options add ’80s Neon (pink, electric blue and purple), ’90s Retro
(teal, purple and cream) and 2000s Y2K (silver, blue and lime). Their preference
values are `eighties`, `nineties` and `y2k`; each supports light/dark mode and the
same automatic browser persistence.

Desert Sunset (terracotta, peach and sand) and Coffee (espresso, caramel and ivory)
use `desert` and `coffee` preference values. BYU is no longer a color scheme;
browsers with a saved `byu` scheme fall back to Kansas. BYU remains an eligible
Big 12 school in My Teams.

## Automatic light and dark mode

To enable, open **Settings → Light and dark mode → Automatic · Kansas City
sunrise/sunset**. This choice is saved separately in each browser and device.

Settings offers Follow device, Automatic · Kansas City sunrise/sunset, Light and
Dark. Automatic uses Kansas City, Missouri (39.0997° N, 94.5786° W), with
America/Chicago dates and daylight-saving-aware labels. The panel shows today's
approximate sunrise and sunset. Calculations use NOAA's fractional-year solar
equations locally; no location permission or external API is required.

The `theme` browser preference accepts `sun` for automatic and `system` for device
mode alongside existing `light`/`dark` values. Existing choices remain in effect;
visitors opt into automatic mode in Settings. Initialization applies the resolved
appearance before page content renders. An open page rechecks every 30 seconds,
and on focus, visibility changes, navigation and cross-tab preference changes.
Switching can occur up to 30 seconds after the calculated time on an active page;
returning to a suspended tab recalculates the appearance. The city stays Kansas
City even when the device is in another timezone.
The header toggle selects a persistent manual Light or Dark choice; choosing
Automatic in Settings resumes sunrise/sunset switching. Blocked storage applies
the choice for the current document and shows a save warning.

Calculation regressions run with `node --test tests/browser/solar-times.test.cjs`.
The browser regression script `tests/browser/theme-mode.browser.cjs` requires
Playwright and a running local app. Set `NEWSFEED_TEST_URL` for the preview URL
(default `http://127.0.0.1:5091`) and expose an existing Playwright installation
through `NODE_PATH` when it is not installed locally. It checks automatic and
manual modes, persistence, navigation, cross-tab updates, blocked storage and a
mobile viewport, with the device timezone deliberately set outside Central time.

## Top Headlines and news source filtering

Added October 17, 2026 (Central). Opening News without a category defaults to
Top Headlines, resolved by name from the enabled category list. Choose All
categories to browse every enabled source; this uses `news?category=all` and stays
selected through reload, source filtering and pagination. Explicit category links
retain their selected category. If Top Headlines is unavailable, the default
falls back to All categories. Categories
are discovered from enabled database sources, including categories without article
rows. Top Headlines is the first named category below All categories; remaining
categories retain alphabetical order. The Source selector lists enabled sources
within the selected category, even before first collection. Membership is dynamic; category/source IDs are not
hardcoded. BBC News and NPR remain in General.

The source choice uses the existing `news?category=<id>&source=<id>` routing and
survives cursor paging, Load more and Back to newest. Category changes reset the
source and cursor; source changes retain the category/search and reset the cursor.
An invalid, disabled or out-of-category source resets to All sources, consistently
with the Load more endpoint. Both selectors have GET-form Apply buttons without
JavaScript. An empty category retains its selectors and says “No articles available
yet. Please check back soon.” Database failures use the normal error path.

Top Headlines uses the existing article cards, outbound URL validation, publisher
labels, saved-item identity and cursor ordering: dated articles newest first, then
undated articles by scrape time, with descending article ID as the tiebreaker.
These are publisher feeds, with no editorial ranking or separate page. The normal
missing-image, summary and publication-date handling is unchanged.

Production verification on October 17, 2026 (Central) found all six sources enabled (NBC News, CBS News, The New
York Times, The Guardian, Al Jazeera and Sky News), but zero collected articles.
The category/empty-state and existing-category cards were verified on desktop and
mobile; actual Top Headlines cards still require a check after scheduled ingestion.
No source seed or scraper deployment was performed.

Targeted query/card/helper validation passed 121 tests. To repeat these checks
against an appropriate development/test database:

```bash
dotnet test myNewspaper.sln --filter 'FullyQualifiedName~FeedQueryTests|FullyQualifiedName~FeedArticlesRenderTests|FullyQualifiedName~FeedHelpersTests'
```

Production browser checks covered category ordering, six source options, source
selection and reload, invalid cross-category source handling, enhanced pagination,
existing-category article cards and saving, no-JavaScript forms and 1440px/390px
layouts. See [release history](release-history.md#top-headlines-and-news-source-selector--october-17-2026)
for the dated ingestion limitation and deployment evidence.

## Team records on sports views

Home's Today & Tonight, Events, Results, MLS results and Championship results now
show current season records beside each covered team. College teams include
overall and conference totals in compact form, for example `12-4 (5-2)`; the
parentheses contain the conference record. The tooltip explains the notation.
Professional labels identify W–L, W–L–D, W–L–OT or W–L–T as appropriate.
Records wrap below the team name on narrow cards; logos, score emphasis and
home/away or neutral-team placement are retained.

Records use the latest collected standings even when viewing an older result.
A tooltip supplies the update time; stale or failed-refresh snapshots display
“last available.” Missing records are omitted. See
[coverage and season rules](sports.md#team-records-beside-matchups).

Records are suppressed league-wide during playoffs/postseason, including on past
results. Only leagues confirmed to be in regular season show records. Status
refreshes in the background every ten minutes; unknown or expired status hides
records until regular season can be confirmed. Games and scores remain visible.

The explanatory team-record paragraph is omitted from Home; record labels and
their tooltips remain available. Other sports pages retain the explanation.

## Dedicated F1 sports section

Open Sports → F1 for Schedule, Results, Standings and News tabs. Schedule offers upcoming
weekends and a full calendar with expandable session timetables. Results has a
Grand Prix/Sprint selector; Standings switches between Drivers and Constructors.
The mobile Browse label identifies F1, and all navigation, disclosures and Apply
forms work without JavaScript. Wide classification tables scroll inside a labelled,
keyboard-focusable region; phones show a scrolling hint.

Season, snapshot update time and the championship as-of round are shown separately.
Confirmed session times use the site display timezone, while date-only sessions
retain their date and say Time TBD. Source attribution and periodic-data limitations
are visible. See [F1 data and behavior](sports.md#f1-section).

Home also includes a dedicated F1 card outside Today & Tonight. It shows the next
unclassified Grand Prix, the next available weekend session, and the latest
published Race/Sprint result, with links to all F1 views. The card spans its
own desktop row and appears after Today & Tonight on mobile. Snapshot freshness,
provider attribution and empty/error states are confined to the card; an F1 read
failure does not take down the rest of Home.

## F1 News

Sports → F1 → News opens `/f1/news`, using the existing article cards, publisher
labels, safe article/image URLs, publication times, image/summary fallbacks and
browser-local saving behavior. The four publishers are BBC Sport — F1, Autosport —
F1, Motorsport.com — F1 and The Guardian — F1. Membership is resolved dynamically
from the enabled F1 News category; no production category/source IDs are hardcoded.
This RSS news feed is independent of Jolpica's F1Source enablement and snapshots.

The source selector includes enabled publishers before their first articles
arrive. Source selection, optional search and cursor pagination retain the F1
route on reload and no-JavaScript navigation. Enhanced Load more uses the normal
feed endpoint with `view=f1-news`, which resolves the category server-side and
keeps subsequent links under `f1/news`. Invalid or out-of-category sources reset
to All sources within F1 News. A missing/disabled category remains empty rather
than falling back to the general news feed. Query failures show a generic error.

F1 news freshness uses successful scrapes in this category/source scope. Existing
publication ordering, saved-article identity and redirect recovery remain
unchanged. The F1 home card includes a News link alongside its sports-data links.

## College Sports navigation

College Sports appears under Sports in both desktop and mobile navigation.
Its Schedule, Results, Standings, Rankings and News tabs share one labelled sport
selector, retained in shareable URLs and across tab changes. Mobile Browse
identifies College Sports as the current section. Existing game cards and
scrollable standings/poll tables retain their responsive layouts. See
[college coverage and routes](sports.md#college-sports-hub).

College schedule rows share the same fixed desktop column widths as the Sports
Events page, so team names, times, venues and links align with their headings
regardless of text length or available broadcast details. Mobile and tablet views
retain the existing stacked game cards.

Personal team highlights in standings and rankings use inset cell borders,
including the pinned team-name column. Adjacent selected rows share one horizontal
edge, and selection does not change column widths. Light and dark themes retain
their existing red highlight colors.

The college hub shows one sport selector with its label vertically centered.
Embedded standings and rankings omit their redundant source dropdowns; standalone
Standings and Top 25 pages keep their source selectors.

## Big 12 News

College Sports → News opens `/college/news`. It reuses the standard article
cards, publisher labels, safe article/image destinations, date formatting,
summary/image fallbacks, browser-local saving and retired-article recovery.
Articles cover the conference across all sports; the selected sport is preserved
in tabs, publisher forms and cursor pagination as navigation context.

The publisher selector is independent of the sport selector. Enabled sources
are discovered from the Big 12 News category by name, including publishers with
no retained articles. Current publishers are Big 12 Conference — All Sports,
Heartland College Sports, Big 12 Conference — Football, Big 12 Conference — Men's
Basketball and Big 12 Conference — Baseball. No dedicated women's basketball
feed is registered; conference-wide feeds can still include women's sports.

Source filtering, optional query-string search and ordinary/enhanced Load more
remain within Big 12 News. The `view=big12-news` feed endpoint enforces the fixed
category server-side, overriding unrelated category parameters; invalid or
out-of-category source IDs reset to All sources in this category. Missing or
disabled coverage stays empty; query failures display a generic error.

Overlapping official feeds retain the existing global URL ownership rules; the
frontend does not duplicate or move articles to fill publisher counts. Freshness
uses successful scrapes in the selected category/source scope. News is independent
of structured schedule, result, standings and poll enablement. No collector
restart, source registration, schema change or new credentials are required.

## YouTube navigation

YouTube is a separate main navigation link on desktop and a dedicated section
in mobile Browse. Videos no longer appears under Entertainment. The existing
`/videos` page, channel filters and playback links remain unchanged. Mobile
Browse identifies the current section as YouTube.

## Personal planning and discovery — October 19, 2026

My Week (including the Home “Plan My Week” action) and its calendar downloads
were removed at the user's request. `/week` and `/calendar/week.ics` return the
standard 404 page. Individual sports, F1, TV and movie schedules remain available
from their existing sections. Home customization and news reading preferences
remain supported.

Watchlist, YouTube Watch Later and episode watched-status controls were removed
at the user's request. Tracked series remain browsable through TV;
normal movie details, retailer actions and YouTube playback links remain available.
Old browser-local watchlist data is unused; existing Saved articles, teams and
other preferences are preserved.

Site-wide Search and its desktop/mobile entry points were removed at the user's
request. `/search` returns the standard 404 page. Existing News/category/publisher,
YouTube/channel, movie and tracked-series filters remain available within their
sections; movie query filtering retains its all-collected-dates scope.

Settings adds home-section visibility, ordering, selected-team-only games,
preferred news category, Hide read stories and muted-publisher management. Custom
home ordering uses full-width sections on desktop to honor the chosen sequence;
the default retains the established two-column news/sports layout. Customize Home
uses fixed button columns on larger screens; at phone widths each section label
sits above equal-width Move up/Move down buttons. Buttons align across rows, retain
at least 44px touch targets and visibly disable at the ordering endpoints. Explicit news
URLs override preferred-category navigation. Read/unread changes synchronize with
Saved articles. Mutes match displayed publisher names. Filtering applies to loaded
cards; source totals and server pagination describe the unfiltered query, and a
status explains hidden loaded items. Existing saved articles and team preferences
retain their original storage keys.

Personal preferences use `mynewsfeed-personal-v1`; reading history holds up to
2,000 entries. Personal data stays on this browser/device and synchronizes
between tabs; clearing storage removes it. Storage failures report a status and
preserve the previously saved value. Personal actions require JavaScript, while
server-rendered browsing and source links remain usable without it.

## Kansas City Local

Local (`/local`) is Kansas City only. Its news uses the enabled category named
`Kansas City News` when registered through existing source administration.
Until then it explicitly labels keyword matches for “Kansas City” from enabled
collected news; it never substitutes an unfiltered general feed. Ordinary cursor
paging, article saving and reading controls remain available. No collector schema
or source registrations are changed by this implementation.

Weather uses the [National Weather Service API](https://www.weather.gov/documentation/services-web-api)
for a fixed Kansas City coordinate, with no geolocation permission or API key.
The browser requests `/local/weather`; the server resolves and validates the NWS
forecast address, caches successful responses for fifteen minutes and retries
failures after two minutes. A last successful snapshot may be retained for up to
six hours and is labelled stale on refresh failure. NWS failures leave an official
forecast link. Forecast updates show Central time.

Forecast cards include locally authored SVG condition icons beside the temperature.
The NWS `isDaytime` flag distinguishes sun/moon and day/night partly cloudy artwork.
Condition summaries select clouds, rain, thunderstorms, snow, freezing conditions,
wintry mixes, fog or wind; unrecognized text uses a neutral thermometer. Icons are
decorative (`aria-hidden`) because the written condition remains visible. SVG paths
are constant local artwork; provider text is never inserted as SVG markup and no
external icon requests are made. Icons adapt to light/dark mode.

Community events link to [Visit KC](https://www.visitkc.com/visitors/events) and the
[City calendar](https://www.kcmo.gov/i-want-to/city-calendar); these are external
calendars, not an imported event database.

## Sports statistics modal

Result rows offer a tappable Box score or Game stats link only when usable cached
statistics are available. It opens a native dialog with a persistent Close button,
Escape dismissal and focus restoration. Player tables and expandable team/period
sections keep wide statistics in internal scrolling regions at phone widths.
Modified clicks and no-JavaScript browsing open the same statistics as a normal
page. Existing external game links and original result scores are retained. See
[box-score behavior and read contract](sports.md#cached-box-scores-and-game-stat-modals).

Baseball detail follows the supplied box-score reference: R/H/E summary, paired
hitting tables, then paired pitching tables, with mobile stacking and internal
horizontal scrolling. Extra collected statistics expand on tap. Missing inning
labels remain explicit; unavailable decisions, photos and scoring plays are omitted.

Soccer detail opens with visible team comparisons: club values on either side of
the statistic label, grouped by available categories. The same layout works in
the modal and standalone page, including narrow screens and both themes.

Football detail groups players by category, pairing both teams under Passing,
Rushing, Receiving and other collected categories. Desktop uses two panels;
mobile stacks them with internal scrolling for wide tables. The modal and regular
fallback page share this presentation.

Repaired period labels display directly from the cache (innings, quarters, halves,
overtime and shootout labels). Baseball uses per-team labeled inning tables when
period sequences differ, preserving sparse/unplayed values. NHL cached detail is
now available through its enabled-source/current stored-season visibility check.

Basketball detail pairs the teams' player tables on desktop and stacks them on
mobile. Dynamic columns retain collected statistics; wide tables scroll within
the panel while player names remain visible. Existing half/quarter/overtime labels
remain under Scoring by period.

Pending local navigation update (October 20, 2026): Where to watch is a direct
main-navigation link, separate from Entertainment. Deployment is on hold while
the user supplies additional feedback.

Deployed October 20, 2026: main-navigation placement and My services selectors are
now live. The user lifted the feedback deployment hold. Service selections are
remembered in this browser and prioritize included/free matches per results page.

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

Deployment status (October 20, 2026): Settings services, section ordering and the
theater-only provider exclusion are now live. Earlier pending/local-only notes
are superseded. See release history for verification and rollback details.

October 20 navigation repair: Arrange Settings controls now initialize on both
direct loads and in-site navigation. A full refresh loads the corrected script in
existing tabs. Prior direct-load-only browser coverage missed this lifecycle case;
regressions now enter Settings through actual navigation links.

### Installed app icon — October 24, 2026

The myNewspaper logo is a white folded newspaper with an “n” silhouette, flowing gold/light-blue
paper ribbons and a golden sunrise on blue. The built-in image-generation tool created the
source at `docs/assets/mynewspaper-ribbon-icon-source.png`; browser canvas exports supply
192px/512px icons and an inset maskable 512px icon under `wwwroot/icons/newspaper-ribbon-*`.
Manifest, favicon, public header and offline screen use these versioned assets.
The app name/short name are exactly `myNewspaper`; id/start URL/scope stay `/`.
Service worker cache `mynewsfeed-pwa-v4` replaces earlier app caches while leaving
unrelated caches intact. Legacy icon assets remain for older installs. Prior
MyDailyFeed artwork remains at `docs/assets/mydailyfeed-icon-source.png`.

Open the private HTTPS site with Tailscale connected and Tailscale DNS/MagicDNS
working. Android Brave can install from its browser menu. An HTTP LAN address is
not sufficient for a secure PWA context. Installed icon updates are browser/OS
controlled; if the old icon persists, remove and reinstall the installed app.
Removing an app can also remove its local preferences depending on the browser.

`tests/browser/pwa.browser.cjs` checks actual icon dimensions, manifest identity,
Chromium installability, worker cache version and offline navigation. Set
`PLAYWRIGHT_MODULE`, `BASE_URL`, and optionally `PWA_HOST_IP` when the test runner
requires an explicit Tailscale hostname mapping. This mapping bypasses DNS only
for the test runner; it does not repair client-device MagicDNS.

### Feed reading toolbar removal — October 20, 2026

Removed the inline Hide read stories / Manage muted publishers & preferred category
bar and its loaded-story hint from News, Local, F1 News and college news. Settings
still provides reading controls; article read/mute/save actions and existing browser
preferences continue to work. Removed the unused component, styling and toolbar
counter rendering. Reading browser checks now change Hide read stories in Settings.

### Remove article read/mute buttons — October 20, 2026

Removed Mark read/Mark unread and Mute publisher from Home and the shared article
feed cards, including dynamically appended cards. Saved cards no longer render
Mark read/Mark unread. Deleted the unused ReadingControls component, its styles
and click handlers. Save/bookmark and Share remain. Settings copy no longer
instructs readers to use removed actions. Existing browser read/mute preferences
can still be cleared or publishers unmuted in Settings.

### Shared Saved list — October 20, 2026

`SharedArticles` stores one JSON article-copy list in app-owned SQL Server table
`dbo.MyDailyFeedSavedList`; `dbo.MyDailyFeedSavedImports` holds one-time browser
import receipts. Apply idempotent `db/018_shared_saved_articles.sql` before deploying
this feature. No scraper rows or relationships change. SQL serializable transactions
and a row update lock serialize additions/removals/imports across app instances.
Explicit add/remove requests are idempotent and never replace another device's
entire list. Normalized HTTP(S) URLs deduplicate entries; links with credentials
are rejected. Text lengths and the 500-article capacity are validated. Display uses
DOM textContent. SQL backups now protect bookmarks; keep both tables on rollback.

Anonymous `/api/saved` GET returns the current list and an antiforgery token with
`Cache-Control: no-store`. JSON POST `/add`, `/remove`, `/import` validate that token
in `X-Saved-CSRF`. The public site intentionally shares this list without an account;
it is not separated by device or user. Do not publicly expose the site expecting
private per-person Saved lists. This is the user-approved shared-list design.

The client queues refreshes and mutations, polls every 15 seconds while visible,
and refreshes on focus, reconnect and Blazor enhancedload. Confirmed data is retained
on errors; offline changes are not queued. Controls recover after successful sync.
Import failure leaves the shared list usable and the original browser list intact.
A UUID receipt is stored before import and committed with the SQL update, preventing
retries or simultaneous tabs from resurrecting removed entries. UUID generation also
works on an old HTTP LAN origin. Browser storage can be blocked for normal shared
saving, but importing old browser data requires that storage to be readable/writable.

Existing browser bookmarks must be imported by visiting their original origin:
HTTPS/Tailscale, HTTP/IP and different browser profiles have separate localStorage.
Open each old address/profile once; subsequent bookmarks are shared from the server.
Personal settings, teams and streaming-service selections remain browser-local.

Verification: `SharedArticlesTests` exercises input validation plus a temporary SQL
Server database for concurrent writers, deduplication, import replay after removal,
persistence and full-list rollback. It creates/drops only its uniquely named test
database. `tests/browser/shared-saved.browser.cjs` checks separate device contexts,
real polling, migrations, blocked storage, unsafe links, CSRF, failure/retry/offline
recovery and mobile/desktop widths. It removes its unique test bookmarks afterward.

### Saved toolbar removal — October 20, 2026

Removed the Reading list / All saved dropdown and the saved-article total from
Saved. The page always shows the entire shared list, including imported articles
with legacy read metadata. Removed obsolete filtering, count and Show all saved
handlers/styles. The header bookmark badge and sync/error status remain. Rendering
signatures include sync readiness so an initially empty list shows its guidance
when the first response completes.

### Mobile grouped navigation — October 20, 2026

Sports and Entertainment inside the mobile Browse menu each have an accessible
named group, larger parent heading, containing border and indented child links
with a vertical guide. Existing destinations, current-page marking, touch targets,
menu scrolling and close-on-selection remain. Desktop dropdowns are unchanged.
Where to watch combines movie/TV title searches but preserves separate browse tabs.

### Direct mobile links — October 20, 2026

YouTube and Saved articles are direct links in the mobile Browse menu, outside
Sports/Entertainment groups. Removed the separate YouTube / Your links labels
and renamed the Videos destination label to YouTube. Settings/admin links remain
direct as well. Sports and Entertainment retain their grouped subnavigation.

### Cascading desktop Home — October 20, 2026

At widths >=1024px, Home uses two equal columns with independent card row spans
instead of fixed full-width section rows. `home-layout.js` measures natural card
heights and rounds spans to 1px rows plus the current row gap; dense auto-placement
fills available column space. The DOM remains in Settings order. Hidden sections
stay hidden and customized layouts keep both columns. ResizeObserver remeasures
card/image/filter changes; MutationObserver handles visibility/order updates;
Blazor enhancedload rebinds after navigation. Desktop card internals use two movie
columns and single-column TV, F1 and video content to fit the narrower tiles.

Below 1024px the script clears desktop spans and the existing stacked layout is
used. Without JavaScript the desktop layout is a normal two-column grid. This
reduces gaps between sections; different column totals can still leave space at
the bottom. No data, settings keys, card content limits or destinations changed.
`tests/browser/home-cascade.browser.cjs` checks gaps/overlap at 1024/1440/1920px,
dynamic height, hide/reorder preferences, enhanced Home navigation, resizing back
to mobile/tablet, and the no-JavaScript fallback.

### YouTube Watch later — October 21, 2026

Home and Videos offer Watch later on each video. Saved combines articles and
YouTube videos in the existing shared list, newest saves first. Video cards retain
the thumbnail, title, channel and publication date, show a YouTube label, and open
YouTube in a new tab. The bookmark removes either type. Header and mobile links
are now labeled Saved. This list is independent of YouTube account Watch Later.

Saved payloads have an optional Kind (article by default, or video); old article
JSON remains compatible. The existing 500-item shared limit, polling, CSRF checks
and server persistence apply to both types. No schema migration is required.

### Separate Saved sections — October 21, 2026

Saved displays Articles followed by Videos on the same page. Each section preserves
the shared list's newest-save-first order and has its own empty message. Older
bookmarks without Kind appear under Articles. Saving, removing, synchronization
and the shared 500-item limit remain unchanged.

### YouTube sharing — October 21, 2026

Home and Videos use the same Share control as news articles. Video sharing also
works in Saved. The dialog says Share video and offers Copy link, mobile SMS, and
native app sharing when supported in a secure browser context. Article sharing
retains its existing label and behavior.

Video Share and Watch later buttons sit together at the right of the metadata
row with a 4px gap, equal button heights and centered icons. The controls wrap
together on narrow screens.

### Homepage columns — October 21, 2026

Updated October 22, 2026: at desktop widths (1024px and above), Where to
watch and Latest news lead the two-column cascading layout. Each following card
uses the shorter column, avoiding large gaps below shorter cards. At narrower
widths, Where to watch comes first, followed by Latest news, Today & Tonight,
Today’s episodes and the remaining cards. Existing hidden-section and team filters
remain effective; legacy saved ordering does not override this priority. Without
JavaScript, desktop falls back to a regular two-column grid with the same leading
cards. This supersedes the October 21 fixed news/planning columns.

Homepage theater and Digital / VOD posters are capped at 120px wide (180px
high at their 2:3 aspect ratio), including the missing-image fallback.

### F1 race-weekend placement — October 21, 2026

From the Friday preceding a Grand Prix through its race day (inclusive), the F1
home summary is nested inside Today & Tonight and the standalone F1 card is
omitted. Race day is calculated in the site's display zone (America/Chicago),
using the confirmed UTC start when present and the calendar race date otherwise.
Saturday races end their window on Saturday. Published results do not end the
window early. Outside that window, the separate F1 card remains. Both placements
retain race/session details, results links, attribution and freshness warnings.
The F1 hidden-section preference applies to either placement.

### Homepage Top 25 and F1 card removal — October 21, 2026

The standalone homepage F1 card is removed. F1 appears only within Today & Tonight
during its Friday-through-race-day window; the dedicated F1 pages remain available.
Top 25 follows Today’s episodes in the right column, ahead of movies. Its Sport
and poll selector uses the rankings page's enabled, season-eligible sources. The
selection is shareable via rankingSourceId on the homepage; View all opens that
source on /rankings. The compact scrollable table includes ranked teams 1–25,
records, logos, Big 12 highlights, poll dates and existing freshness warnings.
Empty/unavailable sources show honest messages. Apply works without JavaScript.

The homepage Top 25 table now contains exactly Rank, School, Record, Conference
and Previous rank. Logos and inline Big 12 badges were removed from School; Big 12
row highlighting remains. Previous rank uses NR when previously unranked and an
em dash when unknown. Conference membership comes from ESPN season-specific
standings, cached for six hours; failed lookups retain available data and retry
after five minutes. Stored Big 12 membership takes precedence over the lookup; unknown conferences
use an em dash.
The conference lookup does not change the stored rankings or dedicated rankings page.

Homepage Top 25 uses a tinted row and left accent marker for Big 12 schools in
light and dark themes. Its fixed column proportions, tabular numbers, compact
cell padding and consistent alignment keep polls visually uniform. School and
Conference align left; Rank, Record and Previous rank align center. The poll
metadata wraps with consistent gaps; its duplicate visual table caption is hidden
while retaining the accessible caption. Narrow layouts scroll within the table.

### Homepage Where to watch search — October 21, 2026

A Where to watch search card now sits above Today & Tonight in the right desktop
column and first in the mobile stack. It replaces the small Where to watch link
beside Kansas City; the main navigation destination remains. A labeled movie/TV
search field submits q directly to /watch, using its existing multi-title search
and results. The required field has the same 120-character limit as that page.
It works with Enter or Search and does not require JavaScript.

At desktop widths (1024px and above), homepage Top 25 expands to show all available
ranked schools without an internal vertical scrollbar. Smaller screens keep the
440px-high scrolling table. The homepage column layout adapts to the taller card.

### Homepage Top 25 without Conference — October 21, 2026

The current homepage columns are Rank, School, Record and Previous rank. Conference
is removed, along with its unused HTTP lookup/cache. Big 12 highlighting still uses
the stored IsBig12 flag, independent of any conference lookup. Desktop continues
to show the full list, while mobile retains its height cap and internal scrolling.

Homepage Top 25 School cells again show compact 22px school logos alongside
the name. Missing/failed logos leave the name visible. Four columns, Big 12 row
highlights, full desktop rankings and mobile scrolling remain unchanged.

The homepage Kansas City shortcut below the greeting has been removed, including
its now-empty daily-links navigation row. Local news remains available in the
main navigation.

The homepage Top 25 final column is labeled Previous (formerly Previous rank).
Its values and behavior are unchanged.

### Forecast using device location — October 21, 2026

The Local forecast offers Use my location, requesting browser geolocation only
after a click, and Use Kansas City to reset. HTTPS (or a trusted local development
origin) is required for browser location. Coordinates are rounded to two decimal
places before submission and again on the server; selection stays in the current
tab's memory rather than persistent browser storage. The forecast heading uses
the NWS nearby city/state and the official forecast link follows the selected point.
Local news and community calendars remain Kansas City content.

The existing /local/weather endpoint accepts paired lat/lon values, validates
finite ranges, caches by rounded location and marks custom responses private/no-store.
A provider failure never substitutes another location's cached forecast. Denied,
unavailable or timed-out geolocation, or an unsupported/unavailable NWS forecast,
falls back to Kansas City with an explanation. Reset cancels pending location
selection; superseded fetches cannot replace newer results. The existing icon,
stale-data and no-JavaScript official-link fallbacks remain. No schema changes.

References: [browser location requirements](https://developer.mozilla.org/en-US/docs/Web/API/Geolocation/getCurrentPosition) and [NWS forecast API](https://www.weather.gov/documentation/services-web-api).

### Scoreboard browsing and highlights

Scoreboard shares sports team badges, fixed score/star columns, Live/Final badges
and consistent action links. Optional full-page metadata lives under Game info;
Home uses compact divided rows.

Sports → Scoreboard offers tap-based previous/next dates, Today, and an enabled
Show dropdown grouped into Leagues, Conferences and Teams. Home has a separate six-game card and View all.
Stars highlight teams within their sport/league using provider IDs; these browser
preferences are independent of existing school/team choices. No typed input is
required. Server-rendered games and navigation work without JavaScript. Automatic
stored-score reads run every five minutes while visible; displayed clocks never
tick locally. See [Live scoreboard](sports.md#live-scoreboard) for coverage and
freshness behavior.

### Daily results consolidation

Scoreboard hosts daily final scores with a grouped Show dropdown and a separate
Games dropdown (All statuses / Final scores). Old Results bookmarks redirect to
the final-score view, preserving dates and follow selections. Date navigation and
background refresh retain both selections. The duplicate Results navigation
item is removed; enabled MLS/Championship season-result links remain on Scoreboard.
Current records share the other sports pages' regular-season/playoff rules.

### Scoreboard mobile controls and homepage order

Today & Tonight precedes Scoreboard in the homepage document/default order and
in saved layouts that previously placed Scoreboard first. Other hidden sections
and team preferences are retained. On Scoreboard, the competition selector is
now part of Show, with All sports as its default and grouped league, conference
and team choices. League choices occur once; choosing a team/conference also
selects its competition. Show and Games apply on selection using GET forms.
Legacy competition/follow query parameters still work: a valid follow takes
precedence over a conflicting league; league follows map to the league choice.
New date links use a single show parameter, preventing conflicting selections. Apply appears only as a no-JavaScript fallback. On phones the Central date
sits above equal-width Previous day / Next day buttons with 44px minimum targets;
Today appears on its own line when applicable. Yesterday/seven-day bounds and
filter preservation are unchanged.

### Entertainment headline placement

Entertainment headlines and TV/Film headlines now sit under Entertainment in both
navigation layouts, with their own page titles, active states and mobile Browse
context. The two pages offer 44px feed-switching tabs and scoped source filters.
Headlines removes these categories/sources and excludes their articles from All
categories, including Load more. Old category links redirect to the corresponding
section. Article save/share controls and JS/no-JS paging remain available. This is
a frontend organization change; database category/source ownership is preserved.

Box Office is grouped under Entertainment too, including its active state for
results, weekend preview and movie forecasts. News no longer contains its menu
entry, and Headlines no longer includes the Weekend box office shortcut. Existing
box-office URLs, section selection and chart/forecast behavior are preserved.

### Sports headline placement

Sports contains Sports headlines, Men's college basketball headlines, Big 12 News
and F1 News in both navigation layouts. Each has a matching active destination
and mobile Browse label. The new general/basketball feeds share 44px switching
tabs, source filtering and JS/no-JS paging; Big 12/F1 keep their existing pages.
Headlines no longer lists or aggregates those four categories or their sources.
Old enabled category bookmarks redirect into the appropriate section with their
filters retained. Home's aggregate and stored category/source data are unchanged.

### Headline hubs

Sports and Entertainment each expose one headline link in desktop/mobile menus.
Their shared news hub keeps a stable title/Browse context and shows the selected
category heading and 44px switching links. Sports offers General, Men's college
basketball, Big 12 and F1; Entertainment offers General and TV/Film. Each tab has
its own URL and scoped source/count/update/paging state. Tabs reset source/cursor
when switching feeds. Old standalone basketball/TV-Film URLs still render the hub;
College/F1 news tabs and Box Office remain independently available.

Sports headlines and Entertainment headlines appear first in their respective
section menus on desktop and mobile.

### Assigning new news categories to hubs

In Admin → Categories, choose Hub when adding or editing a category: Headlines,
Sports or Entertainment. Headlines is the default; names are not guessed. New
categories created by other tools also default to Headlines until assigned.
Enabled sources make their category available automatically in its assigned hub,
including before the first articles arrive. Sources remain assigned to their
existing category; changing its hub moves the feed without moving articles.

Sports/Entertainment tabs discover eligible categories from saved assignments.
New categories use ID-based URLs; renaming them preserves those URLs. Source
filters, totals, timestamps and JS/no-JS pagination stay scoped. Headlines excludes
categories assigned elsewhere. Old enabled numeric category links redirect to the
saved destination. Existing short routes, College/F1 readers and Home's aggregate
remain available. Local is not a routing choice. On phones the admin category
list uses cards with 44px editing controls.

Database prerequisite: `db/019_news_category_hubs.sql`, adding SourceCategories.NewsHub
with default/check constraints and initial assignments. Reapplying it preserves
admin changes. Apply before running the updated web app; no collector change.
