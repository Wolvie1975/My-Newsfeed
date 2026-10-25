const { chromium } = require('playwright');
const assert = require('node:assert/strict');
(async () => {
    const b = await chromium.launch();
    const p = await b.newPage();
    const base = process.env.BASE_URL || 'http://localhost:5295';
    const errors = [];
    p.on('pageerror', (e) => errors.push(e.message));
    for (const theme of ['light', 'dark'])
        for (const width of [390, 768, 1180, 1280, 1440]) {
            await p.setViewportSize({ width, height: 900 });
            await p.goto(base + '/');
            await p.evaluate((theme) => localStorage.setItem('theme', theme), theme);
            for (const path of ['/events', '/tv', '/videos', '/local', '/settings', '/']) {
                const response = await p.goto(base + path);
                assert.equal(response.status(), 200);
                assert.equal(
                    await p.evaluate(() => document.documentElement.scrollWidth > innerWidth),
                    false,
                    `${theme} ${width} ${path}`,
                );
                assert.equal(await p.locator('html').getAttribute('data-theme'), theme);
                if (process.env.SCREENSHOT_DIR && path === '/events' && (width === 390 || width === 1440))
                    await p.screenshot({ path: `${process.env.SCREENSHOT_DIR}/events-${width}-${theme}.png` });
            }
        }
    assert.deepEqual(errors, []);
    console.log('60 responsive/theme route checks passed.');
    await b.close();
})().catch((e) => {
    console.error(e);
    process.exit(1);
});
