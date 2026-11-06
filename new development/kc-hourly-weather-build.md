# Build plan: Kansas City "Next 8 hours" (concept D)

Approved November 6, 2026 (America/Chicago). Concept D from the hourly weather review: the **8-column strip on desktop** (concept A) and the **rain list on phones** (concept C), both opening with the same one-line rain summary. Mockups: the "Kansas City Hourly Weather Concepts" canvas, row D.

Page: News › Kansas City (`/news/kansas-city`, `#local-weather`), `src/myNewspaper.Web/Components/Pages/Local.razor`.

> **Decisions already made.** Refresh stays as it is today: on page load and when the viewer switches location, with no timer. Hours show in the **forecast location's time zone**. The hourly view can fail on its own; the four forecast cards never depend on it.

---

## 1. What it looks like

### Desktop (1024px and up)

Under the four forecast cards and above the closing rule:

```
NEXT 8 HOURS   💧 Rain most likely 6–8 PM, up to 40% at 7 PM            Times in CST
┌────────┬────────┬────────┬────────┬────────┬────────┬────────┬────────┐
│  4 PM  │  5 PM  │  6 PM  │  7 PM  │  8 PM  │  9 PM  │ 10 PM  │ 11 PM  │
│  icon  │  icon  │  icon  │  icon  │  icon  │  icon  │  icon  │  icon  │
│  48°   │  47°   │  45°   │  44°   │  42°   │  41°   │  40°   │  39°   │
│ Mostly │ Mostly │ Chance │ Chance │ Slight │ Mostly │ Partly │ Mostly │
│ Cloudy │ Cloudy │ Rain…  │ Rain…  │ Chance…│ Cloudy │ Cloudy │ Clear  │
│ 💧 10% │ 💧 15% │ 💧 30% │ 💧 40% │ 💧 20% │ 💧 10% │ 💧 5%  │ 💧 0%  │
└────────┴────────┴────────┴────────┴────────┴────────┴────────┴────────┘
```

- One panel with 8 equal columns, a 1px border between columns, and the same border, 14px corners and panel background as the forecast cards.
- Each column, top to bottom: the time (12px uppercase label), a 30px icon, the temperature (Newsreader 26px), the condition (12px muted, at most 2 lines, full text in `title`), then the rain chance with a droplet.
- When the rain chance is 30% or more, the droplet is filled and the percentage is bold (800), so it doesn't rely on color alone.

### Phone (below 1024px; there is no tablet layout on this page)

Inside the existing forecast card, after the three tiles and above **Use my location**:

```
NEXT 8 HOURS                                Times in CST
💧 Rain most likely 6–8 PM, up to 40%
4 PM   icon  48°  ▓░░░░░░░░░░   10%
5 PM   icon  47°  ▓▓░░░░░░░░░   15%
6 PM   icon  45°  ▓▓▓▓░░░░░░░   30%
…eight rows in all
```

- One row per hour with five columns: `46px 24px 34px 1fr 36px`, a minimum height of 40px, and a 1px rule between rows.
- The bar's track is a light tint and its fill uses `--accent`, with the width set to the rain chance. The percentage always shows as text.
- The condition text is hidden on phones; the icon and the screen-reader sentence carry it.
- Nothing scrolls sideways at 320px.

### States

| State | What shows |
|---|---|
| Loading | Nothing. The cards show their own loading text; the hourly block stays hidden until its data arrives. |
| Hourly fetch fails or returns fewer than 8 hours | One muted line where the block would be: **Hourly forecast unavailable.** The cards are untouched. |
| Rain chance missing for an hour | Show **—** instead of a percentage, with an empty bar. Never show it as 0%. |
| Location switched | The block reloads with the cards, and the zone label follows the new location. |
| No JavaScript | Nothing, the same as the cards (the existing `<noscript>` line covers it). |

### Rain summary (one line, both layouts)

Worked out on the server (see section 3) and shown as plain text:

| Hours in the 8 | Summary |
|---|---|
| Any hour at 30% or more | `Rain most likely {first}–{end}, up to {max}% at {time of max}`. Here `{end}` is one hour after the last hour at 30% or more, e.g. *6–8 PM*; a range that crosses noon or midnight reads *11 AM–1 PM*. |
| Highest is 1–29% | `Low chance of rain through {last hour}, up to {max}%` |
| All 0% | `No rain expected through {last hour}` |
| Any hour's rain chance missing | No summary line |

30% is where NWS wording moves from "Slight Chance" to "Chance", so the threshold matches the conditions text.

---

## 2. Markup (Local.razor)

Add one empty host inside `<section id="local-weather">`, **after** `<div class="weather-grid" data-weather-periods></div>`, so it comes after the cards on desktop:

```html
<section class="hw" data-weather-hourly aria-labelledby="hw-title" hidden>
    <div class="hw__head">
        <h3 class="hw__title" id="hw-title">Next 8 hours</h3>
        <p class="hw__summary" data-weather-hourly-summary></p>
        <p class="hw__zone" data-weather-hourly-zone></p>
    </div>
    <ol class="hw__hours" data-weather-hourly-list></ol>
    <p class="hw__unavailable" data-weather-hourly-unavailable hidden>Hourly forecast unavailable.</p>
</section>
```

On phones the block has to sit after `[data-weather-tiles]` and before `.weather-location-controls`. Today the phone card (`#local-weather.mx-local-weather` in `mobile.css`) stacks in DOM order, so the build makes it `display: flex; flex-direction: column` below 1024px and gives `.hw` an `order` after the tiles. The other children keep their current order. Don't add a second copy of the markup.

> **Class prefix.** `hw-` (hourly weather) is new. Add it to `registeredPrefixes` in `tools/check-css.cjs` and to the prefix table in `docs/coding-standards.md` in the same commit. The existing `weather-*` classes are frozen older names; don't add to them.

---

## 3. Data the front end expects

A new endpoint beside `/local/weather`, so each one can fail and be cached on its own:

```
GET local/weather/hourly            → Kansas City
GET local/weather/hourly?lat=&lon=  → the viewer's location (same validation as /local/weather)
```

```json
{
  "location": "Kansas City",
  "timeZone": "America/Chicago",
  "updated": "2026-11-06T21:22:09Z",
  "stale": false,
  "rainSummary": "Rain most likely 6–8 PM, up to 40% at 7 PM",
  "hours": [
    {
      "label": "4 PM",
      "starts": "2026-11-06T16:00:00-06:00",
      "isDaytime": true,
      "temperature": 48,
      "unit": "F",
      "summary": "Mostly Cloudy",
      "rainChance": 10,
      "wind": "5 mph NNW"
    }
  ]
}
```

Backend notes (in `LocalWeather`, for whoever builds the data side):

- [x] Read `properties.forecastHourly` and `properties.timeZone` from the `/points/` response that `FetchForecastAsync` already makes. Cache the points lookup, so the hourly fetch doesn't repeat it.
- [x] Hourly fetch, cache and failure backoff follow the forecast's pattern (one fetch in flight per key, 15 minutes fresh, 2 minutes after a failure, a 6-hour "last" copy marked `stale`), under their own keys (`kc-weather-hourly`, `weather-hourly:{lat}:{lon}`).
- [x] **Exactly 8 hours, from the next hour:** take the periods whose `startTime` is after now (`TimeProvider`), then the first 8. Fewer than 8 counts as a failure and returns `hours: []`.
- [x] `label` comes from `startTime`'s own offset (`h tt`, e.g. "4 PM"), so it's already in the location's local time, with no time-zone database needed.
- [x] `rainChance` comes from `probabilityOfPrecipitation.value`, and `null` stays `null`.
- [x] `rainSummary` follows the table in section 1. It's a pure static method, so it's unit-testable.
- [x] Unit tests (in `LocalWeatherTests`): next-hour start, exactly 8, fewer than 8 fails, a missing rain chance stays null, all four summary cases, and a range crossing midnight.
- [x] Endpoint test: bad coordinates return 400, as `/local/weather` does.

---

## 4. Script (personal.js)

Extend `weather()`. Don't add a new script; this file already owns the forecast, its location switch and its loading state.

- [x] Start the hourly request **alongside** the forecast request (`Promise.allSettled` or two independent `fetch`es), with the same `AbortController`, so switching location cancels both.
- [x] Render the cards exactly as now. The hourly block fills in only when its own response arrives, and never blocks or throws into the cards' `try`.
- [x] On success with 8 hours:
  - Clear `[data-weather-hourly-list]` and add one `<li class="hw__hour">` per hour, built with the existing `node()` helper. Don't use `innerHTML`.
  - Inside each item:
    - `<span class="hw__time" aria-hidden="true">` with the time
    - the icon from `window.myNewspaperWeatherIcons.create(summary, isDaytime)`, wrapped in `aria-hidden`
    - `<span class="hw__temp" aria-hidden="true">` with the temperature
    - `<span class="hw__cond" title="…" aria-hidden="true">` with the condition
    - `<span class="hw__bar" aria-hidden="true"><span style="width:N%"></span></span>`
    - `<span class="hw__pop" aria-hidden="true">` with a droplet and "N%" or "—"
    - `<span class="sr-only">` with the sentence below
  - Add the `is-likely` class to the item when the rain chance is 30% or more.
  - Set the summary text, or hide the summary when `rainSummary` is null.
  - Set the zone label to `Times in ` plus the short zone name, from `new Intl.DateTimeFormat('en-US', { timeZone, timeZoneName: 'short' }).formatToParts(new Date())`. If that throws, leave the label empty.
  - Show the block and hide the "unavailable" line.
- [x] On failure, an empty `hours` array or a non-OK response: show the block with only the "unavailable" line, and hide the list, summary and zone.
- [x] If the **forecast** fails for a chosen location, the existing code falls back to Kansas City and calls `weather(true)` again. The hourly block follows that call, so it needs no fallback of its own.

**Screen-reader sentence per hour** (the visible parts are `aria-hidden`, so each hour reads once, in order):

```
4 PM, 48 degrees, mostly cloudy, 10 percent chance of rain.
6 PM, 45 degrees, chance rain showers, 30 percent chance of rain.
8 PM, 42 degrees, slight chance rain showers then mostly cloudy, chance of rain not given.
```

---

## 5. CSS

Desktop rules go in `src/myNewspaper.Web/wwwroot/sections.css`, after the existing `#local-weather .weather-period` rules. Phone rules go in `mobile.css`, next to `.mx-local-weather`. Use only the registered breakpoints (`max-width: 1023px` / `min-width: 1024px`) and the theme tokens: on desktop `--text`, `--text-2`, `--accent`, `--surface` and `--row-border`; inside the phone card its own `--mx-block-fg`, `--mx-block-2`, `--mx-block-line` and `--mx-block-bg`, as the tiles use. Don't hard-code colors and don't use `prefers-color-scheme`.

| Element | Phone (default) | Desktop (≥1024px) |
|---|---|---|
| `.hw` | Top rule `1px var(--row-border)`, padding-top 12px | No rule; 18px above, like the card grid gap |
| `.hw__head` | Title and zone on one row; summary wraps under them | One row: title, summary, then zone pushed right |
| `.hw__title` | 12px, weight 800, 0.06em, uppercase, `--text-2` | same |
| `.hw__summary` | 13px bold, droplet `::before` in `--accent` | 14px bold |
| `.hw__zone` | 11px `--text-2` | 12px |
| `.hw__hours` | `list-style:none`; rows | `grid-template-columns: repeat(8, minmax(0, 1fr))`; 1px border, 14px corners, the cards' panel background |
| `.hw__hour` | `grid-template-columns: 46px 24px 34px minmax(0,1fr) 36px`, min-height 40px, bottom rule | Column flex, centered, padding `14px 8px 12px`, left border between columns |
| `.hw__cond` | `display: none` | 12px `--text-2`, 2-line clamp, `min-height: 2.6em` |
| `.hw__bar` | 8px track (`color-mix(in srgb, var(--accent) 14%, var(--surface))`), fill `--accent` | `display: none` |
| `.hw__pop` | 12px, right-aligned, no droplet | 12px with droplet; `.is-likely` → bold, filled droplet |
| Icon | 24px | 30px |
| `.hw__temp` | Newsreader 19px | Newsreader 26px |

> **Icon colors.** Use the shared `.weather-icon--*` tints as they are, so partly sunny stays yellow and the rest use the accent, matching the forecast cards.

---

## 6. Checks

- [x] `tools/verify.sh`: build with 0 warnings, all .NET tests, JS unit tests, lint, format and `check-css`.
- [x] New browser check `tests/browser/local-hourly.browser.cjs` built on `tests/browser/lib.cjs`: widths `lib.widths([320, 390, 430, 768, 1024, 1440, 1920])`, both `lib.THEMES`.
  - Exactly 8 `.hw__hour` items, the first one after the current hour.
  - Desktop: one row of 8 inside the forecast area's width. Phone: 8 stacked rows, and `scrollWidth <= innerWidth`.
  - The longest condition, "Slight Chance Rain Showers then Mostly Cloudy" (injected with `page.route` on `**/local/weather/hourly*`), stays within 2 lines on desktop.
  - A missing rain chance shows "—", not "0%".
  - **Failure:** `page.route('**/local/weather/hourly*', r => r.fulfill({ status: 503 }))` → still 4 forecast cards, and the unavailable line shows.
  - **Location:** grant geolocation to a point in another time zone (for example Denver) → the zone label changes and the labels shift by an hour.
  - Screen reader: each item's `.sr-only` sentence holds the time, temperature, conditions and rain chance.
- [ ] Mobile verification per AGENTS.md: 320, 390 and 430px in light and dark, with screenshots inspected. Record the widths, themes and results in the handoff.
- [ ] After deploy: `node tools/live-check.cjs`, plus this page on a phone width against the live NWS data.
- [ ] Release-history entry naming the deployed commit and the rollback command.
