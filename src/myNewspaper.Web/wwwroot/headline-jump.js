// All headlines: pin a slim copy of the Jump to box once it scrolls away, and mark the group being read.
(function () {
    'use strict';
    function init() {
        const box = document.querySelector('[data-hc-jump]'),
            bar = document.querySelector('[data-hc-bar]');
        if (!box || !bar || box.dataset.hcBound) return;
        box.dataset.hcBound = '1';
        const desktop = matchMedia('(min-width: 744px)');
        const syncTop = () => {
            const header = document.querySelector('.pub-header');
            if (header) {
                bar.style.top = Math.max(0, Math.round(header.getBoundingClientRect().height)) + 'px';
            }
        };
        syncTop();
        window.addEventListener('resize', syncTop, { passive: true });
        new IntersectionObserver(
            ([e]) => {
                syncTop();
                bar.hidden = !desktop.matches || e.isIntersecting;
            },
            { rootMargin: '-64px 0px 0px 0px' },
        ).observe(box);

        const mark = (key) =>
            document.querySelectorAll('[data-hc-target]').forEach((a) => {
                if (a.dataset.hcTarget === key) {
                    a.setAttribute('aria-current', 'true');
                } else {
                    a.removeAttribute('aria-current');
                }
            });
        const headings = new IntersectionObserver(
            (entries) => {
                const top = entries
                    .filter((e) => e.isIntersecting)
                    .sort((a, b) => a.boundingClientRect.top - b.boundingClientRect.top)[0];
                if (top) mark(top.target.dataset.hcGroup);
            },
            { rootMargin: '-130px 0px -60% 0px' },
        );
        const watch = () =>
            document.querySelectorAll('[data-hc-group]:not([data-hc-watched])').forEach((h) => {
                h.dataset.hcWatched = '1';
                headings.observe(h);
            });
        watch();
        new MutationObserver(watch).observe(document.querySelector('[data-hc-grid]') || document.body, {
            childList: true,
        });

        document.addEventListener('click', (e) => {
            const a = e.target.closest('[data-hc-target]');
            const target = a && document.getElementById('hc-' + a.dataset.hcTarget);
            if (!target) return;
            e.preventDefault();
            target.scrollIntoView({
                behavior: matchMedia('(prefers-reduced-motion: reduce)').matches ? 'auto' : 'smooth',
            });
        });
    }
    document.addEventListener('DOMContentLoaded', init);
    if (window.Blazor?.addEventListener) window.Blazor.addEventListener('enhancedload', init);
})();
