# Tech and headline pages: four fixes (iPad sub-pages, Your topics, More stories cards, Live now on phone)

Written November 1, 2026 (America/Chicago), against `develop` at `2addbec` ("Check in for ipad updates"). Every file,
class and line number below was checked against that commit.

**Save as:** `docs/tech-fixes-handoff.md`

**Mockups:** the myNewspaper canvas, **Tech section** page, bottom row "Fixes, Nov 1"
(https://claude.ai/artifact/JVQ8mU7R63Z1cziNpeW9Z5):

- "Tech desk — iPad portrait" (820 px): the fixed sub-page top (fix 1) and the More stories cards (fix 3)
- "Live now — phone, consistent tiles" (390 px): fix 4

## Overview

| # | Fix | Files | Root cause |
|---|---|---|---|
| 1 | Headline sub-pages on iPad | `Pages/News.razor`, `headline-cards.css`, `tablet.css` | `News.razor` (All headlines for News, Sports, Entertainment **and Tech**) never opted in to the tablet layout, and a 744–1179 rule forces Jump to into 3 columns |
| 2 | "Your topics" only in News | `TechFront.razor`, `Pages/TechWeek.razor` | Both Tech pages carry their own Your topics box (desktop and phone) |
| 3 | More stories cards | `headline-cards.css`, `sections.css` | `.td-more-cards` uses the headline cards, whose card layout only starts at 1024. On iPad they fall back to the phone row style: small thumbnail left, everything stacked beside it |
| 4 | Live now tiles on phone | `Phone/PhoneScoreTiles.razor`, `Phone/PhoneText.cs`, `mobile.css`, `SportsFront.razor`, `ScoreboardPanel.razor` | `.mx-score__top` wraps, so the league/clock line is one, two or three lines depending on the label; only quarters are shortened |

Do 1 and 3 together (they share the `headline-cards.css` change), then 2, then 4. Check each on an iPad (or DevTools
at 744, 820 and 834) and a phone at 390 before the next.

> **Not in scope, but you'll see it while testing:** some stories land on the wrong desk. The Tech front's lead in the
> screenshot is "how to watch The Gilded Age", and Video Games shows "Adolescence" and "Everything Coming to Apple TV
> in November". Those sources (TechRadar, IGN, Polygon) mix entertainment into their tech/games feeds. That's a
> categorization fix in the scraper, worth its own note.

---

## 1. Headline sub-pages on iPad (744–1023)

### 1.1 What's wrong

`/tech/news` (the "Tech headlines" screenshot) is `Components/Pages/News.razor`, the same page as `/news/all`,
`/sports/news` and `/entertainment/news`. The Tech front (`TechFront.razor`), the desks (`TechDesk.razor`) and Tech
Weekly (`TechWeek.razor`) already opt in with `tab-desktop` / `tab-phone`; `News.razor` doesn't.

| What you see at 820 | Why |
|---|---|
| A purple ribbon with a full-width white **Section** dropdown | The phone ribbon, `<div class="mx-phone" data-phone-tools="headlines">` (line 23). `mobile.css` shows `.mx-phone` everywhere below 1024 |
| A second **Desk** dropdown | `.news-category-select` (line 38) is only hidden at `min-width: 1024px` (`sections.css`) |
| No desk pills | `.sec-pills--desktop.hn-cats` (line 32) is only shown at `min-width: 1024px` |
| **Jump to** as 3 + 2 with an empty cell | `headline-cards.css` line 141: `@media (min-width: 744px) and (max-width: 1179px) { .hc-jump__row { grid-template-columns: repeat(3, …) } }` |
| The "Jump to" / "Today by desk" labels eating a column | `.hc-jump` is `grid-template-columns: auto minmax(0, 1fr)` at every width from 744 |
| Phone-sized cards | `headline-cards.css` only lays out `.hc-top` / `.hc-grid` / `.hc-card` at 1024+ (lines 151 and 351); below that they're `display: contents` (line 7) |

### 1.2 Opt in (`Components/Pages/News.razor`)

Add `tab-desktop` to the **outer** wrapper (line 21), all three branches. Everything inside then opts in at once: the
phone ribbon hides (`.tab-desktop .mx-phone`), the desktop `PageHeading` shows (`.tab-desktop .mx-desktop`):

```razor
<div class="@(Phone ? "mx-desktop tab-desktop" : SectionNews ? "mx-tool-page mx-section-news tab-desktop" : "mx-contents tab-desktop")">
```

The folded phone front (line 135) is a sibling of that wrapper, so it gets `tab-phone`:

```razor
<div class="mx-phone tab-phone">
```

Make the desk pills (line 32) a swipe strip with the foundation helper (`tab-scroll` only does anything inside
`.tab-desktop`):

```razor
<nav class="sec-pills sec-pills--desktop hn-cats tab-scroll" aria-label="@HubCategoryLabel">
```

> **Why the outer wrapper:** the pills, filters, Jump to and feed sit outside the `PageHeading` wrapper. Opting in
> only the inner div would leave them in their phone state.

### 1.3 Headline cards down to 744 (`wwwroot/headline-cards.css`)

`.hc-*` cards are used in two places only, `News.razor` and `TechDesk.razor`'s More block (checked: `hc-feed` /
`Cards="true"` appear nowhere else), and after 1.2 both are opted in on tablets. So widen instead of copying:

| Line | Block | Change |
|---|---|---|
| 7 | Phone: `.hc-top`, `.hc-grid` as `display: contents` | `(max-width: 1023px)` → `(max-width: 743px)` |
| 151 | Card layout: `.hc-feed`, `.hc-top`, `.hc-grid` (2 columns), `.hc-card` | `(min-width: 1024px)` → `(min-width: 744px)` |
| 336 | `.hc-grid` 3 columns | **leave at 1024**: iPads keep two columns |
| 351 | Top-section arrangements (`& .hc-top`, `& .hc-lead`, `& .hc-under` …) | `(min-width: 1024px)` → `(min-width: 744px)` |

Then fix Jump to in the same file. Replace the `.hc-jump__row` rule inside the 744–1179 block (line ~146) so each
row splits evenly by its own count (five hours, three desks), and delete nothing else there:

```css
@media (min-width: 744px) and (max-width: 1179px) {
    .pub .hc-bar__cat,
    .pub .hc-bar__rule {
        display: none;
    }
    /* Each row splits evenly by its own count: 5 hours, 3 desks. No orphan cell. */
    .pub .hc-jump__row {
        grid-template-columns: none;
        grid-auto-flow: column;
        grid-auto-columns: minmax(0, 1fr);
    }
}
```

> More than ~6 groups in one row (a week grouped by day) would get tight at 744. If that can happen, give
> `.hc-jump__row` `overflow-x: auto` and `grid-auto-columns: minmax(120px, 1fr)` instead.

### 1.4 Tablet styles (`wwwroot/tablet.css`)

Add inside the `@media (min-width: 744px) and (max-width: 1023px)` block, after the Home block (line ~1729 onwards):

```css
    /* ---------- Headline sub-pages: All headlines for News, Sports, Entertainment, Tech (docs/tech-fixes-handoff.md §1) ---------- */

    /* The wrapper: undo mobile.css's full-bleed tool-page frame; the foundation's page frame takes over. */
    .pub .page > .mx-tool-page.tab-desktop {
        display: contents;
        margin: 0;
        padding: 0;
        background: none;
    }

    /* One way to pick a desk: the pills. The phone dropdown goes. */
    .pub .tab-desktop .news-category-select {
        display: none;
    }
    .pub .tab-desktop .hn-cats {
        gap: 8px;
        padding-bottom: 12px;
        border-bottom: 1px solid var(--row-border);
    }
    .pub .tab-desktop .hn-cats a {
        display: inline-flex;
        align-items: center;
        min-height: 44px;
        padding: 0 16px;
    }

    /* Source filter: compact, on one line with the count. */
    .pub .tab-desktop .news-front-filters {
        display: flex;
        flex-wrap: wrap;
        align-items: center;
        gap: 12px;
    }
    .pub .tab-desktop .news-front-filters .catselect select {
        width: 240px;
        min-height: 44px;
    }
    .pub .tab-desktop .news-front-filters .count--toolbar {
        margin-left: auto;
    }

    /* Jump to: labels above their rows, not in a column of their own. */
    .pub .tab-desktop .hc-jump {
        grid-template-columns: minmax(0, 1fr);
        gap: 8px;
    }
    .pub .tab-desktop .hc-jump__label br {
        display: none;                       /* "Today by desk" on one line */
    }
    .pub .tab-desktop .hc-jump__row + .hc-jump__label {
        margin-top: 8px;
    }

    /* Top section: one column. Lead across the width with a shorter photo, the two "under" cards side by side,
       Latest as full-width rows. (The desktop 2fr/1fr split leaves a ~250px side column at 820.) */
    :root:is([data-color-scheme], :not([data-color-scheme])) .pub .tab-desktop .hc-feed .hc-top {
        grid-template-columns: minmax(0, 1fr);
        grid-template-rows: none;
    }
    :root:is([data-color-scheme], :not([data-color-scheme])) .pub .tab-desktop .hc-feed :is(.hc-lead, .hc-under, .hc-latest) {
        grid-column: 1;
        grid-row: auto;
    }
    :root:is([data-color-scheme], :not([data-color-scheme])) .pub .tab-desktop .hc-feed .hc-lead .hc-card [data-article-media] {
        aspect-ratio: 21 / 9;
    }
    :root:is([data-color-scheme], :not([data-color-scheme])) .pub .tab-desktop .hc-feed .hc-lead .hc-card .title {
        font-size: 32px;
    }

    /* The pinned Jump to bar: in line with the page's 24px sides. */
    .pub .hc-bar {
        padding: 0 24px;
    }
```

> `headline-jump.js` already treats 744+ as "desktop" for the pinned bar, so nothing changes there.

### 1.5 Verify (DevTools at 744, 820, 834, then a real iPad)

| Check | Expected |
|---|---|
| `/tech/news`, `/news/all`, `/sports/news`, `/entertainment/news` | No ribbon dropdown; heading, then pills, then Source + count on one line |
| Pills | One row; swipe sideways with a fade; current desk filled |
| Jump to | Five equal hour chips, label above; Today by desk three equal tiles |
| Pinned bar after scrolling | Same as before, 24px sides |
| Cards | Lead full width, then two columns |
| Phone 390 | **Unchanged** (ribbon, dropdowns, folded front) |
| 1024+ | Unchanged apart from Jump to at 1024–1179, which now also splits evenly instead of 3 + 2 |

Add the four routes to `tests/browser/ipad-news-home.browser.cjs` (or a new `ipad-headlines.browser.cjs`): assert
`[data-phone-tools="headlines"]` and `.news-category-select` aren't visible at 820, and `.hn-cats` is.

---

## 2. "Your topics" only in the News section

### 2.1 What's wrong

Two Tech pages have their own Your topics box, on both widths:

| Page | Desktop / iPad | Phone |
|---|---|---|
| `TechFront.razor` | `<section class="nf-side nf-topics" aria-labelledby="tc-topics-title">`, lines ~98–117, in the side column between `<TechJustReleased />` and Tech videos | `<section class="mx-section nf-topics" aria-labelledby="tech-topics-m">`, lines ~240–258, between `<TechJustReleased Compact="true" />` and the Desks `FoldGroup` |
| `Pages/TechWeek.razor` | "Your topics this week", `@if (Reader.Enabled) { <section class="nf-side" aria-labelledby="tw-topics-title"> … }`, lines ~180–210 | `@if (Reader.Enabled && summary.Topics.Count > 0) { … "tw-m-topics-h" … }`, lines ~337–350 |

Nothing else outside News carries one. Sports, Entertainment and Home are clean.

### 2.2 Remove them

**`TechFront.razor`:**

1. Delete the desktop `<section class="nf-side nf-topics" …>…</section>`.
2. Delete the phone `<section class="mx-section nf-topics" …>…</section>`.
3. Delete the load in `OnParametersSetAsync` (line ~335) and the field (line ~306):

```csharp
// delete
topics = Reader.HasProfile
    ? (await TopicQuery.SummariesAsync(db, Reader.Id, nowUtc))
        .Where(t => string.IsNullOrEmpty(t.Topic.Hub) || t.Topic.Hub == "tech")
        .ToList()
    : [];
```

```csharp
// delete
private List<TopicSummary> topics = [];
```

**`Pages/TechWeek.razor`:** delete both `@if` blocks above. Then remove whatever fills `summary.Topics` in the week
query. If the summary record is shared with tests (`TechDesksTests.cs`), leaving the property and returning `[]` is
fine for now.

Then check nothing else still points at them:

```bash
cd src/myNewspaper.Web
grep -rn "nf-topics\|Your topics\|TopicQuery\|news/topics" Components \
  | grep -v "Components/NewsFront.razor" | grep -v "Components/Pages/Topics/"
```

It should print nothing. Remove any Tech-front Your topics assertions from `tests/browser/tech-front.browser.cjs`
and add one that `.nf-topics` isn't on `/tech`.

> **The "Tech only" topic scope stays.** `TopicFollow.razor`'s "Look in → Tech only" choice limits which stories a
> topic matches; it isn't about where the box shows. The News front lists every topic regardless of scope (no hub
> filter on `NewsFront.razor`), so Tech-scoped topics stay visible there.

> **Side column after the change:** Tech videos moves up under Just released. Nothing needs to fill the gap.

### 2.3 Verify

| Page | Your topics |
|---|---|
| `/news` desktop, iPad, phone | **Shown** (unchanged) |
| `/news/topics/{id}`, `/news/topics/follow` | Unchanged |
| `/tech`, `/tech/week`, `/tech/{desk}` | **Not shown**, every width |
| `/sports`, `/entertainment`, `/` | Not shown (unchanged) |

---

## 3. More stories cards (with the article image)

### 3.1 What's wrong

`TechDesk.razor`'s More block (line ~133) renders `<div class="hc-feed td-more-cards"><div class="hc-grid">
<FeedArticles Cards="true" …/></div></div>`. On desktop that's already a grid of image-top cards. On iPad the card
rules don't apply (they start at 1024), so each card falls back to the phone row: a ~190 × 140 thumbnail on the left
and six things stacked beside it (desk kicker, a gap, title, summary, source badge + date, Share/Save on their own
row). A 520 px tall card, mostly empty, and "VIDEO GAMES" repeats the page you're on.

**§1.3 already fixes most of this:** with the card block widened to 744, iPads get the desktop card (16:9 image on
top, one source/time line, two-line summary, actions at the right of the source line) in two columns. What's left is
below.

### 3.2 What changes beyond §1.3 (see "Tech desk — iPad portrait" on the canvas)

| | Desktop today | After |
|---|---|---|
| Card | No frame, image floating on the page | **Framed**: 1px border, surface background, image edge to edge at the top |
| Image | 16:10 | **16:9** (shorter, matches the lead) |
| Desk kicker (`.cat`) | Shown | **Hidden on a desk's own page**; still shown on All headlines |
| No usable photo | Card with a P8 placeholder frame | **Text row** across the grid |
| Columns | 3 at 1024+ inside the desk's main column | 2 on iPad, 3 at 1180+ (the main column is too narrow for 3 at 1024–1179) |

### 3.3 Styles (`wwwroot/sections.css`, after `.pub .td-more-cards .hc-grid` at line ~5232)

Scoped to `.td-more-cards`, so All headlines keeps its own look. Same prefix as `headline-cards.css` so these win:

```css
/* ---------- More {desk}: framed photo cards (docs/tech-fixes-handoff.md §3) ---------- */
@media (min-width: 744px) {
    :root:is([data-color-scheme], :not([data-color-scheme])) .pub .td-more-cards .hc-grid {
        grid-template-columns: repeat(2, minmax(0, 1fr));
        grid-auto-flow: row dense;           /* photo cards stay packed when a text row spans the width */
        gap: 16px;
    }
    :root:is([data-color-scheme], :not([data-color-scheme])) .pub .td-more-cards .hc-card {
        gap: 0;
        overflow: hidden;
        border: 1px solid var(--row-border);
        border-radius: 14px;
        background: var(--surface);
    }
    :root:is([data-color-scheme], :not([data-color-scheme])) .pub .td-more-cards .hc-card [data-article-media] {
        border-radius: 0;
        aspect-ratio: 16 / 9;
    }
    :root:is([data-color-scheme], :not([data-color-scheme])) .pub .td-more-cards .hc-card .body {
        gap: 8px;
        padding: 14px 16px 8px;
    }
    /* The desk's own page already says which desk this is. */
    :root:is([data-color-scheme], :not([data-color-scheme])) .pub .td-more-cards .hc-card .cat {
        display: none;
    }

    /* No usable photo (article-images.js sets data-image-layout="none" and removes the image): a text row. */
    :root:is([data-color-scheme], :not([data-color-scheme])) .pub .td-more-cards .hc-card[data-image-layout='none'] {
        grid-column: 1 / -1;
        border-width: 1px 0 0;
        border-radius: 0;
        background: none;
    }
    :root:is([data-color-scheme], :not([data-color-scheme]))
        .pub
        .td-more-cards
        .hc-card[data-image-layout='none']
        :is([data-article-media], .summary) {
        display: none;
    }
    :root:is([data-color-scheme], :not([data-color-scheme])) .pub .td-more-cards .hc-card[data-image-layout='none'] .body {
        padding: 10px 0;
    }
}
@media (min-width: 1024px) {
    :root:is([data-color-scheme], :not([data-color-scheme])) .pub .td-more-cards .hc-grid {
        grid-template-columns: repeat(2, minmax(0, 1fr));   /* beats headline-cards.css's 3 columns in the narrower main column */
    }
}
@media (min-width: 1180px) {
    :root:is([data-color-scheme], :not([data-color-scheme])) .pub .td-more-cards .hc-grid {
        grid-template-columns: repeat(3, minmax(0, 1fr));
        gap: 20px;
    }
}
```

> **Photo-less selector:** `FeedArticles.razor` (line 42) puts `hc-card`, `data-article-story` and
> `data-image-layout` on the same `<article>`, and `article-images.js` updates that attribute, so
> `.hc-card[data-image-layout='none']` matches directly.
> Cards that render the P8 placeholder (`.ph`) on purpose (no image URL at all) keep the frame; only broken or tiny
> images turn into rows. If you'd rather turn those into rows too, add `.hc-card:has([data-article-media] > .ph)` to
> the row selectors.

> **Feed order:** `dense` packing moves photo cards up past a text row, so rows collect at the bottom. Drop `dense`
> to keep strict feed order.

### 3.4 Verify

| Check | Expected |
|---|---|
| `/tech/video-games` at 820 | Two framed cards per row, each row's cards end together; no "VIDEO GAMES" kicker |
| Same at 1100 | Two per row |
| Same at 1440 | Three per row |
| Phone 390 | **Unchanged** (the phone desk page uses its own `mx-phone tab-phone` markup) |
| A story whose image 404s | Full-width text row |
| `/tech/news` | Cards unchanged by §3 (only §1's changes) |

---

## 4. Live now tiles on phone

### 4.1 What's wrong

From the Sports phone screenshot, the three tiles each format their top differently:

| Tile | League line | Clock | Lines used |
|---|---|---|---|
| College football | "COLLEGE FOOTB…" | "8:00 · 4Q", wrapped below | 2 |
| WNBA | "WNBA" | "5.1 · 4Q", beside it | 1 |
| NHL | "NHL" | "11:35 - 3RD PERIOD", over two lines | 3 |

Causes:

1. `.mx-score__top` (`mobile.css` line 673) has `flex-wrap: wrap`, and `.mx-score__clock` (line 690) has
   `white-space: normal; overflow-wrap: anywhere`. Whether the clock fits beside the league depends on label length.
2. `PhoneText.CompactClock` (line 36) only shortens **quarters**. Periods, halves and innings keep the provider's
   long text and the " - " dash.
3. Tiles use `ScoreboardPanel.CompetitionLabel` ("College football"), sized for headings.
4. Three tiles in a 3-column grid leave ~85px for team names ("Oklahom…", "Atlanta Dr…").

### 4.2 The rules (see "Live now — phone" on the canvas)

- League on **line 1**, clock on **line 2**, always; neither wraps.
- **Short league names** on tiles; the full name stays in the `aria-label`.
- **One clock order:** period first, then time: `4Q 8:00` · `3P 11:35` · `2H 63′` · `Half` · `OT` · `Top 5`.
- **Fixed-width tiles (150px) that scroll sideways**, so names get ~40% more room and more than three live games fit.

### 4.3 Clock and league text (`Components/Phone/PhoneText.cs`)

Replace `CompactClock` and add `ShortPeriod` and `TileLeague`:

```csharp
/// <summary>A live tile's clock, period first: "4Q 8:00", "3P 11:35", "2H 63′", "Half", "OT 2:10". Not live: as Clock.</summary>
public static string CompactClock(ScoreboardGame game, FeedDates dates)
{
    var status = Clock(game, dates);
    if (!game.Active) return status;

    // Providers send "8:00 - 4th Quarter", "11:35 - 3rd Period", "63' - 2nd Half", or just "Halftime" / "Top 5th".
    var split = Regex.Match(status, @"^\s*(?<time>[^-–—]+?)\s*[-–—]\s*(?<period>.+?)\s*$");
    var time = split.Success ? split.Groups["time"].Value.Trim() : null;
    var period = ShortPeriod(split.Success ? split.Groups["period"].Value : status);
    return string.IsNullOrEmpty(time) ? period : $"{period} {time.Replace('\'', '′')}";
}

/// <summary>"4th Quarter" → "4Q", "3rd Period" → "3P", "2nd Half" → "2H", "Halftime" → "Half", "Overtime" → "OT",
/// "Top 5th" → "Top 5", "End of 3rd Quarter" → "End 3Q". Anything else is returned as it came.</summary>
private static string ShortPeriod(string period)
{
    var p = period.Trim();
    p = Regex.Replace(p, @"\bEnd of\b", "End", RegexOptions.IgnoreCase);
    p = Regex.Replace(p, @"\b([1-9])(?:st|nd|rd|th)\s+(Quarter|Period|Half)\b",
        m => m.Groups[1].Value + char.ToUpperInvariant(m.Groups[2].Value[0]), RegexOptions.IgnoreCase);
    p = Regex.Replace(p, @"\b(Top|Bot(?:tom)?|Mid(?:dle)?)\s+([1-9][0-9]?)(?:st|nd|rd|th)\b",
        m => $"{m.Groups[1].Value[..3]} {m.Groups[2].Value}", RegexOptions.IgnoreCase);
    p = Regex.Replace(p, @"^Half\s*time$", "Half", RegexOptions.IgnoreCase);
    p = Regex.Replace(p, @"\bOvertime\b", "OT", RegexOptions.IgnoreCase);
    return p;
}

/// <summary>A league name short enough for a 150px tile. The full CompetitionLabel stays in the aria-label.</summary>
public static string TileLeague(string sport, string league) => league switch
{
    "college-football" => "CFB",
    "mens-college-basketball" => "NCAAM",
    "womens-college-basketball" => "NCAAW",
    "eng.1" => "EPL",
    "eng.2" => "EFL",
    "usa.ncaa.w.1" => "NCAAW Soccer",
    "womens-college-volleyball" => "NCAAW Volleyball",
    _ => ScoreboardPanel.CompetitionLabel(sport, league),
};
```

> **Check against real data first.** Dump a few live `StatusText` values per league before relying on the regex
> (`SELECT DISTINCT League, StatusText FROM dbo.SportsScoreboard WHERE Completed = 0 AND StatusText IS NOT NULL`).
> The patterns match the shapes in your screenshot; for a new shape, add a line to `ShortPeriod`.
> A few unit tests on `CompactClock` (next to the existing `PhoneText` tests, if any) will catch regressions.

> **Abbreviations are a choice.** CFB vs NCAAF, 3P vs P3: change them in one place.

### 4.4 Markup (`Components/Phone/PhoneScoreTiles.razor`)

Two separate lines instead of one wrapping line (replaces the `mx-score__top` span on line 11); the strip class on
the container:

```razor
<div class="mx-scores mx-scores--strip">
    @foreach (var game in Games)
    {
        var href = FeedQuery.SafeHref(game.SourceUrl) ?? "scoreboard";
        var external = FeedQuery.SafeHref(game.SourceUrl) is not null;
        <a class="mx-score" href="@href" target="@(external ? "_blank" : null)" rel="@(external ? "noopener noreferrer" : null)"
           aria-label="@($"{ScoreboardPanel.CompetitionLabel(game.Sport, game.League)}, {PhoneText.Clock(game, Dates)}: {game.AwayName} {ScoreboardGame.Score(game.AwayScore)}, {game.HomeName} {ScoreboardGame.Score(game.HomeScore)}")">
            <span class="mx-score__league" aria-hidden="true">@PhoneText.TileLeague(game.Sport, game.League)</span>
            <span class="mx-score__clock" aria-hidden="true">@PhoneText.CompactClock(game, Dates)</span>
            <span class="mx-score__team" aria-hidden="true"><span>@game.AwayName</span><b>@ScoreboardGame.Score(game.AwayScore)</b></span>
            <span class="mx-score__team" aria-hidden="true"><span>@game.HomeName</span><b>@ScoreboardGame.Score(game.HomeScore)</b></span>
        </a>
    }
</div>
```

Let the strip hold more games:

| File | Line | Change |
|---|---|---|
| `Components/SportsFront.razor` | 184 | `live.Take(3)` → `live.Take(8)` |
| `Components/ScoreboardPanel.razor` | 133 | `live.Take(4)` → `live.Take(8)` |
| `Components/Pages/Home.razor` | ~432 | Already passes all live games; leave it |

### 4.5 Styles (`wwwroot/mobile.css`)

Replace the `.mx-score__top` (line 673), `.mx-score__top > span:first-child` and `.mx-score__clock` (line 690)
rules with:

```css
/* Live tiles: four fixed lines (league, clock, away, home), so every tile is the same height. */
.pub .mx-score {
    display: grid;
    grid-template-rows: 16px 18px 24px 24px;
    row-gap: 4px;
}
.mx-score__league,
.mx-score__clock {
    min-width: 0;
    overflow: hidden;
    text-overflow: ellipsis;
    white-space: nowrap;
    font-weight: 800;
    text-transform: uppercase;
}
.mx-score__league {
    font-size: 11px;
    letter-spacing: 0.08em;
    color: var(--mx-block-3);
}
.mx-score__clock {
    font-size: 12px;
    letter-spacing: 0.04em;
    color: var(--mx-block-accent);
}
.mx-score__team b {
    font-variant-numeric: tabular-nums;   /* 1 and 45 line up across tiles */
}

/* The strip: fixed-width tiles you swipe sideways, running to the screen edge with a fade. */
.mx-scores--strip {
    display: flex;
    gap: 8px;
    margin-right: -16px;                  /* to the screen edge… */
    padding-right: 16px;                  /* …while the last tile can still clear it */
    overflow-x: auto;
    overscroll-behavior-x: contain;
    scroll-snap-type: x proximity;
    scrollbar-width: none;
    -webkit-mask-image: linear-gradient(90deg, #000 calc(100% - 40px), transparent);
    mask-image: linear-gradient(90deg, #000 calc(100% - 40px), transparent);
}
.mx-scores--strip::-webkit-scrollbar {
    display: none;
}
.mx-scores--strip > .mx-score {
    flex: none;
    width: 150px;
    scroll-snap-align: start;
}
.mx-block .mx-scores--strip > .mx-score {
    width: 170px;                          /* the dark lead block's bigger scores */
}
/* One or two games: no fade (nothing to scroll to). */
.mx-scores--strip:not(:has(> :nth-child(3))) {
    -webkit-mask-image: none;
    mask-image: none;
}
```

Keep the existing `.mx-scores`, `.mx-score__team` and `.mx-block .mx-score*` rules. The dark block's
`.mx-score__team b { font-size: 22px }` fits the 24px rows; if a colour scheme's serif runs taller, raise the last two
rows to `26px`.

### 4.6 Verify (390 px, then a real phone)

| Check | Expected |
|---|---|
| College football + WNBA + NHL live | All tiles the same height; league on line 1, clock on line 2 |
| Clocks | `4Q 8:00`, `4Q 5.1`, `3P 11:35` |
| 5+ live games | Strip scrolls sideways with a fade on the right |
| VoiceOver on a tile | Reads the full league name and the provider's full clock |
| Home's dark block, Scoreboard page | Same structure; 170px tiles in the dark block |
| iPad / desktop | Unchanged (they don't render `PhoneScoreTiles`) |

---

## Files touched

| File | Fix |
|---|---|
| `Components/Pages/News.razor` | 1 |
| `wwwroot/headline-cards.css` | 1, 3 |
| `wwwroot/tablet.css` | 1 |
| `Components/TechFront.razor` | 2 |
| `Components/Pages/TechWeek.razor` | 2 |
| `wwwroot/sections.css` | 3 |
| `Components/Phone/PhoneText.cs` | 4 |
| `Components/Phone/PhoneScoreTiles.razor` | 4 |
| `Components/SportsFront.razor`, `Components/ScoreboardPanel.razor` | 4 |
| `wwwroot/mobile.css` | 4 |
| `tests/browser/tech-front.browser.cjs`, an iPad headlines browser test | 1, 2 |
