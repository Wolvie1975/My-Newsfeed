// The admin's Requests alert in the public header (profiles handoff 5.4). The header link works on its own; this turns
// it into a quick-look panel (a popover on wide screens, a bottom sheet on phones) that loads its list from
// /admin/api/requests/summary each time it opens. Opening doesn't mark anything seen; only Mark all seen does.
// Event delegation keeps it working after Blazor's enhanced navigation swaps the header.
(function () {
    'use strict';
    let token = null;
    let opener = null;

    const parts = () => {
        const root = document.querySelector('[data-admin-alert]');
        return (
            root && {
                root,
                toggle: root.querySelector('[data-admin-alert-toggle]'),
                panel: root.querySelector('[data-admin-alert-panel]'),
                list: root.querySelector('[data-admin-alert-list]'),
                status: root.querySelector('[data-admin-alert-status]'),
                count: root.querySelector('[data-admin-alert-count]'),
                seen: root.querySelector('[data-admin-alert-seen]'),
            }
        );
    };

    async function api(path, method) {
        const headers = { Accept: 'application/json' };
        if (method === 'POST') {
            if (!token) token = (await api('/token')).token;
            headers['X-Saved-CSRF'] = token;
        }
        const response = await fetch('/admin/api' + path, {
            method: method || 'GET',
            credentials: 'same-origin',
            cache: 'no-store',
            headers,
        });
        if (response.status === 401 || response.status === 403)
            throw new Error('Sign in to Admin again to see requests.');
        if (!response.ok) throw new Error('Requests couldn’t load. Try again, or open Admin · Requests.');
        return response.json();
    }

    // The server sends UTC without a zone.
    const utc = (value) => new Date(/[zZ]|[+-]\d\d:\d\d$/.test(value) ? value : value + 'Z');
    function when(value) {
        const date = utc(value),
            minutes = Math.round((Date.now() - date) / 60000);
        if (minutes < 1) return 'Just now';
        if (minutes < 60) return minutes + ' min ago';
        if (minutes < 24 * 60) return Math.floor(minutes / 60) + ' h ago';
        return date.toLocaleDateString([], { month: 'short', day: 'numeric' });
    }
    const clock = (value) => utc(value).toLocaleTimeString([], { hour: 'numeric', minute: '2-digit' });

    function el(tag, className, text) {
        const node = document.createElement(tag);
        if (className) node.className = className;
        if (text !== undefined) node.textContent = text;
        return node;
    }
    function avatar(name, color) {
        const node = el('span', 'admin-alert__avatar', (name.trim()[0] || '?').toUpperCase());
        node.setAttribute('aria-hidden', 'true');
        if (/^#[0-9a-fA-F]{6}$/.test(color)) node.style.background = color;
        return node;
    }
    function action(href, text, primary) {
        const link = el(
            'a',
            primary ? 'admin-alert__action admin-alert__action--primary' : 'admin-alert__action',
            text,
        );
        link.href = href;
        return link;
    }

    function lockedItem(p) {
        const li = el('li', 'admin-alert__item admin-alert__item--locked');
        li.append(avatar(p.name, p.color));
        const body = el('div', 'admin-alert__body');
        body.append(el('b', 'admin-alert__name', p.name + ' is locked'));
        body.append(
            el(
                'span',
                'admin-alert__meta',
                '5 wrong PINs' + (p.device ? ' on ' + p.device : '') + ' · ' + clock(p.lockedAt),
            ),
        );
        const actions = el('span', 'admin-alert__actions');
        const unlock = el('button', 'admin-alert__action admin-alert__action--primary', 'Unlock');
        unlock.type = 'button';
        unlock.dataset.adminAlertUnlock = p.profileId;
        actions.append(unlock, action('admin/profiles/' + p.profileId + '#pin', 'Reset PIN'));
        body.append(actions);
        li.append(body);
        return li;
    }

    function requestItem(r, seenAt) {
        const fresh = !seenAt || utc(r.createdAt) > utc(seenAt);
        const li = el('li', 'admin-alert__item' + (fresh ? ' is-new' : ''));
        li.append(avatar(r.profileName, r.profileColor));
        const body = el('div', 'admin-alert__body');
        const meta = el('span', 'admin-alert__meta');
        meta.append(el('span', 'admin-alert__kind', r.kind === 'youtube' ? 'YouTube' : 'News source'));
        meta.append(document.createTextNode(' ' + r.profileName + ' · ' + when(r.createdAt)));
        if (fresh) meta.append(el('span', 'admin-alert__new', 'New'));
        body.append(meta, el('b', 'admin-alert__name', r.name));
        if (r.note) body.append(el('span', 'admin-alert__note', '“' + r.note + '”'));
        body.append(
            el(
                'span',
                'admin-alert__check' + (r.checkOk ? ' is-ok' : ''),
                (r.checkOk ? '✓ ' : '') + (r.checkSummary || 'Not checked yet'),
            ),
        );
        const actions = el('span', 'admin-alert__actions');
        actions.append(
            action('admin/requests?id=' + r.id, r.nextAction || 'Review', true),
            action('admin/requests?id=' + r.id + '#decline', 'Decline…'),
        );
        body.append(actions);
        li.append(body);
        return li;
    }

    function showCount(p, count) {
        p.count.textContent = count;
        p.count.hidden = count === 0;
        const label = p.toggle.getAttribute('aria-label') || '';
        const rest = label.includes('scraper problem') ? ', and a scraper problem' : '';
        p.toggle.setAttribute(
            'aria-label',
            'Requests' +
                (count === 0 ? '' : count === 1 ? ', 1 needs attention' : ', ' + count + ' need attention') +
                rest,
        );
    }

    async function load(p) {
        p.status.hidden = false;
        p.status.textContent = 'Loading…';
        p.list.replaceChildren();
        try {
            const data = await api('/requests/summary');
            showCount(p, data.count);
            const waiting = data.waiting.length;
            p.panel.querySelector('#admin-alert-title').textContent =
                'Requests · ' + (waiting === 0 ? 'none waiting' : waiting + ' waiting');
            p.list.append(...data.locked.map(lockedItem), ...data.waiting.map((r) => requestItem(r, data.seenAt)));
            p.status.textContent = data.locked.length + waiting === 0 ? 'Nothing needs you right now.' : '';
            p.status.hidden = data.locked.length + waiting > 0;
            p.seen.hidden = waiting === 0;
        } catch (error) {
            p.status.textContent = error.message;
        }
    }

    const narrow = () => window.matchMedia('(max-width: 743px)').matches;
    function open(p) {
        opener = p.toggle;
        p.panel.hidden = false;
        p.root.classList.add('is-open');
        p.toggle.setAttribute('aria-expanded', 'true');
        p.panel.setAttribute('aria-modal', narrow() ? 'true' : 'false');
        document.documentElement.classList.toggle('admin-alert-sheet', narrow());
        p.panel.querySelector('#admin-alert-title').focus();
        load(p);
    }
    function close(p, refocus) {
        if (p.panel.hidden) return;
        p.panel.hidden = true;
        p.root.classList.remove('is-open');
        p.toggle.setAttribute('aria-expanded', 'false');
        document.documentElement.classList.remove('admin-alert-sheet');
        if (refocus && opener) opener.focus();
    }

    document.addEventListener('click', async (event) => {
        const p = parts();
        if (!p) return;
        const target = event.target;
        if (target.closest('[data-admin-alert-toggle]')) {
            event.preventDefault();
            if (p.panel.hidden) open(p);
            else close(p, true);
            return;
        }
        if (target.closest('[data-admin-alert-close]')) return close(p, true);
        const unlock = target.closest('[data-admin-alert-unlock]');
        const seen = target.closest('[data-admin-alert-seen]');
        if (unlock || seen) {
            const button = unlock || seen;
            button.disabled = true;
            try {
                if (unlock) await api('/profiles/' + Number(unlock.dataset.adminAlertUnlock) + '/unlock', 'POST');
                else showCount(p, (await api('/requests/mark-seen', 'POST')).count);
                await load(p);
            } catch (error) {
                p.status.hidden = false;
                p.status.textContent = error.message;
                button.disabled = false;
            }
            return;
        }
        // Outside the panel (or on the phone sheet's backdrop): close.
        if (!p.panel.contains(target)) close(p, false);
    });
    document.addEventListener('keydown', (event) => {
        const p = parts();
        if (event.key === 'Escape' && p && !p.panel.hidden) close(p, true);
    });
    if (window.Blazor?.addEventListener)
        window.Blazor.addEventListener('enhancedload', () =>
            document.documentElement.classList.remove('admin-alert-sheet'),
        );
})();
