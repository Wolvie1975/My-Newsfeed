// iPad foundation and Watch layouts, using a disposable reader profile.
const { chromium } = require('playwright');
const assert = require('node:assert/strict');
const fs = require('node:fs');
const base = process.env.BASE_URL || 'http://localhost:5295';
const adminBase = process.env.ADMIN_BASE_URL || base;
const password = process.env.ADMIN_PASSWORD;
const shots = process.env.SCREENSHOT_DIR || 'test-results/browser/ipad/';
(async () => {
    fs.mkdirSync(shots, { recursive: true });
    const profiles = (await fetch(base + '/editions')).status !== 404;
    assert(profiles, 'Use profiles-enabled preview; this test never writes to a household Saved list');
    const browser = await chromium.launch();
    let admin = null,
        id = null,
        storageState;
    try {
        if (profiles) {
            assert(password, 'ADMIN_PASSWORD is required to create and remove a disposable reader profile');
            admin = await (await browser.newContext({ viewport: { width: 1440, height: 1000 } })).newPage();
            await admin.goto(adminBase + '/login?returnUrl=%2Fadmin%2Fprofiles');
            await admin.locator('#password').fill(password);
            await admin.getByRole('button', { name: 'Sign in', exact: true }).click();
            await admin.waitForURL('**/admin/profiles');
            await admin.waitForLoadState('networkidle');
            const name = 'zz iPad ' + Date.now().toString(36);
            // Admin can finish its interactive circuit after the server-rendered button appears.
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
            const context = await browser.newContext({ viewport: { width: 390, height: 900 } });
            const p = await context.newPage();
            await p.goto(base + '/', { waitUntil: 'domcontentloaded' });
            await p.locator(`form[action="profiles/${id}/open"]`).getByRole('button').click();
            await p.keyboard.type('3691');
            await p.waitForURL(base + '/');
            storageState = await context.storageState();
            await context.close();
        }

        const context = await browser.newContext({
            storageState,
            hasTouch: true,
            viewport: { width: 1920, height: 1080 },
        });
        const page = await context.newPage();
        const visit = async () => {
            await page.goto(base + '/', { waitUntil: 'networkidle' });
            await page.locator('.bs-masthead').waitFor({ state: 'attached' });
        };
        await visit();
        const preference = async (key, value) => {
            const saved = page.waitForResponse(
                (r) => new URL(r.url()).pathname === '/api/me/settings' && r.request().method() === 'PUT',
            );
            await page.evaluate(([k, v]) => window.myNewspaperPrefs.setItem(k, v), [key, value]);
            const response = await saved;
            assert.equal(response.status(), 200);
            await response.finished();
        };
        const foundation = process.env.FOUNDATION === '1';
        const header = async (width) => {
            const tablet = width >= 744 && width < 1024;
            if (await page.evaluate(() => document.documentElement.scrollWidth > innerWidth + 1)) {
                await page.screenshot({ path: shots + 'overflow.png', fullPage: true });
                console.log(
                    await page.evaluate(() =>
                        [
                            '.site-header',
                            '.brand',
                            '.header-actions',
                            '.header-clock',
                            '.header-search-link',
                            '.edition-chip',
                        ].map((s) => {
                            const n = document.querySelector(s),
                                b = n?.getBoundingClientRect(),
                                c = n && getComputedStyle(n);
                            return { s, x: b?.x, w: b?.width, display: c?.display, order: c?.order, flex: c?.flex };
                        }),
                    ),
                );
                console.log(
                    await page.evaluate(() =>
                        [...document.querySelectorAll('body *')]
                            .filter(
                                (n) => n.getClientRects().length && n.getBoundingClientRect().right > innerWidth + 1,
                            )
                            .map((n) => ({ tag: n.tagName, cls: n.className, right: n.getBoundingClientRect().right }))
                            .slice(0, 15),
                    ),
                );
            }
            assert(
                await page.evaluate(() => document.documentElement.scrollWidth <= innerWidth + 1),
                'No page overflow',
            );
            assert.equal(await page.locator('.header-search-link').isVisible(), tablet, 'Tablet-only search icon');
            assert.equal(await page.locator('.mobile-nav').isVisible(), width < 744, 'Phone-only menu');
            assert.equal(await page.locator('.site-nav--inline').isVisible(), width >= 744, 'Sections visible');
            if (tablet) {
                const positions = await page.evaluate(() => {
                    const b = (s) => document.querySelector(s).getBoundingClientRect();
                    const brand = b('.brand'),
                        actions = b('.header-actions'),
                        nav = b('.site-nav--inline');
                    return {
                        brand: brand.bottom,
                        actions: actions.bottom,
                        nav: nav.top,
                        navBottom: nav.bottom,
                        height: b('.site-header').height,
                        overflow: document.querySelector('.site-header').scrollWidth > innerWidth + 1,
                        rows: new Set(
                            [...document.querySelectorAll('.site-nav--inline a')].map(
                                (n) => n.getBoundingClientRect().top,
                            ),
                        ).size,
                    };
                });
                assert(positions.nav >= Math.max(positions.brand, positions.actions) - 1, 'Navigation is row two');
                assert.equal(positions.rows, 1, 'Six sections fit one row');
                assert(!positions.overflow, 'Header fits');
                assert(positions.height < 125, 'Header is two compact rows');
                for (const target of await page
                    .locator('.header-actions > a:visible, .header-actions > button:visible')
                    .all()) {
                    const rect = await target.boundingBox();
                    assert(rect.width >= 44 && rect.height >= 44, '44px header action');
                }

                for (const n of await page.locator('.site-nav--inline a,.header-search-link').all())
                    assert((await n.boundingBox()).height >= 44, '44px nav target');
            }
        };
        for (const theme of ['light', 'dark']) {
            await preference('theme', theme);
            for (const width of process.env.WIDTHS
                ? process.env.WIDTHS.split(',').map(Number)
                : foundation
                  ? [320, 390, 430, 744, 820, 834, 1023, 1024, 1280, 1440]
                  : [320, 390, 430, 743, 744, 820, 834, 1023, 1024, 1280, 1440, 2560]) {
                await page.setViewportSize({ width, height: 1100 });
                await page.goto(base + '/watch?services=8,9,337', { waitUntil: 'networkidle' });
                await header(width);
                assert.equal(await page.locator('html').getAttribute('data-theme'), theme);
                const tablet = width >= 744 && width < 1024 && !foundation;
                assert.equal(
                    await page.locator('#watch-query').isVisible(),
                    width >= 1024 || tablet,
                    'Correct Watch search layout',
                );
                assert.equal(
                    await page.locator('[data-phone-watch-ribbon]').isVisible(),
                    width < 1024 && !tablet,
                    'Correct phone ribbon',
                );
                if (tablet) {
                    assert.equal(await page.locator('.watch-row__arrow:visible').count(), 0, 'No tablet arrows');
                    const geometry = await page.evaluate(() => {
                        const b = (s) => document.querySelector(s).getBoundingClientRect();
                        const heading = b('.watch-page .page-heading'),
                            search = b('.watch-page .watch-search');
                        return {
                            title: getComputedStyle(document.querySelector('.watch-page h1')).fontSize,
                            headingBottom: heading.bottom,
                            searchTop: search.top,
                            searchWidth: search.width,
                            frame: innerWidth - 48,
                        };
                    });
                    assert(geometry.searchTop >= geometry.headingBottom, 'Search below heading');
                    assert(Math.abs(geometry.searchWidth - geometry.frame) < 2, 'Full width search');
                    assert.equal(geometry.title, '48px');
                    if (width === 744 && theme === 'light') {
                        await page.locator('.watch-mine__logos').evaluate((n) => {
                            while (n.children.length < 6) n.append(n.firstElementChild.cloneNode(true));
                            const more = document.createElement('li');
                            more.className = 'watch-mine__more';
                            more.textContent = '+12';
                            n.append(more);
                        });
                        const bar = await page.evaluate(() => {
                            const filters = document.querySelector('.watch-filters').getBoundingClientRect(),
                                mine = document.querySelector('.watch-mine').getBoundingClientRect();
                            return { filters: filters.right, mine: mine.left, top: filters.top, mineTop: mine.top };
                        });
                        assert(
                            bar.filters <= bar.mine && Math.abs(bar.top - bar.mineTop) < 2,
                            'Six logos plus count fit beside tabs',
                        );
                        await header(width);
                        await page.screenshot({ path: shots + 'watch-services-stress.png', fullPage: true });
                    }
                    const shelf = page.locator('.tab-desktop .watch-shelf').first();
                    await shelf.waitFor();
                    assert((await shelf.locator('li').count()) > 4, 'Representative populated shelf');
                    await shelf.evaluate((n) => n.scrollBy({ left: 350, behavior: 'instant' }));
                    await page.waitForFunction(
                        () => document.querySelector('.tab-desktop .watch-shelf').scrollLeft > 100,
                    );
                    assert.equal(await shelf.evaluate((n) => getComputedStyle(n).gridAutoColumns), '150px');
                }
                if (width < 744) {
                    await page.locator('.mobile-nav summary').click();
                    assert(await page.locator('.mobile-nav__links a[href="watch"]').isVisible());
                    await page.locator('.mobile-nav summary').click();
                }
                if (width === 1024 && theme === 'light') {
                    const next = page.locator('.tab-desktop .watch-row__arrow--next').first();
                    assert(await next.isVisible(), 'Desktop arrows retained');
                    await next.click();
                    await page.waitForFunction(
                        () => document.querySelector('.tab-desktop .watch-shelf').scrollLeft > 50,
                    );
                }
                await page.reload({ waitUntil: 'networkidle' });
                await header(width);
                await page.screenshot({ path: shots + `watch-${width}-${theme}.png`, fullPage: true });
                console.log('PASS', width, theme, foundation ? 'foundation' : 'watch');
            }
        }
        await page.setViewportSize({ width: 820, height: 1100 });
        for (const route of ['/', '/news', '/sports', '/entertainment', '/settings']) {
            await page.goto(base + route, { waitUntil: 'networkidle' });
            await header(820);
            assert(
                (await page.locator('.mx-phone:visible').count()) > 0,
                'Unconverted page keeps phone layout ' + route,
            );
            assert.equal(await page.locator('.tab-desktop').count(), 0, 'No unrelated page opts in');
            console.log('PASS isolation', route);
            await page.screenshot({ path: shots + `other-${route.replaceAll('/', '') || 'home'}.png`, fullPage: true });
        }
        for (const scheme of ['monochrome', 'cardinals']) {
            await preference('mynewsfeed-color-scheme', scheme);
            await page.setViewportSize({ width: 744, height: 1100 });
            await page.goto(base + '/watch?services=8,9,337', { waitUntil: 'networkidle' });
            await header(744);
            await page.screenshot({ path: shots + `watch-744-${scheme}.png`, fullPage: true });
        }
        // Exercise the shortcut in its own page so service restoration cannot race the next test navigation.
        const shortcut = await context.newPage();
        await shortcut.setViewportSize({ width: 820, height: 1100 });
        await shortcut.goto(base + '/entertainment', { waitUntil: 'networkidle' });
        await shortcut.locator('.header-search-link').click();
        await shortcut.waitForURL('**/watch**');
        await shortcut.locator('.watch-page').waitFor();
        await shortcut.close();
        console.log('PASS header search shortcut');
        if (!foundation) {
            await page.setViewportSize({ width: 820, height: 1100 });
            await page.goto(base + '/watch?services=8,9,337', { waitUntil: 'networkidle' });
            const rail = page.locator('.tab-desktop .watch-shelf').first();
            await rail.scrollIntoViewIfNeeded();
            const box = await rail.boundingBox();
            const cdp = await context.newCDPSession(page);
            const touchY = Math.round(box.y + 80),
                touchX = Math.round(box.x + box.width - 80);
            await cdp.send('Input.dispatchTouchEvent', { type: 'touchStart', touchPoints: [{ x: touchX, y: touchY }] });
            for (let step = 1; step <= 10; step++) {
                await cdp.send('Input.dispatchTouchEvent', {
                    type: 'touchMove',
                    touchPoints: [{ x: touchX - step * 30, y: touchY }],
                });
                await page.waitForTimeout(25);
            }
            await cdp.send('Input.dispatchTouchEvent', { type: 'touchEnd', touchPoints: [] });
            await page.waitForFunction(() => document.querySelector('.tab-desktop .watch-shelf').scrollLeft > 100);
            console.log('PASS touch swipe');
            await cdp.detach();
            await page.setViewportSize({ width: 744, height: 1100 });

            await page.goto(base + '/watch?services=8,9,337', { waitUntil: 'networkidle' });
            await page
                .locator('.watch-toggle a')
                .filter({ hasText: /^Movies$/ })
                .click();
            await page.waitForURL('**/watch?**kind=movie**');
            await page.waitForLoadState('networkidle');
            await page
                .locator('.watch-toggle a')
                .filter({ hasText: /^My services$/ })
                .click();
            await page.waitForURL('**/watch?**only=1**');
            await page.waitForLoadState('networkidle');
            await page
                .locator('.watch-toggle a')
                .filter({ hasText: /^TV shows$/ })
                .click();
            await page.waitForURL('**/watch?**kind=tv**');
            await page.waitForLoadState('networkidle');
            await page.locator('#watch-query').fill('Arrival');
            await page.locator('.watch-search button').click();
            await page.waitForURL('**/watch?**q=Arrival**');
            await page.waitForLoadState('networkidle');
            await page.locator('.watch-result').first().waitFor();
            await page.locator('#watch-sort').selectOption('title');
            await page.waitForURL('**/watch?**sort=title**');
            await page.waitForLoadState('networkidle');
            await header(744);
            await page.screenshot({ path: shots + 'results-744.png', fullPage: true });
            await page.locator('.watch-services-link').click();
            await page.waitForURL('**/settings#my-services');
            await page.locator('#my-services').waitFor();
            console.log('PASS browse tabs, service filter, search, auto-sort and Manage');
        }
        const noJs = await browser.newContext({
            storageState,
            javaScriptEnabled: false,
            viewport: { width: 820, height: 1100 },
        });
        const fallback = await noJs.newPage();
        await fallback.goto(base + '/watch', { waitUntil: 'networkidle' });
        await fallback.locator('.header-search-link').click();
        await fallback.waitForURL('**/watch');
        await fallback.locator(foundation ? '#watch-query-m' : '#watch-query').fill('Arrival');
        await fallback.locator(foundation ? '.mx-ribbon__search button' : '.watch-search button').click();
        await fallback.waitForURL('**/watch?**q=Arrival**');
        await fallback.waitForLoadState('networkidle');
        await fallback.locator('#watch-sort').selectOption('title');
        await fallback.locator('.watch-sort button').click();
        await fallback.waitForURL('**/watch?**sort=title**');
        await fallback.setViewportSize({ width: 390, height: 900 });
        await fallback.goto(base + '/watch', { waitUntil: 'networkidle' });
        await fallback.locator('#watch-query-m').fill('Arrival');
        await fallback.locator('.mx-ribbon__search button').click();
        await fallback.waitForURL('**/watch?**q=Arrival**');
        await noJs.close();
        await context.close();
        console.log('PASS tablet header, page isolation, themes, rotation widths and no-JS search');
    } finally {
        try {
            if (admin && id) {
                await admin.goto(adminBase + '/admin/profiles/' + id);
                await admin.waitForLoadState('networkidle');
                const confirm = admin.getByRole('button', { name: 'Delete profile', exact: true });
                for (let attempt = 0; attempt < 12 && !(await confirm.isVisible()); attempt++) {
                    await admin.getByRole('button', { name: 'Delete profile…' }).click();
                    try {
                        await confirm.waitFor({ state: 'visible', timeout: 1500 });
                    } catch {
                        // Retry while the admin circuit becomes interactive.
                    }
                }
                await confirm.click();
                await admin.waitForURL('**/admin/profiles?deleted=*');
            }
        } finally {
            await browser.close();
        }
    }
})().catch((e) => {
    console.error(e);
    process.exitCode = 1;
});
