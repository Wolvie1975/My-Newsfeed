# Plan: "Big 12 · All sports" choice on Scoreboard and Competitions

Requested November 7, 2026 (America/Chicago).

## Decisions (from the user)
- Label: **Big 12 · All sports**.
- It sits **alongside** the existing per-sport Big 12 choices; it doesn't replace them.
- Women's college basketball stays excluded on both pages, as it is today.

## Current state (checked against code and the live database, November 6–7, 2026)

**Competitions (`/events`)**
- The dropdown comes from `EventsQuery.GetScheduleChoicesAsync`
  ([EventsQuery.cs:67](../src/myNewspaper.Shared/Data/EventsQuery.cs#L67)), one choice per provider and sport pair.
  Big 12 appears as `Big 12 Football`, `Big 12 Men's Basketball`, `Big 12 Volleyball` and so on. The choice value is
  `big12|<Sport>`.
- `EventsQuery.GetUpcomingAsync` already filters by `provider` on its own. With `provider: "big12"` and no sport it
  returns every Big 12 sport. `Visible()` already drops `Women's Basketball`.
- [Events.razor](../src/myNewspaper.Web/Components/Pages/Events.razor) finds the selected choice by matching
  `Value` against the list, then passes `selectedChoice.Sport` and `selectedChoice.Provider` to the query.

**Scoreboard (`/scoreboard`, desktop and phone)**
- The Show menu is `ShowOptions` in
  [ScoreboardPanel.razor:408](../src/myNewspaper.Web/Components/ScoreboardPanel.razor#L408), shared by the desktop
  filter and the phone Sport select. Its "Conferences" group lists one choice per conference follow, labelled
  `Big 12 · Football`, `Big 12 · Men's Basketball` and so on (`EspnLeagues.FollowLabel`), with value `f:<follow id>`.
- Live conference follows (`dbo.ResultFollows`, `Kind = 'conference'`, each with 16 members):

  | Id | Sport | League |
  | --- | --- | --- |
  | 64 | football | college-football |
  | 65 | basketball | mens-college-basketball |
  | 1294 | soccer | usa.ncaa.w.1 |
  | 66 | volleyball | womens-college-volleyball |
  | 2029 | basketball | womens-college-basketball (already filtered out of the scoreboard read) |

- `ScoreboardSelection` ([Scoreboard.cs:122](../src/myNewspaper.Shared/Data/Scoreboard.cs#L122)) holds one
  `Competition` or one `FollowId`. `ScoreboardQuery.ReadAsync` then filters by one league and applies one follow's
  `Matches`, so a single selection can't cover several sports yet.

## Changes

### 1. Competitions
- [x] In `GetScheduleChoicesAsync`, when at least one `big12` choice exists, add
      `new ScheduleChoice("big12|", "Big 12 · All sports", "big12", null)`.
- [x] Order it **first among the Big 12 choices**, explicitly rather than by relying on the alphabetical sort.
- [ ] Confirm `Events.razor` passes `Sport = null` and `Provider = "big12"` for it, and that paging (`PageHref`) keeps
      `schedule=big12|`.
- [ ] Confirm `GameGroups.Schedule` labels each sport's heading correctly when the sports are mixed within one day.

### 2. Scoreboard
- [x] Add a conference-group selection value: `g:Big 12`, the follow `Name` with every enabled conference follow of
      that name included.
- [x] `ScoreboardSelection`: add an optional `Group` (the conference name). `Resolve` accepts `g:<name>` only when at
      least one enabled conference follow has that name; otherwise it falls back to `""` like other unknown values.
- [x] `ScoreboardQuery.ReadAsync`: when `Group` is set, skip the single-league SQL filter and keep games where **any**
      follow in the group `Matches`, each follow keeping its own sport and league check. The women's basketball
      exclusion stays in place.
- [x] `ShowOptions`: in the "Conferences" group, add `Big 12 · All sports` (`g:Big 12`) before the per-sport Big 12
      choices, only when there are two or more Big 12 conference follows. Generalize this to any conference name with
      two or more follows, so it isn't hard-coded.
- [x] The coverage warning ([ScoreboardPanel.razor:79](../src/myNewspaper.Web/Components/ScoreboardPanel.razor#L79))
      should consider only the group's leagues, not every league.
- [ ] Check the phone ribbon, the "Only …" fold links, day links (`Href`) and the Final filter keep `show=g:Big 12`.
- [x] Leave the homepage card's `scoreboardShow` filter alone (it lists leagues only).

### 3. Tests
- [x] `EventsTests`: the choice exists only when Big 12 games exist, comes first among Big 12, returns every Big 12
      sport except women's basketball, and leaves the per-sport choices in place.
- [x] `EventsRenderTests`: it's selected after submit and survives paging.
- [x] `ScoreboardTests`: `g:Big 12` resolves; an unknown group falls back to all; matching uses each follow's league
      (a non-member game in the same league is excluded, and a member's game in another league is included only for
      a Big 12 follow league).
- [x] `ScoreboardReadTests`: a mixed-league read returns football, men's basketball, volleyball and soccer games for
      members only.
- [ ] Browser: extend `tests/browser/scoreboard-show.browser.cjs` and add an `/events` check for the new choice
      (desktop and phone).

### 4. Docs
- [x] `docs/sports.md`: describe the choice under "Upcoming schedule" and "Live scoreboard".
- [ ] Release history entry after deploying.

## Progress (updated November 7, 2026)

Work is in progress; nothing is committed or deployed yet. Uncommitted changes on `develop`:

| File | Change |
| --- | --- |
| `src/myNewspaper.Shared/Data/EventsQuery.cs` | Adds `Big 12 · All sports` (`big12|`, provider `big12`, sport `null`) when Big 12 games exist; sorts every Big 12 choice under one key, with the all-sports choice first |
| `src/myNewspaper.Shared/Data/Scoreboard.cs` | `ScoreboardFollow` carries the follow `Name`; `ScoreboardSelection` gains `Group`; `Resolve` accepts `g:<name>` (case-insensitive, normalized to the stored name) from `show`, and also from the legacy `competition` parameter; the read skips the league filter and matches any follow in the group |
| `src/myNewspaper.Web/Components/ScoreboardPanel.razor` | Conferences group lists `<Conference> · All sports` before each conference with two or more follows; `CoverageHasIssues()` scopes the warning to the group's leagues |
| `tests/myNewspaper.Tests/EventsTests.cs` | Choice exists, comes first among Big 12, returns football and men's basketball but not women's basketball |
| `tests/myNewspaper.Tests/EventsRenderTests.cs` | Choice is found by value, and `big12%7C` round-trips through a page link |
| `tests/myNewspaper.Tests/ScoreboardTests.cs` | `g:` resolves (including lower case), unknown and empty groups fall back, matching per follow |
| `tests/myNewspaper.Tests/ScoreboardReadTests.cs` | Live-data read of `g:Big 12` equals the union of Big 12 follow matches, with no women's basketball |
| `docs/sports.md` | Upcoming schedule and Live scoreboard sections describe the new choice |

Also in the working tree, unrelated to this plan: a formatting-only line wrap in
`src/myNewspaper.Web/wwwroot/personal.js` (hourly weather rain chance). Commit it separately or leave it out.

Notes for whoever finishes the work:
- The legacy `competition=g:<name>` path wasn't in the plan; it's harmless, but it has no test.
- `EventsRenderTests` re-creates `PageHref` inside the test rather than rendering `Events.razor`, so it doesn't prove
  the page itself keeps `schedule=big12|` across pages. The browser check below should cover that.
- `GameGroups.Schedule` headings with several Big 12 sports on one day haven't been checked yet.
- Not run yet: `tools/verify.sh`, browser and mobile checks, clean-up, commit, deploy, live checks.

## Verification
- `tools/verify.sh`.
- Browser checks at 320, 390 and 430px and on desktop, in light and dark themes, on `/events?schedule=big12%7C` and
  `/scoreboard?show=g%3ABig%2012`. Check the menus, paging, day switching, the Final filter, fold links, overflow and
  44px tap targets.
- Clean-up of test data per AGENTS.md; `tools/sql/household-check.sh` afterwards.
- Commit locally on `develop`, deploy with `deployment/deploy.sh`, then `node tools/live-check.cjs` plus the two URLs
  above against the live build.

## Open questions
- None outstanding. If a future conference other than Big 12 gets two or more follows, it also gets an "All sports"
  choice on the Scoreboard. Competitions stays Big 12-only, because `big12` is the only conference provider there.
