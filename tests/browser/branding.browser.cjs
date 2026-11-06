const { chromium } = require('playwright');
const lib = require('./lib.cjs');
const assert = require('node:assert/strict');
const base = lib.base,
    root = lib.shotDir;
(async () => {
    const browser = await chromium.launch();
    try {
        const page = await lib.readerPage(browser);
        const errors = [];
        page.on('pageerror', (e) => errors.push(e.message));
        // Preferences saved under the renamed keys still apply: seeded on the open profile with profiles on, in this
        // browser's localStorage (before the first page loads) otherwise.
        const seeded = {
            'mynewsfeed-settings-order-v1': JSON.stringify(['services', 'teams', 'appearance', 'colors']),
            'mynewsfeed-watch-services-v1': '[8]',
            'mydailyfeed-scoreboard-teams-v1': '["soccer|eng.1|359"]',
        };
        await page.goto(base + '/');
        if (await page.evaluate(() => !!window.myNewspaperPrefs?.profile)) {
            for (const [key, value] of Object.entries(seeded)) await lib.setPreference(page, key, value);
        } else {
            await page.addInitScript((values) => {
                if (!localStorage.getItem('rename-test-seeded')) {
                    for (const [key, value] of Object.entries(values)) localStorage.setItem(key, value);
                    localStorage.setItem('rename-test-seeded', '1');
                }
            }, seeded);
        }
        for (const path of [
            '/',
            '/settings',
            '/watch?q=Arrival',
            '/movies',
            '/tv',
            '/sports/news/big12',
            '/entertainment/news/tv-film',
        ]) {
            await page.goto(base + path);
            assert.equal(await page.locator('.brand').getAttribute('aria-label'), 'myNewspaper home');
            assert((await page.title()).includes('myNewspaper'));
            assert(!/MyDailyFeed|MyNewsFeed/.test(await page.locator('body').innerText()));
            // The folded-page mark is inline SVG so it can take the colour scheme's ink.
            for (const part of ['page', 'fold', 'n'])
                assert.equal(
                    await page.locator(`.brand svg.brand-mark .brand-mark__${part}`).count(),
                    1,
                    'mark ' + part,
                );
            assert.equal(
                await page.evaluate(() =>
                    document.querySelector('link[rel="icon"][type="image/svg+xml"]').getAttribute('href'),
                ),
                'img/brand/mark.svg',
            );
        }
        await page.goto(base + '/settings');
        assert.equal(
            await page
                .locator('[data-settings-sections]>[data-settings-section]')
                .first()
                .getAttribute('data-settings-section'),
            'services',
        );
        assert.equal(await lib.getPreference(page, 'mynewsfeed-watch-services-v1'), '[8]');
        assert.equal(await lib.getPreference(page, 'mydailyfeed-scoreboard-teams-v1'), '["soccer|eng.1|359"]');
        const manifest = await (await page.request.get(base + '/manifest.webmanifest')).json();
        assert.equal(manifest.name, 'myNewspaper');
        assert.equal(manifest.short_name, 'myNewspaper');
        assert.equal(manifest.id, '/');
        assert.equal(manifest.scope, '/');
        for (const icon of manifest.icons) {
            assert(icon.src.includes('newspaper-front-'));
            assert.equal((await page.request.get(base + icon.src)).status(), 200);
        }
        const styles = await page.locator('link[rel="stylesheet"]').evaluateAll((ns) => ns.map((n) => n.href));
        assert(styles.some((h) => h.includes('myNewspaper.Web.')));
        for (const href of styles) assert.equal((await page.request.get(href)).status(), 200, href);
        for (const width of [320, 390, 430, 768, 1440])
            for (const theme of lib.THEMES) {
                await page.setViewportSize({ width, height: 900 });
                await page.emulateMedia({ colorScheme: theme });
                for (const path of ['/', '/sports/news', '/entertainment/news', '/settings']) {
                    await page.goto(base + path);
                    assert.equal(await page.locator('html').getAttribute('data-theme'), theme);
                    assert(await page.evaluate(() => document.documentElement.scrollWidth <= innerWidth));
                    assert(!(await page.locator('#blazor-error-ui').isVisible()));
                    // Desktop Home leaves the mark to its own masthead; everywhere else the header brand is a 44px target.
                    const brand = page.locator(width >= 1024 && path === '/' ? '.bs-title__name' : '.brand');
                    assert((await brand.boundingBox()).height >= 44);
                    assert((await brand.boundingBox()).width >= 44);
                    if (width <= 430 && path === '/sports/news') {
                        await page.locator('.mobile-nav > summary').click();
                        const link = page.locator('[aria-labelledby="mobile-entertainment-heading"] a').first();
                        await link.scrollIntoViewIfNeeded();
                        const target = new URL(await link.getAttribute('href'), base + '/').href;
                        await link.click();
                        await page.waitForURL(target);
                    }
                    if (root && width <= 430 && path === '/')
                        await page.screenshot({ path: root + '/home-' + width + '-' + theme + '.png' });
                }
                await page.goto(base + '/offline.html');
                assert.equal(await page.locator('h1').innerText(), 'myNewspaper can’t be reached');
                assert(
                    await page
                        .locator('img')
                        .evaluate((n) => n.complete && n.naturalWidth > 0 && n.getBoundingClientRect().width === 80),
                );
                assert(await page.evaluate(() => document.documentElement.scrollWidth <= innerWidth));
            }
        const plain = await lib.readerPage(browser, {
            javaScriptEnabled: false,
            viewport: { width: 390, height: 900 },
        });
        await plain.goto(base + '/sports/news');
        await plain.locator('.brand').click();
        assert.equal(new URL(plain.url()).pathname, '/');
        await plain.close();
        assert.deepEqual(errors, []);
        console.log(
            'myNewspaper branding, logo/icons/styles, legacy preferences, mobile 320/390/430 and tablet/desktop light/dark, navigation and offline screen passed.',
        );
    } finally {
        await browser.close();
    }
})().catch((e) => {
    console.error(e);
    process.exit(1);
});
