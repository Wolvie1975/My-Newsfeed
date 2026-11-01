# iPad portrait, part 6: Home

**Save as:** `docs/ipad/ipad-06-home.md` · **Needs:** parts 00–05 done and checked, and the `.bs-sheet` wrapper from
the 4K guide (already on `develop`, `Home.razor` line ~90).

**Mockup:** **iPad portrait** page → "Home — iPad portrait".

**Scope:** `Components/Pages/Home.razor` (opt in only), the Home desktop rules in `broadsheet.css` and one block in
`tablet.css`. This page has the most sections, so it comes last.

## What the page looks like at 820

1. **Masthead:**
   - Date eyebrow with a small Refresh pill on the same line.
   - "Good evening" at 48px.
   - The weather, four periods across, each with its icon.
2. **Where to watch** band: the label, field and Search button on one row. It fits at this width, so it stays.
3. **Live now:**
   - Live games as cards you swipe sideways, about three visible.
   - "Scoreboard →" at the top right.
   - Under the cards, the **New tonight** line ("Paramount+ Avatar: Seven Havens · 4 episodes", from the 4K guide 2.4).
4. **The lead** across the full width: photo on top (16:9, or the P8 frame), kicker, a 36px headline.
5. **Two columns:**
   - Left: Today & Tonight.
   - Right: More headlines, then the latest video, box office and Sports headlines (today's front side column).
6. **More from today:**
   - Left: Entertainment headlines and Latest videos.
   - Right: Today's episodes, Movies and Digital, then Scoreboard and Top 25.

## How Home differs from the other pages

Below 1024px the Home desktop markup already has base rules (`broadsheet.css` around line 245):

- `.bs-home` is a flex column;
- `.bs-masthead`, `.bs-front`, `.bs-more` and `.bs-col` are `display: contents`;
- sections are ordered by `data-home-section` (`order: 50`, Home sections hidden in Settings stay hidden).

So on a tablet the desktop markup starts as **one long column**. The tablet rules rebuild the columns on top of that.
They keep the `[hidden]` behaviour, so a section someone hid in Settings stays hidden.

## 1. Opt in (markup)

| File | Wrapper | Add |
|---|---|---|
| `Home.razor` line ~25 | `<div class="mx-desktop">` | `tab-desktop` |
| `Home.razor` line ~383 | `<div class="mx-phone" data-home-grid>` | `tab-phone` |

`HomeLatestVideo Embedded="true"` inside the phone markup uses the same `.home-latest-video` classes as the desktop
card. It's hidden with the phone wrapper, so nothing extra is needed.

## 2. Desktop styles to bring down

The phone Home lives under `.mx-phone[data-home-grid]`, **not** under `.bs-home`. That gives a simple rule:

- **Widen** (to `min-width: 744px`) any `min-width: 1024px` rule in `broadsheet.css` whose selectors **all** start
  with `.pub .bs-home` or a `.bs-` class (`.bs-masthead`, `.bs-title…`, `.bs-glance…`, `.bs-live…`, `.bs-watch`,
  `.bs-col…`, `.bs-band`). The main Home block starts around line 702, and there are more around 960, 1088 and 1170.
- **Don't widen:**
  - `.bs-front` / `.bs-more` / `.bs-sheet` **grid templates** (the tablet sets its own below);
  - the `.bs-title__name` big-logo rules (the tablet shows the header logo instead);
  - anything with `.site-header`, `.brand…` or the bare `.d-card`.
- **Copy** what you need from rules like `.pub .d-lead__title` or `.pub .home-latest-video__title` that aren't under
  `.bs-home`, scoped as `.pub .tab-desktop …`.

Leave the 1920 / 2560 rules (4K guide) alone.

## 3. Tablet styles (`tablet.css`)

```css
    /* ---------- Home (docs/ipad/ipad-06-home.md) ---------- */
    /* Header: the big centred logo isn't shown; the header's logo is on row 1. */
    .pub .tab-desktop .bs-title__name {
        display: none;
    }

    /* Masthead: date + Refresh, greeting, weather four across. */
    .pub .tab-desktop .bs-title {
        display: grid;
        grid-template-columns: auto 1fr;
        grid-template-areas: "date refresh" "greeting greeting";
        align-items: center;
        gap: 4px 10px;
        text-align: left;
    }
    .pub .tab-desktop .bs-title__date {
        grid-area: date;
        margin: 0;
        color: var(--accent);
        font-size: 12px;
        font-weight: 800;
        letter-spacing: 0.1em;
        text-transform: uppercase;
    }
    .pub .tab-desktop .bs-title__greeting {
        grid-area: greeting;
        margin: 0;
        font-size: 48px;
        font-style: normal;
        font-weight: 700;
        line-height: 1;
        letter-spacing: -0.015em;
    }
    .pub .tab-desktop .bs-title .home-refresh {
        grid-area: refresh;
        justify-self: start;
        min-height: 32px;
        margin: 0;
        padding: 0 10px;
        border: 0;
        border-radius: 8px;
        background: color-mix(in srgb, var(--accent) 10%, transparent);
        font-size: 13px;
        position: relative;
    }
    .pub .tab-desktop .bs-title .home-refresh::after {    /* 44px tap target around the 32px pill */
        content: "";
        position: absolute;
        inset: -6px;
    }
    .pub .tab-desktop .bs-glance {
        padding: 0;
        border: 0;
        border-radius: 0;
    }
    .pub .tab-desktop .bs-glance__title {
        position: absolute; width: 1px; height: 1px; overflow: hidden; clip: rect(0 0 0 0);
    }
    .pub .tab-desktop .bs-glance__periods {
        grid-template-columns: repeat(4, minmax(0, 1fr));
        gap: 0;
        margin-inline: -12px;
    }
    .pub .tab-desktop .bs-glance__periods li {
        display: grid;
        grid-template-columns: auto minmax(0, 1fr);
        grid-template-areas: "name name" "icon temp";
        align-items: center;
        gap: 2px 8px;
        padding: 0 12px;
        border-left: 1px solid var(--row-border);
    }
    .pub .tab-desktop .bs-glance__name { grid-area: name; }
    .pub .tab-desktop .bs-glance__periods .weather-icon { grid-area: icon; width: 30px; height: 30px; margin: 0; }
    .pub .tab-desktop .bs-glance__reading {
        grid-area: temp;
        display: flex;
        flex-direction: column;
        font-size: 12px;
    }
    .pub .tab-desktop .bs-glance__reading b {
        display: block;
        font-family: var(--font-serif);
        font-size: 24px;
        line-height: 1.1;
    }

    /* Order: title, weather, Where to watch, Live now, then the sheet. */
    .pub .tab-desktop .bs-home .bs-title  { order: 0; }
    .pub .tab-desktop .bs-home .bs-glance { order: 1; }
    :root:is([data-color-scheme], :not([data-color-scheme])) .pub .tab-desktop .bs-home [data-home-section='watch'] { order: 2; }
    .pub .tab-desktop .bs-home .bs-live   { order: 3; }
    .pub .tab-desktop .bs-home .bs-sheet  { order: 4; }

    /* Live now: cards you swipe sideways; New tonight under them. */
    .pub .tab-desktop .bs-live {
        display: grid;
        grid-template-columns: 1fr auto;
        align-items: center;
        gap: 8px 0;
        padding: 12px 0 14px;
        border: 0;
        border-top: 4px double var(--text);
        border-bottom: 1px solid var(--row-border);
        border-radius: 0;
    }
    .pub .tab-desktop .bs-live .bs-ear__title {
        margin: 0;
        color: var(--live, #b0001e);
        font-size: 12px;
        letter-spacing: 0.1em;
        text-transform: uppercase;
    }
    .pub .tab-desktop .bs-live__list {
        grid-column: 1 / -1;
        display: flex;
        gap: 12px;
        margin: 0 -24px;
        padding: 0 24px 4px;
        overflow-x: auto;
        scroll-snap-type: x proximity;
        scroll-padding-inline: 24px;
        scrollbar-width: none;
        -webkit-mask-image: linear-gradient(90deg, #000 calc(100% - 56px), transparent);
        mask-image: linear-gradient(90deg, #000 calc(100% - 56px), transparent);
    }
    .pub .tab-desktop .bs-live__list li {
        flex: 0 0 230px;
        scroll-snap-align: start;
        border: 0;
    }
    .pub .tab-desktop .bs-live__list a {
        display: flex;
        flex-direction: column;
        gap: 4px;
        height: 100%;
        padding: 10px 12px;
        border: 1px solid var(--border);
        border-radius: 12px;
        background: var(--surface);
    }
    .pub .tab-desktop .bs-live__score { font-weight: 700; }
    .pub .tab-desktop .bs-live__clock { color: var(--text-2); font-size: 12px; }
    .pub .tab-desktop .bs-live__tonight {
        grid-column: 1 / -1;
        display: flex;
        gap: 8px;
        min-height: 44px;
        align-items: center;
        border: 0;
        padding: 0;
    }

    /* The sheet: lead across both columns, then two columns, then More from today. */
    .pub .tab-desktop .bs-sheet {
        display: grid;
        grid-template-columns: minmax(0, 1fr) minmax(0, 1fr);
        align-items: start;
        padding-top: 20px;
    }
    .pub .tab-desktop .bs-sheet .bs-front,
    .pub .tab-desktop .bs-sheet .bs-more,
    .pub .tab-desktop .bs-sheet .bs-front > .bs-col--main {
        display: contents;
    }
    .pub .tab-desktop .bs-sheet .bs-col {
        display: flex;
        flex-direction: column;
        gap: 24px;
        min-width: 0;
        padding: 0;
        border: 0;
    }
    .pub .tab-desktop .bs-sheet .d-lead {                              /* HomeLeadStory */
        grid-column: 1 / -1;
        grid-row: 1;
        padding-bottom: 20px;
        margin-bottom: 4px;
        border-bottom: 1px solid var(--text);
    }
    .pub .tab-desktop .bs-sheet .bs-front > .bs-col--left {
        grid-column: 1;
        grid-row: 2 / span 2;
        padding-right: 20px;
    }
    .pub .tab-desktop .bs-sheet .bs-front > .bs-col--main > :not(.d-lead) {   /* More headlines */
        grid-column: 2;
        grid-row: 2;
        padding-left: 20px;
        border-left: 1px solid var(--row-border);
    }
    .pub .tab-desktop .bs-sheet .bs-front > .bs-col--side {
        grid-column: 2;
        grid-row: 3;
        padding: 24px 0 0 20px;
        border-left: 1px solid var(--row-border);
    }
    .pub .tab-desktop .bs-sheet .bs-band {
        display: block;
        grid-column: 1 / -1;
        grid-row: 4;
        margin: 28px 0 8px;
    }
    .pub .tab-desktop .bs-sheet .bs-more > .bs-col--main {
        grid-column: 1;
        grid-row: 5 / span 2;
        padding-right: 20px;
    }
    .pub .tab-desktop .bs-sheet .bs-more > .bs-col--side {
        grid-column: 2;
        grid-row: 5;
        padding-left: 20px;
        border-left: 1px solid var(--row-border);
    }
    .pub .tab-desktop .bs-sheet .bs-more > .bs-col--left {           /* Scoreboard + Top 25 */
        grid-column: 2;
        grid-row: 6;
        padding: 24px 0 0 20px;
        border-left: 1px solid var(--row-border);
    }

    /* Lead: photo on top, 36px headline. */
    .pub .tab-desktop .bs-home .d-lead__story { display: flex; flex-direction: column; }
    .pub .tab-desktop .bs-home .d-lead__image {
        order: 0;
        width: 100%;
        height: auto;
        aspect-ratio: 16 / 9;
        margin-bottom: 14px;
    }
    .pub .tab-desktop .bs-home .d-lead__image img { width: 100%; height: 100%; object-fit: cover; }
    .pub .tab-desktop .bs-home .d-lead__source  { order: 1; margin: 0 0 6px; }
    .pub .tab-desktop .bs-home .d-lead__title   { order: 2; font-size: 36px; line-height: 1.08; }
    .pub .tab-desktop .bs-home .d-lead__summary { order: 3; font-size: 18px; }
    .pub .tab-desktop .bs-home .d-lead__meta    { order: 4; }

    /* Column headings: one style everywhere (as in the 4K guide 2.4). */
    .pub .tab-desktop .bs-home .bs-tonight .d-card__title {
        font-family: inherit;
        font-size: 12px;
        font-weight: 800;
        letter-spacing: 0.1em;
        text-transform: uppercase;
    }
    .pub .tab-desktop .bs-home .home-latest-video__title {
        font-size: 19px;
        line-height: 1.22;
    }
```

**Rows in the sheet:**

| Row | Left column | Right column |
|---|---|---|
| 1 | Lead (both columns) | |
| 2–3 | Today & Tonight | Row 2: More headlines · row 3: front side column |
| 4 | "More from today" (both columns) | |
| 5–6 | Entertainment headlines + Latest videos | Row 5: TV, Movies, Digital · row 6: Scoreboard, Top 25 |

**The front's main column** holds two children today: `HomeLeadStory` (`.d-lead`) and the "More headlines"
`HomeHeadlineShelf`. The shelf is placed in row 2, column 2. If another section is ever added to that column, wrap the
non-lead children in one `<div>`, or they'll land in the same grid cell and overlap.

**Personalised order:** if `personal.js` reorders Home sections **within** columns (from Settings), that still works.
If it moves sections **between** columns or relies on the flat `order: 50` list, check with a reordered profile and
tell me what you see. The tablet grid places whole columns, not single sections.

**Hidden sections:** a section hidden in Settings is `[hidden]` and stays hidden. An entirely empty column leaves its
grid cell empty, which is acceptable.

## 4. Verify

1. **744, 820, 834px:**
   - Masthead: the date and Refresh share a line, "Good evening" is 48px, and the weather is four across with icons.
     Refresh works.
   - The Where to watch band submits.
   - Live now: the cards swipe and New tonight shows below. Before any game starts the heading reads "Coming up";
     with no games it shows the empty message.
   - The lead runs full width with its photo on top (or the P8 frame).
   - Today & Tonight sits on the left, More headlines + the side column on the right, and the columns end close
     together.
   - "More from today" leads into the two lower columns.
2. **Hide a Home section in Settings** (e.g. Box office): it disappears on the tablet too. Turn it back on and it
   returns.
3. **Rotate:** landscape (1024+) shows the desktop Home, portrait the tablet Home.
4. **Phones:** unchanged phone Home. **Desktop 1024–2560:** unchanged.
5. **The other five pages** still look as their parts left them.

That completes iPad portrait. Every page now has its tablet layout, and the foundation's per-page switch can stay as
it is (it costs nothing). Leave it in place, so a new page can opt in the same way.
