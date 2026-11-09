# Profiles handoff

Written October 27, 2026 (America/Chicago) against `develop` @ `3e5ba34`. These are design decisions and an
implementation plan, not recorded observations; verify current repository state before changing anything.

**Revised October 27, 2026:**
- **Guest is removed.** You either open a profile or you see the picker.
- **Decided after review:**
  - anyone with the link can create a profile
  - every profile must have a PIN
  - 5 wrong PINs lock the profile until the admin acts, and the admin is alerted
  - nothing is auto-approved: every YouTube and news request goes to the admin
  - channels the scraper adds are turned on for everyone, and each profile can turn them off (never delete)
  - the admin resets a forgotten or locked PIN by setting the new one
  - the admin sees only who each profile is and the requests they send; there is no "Open as"
  - a base edition, edited by the admin, is what every new profile starts from
- **Build order:** foundation first, then the admin pieces, then the public front end. See [Build order (start here)](#build-order-start-here).
- **Plan corrections from a repo check against `3e5ba34`** are folded into the phases below; the
  [Repo check](#repo-check-october-27) section lists what was verified.
- **Undecided items** are collected under [Proposed, not decided](#proposed-not-decided). Nothing in that section is
  agreed yet; don't build from it.

**Lives in:** `new development/profiles-handoff.md`. The scraper checks are in
`new development/profiles-scraper-handoff.md`.

**Approved mockups** — the myNewspaper canvas (https://claude.ai/artifact/JVQ8mU7R63Z1cziNpeW9Z5):

| Canvas page | Artboard | What it fixes |
|---|---|---|
| Profiles | B · Pick your edition — desktop / phone | The gate: one column per profile, PIN sheet on protected profiles |
| Profiles | New profile setup — phone | 4 steps: name + color + PIN → teams → services → home sections |
| Profiles | Profile settings — desktop ("Make it yours") | Per-profile Settings: profile, home page, all of today's Settings, shows & channels, requests, household (admin only) |
| Admin | Admin · Profiles — desktop / phone | Profile list, lockouts, household rules |
| Admin | Admin · Manage profile — desktop | Reset PIN, devices, hide/delete |
| Admin | Admin · Requests — desktop | Channel and news-source requests: fill in, test, approve or decline |
| Admin | Admin alert — desktop / phone | Requests icon beside Saved, admin only, with the quick-look panel |

Option A (the tile picker) and the no-gate option were rejected. Don't build them.

> **Where the artboards still show Guest, leave it out:**
> - the picker footer's **Read the standard edition** link
> - the Guest column in the picker
> - the Guest status in the admin profile list
> - the Guest edition switch in the household settings
>
> **Leave out of the admin artboards too** (decision 3):
> - on Admin · Profiles: the likes summary, saved counts, last read and the saved-items total
> - on Admin · Manage profile: the edition summary, **Open as {name}** and **Copy settings**

---

## Build order (start here)

**Foundation first, then the admin pieces, then the public front end.** Stages F1 to F3 build the database, services
and endpoints behind an off switch. Stages A1 and A2 build every admin page while the switch is still off: admin
pages live under `/admin`, which the gate never blocks and which already requires the admin sign-in. Only stage U1
turns profiles on for readers. The phases further down are the reference for *what* to build; this section is the
order to build it in.

**The switch:** `Profiles:Enabled` in configuration, default `false`. While it's off:
- The gate middleware passes every request through, and `ProfileContext` stays empty.
- The public profile routes (`/editions`, `/profiles/*`, `/api/me*`) return 404. **Admin routes and admin APIs work
  either way**; they're protected by the admin sign-in, not the switch.
- `TvQuery` and `VideosQuery` get the household viewer, which means today's behavior (every enabled series, every
  channel). That viewer exists only for switch-off mode. Once the switch is on, every public caller passes the
  current profile, and the household viewer is never used.
- `SharedArticles` keeps reading and writing the legacy `MyDailyFeedSavedList`.
- The household channel rollout service isn't started.

Turning the switch back off is the rollback for every public front-end stage.

### Before F1 (no code)

- [x] Scraper checks done and reported: all six pass, no scraper changes needed
  (`profiles-scraper-compatibility-report.md`, October 27).
- [x] Fresh database backup: copy-only, checksum verified, October 28, on the SQL container at
  `/var/opt/mssql/data/WebScraper_pre020_20261028.bak`. There is one database (`sql2025` / `WebScraper`) for the
  site, the scraper and the integration tests.
- [ ] Admin password is no longer the `CHANGE_ME` placeholder.

### Foundation (no visible change)

These stages have no rendered impact. Per AGENTS.md, record that mobile checks don't apply, and why, instead of
claiming a pass.

**F1 — Database and models.** ✅ Done October 28.
- Run `020` only, not `021`. `021` runs at U1, so the Saved list it copies is current.
- Add the models and DbSets (Phase 1), and the `Profiles:Enabled` switch, set to `false`.
- Verify:
  - `020` runs twice cleanly.
  - The site is unchanged.
  - The next scraper run after `020` succeeds.
- **Status, October 28:** done.
  - `020` applied to `WebScraper` and rerun cleanly; all 8 foreign keys have the planned delete rules.
  - The scraper's hourly runs since then collected 269 of 270 sources, and the YouTube and TV stages succeeded. The
    one failure, ESPN WNBA standings, is unrelated.
  - F1 to F3 deployed together as `mynewspaper-web:profiles-20261028` (`6897db98b04b`); commit `4798ad8` on branch
    `profiles-foundation`. Rollback image: `mynewsfeed-web:before-mynewspaper-profiles-20261028`.
  - The site is unchanged: the previous image, run alongside, served byte-identical TV, YouTube, Saved, Entertainment
    and Settings pages. Details are in `docs/release-history.md`.

**F2 — Core services.** ✅ Done October 28.
- `ProfileContext`, the device and gate cookies, and the gate middleware (2.1, 2.2). It's registered, but passes
  through while the switch is off.
- `PasswordHasher<Profile>` and the PIN rules: required, 4 digits, the fixed limit of 5.
- `SiteDefaults` (C# only; the scripts change at U2), and settings and base-edition validation.
- The `Viewer` parameter and follow filters on `TvQuery` and `VideosQuery`, and `FollowQuery` (4.3).
- `SharedArticles` with a `profileId` (Phase 3), using the legacy list while the switch is off.
- The channel rollout (4.2) and the request checker with its SSRF guard (5.2), built and tested but not started.
- Verify with unit and integration tests:
  - the gate table
  - PIN wrong, lock and reset
  - the privacy test (4.3)
  - the rollout never touching request-approved channels
  - the SSRF cases
- **Status, October 28:** written; not committed or deployed. The full suite passes against `WebScraper` (920 tests,
  including the new unit, database and HTTP tests). Afterwards the live profile tables were checked and held no test
  rows: database tests roll back, and the HTTP tests delete what they create. Where things are:
  - `Profiles/`: `ProfileContext`, `ProfileCookies`, `ProfileGate`, `ProfileGateMiddleware`, `ProfilePins`,
    `ProfileStore`, `ProfileAdmin`, `ProfileSettings`, `SiteDefaults`, `Viewer`, `FollowQuery`, `ChannelRollout`.
  - `Requests/`: `SsrfGuard`, `RequestChecks`, `RequestChecker` (`RequestBacklog` and `RequestCheckerService`),
    `RequestStore`.
  - Not registered as hosted services: `RequestCheckerService` (starts at A2) and `ChannelRolloutService` (starts at U3).
  - Differences from the plan:
    - `SharedArticles` takes `int? profileId`, where null is the legacy list. Callers get it from
      `ProfileContext.SavedListProfileId`.
    - With the switch on and no profile, `ProfileContext.Viewer` is `Viewer.Nobody`, which shows nothing (fails
      closed).
    - Feed parsing uses `XDocument` with DTDs prohibited instead of the `System.ServiceModel.Syndication` package.
    - The settings document has only the keys the scripts store today: `theme`, `colorScheme`, `home`, `teams`,
      `services`, `settingsOrder`. `headlinesDefault`, `mutedPublishers` and `weatherCity` are rejected as unknown
      until those features exist.
    - Videos not yet linked to a feed are shown to no profile.
    - Admin **Reset PIN** signs out all of the profile's devices.
    - Hiding a profile revokes its devices.
    - **Approve and follow** for an already-collected channel leaves the request's `YoutubeVideoFeedId` empty, so the
      channel stays a household channel.
  - Mobile checks don't apply: no rendered change (the switch is off, and the public pages pass `Viewer.Household`,
    which keeps today's results).

**F3 — Endpoints.** ✅ Done October 28.
- Public: `/profiles/*` (open, new, new-pin, sign out) and `/api/me*`, including settings, import and requests (2.3,
  5.1). They return 404 while the switch is off.
- Admin: the admin APIs for profiles (add, edit, unlock, reset PIN, hide, delete, devices), household settings, the
  base edition, and requests (approve, decline, test feed, summary). They work whatever the switch says.
- Verify with integration tests, run with the switch on in the test host:
  - the no-profile sweep and the admin privacy check (Phase 7)
  - antiforgery on every POST and PUT
  - the per-profile Saved list
  - the admin APIs refusing requests without the admin cookie
- **Status, October 28:** written; not committed or deployed; the HTTP tests (`ProfileHttpTests`) pass. They cover
  the no-profile sweep, the switch-off 404s, antiforgery, the PIN flow, per-profile Saved, and admin auth and
  privacy.
  - Profile routes and the JSON APIs turn off the site's status-code pages. Re-running a POST through `/not-found`
    answered 400 for its antiforgery check, which hid the real 401 or 404.
  - `ProfileStore.LockAsync` reloads a row the context already tracks, so a reused context never acts on a stale
    lock state.
  - `Endpoints/ProfileEndpoints.cs` (public) and `Endpoints/AdminProfileEndpoints.cs` (`/admin/api/*`).
  - Profile forms answer with redirects. A fetch with `Accept: application/json` gets the status codes from 2.3
    (401 wrong PIN, 423 locked, 409 needs a PIN). A wrong PIN without JavaScript redirects to
    `/editions?profile={id}&error=wrong-pin&triesLeft=N`; the U1 picker reads that.
  - The setup submit takes steps 2 to 4 as one JSON `settings` form field.
  - Added beyond the list:
    - `PUT /api/me/device` (this device's picker mode) and `POST /api/me/pin` (Change PIN) for 6.3.
    - `GET /admin/api/token` (antiforgery token for admin fetches).
    - `POST /admin/api/requests/{id}/approve-collected`, `/check` and `/find-feeds`.
  - Unauthenticated `/admin/api/*` calls get 401 or 403 instead of a redirect to `/login`.
  - `public partial class Program` was added so the test host can start the app.
  - Mobile checks don't apply: no pages yet.
- **Open question, found while building F2:** deleting a profile cascades its requests away. Its request-approved
  channels then look like household channels, and the next rollout would turn them on for everyone. That's harmless
  while you're the only profile; decide before U4. One option: keep a "private channel" marker that outlives the
  request.
  **Decided October 29, 2026 (the user):** deleting a profile deletes every record related to it in the respective
  tables, so nothing it owned survives or rolls out to others. To build in U4: its requests, TV and channel follows,
  saved items and devices go. Today devices are kept with `ProfileId` set to NULL, and 32 channel follows belong to a
  test profile, so the delete rules and the admin delete need changing. **Confirmed October 29:** channels that only
  its requests added are removed (feed and stored videos), not rolled out to everyone.

### Admin pieces (switch still off)

Readers see no change; only you, signed in as admin, see these. They're rendered pages, so run the Phase 7 mobile and
desktop checks for the admin pages in each stage.

**A1 — Profiles and household.** ✅ Done October 28 (deployed as `mynewspaper-web:profiles-admin-20261028`); your two items below are left.
- [x] Admin · Profiles and Manage profile (6.4), with **Household → Profiles** in the admin sidebar.
- [x] Unlock, Reset PIN (the admin sets it), hide, delete, devices and sign-out.
- [x] **Add a profile** on Admin · Profiles: name, color and PIN, all set by the admin.
- [ ] **You:** create your own profile with its PIN, once A1 is deployed. `021` then finds it at U1 and gives it the
  Saved list and follows, instead of creating a PIN-less profile, so nobody else can choose your PIN first.
- [x] Household settings (picker default for new devices, forget devices after 90 days) and the **base edition editor**.
- [ ] **You:** set the base edition's values, once A1 is deployed.
- [x] The Delete confirmation on Admin → TV series says "Anyone following it will lose it" (4.1).
- [x] Mobile and desktop checks of the new admin pages (Phase 7), run locally against `WebScraper`.
- [x] Commit.
- [x] Deploy, then repeat the phone checks on the live build. The live build showed all 338 streaming services on the
  base edition (a 20,000px phone page), so it now lists only chosen services plus a search; redeployed and rechecked.
- **Status, October 28:** deployed and checked live at 320–1440px in light and dark; the lock banner with a real lock and
  a real device are still unchecked.
  - Pages: `/admin/profiles` (`ProfileList.razor`), `/admin/profiles/{id}` (`ProfileEdit.razor`) and
    `/admin/profiles/base` (`BaseEdition.razor`). Shared pieces: `ProfileTile`, `ProfileColorPicker`, `PinFields`.
    The menu has a **Household** group whose Profiles count is the number of locked profiles.
  - Checks:
    - `tests/browser/admin-profiles.browser.cjs`: add a profile (mismatched and non-digit PINs refused), Reset PIN,
      the save bars, the phone cards and ⋯ menu, and Delete. Also, at 320/390/430/744/820/1024/1440 in light and dark:
      no sideways scroll, AA contrast, and 44px controls. Screenshots reviewed.
    - `admin-layout` now includes the two new list pages and passes on all 23 pages.
    - 920 .NET tests pass, including the admin privacy check on the three pages.
  - Not checked: the lock banner with a real lock (profiles are off, so no PIN can be tried), the base edition's
    service picker (no catalog key locally; the live site has one), and a real device.
  - Differences from the mockups:
    - Picker order uses ↑/↓ buttons instead of drag, which also works by keyboard and touch.
    - The Requests column shows a count without a link; `/admin/requests` comes with A2.
    - The base edition has no home-section order, because the broadsheet layout fixes section positions and
      `personal.js` only hides sections.
    - The admin on/off switch everywhere got an invisible 44px-tall tap area; it looks the same.
    - The color-scheme list moved into `SiteDefaults`, so Settings and the base edition share it. Settings renders
      exactly as before.

**A2 — Requests and the alert.** ✅ Done October 28 (deployed as `mynewspaper-web:profiles-requests-20261028`).
- [x] The `/admin/requests` page (5.3), with **Requests** in the admin sidebar.
- [x] **Suggest to everyone** (`IsHouseholdPick`) on Admin → YouTube feeds → Edit (4.2).
- [x] The admin alert in the site header (5.4), counting waiting requests and locked profiles. It sits in the public
  header but renders only for the admin.
- [x] Start the request checker. Requests can't be submitted until U3, so test this page with seeded data.
- [x] Commit.
- [x] Deploy, then repeat the phone checks on the live build. `admin-requests` and `admin-profiles` passed on the live
  build at every width in light and dark, with seeded rows (removed afterwards); the live checker resolved the YouTube link.
- **Status, October 28:** deployed and checked live. Built and checked locally first against `WebScraper` with seeded rows.
  - Pages and pieces: `RequestList.razor` (`/admin/requests`, with `?status=` and `?id=`), `TestResult.razor`,
    `Components/Layout/AdminAlert.razor` (in `PublicLayout`, inside `AuthorizeView`) and `wwwroot/admin-alert.js`.
    `RequestCheckerService` is registered. Manage profile's requests and the Profiles list's waiting count now link to
    the Requests page.
  - The checker's page limit went from 2 MB to 5 MB: a YouTube channel page is about 2.6 MB, so every `@handle`
    link failed before. With that fixed, `youtube.com/@LinusTechTips` resolves and Test feed reads 15 videos.
    `ljworld.com` really has no working feed (its `/feed/` is a WordPress error), so "No feed found" is right there.
  - Checks:
    - `tests/browser/admin-requests.browser.cjs`, at 320/390/430/744/820/1024/1440 in light and dark: no sideways
      scroll, AA contrast, 44px controls, the queue beside the request from 1024px (one or the other below), Check
      again, Test feed, the channel-ID rule, Decline with a reason, approving an already-collected channel, and the
      header alert as a bottom sheet on phones and a popover from 744px (locked profiles first, Escape closes,
      Unlock, Mark all seen). Readers never get the alert. Screenshots reviewed. It needs seeded rows (see its
      header); it never adds a feed or source.
    - 923 .NET tests pass, including a YouTube-sized page test, news-source approval, the Requests page in the admin
      privacy test, and a test that only the signed-in admin gets the alert.
    - `admin-layout` now includes `/admin/requests` and passes on all 24 pages; `admin-profiles` still passes.
  - Not checked: Add channel and approve and Add source and approve in a browser (they'd add a real feed or source
    to the shared database; the .NET tests cover both in rolled-back transactions), and a real device.
  - Differences from the mockups:
    - The alert panel has no All / YouTube / News filter; the Requests page has the full filters.
    - Reset PIN in the panel opens Manage profile's Reset PIN form, since the admin types the new PIN.
    - Decline… in the panel opens the request on the Requests page at its reason field.

### Public front end (turning it on)

These stages change what readers see. Run the full Phase 7 mobile and desktop checks for each one.

**U1 — Picker and switch-on.** ✅ Switched on October 28. Profiles are live; you're the only profile until U4.
- [x] Build `/editions` (6.1) and the new-PIN page. The new-PIN page is only a fallback now, for a profile that has no PIN.
- [x] Decide the per-device PIN limit ([Proposed security hardening](#proposed-security-hardening) item 4): **keep it as
  is** (decided October 28). Only the per-profile lock after 5 wrong PINs; revisit before U4 adds other people.
- [x] **You:** create your profile with its PIN in Admin · Profiles (021 now refuses to run without exactly one profile).
- [x] Deploy the U1 code with profiles still off (`mynewspaper-web:profiles-picker-20261028`, October 28).
- [x] **You:** take a backup of `WebScraper`.
- [x] Run `021` (10 TV follows, 32 channels, 32 rollouts, Saved list identical), set `Profiles:Enabled=true`, and
  repeat the phone checks (`editions.browser.cjs`) on the live build. All passed.
- [ ] **You:** open your edition with your PIN on each device you use.
- **Status, October 28:** built and checked locally with `Profiles__Enabled=true`.
  - Pages and pieces: `Components/Pages/Editions/Editions.razor` (`/editions`), `NewPin.razor`
    (`/editions/{id}/new-pin`; the open endpoint's needs-PIN redirect now points there, because a component page and the
    `/profiles/{id}/new-pin` POST can't share a route), `PickerLayout.razor`, `Profiles/PickerPreviewQuery.cs` (counts
    only: episodes airing today and videos since the last read from followed shows and channels, and the Saved count;
    it never selects a name), `wwwroot/profiles.js` (the PIN pad, and sending a script's gated 401 to the picker
    except on pages the gate leaves open), the header's edition chip from 744px and **Switch edition** in the phone
    menu. The picker's footer has **Sign this device out** when a profile is open, and **Manage profiles**.
  - `db/021_seed_first_profiles.sql` differs from the sketch below: it never creates a profile and stops unless
    exactly one exists. Dry-run in a rolled-back transaction: 10 TV follows, 32 channel follows, 32 rollouts and the
    Saved list; with no profile it stops with its message.
  - Checks:
    - `tests/browser/editions.browser.cjs` (skips while profiles are off), at 320/390/430/744/820/1024/1440 in light
      and dark: pages without a profile go to the picker and come back, the PIN pad as a bottom sheet on phones and a
      dialog from 744px, wrong PIN with tries left, five wrong PINs lock, the no-JavaScript form, the edition chip,
      Switch edition in the phone menu, a script's fetch sent to the picker when the session's gate cookie is gone,
      sign out, no sideways scroll, contrast and 44px targets. Screenshots reviewed.
    - With profiles off: `personal-layout`, `tablet-layout`, `scheme-contrast`, `theme-mode`, `shared-saved` and
      `branding` pass. `mixed-search-nav` fails locally because there's no streaming-catalog key here, not because
      of this change.
    - 925 .NET tests, including the picker never printing a followed show or channel name, and the first-PIN page.
  - Not checked: a real device; the picker's "Your teams" line (teams arrive with settings at U2).
  - Once profiles are on, the existing public browser checks see the picker. They'll need a profile to sign in with
    (an env var for a test profile and PIN) before they can run against the live site again.
  - Differences from the mockups: every column's button is filled (no first column is highlighted, so the picker
    doesn't hint at the last profile used); no weather in the masthead; **+ Start a new edition** waits for U4.
- From this point, the gate, the follow filters and the per-profile Saved list are all live together.
- You're the only profile until U4, so the follow and Saved data is all yours.

**U2 — Settings move.** ✅ Done October 28 (deployed as `mynewspaper-web:profile-settings-20261028`). `db/022_household_admin_profile.sql` is already applied to `WebScraper` (an empty nullable column; the live
U1 build ignores it).
- [x] `wwwroot/profiles.js` (one file with the gate and PIN pad, rather than a separate `profile.js`):
  `window.myNewspaperPrefs` with localStorage's `getItem`/`setItem`/`removeItem`. With a profile open, the old keys
  (`theme`, `mynewsfeed-color-scheme`, `mynewsfeed-personal-v1` → `home`, `mynewsfeed-sports-teams-v1` → `teams`,
  `mynewsfeed-watch-services-v1` → `services`, `mynewsfeed-settings-order-v1` → `settingsOrder`,
  `mydailyfeed-scoreboard-teams-v1` → the new `scoreboardTeams` setting) read and write the profile (PUT
  `/api/me/settings`, 400 ms debounce, flushed on `pagehide`); other keys stay in localStorage.
- [x] One-time import: only from a browser that has old keys; then they're removed and the page reloads if anything
  changed.
- [x] `App.razor` renders `data-theme-mode`/`data-color-scheme` on `<html>` and `<script id="mnp-profile">`
  (`Profiles/ProfilePageData.cs`); `theme-init.js` reads them first. `personal.js`, `settings-order.js` and the color
  scheme block in `public.js` read `SiteDefaults` from the page, keeping their lists only as fallbacks.
- [x] "Saved to {name}'s edition" wording on Settings and in the scripts' status messages.
- [x] The picker's **Your teams** line (today's first game for one of the profile's teams).
- [x] **Household admin** (asked for on October 28): a switch on Manage profile marks one profile as the admin's
  (`Household.AdminProfileId`, db/022). With profiles on, only that profile's pages show **Admin sign in**; a signed-in
  admin still sees **Admin**. With profiles off, everyone keeps the link as before. `/login` itself is unchanged.
- [x] 927 .NET tests pass (scoreboard stars, page data escaping, damaged stored values). Three older tests were
  updated: since 021 rolled out every channel, a new profile starts following the household channels.
- [x] `tests/browser/profile-settings.browser.cjs` passed locally and on the live build at every width in light and
  dark. Jason is marked as the household admin. What it covers:
  theme, scheme, teams, services, home sections and Settings order saved to the profile and seen on a second device;
  no theme flash; the import (old keys set before opening, gone afterwards, not given to a second profile on the same
  device); the Saved-list import still works; the Admin sign in link only for the marked profile. Phone checks at
  320/390/430 light and dark, plus tablet/desktop. Then deploy, mark your profile as the household admin, and repeat
  the checks live.
- Note: the legacy Saved-list import (`saved-articles`) still goes into whichever profile opens first on that browser.

**U2 — Settings move (as planned).** `profile.js`, `/api/me/settings` wired into the scripts, the one-time browser import, and theme
attributes on `<html>` (2.5). The scripts read `SiteDefaults` from the page instead of their own copies.

**U3 — Make it yours.** ✅ Done October 28 (deployed as `mynewspaper-web:make-it-yours-20261028`).
- [x] Settings sections (6.3): Profile with Change PIN; Shows & channels, with the `SeriesFollowForm` fix and the
  household channel switches; the request form and Your requests.
  - Built as static forms to `/me/*` (`Endpoints/MeEndpoints.cs`, `Profiles/MyFollows.cs`,
    `Components/Settings/*`), not a circuit, because an interactive circuit can't see the open profile. The public
    `/tv/track` uses the same static search (`TvShowSearch`); `SeriesFollowForm` stays for admin, and for `/tv/track`
    only while profiles are off.
  - Not built from 6.3 yet: the Home page order controls on Settings (home sections are still set on the home page),
    **Reading** (Headlines default, weather city, muted publishers: these settings don't exist yet), and the
    admin-only **Household** section (Admin · Profiles covers it).
- [x] Start the household channel rollout service (and trigger it when the admin saves a feed).
- [x] `tests/browser/settings-sections.browser.cjs`, passing locally and live at every width in light and dark.

**U4 — New-profile setup.** ✅ Deployed October 29, 2026 as `mynewspaper-web:profiles-u4-20261029`, built from
`e23a1d7` (U4 `afba7db` plus the other session's Settings panels), rollback container and tag
`mynewsfeed-web-before-profiles-u4-20261029` / `mynewsfeed-web:before-profiles-u4-20261029`. Verified: 957 .NET tests;
`profile-setup` at 320–1440px light/dark, with and without JavaScript, locally and live.
- [x] The four-step setup flow (6.2), prefilled from the base edition. Built at `/editions/new` (`NewEdition.razor`), not
  `/profiles/new`: Razor pages also take POSTs, and `POST /profiles/new` is the create endpoint (NewPin lives under
  `/editions` for the same reason). One form; without JavaScript the four steps show as one long form, and
  `profiles.js` (job 3) steps it with Back, Continue (off until a name and the same PIN twice) and a progress bar,
  posting as JSON so a refused name keeps every choice. The endpoint now also takes the plain fields (`setup=1`;
  `ProfileSettings.FromSetup` lays them over the base edition). Teams use Settings' groups with a search; services
  list every US service (there is no "popular" subset; up to 20); home sections are checkboxes with move-up
  buttons. The mockup's optional-PIN switch is replaced by the required PIN (decision 2); no Skip link.
- [x] Profile deletion removes every record tied to the profile, including channels only its requests added
  (decided October 29; see the F2 open question). `ProfileAdmin.DeleteAsync` runs one transaction under the channel
  rollout lock: devices, then the profile (cascades), then private feeds and their stored videos. Test:
  `Deleting_a_profile_removes_its_devices_and_the_channels_only_it_requested`.
- [x] `+ Start a new edition` on the picker opens the setup (always shown, decision 10); the empty picker points at it.
- **Only after U4 ships, add other people's profiles**, either through setup or with Add a profile in admin.

Record each stage in `docs/release-history.md` with the widths, themes and flows checked (or why mobile doesn't
apply), as usual.

---

## Decisions (agreed in the design session, revised October 27)

1. **A gate at the start.** Every device shows the edition picker (option B) until a profile is chosen. How often it
   reappears is per device: **Every visit** (default), **Once a day**, or **Only after signing out**.
2. **No passwords, but every profile has a PIN.** Tailscale is the security boundary (see
   [Proposed security hardening](#proposed-security-hardening), item 1, for the LAN gap). Every profile has a
   **4-digit PIN**; there is no PIN-free profile. After **5 wrong PINs** the profile stays locked until the admin
   unlocks it or resets the PIN, and the admin is alerted in the site header (5.4). There is no timed unlock.
   **Resetting means the admin sets the new PIN** and tells the person; they can change it later in Settings.
3. **Admin is separate.** The existing admin cookie (`MyNewsFeed.Admin`, `/login`) is the super user. It is not a
   profile and never becomes one. **The admin sees only who each profile is and what they ask the admin for:** name,
   color, whether it's locked or hidden, the devices it's signed in on (to unlock and sign out), and its requests.
   The admin never sees a profile's teams, services, follows, saved items, settings or reading times, and can't open
   the site as a profile. Profiles can't see each other either.
4. **Settings follow the person, not the browser.** Everything in `localStorage` today moves to the profile on the
   server. The browser only remembers *which device this is*.
5. **Each profile has its own Saved list.** Today's single shared list becomes the first profile's list.
6. **TV shows:** anyone adds a show directly (TVmaze search). It's tracked once for the household and followed by
   that profile.
7. **YouTube channels and news sources: by request, always approved by the admin.** The admin fills in the feed
   details, tests, approves or declines. **Nothing is auto-approved**, not even a channel or source that's already
   collected. The requester always sees Waiting, then Added or Declined, so the timing reveals nothing.
8. **Privacy.** A profile never sees what another profile follows or requests. Search results, picker previews and
   suggestions must not reveal it (no "followed by Alex", no "Add and follow" vs "Follow"). The only shared
   suggestions are **Household picks**, a flag the admin sets on a YouTube feed.
9. **No Guest.** Either you're a profile or you're not. Without a profile, every public page and API sends you to the
   picker. The base edition (decision 12) is never readable on its own; it only seeds new profiles.
10. **Anyone with the link can create a profile.** The site is only reachable inside the network, so that group is
    small. **+ Start a new edition** is always on the picker, and new profiles need no admin approval.
11. **Household channels are on for everyone.** Any YouTube channel that didn't come from a profile's request (the
    scraper added it, the admin added it, or it existed before profiles) is a **household channel**. It's turned on
    for every profile, including profiles created later. Each profile can **turn one off** for itself, and back on;
    turning off never deletes or disables the feed. Channels added through a request belong to the requester only
    (decision 8), unless the admin also marks them a household pick.
12. **A base edition seeds every new profile.** The admin edits one settings document, the **base edition**, with the
    same controls as Make it yours, including the site's existing team and service pickers. It holds the household's
    shared facts (local teams, the services the house pays for, the weather city), not anyone's personal tastes. A new
    profile **copies it once** when it's created; changing it later never changes existing profiles. It belongs to
    nobody, so it doesn't conflict with decision 3.

---

## What exists today (the starting point)

| Thing | Where it lives now | Moves to |
|---|---|---|
| Light/dark mode | `localStorage['theme']` (`public.js`, `theme-init.js`) | profile settings |
| Color scheme | `localStorage['mynewsfeed-color-scheme']` | profile settings |
| Home order, hidden sections, teams-only | `localStorage['mynewsfeed-personal-v1']` (`personal.js`) | profile settings |
| My Teams | `localStorage['mynewsfeed-sports-teams-v1']` (`public.js`, `scoreboard.js`) | profile settings |
| Settings section order | `localStorage['mynewsfeed-settings-order-v1']` (`settings-order.js`) | profile settings |
| My services | `localStorage['mynewsfeed-watch-services-v1']` (`watch.js`) | profile settings |
| Saved list | `dbo.MyDailyFeedSavedList` (one row, Id = 1) via `SharedArticles` | per-profile rows |
| Tracked TV series | `dbo.TvTrackedSeries` (scraper-owned; site inserts via `SeriesFollowForm`) | stays; plus per-profile follows |
| YouTube feeds | `dbo.YoutubeVideoFeed` | stays; plus per-profile follows and `IsHouseholdPick` |
| News sources | `dbo.Sources` | stays; requests create rows here |

App-owned tables already use the `MyDailyFeed` prefix (`db/018_shared_saved_articles.sql`). Keep that convention so
it's obvious the scraper never writes them.

---

## Repo check (October 27)

Verified against `develop` @ `3e5ba34`. The Rust scraper is a separate project and was **not** checked.

**Already in place, no work needed:**
- **Data Protection keys persist.** The `Dockerfile` sets `DataProtection__KeysPath=/keys`, `docker-compose.yml`
  mounts the `mynewsfeed-keys` volume there, and `Program.cs` reads it. Device cookies survive rebuilds.
- **No output or response caching.** API responses set `no-store`. The only public cache header is
  `/local/weather` without coordinates (`public,max-age=120`), which isn't per person.
- **The service worker never caches page HTML.** On listed pages it always goes to the network and falls back to
  `offline.html`. It precaches only the offline page and icons, so per-profile HTML and the embedded profile JSON
  are safe.
- **All public TV and YouTube reads go through two classes:**
  - `TvQuery`, used by Home, `SectionAgenda`, `EntertainmentFront`, the TV page, and `SportsFront` through
    `TodayTonight`.
  - `VideosQuery`, used by Home and the YouTube page.

  No public endpoint returns TV or YouTube data directly. That makes these two classes the privacy chokepoint (4.3).

**Folded into the phases below:**
- **Use the context factory.** `Program.cs` registers only `AddDbContextFactory<WebScraperContext>`. There is no
  scoped `WebScraperContext`, so endpoints and middleware take `IDbContextFactory<WebScraperContext>`, like
  `PublicEndpoints` does (2.2, 2.3).
- **`SharedArticles` is a singleton,** so `profileId` must be a method argument. It can't come from an injected
  scoped service (Phase 3).
- **The middleware can't skip static files by path alone.** `UseAuthentication()` runs before `MapStaticAssets()`,
  so the gate middleware sees every request. Skip static assets by endpoint metadata (2.2).
- **`SeriesFollowForm` shows a household-wide "Following" state** (line 26, built from every enabled
  `TvTrackedSeries` at line 57). That reveals other profiles' shows (4.1).
- **Free-form IDs bypass the follow filter.** `VideosQuery.GetVideosAsync(channelId, search)` and
  `TvQuery.GetAsync(show, search)` accept any channel or show ID and a search across everything (4.3).
- **The admin TV Delete removes the tracked row** (`TvQuery.RemoveAsync`, `ExecuteDeleteAsync`). With the planned
  `ON DELETE CASCADE`, that also removes every profile's follow (4.1).
- **The scraper can add YouTube feeds by itself** (`VideosQuery` mentions "a channel the scraper added"). Such a
  feed has no followers, so no profile will see it (4.2).

**Security findings** (see [Proposed security hardening](#proposed-security-hardening)):
- **Tailscale isn't the only way in.** `docker-compose.yml` publishes `${WEB_PORT:-8085}:8080` on every host
  interface, and `DEPLOY.md` says "LAN HTTP remains available". Anyone on the home network can reach the site over
  plain HTTP without Tailscale, so decision 2's boundary doesn't hold as deployed.
- **No limit on admin sign-in attempts.** `/account/login` (`AccountEndpoints`) has no rate limit or lockout, and
  there's no rate limiting anywhere in the app.
- **No security headers.** Nothing sets `Content-Security-Policy`, `frame-ancestors`, `X-Frame-Options` or a cookie
  `SecurePolicy`.

**Scraper checks: done October 27, all pass.** The scraper team checked Rss-Rust-Scrape `main` @ `fde6d4c` and the
production database (`profiles-scraper-compatibility-report.md`):
- No `TRUNCATE`, no delete-and-reinsert, and no deleting `MERGE` on `TvTrackedSeries` or `YoutubeVideoFeed`.
- Every insert into `YoutubeVideoFeed` names its columns; there is no `SELECT *` on either table.
- The scraper's schema scripts only create these tables when they're missing, and never drop or rebuild them.
- Both keys are `ID` (`INT IDENTITY`).
- `020` can keep `ON DELETE CASCADE` on all three foreign keys. No scraper changes are needed.

Notes from the report that matter here:
- **Deleting a YouTube feed with stored videos already fails.** `FK_YouTubeVideos_YoutubeVideoFeed` is
  `NO ACTION`, so the new cascades only ever fire for feeds with no videos.
- **The scraper's schema snapshot** (`sql/schema/WebScraper.schema.sql`, used only for new installs) will pick up the
  `MyDailyFeed*` tables and `IsHouseholdPick` if it's regenerated from the live database after `020`. Whoever
  regenerates it decides whether to keep them.

---

## Phase 1 — Database (`db/020_profiles.sql`)

Same style as 018/019: idempotent, one transaction, safe to rerun. **Back up the database first.** The scraper checks
it depended on are done (October 27, all pass).

```sql
-- App-owned profiles, devices, follows and requests. The scraper never reads or writes these tables,
-- except YoutubeVideoFeed.IsHouseholdPick (web-owned column, like SourceCategories.NewsHub).
-- Run after a fresh backup. Repeatable.
SET XACT_ABORT ON;
BEGIN TRANSACTION;

IF OBJECT_ID(N'dbo.MyDailyFeedProfiles', N'U') IS NULL
    CREATE TABLE dbo.MyDailyFeedProfiles (
        Id               int IDENTITY(1,1) NOT NULL CONSTRAINT PK_MyDailyFeedProfiles PRIMARY KEY,
        Name             nvarchar(40)  NOT NULL,
        Color            char(7)       NOT NULL CONSTRAINT DF_MyDailyFeedProfiles_Color DEFAULT '#0051BA',
        Hidden           bit           NOT NULL CONSTRAINT DF_MyDailyFeedProfiles_Hidden DEFAULT 0,
        SortOrder        int           NOT NULL CONSTRAINT DF_MyDailyFeedProfiles_Sort DEFAULT 0,
        PinHash          nvarchar(200) NULL,          -- PasswordHasher output; NULL = must choose a PIN on next open
        FailedPinCount   int           NOT NULL CONSTRAINT DF_MyDailyFeedProfiles_Failed DEFAULT 0,
        LockedAt         datetime2     NULL,          -- set on the 5th wrong PIN; cleared only by the admin
        LockedByDeviceId uniqueidentifier NULL,
        Settings         nvarchar(max) NOT NULL CONSTRAINT DF_MyDailyFeedProfiles_Settings DEFAULT N'{}',
        BrowserImportAt  datetime2     NULL,          -- set when localStorage settings were imported once
        CreatedAt        datetime2     NOT NULL CONSTRAINT DF_MyDailyFeedProfiles_Created DEFAULT SYSUTCDATETIME(),
        LastReadAt       datetime2     NULL,
        CONSTRAINT UQ_MyDailyFeedProfiles_Name UNIQUE (Name),
        CONSTRAINT CK_MyDailyFeedProfiles_Settings CHECK (ISJSON(Settings) = 1)
    );

IF OBJECT_ID(N'dbo.MyDailyFeedProfileDevices', N'U') IS NULL
    CREATE TABLE dbo.MyDailyFeedProfileDevices (
        Id          uniqueidentifier NOT NULL CONSTRAINT PK_MyDailyFeedProfileDevices PRIMARY KEY,
        ProfileId   int              NULL,            -- NULL = device known, no profile chosen yet
        TokenHash   varbinary(32)    NOT NULL,        -- SHA-256 of the cookie secret
        Label       nvarchar(100)    NOT NULL,        -- "Living room iPad"; editable by the admin
        UserAgent   nvarchar(300)    NULL,
        PickerMode  nvarchar(16)     NOT NULL CONSTRAINT DF_MyDailyFeedProfileDevices_Mode DEFAULT N'every-visit',
        CreatedAt   datetime2        NOT NULL CONSTRAINT DF_MyDailyFeedProfileDevices_Created DEFAULT SYSUTCDATETIME(),
        LastSeenAt  datetime2        NOT NULL CONSTRAINT DF_MyDailyFeedProfileDevices_Seen DEFAULT SYSUTCDATETIME(),
        RevokedAt   datetime2        NULL,
        CONSTRAINT FK_MyDailyFeedProfileDevices_Profile FOREIGN KEY (ProfileId)
            REFERENCES dbo.MyDailyFeedProfiles(Id) ON DELETE SET NULL,
        CONSTRAINT CK_MyDailyFeedProfileDevices_Mode CHECK (PickerMode IN (N'every-visit', N'daily', N'until-signout'))
    );

IF OBJECT_ID(N'dbo.MyDailyFeedProfileSaved', N'U') IS NULL
    CREATE TABLE dbo.MyDailyFeedProfileSaved (
        ProfileId int           NOT NULL CONSTRAINT PK_MyDailyFeedProfileSaved PRIMARY KEY,
        Articles  nvarchar(max) NOT NULL CONSTRAINT DF_MyDailyFeedProfileSaved_Articles DEFAULT N'[]',
        CONSTRAINT FK_MyDailyFeedProfileSaved_Profile FOREIGN KEY (ProfileId)
            REFERENCES dbo.MyDailyFeedProfiles(Id) ON DELETE CASCADE,
        CONSTRAINT CK_MyDailyFeedProfileSaved_Json CHECK (ISJSON(Articles) = 1)
    );

IF OBJECT_ID(N'dbo.MyDailyFeedProfileTvFollows', N'U') IS NULL
    CREATE TABLE dbo.MyDailyFeedProfileTvFollows (
        ProfileId        int       NOT NULL,
        TvTrackedSeriesId int      NOT NULL,
        FollowedAt       datetime2 NOT NULL CONSTRAINT DF_MyDailyFeedProfileTvFollows_At DEFAULT SYSUTCDATETIME(),
        CONSTRAINT PK_MyDailyFeedProfileTvFollows PRIMARY KEY (ProfileId, TvTrackedSeriesId),
        CONSTRAINT FK_MyDailyFeedProfileTvFollows_Profile FOREIGN KEY (ProfileId)
            REFERENCES dbo.MyDailyFeedProfiles(Id) ON DELETE CASCADE,
        CONSTRAINT FK_MyDailyFeedProfileTvFollows_Series FOREIGN KEY (TvTrackedSeriesId)
            REFERENCES dbo.TvTrackedSeries(ID) ON DELETE CASCADE
    );

IF OBJECT_ID(N'dbo.MyDailyFeedProfileChannelFollows', N'U') IS NULL
    CREATE TABLE dbo.MyDailyFeedProfileChannelFollows (
        ProfileId          int       NOT NULL,
        YoutubeVideoFeedId int       NOT NULL,
        FollowedAt         datetime2 NOT NULL CONSTRAINT DF_MyDailyFeedProfileChannelFollows_At DEFAULT SYSUTCDATETIME(),
        CONSTRAINT PK_MyDailyFeedProfileChannelFollows PRIMARY KEY (ProfileId, YoutubeVideoFeedId),
        CONSTRAINT FK_MyDailyFeedProfileChannelFollows_Profile FOREIGN KEY (ProfileId)
            REFERENCES dbo.MyDailyFeedProfiles(Id) ON DELETE CASCADE,
        CONSTRAINT FK_MyDailyFeedProfileChannelFollows_Feed FOREIGN KEY (YoutubeVideoFeedId)
            REFERENCES dbo.YoutubeVideoFeed(ID) ON DELETE CASCADE
    );

-- Household channels already turned on for every profile (decision 11). A row here means "rolled out"; removing a
-- profile's follow afterwards is that profile turning the channel off, and the rollout never re-adds it.
IF OBJECT_ID(N'dbo.MyDailyFeedChannelRollouts', N'U') IS NULL
    CREATE TABLE dbo.MyDailyFeedChannelRollouts (
        YoutubeVideoFeedId int       NOT NULL CONSTRAINT PK_MyDailyFeedChannelRollouts PRIMARY KEY,
        RolledOutAt        datetime2 NOT NULL CONSTRAINT DF_MyDailyFeedChannelRollouts_At DEFAULT SYSUTCDATETIME(),
        CONSTRAINT FK_MyDailyFeedChannelRollouts_Feed FOREIGN KEY (YoutubeVideoFeedId)
            REFERENCES dbo.YoutubeVideoFeed(ID) ON DELETE CASCADE
    );

IF OBJECT_ID(N'dbo.MyDailyFeedContentRequests', N'U') IS NULL
    CREATE TABLE dbo.MyDailyFeedContentRequests (
        Id                 int IDENTITY(1,1) NOT NULL CONSTRAINT PK_MyDailyFeedContentRequests PRIMARY KEY,
        ProfileId          int            NOT NULL,
        Kind               nvarchar(16)   NOT NULL,      -- youtube | news
        Name               nvarchar(200)  NOT NULL,
        Link               nvarchar(2048) NULL,
        Note               nvarchar(500)  NULL,
        Status             nvarchar(16)   NOT NULL CONSTRAINT DF_MyDailyFeedContentRequests_Status DEFAULT N'waiting',
        CheckOk            bit            NULL,          -- NULL = not checked yet
        CheckSummary       nvarchar(300)  NULL,          -- "Channel ID found from the link · feed responds"
        ResolvedChannelId  nvarchar(40)   NULL,
        ResolvedFeedUrls   nvarchar(max)  NULL,          -- JSON array of discovered RSS/Atom URLs
        AdminReply         nvarchar(500)  NULL,          -- shown to the requester when declined
        YoutubeVideoFeedId int            NULL,
        SourceId           int            NULL,
        CreatedAt          datetime2      NOT NULL CONSTRAINT DF_MyDailyFeedContentRequests_Created DEFAULT SYSUTCDATETIME(),
        DecidedAt          datetime2      NULL,
        CONSTRAINT FK_MyDailyFeedContentRequests_Profile FOREIGN KEY (ProfileId)
            REFERENCES dbo.MyDailyFeedProfiles(Id) ON DELETE CASCADE,
        CONSTRAINT CK_MyDailyFeedContentRequests_Kind CHECK (Kind IN (N'youtube', N'news')),
        CONSTRAINT CK_MyDailyFeedContentRequests_Status CHECK (Status IN (N'waiting', N'added', N'declined'))
    );

IF OBJECT_ID(N'dbo.MyDailyFeedHousehold', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.MyDailyFeedHousehold (
        Id                  int          NOT NULL CONSTRAINT PK_MyDailyFeedHousehold PRIMARY KEY,
        DefaultPickerMode   nvarchar(16) NOT NULL CONSTRAINT DF_MyDailyFeedHousehold_Mode DEFAULT N'every-visit',
        ForgetDevicesDays   int          NULL,               -- NULL = never
        RequestsSeenAt      datetime2    NULL,               -- admin's "Mark all seen"
        BaseSettings        nvarchar(max) NOT NULL CONSTRAINT DF_MyDailyFeedHousehold_Base DEFAULT N'{}',  -- base edition (decision 12)
        CONSTRAINT CK_MyDailyFeedHousehold_Id CHECK (Id = 1),
        CONSTRAINT CK_MyDailyFeedHousehold_Base CHECK (ISJSON(BaseSettings) = 1)
    );
    INSERT dbo.MyDailyFeedHousehold(Id) VALUES (1);
END;

IF COL_LENGTH(N'dbo.YoutubeVideoFeed', N'IsHouseholdPick') IS NULL
    ALTER TABLE dbo.YoutubeVideoFeed ADD IsHouseholdPick bit NOT NULL
        CONSTRAINT DF_YoutubeVideoFeed_HouseholdPick DEFAULT 0 WITH VALUES;

COMMIT TRANSACTION;
```

> The wrong-PIN limit is a fixed 5 (decision 2), and anyone can create a profile (decision 10), so neither is a
> household setting.

### `db/021_seed_first_profiles.sql`

Creates your profile, moves today's shared Saved list to you, and makes **everyone** (just you, at this point) follow
everything already tracked, so nothing disappears on day one. Change the name and color first. The profile is created
without a PIN (`PinHash` NULL), so the first time you open it you're asked to choose one (2.3). Following the build
order, you create your profile with its PIN in admin at stage A1 first; `021` then skips creating one and uses
yours.

```sql
SET XACT_ABORT ON;
BEGIN TRANSACTION;

IF NOT EXISTS (SELECT 1 FROM dbo.MyDailyFeedProfiles)
    INSERT dbo.MyDailyFeedProfiles (Name, Color, SortOrder) VALUES (N'Jason', '#0051BA', 1);

DECLARE @first int = (SELECT MIN(Id) FROM dbo.MyDailyFeedProfiles);

IF NOT EXISTS (SELECT 1 FROM dbo.MyDailyFeedProfileSaved WHERE ProfileId = @first)
    INSERT dbo.MyDailyFeedProfileSaved (ProfileId, Articles)
    SELECT @first, ISNULL((SELECT Articles FROM dbo.MyDailyFeedSavedList WHERE Id = 1), N'[]');

INSERT dbo.MyDailyFeedProfileTvFollows (ProfileId, TvTrackedSeriesId)
SELECT @first, t.ID FROM dbo.TvTrackedSeries t
WHERE t.Enabled = 1
  AND NOT EXISTS (SELECT 1 FROM dbo.MyDailyFeedProfileTvFollows f WHERE f.ProfileId = @first AND f.TvTrackedSeriesId = t.ID);

INSERT dbo.MyDailyFeedProfileChannelFollows (ProfileId, YoutubeVideoFeedId)
SELECT @first, y.ID FROM dbo.YoutubeVideoFeed y
WHERE NOT EXISTS (SELECT 1 FROM dbo.MyDailyFeedProfileChannelFollows f WHERE f.ProfileId = @first AND f.YoutubeVideoFeedId = y.ID);

-- Every channel that exists today is a household channel (decision 11) and has just been rolled out to @first.
INSERT dbo.MyDailyFeedChannelRollouts (YoutubeVideoFeedId)
SELECT y.ID FROM dbo.YoutubeVideoFeed y
WHERE NOT EXISTS (SELECT 1 FROM dbo.MyDailyFeedChannelRollouts r WHERE r.YoutubeVideoFeedId = y.ID);

COMMIT TRANSACTION;
```

> **Keep `MyDailyFeedSavedList`** until the release is proven. Rolling back the web container then still finds the
> old list. Drop it in a later script.

> **Column names:** the EF models for `TvTrackedSeries` and `YoutubeVideoFeed` map their keys as `[Column("ID")]`.
> Confirm the actual SQL column names match before running 020.

### EF Core models

Add to `Data/Models/Profiles.cs` and register the DbSets in `WebScraperContext.Partial.cs`, alongside the others:

```csharp
namespace myNewspaper.Web.Data.Models;

[Table("MyDailyFeedProfiles")]
public class Profile
{
    public int Id { get; set; }
    [MaxLength(40)] public string Name { get; set; } = "";
    [MaxLength(7)] public string Color { get; set; } = "#0051BA";
    public bool Hidden { get; set; }
    public int SortOrder { get; set; }
    public string? PinHash { get; set; }
    public int FailedPinCount { get; set; }
    public DateTime? LockedAt { get; set; }
    public Guid? LockedByDeviceId { get; set; }
    public string Settings { get; set; } = "{}";
    public DateTime? BrowserImportAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? LastReadAt { get; set; }
}

[Table("MyDailyFeedProfileDevices")]
public class ProfileDevice
{
    public Guid Id { get; set; }
    public int? ProfileId { get; set; }
    public byte[] TokenHash { get; set; } = [];
    [MaxLength(100)] public string Label { get; set; } = "";
    [MaxLength(300)] public string? UserAgent { get; set; }
    [MaxLength(16)] public string PickerMode { get; set; } = "every-visit";
    public DateTime CreatedAt { get; set; }
    public DateTime LastSeenAt { get; set; }
    public DateTime? RevokedAt { get; set; }
    public Profile? Profile { get; set; }
}

[Table("MyDailyFeedProfileTvFollows"), PrimaryKey(nameof(ProfileId), nameof(TvTrackedSeriesId))]
public class ProfileTvFollow { public int ProfileId { get; set; } public int TvTrackedSeriesId { get; set; } public DateTime FollowedAt { get; set; } }

[Table("MyDailyFeedProfileChannelFollows"), PrimaryKey(nameof(ProfileId), nameof(YoutubeVideoFeedId))]
public class ProfileChannelFollow { public int ProfileId { get; set; } public int YoutubeVideoFeedId { get; set; } public DateTime FollowedAt { get; set; } }

[Table("MyDailyFeedChannelRollouts")]
public class ChannelRollout { [Key] public int YoutubeVideoFeedId { get; set; } public DateTime RolledOutAt { get; set; } }

[Table("MyDailyFeedContentRequests")]
public class ContentRequest
{
    public int Id { get; set; }
    public int ProfileId { get; set; }
    [MaxLength(16)] public string Kind { get; set; } = "youtube";   // youtube | news
    [MaxLength(200)] public string Name { get; set; } = "";
    [MaxLength(2048)] public string? Link { get; set; }
    [MaxLength(500)] public string? Note { get; set; }
    [MaxLength(16)] public string Status { get; set; } = "waiting";  // waiting | added | declined
    public bool? CheckOk { get; set; }
    [MaxLength(300)] public string? CheckSummary { get; set; }
    [MaxLength(40)] public string? ResolvedChannelId { get; set; }
    public string? ResolvedFeedUrls { get; set; }
    [MaxLength(500)] public string? AdminReply { get; set; }
    public int? YoutubeVideoFeedId { get; set; }
    public int? SourceId { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? DecidedAt { get; set; }
    public Profile? Profile { get; set; }
}

[Table("MyDailyFeedHousehold")]
public class Household
{
    public int Id { get; set; } = 1;
    [MaxLength(16)] public string DefaultPickerMode { get; set; } = "every-visit";
    public int? ForgetDevicesDays { get; set; }
    public DateTime? RequestsSeenAt { get; set; }
    public string BaseSettings { get; set; } = "{}";   // same shape as Profile.Settings
}
```

Add `public bool IsHouseholdPick { get; set; }` to `YoutubeVideoFeed`. `MyDailyFeedProfileSaved` can stay raw SQL
inside `SharedArticles`, which already works that way.

### Profile settings JSON

One document per profile, in `Profiles.Settings`. The base edition (`Household.BaseSettings`) uses the same shape.
Missing keys mean the built-in default, so old rows never break.

```json
{
  "theme": "system",                    // system | sun | light | dark
  "colorScheme": "ribbon",
  "home": { "order": ["lead","teams","weather"], "hidden": ["boxoffice"], "teamsOnly": false },
  "teams": ["big12:kansas", "big12:kansas-state"],
  "services": [8, 15, 337],
  "settingsOrder": [],
  "headlinesDefault": null,             // category id, null = site default
  "mutedPublishers": [],
  "weatherCity": null                   // null = household default (Kansas City)
}
```

Keep the values exactly as the JS stores them today, so `personal.js`, `watch.js` and friends need small changes,
not rewrites. Copy the real formats from each file's current `localStorage` value; the sample above is illustrative.

**Built-in defaults move to one place.** Today each script hard-codes its own default (the home order in
`personal.js`, the settings order in `settings-order.js`, `system` and `ribbon` in `theme-init.js`). Move them into a
single server-side `SiteDefaults` (C#) that the layout renders into the page, and have the scripts read that instead
of their own copies. The scripts keep a minimal fallback only for when the page data is missing.

**Where each value comes from:**
- **Reading the site:** the profile's own key if it has one, otherwise `SiteDefaults`. The base edition is **not**
  consulted here, so changing it never changes an existing profile.
- **Creating a profile:** `Profiles.Settings` starts as a copy of `Household.BaseSettings`. Setup steps 2 to 4 are
  prefilled from it, and whatever the person submits is saved.

### Base edition: recommended starting values

The admin sets these in the base edition editor (6.4) at stage A1; 020 creates it empty (`{}`), which means
`SiteDefaults` until then.

| Setting | Base edition | Why |
|---|---|---|
| Light/dark mode | `system` (follow the device) | Already the site default |
| Color scheme | `ribbon` | The site's own look; themed schemes are personal |
| Home sections | All 14 on, in today's order, "only my teams" off | A hidden section is invisible, so new people would never find it |
| Teams | The household's shared local teams, picked with the existing team picker | With "only my teams" off, teams only highlight, never filter |
| Services | The services the household subscribes to, picked with the existing picker | "Where to watch" then matches what's available at home |
| Weather city | Kansas City | The household default |
| Headlines default, muted publishers | Site default, none muted | Personal choices |
| Settings section order | Default | Nothing to gain |
| TV shows | None | TV follows are personal; not part of settings |

---

## Phase 2 — Knowing who's reading

### 2.1 The device cookie

One long-lived, HttpOnly cookie identifies the **device**, not the person: `mnp.device`, holding
`{deviceId}.{secret}` protected with Data Protection. The server hashes the secret and matches it against
`ProfileDevices.TokenHash`, so a copied database can't mint cookies. The device row says which profile it's on.

Data Protection already uses the persistent `/keys` volume (see [Repo check](#repo-check-october-27)), so device
cookies survive container rebuilds.

A second, **session** cookie (`mnp.gate`, no expiry date, so the browser drops it on close) records that the gate was
passed and when. That's what "every visit" and "once a day" check:

| Picker mode | Gate shows when |
|---|---|
| `every-visit` | `mnp.gate` is missing (a new browser session) |
| `daily` | `mnp.gate` is missing, or its date isn't today in Central time |
| `until-signout` | the device has no profile, or the profile was signed out or hidden |

> **iPad / PWA note:** iOS often keeps a home-screen web app's session alive for days, so "every visit" behaves more
> like "when the app restarts". That's acceptable; say so in the Settings help text if anyone asks.

### 2.2 `ProfileContext` (scoped service) and the gate middleware

```csharp
public sealed class ProfileContext
{
    public Profile? Profile { get; internal set; }
    public ProfileDevice? Device { get; internal set; }
    public bool HasProfile => Profile is not null;
    public int Id => Profile?.Id ?? throw new InvalidOperationException("No profile selected.");
}
```

`ProfileGateMiddleware` runs after `UseAuthentication()` (and after `UseRouting()`, so it can read endpoint metadata).
It takes `IDbContextFactory<WebScraperContext>`, because there is no scoped context. It does two separate jobs,
**identify** and **gate**:

1. **Pass straight through** (neither identify nor gate): static assets (detect by endpoint metadata, because
   `UseAuthentication()` runs before `MapStaticAssets()` and the middleware sees every request), `/_framework/*`,
   `/_blazor*`, `/healthz`, `service-worker.js`, `manifest.webmanifest`, `offline.html`.
2. **Identify the device.** This runs on every other request, including `/editions` and `/profiles/*`. Read
   `mnp.device`. If it's missing or invalid, create a device row (no profile) and set the cookie, but **only for page
   requests** (`GET`, `Accept: text/html`) and the `/profiles/*` POSTs. Bots, health probes and API calls without a
   cookie never create rows.
3. **Load the device's profile** into `ProfileContext`. A revoked device, or a hidden or deleted profile, counts as no
   profile.
4. **Gate exemptions** (identified, but never sent to the picker): `/editions`, `/profiles/*`, `/account/*`,
   `/login`, `/admin*`.
5. **Gate everything else** (table above). For page requests, redirect to `/editions?returnUrl=…`. For `/api/*`,
   `/feed/*`, `/home/*`, `/scoreboard/*` and every other non-page endpoint, return **401** with
   `{ "gate": "/editions" }`. The JS shared fetch helper sends the browser there. With no Guest there is no
   anonymous path: **anything not exempt in steps 1 or 4 requires a profile.**
6. Otherwise update `LastSeenAt` (at most once a minute) and the profile's `LastReadAt`, then continue.

Blazor interactive pages get the same `ProfileContext` through a cascading value set in `PublicLayout` from the HTTP
request, or by reading `IHttpContextAccessor` once in `OnInitialized`. Don't read cookies from inside a circuit later.
Interactive components that write, like `SeriesFollowForm`, must capture the profile ID when they start.

> **Admin pages are not gated.** An admin can sign in and manage profiles from a device that hasn't picked one.

### 2.3 Endpoints (`Endpoints/ProfileEndpoints.cs`)

All POSTs are forms or `fetch` calls with the existing antiforgery header (`X-Saved-CSRF`).

| Method | Path | Does |
|---|---|---|
| GET | `/editions` | Picker page (Blazor SSR, `PublicLayout` without the nav) |
| POST | `/profiles/{id}/open` | Body: `pin`. Checks lock and PIN, sets the device's profile and `mnp.gate`, redirects to `returnUrl` |
| GET/POST | `/profiles/{id}/new-pin` | Choose a PIN, only for a profile that has none yet (the one 021 seeds) |
| POST | `/profiles/new` | Setup flow submit: name, color, PIN (required), teams, services, home sections. Then opens it |
| POST | `/profiles/signout` | Clears the device's profile and `mnp.gate`, redirects to `/editions` |
| GET | `/api/me` | Current profile: name, color, settings JSON |
| PUT | `/api/me/settings` | Merge-patch the settings JSON (top-level keys only; validate each) |
| POST | `/api/me/import-browser` | One-time import of old `localStorage` values (2.5) |

**PIN check** — use `PasswordHasher<Profile>` from `Microsoft.AspNetCore.Identity` (no Identity database needed):

```csharp
static async Task<IResult> OpenAsync(int id, [FromForm] string? pin, [FromForm] string? returnUrl, HttpContext http,
    ProfileContext ctx, IDbContextFactory<WebScraperContext> factory, PasswordHasher<Profile> hasher)
{
    // The middleware identifies the device on /profiles/* too, so this only fails without cookies.
    if (ctx.Device is null) return Results.BadRequest();

    await using var db = await factory.CreateDbContextAsync(http.RequestAborted);
    var p = await db.Profiles.FindAsync(id);
    if (p is null || p.Hidden) return Results.NotFound();
    const int MaxWrongPins = 5;                      // decision 2: fixed, not a household setting

    // Locked profiles stay locked until the admin unlocks or resets them; the admin alert counts them (5.4).
    if (p.LockedAt is not null) return Results.Json(new { locked = true }, statusCode: 423);

    // No PIN yet (only the profile 021 seeds): choose one before reading.
    if (p.PinHash is null)
        return Results.LocalRedirect($"/profiles/{p.Id}/new-pin?returnUrl={Uri.EscapeDataString(returnUrl ?? "/")}");

    if (string.IsNullOrEmpty(pin) || hasher.VerifyHashedPassword(p, p.PinHash, pin) == PasswordVerificationResult.Failed)
    {
        p.FailedPinCount++;
        if (p.FailedPinCount >= MaxWrongPins)
        {
            p.LockedAt = DateTime.UtcNow;
            p.LockedByDeviceId = ctx.Device.Id;
            p.FailedPinCount = 0;
        }
        await db.SaveChangesAsync();
        return p.LockedAt is null
            ? Results.Json(new { wrongPin = true, triesLeft = MaxWrongPins - p.FailedPinCount }, statusCode: 401)
            : Results.Json(new { locked = true }, statusCode: 423);
    }

    p.FailedPinCount = 0;
    var device = await db.ProfileDevices.SingleAsync(d => d.Id == ctx.Device.Id);
    device.ProfileId = p.Id;
    await db.SaveChangesAsync();
    GateCookie.Set(http, DateTime.UtcNow);           // mnp.gate, session cookie
    return Results.LocalRedirect(AdminAuth.SafeReturnUrl(returnUrl));   // reuse the open-redirect guard
}
```

Register with `builder.Services.AddSingleton<PasswordHasher<Profile>>();`. PINs are exactly 4 digits; validate
server-side. Note that `AdminAuth.SafeReturnUrl` falls back to `/admin`. Give profiles their own fallback of `/`.

**`/profiles/{id}/new-pin`** only works while `PinHash` is NULL, which only happens if 021 has to create a profile.
New profiles set their PIN in setup step 1, and admin-created profiles get theirs from the admin. Create your own
profile with its PIN in admin at stage A1, before `021` runs; then `021` uses that profile and never creates a
PIN-less one.

**Admin reset** (decision 2) never goes through this page. The admin sets the new PIN on Manage profile (6.4); the
person can change it later in Settings.

**Locking:** the lock counts wrong PINs per profile from any device. Five wrong guesses from anyone lock that person
out until the admin acts. The per-device limit in
[Proposed security hardening](#proposed-security-hardening) item 4 would reduce that, but it's proposed, not decided;
settle it before U1. `LockedByDeviceId` tells the admin which device did it.

### 2.4 No profile, no site

There is no Guest and no anonymous reading. A device without a profile, or one whose `mnp.gate` has expired for its
picker mode, sees only the exemptions in 2.2: the picker, the profile endpoints, admin sign-in and static files.
Everything else redirects or returns 401. Because the gate is the only boundary, Phase 7 sweeps every public route
without a profile.

### 2.5 Moving the browser settings in

Add one script, `wwwroot/profile.js`, loaded before the others:

1. The layout renders the current profile into the page as JSON:
   `<script id="mnp-profile" type="application/json">{…/api/me payload…}</script>`.
2. `profile.js` exposes `window.myNewspaperProfile = { get(key, fallback), set(key, value) }`. `set` writes
   through `PUT /api/me/settings` (debounced 400 ms) and keeps an in-memory copy.
3. **One-time import:** if the profile's `browserImportAt` is null **and** this browser has any of the old keys, POST
   them to `/api/me/import-browser`. The server fills only keys the profile doesn't have yet. Then remove the old
   keys. The existing Saved-list import with a receipt key (`mydailyfeed-saved-import-v1`) is the pattern to copy.
4. Change the readers and writers to use `myNewspaperProfile` instead of `localStorage`:
   - `personal.js`: `PREFS` and `TEAMS`
   - `public.js`: theme (≈ line 85/110), teams (≈ 821), color scheme (≈ 934), saved-list import
   - `scoreboard.js`: teams
   - `settings-order.js`: section order
   - `watch.js`: services

**Theme without a flash:** `theme-init.js` runs before paint and can't wait for a fetch. Have the layout render
`data-theme-mode="{mode}"` and `data-color-scheme="{scheme}"` on `<html>` from the profile. `theme-init.js` then
reads the mode from that attribute instead of `localStorage`, and still resolves `system`/`sun` to light or dark.
On the picker (no profile yet) the attributes are absent and `theme-init.js` falls back to the system setting.

---

## Phase 3 — Per-profile Saved

`SharedArticles` keeps its API (`/api/saved…`) and its 500-item limit, but reads and writes
`MyDailyFeedProfileSaved WHERE ProfileId = @current` instead of `MyDailyFeedSavedList WHERE Id = 1`:

- Add a `profileId` argument to every method. `SharedArticles` is registered as a singleton, so it can't take
  `ProfileContext` itself. `SavedEndpoints` reads `ProfileContext.Id` and passes it in.
- Create the row on first write (`MERGE` or insert-if-missing), so new profiles start empty.
- `MyDailyFeedSavedImports` (the legacy browser import receipt) gains nothing; the old one-time import goes to whichever
  profile runs it first. That's fine, because 021 already copied the shared list to your profile.

---

## Phase 4 — Follows, and keeping them private

### 4.1 TV series

- **Adding** (Make it yours → Shows & channels, and **Track this show** on Where to watch): reuse
  `SeriesFollowForm`'s TVmaze search. On pick:
  1. Find or insert the `TvTrackedSeries` row by `TvmazeShowId` (today's `TvQuery.FollowAsync`). If it exists but is
     disabled, enable it.
  2. Insert the follow row for the current profile.
  3. Every result button says **Follow**, whether or not the show already existed (decision 8).
- **Fix `SeriesFollowForm`'s "Following" state.** Today it marks a result "Following" and disables it when *any*
  enabled `TvTrackedSeries` row matches (`followed` is built at line 57 and used at line 26). Build `followed` from
  **the current profile's** follow rows only. The admin TV page keeps its list of tracked series, which the admin
  needs to run the scraper; it never shows who follows what.
- **Unfollowing:** delete the follow row. If no follow rows remain for that series, call
  `TvQuery.SetEnabledAsync(id, false)`, the same thing "Stop following" does in Admin today. History is kept.
- **Admin Delete** (`TvQuery.RemoveAsync`) deletes the `TvTrackedSeries` row, and the cascade removes every profile's
  follow of it. The Delete confirmation says "Anyone following it will lose it", with no count or names.
- **TV calendar and Tonight on TV:** add `AND EXISTS (SELECT 1 FROM MyDailyFeedProfileTvFollows f WHERE
  f.ProfileId = @p AND f.TvTrackedSeriesId = t.ID)` to the `enabled` query in `TvQuery.GetAsync` and
  `GetOnDateAsync`. That covers every caller: Home, `SectionAgenda`, `EntertainmentFront`, the TV page, and
  `SportsFront` via `TodayTonight`.
- **`/tv/track`** (public page): keep it, but have it follow for the current profile. Its intro text
  ("Add a show to the shared tracked-series list") changes to "Add a show to your TV calendar".

### 4.2 YouTube channels

- **The YouTube page and Home videos** (`VideosQuery`) filter by the profile's followed feeds. That covers
  `GetChannelsAsync`, `GetCategoriesAsync`, `GetVideosAsync`, `GetHomeVideosAsync` and `GetLatestPerChannelAsync`.
  Category tabs and counts come from followed feeds only.
- **Household picks:** a switch on Admin → YouTube feeds → Edit, "Suggest to everyone" (`IsHouseholdPick`). Make it
  yours lists picks the profile doesn't follow yet as one-tap **+** chips.
- **Unfollowing** only deletes the follow row. Feeds are never disabled automatically, because the scraper reads them all
  and the admin owns that list.
- **New channels** arrive two ways:
  - **Through a request** (Phase 5): followed only by the requester.
  - **Household channels** (decision 11): the scraper or the admin adds them, and they're turned on for everyone.
- **Rolling out household channels** (`Profiles/ChannelRollout.cs`):
  - A hosted service runs every 15 minutes, and once right after the admin saves a feed.
  - It finds `YoutubeVideoFeed` rows with no `MyDailyFeedChannelRollouts` row and no `MyDailyFeedContentRequests` row
    pointing at them.
  - For each one, in one transaction, it inserts a follow for every profile and then the rollout row.
- **Request approvals must not be rolled out.** The admin's approve inserts the `YoutubeVideoFeed` row, the follow,
  and sets the request's `YoutubeVideoFeedId` **in one transaction**. Otherwise the rollout could see the new feed in
  between and turn it on for everyone, which would leak the request.
- **New profiles** start with every household channel turned on: setup inserts a follow for each feed in
  `MyDailyFeedChannelRollouts`.
- **Turning a household channel off** deletes that profile's follow row. The rollout row stays, so it's never turned
  back on automatically. **Turn on** re-inserts the follow. The feed is never deleted or disabled.

### 4.3 The privacy rule, in code

Put every "what does someone follow" read behind one class, `FollowQuery`, with two kinds of method:

- `ForProfile(int profileId)`: used by public pages, always the current profile from `ProfileContext`.
- There is **no** admin method (decision 3). Admin pages never read follows, Saved or settings. The only per-profile
  data admin pages read is in `MyDailyFeedProfiles` (name, color, hidden, lock), `MyDailyFeedProfileDevices`, and
  `MyDailyFeedContentRequests`.

`TvQuery` and `VideosQuery` are already the only public readers of TV and YouTube data. Make their public methods
take the viewing profile as a required parameter. Then the compiler finds every caller, and none can forget the
filter. Rules:

- **Filter first.** Apply the follow filter before the `show`, `channel` and `search` parameters, inside the query
  class. Today `TvQuery.GetAsync(show, search)` and `VideosQuery.GetVideosAsync(channelId, search)` accept any ID or
  search term across everything collected.
- **Unfollowed looks like unknown.** `/videos?channel=UC…` or `/tv?show=N` for something the profile doesn't follow
  returns exactly what an ID that doesn't exist returns. Never a 403, which would confirm it exists.
- **No profile IDs from outside.** There must be **no** public endpoint that takes a profile ID as a parameter, and no
  public query that joins follows without `@currentProfile`.
- **Test it.** Add a test that runs each public page's query with profile A and asserts that none of profile B's
  follows appear.

**Edition picker previews** may show other profiles' lead headline, team games, and saved/new counts, but **no show or
channel names**: "New for you: 3 episodes · 12 videos", never "Hanging by a Wire · Paramount+".

---

## Phase 5 — Requests (YouTube channels and news sources)

### 5.1 Submitting (`POST /api/me/requests`)

Body: `{ kind: "youtube" | "news", name, link?, note? }`. Rules:

- Max 5 waiting requests per profile (stops accidental floods).
- Insert with `Status = 'waiting'`, then queue the request ID for the **checker** (5.2) and return at once.
- **No auto-approve** (decision 7). Every request, YouTube or news, waits for the admin, even when the channel or
  source is already collected. The checker only fills in details for the admin.

`GET /api/me/requests` lists only the current profile's requests: name, kind, status, date and `AdminReply` when
declined.

### 5.2 The checker (`Requests/RequestChecker.cs`, a hosted service)

Use a `Channel<int>` queue and one background loop. The queue lives in memory, so on startup re-queue every request
with `Status = 'waiting'` and `CheckOk IS NULL`. It uses a named `HttpClient` ("request-checker", 10 s timeout,
a polite `User-Agent` like the other clients), and every outcome is written to `CheckOk` and `CheckSummary`:

**YouTube**

1. From the link:
   - `youtube.com/channel/UC…` → the ID is in the URL.
   - `youtube.com/@handle`, `/c/…`, `/user/…` → GET the page and read `<link rel="canonical" href=".../channel/UC…">`
     or `<meta itemprop="identifier" content="UC…">`.
   - `youtube.com/watch?v=…` or `youtu.be/…` → GET the video page and read `"channelId":"UC…"`.
   - No link → `CheckOk = false`, "No link given · needs a channel ID".
2. GET `https://www.youtube.com/feeds/videos.xml?channel_id={id}`. If it parses as Atom with entries:
   "Channel ID found from the link · feed responds".
3. If the channel ID is already a `YoutubeVideoFeed`, say "Already collected". Only the admin sees this; the requester
   just sees Waiting.

> Scraping YouTube's HTML can break when they change the page. Treat any failure as "needs a channel ID" and let
> the admin fill it in by hand. Never block the request on it.

**News source**

1. GET the link. If it's already RSS/Atom (parses with `System.ServiceModel.Syndication.SyndicationFeed`, which needs
   the NuGet package `System.ServiceModel.Syndication`), use it directly.
2. Otherwise read `<link rel="alternate" type="application/rss+xml|application/atom+xml" href>` tags and try `/feed`,
   `/rss` and `/feed.xml`. Store what works in `ResolvedFeedUrls`: "2 feeds found on ljworld.com".
3. If a found URL already matches `Sources.Url` (same comparison the unique index uses), say "Already a source", and
   the admin can approve without adding anything. Only the admin sees this; the requester just sees the status.

**SSRF guard:**
- Only allow `http` and `https`.
- Refuse private, loopback and link-local addresses, and anything on your Tailscale range (`100.64.0.0/10`). The
  server sits inside your network; a request link must not be able to probe it.
- Check the address **at connect time**, in `SocketsHttpHandler.ConnectCallback`, not in a DNS lookup beforehand. An
  earlier lookup can be changed by the time the client connects.
- Turn off automatic redirects and check every redirect target the same way.

### 5.3 Admin: Requests page (`/admin/requests`)

Matches the Admin · Requests artboard:

- Filter: **Waiting / Added / Declined / All** with counts. The list is newest first: kind tag, name, requester
  (initial and color), when, note.
- **YouTube selected:** Channel ID (prefilled from `ResolvedChannelId`), display name, YouTube category, read-only
  feed address, **Test feed** (runs the check again on demand and shows the count and newest title), **Follow it for
  {requester}** (on by default), **Add channel and approve**, and **Decline** with a reason.
  Approve: insert `YoutubeVideoFeed` → insert the follow → `Status = 'added'`, `DecidedAt = now`.
- **Already-collected YouTube channel** (the checker said "Already collected"): skip the feed fields and show
  **Approve and follow for {requester}**, which inserts only the follow, and **Decline**.
- **News source selected:** feed URL (pick from `ResolvedFeedUrls`, or **Find feeds**), display name, category (with
  its hub shown), **Test feed**, **Add source and approve**. Approve: insert `Sources` with `Enabled = 1` → `Status =
  'added'`, `SourceId`. News sources aren't followed; the source simply appears for everyone in its section.
- Use the admin redesign's list/editor patterns (`docs/admin-redesign-handoff.md`).

### 5.4 Admin alert in the site header

In `PublicLayout.razor`, inside `<AuthorizeView>` (admin signed in), add a **Requests** item right after **Saved**:
desktop as a text item with an inbox icon and a red count, phone as a 44 × 44 icon button with a yellow count badge.

- **Count** = waiting requests with `CreatedAt > Household.RequestsSeenAt`, **plus every locked profile**
  (`LockedAt IS NOT NULL`), plus a dot when `ScraperHealth` reports a problem.
- Clicking opens the panel (desktop popover, phone bottom sheet). It's the quick-look list from the artboard: kind,
  requester, name, note, the checker's `CheckSummary`, and one button that says what's next (**Review and approve /
  Pick a feed / Fill in details / Approve**), linking to `/admin/requests?id={id}`, plus **Decline…**.
- **Locked profiles come first** in the panel: "Alex is locked · 5 wrong PINs on Living room iPad · 9:14 AM", with
  **Unlock** and **Reset PIN**, and a link to Manage profile.
- **Mark all seen** sets `RequestsSeenAt = now`. Opening the panel doesn't mark them seen; only that button does.
  Locked profiles keep counting until they're unlocked or reset; **Mark all seen** doesn't clear them.
- Load the panel's data from `GET /admin/api/requests/summary` (admin-authorized) when it opens, not on every page.
  Show the count in the layout through a cheap `COUNT(*)`.

---

## Phase 6 — Pages

### 6.1 Edition picker (`/editions`)

- One column per visible profile, in `SortOrder` order.
- Each column shows: initial tile in the profile's color, "{Name}'s edition", a kicker ("Last read 7:12 AM"), the lead
  headline for their first home section, and two lines. The lines are team games, "New for you: N episodes · N
  videos" and unread saved count, **no show or channel names** (4.3).
- Button "Read {Name}'s edition" opens the PIN dialog (every profile has a PIN): a bottom sheet on phones, a centered
  dialog on desktop, with a 4-dot display and number pad. 401 shakes the dots and shows "Wrong PIN · N tries left";
  423 shows "Locked. The admin has been told and will unlock it." A locked profile's column shows a lock badge.
- Footer: **+ Start a new edition** (always shown, decision 10) and **Manage profiles** (links to `/admin/profiles`;
  the admin sign-in protects it). No standard-edition link.
- **With no visible profiles** (all hidden, or a fresh install before 021), show only **+ Start a new edition** and
  the **Manage profiles** link.
- The page works without JavaScript: forms post to `/profiles/{id}/open`. The PIN pad is an enhancement over a plain
  `inputmode="numeric"` field.

### 6.2 New profile setup (`/profiles/new`)

Four steps, one form, with Back/Continue and a progress bar:

1. **Name** (unique, at most 40 characters) + **color** (6 swatches) + **4-digit PIN**, entered twice. The PIN is
   required; Continue stays disabled until it's set.
2. **Teams**, using the same league tabs and buttons as Settings.
3. **Services**, using the popular services list.
4. **Home page sections**: switches plus move-up buttons.

Steps 2 to 4 start prefilled from the base edition (decision 12). Copying another profile's settings would show them
to whoever is creating the new profile (decision 3), so there is no "start from a copy". New profiles also start with
every household channel on (4.2). On phones the Continue button stays at the bottom.

### 6.3 Make it yours (`/settings`, replacing `MyTeams.razor`'s content)

Sections, in this order, with the side index:

1. **Profile:** name, color, **Change PIN** (current PIN, then the new one twice, on the same 4-digit pad; there's no
   way to turn the PIN off), "On this device, show the profile picker" (`ProfileDevices.PickerMode` for *this*
   device).
2. **What's yours and what's shared:** fixed explanatory card.
3. **Home page:** order and switches (today's `personal.js` data).
4. **Light and dark mode**, **Color scheme**, **Reading** (Headlines default, **Weather and Local city**, muted
   publishers, **Request a news source →**), **My services**, **My Teams**: today's Settings, saved to the profile.
5. **Shows & channels:**
   - TV series: **Add a show** (TVmaze search, every result says **Follow**), the followed list with **Unfollow**.
   - YouTube channels:
     - **Household channels**: every channel in `MyDailyFeedChannelRollouts`, each with an on/off switch. Showing
       these to everyone is fine, because nobody requested them.
     - **Your requested channels**, with **Unfollow**.
     - **Household picks** chips, and **Request a channel**.
6. **Requests:** the form (YouTube channel / News source toggle, name, optional link, optional note) and **Your
   requests** with Waiting / Added / Declined and the admin's reply.
7. **Household** (only when the admin cookie is present): the Editing menu at the top, profile list with Edit/Remove,
   **Add a profile**. This duplicates part of `/admin/profiles` for convenience; both write the same rows, and it
   shows the same limited information (decision 3).

### 6.4 Admin: Profiles (`/admin/profiles`, `/admin/profiles/{id}`)

From the Admin · Profiles and Manage profile artboards. Add **Household → Profiles** and **Requests** to the admin
sidebar (NavMenu from the admin redesign).

- **List:**
  - **Add a profile**: name, color and a 4-digit PIN, entered twice, all set by the admin. The new profile starts
    from the base edition, like setup does.
  - Lockout banner per locked profile, with the device that caused it, **Unlock** (clears `LockedAt` and
    `FailedPinCount`) and **Reset PIN**.
  - Totals: profiles, remembered devices, locked profiles, waiting requests.
  - Table: name and color, Active/Locked/Hidden, number of devices, waiting requests (linking to `/admin/requests`
    filtered to that profile). No likes, saved counts or reading times (decision 3). Rows have **Manage** and a ⋯
    menu (Unlock, Reset PIN, Sign out all devices, Hide from picker, Delete). Drag order sets `SortOrder`.
  - Household settings: picker default for new devices, forget devices after 90 days.
  - **Base edition** (`/admin/profiles/base`): one page with the same sections and controls as Make it yours →
    Home page, Light and dark mode, Color scheme, Reading, My services and My Teams, including the site's existing
    team and service pickers. Saves to `Household.BaseSettings` with the same per-key validation as
    `PUT /api/me/settings`. The page says: "New profiles start with these settings. Changing them doesn't change
    existing profiles." **Reset to site defaults** empties it.
- **Manage profile:**
  - Name and color.
  - **Unlock** (when locked): clears `LockedAt` and `FailedPinCount`. The PIN stays the same.
  - **Reset PIN**: the admin types the new 4-digit PIN twice and saves. That replaces `PinHash`, clears `LockedAt`
    and `FailedPinCount`, and signs out the profile's other devices. The admin tells the person the new PIN; they can
    change it in Settings. There is no Remove PIN; every profile keeps one (decision 2).
  - **Show on the edition picker** (`Hidden`). Hiding revokes their devices.
  - Remembered devices: label (editable), a short device description from the user agent ("iPad · Safari"), the
    lock source flag, **Sign out** per device, **Sign out everywhere** (`RevokedAt = now`). No last-used times; the
    admin needs the devices to unlock and sign out, not to see when someone reads.
  - **Their requests**: the same list as `/admin/requests`, filtered to this profile.
  - **Not shown** (decision 3): teams, services, follows, Saved, settings, reading times. There is no **Open as** and
    no **Copy settings**.
  - Remove: **Hide from picker**, or **Delete profile…**, which warns "Their settings, Saved list, follows and requests
    will be deleted" without counts. Cascading foreign keys clean up Saved, follows, requests and devices.
- Deleting or hiding the last visible profile is allowed; the picker then shows only **+ Start a new edition** and
  **Manage profiles** (6.1).

---

## Phase 7 — Verify (per AGENTS.md)

- **Widths:** 320, 390, 430, 744, 820, 1024, 1440, in light **and** dark mode.
- **Gate:**
  - A new device sees the picker.
  - **No-profile sweep:** without a profile, request every public page and every non-admin endpoint (`/api/*`,
    `/feed/more`, `/home/*`, `/scoreboard/fragment`, `/local/weather`, `/results`, …). Pages redirect to `/editions`;
    the rest return 401. Build the route list from the endpoint data source, so new routes are covered automatically.
  - Every visit: the picker returns after closing the browser. Once a day: the picker returns the next day (fake the
    clock in a test).
  - Until signout: the picker never returns until **Sign out of {name}**.
  - Hidden profile and revoked device → picker.
  - `/admin`, `/login`, `/healthz` and static files are never redirected.
  - Requests without a device cookie that aren't page GETs (health probes, API calls) create no device rows.
- **PIN:**
  - Correct opens; wrong shows tries left.
  - The 5th wrong locks the profile with no timed unlock: it stays locked after 15 minutes and after a day (fake the
    clock).
  - The header alert counts the lock, the panel and admin banner name the device, and **Mark all seen** doesn't clear
    it.
  - **Unlock** keeps the old PIN. **Reset PIN** sets the admin's new PIN, the old one stops working, the lock clears,
    and the profile's other devices are signed out.
  - New-profile setup can't finish without a PIN; the 021 profile asks for one on first open; Settings has no way to
    remove a PIN, and changing it asks for the current one.
  - PIN fields only accept 4 digits.
- **Settings move:** on a browser that has old `localStorage` values, opening your profile imports them once; a second
  browser gets the same settings from the server; no theme flash on reload; the picker renders in the system theme.
- **Saved:** each profile has its own list; the old shared list is now yours.
- **Base edition:**
  - A new profile's setup steps 2 to 4 are prefilled with the base edition's teams, services and home sections, and
    the new profile reads with those settings.
  - Changing the base edition afterwards leaves existing profiles unchanged and affects only profiles created later.
  - An empty base edition (`{}`, or after **Reset to site defaults**) gives the same site as a fresh browser today.
  - The base edition editor is admin-only, and invalid values are rejected like `PUT /api/me/settings`.
- **Follows:** adding an existing show and a new show both say Follow; `SeriesFollowForm` marks only this profile's
  shows as Following; the TV calendar shows only followed series; the last unfollow disables the tracked series; the
  YouTube page shows only followed channels; household picks appear as chips.
- **Household channels:**
  - A feed added by the scraper or in Admin appears for every profile within 15 minutes, including profiles created
    afterwards.
  - Turning it off hides it for that profile only and survives the next rollout run; turning it on brings it back;
    the feed itself stays enabled.
  - A channel approved from a request appears only for the requester, never for other profiles, even across a rollout
    run.
- **Privacy:**
  - Signed in as profile B, no page, picker preview, search result, suggestion or API response mentions profile A's
    shows, channels or requests. Run the automated test from 4.3.
  - `/videos?channel=` and `/tv?show=` with an ID only A follows return the same result as an ID that doesn't exist.
  - Signed in as admin with no profile open, no admin page or `/admin/api/*` response links A to a show or channel,
    except through a request A sent. The admin's TV series and YouTube feed lists still name everything collected,
    but never who follows it. No admin response shows A's teams, services, Saved items or reading times. The only per-profile data shown is name, color,
    lock and hidden state, devices, and requests. There is no route that opens a profile without its PIN.
- **Requests:**
  - A link with an `@handle`, a video link and a `channel/UC…` link each resolve, or fall back to "needs a channel ID".
  - An already-collected channel or source still waits for the admin (no auto-approve); the requester sees Waiting,
    the admin sees "Already collected" or "Already a source".
  - News feed discovery finds an RSS link on a normal news site.
  - The SSRF guard rejects `http://localhost`, `http://10.x`, `http://100.64.x` (Tailscale) and `file:`, and a
    redirect from a public URL to any of those.
  - A restart with unchecked waiting requests re-queues them.
  - Approve and decline update the requester's list with the reply.
- **Admin alert:** the count matches new waiting requests, **Mark all seen** clears it, the panel opens on desktop
  and phone, it's absent when not signed in as admin, and other profiles never see it.
- **Service worker:** it precaches only `offline.html` and icons and never caches page HTML (checked October 27), so
  no profile data can be served from its cache. Bump `CACHE` (currently `mynewsfeed-pwa-v5`) only if
  `service-worker.js` itself changes.


---

## Proposed, not decided

Raised in the October 27 review. **None of this is agreed**; settle each item before the release it affects.

### Answered October 27

1. **Who can create a profile?** Anyone with the link; the network limits who that is → decision 10.
2. **Profiles without a PIN?** Not allowed; every profile must have a PIN → decision 2.
3. **YouTube auto-approve?** No → decision 7.
4. **News source auto-approve?** No; every request waits for the admin → decision 7.
5. **Wrong PINs?** 5 tries, then locked until the admin acts, with an alert to the admin → decision 2, 2.3, 5.4.
6. **Channels the scraper adds?** On for everyone; each profile can turn them off, never delete → decision 11, 4.2.
7. **Who sets the new PIN after a reset?** The admin → decision 2, 6.4.
8. **Admin "Open as {name}"?** No. The admin sees only who each profile is and their requests → decision 3, 6.4.

### Still open

Nothing from the review. New questions go here.

### Proposed privacy hardening

- **Build picker previews from counts only.** A dedicated `PickerPreviewQuery` returns numbers and team games and
  never selects a show or channel name, so a later template change can't leak one.
- **Sweep for leaks in the privacy test,** in addition to the per-query test in 4.3. Seed profile A with a show and a
  channel that have unmistakable names. Then, as profile B and with no profile, request every public route and check
  that neither string appears in any response body. This catches pages added later.
- **Keep names out of logs and URLs.** Never log a show or channel name next to a profile ID. Search terms in a URL
  (`?search=`) stay in browser history on shared devices, so send searches as POSTs, or clear them on sign-out.
- **Clean up on sign-out.** Besides clearing the device's profile, drop the in-memory profile copy, `sessionStorage`,
  and anything the page keeps for the profile. On an "Every visit" device, the picker doesn't highlight or preselect
  the last profile used.

### Proposed security hardening

Ranked roughly by how much each one matters. The first two are about the existing deployment and apply even if
profiles are never built.

**1. Make Tailscale the real boundary.** As deployed, the site also answers on the LAN over plain HTTP (see
[Repo check](#repo-check-october-27)). That bypasses Tailscale and sends the admin cookie and every device cookie
unencrypted.
- Publish the port on loopback only: `"127.0.0.1:${WEB_PORT:-8085}:8080"`. Tailscale Serve already proxies
  `http://127.0.0.1:8085`, so the `*.ts.net` address keeps working.
- Check what still uses LAN HTTP first: other containers on `dev-bridge` reach the app directly and aren't affected,
  but any LAN device or bookmark using `http://<host>:8085` will stop working.
- Then mark every cookie `Secure` (`CookieSecurePolicy.Always`) for the admin cookie, `mnp.device` and `mnp.gate`.

**2. Harden admin sign-in.** The admin is now the super user over every profile.
- Add the built-in ASP.NET Core rate limiter (`AddRateLimiter`) to `/account/login`, for example 5 tries per minute per
  client IP, with a fixed slowdown after failures.
- Confirm the live admin password was changed from the `CHANGE_ME` placeholder (`docs/chat-handoff.md`, October 26).
- Require the admin password again before **Delete profile** and **Reset PIN**. A short
  "confirmed in the last 5 minutes" timestamp in the admin cookie is enough.
- Optional, stronger: Tailscale Serve sends a `Tailscale-User-Login` header for tailnet users. Allow admin sign-in
  only when that header matches your own Tailscale account, and only accept it from the trusted proxy address.

**3. Cookies.**
- Name them `__Host-mnp.device` and `__Host-mnp.gate`: the browser then requires `Secure` and `Path=/` and refuses a
  `Domain`, so another site or subdomain can't set them. This needs item 1 first, because `__Host-` cookies only work
  over HTTPS.
- `HttpOnly` and `SameSite=Lax` on both.
- Compare `TokenHash` with `CryptographicOperations.FixedTimeEquals`, like `AdminAuth.PasswordMatches` does.
- Rotate the device secret each time a profile is opened. A copied cookie then stops working once the real device
  opens a profile again.

**4. PIN and lockout.**
- **Rate-limit per device as well as per profile.** Wrong PINs count per profile, and 5 lock the profile until the
  admin acts (decision 2). So anyone on any device can lock a person out with five guesses (a lockout used as a
  prank), and it lasts until you notice. Add a per-device limit on `/profiles/{id}/open` across all profiles, for
  example 10 wrong PINs per hour, after which that device has to wait before trying any profile. The alert names the
  device, so you can sign it out.
- **Reject the most obvious PINs** (`0000`, `1234`, the same digit four times, simple sequences) when a PIN is set.
- **Ask for the current PIN** before switching that device's picker to "Only after signing out". (Asking for it
  before a PIN change is already planned, 6.3.)
- **After a PIN change, sign out the profile's other devices** (`RevokedAt = now`), so whoever knew the old PIN loses
  access. (After an admin Reset PIN this is already planned, 6.4.)
- **Help text:** a PIN is a household lock, not a password. Don't reuse a bank or phone PIN. Four digits hashed in the
  database can be guessed offline if the database is ever copied, which is acceptable for this use.

**5. Validate everything a profile can write.**
- **Name:** trim, normalize, no control characters, at most 40 characters, unique ignoring case.
- **Color:** must match `^#[0-9A-Fa-f]{6}$`. It ends up in `style` attributes, so anything else is a way to inject
  CSS.
- **Settings JSON:** a size cap (for example 32 KB), known top-level keys only, and each value checked against its
  expected type.
- **The profile JSON in the page:** serialize the `<script id="mnp-profile">` content with `System.Text.Json`'s
  default encoder, which escapes `<` and `>`, so a crafted value can't close the tag. Never build it by string
  concatenation.
- **Requests:** names, links and notes are typed by a profile and shown to the admin. Razor encodes them; in the
  header quick-look panel, render them with `textContent`, never `innerHTML`.

**6. Antiforgery on every state-changing request.** The no-JavaScript picker forms post `[FromForm]`, which the
framework validates automatically. Check that `/profiles/signout`, `/profiles/new`, `/api/me/*` and the admin profile
actions all validate too, so another site can't sign a device out or switch its profile.

**7. Limit growth.** Rate-limit `/profiles/new` and request submission per device. Cap the number of devices stored
(for example, delete never-used device rows after 7 days). Default **Forget devices** to 90 days rather than never.

**8. Security headers.** Add `Content-Security-Policy: frame-ancestors 'none'` (and `X-Frame-Options: DENY` for old
browsers), so the picker and PIN pad can't be framed by another page and clicked through. A full script CSP is a
larger job given the inline theme script; leave it for later.

**9. Security log.** Add a small app-owned table, `MyDailyFeedProfileEvents` (time, profile, device, event), for
security events only:
- wrong PIN, lockout, unlock
- PIN set, changed or reset
- device signed out or revoked
- profile created, hidden or deleted

**Never log profile opens or reading activity:** the admin doesn't get to see when someone reads (decision 3). Show the
last 50 on Manage profile. Store event types only, never show or channel names. Keep 90 days.
