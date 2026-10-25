const { chromium } = require('playwright');
const assert = require('node:assert/strict');
const base = process.env.BASE_URL || 'http://localhost:5295';
const path = process.env.BASEBALL_PATH || '/box-score/baseball/mlb/401930515';
(async () => {
    const browser = await chromium.launch();
    try {
        const page = await browser.newPage(),
            errors = [];
        page.on('pageerror', (e) => errors.push(e.message));
        for (const theme of ['light', 'dark']) {
            await page.goto(base + path);
            await page.evaluate((t) => localStorage.setItem('theme', t), theme);
            for (const width of [390, 768, 1180, 1440]) {
                await page.setViewportSize({ width, height: 1000 });
                assert.equal((await page.reload()).status(), 200);
                const grids = page.locator(
                    '.baseball-team-grid[aria-label="Team hitting tables"],.baseball-team-grid[aria-label="Team pitching tables"]',
                );
                assert.equal(await grids.count(), 2);
                for (const grid of await grids.all()) {
                    const positions = await grid
                        .locator(':scope > section')
                        .evaluateAll((nodes) =>
                            nodes.map((n) => ({ x: n.getBoundingClientRect().x, y: n.getBoundingClientRect().y })),
                        );
                    assert.equal(positions.length, 2);
                    if (width >= 1000) assert(Math.abs(positions[0].y - positions[1].y) < 1);
                    else assert(positions[1].y > positions[0].y);
                }
                const headers = await page
                    .locator('.baseball-team-grid')
                    .first()
                    .locator('.baseball-table')
                    .first()
                    .locator('thead th')
                    .allTextContents();
                assert.deepEqual(headers, ['Hitters', 'AB', 'R', 'H', 'RBI', 'HR', 'BB', 'K', 'AVG', 'OBP', 'SLG']);
                assert.equal(await page.locator('.baseball-scoreboard tbody tr').count(), 2);
                assert.equal(await page.evaluate(() => document.documentElement.scrollWidth > innerWidth), false);
                await page.locator('.baseball-extra summary').first().click();
                assert(await page.locator('.baseball-extra[open] table').count());
                if (process.env.SCREENSHOT_DIR)
                    await page.screenshot({
                        path: `${process.env.SCREENSHOT_DIR}/baseball-${width}-${theme}.png`,
                        fullPage: true,
                    });
            }
        }
        // The repaired cache supplies verified descriptive inning labels.
        const periods = await page.locator('.baseball-lines thead th').allTextContents();
        assert.deepEqual(periods.slice(1, 10), [
            '1st inning',
            '2nd inning',
            '3rd inning',
            '4th inning',
            '5th inning',
            '6th inning',
            '7th inning',
            '8th inning',
            '9th inning',
        ]);
        assert.equal(await page.locator('.baseball-scoreboard tbody tr').first().locator('td').count(), 12);
        assert.equal(await page.locator('.baseball-scoreboard tbody tr').last().locator('td').count(), 12);
        assert.deepEqual(errors, []);
        console.log(
            'Eight baseball theme/width layouts passed: paired desktop panels, mobile stacking, reference columns, extra stats, all period values and no overflow.',
        );
    } finally {
        await browser.close();
    }
})().catch((e) => {
    console.error(e);
    process.exit(1);
});
