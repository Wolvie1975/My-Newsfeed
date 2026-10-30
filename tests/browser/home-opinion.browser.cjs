// Folded phone layouts are covered by phone-fronts.browser.cjs; these checks exercise the retained tablet/desktop controls.
const assert = require('node:assert/strict'),
    { chromium } = require('playwright');
const base = process.env.BASE_URL || 'http://localhost:5295',
    root = process.env.SCREENSHOT_DIR;
async function mockSaved(p) {
    let saved = [];
    await p.route('**/api/saved**', async (r) => {
        const path = new URL(r.request().url()).pathname;
        if (r.request().method() === 'POST') {
            const item = r.request().postDataJSON();
            if (path.endsWith('/add')) saved = [item];
            if (path.endsWith('/remove')) saved = [];
        }
        await r.fulfill({
            contentType: 'application/json',
            body: JSON.stringify({ articles: saved, token: 'browser-test' }),
        });
    });
}
(async () => {
    const b = await chromium.launch();
    try {
        const p = await b.newPage();
        await mockSaved(p);
        const errors = [];
        p.on('pageerror', (e) => errors.push(e.message));
        for (const width of [768, 1440])
            for (const theme of ['light', 'dark']) {
                await p.setViewportSize({ width, height: 1000 });
                await p.emulateMedia({ colorScheme: theme });
                await p.goto(base + '/');
                const card = p.locator('[data-home-opinion]');
                await card.scrollIntoViewIfNeeded();
                assert.equal(await card.locator('[data-opinion-article]').count(), 1);
                assert.equal(
                    (await card.locator('h2').textContent()).replace(/\s+/g, ' ').trim(),
                    'Opinion Publisher spotlight · rotates hourly',
                );
                assert(await p.evaluate(() => document.documentElement.scrollWidth <= innerWidth));
                assert(!(await p.locator('#blazor-error-ui').isVisible()));
                // Broadsheet: Opinion heads the side column on desktop; on phones it follows the headlines.
                if (width >= 1024)
                    assert.equal(
                        await p
                            .locator('.bs-front .bs-col--side > [data-home-section]')
                            .first()
                            .getAttribute('data-home-section'),
                        'opinion',
                    );
                else {
                    const top = (key) =>
                        p.locator('[data-home-section="' + key + '"]').evaluate((n) => n.getBoundingClientRect().top);
                    assert((await top('news')) < (await top('opinion')));
                    assert((await top('opinion')) < (await top('sports-news')));
                }
                for (const selector of ['[data-save]', '[data-share-open]']) {
                    await card.locator(selector).waitFor();
                    assert((await card.locator(selector).boundingBox()).height >= 44);
                }
                const url = await card.locator('.d-story__title a').getAttribute('href');
                assert(/^https?:\/\//.test(url));
                if (root && width <= 430)
                    await card.screenshot({ path: root + '/opinion-' + width + '-' + theme + '.png' });
            }
        const card = p.locator('[data-home-opinion]');
        await card.locator('[data-save]').click();
        await p.waitForFunction(
            () => document.querySelector('[data-home-opinion] [data-save]').getAttribute('aria-pressed') === 'true',
        );
        await card.locator('[data-save]').click();
        await p.waitForFunction(
            () => document.querySelector('[data-home-opinion] [data-save]').getAttribute('aria-pressed') === 'false',
        );
        await card.locator('[data-share-open]').click();
        await p.locator('dialog[open]').waitFor();
        await p.keyboard.press('Escape');
        const publisher = await card.locator('.d-src').innerText();
        await p.evaluate(
            (publisher) => localStorage.setItem('mynewsfeed-personal-v1', JSON.stringify({ muted: [publisher] })),
            publisher,
        );
        await p.reload();
        // Old muted-publisher settings no longer hide stories.
        assert(await card.locator('[data-opinion-article]').isVisible());
        await p.evaluate(() => localStorage.setItem('mynewsfeed-personal-v1', JSON.stringify({ hidden: ['opinion'] })));
        await p.reload();
        assert(await p.locator('[data-home-opinion]').evaluate((n) => n.hidden));
        const plain = await b.newPage({ javaScriptEnabled: false, viewport: { width: 820, height: 1000 } });
        await plain.goto(base + '/');
        const readOnly = plain.locator('[data-home-opinion]');
        assert.equal(await readOnly.locator('[data-opinion-article]').count(), 1);
        await readOnly.getByRole('link', { name: 'View all →', exact: true }).click();
        assert.equal(new URL(plain.url()).pathname, '/news/all');
        assert.equal(await plain.locator('#news-section option:checked').innerText(), 'Opinion');
        assert((await plain.locator('#source option').count()) > 1);
        const rotating = await b.newPage({ viewport: { width: 820, height: 1000 } });
        await mockSaved(rotating);
        await rotating.clock.install();
        let requests = 0;
        await rotating.route('**/home/opinion', async (r) => {
            requests++;
            if (requests === 1) {
                await r.fulfill({ status: 503, body: 'unavailable' });
                return;
            }
            const response = await r.fetch();
            const html = (await response.text()).replace(
                /(<span class="d-src">)[^<]*(<\/span>)/,
                '$1Hourly rotation fixture$2',
            );
            await r.fulfill({ response, body: html });
        });
        await rotating.goto(base + '/');
        const before = await rotating
            .locator('[data-home-opinion] [data-opinion-article]')
            .getAttribute('data-opinion-article');
        const delay = Number(await rotating.locator('[data-home-opinion]').getAttribute('data-opinion-refresh-after'));
        const first = rotating.waitForResponse((r) => r.url().endsWith('/home/opinion'));
        await rotating.clock.fastForward(delay + 1000);
        await first;
        await rotating.waitForTimeout(100);
        assert.equal(await rotating.locator('[data-opinion-article]').getAttribute('data-opinion-article'), before);
        const retry = rotating.waitForResponse((r) => r.url().endsWith('/home/opinion'));
        await rotating.clock.fastForward(61000);
        await retry;
        await rotating.getByText('Hourly rotation fixture', { exact: true }).waitFor();
        assert.equal(requests, 2);
        await rotating.locator('[data-home-opinion] [data-save]').waitFor();
        assert(await rotating.evaluate(() => document.documentElement.scrollWidth <= innerWidth));
        assert.deepEqual(errors, []);
        console.log(
            'Home Opinion card: 320/390/430 light/dark plus tablet/desktop, one publisher story, save/share with mocked writes, reading/visibility preferences, no-JS feed link, timed refresh and failed-refresh retention passed.',
        );
    } finally {
        await b.close();
    }
})().catch((e) => {
    console.error(e);
    process.exit(1);
});
