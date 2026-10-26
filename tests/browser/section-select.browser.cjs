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
        // Phones: College has five views, so it keeps the dropdown; check every view for all four sports.
        await p.setViewportSize({ width: 390, height: 900 });
        for (const sport of ['football', 'mens-basketball', 'volleyball', 'soccer']) {
            await p.goto(base + '/college?sport=' + sport);
            for (const view of ['results', 'standings', 'rankings', 'news', 'schedule']) {
                const target = (view === 'schedule' ? '/college' : '/college/' + view) + '?sport=' + sport;
                await p.selectOption('#college-view', target);
                await p.waitForURL(base + target);
                assert.equal(await p.locator('#college-sport').inputValue(), sport);
                assert.equal(await p.locator('#college-view').inputValue(), target);
            }
        }
        await p.goto(base + '/college/results?sport=football');
        await p.selectOption('#college-sport', 'soccer');
        await p.waitForURL((u) => u.pathname === '/college/results' && u.searchParams.get('sport') === 'soccer');
        // F1 views and tables have four or fewer choices: a segmented control of links on phones.
        await p.goto(base + '/f1');
        assert(!(await p.locator('#f1-view').isVisible()));
        await p.locator('.section-nav--few a[href="f1/standings"]').click();
        await p.waitForURL(base + '/f1/standings');
        // Blazor updates the URL before patching the page; wait for the new current tab.
        await p.locator('.section-nav--few a[aria-current="page"][href="f1/standings"]').first().waitFor();
        const constructors = p.locator('.section-nav--few a[href="f1/standings?table=constructors"]');
        if (await constructors.count()) {
            assert((await constructors.boundingBox()).height >= 44);
            await constructors.click();
            await p.waitForURL('**table=constructors');
        }
        // Desktop: the same navigation is underlined tabs.
        await p.setViewportSize({ width: 1440, height: 900 });
        await p.goto(base + '/college?sport=football');
        assert(!(await p.locator('#college-view').isVisible()));
        await p.locator('nav.section-tabs[aria-label="View"] a', { hasText: 'Standings' }).click();
        await p.waitForURL((u) => u.pathname === '/college/standings' && u.searchParams.get('sport') === 'football');
        // Without JavaScript: the dropdown submits through Go; segmented and tab links are plain links.
        const plain = await b.newPage({ javaScriptEnabled: false, viewport: { width: 320, height: 900 } });
        await plain.goto(base + '/college?sport=soccer');
        await plain.selectOption('#college-view', '/college/results?sport=soccer');
        await plain
            .locator('form')
            .filter({ has: plain.locator('#college-view') })
            .getByRole('button', { name: 'Go', exact: true })
            .click();
        await plain.waitForURL(base + '/college/results?sport=soccer');
        for (const [route, href] of [
            ['/f1', 'f1/results'],
            ['/local', 'local#local-events'],
        ]) {
            await plain.goto(base + route);
            await plain.locator('.section-nav--few a[href="' + href + '"]').click();
            await plain.waitForURL(base + '/' + href);
        }
        for (const target of ['https://example.com', '//example.com', '/\\example.com', 'javascript:alert(1)']) {
            const r = await p.request.get(base + '/navigate?to=' + encodeURIComponent(target), { maxRedirects: 0 });
            assert.equal(r.status(), 302);
            assert.equal(r.headers().location, '/');
        }
        assert.deepEqual(errors, []);
        console.log(
            'Section navigation: dropdowns (5+) and segmented links (4 or fewer) on phones, tabs on desktop, all College sports/views, F1 tables, no-JS Go and links, local-only redirects passed.',
        );
    } finally {
        await b.close();
    }
})().catch((e) => {
    console.error(e);
    process.exit(1);
});
