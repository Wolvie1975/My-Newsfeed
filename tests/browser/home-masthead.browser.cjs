// Wide Home masthead and unchanged phone layouts, using a disposable reader profile.
const { chromium } = require('playwright');
const assert = require('node:assert/strict');
const fs = require('node:fs');
const base = process.env.BASE_URL || 'http://localhost:5295';
const adminBase = process.env.ADMIN_BASE_URL || base;
const password = process.env.ADMIN_PASSWORD;
const shots = process.env.SCREENSHOT_DIR || 'test-results/browser/home-masthead/';
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
            const name = 'zz Masthead ' + Date.now().toString(36);
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
                assert(bounds.weather.right <= bounds.live.x + 1, 'Weather overlaps Coming up');
            }
            assert(await page.locator('.site-header .brand').isVisible(), 'Navigation logo visible');
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
        for (const theme of ['light', 'dark']) {
            await preference('theme', theme);
            for (const width of (process.env.WIDTHS || '320,390,430,768,1024,1280,1440,1920,2560,3200,3840')
                .split(',')
                .map(Number)) {
                await page.setViewportSize({ width, height: width >= 2560 ? 1440 : 1080 });
                await visit();
                assert.equal(await page.locator('html').getAttribute('data-theme'), theme, 'Theme persists');
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
                    assert(await page.locator('.bs-title__name').isVisible(), 'Original desktop masthead retained');
                    assert(!(await page.locator('.site-header .brand').isVisible()), 'Original Home header retained');
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
        const noJs = await browser.newContext({
            storageState,
            javaScriptEnabled: false,
            viewport: { width: 1920, height: 1080 },
        });
        const fallback = await noJs.newPage();
        await fallback.goto(base + '/', { waitUntil: 'networkidle' });
        assert(await fallback.locator('.site-header .brand').isVisible());
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
        console.log('PASS long forecasts, navigation, Refresh, themes, phone folds, no-JS fallback');
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
