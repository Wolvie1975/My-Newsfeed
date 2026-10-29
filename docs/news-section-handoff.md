# News section handoff

Written October 28, 2026 (America/Chicago) against `develop` @ `a7ea7d2`. Verify current repository state before
making changes; line numbers and names are from that commit.

**Save as:** `docs/news-section-handoff.md`

**Approved mockups:** the myNewspaper canvas, **Site concept** page (https://claude.ai/artifact/JVQ8mU7R63Z1cziNpeW9Z5):

| Artboard | Shows |
|---|---|
| News — section front, desktop | The News landing page, built like Sports and Entertainment. Yellow **NEW** tags mark content that needs new data |
| News — section front, phone, folded | The same page with the folded phone pieces from `mobile-refresh-handoff.md` |
| Your topics — follow sheet / topic page / follow dialog | Following a phrase, the live preview with its three verdicts, and a topic's own page |
| News › Kansas City — desktop / phone (Local row) | The Local page moved under News |

## Decisions (agreed in the design session)

1. **Headlines becomes a section called News**, with a landing page like Sports and Entertainment. The current headlines
   list moves to **All headlines**.
2. **Local folds into News** as the **Kansas City** tab. The top navigation drops Local and renames Headlines to News
   (7 links → 6).
3. **Your topics:** a profile follows a word or phrase and sees every matching story. Topics are private to the profile,
   with the same rule as TV and channel follows: no admin view of them, only counts.
4. **Share and Save** on every story row with a number or a headline, phones included.
5. **No News video section.**
6. **Later (needs data the site doesn't have):** Markets tile, "How this story developed" (needs story grouping) and
   Weather alerts. They're on the mockups for direction only.

## Routes after this change

| Path | Page | Was |
|---|---|---|
| `/news` | News front (new `NewsFront` component) | Headlines list |
| `/news/all` | All headlines (today's `News.razor` list, unchanged inside) | `/news` |
| `/news/all?category={id}` | One category (the tabs below) | `/news?category={id}` |
| `/news/kansas-city` | Kansas City (today's `Local.razor`) | `/local` |
| `/news/topics/{id}` | One of your topics | new |
| `/local`, `/news?category=…`, `/news?q=…` | Permanent redirects (301) to the new paths, keeping the query string | |

News tabs, in order: **News home** · **All headlines** · **Top Headlines** · **Kansas City** · **Opinion** ·
**Technology** · **Video Games** · **Linux** · **More News**. The category tabs come from the `headlines` hub categories
(`SourceCategories.NewsHub = 'headlines'`), so an admin can add one without code. Kansas City is the one fixed exception:
its tab goes to `/news/kansas-city`, not to a category list.

---

## Phase 1 — Navigation and routes

### 1.1 `Components/SectionChoices.cs`

The tab list is built from the database, so it's a method, not a constant:

```csharp
public const string NewsKansasCityCategory = "Kansas City News";

/// <summary>News tabs: home, all, then each headlines-hub category; Kansas City goes to its own page.</summary>
public static IReadOnlyList<NavigationChoice> News(IEnumerable<FeedCategory> headlinesCategories) =>
[
    new("news", "News home"),
    new("news/all", "All headlines"),
    .. headlinesCategories.Select(c => c.Name == NewsKansasCityCategory
        ? new NavigationChoice("news/kansas-city", "Kansas City")
        : new NavigationChoice($"news/all?category={c.Id}", c.Name)),
];
```

`For(section)` can't build this one without the categories, so `PageHeading` takes the list directly when
`Section == "news"`: add `[Parameter] public IReadOnlyList<NavigationChoice>? Choices { get; set; }` and use it before
falling back to `SectionChoices.For(Section)`. Change the label switch to cover News:

```razor
Label="@(Section switch { "sports" => "Sports", "entertainment" => "Entertainment", _ => "News" }) sections"
```

Load the categories once per request: the section front, All headlines and Kansas City already query categories, so
they pass `FeedQuery.GetCategoriesAsync(db)` filtered to `Hub == "headlines"`.

### 1.2 `Components/Layout/PublicLayout.razor`

- In the inline nav: delete the Local link and rename Headlines:
  ```razor
  <a href="news" aria-current="@(onNewsHome ? "page" : onNewsSection ? "true" : null)">News</a>
  ```
- Replace `onNews` and `onLocal` with:
  ```csharp
  var bare = path.Split('?', '#')[0].TrimEnd('/');
  var onNewsHome = bare.Equals("news", StringComparison.OrdinalIgnoreCase);
  var onNewsSection = onNewsHome || bare.StartsWith("news/", StringComparison.OrdinalIgnoreCase);
  ```
  Keep `onNews` for anything else that reads it, pointing it at `onNewsSection`.
- The phone menu (`mobile-nav__links`) gets the same two edits.

### 1.3 Pages

- `Components/Pages/SectionHub.razor`: add `@page "/news"` and a third branch that renders
  `<PageHeading Section="news" Choices="newsTabs" Title="News" Eyebrow="The day’s news, every source" Description="" />`
  inside `mx-desktop`, then `<NewsFront />`.
- `Components/Pages/News.razor`: change `@page "/news"` to `@page "/news/all"`. Its `BasePath` and the form
  `action="news"` become `news/all`. The `hl-cats` tab strip is replaced by `PageHeading Section="news"` with the tab
  list (desktop tabs, phone dropdown), so delete `hl-cats` and the phone category `<form>` for the headlines hub.
  Sports and Entertainment news routes in this file don't change.
- **Redirects** in `Program.cs`, before `MapRazorComponents`:
  ```csharp
  // Old Headlines and Local addresses (bookmarks, the installed app, Saved links).
  app.MapGet("/local", (HttpContext c) => Results.Redirect("/news/kansas-city" + c.Request.QueryString, permanent: true));
  app.Use(async (context, next) =>
  {
      // /news with a list query (category, source, q, after) is the old list: send it to /news/all.
      if (context.Request.Path.Equals("/news", StringComparison.OrdinalIgnoreCase)
          && context.Request.Query.Keys.Any(k => k is "category" or "source" or "q" or "after"))
      {
          context.Response.Redirect("/news/all" + context.Request.QueryString, permanent: true);
          return;
      }
      await next();
  });
  ```
- `wwwroot/service-worker.js`: add `'/news/all'` and `'/news/kansas-city'` to `PUBLIC_PAGES`, and bump `CACHE` one
  version (the logo patch moved it to `v6`, so `v7`).

### 1.4 Other links to update

Search for `"local"`, `href="local`, `"news?` and `href="news?` in `Components/` and `wwwroot/*.js`. Home's Kansas City
links, the weather tile link and the Saved "open source list" links are the likely ones. Point them at
`news/kansas-city` and `news/all?…`.

---

## Phase 2 — Local becomes News › Kansas City

`Components/Pages/Local.razor`:

1. Add `@page "/news/kansas-city"` and keep `@page "/local"` until the redirect in 1.3 ships, then remove it.
2. Desktop heading: replace
   `<PageHeading Title="Kansas City" Eyebrow="Closer to home" … />` and the `local-section` `SectionSelect` with:
   ```razor
   <PageHeading Section="news" Choices="newsTabs" SectionCurrent="news/kansas-city"
                Title="Kansas City" Eyebrow="News · closer to home" Description="" />
   <nav class="lc-jump" aria-label="On this page">
       <a href="news/kansas-city#local-weather">Forecast</a>
       <a href="news/kansas-city#local-news">Local news</a>
       <a href="news/kansas-city#local-events">Things to do</a>
   </nav>
   ```
   The Overview/News/Weather/Events sub-tabs go away; they were all on this one page.
3. Phone ribbon: kicker `News · closer to home`; the select becomes the News tabs with Kansas City current:
   ```razor
   <PhoneRibbon Title="Kansas City" Kicker="News · closer to home">
       <SectionSelect Id="news-section-m" Label="Section" Choices="newsTabs" Current="news/kansas-city" AlwaysSelect="true" />
   </PhoneRibbon>
   ```
   The `?section=events` phone view keeps working; only its links change from `local?…` to `news/kansas-city?…`.
4. `<PageTitle>` becomes `Kansas City · News · myNewspaper`.

Add to `broadsheet.css` (desktop only):

```css
.pub .lc-jump { display: flex; gap: 18px; margin: -40px 0 18px; justify-content: flex-end; font-size: 14px; font-weight: 700; }
.pub .lc-jump a { color: var(--accent); min-height: 44px; display: flex; align-items: center; }
@media (max-width: 743px) { .pub .lc-jump { display: none; } }
```

---

## Phase 3 — The News front (`Components/NewsFront.razor`)

Same shape as `SportsFront.razor`: injects `IDbContextFactory<WebScraperContext>`, `FeedDates`, `ProfileContext`, and
renders a desktop block (`mx-desktop`) and a phone block (`mx-phone`) from one set of queries.

### 3.1 Data (one `OnInitializedAsync`, all headlines-hub only)

| Module | Query | Notes |
|---|---|---|
| Lead + Top headlines (6) | `FeedQuery.GetHomeNewsAsync(db, 7, hub: "headlines")` | First item is the lead; the rest are Top headlines |
| The day so far (10) | `FeedQuery.GetFeedAsync(db, null, null, null, take: 10, headlinesOnly: true)` | Time, title, source, category dot |
| Kansas City (3) | `GetLatestByCategoryAsync` for the Kansas City News category | Plus the weather line (3.3) |
| Opinion (2) | Same call, Opinion category | Serif italic titles |
| Technology / Video Games / Linux (3 each) | Same call, one round trip | The three desk columns |
| In pictures (5) | new `FeedQuery.GetPhotosAsync(db, take: 5)` below | Uses the article images from the last release |
| Since your last visit | Phase 4 | Hidden for Guest |
| Your topics box | Phase 5 | Hidden for Guest |

```csharp
/// <summary>The newest headlines-hub articles with a usable image, one per source, for the In pictures strip.</summary>
public static async Task<IReadOnlyList<FeedItem>> GetPhotosAsync(WebScraperContext db, int take = 5)
{
    var since = DateTime.UtcNow.AddHours(-36);
    var rows = await Visible(db)
        .Where(p => p.ImageUrl != null && (p.Published ?? p.ScrapedAt) >= since
            && (p.Source.SourceCategory == null || p.Source.SourceCategory.NewsHub == "headlines"))
        .OrderByDescending(p => p.Published ?? p.ScrapedAt).ThenByDescending(p => p.Id)
        .Take(take * 6)
        .Select(ToFeedItem)
        .ToListAsync();
    return rows.Where(r => SafeHref(r.ImageUrl) is not null).DistinctBy(r => r.SourceLabel).Take(take).ToList();
}
```

### 3.2 Desktop layout (reuse the Sports front classes)

```
.ef .nf                                   (new modifier class, like .spf)
├─ section.nf-brief       → 4 tiles: Since your last visit · Kansas City now · Election Day · (Markets: later)
├─ .spf-cols
│  ├─ section.spf-upcoming  → "The day so far" (reuse .spf-agate rows; the category dot replaces the sport tag)
│  ├─ .ef-main              → .ef-lead + "Top headlines" (.ef-headlines with Share + Save)
│  └─ aside.ef-side         → Kansas City (weather line + 3) · Opinion (2) · Your topics box
├─ section.nf-pictures    → 5 photos with caption + source
└─ .nf-desks              → 3 columns: Technology · Linux · Video Games, "More →" to each tab
```

New CSS in `broadsheet.css` (desktop) is limited to `.nf-brief` (4-column grid of tiles with a 4px coloured top
border), `.nf-pictures` (5-column grid, 150px images, `object-fit: cover`) and `.nf-desks` (3-column grid). Everything
else reuses `.ef-*` and `.spf-*`.

### 3.3 Briefing tiles

| Tile | Source | Rule |
|---|---|---|
| Since your last visit | Phase 4 | Hidden for Guest and on a profile's first visit |
| Kansas City now | The existing client-side weather script (`data-weather-*`). Add `data-weather-now` to the tile and fill temperature + short forecast from the first period, the way the Local page fills `data-weather-feature` | Links to `news/kansas-city#local-weather`; shows "Forecast" with no numbers until the script runs |
| Election Day | `appsettings.json` → `"News": { "ElectionDay": "2026-11-03" }` | Shows "N days" → "Today" → hidden from the day after. Links to `news/all?q=election`. Neutral wording only |
| Markets | — | Not built. Needs a quote source; leave the slot out |

### 3.4 Phone (`mx-phone`)

In order, all from `mobile-refresh-handoff.md` pieces:

1. `PhoneRibbon` — kicker `{date} · {n} stories · {m} sources`, title **News**, Section select = the News tabs.
2. Briefing as three `.mx-tile`s on white (Since last visit, Kansas City, Election Day).
3. `.mx-block` lead with Save and Share.
4. `.mx-top` Top headlines (3) — **each row has Share and Save** (the mobile handoff's top list had Save only; add
   `<ShareButton>` before `<SaveButton>` and change the grid to `36px minmax(0,1fr) 40px 40px`). Apply the same change
   to every `.mx-top` list on the site.
5. `PhoneTimeline` "The day so far" (3 newest).
6. `FoldGroup "More News"`: Kansas City (open) · Opinion · Election 2026 (until it's hidden) · Technology · Linux ·
   Video Games · In pictures.

---

## Phase 4 — "Since your last visit"

`Profiles.LastReadAt` is refreshed every minute while someone reads, so it can't answer "what's new since I was last
here". Add a second column that only moves when a new visit starts:

```sql
-- db/023_news_section.sql (part 1 of 2)
IF COL_LENGTH(N'dbo.MyDailyFeedProfiles', N'PreviousVisitAt') IS NULL
    ALTER TABLE dbo.MyDailyFeedProfiles ADD PreviousVisitAt datetime2 NULL;
```

In `ProfileStore.TouchAsync`, before refreshing `LastReadAt`: if the old `LastReadAt` is more than **30 minutes** ago,
copy it into `PreviousVisitAt`. That makes a "visit" any return after a 30-minute gap.

```csharp
if (profile is not null && (profile.LastReadAt is null || profile.LastReadAt < stale))
{
    var rows = db.Profiles.Where(p => p.Id == profile.Id);
    if (profile.LastReadAt is { } last && last < nowUtc.AddMinutes(-30))
        await rows.ExecuteUpdateAsync(s => s.SetProperty(p => p.LastReadAt, nowUtc).SetProperty(p => p.PreviousVisitAt, last), ct);
    else
        await rows.ExecuteUpdateAsync(s => s.SetProperty(p => p.LastReadAt, nowUtc), ct);
}
```

The tile counts headlines-hub articles with `(Published ?? ScrapedAt) > PreviousVisitAt`, and how many of those are in
Kansas City News. Its link goes to `news/all?after=…` — or simply `news/all`, which is newest first anyway.

---

## Phase 5 — Your topics

### 5.1 Table

```sql
-- db/023_news_section.sql (part 2 of 2)
IF OBJECT_ID(N'dbo.MyDailyFeedProfileTopics', N'U') IS NULL
    CREATE TABLE dbo.MyDailyFeedProfileTopics (
        Id            int IDENTITY(1,1) NOT NULL CONSTRAINT PK_MyDailyFeedProfileTopics PRIMARY KEY,
        ProfileId     int            NOT NULL,
        Terms         nvarchar(300)  NOT NULL,   -- "Chiefs stadium, Arrowhead": comma-separated alternatives
        ExceptTerms   nvarchar(200)  NULL,       -- "Chiefs": comma-separated
        Hub           nvarchar(16)   NULL,       -- NULL = all sections; else headlines | sports | entertainment
        CreatedAt     datetime2      NOT NULL CONSTRAINT DF_MyDailyFeedProfileTopics_Created DEFAULT SYSUTCDATETIME(),
        LastViewedAt  datetime2      NULL,
        CONSTRAINT FK_MyDailyFeedProfileTopics_Profile FOREIGN KEY (ProfileId)
            REFERENCES dbo.MyDailyFeedProfiles(Id) ON DELETE CASCADE,
        CONSTRAINT CK_MyDailyFeedProfileTopics_Hub CHECK (Hub IS NULL OR Hub IN (N'headlines', N'sports', N'entertainment'))
    );
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_MyDailyFeedProfileTopics_Profile')
    CREATE INDEX IX_MyDailyFeedProfileTopics_Profile ON dbo.MyDailyFeedProfileTopics (ProfileId);
```

EF model `ProfileTopic` and `DbSet<ProfileTopic> ProfileTopics`, mapped like `ProfileTvFollows`.

### 5.2 Rules

| Rule | Value |
|---|---|
| Topics per profile | 20 |
| Alternatives per topic | up to 5, each 2–60 characters, split on commas |
| Except words | up to 5 |
| Matching | Title or Description; case-insensitive; **whole words** (`\b…\b`); a multi-word alternative must appear as written |
| Window | Preview: past 7 days. Topic page: past 30 days, 20 per page |
| New | `(Published ?? ScrapedAt) > LastViewedAt` (or `CreatedAt` if never viewed) |
| Guest | Can't follow; the box says "Open your edition to follow topics" |
| Admin | No page lists topics. The Profiles admin may show "Topics: 4" as a number only |

### 5.3 Matching (`Profiles/TopicQuery.cs`)

SQL Server's default collation already compares case-insensitively, so `Contains` narrows candidates in SQL. The
whole-word and Except checks run in C# on that small set.

```csharp
public sealed record TopicMatch(FeedItem Item, string Highlight);
public sealed record TopicPreview(int Total, int[] PerDay, string Verdict, IReadOnlyList<FeedItem> Latest, int HiddenByExcept);

public static class TopicQuery
{
    public const int Limit = 20;
    public static string[] Split(string? text, int max) =>
        (text ?? "").Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)
            .Where(t => t.Length is >= 2 and <= 60).Distinct(StringComparer.OrdinalIgnoreCase).Take(max).ToArray();

    private static Regex Words(IEnumerable<string> terms) =>
        new(@"\b(" + string.Join("|", terms.Select(Regex.Escape)) + @")\b", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

    /// <summary>Matching articles, newest first. Candidates come from SQL; whole-word and Except run here.</summary>
    public static async Task<(List<FeedItem> Items, int HiddenByExcept)> MatchAsync(
        WebScraperContext db, string terms, string? except, string? hub, DateTime since, int cap = 2000, CancellationToken ct = default)
    {
        var include = Split(terms, 5);
        if (include.Length == 0) return ([], 0);
        var exclude = Split(except, 5);

        var query = FeedQuery.VisibleSince(db, since, hub);   // new: Visible(db) + date + optional hub filter
        var candidates = new Dictionary<int, FeedItem>();
        foreach (var term in include)                         // ≤ 5 small queries; avoids building an OR expression
            foreach (var item in await query.Where(p => p.Title.Contains(term) || (p.Description != null && p.Description.Contains(term)))
                         .OrderByDescending(p => p.Published ?? p.ScrapedAt).Take(cap).Select(FeedQuery.ToFeedItem).ToListAsync(ct))
                candidates[item.Id] = item;

        var yes = Words(include);
        var no = exclude.Length > 0 ? Words(exclude) : null;
        var matched = candidates.Values.Where(i => yes.IsMatch(i.Title) || yes.IsMatch(i.Description ?? "")).ToList();
        var kept = no is null ? matched : matched.Where(i => !no.IsMatch(i.Title) && !no.IsMatch(i.Description ?? "")).ToList();
        return (kept.OrderByDescending(i => i.Date).ToList(), matched.Count - kept.Count);
    }

    public static string VerdictFor(int perWeek) => perWeek switch
    {
        0 => "none",          // grey: "No matches yet" + spelling tip
        > 100 => "broad",     // amber: "Very broad" + suggestions; still followable
        _ => "good",          // green
    };
}
```

`FeedQuery.ToFeedItem` is already an expression but `private`; make it `internal`, and add
`internal static IQueryable<Page> VisibleSince(WebScraperContext db, DateTime since, string? hub)` next to `Visible`.

### 5.4 Endpoints (`Endpoints/MeEndpoints.cs`, the existing `/me` group)

| Method + path | Does | Notes |
|---|---|---|
| `GET /me/topics/preview?terms=&except=&hub=` | JSON `TopicPreview` | Called by the sheet after a 500 ms pause in typing. No-store. Returns 401 for Guest |
| `POST /me/topics` (form) | Creates a topic | Validates with `Split`; 409 at 20; redirects to `news/topics/{id}` or `returnUrl` |
| `POST /me/topics/{id}` (form) | Edits terms / except / hub | Must match `ProfileId == reader.Id`, else 404 |
| `POST /me/topics/{id}/read` | Sets `LastViewedAt = now` | "Mark as read" |
| `POST /me/topics/{id}/delete` | Stop following | |

Every query filters on `ProfileId == reader.Id`. Like `FollowQuery`, there is deliberately no method that reads
another profile's topics.

### 5.5 Pages and pieces

- **Follow sheet / dialog** (`Components/Topics/TopicFollow.razor`, `@rendermode InteractiveServer` so the preview
  updates live; the form still posts to `/me/topics` without a circuit):
  fields **Words or phrase**, **Except (optional)**, **Look in** (All sections / News / Sports / Entertainment);
  the preview card (verdict, a 7-bar chart with an `aria-label` listing the counts, 3 latest matches with the
  terms in `<mark>`, "N stories hidden by Except: …"); **Follow "…"**; "3 of 20 topics used".
  Phones: a bottom sheet (`<dialog>` styled full-width at the bottom). Desktop: a centred two-column dialog.
- **Your topics box** on the News front: each topic with "N new" or "none today", and **Follow a topic →**.
- **Topic page** (`Components/Pages/Topic.razor`, `@page "/news/topics/{Id:int}"`): ribbon with a **Topic** select to
  switch topics; dark block "N new stories since {LastViewedAt}" with **Mark as read**; **New** list then **Earlier
  this week** (20 at a time); every row has Share and Save; **Edit topic** and **Stop following** at the bottom.
  Opening the page does *not* mark things read; only the button does, so the count survives a quick peek.
- The front's "Since your last visit" count does not add topic matches; topics have their own "N new".

---

## Later (not in this release)

| Idea | Why it waits |
|---|---|
| Markets tile | Needs a quote source (and its terms of use) |
| How this story developed | Needs story grouping across sources (title similarity), which the site doesn't do yet |
| Weather alerts | The NWS alerts endpoint is free, but alerts need their own banner rules on Home and News |

---

## Verify (per AGENTS.md)

1. **Routes:** `/news` shows the front; `/news/all`, `/news/all?category={id}`, `/news/kansas-city` and
   `/news/topics/{id}` load. `/local`, `/news?category=…`, `/news?q=…` answer 301 to the new paths with the query intact.
2. **Navigation:** 6 top links; News is current on every `/news/*` page; the phone menu matches.
3. **Tabs:** desktop shows the underlined News tabs; phones show the Section dropdown (never segments). Adding a
   headlines-hub category in admin adds a tab.
4. **Front:** each module renders with real data and hides cleanly when empty (no In pictures without photos; no
   Election tile after Nov 4; no Since-last-visit for Guest).
5. **Kansas City:** forecast, local news and things to do work as on `/local` today, including "Use my location" and
   the phone events view; the jump links scroll to their sections.
6. **Topics:** preview updates after typing stops; the three verdicts appear for a broad term, a normal term and a
   nonsense term; Except hides matches and says how many; 20-topic limit; another profile can't open
   `/news/topics/{id}` that isn't theirs (404); no admin page shows topic text.
7. **Phones** at 320, 390 and 430px, light and dark, two other schemes: ribbon select fits; Share and Save on every
   top-list row; the sheet scrolls when the keyboard is up; 44px targets.
8. **Desktop / iPad portrait** (744, 1024, 1440): the three columns and the desk grid wrap without sideways scrolling.

## Suggested order of releases

1. Phase 1 + Phase 2: routes, redirects, nav, tabs, Kansas City move (no new data).
2. Phase 3: the News front without the personal tiles.
3. Phase 4: `db/023` part 1 and "Since your last visit".
4. Phase 5: `db/023` part 2 and Your topics.
