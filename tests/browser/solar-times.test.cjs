const { test } = require('node:test');
const assert = require('node:assert/strict');
require('../../src/myNewspaper.Web/wwwroot/solar-times.js');
const solar = globalThis.myNewspaperSolar;
const location = solar.location;

test('switches at sunrise and sunset, with dark mode before dawn', () => {
    const day = solar.times(new Date('2026-10-17T17:00:00Z'), location);
    assert.equal(solar.theme(new Date(day.sunrise - 1), location), 'dark');
    assert.equal(solar.theme(day.sunrise, location), 'light');
    assert.equal(solar.theme(new Date(day.sunset - 1), location), 'light');
    assert.equal(solar.theme(day.sunset, location), 'dark');
});

test('uses the city date near UTC midnight', () => {
    const noon = solar.times(new Date('2026-10-17T17:00:00Z'), location);
    const evening = solar.times(new Date('2026-10-18T01:00:00Z'), location);
    assert.equal(noon.sunset.getTime(), evening.sunset.getTime());
    assert.equal(solar.theme(new Date('2026-10-18T01:00:00Z'), location), 'dark');
});

test('summer days are longer and winter sunset can precede UTC midnight', () => {
    const summer = solar.times(new Date('2026-06-21T17:00:00Z'), location);
    const winter = solar.times(new Date('2026-12-21T18:00:00Z'), location);
    assert.ok(summer.sunset - summer.sunrise > 14 * 3600000);
    assert.ok(winter.sunset - winter.sunrise < 10 * 3600000);
    assert.equal(summer.sunset.toISOString().slice(0, 10), '2026-06-22');
    assert.equal(winter.sunset.toISOString().slice(0, 10), '2026-12-21');
});

test('DST changes affect labels but do not shift astronomy by an hour', () => {
    const before = solar.times(new Date('2026-10-31T17:00:00Z'), location);
    const after = solar.times(new Date('2026-11-01T17:00:00Z'), location);
    assert.ok(Math.abs((after.sunset - before.sunset) - 86400000) < 5 * 60000);
});

test('supports polar seasons and rejects invalid coordinates', () => {
    const arctic = { latitude: 80, longitude: 0, timeZone: 'UTC' };
    assert.equal(solar.theme(new Date('2026-06-21T12:00:00Z'), arctic), 'light');
    assert.equal(solar.theme(new Date('2026-12-21T12:00:00Z'), arctic), 'dark');
    assert.throws(() => solar.times(new Date(), { ...location, latitude: NaN }), RangeError);
});
