const assert = require('node:assert/strict'),
    { chromium } = require('playwright');
const base = process.env.BASE_URL || 'http://localhost:5295',
    root = process.env.SCREENSHOT_DIR;
(async () => {
    const b = await chromium.launch();
    try {
        const p = await b.newPage();
        const errors = [];
        p.on('pageerror', (e) => errors.push(e.message));
        const routes = [
            '/college?sport=football',
            '/college/results?sport=mens-basketball',
            '/college/standings?sport=volleyball',
            '/college/rankings?sport=soccer',
            '/college/news',
            '/f1',
            '/f1/standings',
            '/watch?kind=tv&sort=newest',
            '/local',
            '/mls/results',
        ];
        for (const width of [320, 390, 430, 768, 1440])
            for (const theme of ['light', 'dark'])
                for (const route of routes) {
                    await p.setViewportSize({ width, height: 900 });
                    await p.emulateMedia({ colorScheme: theme });
                    await p.goto(base + route);
                    assert(!(await p.locator('#blazor-error-ui').isVisible()));
                    assert(await p.evaluate(() => document.documentElement.scrollWidth <= innerWidth));
                    // Where to watch uses Movies/TV toggle links by user request (October 25, 2026).
                    assert.equal(
                        await p
                            .locator(
                                route.startsWith('/watch')
                                    ? 'nav.f1-tabs,nav[aria-label="Browse genres"],nav.soccer-league-links'
                                    : 'nav.f1-tabs,nav[aria-label="Content type"],nav[aria-label="Browse genres"],nav.soccer-league-links',
                            )
                            .count(),
                        0,
                    );
                    assert(
                        await p.locator('.section-select select').evaluateAll((ns) =>
                            // Desktop shows tabs instead, so only the visible dropdowns are measured.
                            ns
                                .filter((n) => n.offsetParent !== null)
                                .every(
                                    (n) =>
                                        n.getBoundingClientRect().height >= 44 &&
                                        n.getBoundingClientRect().width <= innerWidth,
                                ),
                        ),
                    );
                    if (
                        root &&
                        width <= 430 &&
                        ['/college?sport=football', '/f1', '/watch?kind=tv&sort=newest'].includes(route)
                    )
                        await p.screenshot({
                            path: root + '/' + route.split('?')[0].slice(1) + '-' + width + '-' + theme + '.png',
                        });
                }
        // Folded College uses Sport in its ribbon and view links inside Explore (or the subpage navigation).
        await p.setViewportSize({ width: 390, height: 900 });
        for (const sport of ['football', 'mens-basketball', 'volleyball', 'soccer']) {
            await p.goto(base + '/college?sport=' + sport);
            for (const view of ['results', 'standings', 'rankings', 'news', 'schedule']) {
                const target = (view === 'schedule' ? '/college' : '/college/' + view) + '?sport=' + sport;
                const explore = p.locator('.mx-phone .mx-fold', { hasText: 'Explore College Sports' });
                if (await explore.count()) await explore.locator('summary').click();
                await p.locator('.mx-phone a[href="' + target.slice(1) + '"]:visible').click();
                await p.waitForURL(base + target);
                await p.waitForLoadState('networkidle');
                assert.equal(await p.locator('#college-sport-m').inputValue(), sport);
            }
        }
        await p.goto(base + '/college/results?sport=football');
        await p.selectOption('#college-sport-m', 'soccer');
        await p.waitForURL((u) => u.pathname === '/college/results' && u.searchParams.get('sport') === 'soccer');
        // F1's View ribbon is always a native select; its table tools keep their existing links.
        await p.goto(base + '/f1');
        assert(await p.locator('#f1-view-m').isVisible());
        await p.selectOption('#f1-view-m', '/f1/standings');
        await p.waitForURL(base + '/f1/standings');
        await p.waitForLoadState('networkidle');
        const table = p.locator('#f1-table');
        assert((await table.boundingBox()).height >= 44);
        await table.selectOption('/f1/standings?table=constructors');
        await p.waitForURL('**table=constructors');
        // Desktop: the same navigation is underlined tabs.
        await p.setViewportSize({ width: 1440, height: 900 });
        await p.goto(base + '/college?sport=football');
        assert(!(await p.locator('#college-view').isVisible()));
        await p.locator('nav.section-tabs[aria-label="View"] a', { hasText: 'Standings' }).click();
        await p.waitForURL((u) => u.pathname === '/college/standings' && u.searchParams.get('sport') === 'football');
        // No-JS College view links and Sport Go, F1 View Go, and the retained Local links.
        const plain = await b.newPage({ javaScriptEnabled: false, viewport: { width: 320, height: 900 } });
        await plain.goto(base + '/college?sport=soccer');
        await plain.locator('.mx-phone .mx-fold', { hasText: 'Explore College Sports' }).locator('summary').click();
        await plain.locator('.mx-phone a[href="college/results?sport=soccer"]').click();
        await plain.waitForURL(base + '/college/results?sport=soccer');
        await plain.selectOption('#college-sport-m', 'football');
        await plain.locator('.mx-ribbon form button').click();
        await plain.waitForURL(base + '/college/results?sport=football');
        await plain.goto(base + '/f1');
        await plain.selectOption('#f1-view-m', '/f1/results');
        await plain.locator('.mx-ribbon form button').click();
        await plain.waitForURL(base + '/f1/results');
        await plain.goto(base + '/local');
        await plain.locator('.section-nav--few a[href="local#local-events"]').click();
        await plain.waitForURL(base + '/local#local-events');
        for (const target of ['https://example.com', '//example.com', '/\\example.com', 'javascript:alert(1)']) {
            const r = await p.request.get(base + '/navigate?to=' + encodeURIComponent(target), { maxRedirects: 0 });
            assert.equal(r.status(), 302);
            assert.equal(r.headers().location, '/');
        }
        assert.deepEqual(errors, []);
        console.log(
            'Section navigation: folded College view links/Sport select, F1 View select and table links, retained navigation controls, desktop tabs, all College sports/views, no-JS Go and links, local-only redirects passed.',
        );
    } finally {
        await b.close();
    }
})().catch((e) => {
    console.error(e);
    process.exit(1);
});
