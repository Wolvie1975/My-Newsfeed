// Real profile-scoped category counts and Saved updates, plus wide/mobile/no-JS regressions.
const { chromium } = require('playwright');
const lib = require('./lib.cjs');
const assert = require('node:assert/strict');
const fs = require('node:fs');
const base = lib.base;
const password = lib.adminPassword;
const shots = lib.screenshots('youtube-sidebar');
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
            const profile = await lib.addProfile(admin, 'YtSidebar', { pin: '3691' });
            storageState = await lib.openProfile(browser, profile, { path: '/videos', waitUntil: 'domcontentloaded' });
        }

        const context = await browser.newContext({ storageState, viewport: { width: 2560, height: 1440 } });
        const page = await context.newPage();
        const errors = [];
        page.on('pageerror', (e) => errors.push(e.message));
        const visit = async () => {
            await page.goto(base + '/videos', { waitUntil: 'networkidle' });
            await page.locator('.yt-top').waitFor({ state: 'attached' });
        };
        const waitList = async (urls) => {
            await page.waitForFunction(
                (expected) =>
                    JSON.stringify([...document.querySelectorAll('[data-yt-watch-list] a')].map((a) => a.href)) ===
                    JSON.stringify(expected),
                urls,
            );
        };
        const toggle = async (button) => {
            const saved = page.waitForResponse(
                (r) => new URL(r.url()).pathname.startsWith('/api/saved/') && r.request().method() === 'POST',
            );
            await button.click();
            const response = await saved;
            assert.equal(response.status(), 200);
            const result = await response.json();
            const urls = result.articles
                .filter((v) => v.kind === 'video')
                .slice(0, 3)
                .map((v) => v.url);
            await waitList(urls);
            return urls;
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
        const noOverflow = async () =>
            assert(
                await page.evaluate(() => document.documentElement.scrollWidth <= innerWidth + 1),
                'Horizontal overflow',
            );
        await visit();
        assert(await page.locator('.yt-aside').isVisible());
        assert.equal(await page.locator('[data-yt-watch-later]').isVisible(), false, 'Empty Watch later stays hidden');
        const counts = await page.locator('.yt-channel-counts a').evaluateAll((ns) =>
            ns.map((n) => ({
                href: n.getAttribute('href'),
                count: n.querySelector('.yt-category-count').textContent,
            })),
        );
        assert(counts.length > 0, 'Representative followed categories');
        assert(
            counts.every((c) => /^\d+$/.test(c.count)),
            'Numeric today counts',
        );
        const buttons = page.locator('.mx-desktop [data-save][data-kind="video"]').filter({ visible: true });
        await buttons.first().waitFor({ state: 'visible' });
        const videos = (
            await buttons.evaluateAll((ns) => ns.map((n) => ({ url: n.dataset.url, title: n.dataset.title })))
        )
            .filter((v, i, ns) => ns.findIndex((n) => n.url === v.url) === i)
            .slice(0, 4);
        assert.equal(videos.length, 4, 'Four representative videos');
        const buttonFor = async (url) =>
            page
                .locator(
                    '.mx-desktop ' + (await page.evaluate((u) => '[data-save][data-url="' + CSS.escape(u) + '"]', url)),
                )
                .first();
        let expected = [];
        for (const video of videos) expected = await toggle(await buttonFor(video.url));
        assert.equal(expected.length, 3, 'Latest three only');
        assert.equal(expected[0], videos[3].url, 'Saved time order');
        expected = await toggle(await buttonFor(videos[3].url));
        assert.equal(expected[0], videos[2].url, 'Removal refills with older saves');
        const payload = await (await context.request.get(base + '/api/saved')).json();
        const dangerousTitle = '<img src=x onerror="window.sidebarInjected=true"> saved video';
        const add = await context.request.post(base + '/api/saved/add', {
            headers: { 'X-Saved-CSRF': payload.token },
            data: {
                url: 'https://example.test/sidebar-safe-title',
                title: dangerousTitle,
                source: 'Sidebar test',
                kind: 'video',
            },
        });
        assert.equal(add.status(), 200);
        expected = (await add.json()).articles
            .filter((v) => v.kind === 'video')
            .slice(0, 3)
            .map((v) => v.url);
        await visit();
        await waitList(expected);
        assert((await page.locator('[data-yt-watch-list]').innerText()).includes(dangerousTitle));
        assert.equal(await page.locator('[data-yt-watch-list] img').count(), 0, 'Saved text stays text');
        assert.equal(await page.evaluate(() => window.sidebarInjected), undefined);
        console.log('PASS real saves/removal, latest-three order, reload and safe saved text');
        await page.locator('.yt-channel-counts a').first().click();
        await page.waitForURL(/category=/);
        await page.waitForLoadState('networkidle');
        await page.locator('.yt-channel-counts a[aria-current="page"]').waitFor();
        assert.equal(await page.locator('.yt-channel-counts a[aria-current="page"]').count(), 1);
        assert.deepEqual(
            await page.locator('.yt-channel-counts a').evaluateAll((ns) =>
                ns.map((n) => ({
                    href: n.getAttribute('href'),
                    count: n.querySelector('.yt-category-count').textContent,
                })),
            ),
            counts,
            'Counts independent of selected category',
        );
        await visit();
        for (const theme of lib.THEMES) {
            await preference('theme', theme);
            for (const width of lib.widths([320, 390, 430, 768, 1024, 1280, 1440, 1920, 2560, 3840])) {
                await page.setViewportSize({ width, height: width === 3840 ? 2160 : width === 2560 ? 1440 : 1080 });
                await visit();
                await noOverflow();
                assert.equal(await page.locator('.yt-aside').isVisible(), width >= 2560, 'Sidebar breakpoint');
                assert.equal(await page.evaluate(() => document.documentElement.dataset.theme), theme);
                if (width >= 1024) {
                    const columns = await page
                        .locator('.yt-top')
                        .evaluate((n) => getComputedStyle(n).gridTemplateColumns.split(' ').length);
                    assert.equal(columns, width >= 2560 ? 3 : 2, 'Featured, Latest and optional sidebar');
                    if (width >= 2560) {
                        await waitList(expected);
                        assert(
                            await page
                                .locator('.yt-aside a')
                                .evaluateAll((ns) => ns.every((n) => n.getBoundingClientRect().height >= 44)),
                            '44px sidebar targets',
                        );
                    }
                }
                if (width <= 430) {
                    const save = page.locator('.mx-phone [data-save][aria-pressed="false"]:visible').first();
                    if (width === 320) {
                        const url = await save.getAttribute('data-url');
                        const selector = await page.evaluate(
                            (u) => '[data-save][data-url="' + CSS.escape(u) + '"]',
                            url,
                        );
                        const same = page
                            .locator('.mx-phone ' + selector)
                            .filter({ visible: true })
                            .first();
                        expected = await toggle(same);
                        expected = await toggle(same);
                    }
                    const menu = page.locator('[data-mobile-nav] summary');
                    await menu.click();
                    assert(await page.locator('.mobile-nav__links').isVisible());
                    await menu.click();
                }
                await page.evaluate(() => scrollTo(0, 0));
                await page.screenshot({ path: shots + 'videos-' + width + '-' + theme + '.png' });
                console.log('PASS ' + width + ' ' + theme);
            }
        }
        for (const scheme of ['monochrome', 'cardinals']) {
            await page.setViewportSize({ width: 2560, height: 1440 });
            await preference('mynewsfeed-color-scheme', scheme);
            await visit();
            await noOverflow();
            assert.equal(await page.evaluate(() => document.documentElement.dataset.colorScheme), scheme);
            await page.screenshot({ path: shots + 'videos-2560-' + scheme + '.png' });
        }
        const noJs = await browser.newContext({
            javaScriptEnabled: false,
            storageState: await context.storageState(),
            viewport: { width: 2560, height: 1440 },
        });
        const fallback = await noJs.newPage();
        await fallback.goto(base + '/videos', { waitUntil: 'domcontentloaded' });
        await fallback.waitForLoadState('networkidle');
        await fallback.screenshot({ path: shots + 'videos-nojs-2560.png' });
        assert.equal(
            await fallback
                .locator('.yt-top')
                .evaluate((n) => getComputedStyle(n).gridTemplateColumns.split(' ').length),
            3,
        );
        assert(await fallback.locator('.yt-aside').isVisible());
        assert.deepEqual(
            await fallback.locator('[data-yt-watch-list] a').evaluateAll((ns) => ns.map((a) => a.href)),
            expected,
            'Server-rendered Saved matches profile',
        );
        assert((await fallback.locator('[data-yt-watch-list]').innerText()).includes(dangerousTitle));
        await fallback.setViewportSize({ width: 390, height: 900 });
        await fallback.locator('.yt-aside').waitFor({ state: 'hidden' });
        assert(!(await fallback.locator('.yt-aside').isVisible()));
        const select = fallback.locator('#video-channel-m');
        await select.selectOption({ index: 1 });
        await select.locator('..').getByRole('button', { name: 'Go', exact: true }).click();
        await fallback.waitForURL(/category=|channel=/);
        assert(await fallback.evaluate(() => document.documentElement.scrollWidth <= innerWidth + 1));
        await noJs.close();
        await context.close();
        assert.deepEqual(errors, []);
        console.log('PASS category links, themes, phone menus/saves, no-JS Saved and filter fallback');
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
