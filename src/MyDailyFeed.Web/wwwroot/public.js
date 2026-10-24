// Behaviour for the public feed: the light/dark toggle and "Load more". Loaded once for the whole document and
// driven by event delegation, so it keeps working after Blazor swaps page content during navigation
// (scripts inside swapped content are not re-run).
(function () {
    var root = document.documentElement;

    // Close the section menu on selection, outside clicks, or Escape.
    document.addEventListener('click', function (event) {
        document.querySelectorAll('[data-mobile-nav][open]').forEach(function (menu) {
            if (!menu.contains(event.target) || event.target.closest('a')) menu.open = false;
        });
    });
    document.addEventListener('keydown', function (event) {
        if (event.key !== 'Escape') return;
        document.querySelectorAll('[data-mobile-nav][open]').forEach(function (menu) {
            var hadFocus = menu.contains(document.activeElement);
            menu.open = false;
            if (hadFocus) menu.querySelector('summary').focus();
        });
    });

    // ---- theme ----------------------------------------------------------------------------------------------

    function currentTheme() {
        return root.getAttribute('data-theme') === 'dark' ? 'dark' : 'light';
    }

    var themeMode = window.MyDailyFeedTheme.readMode();

    var themeSaveFailed = false;

    function syncToggles() {
        var dark = currentTheme() === 'dark';
        document.querySelectorAll('[data-theme-toggle]').forEach(function (button) {
            button.setAttribute('aria-label', dark ? 'Switch to light mode' : 'Switch to dark mode');
            button.setAttribute('aria-pressed', String(dark));
        });
        document.querySelectorAll('[data-theme-mode]').forEach(function (input) {
            input.checked = input.value === themeMode;
        });
        document.querySelectorAll('[data-theme-status]').forEach(function (status) {
            try {
                var solar = window.MyDailyFeedSolar;
                var day = solar.times(new Date(), solar.location);
                var format = new Intl.DateTimeFormat('en-US', { timeZone: solar.location.timeZone, hour: 'numeric', minute: '2-digit', timeZoneName: 'short' });
                var message = 'Kansas City today: sunrise ' + format.format(day.sunrise) +
                    ' · sunset ' + format.format(day.sunset) + '. ' +
                    (themeMode === 'sun' ? 'Automatic mode is on; currently ' + currentTheme() + '.' : 'Currently ' + currentTheme() + '.');
            } catch (e) { message = 'Currently ' + currentTheme() + '.'; }
            if (themeSaveFailed) message += ' This browser could not save your choice; it applies until you reload.';
            if (status.textContent !== message) status.textContent = message;
        });
    }

    // The theme in force. Blazor's in-page navigation (News <-> Sports Events) copies the new page's <html> attributes
    // over the old ones, which has no data-theme, so the attribute is restored below whenever it is changed behind
    // our back. Without this, switching pages would fall back to the operating system's theme.
    var applied = currentTheme();

    function setTheme(theme, persist) {
        applied = theme;
        root.setAttribute('data-theme', theme);
        if (persist) {
            themeMode = theme;
            try { localStorage.setItem('theme', theme); themeSaveFailed = false; } catch (e) { themeSaveFailed = true; }
        }
        syncToggles();
    }

    new MutationObserver(function () {
        if (root.getAttribute('data-theme') !== applied) {
            root.setAttribute('data-theme', applied);
            syncToggles();
        }
    }).observe(root, { attributes: true, attributeFilter: ['data-theme'] });

    function refreshTheme() {
        setTheme(window.MyDailyFeedTheme.resolve(themeMode), false);
    }
    document.addEventListener('change', function (event) {
        if (!event.target.matches('input[data-theme-mode]')) return;
        var choice = event.target.value;
        if (!['system', 'sun', 'light', 'dark'].includes(choice)) return;
        themeMode = choice;
        try { localStorage.setItem('theme', choice); themeSaveFailed = false; } catch (e) { themeSaveFailed = true; }
        refreshTheme();
    });
    if (window.matchMedia) {
        var query = matchMedia('(prefers-color-scheme: dark)');
        if (query.addEventListener) query.addEventListener('change', refreshTheme);
    }
    // Recheck across sunrise, sunset, midnight, suspended tabs and browser navigation.
    setInterval(refreshTheme, 30000);
    window.addEventListener('focus', refreshTheme);
    document.addEventListener('visibilitychange', function () { if (!document.hidden) refreshTheme(); });
    document.addEventListener('enhancedload', refreshTheme);
    window.addEventListener('storage', function (event) {
        if (event.key === 'theme' || event.key === null) {
            themeMode = window.MyDailyFeedTheme.readMode();
            refreshTheme();
        }
    });

    // ---- saved articles -------------------------------------------------------------------------------------
    var SAVED_KEY = 'saved-articles'; // Legacy browser-only bookmarks, imported once.
    var IMPORT_KEY = 'mydailyfeed-saved-import-v1';
    var savedCache = [], savedReady = false, savedBusy = false, savedToken = null;
    var savedQueue = Promise.resolve(), refreshPending = false;

    // Only http(s) addresses are ever used as a link or picture.
    function safeUrl(value) {
        if (typeof value !== 'string' || !value) return null;
        try { var url = new URL(value); return ['http:', 'https:'].includes(url.protocol) && !url.username && !url.password ? url.href : null; }
        catch (_) { return null; }
    }
    function text(value) { return typeof value === 'string' ? value : ''; }
    function readSaved() { return savedCache; }
    function acceptSaved(data) {
        savedCache = data.articles;
        savedReady = true;
        if (data.token) savedToken = data.token;
        syncSaves();
    }
    async function savedRequest(path, body) {
        var controller = new AbortController(), timeout = setTimeout(function () { controller.abort(); }, 15000);
        try {
            var response = await fetch('/api/saved' + path, { method: body ? 'POST' : 'GET', credentials: 'same-origin',
                cache: 'no-store', signal: controller.signal,
                headers: body ? { 'Content-Type': 'application/json', 'X-Saved-CSRF': savedToken } : { 'Accept': 'application/json' },
                body: body ? JSON.stringify(body) : undefined });
            if (!response.ok) {
                var error; try { error = await response.json(); } catch (_) { }
                throw new Error(error && error.error || 'Saved articles could not sync. Check your connection and try again.');
            }
            return await response.json();
        } finally { clearTimeout(timeout); }
    }
    function importId() {
        // randomUUID requires HTTPS; old bookmarks may live on an HTTP LAN origin.
        if (crypto.randomUUID) return crypto.randomUUID();
        var bytes = crypto.getRandomValues(new Uint8Array(16));
        bytes[6] = (bytes[6] & 15) | 64; bytes[8] = (bytes[8] & 63) | 128;
        var hex = Array.from(bytes, function (b) { return b.toString(16).padStart(2, '0'); }).join('');
        return [hex.slice(0, 8), hex.slice(8, 12), hex.slice(12, 16), hex.slice(16, 20), hex.slice(20)].join('-');
    }
    async function importLegacy() {
        var legacy, receipt;
        try {
            receipt = JSON.parse(localStorage.getItem(IMPORT_KEY) || 'null');
            if (receipt && receipt.done) return;
            legacy = JSON.parse(localStorage.getItem(SAVED_KEY) || '[]');
            if (!Array.isArray(legacy) || !legacy.length) return;
            if (!receipt || !receipt.id) {
                receipt = { id: importId(), done: false };
                localStorage.setItem(IMPORT_KEY, JSON.stringify(receipt));
            }
        } catch (_) { return; } // Shared saving works even when browser storage is blocked.
        var articles = legacy.filter(function (a) { return a && safeUrl(a.url); }).slice(0, 500).map(function (a) {
            return { url: safeUrl(a.url), title: text(a.title).slice(0, 1000), source: text(a.source).slice(0, 500),
                image: safeUrl(a.image), summary: text(a.summary).slice(0, 4000), date: text(a.date).slice(0, 100),
                dateLabel: text(a.dateLabel).slice(0, 200), read: a.read === true,
                savedAt: typeof a.savedAt === 'string' && !isNaN(Date.parse(a.savedAt)) ? new Date(a.savedAt).toISOString() : null };
        });
        acceptSaved(await savedRequest('/import', { clientId: receipt.id, articles: articles }));
        // Keep the old list as a backup. The server receipt prevents replay after removal,
        // even if this final browser write fails or two tabs migrate simultaneously.
        try { localStorage.setItem(IMPORT_KEY, JSON.stringify({ id: receipt.id, done: true })); } catch (_) { }
    }
    function refreshSaved() {
        if (refreshPending) return;
        refreshPending = true;
        savedQueue = savedQueue.then(async function () {
            try {
                acceptSaved(await savedRequest(''));
                try { await importLegacy(); }
                catch (importError) { announce("Your shared list is available, but browser bookmarks could not be imported. " + importError.message); return; }
                var status = document.querySelector('[data-saved-status]');
                if (status) status.textContent = 'Shared across devices. Changes sync automatically.';
            } catch (error) {
                savedReady = false; syncSaves();
                announce(error.message);
            } finally { refreshPending = false; }
        });
    }
    setInterval(function () { if (!document.hidden) refreshSaved(); }, 15000);
    window.addEventListener('focus', refreshSaved);
    window.addEventListener('online', refreshSaved);
    document.addEventListener('visibilitychange', function () { if (!document.hidden) refreshSaved(); });
    if (window.Blazor) Blazor.addEventListener('enhancedload', refreshSaved);

    function fromButton(button) {
        return {
            url: button.getAttribute('data-url'),
            title: text(button.getAttribute('data-title')),
            source: text(button.getAttribute('data-source')),
            kind: button.getAttribute('data-kind') === 'video' ? 'video' : 'article',
            image: button.getAttribute('data-image') || null,
            summary: text(button.getAttribute('data-summary')),
            date: button.getAttribute('data-date') || null,
            dateLabel: button.getAttribute('data-date-label') || null,
            savedAt: new Date().toISOString(),
            read: (function () { try { var p = JSON.parse(localStorage.getItem('mynewsfeed-personal-v1') || '{}'); return Array.isArray(p.read) && p.read.includes(button.getAttribute('data-url')); } catch (_) { return false; } })(),
        };
    }

    function el(tag, className, content) {
        var node = document.createElement(tag);
        if (className) node.className = className;
        if (content) node.textContent = content;
        return node;
    }

    // The same markup as a News card (FeedArticles.razor), built with DOM calls so saved text is never parsed as HTML.
    function savedCard(a) {
        var href = safeUrl(a.url);
        var card = el('article', 'article');

        var thumb = el('div', 'thumb');
        thumb.setAttribute('aria-hidden', 'true');
        var image = safeUrl(a.image);
        if (image) {
            var img = el('img');
            img.src = image; img.alt = ''; img.loading = 'lazy'; img.decoding = 'async';
            img.referrerPolicy = 'no-referrer';
            img.onerror = function () { img.remove(); };
            thumb.appendChild(img);
        }
        card.appendChild(thumb);

        var body = el('div', 'body');
        if (a.kind === 'video') body.appendChild(el('span', 'cat', 'YouTube'));
        var title = el('h3', 'title');
        var link = el('a', null, text(a.title) || href);
        link.href = href; link.target = '_blank'; link.rel = 'noopener noreferrer';
        title.appendChild(link);
        body.appendChild(title);
        if (text(a.summary)) body.appendChild(el('p', 'summary', a.summary));

        var src = el('p', 'src');
        var source = text(a.source);
        var mark = el('span', 'src__mark', (source.match(/[A-Za-z0-9]/) || ['?'])[0].toUpperCase());
        mark.setAttribute('aria-hidden', 'true');
        src.appendChild(mark);
        src.appendChild(el('span', 'src__name', source));
        if (text(a.dateLabel)) {
            var sep = el('span', 'src__sep', '·');
            sep.setAttribute('aria-hidden', 'true');
            src.appendChild(sep);
            var time = el('time', null, a.dateLabel);
            if (text(a.date)) time.setAttribute('datetime', a.date);
            src.appendChild(time);
        }

        var share = el('button', 'share-button');
        share.type = 'button'; share.hidden = true;
        share.setAttribute('data-share-open', '');
        share.setAttribute('data-url', href);
        share.setAttribute('data-title', text(a.title));
        share.setAttribute('data-kind', a.kind === 'video' ? 'video' : 'article');
        share.setAttribute('aria-label', a.kind === 'video' ? 'Share video' : 'Share article');
        share.title = a.kind === 'video' ? 'Share video' : 'Share article';
        share.innerHTML = SHARE_ICON;
        src.appendChild(share);

        // The card's own bookmark, pressed: clicking it removes the article from the list.
        var button = el('button', 'save');
        button.type = 'button';
        button.setAttribute('data-save', '');
        button.setAttribute('data-url', href);
        button.setAttribute('data-title', text(a.title));
        button.setAttribute('data-kind', a.kind === 'video' ? 'video' : 'article');
        button.innerHTML = '<svg width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true"><path d="M6 3h12a1 1 0 0 1 1 1v17l-7-4.5L5 21V4a1 1 0 0 1 1-1z"></path></svg>';
        src.appendChild(button);
        body.appendChild(src);

        card.appendChild(body);
        return card;
    }

    // Draws the Saved page from the shared list. Skipped when nothing changed, because this runs on every DOM change
    // (and its own drawing is a DOM change).
    function renderSavedPage(list) {
        var container = document.querySelector('[data-saved-list]');
        if (!container) return;
        var signature = JSON.stringify([savedReady, list]);
        if (container.getAttribute('data-rendered') === signature) return;
        container.setAttribute('data-rendered', signature);

        container.textContent = '';
        [
            { kind: 'article', title: 'Articles', empty: 'No saved articles yet.' },
            { kind: 'video', title: 'Videos', empty: 'No videos saved for later yet.' }
        ].forEach(function (group) {
            var section = el('section', 'saved-section');
            section.setAttribute('data-saved-section', group.kind);
            section.setAttribute('aria-labelledby', 'saved-' + group.kind + '-heading');
            var heading = el('h2', 'saved-head', group.title);
            heading.id = 'saved-' + group.kind + '-heading';
            section.appendChild(heading);
            var items = list.filter(function (a) { return (a.kind === 'video' ? 'video' : 'article') === group.kind; });
            if (items.length) {
                var feed = el('div', 'feed');
                items.forEach(function (a) { feed.appendChild(savedCard(a)); });
                section.appendChild(feed);
            } else if (savedReady) {
                section.appendChild(el('p', 'empty', group.empty));
            }
            container.appendChild(section);
        });

        var empty = document.querySelector('[data-saved-empty]');
        if (empty) {
            empty.hidden = !savedReady || list.length > 0;
            empty.querySelector('[data-saved-empty-title]').textContent = 'Your Saved list is empty';
            empty.querySelector('[data-saved-empty-description]').textContent = 'Save an article or choose Watch later on a YouTube video to keep it here across your devices.';

        }
    }

    function syncSaves(scope) {
        scope = scope || document;
        var list = readSaved();
        var saved = {};
        list.forEach(function (a) { saved[a.url] = true; });

        matchesWithin(scope, '[data-save]').forEach(function (button) {
            var on = saved[button.getAttribute('data-url')] === true;
            button.hidden = false;
            button.disabled = !savedReady || savedBusy;
            if (button.getAttribute('aria-pressed') !== String(on)) button.setAttribute('aria-pressed', String(on));
            var video = button.getAttribute('data-kind') === 'video';
            var label = on ? 'Remove from saved' : video ? 'Watch later' : 'Save for later';
            var caption = button.querySelector('[data-save-label]');
            if (caption && caption.textContent !== (on ? 'Saved' : 'Watch later')) caption.textContent = on ? 'Saved' : 'Watch later';
            if (button.getAttribute('aria-label') !== label) {
                button.setAttribute('aria-label', label);
                button.setAttribute('title', label);
            }
        });

        matchesWithin(scope, '[data-saved-count]').forEach(function (badge) {
            var value = list.length > 99 ? '99+' : String(list.length);
            badge.hidden = list.length === 0;
            if (badge.textContent !== value) badge.textContent = value;
        });

        if (scope === document || matchesWithin(scope, '[data-saved-list]').length) renderSavedPage(list);
    }

    function toggleSave(button) {
        var url = safeUrl(button.getAttribute('data-url'));
        if (!savedReady || savedBusy || !url) return;
        // Explicit add/remove makes retries idempotent and does not overwrite other devices.
        var removing = readSaved().some(function (a) { return a.url === url; });
        savedBusy = true; syncSaves();
        savedQueue = savedQueue.then(async function () {
            try {
                acceptSaved(await savedRequest(removing ? '/remove' : '/add', removing ? { url: url } : fromButton(button)));
                announce(removing ? 'Removed from the shared Saved list.' : 'Saved across your devices.');
            } catch (error) { announce(error.message); }
            finally { savedBusy = false; syncSaves(); }
        });
    }

    // Tells screen readers what just happened, through the page's own status line. Pages without one (the home page)
    // get a hidden one, added once.
    function announce(message) {
        var status = document.querySelector('[data-saved-status]') || document.querySelector('[data-load-status]')
            || document.querySelector('[data-copy-status]');
        if (!status) {
            status = el('span', 'sr-only');
            status.setAttribute('role', 'status');
            status.setAttribute('data-copy-status', '');
            (document.querySelector('.pub') || document.body).appendChild(status);
        }
        status.textContent = message;
    }

    // ---- article sharing ------------------------------------------------------------------------------------

    var SHARE_ICON = '<svg width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true"><circle cx="18" cy="5" r="3"/><circle cx="6" cy="12" r="3"/><circle cx="18" cy="19" r="3"/><path d="m8.6 10.5 6.8-4M8.6 13.5l6.8 4"/></svg>';

    function matchesWithin(scope, selector) {
        var elements = Array.from(scope.querySelectorAll(selector));
        if (scope.nodeType === 1 && scope.matches(selector)) elements.unshift(scope);
        return elements;
    }

    function syncShares(scope) {
        matchesWithin(scope || document, '[data-share-open][hidden]').forEach(function (button) { button.hidden = false; });
    }

    function openShare(button) {
        var url = safeUrl(button.getAttribute('data-url'));
        var host = button.closest('.pub');
        if (!url || !host) return;
        var video = button.getAttribute('data-kind') === 'video';
        var title = button.getAttribute('data-title') || (video ? 'Video' : 'Article');
        var dialog = el('dialog', 'article-share');
        var heading = el('h2', null, video ? 'Share video' : 'Share article');
        heading.id = 'article-share-title';
        dialog.setAttribute('aria-labelledby', heading.id);
        dialog.appendChild(heading);
        dialog.appendChild(el('p', 'article-share__title', title));
        var copy = el('button', 'nf-btn nf-btn--secondary', 'Copy link');
        copy.type = 'button';
        copy.addEventListener('click', function () {
            writeClipboard(url).then(function () { announce('Link copied: ' + title); dialog.close(); },
                function () { announce('Could not copy the link.'); });
        });
        dialog.appendChild(copy);
        var phone = (navigator.userAgentData && navigator.userAgentData.mobile === true)
            || /iPhone|iPod|Android.*Mobile/i.test(navigator.userAgent);
        if (phone) {
            var sms = el('a', 'nf-btn nf-btn--secondary', 'Send by SMS');
            sms.href = 'sms:' + (/iPhone|iPod/i.test(navigator.userAgent) ? '&' : '?')
                + 'body=' + encodeURIComponent(title + '\n' + url);
            dialog.appendChild(sms);
        }
        if (window.isSecureContext && typeof navigator.share === 'function') {
            var apps = el('button', 'nf-btn nf-btn--secondary', 'Share via apps');
            apps.type = 'button';
            apps.addEventListener('click', function () {
                try {
                    Promise.resolve(navigator.share({ title: title, url: url })).then(function () { dialog.close(); }, function (error) {
                        if (error.name !== 'AbortError') announce('Could not open sharing. Try copying the link.');
                    });
                } catch (error) { announce('Could not open sharing. Try copying the link.'); }
            });
            dialog.appendChild(apps);
        }
        if (phone) dialog.appendChild(el('p', 'article-share__hint', 'SMS opens your messaging app. Choose a recipient and send when you’re ready.'));
        var close = el('button', 'nf-btn nf-btn--secondary', 'Close');
        close.type = 'button';
        close.addEventListener('click', function () { dialog.close(); });
        dialog.appendChild(close);
        dialog.addEventListener('close', function () { dialog.remove(); if (button.isConnected) button.focus(); });
        host.appendChild(dialog);
        dialog.showModal();
    }

    // navigator.clipboard exists only on https and localhost; over plain http (the site on the LAN) fall back to
    // the older copy command on a temporary text box.
    function writeClipboard(value) {
        if (navigator.clipboard && window.isSecureContext) {
            return navigator.clipboard.writeText(value);
        }
        return new Promise(function (resolve, reject) {
            var box = el('textarea');
            box.value = value;
            box.setAttribute('readonly', '');
            box.style.position = 'fixed';
            box.style.opacity = '0';
            (document.querySelector('dialog[open]') || document.body).appendChild(box);
            box.select();
            var ok = false;
            try { ok = document.execCommand('copy'); } catch (e) { }
            box.remove();
            if (ok) resolve(); else reject(new Error('copy failed'));
        });
    }

    // Another tab saved or removed something.
    window.addEventListener('storage', function (e) { if (e.key === IMPORT_KEY || e.key === null) refreshSaved(); });

    // Blazor re-renders the toggle button on navigation (and swaps in new pages with fresh save buttons); put labels,
    // pressed states and the saved list back in step.
    var addedScopes = new Set();
    var pending = false;
    new MutationObserver(function (records) {
        records.forEach(function (record) {
            record.addedNodes.forEach(function (node) {
                if (node.nodeType !== 1) return;
                var selector = '[data-share-open], [data-save], [data-saved-list], [data-saved-count], [data-theme-toggle], [data-theme-mode]';
                if (node.matches(selector) || node.querySelector(selector)) addedScopes.add(node);
            });
        });
        if (pending || !addedScopes.size) return;
        pending = true;
        requestAnimationFrame(function () {
            pending = false;
            var scopes = Array.from(addedScopes); addedScopes.clear();
            scopes.filter(function (node) { return node.isConnected && !scopes.some(function (other) { return other !== node && other.contains(node); }); })
                .forEach(function (node) { syncSaves(node); syncShares(node); });
            syncToggles();
        });
    }).observe(document.body, { childList: true, subtree: true });
    syncToggles();
    syncSaves();
    syncShares();
    refreshSaved();

    // ---- load more ------------------------------------------------------------------------------------------

    function loadMore(link) {
        var feed = document.querySelector('.feed');
        if (!feed || link.getAttribute('aria-busy') === 'true') {
            if (!feed) window.location.href = link.href;
            return;
        }

        // The link's own href is the no-JavaScript fallback; reuse its filters and cursor for the fetch.
        var endpoint = new URL('feed/more', document.baseURI);
        new URL(link.href).searchParams.forEach(function (value, key) { endpoint.searchParams.set(key, value); });
        endpoint.searchParams.set('day', feed.getAttribute('data-last-day') || '');
        endpoint.searchParams.set('view', link.getAttribute('data-feed-view') || 'news');

        var label = link.textContent;
        link.setAttribute('aria-busy', 'true');
        link.textContent = 'Loading…';

        fetch(endpoint, { headers: { 'Accept': 'application/json' }, credentials: 'same-origin' })
            .then(function (response) {
                if (!response.ok) throw new Error('HTTP ' + response.status);
                return response.json();
            })
            .then(function (data) {
                var before = feed.querySelectorAll('.article').length;
                feed.insertAdjacentHTML('beforeend', data.html);
                if (data.lastDay) feed.setAttribute('data-last-day', data.lastDay);

                document.querySelectorAll('[data-shown-total]').forEach(function (el) {
                    el.textContent = 'Showing ' + data.shown + ' of ' + data.total;
                });
                var status = document.querySelector('[data-load-status]');
                if (status) status.textContent = data.added + ' more articles loaded';

                if (data.nextHref) {
                    link.href = data.nextHref;
                    link.textContent = label;
                    link.removeAttribute('aria-busy');
                } else {
                    link.remove();
                }

                // Move focus to the first new article so keyboard and screen-reader users continue from there.
                var first = feed.querySelectorAll('.article')[before];
                var target = first && first.querySelector('.title a');
                if (target) target.focus();
            })
            .catch(function () {
                // Fall back to the plain link: a normal page load of the next batch.
                window.location.href = link.href;
            });
    }

    // Capture phase, so this runs before Blazor's own link handling: it must not also navigate to the
    // fallback URL when "Load more" is clicked.
    // Search and category forms are plain GET forms. Leave out empty fields so "All categories" and an empty search
    // give a clean URL instead of "?category=&q=". Capture phase: runs before the form is serialised.
    document.addEventListener('submit', function (e) {
        var form = e.target;
        if (!form.closest || !form.closest('.pub') || (form.method || '').toLowerCase() !== 'get') return;
        var blanks = [];
        form.querySelectorAll('select[name], input[name]').forEach(function (el) {
            if (el.value === '' && !el.disabled) { el.disabled = true; blanks.push(el); }
        });
        // The browser (or Blazor) reads the form during this same event, so re-enabling afterwards is safe.
        setTimeout(function () { blanks.forEach(function (el) { el.disabled = false; }); }, 0);
    }, true);

    document.addEventListener('click', function (e) {
        if (!e.target.closest) return;

        if (e.target.closest('[data-theme-toggle]')) {
            setTheme(currentTheme() === 'dark' ? 'light' : 'dark', true);
            return;
        }


        var save = e.target.closest('[data-save]');
        if (save) {
            toggleSave(save);
            return;
        }

        var share = e.target.closest('[data-share-open]');
        if (share) { openShare(share); return; }

        var more = e.target.closest('[data-load-more]');
        if (more && !(e.ctrlKey || e.metaKey || e.shiftKey || e.button > 0)) {
            e.preventDefault();
            e.stopPropagation();
            loadMore(more);
        }
    }, true);
})();

// Personal sports highlights are stored only in this browser.
(function () {
    var key = 'mynewsfeed-sports-teams-v1';
    var selected = new Set();
    function read() {
        try {
            var values = JSON.parse(localStorage.getItem(key) || '[]');
            selected = new Set(Array.isArray(values) ? values.filter(function (v) { return typeof v === 'string' && v.length <= 200; }).slice(0, 1000) : []);
        } catch (_) { selected = new Set(); }
    }
    function status(message) {
        document.querySelectorAll('[data-sports-status]').forEach(function (el) {
            var value = message || selected.size + ' teams/schools selected. Changes save automatically.';
            if (el.textContent !== value) el.textContent = value;
        });
    }
    function sync() {
        document.querySelectorAll('[data-sports-choice]').forEach(function (box) { box.checked = selected.has(box.value); });
        document.querySelectorAll('[data-sports-teams]').forEach(function (row) {
            var names = [];
            try { names = JSON.parse(row.dataset.sportsTeams); } catch (_) { }
            row.classList.toggle('sports-selected', Array.isArray(names) && names.some(function (name) { return selected.has(name); }));
        });
        status();
    }
    function save() {
        try { localStorage.setItem(key, JSON.stringify(Array.from(selected))); sync(); }
        catch (_) { read(); sync(); status('Selections could not be saved. Allow browser storage and try again.'); }
    }
    document.addEventListener('change', function (e) {
        if (!e.target.matches('[data-sports-choice]')) return;
        if (e.target.checked) selected.add(e.target.value); else selected.delete(e.target.value);
        save();
    });
    document.addEventListener('click', function (e) {
        if (!e.target.closest('[data-sports-clear]')) return;
        selected.clear(); save();
    });
    document.addEventListener('input', function (e) {
        if (!e.target.matches('[data-sports-search]')) return;
        var term = e.target.value.trim().toLowerCase();
        var total = 0;
        document.querySelectorAll('.sports-team-group').forEach(function (group) {
            var count = 0;
            group.querySelectorAll('[data-sports-option]').forEach(function (label) {
                label.hidden = !label.dataset.searchName.includes(term);
                if (!label.hidden) count++;
            });
            group.hidden = count === 0; total += count;
        });
        var empty = document.querySelector('[data-sports-no-matches]');
        if (empty) empty.hidden = total > 0;
    });
    window.addEventListener('storage', function (e) { if (e.key === key || e.key === null) { read(); sync(); } });
    var pending = false;
    new MutationObserver(function (records) {
        var relevant = records.some(function (record) { return Array.from(record.addedNodes).some(function (node) {
            return node.nodeType === 1 && (node.matches('[data-sports-teams], [data-sports-preferences]') || node.querySelector('[data-sports-teams], [data-sports-preferences]'));
        }); });
        if (!relevant || pending) return;
        pending = true;
        requestAnimationFrame(function () { pending = false; sync(); });
    }).observe(document.body, { childList: true, subtree: true });
    read(); sync();
})();

// Color schemes share the existing light/dark setting and survive enhanced navigation.
(function () {
    var root = document.documentElement;
    var key = 'mynewsfeed-color-scheme';
    var choices = ['ribbon', 'monochrome', 'kansas', 'cardinals', 'city', 'christmas', 'halloween', 'st-patricks', 'eighties', 'nineties', 'y2k', 'desert', 'coffee'];
    function read() {
        var value;
        try { value = localStorage.getItem(key); } catch (e) { }
        return choices.includes(value) ? value : 'ribbon';
    }
    var applied = read();
    function sync() {
        if (root.getAttribute('data-color-scheme') !== applied) root.setAttribute('data-color-scheme', applied);
        document.querySelectorAll('[data-color-scheme]').forEach(function (input) {
            if (input.tagName === 'INPUT') input.checked = input.value === applied;
        });
    }
    document.addEventListener('change', function (e) {
        if (!e.target.matches('input[data-color-scheme]') || !choices.includes(e.target.value)) return;
        applied = e.target.value;
        var saved = true;
        try { localStorage.setItem(key, applied); } catch (err) { saved = false; }
        sync();
        var status = document.querySelector('[data-scheme-status]');
        if (status) status.textContent = saved ? 'Color scheme saved.' : 'Color scheme applied. This browser could not save it.';
    });
    window.addEventListener('storage', function (e) { if (e.key === key || e.key === null) { applied = read(); sync(); } });
    new MutationObserver(sync).observe(root, { attributes: true, attributeFilter: ['data-color-scheme'] });
    new MutationObserver(function (records) {
        if (records.some(function (r) { return Array.from(r.addedNodes).some(function (n) {
            return n.nodeType === 1 && (n.matches('input[data-color-scheme]') || n.querySelector('input[data-color-scheme]'));
        }); })) sync();
    }).observe(document.body, { childList: true, subtree: true });
    sync();
})();

// Read the device clock each tick so sleep, midnight and clock changes stay accurate.
(function () {
    var dateFormat = new Intl.DateTimeFormat(undefined, { weekday: 'short', year: 'numeric', month: 'short', day: 'numeric' });
    var compactDateFormat = new Intl.DateTimeFormat(undefined, { year: 'numeric', month: 'short', day: 'numeric' });
    var timeFormat = new Intl.DateTimeFormat(undefined, { hour: 'numeric', minute: '2-digit', hour12: true, timeZoneName: 'short' });
    function updateClock() {
        var now = new Date();
        document.querySelectorAll('[data-header-clock]').forEach(function (clock) {
            clock.querySelector('[data-clock-date]').textContent = dateFormat.format(now);
            clock.querySelector('[data-clock-date-compact]').textContent = compactDateFormat.format(now);
            clock.querySelector('[data-clock-time]').textContent = timeFormat.format(now);
            clock.dateTime = now.toISOString();
            clock.hidden = false;
        });
    }
    updateClock();
    window.setInterval(updateClock, 1000);
    document.addEventListener('visibilitychange', updateClock);
    if (window.Blazor) window.Blazor.addEventListener('enhancedload', updateClock);
})();
