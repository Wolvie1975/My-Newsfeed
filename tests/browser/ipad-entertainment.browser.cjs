// iPad Entertainment layout, using a disposable reader profile.
const { chromium } = require('playwright');
const lib = require('./lib.cjs');
const assert = require('node:assert/strict');
const fs = require('node:fs');
const base = lib.base;
const password = lib.adminPassword;
const shots = lib.screenshots('ipad-entertainment');
(async () => {
    fs.mkdirSync(shots, { recursive: true });
    const profiles = (await fetch(base + '/editions')).status !== 404;
    assert(profiles, 'Use profiles-enabled preview; this test never writes to a household Saved list');
    const browser = await chromium.launch({ args: ['--disable-web-security'] });
    let admin, storageState;
    try {
        if (profiles) {
            assert(password, 'ADMIN_PASSWORD is required to create and remove a disposable reader profile');
            admin = await lib.signInAdmin(browser);
            const profile = await lib.addProfile(admin, 'iPad Entertainment', { pin: '3691' });
            storageState = await lib.openProfile(browser, profile, { path: '/', waitUntil: 'domcontentloaded' });
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
        };
        await context.addInitScript(() => {
            window.copied = null;
            Object.defineProperty(window, 'isSecureContext', { value: true });
            Object.defineProperty(navigator, 'clipboard', {
                value: { writeText: async (value) => (window.copied = value) },
            });
            document.execCommand = () => {
                window.copied = document.querySelector('dialog textarea')?.value;
                return true;
            };
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
                const list = document.querySelector('.ef-onscreen');
                if (list) {
                    const row = document.createElement('li');
                    row.className = 'ef-onscreen__item mx-c-film';
                    row.dataset.batchFixture = '';
                    row.innerHTML =
                        '<span class="ef-onscreen__date ef-onscreen__date--tonight">Tonight</span>' +
                        '<span class="ef-onscreen__body">' +
                        '<a class="ef-screen__item" href="tv"><div class="ef-poster"><span class="sr-only">Poster unavailable for Avatar: Seven Havens</span></div>' +
                        '<span class="ef-screen__text"><b class="ef-screen__title">Avatar: Seven Havens</b>' +
                        '<span class="ef-onscreen__meta"><span class="ef-onscreen__kind">TV</span> · Paramount+ · 4 episodes</span></span></a></span>';
                    list.prepend(row);
                }
            });
        };
        const layout = async () => {
            const g = await page.evaluate(() => {
                const b = (s) => {
                    const el = document.querySelector(s);
                    if (!el) return { x: 0, y: 0, w: 0, h: 0, right: 0, bottom: 0 };
                    const r = el.getBoundingClientRect();
                    return { x: r.x, y: r.y, w: r.width, h: r.height, right: r.right, bottom: r.bottom };
                };
                return {
                    frame: b('.ef-cols'),
                    onscreen: b('.ef-onscreen-col'),
                    lead: b('.ef-lead'),
                    headlines: b('.ef-headlines-col'),
                    aside: b('.ef-side'),
                    poster: b('.ef-screen__item .ef-poster'),
                    title: getComputedStyle(document.querySelector('.ef-lead__title')).fontSize,
                    headingsColumns: getComputedStyle(
                        document.querySelector('.ef-headlines'),
                    ).gridTemplateColumns.split(' ').length,
                    paper: getComputedStyle(document.querySelector('.pub')).backgroundColor,
                };
            });
            assert(g.lead.x > g.onscreen.x, 'Lead sits to the right of On screen');
            assert(g.headlines.y >= g.lead.bottom - 4, 'Headlines below lead');
            assert(g.aside.y >= g.headlines.bottom - 4, 'Side column below headlines');
            assert.equal(g.title, '36px');
            assert.equal(g.headingsColumns, 1, 'Single headline list in left column');
            assert(await page.locator('.ef-onscreen-col').isVisible(), 'On screen timeline visible');
            const image = page.locator('.ef-lead__image');
            if (await image.isVisible()) {
                const r = await image.boundingBox();
                assert(Math.abs(r.width / r.height - 16 / 9) < 0.02, '16:9 lead photo');
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
        for (const theme of lib.THEMES) {
            await preference('theme', theme);
            for (const width of lib.widths('320,390,430,743,744,820,834,1023,1024,1280,1440,1920,2560')) {
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
            let img = n.querySelector('.ef-lead__image');
            if (!img) {
                img = document.createElement('a');
                img.className = 'ef-lead__image';
                n.prepend(img);
            }
            img.replaceChildren();
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
        for (const route of ['/settings']) {
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
            await lib.deleteProfiles();
        } finally {
            await browser.close();
        }
    }
})().catch((e) => {
    console.error(e);
    process.exitCode = 1;
});
