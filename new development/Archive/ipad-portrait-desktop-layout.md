# iPad portrait: serve the desktop layout

**Goal:** iPads in portrait (744–834 px wide) get the desktop design — masthead, section tabs, ruled columns, Latest beside the lead — instead of the phone layout. Phones stay as they are.

**Repo:** `Wolvie1975/My-Newsfeed` · files under `src/myNewspaper.Web/`

---

## Why iPads get the phone layout today

iPad Safari already reports itself as a desktop Mac, so the server is not the cause. The switch is purely CSS: the desktop design starts at **`min-width: 1024px`** in `public.css`, `sections.css` and `broadsheet.css`, and iPad portrait widths fall below that.

| Device (portrait) | CSS width | Today | After |
|---|---|---|---|
| iPhone | 375–440 | Phone | Phone |
| iPad mini | 744 | Phone | **Desktop (tablet-tightened)** |
| iPad 9th gen | 810 | Phone | **Desktop (tablet-tightened)** |
| iPad 10th gen / Air 11" | 820 | Phone | **Desktop (tablet-tightened)** |
| iPad Pro 11" | 834 | Phone | **Desktop (tablet-tightened)** |
| iPad Air 13" / Pro 13" | 1024 | Desktop | Desktop (unchanged) |

## The plan

1. Move the desktop breakpoint from **1024 → 744** everywhere it's used.
2. Add one new stylesheet, **`tablet.css`**, that only applies between **744 and 1023 px** and tightens the desktop layout so it fits: narrower padding, smaller side columns, and the three-column pages become two columns, with the left column moving underneath.

Nothing changes at 1024 px and wider, and nothing changes on phones.

---

## Step 1 — Lower the desktop breakpoint

From `src/myNewspaper.Web/wwwroot`:

```bash
cd src/myNewspaper.Web/wwwroot

# Desktop rules: 1024 → 744, and their phone counterparts: 1023 → 743
sed -i -E 's/min-width: ?1024px/min-width: 744px/g; s/max-width: ?1023px/max-width: 743px/g' \
  public.css sections.css broadsheet.css

# Section tabs (broadsheet.css) currently start at 768; align them so iPad mini gets tabs, not the dropdown
sed -i -E 's/min-width: ?768px/min-width: 744px/g; s/max-width: ?767px/max-width: 743px/g' broadsheet.css
```

Expected replacements (these counts are from the current `main`):

| File | `min-width: 1024px` | `max-width: 1023px` | `768/767` |
|---|---|---|---|
| `public.css` | 11 | 5 | — (leave as is) |
| `sections.css` | 25 | 4 | — |
| `broadsheet.css` | 12 | 5 | 1 / 1 |

Check afterwards:

```bash
grep -nE "1024px|1023px" public.css sections.css broadsheet.css   # should print nothing
```

> **Note:** the `(min-width: 1024px) and (max-width: 1279px)` rule in `public.css` becomes `744px … 1279px`. That's intended — it keeps the section links in the dropdown menu below 1280 px, which is what iPads need too.

> **Leave alone:** `public.css`'s other breakpoints (768, 1000, 1100, 1150, 1180, 1280, 1440, 1680). They're small refinements that still behave correctly.

Also update the two comments that mention "from 1024px" so they stay accurate:
- `Components/Pages/Home.razor` (line ~17)
- `Components/Layout/PublicLayout.razor` (line ~40)

---

## Step 2 — Add `tablet.css`

Create `src/myNewspaper.Web/wwwroot/tablet.css`:

```css
/* =====================================================================================================================
   Tablet portrait (744–1023px): the desktop design, tightened to fit an iPad.
   Loaded last so it overrides the desktop rules in public.css, broadsheet.css and sections.css.
   ===================================================================================================================== */
@media (min-width: 744px) and (max-width: 1023px) {

    /* ---------- Page frame ---------- */
    .pub {
        --page-pad: 24px;
    }
    .pub .page-heading h1 {
        font-size: 48px;
    }

    /* ---------- Home: broadsheet masthead ---------- */
    /* Title across the top; Watch and Live ears side by side underneath. */
    .pub .bs-masthead {
        grid-template-columns: minmax(0, 1fr) minmax(0, 1fr);
        row-gap: 18px;
    }
    .pub .bs-masthead > .bs-title {
        grid-column: 1 / -1;
        grid-row: 1;
    }
    .pub .bs-masthead > .bs-watch {
        grid-column: 1;
        grid-row: 2;
    }
    .pub .bs-masthead > .bs-live {
        grid-column: 2;
        grid-row: 2;
    }
    .pub .bs-title__name {
        font-size: 52px;
    }
    .pub .bs-glance {
        grid-template-columns: minmax(0, 2fr) minmax(220px, 1fr);
    }

    /* ---------- Home: three ruled columns → main + side, left column moves below ---------- */
    .pub .bs-front,
    .pub .bs-more {
        grid-template-columns: minmax(0, 2fr) minmax(240px, 1fr);
    }
    .pub .bs-col--main {
        grid-column: 1;
        grid-row: 1;
        padding-left: 0;
        padding-right: 20px;
        border-left: 0;
    }
    .pub .bs-col--side {
        grid-column: 2;
        grid-row: 1;
        padding-left: 20px;
        padding-right: 0;
    }
    .pub .bs-col--left {
        grid-column: 1 / -1;
        grid-row: 2;
        margin-top: 24px;
        padding: 24px 0 0;
        border-top: 1px solid var(--row-border);
        border-left: 0;
    }

    /* ---------- Sports front: same treatment (upcoming games move below) ---------- */
    .pub .spf-cols {
        grid-template-columns: minmax(0, 2fr) minmax(240px, 1fr);
    }
    .pub .spf-main {
        grid-column: 1;
        grid-row: 1;
        padding: 0 20px 0 0;
    }
    .pub .spf-side {
        grid-column: 2;
        grid-row: 1;
        padding-left: 20px;
    }
    .pub .spf-upcoming {
        grid-column: 1 / -1;
        grid-row: 2;
        margin-top: 24px;
        padding: 24px 0 0;
        border-right: 0;
        border-top: 1px solid var(--row-border);
    }
    .pub .spf-main .ef-lead__image {
        height: 240px;
    }
    .pub .spf-main .ef-lead__title {
        font-size: 32px;
    }

    /* ---------- Two-column section fronts: slimmer side column ---------- */
    /* Entertainment, Movies, TV, College, Local, Saved, YouTube */
    .pub .ef-cols,
    .pub .yt-top {
        grid-template-columns: minmax(0, 3fr) minmax(240px, 2fr);
    }
    .pub .mv-cols,
    .pub .tvc-cols,
    .pub .cl-cols,
    .pub .lc-cols,
    .pub .sv-lists {
        grid-template-columns: minmax(0, 2fr) minmax(240px, 1fr);
    }
    .pub .ef-main,
    .pub .yt-feature {
        padding-right: 20px;
    }
    .pub .ef-side,
    .pub .yt-latest {
        padding-left: 20px;
    }
    .pub .ef-lead__title {
        font-size: 32px;
    }

    /* ---------- Dense grids ---------- */
    .pub .d-movies .d-list {
        grid-template-columns: repeat(4, minmax(0, 1fr));
    }
    .pub .dg-day__list {
        grid-template-columns: repeat(2, minmax(0, 1fr));
    }
    .pub .tvc-row {
        grid-template-columns: 56px minmax(0, 1fr) 110px;
    }
}
```

Load it **last** in `src/myNewspaper.Web/Components/App.razor`, after `sections.css`:

```html
<link rel="stylesheet" href="@Assets["app.css"]" />
<link rel="stylesheet" href="@Assets["public.css"]" />
<link rel="stylesheet" href="@Assets["broadsheet.css"]" />
<link rel="stylesheet" href="@Assets["sections.css"]" />
<link rel="stylesheet" href="@Assets["tablet.css"]" />   <!-- new -->
```

> **Specificity:** a few desktop rules in `sections.css` use the `:root:is([data-color-scheme], :not([data-color-scheme])) .pub …` prefix. If a tablet override doesn't take effect, copy that same prefix onto the selector in `tablet.css`.

> **Service worker:** `wwwroot/service-worker.js` caches assets. If it has a hard-coded precache list, add `tablet.css` to it and bump the cache version so iPads pick up the change.

---

## Step 3 — Touch-friendly targets (optional, recommended)

Add this to the end of `tablet.css`, outside the media query above:

```css
/* Finger-sized targets on any touch screen, whatever its width. */
@media (pointer: coarse) {
    .pub .section-tabs a,
    .pub button,
    .pub select {
        min-height: 44px;
    }
}
```

---

## Step 4 — Test

**Browser dev tools** (Responsive mode). Check each width: **744, 768, 820, 834, 1023**, then **1024** and **1440** to confirm desktop hasn't changed, and **430** to confirm phones haven't changed.

| Page | What to check at 744–1023 |
|---|---|
| Home | Title across the top, Watch + Live side by side, main + side columns with the left column underneath, no horizontal scroll |
| Headlines | Lead story with Latest beside it (Latest doesn't drop below) |
| Sports | Scores strip, lead + standings side by side, upcoming games below |
| Entertainment / Movies / TV | Side column ≥ 240px, posters don't overflow |
| YouTube | Featured video + Latest videos side by side |
| Scoreboard / Standings / Top 25 | Tables fit or scroll inside their box |
| Header | Section links in the dropdown menu (not the inline row), clock visible |

**On a real iPad:** rotate to landscape (1024+) and back. Landscape should look exactly as it does today.

**Quick check for horizontal overflow**, run in the browser console at each width:

```js
document.documentElement.scrollWidth > innerWidth
  ? [...document.querySelectorAll('body *')].filter(e => e.getBoundingClientRect().right > innerWidth + 1)
  : 'no overflow'
```

---

## Rollback

Delete the `tablet.css` `<link>` and revert the `sed` changes (`git checkout -- wwwroot/public.css wwwroot/sections.css wwwroot/broadsheet.css`). No markup or C# changes are involved.
