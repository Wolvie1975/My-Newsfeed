# iPad portrait, part 4: Sports

**Save as:** `docs/ipad/ipad-04-sports.md` · **Needs:** parts 00–03 done and checked. Part 3 added the shared
section-tab rules.

**Mockup:** **iPad portrait** page → "Sports — iPad portrait".

**Scope:** `Components/Pages/SectionHub.razor` (Sports heading only), `Components/SportsFront.razor`, the Sports desktop
block in `sections.css` and one block in `tablet.css`. The layout rules here are written so part 5 (News, which uses
the same `.spf-cols` frame) can reuse them.

## What the page looks like at 820

1. Heading "Sports" with its eyebrow, and the section tabs scrolling sideways (from part 3).
2. **Today's scores:** a strip of score cards you swipe sideways, about three visible, each 236px. The six boxes
   squeezed across a row on desktop don't fit here. "Full scoreboard →" and the "Showing 6 of 19" note stay.
3. **The lead** across the full width: photo on top (16:9), kicker, a 36px headline.
4. **Two columns:**
   - Left: Upcoming games (agate), then Latest headlines.
   - Right: the standings table and the Top 25, each **cut to 10 rows** with its "All standings" / "Top 25" link.

## 1. Opt in (markup)

| File | Wrapper | Add |
|---|---|---|
| `SectionHub.razor`, Sports branch | `<div class="mx-desktop"><PageHeading Section="sports" …/></div>` | `tab-desktop` |
| `SportsFront.razor` line ~10 | `<div class="mx-desktop">` | `tab-desktop` |
| `SportsFront.razor` line ~151 | `<div class="mx-phone">` | `tab-phone` |

`ScoreboardPanel` inside `.spf-scores` has its own `mx-desktop` / `mx-phone` halves. The foundation's
`.tab-desktop .mx-desktop` / `.tab-desktop .mx-phone` rules handle them, so there's nothing to tag inside it.

## 2. Desktop styles to bring down

Sports's phone markup uses none of the `spf-` / `ef-` classes, so **widen** the whole Sports desktop block in
`sections.css` (from about line 2182, the `@media (min-width: 1024px)` after "Sports front") to `min-width: 744px`.
It covers `.spf-scores…`, `.spf-cols`, `.spf-upcoming`, `.spf-agate…`, `.spf-main…`, `.spf-side`,
`.spf-standings`, `.spf-rankings` and `.spf-table`.

**News uses the same classes,** but News's desktop markup stays hidden on tablets until part 5, so widening now is
safe for it.

The `.ef-*` rules that `.spf-main` relies on (`.ef-lead`, `.ef-head`, `.ef-label`) came down in part 3. Check that
part 3 covered `.ef-lead__image` and `.ef-headlines`. If it only copied them under the Entertainment block's
selectors, its `.pub .tab-desktop .ef-…` copies apply here too.

## 3. Tablet styles (`tablet.css`)

```css
    /* ---------- Sports and News fronts: the shared .spf-cols frame (docs/ipad/ipad-04-sports.md) ---------- */
    /* .spf-cols = .spf-upcoming | .ef-main.spf-main (lead, label, headlines, more link) | .ef-side.spf-side
       Tablet: lead across both columns; Upcoming + headlines on the left; the side column on the right. */
    .pub .tab-desktop .spf-cols {
        grid-template-columns: minmax(0, 1fr) minmax(0, 1fr);
        grid-auto-flow: row dense;
    }
    .pub .tab-desktop .spf-main {
        display: contents;
    }
    .pub .tab-desktop .spf-cols .ef-lead {
        grid-column: 1 / -1;
        grid-row: 1;
        padding: 0 0 20px;
        margin-bottom: 4px;
        border: 0;
        border-bottom: 1px solid var(--text);
    }
    .pub .tab-desktop .spf-cols > .spf-upcoming {
        grid-column: 1;
        grid-row: 2;
        padding: 0 20px 0 0;
        border: 0;
    }
    .pub .tab-desktop .spf-cols > .spf-main > :not(.ef-lead) {
        grid-column: 1;
        padding-right: 20px;
    }
    .pub .tab-desktop .spf-cols > .spf-side {
        grid-column: 2;
        grid-row: 2 / span 5;
        padding: 0 0 0 20px;
        border-left: 1px solid var(--row-border);
    }
    .pub .tab-desktop .spf-main .ef-lead__image {
        height: auto;
        aspect-ratio: 16 / 9;
    }
    .pub .tab-desktop .spf-main .ef-lead__title {
        font-size: 36px;
        line-height: 1.08;
    }

    /* ---------- Sports only ---------- */
    /* Today's scores: swipe sideways, about three cards visible. */
    :root:is([data-color-scheme], :not([data-color-scheme])) .pub .tab-desktop .spf-scores .scoreboard-games {
        display: flex;
        flex-wrap: nowrap;
        gap: 12px;
        margin-inline: -24px;
        padding: 2px 24px 10px;
        overflow-x: auto;
        scroll-snap-type: x proximity;
        scroll-padding-inline: 24px;
        scrollbar-width: none;
        -webkit-mask-image: linear-gradient(90deg, #000 calc(100% - 56px), transparent);
        mask-image: linear-gradient(90deg, #000 calc(100% - 56px), transparent);
    }
    :root:is([data-color-scheme], :not([data-color-scheme])) .pub .tab-desktop .spf-scores .scoreboard-game {
        flex: 0 0 236px;
        scroll-snap-align: start;
    }
    /* Standings and Top 25: first 10 rows; the section's link goes to the full table. */
    .pub .tab-desktop .spf-standings .spf-table tbody tr:nth-child(n + 11),
    .pub .tab-desktop .spf-rankings .spf-table tbody tr:nth-child(n + 11) {
        display: none;
    }
    .pub .tab-desktop .spf-side {
        display: flex;
        flex-direction: column;
        gap: 24px;
    }
```

**Why `grid-row: 2 / span 5`:** the left column is Upcoming (row 2), then the headlines label, list and "All sports
headlines" link (rows 3–5), plus one spare row for a status message. The side column spans all of them, so the two
columns sit side by side whatever their heights.

**The favourite-team highlight** in the standings (Kansas City Current) already comes from the desktop styles. Check
that it's still visible when the row is in the top 10. If a favourite team is **below** row 10, keep its row visible:
`.pub .tab-desktop .spf-table tbody tr.is-favorite { display: table-row; }` (use the class the table actually puts on
that row).

**`data-sports-rotation`:** the side column rotates which standings it shows. The rotation script swaps content, not
layout, so it keeps working. Check one rotation at 820.

## 4. Verify

1. **744, 820, 834px:**
   - Score cards swipe, about three visible, and live scores refresh in place (the 10-minute refresh replaces the
     panel, and the strip keeps working afterwards).
   - The lead runs full width. Upcoming + headlines sit on the left, standings + Top 25 on the right, each with 10
     rows and a working link. The columns end close together.
2. **"Full scoreboard →", "Schedule →", "All standings →"** work by touch.
3. **Desktop 1024 / 1280:** unchanged. The widened block still applies there (744 and up includes 1024 and up).
4. **Phones:** unchanged. **News** (not opted in) still shows its phone layout on the iPad, and Entertainment still
   looks as it did after part 3.

Then do `ipad-05-news.md`.
