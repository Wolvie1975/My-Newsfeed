// Household profiles (profiles handoff 2.2 and 6.1). Two jobs:
// 1. The gate for scripts: when profiles are on and this device's profile isn't open (a new browser session on an
//    "every visit" device, say), the server answers a script's fetch with 401 { gate: "/editions" }. Send the browser
//    to the picker, coming back to this page afterwards. Loaded before the other scripts so it sees their fetches.
// 2. The PIN pad on the edition picker. Each column is a plain form that posts the typed PIN; this replaces the PIN
//    field with a dialog (a bottom sheet on phones) and posts the four digits as JSON.
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
