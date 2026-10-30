// Folded phone layouts are covered by phone-fronts.browser.cjs; these checks exercise the retained tablet/desktop controls.
// Broadsheet homepage (October 25, 2026): masthead with the forecast and live ears, Where to watch as a full-width
// band under it (swapped with the forecast on October 26), three ruled columns and a "More from today" band on
// desktop; one ordered column on phones, Where to watch first.
const { chromium } = require('playwright');
const assert = require('node:assert/strict');
const base = process.env.BASE_URL || 'http://localhost:5295';

const phoneOrder = [
    'watch',
    'sports',
    'lead',
    'news',
    'opinion',
    'sports-news',
    'entertainment-news',
    'scoreboard',
    'rankings',
    'videos',
];

(async () => {
    const browser = await chromium.launch();
    try {
        for (const javaScriptEnabled of [true, false]) {
            const context = await browser.newContext({ javaScriptEnabled });
            const page = await context.newPage();
            const errors = [];
            page.on('pageerror', (error) => errors.push(error.message));
            await page.goto(base + '/');
            const box = (selector) => page.locator(selector).first().boundingBox();
            for (const width of [1024, 1280, 1440]) {
                await page.setViewportSize({ width, height: 1000 });
                assert(
                    await page.evaluate(() => document.documentElement.scrollWidth <= innerWidth),
                    'overflow ' + width,
                );
                // Every rendered section is laid out without overlapping another.
                const sections = await page.locator('.bs-home [data-home-section]:not([hidden])').evaluateAll((ns) =>
                    ns.map((n) => {
                        const r = n.getBoundingClientRect();
                        return {
                            key: n.dataset.homeSection,
                            x: r.x,
                            y: r.y + scrollY,
                            right: r.right,
                            bottom: r.bottom + scrollY,
                        };
                    }),
                );
                for (let i = 0; i < sections.length; i++)
                    for (const b of sections.slice(i + 1)) {
                        const a = sections[i];
                        assert(
                            a.right <= b.x + 1 || b.right <= a.x + 1 || a.bottom <= b.y + 1 || b.bottom <= a.y + 1,
                            `${a.key} overlaps ${b.key} at ${width}`,
                        );
                    }
                if (width >= 1024) {
                    const watch = await box('[data-home-section="watch"]');
                    const weather = await box('.bs-glance');
                    const title = await box('.bs-title');
                    const live = await box('.bs-live');
                    const lead = await box('[data-home-section="lead"]');
                    const tonight = await box('[data-home-section="sports"]');
                    const opinion = await box('[data-home-section="opinion"]');
                    assert(watch.y + watch.height < lead.y && watch.y + watch.height < tonight.y);
                    // Where to watch: a band under the whole masthead, as wide as the page.
                    assert(watch.y >= Math.max(weather.y + weather.height, live.y + live.height), 'Watch band below');
                    assert(watch.width >= live.x + live.width - weather.x - 1, 'Watch band full width');
                    if (width >= 1024) {
                        // Masthead: forecast ear left, title centre, live ear right.
                        assert(weather.x < title.x && title.x < live.x, 'masthead order');
                        // Three columns: Today & Tonight | lead and headlines | Opinion.
                        assert(tonight.x < lead.x && lead.x < opinion.x, 'front columns');
                        assert(Math.abs(tonight.y - lead.y) < 40 && Math.abs(lead.y - opinion.y) < 40, 'front row');
                    }
                    assert(await page.locator('.bs-band').isVisible());
                    const band = await box('.bs-band');
                    const scoreboard = await box('[data-home-section="scoreboard"]');
                    assert(band.y > lead.y && scoreboard.y > band.y, 'More from today band');
                    assert(await page.locator('.bs-title__name').isVisible());
                } else {
                    // Phones: one column in the approved order, Where to watch first.
                    const ys = Object.fromEntries(sections.map((s) => [s.key, s.y]));
                    const present = phoneOrder.filter((key) => key in ys);
                    for (let i = 1; i < present.length; i++)
                        assert(
                            ys[present[i - 1]] < ys[present[i]],
                            `${present[i - 1]} before ${present[i]} at ${width}`,
                        );
                    assert(!(await page.locator('.bs-band').isVisible()));
                    const watch = await box('[data-home-section="watch"]');
                    if (await page.locator('.bs-glance').count()) assert(watch.y < (await box('.bs-glance')).y);
                    assert(!(await page.locator('.bs-title__name').isVisible()));
                }
                assert.equal(await page.locator('.bs-agate__row[hidden]').count(), 0);
            }
            assert.deepEqual(errors, []);
            await context.close();
        }
        console.log(
            'Broadsheet home: masthead ears, three front columns and More from today on desktop; phone order with Where to watch first; no overlap or overflow at 320–1440px; JS and no-JS passed.',
        );
    } finally {
        await browser.close();
    }
})().catch((error) => {
    console.error(error);
    process.exit(1);
});
