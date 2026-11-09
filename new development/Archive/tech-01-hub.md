# Tech, part 1: the Tech hub (navigation, routes, categories)

**Save as:** `docs/tech/tech-01-hub.md` · **Read first:** `tech-00-overview.md`.

**Mockups:** **Tech section** page → the plan board.

**Result:**

- **Tech** is in the main navigation.
- `/tech` exists (an empty shell until part 2) and `/tech/news` lists Tech headlines.
- Technology, Video Games and Linux move from News to Tech.
- Admin and Your topics know about the new hub.

No new feeds are needed.

## 1. Database: `db/024_tech_hub.sql`

The same style as `019` and `023`: additive and repeatable. Run it after a backup and **before** deploying this part.

```sql
-- The Tech section (docs/tech/). Additive and repeatable; run after 019 and 023.
-- Part 1: allow NewsHub = 'tech' and move the three tech categories there (only if they're still on 'headlines',
--         so a later Admin change is never overwritten).
-- Part 2: allow Your topics to be limited to Tech.
SET XACT_ABORT ON;
BEGIN TRANSACTION;
IF OBJECT_ID(N'dbo.SourceCategories', N'U') IS NULL OR COL_LENGTH(N'dbo.SourceCategories', N'NewsHub') IS NULL
    THROW 50000, 'Run 019_news_category_hubs.sql first.', 1;
IF OBJECT_ID(N'dbo.MyDailyFeedProfileTopics', N'U') IS NULL
    THROW 50000, 'Run 023_news_section.sql first.', 1;

-- Part 1 of 2
IF EXISTS (SELECT 1 FROM sys.check_constraints WHERE name = N'CK_SourceCategories_NewsHub'
           AND definition NOT LIKE N'%tech%')
BEGIN
    ALTER TABLE dbo.SourceCategories DROP CONSTRAINT CK_SourceCategories_NewsHub;
    ALTER TABLE dbo.SourceCategories ADD CONSTRAINT CK_SourceCategories_NewsHub
        CHECK (NewsHub IN (N'headlines', N'sports', N'entertainment', N'tech'));
END;
UPDATE dbo.SourceCategories SET NewsHub = N'tech'
WHERE NewsHub = N'headlines' AND Category_Name IN (N'Technology', N'Video Games', N'Linux');

-- Part 2 of 2
IF EXISTS (SELECT 1 FROM sys.check_constraints WHERE name = N'CK_MyDailyFeedProfileTopics_Hub'
           AND definition NOT LIKE N'%tech%')
BEGIN
    ALTER TABLE dbo.MyDailyFeedProfileTopics DROP CONSTRAINT CK_MyDailyFeedProfileTopics_Hub;
    ALTER TABLE dbo.MyDailyFeedProfileTopics ADD CONSTRAINT CK_MyDailyFeedProfileTopics_Hub
        CHECK (Hub IS NULL OR Hub IN (N'headlines', N'sports', N'entertainment', N'tech'));
END;
COMMIT TRANSACTION;
```

Check the three category names against the live `SourceCategories` table first. They appear as "Technology", "Video
Games" and "Linux" in `SectionChoices.NewsOrder`. Add the migration to the table in `DEPLOY.md`.

**Rollback:** set the three categories back to `headlines`. The wider constraints are harmless to keep.

## 2. Shared code (`myNewspaper.Shared`)

**`Data/NewsSections.cs`:** add Tech the way Sports and Entertainment are done.

```csharp
public const string Technology = "Technology";
public const string VideoGames = "Video Games";
public const string Linux = "Linux";
public static readonly string[] TechCategories = [Technology, VideoGames, Linux];

// RouteFor(string? name): add
//     Technology => "tech/technology",
//     VideoGames => "tech/video-games",
//     Linux      => "tech/linux",
// ValidHub: add "tech"
// DefaultHub: TechCategories.Contains(name) ? "tech" : …
// HubForView: "tech-hub-news" or "tech-news" => "tech"
// Order: category.Hub == "tech" ? TechCategories : …
// LabelFor: Technology / Video Games / Linux keep their names
```

**`Profiles/TopicQuery.cs`:** `Hubs = ["headlines", "sports", "entertainment", "tech"]`.

**`Profiles/SiteDefaults.cs`:** wherever it lists the Home sections (it references `entertainment-news`), add
`tech-news` after `entertainment-news`, **on** by default, the same as the others.

Search the solution for `"entertainment"` and `EntertainmentCategories` to catch any other hub switch. The hits
today are in `FeedEndpoints.cs`, `FeedQuery.cs`, `NewsSections.cs`, `TopicQuery.cs` and the components below.

## 3. Navigation and routes (`myNewspaper.Web`)

**`Components/SectionChoices.cs`:**

```csharp
public static readonly IReadOnlyList<NavigationChoice> Tech =
[
    new("tech", "Tech home"), new("tech/news", "All headlines"), new("tech/technology", "Technology"),
    new("tech/video-games", "Video Games"), new("tech/linux", "Linux"), new("videos?categories=tech,video-games", "Videos"),
];
// For(): "tech" => Tech,
```

Remove "Technology", "Video Games" and "Linux" from `NewsOrder`. After the migration they're no longer
`headlines`-hub categories, so News's tabs drop them automatically. The array entries just become unused.

**`Components/Layout/PublicLayout.razor`:**

- Add `onTechHub` / `onTechSection` like the Entertainment pair (paths `tech` and `tech/…`).
- **Main nav:** add `<a href="tech" aria-current="…">Tech</a>` **between Entertainment and Where to watch**.
- **Mobile nav:** add a Tech group after Entertainment with the six `SectionChoices.Tech` links, in the same markup as
  the Entertainment group (`mobile-nav-group` / `mobile-subnav`).

Check the header at 1280 and 1440 with seven items. The 1024–1279 range already folds the nav into the dropdown. The
iPad header row (docs/ipad) fits seven items at 744.

**`Components/PageHeading.razor`:** in the label switch add `"tech" => "Tech"`, and update the doc comment.
**`Components/Phone/PhoneRibbon.razor`:** the `Section` doc comment and any switch: add `"tech"`.

**`Components/Pages/SectionHub.razor`:** add `@page "/tech"` and a branch:

```razor
} else if (Section == "tech") {
    <div class="mx-desktop tab-desktop"><PageHeading Section="tech" Title="Tech" Eyebrow="Gadgets, games and Linux, every source" Description="" /></div>
    <TechFront />
}
```

and `"tech" => "Tech"` in `Title`. Part 2 creates `TechFront`. For now, create
`Components/TechFront.razor` containing only `<p class="ef-empty">Coming in part 2.</p>`, so the route works.

**`Components/Pages/News.razor`** (All headlines for each hub). Generalise the two booleans into one hub:

```csharp
private string? SectionHubName => CurrentPath.Split('/')[0].ToLowerInvariant() switch
{
    "sports" => "sports", "entertainment" => "entertainment", "tech" => "tech", _ => null
};
private bool SectionNews => SectionHubName is not null;
private string Hub => SectionHubName ?? "headlines";
private static string HubLabel(string hub) => hub switch { "sports" => "Sports", "entertainment" => "Entertainment", "tech" => "Tech", _ => "News" };
```

Replace every `EntertainmentSection ? … : …` with `Hub` / `HubLabel(Hub)` / `SectionChoices.For(Hub)`. There are
about six places: the phone ribbon kicker, `SectionSelect` choices, `PageHeading` Section / SectionCurrent / Eyebrow,
`Title`, and the `hn-cats` aria-label (`"Desk"` for tech). Then add the routes:

```razor
@page "/tech/news"
@page "/tech/news/{Topic}"
```

`/tech/news` uses the headline-cards layout, like Sports and Entertainment headlines. Part 3 adjusts its Jump to box.

`FeedView` becomes `tech-hub-news` for Tech, which matches `NewsSections.HubForView` above.

**`Endpoints/FeedEndpoints.cs`** (around line 53): the hub lookup takes the hub string, so check that it accepts
`tech`. If it validates against a list, use `NewsSections.ValidHub`.

## 4. Admin and Your topics

- **`myNewspaper.Admin/.../CategoryList.razor`:** the hub `<select>` for a category gets a **Tech** option. The
  sentences in `RequestList.razor` and `SourceEdit.razor` already capitalise the hub name, so they read "Tech hub".
- **`Components/Pages/Topics/TopicFollow.razor`:** add `<option value="tech">Tech only</option>` after
  Entertainment.
- **`Components/Pages/Topics/Topic.razor`** (line ~126): `"tech" => "Tech only"`.

## 5. News keeps a door to Tech; Home gets a Tech shelf

**News front (`Components/NewsFront.razor`):** add a small **From Tech** shelf: four latest Tech headlines with a
"Tech →" link. Put it with the desks (`.nf-desks`), styled like a desk, and use
`FeedQuery.GetHomeNewsAsync(db, 4, "tech")`. Nobody loses the stories that used to be on News.

**Home (`Components/Pages/Home.razor`):** add a shelf after Entertainment headlines in the More from today main
column:

```razor
<HomeHeadlineShelf SectionKey="tech-news" Title="Tech headlines" HeadingId="h-tech-headlines" Href="tech"
                   CssClass="d-tech-headlines" Stories="@techStories" />
```

Load `techStories = await FeedQuery.GetHomeNewsAsync(db, HomeShelfCount, "tech");` next to `entertainmentStories`.
Add the matching phone shelf in the phone Home where Entertainment headlines appears. In `wwwroot/personal.js`, add
`tech-news` wherever `entertainment-news` is listed, so the section can be hidden and reordered in Settings.

## 6. Verify

1. **Migration:** run it twice; the second run changes nothing. The three categories show hub **tech** in Admin →
   Categories.
2. **News:**
   - The tabs no longer show Technology / Video Games / Linux.
   - News All headlines doesn't include their stories.
   - The From Tech shelf shows four.
3. **`/tech/news`:**
   - Tech headlines only, with desk pills (Technology, Video Games, Linux) that filter.
   - The eyebrow reads "Tech · Every story, every source".
   - Phones get the ribbon with the Tech section menu.
4. **`/tech`:** the heading, the tabs, and the placeholder line.
5. **Nav:** Tech sits between Entertainment and Where to watch and is underlined on every Tech page. The phone Menu
   has a Tech group.
6. **Your topics:** follow a topic with "Tech only", and it only matches Tech stories.
7. **Home:** the Tech headlines shelf shows, and hiding it in Settings works.
8. **Mobile** (`AGENTS.md`): 320, 390 and 430px, light and dark, for `/tech`, `/tech/news`, News and Home. Also
   check the iPad header at 744 and 820, and desktop at 1024, 1280 and 1440.

Then do `tech-02-front.md`.
