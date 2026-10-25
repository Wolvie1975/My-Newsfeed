const { chromium } = require('playwright');
const assert = require('node:assert/strict');
const base = process.env.BASE_URL || 'http://localhost:5295';
(async () => {
    const browser = await chromium.launch();
    try {
        const page = await browser.newPage();
        await page.goto(base + '/settings');
        const choice = page.getByLabel('Paramount Plus Premium', { exact: true });
        assert(await choice.count());
        const id = await choice.getAttribute('value');
        await choice.check();
        await page.getByRole('button', { name: 'Save services', exact: true }).click();
        assert.equal(
            await page.evaluate(() => JSON.parse(localStorage.getItem('mynewsfeed-watch-services-v1'))[0]),
            Number(id),
        );
        await page.reload();
        assert(await choice.isChecked());
        await page.locator('a[href="watch"]:visible').first().click();
        await page.waitForURL('**services=*');
        await page.locator('a[href="settings"]:visible').first().click();
        await page.waitForURL('**/settings');
        assert(await choice.isChecked());
        await page.goto(base + '/watch?q=Arrival');
        await page.waitForURL('**services=*');
        assert(await page.locator('.watch-card').first().locator('.watch-match').count());
        assert.equal(await page.locator('[data-watch-services]').count(), 0);
        assert.equal(await page.locator('.site-nav--inline > a[href=watch]').count(), 1);
        await page.goto(base + '/watch');
        await page.waitForURL('**services=*');
        await page.getByRole('link', { name: 'TV shows', exact: true }).click();
        await page.waitForURL((u) => u.searchParams.get('kind') === 'tv');
        assert.equal(new URL(page.url()).searchParams.get('services'), id);
        await page.goto(base + '/settings');
        await page.setViewportSize({ width: 390, height: 900 });
        assert.equal(await page.evaluate(() => document.documentElement.scrollWidth > innerWidth), false);
        await page.locator('#my-services').getByRole('button', { name: 'Clear selections', exact: true }).click();
        assert.deepEqual(
            await page.evaluate(() => JSON.parse(localStorage.getItem('mynewsfeed-watch-services-v1'))),
            [],
        );
        const plain = await browser.newContext({ javaScriptEnabled: false });
        const p = await plain.newPage();
        await p.goto(base + '/settings');
        await p.getByLabel('Paramount Plus Premium', { exact: true }).check();
        await p.getByRole('button', { name: 'Save services', exact: true }).click();
        await p.locator('[data-watch-settings-link]').click();
        assert.equal(new URL(p.url()).searchParams.get('services'), id);
        await p.goto(base + '/watch?q=Arrival&services=' + id);
        assert(await p.locator('.watch-match').count());
        await plain.close();
        console.log(
            'Settings service selection, persistence, ranking, TV navigation, clear, mobile and no-JavaScript form checks passed.',
        );
    } finally {
        await browser.close();
    }
})().catch((e) => {
    console.error(e);
    process.exit(1);
});
