# Article image handling

Implemented and deployed October 28, 2026 (Central), from local commit `62f43bb`.
Production image: `mynewspaper-web:article-images-20261028`
(`sha256:5cfa5bcc085a36d466bf0f5564da2c01341b39be0322b56ee91fc2e94fcf8599`).
Previous container and image retained as `mynewsfeed-web-before-article-images-20261028`
and `mynewspaper-web:before-article-images-20261028`.

Article photography now follows the decoded image dimensions instead of stretching
every feed thumbnail into a feature. Headlines, descriptions, publisher attribution,
links and Save/Share actions remain available in every layout.

- Photos at least 640 × 300px, with an aspect ratio up to 2.2, can use the large
  feature treatment. Since October 29 that includes 4:3, square and portrait photos
  (a 1675px-square NYT lead and a 5712 × 4284 XDA lead had shrunk to thumbnails);
  those under 1.2 crop into a 3:2 frame weighted toward the top. Height is capped at 220px below 900px and
  360px on larger screens. The image frame cannot exceed 1.5× the source pixel width
  (from October 29; at 1× a 740px photo stopped short of the News front's lead column).
  Desktop feed features put the headline beside the photo; Home's top story does so
  only from 1280px, where its middle column is wide enough.
- Other usable images, including small portraits and unusually wide images, use compact
  thumbnails beside the feature headline. `object-fit: scale-down` preserves the
  whole image and prevents enlargement. Regular feed thumbnails also avoid
  enlarging small sources.
- Images below 96px wide or 64px high, failed images and missing images produce
  text-led stories, without a large empty photo area. Rows without a photo give the
  text the whole card rather than the thumbnail column. Compact thumbnails do not
  carry the overlaid “Just in” badge; the article timestamp remains available.
- With JavaScript disabled, featured stories use a text-only fallback. The browser
  cannot reliably check image quality or remove failed images without scripting.

For feature images from the verified BBC `https://ichef.bbci.co.uk/ace/standard/`
service, a supplied size below 1024 requests the 1024px version of the **same asset**.
The original URL is retained for one fallback if that request fails. A successful
response is still measured: a thumbnail returned by the larger URL stays compact.
Other hosts and URL formats are not rewritten. No article pages are fetched on the
server, no new image proxy is introduced, and there is no database migration.
The collector still supplies one image URL; discovering additional publisher
images for other providers is a separate collection-side enhancement.

The shared behavior covers the article feed, Home lead and opinion thumbnails,
and Sports/Entertainment lead stories. It also binds after enhanced navigation,
image URL replacement, hourly Home content replacement and appended feed content.
Movie posters, video thumbnails and brand assets are outside its selectors.
Implementation lives in `wwwroot/article-images.js`, `article-images.css` and
`article-images-nojs.css`, with explicit article/media markers on their Razor markup.

## Local verification

A local preview at port 5397 used the existing runtime read configuration, with
profiles and results-writing background sync disabled. Production was not rebuilt,
restarted or replaced. Evidence: `/tmp/article-images/`.

- `dotnet build myNewspaper.sln`: zero warnings and errors.
- Targeted ESLint and Prettier checks passed for the new assets and browser check.
- `tests/browser/article-images.browser.cjs`: 50 Headlines combinations covering
  320/390/430/744/1440px, light/dark, and large/small/portrait/tiny/broken fixtures.
  Checked thumbnail placement, feature height, overflow, headline and action presence.
- 36 Home/Sports/Entertainment combinations at 390/1440px, light/dark, with
  large/small/broken fixtures. An existing empty Home image slot also received a
  controlled photo to exercise both layouts when the current lead had no image.
- Checked a 640px source on a 2560px viewport and a resize back to 390px; BBC
  higher-resolution success/fallback, no retry loop, strict host matching and query
  preservation; dynamic URL replacement, enhanced navigation and actual More stories /
  Load more interactions; no-JavaScript fallback with both usable and failed images.
- The real BBC article from the reported example loaded its 1024 × 575px image
  instead of the 240 × 134px feed thumbnail, at 390px and 1440px. Representative
  real-image and fixture screenshots were visually inspected.

Run the browser check against an ungated development preview:

```bash
BASE_URL=http://127.0.0.1:5397 SCREENSHOT_DIR=/tmp/article-photo-checks \
  node tests/browser/article-images.browser.cjs
```

These checks use Chromium viewport emulation, not physical devices or Safari.
Dimensions are a practical resolution safeguard; they do not detect subjective
blur, poor composition or a publisher that has already enlarged a low-quality image.
Live deployment evidence is recorded in `/tmp/article-images-deploy/`. Runtime
environment, IPv4/IPv6 ports, network, restart policy, logging and the persistent
keys volume/access were preserved. Docker reports different mount syntax metadata
(`z` instead of `rw`); the named volume, destination and read/write access match.


## Live verification

Repeated the complete browser suite against the production container with household
profiles enabled and a temporary profile. Themes were saved through the profile
preferences API and verified after reload, rather than relying on localStorage.
All 50 Headlines and 36 front-page combinations passed, along with source-size
limits, BBC upgrade/fallback, dynamic replacement, enhanced navigation, Load more
and no-JavaScript checks. The temporary profile was deleted afterward.
Representative live compact-image, Home desktop and real BBC phone/desktop
screenshots were visually inspected. The BBC example decoded at 1024 × 575px on
both 390px and 1440px viewports. Container health is healthy; direct and public
HTTPS `/healthz` return 200. No physical-device or Safari validation was available.


## October 29 navigation correction

A user report exposed a missed case: Blazor enhanced navigation activates the
stylesheet link inside `noscript`. Its fallback hid correctly downloaded images
on Home, Sports and other feature cards. Direct page loads had passed, and the
previous enhanced-navigation check did not assert image visibility on these pages.
The fallback now requires the absence of the article-image JavaScript marker;
every image scan restores that marker after navigation updates the document.
No scraper rerun or database change is required.

Added regression coverage for News → Home → Sports → Entertainment → News at
320/390/430/744/1440px in both themes. It verifies document retention, decoded
image visibility and layout area, plus overflow. The regression failed before the
correction and passed afterward, alongside both no-JavaScript fallback cases.
These were scoped reruns; the unchanged full fixture matrix was not repeated.
Evidence: `/tmp/article-images-deploy/fix-local.log` and `fix-shots/`.


Correction deployed as `mynewspaper-web:article-image-nav-fix-20261029`
(`sha256:756ae425b587d812436d23ff38719c3ad917fae8b081369747684f45f4efbe17`),
from `7ff365d`. Repeated the scoped navigation regression at all five widths and
both themes against production with a throwaway profile, plus no-JavaScript
checks; passed. The runner's inherited final summary lists the full suite, but
only the navigation/no-JavaScript subset ran for this correction. Actual NYT and
ESPN lead images remained visible after enhanced navigation (1675px square and
1296 × 729px respectively). Inspected real desktop and fixture phone screenshots.
Temporary profile removed; live health passed. Runtime configuration and keys
volume/access preserved; rollback container/image retained with suffix/tag
`before-image-nav-fix-20261029`. Live evidence: `fix-live.log`, `fix-live-shots/`,
`nav-home.png` and `nav-sports.png` under `/tmp/article-images-deploy/`.
