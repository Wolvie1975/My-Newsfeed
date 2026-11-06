# Tech front on wide desktops: the desks beside the lead, Just released and videos on the right

Written November 1, 2026 (America/Chicago), against `develop` at `75d6e5f`. Every file, class and line number below
was checked against that commit.

**Save as:** `docs/tech/tech-05-front-wide.md`

**Mockup:** the myNewspaper canvas, **Tech section** page, "Tech front — 2000 px, simplified"
(https://claude.ai/artifact/JVQ8mU7R63Z1cziNpeW9Z5).

> **Replaces** `tech-front-panels-handoff.md` and `scraper-tech-panels-handoff.md` (Your stack, Your servers, New in
> Ollama). Park those; nothing here needs the scraper.

## What changes

**CSS only, 1920 px and up.** No markup moves, so phones, iPad portrait and 1024–1919 are untouched.

| Column | Today at 2000 px | After |
|---|---|---|
| 1 | The day so far | unchanged |
| 2 | Lead · Most covered · Latest headlines | Lead · Most covered (Latest headlines hidden: the desks show the same newest stories) |
| 3 | Tech videos, ~970 px wide and mostly empty | **Technology · Video Games · Linux**, stacked (moved up from the full-width row under the columns) |
| 4 | (part of the same wide side column) | **Just released** (when there's a release in the last 48 h) · **Tech videos** |

The briefing tiles and "This week in Tech" stay full width, above and below.

## How

`TechFront.razor`'s desktop markup (lines ~15–157) is:

```
.ef.spf.nf.tc-front
├── section.nf-brief            (briefing tiles)
├── div.spf-cols
│   ├── section.spf-upcoming.nf-day   (The day so far)
│   ├── div.ef-main.spf-main          (lead, Most covered, Latest headlines)
│   └── aside.ef-side.spf-side        (TechJustReleased, Tech videos)
├── div.nf-desks                (Technology, Video Games, Linux)
└── p.tc-week-banner            (This week in Tech)
```

At 1920+, `.tc-front` becomes the grid and `.spf-cols` dissolves (`display: contents`), so its three children and
`.nf-desks` sit in one row of four columns. The shared 1920 rule that splits `.spf-side` into two inner columns
(`sections.css` line ~4843) is undone for the Tech front only; News and Sports keep it.

## CSS (`wwwroot/sections.css`, after the "News and Sports fronts, 1920px and up" block, line ~4870)

```css
/* ---------- Tech front, 1920px and up: day so far · lead · desks · Just released + videos (docs/tech/tech-05-front-wide.md) ---------- */
@media (min-width: 1920px) {
    .pub .tc-front {
        display: grid;
        grid-template-columns: 300px minmax(0, 1.5fr) minmax(0, 1fr) minmax(0, 1fr);
        align-items: start;
    }
    .pub .tc-front > :is(.nf-brief, .tc-week-banner) {
        grid-column: 1 / -1;
    }
    .pub .tc-front > .spf-cols {
        display: contents;          /* its three children join the .tc-front grid */
    }

    /* Row 2: the four columns. */
    .pub .tc-front > .spf-cols > .spf-upcoming {
        grid-column: 1;
        grid-row: 2;
        padding-top: 20px;
    }
    .pub .tc-front > .spf-cols > .spf-main {
        grid-column: 2;
        grid-row: 2;
        padding: 20px 26px 0;
        border-left: 1px solid var(--row-border);
    }
    .pub .tc-front > .nf-desks {
        grid-column: 3;
        grid-row: 2;
        display: flex;
        flex-direction: column;
        gap: 24px;
        margin: 0;
        padding: 20px 24px 0;
        border-top: 0;
        border-left: 1px solid var(--row-border);
    }
    .pub .tc-front > .spf-cols > .spf-side {
        grid-column: 4;
        grid-row: 2;
        display: flex;              /* undo the shared two-inner-column split */
        flex-direction: column;
        gap: 26px;
        padding: 20px 0 0 24px;
        border-left: 1px solid var(--row-border);
    }
    .pub .tc-front > .spf-cols > .spf-side .nf-side + .nf-side {
        padding-left: 0;            /* the shared rule indents the second box as a second column */
        border-left: 0;
    }
    .pub .tc-front > .tc-week-banner {
        grid-row: 3;
    }

    /* Latest headlines: the desks beside the lead already show the newest stories. */
    .pub .tc-front .spf-main :is(.tc-latest-grid, .ef-label:has(+ .tc-latest-grid)) {
        display: none;
    }

    /* Tech videos in a ~460px column: thumbnail above the text rather than beside it. */
    .pub .tc-front .tc-videos .yt-row {
        display: flex;
        flex-direction: column;
        gap: 8px;
    }
    .pub .tc-front .tc-videos .yt-row__thumb {
        width: 100%;
        aspect-ratio: 16 / 9;
    }
}
```

Two things to check against the live CSS:

| Check | Why |
|---|---|
| `.spf-side .nf-side + .nf-side` (line ~4867) sets more than `padding-left` and `border-left` | Undo whatever else it sets for `.tc-front` too, so Tech videos sits straight under Just released |
| `.yt-row` / `.yt-row__thumb` already have a fixed width or grid at 1920 | The thumbnail override needs to beat it; add the `:root:is([data-color-scheme], …)` prefix if those rules use it |

> **2560 and up:** the 2560 `.spf-cols` rule (line ~4858) no longer affects the Tech front, since `.spf-cols` is
> `display: contents` there. The four columns just get wider. If you want more room for the lead on 4K, change the
> second column to `minmax(0, 1.8fr)` in a 2560 block.

## Verify

| Width | Expected |
|---|---|
| 2000 | Four columns: day so far · lead + Most covered · Technology / Video Games / Linux stacked · Just released (if any) + Tech videos. No full-width desk row under them; This week in Tech below |
| 2000, no release in 48 h | Tech videos alone in column 4 (Just released hides itself, as today) |
| 2560 | Same, wider |
| 1440 | Exactly as today (desks as a full-width row under the columns; Latest headlines shown) |
| iPad portrait, phone | Unchanged |
| News and Sports fronts at 2000 | Unchanged (their side column still splits in two) |

## Files touched

| File | Change |
|---|---|
| `wwwroot/sections.css` | The one block above |
| `tests/browser/tech-front.browser.cjs` | Optional: at 2000 px, `.nf-desks` shares a top offset with `.spf-main` |
