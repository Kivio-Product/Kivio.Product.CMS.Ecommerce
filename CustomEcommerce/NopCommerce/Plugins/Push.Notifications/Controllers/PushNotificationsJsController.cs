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
    }
}