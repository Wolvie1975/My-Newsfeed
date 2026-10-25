// Behaviour for the public feed: the light/dark toggle and "Load more". Loaded once for the whole document and
// driven by event delegation, so it keeps working after Blazor swaps page content during navigation
// (scripts inside swapped content are not re-run).
(function () {
    'use strict';
    const SHARE_ICON =
        '<svg width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true"><circle cx="18" cy="5" r="3"/><circle cx="6" cy="12" r="3"/><circle cx="18" cy="19" r="3"/><path d="m8.6 10.5 6.8-4M8.6 13.5l6.8 4"/></svg>';
    const root = document.documentElement;

    // Close the section menu on selection, outside clicks, or Escape.
    document.addEventListener('click', (event) => {
        document.querySelectorAll('[data-mobile-nav][open]').forEach((menu) => {
            if (!menu.contains(event.target) || event.target.closest('a')) menu.open = false;
        });
    });
    document.addEventListener('keydown', (event) => {
        if (event.key !== 'Escape') return;
        document.querySelectorAll('[data-mobile-nav][open]').forEach((menu) => {
            const hadFocus = menu.contains(document.activeElement);
            menu.open = false;
            if (hadFocus) menu.querySelector('summary').focus();
        });
    });

    // ---- theme ----------------------------------------------------------------------------------------------

    function currentTheme() {
        return root.getAttribute('data-theme') === 'dark' ? 'dark' : 'light';
    }

    let themeMode = window.myNewspaperTheme.readMode();

    let themeSaveFailed = false;

    function syncToggles() {
        const dark = currentTheme() === 'dark';
        document.querySelectorAll('[data-theme-toggle]').forEach((button) => {
            button.setAttribute('aria-label', dark ? 'Switch to light mode' : 'Switch to dark mode');
            button.setAttribute('aria-pressed', String(dark));
        });
        document.querySelectorAll('[data-theme-mode]').forEach((input) => {
            input.checked = input.value === themeMode;
        });
        document.querySelectorAll('[data-theme-status]').forEach((status) => {
            let message;
            try {
                const solar = window.myNewspaperSolar;
                const day = solar.times(new Date(), solar.location);
                const format = new Intl.DateTimeFormat('en-US', {
                    timeZone: solar.location.timeZone,
                    hour: 'numeric',
                    minute: '2-digit',
                    timeZoneName: 'short',
                });
                message =
                    'Kansas City today: sunrise ' +
                    format.format(day.sunrise) +
                    ' · sunset ' +
                    format.format(day.sunset) +
                    '. ' +
                    (themeMode === 'sun'
                        ? 'Automatic mode is on; currently ' + currentTheme() + '.'
                        : 'Currently ' + currentTheme() + '.');
            } catch (e) {
                message = 'Currently ' + currentTheme() + '.';
            }
            if (themeSaveFailed) message += ' This browser could not save your choice; it applies until you reload.';
            if (status.textContent !== message) status.textContent = message;
        });
    }

    // The theme in force. Blazor's in-page navigation (News <-> Sports Events) copies the new page's <html> attributes
    // over the old ones, which has no data-theme, so the attribute is restored below whenever it is changed behind
    // our back. Without this, switching pages would fall back to the operating system's theme.
    let applied = currentTheme();

    function setTheme(theme, persist) {
        applied = theme;
        root.setAttribute('data-theme', theme);
        if (persist) {
            themeMode = theme;
            try {
                localStorage.setItem('theme', theme);
                themeSaveFailed = false;
            } catch (e) {
                themeSaveFailed = true;
            }
        }
        syncToggles();
    }

    new MutationObserver(() => {
        if (root.getAttribute('data-theme') !== applied) {
            root.setAttribute('data-theme', applied);
            syncToggles();
        }
    }).observe(root, { attributes: true, attributeFilter: ['data-theme'] });

    function refreshTheme() {
        setTheme(window.myNewspaperTheme.resolve(themeMode), false);
    }
    document.addEventListener('change', (event) => {
        if (!event.target.matches('input[data-theme-mode]')) return;
        const choice = event.target.value;
        if (!['system', 'sun', 'light', 'dark'].includes(choice)) return;
        themeMode = choice;
        try {
            localStorage.setItem('theme', choice);
            themeSaveFailed = false;
        } catch (e) {
            themeSaveFailed = true;
        }
        refreshTheme();
    });
    if (window.matchMedia) {
        const query = matchMedia('(prefers-color-scheme: dark)');
        if (query.addEventListener) query.addEventListener('change', refreshTheme);
    }
    // Recheck across sunrise, sunset, midnight, suspended tabs and browser navigation.
    setInterval(refreshTheme, 30000);
    window.addEventListener('focus', refreshTheme);
    document.addEventListener('visibilitychange', () => {
        if (!document.hidden) refreshTheme();
    });
    document.addEventListener('enhancedload', refreshTheme);
    window.addEventListener('storage', (event) => {
        if (event.key === 'theme' || event.key === null) {
            themeMode = window.myNewspaperTheme.readMode();
            refreshTheme();
        }
    });

    // ---- saved articles -------------------------------------------------------------------------------------
    const SAVED_KEY = 'saved-articles'; // Legacy browser-only bookmarks, imported once.
    const IMPORT_KEY = 'mydailyfeed-saved-import-v1';
    let savedCache = [],
        savedReady = false,
        savedBusy = false,
        savedToken = null;
    let savedQueue = Promise.resolve(),
        refreshPending = false;

    // Only http(s) addresses are ever used as a link or picture.
    function safeUrl(value) {
        if (typeof value !== 'string' || !value) return null;
        try {
            const url = new URL(value);
            return ['http:', 'https:'].includes(url.protocol) && !url.username && !url.password ? url.href : null;
        } catch (_) {
            return null;
        }
    }
    function text(value) {
        return typeof value === 'string' ? value : '';
    }
    function readSaved() {
        return savedCache;
    }
    function acceptSaved(data) {
        savedCache = data.articles;
        savedReady = true;
        if (data.token) savedToken = data.token;
        syncSaves();
    }
    async function savedRequest(path, body) {
        const controller = new AbortController(),
            timeout = setTimeout(() => {
                controller.abort();
            }, 15000);
        try {
            const response = await fetch('/api/saved' + path, {
                method: body ? 'POST' : 'GET',
                credentials: 'same-origin',
                cache: 'no-store',
                signal: controller.signal,
                headers: body
                    ? { 'Content-Type': 'application/json', 'X-Saved-CSRF': savedToken }
                    : { Accept: 'application/json' },
                body: body ? JSON.stringify(body) : undefined,
            });
            if (!response.ok) {
                let error;
                try {
                    error = await response.json();
                } catch (_) {}
                throw new Error(
                    (error && error.error) || 'Saved articles could not sync. Check your connection and try again.',
                );
            }
            return await response.json();
        } finally {
            clearTimeout(timeout);
        }
    }
    function importId() {
        // randomUUID requires HTTPS; old bookmarks may live on an HTTP LAN origin.
        if (crypto.randomUUID) return crypto.randomUUID();
        const bytes = crypto.getRandomValues(new Uint8Array(16));
        bytes[6] = (bytes[6] & 15) | 64;
        bytes[8] = (bytes[8] & 63) | 128;
        const hex = Array.from(bytes, (b) => {
            return b.toString(16).padStart(2, '0');
        }).join('');
        return [hex.slice(0, 8), hex.slice(8, 12), hex.slice(12, 16), hex.slice(16, 20), hex.slice(20)].join('-');
    }
    async function importLegacy() {
        let legacy, receipt;
        try {
            receipt = JSON.parse(localStorage.getItem(IMPORT_KEY) || 'null');
            if (receipt && receipt.done) return;
            legacy = JSON.parse(localStorage.getItem(SAVED_KEY) || '[]');
            if (!Array.isArray(legacy) || !legacy.length) return;
            if (!receipt || !receipt.id) {
                receipt = { id: importId(), done: false };
                localStorage.setItem(IMPORT_KEY, JSON.stringify(receipt));
            }
        } catch (_) {
            return;
        } // Shared saving works even when browser storage is blocked.
        const articles = legacy
            .filter((a) => {
                return a && safeUrl(a.url);
            })
            .slice(0, 500)
            .map((a) => {
                return {
                    url: safeUrl(a.url),
                    title: text(a.title).slice(0, 1000),
                    source: text(a.source).slice(0, 500),
                    image: safeUrl(a.image),
                    summary: text(a.summary).slice(0, 4000),
                    date: text(a.date).slice(0, 100),
                    dateLabel: text(a.dateLabel).slice(0, 200),
                    read: a.read === true,
                    savedAt:
                        typeof a.savedAt === 'string' && !isNaN(Date.parse(a.savedAt))
                            ? new Date(a.savedAt).toISOString()
                            : null,
                };
            });
        acceptSaved(await savedRequest('/import', { clientId: receipt.id, articles: articles }));
        // Keep the old list as a backup. The server receipt prevents replay after removal,
        // even if this final browser write fails or two tabs migrate simultaneously.
        try {
            localStorage.setItem(IMPORT_KEY, JSON.stringify({ id: receipt.id, done: true }));
        } catch (_) {}
    }
    function refreshSaved() {
        if (refreshPending) return;
        refreshPending = true;
        savedQueue = savedQueue.then(async () => {
            try {
                acceptSaved(await savedRequest(''));
                try {
                    await importLegacy();
                } catch (importError) {
                    announce(
                        'Your shared list is available, but browser bookmarks could not be imported. ' +
                            importError.message,
                    );
                    return;
                }
                const status = document.querySelector('[data-saved-status]');
                if (status) status.textContent = 'Shared across devices. Changes sync automatically.';
            } catch (error) {
                savedReady = false;
                syncSaves();
                announce(error.message);
            } finally {
                refreshPending = false;
            }
        });
    }
    setInterval(() => {
        if (!document.hidden) refreshSaved();
    }, 15000);
    window.addEventListener('focus', refreshSaved);
    window.addEventListener('online', refreshSaved);
    document.addEventListener('visibilitychange', () => {
        if (!document.hidden) refreshSaved();
    });
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
            read: (function () {
                try {
                    const p = JSON.parse(localStorage.getItem('mynewsfeed-personal-v1') || '{}');
                    return Array.isArray(p.read) && p.read.includes(button.getAttribute('data-url'));
                } catch (_) {
                    return false;
                }
            })(),
        };
    }

    function el(tag, className, content) {
        const node = document.createElement(tag);
        if (className) node.className = className;
        if (content) node.textContent = content;
        return node;
    }

    // The same markup as a News card (FeedArticles.razor), built with DOM calls so saved text is never parsed as HTML.
    function savedCard(a) {
        const href = safeUrl(a.url);
        const card = el('article', 'article');

        const thumb = el('div', 'thumb');
        thumb.setAttribute('aria-hidden', 'true');
        const image = safeUrl(a.image);
        if (image) {
            const img = el('img');
            img.src = image;
            img.alt = '';
            img.loading = 'lazy';
            img.decoding = 'async';
            img.referrerPolicy = 'no-referrer';
            img.onerror = function () {
                img.remove();
            };
            thumb.appendChild(img);
        }
        card.appendChild(thumb);

        const body = el('div', 'body');
        if (a.kind === 'video') body.appendChild(el('span', 'cat', 'YouTube'));
        const title = el('h3', 'title');
        const link = el('a', null, text(a.title) || href);
        link.href = href;
        link.target = '_blank';
        link.rel = 'noopener noreferrer';
        title.appendChild(link);
        body.appendChild(title);
        if (text(a.summary)) body.appendChild(el('p', 'summary', a.summary));

        const src = el('p', 'src');
        const source = text(a.source);
        const mark = el('span', 'src__mark', (source.match(/[A-Za-z0-9]/) || ['?'])[0].toUpperCase());
        mark.setAttribute('aria-hidden', 'true');
        src.appendChild(mark);
        src.appendChild(el('span', 'src__name', source));
        if (text(a.dateLabel)) {
            const sep = el('span', 'src__sep', '·');
            sep.setAttribute('aria-hidden', 'true');
            src.appendChild(sep);
            const time = el('time', null, a.dateLabel);
            if (text(a.date)) time.setAttribute('datetime', a.date);
            src.appendChild(time);
        }

        const share = el('button', 'share-button');
        share.type = 'button';
        share.hidden = true;
        share.setAttribute('data-share-open', '');
        share.setAttribute('data-url', href);
        share.setAttribute('data-title', text(a.title));
        share.setAttribute('data-kind', a.kind === 'video' ? 'video' : 'article');
        share.setAttribute('aria-label', a.kind === 'video' ? 'Share video' : 'Share article');
        share.title = a.kind === 'video' ? 'Share video' : 'Share article';
        share.innerHTML = SHARE_ICON;
        src.appendChild(share);

        // The card's own bookmark, pressed: clicking it removes the article from the list.
        const button = el('button', 'save');
        button.type = 'button';
        button.setAttribute('data-save', '');
        button.setAttribute('data-url', href);
        button.setAttribute('data-title', text(a.title));
        button.setAttribute('data-kind', a.kind === 'video' ? 'video' : 'article');
        button.innerHTML =
            '<svg width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true"><path d="M6 3h12a1 1 0 0 1 1 1v17l-7-4.5L5 21V4a1 1 0 0 1 1-1z"></path></svg>';
        src.appendChild(button);
        body.appendChild(src);

        card.appendChild(body);
        return card;
    }

    // Draws the Saved page from the shared list. Skipped when nothing changed, because this runs on every DOM change
    // (and its own drawing is a DOM change).
    function renderSavedPage(list) {
        const container = document.querySelector('[data-saved-list]');
        if (!container) return;
        const signature = JSON.stringify([savedReady, list]);
        if (container.getAttribute('data-rendered') === signature) return;
        container.setAttribute('data-rendered', signature);

        container.textContent = '';
        [
            { kind: 'article', title: 'Articles', empty: 'No saved articles yet.' },
            { kind: 'video', title: 'Videos', empty: 'No videos saved for later yet.' },
        ].forEach((group) => {
            const section = el('section', 'saved-section');
            section.setAttribute('data-saved-section', group.kind);
            section.setAttribute('aria-labelledby', 'saved-' + group.kind + '-heading');
            const heading = el('h2', 'saved-head', group.title);
            heading.id = 'saved-' + group.kind + '-heading';
            section.appendChild(heading);
            const items = list.filter((a) => {
                return (a.kind === 'video' ? 'video' : 'article') === group.kind;
            });
            if (items.length) {
                const feed = el('div', 'feed');
                items.forEach((a) => {
                    feed.appendChild(savedCard(a));
                });
                section.appendChild(feed);
            } else if (savedReady) {
                section.appendChild(el('p', 'empty', group.empty));
            }
            container.appendChild(section);
        });

        const empty = document.querySelector('[data-saved-empty]');
        if (empty) {
            empty.hidden = !savedReady || list.length > 0;
            empty.querySelector('[data-saved-empty-title]').textContent = 'Your Saved list is empty';
            empty.querySelector('[data-saved-empty-description]').textContent =
                'Save an article or choose Watch later on a YouTube video to keep it here across your devices.';
        }
    }

    function syncSaves(scope) {
        scope = scope || document;
        const list = readSaved();
        const saved = {};
        list.forEach((a) => {
            saved[a.url] = true;
        });

        matchesWithin(scope, '[data-save]').forEach((button) => {
            const on = saved[button.getAttribute('data-url')] === true;
            button.hidden = false;
            button.disabled = !savedReady || savedBusy;
            if (button.getAttribute('aria-pressed') !== String(on)) button.setAttribute('aria-pressed', String(on));
            const video = button.getAttribute('data-kind') === 'video';
            const label = on ? 'Remove from saved' : video ? 'Watch later' : 'Save for later';
            const caption = button.querySelector('[data-save-label]');
            if (caption && caption.textContent !== (on ? 'Saved' : 'Watch later'))
                caption.textContent = on ? 'Saved' : 'Watch later';
            if (button.getAttribute('aria-label') !== label) {
                button.setAttribute('aria-label', label);
                button.setAttribute('title', label);
            }
        });

        matchesWithin(scope, '[data-saved-count]').forEach((badge) => {
            const value = list.length > 99 ? '99+' : String(list.length);
            badge.hidden = list.length === 0;
            if (badge.textContent !== value) badge.textContent = value;
        });

        if (scope === document || matchesWithin(scope, '[data-saved-list]').length) renderSavedPage(list);
    }

    function toggleSave(button) {
        const url = safeUrl(button.getAttribute('data-url'));
        if (!savedReady || savedBusy || !url) return;
        // Explicit add/remove makes retries idempotent and does not overwrite other devices.
        const removing = readSaved().some((a) => {
            return a.url === url;
        });
        savedBusy = true;
        syncSaves();
        savedQueue = savedQueue.then(async () => {
            try {
                acceptSaved(
                    await savedRequest(removing ? '/remove' : '/add', removing ? { url: url } : fromButton(button)),
                );
                announce(removing ? 'Removed from the shared Saved list.' : 'Saved across your devices.');
            } catch (error) {
                announce(error.message);
            } finally {
                savedBusy = false;
                syncSaves();
            }
        });
    }

    // Tells screen readers what just happened, through the page's own status line. Pages without one (the home page)
    // get a hidden one, added once.
    function announce(message) {
        let status =
            document.querySelector('[data-saved-status]') ||
            document.querySelector('[data-load-status]') ||
            document.querySelector('[data-copy-status]');
        if (!status) {
            status = el('span', 'sr-only');
            status.setAttribute('role', 'status');
            status.setAttribute('data-copy-status', '');
            (document.querySelector('.pub') || document.body).appendChild(status);
        }
        status.textContent = message;
    }

    // ---- article sharing ------------------------------------------------------------------------------------

    function matchesWithin(scope, selector) {
        const elements = Array.from(scope.querySelectorAll(selector));
        if (scope.nodeType === 1 && scope.matches(selector)) elements.unshift(scope);
        return elements;
    }

    function syncShares(scope) {
        matchesWithin(scope || document, '[data-share-open][hidden]').forEach((button) => {
            button.hidden = false;
        });
    }

    function openShare(button) {
        const url = safeUrl(button.getAttribute('data-url'));
        const host = button.closest('.pub');
        if (!url || !host) return;
        const video = button.getAttribute('data-kind') === 'video';
        const title = button.getAttribute('data-title') || (video ? 'Video' : 'Article');
        const dialog = el('dialog', 'article-share');
        const heading = el('h2', null, video ? 'Share video' : 'Share article');
        heading.id = 'article-share-title';
        dialog.setAttribute('aria-labelledby', heading.id);
        dialog.appendChild(heading);
        dialog.appendChild(el('p', 'article-share__title', title));
        const copy = el('button', 'nf-btn nf-btn--secondary', 'Copy link');
        copy.type = 'button';
        copy.addEventListener('click', () => {
            writeClipboard(url).then(
                () => {
                    announce('Link copied: ' + title);
                    dialog.close();
                },
                () => {
                    announce('Could not copy the link.');
                },
            );
        });
        dialog.appendChild(copy);
        const phone =
            (navigator.userAgentData && navigator.userAgentData.mobile === true) ||
            /iPhone|iPod|Android.*Mobile/i.test(navigator.userAgent);
        if (phone) {
            const sms = el('a', 'nf-btn nf-btn--secondary', 'Send by SMS');
            sms.href =
                'sms:' +
                (/iPhone|iPod/i.test(navigator.userAgent) ? '&' : '?') +
                'body=' +
                encodeURIComponent(title + '\n' + url);
            dialog.appendChild(sms);
        }
        if (window.isSecureContext && typeof navigator.share === 'function') {
            const apps = el('button', 'nf-btn nf-btn--secondary', 'Share via apps');
            apps.type = 'button';
            apps.addEventListener('click', () => {
                try {
                    Promise.resolve(navigator.share({ title: title, url: url })).then(
                        () => {
                            dialog.close();
                        },
                        (error) => {
                            if (error.name !== 'AbortError') announce('Could not open sharing. Try copying the link.');
                        },
                    );
                } catch (error) {
                    announce('Could not open sharing. Try copying the link.');
                }
            });
            dialog.appendChild(apps);
        }
        if (phone)
            dialog.appendChild(
                el(
                    'p',
                    'article-share__hint',
                    'SMS opens your messaging app. Choose a recipient and send when you’re ready.',
                ),
            );
        const close = el('button', 'nf-btn nf-btn--secondary', 'Close');
        close.type = 'button';
        close.addEventListener('click', () => {
            dialog.close();
        });
        dialog.appendChild(close);
        dialog.addEventListener('close', () => {
            dialog.remove();
            if (button.isConnected) button.focus();
        });
        host.appendChild(dialog);
        dialog.showModal();
    }

    // navigator.clipboard exists only on https and localhost; over plain http (the site on the LAN) fall back to
    // the older copy command on a temporary text box.
    function writeClipboard(value) {
        if (navigator.clipboard && window.isSecureContext) {
            return navigator.clipboard.writeText(value);
        }
        return new Promise((resolve, reject) => {
            const box = el('textarea');
            box.value = value;
            box.setAttribute('readonly', '');
            box.style.position = 'fixed';
            box.style.opacity = '0';
            (document.querySelector('dialog[open]') || document.body).appendChild(box);
            box.select();
            let ok = false;
            try {
                ok = document.execCommand('copy');
            } catch (e) {}
            box.remove();
            if (ok) resolve();
            else reject(new Error('copy failed'));
        });
    }

    // Another tab saved or removed something.
    window.addEventListener('storage', (e) => {
        if (e.key === IMPORT_KEY || e.key === null) refreshSaved();
    });

    // Blazor re-renders the toggle button on navigation (and swaps in new pages with fresh save buttons); put labels,
    // pressed states and the saved list back in step.
    const addedScopes = new Set();
    let pending = false;
    new MutationObserver((records) => {
        records.forEach((record) => {
            record.addedNodes.forEach((node) => {
                if (node.nodeType !== 1) return;
                const selector =
                    '[data-share-open], [data-save], [data-saved-list], [data-saved-count], [data-theme-toggle], [data-theme-mode]';
                if (node.matches(selector) || node.querySelector(selector)) addedScopes.add(node);
            });
        });
        if (pending || !addedScopes.size) return;
        pending = true;
        requestAnimationFrame(() => {
            pending = false;
            const scopes = Array.from(addedScopes);
            addedScopes.clear();
            scopes
                .filter((node) => {
                    return (
                        node.isConnected &&
                        !scopes.some((other) => {
                            return other !== node && other.contains(node);
                        })
                    );
                })
                .forEach((node) => {
                    syncSaves(node);
                    syncShares(node);
                });
            syncToggles();
        });
    }).observe(document.body, { childList: true, subtree: true });
    syncToggles();
    syncSaves();
    syncShares();
    refreshSaved();

    // ---- load more ------------------------------------------------------------------------------------------

    function loadMore(link) {
        const feed = document.querySelector('.feed');
        if (!feed || link.getAttribute('aria-busy') === 'true') {
            if (!feed) window.location.href = link.href;
            return;
        }

        // The link's own href is the no-JavaScript fallback; reuse its filters and cursor for the fetch.
        const endpoint = new URL('feed/more', document.baseURI);
        new URL(link.href).searchParams.forEach((value, key) => {
            endpoint.searchParams.set(key, value);
        });
        endpoint.searchParams.set('day', feed.getAttribute('data-last-day') || '');
        endpoint.searchParams.set('view', link.getAttribute('data-feed-view') || 'news');

        const label = link.textContent;
        link.setAttribute('aria-busy', 'true');
        link.textContent = 'Loading…';

        fetch(endpoint, { headers: { Accept: 'application/json' }, credentials: 'same-origin' })
            .then((response) => {
                if (!response.ok) throw new Error('HTTP ' + response.status);
                return response.json();
            })
            .then((data) => {
                const before = feed.querySelectorAll('.article').length;
                feed.insertAdjacentHTML('beforeend', data.html);
                if (data.lastDay) feed.setAttribute('data-last-day', data.lastDay);

                document.querySelectorAll('[data-shown-total]').forEach((el) => {
                    el.textContent = 'Showing ' + data.shown + ' of ' + data.total;
                });
                const status = document.querySelector('[data-load-status]');
                if (status) status.textContent = data.added + ' more articles loaded';

                if (data.nextHref) {
                    link.href = data.nextHref;
                    link.textContent = label;
                    link.removeAttribute('aria-busy');
                } else {
                    link.remove();
                }

                // Move focus to the first new article so keyboard and screen-reader users continue from there.
                const first = feed.querySelectorAll('.article')[before];
                const target = first && first.querySelector('.title a');
                if (target) target.focus();
            })
            .catch(() => {
                // Fall back to the plain link: a normal page load of the next batch.
                window.location.href = link.href;
            });
    }

    // Capture phase, so this runs before Blazor's own link handling: it must not also navigate to the
    // fallback URL when "Load more" is clicked.
    // Search and category forms are plain GET forms. Leave out empty fields so "All categories" and an empty search
    // give a clean URL instead of "?category=&q=". Capture phase: runs before the form is serialised.
    document.addEventListener(
        'submit',
        (e) => {
            const form = e.target;
            if (!form.closest || !form.closest('.pub') || (form.method || '').toLowerCase() !== 'get') return;
            const blanks = [];
            form.querySelectorAll('select[name], input[name]').forEach((el) => {
                if (el.value === '' && !el.disabled) {
                    el.disabled = true;
                    blanks.push(el);
                }
            });
            // The browser (or Blazor) reads the form during this same event, so re-enabling afterwards is safe.
            setTimeout(() => {
                blanks.forEach((el) => {
                    el.disabled = false;
                });
            }, 0);
        },
        true,
    );

    document.addEventListener(
        'click',
        (e) => {
            if (!e.target.closest) return;

            if (e.target.closest('[data-theme-toggle]')) {
                setTheme(currentTheme() === 'dark' ? 'light' : 'dark', true);
                return;
            }

            const save = e.target.closest('[data-save]');
            if (save) {
                toggleSave(save);
                return;
            }

            const share = e.target.closest('[data-share-open]');
            if (share) {
                openShare(share);
                return;
            }

            const more = e.target.closest('[data-load-more]');
            if (more && !(e.ctrlKey || e.metaKey || e.shiftKey || e.button > 0)) {
                e.preventDefault();
                e.stopPropagation();
                loadMore(more);
            }
        },
        true,
    );
})();

// Personal sports highlights are stored only in this browser.
(function () {
    'use strict';
    const key = 'mynewsfeed-sports-teams-v1';
    let selected = new Set();
    function read() {
        try {
            const values = JSON.parse(localStorage.getItem(key) || '[]');
            selected = new Set(
                Array.isArray(values)
                    ? values
                          .filter((v) => {
                              return typeof v === 'string' && v.length <= 200;
                          })
                          .slice(0, 1000)
                    : [],
            );
        } catch (_) {
            selected = new Set();
        }
    }
    function status(message) {
        document.querySelectorAll('[data-sports-status]').forEach((el) => {
            const value = message || selected.size + ' teams/schools selected. Changes save automatically.';
            if (el.textContent !== value) el.textContent = value;
        });
    }
    function sync() {
        document.querySelectorAll('[data-sports-choice]').forEach((box) => {
            box.checked = selected.has(box.value);
        });
        document.querySelectorAll('[data-sports-teams]').forEach((row) => {
            let names = [];
            try {
                names = JSON.parse(row.dataset.sportsTeams);
            } catch (_) {}
            row.classList.toggle(
                'sports-selected',
                Array.isArray(names) &&
                    names.some((name) => {
                        return selected.has(name);
                    }),
            );
        });
        status();
    }
    function save() {
        try {
            localStorage.setItem(key, JSON.stringify(Array.from(selected)));
            sync();
        } catch (_) {
            read();
            sync();
            status('Selections could not be saved. Allow browser storage and try again.');
        }
    }
    document.addEventListener('change', (e) => {
        if (!e.target.matches('[data-sports-choice]')) return;
        if (e.target.checked) selected.add(e.target.value);
        else selected.delete(e.target.value);
        save();
    });
    document.addEventListener('click', (e) => {
        if (!e.target.closest('[data-sports-clear]')) return;
        selected.clear();
        save();
    });
    document.addEventListener('input', (e) => {
        if (!e.target.matches('[data-sports-search]')) return;
        const term = e.target.value.trim().toLowerCase();
        let total = 0;
        document.querySelectorAll('.sports-team-group').forEach((group) => {
            let count = 0;
            group.querySelectorAll('[data-sports-option]').forEach((label) => {
                label.hidden = !label.dataset.searchName.includes(term);
                if (!label.hidden) count++;
            });
            group.hidden = count === 0;
            total += count;
        });
        const empty = document.querySelector('[data-sports-no-matches]');
        if (empty) empty.hidden = total > 0;
    });
    window.addEventListener('storage', (e) => {
        if (e.key === key || e.key === null) {
            read();
            sync();
        }
    });
    let pending = false;
    new MutationObserver((records) => {
        const relevant = records.some((record) => {
            return Array.from(record.addedNodes).some((node) => {
                return (
                    node.nodeType === 1 &&
                    (node.matches('[data-sports-teams], [data-sports-preferences]') ||
                        node.querySelector('[data-sports-teams], [data-sports-preferences]'))
                );
            });
        });
        if (!relevant || pending) return;
        pending = true;
        requestAnimationFrame(() => {
            pending = false;
            sync();
        });
    }).observe(document.body, { childList: true, subtree: true });
    read();
    sync();
})();

// Color schemes share the existing light/dark setting and survive enhanced navigation.
(function () {
    'use strict';
    const root = document.documentElement;
    const key = 'mynewsfeed-color-scheme';
    const choices = [
        'ribbon',
        'monochrome',
        'kansas',
        'cardinals',
        'city',
        'christmas',
        'halloween',
        'st-patricks',
        'eighties',
        'nineties',
        'y2k',
        'desert',
        'coffee',
    ];
    function read() {
        let value;
        try {
            value = localStorage.getItem(key);
        } catch (e) {}
        return choices.includes(value) ? value : 'ribbon';
    }
    let applied = read();
    function sync() {
        if (root.getAttribute('data-color-scheme') !== applied) root.setAttribute('data-color-scheme', applied);
        document.querySelectorAll('[data-color-scheme]').forEach((input) => {
            if (input.tagName === 'INPUT') input.checked = input.value === applied;
        });
    }
    document.addEventListener('change', (e) => {
        if (!e.target.matches('input[data-color-scheme]') || !choices.includes(e.target.value)) return;
        applied = e.target.value;
        let saved = true;
        try {
            localStorage.setItem(key, applied);
        } catch (err) {
            saved = false;
        }
        sync();
        const status = document.querySelector('[data-scheme-status]');
        if (status)
            status.textContent = saved
                ? 'Color scheme saved.'
                : 'Color scheme applied. This browser could not save it.';
    });
    window.addEventListener('storage', (e) => {
        if (e.key === key || e.key === null) {
            applied = read();
            sync();
        }
    });
    new MutationObserver(sync).observe(root, { attributes: true, attributeFilter: ['data-color-scheme'] });
    new MutationObserver((records) => {
        if (
            records.some((r) => {
                return Array.from(r.addedNodes).some((n) => {
                    return (
                        n.nodeType === 1 &&
                        (n.matches('input[data-color-scheme]') || n.querySelector('input[data-color-scheme]'))
                    );
                });
            })
        )
            sync();
    }).observe(document.body, { childList: true, subtree: true });
    sync();
})();

// Read the device clock each tick so sleep, midnight and clock changes stay accurate.
(function () {
    'use strict';
    const dateFormat = new Intl.DateTimeFormat(undefined, {
        weekday: 'short',
        year: 'numeric',
        month: 'short',
        day: 'numeric',
    });
    const compactDateFormat = new Intl.DateTimeFormat(undefined, { year: 'numeric', month: 'short', day: 'numeric' });
    const timeFormat = new Intl.DateTimeFormat(undefined, {
        hour: 'numeric',
        minute: '2-digit',
        hour12: true,
        timeZoneName: 'short',
    });
    function updateClock() {
        const now = new Date();
        document.querySelectorAll('[data-header-clock]').forEach((clock) => {
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
