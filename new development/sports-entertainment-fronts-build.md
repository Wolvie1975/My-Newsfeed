# Build plan: Sports and Entertainment fronts

Approved November 6, 2026 (America/Chicago). Three changes, one patch each so they can ship separately:

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

## 3. Entertainment middle tier: concept C

From 1024px:

```
On screen  (now also lists Friday's theater releases)
┌─ Lead (640×320 max) ─┬─ Latest headlines (5) ─────────┬─ Next weekend ──┐
│                      │                                │ Tonight on TV   │
└──────────────────────┴────────────────────────────────┴─────────────────┘
═══ (double rule) ════════════════════════════════════════════════════════
┌─ General ─────── All → ───────────┬─ TV/Film ─────── All → ────────────┐
│ story 1 │ story 2                  │ story 1 │ story 2                  │
│ story 3 │ story 4                  │ story 3 │ story 4                  │
└───────────────────────────────────┴─────────────────────────────────────┘
```

### Markup (`EntertainmentFront.razor`, desktop half)

- [ ] **On screen picks up Friday's theater releases.**
  - On a Friday (`theaterFriday` set), append `theatrical` items not already in `onScreen` (match on title and date), with `Where = "In theaters"`. Cap the row at 8 items.
  - Then remove the `ef-theaters` section from the right column.
  - "All releases →" stays.
- [ ] **Latest headlines moves beside the lead.**
  - Today `stories.Skip(1)` renders under the lead in `ef-main`, and isn't visible on desktop. Make it its own column between the lead and the right column.
  - Show up to 5 items, then the "All entertainment headlines →" link.
- [ ] **The right column keeps Next weekend (`ef-box`) and Tonight on TV (`ef-tv`) only.**
- [ ] **The desks move into a new `<div class="ef-desk-row">` after `.ef-cols`.**
  - Each desk keeps its `nf-desk` section and heading color, and adds an "All {desk} →" link to that desk's route (`NewsSections.RouteFor`).
  - Each desk lists its 4 items in two columns.
  - One desk spans the full width; three or more wrap two per row.

### CSS (`sections.css`, beside the `.ef-cols` rules at about line 323)

- [ ] `@media (min-width: 1024px)`:
  - `.ef-cols`: `grid-template-columns: minmax(0, 640px) minmax(0, 1fr) 340px`, `column-gap: 32px`.
  - `.ef-desk-row`: `display: grid; grid-template-columns: repeat(2, minmax(0, 1fr)); column-gap: 48px; margin-top: 32px; padding-top: 16px; border-top: 3px double var(--text)`.
  - `.ef-desk-row .ef-headlines`: `display: grid; grid-template-columns: repeat(2, minmax(0, 1fr)); column-gap: 28px`.
  - `.ef-desk-row > :only-child`: `grid-column: 1 / -1`.
- [ ] Lead photo cap: the same rule as Sports, on `.ef-lead__image`. Keep the `data-image-layout="compact"` handling.
- [ ] Remove the now-unused `.ef-cols > .ef-desks` placement rules from 1024px (around lines 214 and 5678).
- [ ] Tablets (`tablet.css` around 582): `.ef-desk-row` stacks the desks full width under `.ef-cols`, each desk's items in two columns.
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
  - Entertainment, 1024px and up:
    - `.ef-desk-row` sits below `.ef-cols`.
    - There is no `ef-theaters` section.
    - On a Friday, theater releases appear in "On screen".
  - Tech front unchanged: its `spf-cols` geometry matches the current check.
- [ ] **Mobile verification per AGENTS.md:** 320, 390 and 430px in light and dark, with screenshots inspected. Record widths, themes and results in the handoff.
- [ ] **After deploy:** `node tools/live-check.cjs`, plus `/sports` and `/entertainment` at 390 and 1920 against live data, ideally once on a Friday.
- [ ] Add a release-history entry for each patch.
