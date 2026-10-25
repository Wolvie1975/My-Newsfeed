const { chromium } = require('playwright');
const assert = require('node:assert/strict');
const base = process.env.BASE_URL || 'http://localhost:5295';
const data = (location) => ({
    location,
    updated: new Date().toISOString(),
    stale: false,
    periods: [
        {
            name: 'Today',
            temperature: location === 'Kansas City' ? 70 : 55,
            unit: 'F',
            summary: 'Clear',
            wind: 'Calm',
            isDaytime: true,
        },
    ],
});
(async () => {
    const browser = await chromium.launch({
        args: process.env.PWA_HOST_IP
            ? ['--host-resolver-rules=MAP ' + new URL(base).hostname + ' ' + process.env.PWA_HOST_IP]
            : [],
    });
    try {
        for (const mode of ['success', 'denied', 'timeout', 'unavailable', 'unsupported', 'reset-pending']) {
            const context = await browser.newContext({
                permissions: ['geolocation'],
                geolocation: { latitude: 40.7128, longitude: -74.006 },
            });
            if (['denied', 'timeout', 'unavailable', 'reset-pending'].includes(mode))
                await context.addInitScript(
                    (mode) =>
                        Object.defineProperty(navigator, 'geolocation', {
                            value: {
                                getCurrentPosition: (ok, fail) =>
                                    mode === 'reset-pending'
                                        ? setTimeout(
                                              () => ok({ coords: { latitude: 40.7128, longitude: -74.006 } }),
                                              250,
                                          )
                                        : fail({ code: mode === 'denied' ? 1 : mode === 'timeout' ? 3 : 2 }),
                            },
                        }),
                    mode,
                );
            const page = await context.newPage();
            const errors = [],
                requests = [];
            page.on('pageerror', (e) => errors.push(e.message));
            await page.route('**/local/weather*', (r) => {
                const u = new URL(r.request().url());
                requests.push(u);
                return r.fulfill({
                    json:
                        mode === 'unsupported' && u.searchParams.has('lat')
                            ? { periods: [] }
                            : data(u.searchParams.has('lat') ? 'New York, NY' : 'Kansas City'),
                });
            });
            await page.goto(base + '/local');
            await page.getByRole('heading', { name: 'Kansas City forecast', exact: true }).waitFor();
            await page.locator('.weather-period').waitFor();
            assert(
                requests.every((u) => !u.searchParams.has('lat')),
                'No automatic location request',
            );
            await page.getByRole('button', { name: 'Use my location', exact: true }).click();
            if (mode === 'success') {
                await page.getByRole('heading', { name: 'New York, NY forecast', exact: true }).waitFor();
                assert(
                    requests.some(
                        (u) => u.searchParams.get('lat') === '40.71' && u.searchParams.get('lon') === '-74.01',
                    ),
                );
                assert((await page.locator('[data-weather-link]').getAttribute('href')).includes('lat=40.71'));
                assert.equal(await page.locator('.weather-temperature').innerText(), '55°F');
                for (const width of [390, 1440]) {
                    await page.setViewportSize({ width, height: 950 });
                    assert(await page.evaluate(() => document.documentElement.scrollWidth <= innerWidth));
                }
                await page.getByRole('button', { name: 'Use Kansas City', exact: true }).click();
                await page.getByRole('heading', { name: 'Kansas City forecast', exact: true }).waitFor();
                await page.waitForFunction(
                    () => document.querySelector('.weather-temperature')?.textContent === '70°F',
                );
            } else {
                if (mode === 'reset-pending') {
                    await page.getByRole('button', { name: 'Use Kansas City', exact: true }).click();
                    await page.waitForTimeout(350);
                }
                await page.waitForFunction(() =>
                    document
                        .querySelector('[data-weather-location-status]')
                        .textContent.includes('Showing Kansas City'),
                );
                await page.waitForFunction(
                    () => document.querySelector('.weather-temperature')?.textContent === '70°F',
                );
                assert.equal(await page.locator('[data-weather-title]').innerText(), 'Kansas City forecast');
                assert(await page.getByRole('button', { name: 'Use my location', exact: true }).isEnabled());
            }
            assert.deepEqual(errors, []);
            await context.close();
        }
        const plain = await browser.newContext({ javaScriptEnabled: false });
        const p = await plain.newPage();
        await p.goto(base + '/local');
        assert(await p.getByRole('button', { name: 'Use my location', exact: true }).isHidden());
        assert((await p.locator('[data-weather-link]').getAttribute('href')).includes('39.0997'));
        await plain.close();
        const apiBase = process.env.API_BASE_URL || base;
        const apiPage = await browser.newPage();
        for (const query of ['?lat=91&lon=0', '?lat=1', '?lat=NaN&lon=0', '?lat=0&lon=-181'])
            assert.equal((await apiPage.request.get(apiBase + '/local/weather' + query)).status(), 400);
        const response = await apiPage.request.get(apiBase + '/local/weather?lat=40.71&lon=-74.01');
        assert.equal(response.status(), 200);
        assert(response.headers()['cache-control'].includes('no-store'));
        assert.equal((await apiPage.request.get(apiBase + '/healthz')).status(), 200);
        console.log(
            'Location forecast: explicit geolocation, rounded coordinates, location label/link, KC reset, denied/timeout/unavailable/unsupported fallback, pending-request cancellation, mobile/desktop, no-JS fallback, API validation/privacy and health passed.',
        );
    } finally {
        await browser.close();
    }
})().catch((e) => {
    console.error(e);
    process.exit(1);
});
