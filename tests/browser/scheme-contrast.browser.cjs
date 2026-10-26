// Readable text under every colour scheme (user request, October 26, 2026): for each scheme in light and dark mode, at
// phone and desktop width, every visible piece of text in the header and page must meet WCAG AA contrast against the
// colour actually behind it — 4.5:1, or 3:1 for large text (24px+, or 18.66px+ bold). Text over pictures is skipped.
// REPORT=1 prints every failure instead of stopping at the first page with one; SCHEMES and PAGES narrow the run.
const { chromium } = require('playwright');
const assert = require('node:assert/strict');
const { audit } = require('./contrast-audit.cjs');
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
    'coastal',
    'olive',
    'golden-hour',
];
if (process.env.SCHEMES) schemes.splice(0, schemes.length, ...process.env.SCHEMES.split(','));
const pages = (process.env.PAGES || '/,/watch,/sports,/settings').split(',');
const widths = [390, 1440];

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
