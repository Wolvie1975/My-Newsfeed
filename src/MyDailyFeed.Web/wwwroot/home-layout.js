// Small grid-row spans let desktop cards cascade without changing DOM/tab order.
(function () {
    'use strict';
    const desktop = matchMedia('(min-width: 1024px)');
    let grid, frame;
    const sizes = new ResizeObserver(schedule);
    const changes = new MutationObserver(schedule);
    function schedule() {
        if (frame) return;
        frame = requestAnimationFrame(() => { frame = null; layout(); });
    }
    function layout() {
        if (!grid || !grid.isConnected) return;
        const cards = [...grid.children].filter(n => n.matches('[data-home-section]'));
        if (!desktop.matches) {
            grid.removeAttribute('data-masonry');
            cards.forEach(card => card.style.removeProperty('grid-row-end'));
            return;
        }
        // align-items:start keeps each card at its natural height inside its span.
        const gap = parseFloat(getComputedStyle(grid).rowGap) || 0;
        cards.forEach(card => {
            if (card.hidden) { card.style.removeProperty('grid-row-end'); return; }
            const span = 'span ' + Math.ceil((card.getBoundingClientRect().height + gap) / (1 + gap));
            if (card.style.gridRowEnd !== span) card.style.gridRowEnd = span;
        });
        grid.setAttribute('data-masonry', '');
    }
    function bind() {
        const current = document.querySelector('[data-home-grid]');
        if (current !== grid) {
            sizes.disconnect(); changes.disconnect(); grid = current;
            if (grid) {
                sizes.observe(grid);
                [...grid.children].forEach(card => sizes.observe(card));
                changes.observe(grid, { childList: true, subtree: true, attributes: true, attributeFilter: ['hidden'] });
            }
        }
        schedule();
    }
    desktop.addEventListener('change', schedule);
    window.addEventListener('resize', schedule);
    if (window.Blazor) Blazor.addEventListener('enhancedload', bind);
    new MutationObserver(records => {
        if (records.some(record => [...record.addedNodes].some(n => n.nodeType === 1 &&
            (n.matches('[data-home-grid]') || n.querySelector('[data-home-grid]'))))) bind();
    }).observe(document.body, { childList: true, subtree: true });
    if (document.fonts) document.fonts.ready.then(schedule);
    bind();
})();
