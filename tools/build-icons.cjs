// Renders the PNG app icons from the vector brand mark (wwwroot/img/brand).
//
//   node tools/build-icons.cjs
//
// Outputs wwwroot/icons/newspaper-page-{192,512}.png and a maskable 512px icon whose artwork sits inside the
// central safe zone on a full-bleed background, as Android adaptive icons expect.
const { chromium } = require('playwright');
const fs = require('node:fs');
const path = require('node:path');

const web = path.resolve(__dirname, '../src/myNewspaper.Web/wwwroot');
const mark = fs.readFileSync(path.join(web, 'img/brand/mark.svg'), 'utf8');
const page = fs.readFileSync(path.join(web, 'img/brand/mark-page.svg'), 'utf8');

const tile = (size) =>
    `<html><body style="margin:0;background:transparent">` +
    mark.replace('<svg ', `<svg width="${size}" height="${size}" `) +
    `</body></html>`;
// Maskable: no rounded corners (the launcher applies its own mask); the page fills ~60% of the canvas.
const maskable = (size) =>
    `<html><body style="margin:0;width:${size}px;height:${size}px;background:#0051ba;display:grid;place-items:center">` +
    page.replace('<svg ', `<svg width="${Math.round(size * 0.52)}" height="${Math.round(size * 0.6)}" `) +
    `</body></html>`;

(async () => {
    const browser = await chromium.launch();
    try {
        const shots = [
            ['newspaper-page-192.png', 192, tile(192), true],
            ['newspaper-page-512.png', 512, tile(512), true],
            ['newspaper-page-maskable-512.png', 512, maskable(512), false],
            ['newspaper-page-180.png', 180, maskable(180), false],
        ];
        for (const [name, size, html, transparent] of shots) {
            const p = await browser.newPage({ viewport: { width: size, height: size } });
            await p.setContent(html);
            await p.screenshot({
                path: path.join(web, 'icons', name),
                omitBackground: transparent,
                clip: { x: 0, y: 0, width: size, height: size },
            });
            await p.close();
            console.log('wrote', name);
        }
    } finally {
        await browser.close();
    }
})();
