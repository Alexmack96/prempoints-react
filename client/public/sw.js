// Deliberately does not cache. A service worker is what makes the app
// installable, and that is the only job wanted here: an offline cache would
// mean a player running last week's bundle against this week's API with no
// obvious way to tell, which is a worse problem than not working on the tube.
//
// Requests pass straight through. If offline support is ever wanted, reach for
// vite-plugin-pwa rather than growing this file, because the hard part is
// invalidation and that is what the plugin already solves.
self.addEventListener('install', () => self.skipWaiting());
self.addEventListener('activate', (event) => event.waitUntil(self.clients.claim()));
self.addEventListener('fetch', () => {});
