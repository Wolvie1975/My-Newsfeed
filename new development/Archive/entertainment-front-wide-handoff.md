# Entertainment front on wide desktops: categories beside the lead, the side boxes in one column

Written November 1, 2026 (America/Chicago), against `develop` at `75d6e5f`. Every file, class and line number below
was checked against that commit.

**Save as:** `docs/entertainment-front-wide-handoff.md`

**Mockup:** the myNewspaper canvas, **4K screens** page, "Entertainment front at 2000 CSS px (Nov 1)"
(https://claude.ai/artifact/JVQ8mU7R63Z1cziNpeW9Z5).

The same idea as the Tech front (`docs/tech/tech-05-front-wide.md`), with one difference: the Tech front already had
its desk lists further down the page, so CSS could move them. The Entertainment front has **no per-category lists**,
so this adds one small block of markup and one query. Below 1920 px the new block is hidden, so phones, iPad portrait
and 1024–1919 are unchanged.

## What changes at 1920 px and up

| Column | Today at 2000 px | After |
|---|---|---|
| — | On screen strip, full width | unchanged |
| 1 | Lead (very large photo) · Latest headlines | Lead with a 16:9 photo · Latest headlines **hidden** (the categories show the same newest stories) |
| 2 | Weekend box office · Friday in theaters | **General · TV/Film · Box Office**, four headlines each, stacked |
| 3 | Tonight on TV (mostly empty) | **Weekend box office · Tonight on TV · Friday in theaters**, stacked |

The categories are whatever the Entertainment Headlines page shows as pills: the `entertainment`-hub categories
from the database (today General, TV/Film, Box Office). That way a category added later appears here too.

---

## 1. Data (`Components/EntertainmentFront.razor`, `@code`)

Add a field and load it after the headlines (line ~363):

```csharp
private IReadOnlyList<(FeedCategory Category, IReadOnlyList<FeedItem> Items)> desks = [];
```

```csharp
try
{
    var hubCategories = (await FeedQuery.GetCategoriesAsync(db))
        .Where(c => c.Hub == "entertainment")
        .OrderBy(NewsSections.Order).ThenBy(c => c.Name)
        .ToList();
    var loaded = new List<(FeedCategory, IReadOnlyList<FeedItem>)>();
    foreach (var category in hubCategories)
    {
        var items = await FeedQuery.GetLatestInHubAsync(db, "entertainment", 4, category.Name);
        if (items.Count > 0) loaded.Add((category, items));
    }
    desks = loaded;
}
catch (Exception ex) when (DataFailures.IsReadFailure(ex))
{ desks = []; Log.LogError(ex, "Could not read entertainment category headlines."); }
```

This is the same query and ordering `News.razor` uses for its category pills (line ~267) and the Tech front uses per
desk (`TechQuery.LatestAsync` → `GetLatestInHubAsync`). Three small queries per page load, the same as the Tech front.

## 2. Markup (`Components/EntertainmentFront.razor`, desktop, between `.ef-main` and `.ef-side`, line ~105)

```razor
        </div>   @* end .ef-main *@

        @if (desks.Count > 0)
        {
            @* Wide screens only (1920+): the categories beside the lead. Hidden below that. *@
            <div class="nf-desks ef-desks">
                @foreach (var (category, items) in desks)
                {
                    var label = NewsSections.LabelFor(category.Name);
                    <section class="nf-desk @PhoneColors.ForCategory(category.Name)" aria-labelledby="@($"ef-desk-{category.Id}")">
                        <h2 id="@($"ef-desk-{category.Id}")" class="ef-label">@label</h2>
                        <ul class="ef-headlines spf-headlines">
                            @foreach (var item in items)
                            {
                                var href = FeedQuery.SafeHref(item.Url);
                                var title = Title(item);
                                var source = Source(item);
                                var summary = FeedText.StripEmoji(FeedQuery.Excerpt(item.Description));
                                <li data-news-url="@href" data-news-source="@source">
                                    @if (href is not null) { <a class="ef-headlines__title" href="@href" target="_blank" rel="noopener noreferrer">@title</a> }
                                    else { <span class="ef-headlines__title">@title</span> }
                                    <div class="ef-meta">
                                        <span class="ef-meta__when">@source@(item.Undated ? "" : " · " + Dates.Published(item.Date))</span>
                                        <ShareButton Url="@item.Url" Title="@title" />
                                        <SaveButton Item="item" Title="@title" Source="@source" Summary="@summary" />
                                    </div>
                                </li>
                            }
                        </ul>
                        <a class="ef-more" href="@(NewsSections.RouteFor(category) ?? "entertainment/news")">All @label <span aria-hidden="true">→</span></a>
                    </section>
                }
            </div>
        }

        <aside class="ef-side">
```

- The rows are the same markup as Latest headlines, without the kicker (the section heading already names the
  category).
- `nf-desk` / `nf-desks` reuse the Tech and News desk styles (`sections.css` line ~2390), so the headings, rules and
  colours match the Tech front.
- `NewsSections.LabelFor` turns "Entertainment" into "General", as the Headlines pills do.
- `PhoneColors.ForCategory` gives each category its colour. Check that "Box Office" has one; if it falls through to
  the default, add a case in `Components/Phone/PhoneColors.cs` so the three headings differ.

## 3. CSS (`wwwroot/sections.css`)

### 3.1 Hidden below 1920 (next to the other `.ef-` base rules, line ~210)

```css
/* The category lists only appear beside the lead on wide screens (docs/entertainment-front-wide-handoff.md). */
.pub .ef-cols > .ef-desks {
    display: none;
}
```

### 3.2 The wide layout (replace the "Entertainment front, 1920px and up" block, line ~4876, and its 2560 sibling)

```css
/* ---------- Entertainment front, 1920px and up: lead · categories · box office, TV, theaters (docs/entertainment-front-wide-handoff.md) ---------- */
@media (min-width: 1920px) {
    .pub .ef-cols {
        grid-template-columns: minmax(0, 1.5fr) minmax(0, 1fr) minmax(0, 1fr);
        align-items: start;
    }
    .pub .ef-screen__list {
        grid-template-columns: repeat(auto-fill, minmax(380px, 1fr)); /* kept from the old block: 4 at 1920, 6 at 2560 */
    }

    /* Column 1: the lead at a sensible size; Latest headlines hidden (the categories show the same stories). */
    .pub .ef-main .ef-lead__image {
        height: auto;
        aspect-ratio: 16 / 9;
    }
    .pub .ef-main .ef-lead__title {
        font-size: 34px;
        line-height: 1.1;
    }
    .pub .ef-main :is(.ef-headlines, .ef-label:has(+ .ef-headlines)) {
        display: none;
    }

    /* Column 2: the categories, stacked. */
    .pub .ef-cols > .ef-desks {
        display: flex;
        flex-direction: column;
        gap: 24px;
        margin: 0;
        padding: 0 24px;
        border-top: 0;
        border-left: 1px solid var(--row-border);
    }

    /* Column 3: box office, tonight on TV, Friday in theaters, in one column (undo the old two-column split). */
    .pub .ef-cols > .ef-side {
        display: flex;
        flex-direction: column;
        gap: 26px;
        padding-left: 24px;
        border-left: 1px solid var(--row-border);
    }
    .pub .ef-cols > .ef-side > .ef-tv:not(:first-child) {
        padding-left: 0;
        border-left: 0;
    }
}
```

Also delete the `.ef-cols > .ef-side > .ef-tv:not(:first-child)` selector from the shared rule further down
(line ~4930, the one that also styles `.spf-rankings`). The block above overrides it, but leaving both invites
confusion. Keep the `.spf-side > .spf-rankings` half of that rule.

Two things to check against the live CSS:

| Check | Why |
|---|---|
| The lead image class (`.ef-lead__image`) and whether it has a fixed `height` at 1024+ (line ~325 block) | The 16:9 override needs to beat it. The live lead is about 280 px tall at 860 px wide, so something sets its height |
| The `.ef-main > .ef-label` rule (line ~186) | If "Latest headlines" isn't the `.ef-label` directly before `.ef-headlines`, adjust the hide selector |

> **2560 and up:** the old 2560 `.ef-cols` rule is replaced by the block above; the three columns just get wider.
> If the lead feels small on 4K, give it `minmax(0, 1.8fr)` in a 2560 block.

## 4. Verify

| Width | Expected |
|---|---|
| 2000 | On screen strip; then lead (16:9 photo, no Latest headlines) · General / TV/Film / Box Office stacked, four headlines each · Weekend box office, Tonight on TV, Friday in theaters stacked |
| 2560 | Same, wider |
| 1440, 1024 | **Exactly as today** (no category lists; Latest headlines shown; the box office and TV side as before) |
| iPad portrait, phone | Unchanged |
| A category with no stories in the window | Its section doesn't render; the others stay |
| "All Box Office →" | Goes to that category's Headlines page (`NewsSections.RouteFor`) |

## Files touched

| File | Change |
|---|---|
| `Components/EntertainmentFront.razor` | §1 query, §2 markup |
| `wwwroot/sections.css` | §3.1 base rule, §3.2 replaces the 1920/2560 Entertainment blocks, one selector removed from the shared rule |
| `Components/Phone/PhoneColors.cs` | Only if Box Office has no colour of its own |
