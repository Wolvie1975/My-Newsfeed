// Local vector artwork: forecast text remains the accessible description.
(function (root) {
    'use strict';
    const sun = '<circle cx="24" cy="24" r="9"/><path d="M24 4v5m0 30v5M4 24h5m30 0h5M10 10l4 4m20 20 4 4M10 38l4-4m20-20 4-4"/>';
    const moon = '<path d="M33 7a18 18 0 1 0 8 28A17 17 0 0 1 33 7Z"/>';
    const cloud = '<path d="M12 33h25a8 8 0 0 0 0-16h-1a12 12 0 0 0-23-2 9 9 0 0 0-1 18Z"/>';
    const smallSun = '<circle cx="16" cy="14" r="7"/><path d="M16 2v3M4 14h3m1-9 2 2m14-2-2 2"/>';
    const smallMoon = '<path d="M21 3A12 12 0 1 0 30 22 12 12 0 0 1 21 3Z"/>';
    const icons = {
        sun, moon, cloud,
        'partly-day': smallSun + cloud,
        'partly-night': smallMoon + cloud,
        rain: cloud + '<path d="m17 38-2 6m11-6-2 6m11-6-2 6"/>',
        storm: cloud + '<path d="m25 30-6 9h7l-4 8m-8-9-2 5m24-5-2 5"/>',
        snow: cloud + '<path d="M15 38v8m-3-6 6 4m0-4-6 4m20-6v8m-3-6 6 4m0-4-6 4"/>',
        ice: cloud + '<path d="m17 37-4 5 4 5 4-5Zm17 0-4 5 4 5 4-5Z"/>',
        mixed: cloud + '<path d="m16 38-2 7m15-7v8m-3-6 6 4m0-4-6 4"/>',
        fog: cloud + '<path d="M6 38h36M10 44h28"/>',
        wind: '<path d="M5 15h26a6 6 0 1 0-6-6M5 24h33a5 5 0 1 0-5-5M5 33h22a6 6 0 1 1-6 6"/>',
        temperature: '<path d="M19 29V9a5 5 0 0 1 10 0v20a10 10 0 1 1-10 0Z"/><path d="M24 15v20m10-23h5m-5 8h5"/><circle cx="24" cy="36" r="3"/>'
    };
    function condition(summary, daytime) {
        const text = typeof summary === 'string' ? summary.toLowerCase() : '';
        if (/thunder|t-storm/.test(text)) return 'storm';
        if (/freezing|ice|icy/.test(text)) return 'ice';
        if (/sleet|wintry|winter.*mix|rain.*snow|snow.*rain/.test(text)) return 'mixed';
        if (/snow|flurr/.test(text)) return 'snow';
        if (/rain|shower|drizzle/.test(text)) return 'rain';
        if (/fog|mist|haze|smoke|dust/.test(text)) return 'fog';
        if (/partly|mostly sunny|mostly clear/.test(text)) return daytime ? 'partly-day' : 'partly-night';
        if (/cloud|overcast/.test(text)) return 'cloud';
        if (/sunny|clear/.test(text)) return daytime ? 'sun' : 'moon';
        if (/wind|breez/.test(text)) return 'wind';
        return 'temperature';
    }
    function create(summary, daytime) {
        const kind = condition(summary, daytime);
        const svg = document.createElementNS('http://www.w3.org/2000/svg', 'svg');
        svg.setAttribute('viewBox', '0 0 48 48');
        svg.setAttribute('class', 'weather-icon weather-icon--' + kind);
        svg.setAttribute('aria-hidden', 'true');
        svg.setAttribute('focusable', 'false');
        svg.setAttribute('fill', 'none');
        svg.setAttribute('stroke', 'currentColor');
        svg.setAttribute('stroke-width', '2.5');
        svg.setAttribute('stroke-linecap', 'round');
        svg.setAttribute('stroke-linejoin', 'round');
        // Only constant, locally authored SVG is inserted; forecast text is never markup.
        svg.innerHTML = icons[kind];
        return svg;
    }
    root.myNewspaperWeatherIcons = { create, condition };
})(window);
