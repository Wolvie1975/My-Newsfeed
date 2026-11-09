// Home sidebar episode spacing, using a disposable reader profile.
const { chromium } = require('playwright');
const lib = require('./lib.cjs');
const assert = require('node:assert/strict');
const fs = require('node:fs');
const base = lib.base;
const password = lib.adminPassword;
const shots = lib.screenshots('episode-spacing');
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
            const profile = await lib.addProfile(admin, 'Episodes', { pin: '3691' });
            storageState = await lib.openProfile(browser, profile, { path: '/', waitUntil: 'domcontentloaded' });
        }

        const context = await browser.newContext({ storageState, viewport: { width: 2560, height: 1440 } });
        const page = await context.newPage();
        const visit = async () => {
            await page.goto(base + '/', { waitUntil: 'networkidle' });
            await page.locator('.d-episodes').waitFor({ state: 'attached' });
        };
        // Today's episodes as Entertainment's On screen timeline: one row per episode, the time once per slot.
        const fixture = async (p) => {
            await p.locator('.d-episodes').evaluate((section) => {
                section.querySelector('.d-empty')?.remove();
                let list = section.querySelector('.ef-onscreen');
                if (!list) {
                    list = document.createElement('ol');
                    list.className = 'ef-onscreen';
                    section.querySelector('.d-tv__credit').before(list);
                }
                const rows = [
                    ['8:00 PM', 'Avatar: Seven Havens', 'S01 E10 · Ghosts and Spirits'],
                    ['', 'Avatar: Seven Havens', 'S01 E11 · Battle of the Ruins'],
                    ['9:00 PM', 'A second series with a longer name than the column', 'S02 E01 · A separate episode'],
                    ['TBA', 'Avatar: Seven Havens', 'S01 E12 · The Dome'],
                    ['', 'Avatar: Seven Havens', 'S01 E13 · The Root Chakra'],
                ];
                list.innerHTML = rows
                    .map(
                        ([time, title, episode]) =>
                            `<li class="ef-onscreen__item mx-c-us"><span class="ef-onscreen__date"${time ? '' : ' aria-hidden="true"'}>${time}</span><span class="ef-onscreen__body"><a class="ef-screen__item" href="tv"><div class="ef-poster"></div><span class="ef-screen__text"><b class="ef-screen__title">${title}</b><span class="ef-onscreen__meta"><span class="ef-onscreen__kind">TV</span> · Paramount+ · ${episode}</span></span></a></span></li>`,
                    )
                    .join('');
                section
                    .querySelectorAll('.bs-episodes-count, .d-card__count')
                    .forEach((n) => (n.textContent = '5 episodes'));
            });
        };
        const layout = async (p) => {
            const metrics = await p.locator('.d-episodes .ef-onscreen').evaluate((list) => {
                const rows = [...list.children];
                const box = (n) => n.getBoundingClientRect();
                const first = rows[0];
                const poster = first.querySelector('.ef-poster'),
                    title = first.querySelector('.ef-screen__title'),
                    body = first.querySelector('.ef-onscreen__body');
                const zoom = parseFloat(getComputedStyle(body.closest('.pub') || body).zoom) || 1;
                return {
                    rows: rows.length,
                    inside: rows.every((r) => box(r).right <= box(list).right + 1 && box(r).left >= box(list).left - 1),
                    stacked: rows.every((r, i) => i === 0 || box(r).top >= box(rows[i - 1]).bottom - 1),
                    beside: box(title).left >= box(poster).right && box(title).top < box(poster).bottom,
                    rail:
                        Math.round(parseFloat(getComputedStyle(body).borderLeftWidth) * zoom) === 2 &&
                        getComputedStyle(body, '::before').width === '10px',
                    blank: rows.filter((r) => r.querySelector('.ef-onscreen__date').textContent.trim() === '').length,
                    links: rows.every((r) => box(r.querySelector('a')).height >= 44),
                };
            });
            assert.equal(metrics.rows, 5, 'One timeline row per episode');
            assert(metrics.inside, 'Rows stay inside the card');
            assert(metrics.stacked, 'Rows stack in time order');
            assert(metrics.beside, 'Title beside the poster');
            assert(metrics.rail, 'Rail and dot drawn as on Entertainment');
            assert.equal(metrics.blank, 2, 'Repeated times left blank');
            assert(metrics.links, '44px episode links');
        };
        await visit();
        for (const theme of lib.THEMES) {
            const saved = page.waitForResponse(
                (r) => new URL(r.url()).pathname === '/api/me/settings' && r.request().method() === 'PUT',
            );
            await page.evaluate((v) => window.myNewspaperPrefs.setItem('theme', v), theme);
            await saved;
            for (const width of lib.widths('320,390,430,768,1024,1280,1440,1920,2560,3200,3840')) {
                await page.setViewportSize({ width, height: 1080 });
                await visit();
                assert.equal(await page.locator('html').getAttribute('data-theme'), theme);
                if (width >= 1024) {
                    await fixture(page);
                    await layout(page);
                    await page.locator('.d-episodes').screenshot({ path: shots + `episodes-${width}-${theme}.png` });
                    await page.locator('.d-episodes .d-more').click();
                    await page.waitForURL('**/tv');
                    await visit();
                    await page.locator('.home-refresh').click();
                    await page.waitForLoadState('networkidle');
                    await fixture(page);
                    await layout(page);
                } else if (width >= 744) {
                    // 744–1023px is the tablet front (tablet-layout.browser.cjs checks it); phones are below 744.
                    assert(await page.locator('.tab-desktop').isVisible(), 'Tablet Home from 744px');
                } else {
                    assert(await page.locator('.mx-phone').isVisible());
                    const fold = page.locator('.mx-phone details').first();
                    await fold.locator('summary').click();
                    assert((await fold.locator('summary').boundingBox()).height >= 44);
                    await page.reload({ waitUntil: 'networkidle' });
                    assert(await page.locator('.mx-phone').isVisible());
                    await page.evaluate(() => window.scrollTo(0, 0));
                    await page.screenshot({ path: shots + `phone-${width}-${theme}.png`, fullPage: true });
                }
                assert(await page.evaluate(() => document.documentElement.scrollWidth <= innerWidth + 1));
                console.log('PASS', width, theme);
            }
        }
        const noJs = await browser.newContext({
            storageState,
            javaScriptEnabled: false,
            viewport: { width: 2560, height: 1440 },
        });
        const fallback = await noJs.newPage();
        await fallback.goto(base + '/', { waitUntil: 'networkidle' });
        await fixture(fallback);
        await layout(fallback);
        await fallback.locator('.d-episodes .d-more').click();
        await fallback.waitForURL('**/tv');
        await fallback.goto(base + '/', { waitUntil: 'networkidle' });
        await fallback.setViewportSize({ width: 390, height: 900 });
        const fold = fallback.locator('.mx-phone details').first();
        const before = (await fold.getAttribute('open')) !== null;
        await fold.locator('summary').click();
        assert.equal((await fold.getAttribute('open')) !== null, !before);
        await noJs.close();
        await context.close();
        console.log(
            'PASS episode timeline: one row per episode, repeated times blank, poster beside title, 44px links, calendar, Refresh and no-JS',
        );
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
