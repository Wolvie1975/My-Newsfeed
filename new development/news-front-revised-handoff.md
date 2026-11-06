# News front, revised: fill the gaps

Written November 4, 2026 (America/Chicago), against `develop` at `7370fbf`. Follows
[the .NET coding standard](dotnet-coding-standard.md) and [the coding standards addendum](coding-standards.md).

**Save as:** `docs/news-front-revised-handoff.md`

**Mockup:** the myNewspaper canvas, **News front, revised** page (https://claude.ai/artifact/JVQ8mU7R63Z1cziNpeW9Z5).
It's drawn at 1920 CSS px with today's headlines. Yellow "NEW" tags mark additions. Bracketed headlines are
placeholders, and the source counts in Most covered are illustrative.

**Scope:**

- `Components/NewsFront.razor`, desktop and phone halves;
- three small query additions in Shared;
- a forecast-alerts call in `LocalWeather`;
- a one-line prefill in `TopicFollow.razor`;
- CSS in `sections.css`;
- one Admin data step (the World category).

No new tables or migrations.

## Summary

| # | Change | Data |
|---|---|---|
| 1 | **Bug:** Top headlines under the lead are hidden from 1920px up by an Entertainment-only rule | — |
| 2 | Remove **From Tech** (desktop desk and phone fold); replace the desks with **U.S. News**, **Top Headlines** and **World** | Existing, plus a World category |
| 3 | **Briefing** tiles at a fixed width, four across: add **Your top topic** and **Most covered**; a **weather alert** tile only during an NWS alert | Existing, plus the NWS alerts endpoint |
| 4 | **Most covered** under the lead (Tech's grouping, made hub-aware) | Existing |
| 5 | **Trending in headlines** (with Follow) under Your topics | Existing |
| 6 | More rows: The day so far 14, Kansas City 5, Opinion 4 | Existing |
| 7 | **Cascade** the side region from 1920 up, so its boxes (including the desks) fill balanced columns and nothing ends early | — |

Order: 1 first (it's a one-line fix on its own), then 2–7 in any order. Each checkbox below is one step to tick off.

---

## 1. Bug: Top headlines hidden on wide screens

- [ ] **Fix the selector.** `sections.css` ~line 5585, inside the Entertainment `@media (min-width: 1920px)` block:

```css
/* Column 1: the lead at a sensible size; Latest headlines hidden (the categories show the same stories). */
.pub .ef-main :is(.ef-headlines, .ef-label:has(+ .ef-headlines)) {
    display: none;
}
```

`.ef-main` is also on the News and Sports fronts (`ef-main spf-main`), so this hides **News's Top headlines** (and
Sports's Latest headlines) from 1920px up. That's the empty space under the lead in your screenshot. Scope it to the
Entertainment frame, and check the two `.ef-main .ef-lead__…` rules just above it for the same leak:

```css
.pub .ef-cols > .ef-main :is(.ef-headlines, .ef-label:has(+ .ef-headlines)) {
    display: none;
}
```

- [ ] **Rename the News list.** With step 2 adding a **Top Headlines** desk (the category), the main column's list,
  which is the hourly mix of every desk, would read "Top headlines" twice on one page. Rename the main column's
  `<h3 class="ef-label">Top headlines</h3>` to **More headlines**, on desktop and on the phone section heading.

## 2. Desks: remove From Tech, add U.S. News, Top Headlines, World

- [ ] **Remove From Tech** from `NewsFront.razor`:
  - the desktop `@if (fromTech.Count > 0) { <section class="nf-desk" …>From Tech… }` block;
  - `|| fromTech.Count > 0` in the `@if` around `.nf-desks`;
  - the phone `<Fold Title="From Tech" …>` block;
  - the `fromTech` field;
  - `fromTech = await FeedQuery.GetHomeNewsAsync(db, 4, "tech");`;
  - `fromTech` in the catch's reset line.
- [ ] **New desks.** Replace `DeskNames` with the News categories (they're still in the `headlines` hub):

```csharp
// The desks below (or, from 1920px, inside) the side column, in the design's order. A desk shows when its
// category exists in the headlines hub and has stories; World appears once its category exists (step 2b).
private static readonly string[] DeskNames = ["U.S. News", "Top Headlines", "World"];
```

  Check the exact category names in Admin → Categories.
- [ ] **Order the tabs.** Add `"U.S. News"` and `"World"` to `SectionChoices.NewsOrder` after `"Opinion"`, so the tabs
  read News home · All headlines · Top Headlines · Kansas City · Opinion · U.S. News · World.
- [ ] **Desks on phones.** These desks show as folds through the existing `@foreach (var desk in desks…)`. Nothing else
  to do.

### 2b. The World category (Admin data step, no code)

- [ ] **Create the category.** Admin → Categories: create **World**, hub **News** (`headlines`).
- [ ] **Add three to five world feeds.** Admin → Sources: add them under World. Candidates, but **confirm each feed
  URL still works and allows use before adding it**:
  - BBC News World (`feeds.bbci.co.uk/news/world/rss.xml`);
  - The Guardian World (`theguardian.com/world/rss`);
  - Al Jazeera (`aljazeera.com/xml/rss/all.xml`);
  - NPR World (`feeds.npr.org/1004/rss.xml`);
  - DW (`rss.dw.com/rdf/rss-en-world`).

  The mockup's Reuters and AP don't, as far as I know, publish public RSS any more, so don't plan on them.
- [ ] **Check the scraper picks them up.** If the scraper collects every enabled source, the World desk fills after
  its next run. If it needs a provider registration, that's a scraper-side note for Jason: a new source category only,
  with no schema change.
- [ ] **Before World has stories**, the desk doesn't render (the loop skips empty desks), so this step can land later.

## 3. Briefing tiles

- [ ] **Fixed width.** Today `.nf-brief__tiles` uses `grid-auto-columns: minmax(0, 1fr)`, so two tiles stretch across
  1,900px. In `sections.css`, at the desktop block (~line 3056, `minmax(0, 220px)`) and the 1920 block (~line 5473),
  make it:

```css
.pub .nf-brief__tiles {
    grid-auto-columns: minmax(0, 300px);   /* tiles keep their size; the row doesn't stretch */
    justify-content: start;
}
```

  Then remove the 1920 `repeat(auto-fit, minmax(260px, 1fr))` override.

- [ ] **Add the new tiles** in `Tiles`, in this order:
  1. **Since last visit** (unchanged);
  2. **Kansas City now** (unchanged);
  3. **Your top topic** (new);
  4. **Most covered** (new);
  5. **Election Day** (unchanged, while configured);
  6. **Weather alert** (new, only during an alert).

  | Tile | Shows when | Label · value · sub | Link | Colour |
  |---|---|---|---|---|
  | Your top topic | The profile has a topic with at least 1 new | "Your top topic" · topic name · "33 new since your last visit" | `/news/topics/{id}` | `mx-c-tech`, or the topic's hub colour |
  | Most covered | Step 4 found a story with 3+ sources today | "Most covered" · "6 sources" · the lead headline, one line with ellipsis | The story's URL | `mx-c-us` |
  | Weather alert | NWS reports an active alert for the forecast point | "Weather alert" · the event ("Wind Advisory") · "until 9 PM" | `news/kansas-city#local-weather` | amber (below) |

  **Your top topic** is the `topics` entry with the most new items (`TopicQuery.SummariesAsync` is already loaded).
  Ties go to the newest topic.

- [ ] **Weather alerts.** Add to `LocalWeather` (Shared), next to `GetAsync`:

```csharp
public sealed record WeatherAlert(string Event, string Headline, string Severity, DateTimeOffset? Ends);

/// <summary>Active NWS alerts for the forecast point; empty on any failure. Cached 5 minutes.</summary>
public async Task<IReadOnlyList<WeatherAlert>> GetAlertsAsync(CancellationToken cancellationToken, double? latitude = null, double? longitude = null)
```

  - **Endpoint:** `https://api.weather.gov/alerts/active?point={lat},{lon}`, with the same client, User-Agent
    (`CoreHostLabels`) and `api.weather.gov` host check as the forecast.
  - **Fields:** `features[].properties.event`, `.headline`, `.severity` and `.ends` (falling back to `.expires`).
  - **Behaviour:** cache it with the same `IMemoryCache` for **5 minutes**, apply a **3-second** timeout as the News
    front does for the forecast, and treat every failure as "no alert".
  - **Tile colour:** add `mx-c-alert` in `mobile.css` (`--edge: #b45309`; dark: `#fbbf24`). Use it for **Moderate**
    and **Minor** alerts. **Severe** and **Extreme** alerts use `mx-c-us` (red).

- [ ] **Phone.** The phone tiles (`.nf-brief--phone`) reuse `Tiles`, so they get the same tiles, two per row. Check
  that six tiles still look right at 320.

## 4. Most covered on News

Tech already has this. `TechQuery.MostCoveredAsync` and the pure `TechQuery.GroupMostCovered` (with tests) group
headlines from different sources that share enough title words. The query is fixed to the `tech` hub.

- [ ] **Make it hub-aware.** Move the grouping and the cached query into `myNewspaper.Shared/Data/CoveredQuery.cs`:

```csharp
public static class CoveredQuery
{
    public static IReadOnlyList<CoveredStory> Group(IEnumerable<FeedItem> items, int minSources, int take = int.MaxValue); // was TechQuery.GroupMostCovered
    public static Task<IReadOnlyList<CoveredStory>> MostCoveredAsync(WebScraperContext db, IMemoryCache cache, string hub,
        DateTime sinceUtc, int minSources, int take, string? category = null, CancellationToken ct = default);   // cache key includes hub
}
```

  `TechQuery.MostCoveredAsync` and `GroupMostCovered` become one-line calls with `hub: "tech"`, or are removed in
  favour of `CoveredQuery`. Move the tests to `CoveredQueryTests` and keep them passing. Pass `sinceUtc` in from the
  caller (now minus 24 hours, from the injected clock), per the planned clock rule, rather than reading `UtcNow` in the
  query.
- [ ] **Show it on News.** Under the lead, before More headlines, render up to **3** groups with 3+ sources from the
  last 24 hours. On the phone, render 1 as a section between More headlines and the folds.
- [ ] **Share the markup.** Today it lives in `TechFront.razor` (`tc-covered`). Move it into a shared component
  `Components/Feed/CoveredList.razor` (`Items`, `Compact`). Rename its classes to a new prefix **`cv-`**
  (`cv-list`, `cv-item`, `cv-item__sources`, `cv-badges`, `cv-badge`) and move the CSS with them. Add `cv-` to the
  prefix table in `coding-standards.md` ("Most covered lists (News, Tech)"). Then switch Tech to the component, so
  the two pages can't drift.
- [ ] **Empty state.** With no group of 3+ (a quiet day), render nothing, not even the heading.

## 5. Trending in headlines

**What it shows:** today's most-used **names and phrases** in News headlines, each a link to search, with a **+**
that opens Follow a topic prefilled. Six at most.

- [ ] **The query.** `myNewspaper.Shared/Data/TrendingQuery.cs`, a pure function plus a cached wrapper:

```csharp
public sealed record TrendingPhrase(string Text, int Stories);
public static class TrendingQuery
{
    public static IReadOnlyList<TrendingPhrase> Phrases(IEnumerable<FeedItem> items, int take = 6);
    public static Task<IReadOnlyList<TrendingPhrase>> TodayAsync(WebScraperContext db, IMemoryCache cache, DateTime sinceUtc, int take, CancellationToken ct = default); // headlines hub, cached 15 min
}
```

  **Algorithm** (over the day's headlines-hub titles, a few hundred):

  1. Tokenise each title on spaces and keep runs of **1–3 consecutive Capitalised words**, allowing "of"/"de"/"&"
     inside a run ("Board of Education"). Skip the title's **first word** unless it's part of a longer run. That
     drops "Can", "Why" and "Best" from sentence-case starts.
  2. Drop stop phrases: days, months, "U.S.", "CBS News"/"BBC News" and any source label, "New", "Watch", "Live",
     "Video", "Opinion", "Editorial", "Update(s)".
  3. Count **distinct stories** per phrase (case-insensitive). Keep phrases in **3 or more** stories. When a phrase is
     inside a longer kept phrase with the same count ("Education" inside "Board of Education"), keep the longer one.
  4. Order by stories, then alphabetically.

  Keep it to capitalised runs. A lower-case word such as "election" only appears when a headline capitalises it
  ("Election Day"). That's simple and predictable, and good enough for names.

  **Unit tests** (`TrendingQueryTests`):

  - "Board of Education" counts as one phrase;
  - a sentence-start "Why" isn't a phrase;
  - a phrase in 2 stories is dropped and one in 3 is kept;
  - a source label isn't a phrase.

- [ ] **Markup.** Under Your topics, in the side region:

```razor
<section class="nw-trending" aria-labelledby="nw-trending-title">
    <h2 id="nw-trending-title" class="ef-label">Trending in headlines</h2>
    <p class="nw-trending__sub">Most-used names and phrases today</p>
    <ul class="nw-trending__list">
        @foreach (var phrase in trending)
        {
            <li class="nw-chip">
                <a class="nw-chip__text" href="@($"news/all?q={Uri.EscapeDataString(phrase.Text)}")">@phrase.Text</a>
                <span class="nw-chip__count">@phrase.Stories</span>
                @if (Reader.HasProfile)
                {
                    <a class="nw-chip__follow" href="@($"news/topics/follow?terms={Uri.EscapeDataString(phrase.Text)}&hub=headlines")" aria-label="@($"Follow {phrase.Text}")">+</a>
                }
            </li>
        }
    </ul>
</section>
```

  `nw-` is a new prefix for News front additions. Add it to the table in `coding-standards.md` ("News front
  additions: trending, cascade"). The **+** is a link, so it works without JavaScript and follows the "tap to link"
  guideline. It opens the existing follow form, already filled in.
  - **Chip styling:** a 36px pill and a 28px round **+**, with the link area padded so each target is at least 44px.
- [ ] **Prefill.** In `TopicFollow.razor` add `[SupplyParameterFromQuery(Name = "terms")] private string? TermsQuery`.
  Use it as the `terms` input's value when not editing: `value="@(editing?.Terms ?? TermsQuery)"`.
- [ ] **Phone.** A row of the same chips you swipe sideways, placed after Your topics. Hide it when empty.

## 6. More rows

- [ ] **The day so far:** 14, up from 10 (`Take(14)`, and ask `GetFeedAsync` for `14 + shown.Count`).
- [ ] **Kansas City and Opinion:** load `GetLatestByCategoryAsync(…, take: 5)`, then show **5** for Kansas City,
  **4** for Opinion and **4** for each desk (trim when rendering).
- [ ] **Phone:** the folds keep their current counts.

## 7. Cascade the side region (1920px and up)

From 1920px the side column already splits into columns (`.spf-side` grid, 2 across at 1920 and 3 at 2560). Each grid
row is as tall as its tallest box, which is what leaves the gaps. Replace that with **CSS columns** on News, so the
boxes stack into whichever column has room, and **move the desks into it**.

- [ ] **Markup** (`NewsFront.razor`, desktop half):
  - Add `nw-cascade` to the News aside: `<aside class="ef-side spf-side nw-cascade">`.
  - Inside it, after Your topics, add the **Trending** section (step 5), then **the desks** (the same
    `@foreach (var desk in desks…)` markup, each `section.nf-desk`).
  - **Keep** the existing `.nf-desks` row below the columns as well. Below 1920 the side column is a single column,
    and the desks read better as a row under it. CSS shows one copy or the other (below), so the duplicate is three
    short lists and no extra queries.
- [ ] **CSS** (`sections.css`, after the existing "News and Sports fronts, 1920px and up" block):

```css
/* ---------- News front, 1920px and up: the side region cascades (news-front-revised-handoff.md) ---------- */
.pub .nw-cascade > .nf-desk {
    display: none;                         /* below 1920 the desks sit in the row under the columns */
}
@media (min-width: 1920px) {
    .pub .spf-side.nw-cascade {
        display: block;                    /* undo the 1920 grid */
        column-count: 2;
        column-gap: 40px;
        column-rule: 1px solid var(--row-border);
    }
    .pub .nw-cascade > * {
        break-inside: avoid;
        margin: 0 0 28px;
    }
    .pub .nw-cascade > .nf-desk {
        display: block;
    }
    .pub .spf-cols:has(.nw-cascade) ~ .nf-desks {
        display: none;                     /* the desks are in the cascade now */
    }
    /* The 4K guide's left rule between side-by-side boxes isn't needed with column rules. */
    .pub .nw-cascade .nf-side + .nf-side {
        padding-left: 0;
        border-left: 0;
    }
}
@media (min-width: 2560px) {
    .pub .spf-side.nw-cascade {
        column-count: 3;
    }
}
```

  If `.nf-desks` isn't a following sibling of `.spf-cols` in the markup, put `nw-has-cascade` on their shared parent
  instead and use `.nw-has-cascade .nf-desks`.
- [ ] **Reading order:** boxes run down the first column, then the second: Kansas City, Opinion, Your topics,
  Trending, then the desks. That's the DOM order, so screen readers and keyboard users get the same order.
- [ ] **iPad** (`tablet.css`, News part 5): the tablet's side column stacks. Add
  `.pub .tab-desktop .nw-cascade { column-count: 1; }` and hide the in-cascade desks there
  (`.pub .tab-desktop .nw-cascade > .nf-desk { display: none; }`), so the tablet keeps its desks row.

## 8. Verify

Record widths, themes and outcomes in the release evidence (`AGENTS.md`).

- [ ] **1920 and 2560:**
  - More headlines shows six stories under the lead, and Most covered (up to 3) sits above it.
  - The side region is 2 columns at 1920 and 3 at 2560, the columns end within about one box of each other, and the
    desks row below is gone.
  - Entertainment at 1920 still hides its own Latest headlines (step 1 didn't undo that).
  - Sports at 1920 now shows its Latest headlines again. Check that that's wanted; it was hidden by the same leak.
- [ ] **1280 and 1440:**
  - Single side column, with the desks row below (U.S. News, Top Headlines, plus World once it has sources).
  - From Tech is gone.
  - The tiles keep their width and don't stretch.
- [ ] **Tiles:**
  - Your top topic links to the topic.
  - Most covered links to the story.
  - With a test alert the weather tile appears. Stub `GetAlertsAsync` in a test, or check during a real alert. Don't
    edit live data.
  - With profiles off, Your top topic is hidden.
- [ ] **Trending:** six chips at most. The search link works. **+** opens Follow a topic prefilled with "Hub: News
  only", and saving it adds the topic. With profiles off there's no **+**. Without JavaScript, everything is a link
  and works.
- [ ] **Phone** (320, 390, 430, light and dark):
  - Tiles wrap 2 per row.
  - More headlines and Most covered (1).
  - The Trending chips swipe.
  - The folds include U.S. News and Top Headlines (and World), and no From Tech.
  - Touch targets are at least 44px.
- [ ] **iPad** (744, 820): unchanged layout, plus the new boxes, and the desks row is still present.
- [ ] **Tests:** `dotnet test` (CoveredQueryTests, TrendingQueryTests, plus the existing suite), `npm run lint`,
  `dotnet format --verify-no-changes`.
- [ ] **Browser checks clean up** temporary profiles and devices (`AGENTS.md`). If one follows a topic through **+**,
  delete it in teardown.
