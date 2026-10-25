const { chromium } = require('playwright');
const assert = require('node:assert/strict');
const base = process.env.BASE_URL || 'http://localhost:5295';
(async () => {
    const browser = await chromium.launch();
    try {
        const page = await browser.newPage();
        const errors = [];
        page.on('pageerror', (e) => errors.push(e.message));
        // Which providers are enabled is admin data; read it from the page, then check canonicalization against it.
        await page.goto(base + '/movies/digital');
        const fallback = new URL(page.url()).searchParams.get('source');
        const options = await page
            .locator('#digital-source option')
            .evaluateAll((o) => o.map((x) => ({ value: x.value, label: x.textContent.trim() })));
        const enabled = options.map((o) => o.value);
        assert(enabled.includes(fallback), `default provider ${fallback} should be an enabled option`);
        assert.equal(enabled.length, new Set(enabled).size);
        for (const source of [null, 'unknown', ...enabled]) {
            await page.goto(base + '/movies/digital' + (source ? '?source=' + source : ''));
            const selected = enabled.includes(source) ? source : fallback;
            await page.waitForURL((u) => u.searchParams.get('source') === selected);
            assert.equal(await page.locator('#digital-source').inputValue(), selected);
            const name = options.find((o) => o.value === selected).label;
            assert(await page.getByRole('heading', { name, exact: true }).isVisible());
            for (const width of [390, 1440]) {
                await page.setViewportSize({ width, height: 900 });
                assert(await page.evaluate(() => document.documentElement.scrollWidth <= innerWidth));
            }
            assert.equal(
                await page
                    .locator('.movie-kind')
                    .filter({ hasText: /subscription|disc/i })
                    .count(),
                0,
            );
        }
        assert.deepEqual(errors, []);
        assert.equal((await page.request.get(base + '/healthz')).status(), 200);
        console.log(
            'Digital providers: default/disabled/unknown URL canonicalization, enabled options, alternate selection, provider labels, responsive layout, no JS errors and health passed.',
        );
    } finally {
        await browser.close();
    }
})().catch((e) => {
    console.error(e);
    process.exit(1);
});
