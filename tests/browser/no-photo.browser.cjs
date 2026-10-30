// P8 fixtures use existing rendered stories; temporary live profiles are deleted in finally.
const { chromium } = require('playwright');
const assert = require('node:assert/strict');

const base = process.env.BASE_URL || 'http://localhost:5295';
const adminBase = process.env.ADMIN_BASE_URL || base;
const shots = process.env.SCREENSHOT_DIR;
const password = process.env.ADMIN_PASSWORD;

(async () => {
    const profiles = (await fetch(base + '/editions', { headers: { Accept: 'text/html' } })).status !== 404;
    if (profiles && !password) {
        console.log('SKIP: profiles are on here; set ADMIN_PASSWORD so the check can add a throwaway profile.');
        return;
    }
    const browser = await chromium.launch();
    const errors = [];
    let admin = null,
        id = null,
        storageState;
    const contextFor = async (width, { scheme = 'kansas', theme = 'light', js = true } = {}) => {
        const context = await browser.newContext({
            viewport: { width, height: 900 },
            javaScriptEnabled: js,
            storageState,
        });
        await context.addInitScript(
            ([s, t]) => {
                try {
                    localStorage.setItem('mynewsfeed-color-scheme', s);
                    localStorage.setItem('theme', t);
                } catch {}
            },
            [scheme, theme],
        );
        context.setDefaultNavigationTimeout(90000);
        return context;
    };
    // With profiles on, the scheme and theme are the open profile's settings, so set them there and reload.
    const visit = async (page, path, scheme, theme) => {
        await page.goto(base + path, { waitUntil: 'networkidle' });
        if (!profiles) return;
        const saved = page.waitForResponse(
            (response) =>
                new URL(response.url()).pathname === '/api/me/settings' && response.request().method() === 'PUT',
        );
        await page.evaluate(
            ([s, t]) => {
                window.myNewspaperPrefs.setItem('mynewsfeed-color-scheme', s);
                window.myNewspaperPrefs.setItem('theme', t);
            },
            [scheme, theme],
        );
        const response = await saved;
        assert.equal(response.status(), 200, 'Profile theme and scheme save succeeds before reload');
        await response.finished();
        await page.reload({ waitUntil: 'networkidle' });
    };
    try {
        if (profiles) {
            admin = await (await browser.newContext({ viewport: { width: 1440, height: 1000 } })).newPage();
            admin.setDefaultNavigationTimeout(90000);
            await admin.goto(adminBase + '/login?returnUrl=%2Fadmin%2Fprofiles');
            await admin.locator('#password').fill(password);
            await admin.getByRole('button', { name: 'Sign in', exact: true }).click();
            await admin.waitForURL('**/admin/profiles');
            await admin.waitForLoadState('networkidle');
            const name = 'zz P8 ' + Date.now().toString(36);
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
            console.log('Temporary P8 profile', id);
            const context = await browser.newContext({ viewport: { width: 390, height: 900 } });
            const p = await context.newPage();
            await p.goto(base + '/news/all');
            await p.locator(`form[action="profiles/${id}/open"]`).getByRole('button').click();
            await p.keyboard.type('3691');
            await p.waitForURL('**/news/all');
            storageState = await context.storageState();
            await context.close();
        }
        if (shots) require('node:fs').mkdirSync(shots, { recursive: true });
        const widths = (process.env.P8_WIDTHS || '320,390,430,820,1024,1280,1440').split(',').map(Number);
        for (const width of widths)
            for (const theme of ['light', 'dark']) {
                const scheme = width === 1280 ? 'monochrome' : width === 1024 ? 'cardinals' : 'kansas';
                const context = await contextFor(width, { scheme, theme });
                await context.route('**/*', (route) => {
                    if (
                        route.request().resourceType() !== 'image' ||
                        new URL(route.request().url()).origin === new URL(base).origin
                    )
                        return route.continue();
                    return route.fulfill({
                        contentType: 'image/svg+xml',
                        body: '<svg xmlns="http://www.w3.org/2000/svg" width="1280" height="720"><rect width="100%" height="100%" fill="#3b738c"/></svg>',
                    });
                });
                const page = await context.newPage();
                page.on('pageerror', (e) => errors.push(e.message));
                await visit(page, width >= 1024 ? '/news/all' : '/sports/news', scheme, theme);
                // CSS masks load on demand; decode the real asset before capturing newly revealed P8 frames.
                await page.evaluate(async () => {
                    const mark = new Image();
                    mark.src = new URL('img/placeholder-mark.svg', document.baseURI).href;
                    await mark.decode();
                });
                for (const path of width >= 1024
                    ? ['/news/all', '/sports/news', '/entertainment/news']
                    : ['/sports/news']) {
                    if (new URL(page.url()).pathname !== path)
                        await page.goto(base + path, { waitUntil: 'networkidle' });
                    assert.equal(await page.locator('html').getAttribute('data-theme'), theme);
                    const lead = page.locator('.hc-lead .hc-card');
                    assert(await lead.count(), 'Representative headline lead required');
                    const media = lead.locator('[data-article-media]');
                    // Dynamic replacement must reclassify the existing node, including after a late 404.
                    await page.route('**/__p8-photo.svg*', (route) => {
                        const kind = new URL(route.request().url()).searchParams.get('kind');
                        if (kind === 'broken') return route.fulfill({ status: 404, body: '' });
                        const size = kind === 'wide' ? [1280, 720] : kind === 'tiny' ? [16, 16] : [240, 135];
                        return route.fulfill({
                            contentType: 'image/svg+xml',
                            body: `<svg xmlns="http://www.w3.org/2000/svg" width="${size[0]}" height="${size[1]}"><rect width="100%" height="100%" fill="#3b738c"/></svg>`,
                        });
                    });
                    for (const kind of ['wide', 'compact', 'tiny', 'broken']) {
                        await media.evaluate(
                            (node, url) => {
                                node.querySelector('img')?.remove();
                                const img = document.createElement('img');
                                img.alt = '';
                                img.src = url;
                                node.append(img);
                            },
                            base + '/__p8-photo.svg?kind=' + kind,
                        );
                        const quality = kind === 'tiny' || kind === 'broken' ? 'none' : kind;
                        await page.waitForFunction(
                            ({ node, quality }) =>
                                node.dataset.imageLayout === quality &&
                                (quality !== 'none' || !node.querySelector('img')),
                            { node: await lead.elementHandle(), quality },
                        );
                        assert(await media.isVisible(), `${kind} keeps a frame`);
                        if (kind === 'wide') assert(!(await media.locator('.ph').isVisible()), 'Real photo hides P8');
                        else if (width >= 1024 || quality === 'none')
                            assert(await media.locator('.ph').isVisible(), 'P8 shown');
                        if (width >= 1024 || quality === 'none') {
                            const b = await media.boundingBox();
                            assert(Math.abs(b.width / b.height - 16 / 9) < 0.03, 'Full 16:9 lead');
                            if (kind !== 'wide') {
                                assert(await media.locator('.ph__src').isVisible(), 'Large frame publisher chip');
                                assert(await media.locator('.ph__cat').isVisible(), 'Large frame category');
                            }
                        }
                    }
                    // Make every other placeholder visible without changing production data.
                    await page
                        .locator('.hc-feed [data-article-media] > img')
                        .evaluateAll((ns) => ns.forEach((n) => n.remove()));
                    await page.waitForTimeout(150);
                    const visible = page.locator('.hc-feed article[data-article-id]:visible');
                    const ids = await visible.evaluateAll((ns) => ns.map((n) => n.dataset.articleId));
                    assert(ids.length > 8);
                    assert.equal(new Set(ids).size, ids.length, 'No repeated visible stories');
                    assert(
                        await page.evaluate(() => document.documentElement.scrollWidth <= innerWidth),
                        'No overflow',
                    );
                    if (width >= 1024) {
                        assert.equal(await page.locator('.hc-under [data-article-media]:visible').count(), 2);
                        assert.equal(
                            await page.locator('.hc-latest [data-article-media]:visible').count(),
                            1,
                            'Latest only first has frame',
                        );
                        for (const card of await visible.all()) {
                            if (
                                (await card.getAttribute('class')).includes('hc-row') &&
                                !(await card.getAttribute('class')).includes('hc-row--first')
                            )
                                continue;
                            const b = await card.locator('[data-article-media]').boundingBox();
                            assert(b && b.height > 0, 'Every card keeps its frame');
                        }
                    } else {
                        const small = page
                            .locator(
                                '.hc-feed .article:not([data-article-story="feature"]):visible [data-article-media]',
                            )
                            .first();
                        assert((await small.boundingBox()).height < 120);
                        const row = small.locator('..');
                        assert.equal(
                            await row.locator('.body').evaluate((n) => getComputedStyle(n).display),
                            'flex',
                            'Publisher and actions have separate lines',
                        );
                        const publisher = await row.locator('.article-publisher').boundingBox();
                        const body = await row.locator('.body').boundingBox();
                        assert(publisher.width >= body.width - 1, 'Publisher has the full text column');
                        assert(!(await small.locator('.ph__src').isVisible()), 'Small chip hidden');
                        assert(!(await small.locator('.ph__cat').isVisible()), 'Small category hidden');
                    }
                    assert.equal(await media.locator('.ph').getAttribute('aria-hidden'), 'true');
                    const mark = await media.locator('.ph__mark').evaluate((n) => ({
                        height: n.getBoundingClientRect().height,
                        mask: getComputedStyle(n).maskImage,
                        opacity: getComputedStyle(n).opacity,
                    }));
                    assert(mark.mask.includes('placeholder-mark.svg'));
                    assert.equal(mark.opacity, '0.2');
                    assert(Math.abs(mark.height / (await media.boundingBox()).height - 1.35) < 0.02);
                    const share = page.locator('.hc-feed [data-share-open]:visible').first();
                    for (const action of [share, lead.locator('[data-save]')]) {
                        const box = await action.boundingBox();
                        assert(box.width >= 44 && box.height >= 44, '44px action target');
                    }
                    await share.click();
                    await page.getByRole('dialog').waitFor();
                    await page.keyboard.press('Escape');
                    if (shots) {
                        if (width >= 1024)
                            await page.locator('[data-hc-top]').screenshot({
                                path: `${shots}/p8-top-${width}-${theme}-${path.split('/')[1]}.png`,
                                style: '.site-header {visibility:hidden!important}',
                            });
                        else
                            await page
                                .locator('.hc-feed .article[data-article-story="row"]:visible')
                                .first()
                                .screenshot({
                                    path: `${shots}/p8-row-${width}-${theme}.png`,
                                    style: '.site-header {visibility:hidden!important}',
                                });
                        await page.evaluate(() => scrollTo(0, 0));
                    }
                    if (shots)
                        await page.screenshot({ path: `${shots}/p8-${width}-${theme}-${path.split('/')[1]}.png` });
                }
                if (width === 390) {
                    const save = page.locator('.hc-lead [data-save]');
                    const wasSaved = await save.getAttribute('aria-pressed');
                    await save.click();
                    await page.waitForFunction(
                        (previous) =>
                            document.querySelector('.hc-lead [data-save]').getAttribute('aria-pressed') !== previous,
                        wasSaved,
                    );
                    await page.reload({ waitUntil: 'networkidle' });
                    assert.equal(
                        await page.locator('html').getAttribute('data-theme'),
                        theme,
                        'Theme survives refresh',
                    );
                    assert.notEqual(await save.getAttribute('aria-pressed'), wasSaved, 'Saved story survives refresh');
                    await save.click();
                    await page.waitForFunction(
                        (previous) =>
                            document.querySelector('.hc-lead [data-save]').getAttribute('aria-pressed') === previous,
                        wasSaved,
                    );
                }
                await page.goto(base + '/', { waitUntil: 'networkidle' });
                if (width >= 1024) {
                    for (const selector of ['.d-lead__image']) {
                        const media = page.locator(selector).first();
                        assert(await media.count(), selector + ' exists');
                        await media.locator('img').evaluateAll((ns) => ns.forEach((n) => n.remove()));
                        assert(await media.locator('.ph').isVisible(), selector + ' P8');
                    }
                    if (shots) await page.screenshot({ path: `${shots}/p8-home-${width}-${theme}.png` });
                }
                await context.close();
                console.log('PASS', width, theme, scheme);
            }
        // Without JS, unmeasured feature photos use P8 and preserve the layout.
        for (const width of [390, 1440]) {
            const context = await contextFor(width, { js: false });
            await context.route('**/*', (route) =>
                route.request().resourceType() === 'image' &&
                new URL(route.request().url()).origin !== new URL(base).origin
                    ? route.fulfill({ status: 404, body: '' })
                    : route.continue(),
            );
            const page = await context.newPage();
            await page.goto(base + '/sports/news', { waitUntil: 'networkidle' });
            const media = page.locator('.hc-lead [data-article-media]');
            assert(await media.locator('.ph').isVisible(), 'No JS P8');
            assert((await media.boundingBox()).height >= 120, 'No JS feature frame');
            assert(await page.evaluate(() => document.documentElement.scrollWidth <= innerWidth));
            if (shots) await page.screenshot({ path: `${shots}/p8-nojs-${width}.png` });
            await context.close();
        }
        assert.deepEqual(errors, []);
        console.log('P8 responsive fixtures, dynamic failures, Home, actions, themes and no-JS passed.');
    } finally {
        if (id) {
            try {
                await admin.goto(adminBase + '/admin/profiles/' + id);
                await admin.waitForLoadState('networkidle');
                const confirm = admin.getByRole('button', { name: 'Delete profile', exact: true });
                for (let attempt = 0; attempt < 12 && !(await confirm.isVisible()); attempt++) {
                    await admin.getByRole('button', { name: 'Delete profile…' }).click();
                    try {
                        await confirm.waitFor({ state: 'visible', timeout: 1500 });
                    } catch {
                        if (attempt === 11) throw new Error('Admin delete confirmation did not become interactive');
                    }
                }
                await confirm.click();
                await admin.waitForURL('**/admin/profiles?deleted=*');
            } catch (e) {
                process.exitCode = 1;
                console.error(
                    `Could not delete the throwaway profile ${id}; remove it in Admin · Profiles. ${e.message}`,
                );
            }
        }
        await browser.close();
    }
})().catch((e) => {
    console.error(e.message || e);
    process.exit(1);
});
