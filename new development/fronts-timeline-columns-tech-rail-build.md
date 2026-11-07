# Build plan: day-so-far timeline, wider left column, Tech bottom rail

Requested November 7, 2026 (America/Chicago), from the production screenshots of the four section fronts (News, Tech, Entertainment, Sports, desktop, dark). Written against `develop` at `5d6034a`, which includes the vertical timelines (`76f6cc7`) and the phone briefing and YouTube picker patches (`0d6c382`, `f5b186b`).

Three changes, one commit each so they can ship separately:

1. **The day so far as a timeline** on News and Tech, in the format of Entertainment's On screen and Sports' Upcoming games.
2. **A wider left column** on all four fronts; Sports and Entertainment take the width from their right side.
3. **A bottom rail on Tech**: its three desks move under the columns, three across, and Latest headlines takes their place beside the lead.

> **Decisions made (November 7).**
> - Tech bottom rail: option A, matching Entertainment. Desks three across. Latest headlines shows 5 stories, as on Entertainment.
>
> **Defaults used for questions not yet answered. Change them here before building.**
> - Day-so-far timeline: **no thumbnails**; keep today's item count; a time shows once per time slot, repeats blank, as in Sports' Upcoming games.
> - Left column: **280px at 1024–1279, 320px at 1280–1919, 360px at 1920–2559, 400px from 2560**.
> - Entertainment's right column: **Next weekend narrows from 300px to 280px**; Latest headlines (`1fr`) absorbs the rest. Sports' right column is Latest headlines (`1fr`), so it shrinks on its own.
>
> **Scope.**
> - Phones don't change: each front's phone half and `PhoneTimeline` stay as they are.
> - Tablets (744–1023px) render the desktop markup (`tab-desktop`), so change 1 reaches them. Changes 2 and 3 are desktop-only (from 1024px); tablet column templates don't change.
> - The screenshots were taken at about 2560 CSS px (the columns measure 0.78 of their CSS widths), so the wide-screen rules matter most.

---

## 1. The day so far as a timeline (News and Tech)

Today both fronts render `<ul class="spf-agate nf-agate">` rows (time, then headline and meta). Switch them to the **existing `spf-timeline` markup** from Sports' Upcoming games (`SportsFront.razor`, around line 30). Its CSS already draws On screen's rail, dot, right-aligned time label and colored meta line. Don't add a third timeline style: `ef-onscreen` and `spf-timeline` already look alike.

```razor
<ol class="spf-timeline nf-day__line">
    @{ string? previousTime = null; }
    @foreach (var item in day)
    {
        var time = item.Undated ? "" : Dates.Time(item.Date);
        var sameAsPrevious = time == previousTime;
        previousTime = time;
        <li class="spf-timeline__item @PhoneColors.ForCategory(item.CategoryName)">
            <span class="spf-timeline__time">@if (!sameAsPrevious) { @time } else { <span class="sr-only">@time</span> }</span>
            <span class="spf-timeline__body">
                <a href="@(FeedQuery.SafeHref(item.Url) ?? "news/all")" target="_blank" rel="noopener noreferrer">@Title(item)</a>
                <span class="spf-timeline__meta"><span class="spf-timeline__sport">@item.CategoryName</span><span aria-hidden="true"> · </span>@item.SourceName</span>
            </span>
        </li>
    }
</ol>
```

- [ ] `NewsFront.razor` (desktop half, `nf-day`, around line 31): replace the `spf-agate nf-agate` list with the timeline. Keep the `ef-head`, the "Newest first, from every source" note, the empty state and the list's contents (what the lead and More headlines already show stays left out).
- [ ] `TechFront.razor` (desktop half, `tc-day-title` section): the same, with `tech/news` as the fallback link.
- [ ] The meta line reads **category · source**, in the category color (`spf-timeline__sport` takes `var(--dot)` from the `mx-c-*` class). Use the same category and source text today's `Meta(item)` shows; if `Meta` already joins them, reuse it rather than rebuilding the string.
- [ ] Undated items: an empty time label, as today.
- [ ] `spf-timeline` CSS sits inside a `@media (min-width: 1024px)` block (around line 3024). Check that tablets (744–1023) get it too, since they render this markup; if not, move the base rules out of the media query rather than copying them into `tablet.css`.
- [ ] Remove `nf-agate` rules that nothing uses afterwards; keep `spf-agate` if Sports' standings or other pages still use it (`grep` first).
- [ ] `ef-head` in the narrow column: "All headlines →" must stay on one line beside the heading (`white-space: nowrap` on the link). With the wider column (change 2) it fits at every width.
- [ ] Phones: no change (`PhoneTimeline` already shows The day so far).

## 2. A wider left column (all four fronts)

One set of widths for the first column on every front, from 1024px:

| Width | Left column |
|---|---|
| 1024–1279px | 280px |
| 1280–1919px | 320px |
| 1920–2559px | 360px |
| 2560px and up | 400px |

Templates today (`sections.css`), and what they become:

| Front | Rule | Today | New |
|---|---|---|---|
| Sports | `.spf-front > .spf-cols` from 1024 (around 2947) | `250px minmax(0, 640px) minmax(0, 1fr)` | first track per the table; add 1280, 1920 and 2560 overrides beside it |
| Entertainment | `.ef-cols` 1024–1279 (around 381) | `250px minmax(0, 1fr) 280px` | `280px minmax(0, 1fr) 260px` |
| Entertainment | `.ef-cols` from 1280 (around 570) | `270px minmax(0, 640px) minmax(0, 1fr) 300px` | `320px minmax(0, 640px) minmax(0, 1fr) 280px`; 360px from 1920 and 400px from 2560, still with 280px last |
| News | `.nf .spf-cols` | `300px …` from 1920, `320px …` from 2560 (around 5732, 5747); below 1920 the shared `.spf-cols` template | first track per the table; give News its own 1024 and 1280 rules if the shared template doesn't produce 280/320 |
| Tech | `.tc-front` grid from 1920 (around 5800) | `300px minmax(0, 1.5fr) minmax(0, 1fr) minmax(0, 1fr)` | see change 3; below 1920 Tech uses the shared `.spf-cols`, so set its first track per the table there too |

- [ ] Measure each front's current left-column width at 1024, 1280, 1440, 1920 and 2560 before editing, and record them in the handoff. The shared `.spf-cols` template (`minmax(260px, 1fr) minmax(0, 2fr) minmax(280px, 1fr)`) may already be wider than 280–320px on News and Tech below 1920. **Don't narrow a column that's already wider than the target**; use `max()` or leave that step out.
- [ ] Scope every rule to its front (`.spf-front`, `.ef-cols`, `.nf`, `.tc-front`), as the existing ones are. The shared `.spf-cols` rule must not change for pages other than these four.
- [ ] The Sports and Entertainment lead stays capped at 640 × 320; its column is `minmax(0, 640px)`, so at 1024–1279 it shrinks rather than overflowing.
- [ ] Entertainment's Next weekend table must still fit at 260px (1024–1279) and 280px: film titles wrap, the predicted range doesn't (`white-space: nowrap`), and nothing scrolls sideways.
- [ ] Run `node tools/check-css.cjs`: no new breakpoints (1024, 1280, 1920 and 2560 are registered) and no new prefixes.

## 3. A bottom rail on Tech

Below 1920px Tech already ends with its desks (`.nf-desks`, `repeat(auto-fit, minmax(240px, 1fr))`) under the columns. From 1920px the wide-screen block (around line 5800) pulls them up into column 3 beside the lead and hides Latest headlines, so the page has nothing under the columns but the "This week in Tech" banner. From 1920px it becomes:

```
┌ The day so far ┬ Lead ───────────┬ Latest headlines (5) ┬ Just released ─┐
│ (timeline)     │ photo, title,   │ kicker / headline     │ Tech videos    │
│                │ Most covered    │ All tech headlines →  │                │
└────────────────┴─────────────────┴───────────────────────┴────────────────┘
═══ (double rule) ═════════════════════════════════════════════════════════
┌ Technology ──── All → ┬ Video Games ──── All → ┬ Linux ──────── All → ──┐
│ stories, one column    │ stories, one column     │ stories, one column    │
└────────────────────────┴─────────────────────────┴────────────────────────┘
This week in Tech: … →
```

### Markup (`TechFront.razor`, desktop half)

- [ ] **Latest headlines becomes its own column**, as on Entertainment: move the `Latest headlines` heading and `tc-latest-grid` list out of `.spf-main` into `<section class="ef-headlines-col tc-headlines-col" aria-labelledby="tc-latest-title">` placed after `.spf-main`, inside `.spf-cols`.
  - 5 stories (`stories.Skip(1).Take(5)`, up from 4), with category kickers as today, then "All tech headlines →". Move that link out of `.spf-main` with them.
  - One list in the DOM at every width; don't render a second copy for wide screens.
- [ ] The desks keep their markup, headings, colors and "All {desk} →" links.

### CSS (`sections.css`)

- [ ] **Below 1920px:** place `.tc-headlines-col` where the list is today, under the lead in column 2 (`grid-column: 2`), as Entertainment does at 1024–1279. Tech below 1920 should look the same as now apart from the fifth story.
- [ ] **From 1920px** (the `.tc-front` block):
  - `.tc-front` grid: `360px minmax(0, 1.5fr) minmax(0, 1fr) minmax(0, 1fr)` (400px first track from 2560).
  - Column 3: `.tc-front > .spf-cols > .tc-headlines-col` at `grid-column: 3; grid-row: 2`, with the same padding and left rule the desks used (`padding: 20px 24px 0; border-left: 1px solid var(--row-border)`).
  - Remove the rule that hides `.tc-latest-grid` and its heading from 1920.
  - `.tc-front > .nf-desks`: `grid-column: 1 / -1; grid-row: 3; display: grid; grid-template-columns: repeat(3, minmax(0, 1fr)); column-gap: 48px; margin-top: 32px; padding: 16px 0 0; border-top: 3px double var(--text); border-left: 0`. Remove its column-3 placement and flex column.
  - Each desk's stories stay a single column.
  - Fewer than three desks with stories: the remaining ones share the row (`repeat(auto-fit, minmax(0, 1fr))` or an `:only-child` rule like Entertainment's).
  - `.tc-front > .tc-week-banner`: `grid-row: 4`.
- [ ] Tech's right column (Just released and Tech videos) keeps its width.
- [ ] Update the section comment ("day so far · lead · desks · Just released + videos") and `docs/tech/tech-05-front-wide.md` to the new order.
- [ ] Record in `docs/interface.md` or the Tech doc that this reverses the October "category lists beside the lead" layout for Tech, as Entertainment did in `d360a37`.

---

## 4. Checks

- [ ] `tools/verify.sh`: build with 0 warnings, all .NET and JS tests, lint, format and `check-css`.
- [ ] Browser checks to update, run against `WebScraper_Test`:
  - `news-front.browser.cjs` and `tech-front.browser.cjs`: The day so far is an `ol.spf-timeline` with one item per story, every item a link, no `spf-agate` rows; a repeated time is visually blank but present for screen readers.
  - `tech-front.browser.cjs` (desks around line 210): at 1920 and 2560 the three desks sit in one row below the columns, under a double rule; Latest headlines has 5 items in column 3, beside the lead; below 1920 the desks and headlines are where they are today.
  - `section-fronts.browser.cjs`: left-column width per the table at 1024, 1280, 1440, 1920 and 2560 on all four fronts; Entertainment's right column 260/280px; Sports lead still ≤ 640 × 320.
  - `wide-screens.browser.cjs`, `ipad-news-home.browser.cjs`, `ipad-entertainment.browser.cjs`, `ipad-sports.browser.cjs`: pass, updating only the assertions these changes are meant to move.
- [ ] No sideways scroll and no clipped "All headlines →" at 1024, 1280, 1440, 1920 and 2560, light and dark.
- [ ] Tablets 744 and 820: the timeline renders with its rail and dots on News and Tech.
- [ ] **Mobile verification per AGENTS.md:** 320, 390 and 430px in light and dark on `/news`, `/tech`, `/sports` and `/entertainment`, screenshots inspected. Phones should be unchanged; record that they were checked, not assumed.
- [ ] Browser test clean-up per AGENTS.md; check for no `zz ` profiles and no new unassigned devices.
- [ ] **After deploy:** `node tools/live-check.cjs`, plus the four fronts at 390 and 2560 against live data.
- [ ] Add a release-history entry for each commit.
