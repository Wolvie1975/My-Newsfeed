const { chromium } = require('playwright');
const assert = require('node:assert/strict');
const base = process.env.BASE_URL || 'http://localhost:5295';
(async () => {
    const browser = await chromium.launch();
    try {
        const page = await browser.newPage();
        const compare = new Intl.Collator('en', { sensitivity: 'base' }).compare;
        const param = (n) => new URL(page.url()).searchParams.get(n);
        await page.goto(base + '/watch?kind=tv&q=Breaking%20Bad&sort=title');
        const names = await page.locator('.watch-card h2').allTextContents();
        assert(names.length);
        assert.deepEqual(names, [...names].sort(compare));
        assert.equal(await page.getByRole('button', { name: 'Apply sort', exact: true }).isVisible(), false);
        await page.locator('#watch-sort').selectOption('newest');
        await page.waitForURL((u) => u.searchParams.get('sort') === 'newest');
        await page.getByRole('link', { name: 'Clear search', exact: true }).click();
        await page.waitForURL((u) => !u.searchParams.get('q'));
        await page.locator('.watch-toggle').first().waitFor();
        await page.getByRole('link', { name: 'Movies', exact: true }).click();
        await page.waitForURL((u) => u.searchParams.get('kind') === 'movie');
        assert.equal(param('sort'), 'services');
        await page.locator('#watch-query').fill('Arrival');
        await page.getByRole('button', { name: 'Search', exact: true }).click();
        await page.waitForURL((u) => u.searchParams.get('q') === 'Arrival');
        await page.locator('.watch-results-head').waitFor();
        assert.equal(param('sort'), null);
        assert.equal(await page.locator('#watch-sort').inputValue(), 'relevance');
        assert.equal(await page.locator('.watch-toggle').count(), 0);
        await page.getByRole('link', { name: 'Clear search', exact: true }).click();
        await page.waitForURL((u) => !u.searchParams.get('q'));
        await page.locator('.watch-grid .watch-card').first().waitFor();
        assert.equal(param('kind'), 'movie');
        await page.goto(base + '/watch?kind=movie&sort=newest');
        const next = page.getByRole('link', { name: 'Next →', exact: true });
        if (await next.count())
            assert(new URL(await next.getAttribute('href'), base).searchParams.get('sort') === 'newest');
        await page.goto(base + '/watch?q=the%20office');
        const top = await page.locator('.watch-result h2').first().innerText();
        assert.equal(top, 'The Office', 'best match first');
        await page.goto(base + '/watch?kind=tv&services=8&sort=popular');
        assert.equal(await page.locator('.watch-toggle').count(), 2);
        await page.getByRole('link', { name: 'My services', exact: true }).click();
        await page.waitForURL((u) => u.searchParams.get('only') === '1');
        await page
            .locator('.watch-toggle[aria-label="Services shown"] [aria-current=page]', { hasText: 'My services' })
            .waitFor();
        await page.locator('.watch-results-count', { hasText: 'on your services' }).waitFor();
        const cards = await page.locator('.watch-grid .watch-card').evaluateAll((ns) =>
            ns.map((n) => ({
                match: !!n.querySelector('.watch-match'),
                unknown: !n.querySelector('.watch-match') && /could not be checked/.test(n.textContent),
                badge: !!n.querySelector('.watch-badge'),
            })),
        );
        assert(cards.length);
        assert(cards.every((c) => c.match || c.unknown));
        assert(cards.filter((c) => c.match).every((c) => c.badge));
        await page.locator('#watch-sort').selectOption('title');
        await page.waitForURL((u) => u.searchParams.get('sort') === 'title');
        assert.equal(param('only'), '1');
        await page.getByRole('link', { name: 'TV shows', exact: true }).click();
        await page.waitForURL((u) => u.searchParams.get('kind') === 'tv');
        assert.equal(param('only'), '1');
        assert.equal(param('sort'), 'title');
        await page.getByRole('link', { name: 'All services', exact: true }).click();
        await page.waitForURL((u) => !u.searchParams.has('only'));
        await page.goto(base + '/watch?kind=movie&only=1');
        assert.equal(await page.locator('.watch-toggle').count(), 1);
        assert(await page.getByRole('link', { name: 'Choose your services →', exact: true }).count());
        await page.goto(base + '/watch?q=Arrival&services=8&sort=services');
        const groups = await page
            .locator('.watch-card')
            .evaluateAll((ns) =>
                ns.map((n) => ({ name: n.querySelector('h2').textContent, match: !!n.querySelector('.watch-match') })),
            );
        for (const match of [true, false]) {
            const group = groups.filter((n) => n.match === match).map((n) => n.name);
            assert.deepEqual(group, [...group].sort(compare));
        }
        assert.equal(await page.locator('#watch-sort').inputValue(), 'services');
        await page.setViewportSize({ width: 390, height: 900 });
        assert.equal(await page.evaluate(() => document.documentElement.scrollWidth > innerWidth), false);
        const plain = await browser.newContext({ javaScriptEnabled: false });
        const p = await plain.newPage();
        await p.goto(base + '/watch?q=Arrival');
        await p.locator('#watch-sort').selectOption('popular');
        await p.getByRole('button', { name: 'Apply sort', exact: true }).click();
        assert.equal(new URL(p.url()).searchParams.get('sort'), 'popular');
        assert(await p.locator('.watch-card').count());
        await p.goto(base + '/watch?kind=movie&services=8&only=1');
        await p.locator('#watch-sort').selectOption('title');
        await p.getByRole('button', { name: 'Apply sort', exact: true }).click();
        assert.equal(new URL(p.url()).searchParams.get('only'), '1');
        await plain.close();
        console.log(
            'Best match default for searches, instant sort with no-JavaScript fallback, A–Z/service-group ordering, Movies/TV and My services toggles preserving search/sort/page state, poster badges and phone layout passed.',
        );
    } finally {
        await browser.close();
    }
})().catch((e) => {
    console.error(e);
    process.exit(1);
});
