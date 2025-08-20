// Web Push Service Worker
self.addEventListener('push', event => {
    console.log('[webpush-sw.js] Push received:', event);
    
    let notificationData = {};
    
    if (event.data) {
        try {
            notificationData = event.data.json();
        } catch (e) {
            notificationData = {
                title: 'Nueva notificación',
                body: event.data.text(),
                icon: '/Plugins/Misc.PushNotifications/logo.jpg',
                data: { url: '/' }
            };
        }
    }
    
    const title = notificationData.title || 'Nueva notificación';
    const options = {
        body: notificationData.body || '',
        icon: notificationData.icon || '/Plugins/Misc.PushNotifications/logo.jpg',
        badge: '/Plugins/Misc.PushNotifications/logo.jpg',
        data: notificationData.data || { url: '/' },
        tag: 'webpush-notification',
        requireInteraction: false,
        actions: [
            {
                action: 'open',
                title: 'Abrir'
            },
            {
                action: 'close',
                title: 'Cerrar'
            }
        ]
    };
    
    event.waitUntil(
        self.registration.showNotification(title, options)
    );
});

self.addEventListener('notificationclick', event => {
    console.log('[webpush-sw.js] Notification click received:', event);
    
    event.notification.close();
    
    if (event.action === 'close') {
        return;
    }
    
    const urlToOpen = event.notification.data?.url || '/';
    
    event.waitUntil(
        clients.matchAll({ type: 'window', includeUncontrolled: true }).then(clientList => {
            // Check if there's already a window/tab open with the target URL
            for (const client of clientList) {
                if (client.url === urlToOpen && 'focus' in client) {
                    return client.focus();
                }
            }
            
            // If no window/tab is open, open a new one
            if (clients.openWindow) {
                return clients.openWindow(urlToOpen);
            }
        })
    );
});

self.addEventListener('notificationclose', event => {
    console.log('[webpush-sw.js] Notification closed:', event);
});

// Handle background sync if needed
self.addEventListener('sync', event => {
    console.log('[webpush-sw.js] Background sync:', event);
});

// Install event
self.addEventListener('install', event => {
    console.log('[webpush-sw.js] Service worker installing...');
    self.skipWaiting();
});

// Activate event
self.addEventListener('activate', event => {
    console.log('[webpush-sw.js] Service worker activating...');
    event.waitUntil(self.clients.claim());
});
