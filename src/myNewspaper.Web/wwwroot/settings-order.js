(function () {
    'use strict';
    const key = 'mynewsfeed-settings-order-v1';
    const defaults = ['appearance', 'colors', 'reading', 'services', 'teams'];
    const labels = {
        appearance: 'Light and dark mode',
        colors: 'Color scheme',
        reading: 'Reading preferences',
        services: 'My services',
        teams: 'My Teams',
    };
    function order() {
        let saved = [];
        try {
            saved = JSON.parse(localStorage.getItem(key) || '[]');
        } catch {}
        if (!Array.isArray(saved)) saved = [];
        return [...new Set(saved.filter((s) => defaults.includes(s))), ...defaults.filter((s) => !saved.includes(s))];
    }
    function render() {
        const grid = document.querySelector('[data-settings-sections]'),
            host = document.querySelector('[data-settings-order-controls]');
        if (!grid || !host) return;
        const current = order();
        const actual = [...grid.querySelectorAll(':scope > [data-settings-section]')].map(
            (n) => n.dataset.settingsSection,
        );
        const signature = JSON.stringify(current);
        if (
            host.dataset.settingsSignature === signature &&
            host.children.length === current.length &&
            JSON.stringify(actual) === signature
        )
            return;
        host.dataset.settingsSignature = signature;
        current.forEach((id) => {
            const section = grid.querySelector(`[data-settings-section="${id}"]`);
            if (section) grid.append(section);
        });
        host.replaceChildren();
        current.forEach((id, index) => {
            const row = document.createElement('div');
            row.className = 'settings-order-row';
            const name = document.createElement('span');
            name.textContent = labels[id];
            row.append(name);
            for (const [delta, text] of [
                [-1, 'Move up'],
                [1, 'Move down'],
            ]) {
                const button = document.createElement('button');
                button.type = 'button';
                button.className = 'nf-btn nf-btn--secondary';
                button.textContent = text;
                button.dataset.settingsMove = id + ':' + delta;
                button.setAttribute('aria-label', text + ' ' + labels[id]);
                button.disabled = delta < 0 ? index === 0 : index === current.length - 1;
                row.append(button);
            }
            host.append(row);
        });
    }
    document.addEventListener('click', (e) => {
        const move = e.target.closest('[data-settings-move]'),
            reset = e.target.closest('[data-settings-order-reset]');
        if (!move && !reset) return;
        const current = order();
        if (move) {
            const [id, delta] = move.dataset.settingsMove.split(':'),
                a = current.indexOf(id),
                b = a + Number(delta);
            if (a < 0 || b < 0 || b >= current.length) return;
            [current[a], current[b]] = [current[b], current[a]];
        }
        const status = document.querySelector('[data-settings-order-status]');
        try {
            localStorage.setItem(key, JSON.stringify(reset ? defaults : current));
            render();
            status.textContent = reset ? 'Default Settings order restored.' : 'Settings order saved in this browser.';
            if (move) {
                const target = document.querySelector(`[data-settings-move="${move.dataset.settingsMove}"]`);
                if (target && !target.disabled) target.focus();
                else document.querySelector('[data-settings-order-reset]').focus();
            }
        } catch {
            status.textContent =
                'Could not save Settings order. Browser storage is unavailable; your previous order is unchanged.';
        }
    });
    // Enhanced navigation can replace the page without rerunning global scripts.
    if (window.Blazor?.addEventListener) window.Blazor.addEventListener('enhancedload', render);
    let scheduled = false;
    new MutationObserver(() => {
        if (scheduled) return;
        scheduled = true;
        queueMicrotask(() => {
            scheduled = false;
            render();
        });
    }).observe(document.body, { childList: true, subtree: true });
    window.addEventListener('storage', (e) => {
        if (e.key === key) render();
    });
    render();
})();
