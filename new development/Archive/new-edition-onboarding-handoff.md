# New edition onboarding handoff

Written October 30, 2026 (America/Chicago) against `develop` @ `13c1858`. This is a proposed plan, not a record of
what was built. Check the repository's current state before changing anything.

**Lives in:** `new development/new-edition-onboarding-handoff.md`. It builds on `new development/profiles-handoff.md`
(the "decision N" numbers below refer to that file) and changes its section 6.2, New profile setup.

**No mockups yet.** Before building U2, add a "New edition setup v2 — phone" artboard to the myNewspaper canvas next to
the current one, and get it approved.

---

## The problem

A new person opens the site, picks **+ Start a new edition**, and goes through four steps
(`Components/Pages/Editions/NewEdition.razor`):

1. name, color and **PIN**
2. teams
3. services
4. home page sections (14 switches with move-up buttons)

Then **Open my edition** sends them to `/` with no explanation. Gaps:

- **It opens on the least welcoming question.** Before anything else, step 1 asks for a PIN and warns "don't reuse a
  bank or phone PIN".
- **Step 4 is hard to answer.** It asks the person to rank sections such as "Box office" and "Upcoming Digital / VOD"
  before they've seen them. The home page already lets them reorder sections later.
- **There's no news or YouTube step.** News sources aren't per-profile at all. Today a new profile silently gets
  every household channel (decision 11), so it starts with 13 Tech, 10 Sports and 8 Video Games channels whether it
  wants them or not.
- **There's no TV step.** "Today's episodes" stays empty and the picker card's "New for you" shows nothing until the
  person finds Settings → Shows & channels.
- **There's no first-visit orientation**, and empty sections don't say how to fill them.
- **The empty picker** says "There are no editions yet…" instead of starting setup.

What exists today (checked October 30):

| Kind | Count | Grouped by | Per-profile today? |
|---|---|---|---|
| News sources | ~92 enabled | 15 `SourceCategories` across 3 hubs (headlines 8, sports 4, entertainment 3) | **No.** Every source shows for everyone (profiles handoff 5.3) |
| YouTube channels | 34 | 4 `YoutubeCategories` + 2 uncategorized | **Yes.** `ProfileChannelFollows`; 32 household channels rolled out to everyone, 2 private (requested) |
| TV shows | — | — | Yes, `ProfileTvFollows` |

---

## Decisions proposed here

Settle each of these before building the release it affects. Each one has a recommendation.

1. **People choose news by category, not by source.** 92 sources is too many to pick from during setup, and most
   people don't know the publishers. The 15 categories ("Technology", "Kansas City News", "F1 News"…) are the right
   size for a choice, and each one shows a few of its publishers so the choice makes sense. Muting a single
   publisher stays in Settings (the planned **muted publishers**, profiles handoff 6.3), not in setup.
2. **Everything starts on. Setup only turns things off.** This keeps decision 11 for YouTube, and news works the same
   way. Someone who skips a step gets the full house paper rather than empty pages. *(Alternative: start every
   channel off and make people opt in. Rejected because a skipped step would leave Videos empty.)*
3. **Whole categories can be switched off in one tap**, and individual channels within them can still be switched off.
   For news, setup only offers category switches.
4. **The privacy rules stay as they are (decision 8).** Setup lists only what is already shared: news categories and
   their sources, and household channels. It never lists:
   - channels added by someone else's request
   - shows other profiles track ("popular in this house" is out, because it would reveal what someone else follows)
5. **Show suggestions come from public data**, not the household's tracked list: TVmaze's schedule/popular results
   through the existing `TvShowSearch` and `WatchCatalog` code.
6. **The PIN moves to the last step.** It's still required (decision 2), and Open my edition stays disabled until both
   PINs match.
7. **The home-sections step goes away.** Its job is done by the news step (turning off a whole hub hides that hub's
   home section, see N3) and by the welcome strip, which points at the home page's own reorder controls.
8. **Every step after the name can be skipped.** Each step has **Skip**, and there's a **Skip to the end** link that
   goes straight to the PIN step with the base edition's values.

---

## The new setup flow (`/editions/new`)

Still one form posting to `/profiles/new`, so it works without JavaScript. `profiles.js` still turns the fieldsets into
steps with Back, Continue and a progress bar.

| Step | Heading | Contents | Default |
|---|---|---|---|
| 1 | Who's this edition for? | Name and color (no PIN) | — |
| 2 | Who do you follow? | Teams (unchanged) | Base edition |
| 3 | What do you watch on? | Services (unchanged) | Base edition |
| 4 | What news do you want? | News categories grouped by hub, with sample publishers | All on |
| 5 | Which YouTube channels? | Household channels grouped by YouTube category | All on |
| 6 | Any shows you're watching? | TVmaze search and a short list of suggestions | None |
| 7 | Lock your edition | PIN twice, then **Open my edition** | — |

The kicker changes from "Step N of 4" to "Step N of 7". Seven steps is fine because each one is a single screen and
steps 2–6 can be skipped. If the canvas review finds it too long, merge steps 4 and 5 into one "Your sources" step
with News and YouTube tabs.

### Step 4 — News

```
What news do you want?
Turn off anything you don't care about. You can change this later in Settings.

HEADLINES                                         [Turn all off]
 [x] Top Headlines      BBC News, NBC News, The New York Times…
 [x] Kansas City News   KSHB 41, KCUR, The Pitch…
 [x] Technology         Ars Technica, The Verge, Engadget…
 [x] Linux              Phoronix, 9to5Linux, It's FOSS…
 [x] Video Games        IGN, Polygon, Eurogamer…
 [x] Opinion            The Guardian, IndieWire, PC Gamer…
 [x] U.S. News          …
SPORTS                                            [Turn all off]
 [x] Big 12 News · Sports · F1 News · Men's College Basketball News
ENTERTAINMENT                                     [Turn all off]
 [x] TV/Film · Box Office · Entertainment
```

- Lists only categories that have at least one enabled source (the same rule as `FeedQuery.GetCategoriesAsync`).
  Under each one, show up to 3 publishers from `Sources.Label`: the part before " — ", without duplicates.
- Unchecked categories are posted as `newsOff` fields.
- **Topics hook (optional):** at the bottom, "Follow a topic" links to the existing Topics follow page after setup,
  rather than repeating that form here.

### Step 5 — YouTube

```
Which YouTube channels?
These are the household's channels. Turn off any you don't want.

TECH (13)          [on]   ▸ show channels
SPORTS (10)        [on]   ▸ show channels
VIDEO GAMES (8)    [on]   ▸ show channels
NEWS & POLITICS (1)[on]   ▸ show channels
OTHER (…)          [on]   ▸ show channels

Want a channel that isn't here? You can request one in Settings after setup.
```

- A category switch toggles every channel in it. Expanding a category shows per-channel checkboxes. Without
  JavaScript, it's a plain list of checkboxes inside `<details>`.
- **Sources:** the rows in `MyDailyFeedChannelRollouts`, which is exactly the set setup turns on today, joined to
  `YoutubeCategories`. Uncategorized channels go under "Other", as the Videos page already does. Requested channels never appear.
- **No household picks in setup.** Setup is the default experience plus opt-outs; adding anything beyond it happens
  later in Settings.
- Unchecked household channels are posted as `channelOff` fields.
- **If there are no household channels**, skip the step automatically.

### Step 6 — Shows

- A search box using the same TVmaze search as Settings → Shows & channels (`TvShowSearch.razor`). Each result has
  **Follow**, never "Add and follow" (decision 8).
- **Suggestions:** up to 8 shows airing this week from public TVmaze schedule data, filtered to the profile's chosen
  services (step 3) when there are any. If that fails, hide the suggestions and keep the search.
- Followed show IDs are posted as `show` fields. **Rename the existing `show` field first**: it's currently the
  home-section field (`form["show"]` in `ProfileEndpoints.CreateAsync`). Rename that one to `section`.
- Without JavaScript, the search falls back to "You can add shows in Settings" and the step is informational.

### Step 7 — Lock your edition

The current PIN fields and hint, unchanged, plus a one-line summary of the earlier steps: "Kansas, Big 12 · 4 services
· 12 news categories · 28 channels · 2 shows". The submit button reads **Open my edition**.

---

## After setup

### Welcome strip (first home visit)

Shown above the lead story, once per profile, until it's dismissed or the person has visited three times:

```
Welcome to Jacob's edition.
Your teams are highlighted in scores · Where to watch lists your services first ·
Headlines and Videos show only what you picked.
[Rearrange your front page]   [Settings]   [Got it]
```

- **Rearrange your front page** scrolls to and opens the home page's existing section order controls.
- Stored in the profile's settings as `welcome: "done"`, not in localStorage, so it doesn't come back on another
  device.

### Empty states that link to the fix

| Where | When | Message |
|---|---|---|
| Home → Today's episodes | No followed shows | "Follow a show to see its episodes here. **Add a show →**" |
| Home → Latest videos, `/videos` | No followed channels | "You've turned off every channel. **Choose channels →**" |
| Headlines hub pages | Every category in the hub is off | "You've turned off all {Sports} news. **Turn it back on →**" |
| Scoreboard / Today & Tonight | No teams | Unchanged (shows everything); add "**Pick your teams →**" under the heading |

Each link goes to the matching Settings section anchor.

### Empty picker

If `page.Editions.Count == 0`, `/editions` redirects to `/editions/new` (keeping `returnUrl`) instead of showing the
"no editions yet" message.

---

## Implementation notes

### N1 — Settings JSON

`ProfileSettings.Keys` rejects unknown keys, so add these two:

```json
{
  "newsOff": [12, 15],      // SourceCategories.ID values the profile turned off; missing = none
  "welcome": "done"         // missing = show the strip
}
```

- `Normalize("newsOff")`: distinct positive ints, at most 200 of them. Unknown IDs are kept: a category that comes back
  later stays off.
- The base edition editor (6.4) should **not** offer `newsOff`. News taste is personal (decision 12), so `FromSetup`
  ignores any `newsOff` that's in the base edition.
- Settings → Reading gains a **News categories** list with the same grouping as step 4, so someone can undo a choice.

### N2 — Filtering news by profile

- `FeedQuery.Visible(db)` is the shared starting point for headline reads. Give it the off-list:
  `Visible(db, IReadOnlySet<int> newsOff)`, filtering with `!newsOff.Contains(p.Source.SourceCategoryId ?? 0)`.
  Thread it through every public method that reads `Pages`, the same way `Viewer` was threaded through `TvQuery` and
  `VideosQuery` (make it a required parameter so the compiler finds every caller).
- Also check the other readers of `Pages` by category: `Big12NewsQuery`, `LocalNewsQuery`, `HomeOpinionQuery` and the
  news part of `BoxOfficeQuery`. Data pages (standings, box office chart, scores) are **not** filtered; they aren't news
  sources.
- `GetCategoriesAsync` (the category tabs) drops categories that are off. A direct `?category=N` link to a category
  that's off shows the empty state with **Turn it back on**. This isn't private data, so it doesn't need to look
  unknown.
- `FeedEndpoints` (`/feed/more`) reads the off-list from `ProfileContext` and must use the same filter, so later
  pages can't bring hidden categories back.
- Picker previews (`ProfilePageData`) use each profile's own off-list for its "New for you" count.

### N3 — Hubs and home sections

When every category in a hub is off, hide that hub's home section in the same save:

| Hub | Home section |
|---|---|
| headlines | `news` (Headlines) |
| sports | `sports-news` |
| entertainment | `entertainment-news` |

The **Opinion** category also maps to the `opinion` section. Turning a category back on doesn't un-hide the section
automatically; the empty state and Settings explain this.

### N4 — Creating the profile

- `ProfileEndpoints.CreateAsync` (setup branch) reads these new fields: `newsOff`, `channelOff` and `show` (TVmaze IDs), and `section`, which is the renamed home-section field (only from old clients; the new form
  doesn't send it).
- The household-channel follow insert in `ProfileStore.CreateAsync` (the `INSERT … FROM dbo.MyDailyFeedChannelRollouts`)
  skips `channelOff` IDs. Ignore any ID that isn't a household channel.
- Shows are followed through the same code as Settings → Add a show (track once for the household, then follow;
  decision 6). Do this after the profile row exists, inside the same transaction where possible. If TVmaze fails,
  create the profile anyway and show "We couldn't add {n} shows; add them in Settings" on the welcome strip.
- The PIN check (`pin != pinConfirm`) is unchanged; it now runs on the last step.

### N5 — Privacy checks

Add to the existing privacy test (profiles handoff 4.3):

- Setup's YouTube step lists no channel that came from profile B's request.
- Show suggestions don't change when profile B follows a show.
- Profile A's `newsOff` never affects profile B's headlines or picker card.

---

## Build order

Each release can ship by itself.

### U1 — Small wins (no new settings)
- [ ] Empty picker redirects to `/editions/new`
- [ ] Move the PIN fields to the last step. Setup becomes name → teams → services → sections → PIN
- [ ] Add **Skip** to each step after the name, and a **Skip to the end** link
- [ ] Add the empty states for Today's episodes and Latest videos/`/videos`
- [ ] Browser test: setup completes with every step skipped

### U2 — Welcome strip
- [ ] Add the `welcome` settings key (N1)
- [ ] Add the strip to `Home.razor` on desktop and phone, with **Got it** saving through the existing settings endpoint
- [ ] Add **Rearrange your front page** to open the existing order controls

### U3 — YouTube step
- [ ] Add step 5 (category switches, per-channel checkboxes)
- [ ] Add `channelOff` to the create endpoint and the follow insert (N4)
- [ ] Add the privacy test for requested channels (N5)

### U4 — News by category
- [ ] Add the `newsOff` key and Normalize rule (N1)
- [ ] Add the `FeedQuery` filter and thread it through every caller, including `/feed/more` and the other news queries
  (N2)
- [ ] Add step 4, and the **News categories** list in Settings → Reading
- [ ] Add the hub → home section rule (N3) and the hub empty state
- [ ] Remove the home-sections step from setup (decision 7) and rename the `show` field to `section`
- [ ] Add the privacy test (N5)

### U5 — Shows step
- [ ] Add step 6 with search and public suggestions
- [ ] Follow the chosen shows on create (N4), including the partial-failure message
- [ ] Add the privacy test for suggestions (N5)

## Verify (per AGENTS.md)

- .NET tests: `dotnet run --project tests/myNewspaper.Tests`
- Browser: create a throwaway profile on desktop and phone widths through every step. Then check:
  - turned-off news categories and channels are missing from Home, Headlines, `/feed/more` and Videos
  - followed shows appear in Today's episodes
  - the welcome strip shows once and stays dismissed on a second device
- Remove the throwaway profile and its device afterwards.

## Still open

- Whether the welcome strip should also appear for **existing** profiles once, to announce news categories after U4.

---

## Side item — Remove Women's Basketball (web app only)

Added October 30, 2026. The site doesn't cover college women's basketball. Every public page already hides it, and
this item also stops the **web app** collecting it. It doesn't depend on the onboarding releases, so it can ship
first. College sports in the rest of this plan are the four on the College pages: Football, Men's Basketball, Women's
Volleyball and Women's Soccer.

**Out of scope:** the scraper. Its rows stay and are ignored:
- ranking source 3 (Women's Basketball AP poll)
- standings source 6 (Big 12 Women's Basketball)
- about 350 Big 12 women's basketball games in `SportsEvents`, which come from the 16 school feeds that carry every
  sport

The web filters below keep all of them hidden.

### What's hidden today

Each of these filters out `"Women's Basketball"` / `womens-college-basketball`:
- `EventsQuery`
- `ResultsQuery`
- `StandingsQuery`
- `Scoreboard`
- `BoxScores`

Keep these filters: they're what hides the scraper's rows.

- [x] **Top 25** (`RankingsQuery.GetAsync`): excludes the Women's Basketball source. Without this, the poll would have
  appeared once Big 12 women's games start (November 2). It's covered by
  `RankingsTests.Basketball_poll_stays_hidden_until_opening_day_and_current_season_publication`, and the rankings
  tests pass. **Done October 30, not committed.** No phone check yet, because the page looks the same until November 2.

### Stop the web app collecting it

- [ ] **Turn off the score follow.** Set `ResultFollows` ID 2029 (Kind `conference`, League
  `womens-college-basketball`, "Big 12") to `Enabled = 0`. Do it in Admin → Results follows if that page has an
  on/off switch; otherwise use a one-line SQL update recorded in `db/` like the other data changes. **This changes
  the live database. Confirm with the user first.** The `ScoresSync` loop then skips it.
- [ ] **Stop it being added again.** Remove the `womens-college-basketball` entry from `EspnLeagues.All`
  (`Scores/EspnClient.cs`), so Admin → Add a results follow no longer offers it. `ScoresSync` only uses
  `EspnLeagues.Find` for optional settings (`?.`), so an old row for that league can't crash the sync.
- [ ] **Decide on old score rows.** `ResultGames` rows already synced for that follow stay hidden by the filters above.
  Leave them unless the user wants them deleted.
- [ ] **Leave the name mappings alone:** the labels in `ScoreboardPanel.razor`, `GameGroups`, `TeamRecords`,
  `ScheduleCoverage` and `LeaguePhases`. They're harmless and still name the scraper's rows correctly if anything
  reads them. WNBA is a separate league (`espn-wnba`, `wnba`) and isn't touched.

### Verify

- Run `dotnet run --project tests/myNewspaper.Tests`. Check whether any test seeds a `womens-college-basketball`
  follow through `EspnLeagues.All`, and update it.
- In Admin → Results follows, check that the follow shows as off and that Add doesn't list Women's College Basketball.
- After the next sync, check that ID 2029's `LastSyncedAt` no longer moves.
- On a phone (320, 390 and 430px, light and dark), check that Scoreboard, Results, Top 25 and College show no women's
  basketball. Then repeat after November 2 against the live build, once the women's season has started.
