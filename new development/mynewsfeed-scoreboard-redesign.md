# Scoreboard redesign: card grid

**Project:** MyNewsFeed (myNewspaper) · **Page:** `/scoreboard` · **Component:** `src/myNewspaper.Web/Components/ScoreboardPanel.razor`
**Mockup:** `mynewsfeed-scoreboard-mockup.html` (Cardinals scheme; switch width, theme and density from the bar at the top)
**Date:** November 2, 2026

> **Goal:** stop wasting horizontal space on wide screens. Today each game is a full-width row (~1,900 px at 1920), with names at the far left and scores at the far right, about 140 px tall per game. At 1920, 15 games fill five screens. The redesign puts the same games in a card grid that fits on roughly one screen.

---

## 1. Scope

| In scope | Out of scope |
|---|---|
| The full scoreboard page (`Compact == false`, desktop and tablet ≥ 1024 px) | The phone front (`PhoneFront` / `.mx-phone`), which already has its own layout |
| Toolbar, grouping, game card, right rail | The Home page's compact scoreboard (`Compact == true`), which can adopt the card later |
| | Data collection, refresh timing, coverage rules |

The card grid also collapses to one column below 760 px, so it works if the phone front ever reuses it.

---

## 2. Before → after

| Area | Today | Redesign |
|---|---|---|
| Notes | Two `scoreboard-note` paragraphs plus MLS/EFL links above the games (~100 px) | Behind one **ⓘ** next to the date |
| Date + filters | Two separate blocks (`scoreboard-dates`, `scoreboard-filter`) | **One sticky toolbar row** |
| Grouping | By competition (`sb-group`) | **By status:** Your teams → Live → Upcoming → Final (competition grouping kept as an option, see §8) |
| Game layout | Full-width row, score at the far right | **Card** about 320–380 px wide in an auto-fill grid |
| Links | Separate row: "Game info · Game details ↗" | Whole card links to game details; ⓘ and ☆ are icons in the corner |
| Records | Always shown, even "0-0 (0-0)" | Hidden until the team has played |
| Wide screens | Empty right side | Right rail at ≥ 1600 px: Top 25, conference standings, Your teams |
| Games per 1920 screen | ~6 | ~15–20 |

---

## 3. Page structure

```
┌ Masthead (unchanged) ─────────────────────────────────────────────────────────┐
├ Toolbar (sticky): ← Monday, Nov 2 → ⓘ ·········· Show [▾] Games [▾] [Cards|List] ┤
├──────────────────────────────────────────────────────────────┬────────────────┤
│ Your teams (starred, pinned)                                 │ Top 25         │
│ [card][card]                                                 │                │
│ ● Live · 5 games                                             │ Conference     │
│ [card][card][card][card]                                     │ standings      │
│ [card]                                                       │                │
│ Upcoming · 3 games                                           │ Your teams     │
│ [card][card][card]                                           │                │
│ Final · 5 games                                              │ (≥1600px only) │
│ [card][card][card][card]                                     │                │
└──────────────────────────────────────────────────────────────┴────────────────┘
```

---

## 4. Toolbar

One row, sticky under the masthead (`position: sticky; top: <masthead height>`).

| Slot | Content | Notes |
|---|---|---|
| Left | `←` · **Monday, Nov 2** · `→` · **ⓘ** | Keep today's rules: previous only after yesterday, next up to 7 days ahead, "Today" link when not on today |
| ⓘ popover | Both current notes + "More results: MLS season · EFL Championship season" | Keyboard reachable (`tabindex="0"`, shows on `:focus`) and `role="tooltip"`. Also move the bottom "Sampled clocks stay fixed…" note here |
| Right | **Show** select · **Games** select · **Cards / List** switch | Reuse the existing `scoreboard-filter` form, so the `noscript` Apply button still works |
| < 760 px | Date row, then Show and Games full width | Hide the labels and the Cards/List switch |

---

## 5. Groups

Order: **Your teams → Live → Upcoming → Final.** Skip any group with no games.

| Group | Contents | Sort | Heading |
|---|---|---|---|
| Your teams | Games involving a starred team | Live first, then by start time | "Your teams · N games" + Edit link |
| Live | In progress | Closest to the end first (2nd half before 1st, less time left first) | Red pulsing dot + "Live · N games" |
| Upcoming | Scheduled | Start time | "Upcoming · N games" |
| Final | Finished | Most recently finished first | "Final · N games" |

> **Rule:** a game pinned under **Your teams** is **not repeated** in its status group below.

Heading style: serif title, a 2 px rule underneath in `--text`, and the count in `--text-3`.

---

## 6. Game card

### Anatomy

```
┌──────────────────────────────────────┐
│ ● 2nd · 1:58                   ⓘ  ☆ │  ← status line + tools
│ (LAS) La Salle Explorers         53 │  ← away team (muted when trailing or lost)
│ (HOU) #10 Houston Cougars      ▸ 94 │  ← home team (bold when leading or won)
│ Top: Lee 22 pts      Recap ↗        │  ← footer, Final only, optional
└──────────────────────────────────────┘
```

### Rules

| Element | Spec |
|---|---|
| Card | `border: 1px solid var(--border)`, radius 12 px, `background: var(--surface)`, padding 12 px 14 px; the whole card is an `<a>` to game details; hover sets the border to `var(--accent)` |
| Live card | Border is the live color mixed 55% into `--border` |
| Your-team card | 3 px inside left bar (`--selection-bg`; a lighter blue in dark mode) |
| Status line | 12 px, weight 700. Live: pill (`--live-tint` background, `--live` text, dot) "2nd · 1:58". Upcoming: "8:00 PM" + network right-aligned. Final: "Final" / "Final · OT" |
| Tools | Top-right, 24 px icon buttons: ⓘ = Game info, ☆/★ = star (★ in gold `#f5b301`). Keep the full 44 px tap area on touch devices |
| Team row | Grid `22px 1fr auto`: logo, name (ellipsis), score |
| Rank | `#10` prefix in `--accent`, 12 px, weight 800 |
| Record | 12 px `--text-3` after the name; **hidden when the team hasn't played** (wins + losses == 0) |
| Score | 22 px, weight 800, `tabular-nums`, right-aligned |
| Leading / winning team | Name and score at full weight, with a **▸ marker to the left of the score** |
| Trailing / losing team | Name and score in `--text-3`, weight 500 |
| Upcoming | No scores; both teams at full weight |
| Footer (Final only) | Top scorer + "Recap ↗", 12 px, hairline above. Optional; leave it out if the data isn't there |

> **Winner marker.** The ▸ is absolutely positioned to the left of the score, so it never shifts the digits. All scores line up on the card's right edge whether or not they're marked.

```css
.sb-card .score { position: relative; font: 800 22px/1 var(--ui); font-variant-numeric: tabular-nums; text-align: right; }
.sb-card .team.win .score::before {
  content: "▸";
  position: absolute; right: 100%; top: 50%; transform: translateY(-50%);
  margin-right: 5px; font-size: 11px; color: var(--text-2);
}
.sb-card .team.lose :is(.name, .score) { color: var(--text-3); font-weight: 500; }
```

### Accessibility

- Mark the leader in text too, for screen readers: `<span class="sr-only">, leading</span>` (live) or `, winner` (final). The ▸ is decorative (`aria-hidden`).
- The live clock pill needs text, not just color: "2nd · 1:58" already has it.
- Card links get an accessible name like "La Salle 53, Houston 94, 2nd half 1:58".

---

## 7. Grid and breakpoints

```css
.sb-grid { display: grid; grid-template-columns: repeat(auto-fill, minmax(320px, 1fr)); gap: 12px; }
.sb-body { display: grid; grid-template-columns: minmax(0, 1fr); gap: 32px; }
@media (min-width: 1600px) { .sb-body { grid-template-columns: minmax(0, 1fr) 340px; } }  /* right rail */
@media (max-width: 759px)  { .sb-grid { grid-template-columns: 1fr; gap: 10px; } }
```

| Width | Columns | Rail |
|---|---|---|
| 1920 | 4 (with rail) | Yes |
| 1600 | 3 (with rail) | Yes |
| 1280 | 3 | No |
| 1024 | 2–3 | No |
| < 760 | 1 | No |

---

## 8. Compact list mode

A **Cards / List** switch in the toolbar. List = one game per row, max 760 px wide: status (150 px) · teams · scores, no footer. Remember the choice per browser in `localStorage` (try/catch) or per profile. Cards is the default.

If you still want grouping **by competition**, offer it in List mode only, or as a third Games option ("Group by league"). Cards read best grouped by status.

---

## 9. Right rail (≥ 1600 px)

| Panel | Contents | Source |
|---|---|---|
| Top 25 | Ranks 1–10 (or 25, scrollable), with "Live" / today's result per team | Existing Top 25 data |
| Conference standings | For the selected Show league; before any conference games: "Shown once teams have played a conference game" | Existing Standings data |
| Your teams | Starred teams with today's status ("8:00 PM", "W 78-61") | Starred teams |

This is the same approach as the Tech and Entertainment fronts on wide desktops: use the empty right side instead of stretching the main column.

---

## 10. Suggested markup

```razor
<div class="sb-body">
  <main>
    @foreach (var group in StatusGroups(visibleGames))   // YourTeams, Live, Upcoming, Final; skip empty
    {
      <section class="sb-status" aria-labelledby="sb-@group.Key">
        <header class="sb-status__head">
          @if (group.Key == "live") { <span class="sb-dot" aria-hidden="true"></span> }
          <h2 id="sb-@group.Key">@group.Title</h2>
          <span class="sb-status__count">@group.Games.Count @(group.Games.Count == 1 ? "game" : "games")</span>
        </header>
        <div class="sb-grid">
          @foreach (var game in group.Games) { @GameCard(game) }
        </div>
      </section>
    }
  </main>
  <aside class="sb-rail" aria-label="More sports"> … </aside>
</div>
```

`GameCard` keeps its current data and gets the new card markup. Leave the existing `sb-group` competition grouping in place behind the List option.

---

## 11. Acceptance checklist

- [ ] At 1920 × 1080, at least 12 cards are visible without scrolling (no Your teams group)
- [ ] Scores sit within one card width of their team names at every breakpoint
- [ ] ▸ never shifts score alignment; all scores share the card's right edge
- [ ] Starred games appear once, under Your teams
- [ ] "0-0 (0-0)" records don't render
- [ ] The toolbar stays visible while scrolling; date stepping and filters still work without JavaScript
- [ ] ⓘ popover opens with the keyboard and contains all former notes and links
- [ ] Works in light and dark for every color scheme (uses `--surface`, `--border`, `--text-*`, `--accent` only; no hard-coded scheme colors except the gold star)
- [ ] Rail appears only at ≥ 1600 px
- [ ] Phone front (`.mx-phone`) unchanged

---

## 12. Mockup data

Real (from the Nov 2 screenshot): the five live games, their scores, clocks and rankings.
Sample only: upcoming and final games, networks, top scorers, the starred Kansas teams, and the rail contents (most Top 25 rows are placeholders).
