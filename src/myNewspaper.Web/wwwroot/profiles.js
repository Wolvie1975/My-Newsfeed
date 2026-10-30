// Household profiles (profiles handoff 2.2, 6.1 and 6.2). Three jobs:
// 1. The gate for scripts: when profiles are on and this device's profile isn't open (a new browser session on an
//    "every visit" device, say), the server answers a script's fetch with 401 { gate: "/editions" }. Send the browser
//    to the picker, coming back to this page afterwards. Loaded before the other scripts so it sees their fetches.
// 2. The PIN pad on the edition picker. Each column is a plain form that posts the typed PIN; this replaces the PIN
//    field with a dialog (a bottom sheet on phones) and posts the four digits as JSON.
// 3. New edition setup (/editions/new): one step at a time with Back, Continue and a progress bar, posting as JSON so a
//    refused name or PIN keeps everything chosen. Without JavaScript the four steps are one long form.
(function () {
    'use strict';

    const nativeFetch = window.fetch.bind(window);
    // Pages the gate never sends to the picker (ProfileGate.IsExempt). Their scripts may still get the gate's 401, for
    // example the Saved count on the picker itself; going to the picker from there would loop.
    const exempt = /^\/(editions|profiles|login|account|admin)(\/|$)/i;
    let leaving = false;
    window.fetch = async function (input, init) {
        const response = await nativeFetch(input, init);
        if (response.status !== 401 || leaving || exempt.test(location.pathname)) return response;
        try {
            const url = new URL(response.url || String(input), location.href);
            if (url.origin !== location.origin) return response;
            const body = await response.clone().json();
            if (body && body.gate === '/editions') {
                leaving = true;
                location.assign('/editions?returnUrl=' + encodeURIComponent(location.pathname + location.search));
            }
        } catch (_) {
            // Not JSON, or not the gate: the caller handles its own 401.
        }
        return response;
    };

    // ---- settings ---------------------------------------------------------------------------------------------
    // window.myNewspaperPrefs has localStorage's getItem/setItem/removeItem, so the page scripts keep their keys and
    // value formats. With a profile open, the keys below read and write that profile's settings (saved to the server
    // 400 ms after the last change); everything else, and everything when no profile is open, stays in localStorage.
    const LEGACY = {
        theme: { setting: 'theme', text: true },
        'mynewsfeed-color-scheme': { setting: 'colorScheme', text: true },
        'mynewsfeed-personal-v1': { setting: 'home' },
        'mynewsfeed-sports-teams-v1': { setting: 'teams' },
        'mynewsfeed-watch-services-v1': { setting: 'services' },
        'mynewsfeed-settings-order-v1': { setting: 'settingsOrder' },
        'mydailyfeed-scoreboard-teams-v1': { setting: 'scoreboardTeams' },
    };
    const HOME_KEYS = ['order', 'hidden', 'teamsOnly'];
    let page = {};
    try {
        page = JSON.parse(document.getElementById('mnp-profile')?.textContent || '{}') || {};
    } catch (_) {}
    const profile = page.profile && typeof page.profile.settings === 'object' ? page.profile : null;
    const settings = profile ? profile.settings : {};
    let patch = {},
        timer = null,
        token = null;

    const local = {
        getItem: (key) => {
            try {
                return localStorage.getItem(key);
            } catch (_) {
                return null;
            }
        },
        setItem: (key, value) => localStorage.setItem(key, value),
        removeItem: (key) => {
            try {
                localStorage.removeItem(key);
            } catch (_) {}
        },
    };

    // A stored browser value as the profile setting it becomes (the home object keeps only the keys the server knows).
    function toSetting(map, raw) {
        if (map.text) return raw;
        const value = JSON.parse(raw);
        if (map.setting !== 'home' || !value || typeof value !== 'object') return value;
        const home = {};
        HOME_KEYS.forEach((k) => {
            if (k in value) home[k] = value[k];
        });
        return home;
    }

    async function getToken() {
        if (!token) {
            const response = await nativeFetch('/api/me', { credentials: 'same-origin', cache: 'no-store' });
            if (!response.ok) throw new Error('profile');
            token = (await response.json()).token;
        }
        return token;
    }
    async function send(body, path, method, keepalive) {
        const response = await nativeFetch(path, {
            method,
            credentials: 'same-origin',
            keepalive: !!keepalive,
            headers: { 'Content-Type': 'application/json', 'X-Saved-CSRF': await getToken() },
            body: JSON.stringify(body),
        });
        if (!response.ok) throw new Error('settings');
        return response.json();
    }
    function flush(keepalive) {
        clearTimeout(timer);
        timer = null;
        const body = patch;
        patch = {};
        if (!Object.keys(body).length) return;
        send(body, '/api/me/settings', 'PUT', keepalive)
            .then(() => document.dispatchEvent(new CustomEvent('myNewspaper:settings-saved', { detail: body })))
            .catch(() => {
                document.dispatchEvent(new CustomEvent('myNewspaper:settings-failed', { detail: body }));
            });
    }

    window.myNewspaperPrefs = profile
        ? {
              profile: true,
              defaults: page.defaults || {},
              getItem(key) {
                  const map = LEGACY[key];
                  if (!map) return local.getItem(key);
                  const value = settings[map.setting];
                  if (value === undefined || value === null) return null;
                  return map.text ? String(value) : JSON.stringify(value);
              },
              setItem(key, raw) {
                  const map = LEGACY[key];
                  if (!map) return local.setItem(key, raw);
                  const value = toSetting(map, String(raw));
                  settings[map.setting] = value;
                  patch[map.setting] = value;
                  if (key === 'theme') document.documentElement.setAttribute('data-theme-mode', value);
                  clearTimeout(timer);
                  timer = setTimeout(flush, 400);
              },
              removeItem(key) {
                  const map = LEGACY[key];
                  if (!map) return local.removeItem(key);
                  delete settings[map.setting];
                  patch[map.setting] = null;
                  clearTimeout(timer);
                  timer = setTimeout(flush, 400);
              },
          }
        : { profile: false, defaults: page.defaults || {}, ...local };
    window.addEventListener('pagehide', () => flush(true));

    // One-time import: the first time a profile opens in a browser that still has the old keys, they fill whatever
    // the profile doesn't have yet; then they're removed, so the next person on this device doesn't inherit them.
    async function importBrowser() {
        if (!profile || profile.browserImportAt) return;
        const values = {};
        Object.entries(LEGACY).forEach(([key, map]) => {
            const raw = local.getItem(key);
            if (raw === null) return;
            try {
                values[map.setting] = toSetting(map, raw);
            } catch (_) {}
        });
        // Only a browser that has old settings uses up the profile's one import.
        if (!Object.keys(values).length) return;
        try {
            const result = await send(values, '/api/me/import-browser', 'POST');
            profile.browserImportAt = new Date().toISOString();
            Object.keys(LEGACY).forEach((key) => local.removeItem(key));
            const before = JSON.stringify(settings);
            Object.keys(settings).forEach((k) => delete settings[k]);
            Object.assign(settings, result.settings || {});
            // The page was drawn with the profile's old settings; draw it again with the imported ones.
            if (JSON.stringify(settings) !== before) location.reload();
        } catch (_) {
            // Try again on the next page.
        }
    }
    importBrowser();

    // ---- PIN pad ------------------------------------------------------------------------------------------------

    let form = null;
    let digits = '';
    let busy = false;

    const dialog = () => document.querySelector('[data-pin-dialog]');

    function show() {
        const d = dialog();
        d.querySelectorAll('[data-pin-dots] span').forEach((dot, i) =>
            dot.classList.toggle('is-on', i < digits.length),
        );
    }
    function status(text) {
        dialog().querySelector('[data-pin-status]').textContent = text;
    }
    function lockPad(locked) {
        dialog()
            .querySelectorAll('[data-pin-key]')
            .forEach((key) => {
                key.disabled = locked;
            });
    }

    function open(target) {
        const d = dialog();
        form = target;
        digits = '';
        busy = false;
        const name = form.dataset.name || '';
        d.style.setProperty('--ed', /^#[0-9a-fA-F]{6}$/.test(form.dataset.color) ? form.dataset.color : '#0051ba');
        d.querySelector('[data-pin-tile]').textContent = (name.trim()[0] || '?').toUpperCase();
        d.querySelector('[data-pin-title]').textContent = 'Enter ' + name + '’s PIN';
        status('');
        lockPad(false);
        show();
        d.showModal();
        d.querySelector('[data-pin-key="1"]').focus();
    }
    function close() {
        const d = dialog();
        if (d.open) d.close();
    }

    async function submit() {
        busy = true;
        lockPad(true);
        status('Checking…');
        const data = new FormData(form);
        data.set('pin', digits);
        let response, body;
        try {
            response = await nativeFetch(form.action, {
                method: 'POST',
                body: data,
                credentials: 'same-origin',
                headers: { Accept: 'application/json' },
            });
            body = await response.json().catch(() => ({}));
        } catch (_) {
            status('That didn’t go through. Check your connection and try again.');
            digits = '';
            show();
            busy = false;
            lockPad(false);
            return;
        }
        if (response.ok && body.opened) {
            status('Opening…');
            location.assign(body.returnUrl || '/');
            return;
        }
        if (response.status === 409 && body.href) {
            location.assign(body.href);
            return;
        }
        digits = '';
        show();
        busy = false;
        if (response.status === 423) {
            status('Locked. The admin has been told and will unlock it.');
            return;
        }
        lockPad(false);
        if (response.status === 401) {
            const dots = dialog().querySelector('[data-pin-dots]');
            dots.classList.remove('is-wrong');
            void dots.offsetWidth;
            dots.classList.add('is-wrong');
            const left = body.triesLeft;
            status('Wrong PIN' + (left ? ' · ' + left + (left === 1 ? ' try left' : ' tries left') : ''));
        } else status(body.error || 'Something went wrong. Reload the page and try again.');
    }

    function press(key) {
        if (busy || !form) return;
        if (key === 'del') digits = digits.slice(0, -1);
        else if (/^\d$/.test(key) && digits.length < 4) digits += key;
        show();
        status(digits.length ? digits.length + ' of 4 digits' : '');
        if (digits.length === 4) submit();
    }

    function enhance() {
        const d = dialog();
        if (!d || typeof d.showModal !== 'function') return;
        d.closest('.picker').classList.add('has-pad');
        document.querySelectorAll('[data-pin-form] .edition__pin').forEach((input) => {
            input.required = false;
        });
    }

    document.addEventListener('submit', (event) => {
        const target = event.target.closest('[data-pin-form]');
        const d = dialog();
        if (!target || !d || typeof d.showModal !== 'function' || target.dataset.needsPin) return;
        event.preventDefault();
        open(target);
    });
    document.addEventListener('click', (event) => {
        const key = event.target.closest('[data-pin-key]');
        if (key) return press(key.dataset.pinKey);
        if (event.target.closest('[data-pin-cancel]')) return close();
        // A click on the backdrop lands on the dialog element itself.
        if (event.target === dialog()) close();
    });
    document.addEventListener('keydown', (event) => {
        const d = dialog();
        if (!d || !d.open) return;
        if (/^\d$/.test(event.key)) {
            event.preventDefault();
            press(event.key);
        } else if (event.key === 'Backspace') {
            event.preventDefault();
            press('del');
        }
    });

    enhance();
    if (window.Blazor?.addEventListener) window.Blazor.addEventListener('enhancedload', enhance);
})();

(function () {
    'use strict';

    // Runs on load and after Blazor's enhanced navigation (the picker's link arrives that way), once per form.
    function setup() {
        const form = document.querySelector('[data-setup]');
        if (!form || form.dataset.setupReady) return;
        form.dataset.setupReady = 'true';
        const steps = [...form.querySelectorAll('[data-setup-step]')];
        const find = (selector) => form.querySelector(selector);
        const back = find('[data-setup-back]'),
            next = find('[data-setup-next]'),
            submit = find('[data-setup-submit]'),
            bar = find('[data-setup-bar]'),
            count = find('[data-setup-count]'),
            error = find('[data-setup-error]'),
            name = find('[data-setup-name]'),
            pin = find('[data-setup-pin]'),
            pinConfirm = find('[data-setup-pin-confirm]'),
            pinStatus = find('[data-setup-pin-status]');
        const maxServices = 20;
        let step = 0;

        const firstName = () => name.value.trim();
        const pinsOk = () => /^\d{4}$/.test(pin.value) && pin.value === pinConfirm.value;
        // Continue stays off on step 1 until there's a name and the same 4-digit PIN twice (handoff 6.2).
        const ready = () => step > 0 || (firstName().length > 0 && pinsOk());

        function paint() {
            steps.forEach((s, i) => (s.hidden = i !== step));
            back.hidden = step === 0;
            next.hidden = step === steps.length - 1;
            submit.hidden = step !== steps.length - 1;
            next.disabled = !ready();
            count.textContent = 'Step ' + (step + 1) + ' of ' + steps.length;
            bar.querySelectorAll('span').forEach((s, i) => s.classList.toggle('is-done', i <= step));
            submit.textContent = firstName() ? 'Open ' + firstName() + '’s edition' : 'Open my edition';
        }

        function go(to) {
            step = Math.max(0, Math.min(steps.length - 1, to));
            paint();
            const heading = steps[step].querySelector('h1, h2');
            heading.focus();
            heading.scrollIntoView({ block: 'nearest' });
        }

        function pinNote() {
            if (pin.value && !/^\d*$/.test(pin.value + pinConfirm.value)) pinStatus.textContent = 'A PIN is 4 digits.';
            else if (pin.value.length === 4 && pinConfirm.value.length === 4 && pin.value !== pinConfirm.value)
                pinStatus.textContent = 'The two PINs don’t match.';
            else pinStatus.textContent = '';
        }

        function filter(input) {
            const term = input.value.trim().toLowerCase();
            const within = input.closest('[data-setup-step]');
            within.querySelectorAll('[data-setup-option]').forEach((o) => {
                o.hidden = term !== '' && !o.dataset.setupOption.includes(term);
            });
            // Searching opens every group, as on Settings, and hides groups with no match.
            within.querySelectorAll('[data-setup-group]').forEach((g) => {
                if (term) g.open = true;
                g.hidden = term !== '' && !g.querySelector('[data-setup-option]:not([hidden])');
            });
        }

        function limitServices() {
            const boxes = [...form.querySelectorAll('[data-setup-service]')];
            const full = boxes.filter((b) => b.checked).length >= maxServices;
            boxes.forEach((b) => (b.disabled = full && !b.checked));
        }

        form.addEventListener('input', (event) => {
            const target = event.target;
            if (target === name) find('[data-setup-initial]').textContent = (firstName()[0] || '?').toUpperCase();
            if (target === pin || target === pinConfirm) pinNote();
            if (target.matches('[data-setup-color]')) form.style.setProperty('--ed', target.value);
            if (target.matches('[data-setup-filter]')) filter(target);
            if (target.matches('[data-setup-service]')) limitServices();
            paint();
        });

        form.addEventListener('click', (event) => {
            const up = event.target.closest('[data-setup-up]');
            if (!up) return;
            const item = up.closest('li');
            if (item.previousElementSibling) item.parentElement.insertBefore(item, item.previousElementSibling);
            up.focus();
        });
        back.addEventListener('click', () => go(step - 1));
        next.addEventListener('click', () => {
            if (ready()) go(step + 1);
        });
        // Enter in a text field moves on instead of submitting a half-finished edition.
        form.addEventListener('keydown', (event) => {
            if (event.key !== 'Enter' || step === steps.length - 1) return;
            if (!event.target.matches('input:not([type=checkbox]):not([type=radio])')) return;
            event.preventDefault();
            if (ready()) go(step + 1);
        });

        form.addEventListener('submit', async (event) => {
            event.preventDefault();
            if (!ready()) return go(0);
            submit.disabled = true;
            error.hidden = true;
            let response = null,
                body;
            try {
                response = await fetch(form.action, {
                    method: 'POST',
                    body: new FormData(form),
                    credentials: 'same-origin',
                    headers: { Accept: 'application/json' },
                });
                body = await response.json().catch(() => ({}));
            } catch (_) {
                body = { error: 'That didn’t go through. Check your connection and try again.' };
            }
            if (response && response.ok && body.opened) {
                location.assign(body.returnUrl || '/');
                return;
            }
            submit.disabled = false;
            error.textContent = body.error || 'Something went wrong. Please try again.';
            error.hidden = false;
            // Every refusal is about the name or the PIN, which are on step 1.
            go(0);
        });

        form.querySelectorAll('[data-setup-up]').forEach((b) => (b.hidden = false));
        form.classList.add('is-stepped');
        bar.hidden = false;
        limitServices();
        paint();
    }

    setup();
    if (window.Blazor?.addEventListener) window.Blazor.addEventListener('enhancedload', setup);
})();
