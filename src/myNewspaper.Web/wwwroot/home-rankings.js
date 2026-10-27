// Switch the homepage Top 25 poll in place, so the page keeps its scroll position. Without JavaScript, or if the
// fragment can't load, the form's normal GET navigation still works.
(function () {
    'use strict';
    let controller;
    document.addEventListener('submit', async (event) => {
        const form = event.target;
        const card = form.closest?.('[data-home-section="rankings"]');
        if (!card || !form.matches('.catselect')) return;
        event.preventDefault();
        const id = new FormData(form).get('rankingSourceId') || '';
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
            card.querySelector('#home-ranking-source')?.focus({ preventScroll: true });
        } catch (error) {
            if (error.name !== 'AbortError' || controller === request) form.submit(); // Fall back to a full load.
        } finally {
            clearTimeout(timeout);
            card.removeAttribute('aria-busy');
            if (controller === request) controller = null;
        }
    });
})();
