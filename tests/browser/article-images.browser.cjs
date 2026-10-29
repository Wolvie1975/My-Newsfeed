// Read-only preview checks: image requests use controlled fixtures; articles and preferences are not changed.
const { chromium } = require('playwright');
const assert = require('node:assert/strict');
const fs = require('node:fs');
const base = process.env.BASE_URL || 'http://localhost:5397';
const shots = process.env.SCREENSHOT_DIR;
const selector = '[data-article-story="feature"]';
const fixtures = {
    wide: [1280, 720, '#3b738c'],
    modest: [640, 360, '#3b738c'],
    compact: [240, 135, '#a06336'],
    portrait: [480, 800, '#70477b'],
    tiny: [16, 16, '#777'],
};
function photo(kind) {
    const [width, height, color] = fixtures[kind];
    return `<svg xmlns="http://www.w3.org/2000/svg" width="${width}" height="${height}" viewBox="0 0 ${width} ${height}"><rect width="100%" height="100%" fill="${color}"/><circle cx="${width / 3}" cy="${height / 2}" r="${height / 4}" fill="#edce88"/><text x="10" y="30" font-size="20" fill="white">${width} × ${height}</text></svg>`;
}
async function ready(page, story, quality) {
    await page.waitForFunction(
        ({ node, quality }) => {
            const img = node.querySelector('[data-article-media] > img');
            return (
                node.dataset.imageLayout === quality &&
                (quality === 'none' ? !img : img?.complete && img.dataset.articleQuality === quality)
            );
        },
        { node: await story.elementHandle(), quality },
    );
}
(async () => {
    const browser = await chromium.launch();
    const errors = [];
    if (shots) fs.mkdirSync(shots, { recursive: true });
    try {
        for (const width of [320, 390, 430, 744, 1440]) {
            for (const theme of ['light', 'dark']) {
                for (const kind of ['wide', 'compact', 'portrait', 'tiny', 'broken']) {
                    const context = await browser.newContext({ viewport: { width, height: 1000 }, colorScheme: theme });
                    await context.addInitScript(
                        ({ theme }) => {
                            localStorage.setItem('theme', theme);
                        },
                        { theme },
                    );
                    await context.route('**/*', async (route) => {
                        if (
                            route.request().resourceType() !== 'image' ||
                            new URL(route.request().url()).origin === new URL(base).origin
                        )
                            return route.fallback();
                        if (kind === 'broken') return route.fulfill({ status: 404, body: '' });
                        return route.fulfill({ contentType: 'image/svg+xml', body: photo(kind) });
                    });
                    const page = await context.newPage();
                    page.on('pageerror', (error) => errors.push(error.message));
                    const response = await page.goto(base + '/news/all', { waitUntil: 'networkidle' });
                    assert.equal(response.status(), 200, 'Use an ungated local preview');
                    const story = page.locator(selector).filter({ visible: true }).first();
                    const quality =
                        kind === 'tiny' || kind === 'broken' ? 'none' : kind === 'portrait' ? 'compact' : kind;
                    console.log(width, theme, kind);
                    await ready(page, story, quality);
                    const media = story.locator('[data-article-media]');
                    if (quality === 'wide') {
                        assert(
                            (await media.boundingBox()).height <= (width >= 900 ? 360 : 220) + 1,
                            'feature image height capped',
                        );
                    } else if (quality === 'compact') {
                        const box = await media.boundingBox();
                        assert(box.width <= 113 && box.height <= 85, 'small/portrait photos stay thumbnails');
                        assert.equal(
                            await media.locator('img').evaluate((img) => getComputedStyle(img).objectFit),
                            'scale-down',
                        );
                        const headline = await story.locator('h2, h3').first().boundingBox();
                        assert(headline.x + headline.width <= box.x + 1, 'thumbnail beside the headline');
                    } else assert(!(await media.isVisible()), 'no empty feature-image area');
                    assert(
                        await page.evaluate(() => document.documentElement.scrollWidth <= innerWidth),
                        `${kind} ${width} ${theme}: no overflow`,
                    );
                    assert(await story.locator('h2, h3').first().isVisible(), 'headline stays visible');
                    assert(await story.locator('[data-save], .save').count(), 'save action retained');
                    if (shots && [390, 1440].includes(width))
                        await page.screenshot({ path: `${shots}/${kind}-${width}-${theme}.png` });
                    await context.close();
                }
            }
        }
        for (const width of [390, 1440])
            for (const theme of ['light', 'dark'])
                for (const kind of ['wide', 'compact', 'broken']) {
                    const context = await browser.newContext({ viewport: { width, height: 1000 }, colorScheme: theme });
                    await context.addInitScript((theme) => localStorage.setItem('theme', theme), theme);
                    await context.route('**/*', (route) => {
                        if (
                            route.request().resourceType() !== 'image' ||
                            new URL(route.request().url()).origin === new URL(base).origin
                        )
                            return route.fallback();
                        return route.fulfill(
                            kind === 'broken'
                                ? { status: 404, body: '' }
                                : { contentType: 'image/svg+xml', body: photo(kind) },
                        );
                    });
                    const page = await context.newPage();
                    page.on('pageerror', (error) => errors.push(error.message));
                    for (const path of ['/', '/sports', '/entertainment']) {
                        await page.goto(base + path, { waitUntil: 'networkidle' });
                        const story = page.locator(selector).filter({ visible: true }).first();
                        assert(await story.count(), path + ' has a representative lead');
                        let hasImage = await story.locator('[data-article-media] > img').count();
                        // A current Home lead can legitimately have no image. Exercise its existing image slot too.
                        if (!hasImage && kind !== 'broken' && (await story.locator('.d-lead__image').count())) {
                            await story.locator('.d-lead__image').evaluate((media) => {
                                const img = document.createElement('img');
                                img.src = 'https://article-fixtures.example.invalid/photo.svg';
                                img.alt = '';
                                media.append(img);
                            });
                            hasImage = 1;
                        }
                        if (kind === 'broken' || !hasImage) await ready(page, story, 'none');
                        else await ready(page, story, kind);
                        assert(
                            await page.evaluate(() => document.documentElement.scrollWidth <= innerWidth),
                            path + ' no overflow',
                        );
                        if (kind === 'wide' && hasImage)
                            assert(
                                (await story.locator('[data-article-media]').boundingBox()).height <=
                                    (width >= 900 ? 360 : 220) + 1,
                                path + ' cap',
                            );
                        if (hasImage && (await story.locator('.d-lead__body').count())) {
                            assert(
                                (await story.locator('.d-lead__body').boundingBox()).width > 0,
                                'Home photo has a real text column',
                            );
                            if (kind === 'compact') {
                                const imageBox = await story.locator('[data-article-media]').boundingBox(),
                                    titleBox = await story.locator('.d-lead__title').boundingBox();
                                assert(
                                    titleBox.x + titleBox.width <= imageBox.x + 1,
                                    'Home thumbnail beside its headline',
                                );
                            }
                        }
                        if (shots && theme === 'light' && kind === 'compact')
                            await page.screenshot({
                                path: shots + '/surface-' + (path.slice(1) || 'home') + '-' + width + '.png',
                            });
                    }
                    await context.close();
                }
        const largeScreen = await browser.newContext({ viewport: { width: 2560, height: 1200 } });
        await largeScreen.route('**/*', (route) => {
            if (
                route.request().resourceType() !== 'image' ||
                new URL(route.request().url()).origin === new URL(base).origin
            )
                return route.fallback();
            return route.fulfill({ contentType: 'image/svg+xml', body: photo('modest') });
        });
        const largePage = await largeScreen.newPage();
        await largePage.goto(base + '/news/all', { waitUntil: 'networkidle' });
        const largeStory = largePage.locator(selector).filter({ visible: true }).first();
        await ready(largePage, largeStory, 'wide');
        assert(
            (await largeStory.locator('[data-article-media]').boundingBox()).width <= 640,
            'never enlarge beyond source pixels',
        );
        await largePage.setViewportSize({ width: 390, height: 1000 });
        await ready(largePage, largePage.locator(selector).filter({ visible: true }).first(), 'wide');
        await largeScreen.close();
        // The known BBC higher-resolution candidate must fall back once to its original thumbnail.
        const context = await browser.newContext({ viewport: { width: 390, height: 1000 } });
        let preferredRequests = 0;
        await context.route('https://ichef.bbci.co.uk/**', (route) => {
            if (route.request().url().includes('/standard/1024/')) {
                preferredRequests++;
                return route.fulfill({ status: 404, body: '' });
            }
            return route.fulfill({ contentType: 'image/svg+xml', body: photo('compact') });
        });
        const page = await context.newPage();
        page.on('pageerror', (error) => errors.push(error.message));
        await page.goto(base + '/news/all', { waitUntil: 'networkidle' });
        await page.evaluate(() => {
            const source = [...document.querySelectorAll('[data-article-story="feature"]')].find(
                (n) => n.offsetWidth > 0,
            );
            const clone = source.cloneNode(true);
            clone.id = 'image-fallback-test';
            clone.dataset.imageLayout = 'compact';
            clone.querySelector('img').src = 'https://ichef.bbci.co.uk/ace/standard/240/cpsprodpb/fixture.jpg';
            source.after(clone);
        });
        const clone = page.locator('#image-fallback-test');
        await ready(page, clone, 'compact');
        assert((await clone.locator('img').getAttribute('src')).includes('/standard/240/'));
        assert(preferredRequests > 0, 'higher-resolution candidate attempted');
        const requestCount = preferredRequests;
        await page.waitForTimeout(300);
        assert.equal(preferredRequests, requestCount, 'no retry loop');
        // Replacing an image URL on an existing node re-evaluates quality.
        await page.route('**/__article-photo-wide.svg', (route) =>
            route.fulfill({ contentType: 'image/svg+xml', body: photo('wide') }),
        );
        await clone.locator('img').evaluate((img, base) => {
            img.src = base + '/__article-photo-wide.svg';
        }, base);
        await ready(page, clone, 'wide');
        await context.unroute('https://ichef.bbci.co.uk/**');
        await context.route('https://ichef.bbci.co.uk/**', (route) =>
            route.fulfill({
                contentType: 'image/svg+xml',
                body: photo(route.request().url().includes('/1024/') ? 'wide' : 'compact'),
            }),
        );
        await clone.locator('img').evaluate((img) => {
            img.src = 'https://ichef.bbci.co.uk/ace/standard/240/cpsprodpb/second.jpg?test=keep';
        });
        await ready(page, clone, 'wide');
        assert.equal(
            await clone.locator('img').getAttribute('src'),
            'https://ichef.bbci.co.uk/ace/standard/1024/cpsprodpb/second.jpg?test=keep',
        );
        await context.route('https://ichef.bbci.co.uk.example.invalid/**', (route) =>
            route.fulfill({ contentType: 'image/svg+xml', body: photo('compact') }),
        );
        await clone.locator('img').evaluate((img) => {
            img.src = 'https://ichef.bbci.co.uk.example.invalid/ace/standard/240/photo.jpg';
        });
        await ready(page, clone, 'compact');
        assert(
            (await clone.locator('img').getAttribute('src')).includes('/standard/240/'),
            'other hosts are never rewritten',
        );
        await context.route('**/*', (route) => {
            if (
                route.request().resourceType() === 'image' &&
                new URL(route.request().url()).origin !== new URL(base).origin
            )
                return route.fulfill({ contentType: 'image/svg+xml', body: photo('wide') });
            return route.fallback();
        });
        await page.evaluate(() => {
            window.articleImageNavigationTest = true;
            const link = document.createElement('a');
            link.id = 'article-nav-test';
            link.href = '/news/all?category=all';
            link.textContent = 'Test headlines navigation';
            document.body.append(link);
        });
        await page.locator('#article-nav-test').click();
        await page.waitForURL('**/news/all?category=all');
        await page.waitForLoadState('networkidle');
        assert(
            await page.evaluate(() => window.articleImageNavigationTest),
            'enhanced navigation preserves the running script',
        );
        await ready(page, page.locator(selector).filter({ visible: true }).first(), 'wide');
        await page.getByRole('link', { name: /^More stories/ }).click();
        await page.waitForURL((u) => u.searchParams.has('after'));
        await page.waitForLoadState('networkidle');
        const before = await page.locator('.article[data-article-story]').count();
        await page.locator('[data-load-more]').click();
        await page.waitForFunction(
            (before) => document.querySelectorAll('.article[data-article-story]').length > before,
            before,
        );
        const rows = await page.locator('.article[data-article-story]').all();
        let appended;
        for (const row of rows.slice(before)) {
            if (await row.locator('[data-article-media] > img').count()) {
                appended = row;
                break;
            }
        }
        assert(appended, 'new batch includes an image to verify');
        await appended.scrollIntoViewIfNeeded();
        await ready(page, appended, 'wide');
        await context.close();
        // Blazor enhanced navigation can activate a noscript stylesheet link.
        for (const width of [320, 390, 430, 744, 1440]) {
            for (const theme of ['light', 'dark']) {
                const navContext = await browser.newContext({ viewport: { width, height: 1000 }, colorScheme: theme });
                await navContext.route('**/*', (route) => {
                    if (
                        route.request().resourceType() !== 'image' ||
                        new URL(route.request().url()).origin === new URL(base).origin
                    )
                        return route.fallback();
                    return route.fulfill({ contentType: 'image/svg+xml', body: photo('wide') });
                });
                const navPage = await navContext.newPage();
                await navPage.goto(base + '/news', { waitUntil: 'networkidle' });
                await navPage.evaluate(() => {
                    window.articleNavigationSentinel = true;
                });
                for (const path of ['', 'sports', 'entertainment', 'news']) {
                    await navPage.evaluate((path) => {
                        document.getElementById('article-nav-regression')?.remove();
                        const link = document.createElement('a');
                        link.href = path || '/';
                        link.id = 'article-nav-regression';
                        link.textContent = 'Navigate';
                        document.body.append(link);
                    }, path);
                    await navPage.locator('#article-nav-regression').evaluate((link) => link.click());
                    await navPage.waitForURL(base + '/' + path);
                    await navPage.waitForLoadState('networkidle');
                    await navPage.waitForTimeout(1000);
                    assert(
                        await navPage.evaluate(() => window.articleNavigationSentinel),
                        'enhanced navigation retained document',
                    );
                    const lead = navPage.locator(selector).filter({ visible: true }).first();
                    const media = lead.locator('[data-article-media]');
                    if (await media.locator('img').count()) {
                        await ready(navPage, lead, 'wide');
                        assert(await media.isVisible(), 'enhanced navigation must not activate image-hiding fallback');
                        assert((await media.boundingBox()).height > 0, 'lead image has visible area');
                    }
                    assert(await navPage.evaluate(() => document.documentElement.scrollWidth <= innerWidth));
                }
                if (shots) await navPage.screenshot({ path: `${shots}/navigation-${width}-${theme}.png` });
                await navContext.close();
            }
        }
        // Without JS, even large images stay modest; failed images leave no large blank region.
        for (const broken of [false, true]) {
            const plain = await browser.newContext({
                viewport: { width: 390, height: 1000 },
                javaScriptEnabled: false,
            });
            await plain.route('**/*', (route) => {
                if (
                    route.request().resourceType() !== 'image' ||
                    new URL(route.request().url()).origin === new URL(base).origin
                )
                    return route.fallback();
                return route.fulfill(
                    broken ? { status: 404, body: '' } : { contentType: 'image/svg+xml', body: photo('compact') },
                );
            });
            const p = await plain.newPage();
            await p.goto(base + '/news/all', { waitUntil: 'networkidle' });
            const story = p.locator(selector).filter({ visible: true }).first(),
                media = story.locator('[data-article-media]');
            assert(!(await media.isVisible()), 'no-JavaScript features use a text-led fallback');
            assert(await p.evaluate(() => document.documentElement.scrollWidth <= innerWidth));
            await plain.close();
        }
        assert.deepEqual(errors, []);
        console.log(
            'PASS: article images at 320/390/430/744/1440 light/dark; wide, thumbnail, portrait, tiny, broken, BBC retry, dynamic replacement, enhanced navigation, Load more, front pages, actions and no-JS.',
        );
    } finally {
        await browser.close();
    }
})().catch((error) => {
    console.error(error);
    process.exit(1);
});
