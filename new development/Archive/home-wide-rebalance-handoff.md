# Home on wide desktops: rebalanced into bands (no VOD, headlines together, Top 25 as a grid, Box office under Today & Tonight)

Written November 1, 2026 (America/Chicago), against `develop` at `2addbec`. Every file, class and line number below
was checked against that commit.

**Save as:** `docs/home-wide-rebalance-handoff.md`

**Mockup:** the myNewspaper canvas, **4K screens** page, "Home at 2000 CSS px, rebalanced (Nov 1)"
(https://claude.ai/artifact/JVQ8mU7R63Z1cziNpeW9Z5). The board's "Box office shows" property switches it between
forecast and results.

## What changes

At 1920 px and up, the home page currently becomes one sheet of columns (`docs/wide-screens-4k-handoff.md`): four
columns at 1920, six at 2560. Each column stacks different sections, so they end at very different heights, and
the headline lists are spread across three of them. This replaces that with **four bands**, each with columns that
end together:

| Band | Columns | Sections |
|---|---|---|
| 1 · The day at a glance | 300px · flexible · 420px | **Today & Tonight (5 games) + Box office** · Top story + More headlines (4 across) · Latest YouTube video + Today's episodes |
| 2 · Headlines | three equal | Sports headlines · Entertainment headlines · Tech headlines |
| 3 · Sports | 5/12 · 5/12 · 2/12 | Scoreboard (6 games, 3 across) · **Top 25 as a 5 × 5 grid** · F1 race weekend / In theaters Friday |
| 4 · Videos | full width | Latest videos, **one row of six** |

Removed: **Upcoming Digital / VOD**, on desktop and phone (§4).

| Width | Effect |
|---|---|
| Phones, iPad portrait | **Unchanged** (they use `.mx-phone`), apart from the VOD line leaving the phone's "Coming to screens" fold |
| 1024–1919 | The same three-column layout as today, with sections in their new columns (see §1). Check it, but it isn't the target |
| 1920+ | The bands |

Work through the sections in order: 1 (markup), 2 (Box office), 3 (Today & Tonight), 4 (VOD), 5 (the wide CSS),
6 (verify).

---

## 1. Move sections between columns (`Components/Pages/Home.razor`)

The two wrappers stay (`.bs-front`, `.bs-more`), each with three `.bs-col`s. Only what sits in each column changes.
Every section keeps its `data-home-section` attribute, so the Settings show/hide toggles (`personal.js`
`renderHome`) keep working.

| Column | Today | After |
|---|---|---|
| `.bs-front > .bs-col--left` | Today & Tonight (with F1 folded inside on race weekends) | Today & Tonight · **Box office** |
| `.bs-front > .bs-col--main` | Top story · More headlines | unchanged |
| `.bs-front > .bs-col--side` | Latest video · Box office · Sports headlines | Latest video · **Today's episodes** |
| `.bs-more > .bs-col--left` | Scoreboard · Top 25 | unchanged |
| `.bs-more > .bs-col--main` | Entertainment headlines · Tech headlines · Latest videos | **Sports headlines** · Entertainment headlines · Tech headlines · Latest videos |
| `.bs-more > .bs-col--side` | Today's episodes · In theaters · Upcoming Digital / VOD | **F1** (race weekends) · In theaters (Fridays) |

### 1.1 Front, left column (lines ~91–139)

Take `F1Home` **out** of the Today & Tonight section (lines ~134–137; it moves to §1.4) and put the Box office block
(§2) after the section's closing `</section>`:

```razor
<div class="bs-col bs-col--left">
    <section data-home-section="sports" class="d-card d-today bs-tonight" aria-labelledby="h-tonight">
        @* … Today & Tonight, unchanged apart from §3 … *@
    </section>
    @BoxOffice
</div>
```

### 1.2 Front, side column (lines ~147–160)

```razor
<div class="bs-col bs-col--side">
    <HomeLatestVideo Item="@latestVideo" Failed="@latestVideoFailed" />
    @* Today's episodes: the whole <section data-home-section="tv" …> block, moved here unchanged from .bs-more's side column. *@
</div>
```

### 1.3 More, main column (lines ~174–236)

Sports headlines moves here, first:

```razor
<div class="bs-col bs-col--main">
    <HomeHeadlineShelf SectionKey="sports-news" Title="Sports headlines" HeadingId="h-sports-headlines" Href="sports/news"
                       CssClass="d-sports-headlines" Stories="@sportsStories" />
    <HomeHeadlineShelf SectionKey="entertainment-news" … />   @* unchanged *@
    <HomeHeadlineShelf SectionKey="tech-news" … />            @* unchanged *@
    <section data-home-section="videos" …>…</section>          @* unchanged *@
</div>
```

### 1.4 More, side column (lines ~238–380)

```razor
<div class="bs-col bs-col--side">
    @if (FoldF1)
    {
        <myNewspaper.Web.Components.F1.F1Home Data="homeF1" Error="@homeF1Error" NowUtc="nowUtc" />
    }
    @if (ShowTheatrical)
    {
        @* the <section data-home-section="movies" …> block, unchanged *@
    }
</div>
```

`F1Home` without `Embedded` renders its own `d-card d-f1` with `data-home-section="f1"`. The `[data-home-f1]`
hide rule in `personal.js` still applies.

### 1.5 Headline counts

The three desk columns in band 2 show five stories each, and the News shelf shows four across under the lead.
Line ~717:

```csharp
private const int HomeShelfCount = 5;
```

At 1024–1919 every shelf gets one more story.

---

## 2. Box office: forecast until results are published

### 2.1 Today

`HomeBoxOfficeSchedule.ShowResults` (`myNewspaper.Shared/Data/HomeBoxOfficeSchedule.cs`) is purely by day:
Sunday–Tuesday show results, other days the forecast. So all of Sunday morning (your screenshot) shows "Results for
this weekend have not been published yet" instead of the forecast that's still useful.

### 2.2 New rule

| Day | Results for the weekend just played exist? | Shows |
|---|---|---|
| Sun–Tue | Yes | **Results** |
| Sun–Tue | No (or the read failed) | **Forecast for that weekend** |
| Wed–Sat | n/a | Forecast for the coming weekend (unchanged) |

### 2.3 Code (`Components/Pages/Home.razor`)

Add two fields next to `homeBoxOffice` (line ~600):

```csharp
private bool showBoxOfficeResults;
private DateOnly boxOfficeWeekend;
```

Replace the box office load (lines ~644–658) with:

```csharp
homeForecasts = new([], []);
homeBoxOffice = []; homeBoxOfficeFailed = false; showBoxOfficeResults = false;
var boxToday = Dates.TodayAt(nowUtc);
boxOfficeWeekend = HomeBoxOfficeSchedule.ResultsWeekend(boxToday);   // the Friday of the weekend just played
var resultsDays = HomeBoxOfficeSchedule.ShowResults(boxToday);
if (resultsDays)
{
    try
    {
        homeBoxOffice = await BoxOfficeChartQuery.GetAsync(db, boxOfficeWeekend);
        showBoxOfficeResults = homeBoxOffice.Count > 0;
    }
    catch (Exception ex) when (DataFailures.IsReadFailure(ex))
    { Log.LogError(ex, "Could not read home weekend results."); homeBoxOfficeFailed = true; }
}
if (!showBoxOfficeResults)
{
    // No results yet: on Sun–Tue, the forecast for the weekend just played (its PeriodEnd is that Sunday,
    // and the query keeps PeriodEnd >= the date passed); otherwise the coming weekend's.
    var forecastDay = resultsDays ? boxOfficeWeekend.AddDays(2) : boxToday;
    try { homeForecasts = await BoxOfficeForecastQuery.GetAsync(db, forecastDay); }
    catch (Exception ex) when (DataFailures.IsReadFailure(ex))
    { Log.LogError(ex, "Could not read home weekend predictions."); }
}
```

Add the fragment that §1.1 renders (in `@code`):

```csharp
private RenderFragment BoxOffice => __builder =>
{
    if (showBoxOfficeResults)
    {
        <HomeBoxOfficeResults Rows="homeBoxOffice" Weekend="@boxOfficeWeekend" Failed="false" />
    }
    else
    {
        <BoxOfficeForecastSection HomeCard="true" Rows="@(homeForecasts.Movies.Where(m => m.Kind == BoxOfficeForecastQuery.Preview).ToList())"
            Source="@(homeForecasts.Sources.SingleOrDefault(s => s.Kind == BoxOfficeForecastQuery.Preview))" UtcNow="@nowUtc" />
    }
};
```

> **Check `PeriodEnd`.** The fallback assumes a forecast's `PeriodEnd` is the weekend's Sunday. If your forecast rows
> end on Monday or another day, change `AddDays(2)` to match. If they end before Sunday, Monday/Tuesday fall back
> to the next weekend's forecast, which is acceptable.

> **Phone:** the folded phone front uses `homeBoxOffice.Count > 0` (line ~510), so it now shows results only when
> they exist. Nothing else to change there.

### 2.4 Box office styles now that it's in the left column (`wwwroot/broadsheet.css`)

The compact box office styles (lines ~1063–1135) are scoped to `.bs-col--side`. Change each
`.pub .bs-home .bs-col--side .boxoffice-…` and `.pub .bs-home .bs-col--side .d-forecasts …` selector in that block
to `.pub .bs-home :is(.bs-col--side, .bs-col--left) …`. Leave the `.d-tv__show` and `.d-movies` rules alone.

Then show five films instead of three (line ~1065):

```css
.pub .bs-home :is(.bs-col--side, .bs-col--left) .boxoffice-movie:nth-child(n + 6),
```

---

## 3. Today & Tonight: five games, then "N more today"

The limit has to respect **"Only my teams"**, which `personal.js` applies in the browser by hiding rows. So the
server marks rows past the fifth, and `personal.js` re-marks them after its team filter.

### 3.1 Markup (`Components/Pages/Home.razor`, lines ~97–132)

Add a running count across the groups, mark rows past the limit, and add the link after the list:

```razor
@{ var tonightIndex = 0; }
<ul class="bs-agate">
    @foreach (var (heading, items) in TodayTonight.Groups(tonight, Dates))
    {
        <li class="bs-agate__group">
            <h3 class="bs-agate__heading">@heading</h3>
            <ul>
                @foreach (var item in items)
                {
                    <li class="bs-agate__row" data-home-team-item data-sports-teams="@SportsTeamPreferences.Keys(item.SchoolName, item.OpponentName)"
                        data-home-over="@(tonightIndex++ >= TonightLimit ? "" : null)">
                        @* … row contents unchanged … *@
                    </li>
                }
            </ul>
        </li>
    }
</ul>
@if (tonight.Count > TonightLimit)
{
    <a class="d-more bs-tonight__more" href="events" data-home-tonight-more>@(tonight.Count - TonightLimit) more today <span aria-hidden="true">→</span></a>
}
```

```csharp
private const int TonightLimit = 5;
```

Keep the existing inner markup of each group and row exactly as it is. Only the `data-home-over` attribute, the
counter and the link are new.

### 3.2 `wwwroot/personal.js` (`renderHome`, after the `rows.forEach(…)` team filter, line ~122)

```js
            // Today & Tonight: the first five rows still shown, then "N more today" (docs/home-wide-rebalance-handoff.md §3).
            const TONIGHT_LIMIT = 5;
            let shown = 0;
            grid.querySelectorAll('.bs-tonight [data-home-team-item]').forEach((row) => {
                row.toggleAttribute('data-home-over', !row.hidden && ++shown > TONIGHT_LIMIT);
            });
            const more = grid.querySelector('[data-home-tonight-more]');
            if (more) {
                const extra = Math.max(0, shown - TONIGHT_LIMIT);
                more.hidden = extra === 0;
                more.firstChild.textContent = `${extra} more today `;
            }
```

### 3.3 Styles (`wwwroot/broadsheet.css`, after the other `.bs-agate` rules)

```css
/* Today & Tonight: rows past the fifth, and any heading left with nothing to show. */
.pub .bs-tonight [data-home-over] {
    display: none;
}
.pub .bs-tonight .bs-agate__group:not(:has(.bs-agate__row:not([hidden], [data-home-over]))) {
    display: none;
}
.pub .bs-tonight__more {
    display: inline-flex;
    align-items: center;
    min-height: 40px;
    margin-top: 4px;
}
```

This applies at every desktop width, not only 1920+. The full list is one click away on All games.

---

## 4. Remove Upcoming Digital / VOD (desktop **and** phone)

The home page shows digital releases in two places, and both go, along with the data load and the Settings toggle.
The Movies page (`/movies`, "Digital rental / purchase") and `/movies/digital` are untouched.

### 4.1 Desktop (`Components/Pages/Home.razor`)

Delete the whole `<section data-home-section="digital" class="d-card d-movies d-digital" …>…</section>` block
(lines ~326–380). §1.4 already left it out of the new side column.

### 4.2 Phone (`Components/Pages/Home.razor`, the "Coming to screens" fold, line ~545)

The fold lists tonight's episodes, Friday's theatrical release, then the first digital release. Delete the digital
line, so the list ends with the theatrical `.Concat(…)`:

```razor
@{
    var screens = episodes.Take(2).Select(e => (Title: e.SeriesTitle, Sub: string.Join(" · ", new[] { "Tonight", e.StartsUtc is { } t ? Dates.Time(t) : null, e.Platform }.Where(p => !string.IsNullOrEmpty(p))), Href: "tv"))
        .Concat((ShowTheatrical ? movies : []).Take(1).Select(m => (Title: m.Title, Sub: "Friday · In theaters", Href: "movies?type=theatrical")))
        .ToList();
}
```

The fold's title ("Coming to screens") and its "TV and releases" link still fit.

### 4.3 The data load (`Components/Pages/Home.razor`)

Nothing renders digital releases now, so stop loading them. Delete:

| Line (approx.) | Code |
|---|---|
| 604 | `private IReadOnlyList<MovieReleaseItem> digitalMovies = [];` |
| 605 | `private DigitalReleasePage? digitalData;` |
| 663–664 | `digitalData = await DigitalReleasesQuery.GetAsync(…);` and `digitalMovies = digitalData.Items;` |

### 4.4 Settings and saved profiles

| File | Change |
|---|---|
| `myNewspaper.Shared/Profiles/SiteDefaults.cs` line ~50 | Remove `"digital"` from `HomeOrder`. `HideableHomeSections` follows automatically |
| `myNewspaper.Shared/Profiles/SiteDefaults.cs` line ~73 | Remove `["digital"] = "Upcoming Digital / VOD",` from `HomeSectionNames` |
| `wwwroot/personal.js` line ~22 | Remove `'digital',` from the fallback `defaults` list. `ProfileSettingsTests` (line ~138) checks this list matches `HomeOrder` exactly, so it fails until both match |
| `wwwroot/personal.js` line ~115 | Simplify to `section.hidden = p.hidden.includes(key);` (the `digital`/`movies` special case has nothing left to hide) |

Saved profiles and base editions that still list `"digital"` are fine: `ProfileSettings` (lines ~96 and ~165–168)
already drops keys that aren't in `HomeOrder` / `HideableHomeSections` when it reads them. Settings, New edition
(`NewEdition.razor`) and the admin base edition (`BaseEdition.razor`) all build their lists from `SiteDefaults`, so
the toggle disappears from all three at once.

### 4.5 Verify

| Check | Expected |
|---|---|
| Desktop home, any width | No Upcoming Digital / VOD card |
| Phone home, "Coming to screens" fold | Episodes and (on Fridays) the theatrical release; no digital line |
| Settings, New edition, admin base edition | No "Upcoming Digital / VOD" toggle |
| A profile saved with `digital` hidden | Loads without error; the key is dropped on next save |
| `dotnet test` | `ProfileSettingsTests` passes (the two lists match) |
| `/movies`, `/movies/digital` | Unchanged |

---

## 5. The wide layout (`wwwroot/broadsheet.css`)

### 5.1 Delete the old sheet

| Lines (approx.) | Block | Action |
|---|---|---|
| 2009–2046 | `@media (min-width: 1920px)` "Home, 1920px and up: one sheet of columns" | **Delete** |
| 2047–2069 | `@media (min-width: 2560px)` sheet with six columns | **Delete** |
| 2077–2083 | `@media (min-width: 2560px)` "The fifth sheet column is a narrow desk" | **Delete** |

Keep the 1920 masthead rules after them (`.bs-masthead`, `.bs-title__…`, `.bs-glance`), unchanged.

### 5.2 Add the bands

Put this where the deleted 1920 block was:

```css
/* ---------- Home, 1920px and up: four bands whose columns end together (docs/home-wide-rebalance-handoff.md) ---------- */
@media (min-width: 1920px) {
    .pub .bs-sheet {
        display: block;
        padding-top: 20px;
    }
    .pub .bs-sheet .bs-band {
        display: none;               /* each band's sections carry their own headings */
    }

    /* Band 1: Today & Tonight + Box office · Top story + More headlines · Latest video + Today's episodes */
    .pub .bs-sheet .bs-front {
        grid-template-columns: 300px minmax(0, 1fr) 420px;
        padding: 6px 0 30px;
        border-bottom: 2px solid var(--text);
    }
    .pub .bs-sheet .bs-front > .bs-col--main {
        padding: 0 32px;
    }

    /* The lead: photo beside the headline, not above it, so the band isn't one tall column. */
    .pub .bs-sheet .d-lead__story {
        display: grid;
        grid-template-columns: minmax(0, 1.15fr) minmax(0, 1fr);
        gap: 28px;
        align-items: start;
    }
    .pub .bs-sheet .d-lead__image {
        aspect-ratio: 4 / 3;
    }
    .pub .bs-sheet .d-lead__title {
        font-size: 40px;
        line-height: 1.08;
    }

    /* More headlines under the lead: four across, text only. */
    .pub .bs-home .bs-sheet .bs-front .d-news .d-headline-shelf__list {
        grid-template-columns: repeat(4, minmax(0, 1fr));
    }
    .pub .bs-home .bs-sheet .bs-front .d-news .d-headline-shelf__list > li:nth-child(n + 5) {
        display: none;
    }

    /* Bands 2–4 share one 12-column grid. The left and main columns dissolve so each section is a grid item. */
    .pub .bs-sheet .bs-more {
        display: grid;
        grid-template-columns: repeat(12, minmax(0, 1fr));
        padding-top: 0;
    }
    .pub .bs-sheet .bs-more > :is(.bs-col--left, .bs-col--main) {
        display: contents;
    }
    .pub .bs-sheet .bs-more > .bs-col > .d-card,
    .pub .bs-sheet .bs-more > .bs-col--side {
        margin: 0;
        padding: 26px 26px 30px;
        border-left: 1px solid var(--row-border);
    }

    /* Band 2: the three desks. */
    .pub .bs-sheet .bs-more [data-home-section='sports-news'] { grid-column: 1 / span 4; grid-row: 1; }
    .pub .bs-sheet .bs-more [data-home-section='entertainment-news'] { grid-column: 5 / span 4; grid-row: 1; }
    .pub .bs-sheet .bs-more [data-home-section='tech-news'] { grid-column: 9 / span 4; grid-row: 1; }

    /* Band 3: Scoreboard · Top 25 · F1 / In theaters. */
    .pub .bs-sheet .bs-more [data-home-section='scoreboard'] { grid-column: 1 / span 5; grid-row: 2; }
    .pub .bs-sheet .bs-more [data-home-section='rankings'] { grid-column: 6 / span 5; grid-row: 2; }
    .pub .bs-sheet .bs-more > .bs-col--side { grid-column: 11 / span 2; grid-row: 2; }
    /* Most weeks the side column is empty (no race, not Friday): Scoreboard and Top 25 split the row. */
    .pub .bs-sheet .bs-more:not(:has(> .bs-col--side > :not([hidden]))) > .bs-col--side { display: none; }
    .pub .bs-sheet .bs-more:not(:has(> .bs-col--side > :not([hidden]))) [data-home-section='scoreboard'] { grid-column: 1 / span 6; }
    .pub .bs-sheet .bs-more:not(:has(> .bs-col--side > :not([hidden]))) [data-home-section='rankings'] { grid-column: 7 / span 6; }

    /* Band 4: videos, full width. */
    .pub .bs-sheet .bs-more [data-home-section='videos'] { grid-column: 1 / -1; grid-row: 3; }

    /* Rules: each band ends with a heavy rule; the first item in each row has no left rule. */
    .pub .bs-sheet .bs-more :is([data-home-section='sports-news'], [data-home-section='entertainment-news'],
        [data-home-section='tech-news'], [data-home-section='scoreboard'], [data-home-section='rankings']),
    .pub .bs-sheet .bs-more > .bs-col--side {
        border-bottom: 2px solid var(--text);
    }
    .pub .bs-sheet .bs-more :is([data-home-section='sports-news'], [data-home-section='scoreboard'], [data-home-section='videos']) {
        padding-left: 0;
        border-left: 0;
    }
    .pub .bs-sheet .bs-more :is([data-home-section='tech-news'], [data-home-section='videos']),
    .pub .bs-sheet .bs-more > .bs-col--side,
    .pub .bs-sheet .bs-more:not(:has(> .bs-col--side > :not([hidden]))) [data-home-section='rankings'] {
        padding-right: 0;
    }

    /* Scoreboard: the six compact games as cards, three across. */
    :root:is([data-color-scheme], :not([data-color-scheme])) .pub .bs-sheet .bs-more .d-scoreboard .scoreboard-games {
        display: grid;
        grid-template-columns: repeat(3, minmax(0, 1fr));
        gap: 12px;
    }

    /* Top 25: a 5 × 5 grid that reads down each column, instead of a 25-row table. */
    .pub .bs-sheet .d-rankings .d-rankings__table {
        overflow: visible;
    }
    .pub .bs-sheet .d-rankings .rankings-table,
    .pub .bs-sheet .d-rankings .rankings-table tbody {
        display: block;
        width: 100%;
    }
    .pub .bs-sheet .d-rankings .rankings-table :is(colgroup, thead) {
        display: none;
    }
    .pub .bs-sheet .d-rankings .rankings-table tbody {
        display: grid;
        grid-template-columns: repeat(5, minmax(0, 1fr));
        grid-template-rows: repeat(5, auto);
        grid-auto-flow: column;
        column-gap: 12px;
    }
    .pub .bs-sheet .d-rankings .rankings-table tr {
        display: grid;
        grid-template-columns: 24px minmax(0, 1fr);
        grid-template-areas: 'rank team' 'rank meta';
        column-gap: 6px;
        align-items: baseline;
        padding: 7px 8px;
        border-bottom: 1px solid var(--row-border);
    }
    .pub .bs-sheet .d-rankings .rankings-table :is(td, th) {
        display: block;
        padding: 0;
        border: 0;
        text-align: left;
    }
    .pub .bs-sheet .d-rankings .rankings-table tr > td:first-child {
        grid-area: rank;
        font-family: var(--font-serif);
        font-size: 16px;
        font-weight: 700;
    }
    .pub .bs-sheet .d-rankings .rankings-table tr > th {
        grid-area: team;
        min-width: 0;
        overflow: hidden;
        text-overflow: ellipsis;
        white-space: nowrap;
    }
    .pub .bs-sheet .d-rankings .rankings-table tr > td:nth-child(3),
    .pub .bs-sheet .d-rankings .rankings-table tr > td:nth-child(4) {
        grid-area: meta;
        font-size: 11px;
        color: var(--text-2);
    }
    .pub .bs-sheet .d-rankings .rankings-table tr > td:nth-child(4) {
        justify-self: end;
    }
    .pub .bs-sheet .d-rankings .rankings-table tr > td:nth-child(4)::before {
        content: 'was ';
    }

    /* Videos: one row of six. */
    .pub .bs-home .bs-sheet .d-vids .d-list {
        grid-template-columns: repeat(6, minmax(0, 1fr));
        gap: 20px;
    }
    .pub .bs-home .bs-sheet .d-vids .d-vid:nth-child(n + 7) {
        display: none;
    }
}
```

> **Top 25 and screen readers.** Changing a table's `display` can drop its table semantics in some browsers. The
> `aria-label` region and `<caption>` stay, and each row still reads rank, school, record, previous in order, which
> is fine for this. If you'd rather keep it a real table, add `role="table"`, `role="row"`, `role="cell"` /
> `role="rowheader"` to the markup in `HomeRankings.razor`.

> **"Previous" wording:** the grid shows "was 3". `RankingsQuery.Previous` may return "NR" or "—"; check those read
> sensibly with "was " in front, or put the prefix in the markup only when it's a number.

> **2560 and up:** with the 2560 sheet rules gone, the bands simply get wider. If they feel too stretched on a 4K
> screen at 100% scaling, cap the page (`.pub .bs-home { max-width: 2400px; margin-inline: auto; }`) rather than
> adding columns back.

### 5.3 1024–1919: the empty side column

`.bs-more`'s side column now only has content on race weekends and Fridays. Inside the existing
`@media (min-width: 1024px)` block that sets `.bs-front, .bs-more` to three columns (line ~822), add:

```css
    /* No F1 and no Friday releases: the main column takes the side column's space. */
    .pub .bs-more:not(:has(> .bs-col--side > :not([hidden]))) {
        grid-template-columns: minmax(280px, 1fr) minmax(0, 3fr);
    }
    .pub .bs-more:not(:has(> .bs-col--side > :not([hidden]))) > .bs-col--side {
        display: none;
    }
```

---

## 6. Verify

| Check | Expected |
|---|---|
| 2000 px (your screen) | Four bands; in each band the columns end together, with no long empty stretches |
| Band 1 | Today & Tonight shows 5 games + "N more today →"; Box office under it; lead photo beside its headline; four More headlines across; Latest video + Today's episodes on the right |
| Band 2 | Sports, Entertainment, Tech: five stories each |
| Band 3, a normal Sunday | Scoreboard and Top 25 split the row; six game cards three across; Top 25 in five columns of five, Big 12 highlighted |
| Band 3, race weekend or Friday | F1 / In theaters in a narrow third column |
| Band 4 | Six videos in one row |
| Upcoming Digital / VOD | Gone on desktop and phone (§4.5) |
| Box office, Sunday before results publish | Forecast for this weekend |
| Box office, once results publish (Sun–Tue) | Results |
| Box office, Wed–Sat | Coming weekend's forecast |
| Settings → "Only my teams" on | Today & Tonight shows your first five games; "N more" counts only your games; empty time headings hidden |
| Settings → hide any home section | Its slot empties, nothing else moves |
| 1440 | Three columns as before; sections in their new columns; no empty side column on a normal day |
| 2560 | Bands, wider |
| Phone, iPad portrait | Unchanged apart from the VOD removal (§4.2) |

`tests/browser/home-columns.browser.cjs` asserts the old column layout at wide widths; update it to check the band
placements (e.g. that `[data-home-section="sports-news"]`, `entertainment-news` and `tech-news` share a top offset at
2000 px, and that `[data-home-section="digital"]` is absent).

## Files touched

| File | Section |
|---|---|
| `Components/Pages/Home.razor` | 1, 2, 3, 4 |
| `wwwroot/personal.js` | 3 |
| `wwwroot/broadsheet.css` | 2.4, 3.3, 5 |
| `myNewspaper.Shared/Profiles/SiteDefaults.cs` | 4 |
| `tests/browser/home-columns.browser.cjs` | 6 |
