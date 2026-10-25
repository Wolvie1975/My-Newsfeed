// Read persisted HTML only; collection stays with the scoreboard service.
(function () {
    'use strict';
    const key = 'mydailyfeed-scoreboard-teams-v1';
    let selected = new Set(),
        pending = false,
        generation = 0;
    function read() {
        try {
            const values = JSON.parse(localStorage.getItem(key) || '[]');
            selected = new Set(
                Array.isArray(values)
                    ? values.filter((v) => typeof v === 'string' && v.length < 300).slice(0, 1000)
                    : [],
            );
        } catch (_) {
            selected = new Set();
        }
    }
    function apply() {
        document.querySelectorAll('[data-scoreboard-team]').forEach((button) => {
            button.hidden = false;
            const active = selected.has(button.dataset.scoreboardTeam);
            button.setAttribute('aria-pressed', String(active));
            button.textContent = active ? '★' : '☆';
        });
        document.querySelectorAll('.scoreboard-game').forEach((game) => {
            game.classList.toggle(
                'scoreboard-selected',
                [...game.querySelectorAll('[data-scoreboard-team]')].some((b) =>
                    selected.has(b.dataset.scoreboardTeam),
                ),
            );
        });
    }
    async function refresh() {
        if (pending || document.hidden || document.querySelector('dialog[open]')) return;
        const panels = [...document.querySelectorAll('[data-scoreboard]')];
        if (!panels.length) return;
        pending = true;
        const currentGeneration = generation;
        try {
            for (const panel of panels) {
                const query = new URLSearchParams(location.search);
                query.set('compact', panel.dataset.scoreboardCompact);
                try {
                    const response = await fetch('/scoreboard/fragment?' + query, {
                        cache: 'no-store',
                        signal: AbortSignal.timeout(30000),
                    });
                    if (!response.ok) throw new Error('Read failed');
                    const doc = new DOMParser().parseFromString(await response.text(), 'text/html');
                    const next = doc.querySelector('[data-scoreboard]');
                    if (!next || next.querySelector('.scoreboard-read-status')?.textContent.trim())
                        throw new Error('Unavailable');
                    if (
                        !panel.isConnected ||
                        currentGeneration !== generation ||
                        document.querySelector('dialog[open]')
                    )
                        continue;
                    // Avoid replacing a focused control during keyboard interaction.
                    if (panel.contains(document.activeElement)) continue;
                    const expanded = new Set(
                        [...panel.querySelectorAll('[data-scoreboard-key]')]
                            .filter((game) => game.querySelector('.scoreboard-info')?.open)
                            .map((game) => game.dataset.scoreboardKey),
                    );
                    next.querySelectorAll('[data-scoreboard-key]').forEach((game) => {
                        const info = game.querySelector('.scoreboard-info');
                        if (info && expanded.has(game.dataset.scoreboardKey)) info.open = true;
                    });
                    panel.replaceWith(next);
                    apply();
                } catch (_) {
                    if (panel.isConnected) {
                        const status = panel.querySelector('.scoreboard-read-status');
                        if (status)
                            status.textContent =
                                'Scores could not be refreshed. Showing the last displayed sample; retrying automatically.';
                    }
                }
            }
        } finally {
            pending = false;
        }
    }
    document.addEventListener('click', (event) => {
        const button = event.target.closest('[data-scoreboard-team]');
        if (!button) return;
        const id = button.dataset.scoreboardTeam;
        selected.has(id) ? selected.delete(id) : selected.add(id);
        try {
            localStorage.setItem(key, JSON.stringify([...selected]));
            apply();
        } catch (_) {
            read();
            apply();
            const status = button.closest('[data-scoreboard]').querySelector('.scoreboard-read-status');
            if (status) status.textContent = 'Team highlights could not be saved. Allow browser storage and retry.';
        }
    });
    window.addEventListener('storage', (event) => {
        if (event.key === key || event.key === null) {
            read();
            apply();
        }
    });
    document.addEventListener('visibilitychange', () => {
        if (!document.hidden) refresh();
    });
    window.addEventListener('online', refresh);
    setInterval(refresh, 300000);
    if (window.Blazor)
        Blazor.addEventListener('enhancedload', () => {
            generation++;
            read();
            apply();
        });
    read();
    apply();
})();
