# Headline top section fix (All headlines, desktop)

Written October 29, 2026 (America/Chicago). Follows `docs/headline-cards-handoff.md`; class and component names below
are the ones that guide introduced (`hc-top`, `hc-lead`, `hc-side`, `hc-card`). The headline cards work isn't on
`develop` on GitHub yet (latest there is `ae54bc5`), so check these names against your local implementation first.

**Save as:** `docs/headline-top-fix-handoff.md`

**Mockup:** the myNewspaper canvas, **Headline pages** page → "All headlines · top section fix"
(https://claude.ai/artifact/JVQ8mU7R63Z1cziNpeW9Z5).

## The problem

The top section is a 2:1 grid: the lead on the left, three stories stacked on the right. Each side story is a photo
card, so the right column runs about three photos tall while the lead is one photo plus a headline. On Top Headlines
that leaves an empty block under the lead. On Opinion it's worse: the lead has no usable photo, so it becomes a short
text card beside three tall ones.

## The fix: two arrangements, chosen by the lead's photo

| The lead's photo | Class on `.hc-top` | Arrangement |
|---|---|---|
| Good (`data-image-layout="wide"`) | `hc-top--photo` | Lead (16:9 photo) with **two text cards under it**, side by side. Right column becomes **Latest**: the first story may keep a shorter 16:9 photo, then **four text rows** (no image, no summary) |
| Small, missing or broken | `hc-top--text` (the default) | Lead becomes a **full-width banner**: headline left, summary and source right. The **next three stories run across** under it |

Both use the same eight stories (lead + 7), so the grid below starts at the same story either way.

| Slot | `hc-top--photo` | `hc-top--text` |
|---|---|---|
| Story 1 | Lead with photo | Banner |
| Stories 2–3 | Under the lead, two text cards | Across, cards 1–2 of 3 |
| Story 4 | Latest: photo card (or text row if its photo isn't good) | Across, card 3 of 3 |
| Stories 5–8 | Latest: four text rows | First four cards of the grid |

The grid below therefore starts at story 9 in the photo arrangement and story 5 in the text one. Stories 5–8 are
rendered in **both** places and CSS shows one: no re-rendering, and "Load more" is unaffected.

---

## 1. Markup (`Components/Feed/NewspaperFeed.razor`, cards mode)

Replace the `hc-top` block from the headline cards guide with:

```razor
@if (Cards && Featured && Items.Count > 0)
{
    var lead = Items.Take(1).ToArray();
    var under = Items.Skip(1).Take(2).ToArray();
    var latest = Items.Skip(3).Take(5).ToArray();       // story 4 (may show a photo) + stories 5–8 as rows
    <div class="hc-top hc-top--text" data-hc-top>
        <section class="hc-lead" aria-label="Lead story">
            <FeedArticles Items="lead" LeadFirst="true" Cards="true" PhoneFeatureFirst="PhoneFeatureFirst" />
        </section>
        @if (under.Length > 0) {
            <section class="hc-under" aria-label="More top stories"><FeedArticles Items="under" Cards="true" /></section>
        }
        @if (latest.Length > 0) {
            <section class="hc-latest" aria-labelledby="hc-latest-title">
                <h2 class="hc-latest__title" id="hc-latest-title">Latest</h2>
                <FeedArticles Items="latest" Cards="true" Rows="true" />
            </section>
        }
    </div>
}
@if (Cards) {
    <div class="hc-grid paper-rows" data-hc-grid>
        @if (Featured) {
            @* Stories 5–8: shown in the grid only for the text arrangement (in Latest for the photo one). *@
            <div class="hc-grid__textonly" style="display: contents"><FeedArticles Items="@Items.Skip(4).Take(4).ToArray()" Cards="true" GroupBy="GroupBy" /></div>
        }
        <FeedArticles Items="@(Featured ? Items.Skip(8).ToArray() : Items)" Cards="true" GroupBy="GroupBy" PreviousGroup="…" PreviousDay="PreviousDay" Total="Total" />
    </div>
}
```

`FeedArticles` gets one more parameter, `[Parameter] public bool Rows { get; set; }`, which adds `hc-row` to each
`<article>` (the first one gets `hc-row hc-row--first`). Nothing else in the article markup changes, so Save, Share and
the image classifier keep working.

> **Duplicate stories.** Stories 5–8 now appear twice in the HTML, once in Latest and once at the start of the grid,
> and CSS hides one copy. Keep this to these four. The `@key` on `<article>` is per component render, so the duplicates
> don't clash. The pinned "Jump to" bar counts come from the database (headline cards guide 2.1), so they don't double.

## 2. Choosing the arrangement (`wwwroot/article-images.js`)

The classifier already decides each story's `data-image-layout`. When it classifies the **lead**, it also sets the
top section's class. Add at the end of `classify(img)`:

```js
// All headlines: the lead's photo decides the top arrangement (headline-top-fix-handoff.md).
const top = story?.closest('.hc-lead')?.closest('[data-hc-top]');
if (top) {
    const photo = quality === 'wide';
    top.classList.toggle('hc-top--photo', photo);
    top.classList.toggle('hc-top--text', !photo);
}
```

The broken-image path (`error` handler) removes the image and marks the story `none`. Add the same three lines there
so a lead whose photo fails stays on `hc-top--text`.

The page starts in `hc-top--text` (from the markup), so the banner shows before the photo is measured. With
JavaScript off it stays that way, which also matches the existing no-JS rule that featured stories are text-only.

## 3. Styles (`wwwroot/headline-cards.css`, inside the existing `min-width: 744px` block)

Replace the `.hc-top`, `.hc-side` and `.hc-lead` rules from the headline cards guide with:

```css
/* ---------- Top section, text arrangement (default): banner, then three across ---------- */
.pub .hc-top { display: grid; gap: 24px; padding-bottom: 28px; border-bottom: 2px solid var(--text); }
.pub .hc-top--text { grid-template-columns: repeat(3, minmax(0, 1fr)); }
.pub .hc-top--text .hc-lead { grid-column: 1 / -1; }
.pub .hc-top--text .hc-lead .hc-card { display: grid; grid-template-columns: minmax(0, 1fr) minmax(280px, 360px);
    gap: 12px 40px; align-items: end; padding: 28px 32px; border-top: 0; border-left: 6px solid var(--hc-cat, var(--text)); }
.pub .hc-top--text .hc-lead .hc-card .thumb { display: none; }
.pub .hc-top--text .hc-lead .hc-card .body { display: contents; }               /* lets title / summary sit in the grid */
.pub .hc-top--text .hc-lead .cat,
.pub .hc-top--text .hc-lead .title { grid-column: 1; }
.pub .hc-top--text .hc-lead .title { font-size: 46px; line-height: 1.06; }
.pub .hc-top--text .hc-lead .summary,
.pub .hc-top--text .hc-lead .src { grid-column: 2; }
.pub .hc-top--text .hc-lead .summary { grid-row: 1 / span 2; align-self: end; font-family: var(--font-serif); font-size: 19px; }
.pub .hc-top--text .hc-under { display: contents; }                             /* stories 2–3 join the row of three */
.pub .hc-top--text .hc-latest { display: contents; }
.pub .hc-top--text .hc-latest__title,
.pub .hc-top--text .hc-latest .hc-row:not(.hc-row--first) { display: none; }    /* story 4 completes the row; 5–8 go to the grid */

/* ---------- Top section, photo arrangement ---------- */
.pub .hc-top--photo { grid-template-columns: minmax(0, 2fr) minmax(0, 1fr); grid-template-rows: auto 1fr; align-items: start; }
.pub .hc-top--photo .hc-lead { grid-column: 1; grid-row: 1; }
.pub .hc-top--photo .hc-lead .thumb { aspect-ratio: 16 / 9; }
.pub .hc-top--photo .hc-lead .title { font-size: 34px; line-height: 1.1; }
.pub .hc-top--photo .hc-under { grid-column: 1; grid-row: 2; display: grid; grid-template-columns: 1fr 1fr; gap: 20px; }
.pub .hc-top--photo .hc-latest { grid-column: 2; grid-row: 1 / span 2; }
.pub .hc-latest__title { margin: 0 0 8px; padding-bottom: 6px; border-bottom: 2px solid var(--text);
    font-size: 12px; font-weight: 800; letter-spacing: 0.1em; text-transform: uppercase; }
.pub .hc-top--photo .hc-row--first[data-image-layout='wide'] .thumb { aspect-ratio: 16 / 9; }
/* Rows: no image, no summary, compact. */
.pub .hc-top--photo .hc-row:not(.hc-row--first),
.pub .hc-top--photo .hc-row--first:not([data-image-layout='wide']) {
    padding: 12px 0; border: 0; border-bottom: 1px solid var(--border); border-radius: 0; background: none; }
.pub .hc-top--photo .hc-row:not(.hc-row--first) .thumb,
.pub .hc-top--photo .hc-row:not(.hc-row--first) .summary,
.pub .hc-top--photo .hc-row--first:not([data-image-layout='wide']) .summary { display: none; }
.pub .hc-top--photo .hc-row:not(.hc-row--first) .title,
.pub .hc-top--photo .hc-row--first:not([data-image-layout='wide']) .title { font-size: 19px; font-weight: 700; }

/* The grid's copies of stories 5–8 only show in the text arrangement. */
.pub .hc-top--photo ~ .hc-grid .hc-grid__textonly { display: none !important; }
```

The last rule needs `.hc-grid` to be a following sibling of `.hc-top` in the markup, as in step 1. If they end up in
different wrappers, have the JavaScript in step 2 also toggle `hc-photo-top` on the shared parent, and use
`.hc-photo-top .hc-grid__textonly { display: none !important; }` instead.

**iPad portrait (744–1023px):** the two-column grid already applies there. Add:

```css
@media (min-width: 744px) and (max-width: 1023px) {
    .pub .hc-top--text { grid-template-columns: repeat(2, minmax(0, 1fr)); }
    .pub .hc-top--text .hc-lead .hc-card { grid-template-columns: 1fr; }
    .pub .hc-top--text .hc-lead .summary, .pub .hc-top--text .hc-lead .src { grid-column: 1; grid-row: auto; }
    .pub .hc-top--text .hc-latest .hc-row--first { display: none; }   /* keep the row even: two across, not three */
}
```

With story 4 hidden on iPad portrait in the text arrangement, add `story 4` to the grid copies at those widths, or
accept that it only shows in the photo arrangement there. Simplest: render `Items.Skip(3).Take(5)` as the grid copies
and hide the first of them from 1024px up.

## 4. Verify (per AGENTS.md)

1. **Top Headlines with a good lead photo:** lead + two under it + Latest (photo + four rows); the two columns end
   within about one row of each other at 1024, 1280 and 1440px.
2. **Opinion (no lead photo):** full-width banner, then three across; no empty column.
3. **A lead whose photo is small or fails:** stays on the banner. **No JavaScript:** banner.
4. **No duplicates on screen:** stories 5–8 appear once in each arrangement; the grid then continues in order.
5. **Load more** appends after the grid as before. The Jump to group headings don't repeat at the seam.
6. **Dark mode and two other colour schemes:** the banner's tint and left edge are readable. Rows in Latest keep 44px
   Share and Save targets.
7. **Phones** (320, 390, 430px): unchanged; everything here is inside the 744px query.
