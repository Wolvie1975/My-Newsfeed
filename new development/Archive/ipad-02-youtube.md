# iPad portrait, part 2: YouTube

**Save as:** `docs/ipad/ipad-02-youtube.md` · **Needs:** parts 00 and 01 done and checked.

**Mockup:** **iPad portrait** page → "YouTube — iPad portrait".

**Scope:** `Components/Pages/Videos.razor`, `Components/Phone/PhoneVideos.razor`, two `min-width` blocks in
`sections.css` and one block in `tablet.css`.

## What the page looks like at 820

1. Heading: "FROM YOUR CHANNELS", "YouTube".
2. Under a double rule, one row: the **category pills** (All, News & Politics, Sports, Tech, Video Games, Other)
   scrolling sideways with a fade, then the **channel dropdown** at the right. The count line ("All channels · 340
   videos · updated 7:03 PM") sits under them.
3. **Featured** across the full width: label and ‹ ▸ › controls (44px), the 16:9 player, kicker, a 30px title, date,
   views and the tools, then the five picks underneath (`.yt-tabs`, five across).
4. **Two columns:** Latest videos on the left (rows with a 132px thumbnail), and By channel + Watch later
   (`.yt-aside`) on the right.
5. **More videos** three across, then the paging buttons.

## 1. Opt in (markup)

| File | Wrapper | Add |
|---|---|---|
| `Videos.razor` line ~16 | `<div class="mx-desktop">` | `tab-desktop` |
| `PhoneVideos.razor` line 3 | `<div class="mx-phone" data-phone-life="videos">` | `tab-phone` |
| `Videos.razor` line ~186 | `<nav class="mx-phone mx-video-pages" …>` | `tab-phone` |

## 2. Desktop styles to bring down

`PhoneVideos.razor` uses no `yt-` classes, so these two `sections.css` blocks style desktop markup only, and you can
**widen them** from `min-width: 1024px` to `min-width: 744px`:

- **Around line 3857:** `.page-heading:has(+ .filters--videos)`, `.yt-cats`, `.yt-category-select` (hidden: the
  pills replace it) and `.filters--videos`.
- **Around line 4448:** the "YouTube v2" desktop block: `.yt-top`, `.yt-feature`, `.yt-latest`, `.yt-kicker`,
  `.yt-slide__title`, `.yt-tabs`, `.yt-tab…`, `.yt-row…` and `.yt-label`.

Before widening, grep `Components/Phone/` for each class in the block. If any turns up, copy that rule into
`tablet.css` instead (foundation 2.1).

`.sec-pills--desktop` is shared with other pages' phone markup, so **don't widen** its rules. The tablet block below
styles `.yt-cats` directly.

## 3. Tablet styles (`tablet.css`)

```css
    /* ---------- YouTube (docs/ipad/ipad-02-youtube.md) ---------- */
    /* Pills + channel menu on one row under a double rule; the pills scroll sideways. */
    .pub .tab-desktop .yt-cats {
        display: flex;
        flex-wrap: nowrap;
        gap: 8px;
        margin: 14px -24px 0 0;            /* run off the right edge... */
        padding: 12px 24px 4px 0;
        border-top: 4px double var(--text);
        overflow-x: auto;
        scrollbar-width: none;
        -webkit-mask-image: linear-gradient(90deg, #000 calc(100% - 48px), transparent);
        mask-image: linear-gradient(90deg, #000 calc(100% - 48px), transparent);
    }
    .pub .tab-desktop .yt-cats a {
        flex: none;
        display: inline-flex;
        align-items: center;
        min-height: 40px;
        padding: 0 14px;
        border: 1px solid var(--row-border);
        border-radius: 999px;
        color: var(--text);
        font-size: 14px;
        font-weight: 600;
        white-space: nowrap;
        text-decoration: none;
    }
    .pub .tab-desktop .yt-cats a[aria-current='page'] {
        border-color: var(--text);
        background: var(--text);
        color: var(--surface);
    }
    .pub .tab-desktop .filters--videos {
        flex-wrap: wrap;
        gap: 6px 12px;
        padding-bottom: 10px;
        border-bottom: 1px solid var(--row-border);
    }
    .pub .tab-desktop .filters--videos .count--toolbar {
        flex-basis: 100%;
        margin: 0;
        font-size: 12px;
    }

    /* Featured full width, then Latest | By channel + Watch later. */
    .pub .tab-desktop .yt-top {
        grid-template-columns: minmax(0, 1fr) minmax(0, 1fr);
        gap: 0;
    }
    .pub .tab-desktop .yt-feature {
        grid-column: 1 / -1;
        padding: 0 0 20px;
        border-right: 0;
        border-bottom: 1px solid var(--text);
        margin-bottom: 4px;
    }
    .pub .tab-desktop .yt-latest {
        padding: 0 20px 0 0;
    }
    .pub .tab-desktop .yt-top:not(:has(.yt-aside)) .yt-latest {
        grid-column: 1 / -1;                 /* no aside (no saved videos, no categories): Latest takes the width */
        padding-right: 0;
    }
    .pub .tab-desktop .yt-aside {
        display: block;                      /* the 4K guide hides it below 2560 */
        padding: 0 0 0 20px;
        border-left: 1px solid var(--row-border);
    }
    .pub .tab-desktop .yt-slide__title {
        font-size: 30px;
        line-height: 1.1;
    }
    .pub .tab-desktop .yt-tabs {
        grid-template-columns: repeat(5, minmax(0, 1fr));
        gap: 12px;
    }
    .pub .tab-desktop .yt-btn {
        width: 44px;
        height: 44px;
    }
    .pub .tab-desktop .yt-row {
        grid-template-columns: 132px minmax(0, 1fr);
    }

    /* More videos: three across. */
    :root:is([data-color-scheme], :not([data-color-scheme])) .pub .tab-desktop .video-front .videos {
        grid-template-columns: repeat(3, minmax(0, 1fr));
        gap: 20px 16px;
    }
    .pub .tab-desktop .video-front .video--featured {
        grid-column: span 1;
    }
```

**The aside below 2560:** if you followed the 4K guide, `.yt-aside` is hidden below 2560 (`display: none`). The rule
above brings it back on tablets only. If your build already shows it at every width, the rule does nothing.

**The five picks:** the mockup draws them as small thumbnails with titles. Today's `.yt-tab` is a progress bar with
a title. Keep the bars for now. Swapping to thumbnails is a markup change worth doing on desktop at the same time, if
you want it.

## 4. Verify

1. **744, 820, 834px:**
   - The pills scroll sideways and the channel menu stays visible on the right.
   - Choosing a pill or a channel filters, as on desktop.
   - The featured video runs full width, and ‹ ▸ › are 44px and work. The featured video still doesn't auto-rotate
     until ▸ is pressed.
   - Latest and By channel / Watch later sit side by side and end close to each other. With no saved videos, Latest
     takes the full width.
   - More videos: three across, and the paging links work.
2. **Watch later and Share** on rows and cards work by touch.
3. **Phones:** unchanged (PhoneVideos). **1024+:** unchanged.
4. **Where to watch** (part 1) still looks right. Other pages still show their phone layout.

Then do `ipad-03-entertainment.md`.
