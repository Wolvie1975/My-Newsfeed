// The phone focal forecast, tiles and full outlook all share one request with the retained wider layout.
const { chromium } = require('playwright');
const assert = require('node:assert/strict');
const base = process.env.BASE_URL || 'http://localhost:5295';
(async () => {
    if ((await fetch(base + '/editions')).status !== 404) {
        console.log('SKIP: this fixture requires a profiles-off preview.');
        return;
    }
    const browser = await chromium.launch();
    try {
        for (const width of [320, 390, 430])
            for (const theme of ['light', 'dark']) {
                const page = await browser.newPage({ viewport: { width, height: 900 } });
                await page.addInitScript((t) => localStorage.setItem('theme', t), theme);
                let mode = 'single',
                    requests = 0;
                await page.route('**/local/weather*', (route) => {
                    requests++;
                    return mode === 'failed'
                        ? route.fulfill({ status: 503 })
                        : route.fulfill({
                              json: {
                                  location: 'Kansas City',
                                  updated: '2026-10-28T12:00:00Z',
                                  stale: true,
                                  periods:
                                      mode === 'empty'
                                          ? []
                                          : [
                                                {
                                                    name: 'Tonight',
                                                    temperature: 61,
                                                    unit: 'F',
                                                    summary: 'Mostly clear',
                                                    wind: 'Calm',
                                                    isDaytime: false,
                                                },
                                            ],
                              },
                          });
                });
                for (mode of ['single', 'empty', 'failed']) {
                    requests = 0;
                    await page.goto(base + '/news/kansas-city', { waitUntil: 'networkidle' });
                    assert.equal(requests, 1, 'one request per load');
                    assert.equal(await page.locator('[data-weather-tiles] article').count(), 0);
                    if (mode === 'single') {
                        assert((await page.locator('[data-weather-feature]').innerText()).includes('61°F'));
                        assert((await page.locator('[data-weather-feature]').innerText()).includes('TONIGHT'));
                        assert(
                            (await page.locator('[data-weather-status]').innerText()).includes('may be out of date'),
                        );
                        assert.equal(await page.locator('[data-phone-forecast] .weather-period').count(), 1);
                        await page.setViewportSize({ width: 1024, height: 900 });
                        assert(!(await page.locator('[data-weather-feature]').isVisible()));
                        assert(!(await page.locator('[data-weather-tiles]').isVisible()));
                        assert(await page.locator('#local-weather .weather-period').isVisible());
                        assert.equal(requests, 1, 'resizing reuses the same forecast');
                        await page.setViewportSize({ width, height: 900 });
                    } else {
                        assert.equal(await page.locator('[data-weather-feature]').textContent(), '');
                        assert(
                            (await page.locator('[data-weather-status]').innerText()).includes(
                                'temporarily unavailable',
                            ),
                        );
                        assert(
                            (await page.locator('[data-phone-forecast]').textContent()).includes(
                                'temporarily unavailable',
                            ),
                        );
                    }
                    assert(await page.locator('[data-weather-link]').isVisible());
                    assert(await page.evaluate(() => document.documentElement.scrollWidth <= innerWidth));
                }
                await page.close();
            }
        console.log(
            'Phone weather: 320/390/430 light/dark, single labelled period, stale attribution, empty/503 fallback, zero fabricated tiles and one request across phone/desktop resizing passed.',
        );
    } finally {
        await browser.close();
    }
})().catch((e) => {
    console.error(e);
    process.exit(1);
});
