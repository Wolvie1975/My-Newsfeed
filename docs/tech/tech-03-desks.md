# Tech, part 3: All headlines and the three desk pages

**Save as:** `docs/tech/tech-03-desks.md` · **Needs:** parts 1 and 2.

**Mockups:** **Tech section** page → "Tech — All headlines", "Technology desk", "Video Games desk", "Linux desk" and
"Linux desk, phone".

**Result:**

- `/tech/news` gets Tech's Jump to box;
- `/tech/technology`, `/tech/video-games` and `/tech/linux` exist, built on existing data.

The Linux **Release tracker** and the Technology desk's **What developers are reading** come in part 4. This part
leaves slots for them.

## 1. All headlines (`/tech/news`)

This is the headline-cards page from `docs/headline-cards-handoff.md`, which part 1 already routed. Two Tech-specific
changes:

- **Jump to** goes **by hour** (Tech has a busy "today", like News), not by day as Sports and Entertainment do. Use the
  same rule the guide uses for News: hours when today has enough stories.
- **"Today by desk"** (the category half of the box) lists Technology, Video Games and Linux with counts, each with a
  4px left edge in the desk colour. The label reads "Today by desk" for Tech, and "Today by category" stays for the
  other hubs.

Cards without a usable photo get the P8 frame. The P8 colour comes from `PhoneColors.ForCategory`, so part 2's
`mx-c-games` / `mx-c-linux` make those cards amber and teal.

## 2. One component for the three desks

Make `Components/Pages/TechDesk.razor` with three routes and a definition per desk:

```razor
@page "/tech/technology"
@page "/tech/video-games"
@page "/tech/linux"
```

```csharp
// myNewspaper.Shared/Data/TechDesks.cs
public sealed record DeskTopic(string Slug, string Label, string[] Words);
public sealed record DeskDefinition(string Slug, string Category, string Title, string Description, string ColorClass, IReadOnlyList<DeskTopic> Topics);

public static class TechDesks
{
    public static readonly DeskDefinition Technology = new("technology", NewsSections.Technology, "Technology",
        "Phones, PCs, AI and apps from Engadget, How-To Geek and XDA.", "mx-c-tech",
        [
            new("phones", "Phones", ["phone", "iphone", "pixel", "galaxy", "android", "ios", "smartphone", "foldable"]),
            new("pcs", "PCs & laptops", ["pc", "laptop", "windows", "macbook", "chromebook", "cpu", "gpu", "nvidia", "amd", "intel", "ryzen"]),
            new("ai", "AI", ["ai", "chatgpt", "openai", "claude", "gemini", "copilot", "llm"]),
            new("apps", "Apps & software", ["app", "apps", "update", "browser", "chrome", "firefox", "office", "software"]),
            new("smart-home", "Smart home", ["smart home", "alexa", "google home", "homekit", "matter", "thermostat", "doorbell"]),
            new("deals", "Deals", ["deal", "deals", "sale", "% off", "discount", "price drop"]),
        ]);
    public static readonly DeskDefinition VideoGames = new("video-games", NewsSections.VideoGames, "Video Games",
        "Platforms, news and deals from IGN, Polygon and GamingOnLinux.", "mx-c-games",
        [
            new("pc", "PC", ["pc", "steam", "epic games store", "gog"]),
            new("xbox", "Xbox", ["xbox", "game pass"]),
            new("playstation", "PlayStation", ["playstation", "ps5", "ps4", "psn"]),
            new("switch", "Switch", ["switch", "nintendo"]),
            new("steam-deck-linux", "Steam Deck & Linux", ["steam deck", "proton", "linux", "steamos"]),
        ]);
    public static readonly DeskDefinition Linux = new("linux", NewsSections.Linux, "Linux",
        "Kernel, distros, desktops and Linux gaming from Phoronix, 9to5Linux, Linuxiac, Linux Today and GamingOnLinux.", "mx-c-linux",
        [
            new("kernel", "Kernel", ["kernel", "linux 6.", "linux 7.", "torvalds", "rc1", "driver"]),
            new("distros", "Distros", ["fedora", "ubuntu", "debian", "arch", "mint", "opensuse", "pop!_os", "distro", "nixos"]),
            new("desktops", "Desktops", ["gnome", "kde", "plasma", "xfce", "cosmic", "wayland", "x11"]),
            new("gaming", "Gaming", ["proton", "steam", "steam deck", "gaming", "vulkan", "wine"]),
            new("hardware", "Hardware", ["benchmark", "cpu", "gpu", "amd", "intel", "nvidia", "arm", "risc-v"]),
        ]);
    public static readonly IReadOnlyList<DeskDefinition> All = [Technology, VideoGames, Linux];
    public static DeskDefinition? For(string slug) => All.FirstOrDefault(d => d.Slug == slug);

    /// <summary>Whole-word, case-insensitive match against the title (and summary when present).</summary>
    public static bool Matches(DeskTopic topic, string title, string? summary);
}
```

- **Matching:** whole words or phrases, case-insensitive, against the **title** (and summary when there is one). Use
  `\b` word boundaries for alphabetic words. A word with punctuation (`% off`, `pop!_os`) is a plain `Contains`. A
  story can match several topics.
- **The pill counts** are today's matches. A pill with **0** today still shows, dimmed. Selecting a pill filters the
  page (lead, list and grid) by `?topic=<slug>`. The pills are links, so they work without JavaScript.
- **The lists are a first draft.** They live in code so they're easy to adjust after a week of real headlines. Unit
  test `Matches`: whole word ("ai" matches "AI tools" but not "said"), and a phrase ("steam deck").

## 3. Desk page layout (all three)

**Desktop:**

1. Heading: the "TECH DESK" eyebrow in the desk colour, the desk name, the one-line description, then the Tech tabs.
2. **Topic pills** row with counts, plus "Topics match words in the headline · updated <time>" on the right.
3. Main column + 340px rail (the `.ef-cols` / `.ef-side` pattern):
   - **Lead:** large photo (P8 if none) with **four list stories** beside it.
   - The desk-specific block (below).
   - **More <desk>:** three-across cards (reuse the headline-cards `.hc-card`), then "All <desk> →" to
     `/tech/news?category=<id>`.
   - **Rail:** desk-specific boxes, then **By source**: today's count per source, with a thin bar in the desk colour.

| Desk | Desk-specific block (main column) | Rail boxes |
|---|---|---|
| Technology | **How-to & guides:** four cards of headlines that start with or contain "How to", "How I", "What is", "I tried", "I tested", "Best … for", "… explained" (case-insensitive), from the desk's sources, last 7 days, newest first. Each card gets a tag: How-to / Guide / Explainer / Tested | *(Part 4: What developers are reading)* · By source · **Tech videos** (YouTube Tech category, 3) · **Also in Tech** (one story each from the other two desks) |
| Video Games | none (the lead and list follow the pills) | **Deals & sales:** desk headlines matching the Deals words (`sale`, `deal`, `% off`, `discount`), last 3 days · **Gaming videos** (YouTube Video Games category, 4) · By source |
| Linux | *(Part 4: Release tracker, at the top of the main column)* | **Most covered** (part 2's function, `minSources = 2`, Linux only, one story) · **Linux gaming:** stories in Linux **or** Video Games that match the Linux desk's Gaming words, 3 · By source |

**Phone** (the Linux phone board shows the shape; all three use it):

- the ribbon in the **desk colour**, with the kicker "Tech · <date>", the desk name, "N stories today", and the Tech
  section menu set to this desk;
- topic pills in a row you swipe;
- *(Linux: the release tracker's top five, from part 4)*;
- the lead;
- the Latest list (Share and Save on each);
- the desk's rail boxes as folds below.

**iPad portrait** (744–1023), if the iPad guides are done: opt in with `tab-desktop` / `tab-phone`. Main and rail
become two equal columns under a full-width lead, like Entertainment in `docs/ipad/ipad-03-entertainment.md`.

## 4. Slots for part 4

```razor
<TechReleaseTracker Compact="@Phone" />    @* Linux desk only: top of main; renders nothing until part 4 *@
<TechDevReading />                         @* Technology desk only: top of the rail; renders nothing until part 4 *@
```

## 5. Verify

1. **`/tech/news`:** Jump to by hour, "Today by desk" with three coloured counts that filter, and P8 cards in desk
   colours.
2. **Each desk:**
   - The pills show today's counts, and selecting one filters the lead, list and grid, with the URL updating.
   - "All" clears the filter, and a 0-count pill is dimmed.
   - **Technology:** How-to & guides finds the practical pieces (check against a day's real headlines).
   - **Video Games:** Deals & sales.
   - **Linux:** Most covered and Linux gaming. By source counts add up to the desk's total.
3. **Without JavaScript:** the pills still filter (they're links).
4. **Phone** (320, 390, 430, light and dark): the desk-coloured ribbon and menu, the pills swipe, folds open and
   close, and touch targets are at least 44px.
5. **`dotnet test`:** the `TechDesks.Matches` tests pass.

Then the scraper work (`scraper-tech-handoff.md`) and, when its tables are populated, `tech-04-live-data.md`.

