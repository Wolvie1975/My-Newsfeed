// Wide-screen grids, zoom geometry and phone/tablet regressions against representative content.
const { chromium } = require('playwright');
const lib = require('./lib.cjs');
const assert = require('node:assert/strict');
const fs = require('node:fs');
const base = lib.base;
const shots = lib.screenshots('wide-screens');
(async () => {
    fs.mkdirSync(shots, { recursive: true });
    const browser = await chromium.launch();
    const profiles = (await fetch(base + '/editions')).status !== 404;
    const password = lib.adminPassword;
    let admin,
        storageState = process.env.STORAGE_STATE;
    try {
        if (profiles && !storageState) {
            assert(password, 'ADMIN_PASSWORD is required to create and remove a disposable reader profile');
            admin = await lib.signInAdmin(browser);
            const profile = await lib.addProfile(admin, 'Phone', { pin: '3691' });
            storageState = await lib.openProfile(browser, profile, {
                path: '/news/all',
                waitUntil: 'domcontentloaded',
            });
        }

        const context = await browser.newContext({ storageState });
        const page = await context.newPage();
        for (const width of lib.widths([320, 390, 430, 768, 1024, 1280, 1440, 1920, 2560, 3200, 3840])) {
            for (const theme of lib.THEMES) {
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
                            assert.equal(
                                await page.locator('.bs-band').isVisible(),
                                false,
                                'Home band hidden on wide screens',
                            );
                            const sportsNews = await page
                                .locator('[data-home-section="sports-news"]')
                                .first()
                                .boundingBox();
                            const entertainmentNews = await page
                                .locator('[data-home-section="entertainment-news"]')
                                .first()
                                .boundingBox();
                            const techNews = await page
                                .locator('[data-home-section="tech-news"]')
                                .first()
                                .boundingBox();
                            if (sportsNews && entertainmentNews && techNews) {
                                assert(
                                    Math.abs(sportsNews.y - entertainmentNews.y) < 2,
                                    'Desk headlines share top offset',
                                );
                                assert(
                                    Math.abs(entertainmentNews.y - techNews.y) < 2,
                                    'Desk headlines share top offset',
                                );
                            }
                            const scoreboard = await page
                                .locator('[data-home-section="scoreboard"]')
                                .first()
                                .boundingBox();
                            const rankings = await page.locator('[data-home-section="rankings"]').first().boundingBox();
                            if (scoreboard && rankings) {
                                assert(
                                    Math.abs(scoreboard.y - rankings.y) < 2,
                                    'Scoreboard and Top 25 share top offset',
                                );
                            }
                        }
                        if (route === '/entertainment') {
                            const efCols = await page
                                .locator('.ef-cols')
                                .evaluate((el) => getComputedStyle(el).gridTemplateColumns.split(' ').length);
                            assert.equal(efCols, 3, 'Entertainment front has 3 columns at 1920+');
                            const desksVisible = await page.locator('.ef-cols > .ef-desks').isVisible();
                            assert.equal(desksVisible, true, 'Entertainment desks visible at 1920+');
                            const latestHeadlinesVisible = await page.locator('.ef-main .ef-headlines').isVisible();
                            assert.equal(
                                latestHeadlinesVisible,
                                false,
                                'Entertainment latest headlines hidden at 1920+',
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
            await lib.deleteProfiles();
        } finally {
            await browser.close();
        }
    }
})().catch((e) => {
    console.error(e);
    process.exitCode = 1;
});
