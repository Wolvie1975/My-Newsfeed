const { chromium } = require('playwright');
const assert = require('node:assert/strict');
const base = process.env.BASE_URL || 'http://localhost:5295';
(async () => {
    const browser = await chromium.launch();
    try {
        const page = await browser.newPage();
        const read = () =>
            page
                .locator('[data-settings-sections]>[data-settings-section]')
                .evaluateAll((ns) => ns.map((n) => n.dataset.settingsSection));
        await page.goto(base + '/');
        await page.locator('a[href="settings"]:visible').first().click();
        await page.waitForURL('**/settings');
        await page.waitForSelector('[data-settings-move]', { state: 'attached' });
        assert.equal(await page.locator('[data-settings-move]').count(), 10);
        const original = await read();
        assert.equal(original.length, 5);
        await page.locator('.settings-arrange summary').click();
        await page.locator('[data-settings-move="teams:-1"]').click();
        assert.deepEqual((await read()).slice(-2), ['teams', 'services']);
        await page.reload();
        assert.deepEqual((await read()).slice(-2), ['teams', 'services']);
        await page.locator('.settings-arrange summary').click();
        await page.setViewportSize({ width: 390, height: 900 });
        assert.equal(await page.evaluate(() => document.documentElement.scrollWidth > innerWidth), false);
        await page.locator('[data-settings-order-reset]').click();
        assert.deepEqual(await read(), original);
        await page.evaluate(() => localStorage.setItem('mynewsfeed-settings-order-v1', '{"bad":true}'));
        await page.reload();
        assert.deepEqual(await read(), original);
        await page.goto(base + '/watch');
        await page.locator('a[href="settings"]:visible').first().click();
        await page.waitForURL('**/settings');
        await page.waitForSelector('[data-settings-move]', { state: 'attached' });
        assert.equal(await page.locator('[data-settings-move]').count(), 10);
        console.log(
            'In-site navigation and five Settings sections reorder, persist, reset and handle malformed storage with no phone overflow.',
        );
    } finally {
        await browser.close();
    }
})().catch((e) => {
    console.error(e);
    process.exit(1);
});
