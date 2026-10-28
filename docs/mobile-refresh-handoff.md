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
| **Ribbon** | Yellow band: a live line (red dot + small caps), a 44px serif title, one labelled `<select>` | Every page |
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
- [ ] 3. **Top 25**, **Standings**, **College Sports**, **F1**.
- [ ] 4. **Entertainment**, **Movies**, **TV & Streaming**, **Digital releases**, **Box office**.
- [ ] 5. **Where to watch**, **Local**, **YouTube**.
- [ ] 6. Ribbon-only pages: both All headlines pages, **Saved**, **Settings**.

## Open questions

- **"Most read"** on Home and Headlines needs read counts the site doesn't collect. Use feed order and call it "Top
  stories" until then, or add a lightweight `MyDailyFeedArticleReads` table later.
- **Game of the week** (College) and **"Your teams"** (Standings, College) depend on the profile's My Teams; for Guest,
  fall back to the first game / hide the tiles.
- **Ribbon color per scheme:** yellow is the default for every scheme. If a scheme looks wrong (e.g. one already built
  around yellow), override `--mx-ribbon-bg` under `:root[data-color-scheme='…']` in `mobile.css`.
