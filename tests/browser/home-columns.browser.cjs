// Folded phone layouts are covered by phone-fronts.browser.cjs; these checks exercise the retained tablet/desktop controls.
// Broadsheet homepage (October 25, 2026): masthead with the forecast and live ears, Where to watch as a full-width
// band under it (swapped with the forecast on October 26), three ruled columns and a "More from today" band on
// desktop; one ordered column on phones, Where to watch first.
const { chromium } = require('playwright');
const lib = require('./lib.cjs');
const assert = require('node:assert/strict');
const base = lib.base;
const password = lib.adminPassword;

const phoneOrder = [
    'watch',
    'sports',
    'lead',
    'news',
    'opinion',
    'sports-news',
    'entertainment-news',
    'tech-news',
    'scoreboard',
    'rankings',
    'videos',
];

(async () => {
    const profiles = (await fetch(base + '/editions')).status !== 404;
    const browser = await chromium.launch();
    let admin,
        storageState = process.env.STORAGE_STATE;
    try {
        if (profiles && !storageState) {
            assert(password, 'ADMIN_PASSWORD is required to create and remove a disposable reader profile');
            admin = await lib.signInAdmin(browser);
            const profile = await lib.addProfile(admin, 'Columns', { pin: '3691' });
            storageState = await lib.openProfile(browser, profile, { path: '/', waitUntil: 'domcontentloaded' });
        }

        for (const javaScriptEnabled of [true, false]) {
            const context = await browser.newContext({ javaScriptEnabled, storageState });
            const page = await context.newPage();
            const errors = [];
            page.on('pageerror', (error) => errors.push(error.message));
            await page.goto(base + '/');
            const box = (selector) => page.locator(selector).first().boundingBox();
            for (const width of [1024, 1280, 1440, 2000]) {
                await page.setViewportSize({ width, height: 1000 });
                assert(
                    await page.evaluate(() => document.documentElement.scrollWidth <= innerWidth),
                    'overflow ' + width,
                );
                assert.equal(await page.locator('[data-home-section="digital"]').count(), 0, 'digital section absent');
                assert.equal(
                    await page.locator('.bs-front [data-home-latest-video]').count(),
                    0,
                    'latest video absent from front',
                );
                assert.equal(await page.locator('.bs-more > .bs-col--side').count(), 0, 'bs-more has no side column');
                // Every rendered section is laid out without overlapping another.
                const sections = await page.locator('.bs-home [data-home-section]:not([hidden])').evaluateAll((ns) =>
                    ns.map((n) => {
                        const r = n.getBoundingClientRect();
                        return {
                            key: n.dataset.homeSection,
                            x: r.x,
                            y: r.y + scrollY,
                            right: r.right,
                            bottom: r.bottom + scrollY,
                        };
                    }),
                );
                for (let i = 0; i < sections.length; i++)
                    for (const b of sections.slice(i + 1)) {
                        const a = sections[i];
                        assert(
                            a.right <= b.x + 1 || b.right <= a.x + 1 || a.bottom <= b.y + 1 || b.bottom <= a.y + 1,
                            `${a.key} overlaps ${b.key} at ${width}`,
                        );
                    }
                if (width >= 1024) {
                    const watch = await box('[data-home-section="watch"]');
                    const weather = await box('.bs-glance');
                    const title = await box('.bs-title');
                    const live = await box('.bs-live');
                    const lead = await box('.d-lead');
                    const tonight = await box('[data-home-section="sports"]');
                    const side = await box('.bs-front > .bs-col--side');
                    if (watch && lead && tonight) {
                        assert(watch.y + watch.height < lead.y && watch.y + watch.height < tonight.y);
                        // Where to watch: a band under the whole masthead, as wide as the page.
                        if (weather && live) {
                            assert(
                                watch.y >= Math.max(weather.y + weather.height, live.y + live.height),
                                'Watch band below',
                            );
                            assert(watch.width >= live.x + live.width - weather.x - 1, 'Watch band full width');
                        }
                    }
                    if (weather && title && live && width < 1920) {
                        assert(weather.x < title.x && title.x < live.x, 'masthead order');
                    }
                    if (tonight && lead && side) {
                        // Three columns: Today & Tonight | lead and headlines | side column (box office + episodes).
                        assert(tonight.x < lead.x && lead.x < side.x, 'front columns');
                        assert(Math.abs(tonight.y - lead.y) < 40 && Math.abs(lead.y - side.y) < 40, 'front row');
                        const sideCard = await box('.bs-front > .bs-col--side > .d-card');
                        if (sideCard) {
                            assert(
                                Math.abs(tonight.y - sideCard.y) <= 1.5,
                                `tonight (${tonight.y}) and side card (${sideCard.y}) top aligned at ${width}`,
                            );
                        }
                    }
                    assert.equal(
                        await page
                            .locator(
                                '.bs-front > .bs-col--side :is([data-home-section="forecasts"], [data-home-section="box-office"])',
                            )
                            .count(),
                        1,
                        'box office in front side column',
                    );
                    assert.equal(
                        await page.locator('.bs-front > .bs-col--side [data-home-section="tv"]').count(),
                        1,
                        'tv in front side column',
                    );
                    // Fridays: theater releases are a full-width row under the front, never in its columns.
                    assert.equal(
                        await page.locator('.bs-front [data-home-section="movies"]').count(),
                        0,
                        'theater releases not in the front columns',
                    );
                    const friday = await box('[data-home-section="movies"]');
                    const front = await box('.bs-front');
                    if (friday && front) {
                        assert(friday.y >= front.y + front.height - 1, 'Friday releases below the front');
                        assert(friday.width >= front.width - 2, 'Friday releases full width');
                    }

                    if (width >= 1920) {
                        assert(!(await page.locator('.bs-band').isVisible()), 'band hidden at 1920+');
                        assert(!(await page.locator('.bs-title__name').isVisible()), 'title name hidden at 1920+');
                        const news = await box('[data-home-section="news"]');
                        if (lead && news) {
                            assert(lead.x < news.x, 'lead and news side by side at 1920+');
                            assert(Math.abs(lead.y - news.y) < 10, 'lead and news share top offset at 1920+');
                        }
                        const sportsNews = await box('[data-home-section="sports-news"]');
                        const entertainmentNews = await box('[data-home-section="entertainment-news"]');
                        const techNews = await box('[data-home-section="tech-news"]');
                        if (sportsNews && entertainmentNews && techNews) {
                            assert(
                                Math.abs(sportsNews.y - entertainmentNews.y) < 2,
                                'sports and entertainment share top offset',
                            );
                            assert(
                                Math.abs(entertainmentNews.y - techNews.y) < 2,
                                'entertainment and tech share top offset',
                            );
                        }
                        const scoreboard = await box('[data-home-section="scoreboard"]');
                        const rankings = await box('[data-home-section="rankings"]');
                        if (scoreboard && rankings) {
                            assert(Math.abs(scoreboard.y - rankings.y) < 2, 'scoreboard and rankings share top offset');
                        }
                        const videos = await box('[data-home-section="videos"]');
                        if (videos && scoreboard) {
                            assert(videos.y > scoreboard.y, 'videos below scoreboard');
                        }
                    } else {
                        assert(await page.locator('.bs-band').isVisible());
                        const band = await box('.bs-band');
                        const scoreboard = await box('[data-home-section="scoreboard"]');
                        if (band && lead && scoreboard) {
                            assert(band.y > lead.y && scoreboard.y > band.y, 'More from today band');
                        }
                        assert(await page.locator('.bs-title__name').isVisible());
                    }
                } else {
                    // Phones: one column in the approved order, Where to watch first.
                    const ys = Object.fromEntries(sections.map((s) => [s.key, s.y]));
                    const present = phoneOrder.filter((key) => key in ys);
                    for (let i = 1; i < present.length; i++)
                        assert(
                            ys[present[i - 1]] < ys[present[i]],
                            `${present[i - 1]} before ${present[i]} at ${width}`,
                        );
                    assert(!(await page.locator('.bs-band').isVisible()));
                    const watch = await box('[data-home-section="watch"]');
                    if (await page.locator('.bs-glance').count()) assert(watch.y < (await box('.bs-glance')).y);
                    assert(!(await page.locator('.bs-title__name').isVisible()));
                }
                assert.equal(await page.locator('.bs-tonight .spf-timeline__item[hidden]').count(), 0);
            }
            assert.deepEqual(errors, []);
            await context.close();
        }
        console.log(
            'Broadsheet home: masthead ears, three front columns and More from today on desktop; phone order with Where to watch first; no overlap or overflow at 320–1440px; JS and no-JS passed.',
        );
    } finally {
        try {
            await lib.deleteProfiles();
        } finally {
            await browser.close();
        }
    }
})().catch((error) => {
    console.error(error);
    process.exit(1);
});
