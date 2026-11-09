// Phones (below 744px) get the mobile version with the Menu disclosure; iPad portrait (744–1023px) keeps the mobile
// content but gets the two-row header with the sections as tabs, and pages that opt in (tab-desktop / tab-phone) show
// their desktop markup (docs/ipad/ipad-00-foundation.md); desktop starts at 1024px.
const { chromium } = require('playwright');
const lib = require('./lib.cjs');
const assert = require('node:assert/strict');
const fs = require('node:fs');
const base = lib.base;
const pages = (
    process.env.PAGES ||
    '/,/news,/news/kansas-city,/sports,/scoreboard,/standings,/rankings,/college,/f1,/entertainment,/movies,/tv,/box-office,/watch,/videos,/saved,/settings'
).split(',');
const widths = [320, 390, 430, 744, 768, 820, 834, 1023, 1024, 1280, 1440];

(async () => {
    const browser = await chromium.launch();
    try {
        for (const theme of lib.THEMES) {
            // The theme is the reader profile's with profiles on, this browser's localStorage otherwise.
            const context = await lib.readerContext(browser);
            // Keep remote image hosts from stalling layout checks; local fonts and scripts still load.
            await context.route('**/*', (route) =>
                route.request().resourceType() === 'image' && !route.request().url().startsWith(base)
                    ? route.abort()
                    : route.continue(),
            );
            const page = await context.newPage();
            const errors = [];
            page.on('pageerror', (e) => errors.push(e.message));
            // A theme in localStorage would start the one-time browser import (and its reload) with a profile open.
            await page.goto(base + '/');
            if (await page.evaluate(() => !!window.myNewspaperPrefs?.profile))
                await lib.setPreference(page, 'theme', theme);
            else await context.addInitScript((value) => localStorage.setItem('theme', value), theme);
            for (const path of pages) {
                const response = await page.goto(base + path, { waitUntil: 'networkidle' });
                assert.equal(response.status(), 200, path);
                assert.notEqual(new URL(page.url()).pathname, '/editions', path + ' showed the edition picker');
                for (const width of widths) {
                    await page.setViewportSize({ width, height: width < 1024 ? 1180 : 900 });
                    const label = `${theme} ${width}px ${path}`;
                    assert.equal(await page.getAttribute('html', 'data-theme'), theme, label);
                    assert(
                        await page.evaluate(() => document.documentElement.scrollWidth <= innerWidth),
                        label + ' overflow',
                    );
                    const tablet = width >= 744 && width < 1024;
                    assert.equal(await page.locator('.mobile-nav').isVisible(), width < 744, label + ' menu');
                    if (tablet) {
                        const tabs = page.locator('.site-nav--inline a');
                        assert(await page.locator('.site-nav--inline').isVisible(), label + ' section tabs');
                        assert((await tabs.first().boundingBox()).height >= 44, label + ' tab touch target');
                    }
                    // Opted-in wrappers on a tablet: tab-phone halves hide, tab-desktop halves show.
                    const optedIn = (n) => !!n.closest('.tab-desktop, .tab-phone');
                    for (const wrapper of await page.locator('.mx-phone').all()) {
                        if (tablet && (await wrapper.evaluate(optedIn))) continue;
                        assert.equal(await wrapper.isVisible(), width < 1024, label + ' mobile content');
                    }
                    for (const wrapper of await page.locator('.mx-phone.tab-phone, .tab-desktop .mx-phone').all()) {
                        if (tablet) assert(!(await wrapper.isVisible()), label + ' tablet hides opted-in phone half');
                    }
                    for (const wrapper of await page.locator('.mx-desktop').all()) {
                        if (width < 744 || (tablet && !(await wrapper.evaluate(optedIn))))
                            assert(!(await wrapper.isVisible()), label + ' desktop hidden');
                    }
                    if (width < 744) {
                        const menu = page.getByLabel('Menu', { exact: true });
                        assert((await menu.boundingBox()).height >= 44, label + ' menu touch target');
                        await menu.click();
                        assert(await page.locator('.mobile-nav__links').isVisible(), label + ' open menu');
                        await menu.click();
                        const fold = page.locator('.mx-phone details > summary').first();
                        if (await fold.isVisible()) {
                            await fold.click();
                            await fold.click();
                        }
                    }
                    if (lib.shotDir && [390, 768, 820, 834, 1024, 1440].includes(width)) {
                        fs.mkdirSync(lib.shotDir, { recursive: true });
                        await page.evaluate(() => scrollTo(0, 0));
                        await page.screenshot({
                            path: `${lib.shotDir}/${path.replaceAll('/', '_') || 'home'}-${width}-${theme}.png`,
                            fullPage: true,
                        });
                    }
                }
                console.log(theme, path, 'layout and controls passed');
                assert.deepEqual(errors.splice(0), [], path + ' script errors');
            }
            await page.setViewportSize({ width: 820, height: 1180 });
            await page.goto(base + '/', { waitUntil: 'networkidle' });
            await page.reload({ waitUntil: 'networkidle' });
            assert.equal(await page.getAttribute('html', 'data-theme'), theme, 'theme survives refresh');
            await context.close();
        }
        // Without JavaScript: the phone Menu and the tablet section tabs both still navigate.
        const context = await lib.readerContext(browser, { javaScriptEnabled: false });
        const page = await context.newPage();
        await context.route('**/*', (route) =>
            route.request().resourceType() === 'image' ? route.abort() : route.continue(),
        );
        await page.setViewportSize({ width: 390, height: 900 });
        await page.goto(base + '/', { waitUntil: 'domcontentloaded' });
        assert(await page.locator('.mx-phone').first().isVisible(), 'no-JS phone content');
        await page.getByLabel('Menu', { exact: true }).click();
        assert(await page.locator('.mobile-nav__links').isVisible(), 'no-JS menu opens');
        await page.locator('.mobile-nav__links a[href="news"]').first().click();
        await page.waitForURL('**/news');
        await page.setViewportSize({ width: 820, height: 1180 });
        await page.goto(base + '/', { waitUntil: 'domcontentloaded' });
        await page.locator('.site-nav--inline a[href="sports"]').click();
        await page.waitForURL('**/sports');
        await context.close();
    } finally {
        await browser.close();
    }
    console.log(
        `Mobile/tablet: ${pages.length} pages, ${widths.join('/')}px light/dark; menu, folds, refresh and no-JS navigation passed.`,
    );
})().catch((e) => {
    console.error(e);
    process.exit(1);
});
