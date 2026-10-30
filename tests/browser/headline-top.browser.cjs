const { chromium } = require('playwright');
const assert = require('node:assert/strict');
const fs = require('node:fs');
const base = process.env.BASE_URL || 'http://127.0.0.1:5398';
(async () => {
    const browser = await chromium.launch();
    const shots = process.env.SCREENSHOT_DIR || 'test-results/browser/';
    fs.mkdirSync(shots, { recursive: true });
    try {
        for (const width of [320, 390, 430, 744, 1024, 1280, 1440]) {
            for (const theme of ['light', 'dark']) {
                for (const kind of ['wide', 'compact', 'broken']) {
                    const page = await browser.newPage({
                        viewport: { width, height: 1000 },
                        colorScheme: theme,
                        storageState: process.env.STORAGE_STATE,
                    });
                    await page.addInitScript((t) => localStorage.setItem('theme', t), theme);
                    await page.route('**/*', (route) => {
                        if (
                            route.request().resourceType() !== 'image' ||
                            new URL(route.request().url()).origin === new URL(base).origin
                        )
                            return route.continue();
                        if (kind === 'broken') return route.fulfill({ status: 404, body: '' });
                        return route.fulfill({
                            contentType: 'image/svg+xml',
                            body: `<svg xmlns="http://www.w3.org/2000/svg" width="${kind === 'wide' ? 1280 : 240}" height="${kind === 'wide' ? 720 : 135}"><rect width="100%" height="100%" fill="#3b738c"/></svg>`,
                        });
                    });
                    await page.goto(base + (width < 1024 ? '/sports/news' : '/news/all'), { waitUntil: 'networkidle' });
                    // All headlines may use a dedicated phone front; test the shared card feed on a hub there.
                    if (!(await page.locator('[data-hc-top]').count()))
                        await page.goto(base + '/sports/news', { waitUntil: 'networkidle' });
                    await page.locator('[data-hc-top]').waitFor({ state: 'attached' });
                    await page.waitForTimeout(200);
                    assert(
                        await page.evaluate(() => document.documentElement.scrollWidth <= innerWidth),
                        `${width} overflow`,
                    );
                    const ids = await page
                        .locator('.hc-feed article[data-article-id]:visible')
                        .evaluateAll((ns) => ns.map((n) => n.dataset.articleId));
                    assert(ids.length > 4, 'Representative stories required');
                    assert.equal(new Set(ids).size, ids.length, 'Visible duplicate stories');
                    if (width >= 1024) {
                        assert.equal(await page.locator('.hc-under [data-article-media]:visible').count(), 2);
                        assert.equal(
                            await page.locator('.hc-row:not(.hc-row--first) [data-article-media]:visible').count(),
                            0,
                        );
                        const cat = await page.locator('.hc-lead .cat').boundingBox();
                        const title = await page.locator('.hc-lead .title').boundingBox();
                        if (cat && title) assert(cat.y + cat.height <= title.y + 1, 'Category overlaps headline');
                    }
                    assert(await page.locator('.hc-top--photo').count(), 'Always uses the photo arrangement');
                    if (width >= 1024 && kind !== 'wide')
                        assert(await page.locator('.hc-lead .ph').isVisible(), 'P8 replaces compact and broken photos');
                    if ([390, 744, 1440].includes(width))
                        await page.screenshot({
                            path: `${shots}headline-top-${width}-${theme}-${kind}.png`,
                            fullPage: false,
                        });
                    const share = page.locator('.hc-feed [data-share-open]:visible').first();
                    await share.click();
                    await page.getByRole('dialog').waitFor();
                    await page.keyboard.press('Escape');
                    await page.close();
                }
            }
        }
        const page = await browser.newPage({
            javaScriptEnabled: false,
            storageState: process.env.STORAGE_STATE,
            viewport: { width: 1440, height: 1000 },
        });
        await page.goto(base + '/sports/news');
        assert(await page.locator('.hc-top--photo').count(), 'No JS retains the photo arrangement');
        assert(await page.locator('.hc-lead .ph').isVisible(), 'No JS retains the P8 frame');
        console.log(
            'Passed responsive layouts, image failures, visible uniqueness, share interactions, screenshots and no-JS P8 frames.',
        );
    } finally {
        await browser.close();
    }
})().catch((e) => {
    console.error(e);
    process.exit(1);
});
