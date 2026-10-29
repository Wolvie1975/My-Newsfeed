# Separate public and admin websites

Started October 29, 2026 (America/Chicago). Status: **split deployed; final live verification recorded below**.

## Purpose and agreed decisions

Separate the reader website and admin into two websites so each can be installed
as its own PWA, while keeping the existing application behavior and sharing the
underlying code and database. Keep one GitHub repository and one combined solution,
with independent application projects, images and deployment configuration.

The requested admin hostname is `admin.mynewspaper`. Development uses public port
5295 and admin port 5297. Production DNS, trusted HTTPS and proxy routing still need
to be configured. Preserve the installed public PWA's identity and give Admin a
separate identity. Preserve the existing production service until the split is
verified and a rollout is explicitly undertaken.

When asked to commit, commit locally on `develop`; the user handles GitHub pushes.

## Current implementation

The repository now contains two independently runnable ASP.NET Core websites and
one shared Razor class library in `myNewspaper.sln`:

- `src/myNewspaper.Web`: the public reader website and existing public PWA.
- `src/myNewspaper.Admin`: admin pages, admin JSON APIs, sign-in and the admin PWA.
- `src/myNewspaper.Shared`: database context/models, domain services, the shared TV
  follow form, fonts, vendor files and common identity assets. Existing backend
  `myNewspaper.Web.*` namespaces are retained to avoid changing model and service
  contracts during the move. Neither website references the other website project.

Production now runs separate public and admin containers. The public address remains
`https://mynewsfeed-prod.tailbf31bc.ts.net`; the admin PWA is at
`https://mynewsfeed-prod.tailbf31bc.ts.net:8443/admin` (tailnet only). The user selected
this working HTTPS address for deployment; `admin.mynewspaper` remains a future
hostname and has not been provisioned. No database migration was needed.

## Local development

Configure the existing SQL connection for each host with user secrets or
`ConnectionStrings__DefaultConnection`; configure `Admin:Password` only for Admin.
The public project's existing user-secrets ID remains unchanged. Admin uses the
separate ID `myNewspaper.Admin`. Do not put credentials in tracked settings.

```bash
dotnet run --project src/myNewspaper.Web --launch-profile http
# http://localhost:5295

dotnet run --project src/myNewspaper.Admin --launch-profile http
# http://localhost:5297
```

Configure `Sites:AdminBaseUrl` on the public host and `Sites:PublicBaseUrl` on Admin.
Public development settings point to `http://localhost:5297`; the intended admin
production hostname is `https://admin.mynewspaper`. Admin's development public link
defaults to `http://localhost:5295`. Preview ports can be overridden without changing
PWA identities. `BackgroundJobs:Enabled=false` on the public host disables scheduled
request checking, channel rollout and results writes for preview/test instances.
Normal public deployments default to enabling those jobs. League-phase checks are
read-only and remain available for public rendering.

## Runtime responsibilities

Both hosts use the same database. The public app owns scheduled request checking,
channel rollout, results synchronization and league-phase checks. Admin registers
clients needed for explicit admin actions, but no application background workers.
Saving a YouTube feed in Admin runs its immediate household-channel rollout against
the shared database rather than signalling an in-memory service in another process.

Household policy is read with each public profile request, so changes made by Admin
take effect on subsequent requests without invalidating another process's cache.
Other persisted data continues to use the existing queries and database tables.

Admin does not run the household profile gate. Signing in as admin does not open a
reader profile. The public host no longer maps `/login`, `/account/*`, `/admin/*`
or admin APIs, and no longer serves admin scripts or CSS. Its eligible household
admin links and the edition picker's Manage profiles link open the admin website.
Admin's View site link opens the configured public website.

## Independent PWAs and authentication

Public PWA `id`, manifest URL, service-worker scope, cache prefix, profile cookies
and data-protection application name remain unchanged. Existing public installations
retain their identity. Admin uses:

- Name `myNewspaper Admin`, manifest ID `/mynewspaper-admin`, start URL `/admin`.
- A different page-style icon set, service worker and `mynewspaper-admin-pwa-*` cache.
- Cookie `myNewspaper.Admin.Session` and antiforgery cookie `myNewspaper.Admin.Csrf`.
- Data-protection application name `myNewspaper.Admin` and a separate persistent
  key volume. A new admin sign-in is required after switching websites.

Use different hostnames in production. Browser storage, installed app identity and
service-worker caches then live on separate origins. Cookies are host-only; ports
alone do not isolate cookies, but the distinct cookie names and protection purposes
also keep local development separate. Neither worker caches reader/admin pages,
API responses or mutations. Admin's offline screen only asks the user to reconnect.

## Build, GitHub and deployment

```bash
dotnet build myNewspaper.sln
dotnet test tests/myNewspaper.Tests/myNewspaper.Tests.csproj
dotnet test tests/myNewspaper.Admin.Tests/myNewspaper.Admin.Tests.csproj
npm run lint
npm run test:unit

docker build -t mynewspaper-web:<release> .
docker build -f Dockerfile.admin -t mynewspaper-admin:<release> .
```

The public and admin GitHub workflows build only their project and referenced shared
library. Changes to Shared trigger both. CI runs selected tests that do not require
SQL; run the SQL integration suites against a configured development database and
the browser checks against both previews before release. Workflows do not deploy.

Browser tests accept `BASE_URL` for public and `ADMIN_BASE_URL` for admin. Admin-only
tests also accept `BASE_URL` as a fallback for compatibility. Provide the password
privately through `ADMIN_PASSWORD`.

```bash
BASE_URL=http://localhost:5295 ADMIN_BASE_URL=http://localhost:5297 \
  npm run test:browser -- admin-separation admin-layout admin-profiles headlines-phone pwa
```

Compose has separate `web` and `admin` services, ports 8085 and 8086 by default,
images and persistent key volumes. `ADMIN_SQL_CONNECTION` optionally supplies a
separate runtime database principal for admin; otherwise both use `SQL_CONNECTION`.
Set `ADMIN_BASE_URL`, `PUBLIC_BASE_URL` and actual deployed profile settings before
rollout. Preserve existing runtime overrides when preparing each container.

For production, first provision DNS and trusted HTTPS for `admin.mynewspaper` and
route that hostname to the admin container. A PWA needs trusted HTTPS on real devices;
localhost is permitted for local development. The hostname alone does not provision
DNS or a certificate, and Tailscale's existing public hostname is not changed here.
Validate both containers and both PWAs before switching the public app to the split
build. Keep the old combined image/container as the rollback target. Repeat mobile
checks on the deployed sites before calling the rollout verified.

## Deployment and verification — October 29, 2026 (America/Chicago)

Containers and images:

| Website | Container | Image | Image ID | Host port |
| --- | --- | --- | --- | --- |
| Public | `mynewsfeed-web` | `mynewspaper-web:split-20261029` | `e48d70bae523` | 8085 |
| Admin | `mynewspaper-admin` | `mynewspaper-admin:split-20261029` | `4e1a75444c26` | 8086 |

Tailscale Serve preserves the public HTTPS listener on 443 and adds a separate
HTTPS listener on 8443 for Admin. Both use the existing hostname and a trusted
certificate. Their ports make them separate browser origins. Cookies on the same
hostname can be sent to both ports, so distinct cookie names and data-protection
purposes remain necessary; neither app accepts the other's sign-in.

The actual sql2025 / WebScraper connection and operational settings were preserved.
The public container retains `mynewsfeed_mynewsfeed-keys`; Admin has the separate
`mynewspaper-admin-keys` volume. The public runtime no longer receives the admin
password. An ignored, mode-600 `.env` was prepared privately from existing runtime
settings for future Compose operations, including movie credentials, profile
switch, result interval, proxy and site URLs. Secrets are not committed.

Verification completed before the final live checks:

- Solution and application builds succeeded without warnings. All 1,006
  public/shared .NET tests and three admin tests passed, including a regression
  test checking actual bytes from normal and fingerprinted shared asset URLs.
- ESLint, repository-wide Prettier and all five JavaScript unit tests passed.
- Both Docker images published successfully. Published public CSS, scripts, icon
  and Blazor script were checked for real content; theme initialization worked.
- The 24-page admin layout suite passed at 320/390/430/744/1440px in light and dark,
  including contrast, menu counts, Sources filters and dirty editor state.
- Admin profile, public headline, PWA and separation browser checks passed locally.
  The separation check covers identity, storage/cache isolation, authentication,
  route separation, phone menu, themes, Requests panel and offline recovery.
- Screenshot review found and corrected the moved Requests panel opening outside
  the viewport. Its final placement was verified at all tested widths/themes.
- A temporary profile was opened on the combined public app before the switch.
  Its existing session still worked after the public container was replaced,
  confirming preservation of the public profile cookies and key volume. The
  temporary profile and its devices were removed afterwards.
- Both production containers are healthy, and both HTTPS health endpoints return
  200. Certificate verification was enabled. Workspace tests use a temporary local
  hostname mapping to the existing Tailscale address because workspace DNS does
  not resolve the tailnet hostname by default.

Final live checks passed over the production HTTPS URLs: admin profiles, admin
separation (including fully loaded Requests data), public headlines and PWA checks.
The public headline suite covers 320/390/430px, light/dark, Kansas/monochrome/Cardinals,
plus no-JS and tablet/desktop regressions. The separation suite covers
320/390/430/744/1440px, light/dark, actual API loading, panel bounds, Escape and
menu controls, storage/cache separation and offline recovery. Screenshots were
inspected. The theme test now waits for the settings save response before reload,
rather than racing the debounce. The Requests toggle explicitly opts out of
Blazor enhanced navigation, preventing navigation from resetting its loaded panel.
Results are also recorded in the release history. Screenshots and
logs are under `test-results/browser/live-split/` (ignored by Git). Phone checks use
Chromium emulation; physical-device PWA installation is not claimed. The seeded
request approval/decline browser suite was skipped because no disposable request
fixtures were present; its domain logic remains covered by the .NET tests.
GitHub workflows have been prepared locally and have not run on GitHub.

## Rollback

The stopped combined container `mynewsfeed-web-before-split-20261029` and image
`mynewspaper-web:before-split-20261029` (`d899ee3c639d`) are retained. To restore the
combined application, stop/rename the split public container, rename the previous
combined container back to `mynewsfeed-web`, then start it. This preserves its
original runtime configuration and key volume. Stop Admin and remove only the
8443 Serve listener if rolling back the entire separation; leave the public 443
listener unchanged. No database rollback or schema deletion is needed.

The first admin image/container was also retained before fixing panel placement;
that is not the combined-app rollback target. Prefer the combined container above
for a complete reversal of the split.

## Future work

- If desired, provision DNS and trusted HTTPS for `admin.mynewspaper`, update both
  site URL settings and reinstall the admin PWA at its new origin.
- Check installation and reopening of both apps on actual phones.
- Run seeded request approval/decline browser flows when disposable fixtures are
  available. Preserve the current shared database and existing content.
- Keep subsequent commits local on `develop`; the user handles GitHub pushes.
