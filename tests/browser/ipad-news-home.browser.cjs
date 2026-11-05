// Complete Sports tables, hourly refresh and mobile/no-JavaScript regressions in a disposable profile.
const { chromium } = require('playwright');
const assert = require('node:assert/strict');
const fs = require('node:fs');
const base = process.env.BASE_URL || 'http://localhost:5295';
const adminBase = process.env.ADMIN_BASE_URL || base;
const password = process.env.ADMIN_PASSWORD;
const shots = process.env.SCREENSHOT_DIR || 'test-results/browser/ipad-news-home/';
(async () => {
    fs.mkdirSync(shots, { recursive: true });
    const profiles = (await fetch(base + '/editions')).status !== 404;
    assert(profiles, 'Use profiles-enabled preview; this test never writes to a household Saved list');
    const browser = await chromium.launch({
        args: ['--disable-web-security'],
    });
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
            const name = 'zz iPad Final ' + Date.now().toString(36);
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
        await context.addInitScript(() => {
            window.copied = null;
            Object.defineProperty(window, 'isSecureContext', { value: true });
            Object.defineProperty(navigator, 'clipboard', {
                value: { writeText: async (v) => (window.copied = v) },
            });
            document.execCommand = () => {
                window.copied = document.querySelector('dialog textarea')?.value;
                return true;
            };
        });
        const page = await context.newPage(),
            errors = [];
        page.on('pageerror', (e) => errors.push(e.message));
        const visit = async (route) => {
            await page.goto(base + route, { waitUntil: 'networkidle' });
        };
        const preference = async (key, value) => {
            const saved = page.waitForResponse(
                (r) => new URL(r.url()).pathname === '/api/me/settings' && r.request().method() === 'PUT',
            );
            await page.evaluate(([k, v]) => window.myNewspaperPrefs.setItem(k, v), [key, value]);
            const response = await saved;
            assert.equal(response.status(), 200);
            await response.finished();
        };
        const box = async (selector) =>
            page
                .locator(selector)
                .first()
                .evaluate((e) => {
                    const r = e.getBoundingClientRect();
                    return { x: r.x, y: r.y, w: r.width, h: r.height, b: r.bottom };
                });
        const noOverflow = async () =>
            assert(
                await page.evaluate(() => document.documentElement.scrollWidth <= innerWidth + 1),
                'No viewport overflow',
            );
        const newsLayout = async () => {
            const frame = await box('.nf .spf-cols'),
                lead = await box('.nf .ef-lead'),
                day = await box('.nf-day'),
                side = await box('.nf .spf-side'),
                headlines = await box('.nf .spf-main > .ef-headlines');
            assert(Math.abs(lead.w - frame.w) < 2, 'News lead spans columns');
            assert(side.x > day.x, 'News side right');
            assert(headlines.y >= Math.max(side.b, day.b) - 1, 'Top headlines below both columns');
            assert(Math.abs(headlines.w - frame.w) < 2, 'Top headlines full width');
            assert.equal(
                await page.locator('.spf-main > .ef-headlines').evaluate((e) => getComputedStyle(e).columnCount),
                '2',
            );
            assert.equal(
                await page
                    .locator('.spf-side .ef-headlines')
                    .first()
                    .evaluate((e) => getComputedStyle(e).columnCount),
                'auto',
                'Side lists remain single-column',
            );
            const tiles = await page
                .locator('.tab-desktop .nf-tile')
                .evaluateAll((els) => els.map((e) => ({ x: e.offsetLeft, y: e.offsetTop, w: e.clientWidth })));
            assert(tiles.length >= 2);
            assert.equal(tiles[0].y, tiles[1].y);
            assert(tiles[1].x > tiles[0].x);
            assert(await page.locator('.tab-phone .nf-brief--phone').isHidden());
            assert.equal(
                await page.locator('.nf-topics > .ef-more').evaluate((e) => e.getBoundingClientRect().height),
                44,
            );
        };
        const homeLayout = async () => {
            const frame = await box('.bs-sheet'),
                lead = await box('.bs-home .d-lead'),
                left = await box('.bs-front > .bs-col--left'),
                main = await box('.bs-front > .bs-col--main > .d-news'),
                side = await box('.bs-front > .bs-col--side'),
                band = await box('.bs-band'),
                lower = await box('.bs-more > .bs-col--main'),
                releases = await box('.bs-more > .bs-col--side');
            assert(Math.abs(lead.w - frame.w) < 2, 'Home lead spans columns');
            assert(left.x < main.x && side.x > left.x, 'Home first two columns');
            assert(band.y >= Math.max(left.b, side.b) - 1, 'More from today follows front');
            assert(lower.y >= band.b - 1 && releases.x > lower.x, 'Home lower columns');
            assert.equal(
                await page.locator('.bs-title__greeting').evaluate((e) => getComputedStyle(e).fontSize),
                '48px',
            );
            assert(await page.locator('.bs-title__date').isVisible());
            assert(await page.locator('.bs-title__name').isHidden());
            assert.equal(
                await page.locator('.bs-home .d-lead__title').evaluate((e) => getComputedStyle(e).fontSize),
                '36px',
            );
            const image = await box('.bs-home .d-lead__image');
            assert(Math.abs(image.w / image.h - 16 / 9) < 0.02, 'Home photo/P8 ratio');
            assert(image.w > frame.w * 0.95);
            const weather = page.locator('.bs-glance__periods li');
            if (await weather.count()) {
                assert.equal(await weather.count(), 4);
                const ys = await weather.evaluateAll((els) => els.map((e) => e.getBoundingClientRect().y));
                assert(Math.max(...ys) - Math.min(...ys) < 2);
            }
            const date = await box('.bs-title__date'),
                refresh = await box('.bs-title .home-refresh');
            assert(Math.abs(date.y - refresh.y) < 20, 'Date and Refresh share line');
            const input = await box('#home-watch-query'),
                button = await box('.bs-watch button');
            assert(Math.abs(input.y - button.y) < 2, 'Watch search one row');
            assert.equal(await page.locator('.bs-live__tonight').count(), 0, 'Home score area remains sports-only');
        };
        await visit('/');
        for (const theme of ['light', 'dark']) {
            await preference('theme', theme);
            for (const width of (process.env.WIDTHS || '320,390,430,743,744,820,834,1023,1024,1280,1920,2560')
                .split(',')
                .map(Number)) {
                await page.setViewportSize({ width, height: 1100 });
                for (const [route, name] of [
                    ['/news', 'news'],
                    ['/', 'home'],
                ]) {
                    await visit(route);
                    await noOverflow();
                    if (width >= 744 && width < 1024) {
                        assert(await page.locator('.tab-phone').isHidden());
                        await (name === 'news' ? newsLayout() : homeLayout());
                    } else if (width < 744) {
                        assert(await page.locator('.tab-phone').isVisible());
                        const fold = page.locator('.tab-phone details').first();
                        if (await fold.count()) {
                            const open = await fold.getAttribute('open');
                            await fold.locator('summary').tap();
                            assert.notEqual(await fold.getAttribute('open'), open);
                        }
                    } else assert(await page.locator(name === 'news' ? '.nf' : '.bs-home').isVisible());
                    await page.evaluate(() => {
                        document.querySelectorAll('img[loading="lazy"]').forEach((e) => (e.loading = 'eager'));
                        scrollTo(0, 0);
                    });
                    await page.waitForTimeout(250);
                    await page.screenshot({ path: shots + `${name}-${width}-${theme}.png`, fullPage: true });
                    console.log('PASS', name, width, theme);
                }
            }
        }
        await page.setViewportSize({ width: 820, height: 1100 });
        await visit('/news');
        const follow = page.locator('.nf-topics > .ef-more');
        await follow.tap();
        await page.waitForURL('**/news/topics/follow');
        await page.locator('#tp-terms').waitFor();
        await page.locator('#tp-terms').fill('Kansas City');
        await page.locator('[data-topic-submit]').tap();
        await page.waitForURL('**/news/topics/*?*');
        await visit('/news');
        assert.match(await page.locator('.nf-topics__list').innerText(), /Kansas City/);
        // Exercise shared active-tab reveal on the front; linked category pages keep their own existing layout.
        await page.evaluate(() => {
            scrollTo(0, 0);
            const links = [...document.querySelectorAll('.tab-desktop .section-tabs a')];
            links.forEach((e) => e.removeAttribute('aria-current'));
            links.at(-1).setAttribute('aria-current', 'page');
            document.querySelector('.tab-desktop .section-tabs').scrollLeft = 0;
            dispatchEvent(new Event('resize'));
        });
        await page.waitForFunction(() => {
            const n = document.querySelector('.tab-desktop .section-tabs'),
                a = n.querySelector('[aria-current]').getBoundingClientRect(),
                r = n.getBoundingClientRect();
            return a.left >= r.left - 1 && a.right <= r.right + 1;
        });
        assert.equal(await page.evaluate(() => scrollY), 0);
        await page.locator('.tab-desktop .section-tabs a').last().tap();
        await page.waitForLoadState('networkidle');
        await noOverflow();
        // Actual Share and Save interactions in the changed fronts, using only this disposable edition.
        for (const [route, selector] of [
            ['/news', '.nf .ef-lead'],
            ['/', '.bs-home .d-lead'],
        ]) {
            await visit(route);
            const story = page.locator(selector),
                share = story.locator('[data-share-open]'),
                url = await share.getAttribute('data-url');
            await share.tap();
            const dialog = page.getByRole('dialog');
            await dialog.waitFor();
            await dialog.getByRole('button', { name: 'Copy link', exact: true }).tap();
            await dialog.waitFor({ state: 'detached' });
            assert.equal(await page.evaluate(() => window.copied), url);
            const save = story.locator('[data-save]');
            await page.waitForFunction((sel) => !document.querySelector(sel + ' [data-save]').disabled, selector);
            await save.tap();
            await page.waitForFunction(
                (sel) => document.querySelector(sel + ' [data-save]').getAttribute('aria-pressed') === 'true',
                selector,
            );
            await visit(route);
            assert.equal(await page.locator(selector + ' [data-save]').getAttribute('aria-pressed'), 'true');
            await page.locator(selector + ' [data-save]').tap();
        }
        await visit('/');
        const title = await page.locator('.bs-title__greeting').innerText();
        await page.locator('.bs-title .home-refresh').tap();
        await page.waitForLoadState('networkidle');
        assert.equal(await page.locator('.bs-title__greeting').innerText(), title);
        await homeLayout();
        await page.locator('#home-watch-query').fill('Coco');
        await page.locator('.bs-watch button').tap();
        await page.waitForURL('**/watch?*');
        await page.locator('#watch-query').waitFor({ state: 'visible' });
        assert.equal(new URL(page.url()).searchParams.get('q'), 'Coco');
        await visit('/');
        const originalOrder = await page
            .locator('.bs-sheet [data-home-section]')
            .evaluateAll((els) => els.map((e) => e.dataset.homeSection));
        // Home section controls were removed from Settings; retained persisted preferences still apply.
        await preference(
            'mynewsfeed-personal-v1',
            JSON.stringify({ hidden: ['forecasts'], order: [...originalOrder].reverse() }),
        );
        await visit('/');
        assert(await page.locator('.bs-home [data-home-section="forecasts"]').isHidden());
        assert.deepEqual(
            await page
                .locator('.bs-sheet [data-home-section]')
                .evaluateAll((els) => els.map((e) => e.dataset.homeSection)),
            originalOrder,
            'Saved order retains fixed broadsheet positions',
        );
        await preference('mynewsfeed-personal-v1', JSON.stringify({ hidden: [] }));
        await visit('/');
        assert(await page.locator('.bs-home [data-home-section="forecasts"]').isVisible());
        await homeLayout();
        const swipe = async () => {
            const rail = page.locator('.bs-live__list');
            // The server caps this strip at three cards; a test-only long sample exercises overflow at 820.
            await rail.evaluate((e) => {
                if (e.scrollWidth <= e.clientWidth && e.firstElementChild) {
                    while (e.children.length < 6) e.append(e.firstElementChild.cloneNode(true));
                }
            });
            await rail.scrollIntoViewIfNeeded();
            const b = await rail.boundingBox(),
                cdp = await context.newCDPSession(page),
                x = Math.min(b.x + b.width - 70, 750),
                y = b.y + 35;
            assert(await rail.evaluate((e) => e.scrollWidth > e.clientWidth), 'Live/Coming up strip overflows');
            await cdp.send('Input.dispatchTouchEvent', { type: 'touchStart', touchPoints: [{ x, y }] });
            for (let i = 1; i <= 10; i++) {
                await cdp.send('Input.dispatchTouchEvent', { type: 'touchMove', touchPoints: [{ x: x - i * 30, y }] });
                await page.waitForTimeout(25);
            }
            await cdp.send('Input.dispatchTouchEvent', { type: 'touchEnd', touchPoints: [] });
            await page.waitForFunction(() => document.querySelector('.bs-live__list').scrollLeft > 50);
            await cdp.detach();
        };
        await swipe();
        // Representative live and empty states are DOM-only; no score data is written.
        await page.locator('[data-home-live] h2').evaluate((e) => (e.textContent = 'Live now'));
        assert(await page.getByRole('heading', { name: 'Live now', exact: true }).isVisible());
        await page.locator('.bs-live__list').evaluate((e) => (e.innerHTML = ''));
        await page.locator('[data-home-live]').evaluate((e) => {
            const p = document.createElement('p');
            p.className = 'd-empty';
            p.textContent = 'No games on today’s scoreboard.';
            e.append(p);
        });
        await noOverflow();
        await page.route(
            (url) => new URL(url).pathname === '/',
            async (route) => {
                const response = await route.fetch();
                const text = await response.text();
                const replaced = text.replace(/data-lead-refresh-after="\d+"/, 'data-lead-refresh-after="1000"');
                await route.fulfill({
                    response,
                    body: replaced,
                });
            },
            { times: 1 },
        );
        const refreshed = page.waitForResponse((r) => new URL(r.url()).pathname === '/home/lead');
        await visit('/');
        await refreshed;
        await page.waitForFunction(
            () => Number(document.querySelector('[data-home-lead]').dataset.leadRefreshAfter) > 1000,
        );
        await homeLayout();
        // P8 remains large when the photo is absent; empty media without P8 remains hidden.
        await page.locator('.bs-home .d-lead__image img').evaluateAll((els) => els.forEach((e) => e.remove()));
        await homeLayout();
        assert(await page.locator('.bs-home .d-lead__image .ph').isVisible());
        await page.screenshot({ path: shots + 'home-820-p8.png' });
        await page.locator('.bs-home .d-lead__image .ph').evaluate((e) => e.remove());
        assert(await page.locator('.bs-home .d-lead__image').isHidden());
        for (const scheme of ['monochrome', 'cardinals']) {
            await preference('mynewsfeed-color-scheme', scheme);
            for (const route of ['/news', '/']) {
                await visit(route);
                await noOverflow();
                await (route === '/news' ? newsLayout() : homeLayout());
            }
        }
        for (const [route, selector] of [
            ['/sports', '.spf-scores'],
            ['/entertainment', '.ef-screen'],
            ['/videos', '.yt-feature'],
            ['/watch', '#watch-query'],
        ]) {
            await visit(route);
            assert(await page.locator(selector).isVisible());
            await noOverflow();
            if (route === '/sports') {
                const lead = await box('.ef-lead'),
                    list = await box('.spf-main > .ef-headlines');
                assert(list.w < lead.w * 0.6, 'Sports headlines remain left');
            }
        }
        const plain = await browser.newContext({
                storageState,
                javaScriptEnabled: false,
                viewport: { width: 820, height: 1100 },
            }),
            fallback = await plain.newPage();
        for (const route of ['/news', '/']) {
            await fallback.goto(base + route, { waitUntil: 'networkidle' });
            assert(await fallback.locator('.tab-phone').isHidden());
        }
        await fallback.locator('#home-watch-query').fill('Coco');
        await fallback.locator('.bs-watch button').click();
        await fallback.waitForURL('**/watch?*');
        await plain.close();
        await context.close();
        assert.deepEqual(errors, []);
        console.log(
            'PASS topics, tabs, Share/Save, Refresh/search, saved hidden/order prefs, score swipe/states, hourly lead, P8, schemes, other fronts, no-JS',
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
