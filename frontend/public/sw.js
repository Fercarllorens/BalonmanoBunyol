const CACHE_NAME = 'cb-bunol-shell-v1'
const SHELL_URL = '/'
const STATIC_FILE = /\.(?:js|css|svg|png|jpg|jpeg|webp|woff2?)$/i

self.addEventListener('install', (event) => {
  event.waitUntil(caches.open(CACHE_NAME).then((cache) => cache.add(SHELL_URL)))
  self.skipWaiting()
})

self.addEventListener('activate', (event) => {
  event.waitUntil(
    caches.keys().then((keys) => Promise.all(keys.filter((key) => key.startsWith('cb-bunol-') && key !== CACHE_NAME).map((key) => caches.delete(key))))
  )
  self.clients.claim()
})

self.addEventListener('fetch', (event) => {
  const request = event.request
  const url = new URL(request.url)

  if (request.method !== 'GET' || url.origin !== self.location.origin || url.pathname.startsWith('/api/')) return

  if (url.pathname.replace(/\/$/, '') === '/auth') return

  if (request.mode === 'navigate') {
    event.respondWith(fetch(request).catch(() => caches.match(SHELL_URL)))
    return
  }

  if (STATIC_FILE.test(url.pathname)) {
    event.respondWith(
      caches.match(request).then((cached) => cached || fetch(request).then((response) => {
        if (response.ok) {
          const copy = response.clone()
          void caches.open(CACHE_NAME).then((cache) => cache.put(request, copy))
        }
        return response
      }))
    )
  }
})
