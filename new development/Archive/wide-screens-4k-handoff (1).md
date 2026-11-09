# Wide screens and 4K (desktop, 1920px and up)

Written October 30, 2026 (America/Chicago), against `develop` at `ac57cb2`. Class names below were checked against
that commit. All headlines also uses the classes from `docs/headline-cards-handoff.md`; its Jump to box (`hc-jump`)
isn't on `develop` yet.

**Save as:** `docs/wide-screens-4k-handoff.md`

**Mockups:** the myNewspaper canvas, **4K screens** page (https://claude.ai/artifact/JVQ8mU7R63Z1cziNpeW9Z5). The
boards are Home width ladder, Home, News, Sports, Entertainment, All headlines and YouTube, all at 2560 CSS px.

## The idea: add columns, not margins

A 4K screen is 3840 device pixels wide, but most people run it at 150% or 200% scaling, so the browser reports 2560 or
1920 CSS px. Today the page stops growing at 1840px (`--page-max` in `public.css`). On a 2560 viewport that leaves
about 360px of empty space on each side. The fix:

1. Let the page fill the screen from 1920px up.
2. Give each page **more columns**, filled with content that today sits below the fold. Type size and card size
   stay the same.
3. At 3200px and up (4K at 100%, TVs), scale the whole 2560 layout up by 1.25 so type stays readable from a
   distance.

| Viewport (CSS px) | Who | Page width | Layout |
|---|---|---|---|
| 1024–1439 | Laptops | 1280 | Unchanged |
| 1440–1919 | Large monitors | 1840 | Unchanged |
| **1920–2559** | 1080p, or 4K at 200% | Full width, 56px sides | **Wide**: one more column |
| **2560–3199** | 1440p, or 4K at 150% | Full width, 56px sides | **4K**: everything above the fold |
| **3200+** | 4K at 100%, TVs | Full width | The 4K layout, `zoom: 1.25` |

Where to watch, the sub-pages (Scoreboard, Standings, Movies, TV and so on), Saved, Settings and Admin get **no special
layout**. They only pick up the wider page from step 1, and their `auto-fit` / `auto-fill` grids add columns on
their own. Check them in step 7, but don't write rules for them.

---

## 1. Page width (`src/myNewspaper.Shared/wwwroot/public.css`)

Put this after the existing `@media (min-width: 1680px)` block in the "Wide screens" section:

```css
/* ---------- 1920px and up: the page fills the screen; pages add columns (wide-screens-4k-handoff.md) ---------- */
@media (min-width: 1920px) {
    .pub {
        --page-max: 100%;
        --page-pad: 56px;
    }
    .pub .videos {
        grid-template-columns: repeat(5, minmax(0, 1fr));
    }
}
@media (min-width: 2560px) {
    .pub .videos {
        grid-template-columns: repeat(6, minmax(0, 1fr));
    }
}
/* 4K at 100% and TVs: reuse the 2560 layout, 25% larger. 3200 / 1.25 = 2560, so the 2560 rules always fit. */
@media (min-width: 3200px) {
    .pub {
        zoom: 1.25;
    }
}
```

**Header:** the `.site-header` padding formula already handles `--page-max: 100%`. It works out to
`max(56px, 0 + 56px)`, so the header lines up with the page edges at every width.

**Zoom:** the media queries still see the real viewport, so at 3840 every `min-width: 2560px` rule applies, and the
zoomed page is 3072 CSS px wide inside. `zoom` is supported in Chrome, Edge, Safari and Firefox 126+. If anything
measures positions in JavaScript (`getBoundingClientRect` in `home-lead.js`, the pinned Jump to bar), check it at
3840 (step 7).

## 2. Home (`Components/Pages/Home.razor`, `wwwroot/broadsheet.css`)

Today the home is two three-column grids: `.bs-front` above the band and `.bs-more` ("More from today") below it. On
wide screens the six `.bs-col` columns join **one** grid. No section moves between columns, so no Razor component
changes.

| Column | 1024–1919 (today) | 1920–2559 | 2560+ |
|---|---|---|---|
| Front left: Today & Tonight | Front, col 1 | Row 1, col 1 | Col 1 |
| Front main: lead + More headlines | Front, col 2 | Row 1, col 2 | Col 2 (widest) |
| Front side: latest video, box office, Sports headlines | Front, col 3 | Row 1, col 3 | Col 3 |
| More left: Scoreboard, Top 25 | Below the band | **Row 1, col 4** | Col 4 |
| More main: Entertainment headlines, Latest videos | Below the band | Band, then row 3, cols 1–2 | Col 5 |
| More side: TV, Movies, Digital | Below the band | Band, then row 3, cols 3–4 | Col 6 |

At 2560 the band ("More from today") disappears, because nothing is below the fold any more. From 1920 the logo also
moves from the masthead to the navigation line (2.3).

> The 2560 mockup groups some sections differently (Sports and Entertainment as their own columns, plus an Opinion,
> Kansas City and In pictures strip under the lead). This guide keeps today's columns so nothing moves. The
> regrouping can be a later pass if you still want it after seeing this live.

### 2.1 Markup

Wrap the two grids and the band in one element:

```razor
<div class="bs-sheet">
    <div class="bs-front"> … unchanged … </div>
    <h2 class="bs-band">More from today</h2>
    <div class="bs-more"> … unchanged … </div>
</div>
```

`.bs-sheet` has no styles below 1920px, so phones and current desktops don't change.

### 2.2 Styles (end of `broadsheet.css`)

```css
/* ---------- Home, 1920px and up: one sheet of columns (wide-screens-4k-handoff.md) ---------- */
@media (min-width: 1920px) {
    .pub .bs-sheet {
        display: grid;
        grid-template-columns: minmax(300px, 1fr) minmax(0, 2fr) minmax(300px, 1fr) minmax(300px, 1fr);
        padding-top: 20px;
    }
    .pub .bs-sheet .bs-front,
    .pub .bs-sheet .bs-more {
        display: contents;              /* their .bs-col children become the sheet's columns */
    }
    /* Every column is ruled on its left except the first; the existing :first-child / + rules don't work across the two wrappers. */
    .pub .bs-sheet .bs-col {
        padding: 0 22px;
        border-left: 1px solid var(--row-border);
    }
    .pub .bs-sheet .bs-front > .bs-col--left {
        padding-left: 0;
        border-left: 0;
    }
    .pub .bs-sheet .bs-more > .bs-col--left { grid-column: 4; grid-row: 1; }
    .pub .bs-sheet .bs-band { grid-column: 1 / -1; grid-row: 2; }
    .pub .bs-sheet .bs-more > .bs-col--main { grid-column: 1 / span 2; grid-row: 3; padding-left: 0; border-left: 0; }
    .pub .bs-sheet .bs-more > .bs-col--side { grid-column: 3 / span 2; grid-row: 3; }
}
@media (min-width: 2560px) {
    .pub .bs-sheet {
        grid-template-columns: 320px minmax(0, 2fr) repeat(4, minmax(320px, 1fr));
    }
    .pub .bs-sheet .bs-band { display: none; }
    .pub .bs-sheet .bs-more > .bs-col--left { grid-column: 4; grid-row: 1; }
    .pub .bs-sheet .bs-more > .bs-col--main { grid-column: 5; grid-row: 1; padding-left: 22px; border-left: 1px solid var(--row-border); }
    .pub .bs-sheet .bs-more > .bs-col--side { grid-column: 6; grid-row: 1; }
}
```

The narrow-column rules already keyed on `.bs-col--side` (Movies and Digital as thumbnail lists) still apply,
because the classes don't change.

### 2.3 Masthead: the logo moves to the navigation line

Today from 1024px up, the home page hides the header logo (`.pub[data-page='home'] .brand { display: none; }`,
`broadsheet.css` around line 1669) and shows a large centred logo in the masthead instead (`.bs-title__name`). From
1920px up that flips:

- The **header logo** (`.brand`: tile + "myNewspaper" wordmark, already in `PublicLayout.razor` before the nav) shows on
  home too, at the start of the navigation line, the same as on every other page.
- The masthead's **big logo** (`.bs-title__name`) is hidden. The masthead becomes a row with the **date and greeting
  on the left** (date as a small blue eyebrow, "Good afternoon" large in the serif), then the weather and Coming up
  ears on the right.

That shortens the masthead by about 120px, which helps the "everything above the fold" goal at 2560. No markup
changes: the header logo is already rendered on home, and `.bs-title__greeting` stays the page's `<h1>`.

Add to the end of `broadsheet.css`, after section 2.2's rules:

```css
/* ---------- Home masthead, 1920px and up: logo on the navigation line (wide-screens-4k-handoff.md) ---------- */
@media (min-width: 1920px) {
    /* Same specificity as the 1024px rule that hides it, and later in the file, so this wins. */
    .pub[data-page='home'] .brand {
        display: flex;
    }
    .pub .bs-title__name {
        display: none;
    }
    .pub .bs-masthead {
        grid-template-columns: minmax(0, 1fr) auto minmax(360px, 460px);   /* greeting · weather row · Coming up */
        align-items: end;
        gap: 28px;
        padding: 18px 0 18px;
    }
    .pub .bs-masthead > .bs-title {
        grid-column: 1;
        display: grid;
        grid-template-columns: auto auto;
        grid-template-areas: "date date" "greeting refresh";
        justify-content: start;
        align-items: end;
        gap: 4px 20px;
        text-align: left;
    }
    .pub .bs-title__date {
        grid-area: date;
        margin: 0;
        color: var(--accent);
    }
    .pub .bs-title__greeting {
        grid-area: greeting;
        margin: 0;
        font-size: 72px;
        font-style: normal;
        font-weight: 800;
        line-height: 1;
        letter-spacing: -0.02em;
    }
    .pub .bs-title .home-refresh {
        grid-area: refresh;
        margin: 0 0 8px;
    }
    .pub .bs-masthead > .bs-glance {
        grid-column: 2;
    }
    .pub .bs-masthead > .bs-live {
        grid-column: 3;
    }
}
```

#### Weather: one row with the existing icons

At 1920 the weather box becomes a single row of four periods, with no box, as in the mockup. Each period shows the
**same icon the site already uses**, from `Components/WeatherIcon.razor`, beside a larger temperature. The markup is
unchanged: each `<li>` in `.bs-glance__periods` already contains `<WeatherIcon Summary=… IsDaytime=… />`, so this is
CSS only. The icon set and how a forecast picks one:

| Forecast summary contains | Icon (`weather-icon--…`) | Shape |
|---|---|---|
| thunder, t-storm | `storm` | Cloud + lightning bolt + rain |
| freezing, ice, icy | `ice` | Cloud + two ice diamonds |
| sleet, wintry, winter mix, rain and snow | `mixed` | Cloud + rain streak + snowflake |
| snow, flurries | `snow` | Cloud + two snowflakes |
| rain, showers, drizzle | `rain` | Cloud + three rain streaks |
| fog, mist, haze, smoke, dust | `fog` | Cloud + two horizontal lines |
| partly…, mostly sunny, mostly clear | `partly-day` / `partly-night` | Sun or moon behind a cloud |
| cloud, overcast | `cloud` | Cloud |
| sunny, clear | `sun` / `moon` | Sun with rays / crescent moon |
| wind, breezy | `wind` | Three wind curls |
| anything else | `temperature` | Thermometer |

The checks run top to bottom, and the first match wins ("Chance Showers And Thunderstorms" → `storm`). Day or night
comes from the forecast period's `IsDaytime`. The icons are line drawings (`stroke="currentColor"`), so they take the
accent colour from `.weather-icon` in `public.css` and follow dark mode and the colour schemes. Nothing to add for that.

```css
@media (min-width: 1920px) {
    /* No box: the weather reads as the masthead's right-hand ear. */
    .pub .bs-glance {
        padding: 0;
        border: 0;
        border-radius: 0;
    }
    .pub .bs-glance__title {
        position: absolute;              /* visually hidden; still names the section for screen readers */
        width: 1px;
        height: 1px;
        overflow: hidden;
        clip: rect(0 0 0 0);
    }
    .pub .bs-glance__weather {
        grid-template-columns: auto auto;
        grid-template-areas: "periods link";
        align-items: end;
        gap: 0 16px;
    }
    .pub .bs-glance__weather > .d-more { grid-area: link; }
    .pub .bs-glance__periods {
        grid-area: periods;
        grid-template-columns: repeat(4, auto);
        gap: 0;
    }
    .pub .bs-glance__periods li {
        display: grid;
        grid-template-columns: auto auto;
        grid-template-areas: "name name" "icon temp";
        align-items: center;
        gap: 2px 8px;
        padding: 0 16px;
        border-left: 1px solid var(--row-border);
    }
    .pub .bs-glance__name { grid-area: name; }
    .pub .bs-glance__periods .weather-icon {
        grid-area: icon;
        width: 34px;
        height: 34px;
        margin: 0;
    }
    .pub .bs-glance__reading {
        grid-area: temp;
        display: flex;
        flex-direction: column;           /* temperature on top, the summary text under it */
        max-width: 170px;
        font-size: 12px;
    }
    .pub .bs-glance__reading b {
        display: block;
        margin: 0;
        font-family: var(--font-serif);
        font-size: 26px;
        line-height: 1.1;
    }
}
```

`.bs-glance__reading` holds `<b>76°</b> Chance Showers And Thunderstorms`. With `flex-direction: column`, the loose
summary text becomes its own line under the temperature without any markup change. `max-width: 170px` wraps long
summaries onto two lines instead of widening the row. **Check** with the longest summaries the forecast service sends
(e.g. "Chance Showers And Thunderstorms Then Mostly Cloudy"): the row should stay one line high apart from that wrap.

The Coming up ear (`.bs-live`) keeps its box. Drop its border the same way if the mix looks off next to the boxless
weather.

The 1024px rules that show `.brand-tile` and `.brand-wordmark` (`broadsheet.css` around line 1661) already apply on
home. They were only invisible because their parent `.brand` was hidden, so the tile and wordmark appear at their
usual 32px / 22px. **Check:** the nav doesn't wrap at 1920 with the logo added (Saved, Settings, the edition chip and
the toggles on the right). If it does, drop `.site-nav--inline`'s gap from 22px to 18px inside this query.

## 3. News and Sports fronts (`Components/NewsFront.razor`, `Components/SportsFront.razor`, `wwwroot/sections.css`)

Both use `.spf-cols`: `.spf-upcoming` | `.ef-main.spf-main` | `.ef-side.spf-side`. On wide screens the **side column
becomes two or three columns of its own**. Its sections sit next to each other instead of stacking:

| | 1024–1919 | 1920–2559 | 2560+ |
|---|---|---|---|
| News side | Kansas City, Opinion, Your topics stacked | 2 across | 3 across (KC, Opinion, Your topics) |
| Sports side | Standings, Top 25 stacked | 2 across | 2 across, wider |

```css
/* ---------- News and Sports fronts, 1920px and up (wide-screens-4k-handoff.md) ---------- */
@media (min-width: 1920px) {
    .pub .spf-cols {
        grid-template-columns: 300px minmax(0, 1.8fr) minmax(0, 1.6fr);
    }
    .pub .spf-side {
        display: grid;
        grid-template-columns: repeat(2, minmax(0, 1fr));
        gap: 0 28px;
        align-content: start;
    }
    .pub .spf-side > * {
        min-width: 0;
        margin-top: 0;
    }
    .pub .nf-brief__tiles {
        grid-template-columns: repeat(auto-fit, minmax(260px, 1fr));
    }
}
@media (min-width: 2560px) {
    .pub .spf-cols {
        grid-template-columns: 320px minmax(0, 1.6fr) minmax(0, 2.4fr);
    }
    .pub .spf-side {
        grid-template-columns: repeat(auto-fit, minmax(300px, 1fr));
    }
}
```

`.nf-side + .nf-side` currently adds a rule between stacked side sections (`sections.css` around line 2450). Once
they sit side by side, swap it for a left rule:

```css
@media (min-width: 1920px) {
    .pub .spf-side .nf-side + .nf-side {
        border-top: 0;
        padding-top: 0;
        padding-left: 28px;
        border-left: 1px solid var(--row-border);
    }
}
```

`.nf-desks` and `.nf-pictures__grid` already use `auto-fit`, so they add columns on their own. **Sports score
strip:** if `ScoreboardPanel Compact` uses a fixed column count, raise it to 6 at 1920 and 8 at 2560. If it uses
`auto-fit`, leave it.

> The 2560 Sports mockup also has a fifth column with F1 and College Sports. That's new content, not layout, so it
> isn't in this guide.

## 4. Entertainment front (`Components/EntertainmentFront.razor`, `wwwroot/sections.css`)

`.ef-cols` is `.ef-main` | `.ef-side`. The side holds `.ef-box` (box office) and `.ef-tv`. Same treatment as above:

```css
/* ---------- Entertainment front, 1920px and up (wide-screens-4k-handoff.md) ---------- */
@media (min-width: 1920px) {
    .pub .ef-cols {
        grid-template-columns: minmax(0, 1.6fr) minmax(0, 1.4fr);
    }
    .pub .ef-cols > .ef-side {
        display: grid;
        grid-template-columns: repeat(2, minmax(0, 1fr));
        gap: 0 28px;
        align-content: start;
    }
    .pub .ef-cols > .ef-side > * {
        min-width: 0;
        margin-top: 0;
    }
    .pub .ef-screen__list {
        grid-template-columns: repeat(auto-fill, minmax(380px, 1fr));   /* 4 at 1920, 6 at 2560 */
    }
}
@media (min-width: 2560px) {
    .pub .ef-cols {
        grid-template-columns: minmax(0, 1.4fr) minmax(0, 1.6fr);
    }
}
```

Use `.ef-cols > .ef-side`, not `.ef-side`, so these rules don't also hit the Sports and News side columns. Those also
carry `ef-side` but sit inside `.spf-cols`.

## 5. All headlines (`wwwroot/headline-cards.css`)

The card grid goes from three columns to four, then five. The top section gets wider columns and keeps the same
arrangement (lead + two under it + Latest).

```css
/* ---------- All headlines, 1920px and up (wide-screens-4k-handoff.md) ---------- */
@media (min-width: 1920px) {
    :root:is([data-color-scheme], :not([data-color-scheme])) .pub .hc-grid {
        grid-template-columns: repeat(4, minmax(0, 1fr));
    }
    :root:is([data-color-scheme], :not([data-color-scheme])) .pub .hc-top {
        grid-template-columns: minmax(0, 2fr) minmax(0, 1.1fr);
        gap: 32px;
    }
    .pub .hc-lead .title {
        font-size: 40px;
    }
}
@media (min-width: 2560px) {
    :root:is([data-color-scheme], :not([data-color-scheme])) .pub .hc-grid {
        grid-template-columns: repeat(5, minmax(0, 1fr));
    }
    /* Jump to box: hours and categories on one row instead of two. */
    .pub .hc-jump {
        grid-template-columns: auto minmax(0, 1fr) auto minmax(0, 1fr);
    }
    .pub .hc-jump__row {
        grid-template-columns: repeat(7, minmax(0, 1fr));
    }
}
```

**Load more** appends whole rows. If the page size isn't a multiple of 4 and 5, the last row is ragged until the next
load. That's acceptable; only change the page size if it bothers you (60 divides by 3, 4 and 5).

## 6. YouTube (`Components/Pages/Videos.razor`, `wwwroot/sections.css`)

### 6.1 Phase 1: layout only

```css
/* ---------- YouTube, 1920px and up (wide-screens-4k-handoff.md) ---------- */
@media (min-width: 1920px) {
    .pub .yt-top {
        grid-template-columns: minmax(0, 2.2fr) minmax(400px, 1fr);
    }
    .pub .yt-slide__title {
        font-size: 40px;
        line-height: 1.08;
    }
    .pub .video-front .videos {
        grid-template-columns: repeat(5, minmax(0, 1fr));
    }
}
@media (min-width: 2560px) {
    .pub .yt-top {
        grid-template-columns: minmax(0, 1fr) 460px;      /* phase 2 adds a third column of 380px */
    }
    .pub .yt-slide__title {
        font-size: 48px;
    }
    .pub .video-front .videos {
        grid-template-columns: repeat(6, minmax(0, 1fr));
    }
    .pub .video-front .video--featured {
        grid-column: span 1;      /* the 1150px rule makes it span 2; at six across it should line up with the rest */
    }
}
```

The featured video stays still by default; nothing here changes the carousel script.

### 6.2 Phase 2 (optional): the third column, and channel groups as tabs

The 2560 mockup adds a third column with:

- **By channel.** Each category (the same categories as `yt-category-select`) with a count of videos published today.
  This needs one grouped query, e.g. `COUNT(*) … WHERE PublishedAt >= @todayStart GROUP BY CategoryId`. It is cheap
  enough to run with the page's existing listing query.
- **Watch later.** The profile's saved videos, newest first, at most three. Use the same source that
  `VideoSaveButton` writes to and the Saved page reads from.

Markup: add `<aside class="yt-aside">` as the third child of `.yt-top`, rendered only when there's something to show.
At 2560:

```css
@media (min-width: 2560px) {
    .pub .yt-top:has(.yt-aside) { grid-template-columns: minmax(0, 1fr) 460px 380px; }
    .pub .yt-aside { padding-left: 28px; border-left: 1px solid var(--row-border); }
    .pub .yt-latest { padding-right: 28px; }
}
```

Below 2560, hide `.yt-aside` (`display: none`). The channel `<select>` stays everywhere. At 2560 the mockup shows
the categories as tabs. The existing `.sec-pills.yt-cats` nav can do that if you show it from 2560 and hide the
select there. Your earlier call was no pills, so only do this if you want it at this width.

## 7. Verify (per AGENTS.md)

Test at **1920×1080, 2560×1440 and 3840×2160** (DevTools device toolbar, custom sizes), plus **1440** and **1280**
to confirm nothing changed there:

1. **Width.** At 1920 and 2560 the page and header reach 56px from each edge, and there's no horizontal scroll at
   any of the five widths.
2. **Home.**
   - At 1920: four columns on top, the band, then two wide columns.
   - At 2560: six columns, no band, every column ruled on its left except the first.
   - The lead image and `home-lead.js` behave the same.
   - From 1920: the logo and wordmark sit at the start of the navigation line, and the big centred logo is gone.
     The date and "Good afternoon" are on the left of the masthead, weather and Coming up on the right. Refresh still
     works. The weather is one row of four with each period's icon. Check a stormy, a clear-night and a snowy
     forecast, for example by temporarily hard-coding summaries. At 1440 and below, the masthead is unchanged (big centred logo, no logo in the nav).
3. **News and Sports.** The side sections sit side by side with a left rule between them. **Entertainment:** box
   office and TV side by side; On screen 4 across at 1920 and 6 at 2560.
4. **All headlines.**
   - Grid 4 across at 1920 and 5 at 2560; the top section's columns end within a row of each other.
   - The pinned Jump to bar still pins and highlights the right hour at 3840 (zoom).
5. **YouTube.**
   - Featured title doesn't overflow at 1920.
   - More videos 5 across at 1920 and 6 at 2560, with no orphaned double-width card.
6. **3840 (zoom).**
   - Everything is 25% larger and laid out like 2560.
   - Hover states, focus rings and the sticky header line up.
   - Tooltips and popovers open in the right place.
7. **Pages with no special layout** (Where to watch, Scoreboard, Standings, Movies, TV, Saved, Settings): nothing
   stretches awkwardly, and line lengths in text blocks stay readable. The existing `max-width: 72ch` on article text
   covers most of this.
8. **Dark mode and two other colour schemes** at 2560: the column rules are visible.
9. **Phones** (320, 390, 430px) and **iPad** (768, 1024): unchanged. Everything here is behind `min-width: 1920px`.
