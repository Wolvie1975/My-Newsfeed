const { chromium } = require('playwright');
const assert = require('node:assert/strict');
const base = process.env.BASE_URL || 'http://localhost:5295';
(async () => {
    const browser = await chromium.launch();
    try {
        const page = await browser.newPage();
        const errors = [];
        page.on('pageerror', (e) => errors.push(e.message));
        const periods = [
            { name: 'Today', summary: 'Sunny', isDaytime: true, temperature: 70, unit: 'F', wind: '5 mph S' },
            { name: 'Tonight', summary: 'Clear', isDaytime: false, temperature: 49, unit: 'F', wind: 'Calm' },
            {
                name: 'Tomorrow',
                summary: 'Rain And Thunderstorms',
                isDaytime: true,
                temperature: 60,
                unit: 'F',
                wind: '10 mph S',
            },
            { name: 'Unknown', summary: '<img src=x onerror=alert(1)>', temperature: 50, unit: 'F', wind: 'Calm' },
        ];
        await page.route('**/local/weather', (route) =>
            route.fulfill({ json: { periods, updated: new Date().toISOString(), stale: false } }),
        );
        for (const theme of ['light', 'dark']) {
            await page.goto(base + '/news/kansas-city');
            await page.evaluate((value) => localStorage.setItem('theme', value), theme);
            for (const width of [390, 768, 1440]) {
                await page.setViewportSize({ width, height: 1000 });
                await page.reload();
                if (width < 1024)
                    await page.getByText('Forecast', { exact: true }).locator('xpath=ancestor::summary').click();
                const host = width < 1024 ? '[data-phone-forecast]' : '#local-weather';
                await page
                    .locator(host + ' .weather-icon')
                    .first()
                    .waitFor();
                const cards = page.locator(host + ' .weather-period');
                assert.equal(await cards.count(), 4);
                for (const [index, kind] of ['sun', 'moon', 'storm', 'temperature'].entries()) {
                    const icon = cards.nth(index).locator('svg');
                    assert((await icon.getAttribute('class')).includes('weather-icon--' + kind));
                    assert.equal(await icon.getAttribute('aria-hidden'), 'true');
                    assert((await cards.nth(index).innerText()).includes(periods[index].summary));
                }
                assert.equal(await page.locator('.weather-period img').count(), 0);
                assert.equal(await page.evaluate(() => document.documentElement.scrollWidth > innerWidth), false);
                if (process.env.SCREENSHOT_DIR)
                    await page.screenshot({
                        path: `${process.env.SCREENSHOT_DIR}/weather-${width}-${theme}.png`,
                        fullPage: true,
                    });
            }
        }
        await page.unroute('**/local/weather');
        await page.route('**/local/weather', (route) => route.fulfill({ status: 503 }));
        await page.reload();
        await page.waitForFunction(() =>
            document.querySelector('[data-weather-status]').textContent.includes('unavailable'),
        );
        assert(await page.locator('[data-local-weather] a').isVisible());
        assert.deepEqual(errors, []);
        console.log(
            'Day/night, storm priority, unknown-condition safety, light/dark, responsive and unavailable-weather checks passed.',
        );
    } finally {
        await browser.close();
    }
})().catch((e) => {
    console.error(e);
    process.exit(1);
});
