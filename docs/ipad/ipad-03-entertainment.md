# iPad portrait, part 3: Entertainment

**Save as:** `docs/ipad/ipad-03-entertainment.md` · **Needs:** parts 00–02 done and checked.

**Mockup:** **iPad portrait** page → "Entertainment — iPad portrait".

**Scope:**

- `Components/Pages/SectionHub.razor` (Entertainment heading only);
- `Components/EntertainmentFront.razor` (opt in, plus grouping one show's episodes);
- the Entertainment block in `sections.css`;
- one block in `tablet.css`.

## What the page looks like at 820

1. Heading "Entertainment" with its eyebrow, and the **section tabs** (Entertainment home, Headlines, Movies, TV &
   streaming, Digital releases, Box office, Where to watch) as one row that scrolls sideways.
2. **On screen:** a row of tall posters you swipe sideways, about 4½ visible. One show airing several episodes
   tonight appears **once**, as "Paramount+ · 4 episodes", not as two identical Avatar posters.
3. **The lead** across the full width: photo on top (16:9), kicker, a 36px headline and the summary.
4. **Two columns:** Latest headlines on the left; Next weekend (box office predictions), Tonight on TV and Friday in
   theaters on the right.

## 1. Opt in (markup)

| File | Wrapper | Add |
|---|---|---|
| `SectionHub.razor`, Entertainment branch | `<div class="mx-desktop"><PageHeading Section="entertainment" …/></div>` | `tab-desktop` |
| `EntertainmentFront.razor` line ~9 | `<div class="mx-desktop">` | `tab-desktop` |
| `EntertainmentFront.razor` line ~208 | `<div class="mx-phone">` | `tab-phone` |

**Leave the Sports branch of `SectionHub.razor` alone.** It opts in with part 4.

## 2. Group one show's episodes (also fixes desktop)

Today `onScreen` takes `episodes.Take(2)`. With four Avatar episodes tonight, that's two identical Avatar posters,
and the same happens on desktop (see your screenshot). Group by show. In `EntertainmentFront.razor`'s `@code`,
replace the `onScreen = episodes.Take(2)…` line's episode part:

```csharp
onScreen = episodes.GroupBy(e => e.ShowId).Take(2)
    .Select(show =>
    {
        var e = show.First();
        var count = show.Count();
        var where = count > 1
            ? string.Join(" · ", new[] { e.Platform, $"{count} episodes" }.Where(p => !string.IsNullOrEmpty(p)))
            : EpisodeWhere(e);
        return new ScreenItem(e.SeriesTitle, e.Poster,
            "Tonight" + (e.StartsUtc is { } start ? " · " + Dates.Time(start) : ""), where, "tv", true);
    })
    .Concat(movies.Select(m => new ScreenItem(m.Title, m.Poster,
        m.Date?.ToString("ddd, MMM d", System.Globalization.CultureInfo.InvariantCulture) ?? "Date to be announced",
        m.Platform ?? MoviesQuery.Types.GetValueOrDefault(m.Type, m.Type), "movies", false)))
    .Take(5).ToList();
```

`.Take(5)`, up from 4, gives the swipe row one poster to peek past the edge. Desktop shows the same five.

**Tonight on TV** (`.ef-tv`) has the same repetition: four rows of Avatar. Group it the same way, one row per show
with the episode range:

```razor
@foreach (var show in episodes.GroupBy(e => e.ShowId).Take(4))
{
    var first = show.First();
    var last = show.Last();
    <a class="ef-tv__item" href="tv">
        <MoviePoster Url="@first.Poster" Title="@first.SeriesTitle" Class="ef-poster" />
        <span><b>@first.SeriesTitle</b><span class="ef-where">@(show.Count() > 1
            ? string.Join(" · ", new[] { first.Platform, $"{show.Count()} new episodes", EpisodeRange(first, last) }.Where(p => !string.IsNullOrEmpty(p)))
            : EpisodeLine(first))</span></span>
    </a>
}
```

```csharp
private static string? EpisodeRange(TvEpisodeItem first, TvEpisodeItem last) =>
    EpisodeCode(first) is { } a && last.Number is { } n ? $"{a}–E{n:00}" : EpisodeCode(first);
```

That gives "Paramount+ · 4 new episodes · S01 E10–E13". It applies on desktop too. Check that the desktop side
column still looks balanced with fewer rows.

## 3. Desktop styles to bring down

The Entertainment desktop block is `sections.css` from about line 270 ("Desktop: On screen across the top, then the
lead and headlines beside a ruled side column") to about line 470. **News's phone markup uses `.ef-headlines` and
`.ef-meta`**, so don't widen the whole block. Instead:

- **Widen** (to `min-width: 744px`) the rules whose selectors are only `.ef`, `.ef-screen…`, `.ef-cols`, `.ef-main`,
  `.ef-side`, `.ef-box…`, `.ef-tv…`, `.ef-theaters`, `.ef-lead…` and `.ef-head`. To widen part of a block, move those
  rules into a new `@media (min-width: 744px)` block just above it.
- **Copy** the `.ef-headlines` / `.ef-meta` rules into `tablet.css`, scoped as `.pub .tab-desktop .ef-headlines …`.

Also, `sections.css` line ~263 hides `.ef-tv` below 1024 (`@media (max-width: 1023px) { .ef-tv { display: none } }`).
The tablet block brings it back.

The section tabs are `.section-tabs` (`broadsheet.css` ~119–150), which are shared, so copy them (below).

## 4. Tablet styles (`tablet.css`)

```css
    /* ---------- Section tabs on opted-in section pages (shared by parts 3–5) ---------- */
    .pub .tab-desktop .section-tabs {
        display: flex;
        flex-wrap: nowrap;
        gap: 0 4px;
        margin-inline: -24px;
        padding: 0 12px;
        border-bottom: 1px solid var(--text);
        overflow-x: auto;
        scrollbar-width: none;
        -webkit-mask-image: linear-gradient(90deg, #000 calc(100% - 56px), transparent);
        mask-image: linear-gradient(90deg, #000 calc(100% - 56px), transparent);
    }
    .pub .tab-desktop .section-tabs a {
        flex: none;
        display: flex;
        align-items: center;
        min-height: 44px;
        padding: 0 12px;
        border-bottom: 3px solid transparent;
        color: var(--text);
        font-size: 15px;
        font-weight: 600;
        white-space: nowrap;
        text-decoration: none;
    }
    .pub .tab-desktop .section-tabs a[aria-current='page'] {
        border-bottom-color: var(--accent);
        color: var(--accent);
        font-weight: 800;
    }
    .pub .tab-desktop .section-nav .section-select {
        display: none;
    }
    /* Scroll the current tab into view: see the note below. */

    /* ---------- Entertainment (docs/ipad/ipad-03-entertainment.md) ---------- */
    .pub .tab-desktop .ef-screen__list {
        display: flex;
        flex-wrap: nowrap;
        gap: 16px;
        margin-inline: -24px;
        padding: 2px 24px 10px;
        overflow-x: auto;
        scroll-snap-type: x proximity;
        scroll-padding-inline: 24px;
        scrollbar-width: none;
        -webkit-mask-image: linear-gradient(90deg, #000 calc(100% - 56px), transparent);
        mask-image: linear-gradient(90deg, #000 calc(100% - 56px), transparent);
    }
    .pub .tab-desktop .ef-screen__list > li {
        flex: 0 0 168px;
        scroll-snap-align: start;
    }
    .pub .tab-desktop .ef-screen__item {
        display: flex;
        flex-direction: column;              /* tall poster on top, text under it */
        gap: 8px;
    }
    .pub .tab-desktop .ef-screen__item .ef-poster {
        width: 100%;
        height: auto;
        aspect-ratio: 2 / 3;
        border-radius: 10px;
    }

    /* Lead full width, then two equal columns. */
    .pub .tab-desktop .ef-cols {
        grid-template-columns: minmax(0, 1fr) minmax(0, 1fr);
    }
    /* .ef-main holds, in order: article.ef-lead, h3.ef-label "Latest headlines", ul.ef-headlines, a.ef-more
       (or a .updated / .ef-empty message). Unwrapping it lets the lead span both columns. */
    .pub .tab-desktop .ef-main {
        display: contents;
    }
    .pub .tab-desktop .ef-cols .ef-lead {
        grid-column: 1 / -1;
        grid-row: 1;
        padding-bottom: 20px;
        margin-bottom: 4px;
        border-bottom: 1px solid var(--text);
    }
    .pub .tab-desktop .ef-cols > .ef-main > :not(.ef-lead) {
        grid-column: 1;
        padding-right: 20px;
    }
    .pub .tab-desktop .ef-cols > .ef-side {
        grid-column: 2;
        grid-row: 2 / span 4;                /* alongside the label, the list and the "All headlines" link */
        padding-left: 20px;
        border-left: 1px solid var(--row-border);
    }
    .pub .tab-desktop .ef-tv {
        display: block;                      /* sections.css hides it below 1024 */
    }
    .pub .tab-desktop .ef-lead__image {
        width: 100%;
        height: auto;
        aspect-ratio: 16 / 9;
        border-radius: 12px;
        overflow: hidden;
    }
    .pub .tab-desktop .ef-lead__image img {
        width: 100%;
        height: 100%;
        object-fit: cover;
    }
    .pub .tab-desktop .ef-lead__title {
        font-size: 36px;
        line-height: 1.08;
    }
```

The lead carries `data-image-layout`, so if `article-images.js` marks its photo `none` (missing or broken), the
existing rules hide `.ef-lead__image` and the headline moves to the top. If the P8 placeholder is in, it fills the
16:9 frame instead.

**Current tab in view:** on Box office or Digital releases, the current tab can start off-screen to the right. Add to
`public.js` (runs on every page and is harmless elsewhere):

```js
// Tablet: scroll the current section tab into view (docs/ipad/).
document.querySelectorAll('.section-tabs [aria-current="page"]').forEach(a =>
    a.scrollIntoView({ block: 'nearest', inline: 'center' }));
```

Hook it to the same `enhancedload` event the rest of `public.js` uses, so it also runs after Blazor navigation.

## 5. Verify

1. **744, 820, 834px:**
   - Tabs scroll sideways, and the current tab is visible on every Entertainment page.
   - On screen swipes; tonight's show appears once with "4 episodes", and posters are tall and lined up.
   - The lead runs full width. Headlines on the left, box office / TV / theaters on the right, and the two columns
     end close together.
2. **Desktop (1280):** On screen shows one Avatar ("· 4 episodes"), Tonight on TV shows one row with the episode
   range, and nothing else moved.
3. **Phones:** unchanged.
4. **Sports** (not opted in yet) still shows its phone layout on the iPad, even though `SectionHub` serves both.

Then do `ipad-04-sports.md`.
