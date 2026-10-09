// Keep only the offline screen and app identity assets; schedules and admin responses are never cached here.
const CACHE = 'mynewsfeed-pwa-v1';
const OFFLINE = '/offline.html';
const ASSETS = [OFFLINE, '/manifest.webmanifest', '/icons/icon-192.png', '/icons/icon-512.png', '/icons/maskable-512.png'];
const PUBLIC_PAGES = new Set(['/', '/news', '/events', '/results', '/movies', '/tv', '/videos', '/saved']);
self.addEventListener('install', event => {
    event.waitUntil(caches.open(CACHE).then(cache => cache.addAll(ASSETS)).then(() => self.skipWaiting()));
});
self.addEventListener('activate', event => {
    event.waitUntil(caches.keys().then(keys => Promise.all(keys.filter(key => key.startsWith('mynewsfeed-pwa-') && key !== CACHE)
        .map(key => caches.delete(key)))).then(() => self.clients.claim()));
});
self.addEventListener('fetch', event => {
    const url = new URL(event.request.url);
    if (url.origin !== self.location.origin || event.request.method !== 'GET') return;
    if (event.request.mode === 'navigate' && PUBLIC_PAGES.has(url.pathname.replace(/\/$/, '') || '/')) {
        event.respondWith(fetch(event.request).catch(() => caches.open(CACHE).then(cache => cache.match(OFFLINE))));
    } else if (ASSETS.includes(url.pathname) && !url.search) {
        event.respondWith(fetch(event.request).catch(() => caches.open(CACHE).then(cache => cache.match(url.pathname))));
    }
});
