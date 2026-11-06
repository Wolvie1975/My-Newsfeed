// Wide Home polish and public-header search, using a disposable reader profile.
const { chromium } = require('playwright');
const lib = require('./lib.cjs');
const assert = require('node:assert/strict');
const fs = require('node:fs');
const base = lib.base;
const password = lib.adminPassword;
const shots = lib.screenshots('home-masthead');
(async () => {
    fs.mkdirSync(shots, { recursive: true });
    const profiles = (await fetch(base + '/editions')).status !== 404;
    assert(profiles, 'Use profiles-enabled preview; this test never writes to a household Saved list');
    const browser = await chromium.launch();
    let admin, storageState;
    try {
        if (profiles) {
            assert(password, 'ADMIN_PASSWORD is required to create and remove a disposable reader profile');
            admin = await lib.signInAdmin(browser);
            const profile = await lib.addProfile(admin, 'Masthead', { pin: '3691' });
            storageState = await lib.openProfile(browser, profile, { path: '/', waitUntil: 'domcontentloaded' });
        }

        const context = await browser.newContext({ storageState, viewport: { width: 1920, height: 1080 } });
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
        const wide = async () => {
            assert.equal(
                await page.locator('.bs-live a[href="tv"]').count(),
                0,
                'Refresh keeps score area sports-only',
            );
            const bounds = await page.evaluate(() => {
                const box = (s) => {
                    const r = document.querySelector(s).getBoundingClientRect();
                    return { x: r.x, right: r.right, y: r.y, bottom: r.bottom };
                };
                return {
                    title: box('.bs-title'),
                    greeting: box('.bs-title__greeting'),
                    refresh: box('.home-refresh'),
                    weather: document.querySelector('.bs-glance') ? box('.bs-glance') : null,
                    live: box('.bs-live'),
                };
            });
            if (bounds.weather) {
                assert(bounds.greeting.right <= bounds.weather.x + 1, 'Greeting overlaps weather');
                assert(bounds.refresh.right <= bounds.weather.x + 1, 'Refresh overlaps weather');
                assert(
                    bounds.live.y >= Math.max(bounds.title.bottom, bounds.weather.bottom) - 1,
                    'Live line is below masthead',
                );
            }
            assert(await page.locator('.site-header .brand').isVisible(), 'Navigation logo visible');
            assert(await page.locator('.header-watch').isVisible(), 'Wide header search');
            assert(
                await page.locator('#header-watch-query').evaluate((n) => {
                    const sample = document.createElement('span');
                    sample.style.color = 'var(--text-2)';
                    n.parentElement.append(sample);
                    const expected = getComputedStyle(sample).color;
                    sample.remove();
                    const placeholder = getComputedStyle(n, '::placeholder');
                    return placeholder.color === expected && placeholder.opacity === '1';
                }),
                'Theme-aware search placeholder',
            );
            assert(!(await page.locator('.bs-watch').isVisible()), 'Wide search band hidden');
            assert.equal((await page.locator('.bs-live__list li').filter({ visible: true }).count()) <= 3, true);
            assert(
                await page.locator('.bs-live').evaluate((n) => n.scrollWidth <= n.clientWidth + 1),
                'Live line fits',
            );
            for (const game of await page.locator('.bs-live__list li').filter({ visible: true }).all()) {
                const score = await game.locator('.bs-live__score').boundingBox();
                const clock = await game.locator('.bs-live__clock').boundingBox();
                assert(
                    Math.abs(score.y + score.height / 2 - (clock.y + clock.height / 2)) < 2,
                    'Score and clock on one line',
                );
            }
            assert.equal(
                await page.locator('.bs-sheet .d-lead__title').evaluate((n) => getComputedStyle(n).fontSize),
                '46px',
            );
            if (page.viewportSize().width >= 2560) {
                const headings = page.locator(
                    '.bs-sheet .bs-col > .d-card:first-child > .d-card__head > .d-card__title',
                );
                // One heading per column (five since the wide-desktop rebalance of November 1, 2026), each on one line.
                assert.equal(await headings.count(), await page.locator('.bs-sheet .bs-col').count());
                assert(await headings.count(), 'column headings');
                for (const heading of await headings.all()) {
                    assert(
                        await heading.evaluate(
                            (n) =>
                                n.getBoundingClientRect().height <=
                                parseFloat(getComputedStyle(n).lineHeight) *
                                    parseFloat(getComputedStyle(n.closest('.pub')).zoom) +
                                    1,
                        ),
                        'Column heading stays on one line',
                    );
                }
                assert(await page.locator('.bs-episodes-count').isVisible(), 'Episode count remains readable');
            }
            const photo = page.locator('.bs-sheet .d-lead__image');
            if (await photo.isVisible()) {
                const rect = await photo.boundingBox();
                const title = await page.locator('.bs-sheet .d-lead__title').boundingBox();
                assert(rect.y + rect.height <= title.y + 1, 'Lead photo above headline');
                assert(Math.abs(rect.width / rect.height - 16 / 9) < 0.03, 'Lead photo 16:9');
            }
            assert(
                await page.locator('.bs-title__greeting').evaluate((n) => n.scrollWidth <= n.clientWidth + 1),
                'Greeting fits without clipping',
            );
            assert(!(await page.locator('.bs-title__name').isVisible()), 'Large masthead logo hidden');
            const nav = await page
                .locator('.site-nav--inline a')
                .evaluateAll((ns) =>
                    ns.filter((n) => n.getClientRects().length).map((n) => Math.round(n.getBoundingClientRect().y)),
                );
            assert.equal(new Set(nav).size, 1, 'Navigation fits one line');
        };
        for (const theme of lib.THEMES) {
            await preference('theme', theme);
            for (const width of lib.widths('320,390,430,768,1024,1280,1440,1920,2560,3200,3840')) {
                await page.setViewportSize({ width, height: width >= 2560 ? 1440 : 1080 });
                await visit();
                assert.equal(await page.locator('html').getAttribute('data-theme'), theme, 'Theme persists');
                assert.equal(await page.locator('.bs-live a[href="tv"]').count(), 0, 'Score area contains no TV link');
                assert.equal(
                    await page.locator('.bs-live__tonight').count(),
                    0,
                    'Score area contains no New tonight summary',
                );
                assert(
                    await page.evaluate(() => document.documentElement.scrollWidth <= innerWidth + 1),
                    'No horizontal overflow',
                );
                if (width >= 1920) {
                    await wide();
                    const refresh = page.locator('.home-refresh');
                    await refresh.click();
                    await page.waitForLoadState('networkidle');
                    await wide();
                    // Stress only test DOM; production weather data and settings remain untouched.
                    await page.locator('.bs-title__greeting').evaluate((n) => (n.textContent = 'Good afternoon'));
                    await page.evaluate(() => {
                        const section = document.querySelector('.d-episodes');
                        if (!section.querySelector('.d-tv__body')) {
                            const row = document.createElement('div');
                            row.className = 'd-tv__show';
                            row.innerHTML =
                                '<div class="tv-poster"></div><div class="d-tv__body"><h3 class="d-vid__title"></h3></div>';
                            section.append(row);
                        }
                        section.querySelector('.d-vid__title').textContent =
                            'Avatar: Seven Havens — a complete long series title that must remain readable';
                    });
                    assert(
                        await page
                            .locator('.d-episodes .d-vid__title')
                            .first()
                            .evaluate(
                                (n) => getComputedStyle(n).display === 'block' && n.scrollHeight <= n.clientHeight + 1,
                            ),
                        'Full TV series title is not clamped',
                    );
                    await wide();
                    if (await page.locator('.bs-glance').count()) {
                        assert.equal(await page.locator('.bs-glance__periods .weather-icon').count(), 4);
                        await page
                            .locator('.bs-glance__reading')
                            .evaluateAll((ns) =>
                                ns.forEach(
                                    (n) =>
                                        (n.innerHTML =
                                            '<b>76°</b> Chance Showers And Thunderstorms Then Mostly Cloudy'),
                                ),
                            );
                        await wide();
                        assert(
                            await page.evaluate(() => document.documentElement.scrollWidth <= innerWidth + 1),
                            'Long forecast fits',
                        );
                    }
                } else if (width >= 1024) {
                    assert(!(await page.locator('.header-watch').isVisible()));
                    assert(await page.locator('.bs-watch').isVisible());
                    assert(await page.locator('.bs-title__name').isVisible(), 'Original desktop masthead retained');
                    assert(!(await page.locator('.site-header .brand').isVisible()), 'Original Home header retained');
                } else if (width >= 744) {
                    // 744–1023px is the tablet front (tablet-layout.browser.cjs checks it); phones are below 744.
                    assert(await page.locator('.tab-desktop').isVisible(), 'Tablet Home from 744px');
                } else {
                    const phone = page.locator('.mx-phone');
                    assert(await phone.isVisible());
                    const fold = phone.locator('details').first();
                    if ((await fold.getAttribute('open')) === null) await fold.locator('summary').click();
                    assert((await fold.locator('summary').boundingBox()).height >= 44);
                    await page.reload({ waitUntil: 'networkidle' });
                    assert(await phone.isVisible(), 'Phone survives refresh');
                }
                await page.evaluate(() => window.scrollTo(0, 0));
                await page.screenshot({ path: shots + `masthead-${width}-${theme}.png`, fullPage: true });
                console.log('PASS', width, theme);
            }
        }
        for (const scheme of ['monochrome', 'cardinals']) {
            await preference('mynewsfeed-color-scheme', scheme);
            await page.setViewportSize({ width: 2560, height: 1440 });
            await visit();
            await wide();
            await page.screenshot({ path: shots + `masthead-2560-${scheme}.png`, fullPage: true });
        }
        await page.setViewportSize({ width: 1920, height: 1080 });
        await visit();
        await page.locator('#header-watch-query').fill('Arrival');
        await page.locator('.header-watch button').click();
        await page.waitForURL('**/watch?q=Arrival');
        await page.locator('.header-watch').waitFor({ state: 'detached' });
        await page.goto(base + '/sports', { waitUntil: 'networkidle' });
        assert(await page.locator('.header-watch').isVisible(), 'Search on other public pages');
        assert(await page.evaluate(() => document.documentElement.scrollWidth <= innerWidth + 1));
        for (const route of ['/news/all', '/videos', '/entertainment', '/settings', '/saved']) {
            await page.goto(base + route, { waitUntil: 'networkidle' });
            assert(await page.locator('.header-watch').isVisible(), 'Header search on ' + route);
            assert(
                await page.evaluate(() => document.documentElement.scrollWidth <= innerWidth + 1),
                'No overflow on ' + route,
            );
        }
        const noJs = await browser.newContext({
            storageState,
            javaScriptEnabled: false,
            viewport: { width: 1920, height: 1080 },
        });
        const fallback = await noJs.newPage();
        await fallback.goto(base + '/', { waitUntil: 'networkidle' });
        assert(await fallback.locator('.site-header .brand').isVisible());
        assert.equal(await fallback.locator('.bs-live a[href="tv"]').count(), 0, 'No-JS score area is sports-only');
        await fallback.locator('#header-watch-query').fill('Arrival');
        await fallback.locator('.header-watch button').click();
        await fallback.waitForURL('**/watch?q=Arrival');
        assert.equal(await fallback.locator('.header-watch').count(), 0);
        await fallback.goto(base + '/', { waitUntil: 'networkidle' });
        await fallback.locator('.home-refresh').click();
        await fallback.waitForLoadState('networkidle');
        await fallback.setViewportSize({ width: 390, height: 900 });
        await fallback.locator('.mx-phone').waitFor({ state: 'visible' });
        const fold = fallback.locator('.mx-phone details').first();
        const wasOpen = (await fold.getAttribute('open')) !== null;
        await fold.locator('summary').click();
        assert.equal((await fold.getAttribute('open')) !== null, !wasOpen, 'Native phone fold works without JS');
        await noJs.close();
        await context.close();
        console.log(
            'PASS header search, placeholder contrast, ticker, lead frame, TV titles, headings, Refresh and phone/no-JS controls',
        );
    } finally {
        try {
            await lib.deleteProfiles();
        } finally {
            await browser.close();
        }
    }
})().catch((e) => {
    console.error(e);
    process.exitCode = 1;
});
