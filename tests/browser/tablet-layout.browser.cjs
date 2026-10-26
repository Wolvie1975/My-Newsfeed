// iPad portrait gets the desktop page design (user request, October 26, 2026): from 744px the home page uses its
// ruled columns while the header keeps the phone bar and Menu sheet until 1024px. At every iPad portrait width, plus
// a phone and a desktop width either side, no page may scroll sideways and pages must load without script errors.
const { chromium } = require('playwright');
const assert = require('node:assert/strict');
const base = process.env.BASE_URL || 'http://localhost:5295';

const pages = (
    process.env.PAGES ||
    '/,/news,/local,/sports,/scoreboard,/standings,/rankings,/college,/f1,/entertainment,/movies,/tv,/box-office,/watch,/videos,/saved,/settings'
).split(',');
const widths = [430, 743, 744, 768, 820, 834, 1023, 1024, 1440];

(async () => {
    const browser = await chromium.launch();
    try {
        for (const theme of ['light', 'dark'])
            for (const width of widths) {
                const context = await browser.newContext({ viewport: { width, height: 1100 } });
                await context.addInitScript((t) => {
                    try {
                        localStorage.setItem('theme', t);
                    } catch {}
                }, theme);
                const page = await context.newPage();
                const errors = [];
                page.on('pageerror', (e) => errors.push(e.message));
                for (const path of pages) {
                    await page.goto(base + path, { waitUntil: 'domcontentloaded' });
                    await page.waitForTimeout(400);
                    const r = await page.evaluate(() => {
                        const over = document.documentElement.scrollWidth - innerWidth;
                        const wide =
                            over > 0
                                ? [...document.querySelectorAll('body *')]
                                      .filter((e) => e.getBoundingClientRect().right > innerWidth + 1)
                                      .slice(0, 3)
                                      .map((e) => e.tagName.toLowerCase() + '.' + [...e.classList].join('.'))
                                : [];
                        const menu = document.querySelector('.mobile-nav');
                        const front = document.querySelector('.bs-front');
                        return {
                            over,
                            wide,
                            menu: !!menu && getComputedStyle(menu).display !== 'none',
                            columns: front ? getComputedStyle(front).display === 'grid' : null,
                        };
                    });
                    const label = `${theme} ${width}px ${path}`;
                    assert.equal(r.over, 0, `${label} scrolls sideways: ${r.wide.join(', ')}`);
                    assert.equal(r.menu, width < 1024, `${label}: Menu button ${r.menu ? 'shown' : 'hidden'}`);
                    if (r.columns !== null)
                        assert.equal(r.columns, width >= 744, `${label}: home ruled columns ${r.columns}`);
                    assert.deepEqual(errors.splice(0), [], `${label}: script errors`);
                }
                await context.close();
            }
    } finally {
        await browser.close();
    }
    console.log(
        `Tablet layout: ${pages.length} pages at ${widths.join('/')}px light/dark, desktop columns from 744px, Menu bar below 1024px, no sideways scroll.`,
    );
})().catch((e) => {
    console.error(e.message || e);
    process.exit(1);
});
