// Complete Sports tables, hourly refresh and mobile/no-JavaScript regressions in a disposable profile.
const { chromium } = require('playwright');
const assert = require('node:assert/strict');
const fs = require('node:fs');
const base = process.env.BASE_URL || 'http://localhost:5295';
const adminBase = process.env.ADMIN_BASE_URL || base;
const password = process.env.ADMIN_PASSWORD;
const shots = process.env.SCREENSHOT_DIR || 'test-results/browser/ipad-sports/';
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
            const name = 'zz iPad Sports ' + Date.now().toString(36);
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

        const context = await browser.newContext({
            storageState,
            hasTouch: true,
            viewport: { width: 820, height: 1100 },
        });
        const page = await context.newPage();
        const visit = async () => {
            await page.goto(base + '/sports', { waitUntil: 'networkidle' });
        };
        await visit();
        const layout = async () => {
            assert(await page.locator('.spf-scores').isVisible());
            const boxes = await page
                .locator('.spf-cols, .spf-cols .ef-lead, .spf-upcoming, .spf-side')
                .evaluateAll((els) =>
                    els.map((e) => ({
                        x: e.getBoundingClientRect().x,
                        y: e.getBoundingClientRect().y,
                        w: e.getBoundingClientRect().width,
                    })),
                );
            const [cols, up, lead, side] = boxes; // DOM order: columns, upcoming, lead, side
            assert(Math.abs(lead.w - cols.w) < 2, 'Lead full width');
            assert(side.x > up.x && side.y >= lead.y, 'Upcoming left, tables right');
            assert.equal(await page.locator('.ef-lead__title').evaluate((e) => getComputedStyle(e).fontSize), '36px');
            for (const table of await page.locator('.spf-side tbody').all()) {
                const rows = table.locator('tr');
                assert.equal(await rows.filter({ visible: true }).count(), Math.min(10, await rows.count()));
            }
        };
        for (const theme of ['light', 'dark']) {
            const saved = page.waitForResponse(
                (r) => new URL(r.url()).pathname === '/api/me/settings' && r.request().method() === 'PUT',
            );
            await page.evaluate((v) => window.myNewspaperPrefs.setItem('theme', v), theme);
            await (await saved).finished();
            for (const width of (process.env.WIDTHS || '320,390,430,743,744,820,834,1023,1024,1280,1920')
                .split(',')
                .map(Number)) {
                await page.setViewportSize({ width, height: 1100 });
                await visit();
                assert(
                    await page.evaluate(() => document.documentElement.scrollWidth <= innerWidth + 1),
                    'No overflow ' + width,
                );
                if (width >= 744 && width < 1024) await layout();
                else if (width < 744) {
                    assert(await page.locator('.tab-phone').isVisible());
                    const fold = page.locator('.tab-phone details').first();
                    if (await fold.count()) {
                        const open = await fold.getAttribute('open');
                        await fold.locator('summary').tap();
                        assert.notEqual(await fold.getAttribute('open'), open);
                    }
                } else {
                    assert(await page.locator('.spf-scores').isVisible());
                    assert.equal(await page.locator('.spf-rankings tbody tr:visible').count(), 25);
                }
                await page.evaluate(() => scrollTo(0, 0));
                await page.screenshot({ path: shots + `sports-${width}-${theme}.png`, fullPage: true });
                console.log('PASS', width, theme);
            }
        }
        await page.setViewportSize({ width: 820, height: 1100 });
        await visit();
        const swipe = async () => {
            const rail = page.locator('.spf-scores .scoreboard-games').first();
            await page.evaluate(() => scrollTo(0, 0));
            assert(await rail.evaluate((e) => e.scrollWidth > e.clientWidth));
            const box = await rail.boundingBox(),
                x = box.x + box.width - 70,
                y = box.y + 40;
            const cdp = await context.newCDPSession(page);
            await cdp.send('Input.dispatchTouchEvent', { type: 'touchStart', touchPoints: [{ x, y }] });
            for (let i = 1; i <= 10; i++) {
                await cdp.send('Input.dispatchTouchEvent', { type: 'touchMove', touchPoints: [{ x: x - i * 30, y }] });
                await page.waitForTimeout(25);
            }
            await cdp.send('Input.dispatchTouchEvent', { type: 'touchEnd', touchPoints: [] });
            await page.waitForFunction(() => document.querySelector('.spf-scores .scoreboard-games').scrollLeft > 50);
            await cdp.detach();
        };
        await swipe();
        const panel = page.locator('.spf-scores [data-scoreboard]').first();
        await panel.evaluate((e) => (e.dataset.testOld = 'yes'));
        const refreshed = page.waitForResponse((r) => r.url().includes('/scoreboard/fragment'));
        await page.evaluate(() => dispatchEvent(new Event('online')));
        await refreshed;
        await page.waitForFunction(() => !document.querySelector('.spf-scores [data-test-old]'));
        await swipe();
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
        const rotated = page.waitForResponse(
            (r) => new URL(r.url()).pathname === '/sports' && r.request().resourceType() === 'fetch',
        );
        await visit();
        await rotated;
        await page.waitForFunction(
            () => Number(document.querySelector('[data-sports-rotation]').dataset.rotationRefreshAfter) > 1000,
        );
        await layout();
        const lower = page.locator('.spf-standings tbody tr').nth(14);
        const favorite = await lower.evaluate((e) => JSON.parse(e.dataset.sportsTeams)[0]);
        const favoriteSaved = page.waitForResponse(
            (r) => new URL(r.url()).pathname === '/api/me/settings' && r.request().method() === 'PUT',
        );
        await page.evaluate(
            (team) => window.myNewspaperPrefs.setItem('mynewsfeed-sports-teams-v1', JSON.stringify([team])),
            favorite,
        );
        await (await favoriteSaved).finished();
        await visit();
        assert(await lower.isVisible(), 'Persisted favorite below row ten remains visible');
        assert(await lower.evaluate((e) => e.classList.contains('sports-selected')));
        for (const selector of [
            '.spf-scores .ef-head a',
            '.spf-upcoming .ef-head a',
            '.spf-standings .ef-more',
            '.spf-rankings .ef-more',
        ]) {
            await visit();
            const link = page.locator(selector),
                href = await link.getAttribute('href');
            await link.tap();
            await page.waitForURL(base + '/' + href);
            await page.waitForLoadState('networkidle');
        }
        for (const route of ['/news', '/']) {
            await page.goto(base + route, { waitUntil: 'networkidle' });
            assert.equal(await page.locator('.tab-desktop').count(), 0);
        }
        await page.goto(base + '/entertainment', { waitUntil: 'networkidle' });
        assert(await page.locator('.ef-lead').isVisible());
        const plain = await browser.newContext({
                storageState,
                javaScriptEnabled: false,
                viewport: { width: 820, height: 1100 },
            }),
            fallback = await plain.newPage();
        await fallback.goto(base + '/sports', { waitUntil: 'networkidle' });
        assert(await fallback.locator('.spf-scores').isVisible());
        await fallback.locator('.spf-rankings .ef-more').click();
        await fallback.waitForURL('**/rankings?*');
        await plain.close();
        await context.close();
        console.log(
            'PASS swipe, score replacement, hourly replacement, favorite exception, touch links, isolation, no-JS',
        );
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
