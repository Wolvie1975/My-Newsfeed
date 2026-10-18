# Public interface and administration

Current interface notes consolidated October 13, 2026 (Central). Data contracts
are in [Sports](sports.md) and [Entertainment](entertainment.md); historical releases
are in [Release history](release-history.md). Home-page completion filtering and
automatic appearance notes were updated October 17, 2026 (Central).

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

Added October 17, 2026 (Central). Choose News → Category → Top Headlines. Categories
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
dotnet test MyNewsFeed.sln --filter 'FullyQualifiedName~FeedQueryTests|FullyQualifiedName~FeedArticlesRenderTests|FullyQualifiedName~FeedHelpersTests'
```

Production browser checks covered category ordering, six source options, source
selection and reload, invalid cross-category source handling, enhanced pagination,
existing-category article cards and saving, no-JavaScript forms and 1440px/390px
layouts. See [release history](release-history.md#top-headlines-and-news-source-selector--october-17-2026)
for the dated ingestion limitation and deployment evidence.
