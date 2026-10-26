// The YouTube page's featured carousel. It holds still until Play is pressed; then it advances every seven seconds
// with a progress bar, and pauses while the page is hidden. Arrows and the title tabs switch videos at any time.
// Without JavaScript the first featured video shows and the controls stay hidden.
(function () {
    'use strict';
    const SECONDS = 7;
    const TICK = 250;
    const state = new WeakMap();

    function show(carousel, index) {
        const s = state.get(carousel);
        const slides = carousel.querySelectorAll('[data-yt-slide]');
        s.index = (index + slides.length) % slides.length;
        s.ticks = 0;
        slides.forEach((slide, i) => {
            slide.hidden = i !== s.index;
        });
        carousel.querySelectorAll('[data-yt-pick]').forEach((tab, i) => {
            tab.setAttribute('aria-current', String(i === s.index));
        });
        const position = carousel.querySelector('[data-yt-index]');
        if (position) position.textContent = String(s.index + 1);
        bar(carousel);
    }

    function bar(carousel) {
        const s = state.get(carousel);
        const width = s.playing ? Math.min(100, (s.ticks * TICK * 100) / (SECONDS * 1000)) + '%' : '0%';
        carousel.querySelectorAll('[data-yt-bar]').forEach((b) => {
            b.style.width = width;
        });
    }

    function setPlaying(carousel, playing) {
        const s = state.get(carousel);
        s.playing = playing;
        s.ticks = 0;
        const toggle = carousel.querySelector('[data-yt-toggle]');
        toggle.setAttribute('aria-pressed', String(playing));
        toggle.setAttribute('aria-label', playing ? 'Pause rotation' : 'Play rotation');
        // Announce slide changes only when the viewer is choosing them, not every few seconds.
        carousel.querySelector('[data-yt-live]').setAttribute('aria-live', playing ? 'off' : 'polite');
        bar(carousel);
    }

    function init() {
        document.querySelectorAll('[data-yt-carousel]').forEach((carousel) => {
            if (state.has(carousel)) return;
            state.set(carousel, { index: 0, ticks: 0, playing: false });
            carousel.querySelectorAll('[data-yt-controls], [data-yt-tabs], [data-yt-position]').forEach((n) => {
                n.hidden = false;
            });
            setPlaying(carousel, false);
        });
    }

    document.addEventListener('click', (event) => {
        const carousel = event.target.closest('[data-yt-carousel]');
        if (!carousel || !state.has(carousel)) return;
        const s = state.get(carousel);
        const step = event.target.closest('[data-yt-step]');
        const pick = event.target.closest('[data-yt-pick]');
        if (step) show(carousel, s.index + Number(step.dataset.ytStep));
        else if (pick) show(carousel, Number(pick.dataset.ytPick));
        else if (event.target.closest('[data-yt-toggle]')) setPlaying(carousel, !s.playing);
    });

    setInterval(() => {
        if (document.hidden) return;
        document.querySelectorAll('[data-yt-carousel]').forEach((carousel) => {
            const s = state.get(carousel);
            if (!s || !s.playing) return;
            s.ticks++;
            if (s.ticks * TICK >= SECONDS * 1000) show(carousel, s.index + 1);
            else bar(carousel);
        });
    }, TICK);

    init();
    if (window.Blazor) window.Blazor.addEventListener('enhancedload', init);
})();
