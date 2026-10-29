// Reaching Kansas City through enhanced navigation must still load the forecast, not leave the placeholder.
const { chromium } = require('playwright');
const assert = require('node:assert/strict');
const base = process.env.BASE_URL || 'http://localhost:5295';
(async () => {
    const browser = await chromium.launch();
    try {
        for (const from of ['/news', '/news/all', '/', '/sports']) {
            const page = await browser.newPage({ viewport: { width: 1440, height: 1000 } });
            const errors = [];
            page.on('pageerror', (e) => errors.push(e.message));
            await page.route('**/local/weather*', (r) =>
                r.fulfill({
                    json: {
                        location: 'Kansas City',
                        updated: new Date().toISOString(),
                        stale: false,
                        periods: [
                            {
                                name: 'Today',
                                temperature: 63,
                                unit: 'F',
                                summary: 'Partly Cloudy',
                                wind: 'S 5 mph',
                                isDaytime: true,
                            },
                        ],
                    },
                }),
            );
            const response = await page.goto(base + from, { waitUntil: 'networkidle' });
            assert.equal(response.status(), 200, 'Use an ungated local preview');
            await page.evaluate(() => {
                const link = document.createElement('a');
                link.href = 'news/kansas-city';
                link.textContent = 'go';
                link.id = 'go-kc';
                document.body.prepend(link);
            });
            await page.click('#go-kc');
            await page.waitForURL('**/news/kansas-city');
            const host = page.locator('[data-local-weather]');
            await host.locator('.weather-period').first().waitFor({ timeout: 5000 });
            assert.match(
                await host.locator('[data-weather-status]').textContent(),
                /National Weather Service · updated/,
            );
            assert.deepEqual(errors, []);
            console.log('ok', from);
            await page.close();
        }
    } finally {
        await browser.close();
    }
})().catch((e) => {
    console.error(e);
    process.exit(1);
});
