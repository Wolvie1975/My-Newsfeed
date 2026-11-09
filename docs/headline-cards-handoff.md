# Headline cards handoff (All headlines pages, desktop)

Written October 29, 2026 (America/Chicago) against `develop` @ `ae54bc5`. Verify current repository state before
making changes; names and line references are from that commit.

**Save as:** `docs/headline-cards-handoff.md`

**Approved mockups:** the myNewspaper canvas, **Headline pages** page (https://claude.ai/artifact/JVQ8mU7R63Z1cziNpeW9Z5):

| Artboard | Shows |
|---|---|
| All headlines · D | News: the Jump to box, lead + three beside it, then a three-column card grid with hour headings |
| All headlines · D scrolled | The box collapsed to a pinned bar under the site header |
| Sports / Entertainment · All headlines in concept D | The same layout; Jump to goes by day |

(A, B and C on the same page are the explorations D came from.)

## Status

- [x] **Release 1 — cards** (Phases 1 and 3, without the box). October 29, 2026.
- [ ] **Release 2 — Jump to box and pinned bar** (Phase 2).

## Review corrections (October 29, before building)

The plan below is kept as written; these points override it.

1. **Load more appends to `.feed`, not `.paper-rows`.** `public.js` now inserts into `[data-hc-grid]` when the page has
   one. The Load more cards/group values live on the `.feed` section (`data-feed-cards`, `data-feed-group-by`,
   `data-last-group`) beside `data-last-day`, not on the button.
2. **The grid does not keep `paper-rows`.** The `paper-*` desktop rules (`.pub .paper-feed .paper-rows .article`, the
   three-row `.src` grid) outrank `.pub .hc-card`. Cards use `hc-*` wrappers only; `headline-cards.css` uses the
   `:root:is([data-color-scheme], :not([data-color-scheme])) .pub .hc-feed …` prefix to beat them and
   `article-images.css`. The `.feed` section gets `hc-feed` (block, not public.css's three-column grid).
3. **Phones.** Sports/Entertainment news, searches, source filters and later pages render this feed on phones too.
   Below 744px `.hc-top`, its sections and `.hc-grid` are `display: contents` and `.hc-group` is hidden, so phones get
   the same one-column list as before.
4. **BBC images.** `article-images.js` requests BBC's 1024px image for desktop cards too, not only feature stories,
   or BBC's 240px feed images would always be text cards.
5. **Colours.** Cards take their category colour from the phone classes (`PhoneColors.ForCategory` → `mx-c-*`, which set
   `--edge`/`--dot`), so every scheme and dark mode carry over. The text-card tint is
   `color-mix(in srgb, var(--text) 5%, var(--surface))`, not a fixed hex.
6. **`FeedDates`** already had `GroupLabel(string dayKey)`; the new helpers are `CardGroupKey`, `CardGroupLabel` and
   `DayHeading`, each taking the current UTC time.
7. **`--site-header-h` doesn't exist** (release 2): measure the real header for the pinned bar's `top`.
8. **Category row (release 2): always shown.** Sports and Entertainment always have a topic selected, so "hidden when a
   category is selected" would hide it there; the Sports mockup shows it. Show every category of the hub with today's
   count, the current one marked (`aria-current`), on News too.
9. **The brief swap is handled in release 1** for the lead and the three beside it, as suggested below
   (`data-image-measured`, set by `classify()` and by the failed/missing-image paths).
10. **Hour or day** is decided in `News.razor` already (release 1): `FeedQuery.CountPublishedSinceAsync` over the same
    filters as the list (the shared `Filtered` query), at least `FeedQuery.HourGroupingThreshold` (20) since midnight
    Central. Release 2's box should reuse that.

## Decisions

1. **No small thumbnail on desktop.** A story either gets a **photo card** (the photo across the full card width, one
   consistent 16:10 crop) or a **text card** (tinted panel, coloured top edge, bigger headline). Nothing in between.
2. **Which one** is decided by the existing classifier in `article-images.js`: `data-image-layout="wide"` → photo card;
   `compact` or `none` → text card. The 640 × 300 / ratio ≤ 2.2 rule from `docs/article-images.md` stays as it is.
3. **Jump to + Today by category** sit in one box above the stories. When the box scrolls away it becomes a 60px bar
   pinned under the site header, with the current group dark and a **Top ↑** link.
4. **Jump to unit:** hours when the list has a busy today (News), days otherwise (Sports, Entertainment). Rule in 2.2.
5. **Pages:** `/news/all` (and its categories), `/sports/news` and its topics, `/entertainment/news` and its topics —
   everything `News.razor` renders. Box office and Big 12 news keep their current layouts.
6. **Desktop only.** Everything new sits behind `@media (min-width: 744px)` (iPad portrait already gets the desktop
   layout per `tablet.css`). Phones render the same markup and keep today's rows unchanged.

---

## Phase 1 — Markup: cards mode for the existing feed components

The card is today's `<article>` from `FeedArticles.razor` with one more class. That keeps one markup for page load and
`/feed/more`, keeps Save/Share/analytics hooks, keeps the image classifier working, and leaves phones untouched.

### 1.1 `Components/Feed/FeedArticles.razor`

Add two parameters:

```csharp
/// <summary>Desktop card layout for the All headlines pages; phones still render these as rows.</summary>
[Parameter] public bool Cards { get; set; }
/// <summary>Cards only: how headings are grouped ("hour" or "day"), and the last group already on the page.</summary>
[Parameter] public string GroupBy { get; set; } = "day";
[Parameter] public string? PreviousGroup { get; set; }
```

In cards mode, emit a group heading (in addition to the existing `daylabel`, which stays for phones) whenever the
group key changes, and add the card class to each article:

```razor
@if (Cards && groupKey != previousGroup)
{
    previousGroup = groupKey;
    <h3 class="hc-group" id="@($"hc-{groupKey}")" data-hc-group="@groupKey">@groupLabel</h3>
}
<article class="article hc-card @(isLead ? "article--lead" : "")" … >   @* everything else unchanged *@
```

Group keys and labels (put the helpers in `FeedDates`):

| Group by | Key | Label |
|---|---|---|
| hour (today only) | `h-2026-10-29-09` | `9 AM hour` |
| day | `d-2026-10-28` | `Yesterday · Wed, Oct 28`, `Today`, or `Mon, Oct 26` |
| undated | `d-undated` | `Date unknown` |

In hour mode, stories from before today fall back to day groups, so the list reads 9 AM hour → 8 AM hour → … →
Yesterday · Wed, Oct 28 → ….

```csharp
public string GroupKey(FeedItem item, string groupBy)
{
    if (item.Undated) return "d-undated";
    var local = Local(item.Date);
    return groupBy == "hour" && local.Date == Local(DateTime.UtcNow).Date
        ? "h-" + local.ToString("yyyy-MM-dd-HH", Culture)
        : "d-" + local.ToString("yyyy-MM-dd", Culture);
}

public string GroupLabel(FeedItem item, string groupBy) => GroupKey(item, groupBy) switch
{
    "d-undated" => "Date unknown",
    var k when k.StartsWith("h-") => Local(item.Date).ToString("h tt", Culture) + " hour",
    _ => DayHeading(item.Date),   // "Today" / "Yesterday · Wed, Oct 28" / "Mon, Oct 26"
};
```

### 1.2 `Components/Feed/NewspaperFeed.razor`

Add `[Parameter] public bool Cards { get; set; }` and `[Parameter] public string GroupBy { get; set; } = "day";`.
In cards mode the featured top becomes **lead + three beside it**, then the grid:

```razor
@if (Cards && Featured && Items.Count > 0)
{
    <div class="hc-top">
        <section class="hc-lead" aria-label="Lead story"><FeedArticles Items="@Items.Take(1).ToArray()" LeadFirst="true" Cards="true" PhoneFeatureFirst="PhoneFeatureFirst" /></section>
        @if (Items.Count > 1) {
            <section class="hc-side" aria-label="Latest headlines"><FeedArticles Items="@Items.Skip(1).Take(3).ToArray()" Cards="true" /></section>
        }
    </div>
}
@if (Cards) {
    <div class="hc-grid paper-rows" data-hc-grid><FeedArticles Items="@(Featured ? Items.Skip(4).ToArray() : Items)" Cards="true" GroupBy="GroupBy" PreviousDay="PreviousDay" Total="Total" /></div>
} else {
    @* existing markup unchanged *@
}
```

Keep `paper-rows` on the grid so `public.js` still appends "Load more" results into it.

### 1.3 `Components/Pages/News.razor`

At line ~106:

```razor
<NewspaperFeed Items="feed.Items" Featured="@(After is null)" PhoneFeatureFirst="SectionNews"
               Total="@(After is null ? feed.Total : null)" Cards="true" GroupBy="@jump.GroupBy" />
```

and render the box (Phase 2) just above it, only on the first page (`After is null`) and only when there are stories.

### 1.4 `Endpoints/FeedEndpoints.cs` (`/feed/more`)

Accept `cards`, `group` and `groupBy`, pass them to `FeedArticles`, and return the last group so the next batch doesn't
repeat a heading:

```csharp
[nameof(FeedArticles.Cards)] = cards == "1",
[nameof(FeedArticles.GroupBy)] = groupBy is "hour" ? "hour" : "day",
[nameof(FeedArticles.PreviousGroup)] = group,
…
lastGroup = page.Items.Count > 0 ? dates.GroupKey(page.Items[^1], groupBy ?? "day") : group,
```

In `public.js`, the Load more handler adds `cards=1&groupBy=…&group=…` from `data-*` attributes on the button
(`data-feed-cards`, `data-feed-group-by`, `data-feed-group`) and updates `data-feed-group` from `lastGroup`, the same
way it already does for `day` / `lastDay`.

---

## Phase 2 — The Jump to box and the pinned bar

### 2.1 Data (`Data/FeedQuery.cs`)

`FeedCategory.Count` is an all-time count, so the box needs its own numbers for the current filter (hub, category,
source and search). One query pulls the sort dates and category of the past seven days of matching stories; the
grouping happens in C# with `FeedDates`, so hours and days are in Central time.

```csharp
public sealed record JumpGroup(string Key, string Label, int Count, string? Href);
public sealed record JumpCategory(int Id, string Label, int Count, string Href, string Color);
public sealed record JumpBox(string GroupBy, IReadOnlyList<JumpGroup> Groups, IReadOnlyList<JumpCategory> Categories);

public static async Task<IReadOnlyList<(DateTime Date, int? CategoryId)>> GetRecentDatesAsync(
    WebScraperContext db, int? categoryId, string? search, int? sourceId, string hub, int days = 7) =>
    (await FilteredQuery(db, categoryId, search, sourceId, hub)           // factor out of GetFeedAsync
        .Where(p => (p.Published ?? p.ScrapedAt) >= DateTime.UtcNow.AddDays(-days))
        .Select(p => new { Date = p.Published ?? p.ScrapedAt, p.Source.SourceCategoryId })
        .ToListAsync())
    .Select(r => (r.Date, r.SourceCategoryId)).ToList();
```

Refactor the filter part of `GetFeedAsync` (hub, category, source, search) into `FilteredQuery` so the list and the
box can't disagree.

### 2.2 Building the box (`Components/Feed/JumpBoxBuilder.cs`)

| Rule | Value |
|---|---|
| Group by hour | When at least **20** of the matching stories are from today; otherwise by day |
| Hour buttons | Today's hours that have stories, newest first, **up to 4**, then **Earlier** |
| Day buttons | Today, Yesterday, then the next days with stories, **up to 4**, then **Earlier** |
| Category buttons | Today's count per category in this hub, highest first, **up to 5**, zero-count ones left out. Hidden when a category is already selected |
| Colours | Use the category colour tokens from the mobile refresh (`--mx-world` …) mapped per category, or `--accent` when a category has none |

**Where a button goes:**
- A group already on the first page: `#hc-{key}` (smooth-scroll when JavaScript is on).
- A group further down: the same list URL with an `after` cursor that starts at that group, so it opens with that hour
  or day at the top. Build the cursor from the group's newest boundary:
  `new FeedCursor(groupEndUtc, int.MaxValue, false).ToToken()`.
- **Earlier**: the cursor for the start of the oldest group shown.
- A category: its tab URL (`NewsSections.RouteFor(category)` for Sports and Entertainment;
  `news/all?category={id}` for News).

### 2.3 `Components/Feed/JumpBox.razor`

```razor
<section class="hc-jump" aria-label="Find your place" data-hc-jump>
    <h2 class="hc-jump__label">Jump to</h2>
    <nav class="hc-jump__row" aria-label="Jump to an @(Box.GroupBy == "hour" ? "hour" : "day")">
        @foreach (var (g, i) in Box.Groups.Select((g, i) => (g, i)))
        {
            <a class="hc-jump__group" href="@g.Href" data-hc-target="@g.Key" aria-current="@(i == 0 ? "true" : null)">
                <b>@g.Label</b><span>@g.Count stories</span>
            </a>
        }
    </nav>
    @if (Box.Categories.Count > 0)
    {
        <h2 class="hc-jump__label">Today by<br />category</h2>
        <nav class="hc-jump__row" aria-label="Today by category">
            @foreach (var c in Box.Categories)
            {
                <a class="hc-jump__cat" href="@c.Href" style="--cat: @c.Color"><span>@c.Label</span><b>@c.Count</b></a>
            }
        </nav>
    }
</section>

@* The pinned copy. Hidden until headline-jump.js shows it; never shown without JavaScript. *@
<nav class="hc-bar" aria-label="Jump to" data-hc-bar hidden>
    <b class="hc-bar__title">@Title</b>
    @foreach (var g in Box.Groups) { <a href="@g.Href" data-hc-target="@g.Key"><b>@g.ShortLabel</b> <span>@g.Count</span></a> }
    @if (Box.Categories.Count > 0) {
        <span class="hc-bar__rule" aria-hidden="true"></span>
        @foreach (var c in Box.Categories) { <a class="hc-bar__cat" href="@c.Href" style="--cat: @c.Color">@c.Label <b>@c.Count</b></a> }
    }
    <a class="hc-bar__top" href="#top-of-list">Top ↑</a>
</nav>

@code {
    [Parameter, EditorRequired] public JumpBox Box { get; set; } = default!;
    [Parameter, EditorRequired] public string Title { get; set; } = "All headlines";
}
```

`ShortLabel` is the label without "hour" or the weekday ("9 AM", "Yesterday", "Oct 26").

### 2.4 `wwwroot/headline-jump.js` (new, loaded after `public.js`)

```js
// All headlines: pin a slim copy of the Jump to box once it scrolls away, and mark the group being read.
(function () {
    'use strict';
    function init() {
        const box = document.querySelector('[data-hc-jump]'), bar = document.querySelector('[data-hc-bar]');
        if (!box || !bar || box.dataset.hcBound) return;
        box.dataset.hcBound = '1';
        const desktop = matchMedia('(min-width: 744px)');
        new IntersectionObserver(([e]) => { bar.hidden = !desktop.matches || e.isIntersecting; },
            { rootMargin: '-64px 0px 0px 0px' }).observe(box);

        const mark = (key) => document.querySelectorAll('[data-hc-target]').forEach((a) => {
            a.toggleAttribute('aria-current', a.dataset.hcTarget === key);
        });
        const headings = new IntersectionObserver((entries) => {
            const top = entries.filter((e) => e.isIntersecting).sort((a, b) => a.boundingClientRect.top - b.boundingClientRect.top)[0];
            if (top) mark(top.target.dataset.hcGroup);
        }, { rootMargin: '-130px 0px -60% 0px' });
        const watch = () => document.querySelectorAll('[data-hc-group]:not([data-hc-watched])').forEach((h) => {
            h.dataset.hcWatched = '1'; headings.observe(h);
        });
        watch();
        new MutationObserver(watch).observe(document.querySelector('[data-hc-grid]') || document.body, { childList: true });

        document.addEventListener('click', (e) => {
            const a = e.target.closest('[data-hc-target]');
            const target = a && document.getElementById('hc-' + a.dataset.hcTarget);
            if (!target) return;                    // not loaded yet: follow the href (cursor URL)
            e.preventDefault();
            target.scrollIntoView({ behavior: matchMedia('(prefers-reduced-motion: reduce)').matches ? 'auto' : 'smooth' });
        });
    }
    document.addEventListener('DOMContentLoaded', init);
    // Blazor enhanced navigation swaps the page without a reload; re-run, as article-images.js does.
    if (window.Blazor?.addEventListener) window.Blazor.addEventListener('enhancedload', init);
})();
```

Add `<script src="@Assets["headline-jump.js"]" defer></script>` in `App.razor` after `article-images.js` (line ~41),
and `<link rel="stylesheet" href="@Assets["headline-cards.css"]" />` after `article-images.css` (line ~18).

---

## Phase 3 — Styles (`wwwroot/headline-cards.css`, linked after `article-images.css`)

Everything is inside the 744px query, so phones keep today's rows.

```css
/* All headlines, desktop: photo cards or text cards, never a small thumbnail. Phones keep the row layout. */
@media (min-width: 744px) {
    .pub .hc-top { display: grid; grid-template-columns: minmax(0, 2fr) minmax(0, 1fr); gap: 28px;
        padding-bottom: 28px; border-bottom: 2px solid var(--text); }
    .pub .hc-side { display: flex; flex-direction: column; gap: 20px; }
    .pub .hc-grid { display: grid; grid-template-columns: repeat(2, minmax(0, 1fr)); gap: 28px; margin-top: 28px; }
    .pub .hc-grid .daylabel { display: none; }           /* phones' day headings; cards use .hc-group */
    .pub .hc-group { grid-column: 1 / -1; margin: 6px 0 -8px; padding-bottom: 6px; border-bottom: 2px solid var(--text);
        font-size: 12px; font-weight: 800; letter-spacing: 0.1em; text-transform: uppercase; scroll-margin-top: 140px; }

    /* Every card: one column, actions pinned to the bottom. */
    .pub .hc-card { display: flex; flex-direction: column; gap: 10px; margin: 0; padding: 0; border: 0;
        border-radius: 10px; background: var(--surface); }
    .pub .hc-card .body { display: flex; flex-direction: column; gap: 8px; flex: 1 1 auto; }
    .pub .hc-card .src { margin-top: auto; }
    .pub .hc-card .title { font-family: var(--font-serif); font-size: 21px; line-height: 1.15; }
    .pub .hc-card .summary { -webkit-line-clamp: 3; line-clamp: 3; display: -webkit-box; -webkit-box-orient: vertical; overflow: hidden; }

    /* Photo card: full width, one crop for all. Overrides the classifier's frame ratio and pixel cap. */
    .pub .hc-card[data-image-layout='wide'] .thumb { width: 100%; max-width: none; aspect-ratio: 16 / 10; height: auto;
        border-radius: 10px; overflow: hidden; }
    .pub .hc-card[data-image-layout='wide'] .thumb img { width: 100%; height: 100%; object-fit: cover; object-position: center 30%; }

    /* Text card: no image area at all, tinted, coloured top edge, bigger headline. */
    .pub .hc-card:not([data-image-layout='wide']) { padding: 18px; background: var(--hc-tint, #f4f6fa);
        border-top: 4px solid var(--hc-cat, var(--accent)); }
    .pub .hc-card:not([data-image-layout='wide']) .thumb { display: none; }
    .pub .hc-card:not([data-image-layout='wide']) .title { font-size: 25px; font-weight: 800; }

    /* Lead: bigger everything; a lead without a good photo becomes a large text panel. */
    .pub .hc-lead .hc-card .title { font-size: 34px; font-weight: 800; line-height: 1.1; }
    .pub .hc-lead .hc-card[data-image-layout='wide'] .thumb { aspect-ratio: 16 / 9; }
    .pub .hc-lead .hc-card:not([data-image-layout='wide']) .title { font-size: 44px; }

    /* Jump to box. */
    .pub .hc-jump { display: grid; grid-template-columns: auto minmax(0, 1fr); gap: 14px 22px; align-items: center;
        margin: 0 0 26px; padding: 16px 18px; border-radius: 14px; background: var(--hc-tint, #f4f6fa); }
    .pub .hc-jump__label { margin: 0; font-size: 12px; font-weight: 800; letter-spacing: 0.1em; text-transform: uppercase; }
    .pub .hc-jump__row { display: grid; grid-template-columns: repeat(5, minmax(0, 1fr)); gap: 10px; }
    .pub .hc-jump__group, .pub .hc-jump__cat { display: flex; min-height: 52px; padding: 0 14px; border-radius: 10px;
        background: var(--surface); border: 1px solid var(--border); color: var(--text); text-decoration: none; }
    .pub .hc-jump__group { flex-direction: column; justify-content: center; }
    .pub .hc-jump__group span { font-size: 12px; color: var(--text-3); }
    .pub .hc-jump__group[aria-current] { background: var(--text); color: var(--surface); border-color: var(--text); }
    .pub .hc-jump__group[aria-current] span { color: inherit; opacity: 0.8; }
    .pub .hc-jump__cat { align-items: center; gap: 8px; border-left: 4px solid var(--cat); }
    .pub .hc-jump__cat b { margin-left: auto; font-family: var(--font-serif); font-size: 20px; color: var(--cat); }

    /* Pinned bar. */
    .pub .hc-bar { position: fixed; top: var(--site-header-h, 64px); left: 0; right: 0; z-index: 9; display: flex;
        align-items: center; gap: 6px; height: 60px; padding: 0 48px; background: var(--hc-tint, #f4f6fa);
        border-bottom: 1px solid var(--border); box-shadow: 0 6px 16px rgb(20 27 43 / 0.1); }
    .pub .hc-bar[hidden] { display: none; }
    .pub .hc-bar a { display: flex; align-items: center; gap: 6px; min-height: 44px; padding: 0 12px; border-radius: 8px;
        color: var(--text); font-size: 14px; font-weight: 700; white-space: nowrap; text-decoration: none; }
    .pub .hc-bar a[aria-current] { background: var(--text); color: var(--surface); }
    .pub .hc-bar__title { margin-right: 10px; font-family: var(--font-serif); font-size: 20px; }
    .pub .hc-bar__rule { width: 1px; height: 28px; margin: 0 6px; background: var(--border); }
    .pub .hc-bar__cat::before { content: ''; width: 8px; height: 8px; border-radius: 999px; background: var(--cat); }
    .pub .hc-bar__top { margin-left: auto; border: 1px solid var(--border); background: var(--surface); }
}
@media (min-width: 1024px) {
    .pub .hc-grid { grid-template-columns: repeat(3, minmax(0, 1fr)); }
}
@media (min-width: 744px) and (max-width: 1179px) {
    .pub .hc-bar__cat, .pub .hc-bar__rule { display: none; }   /* iPad: hours or days only */
    .pub .hc-jump__row { grid-template-columns: repeat(3, minmax(0, 1fr)); }
}
:root[data-theme='dark'] .pub { --hc-tint: #0d1a2f; }
```

Set `--hc-cat` per card from its category (an inline `style="--hc-cat: …"` in `FeedArticles` in cards mode, using the
same colour map as the box). Check the selectors against the current `article-images.css` rules for `.article` /
`.thumb`; the card rules must win on the All headlines pages only (they're scoped by `.hc-card`).

> **About the brief swap.** `article-images.js` marks every story `compact` until its image has loaded and been
> measured, so a photo card starts as a text card and turns into a photo card a moment later. Above the fold that's the
> lead and the three beside it; if the flash bothers you, keep the lead's `.thumb` visible (with its placeholder
> background) until the classifier has run, by adding `[data-image-layout]:not([data-image-measured])` to the photo
> rule and setting `data-image-measured` in `classify()`.

---

## Phase 4 — Verify (per AGENTS.md)

1. **Pages:** `/news/all`, `/news/all?category={id}`, `/sports/news`, `/sports/news/f1`, `/entertainment/news`,
   `/entertainment/news/tv-film`. Box office and Big 12 news unchanged.
2. **Cards:** a large photo → photo card at 16:10; a small, wide, tiny, broken or missing image → text card with no
   empty image area; a lead without a good photo → large text panel. Use the fixtures in
   `tests/browser/article-images.browser.cjs` and extend it with a cards case.
3. **Jump to:** News on a normal morning groups by hour; Sports/Entertainment by day; counts match the list for the
   current category, source and search; a group not loaded yet opens the list starting there; Earlier works;
   categories open their tab; the category row hides when a category is selected.
4. **Pinned bar:** appears once the box leaves the screen, hides when it comes back, highlights the group being read,
   Top ↑ returns; categories hide between 744 and 1179px; never appears below 744px or without JavaScript.
5. **Load more:** appends cards with the right group headings and no repeated heading at the seam.
6. **Phones** at 320, 390 and 430px: identical to today (the CSS is all behind 744px).
7. **Desktop / iPad** at 744, 1024, 1280 and 1440px, light and dark, two other colour schemes: no sideways scrolling;
   text cards readable in dark mode; focus rings visible on box and bar links; 44px targets.

## Suggested order of releases

1. Phase 1 + Phase 3 cards only (no box): the visible fix for the small thumbnails.
2. Phase 2: the box and the pinned bar.

## Release 1 record

Built October 29, 2026. Files: `FeedArticles.razor` (cards mode, `hc-group` headings, `mx-c-*` colour class),
`NewspaperFeed.razor` (lead + three, `hc-grid`), `News.razor` (`hc-feed`, hour/day rule, Load more attributes),
`FeedEndpoints.cs` (`cards`/`groupBy`/`group`, `lastGroup`), `FeedDates.cs` (card group helpers), `FeedQuery.cs`
(`Filtered`, `CountPublishedSinceAsync`), `public.js` (append into the grid), `article-images.js` (BBC for cards,
`data-image-measured`), new `headline-cards.css`.

Verified locally: `/news/all` and `/sports/news` at 744/1024/1440px light and dark (hour headings, photo and text
cards, no overflow); Load more three times (49 cards, all inside the grid, no repeated heading); phones at 390px on
`/sports/news` and a search (no group headings, one-column list unchanged). Browser suites: article-images (desktop
expectations moved to the card rules; the 2560px pixel-cap check moved to the News front), publisher-layout (now
`/news/all` and `.hc-side`), news-front, section-hubs, sports-news, entertainment-news, headlines-phone,
phone-screens, tablet-layout, saved-sections, video-share and section-select passed. mixed-search-nav fails the same
way without these changes (Where to watch search results). .NET: `HeadlineCardGroupTests` added; 998 passed.
