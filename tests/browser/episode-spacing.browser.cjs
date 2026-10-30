// Home sidebar episode spacing, using a disposable reader profile.
const { chromium } = require('playwright');
const assert = require('node:assert/strict');
const fs = require('node:fs');
const base = process.env.BASE_URL || 'http://localhost:5295';
const adminBase = process.env.ADMIN_BASE_URL || base;
const password = process.env.ADMIN_PASSWORD;
const shots = process.env.SCREENSHOT_DIR || 'test-results/browser/episode-spacing/';
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
            const name = 'zz Episodes ' + Date.now().toString(36);
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
        for (const theme of ['light', 'dark']) {
            const saved = page.waitForResponse(
                (r) => new URL(r.url()).pathname === '/api/me/settings' && r.request().method() === 'PUT',
            );
            await page.evaluate((v) => window.myNewspaperPrefs.setItem('theme', v), theme);
            await (await saved).finished();
            for (const width of (process.env.WIDTHS || '320,390,430,768,1024,1280,1440,1920,2560,3200,3840')
                .split(',')
                .map(Number)) {
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
