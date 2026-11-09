# Logo: switch to the front-page mark (H)

Written October 28, 2026 (America/Chicago) against `develop` @ `bb368cc`.

**Save as:** `docs/logo-front-page-handoff.md`
**Patch:** `logo-front-page.patch` (made with `git diff --binary`; checked with `git apply --check` on a clean `bb368cc`)
**Mockup:** canvas → **Logo** page → "Logo — round 2", row **H · Front page**

## What changes

The folded-page "n" becomes a tiny front page: nameplate, rule, headline, a photo and three lines of type, keeping the
gold folded corner. Same 64×64 grid, same page-only crop (`viewBox="11 7 43 50"`), same CSS classes, so colour schemes
and dark mode keep working with no CSS edits.

| Part | Before | After |
|---|---|---|
| Page | `M15 11h25l10 10v32H15z` | `M12 8h29l11 11v37H12z` |
| Fold | `M40 11v10h10z` | `M41 8v11h11z` |
| Ink | one `<path>` (the "n") | `<g>` of eight `<rect>`s; the photo has `fill-opacity=".35"` |

The ink keeps its class (`brand-mark__n`, `bs-mark-n`), now on the `<g>`, so the rectangles inherit the scheme colour
from the existing rules in `broadsheet.css` (lines ~67 and ~931).

## Files in the patch

| File | Change |
|---|---|
| `Components/Layout/PublicLayout.razor` | Phone-bar mark and desktop tile mark |
| `Components/Pages/Home.razor` | Masthead tile mark (`bs-title__mark`) |
| `Components/Pages/Editions/Editions.razor` | Picker masthead mark |
| `wwwroot/img/brand/mark.svg` | Favicon / admin header / login (tile version) |
| `wwwroot/img/brand/mark-page.svg` | Page-only version |
| `wwwroot/img/placeholder-mark.svg` | One-colour mask for image placeholders (type and photo cut out) |
| `wwwroot/favicon.png` | 32px, rounded tile |
| `wwwroot/icons/newspaper-front-180/192/512.png`, `-maskable-512.png` | New PNGs (new names so browsers and iOS don't serve the cached old icon) |
| `Components/App.razor`, `wwwroot/manifest.webmanifest` | Point at the new PNGs |
| `wwwroot/service-worker.js` | Precache list → new PNGs; `CACHE` `v5` → `v6` so installed apps pick up the new icon |

The old `newspaper-page-*.png` files are left in place; delete them in a later release once nothing references them.

## Apply

```bash
cd ~/path/to/My-Newsfeed
git switch develop
git apply --index logo-front-page.patch
git status
```

## Verify (per AGENTS.md)

1. Phone header at 320, 390 and 430px, light and dark, and in two other schemes (e.g. `cardinals`, `monochrome`): the
   page is white (pale in dark), the type takes the header colour, the corner takes the scheme's accent.
2. Desktop masthead (1024px+), iPad portrait (744px), Home's big masthead tile, and the Editions picker.
3. Browser tab favicon (SVG in Chrome/Firefox; PNG in Safari).
4. Image placeholders still show the mark (uses `placeholder-mark.svg` as a CSS mask).
5. Admin header and Login (`<img src="img/brand/mark.svg">`).
6. Installed app: after deploy, reopen the PWA; the service worker moves to `mynewsfeed-pwa-v6`. iOS home-screen icons
   only refresh when the shortcut is removed and re-added.

Not verified here: I couldn't run the .NET build in my workspace. The Razor edits are markup only (SVG elements swapped),
and I rendered all the SVGs and PNGs in a browser to check them.
