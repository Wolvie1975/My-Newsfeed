# Tech, part 4: release tracker, Just released, What developers are reading, Service status

**Save as:** `docs/tech/tech-04-live-data.md` · **Needs:** parts 1–3, and the scraper work in
`scraper-tech-handoff.md`. Its five tables must exist in the target database, populated, with `SELECT` granted to the
web user.

**Mockups:** **Tech section** page:

- Tech front (the status strip; Just released at the top of the side column);
- Linux desk (the Release tracker at the top of the main column);
- Technology desk (What developers are reading at the top of the rail);
- Linux desk, phone (the tracker's top five).

**Result:** the four slot components from parts 2 and 3 render real data. Each one checks its tables exist and renders
nothing if they don't, so a web deployment ahead of the scraper is safe.

## 1. Models (read-only, `myNewspaper.Shared/Data/Models/Tech.cs`)

Map the scraper's tables as **keyless or read-only entities** in `WebScraperContext`, like the other scraper-owned
tables the web reads (standings, rankings, box office). Don't add EF migrations for them.

```csharp
public sealed class TechReleaseProject { public int Id; public string Slug = ""; public string Name = ""; public string Kind = "";
    public string NotesLabel = "Release notes"; public bool Enabled; public int SortOrder; public DateTime? LastCheckedAt; public string? LastError; }
public sealed class TechRelease { public int Id; public int ProjectId; public string Version = ""; public DateTime ReleasedAt;
    public string? NotesUrl; public bool IsPrerelease; public TechReleaseProject Project = null!; }
public sealed class TechDevReadingItem { public string Site = ""; public int Rank; public string Title = ""; public string? Url;
    public string? Domain; public int Points; public int Comments; public string DiscussionUrl = ""; public DateTime SnapshotAt; }
public sealed class TechStatusService { public int Id; public string Slug = ""; public string Name = ""; public string PageUrl = ""; public bool Enabled; public int SortOrder; }
public sealed class TechServiceStatus { public int ServiceId; public string Indicator = "unknown"; public string? Description;
    public string? IncidentName; public string? IncidentComponent; public DateTime? IncidentStartedAt; public string? IncidentUrl; public DateTime CheckedAt; }
```

(Properties, not fields, in the real code. These are shorthand for the columns.)

## 2. Queries (`TechQuery`, extended)

```csharp
public sealed record TrackedRelease(string Project, string Kind, string Version, DateTime ReleasedAt, string? NotesUrl, string NotesLabel, string Source);
public sealed record DevStory(int Rank, string Title, string? Url, string? Domain, int Points, int Comments, string DiscussionUrl);
public sealed record ServiceState(string Name, string Indicator, string? Incident, string? Component, DateTime? Since, string PageUrl);

Task<bool> LiveTablesAsync(db, string group, ct);                  // "releases" | "devreading" | "status": OBJECT_ID checks, cached 10 min
Task<IReadOnlyList<TrackedRelease>> TrackerAsync(db, ct);          // newest non-prerelease per enabled project, by SortOrder
Task<IReadOnlyList<TrackedRelease>> JustReleasedAsync(db, TimeSpan window, int take, ct);   // releases in the window, newest first
Task<IReadOnlyList<DevStory>> DevReadingAsync(db, string site, int take, ct);  // "hn" | "lobsters", by Rank
Task<IReadOnlyList<ServiceState>> ServiceStatesAsync(db, ct);       // enabled services with their status, by SortOrder
```

- **`Source`** is a display name derived from the project's `SourceKind` + `SourceUrl`: "kernel.org", "GitHub
  releases", the feed's host ("kde.org", "fedoramagazine.org"), or "GitLab". Don't expose raw URLs as labels.
- **Missing tables:** catch the "invalid object name" error (`DataFailures.IsReadFailure`) and return empty, as well as
  checking `LiveTablesAsync`.

## 3. Release tracker (Linux desk, `TechReleaseTracker.razor`)

**Desktop:** a table under the heading "Release tracker", subtitle "Straight from each project: kernel.org, GitHub
releases, KDE and GNOME announcements, distro feeds", with a "Release history →" link (section 3.1).

| Column | Content |
|---|---|
| Project | `Name`, bold |
| Latest | `Version` (tabular numbers) |
| Released | "Today" (bold, Linux teal) / "Yesterday" / `MMM d`, America/Chicago |
| Source | `Source`, small and muted |
| Type | `Kind` as a small teal tag (Kernel, Distro, Desktop, Graphics, Gaming, App) |
| (link) | `NotesLabel ↗` to `NotesUrl`, `target="_blank" rel="noopener"`. The link is at least 40px tall |

- A row released **today** gets the light teal background, as in the mockup.
- Under the table: "Checked hourly · " plus the oldest `LastCheckedAt` as a time.

> The mockup says "you choose which projects to track in Settings · Add a project". The projects live in the
> scraper's `TechReleaseProjects` table and are **household-wide**, so manage them in **Admin** rather than per-profile
> Settings. The simplest version is a read-only list on the Admin status page: name, last checked, last error. Point
> the "Add a project" link there. A full Admin editor can come later.

**Phone (`Compact`):** a list of the **top five** by `ReleasedAt`:

- project and Kind · Source on the left;
- version and released date on the right ("Today" in teal);
- an "All" link to the full list.

### 3.1 Release history (`/tech/linux/releases`)

A simple page: every row in `TechReleases` from the last 12 months, newest first, grouped by month, with the same
columns plus a project filter (pills from the enabled projects). The desk's "Release history →" and the phone's "All"
link here.

## 4. Just released (Tech front, `TechJustReleased.razor`)

- The side column's first box: "Just released", subtitle "From kernel.org and project release feeds · last 48 hours",
  with a "Release tracker →" link to `/tech/linux`.
- **Rows:** the Kind tag in Linux teal, then "<Project> <Version>" bold, then "<Source> · <time>" muted. Up to **4** on
  desktop and **3** on the phone. `JustReleasedAsync(db, 48 h, 4)`.
- **Empty window:** hide the box. Don't show "No releases".

## 5. What developers are reading (Technology desk rail, `TechDevReading.razor`)

- The heading "What developers are reading", subtitle "Top stories right now".
- **Two tabs, Hacker News | Lobsters.** Use real links (`?dev=hn` / `?dev=lobsters`), so they work without
  JavaScript, styled as the mockup's pills (`role="tablist"` is optional; plain links in a `nav` are fine).
- **Five rows:** rank (muted), the **title** linking to `Url` (or to `DiscussionUrl` for text posts), then
  "<domain> · <points> points · <comments> comments", where "comments" links to `DiscussionUrl`. External links get
  `target="_blank" rel="noopener"`.
- **Footer:** "Updated <SnapshotAt as time>". If the snapshot is older than 2 hours, add "· may be out of date".

## 6. Service status (Tech front, `TechStatusStrip.razor`)

Shows **only when something isn't fine.**

- Every service is `none`, or the tables are missing: render **nothing**.
- Any service is `minor` / `major` / `critical` / `maintenance`: one strip under the tabs.
  - **Desktop:** an amber strip (`#fff4e5`, border `#f3c58a`; red tones for `major`/`critical`), with
    "● <Service>" bold, then "<Component>: <Incident> · since <time>", then "<page host> ↗" to `IncidentUrl` or
    `PageUrl`. On the right, the other services with a small green dot each.
  - Several services affected: list them one after another, ", " separated, and keep the strip on one line by
    truncating the incident text with an ellipsis.
  - **Phone:** a single line, "● GitHub · Actions degraded since 9:40 AM  Status ↗".
- `unknown` alone never shows the strip. That's a fetch problem, not an outage.
- **Dark mode:** use translucent amber and red backgrounds (`color-mix(in srgb, #f59e0b 18%, transparent)`) so the
  text stays readable.

## 7. Verify

1. **With the tables missing** (point a local build at a database without them): the front, Linux desk and
   Technology desk render with **no** gaps or errors where the four features go.
2. **With data:**
   - The tracker matches each project's real latest version, and "Today" highlights.
   - The notes links open the right pages.
   - The release history groups by month and filters.
   - Just released shows the last 48 hours, or hides.
   - Dev reading shows 5 per tab, and the tabs switch with and without JavaScript.
3. **Status:** set one `TechServiceStatus` row to `minor` with an incident (on a test database) and the strip
   appears; set it back to `none` and the strip disappears. Check `major` (red) and two services at once.
4. **Mobile** (320, 390, 430, light and dark):
   - The Linux phone tracker shows five rows and an All link.
   - The status line fits on one line with an ellipsis at 320.
   - The dev reading tabs are at least 44px.
5. **Desktop** (1280, 1440) and **iPad** (744, 820) if the iPad guides are done.

Then `tech-05-weekly.md`.

