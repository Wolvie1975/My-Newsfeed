// Wide-screen grids, zoom geometry and phone/tablet regressions against representative content.
const { chromium } = require('playwright');
const assert = require('node:assert/strict');
const fs = require('node:fs');
const base = process.env.BASE_URL || 'http://localhost:5295';
const shots = process.env.SCREENSHOT_DIR || 'test-results/browser/wide-screens/';
(async () => {
    fs.mkdirSync(shots, { recursive: true });
    const browser = await chromium.launch();
    const profiles = (await fetch(base + '/editions')).status !== 404;
    const adminBase = process.env.ADMIN_BASE_URL || base;
    const password = process.env.ADMIN_PASSWORD;
    let admin = null,
        id = null,
        storageState = process.env.STORAGE_STATE;
    try {
        if (profiles && !storageState) {
            assert(password, 'ADMIN_PASSWORD is required to create and remove a disposable reader profile');
            admin = await (await browser.newContext({ viewport: { width: 1440, height: 1000 } })).newPage();
            await admin.goto(adminBase + '/login?returnUrl=%2Fadmin%2Fprofiles');
            await admin.locator('#password').fill(password);
            await admin.getByRole('button', { name: 'Sign in', exact: true }).click();
            await admin.waitForURL('**/admin/profiles');
            await admin.waitForLoadState('networkidle');
            const name = 'zz Phone ' + Date.now().toString(36);
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
            await p.goto(base + '/news/all', { waitUntil: 'domcontentloaded' });
            await p.locator(`form[action="profiles/${id}/open"]`).getByRole('button').click();
            await p.keyboard.type('3691');
            await p.waitForURL('**/news/all');
            storageState = await context.storageState();
            await context.close();
        }

        const context = await browser.newContext({ storageState });
        const page = await context.newPage();
        for (const width of process.env.WIDTHS
            ? process.env.WIDTHS.split(',').map(Number)
            : [320, 390, 430, 768, 1024, 1280, 1440, 1920, 2560, 3200, 3840]) {
            for (const theme of ['light', 'dark']) {
                await page.setViewportSize({ width, height: width >= 3200 ? 2160 : width >= 2560 ? 1440 : 1080 });
                await page.addInitScript((t) => localStorage.setItem('theme', t), theme);
                for (const route of ['/', '/news', '/sports', '/entertainment', '/news/all', '/videos']) {
                    await page.goto(base + route, { waitUntil: 'networkidle' });
                    if (profiles && (await page.evaluate(() => document.documentElement.dataset.theme)) !== theme) {
                        const saved = page.waitForResponse(
                            (r) => new URL(r.url()).pathname === '/api/me/settings' && r.request().method() === 'PUT',
                        );
                        await page.evaluate((t) => window.myNewspaperPrefs.setItem('theme', t), theme);
                        assert.equal((await saved).status(), 200);
                        await page.reload({ waitUntil: 'networkidle' });
                    }

                    assert(
                        !page.url().includes('/profiles'),
                        'Open a disposable reader profile or use a profiles-disabled preview',
                    );
                    await page.evaluate(() => document.fonts.ready);
                    const geometry = await page.evaluate(() => {
                        const columns = (selector) => {
                            const el = [...document.querySelectorAll(selector)].find(
                                (n) => n.getBoundingClientRect().width > 0,
                            );
                            return el ? getComputedStyle(el).gridTemplateColumns.split(' ').length : 0;
                        };
                        return {
                            overflow: document.documentElement.scrollWidth > innerWidth + 1,
                            home: columns('.bs-sheet'),
                            cards: columns('.hc-grid'),
                            videos: columns('.video-front .videos'),
                            zoom: Number(getComputedStyle(document.querySelector('.pub')).zoom),
                            headerPad: parseFloat(getComputedStyle(document.querySelector('.site-header')).paddingLeft),
                            pagePad: parseFloat(getComputedStyle(document.querySelector('main.page')).paddingLeft),
                        };
                    });
                    assert(!geometry.overflow, `${route} ${width} ${theme}: horizontal overflow`);
                    if (width >= 1920) {
                        assert.equal(geometry.zoom, width >= 3200 ? 1.25 : 1, `${route}: zoom`);
                        assert.equal(geometry.headerPad, 56, 'Header edge padding');
                        assert.equal(geometry.pagePad, 56, 'Page edge padding');
                        if (route === '/') {
                            assert.equal(geometry.home, width >= 2560 ? 6 : 4, 'Home columns');
                            const positions = await page.locator('.bs-sheet .bs-col').evaluateAll((ns) =>
                                ns.map((n) => {
                                    const r = n.getBoundingClientRect();
                                    return { x: r.x, y: r.y, right: r.right, bottom: r.bottom };
                                }),
                            );
                            for (let i = 0; i < positions.length; i++)
                                for (const b of positions.slice(i + 1)) {
                                    const a = positions[i];
                                    assert(
                                        a.right <= b.x + 1 ||
                                            b.right <= a.x + 1 ||
                                            a.bottom <= b.y + 1 ||
                                            b.bottom <= a.y + 1,
                                        'Home columns overlap',
                                    );
                                }
                            assert(Math.abs(positions[0].y - positions[3].y) < 1, 'Scoreboard joins first row');
                            assert.equal(
                                await page.locator('.bs-band').isVisible(),
                                width < 2560,
                                'Home band visibility',
                            );
                        }
                        if (route === '/news/all')
                            assert.equal(geometry.cards, width >= 2560 ? 5 : 4, 'Headline columns');
                        if (route === '/videos') assert.equal(geometry.videos, width >= 2560 ? 6 : 5, 'Video columns');
                    }
                    if (route === '/' && width <= 430) {
                        const menu = page.locator('[data-mobile-nav] summary');
                        await menu.click();
                        assert(await page.locator('.mobile-nav__links').isVisible(), 'Phone menu opens');
                        await menu.click();
                    }
                    if (route === '/' && width === 3840) {
                        await page.evaluate(() => scrollTo(0, 900));
                        assert(
                            Math.abs((await page.locator('.site-header').boundingBox()).y) < 1,
                            'Zoomed sticky header',
                        );
                        const share = page.locator('[data-share-open]:visible').first();
                        await share.click();
                        const dialog = page.getByRole('dialog').filter({ visible: true }).first();
                        await dialog.waitFor();
                        const box = await dialog.boundingBox();
                        assert(box.x >= 0 && box.x + box.width <= width && box.y >= 0, 'Zoomed share dialog bounds');
                        await page.keyboard.press('Escape');
                        await page.evaluate(() => scrollTo(0, 0));
                    }
                    await page.screenshot({
                        path: `${shots}${route.replaceAll('/', '') || 'home'}-${width}-${theme}.png`,
                    });
                }
                console.log(`PASS ${width} ${theme}: six fronts, overflow and grid geometry`);
            }
        }
        for (const width of [1920, 2560, 3840]) {
            await page.setViewportSize({ width, height: 1440 });
            for (const route of ['/watch', '/scoreboard', '/standings', '/movies', '/tv', '/saved', '/settings']) {
                await page.goto(base + route, { waitUntil: 'networkidle' });
                assert(
                    await page.evaluate(() => document.documentElement.scrollWidth <= innerWidth + 1),
                    `${route} ${width}: overflow`,
                );
            }
        }
        await page.setViewportSize({ width: 2560, height: 1440 });
        for (const scheme of ['monochrome', 'cardinals']) {
            await page.goto(base + '/', { waitUntil: 'networkidle' });
            if (profiles) {
                const saved = page.waitForResponse(
                    (r) => new URL(r.url()).pathname === '/api/me/settings' && r.request().method() === 'PUT',
                );
                await page.evaluate((s) => window.myNewspaperPrefs.setItem('mynewsfeed-color-scheme', s), scheme);
                assert.equal((await saved).status(), 200);
            } else {
                await page.evaluate((s) => localStorage.setItem('mynewsfeed-color-scheme', s), scheme);
            }
            await page.reload({ waitUntil: 'networkidle' });
            assert.equal(await page.evaluate(() => document.documentElement.dataset.colorScheme), scheme);
            assert(
                await page.evaluate(() => document.documentElement.scrollWidth <= innerWidth + 1),
                scheme + ' overflow',
            );
            await page.screenshot({ path: shots + 'home-2560-' + scheme + '.png' });
        }
        console.log('PASS wide secondary pages, extra schemes and all requested widths');
        await context.close();
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
