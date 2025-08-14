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
  // Customize notification here
  const notificationTitle = payload.notification.title;
  const notificationOptions = {{
    body: payload.notification.body,
  icon: '{iconUrl}',
  data: {{ urlToOpen: payload?.data?.urlToOpen || '/' }}
  }};

  self.registration.showNotification(notificationTitle, notificationOptions);
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