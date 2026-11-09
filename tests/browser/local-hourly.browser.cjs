const { chromium } = require('playwright');
const lib = require('./lib.cjs');
const assert = require('node:assert/strict');
const base = lib.base;
const shots = lib.screenshots('local-hourly');

(async () => {
    const browser = await chromium.launch();
    try {
        const page = await lib.readerPage(browser);
        const errors = [];
        page.on('pageerror', (e) => errors.push(e.message));

        const hoursData = [
            {
                label: '4 PM',
                starts: '2026-11-06T16:00:00-06:00',
                isDaytime: true,
                temperature: 48,
                unit: 'F',
                summary: 'Slight Chance Rain Showers then Mostly Cloudy',
                rainChance: 10,
                wind: '5 mph NNW',
            },
            {
                label: '5 PM',
                starts: '2026-11-06T17:00:00-06:00',
                isDaytime: true,
                temperature: 47,
                unit: 'F',
                summary: 'Mostly Cloudy',
                rainChance: 15,
                wind: '5 mph NNW',
            },
            {
                label: '6 PM',
                starts: '2026-11-06T18:00:00-06:00',
                isDaytime: false,
                temperature: 45,
                unit: 'F',
                summary: 'Chance Rain Showers',
                rainChance: 30,
                wind: '5 mph NNW',
            },
            {
                label: '7 PM',
                starts: '2026-11-06T19:00:00-06:00',
                isDaytime: false,
                temperature: 44,
                unit: 'F',
                summary: 'Chance Rain Showers',
                rainChance: 40,
                wind: '5 mph NNW',
            },
            {
                label: '8 PM',
                starts: '2026-11-06T20:00:00-06:00',
                isDaytime: false,
                temperature: 42,
                unit: 'F',
                summary: 'Slight Chance Rain Showers',
                rainChance: 20,
                wind: '5 mph NNW',
            },
            {
                label: '9 PM',
                starts: '2026-11-06T21:00:00-06:00',
                isDaytime: false,
                temperature: 41,
                unit: 'F',
                summary: 'Mostly Cloudy',
                rainChance: 10,
                wind: '5 mph NNW',
            },
            {
                label: '10 PM',
                starts: '2026-11-06T22:00:00-06:00',
                isDaytime: false,
                temperature: 40,
                unit: 'F',
                summary: 'Partly Cloudy',
                rainChance: 5,
                wind: '5 mph NNW',
            },
            {
                label: '11 PM',
                starts: '2026-11-06T23:00:00-06:00',
                isDaytime: false,
                temperature: 39,
                unit: 'F',
                summary: 'Mostly Clear',
                rainChance: null,
                wind: '5 mph NNW',
            },
        ];

        const mockHourly = {
            location: 'Kansas City',
            timeZone: 'America/Chicago',
            updated: '2026-11-06T21:22:09Z',
            stale: false,
            rainSummary: 'Rain most likely 6–8 PM, up to 40% at 7 PM',
            hours: hoursData,
        };
        let hourlyFail = false;

        await page.route('**/local/weather/hourly*', (route) => {
            if (hourlyFail) return route.fulfill({ status: 503 });
            const url = new URL(route.request().url());
            if (url.searchParams.get('lat') === '39.74' && url.searchParams.get('lon') === '-104.99') {
                return route.fulfill({
                    json: {
                        location: 'Denver, CO',
                        timeZone: 'America/Denver',
                        updated: '2026-11-06T21:22:09Z',
                        stale: false,
                        rainSummary: 'Low chance of rain through 10 PM, up to 10%',
                        hours: hoursData.map((h, i) => ({
                            ...h,
                            label: (3 + i) % 12 === 0 ? '12 PM' : `${(3 + i) % 12} ${3 + i >= 12 ? 'PM' : 'AM'}`,
                        })),
                    },
                });
            }
            return route.fulfill({ json: mockHourly });
        });

        const widths = lib.widths([320, 390, 430, 768, 1024, 1440, 1920]);
        for (const theme of lib.THEMES) {
            await page.goto(base + '/news/kansas-city');
            await page.evaluate((value) => localStorage.setItem('theme', value), theme);
            for (const width of widths) {
                await page.setViewportSize({ width, height: 950 });
                await page.reload();
                await page.locator('.hw[data-weather-hourly]').waitFor({ state: 'visible' });

                const items = page.locator('.hw__hour');
                assert.equal(await items.count(), 8, `Exactly 8 hourly items at ${width}px`);

                const firstLabel = await items.first().locator('.hw__time').innerText();
                assert.equal(firstLabel, '4 PM');

                const lastPop = await items.last().locator('.hw__pop').innerText();
                assert.equal(lastPop, '—', `Missing rain chance shows — at ${width}px`);

                const srText = await items.first().locator('.sr-only').innerText();
                assert(
                    srText.includes('4 PM, 48 degrees') &&
                        srText.includes('slight chance rain showers then mostly cloudy') &&
                        srText.includes('10 percent chance of rain'),
                    `Accessible sentence includes conditions: ${srText}`,
                );

                const srTextMissing = await items.last().locator('.sr-only').innerText();
                assert(
                    srTextMissing.includes('chance of rain not given'),
                    `Accessible sentence handles missing rain: ${srTextMissing}`,
                );

                assert.equal(
                    await page.evaluate(() => document.documentElement.scrollWidth <= innerWidth),
                    true,
                    `No horizontal overflow at ${width}px`,
                );

                if (width >= 1024) {
                    const condHeight = await page.evaluate(() => {
                        const cond = document.querySelector('.hw__cond');
                        return cond.clientHeight;
                    });
                    const lineHeight = await page.evaluate(() => {
                        const cond = document.querySelector('.hw__cond');
                        return parseFloat(getComputedStyle(cond).lineHeight);
                    });
                    assert(
                        condHeight <= lineHeight * 2.5,
                        `Condition text stays within 2 lines on desktop at ${width}px`,
                    );
                }

                if (lib.shotDir) {
                    await page.screenshot({
                        path: `${shots}/hw-${width}-${theme}.png`,
                        fullPage: false,
                    });
                }
            }
        }

        // 2. Failure test: 503 on hourly endpoint -> cards untouched, unavailable line shown
        hourlyFail = true;
        await page.reload();
        await page.locator('#local-weather .weather-period').first().waitFor({ state: 'visible' });
        assert.equal(await page.locator('#local-weather .weather-period').count(), 4, '4 forecast cards remain');
        assert(await page.locator('[data-weather-hourly-unavailable]').isVisible(), 'Unavailable line is visible');
        assert.equal(await page.locator('.hw__hours').isHidden(), true, 'Hourly list is hidden on failure');
        hourlyFail = false;

        // 3. Location test: grant geolocation to Denver -> zone label updates
        const context = page.context();
        await context.grantPermissions(['geolocation']);
        await context.setGeolocation({ latitude: 39.7392, longitude: -104.9903 });
        await page.reload();
        const locateBtn = page.locator('[data-weather-locate]');
        await locateBtn.waitFor({ state: 'visible' });
        const hourlyResponse = page.waitForResponse(
            (r) => r.url().includes('/local/weather/hourly') && r.status() === 200,
        );
        await locateBtn.click();
        await hourlyResponse;
        await page.waitForTimeout(200);
        const zoneText = await page.locator('[data-weather-hourly-zone]').innerText();
        assert(zoneText.includes('MST') || zoneText.includes('MDT'), `Zone updated to Denver: ${zoneText}`);

        assert.equal(errors.length, 0, `No console errors: ${errors.join(', ')}`);
        console.log(
            'Local hourly check: 8 columns desktop, 8 rows phone, clamp, missing pop, failure & location passed.',
        );
    } finally {
        await browser.close();
    }
})().catch((err) => {
    console.error(err);
    process.exit(1);
});
