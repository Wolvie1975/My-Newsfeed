# Tech, part 2: the Tech front (`/tech`)

**Save as:** `docs/tech/tech-02-front.md` · **Needs:** part 1.

**Mockups:** **Tech section** page → "Tech front — desktop" and "Tech front — phone".

**Result:** the Tech front with everything that runs on existing data. **Just released** and **Service status**
need the scraper's tables. They're built in part 4, and this part leaves slots for them that render nothing.

## Layout

**Desktop** (reuses the News front's classes, so most styling already exists):

1. Heading and Tech tabs (from part 1).
2. *(Part 4: the service status strip.)*
3. **The briefing:** four tiles (`.nf-brief__tiles` / `.nf-tile`):
   - Technology, Video Games and Linux, each showing **stories today**;
   - **Tech videos**, showing videos published today by your YouTube channels in the Tech and Video Games categories.
4. The **`.spf-cols`** frame:
   - **The day so far** (`.spf-upcoming.nf-day`): newest first, every tech source, the time in the desk colour.
   - **Main:**
     - the lead (`.ef-lead`);
     - then **Most covered** (new, section 2);
     - then **Latest headlines** (2 × 2, `.ef-headlines`).
   - **Side** (`.ef-side.spf-side`):
     - *(Part 4: Just released.)*
     - **Your topics** (section 3);
     - **Tech videos** (three rows).
5. **Desks** (`.nf-desks`): three columns, Technology | Video Games | Linux, with four headlines each and an "All
   <desk> →" link to the desk page.

**Phone** (`.mx-phone`):

- the ribbon in Tech purple with the Tech section menu (`PhoneRibbon Section="tech"`);
- *(part 4: the status line)*;
- tiles 2 × 2;
- the lead;
- Top tech (numbered, five);
- Most covered (one item);
- *(part 4: Just released)*;
- Your topics;
- desks as folds, with **Linux open** by default;
- Tech videos as a row you swipe.

Copy the structure of `NewsFront.razor`: the same `mx-desktop` / `mx-phone` halves, `tab-desktop` / `tab-phone`
classes (docs/ipad), and data loading in `OnInitializedAsync` with the same `DataFailures.IsReadFailure` handling.

## 1. Data (`myNewspaper.Shared/Data/TechQuery.cs`, new)

```csharp
public sealed record TechBriefing(int Technology, int VideoGames, int Linux, int Videos);
public sealed record CoveredStory(FeedItem Lead, IReadOnlyList<FeedItem> Others, int Sources);   // Sources = distinct sources incl. Lead

public static class TechQuery
{
    public static readonly string[] VideoCategories = ["Tech", "Video Games"];   // YouTube category names (check VideosQuery's list)

    public static Task<TechBriefing> BriefingAsync(WebScraperContext db, FeedDates dates, CancellationToken ct = default);
    public static Task<IReadOnlyList<FeedItem>> LatestAsync(WebScraperContext db, int take, string? category = null, CancellationToken ct = default);
    public static Task<IReadOnlyList<CoveredStory>> MostCoveredAsync(WebScraperContext db, TimeSpan window, int minSources, int take, string? category = null, CancellationToken ct = default);
    public static Task<IReadOnlyList<VideoItem>> VideosAsync(WebScraperContext db, int take, CancellationToken ct = default);
}
```

- **"Today"** is the America/Chicago day, using `FeedDates`, as the rest of the site does.
- **`LatestAsync`** is `FeedQuery`'s hub query with `hub = "tech"` (plus the category when given). Reuse it; don't
  copy it.
- **`VideosAsync`** uses `VideosQuery` filtered to the channels whose category is one of `VideoCategories`. If
  `VideosQuery` only takes one category, add an overload that takes a list. Part 1's Videos tab
  (`videos?categories=tech,video-games`) needs the same thing. Parse `categories` as comma-separated slugs in
  `Videos.razor`.

## 2. Most covered

**What it shows:** a story that several of your sources covered in the last 24 hours. The story is shown once, with
stacked source badges and "4 sources · Phoronix, 9to5Linux, XDA, Linuxiac". The front shows up to two (one on
phones).

**How** (runs in memory over at most a day of Tech headlines, a few hundred rows):

1. Take Tech-hub headlines from the window. For each, make a **key set** from the title:
   - lower-case it and strip punctuation;
   - drop stop words (a small list: the, a, an, and, of, to, in, for, on, with, is, are, how, why, what, your, you,
     this, that, it, its, new, now, after, over, from, by, at, as, be, will);
   - keep words of three or more letters, **and** any token with a digit ("6.18", "44", "rtx5090");
   - normalise simple plurals by trimming a final "s" on words longer than four letters.
2. Two headlines **match** when they're from **different sources** and their key sets have a Jaccard similarity of
   at least **0.34** (shared ÷ union), **or** they share two or more digit tokens plus one other word. That catches
   the same version release worded differently.
3. Group matches transitively (union-find). Keep groups with at least `minSources` distinct sources (**3** on the
   front, **2** on desk pages).
4. **Lead** of a group: the earliest headline from a source with a summary. Otherwise the earliest headline.
   **Order:** distinct sources (most first), then the newest headline.
5. Cache the result for **10 minutes** (`IMemoryCache`, key `tech-covered:{category}:{window}`).

Put this in `TechQuery` as a pure function over a list of `FeedItem`, plus a thin async wrapper, so it's easy to test.
**Add unit tests** in `tests/myNewspaper.Tests/TechCoveredTests.cs`:

- two near-identical titles from different sources group;
- the same source twice doesn't group;
- "Linux 6.18 released" and "Linux kernel 6.18 is out with …" group;
- unrelated titles that share one word don't group.

**Markup** (reuse `.ef-headlines` styles and add a badge row):

```razor
<li class="tc-covered">
    <p class="ef-kicker">@Desk(story.Lead)</p>
    <a class="ef-headlines__title" href="@Href(story.Lead)">@story.Lead.Title</a>
    <span class="tc-covered__sources">
        <span class="tc-badges" aria-hidden="true">@foreach (var s in story.SourceMarks.Take(5)) { <span class="tc-badge @PhoneColors.ForCategory(s.Category)">@s.Mark</span> }</span>
        <b>@story.Sources sources</b> · @string.Join(", ", story.SourceNames)
    </span>
</li>
```

Source marks use `FeedText.SourceMark` (one letter today). Two letters read better here ("9L"), so add a
`SourceMark(name, 2)` overload if you like. Badges overlap by 6px with a 2px ring in the page background, as in the
mockup.

## 3. Your topics on the front

Show the open profile's topics with **"N new"** since `TopicQuery.NewSince(topic)`, using
`TopicQuery.SummariesAsync`, and a **Follow a topic** link to `/news/topics/follow?hub=tech`. Make `TopicFollow`
pre-select the hub from the query string.

- Show topics whose hub is `tech` **or** empty (all sections).
- With profiles off (`ProfileGate`) or no topics, show the explainer and the Follow button, as the News front does.

## 4. Tiles, desks and the day so far

- **Tiles:** the colour edge and figure use `PhoneColors.ForCategory` (`mx-c-tech` for Technology). Video Games and
  Linux need their own classes: add `mx-c-games` (`--edge: var(--mx-business)`) and `mx-c-linux`
  (`--edge: var(--mx-local)`) in `mobile.css`. Map "Video Games" and "Linux" to them in `PhoneColors.ForCategory`,
  **before** the `n.Contains("game")` / `"linux"` → tech rule. Today's rule paints all three purple.
- **The day so far:** the News front's `.nf-agate` markup, with the source and desk after the headline.
- **Desks:** the News front's `.nf-desk` markup for each category, using `LatestAsync(db, 4, category)`.

## 5. Slots for part 4

Leave two empty components and render them where the layout shows them:

```razor
<TechStatusStrip />    @* part 4: under the tabs; renders nothing for now *@
<TechJustReleased />   @* part 4: top of the side column; renders nothing for now *@
```

## 6. Verify

1. **`/tech` desktop** (1280, 1440):
   - The tiles show today's counts, and Tech videos counts only Tech + Video Games channels.
   - The day so far, the lead and Latest are all Tech stories.
   - Most covered shows groups of 3+ sources, or is hidden on a quiet day.
   - Your topics shows N new and the link works.
   - Each desk shows four stories and its link goes to `/tech/<desk>`.
2. **Desk colours:** Technology is purple, Video Games amber and Linux teal, in the tiles, desks and kickers, in light
   and dark mode.
3. **Phone** (320, 390, 430, light and dark):
   - The ribbon menu, tiles 2 × 2, the numbered top five with Share and Save, and Linux's fold open.
   - Videos swipe. Touch targets are at least 44px.
4. **iPad** (744, 820), if the iPad guides are done: two columns, lead full width (as News at part 5).
5. **`dotnet test`:** the Most covered tests pass.

Then do `tech-03-desks.md`.

