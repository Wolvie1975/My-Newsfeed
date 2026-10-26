// Shared WCAG AA text-contrast audit (used by scheme-contrast and admin-layout). Pass it to page.evaluate: it returns
// every visible text element below 4.5:1 (3:1 for 24px+, or 18.66px+ bold) against the colour actually behind it.
// Text over pictures and visually hidden text are skipped.
function audit() {
    const parse = (c) => {
        const m = c.match(/rgba?\(([^)]+)\)/);
        if (!m) return null;
        const [r, g, b, a = '1'] = m[1].split(/[\s,/]+/).filter(Boolean);
        return [Number(r), Number(g), Number(b), Number(a)];
    };
    const blend = (top, under) => {
        const a = top[3];
        return [0, 1, 2].map((i) => top[i] * a + under[i] * (1 - a)).concat(1);
    };
    const lum = ([r, g, b]) => {
        const f = (v) => {
            v /= 255;
            return v <= 0.03928 ? v / 12.92 : ((v + 0.055) / 1.055) ** 2.4;
        };
        return 0.2126 * f(r) + 0.7152 * f(g) + 0.0722 * f(b);
    };
    // The colour painted behind an element: blend backgrounds up the tree until one is opaque.
    const behind = (el) => {
        const layers = [];
        for (let n = el; n; n = n.parentElement) {
            const s = getComputedStyle(n);
            if (s.backgroundImage !== 'none' && !s.backgroundImage.startsWith('linear-gradient')) return null;
            if (n.tagName === 'IMG' || n.querySelector(':scope > img')) {
                const img = n.tagName === 'IMG' ? n : n.querySelector(':scope > img');
                const a = img.getBoundingClientRect(),
                    b = el.getBoundingClientRect();
                if (a.width && b.left < a.right && b.right > a.left && b.top < a.bottom && b.bottom > a.top)
                    return null;
            }
            const c = parse(s.backgroundColor);
            if (c && c[3] > 0) {
                layers.push(c);
                if (c[3] >= 1) break;
            }
        }
        let colour = [255, 255, 255, 1];
        for (const layer of layers.reverse()) colour = blend(layer, colour);
        return colour;
    };
    const out = [];
    const walker = document.createTreeWalker(document.body, NodeFilter.SHOW_TEXT);
    const seen = new Set();
    for (let t = walker.nextNode(); t; t = walker.nextNode()) {
        if (!t.textContent.trim()) continue;
        const el = t.parentElement;
        if (!el || seen.has(el) || el.closest('script,style,noscript,svg,[hidden],.sr-only,#blazor-error-ui')) continue;
        seen.add(el);
        const r = el.getBoundingClientRect();
        const s = getComputedStyle(el);
        if (
            r.width <= 1 ||
            r.height <= 1 ||
            s.visibility === 'hidden' ||
            Number(s.opacity) === 0 ||
            !el.checkVisibility()
        )
            continue;
        if (el.closest('details:not([open])') && !el.closest('summary')) continue;
        const bg = behind(el);
        if (!bg) continue;
        const fgRaw = parse(s.color);
        if (!fgRaw) continue;
        const fg = blend(fgRaw, bg);
        const [l1, l2] = [lum(fg), lum(bg)].sort((a, b) => b - a);
        const ratio = (l1 + 0.05) / (l2 + 0.05);
        const size = parseFloat(s.fontSize),
            bold = Number(s.fontWeight) >= 700;
        const need = size >= 24 || (bold && size >= 18.66) ? 3 : 4.5;
        if (ratio + 0.005 < need)
            out.push({
                text: t.textContent.trim().slice(0, 40),
                where:
                    el.tagName.toLowerCase() +
                    (el.className && typeof el.className === 'string'
                        ? '.' + el.className.trim().split(/\s+/).join('.')
                        : ''),
                fg: s.color,
                bg: 'rgb(' + bg.slice(0, 3).map(Math.round).join(', ') + ')',
                ratio: Math.round(ratio * 100) / 100,
                need,
            });
    }
    return out;
}

module.exports = { audit };
