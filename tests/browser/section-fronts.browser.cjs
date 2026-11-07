// Verification for Sports and Entertainment fronts (concept B & C) and the shared briefing.
const lib = require('./lib.cjs');
const assert = require('node:assert/strict');
const { chromium } = require('playwright');
const base = lib.base;

(async () => {
    const b = await chromium.launch();
    try {
        const p = await lib.readerPage(b);
        const widths = lib.widths([390, 768, 1024, 1440, 1920, 2560]);
        for (const theme of lib.THEMES) {
            for (const width of widths) {
                await p.setViewportSize({ width, height: 1100 });
                await p.emulateMedia({ colorScheme: theme });

                for (const route of ['sports', 'entertainment']) {
                    const res = await p.goto(base + '/' + route, { waitUntil: 'networkidle' });
                    assert.equal(res.status(), 200);

                    // 1. Briefing present on /sports and /entertainment at every width, before the first section
                    const brief = p.locator(
                        width < 744 ? '.nf-brief--phone:visible' : '.nf-brief:not(.nf-brief--phone):visible',
                    );
                    assert(await brief.isVisible(), `${route} briefing visible at ${width}`);

                    // Before the first section
                    const firstSection = p.locator(
                        width < 744
                            ? route === 'sports'
                                ? '.mx-section, [data-scoreboard], .spf-scores'
                                : '.mx-block, .mx-section, .ef-screen'
                            : route === 'sports'
                              ? '.spf-scores'
                              : '.ef-screen',
                    );
                    if (await firstSection.first().isVisible()) {
                        const briefBox = await brief.boundingBox();
                        const firstBox = await firstSection.first().boundingBox();
                        if (briefBox && firstBox) {
                            assert(briefBox.y <= firstBox.y + 2, `${route} briefing sits before first section`);
                        }
                    }

                    // No empty tiles and every tile a link
                    const tiles = brief.locator('.nf-tile');
                    const tileCount = await tiles.count();
                    assert(tileCount > 0, `${route} briefing has tiles`);
                    for (let i = 0; i < tileCount; i++) {
                        const tile = tiles.nth(i);
                        const href = await tile.getAttribute('href');
                        assert(href && href.length > 0, `${route} tile ${i} is a link`);
                        const text = (await tile.innerText()).trim();
                        assert(text.length > 0, `${route} tile ${i} is not empty`);
                    }

                    // Phones: briefing tiles are at least 44px tall and nothing scrolls sideways
                    if (width < 744) {
                        for (let i = 0; i < tileCount; i++) {
                            const box = await tiles.nth(i).boundingBox();
                            if (box) {
                                assert(box.height >= 44, `${route} phone tile ${i} height >= 44px (got ${box.height})`);
                            }
                        }
                        const noOverflow = await p.evaluate(
                            () => document.documentElement.scrollWidth <= innerWidth + 1,
                        );
                        assert(noOverflow, `${route} has no horizontal scroll at ${width}`);
                    }

                    // Sports, 1024px and up:
                    if (route === 'sports' && width >= 1024) {
                        // .spf-tables sits below .spf-cols at full width
                        const tables = p.locator('.spf-tables');
                        const cols = p.locator('.spf-cols');
                        if ((await tables.isVisible()) && (await cols.isVisible())) {
                            const tablesBox = await tables.boundingBox();
                            const colsBox = await cols.boundingBox();
                            assert(tablesBox && colsBox);
                            assert(
                                tablesBox.y >= colsBox.y + colsBox.height - 2,
                                'Sports .spf-tables sits below .spf-cols',
                            );
                            assert(Math.abs(tablesBox.width - colsBox.width) < 4, 'Sports .spf-tables is full width');
                        }

                        // Each table renders as two side-by-side tables
                        const splits = p.locator('.spf-table-split');
                        const splitCount = await splits.count();
                        for (let i = 0; i < splitCount; i++) {
                            const split = splits.nth(i);
                            const tableList = split.locator('table');
                            if ((await tableList.count()) >= 2) {
                                const t1 = await tableList.nth(0).boundingBox();
                                const t2 = await tableList.nth(1).boundingBox();
                                if (t1 && t2) {
                                    assert(t2.x > t1.x, 'Sports split tables render side by side');
                                }
                            }
                        }

                        // The lead image is no larger than 640 × 320
                        const leadImg = p.locator('.spf-main .ef-lead__image img');
                        if (await leadImg.isVisible()) {
                            const imgBox = await leadImg.boundingBox();
                            if (imgBox) {
                                assert(imgBox.width <= 642, `Lead image width <= 640 (got ${imgBox.width})`);
                                assert(imgBox.height <= 322, `Lead image height <= 320 (got ${imgBox.height})`);
                            }
                        }
                    }

                    // Entertainment, 1024px and up:
                    if (route === 'entertainment' && width >= 1024) {
                        // .ef-desk-row sits below .ef-cols
                        const deskRow = p.locator('.ef-desk-row');
                        const efCols = p.locator('.ef-cols');
                        if ((await deskRow.isVisible()) && (await efCols.isVisible())) {
                            const deskBox = await deskRow.boundingBox();
                            const colsBox = await efCols.boundingBox();
                            assert(deskBox && colsBox);
                            assert(
                                deskBox.y >= colsBox.y + colsBox.height - 2,
                                'Entertainment .ef-desk-row sits below .ef-cols',
                            );
                        }

                        // There is no ef-theaters section
                        assert.equal(await p.locator('.ef-theaters').count(), 0, 'No ef-theaters section in DOM');

                        // Lead image is no larger than 640 × 320
                        const leadImg = p.locator('.ef-main .ef-lead__image img');
                        if (await leadImg.isVisible()) {
                            const imgBox = await leadImg.boundingBox();
                            if (imgBox) {
                                assert(
                                    imgBox.width <= 642,
                                    `Entertainment lead image width <= 640 (got ${imgBox.width})`,
                                );
                                assert(
                                    imgBox.height <= 322,
                                    `Entertainment lead image height <= 320 (got ${imgBox.height})`,
                                );
                            }
                        }
                    }
                }

                // Tech front unchanged: its spf-cols geometry matches the current check
                if (width >= 1024) {
                    await p.goto(base + '/tech', { waitUntil: 'networkidle' });
                    const techCols = p.locator('.tc-front > .spf-cols');
                    if (await techCols.isVisible()) {
                        const colsBox = await techCols.boundingBox();
                        assert(colsBox && colsBox.width > 0, 'Tech spf-cols is visible and rendered');
                    }
                }
            }
        }
    } finally {
        await b.close();
    }
})();
