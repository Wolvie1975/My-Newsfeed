// Readable text under every colour scheme (user request, October 26, 2026): for each scheme in light and dark mode, at
// phone and desktop width, every visible piece of text in the header and page must meet WCAG AA contrast against the
// colour actually behind it — 4.5:1, or 3:1 for large text (24px+, or 18.66px+ bold). Text over pictures is skipped.
// REPORT=1 prints every failure instead of stopping at the first page with one; SCHEMES and PAGES narrow the run.
const { chromium } = require('playwright');
const assert = require('node:assert/strict');
const base = process.env.BASE_URL || 'http://localhost:5295';
const report = process.env.REPORT === '1';

const schemes = [
    'ribbon',
    'monochrome',
    'kansas',
    'cardinals',
    'city',
    'christmas',
    'halloween',
    'st-patricks',
    'eighties',
    'nineties',
    'y2k',
    'desert',
    'coffee',
];
if (process.env.SCHEMES) schemes.splice(0, schemes.length, ...process.env.SCHEMES.split(','));
const pages = (process.env.PAGES || '/,/watch,/sports,/settings').split(',');
const widths = [390, 1440];

// Runs in the page: returns the text elements that fail, with their colours and ratio.
function audit() {
    const parse = (c) => {
        const m = c.match(/rgba?\(([^)]+)\)/);
        if (!m) return null;
        const [r, g, b, a = '1'] = m[1].split(/[\s,/]+/).filter(Boolean);
        return [Number(r), Number(g), Number(b), Number(a)];
    };
    const blend = (top, under) => {
        const a = top[3];
        return [0, 1, 2].map((i) => top[i] * a + under[i] * (1 - a)).concat(1);
    };
    const lum = ([r, g, b]) => {
        const f = (v) => {
            v /= 255;
            return v <= 0.03928 ? v / 12.92 : ((v + 0.055) / 1.055) ** 2.4;
        };
        return 0.2126 * f(r) + 0.7152 * f(g) + 0.0722 * f(b);
    };
    // The colour painted behind an element: blend backgrounds up the tree until one is opaque.
    const behind = (el) => {
        const layers = [];
        for (let n = el; n; n = n.parentElement) {
            const s = getComputedStyle(n);
            if (s.backgroundImage !== 'none' && !s.backgroundImage.startsWith('linear-gradient')) return null;
            if (n.tagName === 'IMG' || n.querySelector(':scope > img')) {
                const img = n.tagName === 'IMG' ? n : n.querySelector(':scope > img');
                const a = img.getBoundingClientRect(),
                    b = el.getBoundingClientRect();
                if (a.width && b.left < a.right && b.right > a.left && b.top < a.bottom && b.bottom > a.top)
                    return null;
            }
            const c = parse(s.backgroundColor);
            if (c && c[3] > 0) {
                layers.push(c);
                if (c[3] >= 1) break;
            }
        }
        let colour = [255, 255, 255, 1];
        for (const layer of layers.reverse()) colour = blend(layer, colour);
        return colour;
    };
    const out = [];
    const walker = document.createTreeWalker(document.body, NodeFilter.SHOW_TEXT);
    const seen = new Set();
    for (let t = walker.nextNode(); t; t = walker.nextNode()) {
        if (!t.textContent.trim()) continue;
        const el = t.parentElement;
        if (!el || seen.has(el) || el.closest('script,style,noscript,svg,[hidden],.sr-only,#blazor-error-ui')) continue;
        seen.add(el);
        const r = el.getBoundingClientRect();
        const s = getComputedStyle(el);
        if (
            r.width <= 1 ||
            r.height <= 1 ||
            s.visibility === 'hidden' ||
            Number(s.opacity) === 0 ||
            !el.checkVisibility()
        )
            continue;
        if (el.closest('details:not([open])') && !el.closest('summary')) continue;
        const bg = behind(el);
        if (!bg) continue;
        const fgRaw = parse(s.color);
        if (!fgRaw) continue;
        const fg = blend(fgRaw, bg);
        const [l1, l2] = [lum(fg), lum(bg)].sort((a, b) => b - a);
        const ratio = (l1 + 0.05) / (l2 + 0.05);
        const size = parseFloat(s.fontSize),
            bold = Number(s.fontWeight) >= 700;
        const need = size >= 24 || (bold && size >= 18.66) ? 3 : 4.5;
        if (ratio + 0.005 < need)
            out.push({
                text: t.textContent.trim().slice(0, 40),
                where:
                    el.tagName.toLowerCase() +
                    (el.className && typeof el.className === 'string'
                        ? '.' + el.className.trim().split(/\s+/).join('.')
                        : ''),
                fg: s.color,
                bg: 'rgb(' + bg.slice(0, 3).map(Math.round).join(', ') + ')',
                ratio: Math.round(ratio * 100) / 100,
                need,
            });
    }
    return out;
}

(async () => {
    const browser = await chromium.launch();
    let failures = 0;
    try {
        for (const scheme of schemes)
            for (const theme of ['light', 'dark'])
                for (const width of widths) {
                    const context = await browser.newContext({ viewport: { width, height: 900 } });
                    await context.addInitScript(
                        ([s, t]) => {
                            try {
                                localStorage.setItem('mynewsfeed-color-scheme', s);
                                localStorage.setItem('theme', t);
                            } catch {}
                        },
                        [scheme, theme],
                    );
                    const page = await context.newPage();
                    for (const path of pages) {
                        await page.goto(base + path, { waitUntil: 'domcontentloaded' });
                        await page.waitForTimeout(600);
                        const bad = await page.evaluate(audit);
                        if (bad.length) {
                            failures += bad.length;
                            const label = `${scheme} ${theme} ${width}px ${path}`;
                            if (report)
                                for (const b of bad)
                                    console.log(
                                        label,
                                        '|',
                                        b.where,
                                        '|',
                                        JSON.stringify(b.text),
                                        '|',
                                        b.fg,
                                        'on',
                                        b.bg,
                                        '|',
                                        b.ratio,
                                        '<',
                                        b.need,
                                    );
                            else assert.fail(`${label}: ${bad.length} unreadable, e.g. ${JSON.stringify(bad[0])}`);
                        }
                    }
                    await context.close();
                }
    } finally {
        await browser.close();
    }
    assert.equal(failures, 0, failures + ' unreadable text elements');
    console.log(
        `Text contrast passed for ${schemes.length} schemes × light/dark × ${widths.join('/')}px on ${pages.join(' ')}.`,
    );
})().catch((error) => {
    console.error(error.message || error);
    process.exit(1);
});
