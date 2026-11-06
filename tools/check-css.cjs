// The CSS guard (docs/coding-standards.md, CSS; guardrails table). Fails on:
//
//   1. a media-query width that isn't a registered breakpoint (744, 1024, 1920, 2560 and their max-width partners
//      743, 1023, 1919, 2559) or listed in `oneOffWidths` below with the reason it exists;
//   2. the visually-hidden clip pattern (`clip: rect(…)` or `clip-path: inset(50%)`) outside `.pub .sr-only`, except
//      the width-dependent rules listed in `widthDependentHiding` (text hidden at some widths and shown at others);
//   3. a class prefix (the part of a class name before the first - or _) that isn't in the prefix table or the frozen
//      list of older prefixes, and any `admin-` class (Admin uses `adm-`).
//
//   node tools/check-css.cjs            check every tracked stylesheet under src/ (not wwwroot/lib)
//   node tools/check-css.cjs --list     print each file's media widths, clip rules and prefixes instead
//
// To add a breakpoint, a hidden-at-some-widths rule or a feature prefix, add it to the lists here (and the prefix
// table in docs/coding-standards.md) with its reason, in the same change.
const { execFileSync } = require('node:child_process');
const fs = require('node:fs');
const path = require('node:path');

const root = path.resolve(__dirname, '..');

const breakpoints = new Set([
    'min:744',
    'max:743',
    'min:1024',
    'max:1023',
    'min:1920',
    'max:1919',
    'min:2560',
    'max:2559',
]);

// Widths in use before the standard (November 5, 2026), kept because folding them into the breakpoints above would
// change what people see at in-between sizes. Fold one when its rule is next edited, and remove it here.
const oneOffWidths = {
    'max:359':
        'phones narrower than 360px (320px class): smaller type and tighter controls so headings and buttons fit',
    'max:360': 'Watch search row on 320–360px phones',
    'max:374': 'header actions stay 44px and the date stays readable under 375px',
    'max:380': 'Home watch search row on narrow phones',
    'max:400': 'narrow phones and large system text: section select label, TV agate line',
    'max:430': 'News front filters on phones up to 430px, the widest common phone',
    'max:479': 'narrow phones: header clock on its own row, colour scheme picker in one column',
    'max:480': 'soccer stats panel stacks on narrow phones',
    'max:550': 'Watch detail and Settings order rows stack',
    'max:600': 'phones vs 7-inch tablets (600px portrait): box office, Watch page, Home choice, Admin category list',
    'max:650': 'movie grid drops to two columns',
    'max:699': 'section hub, Watch results and Load more stack below 700px',
    'min:700': 'paper top and video feature go two-column from 700px',
    'max:759': 'scoreboard toolbar and grid stack below 760px',
    'max:900': 'Admin layout on narrow screens',
    'min:900': 'feature articles use the wide image layout from 900px',
    'min:1000': 'box score team grids side by side from 1000px',
    'max:1100': 'movie grid drops to three columns',
    'min:1150': 'News hub front grid from 1150px',
    'max:1179': 'headline card category bar on tablets and small laptops (pairs with min 1180)',
    'min:1180': 'results and scoreboard show real table rows from 1180px',
    'max:1279': 'seven navigation destinations need the dropdown on smaller laptops (pairs with min 1280)',
    'min:1280': 'inline navigation, brand wordmark, Home lead image and paper support layout from 1280px',
    'min:1440': 'wider page and three video columns from 1440px',
    'min:1600': 'header clock and the scoreboard side rail from 1600px',
    'min:1680': 'four feed columns and a larger lead title from 1680px',
    'min:3200': '4K at 100% and TVs: the 2560 layout, 25% larger',
};

// Text hidden at some widths and shown at others, so it can't carry .sr-only in markup. file|selector → reason.
const widthDependentHiding = {
    'src/myNewspaper.Web/wwwroot/broadsheet.css|.pub .bs-watch__search label':
        'Home search label, shown again on desktop',
    'src/myNewspaper.Web/wwwroot/broadsheet.css|.pub .bs-title__date': 'masthead date, hidden in the compact header',
    'src/myNewspaper.Web/wwwroot/broadsheet.css|.pub .saved-link__label':
        'Saved label, icon only in the compact header',
    'src/myNewspaper.Web/wwwroot/sections.css|.pub .dg-bar__label': 'Digital source label, shown on desktop',
    'src/myNewspaper.Web/wwwroot/sections.css|.pub .sb-toolbar__filter label': 'scoreboard filter labels on phones',
    'src/myNewspaper.Shared/wwwroot/public.css|.pub .settings-link__label': 'Settings label, icon only below 1024px',
    'src/myNewspaper.Shared/wwwroot/public.css|.pub .watch-shelf .watch-match':
        'service match line, hidden only in shelves',
};

// The prefix table in docs/coding-standards.md.
const registeredPrefixes = [
    'pub',
    'mx',
    'd',
    'bs',
    'tab',
    'sb',
    'td',
    'tc',
    'tw',
    'spf',
    'ef',
    'hc',
    'nf',
    'yt',
    'tp',
    'mv',
    'set',
    'bo',
    'tv',
    'tvc',
    'dg',
    'rk',
    'adm',
    'cv',
    'nw',
    'is',
    'has',
    'sr',
];

// Prefixes already in use on November 5, 2026, frozen: don't add to this list; give a new feature its own prefix in
// the table instead. Bootstrap's own classes (used by Admin and the public subset) are here too.
const olderPrefixes = [
    'agenda',
    'alert',
    'article',
    'baseball',
    'basketball',
    'big12',
    'blazor',
    'body',
    'box',
    'boxoffice',
    'brand',
    'btn',
    'cat',
    'category',
    'catselect',
    'championship',
    'channel',
    'chip',
    'chips',
    'cl',
    'college',
    'components',
    'content',
    'count',
    'danger',
    'darker',
    'daylabel',
    'desktop',
    'digital',
    'dismiss',
    'edition',
    'empty',
    'event',
    'eyebrow',
    'f1',
    'favorite',
    'feed',
    'filters',
    'football',
    'form',
    'front',
    'game',
    'games',
    'group',
    'groups',
    'header',
    'hn',
    'home',
    'hub',
    'icon',
    'invalid',
    'lc',
    'league',
    'links',
    'loadmore',
    'loc',
    'local',
    'mark',
    'mls',
    'mobile',
    'more',
    'movie',
    'movies',
    'name',
    'nav',
    'neutral',
    'news',
    'nhl',
    'nowrap',
    'num',
    'page',
    'paper',
    'personal',
    'ph',
    'picker',
    'pin',
    'play',
    'poll',
    'pubdate',
    'publication',
    'ranking',
    'rankings',
    'release',
    'requests',
    'result',
    'results',
    'rq',
    'save',
    'saved',
    'scheme',
    'score',
    'scoreboard',
    'search',
    'sec',
    'section',
    'settings',
    'setup',
    'share',
    'site',
    'soccer',
    'sports',
    'src',
    'st',
    'standing',
    'standings',
    'summary',
    'sv',
    'table',
    'team',
    'text',
    'theater',
    'theme',
    'thumb',
    'title',
    'tracked',
    'tt',
    'updated',
    'valid',
    'validation',
    'vbody',
    'vfeed',
    'video',
    'videos',
    'vmeta',
    'vthumb',
    'vtitle',
    'watch',
    'weather',
    'when',
];

function trackedStylesheets() {
    return execFileSync('git', ['ls-files', '-z', 'src/*.css'], { cwd: root })
        .toString()
        .split('\0')
        .filter((f) => f && !f.includes('/wwwroot/lib/'));
}

// Comments, strings and url(…) blanked out (newlines kept, so offsets still give line numbers).
function strip(text) {
    const blank = (m) => m.replace(/[^\n]/g, ' ');
    return text
        .replace(/\/\*[\s\S]*?\*\//g, blank)
        .replace(/url\([^)]*\)/g, blank)
        .replace(/"(?:[^"\\\n]|\\.)*"|'(?:[^'\\\n]|\\.)*'/g, blank);
}

// Walks the blocks of a stylesheet: yields { prelude, body, line } for every rule and at-rule, innermost first.
function* blocks(text) {
    const stack = [];
    let start = 0;
    for (let i = 0; i < text.length; i++) {
        const c = text[i];
        if (c === '{') {
            stack.push({ prelude: text.slice(start, i).trim(), open: i, preludeAt: start });
            start = i + 1;
        } else if (c === '}') {
            const b = stack.pop();
            if (b) {
                const at =
                    b.preludeAt +
                    (text.slice(b.preludeAt, b.open).length - text.slice(b.preludeAt, b.open).trimStart().length);
                yield {
                    prelude: b.prelude,
                    body: text.slice(b.open + 1, i),
                    line: text.slice(0, at).split('\n').length,
                };
            }
            start = i + 1;
        } else if (c === ';') {
            // A selector never contains ;, so the next prelude starts after the last declaration.
            start = i + 1;
        }
    }
}

function inspect(file) {
    const text = strip(fs.readFileSync(path.join(root, file), 'utf8'));
    const widths = [];
    const clips = [];
    const classes = [];
    for (const { prelude, body, line } of blocks(text)) {
        if (prelude.startsWith('@media')) {
            for (const m of prelude.matchAll(/\((min|max)-width:\s*([\d.]+)(px|em|rem)\)/g))
                widths.push({ key: `${m[1]}:${m[2]}${m[3] === 'px' ? '' : m[3]}`, line });
            continue;
        }
        if (prelude.startsWith('@')) continue;
        // Declarations only: the part of the body outside nested blocks.
        const own = body.replace(/\{[^{}]*\}/g, '');
        if (/(^|[;\s])clip\s*:\s*rect\(|clip-path\s*:\s*inset\(\s*50%\s*\)/.test(own))
            for (const selector of splitSelectors(prelude)) clips.push({ selector, line });
        const selectors = prelude.replace(/\[[^\]]*\]/g, '');
        for (const m of selectors.matchAll(/(?<![\w-])\.(-?[_a-zA-Z][\w-]*)/g)) classes.push({ name: m[1], line });
    }
    return { widths, clips, classes };
}

const prefixOf = (name) => name.split(/[-_]/)[0];

// Splits a selector list at its top-level commas (not the ones inside :is(…), :not(…) or :where(…)).
function splitSelectors(list) {
    const out = [];
    let depth = 0;
    let start = 0;
    for (let i = 0; i < list.length; i++) {
        if (list[i] === '(') depth++;
        else if (list[i] === ')') depth--;
        else if (list[i] === ',' && depth === 0) {
            out.push(list.slice(start, i));
            start = i + 1;
        }
    }
    out.push(list.slice(start));
    return out.map((s) => s.trim().replace(/\s+/g, ' '));
}

// A width-dependent rule is allowed when the selector ends with the listed one (theme wrappers such as
// :root:is([data-color-scheme], …) in front don't matter).
const hidingKey = (file, selector) =>
    Object.keys(widthDependentHiding).find((key) => {
        const [f, s] = key.split('|');
        return f === file && (selector === s || selector.endsWith(' ' + s));
    });

function main() {
    const known = new Set([...registeredPrefixes, ...olderPrefixes]);
    const problems = [];
    const listing = process.argv.includes('--list');
    // What the lists are used for, so an entry left behind after its rule was folded or removed is reported too.
    const used = { widths: new Set(), hiding: new Set(), prefixes: new Set() };
    for (const file of trackedStylesheets()) {
        const { widths, clips, classes } = inspect(file);
        if (listing) {
            console.log(`${file}\n  widths: ${[...new Set(widths.map((w) => w.key))].sort().join(' ')}`);
            console.log(`  clip rules: ${clips.map((c) => c.selector).join(' | ') || '-'}`);
            console.log(`  prefixes: ${[...new Set(classes.map((c) => prefixOf(c.name)))].sort().join(' ')}`);
            continue;
        }
        for (const w of widths) used.widths.add(w.key);
        for (const c of classes) used.prefixes.add(prefixOf(c.name));
        for (const w of widths)
            if (!breakpoints.has(w.key) && !oneOffWidths[w.key])
                problems.push(
                    `${file}:${w.line}: media width ${w.key.replace(':', '-width: ')}px isn't a breakpoint (744, 1024, 1920, 2560); use one, or register it in tools/check-css.cjs with its reason`,
                );
        for (const c of clips) {
            const sr = c.selector === '.pub .sr-only' && file === 'src/myNewspaper.Shared/wwwroot/public.css';
            const key = hidingKey(file, c.selector);
            if (key) used.hiding.add(key);
            if (!sr && !key)
                problems.push(
                    `${file}:${c.line}: visually-hidden clip pattern on "${c.selector}"; put class="sr-only" on the element (Admin: visually-hidden)`,
                );
        }
        for (const c of classes) {
            if (/^admin-/.test(c.name))
                problems.push(`${file}:${c.line}: class .${c.name}: Admin styles use the adm- prefix`);
            else if (!known.has(prefixOf(c.name)))
                problems.push(
                    `${file}:${c.line}: class .${c.name}: prefix "${prefixOf(c.name)}" isn't registered; pick a feature prefix and add it to the table in docs/coding-standards.md and to registeredPrefixes`,
                );
        }
    }
    if (listing) return 0;
    for (const key of Object.keys(oneOffWidths))
        if (!used.widths.has(key))
            problems.push(`tools/check-css.cjs: oneOffWidths has ${key}, no longer used; remove it`);
    for (const key of Object.keys(widthDependentHiding))
        if (!used.hiding.has(key))
            problems.push(`tools/check-css.cjs: widthDependentHiding has ${key}, no longer used; remove it`);
    for (const prefix of olderPrefixes)
        if (!used.prefixes.has(prefix))
            problems.push(`tools/check-css.cjs: olderPrefixes has "${prefix}", no longer used; remove it`);
    for (const p of [...new Set(problems)]) console.log(p);
    console.log(problems.length ? `check-css: ${new Set(problems).size} problem(s)` : 'check-css: ok');
    return problems.length ? 1 : 0;
}

if (require.main === module) process.exitCode = main();
module.exports = { inspect, trackedStylesheets, prefixOf, registeredPrefixes };
