# Tech, part 5: the weekly roundup (`/tech/week`)

**Save as:** `docs/tech/tech-05-weekly.md` · **Needs:** part 2 (Most covered). Its "Released this week" box appears
once part 4 is in.

**Mockup:** **Tech section** page → "Tech — weekly roundup (This week in Linux)".

**Result:** a Sunday roundup per desk, plus "All of Tech", built entirely from the week's stored data. No scheduled
job and no new tables: the page computes the week on request and caches it.

## The week

- **The latest roundup** covers the last **complete Sunday–Saturday week** in America/Chicago. On Sunday November 1 it
  shows **October 25–31**.
- It "publishes" at 6:00 AM Sunday Central. Before then, the previous week is still the latest.
- **Address:** `/tech/week` (All of Tech) and `/tech/week/{desk}` (`technology`, `video-games`, `linux`). An optional
  `?week=2026-10-25` (that Sunday's date) shows an earlier week. Weeks older than 12 are a 404.
- **Caching:** a finished week never changes, so cache each `(desk, week)` result for **24 hours**
  (`IMemoryCache`), keyed by desk and week start.

## Data (`TechQuery.WeekAsync`)

```csharp
public sealed record WeekSummary(
    DateOnly Start, DateOnly End, string Desk,
    int Stories, int Sources,                    // tile 1 ("84 stories from 6 sources")
    int Releases,                                // tile 2 (0 and hidden until part 4)
    string? BusiestSource, int BusiestSourceCount,
    DayOfWeek BusiestDay, int BusiestDayCount,
    IReadOnlyList<CoveredStory> Biggest,         // top 5
    IReadOnlyList<(DateOnly Day, int Count, FeedItem? Top)> Days,   // 7 entries
    IReadOnlyList<TrackedRelease> ReleasedThisWeek,
    IReadOnlyList<(string Topic, int Count, string Hub)> Topics);    // the open profile's topics, this week's matches
```

- **Biggest stories:** part 2's Most covered function over the **7-day** window, for the desk (or all Tech),
  `minSources = 2`, top **5**, ranked by distinct sources and then by headline count.
- **Day by day:** Tech headlines per Chicago day. `Top` is that day's most covered story, falling back to the day's
  first headline.
- **Released this week:** `TechReleases` with `ReleasedAt` inside the week (part 4's query with explicit dates).
  Empty, or tables missing: hide the box and the tile.
- **Your topics this week:** `TopicQuery.MatchAsync` per topic over the week, hub `tech` or all. Only for the open
  profile; hidden with profiles off. **Not cached** with the rest, because it's per profile.

## Layout

**Desktop:**

1. **Heading:**
   - eyebrow "WEEKLY ROUNDUP · SUNDAY, NOVEMBER 1" in the desk colour (Tech purple for All of Tech);
   - H1 "This week in Linux" ("This week in Tech" for all);
   - a description with the date range;
   - the Tech tabs, with the desk's tab current (none for All of Tech).
2. **Desk pills:** All of Tech · Technology · Video Games · Linux, each with that week's story count. They're links to
   `/tech/week[/desk]` with the same `week`.
3. Main + 340px rail.
   - **Main:**
     - **four tiles:** Stories, Releases, Busiest source, Busiest day, with the `.nf-tile` look in the desk colour;
     - **"The week's biggest stories":** ranked 1–5 with a large serif number in the desk colour, the kicker, the
       title (the first one larger), and the source badges with "N sources · names", reusing part 2's
       `.tc-covered` markup;
     - **"Day by day":** seven cards (Mon…Sun as the mockup labels them; order them Sun→Sat to match the week), each
       with a count, a thin bar scaled to the busiest day, and the day's top headline.
   - **Rail:**
     - **Released this week** (project, source, version, date);
     - **Your topics this week** ("Steam Deck · 9 stories");
     - **Earlier roundups:** the three previous weeks with their story counts, linking with `?week=`.

**Phone:** the ribbon in the desk colour, with "This week in Linux" and the date range as kicker, then:

- desk pills you swipe;
- tiles 2 × 2;
- the biggest stories (numbered);
- day by day as a 7-row list (day · count · top headline);
- Released this week;
- Your topics;
- Earlier roundups.

## Links in

- **Each desk page:** a "This week in <Desk> →" link at the bottom of the rail.
- **Tech front:** a line under the desks, "This week in Tech: <N> stories, <M> releases →", shown Sunday to Tuesday.
- **Home:** nothing.

## Verify

1. **The week boundary:** on a Saturday it shows the previous week, and Sunday after 6:00 AM Central it shows the week
   just finished. `?week=` with an older Sunday works, and beyond 12 weeks is a 404.
2. **Numbers add up:**
   - The stories tile equals the sum of the seven day counts.
   - The busiest day matches the tallest bar.
   - The busiest source matches a count query.
3. **The biggest stories** look right against that week's real headlines. The top one is genuinely the most widely
   covered.
4. **Before part 4:** the Releases tile and Released this week are hidden.
5. **Profiles off:** Your topics is hidden, and nothing errors.
6. **Mobile** (320, 390, 430, light and dark): the pills swipe, the tiles are 2 × 2, the numbered list and day rows
   don't overflow, and touch targets are at least 44px.
7. **Desktop** (1280, 1440) and **iPad** (744, 820) if the iPad guides are done.

That completes the Tech section.
