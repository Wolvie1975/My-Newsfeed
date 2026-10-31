# iPad portrait rollout

Guides 00–03 are implemented. The remaining guides are separate future changes.
The source proposals are retained beside this handoff; implementation notes here
record adjustments needed for the current app.

## Foundation

`tablet.css` loads after headline cards and confines all rules to 744–1023px.
The header uses two rows on every public page, with six section links and a
44px search shortcut. Settings uses an icon at tablet widths, and the edition
switch shows its initial. Phone and desktop presentations remain in their existing
ranges. The header is about 99px tall at normal text size; Settings anchors use
120px scroll margins. The shell of an opted-in tablet page uses `--surface`, matching
its content paper through the 24px gutters and behind edge-to-edge strips. This
also covers short pages through the shell's existing viewport minimum height.
Unconverted tablet fronts, phones and desktop keep their existing backgrounds.
Content stays in its existing phone presentation until its
wrappers opt in with `tab-desktop` / `tab-phone`. Shared strip/grid/touch helpers
are available for subsequent page guides.

## Where to watch

Watch was the first page to opt in. It uses a 48px heading, a full-width labeled search panel,
compact browse/services row, 150px poster shelves with a right fade, and no tablet
arrows. Manage has a shorter tablet label so six service logos plus the extra count
fit at 744px. The existing phone ribbon's negative margins are reset only on
opted-in Watch. Search results already use a readable single-column list, so they
retain that layout; the phone result-margin override is reset on tablet Watch.
Desktop arrow controls and phone Watch remain in their existing ranges.

## YouTube

YouTube now opts in at 744–1023px. A shared browse wrapper groups the horizontally
scrolling category pills and the channel dropdown, with the count below. It uses
`display: contents` outside the tablet range, preserving the existing desktop flow.
Two desktop-only `sections.css` blocks start at 744px after checking that none of
their selectors appear in phone components. Shared section-pill rules were unchanged.
The phone frame's negative margins are reset only on opted-in content.

Featured spans both tablet columns, with 44px controls, a 30px title and five
progress/title picks. Latest uses 132px thumbnails beside the By channel / Watch
later sidebar; sidebar link styles are copied from the 4K rules into the tablet
scope. Categories keep the sidebar useful when Watch later is empty. When neither
categories nor saved videos produces an aside, Latest uses the full width. More
videos uses three columns. Phone layout and 1024px+ layouts retain their ranges.

## Entertainment

The Entertainment front and its heading now opt in; Sports remains unchanged.
Six existing section tabs scroll horizontally, without reintroducing Where to
watch. The shared tab helper reveals an off-screen current tab at tablet widths
using only horizontal scrolling, on initial load, enhanced navigation and resize.
Linked Movies/TV/Digital/Box office/headlines pages keep their existing layouts and
phone section dropdowns; this guide does not convert those subpages.

On screen uses tall 168px cards in a swipe strip. The 16:9 lead spans the page above
a single headline list and the box office/TV/theaters column. Entertainment desktop
rules were copied into the tablet scope instead of widening the mixed block, keeping
News phone headlines/meta untouched. The lead overrides the image classifier's
thumbnail geometry/height cap, while empty/failed photos stay hidden and a P8
placeholder can fill the frame. Existing desktop geometry is retained.

The desktop/tablet On screen and Tonight on TV summaries group by show, with a title
fallback when the show ID is unknown. Batches show an episode count; consecutive
numbered episodes in one season get a compact range. Gaps, cross-season episodes
and missing numbers avoid a misleading range. Up to five On screen items include
up to two distinct shows. Phone timelines and episode lists remain unchanged.

Four .NET tests cover grouping/ranges. Browser screenshots use real front data plus
a DOM-only four-episode Avatar fixture for poster/sidebar spacing, without editing
production episodes or follows. Release history records completed matrices, flows
and remaining device/browser limits.

## Verification

The foundation was checked before opting in Watch. Chromium preview checks covered
320/390/430/744/820/834/1023/1024/1280/1440px in light/dark. Final Watch preview/live
checks use 320/390/430/743/744/820/834/1023/1024/1280/1440/2560px in light/dark,
plus Monochrome and Cardinals at 744px. See release history for the completed
interaction checks, deployment details and evidence. Screenshots include populated
shelves and a test-only six-logo/+12 services fixture; production follows and
preferences are untouched. Each run creates and deletes its own reader profile.

Verification uses Chromium browser emulation, including a touch-enabled context.
Physical iPads, Safari and Firefox are unavailable. Width changes exercise the
portrait/landscape breakpoints; no physical-device rotation is claimed.

YouTube verification uses the same phone/tablet breakpoints plus 1920 and 3840px.
The browser check exercises carousel arrows/picks, rotation only after Play, a touch
swipe on categories, category/channel filters, paging, row/card Watch later and Share,
no-aside fallback, no-JavaScript filtering, Watch regression and other-front isolation.
Release history records actual completed runs and deployment evidence.
