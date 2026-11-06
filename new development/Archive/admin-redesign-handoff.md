# Admin redesign handoff

Written October 26, 2026 (America/Chicago) against `develop` @ `588809d`. Verify current repository state before
making changes; line numbers below are approximate.

**Save as:** `docs/admin-redesign-handoff.md`

**Approved mockups:** the myNewspaper canvas, **Admin** page (https://claude.ai/artifact/JVQ8mU7R63Z1cziNpeW9Z5):

| Artboard | Shows |
|---|---|
| Admin · Scraper status — desktop / phone | Status home: health line, area tiles, Needs attention, Catch-up tasks, news sources table |
| Admin · Sources — desktop / phone | List pattern: toolbar (search, category, status filter), status pills, switches, ⋯ menu, pager; cards on phones |
| Admin · Edit source — desktop / phone | Editor pattern: breadcrumb, labelled fields with help, enabled switch, save bar with unsaved state, health panel, Remove panel |

**Goal:** the admin area looks like the rest of myNewspaper (Newsreader + Schibsted Grotesk, `#0051ba` blue, `#ffd34d`
yellow, double-ruled headings) instead of the stock Blazor template, and works at phone widths without sideways scrolling.

**Scope rule:** everything new is scoped under one `.adm` root class, so public pages are untouched. Bootstrap stays
loaded; `admin.css` restyles the Bootstrap classes the admin pages already use (`.btn-*`, `.form-control`, `.table`,
`.alert`), so pages that haven't been converted yet still improve.

---

## Phase 1 — Shell (layout, sidebar, styles)

### 1.1 Add `wwwroot/admin.css`

Create `src/myNewspaper.Web/wwwroot/admin.css`:

```css
/* =====================================================================================================================
   Admin workspace. Everything is scoped to .adm (MainLayout's root), so public pages are unaffected.
   Fonts come from public.css's @font-face rules, which load on every page.
   ===================================================================================================================== */
.adm {
    --adm-ink: #141b2b;
    --adm-text: #141b2b;
    --adm-text-2: #4b5567;
    --adm-text-3: #5b6577;
    --adm-bg: #f4f6fa;
    --adm-surface: #ffffff;
    --adm-surface-2: #f8fafd;
    --adm-border: #dce3ef;
    --adm-row: #eef2f8;
    --adm-field: #c6cede;
    --adm-accent: #0051ba;
    --adm-accent-hover: #003e91;
    --adm-yellow: #ffd34d;
    --adm-side-bg: #141b2b;
    --adm-side-text: #c9d3e6;
    --adm-side-muted: #8f9bb3;
    --adm-side-active: #26324d;
    --adm-side-rule: #2a3654;
    --adm-ok-bg: #e3f4e8;   --adm-ok-fg: #136c34;
    --adm-warn-bg: #fff1cc; --adm-warn-fg: #6b4500;
    --adm-bad-bg: #fde8ea;  --adm-bad-fg: #9a0019; --adm-bad-line: #f2c4ca;
    --adm-off-bg: #eef2f8;  --adm-off-fg: #4b5567;
    --adm-serif: 'Newsreader', Georgia, serif;
    --adm-sans: 'Schibsted Grotesk', system-ui, sans-serif;

    display: flex;
    flex-wrap: wrap;
    min-height: 100dvh;
    background: var(--adm-bg);
    color: var(--adm-text);
    font-family: var(--adm-sans);
}
:root[data-theme='dark'] .adm {
    --adm-text: #e8edf6;
    --adm-text-2: #b4bfd2;
    --adm-text-3: #9aa6bb;
    --adm-bg: #0d1220;
    --adm-surface: #161d2e;
    --adm-surface-2: #1b2336;
    --adm-border: #2a3550;
    --adm-row: #222b40;
    --adm-field: #3a4766;
    --adm-accent: #5b9bff;
    --adm-accent-hover: #86b5ff;
    --adm-side-bg: #090d17;
    --adm-ok-bg: #12351f;   --adm-ok-fg: #8fe0a8;
    --adm-warn-bg: #3a2c05; --adm-warn-fg: #ffd77a;
    --adm-bad-bg: #3d1218;  --adm-bad-fg: #ffadb6; --adm-bad-line: #6b2530;
    --adm-off-bg: #222b40;  --adm-off-fg: #b4bfd2;
}
.adm a { color: var(--adm-accent); text-decoration: none; }
.adm a:hover { color: var(--adm-accent-hover); text-decoration: underline; }
.adm :is(a, button, input, select, textarea, summary):focus-visible {
    outline: 3px solid #73aef5;
    outline-offset: 2px;
}
.adm-sr {
    position: absolute; width: 1px; height: 1px; overflow: hidden; clip: rect(0 0 0 0); white-space: nowrap;
}

/* ---------- Sidebar ---------- */
.adm-side {
    flex: 1 1 248px;
    max-width: 100%;
    background: var(--adm-side-bg);
    color: var(--adm-side-text);
    padding: 18px 14px;
    box-sizing: border-box;
    display: flex;
    flex-direction: column;
    gap: 18px;
}
.adm-brand { display: flex; align-items: center; gap: 10px; padding: 4px 6px; color: #fff; }
.adm-brand:hover { color: #fff; text-decoration: none; }
.adm-brand__name { display: block; font-family: var(--adm-serif); font-size: 20px; font-weight: 800; line-height: 1.1; }
.adm-brand__tag {
    display: block; font-size: 11px; font-weight: 800; letter-spacing: 0.12em; text-transform: uppercase;
    color: var(--adm-yellow);
}
.adm-nav { display: flex; flex-direction: column; gap: 14px; }
.adm-nav__group { display: flex; flex-direction: column; gap: 2px; }
.adm-nav__label {
    padding: 0 12px 4px; font-size: 11px; font-weight: 800; letter-spacing: 0.12em; text-transform: uppercase;
    color: var(--adm-side-muted);
}
.adm-nav__link {
    display: flex; align-items: center; gap: 10px; min-height: 40px; padding: 0 12px; border-radius: 8px;
    border: 0; background: transparent; width: 100%; text-align: left;
    color: var(--adm-side-text); font: inherit; font-size: 14px; font-weight: 600;
}
.adm .adm-nav__link:hover { background: #ffffff14; color: #fff; text-decoration: none; }
.adm .adm-nav__link.active { background: var(--adm-side-active); color: #fff; font-weight: 700; }
.adm-nav__link.active .adm-icon { color: var(--adm-yellow); }
.adm-nav__text { flex: 1 1 auto; }
.adm-nav__count {
    min-width: 22px; padding: 1px 7px; border-radius: 999px; background: #b0001e; color: #fff;
    font-size: 12px; font-weight: 800; text-align: center;
}
.adm-side__foot { margin-top: auto; padding-top: 12px; border-top: 1px solid var(--adm-side-rule); display: flex; flex-direction: column; gap: 2px; }
.adm-icon { flex: none; width: 18px; height: 18px; }

/* Phone header and menu toggle (CSS-only, works before Blazor connects). */
.adm-bar, .adm-menu-toggle { display: none; }

/* ---------- Main column ---------- */
.adm-main { flex: 999 1 560px; min-width: 0; padding: 28px 36px 56px; box-sizing: border-box; }
.adm-main__inner { max-width: 1180px; margin: 0 auto; display: flex; flex-direction: column; gap: 18px; }

.adm-crumbs { font-size: 13px; font-weight: 600; color: var(--adm-text-2); }
.adm-crumbs span[aria-hidden] { margin: 0 6px; }

.adm-head {
    display: flex; flex-wrap: wrap; align-items: flex-end; justify-content: space-between; gap: 12px 24px;
    padding-bottom: 16px; border-bottom: 4px double var(--adm-text);
}
.adm-head__eyebrow {
    margin: 0; font-size: 12px; font-weight: 800; letter-spacing: 0.12em; text-transform: uppercase; color: var(--adm-accent);
}
.adm-head__title-row { display: flex; flex-wrap: wrap; align-items: center; gap: 12px; margin-top: 4px; }
.adm .adm-head h1 {
    margin: 0; font-family: var(--adm-serif); font-size: 44px; font-weight: 800; line-height: 1; letter-spacing: -0.02em;
}
.adm-head__desc { margin: 8px 0 0; font-size: 14px; color: var(--adm-text-2); }
.adm-head__actions { display: flex; flex-wrap: wrap; align-items: center; gap: 12px; }
.adm-head__meta { font-size: 13px; color: var(--adm-text-2); }

/* ---------- Cards, kickers, tiles ---------- */
.adm-card { padding: 18px 20px; border-radius: 14px; background: var(--adm-surface); border: 1px solid var(--adm-border); min-width: 0; }
.adm-card--flush { padding: 0; overflow: hidden; }
.adm-kicker { margin: 0 0 6px; font-size: 12px; font-weight: 800; letter-spacing: 0.1em; text-transform: uppercase; color: var(--adm-text); }
.adm-h2 { margin: 0; font-family: var(--adm-serif); font-size: 24px; font-weight: 800; }
.adm-cols { display: flex; flex-wrap: wrap; gap: 22px; align-items: flex-start; }
.adm-cols > .adm-col-wide { flex: 3 1 480px; min-width: 0; }
.adm-cols > .adm-col-side { flex: 2 1 320px; min-width: 0; display: flex; flex-direction: column; gap: 18px; }

.adm-health { display: flex; flex-wrap: wrap; align-items: center; gap: 14px 22px; }
.adm-health__icon { flex: none; width: 48px; height: 48px; border-radius: 999px; display: grid; place-items: center; }
.adm-health__icon--ok { background: var(--adm-ok-bg); color: var(--adm-ok-fg); }
.adm-health__icon--bad { background: var(--adm-bad-bg); color: var(--adm-bad-fg); }
.adm-health__text { flex: 1 1 320px; }
.adm-health__title { margin: 0; font-family: var(--adm-serif); font-size: 24px; font-weight: 800; }
.adm-health__sub { margin: 2px 0 0; font-size: 14px; color: var(--adm-text-2); }

.adm-tiles { display: grid; grid-template-columns: repeat(auto-fit, minmax(min(100%, 200px), 1fr)); gap: 14px; }
.adm-tile {
    display: flex; flex-direction: column; gap: 8px; padding: 16px 16px 14px; border-radius: 14px;
    background: var(--adm-surface); border: 1px solid var(--adm-border); color: var(--adm-text);
}
.adm .adm-tile:hover { border-color: var(--adm-accent); text-decoration: none; color: var(--adm-text); }
.adm-tile__label { font-size: 12px; font-weight: 800; letter-spacing: 0.08em; text-transform: uppercase; color: var(--adm-text-2); }
.adm-tile__total { font-family: var(--adm-serif); font-size: 34px; font-weight: 800; line-height: 1; }
.adm-tile__unit { margin-left: 6px; font-size: 13px; color: var(--adm-text-3); }
.adm-tile__pills { display: flex; flex-wrap: wrap; gap: 6px; }

.adm-issue { display: grid; grid-template-columns: 82px minmax(0, 1fr) auto; gap: 14px; align-items: start; padding: 14px 0; border-top: 1px solid var(--adm-row); }
.adm-issue__name { display: block; font-size: 16px; font-weight: 700; }
.adm-issue__meta { display: block; margin-top: 2px; font-size: 13px; color: var(--adm-text-2); }
.adm-code {
    display: block; margin-top: 8px; padding: 8px 10px; border-radius: 8px; background: var(--adm-bg);
    color: var(--adm-text); font-size: 12px; white-space: normal; word-break: break-word;
}
.adm-task { display: flex; flex-direction: column; gap: 8px; padding: 14px; border-radius: 12px; background: var(--adm-bg); }
.adm-task b { font-size: 15px; }
.adm-task span { font-size: 13px; color: var(--adm-text-2); }

/* ---------- Pills ---------- */
.adm-pill { display: inline-block; padding: 3px 9px; border-radius: 999px; font-size: 12px; font-weight: 800; white-space: nowrap; }
.adm-pill--ok { background: var(--adm-ok-bg); color: var(--adm-ok-fg); }
.adm-pill--warn { background: var(--adm-warn-bg); color: var(--adm-warn-fg); }
.adm-pill--bad { background: var(--adm-bad-bg); color: var(--adm-bad-fg); }
.adm-pill--off { background: var(--adm-off-bg); color: var(--adm-off-fg); }
.adm-tag { display: inline-block; padding: 1px 8px; border-radius: 999px; background: var(--adm-row); color: var(--adm-text); font-size: 12px; font-weight: 600; }

/* ---------- Toolbar ---------- */
.adm-toolbar {
    display: flex; flex-wrap: wrap; align-items: center; gap: 10px 14px; padding: 12px 14px; border-radius: 14px;
    background: var(--adm-surface); border: 1px solid var(--adm-border);
}
.adm-search {
    flex: 1 1 260px; display: flex; align-items: center; gap: 8px; min-height: 44px; padding: 0 12px;
    border: 1px solid var(--adm-field); border-radius: 10px; background: var(--adm-surface);
}
.adm-search input { flex: 1 1 auto; min-width: 0; border: 0; outline: none; font: inherit; font-size: 15px; background: transparent; color: inherit; }
.adm-seg { display: flex; padding: 3px; border-radius: 10px; background: var(--adm-row); }
.adm-seg button {
    min-height: 38px; padding: 0 12px; border: 0; border-radius: 8px; background: transparent; color: var(--adm-text);
    font: inherit; font-size: 13px; font-weight: 700;
}
.adm-seg button[aria-pressed='true'] { background: var(--adm-surface); box-shadow: 0 1px 2px #141b2b26; }
.adm-seg__n { margin-left: 4px; font-weight: 600; color: var(--adm-text-3); }

/* ---------- Tables ---------- */
.adm-table-wrap { overflow-x: auto; }
.adm-table { width: 100%; border-collapse: collapse; font-size: 14px; }
.adm-table th[scope='col'] {
    text-align: left; padding: 12px 10px; border-bottom: 1px solid var(--adm-border); background: var(--adm-surface-2);
    font-size: 11px; font-weight: 800; letter-spacing: 0.06em; text-transform: uppercase; color: var(--adm-text-2); white-space: nowrap;
}
.adm-table :is(td, th[scope='row']) { padding: 12px 10px; border-bottom: 1px solid var(--adm-row); vertical-align: middle; text-align: left; font-weight: 400; }
.adm-table :is(th, td):first-child { padding-left: 18px; }
.adm-table :is(th, td):last-child { padding-right: 18px; }
.adm-table .num { text-align: right; font-variant-numeric: tabular-nums; }
.adm-table .nowrap { white-space: nowrap; }
.adm-table tr.is-problem { background: color-mix(in srgb, var(--adm-bad-bg) 30%, var(--adm-surface)); }
.adm-table tr.is-off .adm-row-title { color: var(--adm-text-2); }
.adm-row-title { font-size: 15px; font-weight: 700; color: var(--adm-text); }
.adm-row-sub { display: block; margin-top: 2px; font-size: 12px; color: var(--adm-text-3); word-break: break-all; }
.adm-row-error { display: block; margin-top: 4px; font-size: 12px; color: var(--adm-bad-fg); }
.adm-actions { text-align: right; white-space: nowrap; }

/* Row menu: <details> so it works with no extra JavaScript. */
.adm-menu { position: relative; display: inline-block; vertical-align: middle; }
.adm-menu > summary {
    list-style: none; width: 40px; height: 40px; border-radius: 10px; display: grid; place-items: center; cursor: pointer; color: var(--adm-text-2);
}
.adm-menu > summary::-webkit-details-marker { display: none; }
.adm-menu > summary:hover { background: var(--adm-row); }
.adm-menu__list {
    position: absolute; right: 0; top: 44px; z-index: 5; min-width: 200px; padding: 6px; border-radius: 12px;
    background: var(--adm-surface); border: 1px solid var(--adm-border); box-shadow: 0 8px 24px #141b2b26; text-align: left;
}
.adm-menu__list button, .adm-menu__list a {
    display: flex; align-items: center; width: 100%; min-height: 40px; padding: 0 12px; border: 0; border-radius: 8px;
    background: transparent; color: var(--adm-text); font: inherit; font-size: 14px; font-weight: 600;
}
.adm-menu__list :is(button, a):hover { background: var(--adm-row); text-decoration: none; }
.adm-menu__list .danger { color: var(--adm-bad-fg); }

/* ---------- Switch (a real button with role=switch) ---------- */
.adm-switch { position: relative; flex: none; width: 44px; height: 26px; padding: 0; border: 0; border-radius: 999px; background: #a3adbf; cursor: pointer; }
.adm-switch::after {
    content: ''; position: absolute; top: 3px; left: 3px; width: 20px; height: 20px; border-radius: 999px; background: #fff;
    box-shadow: 0 1px 3px #0000004d; transition: left 0.15s;
}
.adm-switch[aria-checked='true'] { background: var(--adm-accent); }
.adm-switch[aria-checked='true']::after { left: 21px; }
.adm-switch:disabled { opacity: 0.5; cursor: default; }
@media (prefers-reduced-motion: reduce) { .adm-switch::after { transition: none; } }

/* ---------- Forms ---------- */
.adm-form { display: flex; flex-direction: column; border-radius: 14px; background: var(--adm-surface); border: 1px solid var(--adm-border); }
.adm-form__body { padding: 22px 24px; display: flex; flex-direction: column; gap: 20px; }
.adm-field { display: flex; flex-direction: column; gap: 6px; }
.adm-field > label, .adm-label { font-size: 14px; font-weight: 700; }
.adm-field__opt { font-weight: 500; color: var(--adm-text-2); }
.adm-help { font-size: 13px; color: var(--adm-text-2); }
.adm-toggle-row { display: flex; align-items: flex-start; justify-content: space-between; gap: 16px; padding: 14px 16px; border-radius: 12px; background: var(--adm-bg); }
.adm-savebar {
    display: flex; flex-wrap: wrap; align-items: center; gap: 10px; padding: 14px 24px; border-top: 1px solid var(--adm-border);
    background: var(--adm-surface-2); border-radius: 0 0 14px 14px;
}
.adm-savebar__state { display: flex; align-items: center; gap: 8px; flex: 1 1 auto; font-size: 13px; font-weight: 600; color: var(--adm-warn-fg); }
.adm-savebar__state::before { content: ''; width: 8px; height: 8px; border-radius: 999px; background: #e0a100; }
.adm-savebar__state.is-clean { color: var(--adm-text-3); }
.adm-savebar__state.is-clean::before { background: var(--adm-border); }
.adm-dl { margin: 0; display: grid; grid-template-columns: 120px minmax(0, 1fr); gap: 10px 12px; font-size: 14px; }
.adm-dl dt { color: var(--adm-text-2); }
.adm-dl dd { margin: 0; }
.adm-errorbox { margin-top: 14px; padding: 12px; border-radius: 10px; background: var(--adm-bad-bg); border: 1px solid var(--adm-bad-line); }
.adm-errorbox__title { display: block; font-size: 12px; font-weight: 800; letter-spacing: 0.06em; text-transform: uppercase; color: var(--adm-bad-fg); }

/* ---------- Confirm panel (replaces Bootstrap alert-warning confirms) ---------- */
.adm-confirm {
    display: flex; flex-wrap: wrap; gap: 14px 20px; align-items: center; padding: 16px 18px; border-radius: 14px;
    background: var(--adm-warn-bg); border: 1px solid #f0d58a;
}
.adm-confirm__text { flex: 1 1 420px; }
.adm-confirm__text h2 { margin: 0; font-size: 16px; font-weight: 800; }
.adm-confirm__text p { margin: 4px 0 0; font-size: 14px; }
.adm-confirm__actions { display: flex; flex-wrap: wrap; gap: 8px; }

/* ---------- Buttons: restyle Bootstrap's classes inside the admin ---------- */
.adm .btn {
    display: inline-flex; align-items: center; justify-content: center; gap: 8px; min-height: 44px; padding: 0 16px;
    border-radius: 10px; font-family: var(--adm-sans); font-size: 14px; font-weight: 700; line-height: 1.2;
}
.adm .btn-sm { min-height: 40px; padding: 0 14px; font-size: 13px; }
.adm .btn-primary { background: var(--adm-accent); border-color: var(--adm-accent); color: #fff; font-weight: 800; }
.adm .btn-primary:hover { background: var(--adm-accent-hover); border-color: var(--adm-accent-hover); color: #fff; }
.adm :is(.btn-outline-primary, .btn-outline-secondary, .btn-secondary) {
    background: var(--adm-surface); border: 1px solid var(--adm-field); color: var(--adm-text);
}
.adm :is(.btn-outline-primary, .btn-outline-secondary, .btn-secondary):hover { background: var(--adm-row); color: var(--adm-text); border-color: var(--adm-field); }
.adm .btn-outline-danger { background: var(--adm-surface); border: 1px solid #b0001e; color: var(--adm-bad-fg); }
.adm .btn-outline-danger:hover { background: var(--adm-bad-bg); color: var(--adm-bad-fg); }
.adm .btn-dark { background: var(--adm-ink); border-color: var(--adm-ink); color: #fff; }
.adm .btn-link { color: var(--adm-accent); }

/* Bootstrap fields and tables used by pages not yet converted. */
.adm :is(.form-control, .form-select) {
    min-height: 46px; border: 1px solid var(--adm-field); border-radius: 10px; font-size: 15px;
    background-color: var(--adm-surface); color: var(--adm-text);
}
.adm .form-label { font-size: 14px; font-weight: 700; }
.adm .form-text { font-size: 13px; color: var(--adm-text-2); }
.adm .table-responsive { background: var(--adm-surface); border: 1px solid var(--adm-border); border-radius: 14px; }
.adm .table { margin: 0; font-size: 14px; --bs-table-bg: transparent; --bs-table-color: var(--adm-text); --bs-table-border-color: var(--adm-row); }
.adm .table > thead > tr > th {
    background: var(--adm-surface-2); padding: 12px 10px; font-size: 11px; font-weight: 800; letter-spacing: 0.06em;
    text-transform: uppercase; color: var(--adm-text-2); white-space: nowrap;
}
.adm .table > tbody > tr > td { padding: 12px 10px; }
.adm .alert { border-radius: 14px; padding: 16px 18px; }
.adm h1:not(.adm-head h1) { font-family: var(--adm-serif); font-weight: 800; letter-spacing: -0.02em; }

/* ---------- Phones ---------- */
.adm-cards { display: none; }
@media (max-width: 900px) {
    .adm { flex-direction: column; flex-wrap: nowrap; }
    .adm-bar {
        display: flex; align-items: center; gap: 10px; padding: 12px 16px; background: var(--adm-side-bg); color: #fff;
        position: sticky; top: 0; z-index: 10;
    }
    .adm-bar .adm-brand__name { font-size: 17px; }
    .adm-bar__spacer { flex: 1 1 auto; }
    .adm-bar__btn {
        position: relative; width: 44px; height: 44px; border: 1px solid #3a4766; border-radius: 10px; color: #fff;
        display: grid; place-items: center; cursor: pointer;
    }
    .adm-bar__dot { position: absolute; top: 6px; right: 6px; width: 9px; height: 9px; border-radius: 999px; background: #ff4d5e; border: 2px solid var(--adm-side-bg); }
    .adm-side { display: none; flex: none; }
    .adm-menu-toggle:checked ~ .adm-side { display: flex; }
    .adm-side .adm-brand { display: none; }
    .adm-main { padding: 18px 16px 40px; }
    .adm .adm-head h1 { font-size: 32px; }
    .adm-tiles { grid-template-columns: repeat(2, minmax(0, 1fr)); gap: 10px; }
    .adm-tile__total { font-size: 28px; }
    .adm-issue { grid-template-columns: minmax(0, 1fr); gap: 6px; }
    .adm-seg { width: 100%; overflow-x: auto; }
    /* Lists swap the table for cards. */
    .adm-has-cards .adm-table-wrap { display: none; }
    .adm-has-cards .adm-cards { display: flex; flex-direction: column; gap: 10px; padding: 10px; }
    .adm-form__body { padding: 16px; }
    .adm-savebar { position: sticky; bottom: 0; z-index: 4; padding: 12px 16px 18px; border-radius: 0; box-shadow: 0 -4px 16px #141b2b14; }
    .adm-savebar .btn { flex: 1 1 auto; }
}
.adm-cardrow { padding: 12px 14px; border-radius: 14px; background: var(--adm-surface); border: 1px solid var(--adm-border); display: flex; flex-direction: column; gap: 6px; }
.adm-cardrow.is-problem { border-color: var(--adm-bad-line); }
.adm-cardrow__top { display: flex; align-items: flex-start; gap: 10px; }
.adm-cardrow__title { flex: 1 1 auto; min-width: 0; }
.adm-cardrow__meta { display: flex; flex-wrap: wrap; align-items: center; gap: 6px; font-size: 12px; color: var(--adm-text-2); }
.adm-cardrow .adm-row-sub { white-space: nowrap; overflow: hidden; text-overflow: ellipsis; }
```

Load it in `Components/App.razor`, after `tablet.css`:

```html
<link rel="stylesheet" href="@Assets["tablet.css"]" />
<link rel="stylesheet" href="@Assets["admin.css"]" />   <!-- new -->
```

Then **delete** the old admin styling so it doesn't fight the new rules:
- `Components/Layout/MainLayout.razor.css` — template rules except `#blazor-error-ui` (move that block into `admin.css`).
- `Components/Layout/NavMenu.razor.css` — delete the file (all template sidebar rules and the `.bi-*` data-URI icons).
- `wwwroot/app.css` — the `/* Admin workspace … */` block (≈ lines 78–314, everything under `div.page`). Keep the top
  part (`.valid.modified`, `.validation-message`, `.blazor-error-boundary`, etc.).

If `service-worker.js` precaches a fixed list, add `admin.css` and bump the cache version.

### 1.2 `Components/AdminIcon.razor` (new)

Stroke icons in one place, so pages and the menu name them instead of pasting SVG:

```razor
<svg class="adm-icon @Class" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"
     stroke-linecap="round" stroke-linejoin="round" aria-hidden="true">
    @switch (Name)
    {
        case "status": <path d="M3 12h4l3-8 4 16 3-8h4" /> break;
        case "coverage": <rect x="3" y="5" width="18" height="16" rx="2" /><path d="M16 3v4M8 3v4M3 10h18M9 15l2 2 4-4" /> break;
        case "sources": <path d="M4 11a9 9 0 0 1 9 9M4 4a16 16 0 0 1 16 16" /><circle cx="5" cy="19" r="1.5" /> break;
        case "categories": <path d="M3 7h7l2 2h9v10H3z" /> break;
        case "pages": <path d="M14 3H6v18h12V7z" /><path d="M14 3v4h4M9 12h6M9 16h6" /> break;
        case "feeds": <rect x="2" y="5" width="20" height="14" rx="4" /><path d="m10 9 5 3-5 3z" /> break;
        case "videos": <rect x="3" y="3" width="18" height="18" rx="2" /><path d="M7 3v18M17 3v18M3 8h4M3 16h4M17 8h4M17 16h4" /> break;
        case "event-feeds": <path d="M9 6h12M9 12h12M9 18h12M4 6h.01M4 12h.01M4 18h.01" /> break;
        case "events": <rect x="3" y="5" width="18" height="16" rx="2" /><path d="M16 3v4M8 3v4M3 10h18" /> break;
        case "results": <path d="m12 3 2.7 5.6 6.1.9-4.4 4.3 1 6.1L12 17l-5.4 2.9 1-6.1-4.4-4.3 6.1-.9z" /> break;
        case "tv": <rect x="2" y="5" width="20" height="13" rx="2" /><path d="M8 21h8M12 18v3" /> break;
        case "external": <path d="M14 4h6v6M20 4l-9 9M18 14v6H4V6h6" /> break;
        case "signout": <path d="M9 21H5V3h4M16 17l5-5-5-5M21 12H9" /> break;
        case "menu": <path d="M4 7h16M4 12h16M4 17h16" /> break;
        case "refresh": <path d="M20 11a8 8 0 1 0-2.3 5.7M20 4v7h-7" /> break;
        case "plus": <path d="M12 5v14M5 12h14" /> break;
        case "search": <circle cx="11" cy="11" r="7" /><path d="m20 20-4-4" /> break;
        case "check": <path d="m5 12 5 5 9-10" /> break;
        case "alert": <path d="M12 3 2 21h20zM12 10v5M12 18h.01" /> break;
        case "more": <circle cx="5" cy="12" r="1.5" fill="currentColor" /><circle cx="12" cy="12" r="1.5" fill="currentColor" /><circle cx="19" cy="12" r="1.5" fill="currentColor" /> break;
        case "back": <path d="m15 6-6 6 6 6" /> break;
    }
</svg>

@code {
    [Parameter, EditorRequired] public string Name { get; set; } = "";
    [Parameter] public string? Class { get; set; }
}
```

### 1.3 Problem counts for the menu: `Data/AdminNavCounts.cs` (new)

Uses the same report as the status page, so the red counts always agree with it:

```csharp
namespace myNewspaper.Web.Data;

public sealed record AdminNavCounts(int Total, int Sources, int Channels, int EventFeeds, int TvSeries)
{
    public static readonly AdminNavCounts None = new(0, 0, 0, 0, 0);

    public static AdminNavCounts From(ScraperHealthReport report)
    {
        static int Bad(IEnumerable<HealthRow> rows) => rows.Count(r => r.Enabled && r.Level == HealthLevel.Problem);
        var sources = Bad(report.Sources);
        var channels = Bad(report.Channels);
        var feeds = Bad(report.EventFeeds);
        var tv = Bad(report.TvSeries);
        var stale = report.LastRun is null || report.Stale ? 1 : 0;
        return new(sources + channels + feeds + tv + Bad(report.MovieSources) + stale, sources, channels, feeds, tv);
    }
}
```

### 1.4 Rewrite `Components/Layout/NavMenu.razor`

This also fixes the duplicate Home / Scraper status entry (both pointed to `/admin`). The menu renders on the server
with each page request, so counts refresh as you move between pages. A failed count never breaks the menu.

```razor
@inject IDbContextFactory<WebScraperContext> Factory
@inject IConfiguration Config
@inject FeedDates Dates
@using Microsoft.EntityFrameworkCore
@using myNewspaper.Web.Data

<nav class="adm-nav" aria-label="Administration">
    <div class="adm-nav__group">
        <span class="adm-nav__label">Overview</span>
        <NavLink class="adm-nav__link" href="admin" Match="NavLinkMatch.All">
            <AdminIcon Name="status" /><span class="adm-nav__text">Scraper status</span>
            @if (counts.Total > 0) { <span class="adm-nav__count" aria-label="@counts.Total need attention">@counts.Total</span> }
        </NavLink>
        <NavLink class="adm-nav__link" href="admin/schedule-coverage"><AdminIcon Name="coverage" />Schedule coverage</NavLink>
    </div>
    <div class="adm-nav__group">
        <span class="adm-nav__label">News</span>
        <NavLink class="adm-nav__link" href="admin/sources">
            <AdminIcon Name="sources" /><span class="adm-nav__text">Sources</span>
            @if (counts.Sources > 0) { <span class="adm-nav__count" aria-label="@counts.Sources with problems">@counts.Sources</span> }
        </NavLink>
        <NavLink class="adm-nav__link" href="admin/categories"><AdminIcon Name="categories" />Categories</NavLink>
        <NavLink class="adm-nav__link" href="admin/pages"><AdminIcon Name="pages" />Pages</NavLink>
    </div>
    <div class="adm-nav__group">
        <span class="adm-nav__label">Videos</span>
        <NavLink class="adm-nav__link" href="admin/youtube-feeds">
            <AdminIcon Name="feeds" /><span class="adm-nav__text">YouTube feeds</span>
            @if (counts.Channels > 0) { <span class="adm-nav__count" aria-label="@counts.Channels with problems">@counts.Channels</span> }
        </NavLink>
        <NavLink class="adm-nav__link" href="admin/youtube-videos"><AdminIcon Name="videos" />YouTube videos</NavLink>
    </div>
    <div class="adm-nav__group">
        <span class="adm-nav__label">Sports</span>
        <NavLink class="adm-nav__link" href="admin/sports-event-types">
            <AdminIcon Name="event-feeds" /><span class="adm-nav__text">Event feeds</span>
            @if (counts.EventFeeds > 0) { <span class="adm-nav__count" aria-label="@counts.EventFeeds with problems">@counts.EventFeeds</span> }
        </NavLink>
        <NavLink class="adm-nav__link" href="admin/sports-events"><AdminIcon Name="events" />Events</NavLink>
        <NavLink class="adm-nav__link" href="admin/results"><AdminIcon Name="results" />Results follows</NavLink>
    </div>
    <div class="adm-nav__group">
        <span class="adm-nav__label">Television</span>
        <NavLink class="adm-nav__link" href="admin/tv-series">
            <AdminIcon Name="tv" /><span class="adm-nav__text">TV series</span>
            @if (counts.TvSeries > 0) { <span class="adm-nav__count" aria-label="@counts.TvSeries with problems">@counts.TvSeries</span> }
        </NavLink>
    </div>
</nav>

<div class="adm-side__foot">
    <a class="adm-nav__link" href="/" target="_blank" rel="noopener noreferrer"><AdminIcon Name="external" />View site</a>
    <form method="post" action="/account/logout">
        <AntiforgeryToken />
        <button type="submit" class="adm-nav__link"><AdminIcon Name="signout" />Sign out</button>
    </form>
</div>

@code {
    [CascadingParameter] private Task<AuthenticationState>? AuthState { get; set; }
    private AdminNavCounts counts = AdminNavCounts.None;

    protected override async Task OnInitializedAsync()
    {
        if (AuthState is null || (await AuthState).User.Identity?.IsAuthenticated != true) return;
        try
        {
            var now = DateTime.UtcNow;
            await using var db = await Factory.CreateDbContextAsync();
            var report = await ScraperHealth.GetReportAsync(
                db, now, Dates.TodayAt(now), ScraperHealth.StaleAfter(Config["Scraper:StaleAfterHours"]));
            counts = AdminNavCounts.From(report);
        }
        catch (Exception)
        {
            counts = AdminNavCounts.None;   // the menu still renders; the status page shows the real error
        }
    }
}
```

Add `@using Microsoft.AspNetCore.Components.Authorization` if it isn't already in `Components/_Imports.razor`.

> **Label change:** "Sports event types" becomes **Event feeds** in the menu, which matches what the page's own
> description says a type is. Rename the page heading and `<PageTitle>` to match, or keep the old wording if you prefer.

### 1.5 Rewrite `Components/Layout/MainLayout.razor`

```razor
@inherits LayoutComponentBase

<div class="adm">
    <div class="adm-bar">
        <a class="adm-brand" href="admin">
            <img src="img/brand/mark.svg" alt="" width="32" height="32" />
            <span><span class="adm-brand__name">myNewspaper</span><span class="adm-brand__tag">Admin</span></span>
        </a>
        <span class="adm-bar__spacer"></span>
        <a class="adm-bar__btn" href="/" target="_blank" rel="noopener noreferrer" aria-label="View site"><AdminIcon Name="external" /></a>
        <label class="adm-bar__btn" for="adm-menu-toggle"><span class="adm-sr">Menu</span><AdminIcon Name="menu" /></label>
    </div>
    <input id="adm-menu-toggle" class="adm-menu-toggle adm-sr" type="checkbox" />

    <aside class="adm-side">
        <a class="adm-brand" href="admin">
            <img src="img/brand/mark.svg" alt="" width="34" height="34" />
            <span><span class="adm-brand__name">myNewspaper</span><span class="adm-brand__tag">Admin</span></span>
        </a>
        <NavMenu />
    </aside>

    <main class="adm-main" id="main">
        <div class="adm-main__inner">
            @Body
        </div>
    </main>
</div>

<div id="blazor-error-ui" data-nosnippet>
    An unhandled error has occurred.
    <a href="." class="reload">Reload</a>
    <span class="dismiss">🗙</span>
</div>
```

Check `img/brand/mark.svg` is the tile mark (blue square, white page, yellow corner). If it's the bare page mark, use
the same inline SVG as `PublicLayout.razor`'s masthead.

> **Phone menu dot:** the mockup shows a red dot on the menu button when something needs attention. The layout can't
> see NavMenu's counts directly; the simplest route is to move the count lookup into a small `AdminNavState` scoped
> service that both read. Optional, so it's left out of the code above.

### 1.6 Error boundary — correction to the earlier review

The earlier review suggested wrapping `@Body` in `<ErrorBoundary>` inside `MainLayout`. **That won't work in this
app:** the layout renders on the server statically, and each admin page runs in its own interactive circuit, so a
layout-level boundary never sees errors thrown by page event handlers. Put the boundary inside the interactive tree:

`Components/Pages/Admin/AdminContent.razor` (new):

```razor
<ErrorBoundary @ref="boundary">
    <ChildContent>@ChildContent</ChildContent>
    <ErrorContent>
        <div class="adm-confirm" role="alert">
            <div class="adm-confirm__text">
                <h2>Something went wrong</h2>
                <p>The database may be busy or restarting. Nothing was changed by the failed action.</p>
            </div>
            <div class="adm-confirm__actions">
                <button type="button" class="btn btn-dark" @onclick="Recover">Try again</button>
            </div>
        </div>
    </ErrorContent>
</ErrorBoundary>

@code {
    [Parameter] public RenderFragment? ChildContent { get; set; }
    private ErrorBoundary? boundary;
    private void Recover() => boundary?.Recover();
}
```

Wrap each admin page's markup in `<AdminContent> … </AdminContent>` as you convert it (Phase 3/4). Local `try/catch`
blocks for expected cases (unique and foreign-key violations) stay as they are.

---

## Phase 2 — Shared pieces

### 2.1 `AdminPageHeader.razor` (new, `Components/Pages/Admin/`)

```razor
@if (Crumbs is not null)
{
    <nav class="adm-crumbs" aria-label="Breadcrumb">@Crumbs</nav>
}
<header class="adm-head">
    <div>
        <p class="adm-head__eyebrow">@Eyebrow</p>
        <div class="adm-head__title-row">
            <h1>@Title</h1>
            @Badge
        </div>
        @if (!string.IsNullOrEmpty(Description))
        {
            <p class="adm-head__desc">@Description</p>
        }
    </div>
    @if (Actions is not null)
    {
        <div class="adm-head__actions">@Actions</div>
    }
</header>

@code {
    [Parameter, EditorRequired] public string Eyebrow { get; set; } = "";
    [Parameter, EditorRequired] public string Title { get; set; } = "";
    [Parameter] public string? Description { get; set; }
    [Parameter] public RenderFragment? Badge { get; set; }
    [Parameter] public RenderFragment? Actions { get; set; }
    [Parameter] public RenderFragment? Crumbs { get; set; }
}
```

### 2.2 Replace `StatusBadge.razor` (same file, new markup)

Keeps the existing name, so every current use updates at once. The word carries the meaning, not only the colour:

```razor
<span class="adm-pill adm-pill--@(Level switch { HealthLevel.Problem => "bad", HealthLevel.Warning => "warn", _ => "ok" })">
    @(Text ?? Level switch { HealthLevel.Problem => "Problem", HealthLevel.Warning => "Check", _ => "OK" })
</span>

@code {
    [Parameter] public HealthLevel Level { get; set; }
    [Parameter] public string? Text { get; set; }
}
```

For disabled rows use `<span class="adm-pill adm-pill--off">Disabled</span>`.

### 2.3 `AdminSwitch.razor` (new)

A real `<button role="switch">`, so it's keyboard- and screen-reader-friendly and never gets out of sync with the
database the way an `<input @onchange>` can:

```razor
<button type="button" class="adm-switch" role="switch" aria-checked="@(Checked ? "true" : "false")"
        aria-label="@Label" aria-labelledby="@LabelledBy" aria-describedby="@DescribedBy"
        disabled="@Disabled" @onclick="() => OnChange.InvokeAsync(!Checked)"></button>

@code {
    [Parameter] public bool Checked { get; set; }
    [Parameter] public bool Disabled { get; set; }
    [Parameter] public string? Label { get; set; }
    [Parameter] public string? LabelledBy { get; set; }
    [Parameter] public string? DescribedBy { get; set; }
    [Parameter] public EventCallback<bool> OnChange { get; set; }
}
```

### 2.4 Restyle `Components/AdminPager.razor`

Same parameters; new look. Then use it on **Pages**, **YouTube videos** and **Sports events** too, replacing their
hand-written Previous/Next blocks:

```razor
@if (Total > PageSize)
{
    <nav class="d-flex flex-wrap justify-content-between align-items-center gap-2 px-3 py-3" aria-label="List pages">
        <span class="adm-help">@((PageIndex * PageSize) + 1)–@(Math.Min((PageIndex + 1) * PageSize, Total)) of @Total.ToString("N0") @Noun</span>
        <span class="d-flex gap-2">
            <button type="button" class="btn btn-sm btn-outline-secondary" disabled="@(Busy || PageIndex == 0)" @onclick="() => PageChanged.InvokeAsync(PageIndex - 1)">Previous</button>
            <button type="button" class="btn btn-sm btn-outline-secondary" disabled="@(Busy || (PageIndex + 1) * PageSize >= Total)" @onclick="() => PageChanged.InvokeAsync(PageIndex + 1)">Next</button>
        </span>
    </nav>
}
@code {
    [Parameter] public int Total { get; set; }
    [Parameter] public int PageIndex { get; set; }
    [Parameter] public int PageSize { get; set; } = 25;
    [Parameter] public bool Busy { get; set; }
    [Parameter] public string Noun { get; set; } = "";
    [Parameter] public EventCallback<int> PageChanged { get; set; }
}
```

---

## Phase 3 — The three pattern pages

### 3.1 Scraper status (`ScraperStatus.razor`)

Data stays the same (`ScraperHealth.GetReportAsync`). The page changes to:

1. **Header** — `AdminPageHeader` Eyebrow "Overview", Title "Scraper status", Actions = "Checked {time}" + Refresh button.
2. **Health line** — one `.adm-card.adm-health`, three states:
   - no run yet → red icon, "The scraper hasn't written anything yet"
   - stale → red icon, "The scraper hasn't run for {age}" + the existing explanation
   - running → green check, "The scraper is running", "Last run {age} ago at {time}. It normally runs every hour.
     Anything older than {StaleAfter} is flagged." Plus, when `Problems + Warnings > 0`, an `<a href="#attention">`
     pill: "{n} items need attention ↓".
3. **Area tiles** — `.adm-tiles` with one `<a class="adm-tile">` per area (News sources → `admin/sources`, YouTube
   channels → `admin/youtube-feeds`, TV series → `admin/tv-series`, Movie releases → no link, Sports feeds →
   `admin/sports-event-types`). Each shows the row count and OK / Check / Problem pills; only show Check and Problem
   when non-zero.
4. **Needs attention** — the non-OK rows from every area, Problems first, as `.adm-issue` rows: pill, name, area +
   "last refreshed {LastSeen}", error in `.adm-code`, and an "Open" link to the row's edit page. Hide the whole card
   when nothing needs attention.
5. **Catch-up tasks** — move the two catch-up actions here from the YouTube feeds and Event types pages (keep them
   there too if you like):
   - "{UnlinkedVideos} videos aren't linked to a feed" → `AdminData.SyncYoutubeFeedsAsync`
   - "{UnlinkedEvents} events don't have a feed · {linkable} can be matched by website" → `AdminData.LinkSportsEventsBySiteAsync`
     (`linkable` = `(await AdminData.PlanSportsEventLinksAsync(db)).Count`)
   - Hide each task when its count is 0; hide the card when both are.
6. **Per-area tables** — keep them below, but render them all through `HealthTable` (the news-sources table is
   currently a hand-written copy). Update `HealthTable` to the `.adm-table` markup with `StatusBadge`.

Build the attention list in `LoadAsync`:

```csharp
private sealed record Issue(HealthRow Row, string Area, string? EditHref);

private List<Issue> issues = [];

// after report is loaded:
issues = new[]
    {
        report.Sources.Select(r => new Issue(r, "News source", $"admin/sources/{r.Id}/edit")),
        report.Channels.Select(r => new Issue(r, "YouTube channel", $"admin/youtube-feeds/{r.Id}/edit")),
        report.EventFeeds.Select(r => new Issue(r, "Sports feed", $"admin/sports-event-types/{r.Id}/edit")),
        report.TvSeries.Select(r => new Issue(r, "TV series", "admin/tv-series")),
        report.MovieSources.Select(r => new Issue(r, "Movie releases", null)),
    }
    .SelectMany(x => x)
    .Where(i => i.Row.Enabled && i.Row.Level != HealthLevel.Ok)
    .OrderBy(i => i.Row.Level == HealthLevel.Problem ? 0 : 1)
    .ThenBy(i => i.Row.Name)
    .ToList();
```

Wrap `LoadAsync`, the two catch-up actions and Refresh in `try/catch` that sets an `error` message, or rely on
`AdminContent` (1.6).

### 3.2 Sources list (`SourceList.razor`)

Add `class="adm-has-cards"` to the list card so phones get cards. Changes:

**Toolbar** (replaces the lone category `<select>`):
- **Search** — new: matches `Label` or `Url` (`s.Url.Contains(q) || (s.Label != null && s.Label.Contains(q))`).
- **Category** — as today, plus "Uncategorized".
- **Status** segmented filter: All / Enabled / Disabled / Errors, each with a count.
  `Errors` = `s.Enabled && s.LastError != null`.

**Filters in the address.** Read them from the query string and navigate on change, so Back from an edit page keeps
your place and lists are bookmarkable:

```csharp
[SupplyParameterFromQuery(Name = "q")] private string? Q { get; set; }
[SupplyParameterFromQuery(Name = "categoryId")] private string? CategoryIdText { get; set; }
[SupplyParameterFromQuery(Name = "status")] private string? Status { get; set; }   // enabled | disabled | errors
[SupplyParameterFromQuery(Name = "page")] private int? PageNumber { get; set; }
[Inject] private NavigationManager Nav { get; set; } = default!;

protected override async Task OnParametersSetAsync()
{
    searchInput = Q ?? "";
    categoryFilter = CategoryIdText;
    pageIndex = Math.Max(0, (PageNumber ?? 1) - 1);
    await LoadAsync();
}

private void Go(string? q = null, string? category = null, string? status = null, int? page = null, bool keep = true) =>
    Nav.NavigateTo(Nav.GetUriWithQueryParameters(new Dictionary<string, object?>
    {
        ["q"] = string.IsNullOrWhiteSpace(q ?? (keep ? Q : null)) ? null : (q ?? Q)!.Trim(),
        ["categoryId"] = string.IsNullOrEmpty(category ?? (keep ? CategoryIdText : null)) ? null : (category ?? CategoryIdText),
        ["status"] = string.IsNullOrEmpty(status ?? (keep ? Status : null)) ? null : (status ?? Status),
        ["page"] = page is > 1 ? page : null,
    }), replace: true);
```

(Load the category list once in `OnInitializedAsync`; `OnParametersSetAsync` reruns on every query change.)

**Table columns:** Source (name + category tag + URL + error line) · Status (`StatusBadge`, or "Disabled") · Enabled
(`AdminSwitch`, `Label="Enabled: {name}"`) · Pages (link) · Last scraped · actions (Edit button + `⋯` menu).
Problem rows get `class="is-problem"`, disabled rows `class="is-off"`.

**Row menu** — move Delete out of the row into a `<details class="adm-menu">`:

```razor
<details class="adm-menu">
    <summary aria-label="More actions for @row.Name"><AdminIcon Name="more" /></summary>
    <div class="adm-menu__list">
        <a href="admin/pages?sourceId=@row.Id">View pages</a>
        <button type="button" @onclick="() => ToggleAsync(row.Id)">@(row.Enabled ? "Disable" : "Enable")</button>
        <button type="button" class="danger" @onclick="() => StartDelete(row)">Delete…</button>
    </div>
</details>
```

**Confirm panel** — restyle the existing `pendingDelete` block as `.adm-confirm` with the heading "Delete {name}?",
the page count spelled out, and the buttons in this order: **Disable instead** (`btn-dark`, only when enabled) ·
**Delete source and {n} pages** (`btn-outline-danger`) · **Cancel** (`btn-link`). Logic is unchanged.

**Phone cards** — inside the same card, after the table wrapper:

```razor
<div class="adm-cards">
    @foreach (var row in rows)
    {
        <article class="adm-cardrow @(row.HasProblem ? "is-problem" : null)">
            <div class="adm-cardrow__top">
                <div class="adm-cardrow__title">
                    <a class="adm-row-title" href="admin/sources/@row.Id/edit">@row.Name</a>
                    <span class="adm-row-sub">@row.Url</span>
                </div>
                <AdminSwitch Checked="row.Enabled" Label="@($"Enabled: {row.Name}")" OnChange="_ => ToggleAsync(row.Id)" />
            </div>
            @if (row.HasProblem) { <span class="adm-row-error">@Truncate(row.LastError!, 120)</span> }
            <div class="adm-cardrow__meta">
                @StatusFor(row)
                <span class="adm-tag">@(row.CategoryName ?? "Uncategorized")</span>
                <span>@row.PageCount.ToString("N0") pages · @Dates.Stamp(row.LastScrapedAt)</span>
            </div>
        </article>
    }
</div>
```

with `HasProblem => Enabled && !string.IsNullOrEmpty(LastError)` on `SourceRow` and a `StatusFor` helper that
returns `StatusBadge` (Problem/OK) or the Disabled pill.

### 3.3 Source editor (`SourceEdit.razor`)

Layout: `AdminPageHeader` with `Crumbs` ("Sources / {name}"), Eyebrow "Edit source" (or "Add source"), Title = the
source's name, `Badge` = its status; `Actions` = "Added {date} · changes apply on the scraper's next run". Below,
`.adm-cols` with the form (`.adm-col-wide`) and an aside (`.adm-col-side`).

**Form** — `EditForm` with an explicit `EditContext` so the save bar knows what changed:

```razor
<EditForm EditContext="editContext" OnValidSubmit="SaveAsync" class="adm-form adm-col-wide">
    <DataAnnotationsValidator />
    <div class="adm-form__body">
        <h2 class="adm-kicker">Details</h2>
        @if (error is not null) { <div class="alert alert-danger" role="alert">@error</div> }

        <div class="adm-field">
            <label for="url">Feed or website URL</label>
            <InputText id="url" class="form-control" @bind-Value="form.Url" aria-describedby="url-help" />
            <span id="url-help" class="adm-help">Starts with http:// or https://. Each URL can only be added once.</span>
            <ValidationMessage For="() => form.Url" />
        </div>

        <div class="adm-field">
            <label for="label">Display name <span class="adm-field__opt">(optional)</span></label>
            <InputText id="label" class="form-control" @bind-Value="form.Label" aria-describedby="label-help" />
            <span id="label-help" class="adm-help">Shown on articles and in filters. Leave empty to show the URL.</span>
        </div>

        <div class="adm-field">
            <label for="category">Category</label>
            <InputSelect id="category" class="form-select" @bind-Value="form.SourceCategoryId" aria-describedby="cat-help">
                <option value="">No category</option>
                @foreach (var c in categories) { <option value="@c.Id">@c.CategoryName</option> }
            </InputSelect>
            <span id="cat-help" class="adm-help">@HubHelp <a href="admin/categories">Manage categories</a></span>
        </div>

        <div class="adm-toggle-row">
            <span>
                <span id="en-l" class="adm-label d-block">Scraper reads this source</span>
                <span id="en-d" class="adm-help d-block">Turn off to stop collecting and hide its articles from the site. Nothing is deleted.</span>
            </span>
            <AdminSwitch Checked="form.Enabled" LabelledBy="en-l" DescribedBy="en-d" OnChange="SetEnabled" />
        </div>
    </div>

    <div class="adm-savebar">
        <span class="adm-savebar__state @(Dirty ? null : "is-clean")" role="status">@SaveState</span>
        <a class="btn btn-link" href="admin/sources">Cancel</a>
        <button type="submit" class="btn btn-primary" disabled="@(saving || (!IsNew && !Dirty))">@(IsNew ? "Add source" : "Save changes")</button>
    </div>
</EditForm>
```

```csharp
private EditContext editContext = default!;

// where the form is created (OnParametersSetAsync):
editContext = new EditContext(form);

private void SetEnabled(bool value)
{
    form!.Enabled = value;
    editContext.NotifyFieldChanged(FieldIdentifier.Create(() => form.Enabled));
}

private bool Dirty => editContext?.IsModified() == true;

private string SaveState
{
    get
    {
        if (!Dirty) return "No unsaved changes";
        var names = new List<string>();
        if (editContext.IsModified(() => form!.Url)) names.Add("URL");
        if (editContext.IsModified(() => form!.Label)) names.Add("name");
        if (editContext.IsModified(() => form!.SourceCategoryId)) names.Add("category");
        if (editContext.IsModified(() => form!.Enabled)) names.Add("enabled");
        return $"Unsaved {(names.Count == 1 ? "change" : "changes")}: {string.Join(", ", names)}";
    }
}

// "Top Headlines appears in the Headlines hub." — from the chosen category's NewsHub.
private string HubHelp => categories.FirstOrDefault(c => c.Id.ToString() == form?.SourceCategoryId) is { } c
    ? $"{c.CategoryName} appears in the {char.ToUpper(c.NewsHub[0])}{c.NewsHub[1..]} hub."
    : "Uncategorized sources appear in Headlines.";
```

> Check the last fallback line against how the site actually treats uncategorized sources, and adjust the wording.

**Aside — Source health** (edit mode only; read-only): an `.adm-card` with a `.adm-dl`: Last scraped (with age, red
when stale), Articles (`{pageCount} pages →` link), Newest article (`max(Published)` for the source). When
`LastError` is set, an `.adm-errorbox` with the error in `.adm-code`. "6 runs in a row" needs a failure counter the
scraper doesn't store today; leave it out or add the column later.

**Aside — Remove this source**: short text ("Disabling keeps its {n} pages and can be undone. Deleting removes the
source and every page it collected.") with **Disable source** (`btn-outline-secondary`, hidden when already
disabled) and **Delete source…** (`btn-outline-danger`). Delete shows the same `.adm-confirm` panel as the list
(reuse `AdminData.DeleteSourceAsync`) and returns to `admin/sources` with a message.

**Phones**: `.adm-cols` stacks; the save bar sticks to the bottom (CSS above). In the mockup the health block shrinks
to the red error strip above the form on phones; that's optional.

---

## Phase 4 — Roll the patterns out

Each page: wrap it in `<AdminContent>`, swap the heading for `AdminPageHeader`, then apply the list or editor pattern.

| Page | Eyebrow | Pattern notes |
|---|---|---|
| Schedule coverage | Overview | Table → `.adm-table`; coverage word as a pill |
| Categories | News | Keep inline rename; Add form → `.adm-toolbar`; Hub as `.adm-tag` |
| Pages | News | List pattern; source filter + search in the toolbar; `AdminPager`; ⋯ menu (Edit, Open article, Delete…) |
| Page editor | News | Editor pattern; aside = URL, source, scraped; label Published as Central time (or keep UTC, but say so) |
| YouTube feeds | Videos | List pattern; thumbnail-less rows; catch-up banner → small task card at top |
| Feed editor | Videos | Editor pattern; Channel ID read-only note when locked |
| YouTube videos | Videos | List pattern with 16:9 thumbnails; `AdminPager`; phone cards with the thumbnail on the left |
| Video editor | Videos | Editor pattern; read-only details in the aside, the Feed select is the only field |
| Event feeds (types) | Sports | List pattern; Enabled as `AdminSwitch`; "Disable in Edit" → ⋯ menu note; hide Delete for shared-attribution feeds |
| Events | Sports | List pattern; four filters wrap in the toolbar; date + time stacked; `AdminPager` |
| Event editor | Sports | Editor pattern; locked state explained in the aside |
| Results follows | Sports | List pattern; logo in the name cell; Sync now in the ⋯ menu; "Sync all now" in the header actions |
| Follow a league or team | Sports | League select + team filter in a toolbar; team rows as cards on phones |
| TV series | Television | List pattern; Following as `AdminSwitch` (Stop following / Follow again) |
| Login | — | Centered card on `--adm-bg`, logo + "Admin sign in" in the serif, one field, full-width button |

Fold in the bug fixes from the review while touching each page (unhandled `InvalidOperationException` on event-type
delete, sync-after-follow failure, scraper status load errors).

---

## Phase 5 — Verify (per AGENTS.md)

- **Widths:** 320, 390, 430 (phone), 744, 820 (iPad portrait), 1024, 1440. Light **and** dark mode at each.
- **Shell:** menu opens and closes on phones without JavaScript; current page highlighted; counts match the status
  page; View site opens a new tab; Sign out works.
- **Status:** each health state (fresh, stale, never run — stop the scraper or point at an empty DB to check); tiles
  link correctly; catch-up buttons hide at zero; Refresh works.
- **Sources:** search, category and status filters combine; Back from Edit keeps them; switches toggle and stay in
  sync after reload; ⋯ menu works by keyboard (Tab, Enter, Esc closes `<details>` only with a small handler, so
  check Tab-away closes it acceptably); Disable instead / Delete with pages; cards replace the table below 900px.
- **Editor:** save bar shows the right changed fields; Save disabled until something changes; duplicate URL error;
  deleted-category error; Remove panel; sticky save bar on phones doesn't cover the last field.
- **No horizontal scroll** anywhere at 320px (console check from the iPad guide).
- **Contrast:** pills, help text and sidebar labels at 4.5:1 in both themes.
- **Public site unchanged:** spot-check Home, Headlines, Sports and Settings at 390 and 1440 after `admin.css` lands.

Record widths, themes, flows and anything not verified in the release notes, as usual.
