# Tech front: Your stack, Your servers, New in Ollama, What developers are reading (web)

Written November 1, 2026 (America/Chicago), against `develop` at `75d6e5f`. Every file, class and line number below
was checked against that commit.

**Save as:** `docs/tech/tech-05-front-panels.md`

**Needs:** the scraper work in `docs/scraper-tech-panels-handoff.md` (a separate handoff for the Rss-Rust-Scrape
project). As with `tech-04-live-data.md`, the scraper owns every table here. **Don't add EF migrations**; map them
read-only, and render nothing when a table or column is missing, so this can deploy before the scraper.

**Mockup:** the myNewspaper canvas, **Tech section** page, "Tech front ideas (Nov 1)"
(https://claude.ai/artifact/JVQ8mU7R63Z1cziNpeW9Z5). The board's "Show a server down" property shows the red state.

## What changes

| # | Panel | New data? | Who sees it | Where |
|---|---|---|---|---|
| 1 | **Your stack**: latest release of each tool you use | One new column on `TechReleaseProjects` + seeded projects | Everyone | Tech front side column, top |
| 3 | **Your servers**: a dot per self-hosted service in the status strip | Two new tables | **Household admin only** | `TechStatusStrip`, after the public services |
| 6 | **New in Ollama**: new and updated library models, with which sizes fit the AI machine and which are installed | Two new tables | Everyone | Tech front side column |
| 7 | **What developers are reading** | None: `TechDevReading` already exists (Technology desk rail) | Everyone | Also on the Tech front side column |

| Width | Layout |
|---|---|
| 1920+ | The side column (`.spf-side`) splits into **two columns**: Your stack + Tech videos · New in Ollama + What developers are reading. That fills the wide, mostly empty Tech videos column |
| 1024–1919 | The side column stacks the four panels in that order |
| Phone, iPad portrait | Only Your servers changes (a one-line summary in the compact strip, admin only). The other three stay off phones for now |

**Just released** (`TechJustReleased`) leaves the **desktop** front: Your stack's "new in the last 48 hours" dot
replaces it there. The phone keeps it (line ~217).

---

## 1. Models (`myNewspaper.Shared/Data/Models/Tech.cs`)

Add to the existing file, read-only like the others, and register them in `WebScraperContext.Partial.cs` the same way
as `TechReleaseProject` etc.

```csharp
// TechReleaseProject: one new column.
public bool InYourStack { get; set; }

[Table("HomelabServices")]
public sealed class HomelabService
{
    [Key, Column("ID")] public int Id { get; set; }
    public string Slug { get; set; } = "";
    public string Name { get; set; } = "";
    public string? LinkUrl { get; set; }        // where a click goes (the service's own UI); never shown as text
    public bool Enabled { get; set; }
    public int SortOrder { get; set; }
}

[Table("HomelabServiceStatus")]
public sealed class HomelabServiceStatus
{
    [Key] public int ServiceId { get; set; }
    public string State { get; set; } = "unknown";   // ok | slow | down | unknown
    public int? LatencyMs { get; set; }
    public string? Detail { get; set; }              // "2 streaming", "last run 3 min ago", "HTTP 502"
    public DateTime StateSince { get; set; }         // UTC: when State last changed
    public DateTime CheckedAt { get; set; }          // UTC: last check
}

[Table("OllamaLibraryModels")]
public sealed class OllamaLibraryModel
{
    [Key, Column("ID")] public int Id { get; set; }
    public string Name { get; set; } = "";           // "llama4", "qwen3"
    public string? Description { get; set; }
    public string Change { get; set; } = "new";      // new | updated
    public DateTime ChangedAt { get; set; }          // UTC: first seen (new) or last update seen (updated)
    public string LibraryUrl { get; set; } = "";
    public DateTime SnapshotAt { get; set; }
}

[Table("OllamaLibraryTags")]
public sealed class OllamaLibraryTag
{
    [Key, Column("ID")] public int Id { get; set; }
    public int ModelId { get; set; }
    public string Tag { get; set; } = "";            // "8b", "70b", "e4b"
    public decimal? SizeGb { get; set; }
    public bool Fits { get; set; }                   // computed by the scraper against the AI machine's memory
    public bool Installed { get; set; }              // present in the AI machine's /api/tags
    public int SortOrder { get; set; }
}
```

> `InYourStack` is a new column on an existing table, so an older scraper database won't have it. Check with
> `COL_LENGTH('dbo.TechReleaseProjects', 'InYourStack')` in `LiveTablesAsync` (§2) before querying, or EF's
> generated SELECT will fail for **every** release query, including the existing tracker. If that's awkward, map
> `InYourStack` on a separate keyless query instead of the entity.

## 2. Queries (`myNewspaper.Shared/Data/TechQuery.cs`)

Extend `LiveTablesAsync` (line ~261) with three groups, cached the same way:

```csharp
"stack" => "SELECT CASE WHEN COL_LENGTH(N'dbo.TechReleaseProjects', N'InYourStack') IS NOT NULL THEN 1 ELSE 0 END AS [Value]",
"homelab" => "SELECT CASE WHEN OBJECT_ID(N'dbo.HomelabServices', N'U') IS NOT NULL AND OBJECT_ID(N'dbo.HomelabServiceStatus', N'U') IS NOT NULL THEN 1 ELSE 0 END AS [Value]",
"ollama" => "SELECT CASE WHEN OBJECT_ID(N'dbo.OllamaLibraryModels', N'U') IS NOT NULL AND OBJECT_ID(N'dbo.OllamaLibraryTags', N'U') IS NOT NULL THEN 1 ELSE 0 END AS [Value]",
```

New records and methods:

```csharp
public sealed record StackRelease(string Project, string Kind, string Version, string? Prerelease, DateTime ReleasedAt, string? NotesUrl, string Source);
public sealed record HomelabState(string Name, string State, int? LatencyMs, string? Detail, DateTime StateSince, DateTime CheckedAt, string? LinkUrl);
public sealed record OllamaModel(string Name, string? Description, string Change, DateTime ChangedAt, string LibraryUrl, IReadOnlyList<OllamaSize> Sizes);
public sealed record OllamaSize(string Tag, decimal? SizeGb, bool Fits, bool Installed);

Task<IReadOnlyList<StackRelease>> StackAsync(db, ct);
Task<IReadOnlyList<HomelabState>> HomelabAsync(db, ct);
Task<IReadOnlyList<OllamaModel>> OllamaRecentAsync(db, TimeSpan window, int take, ct);
```

| Method | Rule |
|---|---|
| `StackAsync` | Enabled projects with `InYourStack = 1`, by `SortOrder`. `Version` = newest **non-prerelease**. `Prerelease` = the newest prerelease **if it's newer** than that stable and from the last 30 days (so today's kernel shows "7.3 · 7.4-rc1"), else null. `ReleasedAt` is the newer of the two. `Source` via the existing `ReleaseSourceLabel` |
| `HomelabAsync` | Enabled services with their status row, by `SortOrder`. A service with no status row comes back as `unknown` |
| `OllamaRecentAsync` | Models with `ChangedAt` in the window (7 days), newest first, `take` 4; each with its tags by `SortOrder` |

Wrap each in the same `try … catch (DataFailures.IsReadFailure)` → empty as the existing Tech queries.

---

## 3. Your stack (`Components/TechYourStack.razor`, new)

Modelled on `TechReleaseTracker.razor`'s desktop table, but a simple list:

```razor
@inject IDbContextFactory<WebScraperContext> Factory
@inject FeedDates Dates

@if (rows.Count > 0)
{
    var today = Dates.TodayAt(nowUtc);
    <section class="nf-side tc-stack" aria-labelledby="tc-stack-title">
        <div class="ef-head"><h2 id="tc-stack-title" class="ef-label">Your stack <span class="tc-stack__sub">· latest releases</span></h2></div>
        <ul class="tc-stack__list">
            @foreach (var r in rows)
            {
                var fresh = nowUtc - r.ReleasedAt < TimeSpan.FromHours(48);
                <li class="tc-stack__row @(fresh ? "tc-stack__row--new" : null)">
                    <span class="tc-stack__dot" aria-hidden="true"></span>
                    <span class="tc-stack__name">
                        <b>@r.Project</b> <span class="tc-stack__ver">@r.Version@(r.Prerelease is { } pre ? $" · {pre}" : "")</span>
                        <small>@r.Kind · @r.Source</small>
                    </span>
                    <span class="tc-stack__when">@(fresh ? "New · " : "")@FormatReleased(Dates.TodayAt(r.ReleasedAt), today)</span>
                    @if (FeedQuery.SafeHref(r.NotesUrl) is { } notes)
                    { <a class="tc-stack__notes" href="@notes" target="_blank" rel="noopener noreferrer">Notes <span aria-hidden="true">↗</span></a> }
                </li>
            }
        </ul>
    </section>
}

@code {
    private IReadOnlyList<TechQuery.StackRelease> rows = [];
    private DateTime nowUtc;
    protected override async Task OnInitializedAsync()
    {
        nowUtc = DateTime.UtcNow;
        await using var db = await Factory.CreateDbContextAsync();
        if (await TechQuery.LiveTablesAsync(db, "stack")) rows = await TechQuery.StackAsync(db);
    }
}
```

`FormatReleased` is the same "Today / Yesterday / MMM d" helper `TechReleaseTracker` uses. Move it to `TechQuery` (or
a small static) rather than copying it. "New · " is there so the 48-hour state isn't carried by the yellow dot alone.

> **Managing the list** stays household-wide in the scraper's table, like the tracker (`tech-04` §3). There's no
> "Manage" link for now; add projects through the scraper's seed. A later Admin page can edit `InYourStack`.

## 4. Your servers (`Components/TechStatusStrip.razor`)

### 4.1 Who sees it

**Only the household admin** (`ProfileContext.IsHouseholdAdmin`, set in `ProfileGateMiddleware.cs` line ~68). Inject
`myNewspaper.Web.Profiles.ProfileContext Reader` into the strip and load `HomelabAsync` only when
`Reader.IsHouseholdAdmin` and `LiveTablesAsync(db, "homelab")` are both true. Other profiles get exactly today's
strip.

### 4.2 When the strip shows

Today the strip renders only when a public service has a problem (`tech-04` §6). For the admin it now **always
renders** when there are homelab services, so the green row is the "all fine" signal:

| Admin? | Public services | Your servers | Strip |
|---|---|---|---|
| No | all fine | n/a | hidden (unchanged) |
| No | a problem | n/a | as today |
| Yes | all fine | any | one line: the public services as small green dots, then **Your servers** |
| Yes | a problem | any | today's incident text first, then **Your servers** |

### 4.3 States

| State | Dot | Text after the name | Notes |
|---|---|---|---|
| `ok` | green | `Detail` if any ("· 2 streaming") | |
| `slow` | amber | "· slow 2.4 s" (from `LatencyMs`) | |
| `down` | red | "· down 12 min" (from `StateSince`) | name bold |
| `unknown` | grey | "· not checked" | |
| **stale** (`CheckedAt` > 5 min ago) | grey | "· not checked since 4:12 PM" | Means the **scraper** isn't running checks. Compute it here; the scraper can't report its own absence |

- Each service is a link to `LinkUrl` when set (`target="_blank"`), at least 36px tall, with
  `aria-label="Plex: down for 12 minutes"`-style text so the colour isn't the only signal.
- After the services: "checked 1 min ago" from the newest `CheckedAt`, muted.
- The scraper's own heartbeat row ("myNewspaper scraper · last run 3 min ago") comes through as an ordinary service.
  It goes grey or red like the others when it stops updating.

### 4.4 Phone (`Compact`)

For the admin, one extra line under (or instead of) today's line:

- everything `ok`: "● All 7 servers OK" in green;
- otherwise the worst one: "● Plex down 12 min · 1 other slow".

### 4.5 Styles (`wwwroot/sections.css`, next to the `.tc-status` rules)

```css
.pub .tc-status__servers {
    display: flex;
    flex-wrap: wrap;
    align-items: center;
    gap: 4px 18px;
}
.pub .tc-status__servers-label {
    font-size: 11px;
    font-weight: 800;
    letter-spacing: 0.1em;
    text-transform: uppercase;
}
.pub .tc-server {
    display: inline-flex;
    align-items: center;
    gap: 6px;
    min-height: 36px;
    color: inherit;
    text-decoration: none;
}
.pub .tc-server::before {
    content: '';
    width: 8px;
    height: 8px;
    border-radius: 999px;
    background: var(--tc-server-dot, #3ecf8e);
}
.pub .tc-server--slow { --tc-server-dot: #f59e0b; }
.pub .tc-server--down { --tc-server-dot: #ef4444; font-weight: 700; }
.pub :is(.tc-server--unknown, .tc-server--stale) { --tc-server-dot: #94a3b8; }
.pub .tc-server__detail {
    color: var(--text-2);
}
```

Separate the public services from Your servers with a 1px vertical rule (`.tc-status__rule`, `var(--row-border)`).

## 5. New in Ollama (`Components/TechOllama.razor`, new)

```razor
@if (models.Count > 0)
{
    <section class="nf-side tc-ollama" aria-labelledby="tc-ollama-title">
        <div class="ef-head"><h2 id="tc-ollama-title" class="ef-label">New in Ollama <span class="tc-stack__sub">· this week</span></h2>
            <a href="https://ollama.com/library" target="_blank" rel="noopener noreferrer">Library <span aria-hidden="true">↗</span></a></div>
        <ul class="tc-ollama__list">
            @foreach (var m in models)
            {
                <li class="tc-ollama__row">
                    <p class="tc-ollama__head">
                        @if (FeedQuery.SafeHref(m.LibraryUrl) is { } href)
                        { <a href="@href" target="_blank" rel="noopener noreferrer"><b>@m.Name</b></a> } else { <b>@m.Name</b> }
                        <span class="tc-ollama__change tc-ollama__change--@m.Change">@(m.Change == "new" ? "New" : "Updated")</span>
                        <span class="tc-ollama__when">@Ago(m.ChangedAt)</span>
                    </p>
                    @if (!string.IsNullOrWhiteSpace(m.Description)) { <p class="tc-ollama__desc">@m.Description</p> }
                    <ul class="tc-ollama__sizes" aria-label="Sizes">
                        @foreach (var s in m.Sizes)
                        {
                            <li class="tc-ollama__size @(s.Installed ? "is-installed" : s.Fits ? "is-fit" : null)"
                                title="@(s.Installed ? "Installed" : s.Fits ? "Fits the AI machine" : "Too large for the AI machine")">
                                @(s.Installed ? "✓ " : "")@s.Tag@(s.SizeGb is { } gb ? $" · {gb:0.#} GB" : "")
                            </li>
                        }
                    </ul>
                </li>
            }
        </ul>
        <p class="ef-note">Green: fits the AI machine · ✓ installed</p>
    </section>
}
```

`@code`: `OllamaRecentAsync(db, TimeSpan.FromDays(7), 4)` behind `LiveTablesAsync(db, "ollama")`. `Ago` gives
"today", "2 days ago" and so on.

The fit and installed flags come **from the scraper**, which knows the AI machine's address and memory. The web never
talks to Ollama directly; it's on a different machine.

```css
.pub .tc-ollama__sizes {
    display: flex;
    flex-wrap: wrap;
    gap: 6px;
    margin: 6px 0 0;
    padding: 0;
    list-style: none;
}
.pub .tc-ollama__size {
    padding: 2px 8px;
    border: 1px solid var(--row-border);
    border-radius: 999px;
    color: var(--text-2);
    font-size: 12px;
    font-weight: 700;
}
.pub .tc-ollama__size.is-fit,
.pub .tc-ollama__size.is-installed {
    border-color: #3ecf8e;
    color: var(--text);
}
.pub .tc-ollama__change {
    font-size: 11px;
    font-weight: 800;
    letter-spacing: 0.06em;
    text-transform: uppercase;
    color: var(--accent);
}
```

## 6. What developers are reading (no new component)

`TechDevReading.razor` already does this (two tabs, five rows, `?dev=hn|lobsters`). Render it on the front too (§7).
Its tab links build on the current URL, so on `/tech` they become `/tech?dev=lobsters`. Check `TabHref` keeps the
path rather than assuming `tech/technology`.

## 7. Placement (`Components/TechFront.razor`, desktop, lines ~95–127)

Replace `<TechJustReleased />` at the top of the side column, and add the three panels around Tech videos:

```razor
<aside class="ef-side spf-side tc-side">
    <TechYourStack />
    <section class="nf-side tc-videos" …>…</section>   @* Tech videos, unchanged *@
    <TechOllama />
    <TechDevReading />
</aside>
```

`TechStatusStrip` (line ~14) is unchanged in placement; §4 changes what it renders.

### 7.1 Two side columns at 1920+ (`wwwroot/sections.css`)

```css
/* Tech front, 1920px and up: the wide side column becomes two (docs/tech/tech-05-front-panels.md). */
@media (min-width: 1920px) {
    .pub .tc-front .tc-side {
        display: grid;
        grid-template-columns: repeat(2, minmax(0, 1fr));
        grid-auto-flow: row dense;
        column-gap: 28px;
        align-content: start;
        align-items: start;
    }
    .pub .tc-front .tc-side > .tc-stack,
    .pub .tc-front .tc-side > .tc-videos {
        grid-column: 1;
    }
    .pub .tc-front .tc-side > .tc-ollama,
    .pub .tc-front .tc-side > .td-dev {
        grid-column: 2;
    }
    .pub .tc-front .tc-side > .tc-videos .yt-row__thumb {
        width: 96px;          /* the rows are half as wide now: smaller thumbnails */
    }
}
```

If a panel is empty it renders nothing, and `dense` packing pulls the next one up in its column. If both panels in
one column are empty, the other column simply stands alone; that's acceptable.

Check the Tech videos thumbnail class name (`yt-row__…`) against `TechFront.razor` line ~110; adjust the selector to
match.

## 8. Verify

| Check | Expected |
|---|---|
| Scraper tables/column missing (point a local build at an old database) | Front renders exactly as today, with no errors; the existing release tracker still works |
| 2000 px | Side column in two: Your stack + Tech videos · New in Ollama + Developers reading |
| 1440 px | Four panels stacked in the side column |
| Your stack | One row per `InYourStack` project; kernel shows "7.3 · 7.4-rc1" style when an rc is newer; rows from the last 48 hours read "New · Today" with the yellow dot; Notes opens the changelog |
| Just released | Gone from the desktop front; still on the phone |
| Your servers, admin | Strip always visible; green dots; set one status row to `down` (test DB): red, "down N min"; set `CheckedAt` 10 min back: grey "not checked since …" |
| Your servers, other profile | Strip exactly as today (hidden when public services are fine) |
| Your servers, phone, admin | "● All N servers OK" or the worst one |
| New in Ollama | Up to four models from the last 7 days; green outline on sizes that fit; ✓ on installed; hidden when the window is empty |
| Developers reading on the front | Tabs switch, with and without JavaScript, and stay on `/tech` |
| Dark and light schemes | Dots and outlines readable in both |

Add to `tests/browser/tech-front.browser.cjs`: the side column has two grid columns at 2000 px, and the strip shows
`.tc-server` only for the admin profile.

## Files touched

| File | Section |
|---|---|
| `myNewspaper.Shared/Data/Models/Tech.cs`, `WebScraperContext.Partial.cs` | 1 |
| `myNewspaper.Shared/Data/TechQuery.cs` | 2 |
| `Components/TechYourStack.razor` (new) | 3 |
| `Components/TechStatusStrip.razor` | 4 |
| `Components/TechOllama.razor` (new) | 5 |
| `Components/TechDevReading.razor` (check only) | 6 |
| `Components/TechFront.razor` | 7 |
| `wwwroot/sections.css` | 4.5, 5, 7.1 |
| `tests/browser/tech-front.browser.cjs` | 8 |
