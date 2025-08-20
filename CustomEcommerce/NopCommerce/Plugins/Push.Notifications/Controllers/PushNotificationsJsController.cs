using Microsoft.AspNetCore.Mvc;
using Nop.Plugin.Misc.PushNotifications.Models;
using Nop.Services.Configuration;
using System.Text;
using System.Threading.Tasks;

namespace Nop.Plugin.Misc.PushNotifications.Controllers
{
    public class PushNotificationsJsController : Controller
    {
        private readonly ISettingService _settingService;

        public PushNotificationsJsController(ISettingService settingService)
        {
            _settingService = settingService;
        }

        public async Task<IActionResult> FirebaseMessagingSw()
        {
            var settings = await _settingService.LoadSettingAsync<PushNotificationsSettings>();
            var firebaseConfig = settings.FirebaseConfig ?? "{}";
            var iconUrl = settings.NotificationIconUrl ?? "/Plugins/Misc.PushNotifications/logo.jpg";

            var script = $@"
// Import and configure the Firebase SDK
importScripts('https://www.gstatic.com/firebasejs/9.23.0/firebase-app-compat.js');
importScripts('https://www.gstatic.com/firebasejs/9.23.0/firebase-messaging-compat.js');

const firebaseConfig = {firebaseConfig};

firebase.initializeApp(firebaseConfig);

const messaging = firebase.messaging();

messaging.onBackgroundMessage(function(payload) {{
  console.log('[firebase-messaging-sw.js] Received background message ', payload);
  // Avoid duplicate notifications:
  // If the message contains a 'notification' payload, the FCM SDK will display it automatically.
  // Only manually display when we have a data-only message.
  if (payload && payload.notification) {{
    return;
  }}

  // Build from data-only payload
  const titleFromData = payload?.data?.title || '';
  const bodyFromData = payload?.data?.body || '';
  const iconFromData = payload?.data?.icon || '{iconUrl}';

  if (titleFromData || bodyFromData) {{
    const notificationOptions = {{
      body: bodyFromData,
      icon: iconFromData,
      data: {{ urlToOpen: payload?.data?.urlToOpen || '/' }}
    }};

    self.registration.showNotification(titleFromData || 'Notification', notificationOptions);
  }}
}});


self.addEventListener('notificationclick', function(event) {{
  console.log('User clicked on notification', event);

  event.notification.close();
  const urlToOpen = event.notification?.data?.urlToOpen || '/';

  event.waitUntil(
    clients.openWindow(urlToOpen)
  );
}});
";
            return Content(script, "application/javascript");
        }

        public async Task<IActionResult> WebPushSw()
        {
            var settings = await _settingService.LoadSettingAsync<PushNotificationsSettings>();
            var iconUrl = settings.NotificationIconUrl ?? "/Plugins/Misc.PushNotifications/logo.jpg";

            var script = $@"
// Web Push Service Worker
self.addEventListener('push', event => {{
    console.log('[webpush-sw.js] Push received:', event);
    
    let notificationData = {{}};
    
    if (event.data) {{
        try {{
            notificationData = event.data.json();
        }} catch (e) {{
            notificationData = {{
                title: 'Nueva notificación',
                body: event.data.text(),
                icon: '{iconUrl}',
                data: {{ url: '/' }}
            }};
        }}
    }}
    
    const title = notificationData.title || 'Nueva notificación';
    const options = {{
        body: notificationData.body || '',
        icon: notificationData.icon || '{iconUrl}',
        badge: '{iconUrl}',
        data: notificationData.data || {{ url: '/' }},
        tag: 'webpush-notification',
        requireInteraction: false,
        actions: [
            {{
                action: 'open',
                title: 'Abrir'
            }},
            {{
                action: 'close',
                title: 'Cerrar'
            }}
        ]
    }};
    
    event.waitUntil(
        self.registration.showNotification(title, options)
    );
}});

self.addEventListener('notificationclick', event => {{
    console.log('[webpush-sw.js] Notification click received:', event);
    
    event.notification.close();
    
    if (event.action === 'close') {{
        return;
    }}
    
    const urlToOpen = event.notification.data?.url || '/';
    
    event.waitUntil(
        clients.matchAll({{ type: 'window', includeUncontrolled: true }}).then(clientList => {{
            // Check if there's already a window/tab open with the target URL
            for (const client of clientList) {{
                if (client.url === urlToOpen && 'focus' in client) {{
                    return client.focus();
                }}
            }}
            
            // If no window/tab is open, open a new one
            if (clients.openWindow) {{
                return clients.openWindow(urlToOpen);
            }}
        }})
    );
}});

self.addEventListener('notificationclose', event => {{
    console.log('[webpush-sw.js] Notification closed:', event);
}});

// Handle background sync if needed
self.addEventListener('sync', event => {{
    console.log('[webpush-sw.js] Background sync:', event);
}});

// Install event
self.addEventListener('install', event => {{
    console.log('[webpush-sw.js] Service worker installing...');
    self.skipWaiting();
}});

// Activate event
self.addEventListener('activate', event => {{
    console.log('[webpush-sw.js] Service worker activating...');
    event.waitUntil(self.clients.claim());
}});
";
            return Content(script, "application/javascript");
        }
    }
}