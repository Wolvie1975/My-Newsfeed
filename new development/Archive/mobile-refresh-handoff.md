# Mobile refresh handoff

Written October 28, 2026 (America/Chicago) against `develop` @ `2871b34`. Verify current repository state before
making changes; file names and selectors below are from that commit.

**Save as:** `docs/mobile-refresh-handoff.md`

**Approved mockups:** the myNewspaper canvas (https://claude.ai/artifact/JVQ8mU7R63Z1cziNpeW9Z5). Every artboard named
"… — phone, folded" is a target, plus four "ribbon" artboards. Press Play on an artboard to try the folds.

**Goal:** phone pages stop reading as a stack of identical white cards. Each page gets one colored title band, one dark
focal block, and everything past the first screen folds into one-line summaries that open in place, one at a time.

**Scope rule:** phones only (`max-width: 743px`, the existing phone breakpoint). Desktop and iPad portrait (744px and up,
per `tablet.css`) are untouched. All new CSS lives in a new `mobile.css` under a `.mx` prefix so nothing collides with
`public.css`, `sections.css` or `broadsheet.css`.

> **Feedback carried into these designs — keep it.**
> - No pill chips or segmented tabs on phones. Every filter is one native `<select>` in the ribbon.
> - Don't show everything at once. Past the lead and one short list, content folds.
> - Only one fold open at a time; the first one starts open.

---

## The pieces (shared by every page)

| Piece | What it is | Used on |
|---|---|---|
| **Ribbon** | Yellow band: a live line (red dot + small caps), a 44px serif title, one labelled `<select>` | Every page |
| **Dark block** | `#141b2b` band for the page's one focal item: lead story, featured video, game of the week, No. 1, the leader, today's weather | Every folded page |
| **Timeline** | Times down the left, colored dots on a rule, title + subline, a tag on the right (TV network, source) | Headlines, Sports, Scoreboard, College, F1, TV |
| **Top list** | Big serif numerals 1–3 (or more), title, colored category dot, Save button | Home, Headlines, Local, Top 25, Box office |
| **Fold group** | A heading, then rows: color edge + serif name + one-line gist + chevron; opens to a few rows + "See all →" | Every folded page |
| **Dark tiles** | 2–3 small cards inside the dark block (live scores, biggest moves, weather periods, weekend numbers) | Scoreboard, Top 25, Box office, Local |

### Category and status colors

| Token | Light | Use |
|---|---|---|
| `--mx-world` | `#0051ba` | World, default dots |
| `--mx-us` | `#b0001e` | U.S., "your team", drops, live |
| `--mx-local` | `#0f766e` | Kansas City, soccer |
| `--mx-business` | `#b45309` | Business, NFL |
| `--mx-tech` | `#6d28d9` | Technology, volleyball, "new" |
| `--mx-gain` | `#15803d` | ▲ movement, W streaks |

Dots and edges are decoration. Every color also comes with a word (the category name, ▲/▼, W/L), so nothing depends
on color alone.

---

## Phase 1 — `mobile.css` and five components

### 1.1 Add `wwwroot/mobile.css`

Create `src/myNewspaper.Web/wwwroot/mobile.css`:

```css
/* =====================================================================================================================
   Phone refresh. Everything is .mx-prefixed and only applies below 744px; desktop and iPad portrait never see it.
   Uses the public.css tokens (--text, --text-2, --surface, --border, --accent) so color schemes and dark mode carry over.
   ===================================================================================================================== */
.pub {
    --mx-ribbon-bg: #ffd34d;
    --mx-ribbon-fg: #141b2b;
    --mx-ribbon-kicker: #3a2e00;
    --mx-live: #b0001e;
    --mx-block-bg: #141b2b;
    --mx-block-fg: #ffffff;
    --mx-block-2: #c9d6ee;
    --mx-block-3: #9fb0d0;
    --mx-block-tile: #1f2940;
    --mx-block-line: #3a4766;
    --mx-block-accent: #ffd34d;
    --mx-band: #f4f6fa;
    --mx-world: #0051ba;
    --mx-us: #b0001e;
    --mx-local: #0f766e;
    --mx-business: #b45309;
    --mx-tech: #6d28d9;
    --mx-gain: #15803d;
    --mx-serif: 'Newsreader', Georgia, serif;
}
:root[data-theme='dark'] .pub {
    --mx-ribbon-bg: #3a2f0a;
    --mx-ribbon-fg: #fff3c4;
    --mx-ribbon-kicker: #ffd34d;
    --mx-live: #ff6b7d;
    --mx-block-bg: #050b17;
    --mx-block-tile: #111f36;
    --mx-block-line: #293d5d;
    --mx-band: #0d1a2f;
    --mx-world: #8bbcff;
    --mx-us: #ff8590;
    --mx-local: #5eead4;
    --mx-business: #fbbf24;
    --mx-tech: #c4b5fd;
    --mx-gain: #4ade80;
}

/* Phone-only and desktop-only wrappers. */
.mx-phone { display: none; }
@media (max-width: 743px) {
    .mx-phone { display: block; }
    .mx-desktop { display: none !important; }
}

/* ---------------------------------------------------------------- Ribbon */
.mx-ribbon {
    display: flex; flex-direction: column; gap: 12px;
    padding: 16px;
    background: var(--mx-ribbon-bg); color: var(--mx-ribbon-fg);
}
.mx-ribbon__kicker {
    display: flex; align-items: center; gap: 8px;
    font-size: 11px; font-weight: 800; letter-spacing: 0.14em; text-transform: uppercase;
    color: var(--mx-ribbon-kicker);
}
.mx-ribbon__kicker::before {
    content: ""; flex: none; width: 8px; height: 8px; border-radius: 999px;
    background: var(--mx-live); box-shadow: 0 0 0 4px color-mix(in srgb, var(--mx-live) 18%, transparent);
}
.mx-ribbon h1 {
    margin: 4px 0 0;
    font-family: var(--mx-serif); font-size: 44px; font-weight: 800; line-height: 0.95; letter-spacing: -0.02em;
    overflow-wrap: anywhere;
}
.mx-ribbon__field,
.mx-ribbon .section-select { display: flex; align-items: center; gap: 10px; margin: 0; }
.mx-ribbon .section-tabs { display: none; }                    /* SectionSelect's desktop tabs */
.mx-ribbon label { flex: none; font-size: 13px; font-weight: 800; color: var(--mx-ribbon-kicker); }
.mx-ribbon select,
.mx-ribbon input {
    flex: 1 1 auto; min-width: 0; width: 100%; min-height: 48px; padding: 0 12px;
    font: inherit; font-size: 16px; font-weight: 700;          /* 16px stops iOS zooming on focus */
    color: #141b2b; background: #ffffff;
    border: 2px solid #141b2b; border-radius: 10px;
}

/* ---------------------------------------------------------------- Dark block */
.mx-block {
    display: flex; flex-direction: column; gap: 14px;
    padding: 18px 16px;
    background: var(--mx-block-bg); color: var(--mx-block-fg);
}
.mx-block a { color: inherit; }
.mx-block__kicker { font-size: 11px; font-weight: 800; letter-spacing: 0.12em; text-transform: uppercase; color: var(--mx-block-accent); }
.mx-block__title { margin: 0; font-family: var(--mx-serif); font-size: 27px; font-weight: 800; line-height: 1.1; }
.mx-block__quote { margin: 0; font-family: var(--mx-serif); font-style: italic; font-size: 17px; line-height: 1.4; color: var(--mx-block-2); }
.mx-block__media { position: relative; display: grid; place-items: center; height: 200px; border-radius: 14px; overflow: hidden; background: var(--mx-block-tile); }
.mx-block__media img { width: 100%; height: 100%; object-fit: cover; }
.mx-block__badge {
    position: absolute; top: 12px; left: 12px; padding: 5px 10px; border-radius: 6px;
    background: #b0001e; color: #ffffff; font-size: 11px; font-weight: 800; letter-spacing: 0.08em; text-transform: uppercase;
}
.mx-block__actions { display: flex; gap: 8px; }
.mx-block__actions button,
.mx-block__ghost {
    display: inline-flex; align-items: center; justify-content: center; gap: 8px; min-height: 44px; padding: 0 14px;
    border: 1px solid var(--mx-block-line); border-radius: 10px; background: transparent; color: var(--mx-block-fg);
    font: inherit; font-size: 14px; font-weight: 700;
}
.mx-block__cta {
    display: flex; align-items: center; justify-content: center; min-height: 48px; border-radius: 10px;
    background: var(--mx-block-accent); color: #141b2b !important; font-size: 15px; font-weight: 800; text-decoration: none;
}
.mx-big { font-family: var(--mx-serif); font-weight: 800; line-height: 0.85; color: var(--mx-block-accent); }

/* Small cards inside the dark block. */
.mx-tiles { display: grid; grid-template-columns: repeat(var(--mx-tile-cols, 3), minmax(0, 1fr)); gap: 8px; }
.mx-tile { display: flex; flex-direction: column; gap: 3px; padding: 10px 12px; border-radius: 12px; background: var(--mx-block-tile); min-width: 0; }
.mx-tile__label { font-size: 11px; font-weight: 800; text-transform: uppercase; color: var(--mx-block-3); }
.mx-tile__value { font-family: var(--mx-serif); font-size: 22px; font-weight: 800; line-height: 1; }
.mx-tile__sub { font-size: 12px; color: var(--mx-block-2); }

/* ---------------------------------------------------------------- Section heading (double rule) */
.mx-head {
    display: flex; align-items: center; justify-content: space-between;
    margin: 0 0 4px; padding-bottom: 6px; border-bottom: 3px solid var(--text);
}
.mx-head h2 { margin: 0; font-size: 12px; font-weight: 800; letter-spacing: 0.12em; text-transform: uppercase; }
.mx-head a { display: flex; align-items: center; min-height: 44px; font-size: 13px; font-weight: 800; color: var(--accent); }
.mx-section { padding: 20px 16px 4px; }

/* ---------------------------------------------------------------- Timeline */
.mx-timeline { padding: 20px 16px 8px; background: var(--mx-band); }
.mx-timeline__top { display: flex; align-items: baseline; justify-content: space-between; }
.mx-timeline__top h2 { margin: 0; font-family: var(--mx-serif); font-size: 28px; font-weight: 800; }
.mx-timeline__top span { font-size: 13px; font-weight: 700; color: var(--text-2); }
.mx-timeline ol { list-style: none; margin: 14px 0 0 7px; padding: 0 0 0 18px; border-left: 2px solid var(--border); }
.mx-timeline li { position: relative; padding: 8px 0 8px 10px; }
.mx-timeline li::before {
    content: ""; position: absolute; left: -26px; top: 15px; width: 14px; height: 14px; border-radius: 999px;
    background: var(--dot, var(--mx-world)); border: 3px solid var(--mx-band);
}
.mx-timeline li > a { display: grid; grid-template-columns: 74px minmax(0, 1fr) auto; gap: 8px; align-items: baseline; color: inherit; text-decoration: none; }
.mx-timeline__time { font-size: 13px; font-weight: 800; color: var(--dot, var(--mx-world)); }
.mx-timeline__title { font-size: 15px; font-weight: 700; line-height: 1.25; }
.mx-timeline__sub { display: block; font-size: 12px; color: var(--text-3); }
.mx-timeline__tag { font-size: 12px; font-weight: 800; color: var(--text-2); }
.mx-more {
    display: flex; align-items: center; justify-content: center; min-height: 48px; margin: 10px 0 8px;
    border: 2px solid var(--text); border-radius: 10px; background: var(--surface); color: var(--text);
    font-size: 15px; font-weight: 800; text-decoration: none;
}

/* ---------------------------------------------------------------- Top list */
.mx-top { list-style: none; margin: 0; padding: 0; }
.mx-top li { display: grid; grid-template-columns: 40px minmax(0, 1fr) 44px; gap: 8px; align-items: start; padding: 12px 0; border-bottom: 1px solid var(--row-border); }
.mx-top__n { font-family: var(--mx-serif); font-size: 38px; font-weight: 800; line-height: 0.9; color: var(--accent); }
.mx-top__title { display: block; font-size: 16px; font-weight: 700; line-height: 1.3; color: var(--text); }
.mx-top__meta { display: flex; align-items: center; gap: 6px; margin-top: 4px; font-size: 12px; color: var(--text-3); }
.mx-top__meta::before { content: ""; width: 8px; height: 8px; border-radius: 999px; background: var(--dot, var(--mx-world)); }

/* ---------------------------------------------------------------- Fold group (native <details>, one open at a time) */
.mx-folds { padding: 22px 16px 28px; }
.mx-fold { border-bottom: 1px solid var(--row-border); }
.mx-fold > summary {
    display: grid; grid-template-columns: 6px minmax(0, 1fr) 24px; gap: 12px; align-items: center;
    min-height: 64px; padding: 8px 0; cursor: pointer; list-style: none;
}
.mx-fold > summary::-webkit-details-marker { display: none; }
.mx-fold > summary::before { content: ""; align-self: stretch; border-radius: 3px; background: var(--edge, var(--mx-world)); }
.mx-fold > summary::after {
    content: ""; display: block; width: 20px; height: 20px; justify-self: center;
    background: var(--text);
    -webkit-mask: url("data:image/svg+xml,%3Csvg xmlns='http://www.w3.org/2000/svg' viewBox='0 0 24 24' fill='none' stroke='black' stroke-width='2.4' stroke-linecap='round' stroke-linejoin='round'%3E%3Cpath d='m6 9 6 6 6-6'/%3E%3C/svg%3E") center / 20px no-repeat;
            mask: url("data:image/svg+xml,%3Csvg xmlns='http://www.w3.org/2000/svg' viewBox='0 0 24 24' fill='none' stroke='black' stroke-width='2.4' stroke-linecap='round' stroke-linejoin='round'%3E%3Cpath d='m6 9 6 6 6-6'/%3E%3C/svg%3E") center / 20px no-repeat;
}
.mx-fold[open] > summary::after { transform: rotate(180deg); }
@media (prefers-reduced-motion: no-preference) { .mx-fold > summary::after { transition: transform 0.15s ease; } }
.mx-fold__name { display: block; font-family: var(--mx-serif); font-size: 20px; font-weight: 800; line-height: 1.1; }
.mx-fold__gist { display: block; font-size: 13px; color: var(--text-2); white-space: nowrap; overflow: hidden; text-overflow: ellipsis; }
.mx-fold__body { margin: 0 0 14px 18px; padding: 4px 0 4px 12px; border-left: 3px solid var(--edge, var(--mx-world)); }
.mx-fold__row {
    display: grid; grid-template-columns: minmax(0, 1fr) auto; gap: 10px; align-items: baseline;
    padding: 9px 0; border-bottom: 1px solid var(--row-border); color: var(--text); text-decoration: none;
}
.mx-fold__row b { font-size: 15px; line-height: 1.3; }
.mx-fold__row small { display: block; font-size: 12px; color: var(--text-3); }
.mx-fold__value { font-family: var(--mx-serif); font-size: 18px; font-weight: 800; color: var(--edge, var(--mx-world)); }
.mx-fold__all { display: flex; align-items: center; min-height: 44px; font-size: 14px; font-weight: 800; color: var(--edge, var(--mx-world)); }

/* Category helpers for --dot / --edge. */
.mx-c-world { --dot: var(--mx-world); --edge: var(--mx-world); }
.mx-c-us { --dot: var(--mx-us); --edge: var(--mx-us); }
.mx-c-local { --dot: var(--mx-local); --edge: var(--mx-local); }
.mx-c-business { --dot: var(--mx-business); --edge: var(--mx-business); }
.mx-c-tech { --dot: var(--mx-tech); --edge: var(--mx-tech); }
.mx-c-gain { --dot: var(--mx-gain); --edge: var(--mx-gain); }
.mx-c-ink { --dot: var(--text); --edge: var(--text); }
```

Add it to `Components/App.razor` after `tablet.css` and before `admin.css`:

```html
<link rel="stylesheet" href="@Assets["mobile.css"]" />
```

> **Why `<details name="…">` for folds.** Most public pages render statically (no `@rendermode`), so a fold must work
> without a Blazor circuit. Native `<details>` opens and closes with no JavaScript, is announced as expandable by screen
> readers, and the `name` attribute makes a group exclusive (opening one closes the others) in Chrome 120+, Safari 17.2+
> and Firefox 130+. On an older browser several can be open at once, which is harmless.

### 1.2 Components (`Components/Phone/`)

Create the folder and add `@using myNewspaper.Web.Components.Phone` to `Components/_Imports.razor`.

**`PhoneRibbon.razor`** — the title band. The select is passed in as child content so each page keeps its existing
form/`navigate` behaviour.

```razor
<header class="mx-ribbon">
    <div>
        <span class="mx-ribbon__kicker">@Kicker</span>
        <h1>@Title</h1>
    </div>
    @Extra
    @ChildContent
</header>

@code {
    [Parameter, EditorRequired] public string Kicker { get; set; } = "";
    [Parameter, EditorRequired] public string Title { get; set; } = "";
    /// <summary>Optional row between the title and the select (date stepper, search box).</summary>
    [Parameter] public RenderFragment? Extra { get; set; }
    /// <summary>The one select, wrapped in &lt;div class="mx-ribbon__field"&gt; with its label.</summary>
    [Parameter] public RenderFragment? ChildContent { get; set; }
}
```

**`SectionSelect.razor` change** — the ribbon always wants the dropdown, never the segmented control. Add a parameter
and use it in the class switch:

```razor
<div class="section-nav @(Choices.Count <= SegmentedLimit && !AlwaysSelect ? "section-nav--few" : "section-nav--many")">
```

```csharp
/// <summary>Phones: always show the dropdown (the ribbon has no room for pills or segments).</summary>
[Parameter] public bool AlwaysSelect { get; set; }
```

Placed inside `<PhoneRibbon>`, `mobile.css` hides its tabs and styles its form as the ribbon field, so no other change is
needed. Give the phone copy its own `Id` (e.g. `sports-section-m`).

**`FoldGroup.razor` and `Fold.razor`**

```razor
@* FoldGroup.razor *@
<section class="mx-folds" aria-labelledby="@($"{Name}-h")">
    <div class="mx-head"><h2 id="@($"{Name}-h")">@Heading</h2></div>
    <CascadingValue Value="this" IsFixed="true">@ChildContent</CascadingValue>
    @if (Foot is not null) { <a class="mx-more" href="@FootHref">@Foot</a> }
</section>

@code {
    /// <summary>Shared &lt;details name&gt;; must be unique on the page.</summary>
    [Parameter, EditorRequired] public string Name { get; set; } = "";
    [Parameter, EditorRequired] public string Heading { get; set; } = "";
    [Parameter] public string? Foot { get; set; }
    [Parameter] public string? FootHref { get; set; }
    [Parameter] public RenderFragment? ChildContent { get; set; }
}
```

```razor
@* Fold.razor *@
<details class="mx-fold @Color" name="@Group.Name" open="@Open">
    <summary>
        <span><span class="mx-fold__name">@Title</span><span class="mx-fold__gist">@Gist</span></span>
    </summary>
    <div class="mx-fold__body">
        @ChildContent
        @if (AllHref is not null) { <a class="mx-fold__all" href="@AllHref">@AllText →</a> }
    </div>
</details>

@code {
    [CascadingParameter] public FoldGroup Group { get; set; } = default!;
    [Parameter, EditorRequired] public string Title { get; set; } = "";
    [Parameter, EditorRequired] public string Gist { get; set; } = "";
    /// <summary>One of the .mx-c-* classes.</summary>
    [Parameter] public string Color { get; set; } = "mx-c-world";
    [Parameter] public bool Open { get; set; }
    [Parameter] public string? AllHref { get; set; }
    [Parameter] public string AllText { get; set; } = "See all";
    [Parameter] public RenderFragment? ChildContent { get; set; }
}
```

Rows inside a fold are plain markup so each page can use its own links:

```html
<a class="mx-fold__row" href="…"><span><b>Kansas at TCU</b><small>Sat, Oct 31 · 2:30 PM · FS1</small></span><span class="mx-fold__value">3-4</span></a>
```

**`PhoneTimeline.razor`** — takes a list of a small record so pages map their own data:

```csharp
// Components/Phone/TimelineItem.cs
namespace myNewspaper.Web.Components.Phone;
public sealed record TimelineItem(string Time, string Title, string? Sub, string? Tag, string Href, string Color = "mx-c-world");
```

```razor
<section class="mx-timeline">
    <div class="mx-timeline__top"><h2>@Heading</h2><span>@Sub</span></div>
    @ChildContent
    <ol>
        @foreach (var i in Items) {
            <li class="@i.Color">
                <a href="@i.Href">
                    <span class="mx-timeline__time">@i.Time</span>
                    <span><span class="mx-timeline__title">@i.Title</span><span class="mx-timeline__sub">@i.Sub</span></span>
                    <span class="mx-timeline__tag">@i.Tag</span>
                </a>
            </li>
        }
    </ol>
    @if (MoreText is not null) { <a class="mx-more" href="@MoreHref">@MoreText →</a> }
</section>

@code {
    [Parameter, EditorRequired] public string Heading { get; set; } = "";
    [Parameter] public string? Sub { get; set; }
    [Parameter, EditorRequired] public IReadOnlyList<TimelineItem> Items { get; set; } = [];
    [Parameter] public string? MoreText { get; set; }
    [Parameter] public string? MoreHref { get; set; }
    /// <summary>Optional content above the list (Sports puts live score tiles here).</summary>
    [Parameter] public RenderFragment? ChildContent { get; set; }
}
```

The dark block and the top list vary too much per page to be worth a component; use the `.mx-block*`, `.mx-tile*` and
`.mx-top*` classes directly.

### 1.3 How a page adopts it

Each page keeps its current markup for desktop and adds a phone version beside it, fed from the same data it already
loads (no second query):

```razor
<div class="mx-desktop">
    @* existing page content, unchanged *@
</div>
<div class="mx-phone">
    <PhoneRibbon Kicker="…" Title="…"> … </PhoneRibbon>
    <section class="mx-block"> … </section>
    <PhoneTimeline … />
    <FoldGroup Name="sports-more" Heading="More in Sports"> <Fold Open="true" …>…</Fold> … </FoldGroup>
</div>
```

Hide the old phone-only page heading on converted pages (`.mx-desktop` already covers it if `PageHeading` sits inside).

> **Watch the duplicate IDs.** Both copies are in the DOM. Give phone controls their own IDs (`sports-section-m`), and
> keep only one copy of anything that `public.js` binds by ID (Save buttons use `data-*`, so they are fine).

---

## Phase 2 — Page specs

Every folded page: ribbon → dark block → (one middle list) → fold group. "First open" is the fold that starts open.
Values in *italics* on the canvas are sample data; the column "Data" says where the real value comes from or flags it.

### Home (`/`, `Home.razor`) — artboard "Home — phone, folded"

| Part | Content |
|---|---|
| Ribbon | Kicker = the date; title **Good afternoon** (morning/evening by time); today's weather as one line of four periods; the Where to watch search box instead of a select |
| Dark block | Lead story (`HomeLeadStory` data) with Save/Share |
| Middle | "Today & Tonight": live game tiles, then an "Up next" timeline with sport-colored dots; then top 3 "Headlines" |
| Folds ("More today") | Sports (open), Scoreboard, Top 25, Entertainment, Box office, Videos, Coming to screens, Opinion |

### Headlines (`/news`, `News.razor`) — "Headlines — phone, folded"

| Part | Content | Data |
|---|---|---|
| Ribbon | "Updated 2 min ago · 80 stories · 9 sources" / **The day so far** / select **Category** (replaces `hn-cats` pills) | existing feed counts |
| Dark block | Lead story, "Developing" badge, one-line standfirst | first story of Top Headlines |
| Middle | Top 3 ("Most read this morning"), then "This hour" timeline (last 3 stories, colored by category) | **flag:** there are no read counts today — label it "Top stories" and use feed order until reads are tracked |
| Folds | Kansas City (open), World, U.S., Business, Technology, Opinion | 2–3 latest per category; gist = first 1–2 titles |

### Sports (`/sports`, `SportsFront.razor`) — "Sports — phone, folded"

Ribbon select **Section** (`SectionChoices.Sports`, `AlwaysSelect`). Dark block = the featured game/story. Timeline
"Today's games" with live score tiles above it. Top 3 sports headlines. Folds ("More in Sports"): Big 12 standings
(open), AP Top 25, F1, College Sports, More headlines.

### Scoreboard (`/scoreboard`, `Scoreboard.razor`) — "Scoreboard — phone, folded"

| Part | Content |
|---|---|
| Ribbon | "5 live now · 11 more today" / **Scoreboard** / date stepper (prev ◂ date ▸ next, 48px buttons) / select **Sport** |
| Dark block | "Live now" — up to 4 live games as 2-column tiles (league + clock, both teams, serif scores) |
| Middle | "Up next" timeline: start time, matchup, league, TV tag |
| Folds | One per league: gist "5 games · 4 live · Leeds at Sunderland 11:30"; rows = matchup + clock/TV + score. First open: the league with the most live games |

The old All / Live / Final segmented control goes away; "Live now" covers it, and Final games stay in each league's fold.

### Top 25 (`/rankings`, `Rankings.razor`) — "Top 25 — phone, folded"

| Part | Content |
|---|---|
| Ribbon | "Poll dated Oct 18 · Big 12 highlighted" / **Top 25** / select **Poll** |
| Dark block | No. 1 with a 104px numeral and "up 2"; "Biggest moves" tiles (two largest rises, largest drop) |
| Middle | 2–10: numeral, school, Big 12 tag, record, ▲/▼/NEW (Big 12 rows tinted) |
| Folds | 11–25 (open), Big 12 in the poll, New this week, Biggest drops |

Moves are computed from rank and previous rank, which the page already shows.

### Standings (`/standings`, `Standings.razor`) — "Standings — phone, folded"

| Part | Content | Data |
|---|---|---|
| Ribbon | "Big 12 Football · 2026 season" / **Standings** / select **League** (replaces `st-leagues` pills) | |
| Dark block | First place (team, conference record big, overall + streak); "Your teams" tiles with place, record, streak, next game | **My Teams** setting; next game from the schedule |
| Middle | Places 1–6 table with a conference-win bar | |
| Folds | Places 7–16 (open), This week's games, Big 12 in the AP poll | |

### College Sports (`/college`, `College.razor`) — "College Sports — phone, folded"

| Part | Content |
|---|---|
| Ribbon | "Big 12 · Week 10 · times are Central" / **College Sports** / select **Sport** (the View select goes; its views become folds and links) |
| Dark block | Game of the week: both teams with rank and record, kickoff, network, "Game details" |
| Middle | Saturday timeline; your teams' games get the red dot and "your team" in the subline |
| Folds | Your teams (open), Friday night, Big 12 standings, Big 12 in the Top 25, Women's Volleyball |

> **Game of the week** needs a rule: pick the game between two ranked teams with the best combined rank; else the game
> involving the highest-ranked team; else the first game of the day.

### F1 (`/f1`, `F1.razor` / `F1Home.razor`) — "F1 — phone, folded"

Ribbon select **View** (replaces `F1Tabs` on phones). Dark block = next or current race, start time as a 60px numeral,
"Weekend timetable". Middle = championship top 3 with a points bar against the leader and the gap. Timeline "Next up"
(3 rounds). Folds: Drivers' standings (open), Constructors' standings, F1 headlines.
**Flag:** constructors and F1 headlines are placeholders on the canvas; use whatever the F1 page already has, or drop
those folds.

### Movies (`/movies`, `Movies.razor`) — "Movies — phone, folded"

Ribbon select **Release type** (`MovieViewSelect`). A 7-day strip under the ribbon (today in dark, a red dot on days with
releases). Dark block = the next release this week (poster, service, "Streaming now"). Folds: Looking ahead (open),
In theaters (box office top 3), Trending.

### TV & Streaming (`/tv`, `Tv.razor`) — "TV & Streaming — phone, folded"

Ribbon "Your episode calendar · N tracked" / select **Week** (This / Next / Last). Dark block = tonight's episode, or the
next one. Timeline "This week" (day, show, episode, network tag). Folds: Your shows (open; per-profile once profiles
ship), Trending TV.

### Digital releases (`/movies/digital`, `DigitalReleases.razor`) — "Digital releases — phone, folded"

Ribbon select **Source** (replaces the 3-way segmented control). Dark block holds the empty state ("No confirmed dates
yet" + yellow "Try …" button + two outline buttons) or, when there are dates, the next release. Folds: Coming soon to
digital (open), Still in theaters.

### Box office (`/box-office`, `BoxOffice.razor`) — "Box office — phone, folded"

| Part | Content |
|---|---|
| Ribbon | "Estimates · The Numbers" / **Box office** / select **View** (Results · weekend / Next weekend / Forecasts) |
| Dark block | No. 1 with a 96px numeral, title, gross; tiles: number of openers, best hold, biggest drop |
| Middle | 1–5 with a gross bar scaled to No. 1, change %, NEW badge |
| Folds | 6–10 (open), Biggest totals, Next weekend (from `BoxOfficeForecastSection`) |

### Where to watch (`/watch`, `Watch.razor`) — "Where to watch — phone, folded"

Ribbon "Movies & TV · N services chosen" / **Where to watch** / search box + dark Search button as the extra row /
select **Browse** (Featured / Movies / TV shows). Dark block = numbered "Trending movies" poster rail (scrolls
sideways inside the block only). Folds: Trending TV (open), New movies you can watch, Your services.

### Local (`/local`, `Local.razor`) — "Local — phone, folded"

Ribbon "Closer to home · updated 10:29 AM" / **Kansas City** / select **Section** (Overview / News / Weather / Events).
Dark block = today's temperature (80px), summary, wind, "Use my location", three period tiles, "National Weather
Service". Top 3 local news. Folds: More local news (open), Forecast, Things to do.

### YouTube (`/videos`, `Videos.razor`) — "YouTube — phone, folded with dark featured block"

Ribbon select **Channel**. Dark block = featured video with a play button, "Watch later" and Share. "Latest" list.
Folds ("By channel"): Big 12 Sports (open), Tech, Games, Politics, Watch later.

### Entertainment (`/entertainment`, `EntertainmentFront.razor`) — "Entertainment — phone, folded"

Ribbon select **Section** (`SectionChoices.Entertainment`, `AlwaysSelect`). Dark block = lead story. "On screen"
timeline. Top 3 entertainment headlines. Folds ("More in Entertainment"): Weekend box office (open), Tonight on TV,
Movies this week, Friday in theaters, More headlines.

### Ribbon-only pages (no folds)

These are long lists or tools; folding would get in the way.

| Page | Change |
|---|---|
| Sports · All headlines (`/sports/news`) | `PageHeading` → ribbon "Sports · every story, every source" / **Sports headlines** / **Section**; first article becomes a dark card (dark background, white title, yellow kicker). Sport and Source selects stay as they are |
| Entertainment · All headlines (`/entertainment/news`) | Same treatment with **Entertainment headlines** |
| Saved (`/saved`) | Ribbon "Shared across your devices" / **Saved** / select **Show** (All · n, Articles · n, Videos · n) replacing the segmented control; the first saved item becomes a dark card |
| Settings (`/settings`, `MyTeams.razor`) | Ribbon "Make it yours · saved on this profile" / **Settings** / select **Jump to** that scrolls to each section (`location.hash` on change; works without JS via a Go button) |

> **Dark card on white list pages:** use `.mx-block` with `border-radius: 16px` on the first `<article>` only. Every
> button inside needs `color: var(--mx-block-fg)`; the Saved mockup first shipped with an invisible Share icon because
> the icon kept the dark ink color.

---

## Phase 3 — Verify (per AGENTS.md)

Run at 320, 390 and 430px, light and dark, and at least two other color schemes (`monochrome`, `cardinals`):

1. **Ribbon:** the title wraps at 320px without overflow ("Entertainment headlines", "Digital releases"); the select
   never pushes past the right edge with long options ("Women's Volleyball · AVCA Top 25"). Font size stays 16px.
2. **Folds:** with JavaScript off, every fold opens and closes; opening one closes the others in the same group; the
   first one starts open; keyboard Enter/Space toggles; VoiceOver/TalkBack announce "collapsed/expanded".
3. **Dark block:** text contrast ≥ 4.5:1 for body, ≥ 3:1 for the big numerals; in dark mode the block still separates
   from the page (`--mx-block-bg` is darker than `--bg`).
4. **Touch targets:** summaries ≥ 64px tall, all buttons and links ≥ 44px.
5. **Data edges:** no live games (Scoreboard dark block hides; "Up next" leads), empty folds (hide the fold, don't show
   "0"), a poll with no previous ranks (no moves tiles), Digital releases empty state, a league with one game.
6. **Desktop/iPad:** at 744, 1024 and 1440px nothing changes from today.
7. **Duplicate IDs:** run the browser's accessibility check; no repeated `id` on converted pages.

Record widths, themes and outcomes in the release notes, as AGENTS.md requires.

---

## Suggested order of releases

1. `mobile.css`, the components, the `SectionSelect` parameters, and **Headlines** (the pattern the rest copy).
2. **Home**, **Sports**, **Scoreboard**.
3. **Top 25**, **Standings**, **College Sports**, **F1**.
4. **Entertainment**, **Movies**, **TV & Streaming**, **Digital releases**, **Box office**.
5. **Where to watch**, **Local**, **YouTube**.
6. Ribbon-only pages: both All headlines pages, **Saved**, **Settings**.

Each release is independent: a page that hasn't been converted keeps its current phone layout.

## Open questions

- **"Most read"** on Home and Headlines needs read counts the site doesn't collect. Use feed order and call it "Top
  stories" until then, or add a lightweight `MyDailyFeedArticleReads` table later.
- **Game of the week** (College) and **"Your teams"** (Standings, College) depend on the profile's My Teams; for Guest,
  fall back to the first game / hide the tiles.
- **Ribbon color per scheme:** yellow is the default for every scheme. If a scheme looks wrong (e.g. one already built
  around yellow), override `--mx-ribbon-bg` under `:root[data-color-scheme='…']` in `mobile.css`.
