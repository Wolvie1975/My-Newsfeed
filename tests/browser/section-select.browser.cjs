const assert = require('node:assert/strict'),
    { chromium } = require('playwright');
const base = process.env.BASE_URL || 'http://localhost:5295',
    adminBase = process.env.ADMIN_BASE_URL || base,
    password = process.env.ADMIN_PASSWORD,
    root = process.env.SCREENSHOT_DIR;
(async () => {
    const profiles = (await fetch(base + '/editions', { headers: { Accept: 'text/html' } })).status !== 404;
    if (profiles && !password) {
        console.log('SKIP: profiles are on here; set ADMIN_PASSWORD so the check can add a throwaway profile.');
        return;
    }

    const b = await chromium.launch({
        args: ['--disable-web-security', '--unsafely-treat-insecure-origin-as-secure=' + base],
    });
    let admin = null;
    let id = null;
    let storageState = process.env.STORAGE_STATE;

    try {
        if (profiles && !storageState) {
            admin = await (await b.newContext({ viewport: { width: 1440, height: 1000 } })).newPage();
            await admin.goto(adminBase + '/login?returnUrl=%2Fadmin%2Fprofiles');
            await admin.locator('#password').fill(password);
            await admin.getByRole('button', { name: 'Sign in', exact: true }).click();
            await admin.waitForURL('**/admin/profiles');
            await admin.waitForLoadState('networkidle');

            const name = 'zz SectionSelect ' + Date.now().toString(36);
            for (let attempt = 0; attempt < 12 && !(await admin.locator('#add-name').isVisible()); attempt++) {
                await admin.getByRole('button', { name: 'Add profile', exact: true }).first().click();
                try {
                    await admin.locator('#add-name').waitFor({ state: 'visible', timeout: 1500 });
                } catch {
                    if (attempt === 11) throw new Error('Admin Add profile did not become interactive');
                }
            }
            await admin.locator('#add-name').fill(name);
            await admin.locator('#add-pin').fill('3691');
            await admin.locator('#add-confirm').fill('3691');
            await admin.locator('form[aria-labelledby="add-title"] button[type="submit"]').click();
            await admin.getByText(`Added ${name}.`).waitFor();

            id = Number(
                (await admin.locator('.adm-table a.adm-row-title', { hasText: name }).getAttribute('href')).match(
                    /(\d+)$/,
                )[1],
            );

            const context = await b.newContext({ viewport: { width: 390, height: 900 } });
            const p = await context.newPage();
            await p.goto(base + '/college?sport=football');
            await p.locator(`form[action="profiles/${id}/open"]`).getByRole('button').click();
            await p.keyboard.type('3691');
            await p.waitForURL('**/college?sport=football');
            const saved = p.waitForResponse(
                (r) => new URL(r.url()).pathname === '/api/me/settings' && r.request().method() === 'PUT',
            );
            await p.evaluate(() => {
                window.myNewspaperPrefs.setItem('theme', 'system');
            });
            await (await saved).finished();
            storageState = await context.storageState();
            await context.close();
        }

        const context = await b.newContext({ storageState, viewport: { width: 390, height: 900 } });
        const p = await context.newPage();
        const errors = [];
        p.on('pageerror', (e) => errors.push(e.message));
        const routes = [
            '/college?sport=football',
            '/college/results?sport=mens-basketball',
            '/college/standings?sport=volleyball',
            '/college/rankings?sport=soccer',
            '/f1',
            '/f1/standings',
            '/watch?kind=tv&sort=newest',
            '/news/kansas-city',
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
            for (const view of ['results', 'standings', 'rankings', 'schedule']) {
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
        const plain = await (
            await b.newContext({ storageState, javaScriptEnabled: false, viewport: { width: 320, height: 900 } })
        ).newPage();
        await plain.goto(base + '/college?sport=soccer');
        await plain.locator('.mx-phone .mx-fold', { hasText: 'Explore College Sports' }).locator('summary').click();
        await plain.locator('.mx-phone a[href="college/results?sport=soccer"]').click();
        await plain.waitForURL(base + '/college/results?sport=soccer');
        await plain.selectOption('#college-sport-m', 'football');
        await plain.locator('.mx-ribbon form:has(#college-sport-m) button').click();
        await plain.waitForURL(base + '/college/results?sport=football');
        await plain.goto(base + '/f1');
        await plain.selectOption('#f1-view-m', '/f1/results');
        await plain.locator('.mx-ribbon form:has(#f1-view-m) button').click();
        await plain.waitForURL(base + '/f1/results');
        // Sports pages with their own ribbon still carry the Section dropdown.
        for (const [path, current] of [
            ['/scoreboard', '/scoreboard'],
            ['/standings', '/standings'],
            ['/college/standings', '/college'],
            ['/f1/results', '/f1'],
        ]) {
            await plain.goto(base + path);
            assert.equal(await plain.locator('#sports-section-m').inputValue(), current, path + ' Section dropdown');
        }
        await plain.selectOption('#sports-section-m', '/college');
        await plain.locator('.mx-ribbon form:has(#sports-section-m) button').click();
        await plain.waitForURL(base + '/college');

        // Redirects: /rankings?sourceId=1 and /college/news?q=utah return 301
        const r1 = await p.request.get(base + '/rankings', { maxRedirects: 0 });
        assert.equal(r1.status(), 301);
        assert(r1.headers().location.includes('/college/rankings?sport=football'));

        const r2 = await p.request.get(base + '/college/news?q=utah', { maxRedirects: 0 });
        assert.equal(r2.status(), 301);
        assert(r2.headers().location.includes('/sports/news/big12?q=utah'));
        // Kansas City lives under News: its ribbon select is the News tabs (phone-life covers the folds).
        await plain.goto(base + '/news/kansas-city');
        assert.equal(await plain.locator('#news-section-m').inputValue(), '/news/kansas-city');
        await plain.selectOption('#news-section-m', '/news/all');
        await plain.locator('.mx-ribbon button[type=submit]').click();
        await plain.waitForURL(base + '/news/all');
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
        if (id && admin) {
            try {
                await admin.goto(adminBase + '/admin/profiles/' + id);
                await admin.waitForLoadState('networkidle');
                const confirm = admin.getByRole('button', { name: 'Delete profile', exact: true });
                for (let attempt = 0; attempt < 12 && !(await confirm.isVisible()); attempt++) {
                    await admin.getByRole('button', { name: 'Delete profile…' }).click();
                    try {
                        await confirm.waitFor({ state: 'visible', timeout: 1500 });
                    } catch {}
                }
                await confirm.click();
                await admin.waitForURL('**/admin/profiles?deleted=*');
            } catch (e) {
                console.error(`Could not delete throwaway profile ${id}: ${e.message}`);
            }
        }
        await b.close();
    }
})().catch((e) => {
    console.error(e);
    process.exit(1);
});
