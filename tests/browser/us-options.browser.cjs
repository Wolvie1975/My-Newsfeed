const { chromium } = require('playwright');
const assert = require('node:assert/strict');
const base = process.env.BASE_URL || 'http://localhost:5295';
(async () => {
    const browser = await chromium.launch();
    try {
        const page = await browser.newPage();
        let checked = 0;
        for (const path of ['/watch?kind=movie', '/watch?kind=tv', '/watch?kind=movie&q=Arrival']) {
            await page.goto(base + path);
            assert(
                await page
                    .locator('.watch-results-count,.watch-results-head', { hasText: 'US viewing options' })
                    .count(),
            );
            const links = await page
                .locator('[data-watch-title]')
                .evaluateAll((ns) => ns.map((n) => n.getAttribute('href')));
            assert(links.length, path);
            for (let i = 0; i < links.length; i += 4)
                await Promise.all(
                    links.slice(i, i + 4).map(async (href) => {
                        const response = await page.request.get(new URL(href, base).href);
                        assert.equal(response.status(), 200);
                        assert((await response.text()).includes('class="watch-offer"'), href);
                        checked++;
                    }),
                );
        }
        console.log('US viewing options verified for ' + checked + ' movie/TV/search result cards.');
    } finally {
        await browser.close();
    }
})().catch((e) => {
    console.error(e);
    process.exit(1);
});
