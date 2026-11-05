const { chromium } = require('playwright');
const lib = require('./lib.cjs');
const assert = require('node:assert/strict');
(async () => {
    const b = await chromium.launch();
    const p = await b.newPage();
    const base = lib.base;
    const errors = [];
    p.on('pageerror', (e) => errors.push(e.message));
    for (const theme of lib.THEMES)
        for (const width of [390, 768, 1180, 1280, 1440]) {
            await p.setViewportSize({ width, height: 900 });
            await p.goto(base + '/');
            await p.evaluate((theme) => localStorage.setItem('theme', theme), theme);
            for (const path of ['/events', '/tv', '/videos', '/news/kansas-city', '/settings', '/']) {
                const response = await p.goto(base + path);
                assert.equal(response.status(), 200);
                assert.equal(
                    await p.evaluate(() => document.documentElement.scrollWidth > innerWidth),
                    false,
                    `${theme} ${width} ${path}`,
                );
                assert.equal(await p.locator('html').getAttribute('data-theme'), theme);
                if (lib.shotDir && path === '/events' && (width === 390 || width === 1440))
                    await p.screenshot({ path: `${lib.shotDir}/events-${width}-${theme}.png` });
            }
        }
    assert.deepEqual(errors, []);
    console.log('60 responsive/theme route checks passed.');
    await b.close();
})().catch((e) => {
    console.error(e);
    process.exit(1);
});
