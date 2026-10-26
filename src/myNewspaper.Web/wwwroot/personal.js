/* Personal homepage, teams and weather. Copies stay on this device; no account or server writes. */
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
    let storage = true,
        pending = false,
        weatherController;
    try {
        localStorage.setItem('mynewsfeed-personal-probe', '1');
        localStorage.removeItem('mynewsfeed-personal-probe');
    } catch (_) {
        storage = false;
    }
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
            // The broadsheet layout fixes where each section sits; saved preferences only hide sections.
            defaults.forEach((key) => {
                const section = grid.querySelector(`[data-home-section="${key}"]`);
                if (section)
                    section.hidden = p.hidden.includes(key) || (key === 'digital' && p.hidden.includes('movies'));
            });
            grid.querySelectorAll('[data-home-f1]').forEach((section) => (section.hidden = p.hidden.includes('f1')));
            const empty = document.querySelector('[data-home-empty]');
            if (empty) empty.hidden = grid.querySelector('[data-home-section]:not([hidden])') !== null;
            const rows = [...grid.querySelectorAll('[data-home-team-item]')];
            rows.forEach((row) => (row.hidden = p.teamsOnly && !matchesTeams(row, selected)));
            const noTeams = document.querySelector('[data-home-teams-empty]');
            if (noTeams) noTeams.hidden = !p.teamsOnly || !rows.length || rows.some((row) => !row.hidden);
        }
        document.querySelectorAll('[data-home-teams-only]').forEach((n) => (n.checked = p.teamsOnly));
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
    }
    document.addEventListener('click', (e) => {
        const p = prefs();
        if (e.target.closest('[data-home-reset]')) {
            p.order = defaults;
            p.hidden = [];
            p.teamsOnly = false;
            save(PREFS, p, 'Default homepage restored.');
        }
    });
    document.addEventListener('change', (e) => {
        const p = prefs(),
            n = e.target;
        if (n.matches('[data-home-teams-only]')) {
            p.teamsOnly = n.checked;
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
    const markers = '[data-home-grid], [data-home-preferences], [data-local-weather], [data-personal-control]';
    new MutationObserver((records) => {
        if (
            records.some((r) =>
                [...r.addedNodes].some((n) => n.nodeType === 1 && (n.matches(markers) || n.querySelector(markers))),
            )
        )
            schedule();
    }).observe(document.body, { childList: true, subtree: true });
    window.addEventListener('storage', (e) => {
        if ([PREFS, TEAMS, null].includes(e.key)) schedule();
    });
    document.addEventListener('click', (e) => {
        if (e.target.closest('[data-sports-clear]')) schedule();
    });
    sync();
    weather();
})();
