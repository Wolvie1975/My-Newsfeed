# College Sports, revised: Top 25 into Rankings, News into Headlines

Written November 4, 2026 (America/Chicago), against `develop` at `974ddbe`. Follows
[the .NET coding standard](dotnet-coding-standard.md) and [the coding standards addendum](coding-standards.md).

**Save as:** `docs/college-sports-revised-handoff.md`

**Mockups:** the myNewspaper canvas, **College Sports, revised** page (https://claude.ai/artifact/JVQ8mU7R63Z1cziNpeW9Z5):

- Schedule (desktop) and Rankings (desktop), at 1440;
- Schedule (phone) and Rankings (phone), at 390.

Red tags on the boards show what moved. Big 12 headlines, non-Big 12 teams and poll names are placeholders.

## What changes

1. **Top 25 joins College Sports.**
   - The Sports tab and the phone menu item go.
   - College → **Rankings** becomes the only rankings page, with the poll pills that used to be on `/rankings` only.
   - `/rankings` redirects there.
2. **College → News goes.** It showed the same stories as Sports → Headlines → **Big 12** (the "Big 12 News"
   category, `/sports/news/big12`). Instead:
   - `/college/news` redirects there;
   - College gets a small **Big 12 headlines** box (desktop rail and phone fold).

**Result:**

- Sports tabs: Sports home · Headlines · Scoreboard · Competitions · Standings · College Sports · F1.
- College views: Schedule · Results · Standings · Rankings.

Nothing else on the site loses a feature. No database changes.

---

## 1. Navigation

- [ ] **`Components/SectionChoices.cs`:** remove `new("rankings", "Top 25")` from `Sports`.
- [ ] **`Components/Layout/PublicLayout.razor`:**
  - Remove the mobile-nav `<a href="rankings" …>Top 25</a>` (~line 146).
  - Remove `onRankings` and `onBig12News` and their uses in `onSportsSection`. `/college/rankings` is already covered
    by `onCollege`.
- [ ] **`Components/Pages/College.razor`:** `Tabs` becomes
  `[("schedule", "Schedule"), ("results", "Results"), ("standings", "Standings"), ("rankings", "Rankings")]`.
  - Remove `@page "/college/news"`.
  - Remove the `view == "news"` branch.
  - Remove `"news"` from the `view =` parse and the early-return check (~lines 172–175).
- [ ] **`Components/Phone/PhoneCollege.razor`:** `Views` becomes
  `[("schedule", "Schedule"), ("results", "Results"), ("standings", "Standings"), ("rankings", "Rankings")]`. The
  label "Top 25" becomes "Rankings", and the `("news", "Headlines")` entry goes. The Explore fold lists these plus the
  other sports, as the mockup shows.

## 2. Redirects (bookmarks, the installed app, Saved links)

Add both to `PublicEndpoints.cs`, next to `/navigate`, as minimal-API endpoints. They're permanent (301) and keep the
query:

- [ ] **`/college/news` → `/sports/news/big12`.** Keep `q`, `source` and `after`, and drop `sport`. Big12News used the
  same `source` ids (the Big 12 News category's sources), so a source filter carries over.

```csharp
app.MapGet("/college/news", (HttpContext http) =>
{
    var keep = http.Request.Query.Where(q => q.Key is "q" or "source" or "after");
    var query = QueryString.Create(keep.Select(q => KeyValuePair.Create(q.Key, (string?)q.Value.ToString())));
    return Results.Redirect(http.Request.PathBase + "/sports/news/big12" + query, permanent: true);
});
```

- [ ] **`/rankings` → `/college/rankings?sport=<key>[&sourceId=<id>]`.** With `sourceId`, look up the source's `Sport`
  and resolve it with `CollegeHub.Resolve(sport)`. Without one, go to football. Add the lookup as
  `RankingsQuery.SportForSourceAsync(db, id, ct)`, returning `string?`, so the endpoint doesn't query directly
  (coding standards: Data). An unknown id goes to `/college/rankings` with no sport.
- [ ] **Remove the page route.** Remove `@page "/rankings"` from `Rankings.razor`. The component stays, used by
  College's Rankings view.
- [ ] **Check every enabled poll has a home.** `RankingsQuery.GetAsync` already excludes Women's Basketball. Any
  other enabled `SportsRankingSources.Sport` that isn't one of `CollegeHub.Sports`' `StandingsSport` values would
  lose its page. Run
  `SELECT DISTINCT Sport FROM SportsRankingSources WHERE Enabled = 1` against the test database. If anything is left
  over, add it to `CollegeHub.Sports`, or tell Jason, before removing the route.

## 3. One helper for rankings links

Several places link to `rankings?sourceId=…`. Give them one helper in Shared:

- [ ] **`CollegeHub.RankingsHref`** (`myNewspaper.Shared/Data/CollegeHub.cs`):

```csharp
/// <summary>College → Rankings for a poll: its sport's tab, with the poll selected.</summary>
public static string RankingsHref(SportsRankingSource? source) =>
    source is null ? "college/rankings" : Href("rankings", Resolve(source.Sport)) + "&sourceId=" + source.Id.ToString(CultureInfo.InvariantCulture);
```

- [ ] **Point every link at it** (from a search of the current code):

| File | Today | Change to |
|---|---|---|
| `HomeRankings.razor` ~6 | `rankings?sourceId={selected.Id}` / `rankings` | `CollegeHub.RankingsHref(Data.Selected)` |
| `Pages/Home.razor` ~428, ~432 (phone Top 25 fold) | `AllHref="rankings"`, `href="rankings"` | `CollegeHub.RankingsHref(homeRankings.Selected)` |
| `SportsFront.razor` ~142 | `rankings?sourceId={rankings.Selected?.Id}` | `CollegeHub.RankingsHref(rankings.Selected)` |
| `SportsFront.razor` ~227 (phone fold) | `AllHref` to rankings | same helper |
| `ScoreboardPanel.razor` ~148 | `href="rankings"` ("Full poll →") | the helper with the panel's poll source, or `college/rankings` |
| `Phone/PhoneStandings.razor` ~67 | "Big 12 in the poll" `AllHref` | the helper |
| `SectionAgenda.razor` ~51 | the "Sports guides" links | drop Top 25, or point it at `college/rankings` |

The Home section key `rankings` and its label "Top 25" in `SiteDefaults.cs` stay as they are. That's the Home box's
own name, and people's Settings refer to it.

## 4. College → Rankings becomes the rankings page

The `Rankings` component already runs in college mode (`CollegeSport` set, `BasePath = "college/rankings"`). What
the Top 25 page had and college mode doesn't:

- [ ] **Poll pills in college mode.** Today the pills and the dropdown render only when `CollegeSport is null`. Change
  them to render whenever the sport has **more than one** enabled poll (`data.Sources.Count > 1`):
  - **Desktop:** pills beside the heading, as in the mockup (AP Top 25 · Coaches Poll · CFP Rankings). Each links to
    `college/rankings?sport=<key>&sourceId=<id>`. Reuse `.sec-pills` for the look.
  - **Phone:** a segmented control. Reuse the `.sec-pills` phone presentation, which is already a segmented control
    for up to three choices. Above three, keep the dropdown that's there today.
  - **No JavaScript:** links, plus the existing GET form fallback.
- [ ] **Remove the Top 25 page shell.** Delete the `CollegeSport is null` branches: the `PageTitle`, the
  `PageHeading`, the `mx-desktop` wrapper and `<PhoneRankings …/>`. Then **delete `Phone/PhoneRankings.razor`**,
  which only the Top 25 page used. Check no CSS targets `[data-phone-sports="rankings"]` alone; if something does,
  remove it too.
- [ ] **Heading.** "<Sport> rankings" (for example "Football rankings"), then the existing poll line (poll name ·
  season · dated · source link) as a sub-line.
- [ ] **"Other sports' polls"** (desktop rail and phone fold): one row per other `CollegeHub.Sports` entry that has an
  enabled poll, showing the poll's name and linking to `CollegeHub.RankingsHref(thatSportsFirstSource)`. The data is
  `RankingsQuery.GetAsync(db, null, …, collegeSport: null).Sources` grouped by `Sport`. Load it once in
  `College.razor` for the Rankings view.
- [ ] **Page title.** `College Sports · <Sport> rankings · myNewspaper` on the Rankings view.

## 5. Big 12 headlines box (replaces the News tab)

- [ ] **Data.** In `College.razor` (all views), load the 3 latest Big 12 News headlines:
  `(await Big12NewsQuery.GetAsync(db, null, null, null)).Feed.Items.Take(3)`. If that loads more than it needs, add
  `Big12NewsQuery.LatestAsync(db, take, ct)`. Catch read failures with `DataFailures.IsReadFailure`, and hide the box
  on failure.
- [ ] **Desktop.** The last box in `.cl-side` (Schedule), and in the Rankings rail:

```razor
@if (big12News.Count > 0)
{
    <section aria-labelledby="cl-news-title">
        <h2 id="cl-news-title" class="ef-label">Big 12 headlines</h2>
        <ul class="ef-headlines spf-headlines">
            @foreach (var item in big12News) { @* same row as the Sports front's headlines: kicker = source, title, time, Share, Save *@ }
        </ul>
        <a class="ef-more" href="sports/news/big12">More in Headlines <span aria-hidden="true">→</span></a>
    </section>
}
```

  Reuse the Sports front's headline row (`SportsFront.razor`), not a copy. If it's a private `RenderFragment`,
  promote it to a small shared component (`Components/Feed/HeadlineRow.razor`) and use it in both.
- [ ] **Phone.** `PhoneCollege.razor`: a **Big 12 headlines** fold after "Big 12 in the Top 25", open when it's the
  first open fold (the `first` pattern), with `AllHref="sports/news/big12"` and `AllText="More in Headlines"`.
  - On the phone **Rankings** view, render the same fold (closed) after the list, then **Other sports' polls**.
- [ ] **Delete `Pages/Big12News.razor`.** Headlines has the source filter and search. Keep `Big12NewsQuery`, which
  the box uses and `/sports/news/big12` may share. Check it still has callers, and remove anything that's become
  unused.

## 6. Tests

- [ ] **.NET:**
  - **`RankingsTests`:** add `CollegeHub.RankingsHref` cases (null → `college/rankings`; a Football source →
    `college/rankings?sport=football&sourceId=…`; a Volleyball source → `sport=volleyball`) and
    `SportForSourceAsync`.
  - **`HomeRankingsTests` / `PhoneSportsTests`:** update any expected `rankings…` hrefs.
- [ ] **Browser** (update the checks that reference the old routes):
  - `home-top25`, `scoreboard-top25`, `sports-panels`, `sports-news`, `phone-sports`, `phone-life`, `section-select`
    and `wide-screens` reference `/rankings`, `college/news` or "Top 25".
  - Point them at the new routes, and add a check that `/rankings?sourceId=<id>` and `/college/news?q=x` return 301 to
    the right place.
  - Every run cleans up its own data (`AGENTS.md`).

## 7. Verify

Record widths, themes and outcomes in the release evidence (`AGENTS.md`).

- [ ] **Sports tabs and menus** (desktop tabs, phone ribbon menu, header Menu): no Top 25, and College Sports is
  underlined on every College view, Rankings included.
- [ ] **College views:**
  - Four tabs on desktop and phone.
  - Schedule shows Big 12 headlines (3) with "More in Headlines →" going to `/sports/news/big12`.
  - With no Big 12 stories, the box is hidden.
- [ ] **Rankings:**
  - Each sport shows its poll. With 2+ polls, the pills or segmented control switch them.
  - Big 12 teams are highlighted.
  - Other sports' polls links land on the right sport and poll.
  - Men's Basketball before its season shows the existing "not available" state, not an error.
- [ ] **Redirects:**
  - `/rankings` goes to football.
  - `/rankings?sourceId=<volleyball poll>` goes to `college/rankings?sport=volleyball&sourceId=…`.
  - `/college/news?q=utah` goes to `/sports/news/big12?q=utah`.
  - All return 301.
- [ ] **Old links on Home, the Sports front, Scoreboard and Standings** all land on College → Rankings.
- [ ] **Phone** (320, 390, 430, light and dark):
  - The four view tabs fit at 320.
  - The segmented control fits three polls at 320, or falls back to the dropdown.
  - The folds open and close, and every touch target is at least 44px.
- [ ] **Desktop** (1024, 1440, 1920) and **iPad** (744, 820): no orphaned spacing where the News tab or Top 25 used
  to be.
- [ ] **Without JavaScript:** the poll switcher and sport switcher work as links or GET forms.
- [ ] **`dotnet test`**, `npm run lint`, `dotnet format --verify-no-changes`.
