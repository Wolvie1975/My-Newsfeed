// Switch the homepage Top 25 poll in place, so the page keeps its scroll position. Works for the desktop sport pills
// and the dropdown. Without JavaScript, or if the fragment can't load, the links and the form's normal GET navigation
// still work.
(function () {
    'use strict';
    let controller;
    async function show(card, id, focusSelector, fallback) {
        if (controller) controller.abort();
        const request = new AbortController();
        controller = request;
        const timeout = setTimeout(() => request.abort(), 15000);
        card.setAttribute('aria-busy', 'true');
        try {
            const response = await fetch('/home/rankings?rankingSourceId=' + encodeURIComponent(id), {
                cache: 'no-store',
                signal: request.signal,
            });
            if (!response.ok) throw new Error('Rankings unavailable');
            const doc = new DOMParser().parseFromString(await response.text(), 'text/html');
            const next = doc.querySelector('[data-home-section="rankings"]');
            if (!next || !card.isConnected) throw new Error('Rankings unavailable');
            // Keep this section's position, hidden preference and layout observers intact.
            card.innerHTML = next.innerHTML;
            const url = new URL(location.href);
            url.searchParams.set('rankingSourceId', id);
            history.replaceState(history.state, '', url);
            card.querySelector(focusSelector)?.focus({ preventScroll: true });
        } catch (error) {
            if (error.name !== 'AbortError' || controller === request) fallback(); // Fall back to a full load.
        } finally {
            clearTimeout(timeout);
            card.removeAttribute('aria-busy');
            if (controller === request) controller = null;
        }
    }
    document.addEventListener('submit', (event) => {
        const form = event.target;
        const card = form.closest?.('[data-home-section="rankings"]');
        if (!card || !form.matches('.catselect')) return;
        event.preventDefault();
        show(card, new FormData(form).get('rankingSourceId') || '', '#home-ranking-source', () => form.submit());
    });
    document.addEventListener('click', (event) => {
        const link = event.target.closest?.('[data-home-section="rankings"] a[data-ranking-source]');
        if (!link || event.button !== 0 || event.ctrlKey || event.metaKey || event.shiftKey || event.altKey) return;
        event.preventDefault();
        const id = link.dataset.rankingSource;
        show(link.closest('[data-home-section="rankings"]'), id, `a[data-ranking-source="${id}"]`, () => {
            location.href = link.href;
        });
    });
})();
