const { chromium } = require('playwright');
const assert = require('node:assert/strict');
const base = process.env.BASE_URL || 'http://localhost:5295';
(async () => {
    const browser = await chromium.launch();
    try {
        for (const mobile of [false, true]) {
            const context = await browser.newContext({
                viewport: { width: mobile ? 390 : 1440, height: 900 },
                ...(mobile
                    ? {
                          userAgent:
                              'Mozilla/5.0 (Linux; Android 14) AppleWebKit/537.36 Chrome/130.0 Mobile Safari/537.36',
                      }
                    : {}),
            });
            await context.addInitScript(() => {
                window.copied = null;
                Object.defineProperty(navigator, 'clipboard', {
                    value: {
                        writeText: async (value) => {
                            window.copied = value;
                        },
                    },
                });
                document.execCommand = () => {
                    window.copied = document.querySelector('dialog textarea')?.value;
                    return true;
                };
            });
            const page = await context.newPage();
            const errors = [];
            page.on('pageerror', (e) => errors.push(e.message));
            // The folded phone Home links to videos; sharing is exercised on the video page.
            for (const route of mobile ? ['/videos', '/saved', '/news'] : ['/', '/videos', '/saved', '/news']) {
                if (route === '/saved')
                    await page.route('**/api/saved', (r) =>
                        r.fulfill({
                            contentType: 'application/json',
                            body: JSON.stringify({
                                token: 'test',
                                articles: [
                                    {
                                        url: 'https://www.youtube.com/watch?v=test',
                                        title: 'Saved video',
                                        kind: 'video',
                                    },
                                ],
                            }),
                        }),
                    );
                await page.goto(base + route);
                const button = page
                    .locator(route === '/news' ? '[data-share-open]' : '[data-share-open][data-kind="video"]')
                    // Phone Headlines has a hidden desktop copy of each story.
                    .filter({ visible: true })
                    .first();
                await button.waitFor({ state: 'visible' });
                const url = await button.getAttribute('data-url');
                const title = await button.getAttribute('data-title');
                await button.click();
                const dialog = page.getByRole('dialog');
                await dialog.waitFor();
                assert.equal(
                    await dialog.locator('h2').innerText(),
                    route === '/news' ? 'Share article' : 'Share video',
                );
                assert.equal(await dialog.locator('.article-share__title').innerText(), title);
                assert.equal(await dialog.getByRole('link', { name: 'Send by SMS' }).count(), mobile ? 1 : 0);
                if (mobile)
                    assert(
                        (await dialog.getByRole('link', { name: 'Send by SMS' }).getAttribute('href')).includes(
                            encodeURIComponent(url),
                        ),
                    );
                await dialog.getByRole('button', { name: 'Copy link', exact: true }).click();
                await dialog.waitFor({ state: 'detached' });
                assert.equal(await page.evaluate(() => window.copied), url);
                await button.click();
                await page.keyboard.press('Escape');
                await page.getByRole('dialog').waitFor({ state: 'detached' });
                assert(await button.evaluate((b) => document.activeElement === b));
                assert(await page.evaluate(() => document.documentElement.scrollWidth <= innerWidth));
                if (route === '/saved') await page.unroute('**/api/saved');
            }
            assert.deepEqual(errors, []);
            await context.close();
        }
        console.log(
            'Video sharing on Home/Videos/Saved and article regression: dialog labels/title, copy URL, mobile SMS, Escape/focus return and responsive layout passed.',
        );
    } finally {
        await browser.close();
    }
})().catch((e) => {
    console.error(e);
    process.exit(1);
});
