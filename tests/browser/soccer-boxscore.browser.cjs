const { chromium } = require('playwright');
const lib = require('./lib.cjs');
const assert = require('node:assert/strict');
const base = lib.base;
(async () => {
    const browser = await chromium.launch();
    try {
        const page = await lib.readerPage(browser);
        const errors = [];
        page.on('pageerror', (e) => errors.push(e.message));
        await page.goto(base + '/mls/results');
        const link = page.locator('[data-box-score]').first();
        assert(await link.count());
        const href = await link.getAttribute('href');
        for (const theme of lib.THEMES)
            for (const width of [390, 768, 1440]) {
                await page.setViewportSize({ width, height: 900 });
                await page.evaluate((t) => localStorage.setItem('theme', t), theme);
                await page.reload();
                await page.locator(`[href="${href}"]`).click();
                await page.waitForSelector('.box-score-dialog[open] .soccer-comparison');
                const table = page.locator('.box-score-dialog .soccer-comparison').first();
                assert(await table.isVisible());
                assert.equal(await table.locator('thead th').count(), 3);
                const rows = table.locator('tbody tr');
                assert(await rows.count());
                for (const row of await rows.all()) {
                    assert.equal(await row.locator('th[scope=row]').count(), 1);
                    assert.equal(await row.locator('td').count(), 2);
                }
                assert.equal(await page.evaluate(() => document.documentElement.scrollWidth > innerWidth), false);
                if (lib.shotDir) await page.screenshot({ path: `${lib.shotDir}/soccer-${width}-${theme}.png` });
                await page.keyboard.press('Escape');
                await page.waitForSelector('.box-score-dialog', { state: 'detached' });
                assert.equal(await page.evaluate(() => document.activeElement?.getAttribute('href')), href);
            }
        assert.deepEqual(errors, []);
        console.log(
            'Six soccer modal theme/width layouts passed: visible team comparisons, aligned rows, no overflow and keyboard/focus return.',
        );
    } finally {
        await browser.close();
    }
})().catch((e) => {
    console.error(e);
    process.exit(1);
});
