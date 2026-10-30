// Phone and iPad portrait widths share the mobile version; desktop starts at 1024px.
const { chromium } = require('playwright');
const assert = require('node:assert/strict');
const fs = require('node:fs');
const base = process.env.BASE_URL || 'http://localhost:5295';
const pages = (
    process.env.PAGES ||
    '/,/news,/news/kansas-city,/sports,/scoreboard,/standings,/rankings,/college,/f1,/entertainment,/movies,/tv,/box-office,/watch,/videos,/saved,/settings'
).split(',');
const widths = [320, 390, 430, 744, 768, 820, 834, 1023, 1024, 1280, 1440];

(async () => {
    const browser = await chromium.launch();
    try {
        for (const theme of ['light', 'dark']) {
            const context = await browser.newContext();
            await context.addInitScript((value) => localStorage.setItem('theme', value), theme);
            // Keep remote image hosts from stalling layout checks; local fonts and scripts still load.
            await context.route('**/*', (route) =>
                route.request().resourceType() === 'image' && !route.request().url().startsWith(base)
                    ? route.abort()
                    : route.continue(),
            );
            const page = await context.newPage();
            const errors = [];
            page.on('pageerror', (e) => errors.push(e.message));
            for (const path of pages) {
                const response = await page.goto(base + path, { waitUntil: 'networkidle' });
                assert.equal(response.status(), 200, path);
                assert.notEqual(new URL(page.url()).pathname, '/editions', 'Use a preview with profiles disabled.');
                for (const width of widths) {
                    await page.setViewportSize({ width, height: width < 1024 ? 1180 : 900 });
                    const label = `${theme} ${width}px ${path}`;
                    assert.equal(await page.getAttribute('html', 'data-theme'), theme, label);
                    assert(
                        await page.evaluate(() => document.documentElement.scrollWidth <= innerWidth),
                        label + ' overflow',
                    );
                    assert.equal(await page.locator('.mobile-nav').isVisible(), width < 1024, label + ' menu');
                    for (const wrapper of await page.locator('.mx-phone').all()) {
                        assert.equal(await wrapper.isVisible(), width < 1024, label + ' mobile content');
                    }
                    for (const wrapper of await page.locator('.mx-desktop').all()) {
                        if (width < 1024) assert(!(await wrapper.isVisible()), label + ' desktop hidden');
                    }
                    if (width < 1024) {
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
                    if (process.env.SCREENSHOT_DIR && [390, 768, 820, 834, 1024, 1440].includes(width)) {
                        fs.mkdirSync(process.env.SCREENSHOT_DIR, { recursive: true });
                        await page.evaluate(() => scrollTo(0, 0));
                        await page.screenshot({
                            path: `${process.env.SCREENSHOT_DIR}/${path.replaceAll('/', '_') || 'home'}-${width}-${theme}.png`,
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
        const context = await browser.newContext({ javaScriptEnabled: false, viewport: { width: 820, height: 1180 } });
        const page = await context.newPage();
        await context.route('**/*', (route) =>
            route.request().resourceType() === 'image' ? route.abort() : route.continue(),
        );
        await page.goto(base + '/', { waitUntil: 'domcontentloaded' });
        assert(await page.locator('.mx-phone').first().isVisible());
        await page.getByLabel('Menu', { exact: true }).click();
        assert(await page.locator('.mobile-nav__links').isVisible());
        await page.locator('.mobile-nav__links a[href="news"]').first().click();
        await page.waitForURL('**/news');
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
