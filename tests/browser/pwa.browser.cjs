const assert = require('node:assert/strict');
const { chromium } = require('playwright');
const base = process.env.BASE_URL || 'https://mynewsfeed-prod.tailbf31bc.ts.net';
// Service-worker caches and geolocation only exist in secure contexts (HTTPS or localhost).
{
    const { protocol, hostname } = new URL(base);
    if (protocol !== 'https:' && !['localhost', '127.0.0.1', '[::1]'].includes(hostname)) {
        console.log(`SKIP: needs a secure context; ${base} is plain HTTP on a non-localhost host.`);
        process.exit(0);
    }
}
(async () => {
    const browser = await chromium.launch({
        args: process.env.PWA_HOST_IP
            ? [`--host-resolver-rules=MAP ${new URL(base).hostname} ${process.env.PWA_HOST_IP}`]
            : [],
    });
    try {
        const page = await browser.newPage();
        await page.goto(base + '/offline.html');
        await page.evaluate(async () => {
            await caches.open('mynewsfeed-pwa-v6');
            await caches.open('unrelated-cache');
        });
        await page.goto(base);
        await page.evaluate(() => navigator.serviceWorker.ready);
        await page.waitForFunction(() => !!navigator.serviceWorker.controller);
        const manifest = await page.evaluate(async () => (await fetch('/manifest.webmanifest')).json());
        assert.equal(manifest.id, '/');
        assert.equal(manifest.display, 'standalone');
        for (const icon of manifest.icons) {
            assert(icon.src.includes('newspaper-front-'));
            const dimensions = await page.evaluate(async (src) => {
                const img = new Image();
                img.src = src;
                await img.decode();
                return `${img.naturalWidth}x${img.naturalHeight}`;
            }, icon.src);
            assert.equal(dimensions, icon.sizes);
        }
        assert(manifest.icons.some((icon) => icon.purpose === 'maskable'));
        const cdp = await page.context().newCDPSession(page);
        await cdp.send('Page.enable');
        assert.deepEqual((await cdp.send('Page.getInstallabilityErrors')).installabilityErrors, []);
        const keys = await page.evaluate(() => caches.keys());
        assert(keys.includes('mynewsfeed-pwa-v7'));
        assert(!keys.includes('mynewsfeed-pwa-v6'));
        assert(keys.includes('unrelated-cache'));
        await page.context().setOffline(true);
        await page.goto(base + '/news');
        assert.equal(await page.locator('h1').innerText(), 'myNewspaper can’t be reached');
        assert((await page.locator('img').getAttribute('src')).includes('/img/brand/mark.svg'));
        console.log(
            'PWA icon dimensions, maskable icon, identity, installability, service worker cache and offline screen passed.',
        );
    } finally {
        await browser.close();
    }
})().catch((error) => {
    console.error(error);
    process.exit(1);
});
