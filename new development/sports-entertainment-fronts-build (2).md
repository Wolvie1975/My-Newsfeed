# Build plan: Sports and Entertainment fronts

Approved November 6, 2026 (America/Chicago). Three changes, one patch each so they can ship separately (the Upcoming games timeline rides with change 2):

1. **The briefing** on Sports and Entertainment, like News and Tech. Canvas: "Sports and Entertainment Briefing".
2. **Sports middle tier, concept B**: the standings and rankings tables move to their own full-width row, and the lead photo is capped at 640 × 320. Canvas: "Sports Middle Tier Concepts", row B (revised).
3. **Entertainment middle tier, concept C**: the desks get their own row, and Friday's theater releases fold into "On screen". Canvas: "Entertainment Middle Tier Concepts", row C.

Pages: `src/myNewspaper.Web/Components/SportsFront.razor` and `EntertainmentFront.razor`. Each holds a desktop/tablet half (`mx-desktop tab-desktop`) and a phone half.

> **Scope.**
> - Phones keep their current middle tier.
> - Tablets (744–1023px) render the same markup as desktop (`tab-desktop`), so moving the tables and desks into their own rows reaches tablets too. That's intended: on a tablet the new row stacks full width (each table or desk as one column), which is what tablets want anyway. The column templates below are from 1024px; the tablet rules only need the new rows to sit full width under `.spf-cols` and `.ef-cols`.
> - The briefing appears at every width: desktop row, tablet row, and the phone strip (`nf-brief--phone`), as on News and Tech.
> - Nothing is invented. Every tile and list uses data the page already loads, except the two additions in section 4.

---

## 1. The briefing (both pages)

### Shared tile

News and Tech each define the same private `Tile()` helper. Replace both with one component, so all four fronts share it:

```razor
@* Components/BriefTile.razor: one briefing tile, a link with a label, a big value and a short line. *@
<a class="nf-tile @Color" href="@Href"><span class="nf-tile__label">@Label</span><b class="nf-tile__value">@Value</b><span class="nf-tile__sub">@Sub</span></a>
@code {
    [Parameter, EditorRequired] public string Href { get; set; } = "";
    [Parameter, EditorRequired] public string Color { get; set; } = "";
    [Parameter, EditorRequired] public string Label { get; set; } = "";
    [Parameter, EditorRequired] public string Value { get; set; } = "";
    [Parameter] public string Sub { get; set; } = "";
}
```

- [ ] Add `BriefTile.razor` and switch `NewsFront` and `TechFront` to it. Their markup must come out identical, so the existing briefing checks still pass unchanged.
- [ ] Place the section exactly as the other fronts do: first inside `<div class="ef spf">`, before "Today's scores" or "On screen".

```razor
<section class="nf-brief" aria-labelledby="sp-brief-title">
    <h2 id="sp-brief-title" class="ef-label">The briefing · @Dates.TodayLabel(nowUtc)</h2>
    <div class="nf-brief__tiles">@Tiles</div>
</section>
```

- [ ] Phone half: `<section class="nf-brief nf-brief--phone" aria-label="The briefing"><div class="nf-brief__tiles">@Tiles</div></section>`, placed where News puts its own.
- [ ] Colors: use the existing `mx-c-*` classes (`PhoneColors.ForSport`, `PhoneColors.ForCategory`). Don't add hex values.
- [ ] A tile with nothing true to say is **left out**, not shown empty. That is News's rule too: its alert and election tiles only appear when they apply.

### Sports tiles (in this order)

| Tile | Value | Small line | Link | Data | Shown when |
|---|---|---|---|---|---|
| Live now | `6 live` | `28 games today` | `scoreboard` | `live.Count`, `board.Count` | `board.Count > 0`. With nothing live: value `28 games`, line `none live right now` |
| Next up | `9:00 PM` | `Utah Royals at San Diego Wave FC · Prime Video` | the item's URL, else `events` | `upcoming[0]`, using `PhoneText.Matchup` and `Tv` | `upcoming.Count > 0` |
| Top 25 in action | `2 games` | `#20 Alabama, #18 Iowa State` | `scoreboard` | `board` games with `HomeRank` or `AwayRank` between 1 and 25; names the ranked teams, at most 3, then "…" | at least 1 ranked game |
| F1 | `Brazilian GP` | `Sun, Nov 8 · 11:00 AM` | `f1` | `f1Race` (already loaded) | `f1Race is not null` |
| Sports news | `N stories` | `today` | `sports/news` | **new count**, see section 4 | always; `0 stories` reads "nothing new yet" |

### Entertainment tiles (in this order)

| Tile | Value | Small line | Link | Data | Shown when |
|---|---|---|---|---|---|
| Entertainment news | `N stories` | `today` | `entertainment/news` | **new count**, see section 4 | always |
| Next weekend | the #1 forecast title, or `Not out yet` | `$NM forecast`, or `predictions publish later` | `box-office` | `forecasts` (already loaded) | the forecast block is in use (not the results weekend) |
| Last weekend | the #1 title | its `BoxOfficeChartQuery.Compact(gross)` | `box-office` | `boxOffice[0]` | `showResults` and `boxOffice.Count > 0`. Takes Next weekend's place |
| Tonight on TV | `N episodes` | the first series name, or `Track a series →` | `tv` | `episodes` (already loaded; follows the profile's series) | always |
| In theaters Friday | `N films` | the first two or three titles | `movies?type=theatrical` | `theatrical` | `theatrical.Count > 0` |

> **No "Out today" tile.** "On screen" sits directly under the briefing and already lists the day's releases.

---

## 2. Sports middle tier: concept B

Today the middle is four columns: Upcoming, lead plus headlines, standings, Top 25. From 1024px it becomes **two rows**:

```
┌─ Upcoming games ─┬─ Lead (640×320 max) ─┬─ Latest headlines (6) ─────────┐
│ Tonight · CT     │ photo                │ kicker / headline / source      │
│ 9:00 PM …        │ kicker               │ …                               │
│ Tomorrow · Sat   │ title                │                                 │
│ …                │ date                 │                                 │
└──────────────────┴──────────────────────┴─────────────────────────────────┘
═══ (double rule) ════════════════════════════════════════════════════════
┌─ Big 12 football standings ─ All → ─┬─ Men's basketball · AP Top 25 ─ → ─┐
│ teams 1–8        │ teams 9–16       │ 1–13            │ 14–25            │
└───────────────────────────────────────┴─────────────────────────────────────┘
```

### Markup (`SportsFront.razor`, desktop half)

- [ ] Split today's `.spf-cols` so the standings and rankings sections move into a new `<div class="spf-tables">` placed **after** `.spf-cols`.
  - The sections themselves (`spf-standings`, `spf-rankings`) keep their markup, headings and links.
  - Each table renders its rows into **two `<table>`s**, the first half and the second half, both with the header row. The second table's caption is visually hidden and reads "(continued)".
  - Don't use CSS columns on a single table: screen readers and the existing table checks expect real rows.
  - Halves: standings `ceil(n/2)` and the rest (8 + 8 for the Big 12); Top 25 is 13 + 12.
- [ ] **Upcoming games gets "Tomorrow".** When `upcoming.Count < 4`, render a second list under it:
  - Heading: "Tomorrow" with the day, for example `Sat, Nov 7`.
  - Content: up to `8 - upcoming.Count` games from tomorrow (section 4), in the same `spf-agate` row markup.
  - When there are no games tomorrow, show nothing.
- [ ] **Latest headlines becomes its own column** beside the lead. It's the same `ef-headlines spf-headlines` list, moved out from under the lead. It shows `stories.Skip(1)`, which today is 6 items (`GetHomeNewsAsync(db, 7, …)`).

### Upcoming games: timeline (concept C, November 7, 2026)

Canvas: "Upcoming Games Column Concepts", artboard C. This replaces the three-part rows (time | matchup | TV), which wrapped team names onto three to five lines and broke long names ("Wolverham…").

```
 6:30 AM ●  Wolverhampton Wanderers at Wrexham
         │  Soccer · Paramount+
11:00 AM ●  Iowa State at Baylor
         │  Football · FS1
         ●  Angel City FC at Kansas City Current
         │  Soccer · CBS
12:00 PM ●  St. Louis Blues at Nashville Predators
         │  Hockey · NHL Net +3
```

- [ ] **New list, Sports front only.** `spf-agate` is shared with the News and Tech fronts, so leave it alone and give Sports its own `<ol class="spf-timeline">`, keeping the `spf-` prefix (already registered). The Tomorrow list uses the same markup.
- [ ] **Markup per game:**
  ```razor
  <li class="spf-timeline__item @PhoneColors.ForSport(item.Kind)">
      <span class="spf-timeline__time">@(sameAsPrevious ? null : time)</span>
      <span class="spf-timeline__body">
          <a href="…"><TonightMatchup Item="item" /></a>
          <span class="spf-timeline__meta"><span class="spf-timeline__sport">@item.Kind</span>@if (item.Tv is not null) { <span aria-hidden="true"> · </span><b title="@item.Tv">@ShortTv(item.Tv)</b> }</span>
      </span>
  </li>
  ```
  - The time shows only on the first game at that time. Later games at the same time leave the time cell empty, but keep a visually hidden copy (`<span class="sr-only">11:00 AM</span>`) so each item still reads with its time.
  - The sport stays as visible text. The color is extra, never the only signal.
- [ ] **`ShortTv`:** split on `", "`. With one network, show it as is. With more, show `"{first} +{n}"`, for example "NHL Net +3". Keep the full list in `title`, and add a visually hidden " (also …)" with the rest, so screen readers get every network. "TNT and HBO Max" is one entry and stays whole. Put it in a small static helper and unit-test it.
- [ ] **CSS (from 744px; phones keep their own layout):**
  - `.spf-timeline`: no list style, `margin: 12px 0 0`, `padding: 0`.
  - `.spf-timeline__item`: `display: grid; grid-template-columns: 60px minmax(0, 1fr)`.
  - `.spf-timeline__time`: 12px, weight 800, `--text-2`, right-aligned, `padding: 3px 10px 0 0`, `white-space: nowrap`.
  - `.spf-timeline__body`:
    - `position: relative`, `display: flex; flex-direction: column; gap: 3px`, `padding: 0 0 16px 14px`, `border-left: 2px solid var(--row-border)`.
    - Its `::before` is the 10px marker: `position: absolute; left: -6px; top: 4px; border-radius: 50%; background: var(--dot)`.
  - The matchup link: 14px, weight 700, `--text`, `line-height: 1.3`, so it wraps whole words only. The "at" or "vs" uses the existing `TonightMatchup` muted style.
  - `.spf-timeline__sport`: `color: var(--dot)`, weight 800, 12px. **Check contrast.** Some `--mx-*` colors were picked for phone tiles. If any fails 4.5:1 against the page background in either theme, color only the marker and leave the label in `--text-2`.
  - `.spf-timeline__meta b`: 12px, `--text`.
  - Make sure the link reaches 44px of height per game. The two-line body usually does; otherwise extend it with padding.

### CSS (`sections.css`, beside the existing `.spf-cols` rules)

- [ ] `@media (min-width: 1024px)`:
  - `.spf-cols`: `grid-template-columns: 250px minmax(0, 640px) minmax(0, 1fr)`, `column-gap: 28px`.
  - `.spf-tables`: full-width grid `repeat(2, minmax(0, 1fr))`, `column-gap: 48px`, `margin-top: 32px`, `padding-top: 16px`, `border-top: 3px double var(--text)`.
  - Each section's pair of tables: a 2-column grid with `column-gap: 24px`.
- [ ] **Lead photo cap:**
  - `.spf-main .ef-lead__image { max-width: 640px; aspect-ratio: 2 / 1; }`, with `object-fit: cover` on the image.
  - The cap is a maximum, so narrow desktops shrink the photo and wide ones never grow it.
  - Article images are normally 1,200px or wider, so this stays sharp.
- [ ] Remove the rules that set the old four-column `.spf-cols` template from 1024px, at `sections.css` around lines 2835, 5540 and 5555.
- [ ] **News and Tech also use `spf-cols`.** Tech's front is `ef spf nf tc-front` (`.tc-front > .spf-cols` at about line 5615), and News's front is checked as `.nf .spf-cols` in `ipad-news-home`.
  - Scope every new rule to the Sports front only. Add a class to its wrapper, `<div class="ef spf sp-front">`, and select `.sp-front > .spf-cols` and `.sp-front > .spf-tables`.
  - Register the `sp-` prefix in `tools/check-css.cjs` and the coding-standards prefix table, or reuse `spf-` with a modifier (`spf--front`) if that fits the table's rules better. **News and Tech must not change.**
- [ ] Tablets (`tablet.css`): `.tab-desktop .sp-front > .spf-tables` stacks the two tables full width under the main row, each table still split into two halves when the width allows (≥ 900px), otherwise one.
- [ ] Run `node tools/check-css.cjs`. It should pass with no new breakpoints or prefixes, since `spf-` is registered.

---

## 3. Entertainment middle tier: concept C, with On screen down the left

From 1024px:

```
┌─ On screen ──┬─ Lead (640×320 max) ─┬─ Latest headlines (5) ──────┬─ Next weekend ─┐
│ timeline,    │                      │                             │ (box office    │
│ up to 6      │                      │                             │  predictions)  │
└──────────────┴──────────────────────┴─────────────────────────────┴────────────────┘
═══ (double rule) ═══════════════════════════════════════════════════════════════════
┌─ General ─────── All → ───────────┬─ TV/Film ─────── All → ─────────────────────────┐
│ story 1 │ story 2                  │ story 1 │ story 2                               │
│ story 3 │ story 4                  │ story 3 │ story 4                               │
└───────────────────────────────────┴──────────────────────────────────────────────────┘
```

Canvas: "Entertainment Middle Tier Concepts", artboard "On screen as a vertical timeline, left of the lead" (chosen November 7, 2026). It replaces the across-the-row On screen band; the artboard "On screen as a timeline" is the rejected horizontal version.

### Markup (`EntertainmentFront.razor`, desktop half)

- [ ] **On screen moves into `.ef-cols` as its first column**, left of the lead. It is no longer a full-width band above the columns.
- [ ] **On screen picks up Friday's theater releases.**
  - On a Friday (`theaterFriday` set), append `theatrical` items not already in `onScreen` (match on title and date, using the duplicate merge below), with `Kind = "In theaters"`.
  - Then remove the `ef-theaters` section from the right column.
  - "All releases →" stays, in the On screen heading row.
- [ ] **Latest headlines moves beside the lead.**
  - Today `stories.Skip(1)` renders under the lead in `ef-main`, and isn't visible on desktop. Make it its own column between the lead and the right column.
  - Show up to 5 items, then the "All entertainment headlines →" link.
- [ ] **The right column keeps Next weekend (`ef-box`) only.**
  - Remove Tonight on TV (`ef-tv`) from the desktop half: tonight's episodes are now the first rows of the On screen timeline, and the briefing has a Tonight on TV tile.
  - The phone half keeps whatever it shows today.
- [ ] **The desks move into a new `<div class="ef-desk-row">` after `.ef-cols`.**
  - Each desk keeps its `nf-desk` section and heading color, and adds an "All {desk} →" link to that desk's route (`NewsSections.RouteFor`).
  - Each desk lists its 4 items in two columns.
  - One desk spans the full width; three or more wrap two per row.

### On screen: a vertical timeline (November 7, 2026)

Same look as the Upcoming games timeline on Sports (`spf-timeline`): a date column, a line with a dot per release, then the release.

```
On screen                 All releases →
Tonight and the week ahead · CT
───────────────────────────────────────
   Tonight │● [poster] Saturday Night Live
  10:29 PM │          TV · NBC · S52 E05
           │
       Sat │● [poster] Gail Daughtry and the…
     Nov 7 │          Netflix · Streaming
           │
       Mon │● [poster] Misty Green (2026)
     Nov 9 │          Netflix · Streaming
           │
       Tue │● [poster] Crawlers (2026)
    Nov 10 │          Digital · Buy or rent
```

- [ ] **Data (`EntertainmentFront.razor`, where `onScreen` is built at about line 433).** Give `ScreenItem` three more fields:
  - `DateOnly? Day`: today for tonight's episodes, the release date for movies.
  - `string Kind`: "TV" for episodes, or `MoviesQuery.Types[type]` ("In theaters", "Digital"…), or the platform when it's a streamer such as Netflix.
  - `string Color`: `PhoneColors.ForRelease(type)`, or `ForRelease("tv")` for episodes.
- [ ] **Merge duplicates before taking items** (today's page shows "Crawlers" and "Crawlers (2026)" for Tue, Nov 10).
  - The key is the title with any trailing ` (YYYY)` removed, compared case-insensitively, plus the day.
  - Keep the copy that has a poster; if both or neither do, keep the one whose title includes the year.
  - Put this in a small static helper with unit tests: the year/no-year pair, two different films on the same day, and the same title on different days, which stays as two items.
- [ ] **Order and cap:** sort by day (tonight first, then by date, "Date to be announced" last), then by time within the day. Show at most **6 items**; Friday's theater releases count toward the 6. Everything else is behind "All releases →".
- [ ] **Date shown once per day.** The first item of a day shows the date in the left column ("Tonight" over "10:29 PM", or "Sat" over "Nov 7"). Later items on the same day leave the visible date empty but keep it in a visually hidden span, so a screen reader hears the date on every item. This is the same rule as the shared game time in `spf-timeline`.
- [ ] **Markup:**
  ```razor
  <section class="ef-onscreen-col" aria-labelledby="ef-onscreen-h">
      <div class="ef-onscreen__head">
          <h2 id="ef-onscreen-h">On screen</h2>
          <a href="/entertainment/releases">All releases →</a>   @* keep today's href *@
      </div>
      <span class="ef-onscreen__sub">Tonight and the week ahead · CT</span>
      <ol class="ef-onscreen">
          @foreach (var item in shown)
          {
              <li class="ef-onscreen__item @item.Color">
                  <span class="ef-onscreen__date @(item.Tonight ? "ef-onscreen__date--tonight" : null)"
                        aria-hidden="@(item.FirstOfDay ? null : "true")">
                      @(item.FirstOfDay ? item.DateLabel : null)   @* "Tonight\n10:29 PM" or "Sat\nNov 7" *@
                  </span>
                  <span class="ef-onscreen__body">
                      @if (!item.FirstOfDay) { <span class="visually-hidden">@item.DateLabel: </span> }
                      <a class="ef-screen__item" href="@item.Href">
                          <MoviePoster Url="@item.Poster" Title="@item.Title" Class="ef-poster" />
                          <span class="ef-screen__text"><b class="ef-screen__title">@item.Title</b>
                              <span class="ef-onscreen__meta"><span class="ef-onscreen__kind">@item.Kind</span> · @item.Where</span></span>
                      </a>
                  </span>
              </li>
          }
      </ol>
  </section>
  ```
  The existing `ef-screen__item`, `ef-poster` and `ef-screen__title` classes and the "All releases →" link stay, so their checks keep working. Use whichever visually-hidden class the repo already has; don't add a new one.
- [ ] **CSS (`sections.css`, from 1024px; mirror the `.spf-timeline` rules):**
  - `.ef-onscreen-col`: flex column. `.ef-onscreen__head`: flex, space-between, baseline; h2 in the section serif at 26px. `.ef-onscreen__sub`: 12px `--text-2`, `padding: 4px 0 12px`, `border-bottom: 2px solid var(--text)`.
  - `.ef-onscreen`: `list-style: none; margin: 0; padding: 12px 0 0`.
  - `.ef-onscreen__item`: `display: grid; grid-template-columns: 64px minmax(0, 1fr)`.
  - `.ef-onscreen__date`: 12px, weight 800, line-height 1.3, `white-space: pre-line`, right-aligned, `padding: 3px 10px 0 0`, `color: var(--text-2)`; `--tonight` uses the existing tonight color (`.ef-when--tonight`).
  - `.ef-onscreen__body`: `position: relative; padding: 0 0 18px 14px; border-left: 2px solid var(--row-border)`. Its `::before` is the dot: 10px circle at `left: -6px; top: 4px`, `background: var(--dot)`. The last item drops its bottom padding.
  - `.ef-onscreen__body .ef-screen__item`: `display: grid; grid-template-columns: 34px minmax(0, 1fr); gap: 10px; align-items: start`. `.ef-poster` inside it is 34 × 50, radius 3px.
  - `.ef-screen__title`: 14px, line-height 1.3, wraps (no ellipsis). `.ef-onscreen__meta`: 12px `--text-2`.
  - `.ef-onscreen__kind`: `color: var(--dot)`, weight 800. Use the same contrast fallback as the Sports timeline: if a release color is under 4.5:1 as text, color only the dot and leave the label in `--text`.
  - Register the `ef-onscreen` prefix in `tools/check-css.cjs` if `ef-` isn't already covered.
  - Remove the old full-width On screen band rules once nothing uses them (grep `ef-screen` placement rules above `.ef-cols`).
- [ ] **Phones:** the phone half already draws its own `screenLine` timeline (`TimelineItem`). Apply the same duplicate merge to `screenLine` so the phone doesn't show the doubled title either. No layout change.

### CSS (`sections.css`, beside the `.ef-cols` rules at about line 323)

- [ ] `@media (min-width: 1024px)`:
  - `.ef-cols`: `grid-template-columns: 270px minmax(0, 640px) minmax(0, 1fr) 300px`, `column-gap: 32px`. Order: On screen, lead, Latest headlines, Next weekend.
  - Between 1024 and 1279px the four columns are tight (the lead shrinks to about 330px). At that range, use `grid-template-columns: 250px minmax(0, 1fr) 280px` and drop Latest headlines under the lead (`grid-column: 2`, second row), so the lead keeps a usable width. Check this at 1024 and 1280.
  - `.ef-desk-row`: `display: grid; grid-template-columns: repeat(2, minmax(0, 1fr)); column-gap: 48px; margin-top: 32px; padding-top: 16px; border-top: 3px double var(--text)`.
  - `.ef-desk-row .ef-headlines`: `display: grid; grid-template-columns: repeat(2, minmax(0, 1fr)); column-gap: 28px`.
  - `.ef-desk-row > :only-child`: `grid-column: 1 / -1`.
- [ ] Lead photo cap: the same rule as Sports, on `.ef-lead__image`. Keep the `data-image-layout="compact"` handling.
- [ ] Remove the now-unused `.ef-cols > .ef-desks` placement rules from 1024px (around lines 214 and 5678).
- [ ] Tablets (`tablet.css` around 582, 744–1023px): two columns. On screen (same vertical timeline) on the left at 250px, the lead and Latest headlines stacked on the right, Next weekend under them. `.ef-desk-row` stacks the desks full width under `.ef-cols`, each desk's items in two columns. Update `ipad-*` checks if they measure the old On screen band.
- [ ] Phones: no change. The phone half already folds theaters into its own "In theaters" fold.

---

## 4. Data additions (two, both small)

- [ ] **Story count for a hub today.** Generalize the count inside `TechQuery.BriefingAsync` into a shared query, then use it in all three places:
  ```csharp
  // FeedQuery: visible stories in a hub published (or scraped) today in the display time zone.
  public static Task<int> CountTodayInHubAsync(WebScraperContext db, FeedDates dates, string hub, CancellationToken ct = default)
  ```
  - `TechQuery.BriefingAsync` keeps its per-category split, so only the date window and visibility filter are shared.
  - Unit tests: a story at 11:59 PM yesterday is excluded and one at 12:00 AM today counts, both in America/Chicago; hidden sources are excluded.
- [ ] **Tomorrow's games (Sports).**
  - Use the existing `EventsQuery.GetOnDateAsync(db, today.AddDays(1), Ct)` through the same `HomeEventResults` and `TodayTonight.Build` path that `upcoming` uses.
  - Load it only when `upcoming.Count < 4`, so the usual case adds no query.
  - On a read failure, log it and show nothing, the same pattern as the other sections.

---

## 5. Checks

- [ ] `tools/verify.sh`: build with 0 warnings, all .NET and JS tests, lint, format and `check-css`.
- [ ] **Existing browser checks to update:**
  - `ipad-sports.browser.cjs` expects the tables inside the columns (around lines 36, 77 and 134); move those assertions to `.spf-tables`.
  - `sports-panels.browser.cjs` reads the tables' "more" links; these keep working if the `spf-standings` and `spf-rankings` sections keep their classes.
  - `ipad-news-home.browser.cjs` measures `.nf .spf-cols`; it must pass unchanged.
  - Run them all against `WebScraper_Test`.
- [ ] **New browser check `tests/browser/section-fronts.browser.cjs`**, using `lib.widths([390, 768, 1024, 1440, 1920, 2560])` and both `lib.THEMES`:
  - Briefing present on `/sports` and `/entertainment` at every width, before the first section, with no empty tiles and every tile a link.
  - Phones: briefing tiles are at least 44px tall and nothing scrolls sideways.
  - Sports, 1024px and up:
    - `.spf-tables` sits below `.spf-cols` at full width.
    - Each table renders as two side-by-side tables.
    - The lead image is no larger than 640 × 320.
  - Sports with fewer than 4 games left (mock the events read or use a late-evening test day): "Tomorrow" appears.
  - Upcoming games timeline:
    - Each game is one `.spf-timeline__item` whose matchup wraps only between words. Check that no team name is cut with "…", for example with a long name such as "Wolverhampton Wanderers".
    - A game sharing the previous game's time shows no visible time, but its text still includes it.
    - A multi-network game shows "first +N", with the full list in `title`.
    - Every sport label passes 4.5:1 contrast in both themes.
    - The News and Tech fronts' `spf-agate` lists are unchanged.
  - Entertainment, 1024px and up:
    - `.ef-desk-row` sits below `.ef-cols`.
    - There is no `ef-theaters` section.
    - On a Friday, theater releases appear in "On screen".
  - On screen (vertical timeline):
    - From 1024px, `.ef-onscreen-col` is the first column of `.ef-cols` and sits left of the lead (its right edge is left of the lead's left edge, tops aligned within 4px).
    - At most 6 `.ef-onscreen__item`s, sorted by day with tonight first.
    - No duplicate titles once a trailing year is ignored (seed "Crawlers" and "Crawlers (2026)" on the same day; one shows).
    - The visible date appears only on the first item of each day; every item's accessible text still includes its date.
    - Titles wrap; none is cut with "…".
    - Every kind label passes 4.5:1 contrast in both themes (or falls back to `--text`).
    - The right column has no `ef-tv` and no `ef-theaters` on desktop.
    - 1024px: the lead image is at least 300px wide; no sideways scroll at 768, 1024 or 1280.
  - Tech front unchanged: its `spf-cols` geometry matches the current check.
- [ ] **Mobile verification per AGENTS.md:** 320, 390 and 430px in light and dark, with screenshots inspected. Record widths, themes and results in the handoff.
- [ ] **After deploy:** `node tools/live-check.cjs`, plus `/sports` and `/entertainment` at 390 and 1920 against live data, ideally once on a Friday.
- [ ] Add a release-history entry for each patch.
