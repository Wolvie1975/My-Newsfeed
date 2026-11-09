# iPad portrait, part 0: foundation (do this first)

Written October 30, 2026 (America/Chicago), against `develop` at `472c8f9`.

**Save as:** `docs/ipad/ipad-00-foundation.md`

**Mockups:** the myNewspaper canvas, **iPad portrait** page (https://claude.ai/artifact/JVQ8mU7R63Z1cziNpeW9Z5).
The plan board explains the approach. The six page boards are drawn at 820 CSS px.

## How this set of guides works

There are seven guides, and each one is a separate piece of work. Do them in order, and stop after each one to check
it on an iPad (or in DevTools at 744, 820 and 834px) before starting the next:

| Guide | What it does |
|---|---|
| **00 foundation** (this one) | The tablet stylesheet, a per-page switch, the two-row header and shared helpers. **No page changes how it looks yet**, apart from the header |
| 01 Where to watch | Simplest page: proves the switch works |
| 02 YouTube | |
| 03 Entertainment | |
| 04 Sports | |
| 05 News | |
| 06 Home | Most sections: last |

Only the page in hand moves to the tablet layout. Every other page keeps today's phone layout on an iPad until its
own guide is done, so the site never ends up half-converted.

## What changes, and what doesn't

| Width (CSS px) | Device | Today | After all seven guides |
|---|---|---|---|
| Under 744 | Phones | Phone layout | **Unchanged** |
| **744–1023** | iPad mini, iPad, iPad Air, iPad Pro 11″ in portrait | Phone layout, stretched | **Tablet layout:** the desktop markup with tablet styling (two columns, sideways-scrolling strips) |
| 1024 and up | iPad landscape, iPad Pro 13″ portrait, desktop | Desktop | **Unchanged** |

The tablet layout uses each page's **desktop markup** (`.mx-desktop`), not the phone markup (`.mx-phone`). The
desktop markup has the richer sections (two-column fronts, standings, the full YouTube page). The phone markup stays
for phones only.

---

## 1. The tablet stylesheet

Create `src/myNewspaper.Web/wwwroot/tablet.css` and load it **after** `headline-cards.css` in
`Components/App.razor`, so it overrides everything before it:

```razor
<link rel="stylesheet" href="@Assets["headline-cards.css"]" />
<link rel="stylesheet" href="@Assets["tablet.css"]" />
```

Every rule in `tablet.css` goes inside one range:

```css
/* =====================================================================================================================
   iPad portrait: 744–1023 CSS px (docs/ipad/). Phones (< 744) and desktop (>= 1024) never see these rules.
   Page rules are scoped to .tab-desktop, so they only apply on pages that have opted in (section 2).
   ===================================================================================================================== */
@media (min-width: 744px) and (max-width: 1023px) {

    /* section 2: the per-page switch */
    /* section 3: the header */
    /* section 4: shared helpers */
    /* each page guide adds its own block here, with a comment naming the page */

}
```

## 2. The per-page switch

Today `mobile.css` shows `.mx-phone` and hides `.mx-desktop` everywhere below 1024px. A page **opts in** to the tablet
layout by adding one class to each of its wrappers:

- `tab-desktop` on its `.mx-desktop` wrapper(s);
- `tab-phone` on its `.mx-phone` wrapper(s).

Each page guide lists the exact wrappers. Add this to `tablet.css`:

```css
    /* ---------- The switch: opted-in pages show their desktop markup on a tablet ---------- */
    .mx-desktop.tab-desktop {
        display: contents !important;        /* beats mobile.css's `.mx-desktop { display: none !important }` (more specific) */
    }
    .mx-phone.tab-phone {
        display: none !important;
    }
    /* Shared components with their own phone/desktop halves (e.g. ScoreboardPanel) follow the page they sit in. */
    .tab-desktop .mx-desktop {
        display: contents !important;
    }
    .tab-desktop .mx-phone {
        display: none !important;
    }
    /* The page frame on an opted-in page: 24px sides, no 1280 cap. */
    .pub .page:has(.tab-desktop) {
        max-width: none;
        padding: 16px 24px 40px;
        gap: 16px;
    }
```

`:has()` is supported in Safari 15.4 and later, which covers every iPad that runs a current browser.

### 2.1 Getting the desktop styles at tablet widths

Most desktop styling sits in `@media (min-width: 1024px)` blocks, so on a tablet the desktop markup starts with only
its base styles. Each page guide says which of that page's blocks to bring down. There are two ways to do it:

1. **Widen the block to `min-width: 744px`.** Only do this if **every selector in the block** targets classes that
   exist only inside desktop markup (`.mx-desktop`). Pages that haven't opted in hide their desktop markup on tablets,
   so widening such a block changes nothing for them.
2. **Otherwise, copy the declarations you need into `tablet.css`,** scoped under `.tab-desktop` (e.g.
   `.pub .tab-desktop .section-tabs a { … }`). Use this for anything shared with the phone markup or the header:
   `.section-tabs`, `.sec-pills`, `.d-card`, `.site-header` and so on.

When in doubt, copy. Copying never affects a page that hasn't opted in.

Keep the `:root:is([data-color-scheme], :not([data-color-scheme]))` prefix wherever the desktop rule has it. Without
it the copied rule loses to the colour-scheme rules.

## 3. The header (all pages, 744–1023)

The header is shared, so this applies to every page on an iPad, opted in or not. It becomes two rows:

- **Row 1:** logo tile + "myNewspaper" wordmark, then the icons: Where to watch search, Saved, Settings, theme,
  colour scheme, and your edition (initial only).
- **Row 2:** the six sections as a row of tabs, with the current one underlined. This replaces the "Menu" disclosure
  (`.mobile-nav`), which stays for phones.

Admin sign in and the header clock are hidden at this width. Admin sign in is still in Settings.

### 3.1 Markup (`Components/Layout/PublicLayout.razor`)

Add a search link as the first child of `.header-actions`. It goes to the Where to watch page, where the search box
is. That's simpler than an expanding field and works without JavaScript:

```razor
<a class="header-search-link" href="watch" aria-label="Where to watch search">
    <svg width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" aria-hidden="true"><circle cx="11" cy="11" r="7"/><path d="m20 20-3.5-3.5"/></svg>
</a>
```

In `public.css`, outside any media query: `.pub .header-search-link { display: none; }`.

### 3.2 Styles (`tablet.css`)

The phone header rules in `broadsheet.css` (`@media (max-width: 1023px)`, around line 1404) use the long
`:root:is(…):is(…)` prefix, so these rules need it too:

```css
    /* ---------- Header: two rows ---------- */
    :root:is([data-color-scheme], :not([data-color-scheme])):is([data-theme], :not([data-theme])) .pub .site-header {
        flex-wrap: wrap;
        gap: 0 8px;
        min-height: 0;
        height: auto;
        padding: 6px 16px 0 24px;
    }
    :root:is([data-color-scheme], :not([data-color-scheme])):is([data-theme], :not([data-theme])) .pub .brand {
        display: inline-flex;
        width: auto;
        gap: 10px;
        min-height: 48px;
    }
    :root:is([data-color-scheme], :not([data-color-scheme])):is([data-theme], :not([data-theme])) .pub .brand-mark,
    :root:is([data-color-scheme], :not([data-color-scheme])):is([data-theme], :not([data-theme])) .pub .header-clock,
    :root:is([data-color-scheme], :not([data-color-scheme])):is([data-theme], :not([data-theme])) .pub .mobile-nav,
    :root:is([data-color-scheme], :not([data-color-scheme])):is([data-theme], :not([data-theme])) .pub .header-watch,
    :root:is([data-color-scheme], :not([data-color-scheme])):is([data-theme], :not([data-theme])) .pub .header-actions > .admin-link,
    :root:is([data-color-scheme], :not([data-color-scheme])):is([data-theme], :not([data-theme])) .pub .edition-chip__name {
        display: none;
    }
    :root:is([data-color-scheme], :not([data-color-scheme])):is([data-theme], :not([data-theme])) .pub .brand-tile {
        display: block;
        flex: none;
    }
    :root:is([data-color-scheme], :not([data-color-scheme])):is([data-theme], :not([data-theme])) .pub .brand-wordmark {
        display: inline;
        font-size: 22px;
    }
    :root:is([data-color-scheme], :not([data-color-scheme])):is([data-theme], :not([data-theme])) .pub .header-actions {
        margin-left: auto;
        gap: 4px;
    }
    :root:is([data-color-scheme], :not([data-color-scheme])):is([data-theme], :not([data-theme])) .pub .header-actions > .settings-link {
        display: inline-flex;
        align-items: center;
        min-height: 44px;
        padding: 0 8px;
    }
    .pub .header-search-link {
        display: grid;
        place-items: center;
        width: 44px;
        height: 44px;
        border-radius: 10px;
        color: inherit;
    }
    /* Row 2: the sections. */
    :root:is([data-color-scheme], :not([data-color-scheme])):is([data-theme], :not([data-theme])) .pub .site-nav--inline {
        display: flex;
        order: 10;
        flex: 0 0 100%;
        gap: 0 22px;
        margin: 0;
        padding: 0;
        overflow-x: auto;
        scrollbar-width: none;
    }
    :root:is([data-color-scheme], :not([data-color-scheme])):is([data-theme], :not([data-theme])) .pub .site-nav--inline a {
        display: inline-flex;
        align-items: center;
        min-height: 44px;
        padding: 0 2px;
        border-bottom: 3px solid transparent;
        font-size: 15px;
        font-weight: 600;
        white-space: nowrap;
    }
    :root:is([data-color-scheme], :not([data-color-scheme])):is([data-theme], :not([data-theme])) .pub .site-nav--inline a[aria-current] {
        border-bottom-color: currentColor;
        font-weight: 700;
    }
```

The six sections take about 480px, so they fit on one row at 744 without scrolling. The `overflow-x: auto` is there
in case someone uses large system text.

**Sticky height:** the header grows from 64px to about 104px. Anything that offsets for it (scroll padding, the
pinned Jump to bar from the headline cards guide) should use the header's measured height rather than a fixed 64. If
something uses a fixed 64, add a tablet override of 104.

## 4. Shared helpers (`tablet.css`)

Three patterns repeat across the pages. Define them once here, and each page guide applies them to its own classes.

```css
    /* ---------- Helper: a strip you swipe sideways (scores, posters, pills, section tabs) ---------- */
    .pub .tab-desktop .tab-scroll {
        display: flex;
        flex-wrap: nowrap;
        gap: 12px;
        margin-inline: -24px;              /* runs to the screen edge... */
        padding: 2px 24px 10px;            /* ...but starts in line with the page */
        overflow-x: auto;
        overscroll-behavior-x: contain;
        scroll-snap-type: x proximity;
        scroll-padding-inline: 24px;
        scrollbar-width: none;
        -webkit-mask-image: linear-gradient(90deg, #000 calc(100% - 56px), transparent);   /* the fade says "there's more" */
        mask-image: linear-gradient(90deg, #000 calc(100% - 56px), transparent);
    }
    .pub .tab-desktop .tab-scroll::-webkit-scrollbar { display: none; }
    .pub .tab-desktop .tab-scroll > * {
        flex: none;
        scroll-snap-align: start;
    }

    /* ---------- Helper: two equal columns with a rule between ---------- */
    .pub .tab-desktop .tab-two {
        display: grid;
        grid-template-columns: minmax(0, 1fr) minmax(0, 1fr);
        gap: 0;
        align-items: start;
    }
    .pub .tab-desktop .tab-two > * {
        min-width: 0;
    }
    .pub .tab-desktop .tab-two > :first-child { padding-right: 20px; }
    .pub .tab-desktop .tab-two > :last-child  { padding-left: 20px; border-left: 1px solid var(--row-border); }

    /* ---------- Helper: touch targets ---------- */
    .pub .tab-desktop :is(a, button, select, summary):not(.tab-inline) {
        min-height: 44px;
    }
```

The page guides mostly **apply** these with the page's own selectors. For example
`.pub .tab-desktop .ef-screen__list` gets the same declarations as `.tab-scroll`, so the markup barely changes. Where
the markup is easy to touch, adding the class (`class="ef-screen__list tab-scroll"`) is fine too.

**The touch-target rule:** some inline links (a headline inside a sentence, a link in fine print) shouldn't be 44px
tall. Give those `class="tab-inline"`, or narrow the selector if it catches too much. Check each page's layout after
turning it on.

**Hover:** the tablet rules never depend on hover. Share and Save stay visible on cards, as they already are on
desktop.

## 5. Verify

1. **Phones** (320, 390, 430px): nothing changed, including the header.
2. **Desktop** (1024, 1280, 1440px): nothing changed.
3. **Tablet** (744, 820, 834, 1023px), every page:
   - The header is two rows, every section is reachable in row 2, and the search icon goes to Where to watch.
   - The Menu disclosure is gone.
   - Every page still shows its phone layout under the new header, because none has opted in yet.
4. **Dark mode and two other colour schemes:** the header's two rows and the current-section underline are readable.
5. **Rotate the iPad:** in landscape (1024+) the desktop header and layout appear, and back in portrait the tablet
   header returns.

Then do `ipad-01-where-to-watch.md`.
