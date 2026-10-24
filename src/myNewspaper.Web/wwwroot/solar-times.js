// Approximate sunrise/sunset from NOAA's fractional-year equations:
// https://gml.noaa.gov/grad/solcalc/solareqns.PDF
// No network requests or browser location access are required.
(function (global) {
    'use strict';
    var dayMs = 86400000;
    var rad = Math.PI / 180;
    function times(now, location) {
        if (!Number.isFinite(location.latitude) || Math.abs(location.latitude) >= 90 ||
            !Number.isFinite(location.longitude) || Math.abs(location.longitude) > 180) {
            throw new RangeError('Invalid solar location');
        }
        var parts = new Intl.DateTimeFormat('en-US', {
            timeZone: location.timeZone, year: 'numeric', month: 'numeric', day: 'numeric'
        }).formatToParts(now);
        function part(type) { return Number(parts.find(function (p) { return p.type === type; }).value); }
        var year = part('year');
        var midnight = Date.UTC(year, part('month') - 1, part('day'));
        var yearStart = Date.UTC(year, 0, 1);
        var daysInYear = (Date.UTC(year + 1, 0, 1) - yearStart) / dayMs;
        var gamma = 2 * Math.PI * ((midnight - yearStart) / dayMs) / daysInYear;
        var eq = 229.18 * (0.000075 + 0.001868 * Math.cos(gamma) - 0.032077 * Math.sin(gamma)
            - 0.014615 * Math.cos(2 * gamma) - 0.040849 * Math.sin(2 * gamma));
        var decl = 0.006918 - 0.399912 * Math.cos(gamma) + 0.070257 * Math.sin(gamma)
            - 0.006758 * Math.cos(2 * gamma) + 0.000907 * Math.sin(2 * gamma)
            - 0.002697 * Math.cos(3 * gamma) + 0.00148 * Math.sin(3 * gamma);
        var lat = location.latitude * rad;
        var cosine = Math.cos(90.833 * rad) / (Math.cos(lat) * Math.cos(decl)) - Math.tan(lat) * Math.tan(decl);
        if (cosine > 1) return { sunrise: null, sunset: null, polarTheme: 'dark' };
        if (cosine < -1) return { sunrise: null, sunset: null, polarTheme: 'light' };
        var angle = Math.acos(cosine) / rad;
        var noon = 720 - 4 * location.longitude - eq;
        return {
            sunrise: new Date(midnight + (noon - 4 * angle) * 60000),
            sunset: new Date(midnight + (noon + 4 * angle) * 60000)
        };
    }
    function theme(now, location) {
        var day = times(now, location);
        return day.polarTheme || (now >= day.sunrise && now < day.sunset ? 'light' : 'dark');
    }
    global.myNewspaperSolar = {
        times: times, theme: theme,
        location: { latitude: 39.0997, longitude: -94.5786, timeZone: 'America/Chicago' }
    };
})(typeof window === 'undefined' ? globalThis : window);
