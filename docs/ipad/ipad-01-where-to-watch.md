# iPad portrait, part 1: Where to watch

**Save as:** `docs/ipad/ipad-01-where-to-watch.md` · **Needs:** `ipad-00-foundation.md` done and checked.

**Mockup:** **iPad portrait** page → "Where to watch — iPad portrait".

**Scope:** `Components/Pages/Watch.razor`, `Components/Phone/PhoneWatch.razor` and one block in `tablet.css`. No
other page changes. This is the simplest page: the base styles already stack the search in its blue panel, and the
poster shelves already scroll sideways. The work is mainly opting in and tidying.

## What the page looks like at 820

1. Heading: "MOVIES & TV" eyebrow, "Where to watch" at 48px, the one-line description.
2. **Search at full width** in the blue panel, with the label "Movie or TV title", a 48px field and the yellow Search
   button. On desktop it shares a row with the title, which would be cramped here.
3. Under a double rule: **Featured / Movies / TV shows** tabs on the left, then **your services** on the right as a
   compact row (six logos, "+12", "Manage →").
4. Each list (Popular on your services, Trending movies, Trending TV) is **one row of posters you swipe sideways**,
   about 4½ visible, with a fade at the right edge. The service badges stay on each poster. There are no arrow
   buttons: on a touchscreen you swipe.

## 1. Opt in (markup)

| File | Wrapper | Add |
|---|---|---|
| `Watch.razor` line ~8 | `<div class="mx-desktop">` (heading + controls) | `tab-desktop` |
| `Watch.razor` line ~51 | `<div class="mx-desktop">` (the rows / results) | `tab-desktop` |
| `Watch.razor` line ~31 | `<div class="mx-phone" data-phone-watch-ribbon>` | `tab-phone` |
| `PhoneWatch.razor` line 2 | `<div class="mx-phone" data-phone-life="watch">` | `tab-phone` |

For example: `<div class="mx-desktop tab-desktop">`. Search for any other `mx-phone` in `Watch.razor` and tag it the
same way.

## 2. Desktop styles to bring down

Most of this page's look comes from base rules (`public.css` around 7065–7170, `sections.css` around 1450,
`.watch-page .watch-controls`), so they already apply on a tablet. Copy only these from the `min-width: 1024px`
blocks into `tablet.css`, scoped to `.tab-desktop`:

- `.watch-shelf`: `grid-auto-columns` and `gap`. The tablet values are below.
- Whatever `min-width: 1024px` rule draws the **double rule and browse bar** under the search. Look near the
  `sections.css` 1450 block ("the browse bar runs under both") and copy its `border-top` / `padding`.

Leave the arrow rules (`.watch-row--js .watch-row__arrow`) on desktop only.

## 3. Tablet styles (`tablet.css`)

```css
    /* ---------- Where to watch (docs/ipad/ipad-01-where-to-watch.md) ---------- */
    .pub .watch-page .tab-desktop .page-heading > p:last-child {
        display: block;                              /* keep the description (the <= 600px rule hides it) */
    }
    :root:is([data-color-scheme], :not([data-color-scheme])) .pub .watch-page .tab-desktop .watch-controls {
        display: grid;
        grid-template-columns: minmax(0, 1fr) auto;
        align-items: center;
        gap: 14px 16px;
    }
    .pub .watch-page .tab-desktop .watch-search {
        grid-column: 1 / -1;                         /* full width */
    }
    .pub .watch-page .tab-desktop .watch-filters,
    .pub .watch-page .tab-desktop .watch-mine {
        border-top: 4px double var(--text);
        padding-top: 6px;
    }
    .pub .watch-page .tab-desktop .watch-mine {
        justify-self: end;
    }
    .pub .watch-page .tab-desktop .watch-mine__logos li {
        width: 34px;
        height: 34px;
    }
    .pub .watch-page .tab-desktop .watch-controls-note {
        grid-column: 1 / -1;
    }

    /* Shelves: swipe, no arrows, run to the screen edge, fade on the right. */
    .pub .watch-page .tab-desktop .watch-shelf {
        grid-auto-columns: 150px;
        gap: 16px;
        margin-inline: -24px;
        padding: 2px 24px 10px;
        scroll-padding-inline: 24px;
        scroll-snap-type: x proximity;
        scrollbar-width: none;
        -webkit-mask-image: linear-gradient(90deg, #000 calc(100% - 56px), transparent);
        mask-image: linear-gradient(90deg, #000 calc(100% - 56px), transparent);
    }
    .pub .watch-page .tab-desktop .watch-row__arrow {
        display: none !important;
    }
    .pub .watch-page .tab-desktop .watch-row__head h2 {
        font-size: 26px;
    }
```

`.tab-desktop` is a wrapper with `display: contents` inside `.watch-page`. That's why the selectors read
`.watch-page .tab-desktop …`.

**Search results** (after searching a title) use `.watch-rows` / `.watch-card.watch-result` in the second desktop
wrapper. Check that a results page reads well at 820. If the result cards are a multi-column grid, two across is
right for this width:
`.pub .watch-page .tab-desktop .watch-results-list { grid-template-columns: repeat(2, minmax(0, 1fr)); }`
(use the actual list class).

## 4. Verify

1. **744, 820, 834px:**
   - Heading, then full-width search; tabs and services on one row under a double rule.
   - Shelves swipe with a finger and stop on a poster edge (snap). The fade shows at the right, there are no arrow
     buttons, and the first poster lines up with the heading.
2. **Search for a title:** the results page is readable, and Sort and the service filters work.
3. **Movies and TV shows tabs,** and "Manage →", work.
4. **Phones (390px):** unchanged phone page (ribbon + PhoneWatch). **1024px+:** unchanged desktop, arrows included.
5. **Dark mode:** the blue panel and badges are readable.
6. **Other pages on the iPad** still show their phone layout under the new header.

Then do `ipad-02-youtube.md`.
