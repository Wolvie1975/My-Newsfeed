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
dotnet test MyDailyFeed.sln --filter 'FullyQualifiedName~FeedQueryTests|FullyQualifiedName~FeedArticlesRenderTests|FullyQualifiedName~FeedHelpersTests'
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

### Installed app icon — October 20, 2026

User selected concept 4: a white ribbon “d”, layered feed cards and a golden
sunrise on blue. The original built-in imagegen output is preserved at
`docs/assets/mydailyfeed-icon-source.png`. Prompt direction: combine the ribbon
“d” with layered daily-feed cards and a sunrise, retaining a simple blue/white/
gold app mark readable at small sizes. Browser canvas exports provide 192px and
512px PNGs plus an inset maskable 512px variant under `wwwroot/icons/daily-ribbon-*`.
Manifest, favicon and offline screen reference the versioned filenames; app id,
start URL and scope stay `/`. Service worker cache is `mynewsfeed-pwa-v3` and
activation removes older app caches. Legacy icon files remain for older installs.

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
