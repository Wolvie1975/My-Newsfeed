// iPad Entertainment layout, using a disposable reader profile.
const { chromium } = require('playwright');
const assert = require('node:assert/strict');
const fs = require('node:fs');
const base = process.env.BASE_URL || 'http://localhost:5295';
const adminBase = process.env.ADMIN_BASE_URL || base;
const password = process.env.ADMIN_PASSWORD;
const shots = process.env.SCREENSHOT_DIR || 'test-results/browser/ipad-entertainment/';
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
            const name = 'zz iPad Entertainment ' + Date.now().toString(36);
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
        const visitFront = async () => {
            await page.goto(base + '/entertainment', { waitUntil: 'networkidle' });
            await page.locator('.ef').waitFor({ state: 'attached' });
        };
        const noOverflow = async () =>
            assert(
                await page.evaluate(() => document.documentElement.scrollWidth <= innerWidth + 1),
                'No horizontal overflow',
            );
        const fixture = async () => {
            await page.evaluate(() => {
                const list = document.querySelector('.ef-screen__list');
                const row = document.createElement('li');
                row.dataset.batchFixture = '';
                row.innerHTML =
                    '<a class="ef-screen__item" href="tv"><div class="ef-poster"><span class="sr-only">Poster unavailable for Avatar: Seven Havens</span></div><span class="ef-screen__text"><span class="ef-when ef-when--tonight">Tonight</span><b class="ef-screen__title">Avatar: Seven Havens</b><span class="ef-where">Paramount+ · 4 episodes</span></span></a>';
                list.prepend(row);
                while (list.children.length < 5) list.append(list.lastElementChild.cloneNode(true));
                while (list.children.length > 5) list.lastElementChild.remove();
                const tv = document.querySelector('.ef-tv');
                tv.querySelector('.ef-empty')?.remove();
                const item = document.createElement('a');
                item.href = 'tv';
                item.className = 'ef-tv__item';
                item.dataset.batchFixture = '';
                item.innerHTML =
                    '<div class="ef-poster"></div><span><b>Avatar: Seven Havens</b><span class="ef-where">Paramount+ · 4 new episodes · S01 E10–E13</span></span>';
                tv.querySelector('h2').after(item);
            });
            await page.locator('.ef-screen__list').evaluate((n) => n.scrollTo({ left: 0, behavior: 'instant' }));
        };
        const layout = async () => {
            const g = await page.evaluate(() => {
                const b = (s) => {
                    const r = document.querySelector(s).getBoundingClientRect();
                    return { x: r.x, y: r.y, w: r.width, h: r.height, right: r.right, bottom: r.bottom };
                };
                return {
                    frame: b('.ef-cols'),
                    lead: b('.ef-lead'),
                    headlines: b('.ef-headlines'),
                    aside: b('.ef-side'),
                    poster: b('.ef-screen__item .ef-poster'),
                    title: getComputedStyle(document.querySelector('.ef-lead__title')).fontSize,
                    headingsColumns: getComputedStyle(
                        document.querySelector('.ef-headlines'),
                    ).gridTemplateColumns.split(' ').length,
                    paper: getComputedStyle(document.querySelector('.pub')).backgroundColor,
                };
            });
            assert(Math.abs(g.lead.w - g.frame.w) < 2, 'Full width lead');
            assert(g.headlines.y >= g.lead.bottom, 'Headlines below lead');
            assert(g.headlines.right <= g.aside.x + 1, 'Headlines and side column do not overlap');
            assert(g.aside.y >= g.lead.bottom - 1, 'Side column below lead');
            assert.equal(g.title, '36px');
            assert.equal(g.headingsColumns, 1, 'Single headline list in left column');
            assert(Math.abs(g.poster.w / g.poster.h - 2 / 3) < 0.01, 'Tall posters');
            assert(await page.locator('.ef-tv').isVisible(), 'Tonight on TV visible');
            const image = page.locator('.ef-lead__image');
            if (await image.isVisible()) {
                const r = await image.boundingBox();
                assert(Math.abs(r.width / r.height - 16 / 9) < 0.02, '16:9 lead photo');
                assert(Math.abs(r.width - g.lead.w) < 2, 'Full width lead photo');
            }
            assert(await page.locator('.page-heading .section-tabs').isVisible());
            assert(!(await page.locator('.page-heading .section-select').isVisible()));
            assert.equal(
                await page.locator('.page-heading .section-tabs a[href="watch"]').count(),
                0,
                'Standalone Watch stays out of section tabs',
            );
            await noOverflow();
        };
        for (const theme of ['light', 'dark']) {
            await preference('theme', theme);
            for (const width of (process.env.WIDTHS || '320,390,430,743,744,820,834,1023,1024,1280,1440,1920,2560')
                .split(',')
                .map(Number)) {
                await page.setViewportSize({ width, height: 1100 });
                await visitFront();
                await noOverflow();
                assert.equal(
                    await page.locator('.ef').isVisible(),
                    width >= 744,
                    'Correct Entertainment desktop range',
                );
                assert.equal(
                    await page.locator('.mx-phone .mx-ribbon h1').isVisible(),
                    width < 744,
                    'Phone ribbon range',
                );
                assert.equal(await page.locator('html').getAttribute('data-theme'), theme);
                if (width >= 744) await fixture();
                if (width >= 744 && width < 1024) await layout();
                if (width < 744) {
                    const fold = page.locator('.mx-phone details').first();
                    const open = (await fold.getAttribute('open')) !== null;
                    await fold.locator('summary').tap();
                    assert.equal((await fold.getAttribute('open')) !== null, !open);
                    assert((await fold.locator('summary').boundingBox()).height >= 44);
                }
                await page.reload({ waitUntil: 'networkidle' });
                await noOverflow();
                if (width >= 744) await fixture();
                if (width >= 744 && width < 1024) await layout();
                await page.evaluate(() => window.scrollTo(0, 0));
                await page.screenshot({ path: shots + `entertainment-${width}-${theme}.png`, fullPage: true });
                if (width >= 744 && width < 1024)
                    await page.screenshot({ path: shots + `entertainment-top-${width}-${theme}.png` });
                console.log('PASS', width, theme);
            }
        }
        await page.setViewportSize({ width: 820, height: 1100 });
        await visitFront();
        await fixture();
        const rail = page.locator('.ef-screen__list'),
            r = await rail.boundingBox(),
            cdp = await context.newCDPSession(page);
        const x = Math.round(r.x + r.width - 50),
            y = Math.round(r.y + 100);
        await cdp.send('Input.dispatchTouchEvent', { type: 'touchStart', touchPoints: [{ x, y }] });
        for (let i = 1; i <= 10; i++) {
            await cdp.send('Input.dispatchTouchEvent', { type: 'touchMove', touchPoints: [{ x: x - i * 30, y }] });
            await page.waitForTimeout(25);
        }
        await cdp.send('Input.dispatchTouchEvent', { type: 'touchEnd', touchPoints: [] });
        await page.waitForFunction(() => document.querySelector('.ef-screen__list').scrollLeft > 50);
        await cdp.detach();
        // A rightmost active tab fixture exercises the shared horizontal reveal without moving page scroll.
        await page.evaluate(() => {
            const links = [...document.querySelectorAll('.page-heading .section-tabs a')];
            links.forEach((n) => n.removeAttribute('aria-current'));
            links[0].textContent = 'Entertainment home with a deliberately long section label';
            links.at(-1).setAttribute('aria-current', 'page');
            document.querySelector('.page-heading .section-tabs').scrollLeft = 0;
            window.dispatchEvent(new Event('resize'));
        });
        await page.waitForFunction(() => {
            const n = document.querySelector('.page-heading .section-tabs'),
                a = n.querySelector('[aria-current]'),
                r = a.getBoundingClientRect(),
                b = n.getBoundingClientRect();
            return r.left >= b.left - 1 && r.right <= b.right + 1;
        });
        assert.equal(await page.evaluate(() => scrollY), 0, 'Tab reveal does not scroll the page');
        assert(
            await page.locator('.page-heading .section-tabs').evaluate((n) => n.scrollLeft > 0),
            'Off-screen current tab is revealed',
        );
        await visitFront();
        await page.locator('.page-heading .section-tabs a[href="movies"]').tap();
        await page.waitForURL('**/movies');
        await page.waitForLoadState('networkidle');
        await page.locator('#movie-view-m').waitFor({ state: 'visible' });
        assert(await page.locator('.mx-phone .mx-ribbon').isVisible(), 'Unconverted subpage keeps its layout');
        for (const route of ['tv', 'movies/digital', 'box-office', 'entertainment/news']) {
            await page.goto(base + '/' + route, { waitUntil: 'networkidle' });
            await noOverflow();
        }
        await visitFront();
        await fixture();
        const share = page.locator('.ef-lead [data-share-open]');
        const url = await share.getAttribute('data-url');
        await share.tap();
        const dialog = page.getByRole('dialog');
        await dialog.waitFor();
        await dialog.getByRole('button', { name: 'Copy link', exact: true }).tap();
        await dialog.waitFor({ state: 'detached' });
        assert.equal(await page.evaluate(() => window.copied), url);
        const save = page.locator('.ef-lead [data-save]');
        const saved = page.waitForResponse(
            (r) => new URL(r.url()).pathname.startsWith('/api/saved/') && r.request().method() === 'POST',
        );
        await save.tap();
        assert.equal((await saved).status(), 200);
        await page.reload({ waitUntil: 'networkidle' });
        assert.equal(await page.locator('.ef-lead [data-save]').getAttribute('aria-pressed'), 'true');
        const remove = page.waitForResponse(
            (r) => new URL(r.url()).pathname.startsWith('/api/saved/') && r.request().method() === 'POST',
        );
        await page.locator('.ef-lead [data-save]').tap();
        assert.equal((await remove).status(), 200);
        // Missing-photo classification remains hidden; a P8 placeholder still fills the hero frame.
        await page.locator('.ef-lead').evaluate((n) => {
            n.dataset.imageLayout = 'none';
            n.querySelector('.ef-lead__image')?.replaceChildren();
        });
        assert(!(await page.locator('.ef-lead__image').isVisible()));
        await page.locator('.ef-lead__image').evaluate((n) => {
            const p = document.createElement('div');
            p.className = 'ph';
            p.textContent = 'Entertainment';
            n.append(p);
        });
        const ph = await page.locator('.ef-lead__image').boundingBox();
        assert(Math.abs(ph.width / ph.height - 16 / 9) < 0.02);
        for (const scheme of ['monochrome', 'cardinals']) {
            await preference('mynewsfeed-color-scheme', scheme);
            await visitFront();
            await fixture();
            await layout();
            await page.screenshot({ path: shots + `entertainment-820-${scheme}.png` });
        }
        for (const route of ['/sports', '/news', '/']) {
            await page.goto(base + route, { waitUntil: 'networkidle' });
            assert((await page.locator('.mx-phone:visible').count()) > 0);
            assert.equal(await page.locator('.tab-desktop').count(), 0);
            await noOverflow();
        }
        await page.goto(base + '/watch?services=8,9,337', { waitUntil: 'networkidle' });
        assert(await page.locator('#watch-query').isVisible());
        await noOverflow();
        await page.goto(base + '/videos', { waitUntil: 'networkidle' });
        assert(await page.locator('.yt-feature').isVisible());
        await noOverflow();
        const plain = await browser.newContext({
                storageState,
                javaScriptEnabled: false,
                viewport: { width: 820, height: 1100 },
            }),
            fallback = await plain.newPage();
        await fallback.goto(base + '/entertainment', { waitUntil: 'networkidle' });
        assert(await fallback.locator('.ef').isVisible());
        assert(await fallback.locator('.page-heading .section-tabs').isVisible());
        await fallback.locator('.page-heading .section-tabs a[href="box-office"]').click();
        await fallback.waitForURL('**/box-office');
        await fallback.setViewportSize({ width: 390, height: 900 });
        await fallback.goto(base + '/entertainment', { waitUntil: 'networkidle' });
        const fold = fallback.locator('.mx-phone details').first(),
            was = (await fold.getAttribute('open')) !== null;
        await fold.locator('summary').click();
        assert.equal((await fold.getAttribute('open')) !== null, !was);
        await plain.close();
        await context.close();
        console.log(
            'PASS touch posters, section navigation/reveal, Share/Save, photo fallbacks, schemes, isolation and no-JS',
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
