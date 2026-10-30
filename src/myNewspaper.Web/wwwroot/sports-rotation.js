// Refresh visible desktop standings and polls at the hour; collection stays with the scraper.
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
        due = Date.now() + Math.max(1000, Number(host.dataset.rotationRefreshAfter) || 60000);
        schedule(due - Date.now() + 250);
    }
    async function check() {
        if (!host?.isConnected || document.hidden || !host.getClientRects().length || controller) return;
        if (Date.now() < due) {
            schedule(due - Date.now() + 250);
            return;
        }
        const current = host,
            request = new AbortController();
        controller = request;
        const timeout = setTimeout(() => request.abort(), 15000);
        try {
            const response = await fetch('/sports', { cache: 'no-store', signal: request.signal });
            if (!response.ok) throw new Error('Sports refresh unavailable');
            const doc = new DOMParser().parseFromString(await response.text(), 'text/html');
            const next = doc.querySelector('[data-sports-rotation]');
            if (!next || host !== current || !current.isConnected) return;
            // Keep the last displayed tables while a link is focused or a dialog is open.
            if (current.contains(document.activeElement) || document.querySelector('dialog[open]')) {
                schedule(1000);
                return;
            }
            current.innerHTML = next.innerHTML;
            current.dataset.rotationRefreshAfter = next.dataset.rotationRefreshAfter;
            arm();
        } catch (_) {
            if (host === current && current.isConnected) schedule(60000); // Keep the last good tables.
        } finally {
            clearTimeout(timeout);
            if (controller === request) controller = null;
        }
    }
    function bind() {
        const next = document.querySelector('[data-sports-rotation]');
        if (next === host) return;
        clearTimeout(timer);
        if (controller) controller.abort();
        controller = null;
        host = next;
        if (host) arm();
    }
    document.addEventListener('visibilitychange', check);
    window.addEventListener('focus', check);
    window.addEventListener('resize', check);
    if (window.Blazor) Blazor.addEventListener('enhancedload', bind);
    new MutationObserver((records) => {
        if (
            records.some((r) =>
                [...r.addedNodes].some(
                    (n) =>
                        n.nodeType === 1 &&
                        (n.matches('[data-sports-rotation]') || n.querySelector('[data-sports-rotation]')),
                ),
            )
        )
            bind();
        if (records.some((r) => r.type === 'attributes' && r.target === host && !host.hidden)) check();
    }).observe(document.body, { childList: true, subtree: true, attributes: true, attributeFilter: ['hidden'] });
    bind();
})();
