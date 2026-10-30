// Complete Sports tables, hourly refresh and mobile/no-JavaScript regressions in a disposable profile.
const { chromium } = require('playwright');
const assert = require('node:assert/strict');
const fs = require('node:fs');
const base = process.env.BASE_URL || 'http://localhost:5295';
const adminBase = process.env.ADMIN_BASE_URL || base;
const password = process.env.ADMIN_PASSWORD;
const shots = process.env.SCREENSHOT_DIR || 'test-results/browser/sports-panels/';
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
            const name = 'zz SportsPanels ' + Date.now().toString(36);
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
            await p.goto(base + '/sports', { waitUntil: 'domcontentloaded' });
            await p.locator(`form[action="profiles/${id}/open"]`).getByRole('button').click();
            await p.keyboard.type('3691');
            await p.waitForURL('**/sports');
            storageState = await context.storageState();
            await context.close();
        }

        const context = await browser.newContext({ storageState, viewport: { width: 2560, height: 1440 } });
        const page = await context.newPage();
        const visit = async () => {
            await page.goto(base + '/sports', { waitUntil: 'networkidle' });
            await page.locator('.spf-standings').waitFor({ state: 'attached' });
        };
        await visit();
        const heading = await page.locator('#spf-stand-title').textContent();
        const standingsHref = await page.locator('.spf-standings .ef-more').getAttribute('href');
        const rankingHref = await page.locator('.spf-rankings .ef-more').getAttribute('href');
        const teams = await page.locator('.spf-standings tbody tr').count();
        const ranks = await page.locator('.spf-rankings tbody tr td:first-child').allTextContents();
        assert(teams > 0);
        assert.equal(ranks.length, 25, 'Full Top 25');
        assert.deepEqual(
            ranks.map(Number),
            [...ranks.map(Number)].sort((a, b) => a - b),
        );
        const detail = await context.newPage();
        await detail.goto(base + '/' + standingsHref, { waitUntil: 'networkidle' });
        assert.equal(
            await detail.locator('.mx-desktop .standings-table tbody tr').count(),
            teams,
            'Every standings group and team',
        );
        await detail.close();
        for (const theme of ['light', 'dark']) {
            const saved = page.waitForResponse(
                (r) => new URL(r.url()).pathname === '/api/me/settings' && r.request().method() === 'PUT',
            );
            await page.evaluate((v) => window.myNewspaperPrefs.setItem('theme', v), theme);
            await (await saved).finished();
            for (const width of (process.env.WIDTHS || '320,390,430,1024,1440,1920,2560,3840').split(',').map(Number)) {
                await page.setViewportSize({ width, height: 1000 });
                await visit();
                assert(
                    await page.evaluate(() => document.documentElement.scrollWidth <= innerWidth + 1),
                    'No overflow at ' + width,
                );
                assert.equal(
                    await page.locator('#spf-stand-title').textContent(),
                    heading,
                    'Same selection within hour',
                );
                if (width < 768) {
                    const fold = page.locator('.mx-phone details').first();
                    if ((await fold.getAttribute('open')) === null) await fold.locator('summary').click();
                    assert(await fold.locator('summary').isVisible());
                    const all = page.locator('.mx-phone a').filter({ hasText: 'All standings' }).first();
                    assert((await fold.locator('summary').boundingBox()).height >= 44, 'Phone fold target');
                    assert((await all.boundingBox()).height >= 44, 'Phone full standings target');
                    await all.click();
                    await page.waitForURL('**/standings?sourceId=*');
                    await visit();
                } else {
                    assert(await page.locator('.spf-standings').isVisible());
                    assert.equal(await page.locator('.spf-standings tbody tr').count(), teams);
                    assert.equal(await page.locator('.spf-rankings tbody tr').count(), 25);
                }
                await page.screenshot({ path: shots + `sports-${width}-${theme}.png`, fullPage: true });
                console.log('PASS', width, theme);
            }
        }
        // Accelerate only the server-provided first refresh delay; selection still uses real server time.
        await page.setViewportSize({ width: 1920, height: 1080 });
        await page.route(
            '**/sports',
            async (route) => {
                const response = await route.fetch();
                await route.fulfill({
                    response,
                    body: (await response.text()).replace(
                        /data-rotation-refresh-after="\d+"/,
                        'data-rotation-refresh-after="1000"',
                    ),
                });
            },
            { times: 1 },
        );
        const refreshResponse = page.waitForResponse(
            (r) => new URL(r.url()).pathname === '/sports' && r.request().resourceType() === 'fetch',
        );
        await visit();
        const refreshed = await refreshResponse;
        assert.equal(refreshed.status(), 200, 'Automatic panel refresh');
        await page.waitForFunction(
            () => Number(document.querySelector('[data-sports-rotation]').dataset.rotationRefreshAfter) > 1000,
        );
        assert.equal(await page.locator('.spf-rankings tbody tr').count(), 25);
        console.log('PASS automatic hourly panel refresh');
        const noJs = await browser.newContext({
            storageState,
            javaScriptEnabled: false,
            viewport: { width: 1920, height: 1080 },
        });
        const fallback = await noJs.newPage();
        await fallback.goto(base + '/sports', { waitUntil: 'networkidle' });
        assert.equal(await fallback.locator('.spf-rankings tbody tr').count(), 25);
        await fallback.locator('.spf-rankings .ef-more').click();
        await fallback.waitForURL('**/' + rankingHref);
        await noJs.close();
        await context.close();
        console.log('PASS full standings, ordered Top 25, hourly stability, phone links, no-JS poll link');
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
