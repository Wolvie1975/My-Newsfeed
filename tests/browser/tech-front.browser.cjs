// Tech section verification (docs/tech/tech-01-hub.md through docs/tech/tech-05-weekly.md):
// phone (320, 390, 430 × light and dark), iPad (744, 820), and desktop (1024, 1280, 1440).
const { chromium } = require('playwright');
const lib = require('./lib.cjs');
const assert = require('node:assert/strict');
const path = require('node:path');
const fs = require('node:fs');

const base = lib.base;
const shots = lib.screenshots('tech-front');
const password = lib.adminPassword;

fs.mkdirSync(shots, { recursive: true });

(async () => {
    const profiles = (await fetch(base + '/editions', { headers: { Accept: 'text/html' } })).status !== 404;
    if (profiles && !password) {
        console.log('SKIP: profiles are on here; set ADMIN_PASSWORD so the check can add a throwaway profile.');
        return;
    }

    const browser = await chromium.launch();
    const errors = [];
    let admin, storageState;

    const contextFor = async (width, { scheme = 'kansas', theme = 'light' } = {}) => {
        const context = await browser.newContext({
            viewport: { width, height: 900 },
            storageState,
        });
        await context.addInitScript(
            ([s, t]) => {
                try {
                    localStorage.setItem('mynewsfeed-color-scheme', s);
                    localStorage.setItem('theme', t);
                } catch {}
            },
            [scheme, theme],
        );
        return context;
    };

    const visit = async (page, urlPath, scheme = 'kansas', theme = 'light') => {
        await page.goto(base + urlPath, { waitUntil: 'domcontentloaded' });
        await page.waitForTimeout(350);
        if (!profiles) return;
        const needsPrefSync = await page.evaluate(
            ([s, t]) =>
                document.documentElement.getAttribute('data-theme') !== t ||
                (document.documentElement.getAttribute('data-color-scheme') || 'kansas') !== s,
            [scheme, theme],
        );
        if (needsPrefSync) {
            await page.evaluate(
                ([s, t]) => {
                    window.myNewspaperPrefs.setItem('mynewsfeed-color-scheme', s);
                    window.myNewspaperPrefs.setItem('theme', t);
                },
                [scheme, theme],
            );
            await page.waitForTimeout(600);
            await page.reload({ waitUntil: 'domcontentloaded' });
            await page.waitForTimeout(350);
        }
    };

    const layout = (page, scope) =>
        page.evaluate((scope) => {
            const visible = (n) => n.offsetParent !== null || getComputedStyle(n).position === 'fixed';
            const ids = [...document.querySelectorAll('[id]')].map((n) => n.id);
            return {
                over: document.documentElement.scrollWidth - innerWidth,
                dupes: ids.filter((id, i) => ids.indexOf(id) !== i),
                small: [...document.querySelectorAll(`${scope} a, ${scope} button:not([hidden]), ${scope} select`)]
                    .filter(visible)
                    .filter(
                        (n) =>
                            !n.closest(
                                '.mx-block__title, .mx-top__title, .ef-headlines__title, .ef-lead__title, .ef-note, .spf-agate, .spf-timeline, .mx-ribbon__kicker, .yt-row__title, .tw-biggest__title, .tw-day-card__title, .tw-m-days__top, .td-howto-card__title',
                            ),
                    )
                    .filter((n) => n.getBoundingClientRect().height < 43.9)
                    .map((n) => `${n.tagName}.${n.className}(${Math.round(n.getBoundingClientRect().height)})`),
            };
        }, scope);

    try {
        if (profiles) {
            admin = await lib.signInAdmin(browser);
            const profile = await lib.addProfile(admin, 'Tech', { pin: '3691' });
            storageState = await lib.openProfile(browser, profile, { path: '/tech' });
        }

        // 1. Phone widths: 320, 390, 430 in light and dark mode
        for (const width of lib.PHONE_WIDTHS) {
            for (const theme of lib.THEMES) {
                const context = await contextFor(width, { theme });
                const page = await context.newPage();
                page.on('pageerror', (e) => errors.push(`phone ${width} ${theme}: ${e.message}`));
                await visit(page, '/tech', 'kansas', theme);

                const l = await layout(page, '.mx-phone');
                assert.ok(l.over <= 0, `/tech ${width} ${theme} overflow ${l.over}`);
                assert.deepEqual(l.dupes, [], `/tech ${width} ${theme} dupes`);
                assert.deepEqual(l.small, [], `/tech ${width} ${theme} small targets: ${l.small.join(', ')}`);

                const phoneInfo = await page.evaluate(() => {
                    const ribbon = document.querySelector('.mx-phone .mx-ribbon');
                    const tilesGrid = document.querySelector('.mx-phone .tc-brief--phone .nf-brief__tiles');
                    const tiles = [...document.querySelectorAll('.mx-phone .tc-brief--phone .nf-tile')].map((t) => ({
                        label: t.querySelector('.nf-tile__label')?.textContent.trim(),
                        cls: t.className,
                    }));
                    const topCount = document.querySelectorAll('.mx-phone .mx-top > li').length;
                    const openFold = document
                        .querySelector('.mx-phone details.mx-fold[open] .mx-fold__name')
                        ?.textContent.trim();
                    const hasTopics = Boolean(document.querySelector('.mx-phone .nf-topics'));
                    const videoRail = document.querySelector('.mx-phone .tc-video-rail');
                    return {
                        ribbonSection: ribbon?.getAttribute('data-section'),
                        ribbonBg: ribbon ? getComputedStyle(ribbon).backgroundColor : null,
                        tileCols: tilesGrid ? getComputedStyle(tilesGrid).gridTemplateColumns.split(' ').length : 0,
                        tiles,
                        topCount,
                        openFold,
                        hasTopics,
                        hasVideoRail: Boolean(videoRail),
                    };
                });

                assert.equal(phoneInfo.ribbonSection, 'tech', `/tech ${width} ${theme} ribbon section`);
                assert.equal(phoneInfo.tileCols, 2, `/tech ${width} ${theme} tiles 2x2`);
                assert.deepEqual(
                    phoneInfo.tiles.map((t) => t.label),
                    ['Technology', 'Video Games', 'Linux', 'Tech videos'],
                );
                assert.ok(phoneInfo.topCount >= 1 && phoneInfo.topCount <= 5, `/tech ${width} top count`);
                assert.equal(phoneInfo.openFold, 'Linux', `/tech ${width} Linux fold open by default`);
                assert.equal(phoneInfo.hasTopics, false, `/tech ${width} has no topics on phone`);
                assert.ok(phoneInfo.hasVideoRail, `/tech ${width} video rail`);

                await page.screenshot({
                    path: path.join(shots, `tech-phone-${width}-${theme}.png`),
                    fullPage: false,
                });
                await context.close();
            }
        }

        // 2. iPad portrait widths: 744, 820
        for (const width of [744, 820]) {
            const context = await contextFor(width, { theme: 'light' });
            const page = await context.newPage();
            page.on('pageerror', (e) => errors.push(`ipad ${width}: ${e.message}`));
            await visit(page, '/tech', 'kansas', 'light');

            const l = await layout(page, '.mx-desktop');
            assert.ok(l.over <= 0, `/tech ipad ${width} overflow ${l.over}`);
            assert.deepEqual(l.dupes, [], `/tech ipad ${width} dupes`);

            const ipadInfo = await page.evaluate(() => {
                const cols = document.querySelector('.spf-cols');
                const lead = document.querySelector('.spf-cols .ef-lead');
                return {
                    colsWidth: cols?.getBoundingClientRect().width ?? 0,
                    leadWidth: lead?.getBoundingClientRect().width ?? 0,
                };
            });
            assert.ok(
                Math.abs(ipadInfo.colsWidth - ipadInfo.leadWidth) < 2,
                `/tech ipad ${width} lead spans full width`,
            );
            await page.screenshot({ path: path.join(shots, `tech-ipad-${width}.png`), fullPage: false });
            await context.close();
        }

        // 3. Desktop widths: 1024, 1280, 1440, 2000 (light and dark at 1280)
        for (const [width, theme] of [
            [1024, 'light'],
            [1280, 'light'],
            [1280, 'dark'],
            [1440, 'light'],
            [2000, 'light'],
        ]) {
            const context = await contextFor(width, { theme });
            const page = await context.newPage();
            page.on('pageerror', (e) => errors.push(`desktop ${width} ${theme}: ${e.message}`));
            await visit(page, '/tech', 'kansas', theme);

            const l = await layout(page, '.mx-desktop');
            assert.ok(l.over <= 0, `/tech desktop ${width} overflow ${l.over}`);
            assert.deepEqual(l.dupes, [], `/tech desktop ${width} dupes`);

            const desktopInfo = await page.evaluate(() => {
                const navLinks = [...document.querySelectorAll('.site-nav--inline > a')].map((a) =>
                    a.textContent.trim(),
                );
                const activeNav = document
                    .querySelector('.site-nav--inline > a[aria-current="page"]')
                    ?.textContent.trim();
                const tabs = [...document.querySelectorAll('.page-heading .section-tabs a')].map((a) => ({
                    text: a.textContent.trim(),
                    href: a.getAttribute('href'),
                }));
                const tiles = [...document.querySelectorAll('.mx-desktop .nf-brief .nf-tile')].map((t) => ({
                    label: t.querySelector('.nf-tile__label')?.textContent.trim(),
                    color: getComputedStyle(t.querySelector('.nf-tile__value')).color,
                }));
                const desks = [...document.querySelectorAll('.mx-desktop .nf-desks .nf-desk')].map((d) => ({
                    title: d.querySelector('.ef-label')?.textContent.trim(),
                    count: d.querySelectorAll('li').length,
                    moreHref: d.querySelector('.ef-more')?.getAttribute('href'),
                    labelColor: getComputedStyle(d.querySelector('.ef-label')).color,
                }));
                const videoCount = document.querySelectorAll('.mx-desktop .tc-videos .yt-row').length;
                const hasTopics = Boolean(document.querySelector('.mx-desktop .nf-topics'));
                return { navLinks, activeNav, tabs, tiles, desks, videoCount, hasTopics };
            });

            assert.deepEqual(desktopInfo.navLinks, [
                'Home',
                'News',
                'Sports',
                'Entertainment',
                'Tech',
                'Where to watch',
                'YouTube',
            ]);
            assert.equal(desktopInfo.activeNav, 'Tech');
            assert.deepEqual(
                desktopInfo.tabs.map((t) => t.text),
                ['Tech home', 'All headlines', 'Technology', 'Video Games', 'Linux', 'Videos'],
            );
            assert.equal(desktopInfo.tiles.length, 4);
            // Technology (purple), Video Games (amber), Linux (teal) must have distinct colors in light & dark mode
            assert.notEqual(desktopInfo.tiles[0].color, desktopInfo.tiles[1].color, 'Tech vs Games tile color');
            assert.notEqual(desktopInfo.tiles[1].color, desktopInfo.tiles[2].color, 'Games vs Linux tile color');
            assert.notEqual(desktopInfo.tiles[0].color, desktopInfo.tiles[2].color, 'Tech vs Linux tile color');

            assert.deepEqual(
                desktopInfo.desks.map((d) => ({ title: d.title, count: d.count, moreHref: d.moreHref })),
                [
                    { title: 'Technology', count: 4, moreHref: 'tech/technology' },
                    { title: 'Video Games', count: 4, moreHref: 'tech/video-games' },
                    { title: 'Linux', count: 4, moreHref: 'tech/linux' },
                ],
            );
            assert.notEqual(desktopInfo.desks[0].labelColor, desktopInfo.desks[1].labelColor);
            assert.notEqual(desktopInfo.desks[1].labelColor, desktopInfo.desks[2].labelColor);
            assert.equal(desktopInfo.videoCount, 3);
            assert.equal(desktopInfo.hasTopics, false, '/tech has no topics on desktop');
            assert(await page.locator('.tc-front ol.spf-timeline').isVisible(), 'Tech day timeline visible');
            assert.equal(await page.locator('.tc-front .spf-agate').count(), 0, 'Tech no spf-agate');

            if (width >= 1920) {
                const wideCheck = await page.evaluate(() => {
                    const front = document.querySelector('.tc-front');
                    const cols = front ? getComputedStyle(front).gridTemplateColumns.split(' ').length : 0;
                    const latest = document.querySelector('.tc-latest-grid');
                    const latestHidden = !latest || getComputedStyle(latest).display === 'none';
                    const mainBox = document.querySelector('.spf-main')?.getBoundingClientRect();
                    const desksBox = document.querySelector('.nf-desks')?.getBoundingClientRect();
                    const aligned = mainBox && desksBox ? Math.abs(mainBox.top - desksBox.top) < 2 : false;
                    return { cols, latestHidden, aligned };
                });
                assert.equal(wideCheck.cols, 4, 'Tech front has 4 columns at 1920+');
                assert.equal(wideCheck.latestHidden, true, 'Latest headlines hidden at 1920+');
                assert.equal(wideCheck.aligned, true, 'Desks share top offset with main column at 1920+');
            }

            await page.screenshot({
                path: path.join(shots, `tech-desktop-${width}-${theme}.png`),
                fullPage: false,
            });

            if (width === 1280 && theme === 'light') {
                // Check /tech/news (Part 3: All Tech headlines + Jump to by hour + Today by desk)
                await visit(page, '/tech/news', 'kansas', 'light');
                const pills = await page
                    .locator('.hn-cats a')
                    .evaluateAll((els) =>
                        els.map((a) => ({ text: a.textContent.trim(), href: a.getAttribute('href') })),
                    );
                assert.deepEqual(pills, [
                    { text: 'Technology', href: 'tech/technology' },
                    { text: 'Video Games', href: 'tech/video-games' },
                    { text: 'Linux', href: 'tech/linux' },
                ]);
                const jumpDesks = await page.locator('.hc-jump__cat').evaluateAll((els) =>
                    els.map((a) => ({
                        label: a.querySelector('span')?.textContent.trim(),
                        href: a.getAttribute('href'),
                        borderLeftColor: getComputedStyle(a).borderLeftColor,
                    })),
                );
                assert.equal(jumpDesks.length, 3, 'Today by desk has 3 desks');
                assert.deepEqual(
                    jumpDesks.map((d) => d.label),
                    ['Technology', 'Video Games', 'Linux'],
                );
                assert.notEqual(jumpDesks[0].borderLeftColor, jumpDesks[1].borderLeftColor);
                assert.notEqual(jumpDesks[1].borderLeftColor, jumpDesks[2].borderLeftColor);

                // Filter /tech/news by a desk category id
                const firstDeskHref = jumpDesks[0].href;
                await visit(page, '/' + firstDeskHref, 'kansas', 'light');
                const activeDeskChip = await page.locator('.hc-jump__cat[aria-current="page"] span').textContent();
                assert.equal(activeDeskChip?.trim(), 'Technology');

                // Check /videos?categories=tech,video-games
                await visit(page, '/videos?categories=tech,video-games', 'kansas', 'light');
                const toolbarText = await page.locator('.count--toolbar').textContent();
                assert.ok(toolbarText.includes('Tech + Video Games'), `Videos toolbar: ${toolbarText}`);

                // Check /news/topics/follow?hub=tech when profile is open
                if (profiles) {
                    await visit(page, '/news/topics/follow?hub=tech', 'kansas', 'light');
                    const selectedHub = await page.locator('#tp-hub').inputValue();
                    assert.equal(selectedHub, 'tech', 'TopicFollow pre-selects tech hub');
                }
            }

            await context.close();
        }

        // 4. Desk pages (/tech/technology, /tech/video-games, /tech/linux) + /tech/week across phone, iPad, desktop
        for (const width of lib.PHONE_WIDTHS) {
            for (const theme of lib.THEMES) {
                const context = await contextFor(width, { theme });
                const page = await context.newPage();
                page.on('pageerror', (e) => errors.push(`desk phone ${width} ${theme}: ${e.message}`));

                const ribbonColors = {};
                for (const [slug, expectedDesk] of [
                    ['technology', 'technology'],
                    ['video-games', 'video-games'],
                    ['linux', 'linux'],
                ]) {
                    await visit(page, `/tech/${slug}`, 'kansas', theme);
                    const l = await layout(page, '.mx-phone');
                    assert.ok(l.over <= 0, `/tech/${slug} ${width} ${theme} overflow ${l.over}`);
                    assert.deepEqual(l.dupes, [], `/tech/${slug} ${width} ${theme} dupes`);
                    assert.deepEqual(
                        l.small,
                        [],
                        `/tech/${slug} ${width} ${theme} small targets: ${l.small.join(', ')}`,
                    );

                    const info = await page.evaluate(() => {
                        const r = document.querySelector('.mx-phone .mx-ribbon');
                        const pills = document.querySelector('.mx-phone .td-pills--phone');
                        return {
                            deskAttr: r?.getAttribute('data-desk') ?? null,
                            bg: r ? getComputedStyle(r).backgroundColor : null,
                            pillCount: pills ? pills.querySelectorAll('a.td-pill').length : 0,
                        };
                    });
                    assert.equal(info.deskAttr, expectedDesk, `/tech/${slug} data-desk`);
                    assert.ok(info.pillCount >= 5, `/tech/${slug} topic pills count`);
                    ribbonColors[slug] = info.bg;

                    if (width === 390) {
                        await page.screenshot({
                            path: path.join(shots, `tech-desk-${slug}-phone-${width}-${theme}.png`),
                            fullPage: false,
                        });
                    }
                }
                assert.notEqual(ribbonColors['technology'], ribbonColors['video-games'], 'Phone ribbon Tech vs Games');
                assert.notEqual(ribbonColors['video-games'], ribbonColors['linux'], 'Phone ribbon Games vs Linux');
                assert.notEqual(ribbonColors['technology'], ribbonColors['linux'], 'Phone ribbon Tech vs Linux');

                // Check /tech/week and /tech/week/linux on phone
                for (const weekUrl of ['/tech/week', '/tech/week/linux']) {
                    await visit(page, weekUrl, 'kansas', theme);
                    const l = await layout(page, '.mx-phone');
                    assert.ok(l.over <= 0, `${weekUrl} ${width} ${theme} overflow ${l.over}`);
                    assert.deepEqual(l.dupes, [], `${weekUrl} ${width} ${theme} dupes`);
                    assert.deepEqual(l.small, [], `${weekUrl} ${width} ${theme} small targets: ${l.small.join(', ')}`);
                }
                if (width === 390) {
                    await page.screenshot({
                        path: path.join(shots, `tech-week-phone-${width}-${theme}.png`),
                        fullPage: false,
                    });
                }

                await context.close();
            }
        }

        // Desk & Week on iPad (744, 820) and Desktop (1280, 1440)
        for (const width of [744, 820, 1280, 1440]) {
            const context = await contextFor(width, { theme: 'light' });
            const page = await context.newPage();
            page.on('pageerror', (e) => errors.push(`desk/week ${width}: ${e.message}`));

            for (const slug of ['technology', 'video-games', 'linux']) {
                await visit(page, `/tech/${slug}`, 'kansas', 'light');
                const l = await layout(page, '.mx-desktop');
                assert.ok(l.over <= 0, `/tech/${slug} ${width} overflow ${l.over}`);
                assert.deepEqual(l.dupes, [], `/tech/${slug} ${width} dupes`);

                if (width === 1280) {
                    const deskCheck = await page.evaluate(() => {
                        const allPillText = document
                            .querySelector('.mx-desktop .td-pills a.td-pill[aria-current="page"]')
                            ?.textContent.trim();
                        const sourceCounts = [...document.querySelectorAll('.mx-desktop .td-source-row__count')].map(
                            (n) => Number(n.textContent.trim()),
                        );
                        return { allPillText, sourceCounts };
                    });
                    assert.ok(deskCheck.allPillText?.startsWith('All'), `/tech/${slug} default All pill`);

                    await page.screenshot({
                        path: path.join(shots, `tech-desk-${slug}-desktop-${width}.png`),
                        fullPage: false,
                    });
                }
            }

            if (width === 1280) {
                // Verify ?topic= filtering on /tech/technology?topic=ai
                await visit(page, '/tech/technology?topic=ai', 'kansas', 'light');
                const activeTopic = await page
                    .locator('.mx-desktop .td-pills a.td-pill[aria-current="page"]')
                    .textContent();
                assert.ok(activeTopic?.includes('AI'), `Active topic pill: ${activeTopic}`);

                // Verify /tech/linux/releases renders cleanly even before Part 4 tables exist
                await visit(page, '/tech/linux/releases', 'kansas', 'light');
                const relLayout = await layout(page, 'main');
                assert.ok(relLayout.over <= 0, `/tech/linux/releases overflow ${relLayout.over}`);
            }

            // Check /tech/week and /tech/week/linux
            for (const weekUrl of ['/tech/week', '/tech/week/linux']) {
                await visit(page, weekUrl, 'kansas', 'light');
                const l = await layout(page, '.mx-desktop');
                assert.ok(l.over <= 0, `${weekUrl} ${width} overflow ${l.over}`);
                assert.deepEqual(l.dupes, [], `${weekUrl} ${width} dupes`);

                if (width === 1280 && weekUrl === '/tech/week') {
                    const weekInfo = await page.evaluate(() => {
                        const tiles = [...document.querySelectorAll('.mx-desktop .tw-tiles .nf-tile')].map((t) => ({
                            label: t.querySelector('.nf-tile__label')?.textContent.trim(),
                            val: t.querySelector('.nf-tile__value')?.textContent.trim(),
                        }));
                        const dayCounts = [
                            ...document.querySelectorAll('.mx-desktop .tw-day-card[data-day-count]'),
                        ].map((n) => Number(n.getAttribute('data-day-count')));
                        const ranks = [...document.querySelectorAll('.mx-desktop .tw-rank')].map((n) =>
                            Number(n.textContent.trim()),
                        );
                        return { tiles, dayCounts, ranks };
                    });
                    const storiesTile = Number((weekInfo.tiles[0]?.val ?? '0').replace(/,/g, ''));
                    const daySum = weekInfo.dayCounts.reduce((a, b) => a + b, 0);
                    assert.equal(storiesTile, daySum, 'Stories tile equals sum of 7 day counts');
                    assert.ok(
                        weekInfo.ranks.every((r, i) => r === i + 1) && weekInfo.ranks.length <= 5,
                        `Biggest stories ranked 1..N: ${weekInfo.ranks.join(',')}`,
                    );

                    await page.screenshot({
                        path: path.join(shots, `tech-week-desktop-${width}.png`),
                        fullPage: false,
                    });
                }
            }

            if (width === 1280) {
                // Verify ?week= older than 12 weeks returns 404
                const oldRes = await page.goto(base + '/tech/week?week=2020-01-05', {
                    waitUntil: 'domcontentloaded',
                });
                assert.equal(oldRes?.status(), 404, 'Week older than 12 weeks returns 404');
            }

            await context.close();
        }

        assert.deepEqual(errors, [], `Page errors: ${errors.join('; ')}`);
        console.log('PASS tech-front.browser.cjs');
    } finally {
        await lib.deleteProfiles();
        await browser.close();
    }
})();
