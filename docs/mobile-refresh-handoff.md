# Mobile refresh handoff

Written October 28, 2026 (America/Chicago) against `develop` @ `2871b34`. Verify current repository state before
making changes; file names and selectors below are from that commit.

**Save as:** `docs/mobile-refresh-handoff.md`

**Approved mockups:** the myNewspaper canvas (https://claude.ai/artifact/JVQ8mU7R63Z1cziNpeW9Z5). Every artboard named
"… — phone, folded" is a target, plus four "ribbon" artboards. Press Play on an artboard to try the folds.

**Goal:** phone pages stop reading as a stack of identical white cards. Each page gets one colored title band, one dark
focal block, and everything past the first screen folds into one-line summaries that open in place, one at a time.

**Scope rule:** phones only (`max-width: 743px`, the existing phone breakpoint). Desktop and iPad portrait (744px and up,
per `tablet.css`) are untouched. All new CSS lives in a new `mobile.css` under a `.mx` prefix so nothing collides with
`public.css`, `sections.css` or `broadsheet.css`.

> **Feedback carried into these designs — keep it.**
> - No pill chips or segmented tabs on phones. Every filter is one native `<select>` in the ribbon.
> - Don't show everything at once. Past the lead and one short list, content folds.
> - Only one fold open at a time; the first one starts open.

---

## The pieces (shared by every page)

| Piece | What it is | Used on |
|---|---|---|
| **Ribbon** | Scheme-colored band: a live line (matching dot + small caps), a 44px serif title, one labelled `<select>` | Every page |
| **Dark block** | `#141b2b` band for the page's one focal item: lead story, featured video, game of the week, No. 1, the leader, today's weather | Every folded page |
| **Timeline** | Times down the left, colored dots on a rule, title + subline, a tag on the right (TV network, source) | Headlines, Sports, Scoreboard, College, F1, TV |
| **Top list** | Big serif numerals 1–3 (or more), title, colored category dot, Save button | Home, Headlines, Local, Top 25, Box office |
| **Fold group** | A heading, then rows: color edge + serif name + one-line gist + chevron; opens to a few rows + "See all →" | Every folded page |
| **Dark tiles** | 2–3 small cards inside the dark block (live scores, biggest moves, weather periods, weekend numbers) | Scoreboard, Top 25, Box office, Local |

### Category and status colors

| Token | Light | Use |
|---|---|---|
| `--mx-world` | `#0051ba` | World, default dots |
| `--mx-us` | `#b0001e` | U.S., "your team", drops, live |
| `--mx-local` | `#0f766e` | Kansas City, soccer |
| `--mx-business` | `#b45309` | Business, NFL |
| `--mx-tech` | `#6d28d9` | Technology, volleyball, "new" |
| `--mx-gain` | `#15803d` | ▲ movement, W streaks |

Dots and edges are decoration. Every color also comes with a word (the category name, ▲/▼, W/L), so nothing depends
on color alone.

---

## Phase 1 — `mobile.css` and five components

### 1.1 Add `wwwroot/mobile.css`

Create `src/myNewspaper.Web/wwwroot/mobile.css` (the shipped file is the reference; the original draft's tokens,
ribbon, dark block, tiles, section heading, timeline, top list, fold group and `.mx-c-*` helpers are all in it).

Add it to `Components/App.razor` after `tablet.css` and before `admin.css`:

```html
<link rel="stylesheet" href="@Assets["mobile.css"]" />
```

> **Why `<details name="…">` for folds.** Most public pages render statically (no `@rendermode`), so a fold must work
> without a Blazor circuit. Native `<details>` opens and closes with no JavaScript, is announced as expandable by screen
> readers, and the `name` attribute makes a group exclusive (opening one closes the others) in Chrome 120+, Safari 17.2+
> and Firefox 130+. On an older browser several can be open at once, which is harmless.

### 1.2 Components (`Components/Phone/`)

Create the folder and add `@using myNewspaper.Web.Components.Phone` to `Components/_Imports.razor`.

**`PhoneRibbon.razor`** — the title band. The select is passed in as child content so each page keeps its existing
form/`navigate` behaviour. Parameters: `Kicker`, `Title`, optional `Extra` (a row between the title and the select:
date stepper, search box) and `ChildContent` (the one select, wrapped in `<div class="mx-ribbon__field">` with its
label).

**`SectionSelect.razor` change** — the ribbon always wants the dropdown, never the segmented control. Add a parameter
and use it in the class switch:

```razor
<div class="section-nav @(Choices.Count <= SegmentedLimit && !AlwaysSelect ? "section-nav--few" : "section-nav--many")">
```

```csharp
/// <summary>Phones: always show the dropdown (the ribbon has no room for pills or segments).</summary>
[Parameter] public bool AlwaysSelect { get; set; }
```

Placed inside `<PhoneRibbon>`, `mobile.css` hides its tabs and styles its form as the ribbon field, so no other change is
needed. Give the phone copy its own `Id` (e.g. `sports-section-m`).

**`FoldGroup.razor` and `Fold.razor`** — a `<section class="mx-folds">` with a double-rule heading, cascading itself to
`Fold`s; each `Fold` is a `<details class="mx-fold" name="@Group.Name">` with a serif name, a one-line gist, a body and an
optional "See all →" link. Parameters: `FoldGroup(Name, Heading, Foot?, FootHref?)`,
`Fold(Title, Gist, Color = "mx-c-world", Open, AllHref?, AllText = "See all")`.

Rows inside a fold are plain markup so each page can use its own links:

```html
<a class="mx-fold__row" href="…"><span><b>Kansas at TCU</b><small>Sat, Oct 31 · 2:30 PM · FS1</small></span><span class="mx-fold__value">3-4</span></a>
```

**`PhoneTimeline.razor`** — takes a list of a small record so pages map their own data:

```csharp
// Components/Phone/TimelineItem.cs
public sealed record TimelineItem(string Time, string Title, string? Sub, string? Tag, string Href, string Color = "mx-c-world");
```

Parameters: `Heading`, `Sub?`, `Items`, `MoreText?`, `MoreHref?`, and `ChildContent` above the list (Sports puts live
score tiles there).

The dark block and the top list vary too much per page to be worth a component; use the `.mx-block*`, `.mx-tile*` and
`.mx-top*` classes directly.

### 1.3 How a page adopts it

Each page keeps its current markup for desktop and adds a phone version beside it, fed from the same data it already
loads (no second query):

```razor
<div class="mx-desktop">
    @* existing page content, unchanged *@
</div>
<div class="mx-phone">
    <PhoneRibbon Kicker="…" Title="…"> … </PhoneRibbon>
    <section class="mx-block"> … </section>
    <PhoneTimeline … />
    <FoldGroup Name="sports-more" Heading="More in Sports"> <Fold Open="true" …>…</Fold> … </FoldGroup>
</div>
```

Hide the old phone-only page heading on converted pages (`.mx-desktop` already covers it if `PageHeading` sits inside).

> **Watch the duplicate IDs.** Both copies are in the DOM. Give phone controls their own IDs (`sports-section-m`), and
> keep only one copy of anything that `public.js` binds by ID (Save buttons use `data-*`, so they are fine).

---

## Phase 2 — Page specs

Every folded page: ribbon → dark block → (one middle list) → fold group. "First open" is the fold that starts open.
Values in *italics* on the canvas are sample data; the column "Data" says where the real value comes from or flags it.

### Home (`/`, `Home.razor`) — artboard "Home — phone, folded"

| Part | Content |
|---|---|
| Ribbon | Kicker = the date; title **Good afternoon** (morning/evening by time); today's weather as one line of four periods; the Where to watch search box instead of a select |
| Dark block | Lead story (`HomeLeadStory` data) with Save/Share |
| Middle | "Today & Tonight": live game tiles, then an "Up next" timeline with sport-colored dots; then top 3 "Headlines" |
| Folds ("More today") | Sports (open), Scoreboard, Top 25, Entertainment, Box office, Videos, Coming to screens, Opinion |

### Headlines (`/news`, `News.razor`) — "Headlines — phone, folded"

| Part | Content | Data |
|---|---|---|
| Ribbon | "Updated 2 min ago · 80 stories · 9 sources" / **The day so far** / select **Category** (replaces `hn-cats` pills) | existing feed counts |
| Dark block | Lead story, "Developing" badge, one-line standfirst | first story of Top Headlines |
| Middle | Top 3 ("Most read this morning"), then "This hour" timeline (last 3 stories, colored by category) | **flag:** there are no read counts today — label it "Top stories" and use feed order until reads are tracked |
| Folds | Kansas City (open), World, U.S., Business, Technology, Opinion | 2–3 latest per category; gist = first 1–2 titles |

### Sports (`/sports`, `SportsFront.razor`) — "Sports — phone, folded"

Ribbon select **Section** (`SectionChoices.Sports`, `AlwaysSelect`). Dark block = the featured game/story. Timeline
"Today's games" with live score tiles above it. Top 3 sports headlines. Folds ("More in Sports"): Big 12 standings
(open), AP Top 25, F1, College Sports, More headlines.

### Scoreboard (`/scoreboard`, `Scoreboard.razor`) — "Scoreboard — phone, folded"

| Part | Content |
|---|---|
| Ribbon | "5 live now · 11 more today" / **Scoreboard** / date stepper (prev ◂ date ▸ next, 48px buttons) / select **Sport** |
| Dark block | "Live now" — up to 4 live games as 2-column tiles (league + clock, both teams, serif scores) |
| Middle | "Up next" timeline: start time, matchup, league, TV tag |
| Folds | One per league: gist "5 games · 4 live · Leeds at Sunderland 11:30"; rows = matchup + clock/TV + score. First open: the league with the most live games |

The old All / Live / Final segmented control goes away; "Live now" covers it, and Final games stay in each league's fold.

### Top 25 (`/rankings`, `Rankings.razor`) — "Top 25 — phone, folded"

| Part | Content |
|---|---|
| Ribbon | "Poll dated Oct 18 · Big 12 highlighted" / **Top 25** / select **Poll** |
| Dark block | No. 1 with a 104px numeral and "up 2"; "Biggest moves" tiles (two largest rises, largest drop) |
| Middle | 2–10: numeral, school, Big 12 tag, record, ▲/▼/NEW (Big 12 rows tinted) |
| Folds | 11–25 (open), Big 12 in the poll, New this week, Biggest drops |

Moves are computed from rank and previous rank, which the page already shows.

### Standings (`/standings`, `Standings.razor`) — "Standings — phone, folded"

| Part | Content | Data |
|---|---|---|
| Ribbon | "Big 12 Football · 2026 season" / **Standings** / select **League** (replaces `st-leagues` pills) | |
| Dark block | First place (team, conference record big, overall + streak); "Your teams" tiles with place, record, streak, next game | **My Teams** setting; next game from the schedule |
| Middle | Places 1–6 table with a conference-win bar | |
| Folds | Places 7–16 (open), This week's games, Big 12 in the AP poll | |

### College Sports (`/college`, `College.razor`) — "College Sports — phone, folded"

| Part | Content |
|---|---|
| Ribbon | "Big 12 · Week 10 · times are Central" / **College Sports** / select **Sport** (the View select goes; its views become folds and links) |
| Dark block | Game of the week: both teams with rank and record, kickoff, network, "Game details" |
| Middle | Saturday timeline; your teams' games get the red dot and "your team" in the subline |
| Folds | Your teams (open), Friday night, Big 12 standings, Big 12 in the Top 25, Women's Volleyball |

> **Game of the week** needs a rule: pick the game between two ranked teams with the best combined rank; else the game
> involving the highest-ranked team; else the first game of the day.

### F1 (`/f1`, `F1.razor` / `F1Home.razor`) — "F1 — phone, folded"

Ribbon select **View** (replaces `F1Tabs` on phones). Dark block = next or current race, start time as a 60px numeral,
"Weekend timetable". Middle = championship top 3 with a points bar against the leader and the gap. Timeline "Next up"
(3 rounds). Folds: Drivers' standings (open), Constructors' standings, F1 headlines.
**Flag:** constructors and F1 headlines are placeholders on the canvas; use whatever the F1 page already has, or drop
those folds.

### Movies (`/movies`, `Movies.razor`) — "Movies — phone, folded"

Ribbon select **Release type** (`MovieViewSelect`). A 7-day strip under the ribbon (today in dark, a red dot on days with
releases). Dark block = the next release this week (poster, service, "Streaming now"). Folds: Looking ahead (open),
In theaters (box office top 3), Trending.

### TV & Streaming (`/tv`, `Tv.razor`) — "TV & Streaming — phone, folded"

Ribbon "Your episode calendar · N tracked" / select **Week** (This / Next / Last). Dark block = tonight's episode, or the
next one. Timeline "This week" (day, show, episode, network tag). Folds: Your shows (open; per-profile once profiles
ship), Trending TV.

### Digital releases (`/movies/digital`, `DigitalReleases.razor`) — "Digital releases — phone, folded"

Ribbon select **Source** (replaces the 3-way segmented control). Dark block holds the empty state ("No confirmed dates
yet" + yellow "Try …" button + two outline buttons) or, when there are dates, the next release. Folds: Coming soon to
digital (open), Still in theaters.

### Box office (`/box-office`, `BoxOffice.razor`) — "Box office — phone, folded"

| Part | Content |
|---|---|
| Ribbon | "Estimates · The Numbers" / **Box office** / select **View** (Results · weekend / Next weekend / Forecasts) |
| Dark block | No. 1 with a 96px numeral, title, gross; tiles: number of openers, best hold, biggest drop |
| Middle | 1–5 with a gross bar scaled to No. 1, change %, NEW badge |
| Folds | 6–10 (open), Biggest totals, Next weekend (from `BoxOfficeForecastSection`) |

### Where to watch (`/watch`, `Watch.razor`) — "Where to watch — phone, folded"

Ribbon "Movies & TV · N services chosen" / **Where to watch** / search box + dark Search button as the extra row /
select **Browse** (Featured / Movies / TV shows). Dark block = numbered "Trending movies" poster rail (scrolls
sideways inside the block only). Folds: Trending TV (open), New movies you can watch, Your services.

### Local (`/local`, `Local.razor`) — "Local — phone, folded"

Ribbon "Closer to home · updated 10:29 AM" / **Kansas City** / select **Section** (Overview / News / Weather / Events).
Dark block = today's temperature (80px), summary, wind, "Use my location", three period tiles, "National Weather
Service". Top 3 local news. Folds: More local news (open), Forecast, Things to do.

### YouTube (`/videos`, `Videos.razor`) — "YouTube — phone, folded with dark featured block"

Ribbon select **Channel**. Dark block = featured video with a play button, "Watch later" and Share. "Latest" list.
Folds ("By channel"): Big 12 Sports (open), Tech, Games, Politics, Watch later.

### Entertainment (`/entertainment`, `EntertainmentFront.razor`) — "Entertainment — phone, folded"

Ribbon select **Section** (`SectionChoices.Entertainment`, `AlwaysSelect`). Dark block = lead story. "On screen"
timeline. Top 3 entertainment headlines. Folds ("More in Entertainment"): Weekend box office (open), Tonight on TV,
Movies this week, Friday in theaters, More headlines.

### Ribbon-only pages (no folds)

These are long lists or tools; folding would get in the way.

| Page | Change |
|---|---|
| Sports · All headlines (`/sports/news`) | `PageHeading` → ribbon "Sports · every story, every source" / **Sports headlines** / **Section**; first article becomes a dark card (dark background, white title, yellow kicker). Sport and Source selects stay as they are |
| Entertainment · All headlines (`/entertainment/news`) | Same treatment with **Entertainment headlines** |
| Saved (`/saved`) | Ribbon "Shared across your devices" / **Saved** / select **Show** (All · n, Articles · n, Videos · n) replacing the segmented control; the first saved item becomes a dark card |
| Settings (`/settings`, `MyTeams.razor`) | Ribbon "Make it yours · saved on this profile" / **Settings** / select **Jump to** that scrolls to each section (`location.hash` on change; works without JS via a Go button) |

> **Dark card on white list pages:** use `.mx-block` with `border-radius: 16px` on the first `<article>` only. Every
> button inside needs `color: var(--mx-block-fg)`; the Saved mockup first shipped with an invisible Share icon because
> the icon kept the dark ink color.

---

## Phase 3 — Verify (per AGENTS.md)

Run at 320, 390 and 430px, light and dark, and at least two other color schemes (`monochrome`, `cardinals`):

1. **Ribbon:** the title wraps at 320px without overflow ("Entertainment headlines", "Digital releases"); the select
   never pushes past the right edge with long options ("Women's Volleyball · AVCA Top 25"). Font size stays 16px.
2. **Folds:** with JavaScript off, every fold opens and closes; opening one closes the others in the same group; the
   first one starts open; keyboard Enter/Space toggles; VoiceOver/TalkBack announce "collapsed/expanded".
3. **Dark block:** text contrast ≥ 4.5:1 for body, ≥ 3:1 for the big numerals; in dark mode the block still separates
   from the page (`--mx-block-bg` is darker than `--bg`).
4. **Touch targets:** summaries ≥ 64px tall, all buttons and links ≥ 44px.
5. **Data edges:** no live games (Scoreboard dark block hides; "Up next" leads), empty folds (hide the fold, don't show
   "0"), a poll with no previous ranks (no moves tiles), Digital releases empty state, a league with one game.
6. **Desktop/iPad:** at 744, 1024 and 1440px nothing changes from today.
7. **Duplicate IDs:** run the browser's accessibility check; no repeated `id` on converted pages.

Record widths, themes and outcomes in the release notes, as AGENTS.md requires.

---

## Suggested order of releases

Each release is independent: a page that hasn't been converted keeps its current phone layout.

- [x] 1. `mobile.css`, the components, the `SectionSelect` parameters, and **Headlines** (the pattern the rest copy).
  Deployed October 28, 2026 as `phone-headlines-20261028` (commit `47ab9ca`); see the release history.
- [x] 2. **Home**, **Sports**, **Scoreboard**. Commit `9e96beb`, deployed October 28, 2026 as `phone-fronts-20261028`
  (image `e1ea2d9dc6c1`); see release history. `phone-fronts.browser.cjs` passes locally (profiles off) and live
  (profiles on, temporary profile deleted) at 320/390/430px, three schemes, light/dark, no-JS folds,
  date stepper, Sport/Section selects, refresh keeping the open fold, hidden Home section, and retained
  744/1024/1440px layouts. Screenshots reviewed. 931 .NET tests pass. Tablet scoreboard grid selectors were updated
  for the new wrapper after verification found overflow. The Sports **College Sports** fold was left out
  (the Sports front loads no college schedule). Real-device and VoiceOver/TalkBack checks remain unverified.
- [x] 3. **Top 25**, **Standings**, **College Sports**, **F1**. Deployed October 28, 2026 as
  `phone-sports-20261028` (image `df265789b18e`); see release history.
  Uses the shared phone ribbons, dark blocks, native exclusive folds and existing data. Top 25 shows the leader,
  rank changes, 2–10 and poll folds. Standings retains each provider group/order, with top-six rows, remaining
  places, My Teams tiles, next games and a matching Big 12 poll. College chooses its featured game by combined
  ranks (then highest-ranked participant, then first scheduled), with a weekly timeline and personal/schedule/
  standings/poll folds. Its other views retain their content with Sport in the ribbon and plain view links.
  F1 shows the next weekend, timetable, championship top three, upcoming rounds, full driver/constructor folds
  and the collected calendar; Results and Standings retain their tables with native phone selectors. The F1
  headlines fold is omitted because no headlines are loaded there; a link opens Sports' F1 headlines instead.
  Friday games are labelled by day rather than assuming every sport plays at night. Other college sports are
  linked from Explore instead of inventing unqueried schedules. Shared My Teams choices work from the open
  profile (also without JavaScript) or local browser preferences while profiles are off.
  948 .NET tests pass. Local and live `phone-sports` pass at 320/390/430px in three schemes, light/dark,
  including contrast, touch targets, native/keyboard folds, no-JS selectors, saved My Teams and navigation.
  Retained tablet/desktop layouts, all College views, F1 timetable/calendar, and earlier phone fronts passed
  regression checks. Representative screenshots reviewed. A final two-column Standings My Teams refinement
  was checked again at all three phone widths in both themes. Runtime configuration and keys preserved;
  rollback is `mynewsfeed-web:before-mynewspaper-phone-sports-20261028` (`0949497c41b3`). Evidence:
  `/tmp/release3/`. Real-device and VoiceOver/TalkBack checks remain unverified. Included in the scheme-ribbon local check-in.
  Follow-up: soccer standings now highlight **points**, followed by **W–D–L**, across the leader, personal tiles,
  top-six and folded rows (including College soccer). This replaces the accidental games-played highlight.
  Deployed as `soccer-points-20261028` (`79a593092535`); 955 .NET tests pass. Local NWSL checks passed at
  320/390/430px in light/dark, plus MLS/Championship/Premier League data checks, no-JS and retained desktop tables.
  Live verification is recorded in release history. Evidence: `/tmp/soccer-points/`. Included in the scheme-ribbon local check-in.
- [x] 4. **Entertainment**, **Movies**, **TV & Streaming**, **Digital releases**, **Box office** — ✅ Done October 28, 2026 (Central): commit `ec95073`, deployed as `phone-screens-20261029` (`2cf78ecff635`); see the working record below.
- [x] 5. **Where to watch**, **Local**, **YouTube** — deployed October 28, 2026 as `phone-life-20261028` (`7176ce3f5fa0`); see the working record below.
- [x] 6. Ribbon-only pages: both All headlines pages, **Saved**, **Settings** — ✅ Done October 28, 2026: commit `63748dc`, deployed as `phone-tools-20261028` (`ce1907bbd201`); see the working record below.

## Follow-up — Home lead restricted to Top Headlines (deployed October 28, 2026)

Deployed as `top-headlines-lead-20261028` (image `0949497c41b3`): `HomeLeadQuery.Snapshot` restricts candidates to **Top Headlines**
before publisher grouping and hourly selection. Desktop and phone Home share this snapshot, as do `/home/lead` and
the edition-picker preview. No fallback to other categories; hourly publisher rotation is retained.

Verification: 934 .NET tests passed, including three new lead-selection tests (mixed categories, empty Top Headlines,
and newest-per-publisher/hourly rotation). Local browser checks with profiles off passed at 320/390/430/768/1440px
in light/dark: desktop source is Top Headlines, phone and desktop link to the same story, reload preserves selection,
Share opens/closes, and no sideways overflow. All six phone screenshots reviewed. No-JavaScript phone and the
`/home/lead` endpoint also passed. Evidence: `/tmp/home-top-headlines/` and `/tmp/home-top-headlines-tests.log`.
The same widths/themes and flows passed live with profiles on, including the no-JavaScript phone and refresh
endpoint; all six phone screenshots reviewed. The temporary profile was deleted. Live evidence:
`/tmp/home-top-headlines/deploy/`. Runtime settings and keys preserved; rollback is
`mynewsfeed-web:before-mynewspaper-top-headlines-lead-20261028` (`e1ea2d9dc6c1`). See release history.
Not checked: a real device.

## Open questions

- **"Most read"** on Home and Headlines needs read counts the site doesn't collect. Use feed order and call it "Top
  stories" until then, or add a lightweight `MyDailyFeedArticleReads` table later.
- **Game of the week** (College) and **"Your teams"** (Standings, College) depend on the profile's My Teams; for Guest,
  fall back to the first game / hide the tiles.
## Scheme-colored phone ribbons — October 28 follow-up

Deployed as `mynewspaper-web:scheme-ribbons-20261028` (`c1e3fa5259c3`); included in the local check-in on `develop`. Shared phone title bands (including Home greeting,
weather and search) use the selected scheme's secondary selection background and paired foreground.
Daily Ribbon retains yellow in light mode. Dark mode mixes 25% secondary color with the scheme surface
and uses the scheme text color. The search button has a contrasting outline; the live dot follows the
ribbon foreground. Tablet and desktop layouts retain their existing styling.

Verification evidence: `/tmp/ribbon-schemes/`. Build passed with no warnings or errors.
Browser verification passed: all 16 schemes × light/dark × 320/390/430px on Home, including
contrast computed against blended weather/ribbon backgrounds and horizontal overflow. Inspected Home
screenshots for Kansas light, City dark, Coastal light and the 320px no-JavaScript fallback, plus Standings
dark at 320px. Seven other shared-ribbon routes (News, Sports, Scoreboard, Rankings, Standings, College,
F1) passed visibility/overflow checks at all three widths in both themes. Saved scheme/theme reload,
Home search navigation and 44px input/button targets passed. At 768/1440px the phone ribbon stays hidden;
Kansas/City Home contrast passed at 390/1440px in both themes. The same 16-scheme/phone/theme matrix passed live with profiles enabled, including saved preferences,
search, controls, the seven other ribbon routes, tablet/desktop visibility and no-JS Home. Live Kansas
light, City dark and Coastal light screenshots inspected. Temporary test profile deleted. Deployment
and rollback recorded in release history; evidence `/tmp/ribbon-deploy/`. Real devices remain unverified.


## Release 5 working record — October 28, 2026

Status: implemented and deployed from `develop` after `bb368cc`; source and deployment records are
included in the release 5 local check-in. The user requested Markdown updates throughout implementation and deployment. Release 4
remains pending; release 5 was taken next as requested.

- [x] Watch: scheme ribbon with search/Browse, numbered trending-movie rail and folds for Trending TV,
  new movies and services. Existing search/provider results, sort, paging and detail modal retained.
- [x] Local: scheme ribbon, one weather request/controller shared across layouts, labelled first forecast
  period and next-three tiles, top three news stories, folds for more news/full outlook/event calendars.
  Location/reset, official/stale/error labels, honest feed coverage and cursor pagination retained.
- [x] YouTube: scheme ribbon with a combined category/channel select retaining query context, dark
  featured video, Watch later/Share, short Latest list and folds from real categories/channels.
- [x] Short fold previews link to complete phone lists (`local?view=news`, `videos?view=all`). Empty
  groups are omitted; Saved's existing video view and pagination remain accessible.
- [x] Local mobile/interaction and tablet/desktop verification, screenshot review, build/lint/formatting.
- [x] Deploy with runtime/keys preserved, retain rollback, repeat live checks and record evidence.

Implementation checkpoints found and corrected string-parameter bindings and short headline touch targets.
Screenshot review also caught Watch shelves narrowed by the display:contents wrapper; wrapper-aware grid
rules restore the original full-width shelves, with an assertion at 744/1024/1440px. Each page reuses its
existing data; no second query, schema migration or collector change was introduced.

Local verification: `phone-life` passed 320/390/430px × Kansas/Monochrome/Cardinals × light/dark, including
contrast, overflow, unique IDs, 44px targets, 64px summaries, native exclusive/keyboard folds, weather
focal/tiles/full outlook, mocked save/unsave, sharing/Escape/focus, Watch modal/search/sort, category/query/
reload, complete lists and empty video search. No-JavaScript disclosures/selectors passed at all three
phone widths in the default fallback rendering. Retained 744/1024/1440px layouts passed in both themes.
After the wrapper correction, targeted Watch checks reconfirmed full-width shelves, same-row title/search,
arrows and phone layout in both themes. Build passed with zero warnings/errors, plus lint, changed-code
formatting and `git diff --check`. The full .NET suite was not rerun for this UI-only change.

Existing local suites passed: `video-share`, `youtube-categories`, `location-weather`, `weather-icons`,
`watch-featured`, `watch-sort`, `watch-results`, `watch-paging`, and `watch`. `phone-local-weather` passed
320/390/430px light/dark with single/stale/empty/503 forecasts, no invented extra tiles and one request
across phone/desktop resizing. Representative phone, wider-layout and no-JavaScript screenshots reviewed.
Local evidence: `/tmp/release5/`; the preview is stopped.

Deployed `mynewspaper-web:phone-life-20261028`, image
`sha256:7176ce3f5fa02019ab52f8ee66e89e6f4cdf5506f3199ff097b88ce7ea0a2a3c`.
Rollback: `mynewsfeed-web:before-mynewspaper-phone-life-20261028` (`c1e3fa5259c3`). Environment values,
entrypoint, ports, persistent keys/mounts, restart policy, health check and network verified preserved.
Container healthy; `/healthz` 200 Healthy. Profiles remain enabled.

Live `phone-life` passed the same three-width/three-scheme/light-dark matrix with profiles on, including
all phone links/buttons/input/select targets, native/keyboard/no-JS controls, mocked save/unsave, share,
Watch modal/search/sort, category/query/reload, complete lists, empty videos and retained 744/1024/1440px
layouts. Real weather API returned 200 and populated four forecast periods. Authenticated main-route
checks passed; recent application logs had no error markers and regular-season phase checks ran.
Phone screenshots across all required widths/themes, the wider Watch layout and real forecast were
inspected. The temporary profile and two orphaned headless test-device records were removed;
Jason and Jacob remain.
Live evidence: `/tmp/release5-deploy/`.

Limits: no real device or VoiceOver/TalkBack check. Live browser weather fixtures and mocked Saved API
isolate test data; the real forecast was separately verified. Geolocation permission/fallback flows were
verified on the secure local preview, not repeated over the container's plain-HTTP live address.
User subsequently requested a local commit before starting release 6. Release 5 source, tests and
deployment records are included in that check-in on `develop`. No push performed.


## Release 6 working record — October 28, 2026 — ✅ Done

Status: committed locally as `63748dc` on `develop` (not pushed) and deployed as `phone-tools-20261028`.
The user requested continued Markdown updates. Release 4 remains pending.

- [x] Review Sports/Entertainment headline routes, Saved rendering/counts and Settings anchors/forms.
- [x] Headlines: scheme-colored phone ribbons with native Section selection; retain Sport/Topic and
  Source filters, pagination/Load more and article save/share. Highlight the first displayed article.
- [x] Saved: scheme ribbon with counted Show select; highlight the first displayed saved item without
  duplicating the list or changing the Saved API. Retain remove/share, empty/error and no-JS messaging.
- [x] Settings: scheme ribbon with Jump to selection and no-JS Go path; keep the existing sections,
  forms, ordering and preference persistence. Show only anchors applicable to the open profile.
- [x] Verify 320/390/430px × light/dark and representative schemes, interactions, saved preferences,
  no-JS, empty/error states, touch targets, contrast, overflow and unique IDs; inspect screenshots.
- [x] Verify retained 744/1024/1440px layouts and relevant headline/Saved/Settings regressions.
  Full local browser suite: 40 passed, 10 skipped (profiles-only), 15 failed. Of the failures, 10 are the
  known local-only ones without a streaming-catalog key (watch*, active-tv, us-options, mixed-search-nav,
  track-series; phone-life's Watch rail for the same reason). `sports-news` and `entertainment-news` (two
  `h1`s now) and two release 5 leftovers, `watch-later` (phone copy of the save button) and `section-select`
  (Local's links moved into the ribbon select), were updated to the visible copy; all four pass. 955 .NET
  tests pass; eslint, prettier and `git diff --check` clean.
- [x] Finalize implementation and verification details here and in interface documentation.
  `docs/interface.md` ("Phone ribbons on All headlines, Saved and Settings") and `docs/release-history.md`.
- [x] Deploy with runtime/keys preserved, retain rollback, repeat live checks and record evidence.

Local verification (profiles off, port 5296): new `tests/browser/phone-tools.browser.cjs` passes
at 320/390/430px × Kansas/Monochrome/Cardinals × light/dark on `/sports/news`, `/entertainment/news`,
`/saved` and `/settings`: ribbon shown and desktop heading hidden, contrast audit, no overflow, unique IDs,
44px ribbon/feature targets, exactly one feature (the first article), counted Show options, Settings index
hidden and every Jump to anchor present (profile-only anchors absent with profiles off). Interactions:
Section select, Load more adds no feature, feature Share/Escape, removing the featured Saved item moves the
feature and updates counts, Show → Videos, Jump to scrolls to My Teams. Empty Saved (zero counts, no
feature) and an unreachable Saved API (503) keep the ribbon. No-JS Go paths for Section, Show and Jump to
(the redirect keeps `#my-services`). At 744/1024/1440px light/dark: no ribbon, headings/tabs/index visible,
no phone feature styling, contrast and overflow pass. Screenshots reviewed; evidence `/tmp/release6/local/`.

Found and fixed during verification: at 320px the 44px ribbon title split "Entertainme / nt"
(titles now 36px below 360px; the check asserts no word splits); the headlines ribbon `h1` showed a focus
ring because Blazor focuses the first `h1` after navigation (`.mx-ribbon h1:focus` now has no outline, as
the other headings do); `FeedArticles`' new parameter had taken `LeadFirst`'s doc comment.

Deployed `mynewspaper-web:phone-tools-20261028` (`ce1907bbd201`) from `/tmp/release5-deploy/override.json`
with only the image tag changed. Rollback: `mynewsfeed-web:before-mynewspaper-phone-tools-20261028`
(`7176ce3f5fa0`). Environment hash unchanged; container healthy, `/healthz` 200. Live `phone-tools` (profiles
on, throwaway profile) passed the same matrix, including the profile-only Jump to anchors; the profile was
deleted and two headless test-device records without a profile were removed (Jason and Jacob remain). Live
screenshots reviewed; evidence `/tmp/release6/deploy/`. Real-device and VoiceOver/TalkBack checks are
unavailable.

Implementation checkpoint: shared headline cards accept a phone-feature flag only for the first initial
article, so Load more cannot introduce extra dark leads. Saved retains one DOM list and adds counts to
the native Show options; the first item is chosen after filtering/grouping and moves when removed.
Its render signature now includes the selected filter. Settings Jump to uses the current page hash with
JavaScript and the existing local `/navigate` redirect without it. Profile-only anchors are conditional.
Dark-card foreground/button overrides are scoped to phone widths. Build passed with zero warnings/errors.

## Release 4 working record — October 28, 2026 — ✅ Done

Status: committed locally as `ec95073` on `develop` (not pushed) and deployed as `phone-screens-20261029`. All six releases are now live.

- [x] Entertainment: Section ribbon, dark lead, "On screen" timeline, top three headlines, folds (Weekend box
  office open, Tonight on TV, Movies this week, Friday in theaters, More headlines).
- [x] Movies: Release type ribbon, 7-day strip, dark block for the next release this week, folds (Looking ahead
  open, In theaters, Trending).
- [x] TV & Streaming: Week ribbon, dark block for tonight's (or the next) episode, "This week" timeline, folds
  (Your shows open, Trending TV).
- [x] Digital releases: Source ribbon, dark block (next release or the empty state), folds (Coming soon to
  digital open, Still in theaters).
- [x] Box office: View ribbon, dark No. 1 block with tiles, 1–5 with gross bars, folds (6–10 open, Biggest
  totals, Next weekend).
- [x] `phone-screens` browser check: 320/390/430px × three schemes × light/dark, no-JS folds and selects, data
  edges, 744/1024/1440px retained; screenshots reviewed.
- [x] Full browser suite, .NET tests, lint/format; commit, deploy, live check, records.
  Full local browser suite: 43 passed, 10 skipped (profiles-only), 13 failed. 11 are the known local-only ones
  without a streaming-catalog key (watch*, active-tv, us-options, mixed-search-nav, track-series, phone-life's Watch
  rail). `section-hubs` (Entertainment's Section select is now the phone copy) and `theatrical-provider` (the
  phone Movies page had lost the AMC Theatres link; restored under the week strip, and the Digital releases note
  now matches the desktop wording) were fixed and pass. 955 .NET tests pass; eslint, prettier and
  `git diff --check` clean.

Implementation notes and deviations from the mockups:

- Every page keeps its desktop markup in `.mx-desktop` and adds an `.mx-phone` copy from the data it already loads.
  Phone controls have their own IDs (`entertainment-section-m`, `movie-view-m`, `tv-week-m`, `digital-source-m`,
  `box-office-view-m`), each with a no-JavaScript Go button.
- **Trending** (Movies) and **Trending TV** (TV) are left out: they would need the streaming catalog's live API.
  Where to watch already carries trending titles on phones.
- **Movies:** the mockup's strip only marks release days, so a "This week" timeline lists the week's releases
  (the strip links to it); a title search keeps the old layout at every width. The dark block links to Where to
  watch (and the store, when there is one) instead of Save/Share, which only articles and videos support. The
  In theaters fold adds one box office read (the last weekend's top three), as the Entertainment front does.
- **TV:** weeks start today, so the Week select offers This week / Next week / In two weeks (no Last week). The
  dark block uses the batch label for multi-episode drops ("E10–E13 · 4 episodes"). TVmaze attribution stays on
  phones. Your shows' "Manage" link appears only with a profile open.
- **Digital releases:** the Coming soon fold lists every remaining release so nothing is lost on phones; Still in
  theaters adds one box office read (biggest totals). The empty state could not be shown locally (every source has
  dates); it renders from the same `data.Items.Count == 0` branch as the desktop empty state.
- **Box office:** Results is folded; the two forecast views keep their single section under the ribbon
  (`.mx-tool-page`). The Results view also reads next weekend's predictions for its last fold.
- **Entertainment:** "Movies this week" is titled "Next releases" when the next four reach past seven days.
- Shared CSS: dark-block and top-list title links are now 44px flex targets everywhere (release 5 had this for its
  own pages only); new `.mx-days`, `.mx-chart`, `.mx-gross`, `.mx-source-note`.
- Bug fixed along the way: `/movies?type=digital` (the Digital option in the Movies select, desktop and phone)
  returned a 500. In .NET 10 static rendering still renders the page after `NavigateTo`, and the week math ran on
  an unset date. It now redirects to `/movies/digital`.

Local verification so far (profiles off, port 5296): `phone-screens` passes 320/390/430px × Kansas/Monochrome/
Cardinals × light/dark on all seven routes (Entertainment, Movies, TV, Digital releases, Box office results,
next weekend and forecasts): one ribbon, desktop heading hidden, no overflow, unique IDs, contrast audit, no split
title words, first fold open, exclusive and keyboard folds, 64px summaries, 44px targets. Interactions: lead
save/share, every ribbon select, Movies' strip (seven days, today marked, counts in words) and search, Digital
source switch, TV week, Box office chart and View. No-JS folds and Go paths at each phone width. 744/1024/1440px
light/dark: no phone copy visible, headings shown, contrast and overflow pass. Screenshots reviewed. Found and
fixed: a one-line dark-block title and two-line top-list titles under 44px; the NEW badge's dark-mode contrast
(2.33:1); the Movies 500 above.

Deployed `mynewspaper-web:phone-screens-20261029` (`2cf78ecff635`; the tag uses the UTC date) from a copy of the
release 6 override with only the image tag changed. Rollback: `mynewsfeed-web:before-mynewspaper-phone-screens-20261029`
(`ce1907bbd201`). Environment hash unchanged; healthy; `/healthz` 200. Live `phone-screens` (profiles on, throwaway
profile) passed after the check started blocking third-party images: the first two runs stalled because the poster
host throttled the run's fresh fetches (without JavaScript Chrome ignores `loading="lazy"`). Throwaway profiles
deleted; 14 headless test devices without a profile removed; Jason and Jacob remain. Live screenshots reviewed.
Evidence: `/tmp/release4/`. Not checked: the Digital releases empty state with real data, a real device,
VoiceOver/TalkBack. See the release history.

### Mobile score-card quarter shorthand — October 29, 2026

Phone score tiles display quarter statuses as e.g. `1:56 · 4Q`, retaining
the original full status in the accessible link label. Other statuses and desktop
markup are unchanged. All 38 PhoneSportsTests passed, including quarter mappings
and rendered accessible status checks. Browser layout fixtures used the rendered
component and repository CSS at 320/390/430px in light/dark; status text stayed
inside its card and the page had no horizontal overflow. Screenshots were inspected.
Fixtures blocked network assets, so fallback fonts were used. These checks cover
representative markup rather than a live game or real device. Full live-page
interaction and post-deployment evidence are recorded in the release history.

### Requests choice alignment — October 29, 2026

The Requests form now uses a normal fieldset with the legend above a separate
choices grid. Choices stack below 360px and share an equal-width row at larger
widths, including desktop; the group aligns with the form fields. Public build
passed with zero warnings/errors; CSS Prettier and diff whitespace checks passed.
Actual preview Settings checks passed at 320/390/430/768/1440px light/dark:
legend placement, choice alignment, both radio selections, 44px minimum targets,
no horizontal overflow, refresh, saved theme and no-JavaScript radio selection.
Screenshots were inspected under `test-results/browser/request-format/` (ignored).
The temporary profile was removed; no content requests were submitted. Physical
device checks were unavailable. Deployment and live verification are recorded in
the release history.

### iPad portrait uses the mobile version — October 29, 2026 (Central)

The public website now switches to desktop at 1024px. The mobile version covers
768/820/834px iPad portrait widths and the complete range below 1024px, with the
same menu, ribbons, section selects and folds as phones. The former tablet-only
desktop stylesheet was removed. Shared public CSS, public page CSS and article
image loading now use the same boundary; browser assertions were adjusted. The
1024px-wide large iPad portrait remains desktop under this width-based policy.
A long live hockey status found during the 320px check now wraps within its card.
Unrelated local ranking query/test edits were preserved.

Validation: public build succeeded with zero warnings/errors; ESLint passed.
The preview used profiles disabled and background jobs disabled. All page/width/
theme checks logged success for 17 public pages at 320/390/430/744/768/820/834/1023/
1024/1280/1440px light/dark: Home, News, Kansas City, Sports, Scoreboard, Standings,
Rankings, College, F1, Entertainment, Movies, TV, Box office, Watch, Videos, Saved
and Settings. Assertions covered mobile/desktop visibility, no page overflow,
menu targets and opening/closing, and available folds. The broad harness exited
143 after the page checks; a focused Home run then completed successfully,
including all widths/themes, refresh, saved theme and no-JavaScript menu navigation.
Screenshots were inspected for Home at phone/iPad/desktop widths in both themes
and News on iPad. Artifacts: `test-results/browser/ipad-mobile/` (ignored). Remote
images were blocked to avoid third-party stalls; local fonts/scripts loaded.
Physical-device checks were unavailable. Signed-in production verification and
deployment evidence are recorded in the release history.

### Ranking source exclusion — local commit, October 29, 2026 (Central)

Committed the existing edit excluding Women's Basketball sources from RankingsQuery
and its season-start regression assertion. All 13 RankingsTests passed, including
the SQL-backed source-filter test. The earlier local iPad/mobile preview included
these edits while checking the Rankings page at 320/390/430/744/768/820/834/1023/
1024/1280/1440px light/dark, menus, folds and overflow. That supplies local rendered
verification; the focused integration test checks the exclusion itself. Real-device
checks were unavailable. This change was excluded from the deployed iPad image and
is now included in the masthead-links deployment; production verification is
recorded in the release history.

### Homepage masthead links — October 29, 2026 (Central)

Moved Kansas City immediately below the weather heading and Scoreboard immediately
below Live now/Coming up. The forecast uses a single full-width parent column,
with its existing two-by-two desktop period grid. Obsolete link-side-column rules
were removed. Public build passed with zero warnings/errors; CSS Prettier and diff
whitespace checks passed. Actual preview checks passed at 320/390/430/768/820/834/
1024/1280/1440/1817/1920px light/dark: no horizontal overflow, existing phone/iPad
layout visibility, desktop heading/link/content order, left alignment, 44px links
and full forecast width. Both links opened the correct pages with and without
JavaScript; refresh retained the theme. Desktop masthead and representative phone
screenshots were inspected under `test-results/browser/masthead-links/` (ignored).
Preview used profiles/background jobs disabled and blocked third-party images;
local fonts/scripts loaded. Physical-device testing was unavailable. Deployment
and live verification are recorded in the release history.

### Standings team logos, without win bars — October 29, 2026 (Central)

Removed the mobile standings win-percentage meters. Restored safe provider logos
in team rows, the featured leader and Your teams, with a white backing for dark
logos. Missing/failed images leave names and numeric records readable. Desktop
keeps its existing table logos. All 41 focused PhoneSportsTests passed. Preview
and production checks covered 320/390/430/768/820/834/1024/1440px light/dark, real
logo loading, no meters or overflow, team/record spacing, 44px links, followed-team
and leader logos, league selection, refresh, saved preferences, failed-logo fallback
and no-JavaScript League form navigation. Screenshots were inspected. Production
is running `mynewspaper-web:standings-logos-final-20261029` (`3e5afc6aa2ea`).
Logs/screenshots are under `test-results/browser/standings-logos-live/` (ignored).
A disposable profile was removed. Real-device checks were unavailable; release
and rollback evidence are in the release history.

### Complete box-office list without bars — October 29, 2026 (Central)

The mobile/iPad Results chart renders all collected rows in one ordered list,
without the blue gross bars or separate 6–10 fold. Numeric grosses, new-release
badges and change labels remain. Two-digit ranks have additional column space.
Desktop retains its full table. The existing browser regression now requires
mobile/desktop row counts to match and asserts no bars or 6–10 disclosure.

Public build passed with zero warnings/errors. CSS/browser-test formatting and
diff whitespace checks passed. Actual preview checks covered
320/390/430/768/820/834/1024/1440px light/dark with ten real chart entries: matching
ranks/counts, all rows outside disclosures, rank-10 fit, titles clear of grosses,
no horizontal overflow and desktop-table visibility. Screenshots were inspected.
Refresh retained theme, View changed to Next weekend and back, and all entries
remained available without JavaScript; the no-JavaScript View form opened Forecasts.
Artifacts: `test-results/browser/full-boxoffice/` (ignored). Profiles/background
jobs were disabled in preview. Physical-device and signed-in checks were not run.
Deployment and production verification are recorded in the release history.

### P8 no-photo frames — October 29, 2026 (Central)

The approved [P8 design](no-photo-placeholder-handoff.md) is implemented in shared
feed thumbnails, headline cards, Home lead and missing-photo Opinion thumbnails.
Headline cards always keep a frame and the lead + two supporting + Latest layout.
Phone lists keep a full first-feature frame and use 96px thumbnails for rows;
small frames omit the initials/category labels. Publisher names and action buttons
have separate lines beside the thumbnail at narrow widths. Dark mode uses the
category/navy mix. The dedicated folded phone News front is unchanged.

Preview checks passed 320/390/430/820px light/dark and 1024/1280/1440px across
Kansas, Cardinals and monochrome schemes. Fixtures covered wide, compact, tiny,
missing and late-failing images, correct frame ratios, label suppression,
decorative markup, story order, no overflow, share dialogs, 44px actions, save and
theme persistence after refresh. Article-image regression covered enhanced
navigation, Load more and no-JavaScript fallbacks. Screenshots were inspected.

Final live verification passed 320/390/430/820px light/dark and
1024/1280/1440px desktop (Kansas, Cardinals and monochrome schemes). Image
fixtures/failures, frames and label suppression, story order, Home placeholders,
share dialogs, 44px actions, 390px save/refresh/unsave, saved themes and
no-JavaScript P8 passed. Phone, iPad and desktop screenshots were inspected.
Both HTTPS health endpoints returned 200; test profiles were removed.

See [release history](release-history.md#p8-no-photo-frames--october-29-2026-central)
for image tags, rollback and artifacts. Physical devices and Safari were unavailable.

### Homepage “New Tonight” placement — October 30, 2026

Moved the masthead episode teaser from the weather box to the Coming up / Live now
box, below its games. The forecast box now renders only when forecast data exists;
episode title, platform/time and the TV link are retained. The separate phone
“Coming to screens” fold is unchanged.

Validation: public build passed with zero warnings/errors; CSS formatting and
whitespace checks passed. Preview browser checks covered 320/390/430/820/1024/
1440/1870px in light/dark: correct parent box and order, no overflow, 44px desktop
episode link, saved theme after refresh, phone TV fold opening and navigation.
The TV link also worked without JavaScript at 390/1440px. Existing homepage-column
regression passed with and without JavaScript. Representative phone and desktop
screenshots were inspected. Evidence: `test-results/browser/tonight-move/` and
`/tmp/tonight-move-*.log`. Physical devices and Safari were unavailable.
Deployed `mynewspaper-web:tonight-box-20261030` (`d375ffdbd724`); runtime settings
and keys were preserved. Live checks passed all seven widths in light/dark, including placement, overflow,
44px link, phone fold/TV navigation and saved theme after refresh. No-JavaScript
TV links worked at 390/1440px. Live screenshots were inspected; both HTTPS health
endpoints returned 200. The temporary test profile was removed. See release
history for rollback and deployment evidence.

### Mobile header theme toggle — October 30, 2026

Exposed the existing sun/moon theme button immediately beside Saved in the mobile
header, using the same outlined 44px treatment as Saved and Menu. The existing
JavaScript keeps both header/menu toggle labels, icons and pressed state in sync,
and retains the selected theme across refresh and navigation. Desktop order is
unchanged. Narrow-phone spacing accommodates long weekday/date and time labels.
If an admin also has the Requests action, narrow screens place the clock on a
second row; the menu sheet fills the remaining viewport below the actual header.

Preview checks passed 320/390/430/820/1024/1440px light/dark: switching both ways,
correct icon/accessible label/pressed state, refresh/navigation persistence,
44px mobile controls, Saved navigation, Menu/Escape, long Wednesday/date and
12:59 PM CDT clock labels, and no overflow. Representative phone screenshots were
inspected. An injected admin Requests notification fixture passed 320/390/430px
light/dark with no overlapping controls and a correctly sized menu sheet. Without
JavaScript, native Menu and Saved links still work; theme switching requires
JavaScript, as it did previously. Build passed with zero warnings/errors; changed
CSS formatting and whitespace checks passed. Evidence:
`test-results/browser/mobile-theme/` and `/tmp/mobile-theme-*.log`.
Physical-device and Safari checks were unavailable. Deployed public image
`mynewspaper-web:mobile-theme-20261030` (`ba56cac5113b`); runtime settings and
persistent keys were preserved. Final HTTPS checks passed at 320/390/430/820/1024/1440px in light/dark.
Every toggle saved successfully to the temporary profile; icons, labels and
pressed state matched. Preferences survived refresh and navigation, and toggling
still worked after enhanced navigation. Long clock labels, Saved/Menu/Escape,
44px mobile controls and no overflow passed. The injected Requests fixture also
passed 320/390/430px light/dark, including menu geometry. Native no-JavaScript
Menu/Saved navigation worked. Representative live screenshots were inspected.
Both HTTPS health endpoints returned 200, the public container is healthy and
startup league-phase checks ran without application errors. The temporary profile
and workspace hostname mapping were removed after verification.


### Header color-scheme modal — October 30, 2026

Added the palette shortcut beside light/dark mode on both mobile and desktop.
It opens the shared 16-scheme catalog in a native dialog with immediate previews,
automatic saving and Settings synchronization. Light/dark mode is retained.
Keyboard focus stays inside; Escape, backdrop, Close and Done restore the shortcut.
Save feedback stays visible while choices scroll, including a retry on failure.
Without JavaScript, the shortcut leads to Settings. Four mobile actions retain
44px targets; narrow phones put the clock on a separate row, including admin
notification layouts.

Preview checks passed 320/390/430/820/1024/1280/1440px light/dark, all 16 schemes,
focus/dismissal, viewport fit, no overflow, persistence, Settings synchronization,
local save failure/retry and no-JavaScript fallback. An admin Requests fixture
also passed 320/390/430px light/dark. Representative screenshots were inspected.
All 139 profile-related .NET tests, public build, targeted lint/format and
whitespace checks passed.

Deployed `mynewspaper-web:scheme-picker-20261030` (`48accb3ec847`); runtime
configuration and keys were preserved. Final HTTPS checks passed all seven widths in light/dark on a disposable profile.
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
See release history for rollback. Physical devices and Safari were unavailable.

### Top story divider spacing — October 30, 2026

Local change: the desktop home lead story has 16px top padding so its photo or
placeholder clears the Top story divider. Chromium preview at localhost:5305
passed overflow checks at 320, 390, 430, 820, 1024 and 1440px, light/dark, with
JavaScript on/off. Desktop geometry confirmed the 16px gap. Screenshots inspected
at 390 and 1440px; artifacts in test-results/browser/lead-spacing. CSS formatting
passed. Physical devices/Safari were unavailable. Deployed as
`mynewspaper-web:lead-spacing-20261030`; live Chromium checks passed all six widths
in both themes, including 16px desktop geometry and screenshot review. The temporary
verification profile was deleted. Public/admin containers healthy, public health HTTP 200.

### Mobile feature card width — October 30, 2026

Fixed the narrow text/publisher column shown in the Big 12 news screenshot.
At widths below 1024px, compact feature photos share a row with the category;
headline and summary span the card below. Feature metadata wraps publisher,
date and action controls into readable rows. The single-column body also keeps
wide-photo and no-JavaScript placeholder cards readable. Desktop styling is unchanged.

Preview Chromium checks on /sports/news/big12 used a controlled small square
image and the screenshot's headline, summary and publisher text. Passed at 320,
390, 430, 820 and 1440px in light/dark; checked full-width text/publisher, no
horizontal overflow, 44px save/share controls, and saving/unsaving. Repeated with
a large landscape image and with JavaScript disabled (P8 fallback). Inspected
compact, landscape and no-JavaScript screenshots; artifacts are in
test-results/browser/feature-card. CSS formatting and diff checks passed.
Physical devices/Safari unavailable. Deployed as
`mynewspaper-web:feature-card-20261030`. Live Chromium repeated all five widths,
both themes, JavaScript on/off, actual Big 12 story content and controlled compact
thumbnail checks. Full-width text/publisher, 44px controls and saving/unsaving
passed; live phone and desktop screenshots inspected. Temporary profile deleted.
Public/admin healthy, public HTTPS health HTTP 200. Admin was not replaced.

### Latest followed YouTube video replaces Opinion — October 30, 2026

Desktop's side-column Opinion card and the phone Opinion fold are replaced by
Latest YouTube video. The newest published video across the current profile's
followed channels/categories is selected with the existing follow-filtered video
query (publish time descending, row ID descending for ties), independently of the
rotating video shelf. It includes thumbnail, title, channel/category, publication
date/time, YouTube link, watch-later and share controls. Empty state links to
Settings' channels; read failures show a retry message. Refresh reads the latest
video again. The old `opinion` preference key is retained for this slot so existing
visibility and order choices survive; its Settings label is updated. Opinion news
remains available through News. The old Opinion polling script is no longer loaded.

Preview Chromium passed 320, 390, 430, 820, 1024 and 1440px, light/dark, JS on/off,
phone-fold opening, consistent profile-scoped selection, empty state, links,
publication text, no horizontal overflow, 44px actions and save/unsave. Phone and
desktop screenshots inspected; browser artifacts: test-results/browser/latest-video/preview.
The temporary browser profile was deleted. SQL transaction tests cover newest
across categories, newer unfollowed exclusion, tied timestamps, unfollow and nobody
open; targeted video tests passed 36/36. The broader video/ProfileData run was
52/53: existing Settings_patch_by_key_and_the_browser_import_happens_once assumes
an empty household base settings document, unlike the configured database. No
household settings were changed to satisfy it. Solution build passed with zero
warnings/errors. Changed-JS lint and formatting passed; repository lint excluding
ignored test artifacts and the pre-existing no-unsafe-finally error in
no-photo.browser.cjs passed. Physical devices/Safari unavailable.

Deployed public image `mynewspaper-web:latest-video-20261030` (prefix
`fa00b619f79b`). Live Chromium repeated all six widths/themes with JS on/off,
empty state, follow changes, current-profile selection, phone fold, links,
publication text, 44px actions and save/unsave; passed. Production phone/desktop
screenshots inspected, plus a full phone viewport in the matching preview.
Temporary verification profile deleted. Both public/admin containers healthy,
public HTTPS health 200; no fail/critical startup entries observed. Runtime
configuration and keys preserved. Rollback tag
`mynewspaper-web:before-latest-video-20261030` and stopped container
`mynewsfeed-web-before-latest-video-20261030` retain previous image `d93da38f3063`.
Admin was not redeployed. Live browser artifacts: test-results/browser/latest-video/live.
