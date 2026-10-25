/* Personal planning and reading. Copies stay on this device; no account or server writes. */
(function () {
    'use strict';
    const PREFS = 'mynewsfeed-personal-v1',
        TEAMS = 'mynewsfeed-sports-teams-v1';
    const defaults = [
        'lead',
        'watch',
        'opinion',
        'news',
        'sports-news',
        'entertainment-news',
        'sports',
        'scoreboard',
        'tv',
        'videos',
        'rankings',
        'forecasts',
        'movies',
        'digital',
    ];
    const labels = {
        lead: 'Top story',
        scoreboard: 'Scoreboard',
        watch: 'Where to watch',
        sports: 'Today & Tonight',
        'sports-news': 'Sports headlines',
        'entertainment-news': 'Entertainment headlines',
        f1: 'F1',
        rankings: 'Top 25',
        news: 'More headlines',
        opinion: 'Opinion',
        forecasts: 'Weekend box office',
        movies: 'Friday theater releases',
        digital: 'Upcoming Digital / VOD',
        tv: 'Today’s episodes',
        videos: 'Latest videos',
    };
    let storage = true,
        pending = false,
        weatherController;
    try {
        localStorage.setItem('mynewsfeed-personal-probe', '1');
        localStorage.removeItem('mynewsfeed-personal-probe');
    } catch (_) {
        storage = false;
    }
    const safe = (value) => {
        try {
            const url = new URL(value);
            return ['https:', 'http:'].includes(url.protocol) ? url.href : null;
        } catch (_) {
            return null;
        }
    };
    const txt = (value) => (typeof value === 'string' ? value.slice(0, 2048) : '');
    function load(key, fallback) {
        try {
            return JSON.parse(localStorage.getItem(key)) ?? fallback;
        } catch (_) {
            return fallback;
        }
    }
    function list(value) {
        return Array.isArray(value) ? value : [];
    }
    function prefs() {
        const raw = load(PREFS, {}),
            p = raw && typeof raw === 'object' && !Array.isArray(raw) ? raw : {};
        const order = [...new Set(list(p.order).filter((k) => defaults.includes(k)))];
        const arranged = order.concat(defaults.filter((k) => !order.includes(k)));
        // Keep the sports overview before its detailed scoreboard in saved layouts too.
        if (arranged.indexOf('scoreboard') < arranged.indexOf('sports')) {
            arranged.splice(arranged.indexOf('scoreboard'), 1);
            arranged.splice(arranged.indexOf('sports') + 1, 0, 'scoreboard');
        }
        return {
            order: arranged,
            hidden: list(p.hidden).filter((k) => defaults.includes(k) || k === 'f1'),
            teamsOnly: p.teamsOnly === true,
            hideRead: p.hideRead === true,
            muted: [...new Set(list(p.muted).filter((s) => typeof s === 'string' && s.length <= 2048))].slice(0, 500),
            read: [...new Set(list(p.read).filter((s) => safe(s)))].slice(0, 2000),
            category: typeof p.category === 'string' && /^(all|[1-9][0-9]*)$/.test(p.category) ? p.category : '',
        };
    }
    function announce(message) {
        let targets = [...document.querySelectorAll('[data-personal-status]')];
        if (!targets.length) {
            const main = document.querySelector('.pub main.page');
            if (main) {
                const status = node('p', '', 'updated');
                status.setAttribute('data-personal-status', '');
                status.setAttribute('role', 'status');
                main.prepend(status);
                targets = [status];
            }
        }
        targets.forEach((n) => (n.textContent = message));
    }
    function save(key, value, message) {
        try {
            localStorage.setItem(key, JSON.stringify(value));
            sync();
            announce(message || 'Saved in this browser.');
            return true;
        } catch (_) {
            announce('Could not save. Browser storage is full or blocked. Your previous choices are unchanged.');
            sync();
            return false;
        }
    }
    function node(tag, text, className) {
        const n = document.createElement(tag);
        if (text) n.textContent = text;
        if (className) n.className = className;
        return n;
    }
    function button(label, attr, value) {
        const b = node('button', label, 'nf-btn nf-btn--secondary');
        b.type = 'button';
        b.setAttribute(attr, value);
        return b;
    }
    function teams() {
        return new Set(list(load(TEAMS, [])).filter((k) => typeof k === 'string'));
    }
    function matchesTeams(element, selected) {
        try {
            return list(JSON.parse(element.dataset.sportsTeams || '[]')).some((k) => selected.has(k));
        } catch (_) {
            return false;
        }
    }
    function renderHome(p, selected) {
        const grid = document.querySelector('[data-home-grid]');
        if (grid) {
            grid.classList.toggle(
                'is-customized',
                p.hidden.length > 0 || JSON.stringify(p.order) !== JSON.stringify(defaults),
            );
            defaults.forEach((key, index) => {
                const section = grid.querySelector(`[data-home-section="${key}"]`);
                if (section) {
                    section.hidden = p.hidden.includes(key) || (key === 'digital' && p.hidden.includes('movies'));
                    if (grid.children[index] !== section) grid.insertBefore(section, grid.children[index] || null);
                }
            });
            grid.querySelectorAll('[data-home-f1]').forEach((section) => (section.hidden = p.hidden.includes('f1')));
            const empty = document.querySelector('[data-home-empty]');
            if (empty) empty.hidden = grid.querySelector('[data-home-section]:not([hidden])') !== null;
            const rows = [...grid.querySelectorAll('[data-home-team-item]')];
            rows.forEach((row) => (row.hidden = p.teamsOnly && !matchesTeams(row, selected)));
            const noTeams = document.querySelector('[data-home-teams-empty]');
            if (noTeams) noTeams.hidden = !p.teamsOnly || !rows.length || rows.some((row) => !row.hidden);
        }
        const host = document.querySelector('[data-home-preferences]');
        if (host && host.dataset.signature !== JSON.stringify([p.order, p.hidden])) {
            host.dataset.signature = JSON.stringify([p.order, p.hidden]);
            host.replaceChildren();
            p.order.forEach((key, index) => {
                const row = node('div', '', 'home-choice'),
                    label = node('label'),
                    input = node('input');
                input.type = 'checkbox';
                input.checked = !p.hidden.includes(key);
                input.setAttribute('data-home-visible', key);
                label.append(input, document.createTextNode(' ' + labels[key]));
                const up = button('Move up', 'data-home-move', key + ':-1'),
                    down = button('Move down', 'data-home-move', key + ':1');
                up.disabled = index === 0;
                down.disabled = index === p.order.length - 1;
                up.setAttribute('aria-label', 'Move ' + labels[key] + ' up');
                down.setAttribute('aria-label', 'Move ' + labels[key] + ' down');
                row.append(label, up, down);
                host.append(row);
            });
        }
        document.querySelectorAll('[data-home-teams-only]').forEach((n) => (n.checked = p.teamsOnly));
    }
    function renderReading(p) {
        document.querySelectorAll('[data-news-hide-read]').forEach((n) => (n.checked = p.hideRead));
        document.querySelectorAll('[data-news-category]').forEach((n) => (n.value = p.category));
        document.querySelectorAll('[data-news-url]').forEach((n) => {
            n.hidden = p.muted.includes(n.dataset.newsSource) || (p.hideRead && p.read.includes(n.dataset.newsUrl));
        });
        // Feed headings must not be left standing above a fully hidden group.
        document.querySelectorAll('.feed > .daylabel').forEach((heading) => {
            let sibling = heading.nextElementSibling,
                visible = false;
            while (sibling && !sibling.matches('.daylabel')) {
                if (sibling.matches('[data-news-url]') && !sibling.hidden) visible = true;
                sibling = sibling.nextElementSibling;
            }
            heading.hidden = !visible;
        });
        const opinion = document.querySelector('[data-home-opinion]');
        if (opinion) {
            const filtered = opinion.querySelector('[data-opinion-filtered]'),
                article = opinion.querySelector('[data-news-url]');
            if (filtered && article) filtered.hidden = !article.hidden;
        }
        const muted = document.querySelector('[data-news-muted]');
        if (muted && muted.dataset.signature !== JSON.stringify(p.muted)) {
            muted.dataset.signature = JSON.stringify(p.muted);
            muted.replaceChildren();
            if (!p.muted.length) muted.append(node('p', 'No muted publishers.', 'updated'));
            p.muted.forEach((name) => {
                const row = node('p', name + ' ');
                row.append(button('Unmute', 'data-news-unmute', name));
                muted.append(row);
            });
        }
        const page = document.querySelector('[data-news-page]');
        if (page && page.dataset.categoryExplicit === 'false' && p.category) {
            const current = page.dataset.categoryCurrent;
            // Verify preference is still present before redirecting, and make it explicit to avoid loops.
            const valid = p.category === 'all' || document.querySelector(`#category option[value="${p.category}"]`);
            if (valid && p.category !== current) {
                const url = new URL(location.href);
                url.searchParams.set('category', p.category);
                location.replace(url.href);
            }
        }
    }
    function sync() {
        document.querySelectorAll('[data-personal-control]').forEach((n) => (n.hidden = !storage));
        if (!storage) {
            announce('Browser storage is unavailable. Enable site storage to save personal preferences.');
            return;
        }
        const p = prefs(),
            selected = teams();
        renderHome(p, selected);
        renderReading(p);
    }
    document.addEventListener('click', (e) => {
        const p = prefs(),
            unmute = e.target.closest('[data-news-unmute]');
        if (unmute) {
            p.muted = p.muted.filter((i) => i !== unmute.dataset.newsUnmute);
            save(PREFS, p, 'Publisher unmuted.');
            return;
        }
        const move = e.target.closest('[data-home-move]');
        if (move) {
            const [key, delta] = move.dataset.homeMove.split(':'),
                index = p.order.indexOf(key),
                target = index + Number(delta);
            if (index >= 0 && target >= 0 && target < p.order.length) {
                [p.order[index], p.order[target]] = [p.order[target], p.order[index]];
                save(PREFS, p);
                document.querySelector(`[data-home-move="${move.dataset.homeMove}"]`)?.focus();
            }
            return;
        }
        if (e.target.closest('[data-home-reset]')) {
            p.order = defaults;
            p.hidden = [];
            p.teamsOnly = false;
            save(PREFS, p, 'Default homepage restored.');
        }
        if (e.target.closest('[data-news-reset]')) {
            p.muted = [];
            p.read = [];
            p.hideRead = false;
            p.category = '';
            if (save(PREFS, p, 'Reading preferences reset.'))
                document.dispatchEvent(new CustomEvent('mynewsfeed-reading-change', { detail: { reset: true } }));
        }
    });
    document.addEventListener('change', (e) => {
        const p = prefs(),
            n = e.target;
        if (n.matches('[data-home-visible]')) {
            p.hidden = n.checked
                ? p.hidden.filter((k) => k !== n.dataset.homeVisible)
                : [...p.hidden, n.dataset.homeVisible];
            save(PREFS, p);
        } else if (n.matches('[data-home-teams-only]')) {
            p.teamsOnly = n.checked;
            save(PREFS, p);
        } else if (n.matches('[data-news-hide-read]')) {
            p.hideRead = n.checked;
            save(PREFS, p);
        } else if (n.matches('[data-news-category]')) {
            p.category = n.value;
            save(PREFS, p);
        } else if (n.matches('[data-sports-choice]')) sync();
    });
    let weatherLocation = null,
        weatherLocationRequest = 0;
    function weatherNotice(host, message) {
        const notice = host.querySelector('[data-weather-location-status]');
        if (notice) notice.textContent = message;
    }
    function resetWeather(host, message) {
        weatherLocationRequest++;
        weatherLocation = null;
        const button = host.querySelector('[data-weather-locate]');
        if (button) button.disabled = false;
        weatherNotice(host, message || 'Showing Kansas City.');
        weather(true);
    }
    function locateWeather(host) {
        const request = ++weatherLocationRequest;
        if (!window.isSecureContext || !navigator.geolocation) {
            resetWeather(
                host,
                'Location is unavailable in this browser. Open the site over HTTPS to use your location. Showing Kansas City.',
            );
            return;
        }
        host.querySelector('[data-weather-locate]').disabled = true;
        host.querySelector('[data-weather-reset]').hidden = false;
        weatherNotice(host, 'Finding your location…');
        navigator.geolocation.getCurrentPosition(
            (position) => {
                if (request !== weatherLocationRequest || !host.isConnected) return;
                host.querySelector('[data-weather-locate]').disabled = false;
                const lat = Number(position.coords.latitude.toFixed(2)),
                    lon = Number(position.coords.longitude.toFixed(2));
                if (!Number.isFinite(lat) || !Number.isFinite(lon) || Math.abs(lat) > 90 || Math.abs(lon) > 180) {
                    resetWeather(host, 'Your location could not be determined. Showing Kansas City.');
                    return;
                }
                weatherLocation = { lat, lon };
                weatherNotice(host, 'Using your approximate location for this tab.');
                weather(true);
            },
            (error) => {
                if (request !== weatherLocationRequest || !host.isConnected) return;
                resetWeather(
                    host,
                    error.code === 1
                        ? 'Location access was declined. Showing Kansas City.'
                        : error.code === 3
                          ? 'Location request timed out. Showing Kansas City.'
                          : 'Your location is unavailable. Showing Kansas City.',
                );
            },
            { enableHighAccuracy: false, timeout: 10000, maximumAge: 300000 },
        );
    }
    document.addEventListener('click', (event) => {
        const button = event.target.closest('[data-weather-locate], [data-weather-reset]');
        if (!button) return;
        const host = button.closest('[data-local-weather]');
        if (!host) return;
        if (button.matches('[data-weather-locate]')) locateWeather(host);
        else resetWeather(host);
    });
    async function weather(force = false) {
        const host = document.querySelector('[data-local-weather]');
        if (!host || (host.dataset.loading && !force)) return;
        host.dataset.loading = 'true';
        const point = weatherLocation;
        const locate = host.querySelector('[data-weather-locate]');
        if (locate) locate.hidden = false;
        const reset = host.querySelector('[data-weather-reset]');
        if (reset) reset.hidden = !point;
        weatherController?.abort();
        const controller = new AbortController();
        weatherController = controller;
        const status = host.querySelector('[data-weather-status]'),
            periods = host.querySelector('[data-weather-periods]');
        const title = host.querySelector('[data-weather-title]'),
            official = host.querySelector('[data-weather-link]');
        if (title) title.textContent = point ? 'Your local forecast' : 'Kansas City forecast';
        if (official)
            official.href =
                'https://forecast.weather.gov/MapClick.php?lat=' +
                (point?.lat ?? 39.0997) +
                '&lon=' +
                (point?.lon ?? -94.5786);
        periods.replaceChildren();
        status.textContent = 'Loading the National Weather Service forecast…';
        try {
            const query = point ? '?' + new URLSearchParams(point) : '';
            const response = await fetch('local/weather' + query, { signal: controller.signal });
            if (!response.ok) throw Error('Forecast unavailable');
            const data = await response.json();
            if (!host.isConnected || controller.signal.aborted) return;
            if (!Array.isArray(data.periods) || !data.periods.length) throw Error('No forecast');
            if (title)
                title.textContent = (txt(data.location) || (point ? 'Your location' : 'Kansas City')) + ' forecast';
            data.periods.forEach((p) => {
                const card = node('article', '', 'weather-period');
                const reading = node('div', '', 'weather-reading');
                if (window.myNewspaperWeatherIcons)
                    reading.append(window.myNewspaperWeatherIcons.create(p.summary, p.isDaytime !== false));
                reading.append(node('p', `${p.temperature}°${txt(p.unit)}`, 'weather-temperature'));
                card.append(
                    node('h3', txt(p.name)),
                    reading,
                    node('p', txt(p.summary)),
                    node('p', 'Wind ' + txt(p.wind), 'updated'),
                );
                periods.append(card);
            });
            const date = new Date(data.updated);
            status.textContent =
                (data.stale ? 'Last available forecast · may be out of date. ' : '') +
                'National Weather Service · updated ' +
                (Number.isNaN(date.getTime())
                    ? 'time unavailable'
                    : date.toLocaleString('en-US', { timeZone: 'America/Chicago', timeZoneName: 'short' }));
        } catch (_) {
            if (!host.isConnected || controller.signal.aborted) return;
            if (point) {
                weatherLocation = null;
                weatherNotice(host, 'A forecast is unavailable for this location. Showing Kansas City.');
                return weather(true);
            }
            status.textContent =
                'Forecast temporarily unavailable. Open the National Weather Service link for current conditions.';
        }
    }
    function schedule() {
        if (pending) return;
        pending = true;
        requestAnimationFrame(() => {
            pending = false;
            sync();
            weather();
        });
    }
    const markers =
        '[data-home-grid], [data-home-preferences], [data-news-url], [data-news-page], [data-news-muted], [data-local-weather], [data-personal-control]';
    new MutationObserver((records) => {
        if (
            records.some((r) =>
                [...r.addedNodes].some((n) => n.nodeType === 1 && (n.matches(markers) || n.querySelector(markers))),
            )
        )
            schedule();
    }).observe(document.body, { childList: true, subtree: true });
    document.addEventListener('mynewsfeed-saved-changed', (event) => {
        const p = prefs(),
            previous = JSON.stringify(p.read);
        list(event.detail).forEach((article) => {
            if (!safe(article.url)) return;
            p.read = p.read.filter((url) => url !== article.url);
            if (article.read === true) p.read.unshift(article.url);
        });
        p.read = p.read.slice(0, 2000);
        if (JSON.stringify(p.read) !== previous) save(PREFS, p, 'Reading status saved.');
    });
    window.addEventListener('storage', (e) => {
        if ([PREFS, TEAMS, null].includes(e.key)) schedule();
    });
    document.addEventListener('click', (e) => {
        if (e.target.closest('[data-sports-clear]')) schedule();
    });
    sync();
    weather();
})();
