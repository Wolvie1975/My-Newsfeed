// Keep the homepage lead in step with the server's hourly headline selection.
(function () {
    'use strict';
    let host,
        timer,
        due = 0,
        controller;
    function schedule(delay) {
        clearTimeout(timer);
        timer = setTimeout(check, Math.max(250, delay));
    }
    function arm() {
        due = Date.now() + Math.max(1000, Number(host.dataset.leadRefreshAfter) || 60000);
        schedule(due - Date.now() + 250);
    }
    async function check() {
        if (!host?.isConnected || document.hidden || host.hidden || controller) return;
        if (Date.now() < due) {
            schedule(due - Date.now() + 250);
            return;
        }
        const current = host,
            request = new AbortController();
        controller = request;
        const timeout = setTimeout(() => request.abort(), 15000);
        try {
            const response = await fetch('/home/lead', { cache: 'no-store', signal: request.signal });
            if (!response.ok) throw new Error('Lead refresh unavailable');
            const doc = new DOMParser().parseFromString(await response.text(), 'text/html');
            const next = doc.querySelector('[data-home-lead]');
            if (!next || host !== current || !current.isConnected) return;
            // Keep its page position, saved visibility preference and masonry observer.
            current.innerHTML = next.innerHTML;
            current.dataset.leadHour = next.dataset.leadHour;
            current.dataset.leadRefreshAfter = next.dataset.leadRefreshAfter;
            arm();
        } catch (_) {
            if (host === current && current.isConnected) schedule(60000); // Keep the last good headline.
        } finally {
            clearTimeout(timeout);
            if (controller === request) controller = null;
        }
    }
    function bind() {
        const next = document.querySelector('[data-home-lead]');
        if (next === host) return;
        clearTimeout(timer);
        if (controller) controller.abort();
        controller = null;
        host = next;
        if (host) arm();
    }
    document.addEventListener('visibilitychange', check);
    window.addEventListener('focus', check);
    if (window.Blazor) Blazor.addEventListener('enhancedload', bind);
    new MutationObserver((records) => {
        if (
            records.some((r) =>
                [...r.addedNodes].some(
                    (n) => n.nodeType === 1 && (n.matches('[data-home-lead]') || n.querySelector('[data-home-lead]')),
                ),
            )
        )
            bind();
        if (records.some((r) => r.type === 'attributes' && r.target === host && !host.hidden)) check();
    }).observe(document.body, { childList: true, subtree: true, attributes: true, attributeFilter: ['hidden'] });
    bind();
})();
