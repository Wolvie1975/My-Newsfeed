const assert = require('node:assert/strict'),
    { chromium } = require('playwright');
const base = process.env.BASE_URL || 'http://localhost:5295',
    root = process.env.SCREENSHOT_DIR;
(async () => {
    const browser = await chromium.launch();
    try {
        // Phone width: the category dropdown (desktop shows category pills instead).
        const p = await browser.newPage({ viewport: { width: 390, height: 900 } });
        const errors = [];
        p.on('pageerror', (e) => errors.push(e.message));
        const categories = [],
            movedSources = new Set();
        for (const [route, title] of [
            ['entertainment/news', 'Entertainment headlines', 'entertainment-news'],
            ['entertainment/news/tv-film', 'Entertainment headlines', 'tv-film-news'],
        ]) {
            await p.setViewportSize({ width: 390, height: 900 });
            await p.goto(base + '/' + route);
            assert.equal(await p.locator('h1:visible').innerText(), title);
            assert.equal(
                await p.locator('#hub-category').inputValue(),
                await p.locator('form input[name="category"]').inputValue(),
            );
            // Pick TV/Film by name: other topics (Box Office, October 26) can follow it in the list.
            await p.selectOption('#hub-category', { label: 'TV/Film' });
            await p.waitForURL(base + '/entertainment/news/tv-film');
            await p.getByRole('heading', { name: 'TV/Film', exact: true }).waitFor();
            assert.equal(await p.locator('h2').first().innerText(), 'TV/Film');
            await p.goto(base + '/' + route);
            const category = await p.locator('form input[name="category"]').inputValue();
            categories.push(category);
            (await p.locator('#source option').evaluateAll((ns) => ns.map((n) => n.value).filter(Boolean))).forEach(
                (id) => movedSources.add(id),
            );
            const firstSource = await p.locator('#source option').evaluateAll((ns) => ns.find((n) => n.value)?.value);
            const redirect = await p.request.get(base + '/news/all?category=' + category + '&source=' + firstSource, {
                maxRedirects: 0,
            });
            assert.equal(redirect.status(), 302);
            const destination = new URL(redirect.headers().location, base);
            assert.equal(destination.pathname, '/' + route);
            assert.equal(destination.searchParams.get('source'), firstSource);
            const before = await p.locator('.feed .article').count();
            assert(before > 0);
            const more = p.locator('[data-load-more]');
            if (await more.count()) {
                const responsePromise = p.waitForResponse((r) => r.url().includes('/feed/more?'));
                await more.click();
                const response = await responsePromise;
                assert.equal(new URL(response.url()).searchParams.get('view'), 'entertainment-hub-news');
                assert.equal(response.status(), 200);
                const result = await response.json();
                if (result.nextHref) assert(result.nextHref.startsWith(route + '?'));
                await p.waitForFunction((n) => document.querySelectorAll('.feed .article').length > n, before);
                const ids = await p.locator('.feed .article').evaluateAll((ns) => ns.map((n) => n.dataset.articleId));
                assert.equal(ids.length, new Set(ids).size);
                const leaked = await p.request.get(
                    base +
                        '/feed/more?view=news&category=' +
                        category +
                        '&after=' +
                        new URL(response.url()).searchParams.get('after'),
                );
                assert.equal((await leaked.json()).total, 0);
            }
            await p.selectOption('#source', firstSource);
            await p.waitForURL((u) => u.searchParams.get('source') === firstSource);
            assert.equal(new URL(p.url()).pathname, '/' + route);
            for (const width of [320, 390, 430, 768, 1440])
                for (const theme of ['light', 'dark']) {
                    await p.setViewportSize({ width, height: 900 });
                    await p.emulateMedia({ colorScheme: theme });
                    await p.goto(base + '/' + route);
                    assert.equal(await p.locator('html').getAttribute('data-theme'), theme);
                    assert(await p.evaluate(() => document.documentElement.scrollWidth <= innerWidth));
                    assert(!(await p.locator('#blazor-error-ui').isVisible()));
                    // Desktop shows topic pills instead of the dropdown.
                    if (width < 1024)
                        assert(
                            await p.locator('#hub-category').evaluate((n) => n.getBoundingClientRect().height >= 44),
                        );
                    if (width < 768) {
                        await p.locator('.mobile-nav > summary').click();
                        const group = p.locator('[aria-labelledby="mobile-entertainment-heading"]');
                        assert.equal(
                            await group.locator('a[href="entertainment/news"][aria-current="page"]').count(),
                            1,
                        );
                        assert.equal(
                            await p.locator('[aria-labelledby="mobile-news-heading"] a[href="' + route + '"]').count(),
                            0,
                        );
                        assert(
                            await group
                                .locator('a[href="entertainment/news"]')
                                .evaluate((n) => n.getBoundingClientRect().height >= 44),
                        );
                        await p.locator('.mobile-nav > summary').click();
                    }
                    if (root && width < 768) await p.waitForTimeout(250);
                    if (root && width < 768)
                        await p.screenshot({
                            path: root + '/' + route.replaceAll('/', '-') + '-' + width + '-' + theme + '.png',
                        });
                }
        }
        await p.goto(base + '/entertainment/tv-film');
        await p.getByRole('heading', { name: 'TV/Film', exact: true }).waitFor();
        assert.equal(await p.locator('h2').first().innerText(), 'TV/Film');
        await p.goto(base + '/news/all');
        const names = await p.locator('#news-section option').allTextContents();
        assert(!names.includes('Entertainment') && !names.includes('TV/Film'));
        const sourceIds = await p.locator('#source option').evaluateAll((ns) => ns.map((n) => n.value));
        assert(sourceIds.every((id) => !movedSources.has(id)));
        const plain = await browser.newPage({ javaScriptEnabled: false, viewport: { width: 390, height: 900 } });
        for (const route of ['entertainment/news', 'entertainment/news/tv-film']) {
            await plain.goto(base + '/' + route);
            const current = await plain.locator('#hub-category').inputValue();
            await plain.selectOption('#hub-category', current);
            await plain.locator('.news-category-select button').click();
            await plain.waitForURL((u) => u.pathname === '/' + route);
            const more = plain.locator('[data-load-more]');
            if (await more.count()) {
                await more.click();
                await plain.waitForURL((u) => u.searchParams.has('after'));
                assert.equal(new URL(plain.url()).pathname, '/' + route);
                assert((await plain.locator('.feed .article').count()) > 0);
            }
            assert(await plain.evaluate(() => document.documentElement.scrollWidth <= innerWidth));
        }
        // In-site navigation between same-layout pages updates the stories in place (October 26, 2026): the share
        // buttons must still be revealed without a refresh.
        await p.setViewportSize({ width: 1440, height: 900 });
        await p.goto(base + '/entertainment/news');
        await p.locator('a[href="news"]:visible').first().click();
        await p.waitForURL(base + '/news');
        await p.waitForTimeout(800);
        assert(await p.locator('[data-share-open]').count());
        assert.equal(await p.locator('[data-share-open][hidden]').count(), 0, 'share buttons after in-site navigation');
        assert.deepEqual(errors, []);
        await plain.close();
        console.log(
            'Entertainment/TV-Film navigation, active states, old links, source filters, scoped JS/no-JS pagination, Headlines separation and mobile light/dark layouts passed.',
        );
    } finally {
        await browser.close();
    }
})().catch((e) => {
    console.error(e);
    process.exit(1);
});
