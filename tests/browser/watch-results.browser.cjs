const assert = require('node:assert/strict');
const fs = require('node:fs');
const {chromium} = require(process.env.PLAYWRIGHT_MODULE || '/tmp/mydailyfeed-cascade-check/node_modules/playwright');
const base = process.env.BASE_URL || 'http://127.0.0.1:5342';
const root = process.env.SCREENSHOT_DIR || '/tmp/mynewspaper-watch-results-deploy/preview';

(async () => {
    fs.mkdirSync(root, {recursive: true});
    const browser = await chromium.launch();
    try {
        for (const js of [true, false]) {
            const page = await browser.newPage({javaScriptEnabled: js});
            const errors = [];
            page.on('pageerror', error => errors.push(error.message));
            for (const width of [320, 390, 430, 768, 1440]) {
                for (const theme of ['light', 'dark']) {
                    await page.setViewportSize({width, height: 1000});
                    await page.emulateMedia({colorScheme: theme});
                    const response = await page.goto(base + '/watch?q=arrow&sort=popular');
                    assert.equal(response.status(), 200);
                    await page.locator('.watch-result').first().waitFor();
                    assert(await page.evaluate(() => document.documentElement.scrollWidth <= innerWidth), `overflow ${width} ${theme}`);
                    const row = page.locator('.watch-result').filter({has: page.getByRole('heading', {name: 'Arrow', exact: true})}).first();
                    assert(await row.count(), 'Arrow series result');
                    assert(await row.locator('.watch-provider-group').count());
                    assert(await row.locator('.watch-provider img').count(), 'provider logos');
                    assert((await row.locator('.watch-provider-group h3').allTextContents()).some(x => ['Stream', 'Buy', 'Rent'].includes(x)));
                    assert.equal(await page.locator('[data-watch-title]').count(), await page.locator('.watch-result').count(), 'one primary link per result');
                    assert(await row.locator('.watch-result__links a').evaluateAll(nodes => nodes.every(n => n.getBoundingClientRect().height >= 44)));
                    const poster = await row.locator('.watch-result__poster').boundingBox();
                    const title = await row.locator('.watch-result__heading').boundingBox();
                    assert(title.x >= poster.x + poster.width, 'poster beside title');
                    if (width < 700) {
                        const offers = await row.locator('.watch-result__offers').boundingBox();
                        assert(offers.y >= poster.y + poster.height - 1, 'mobile offers below poster');
                    }
                    await row.screenshot({path: `${root}/arrow-${width}-${theme}-${js}.png`});
                    await page.locator('#watch-sort').selectOption('title');
                    await page.getByRole('button', {name: 'Apply sort', exact: true}).click();
                    await page.waitForURL(u => u.searchParams.get('sort') === 'title');
                    assert.equal(new URL(page.url()).searchParams.get('q'), 'arrow');
                    assert(await page.locator('.watch-result').count());
                }
            }
            await page.goto(base + '/watch?q=arrow');
            const details = page.locator('.watch-result [data-watch-open]').first();
            await details.click();
            if (js) {
                await page.locator('dialog[open] .watch-detail').waitFor();
                await page.keyboard.press('Escape');
                await page.locator('dialog.box-score-dialog').waitFor({state: 'detached'});
                assert(await details.evaluate(n => document.activeElement === n));
            } else {
                await page.locator('[data-watch-content] .watch-detail').waitFor();
            }
            await page.goto(base + '/watch?kind=tv');
            assert(await page.locator('.watch-grid .watch-card').count(), 'browse grid retained');
            assert.equal(await page.locator('.watch-result').count(), 0);
            assert.deepEqual(errors, []);
            await page.close();
        }
        console.log('20 width/theme/JS cases passed: grouped provider logos, poster layout, 44px links, sorting/search state, modal/focus, no-JS details and browse grid.');
    } finally {
        await browser.close();
    }
})().catch(error => { console.error(error); process.exit(1); });
