# No-photo placeholder (P8) handoff

Written October 29, 2026 (America/Chicago) against `develop` @ `69fd0c9`. Verify current repository state before
making changes; names and line numbers are from that commit.

**Save as:** `docs/no-photo-placeholder-handoff.md`

**Approved mockups:** the myNewspaper canvas:

| Page → artboard | Shows |
|---|---|
| Logo → "Logo H as the no-photo placeholder", row **P8** | The placeholder at card (16:10), lead (16:9), square, phone row (96px) and dark mode |
| Headline pages → "All headlines · D with P8 placeholders" | Every card the same shape: a real photo, or P8 |

## What P8 is

A story without a usable photo gets an image area anyway, drawn in CSS:

| Layer | What | Notes |
|---|---|---|
| Background | The story's **category colour** | The existing `--edge` from the `mx-c-*` class (`PhoneColors.ForCategory`) |
| Logo | Logo H in white at **20%**, **135% of the frame's height**, cropped off the bottom-right corner | Uses the existing `img/placeholder-mark.svg` as a mask |
| Fold | A **gold triangle** in the top-right corner, 22% of the height | `#ffd34d`, like the logo's folded page |
| Source mark | The publisher's **initials** in a white chip, top-left, serif, in the category colour | New `FeedText.SourceInitials` |
| Category | The category name, white caps, bottom-left | |

**Below 120px tall** (phone rows, small squares) the chip and the category name drop out: the row text already names
both. **Dark mode:** the background becomes the category colour mixed into navy so the white logo still shows; chip and
fold stay as they are.

## Decisions

1. **P8 replaces today's small grey mark** everywhere the site shows a no-photo image area: feed rows and cards
   (`.thumb`), Home's lead (`.d-lead__image`) and opinion thumbnails (`.d-opinion .d-thumb`).
2. **Headline cards always have an image area.** No-photo stories get P8 instead of turning into tinted text cards, so
   every card is the same shape. Small images the classifier marks `compact` also show P8 in cards: a small photo
   stretched to card width is what we're avoiding.
3. **The top section always uses the photo arrangement** (lead + two under + Latest). The full-width text banner from
   `headline-top-fix-handoff.md` is no longer needed.
4. **No server or database change.** The colour and initials come from data the page already has.

---

## 1. Source initials (`src/myNewspaper.Shared/Data/FeedText.cs`)

Add beside `SourceMark` (line ~56). `SourceMark` stays for the one-letter publisher badge.

```csharp
private static readonly HashSet<string> MinorWords = new(StringComparer.OrdinalIgnoreCase) { "the", "of", "and", "a", "an", "&" };

/// <summary>
/// Up to three characters for the no-photo placeholder: an all-caps first word as is (BBC News → BBC, KSHB 41 → KSHB),
/// otherwise the first letters of the main words (The Guardian → G, Al Jazeera → AJ, The New York Times → NYT),
/// numbers kept whole (Big 12 Conference → B12). Section suffixes are ignored ("The Guardian — U.S. Opinion" → G).
/// </summary>
public static string SourceInitials(string name)
{
    var main = Regex.Split(name ?? "", @"\s+[—–|·]\s+")[0];
    var words = Regex.Split(main, @"[\s\-]+").Where(w => w.Length > 0).ToArray();
    if (words.Length > 0 && Regex.IsMatch(words[0], "^[A-Z0-9]{2,4}$")) return words[0];
    var initials = string.Concat(words
        .Where(w => !MinorWords.Contains(w) && char.IsLetterOrDigit(w[0]))
        .Select(w => w.All(char.IsDigit) ? w : char.ToUpper(w[0], CultureInfo.InvariantCulture).ToString()));
    return initials.Length == 0 ? "•" : initials[..Math.Min(3, initials.Length)];
}
```

Unit tests (add to the existing `FeedText` tests):

| Input | Expected |
|---|---|
| `BBC News` | `BBC` |
| `NBC News` | `NBC` |
| `KSHB 41` | `KSHB` |
| `The Guardian` | `G` |
| `The Guardian — U.S. & World Opinion` | `G` |
| `Al Jazeera` | `AJ` |
| `The New York Times` | `NYT` |
| `Sky News` | `SN` |
| `Big 12 Conference` | `B12` |
| `Motorsport.com` | `M` |
| empty / punctuation only | `•` |

## 2. The placeholder markup (`Components/Feed/NoPhoto.razor`, new)

One small component, placed **inside** each image container, before the `<img>`:

```razor
@* P8 no-photo placeholder (docs/no-photo-placeholder-handoff.md). Decorative: the headline, category and source are
   already in the text next to it. Hidden by CSS whenever a real photo is showing. *@
<span class="ph" aria-hidden="true">
    <span class="ph__mark"></span>
    <span class="ph__fold"></span>
    @if (!string.IsNullOrWhiteSpace(Source)) { <span class="ph__src">@FeedText.SourceInitials(Source)</span> }
    @if (!string.IsNullOrWhiteSpace(Category)) { <span class="ph__cat">@Category</span> }
</span>

@code {
    [Parameter] public string? Source { get; set; }
    [Parameter] public string? Category { get; set; }
}
```

### Where it goes

| File | Container | Add |
|---|---|---|
| `Components/Feed/FeedArticles.razor` line ~44 | `<div class="thumb" data-article-media …>` | `<NoPhoto Source="@sourceName" Category="@item.CategoryName" />` as the first child |
| `Components/HomeLeadStory.razor` line ~27 | `<a class="d-lead__image" data-article-media …>` | the same, with that story's source and category |
| `Components/HomeOpinionCard.razor` line ~17 | `<a class="d-thumb" data-article-media …>` | `<NoPhoto Source="…" Category="Opinion" />` |

**Category colour class.** The colour comes from an `mx-c-*` class on an ancestor. Cards already have it
(`FeedArticles.razor` adds `PhoneColors.ForCategory(item.CategoryName)` in cards mode). Add the same class to the
`<article>` in **row** mode too, and to the Home lead and opinion wrappers:

```razor
class="article … @PhoneColors.ForCategory(item.CategoryName)"
```

`PhoneColors` lives in `Components.Phone`; it's a plain static class, so using it on desktop is fine. Rename it to
`CategoryColors` if you prefer the name to say what it's for now.

`Home.razor` line ~204 has its own `d-thumb__ph` icon placeholder. Leave it unless it shows on a story list; if it
does, swap it for `<NoPhoto />` in the same way.

## 3. Styles

### 3.1 Remove the old mark (`src/myNewspaper.Shared/wwwroot/public.css`, lines ~406–431)

Delete the `::before` rule (`:is(.thumb, .d-lead__image, .d-opinion .d-thumb)::before`) and its `:has(> img)::before`
companion. Keep `.pub .thumb { position: relative; overflow: hidden; aspect-ratio: 16 / 9; background: var(--ph-bg); }`
and the `img` rule.

### 3.2 Add P8 (same file, in their place)

```css
/* No photo (or one that failed and was removed): P8 — the category colour, logo H cropped large off the corner, a gold
   fold, the publisher's initials and the category. docs/no-photo-placeholder-handoff.md. A real photo covers it. */
.pub .ph {
    --ph-cat: var(--edge, var(--mx-world, #0051ba));
    position: absolute;
    inset: 0;
    z-index: 0;
    container-type: size;
    overflow: hidden;
    background: var(--ph-cat);
    pointer-events: none;
}
:root[data-theme='dark'] .pub .ph {
    /* The dark tokens are light pastels; mix them into navy so the white logo still reads. */
    background: color-mix(in srgb, var(--ph-cat) 35%, #111f36);
}
.pub .ph__mark {
    position: absolute;
    height: 135cqh;
    aspect-ratio: 43 / 50;
    right: -18cqh;
    bottom: -40cqh;
    background: #ffffff;
    opacity: 0.2;
    -webkit-mask: url('img/placeholder-mark.svg') center / contain no-repeat;
    mask: url('img/placeholder-mark.svg') center / contain no-repeat;
}
.pub .ph__fold {
    position: absolute;
    top: 0;
    right: 0;
    width: 22cqh;
    height: 22cqh;
    background: #ffd34d;
    clip-path: polygon(0 0, 100% 0, 100% 100%);
}
.pub .ph__src {
    position: absolute;
    left: max(8px, 6cqh);
    top: max(8px, 6cqh);
    display: grid;
    place-items: center;
    min-width: max(26px, 17cqh);
    height: max(26px, 17cqh);
    padding: 0 3cqh;
    box-sizing: border-box;
    border-radius: 4cqh;
    background: #ffffff;
    color: var(--ph-cat);
    font-family: var(--font-serif, 'Newsreader', Georgia, serif);
    font-size: max(11px, 7cqh);
    font-weight: 800;
    letter-spacing: -0.01em;
}
:root[data-theme='dark'] .pub .ph__src {
    color: #141b2b;
}
.pub .ph__cat {
    position: absolute;
    left: max(10px, 7cqh);
    bottom: max(8px, 6cqh);
    max-width: 60%;
    overflow: hidden;
    white-space: nowrap;
    text-overflow: ellipsis;
    color: #ffffff;
    font-size: clamp(10px, 5.5cqh, 13px);
    font-weight: 800;
    letter-spacing: 0.1em;
    text-transform: uppercase;
}
/* Too small to read: colour, crop and fold only. The row text names the source and category. */
@container (max-height: 119px) {
    .pub .ph__src,
    .pub .ph__cat {
        display: none;
    }
}
/* A real photo covers the placeholder; if it fails, article-images.js removes it and the placeholder shows again. */
.pub [data-article-media]:has(> img) > .ph {
    display: none;
}
.pub [data-article-media] > img {
    position: relative;
    z-index: 1;
}
```

`cqh` units scale everything with the frame's height, so one rule set serves 96px rows, 200px cards and 470px leads.
They need `container-type: size` on `.ph`. That's safe because `.ph` is absolutely positioned inside a sized box.
Supported in current Chrome, Safari and Firefox.

### 3.3 Headline cards (`src/myNewspaper.Web/wwwroot/headline-cards.css`)

Every card now keeps its image area:

1. **Remove** the text-card look: in the `.hc-card` rule (line ~63) drop `padding: 18px`, `border-top` and the
   `background: var(--hc-tint)`, and delete the rule that hides the media (line ~75,
   `.hc-card [data-article-media] { display: none; }`).
2. **Give every card the photo frame** by changing the selectors at lines ~163–198 from
   `.hc-card[data-image-layout='wide'] …` to plain `.hc-card …` (padding 0, transparent background, the media block at
   16:10 with `border-radius: 10px`, the `img` covering it).
3. **Hide small photos in cards** so P8 shows instead of a stretched thumbnail:

```css
:root:is([data-color-scheme], :not([data-color-scheme])) .pub .hc-feed .hc-card:not([data-image-layout='wide']) [data-article-media] > img {
    display: none;
}
:root:is([data-color-scheme], :not([data-color-scheme])) .pub .hc-feed .hc-card:not([data-image-layout='wide']) [data-article-media] > .ph {
    display: block;   /* overrides the :has(> img) rule, because the img is still in the DOM, only hidden */
}
```

4. **Top section:** in `Components/Feed/NewspaperFeed.razor` line 7 change `hc-top hc-top--text` to
   `hc-top hc-top--photo`, and delete the three lines in `wwwroot/article-images.js` (lines ~20–23) that toggle
   `hc-top--photo` / `hc-top--text`. The `hc-top--text` rules in `headline-cards.css` (line ~248 onward) can go. In
   **Latest**, the first story keeps its image area (photo or P8); rows 2–5 stay text-only.
5. The lead's frame is 16:9 and the cards' 16:10; P8 scales with both.

### 3.4 Phones

Phone rows use the same `.thumb` and get P8 automatically at their small size (colour, crop and fold). The phone
"feature" story at the top of lists (`mx-list-feature`) gets the full P8 with initials and category. Nothing else in
`mobile.css` changes.

### 3.5 Without JavaScript

`article-images-nojs.css` hides featured images today. With no script the classifier never runs, so cards stay
`compact` and show P8 even when a photo exists. That's the same "text-safe" fallback as now, just with a coloured frame
instead of a text card. No change needed.

---

## 4. Verify (per AGENTS.md)

1. **All headlines** (News, Sports, Entertainment) at 1024, 1280 and 1440px: every card has the same image frame;
   good photos show; missing, broken, tiny and small photos show P8 with the right category colour, initials and
   category name. The lead without a photo shows P8 at 16:9 with the larger chip.
2. **Category colours:** Kansas City teal, Top Headlines red, Technology / Linux / Video Games violet, Opinion ink,
   others blue. A category that maps to the default shows blue, not grey.
3. **Initials:** spot-check BBC, NYT, G, AJ, KSHB, B12 against the table in step 1.
4. **Home** lead and opinion without photos show P8; with photos, unchanged.
5. **Phones** at 320, 390 and 430px: row thumbnails show colour, crop and fold only (no chip squeezed in); the first
   list feature shows the full P8.
6. **Dark mode and two other colour schemes:** the logo crop is visible on every category colour; chip text readable;
   nothing pastel-on-white.
7. **Broken image:** an image that 404s after loading the page is removed and P8 appears in its place, with no empty
   grey box.
8. **Screen readers:** P8 is `aria-hidden`; the headline and source text still read once.


## Implementation record — October 29, 2026 (Central)

Implemented on `develop`; release details, rollback images and test evidence are
in [release history](release-history.md#p8-no-photo-frames--october-29-2026-central).

The shared image rules also needed adjustments to preserve empty feature frames
and the no-JavaScript fallback. Phone rows now use the specified 96px thumbnail;
the publisher and action buttons have separate lines so names fit at 320px.
The broadsheet Opinion column retains its existing photo treatment and shows a
P8 thumbnail when the photo is absent. No migration or stored feed-data change.

Preview verification passed 320/390/430/820px light/dark and 1024/1280/1440px,
including Kansas, Cardinals and monochrome schemes. Wide, compact, tiny, missing
and late-failing images, Home and Opinion, label suppression, frame ratios,
visible story order, share/save actions, refresh persistence, enhanced navigation,
Load more and no-JavaScript fallback were checked. All 1,032 .NET tests passed,
including the initials table and decorative render tests.

Final production verification passed 320/390/430/820px light/dark and
1024/1280/1440px desktop, covering all three headline hubs on desktop and the
Kansas, Cardinals and monochrome schemes. Dynamic image failures, frame and label
rules, Home/Opinion placeholders, sharing, 390px save/refresh/unsave and saved
theme persistence, and no-JavaScript P8 passed. Live screenshots were inspected.
Both HTTPS health endpoints returned 200. Temporary test profiles were removed.

Physical-device and Safari checks were unavailable. Screenshots and logs are
listed in release history.
