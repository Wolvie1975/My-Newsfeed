const { chromium } = require('playwright');
const assert = require('node:assert/strict');
const base = process.env.BASE_URL || 'http://localhost:5295';
(async () => {
    const browser = await chromium.launch();
    try {
        const page = await browser.newPage();
        await page.goto(base + '/watch/movie/329865');
        assert.equal(await page.locator('[data-track-series]').count(), 0);
        await page.goto(base + '/watch?kind=tv&q=Breaking%20Bad');
        await page.locator('[data-watch-title]').first().click();
        await page.waitForSelector('dialog[open] [data-track-series]');
        const link = page.locator('dialog [data-track-series]');
        const href = await link.getAttribute('href');
        assert(href.includes('tv/track?q=Breaking%20Bad'));
        assert(href.includes('year=2008'));
        await link.click();
        await page.waitForURL('**/tv/track?**');
        assert.equal(await page.locator('#tv-query').inputValue(), 'Breaking Bad');
        await page.waitForSelector('table tbody tr');
        assert(await page.locator('table tbody tr').count());
        assert((await page.locator('.alert-info').textContent()).includes('2008'));
        assert.equal(await page.locator('input[name=password]').count(), 0);
        await page.waitForSelector('table button');
        assert(await page.locator('table button').count());
        await page.setViewportSize({ width: 390, height: 900 });
        assert.equal(await page.evaluate(() => document.documentElement.scrollWidth > innerWidth), false);
        const protectedResponse = await page.request.get(base + '/admin/tv-series', { maxRedirects: 0 });
        assert([302, 401, 403].includes(protectedResponse.status()));
        if (protectedResponse.status() === 302) assert(protectedResponse.headers().location.includes('login'));
        console.log(
            'Anonymous TV tracking page, modal navigation, prefilled TVmaze results, movie exclusion and mobile layout passed; admin list remains protected. No production series was added by this check.',
        );
    } finally {
        await browser.close();
    }
})().catch((e) => {
    console.error(e);
    process.exit(1);
});
