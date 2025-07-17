const CACHE_NAME = 'nopcommerce-cache-v1';
const precacheAssets = [
  '/',
  '/Themes/MarketTheme/Content/css/styles.css', 
  '/Themes/MarketTheme/Content/css/Phoenix.css'
];

self.addEventListener('install', event => {
  event.waitUntil(
    caches.open(CACHE_NAME)
      .then(cache => {
        console.log('Opened cache');
        return cache.addAll(precacheAssets);
      })
  );
});

self.addEventListener('fetch', event => {
  const request = event.request;

  if (request.mode === 'navigate' || request.destination === 'document') {
    event.respondWith(
      fetch(request).then(response => {
        if (response.ok) {
          caches.open(CACHE_NAME).then(cache => {
            cache.put(request, response.clone());
          });
          return response;
        } else {
          return caches.match(request).then(cacheResponse => {
            if (cacheResponse) {
              return cacheResponse; 
            }
            return new Response('Offline', {
              status: 503,
              statusText: 'Service Unavailable',
              headers: { 'Content-Type': 'text/plain' }
            });
          });
        }
      }).catch(error => {
        console.log('Network request failed, falling back to cache', error);
        return caches.match(request).then(cacheResponse => {
          if (cacheResponse) {
            return cacheResponse; 
          }
          return new Response('Offline', {
            status: 503,
            statusText: 'Service Unavailable',
            headers: { 'Content-Type': 'text/plain' }
          });
        });
      })
    );
  } else {
    event.respondWith(
      fetch(request).then(response => {
        if (response.ok) {
          caches.open(CACHE_NAME).then(cache => {
            cache.put(request, response.clone());
          });
          return response;
        }
        return caches.match(request).then(cacheResponse => {
          return cacheResponse || new Response('Offline', {
            status: 503,
            statusText: 'Service Unavailable',
            headers: { 'Content-Type': 'text/plain' }
          });
        });
      }).catch(error => {
        console.log('Network request failed for asset, falling back to cache', error);
        return caches.match(request).then(cacheResponse => {
          return cacheResponse || new Response('Offline', {
            status: 503,
            statusText: 'Service Unavailable',
            headers: { 'Content-Type': 'text/plain' }
          });
        });
      })
    );
  }
});

self.addEventListener('activate', event => {
  const cacheWhitelist = [CACHE_NAME]; 
  event.waitUntil(
    caches.keys().then(cacheNames => {
      return Promise.all(
        cacheNames.map(cacheName => {
          if (cacheWhitelist.indexOf(cacheName) === -1) {
            return caches.delete(cacheName); 
          }
        })
      );
    })
  );
});