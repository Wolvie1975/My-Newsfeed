const { chromium } = require('playwright');
const assert = require('node:assert/strict');
const base = process.env.BASE_URL || 'http://localhost:5295';
(async () => {
    const browser = await chromium.launch();
    try {
        const page = await browser.newPage();
        const errors = [];
        page.on('pageerror', (e) => errors.push(e.message));
        const link = page.locator('[data-box-score][href*="football/nfl/"]').first();
        const day = (offset) =>
            new Date(Date.now() + offset * 864e5).toLocaleDateString('en-CA', { timeZone: 'America/Chicago' });
        for (const path of process.env.RESULTS_PATH
            ? [process.env.RESULTS_PATH]
            : [0, -1].map((d) => `/scoreboard?status=final&day=${day(d)}`)) {
            await page.goto(base + path);
            if (await link.count()) break;
        }
        if (!(await link.count())) {
            console.log('SKIP: no cached NFL box score today or yesterday; set RESULTS_PATH to a page with one.');
            return;
        }
        const href = await link.getAttribute('href');
        for (const theme of ['light', 'dark'])
            for (const width of [390, 768, 1440]) {
                await page.setViewportSize({ width, height: 900 });
                await page.evaluate((t) => localStorage.setItem('theme', t), theme);
                await page.reload();
                await page.locator(`[href="${href}"]`).click();
                await page.waitForSelector('.box-score-dialog[open] .football-category');
                const categories = page.locator('.box-score-dialog .football-category');
                assert(await categories.count());
                assert.equal(await categories.first().locator('h3').textContent(), 'Passing');
                const panels = categories.first().locator('.football-team-panel');
                assert.equal(await panels.count(), 2);
                const a = await panels.nth(0).boundingBox(),
                    b = await panels.nth(1).boundingBox();
                if (width >= 1000) assert(Math.abs(a.y - b.y) < 2);
                else assert(b.y >= a.y + a.height);
                assert(await panels.first().locator('table tbody tr').count());
                assert.equal(await page.evaluate(() => document.documentElement.scrollWidth > innerWidth), false);
                if (process.env.SCREENSHOT_DIR)
                    await page.screenshot({ path: `${process.env.SCREENSHOT_DIR}/football-${width}-${theme}.png` });
                await page.keyboard.press('Escape');
                await page.waitForSelector('.box-score-dialog', { state: 'detached' });
                assert.equal(await page.evaluate(() => document.activeElement?.getAttribute('href')), href);
            }
        assert.deepEqual(errors, []);
        console.log(
            'Six football modal theme/width layouts passed: ordered categories, paired desktop teams, mobile stacking, no overflow and keyboard/focus return.',
        );
    } finally {
        await browser.close();
    }
})().catch((e) => {
    console.error(e);
    process.exit(1);
});
