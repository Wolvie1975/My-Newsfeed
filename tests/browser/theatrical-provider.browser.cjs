const { chromium } = require('playwright');
const assert = require('node:assert/strict');
const base = process.env.BASE_URL || 'http://localhost:5295';
(async () => {
    const browser = await chromium.launch();
    try {
        const page = await browser.newPage();
        const errors = [];
        page.on('pageerror', (error) => errors.push(error.message));
        for (const width of [390, 1440]) {
            await page.setViewportSize({ width, height: 1000 });
            assert.equal((await page.goto(base + '/')).status(), 200);
            const heading = page.locator('#h-movies');
            // An empty theater section is left out (display: none), so only a visible one is exercised.
            if (await heading.isVisible()) {
                const link = page.locator('[data-home-section="movies"] .d-more').first();
                assert.equal(await link.getAttribute('href'), 'movies?type=theatrical');
                await link.focus();
                assert(await link.evaluate((node) => node === document.activeElement));
                const homeTitles = await page
                    .locator('.d-movies > .d-list')
                    .first()
                    .locator('.d-vid__title')
                    .allTextContents();
                assert(homeTitles.length <= 5);
                await link.press('Enter');
                await page.waitForURL('**/movies?type=theatrical');
                assert.equal(await page.locator('#movie-view').inputValue(), 'theatrical');
                const titles = await page.locator('.movie-card h3').allTextContents();
                console.log(width, 'Home theatrical films:', homeTitles.length, 'Movies films:', titles.length);
            }
            await page.goto(base + '/movies?type=theatrical');
            assert(await page.locator('.theater-site-link').isVisible());
            for (const link of await page.locator('.movie-commerce-action a, a.movie-commerce-action').all())
                assert.match(await link.getAttribute('href'), /^https:\/\//);
            assert(await page.evaluate(() => document.documentElement.scrollWidth <= innerWidth));
            await page.goto(base + '/movies');
            assert(!(await page.locator('.movie-kind').allTextContents()).includes('On disc'));
            await page.goto(base + '/movies/digital?source=dvdsreleasedates');
            assert.equal(await page.locator('#digital-source').inputValue(), 'dvdsreleasedates');
            assert(
                await page
                    .getByText('This provider supplies theater and digital release dates too.', { exact: false })
                    .isVisible(),
            );
            assert(await page.evaluate(() => document.documentElement.scrollWidth <= innerWidth));
        }
        assert.deepEqual(errors, []);
        console.log(
            'Theatrical View all keyboard navigation, provider wording, non-disc All releases and responsive checks passed.',
        );
    } finally {
        await browser.close();
    }
})().catch((error) => {
    console.error(error);
    process.exit(1);
});
