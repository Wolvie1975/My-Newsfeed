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
        const fixture = async (p) => {
            await p.locator('.d-episodes').evaluate((section) => {
                section.querySelector('.d-empty')?.remove();
                let list = section.querySelector('.d-tv__list');
                if (!list) {
                    list = document.createElement('ul');
                    list.className = 'd-list d-tv__list';
                    section.querySelector('.d-tv__credit').before(list);
                }
                const shows = [
                    [
                        'Avatar: Seven Havens',
                        ['Ghosts and Spirits', 'Battle of the Ruins', 'The Dome', 'The Root Chakra'],
                    ],
                    ['A second series with a longer name', ['A separate episode']],
                ];
                list.innerHTML = shows
                    .map(
                        ([title, episodes]) =>
                            `<li class="d-tv__show"><div class="tv-poster" aria-hidden="true"></div><div class="d-tv__body"><p class="movie-kind">Paramount+</p><h3 class="d-vid__title">${title}</h3><ul class="d-tv__episodes">${episodes.map((title, i) => `<li><a href="tv">S01 E${10 + i} · ${title}</a><span class="movie-meta">Time not confirmed</span></li>`).join('')}</ul></div></li>`,
                    )
                    .join('');
                section
                    .querySelectorAll('.bs-episodes-count, .d-card__count')
                    .forEach((n) => (n.textContent = '5 episodes'));
            });
        };
        const layout = async (p) => {
            const metrics = await p.locator('.d-tv__list').evaluate((list) => {
                const shows = [...list.children];
                const row = shows[0],
                    episodes = row.querySelector('.d-tv__episodes');
                const box = (n) => n.getBoundingClientRect();
                return {
                    cols: getComputedStyle(list).gridTemplateColumns.split(' ').length,
                    width: box(list).width,
                    rowWidth: box(row).width,
                    full: box(episodes).width / box(row).width,
                    below: box(episodes).top >= box(row.querySelector('.tv-poster')).bottom,
                    stacked: box(shows[1]).top >= box(row).bottom,
                    status: [...row.querySelectorAll('.movie-meta')].every(
                        (n) =>
                            box(n).height <=
                            parseFloat(getComputedStyle(n).lineHeight) *
                                parseFloat(getComputedStyle(n.closest('.pub')).zoom) +
                                1,
                    ),
                    links: [...row.querySelectorAll('.d-tv__episodes a')].every((n) => box(n).height >= 44),
                };
            });
            assert.equal(metrics.cols, 1, 'One show per row in sidebar');
            assert(metrics.rowWidth >= metrics.width - 1);
            assert(metrics.full > 0.95, 'Episode details use full card width');
            assert(metrics.below, 'Episodes below poster');
            assert(metrics.stacked, 'Second series stacks below first');
            assert(metrics.status, 'Time not confirmed stays on one line');
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
        console.log('PASS four-episode batch, multiple shows, full-width details, calendar, Refresh and no-JS');
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
