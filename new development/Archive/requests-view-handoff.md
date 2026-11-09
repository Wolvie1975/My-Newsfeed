# Requests alert for the admin's profile (view only)

Written October 29, 2026 (America/Chicago) against `develop` @ `c8ac1e9`. Verify current repository state before
making changes; names and line numbers are from that commit.

**Save as:** `docs/requests-view-handoff.md`

**Approved mockups:** the myNewspaper canvas, **Admin** page, row "Admin alert" (right of the existing panel):

| Artboard | Shows |
|---|---|
| Requests (view only) — phone | Inbox button with a yellow count next to Saved; the Requests page |
| Requests (view only) — desktop | A yellow "Requests 3" pill between Saved and Settings; the same page with the sign-in on the right |

## Why

`AdminAlert` (profiles handoff 5.4) only renders inside `<AuthorizeView>`, so it shows **only while signed in to
Admin**. Reading as the household admin's profile without signing in, there is no sign that requests are waiting.

## Decisions

1. **Who sees it:** only the household admin's profile (`ProfileContext.IsHouseholdAdmin`, db/022) **while not signed
   in to Admin**. Signed in, the existing `AdminAlert` panel shows instead, never both.
2. **View only.** The page lists what's waiting and links each item to Admin · Requests. It has no Approve, Decline,
   Unlock or "Mark all seen" controls, and it writes nothing.
3. **The counter** is the number of requests **waiting** plus **locked profiles**: the things that need the admin.
   Unlike the signed-in alert, it doesn't drop to 0 after "Mark all seen"; it counts until each one is dealt with in
   Admin. "New" tags still use `Household.RequestsSeenAt`, so they match the panel.
4. **No database change.** Everything comes from `RequestStore` queries that already exist.
5. **Phones and desktop:** the same page at `/requests`; the header control is a plain link (no pop-up).

---

## 1. Data (`Requests/RequestStore.cs`)

Add one query that returns what the page shows, with nothing the page can act on:

```csharp
/// <summary>What the household admin's profile sees without an Admin session: counts and a read-only list.</summary>
public sealed record RequestOverview(int Waiting, int Locked, int NewSinceSeen, IReadOnlyList<AdminRequestRow> Requests,
    IReadOnlyList<LockedProfileAlert> LockedProfiles, DateTime? SeenAt);

/// <summary>The view-only header count: waiting requests plus locked profiles (not reset by Mark all seen).</summary>
public static async Task<int> OpenItemsCountAsync(WebScraperContext db, CancellationToken ct = default) =>
    await db.ContentRequests.CountAsync(r => r.Status == Waiting, ct) + await db.Profiles.CountAsync(p => p.LockedAt != null, ct);

public static async Task<RequestOverview> OverviewAsync(WebScraperContext db, CancellationToken ct = default)
{
    var summary = await SummaryAsync(db, ct);                       // locked profiles, 20 waiting, SeenAt
    var waiting = await db.ContentRequests.CountAsync(r => r.Status == Waiting, ct);
    var fresh = summary.Waiting.Count(r => summary.SeenAt is null || r.CreatedAt > summary.SeenAt);
    return new(waiting, summary.Locked.Count, fresh, summary.Waiting, summary.Locked, summary.SeenAt);
}
```

`ListAsync(db, Waiting, take: 20)` already orders and shapes the rows; the page shows **20 at most** and says
"and N more in Admin" when `Waiting > 20`.

## 2. Header control (`Components/Layout/RequestsViewLink.razor`, new)

Reuse the existing alert's look (`.admin-alert__button`, `.admin-alert__count`) so the two read as the same control.

```razor
@using myNewspaper.Web.Requests
@inject IDbContextFactory<WebScraperContext> Factory
@inject ILogger<RequestsViewLink> Log

@* The household admin's profile without an Admin session (requests-view-handoff.md): a plain link to /requests. *@
@if (count > 0)
{
    <div class="admin-alert admin-alert--view">
        <a class="admin-alert__button" href="requests" aria-current="@(Current ? "page" : null)" aria-label="@Label">
            <svg width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true"><path d="M3 13h5l2 3h4l2-3h5"></path><path d="M5 5h14l2 8v6H3v-6z"></path></svg>
            <span class="admin-alert__label" aria-hidden="true">Requests</span>
            <span class="admin-alert__count" aria-hidden="true">@count</span>
        </a>
    </div>
}

@code {
    [Parameter] public bool Current { get; set; }
    private int count;
    private string Label => count == 1 ? "Requests, 1 waiting" : $"Requests, {count} waiting";

    protected override async Task OnInitializedAsync()
    {
        try
        {
            await using var db = await Factory.CreateDbContextAsync();
            count = await RequestStore.OpenItemsCountAsync(db);
        }
        catch (Exception ex) when (DataFailures.IsReadFailure(ex))
        {
            Log.LogWarning(ex, "Could not count requests for the admin's profile.");   // no control; nothing breaks
        }
    }
}
```

The control is hidden when there's nothing waiting, like the Saved count.

### `Components/Layout/PublicLayout.razor`

Line ~86, replace the single `AuthorizeView` line:

```razor
@* Admin session: the full Requests panel. The admin's own profile without a session: the view-only link. *@
<AuthorizeView>
    <Authorized><AdminAlert /></Authorized>
    <NotAuthorized>@if (Reader.IsHouseholdAdmin) { <RequestsViewLink Current="@onRequests" /> }</NotAuthorized>
</AuthorizeView>
```

and in the path flags at the top: `var onRequests = path.Split('?')[0].Equals("requests", StringComparison.OrdinalIgnoreCase);`

In the phone menu (`mobile-nav__links`, line ~156) add, for the same condition: `<a href="requests">Requests</a>`.

### Styles (`wwwroot/public.css`, next to the `.admin-alert` rules at line ~665)

The view-only control uses the existing rules unchanged. Two additions:

```css
/* View-only link (admin's profile, no Admin session): the count is yellow so it reads as "waiting", not "new". */
.pub .admin-alert--view .admin-alert__count { background: var(--mx-ribbon-bg, #ffd34d); color: #141b2b; }
.pub .admin-alert--view .admin-alert__button[aria-current='page'] { background: rgba(255, 255, 255, 0.18); }
@media (min-width: 1024px) {
    /* Desktop masthead: a pill with the word, like the mockup. */
    .pub .admin-alert--view .admin-alert__button { padding: 0 12px; border-radius: 999px; background: #fff6d6;
        border: 1px solid #ffd34d; color: #141b2b; font-weight: 800; }
    .pub .admin-alert--view .admin-alert__label { display: inline; }
    .pub .admin-alert--view .admin-alert__count { position: static; }
}
```

Check these against the dark theme and two other colour schemes; the desktop pill uses fixed light colours on
purpose, like the ribbon.

## 3. The page (`Components/Pages/RequestsView.razor`, new)

Static server rendering (no `@rendermode`): it only reads.

```razor
@page "/requests"
@layout PublicLayout
@using myNewspaper.Web.Requests
@inject IDbContextFactory<WebScraperContext> Factory
@inject ProfileContext Reader
@inject FeedDates Dates
@inject NavigationManager Nav

<PageTitle>Requests · myNewspaper</PageTitle>

@if (!allowed) { <NotFoundContent /> }   @* or whatever NotFound.razor renders: never reveal the page exists *@
else if (overview is { } o)
{
    <div class="mx-phone">
        <PhoneRibbon Title="Requests" Kicker="@($"Household · {o.NewSinceSeen} new since you last looked")">
            <span class="rq-sub">@WaitingText(o)</span>
        </PhoneRibbon>
    </div>
    <div class="rq">
        <header class="rq-head mx-desktop">
            <p class="page-eyebrow">Household · view only</p>
            <h1>Requests</h1>
            <p>@WaitingText(o) · @o.NewSinceSeen new since you last looked</p>
        </header>
        <aside class="rq-side">
            <div class="rq-note" role="note"><b>View only.</b> Approving, declining and filling in details happen in
                Admin · Requests. You’ll be asked to sign in.</div>
            <a class="rq-signin" href="@($"login?returnUrl={Uri.EscapeDataString("/admin/requests")}")">Sign in to Admin · Requests</a>
        </aside>
        <ul class="rq-list">
            @foreach (var p in o.LockedProfiles)
            {
                <li class="rq-item rq-item--locked">
                    <span class="rq-avatar" style="background: @p.Color">@Initial(p.Name)</span>
                    <b>@p.Name is locked</b>
                    <span class="rq-meta">5 wrong PINs@(p.Device is null ? "" : " on " + p.Device) · @Dates.Published(p.LockedAt)</span>
                    <a class="rq-open" href="@($"admin/profiles/{p.ProfileId}#pin")">Open in Admin →</a>
                </li>
            }
            @foreach (var r in o.Requests)
            {
                var fresh = o.SeenAt is null || r.CreatedAt > o.SeenAt;
                <li class="rq-item @(fresh ? "is-new" : null)">
                    <span class="rq-avatar" style="background: @r.ProfileColor">@Initial(r.ProfileName)</span>
                    <b>@r.Name</b>@if (fresh) { <span class="rq-new">New</span> }
                    <span class="rq-meta">@(r.Kind == RequestStore.Youtube ? "YouTube channel" : "News source") · asked by @r.ProfileName · @Ago(r.CreatedAt)</span>
                    @if (!string.IsNullOrWhiteSpace(r.Note)) { <q class="rq-note-text">@r.Note</q> }
                    <span class="rq-check @(r.CheckOk == true ? "is-ok" : null)">@(r.CheckOk == true ? "✓ " : "")@(r.CheckSummary ?? "Not checked yet")</span>
                    <a class="rq-open" href="@($"admin/requests?id={r.Id}")">Open in Admin →</a>
                </li>
            }
        </ul>
        @if (o.Waiting > o.Requests.Count) { <p class="rq-more">and @(o.Waiting - o.Requests.Count) more in Admin</p> }
        @if (o.Waiting == 0 && o.Locked == 0) { <p class="rq-empty">Nothing is waiting. New requests show up here and in the header.</p> }
        <a class="rq-signin mx-phone" href="@($"login?returnUrl={Uri.EscapeDataString("/admin/requests")}")">Sign in to Admin · Requests</a>
    </div>
}

@code {
    private bool allowed;
    private RequestOverview? overview;

    protected override async Task OnInitializedAsync()
    {
        // Only the household admin's profile; an open Admin session goes straight to the real page.
        allowed = Reader.IsHouseholdAdmin;
        if (!allowed) return;
        await using var db = await Factory.CreateDbContextAsync();
        overview = await RequestStore.OverviewAsync(db);
    }
    // If an Admin session is open, send the admin to admin/requests (check AuthenticationState in OnInitializedAsync
    // and NavigateTo("admin/requests")); the view-only page is for the profile without a session.

    private static string Initial(string name) => name.Trim() is { Length: > 0 } n ? char.ToUpperInvariant(n[0]).ToString() : "?";
    private static string WaitingText(RequestOverview o) => o.Waiting == 1 ? "1 waiting" : $"{o.Waiting} waiting";
    private string Ago(DateTime utc) => …;   // reuse the panel's "2 min ago / 1 h ago / Yesterday" wording
}
```

Links:
- **Open in Admin →** goes to `admin/requests?id={id}` (the existing page already opens the item from `?id=`) or, for
  a locked profile, to `admin/profiles/{id}#pin`. Admin pages already redirect to `login` when there's no session
  and come back afterwards, so these links work from here.
- **Sign in to Admin · Requests** goes to `login?returnUrl=/admin/requests` (`AdminAuth.SafeReturnUrl` accepts it).

### Page styles (`wwwroot/sections.css`)

- `.rq`: on desktop, a two-column grid (`minmax(0, 1fr) 360px`) with the list on the left in two columns of cards and
  `.rq-side` sticky on the right. On phones, one column, with the note at the top and the sign-in button at the bottom.
- `.rq-item`: a card (14px radius, 1px border, white). `.is-new` gets the light-blue tint and a blue "New" tag, as in
  the panel.
- `.rq-check.is-ok` green, otherwise amber (`#6b4500`).
- `.rq-open`: a full-width 44px outline button in the accent colour.
- `.rq-note`: yellow note with a lock icon. `.rq-signin`: a dark 52px button with a lock icon.

---

## 4. Privacy and safety

- The page and the header link exist only for the household admin's profile. Everyone else gets 404 at `/requests`,
  not a "forbidden" page, so the address reveals nothing.
- It shows who asked and their note: the same things the admin sees in Admin. If the admin's profile has **no PIN**,
  anyone who picks that edition on a shared device can read the list (not act on it). Recommend setting a PIN on the
  admin's profile; consider a one-line hint on the page when it has none.
- Nothing on the page posts or changes data, so it needs no anti-forgery token or Admin session.

## 5. Optional: make the signed-in panel strictly view-only too

The existing panel (`admin-alert.js`) has two in-place actions: **Unlock** for a locked profile and **Mark all seen**.
Approve and Decline already link to Admin. To make the panel view-only as well, swap **Unlock** for a link to
`admin/profiles/{id}#pin` (the `lockedItem` function, line ~90) and remove **Mark all seen**. If **Mark all seen**
goes, the signed-in count stops resetting; switch `AdminAlert` to `OpenItemsCountAsync` so both counters agree.

## 6. Verify (per AGENTS.md)

1. **As the admin's profile, not signed in:** the counter shows when something is waiting and hides at 0; it opens
   `/requests`; the list matches Admin · Requests (waiting only), locked profiles first.
2. **Each "Open in Admin →"** goes to login, then lands on that request (or that profile's PIN section).
3. **No actions:** there is no button on the page that changes anything (check the HTML for forms and buttons).
4. **Signed in to Admin:** the header shows the existing panel, not the view-only link; `/requests` redirects to
   `admin/requests`.
5. **Another profile or Guest:** no header control; `/requests` returns 404.
6. **Phones** at 320, 390 and 430px and **desktop** at 744, 1024 and 1440px, light and dark, two other colour
   schemes: the badge stays inside the header, cards don't overflow, every link is at least 44px tall.
7. **Database down:** the header renders without the control; `/requests` shows the site's usual read-failure message.

## Implementation log — October 29, 2026 (UTC)

Implementation started after the admin website split. The public alert is gated by the household admin profile, independent of the separate admin login. All management links use the configured absolute admin address. The read-only page returns HTTP 404 for other selected profiles; without a selected profile, the existing edition picker remains in force. New counts query all waiting requests, independently of the 20 displayed cards. Database read failures are handled without exposing details. No schema changes or changes to the existing admin panel actions. Verification and deployment results will follow below.

Progress: implementation and local verification complete. The current maintained implementation notes are in `docs/requests-view-handoff.md`; they supersede the pre-split authentication, relative links, paths and limited new-count examples above. Public image `mynewspaper-web:requests-20261029` is deployed; live verification is running.

Completed: deployed `mynewspaper-web:requests-20261029` (image `f057f45e0be1`), healthy over HTTPS. Live Requests page assertions and 36 screenshots passed at 320/390/430/744/1024/1440px, light/dark, ribbon/Kansas/monochrome. Original household admin selection restored to Jason; temporary test profiles and requests removed, verified by database check. Detailed evidence and outstanding verification limits are recorded in `docs/requests-view-handoff.md`.
