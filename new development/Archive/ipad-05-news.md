# iPad portrait, part 5: News

**Save as:** `docs/ipad/ipad-05-news.md` · **Needs:** parts 00–04 done and checked. This part reuses part 4's
`.spf-cols` frame and part 3's section tabs.

**Mockup:** **iPad portrait** page → "News — iPad portrait".

**Scope:** `Components/NewsFront.razor`, the News desktop block in `sections.css` and a small block in `tablet.css`.

## What the page looks like at 820

1. "THE DAY'S NEWS, EVERY SOURCE", "News", and the section tabs (News home, All headlines, Top Headlines, Kansas City,
   Opinion, Technology, Video Games, Linux, U.S. News) scrolling sideways.
2. **The briefing:** tiles **two across** (Kansas City now, Election day), with room for more tiles in rows of two.
   Desktop stretches two tiles into full-width bars, which looks empty.
3. **The lead** across the full width: photo on top, kicker, a 36px headline.
4. **Two columns:**
   - Left: **The day so far** (time, headline, source).
   - Right: **Kansas City**, **Opinion** and **Your topics**, stacked.
5. **Top headlines** under the columns, as a two-column list.
6. **In pictures** and **the desks** follow, as on desktop (their `auto-fit` grids settle at two or three across).

## 1. Opt in (markup)

| File | Wrapper | Add |
|---|---|---|
| `NewsFront.razor` line ~13 | `<div class="mx-desktop">` | `tab-desktop` |
| `NewsFront.razor` line ~160 | `<div class="mx-phone">` | `tab-phone` |

NewsFront draws its own heading and tabs, so `SectionHub.razor`'s News branch needs nothing.

**`.nf-brief--phone`** (line ~145) is a phone-only copy of the briefing that sits **inside** the desktop wrapper and
is hidden by CSS on desktop. Check how it's hidden. If it's by `max-width: 1023px` / `min-width: 1024px` rules, hide
it on tablets too: `.pub .tab-desktop .nf-brief--phone { display: none; }`.

## 2. Desktop styles to bring down

The News desktop block is `sections.css` from about line 2425 (the `@media (min-width: 1024px)` after "News front").
**News's phone markup uses `.nf-brief`, `.nf-tile` and `.nf-pictures`**, so don't widen the whole block. Instead:

- **Widen** (to `min-width: 744px`) the rules for `.nf-agate .spf-agate__time`, `.nf-side + .nf-side`,
  `.nf-side .ef-note a` and `.nf-desks`. Move them into a new `@media (min-width: 744px)` block above the 1024 block.
- **Copy** the `.nf-brief`, `.nf-brief .ef-label`, `.nf-brief__tiles`, `.nf-tile`, `.nf-tile__value` and
  `.nf-pictures` rules into `tablet.css`, scoped under `.tab-desktop`.

Part 4 already widened the `.spf-*` rules, and they apply here.

## 3. Tablet styles (`tablet.css`)

Part 4's `.spf-cols` rules already put the lead across the top and The day so far (`.spf-upcoming.nf-day`) on the
left, with the side column (Kansas City, Opinion, Your topics) on the right. News needs one difference: **Top
headlines** (the `.ef-label` + `.ef-headlines` + `.ef-more` in `.spf-main`) runs **under** both columns at full width,
instead of continuing the left column, because The day so far is already a long list.

```css
    /* ---------- News (docs/ipad/ipad-05-news.md) ---------- */
    /* The briefing: tiles two across. */
    .pub .tab-desktop .nf-brief__tiles {
        display: grid;
        grid-template-columns: repeat(2, minmax(0, 1fr));
        gap: 12px;
    }

    /* Top headlines below both columns, as a two-column list. */
    .pub .tab-desktop .spf-cols:has(.nf-day) > .spf-side {
        grid-row: 2;                               /* only beside The day so far */
    }
    .pub .tab-desktop .spf-cols:has(.nf-day) > .spf-main > :not(.ef-lead) {
        grid-column: 1 / -1;
        padding-right: 0;
    }
    .pub .tab-desktop .spf-cols:has(.nf-day) > .spf-main > .ef-label {
        margin-top: 24px;
        padding-top: 4px;
        border-top: 1px solid var(--text);
    }
    .pub .tab-desktop .spf-cols:has(.nf-day) .ef-headlines {
        columns: 2;
        column-gap: 28px;
    }
    .pub .tab-desktop .spf-cols:has(.nf-day) .ef-headlines > li {
        break-inside: avoid;
    }

    /* The day so far: time | headline + source. */
    .pub .tab-desktop .nf-agate a {
        display: grid;
        grid-template-columns: 58px minmax(0, 1fr);
        gap: 8px;
    }

    /* Side column: the three boxes stacked with room between. */
    .pub .tab-desktop .spf-cols:has(.nf-day) > .spf-side {
        display: flex;
        flex-direction: column;
        gap: 24px;
    }
    .pub .tab-desktop .nf-side + .nf-side {
        padding-top: 0;
        border-top: 0;
    }

    /* In pictures and the desks: two across. */
    .pub .tab-desktop .nf-pictures__grid {
        grid-template-columns: repeat(2, minmax(0, 1fr));
    }
    .pub .tab-desktop .nf-desks {
        grid-template-columns: repeat(2, minmax(0, 1fr));
    }
```

**`:has(.nf-day)`** limits these overrides to News's frame, so Sports (part 4) keeps headlines in its left column.

**The 4K guide** gave `.nf-side + .nf-side` a left rule for side-by-side boxes from 1920. On tablets they stack, so
the rule above resets them to plain stacking.

**Your topics' "Follow a topic"** should be a 44px button-shaped link. If the desktop style is a plain text link,
give it `display: inline-flex; min-height: 44px; align-items: center; padding: 0 14px; border: 1px solid var(--accent);
border-radius: 10px;` under `.tab-desktop .nf-topics`.

## 4. Verify

1. **744, 820, 834px:**
   - Tabs scroll sideways, and on Linux or U.S. News the current tab is scrolled into view (part 3's script).
   - Briefing tiles are two across. The lead runs full width.
   - The day so far sits on the left, Kansas City / Opinion / Your topics on the right, and they end close together.
   - Top headlines runs full width in two columns, then In pictures and the desks.
2. **Your topics:** follow a topic from the tablet. The form works and the topic appears.
3. **Sports** (part 4) still has its headlines in the left column. **Entertainment** (part 3) is unchanged.
4. **Phones:** unchanged, including the phone briefing (`.nf-brief--phone`). **Desktop:** unchanged.

Then do `ipad-06-home.md`.
