// Cascade cards into the shorter column, with Watch directly after the full-width lead.
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
            cards.forEach(card => { card.style.removeProperty('grid-row-end'); card.style.removeProperty('grid-row-start'); card.style.removeProperty('grid-column'); });
            return;
        }
        // align-items:start keeps each card at its natural height inside its span.
        const gap = parseFloat(getComputedStyle(grid).rowGap) || 0;
        const nextRow = [1, 1];
        cards.forEach(card => {
            if (card.hidden) { card.style.removeProperty('grid-row-end'); card.style.removeProperty('grid-row-start'); card.style.removeProperty('grid-column'); return; }
            const span = 'span ' + Math.ceil((card.getBoundingClientRect().height + gap) / (1 + gap));
            if (card.hasAttribute('data-home-wide')) {
                const start = String(Math.max(nextRow[0], nextRow[1]));
                if (card.style.gridColumn !== '1 / -1') card.style.gridColumn = '1 / -1';
                if (card.style.gridRowStart !== start) card.style.gridRowStart = start;
                if (card.style.gridRowEnd !== span) card.style.gridRowEnd = span;
                const next = Number(start) + parseInt(span.slice(5), 10);
                nextRow[0] = nextRow[1] = next;
                return;
            }
            const column = nextRow[0] <= nextRow[1] ? 0 : 1;
            const columnValue = String(column + 1);
            if (card.style.gridColumn !== columnValue) card.style.gridColumn = columnValue;
            const start = String(nextRow[column]);
            if (card.style.gridRowStart !== start) card.style.gridRowStart = start;
            if (card.style.gridRowEnd !== span) card.style.gridRowEnd = span;
            nextRow[column] += parseInt(span.slice(5), 10);
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
