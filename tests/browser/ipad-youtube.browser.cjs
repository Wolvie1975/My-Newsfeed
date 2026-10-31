// iPad YouTube layout, using a disposable reader profile.
const { chromium } = require('playwright');
const assert = require('node:assert/strict');
const fs = require('node:fs');
const base = process.env.BASE_URL || 'http://localhost:5295';
const adminBase = process.env.ADMIN_BASE_URL || base;
const password = process.env.ADMIN_PASSWORD;
const shots = process.env.SCREENSHOT_DIR || 'test-results/browser/ipad-youtube/';
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
            const name = 'zz iPad YouTube ' + Date.now().toString(36);
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
        await context.addInitScript(() => {
            Object.defineProperty(navigator, 'clipboard', {
                value: { writeText: async (value) => (window.copied = value) },
            });
        });
        const visitVideos = async () => {
            await page.goto(base + '/videos', { waitUntil: 'networkidle' });
            await page.locator('.yt-top').waitFor({ state: 'attached' });
        };
        const overflow = async () =>
            assert(
                await page.evaluate(() => document.documentElement.scrollWidth <= innerWidth + 1),
                'No horizontal overflow',
            );
        const geometry = async () => {
            const g = await page.evaluate(() => {
                const box = (s) => {
                    const r = document.querySelector(s).getBoundingClientRect();
                    return { x: r.x, y: r.y, w: r.width, h: r.height, right: r.right, bottom: r.bottom };
                };
                return {
                    top: box('.yt-top'),
                    feature: box('.yt-feature'),
                    latest: box('.yt-latest'),
                    aside: document.querySelector('.yt-aside') ? box('.yt-aside') : null,
                    pills: box('.yt-cats'),
                    channel: box('#channel'),
                    count: box('.count--toolbar'),
                    cols: getComputedStyle(document.querySelector('.video-front .videos')).gridTemplateColumns.split(
                        ' ',
                    ).length,
                    title: getComputedStyle(document.querySelector('.yt-slide__title')).fontSize,
                    thumb: box('.yt-row__thumb').w,
                    tabs: getComputedStyle(document.querySelector('.yt-tabs')).gridTemplateColumns.split(' ').length,
                };
            });
            assert(Math.abs(g.top.w - g.feature.w) < 2, 'Full width feature');
            assert(g.latest.y >= g.feature.bottom - 1, 'Latest below feature');
            if (g.aside) {
                assert(Math.abs(g.aside.y - g.latest.y) < 2, 'Latest and aside share row');
                assert(g.latest.right <= g.aside.x + 1, 'Columns do not overlap');
            } else assert(Math.abs(g.latest.w - g.top.w) < 2, 'Latest fills width without aside');
            assert(g.pills.right <= g.channel.x + 1, 'Channel stays right of pills');
            if (g.count.y < g.channel.bottom - 1) {
                console.log(g);
                await page.screenshot({ path: shots + 'geometry.png' });
            }
            assert(g.count.y >= g.channel.bottom - 1, 'Count below controls');
            assert.equal(g.cols, 3, 'Three More videos columns');
            assert.equal(g.tabs, 5, 'Five picks');
            assert.equal(g.title, '30px');
            assert(Math.abs(g.thumb - 132) < 2, '132px row thumbnail');
            await overflow();
        };
        for (const theme of ['light', 'dark']) {
            await preference('theme', theme);
            for (const width of (process.env.WIDTHS || '320,390,430,743,744,820,834,1023,1024,1280,1440,1920,2560,3840')
                .split(',')
                .map(Number)) {
                await page.setViewportSize({ width, height: 1100 });
                await visitVideos();
                await overflow();
                const tablet = width >= 744 && width < 1024;
                assert.equal(
                    await page.locator('.yt-feature').isVisible(),
                    width >= 744,
                    'Correct desktop content range',
                );
                assert.equal(
                    await page.locator('[data-phone-life="videos"]').isVisible(),
                    width < 744,
                    'Correct phone content range',
                );
                assert.equal(await page.locator('html').getAttribute('data-theme'), theme);
                if (width >= 1024) {
                    const cols = await page
                        .locator('.video-front .videos')
                        .evaluate((n) => getComputedStyle(n).gridTemplateColumns.split(' ').length);
                    assert.equal(cols, width >= 2560 ? 6 : width >= 1920 ? 5 : 4, 'Existing desktop card columns');
                }
                if (tablet) {
                    await geometry();
                    for (const button of await page.locator('.yt-feature__controls button').all()) {
                        const r = await button.boundingBox();
                        assert(r.width >= 44 && r.height >= 44, '44px carousel target');
                    }
                }
                if (width < 744) {
                    await page.locator('.mobile-nav summary').tap();
                    assert(await page.locator('.mobile-nav__links a[href="videos"]').isVisible());
                    await page.locator('.mobile-nav summary').tap();
                    const fold = page.locator('[data-phone-life="videos"] details').first();
                    const wasOpen = (await fold.getAttribute('open')) !== null;
                    await fold.locator('summary').tap();
                    assert.equal((await fold.getAttribute('open')) !== null, !wasOpen, 'Phone fold toggles');
                    assert((await fold.locator('summary').boundingBox()).height >= 44, '44px phone fold');
                }
                await page.reload({ waitUntil: 'networkidle' });
                await overflow();
                await page.evaluate(() => window.scrollTo(0, 0));
                await page.screenshot({ path: shots + `youtube-${width}-${theme}.png`, fullPage: true });
                if (tablet) await page.screenshot({ path: shots + `youtube-top-${width}-${theme}.png` });
                console.log('PASS', width, theme);
            }
        }
        if (process.env.MATRIX_ONLY === '1') {
            await context.close();
            console.log('PASS requested layout matrix, theme persistence, Refresh and phone controls');
            return;
        }
        await page.setViewportSize({ width: 820, height: 1100 });
        await visitVideos();
        const current = async () => page.locator('[data-yt-index]').innerText();
        const first = await current();
        assert.equal(await page.locator('[data-yt-toggle]').getAttribute('aria-pressed'), 'false');
        await page.waitForTimeout(7500);
        assert.equal(await current(), first, 'No auto rotation before Play');
        await page.locator('[data-yt-step="1"]').tap();
        assert.notEqual(await current(), first);
        await page.locator('[data-yt-step="-1"]').tap();
        assert.equal(await current(), first);
        await page.locator('[data-yt-pick="3"]').tap();
        assert.equal(await current(), '4');
        await page.locator('[data-yt-toggle]').tap();
        await page.waitForFunction(
            () => document.querySelector('[data-yt-index]').textContent !== '4',
            {},
            { timeout: 10000 },
        );
        await page.locator('[data-yt-toggle]').tap();
        assert.equal(await page.locator('[data-yt-toggle]').getAttribute('aria-pressed'), 'false');
        console.log('PASS manual carousel and opt-in rotation');
        // Use the narrow tablet where the real category list overflows.
        await page.setViewportSize({ width: 744, height: 1100 });
        await visitVideos();
        await page.evaluate(() => window.scrollTo(0, 0));
        // Swipe the category strip through real touch events.
        const pills = page.locator('.yt-cats');
        await pills.scrollIntoViewIfNeeded();
        const pb = await pills.boundingBox();
        const cdp = await context.newCDPSession(page);
        const tx = Math.round(pb.x + pb.width - 40),
            ty = Math.round(pb.y + pb.height / 2);
        await cdp.send('Input.dispatchTouchEvent', { type: 'touchStart', touchPoints: [{ x: tx, y: ty }] });
        for (let step = 1; step <= 8; step++) {
            await cdp.send('Input.dispatchTouchEvent', {
                type: 'touchMove',
                touchPoints: [{ x: tx - step * 25, y: ty }],
            });
            await page.waitForTimeout(25);
        }
        await cdp.send('Input.dispatchTouchEvent', { type: 'touchEnd', touchPoints: [] });
        await page.waitForFunction(() => document.querySelector('.yt-cats').scrollLeft > 30);
        await cdp.detach();
        const category = page.locator('.yt-cats a').nth(1),
            href = await category.getAttribute('href');
        await category.tap();
        await page.waitForURL(base + '/' + href);
        await page.waitForLoadState('networkidle');
        await page
            .locator('.yt-cats a[aria-current="page"]')
            .filter({ hasText: await category.innerText() })
            .waitFor();
        const channel = await page.locator('#channel option').nth(1).getAttribute('value');
        await page.locator('#channel').selectOption(channel);
        await page.waitForURL('**/videos?**channel=**');
        await page.waitForLoadState('networkidle');
        await visitVideos();
        const next = page.locator('.yt-pages a').filter({ hasText: 'More videos' });
        await next.tap();
        await page.waitForURL('**/videos?**page=2**');
        await page.waitForLoadState('networkidle');
        const previous = page.locator('.yt-pages a').filter({ hasText: 'Newer videos' });
        const previousHref = await previous.getAttribute('href');
        await previous.tap();
        await page.waitForURL(base + '/' + previousHref);
        await previous.waitFor({ state: 'detached' });
        await page.waitForLoadState('networkidle');
        await visitVideos();
        for (const selector of ['.yt-row', '.video-front .video']) {
            const save = page.locator(selector + ' [data-save][aria-pressed="false"]').first();
            await save.waitFor();
            const saveBox = await save.boundingBox();
            assert(saveBox.width >= 44 && saveBox.height >= 44, '44px Watch later target');
            const url = await save.getAttribute('data-url');
            const savedTitle = await save.getAttribute('data-title');
            const response = page.waitForResponse(
                (r) => new URL(r.url()).pathname.startsWith('/api/saved/') && r.request().method() === 'POST',
            );
            await save.tap();
            assert.equal((await response).status(), 200);
            await page.locator('[data-yt-watch-list] a').filter({ hasText: savedTitle }).first().waitFor();
            await page.reload({ waitUntil: 'networkidle' });
            assert((await page.locator('[data-yt-watch-list] a').count()) > 0, 'Saved video persists');
            await page.evaluate(() => window.scrollTo(0, 0));
            await page.screenshot({
                path: shots + (selector === '.yt-row' ? 'youtube-saved-row.png' : 'youtube-saved-card.png'),
                fullPage: true,
            });
            const share = page.locator(selector + ' [data-share-open]').first();
            const shareBox = await share.boundingBox();
            assert(shareBox.width >= 44 && shareBox.height >= 44, '44px Share target');
            const shareUrl = await share.getAttribute('data-url');
            await share.tap();
            const dialog = page.getByRole('dialog');
            await dialog.waitFor();
            await overflow();
            await dialog.getByRole('button', { name: 'Copy link', exact: true }).tap();
            await dialog.waitFor({ state: 'detached' });
            assert.equal(await page.evaluate(() => window.copied), shareUrl);
            const exact = page.locator('[data-save]').filter({ visible: true });
            const target = await exact.evaluateAll(
                (ns, u) => ns.findIndex((n) => n.getAttribute('data-url') === u),
                url,
            );
            assert(target >= 0, 'Saved item has a remove control');
            const deleted = page.waitForResponse(
                (r) => new URL(r.url()).pathname.startsWith('/api/saved/') && r.request().method() === 'POST',
            );
            await exact.nth(target).tap();
            assert.equal((await deleted).status(), 200);
        }
        console.log('PASS touch categories, channel filter, paging, Watch later persistence/removal and Share');
        await page.setViewportSize({ width: 820, height: 1100 });
        await visitVideos();
        await page.locator('.yt-aside').evaluate((n) => n.remove());
        await geometry();
        await page.screenshot({ path: shots + 'youtube-no-aside.png' });
        for (const scheme of ['monochrome', 'cardinals']) {
            await preference('mynewsfeed-color-scheme', scheme);
            await visitVideos();
            await geometry();
            await page.screenshot({ path: shots + `youtube-820-${scheme}.png` });
        }
        for (const route of ['/', '/news']) {
            await page.goto(base + route, { waitUntil: 'networkidle' });
            assert((await page.locator('.mx-phone:visible').count()) > 0, 'Other fronts keep phone layout');
        }
        await page.goto(base + '/watch?services=8,9,337', { waitUntil: 'networkidle' });
        assert(await page.locator('#watch-query').isVisible());
        assert(!(await page.locator('[data-phone-watch-ribbon]').isVisible()));
        await overflow();
        await page.screenshot({ path: shots + 'watch-regression-820.png' });
        const noJs = await browser.newContext({
            storageState,
            javaScriptEnabled: false,
            viewport: { width: 820, height: 1100 },
        });
        const fallback = await noJs.newPage();
        await fallback.goto(base + '/videos', { waitUntil: 'networkidle' });
        assert(await fallback.locator('.yt-feature').isVisible());
        assert.equal(await fallback.locator('.yt-btn:visible').count(), 0);
        assert.equal(await fallback.locator('[data-yt-slide]:visible').count(), 1);
        const choice = await fallback.locator('#channel option').nth(1).getAttribute('value');
        await fallback.locator('#channel').selectOption(choice);
        await fallback.locator('.filters--videos button').click();
        await fallback.waitForURL('**/videos?**channel=**');
        await fallback.setViewportSize({ width: 390, height: 900 });
        await fallback.goto(base + '/videos', { waitUntil: 'networkidle' });
        assert(await fallback.locator('[data-phone-life="videos"]').isVisible());
        await fallback.locator('.mobile-nav summary').click();
        assert(await fallback.locator('.mobile-nav__links a[href="videos"]').isVisible());
        await noJs.close();
        await context.close();
        console.log('PASS no-aside fallback, schemes, page isolation, Watch regression and no-JS navigation');
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
