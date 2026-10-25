const { chromium } = require('playwright');
const assert = require('node:assert/strict');
const base = process.env.BASE_URL || 'http://localhost:5295';
(async () => {
    const browser = await chromium.launch();
    try {
        const page = await browser.newPage();
        let checked = 0;
        for (const path of ['/watch?kind=tv', '/watch?kind=tv&page=2']) {
            await page.goto(base + path);
            assert(await page.getByText('Active and upcoming series.', { exact: false }).count());
            const hrefs = await page
                .locator('[data-watch-title]')
                .evaluateAll((ns) => ns.map((n) => n.getAttribute('href')));
            assert(hrefs.length);
            if (process.env.MYDAILYFEED_TEST_TMDB_TOKEN) {
                for (let i = 0; i < hrefs.length; i += 4)
                    await Promise.all(
                        hrefs.slice(i, i + 4).map(async (href) => {
                            const id = href.split('/').pop();
                            const r = await fetch('https://api.themoviedb.org/3/tv/' + id, {
                                headers: { Authorization: 'Bearer ' + process.env.MYDAILYFEED_TEST_TMDB_TOKEN },
                            });
                            assert.equal(r.status, 200);
                            const d = await r.json();
                            assert(!['Ended', 'Canceled'].includes(d.status), d.name + ' ' + d.status);
                            checked++;
                        }),
                    );
            }
        }
        await page.goto(base + '/watch?kind=tv&q=The%20Mentalist');
        assert(await page.getByRole('heading', { name: 'The Mentalist', exact: true }).count());
        assert.equal(await page.getByText('Active and upcoming series.', { exact: false }).count(), 0);
        await page.goto(base + '/watch?kind=movie');
        assert(await page.locator('[data-watch-title]').count());
        assert.equal(await page.getByText('Active and upcoming series.', { exact: false }).count(), 0);
        console.log(
            'Active TV browse pagination, ended-series search and unchanged movie browsing passed; ' +
                checked +
                ' displayed TV statuses verified against TMDB.',
        );
    } finally {
        await browser.close();
    }
})().catch((e) => {
    console.error(e);
    process.exit(1);
});
