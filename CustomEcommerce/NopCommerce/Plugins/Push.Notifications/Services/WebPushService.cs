using Nop.Services.Configuration;
using Nop.Services.Logging;
using System;
using System.Text.Json;
using System.Threading.Tasks;
using WebPush;

namespace Nop.Plugin.Misc.PushNotifications.Services
{
    public class WebPushService : IWebPushService
    {
        private readonly PushNotificationsSettings _settings;
        private readonly ILogger _logger;

        public WebPushService(ISettingService settingService, ILogger logger)
        {
            _settings = settingService.LoadSetting<PushNotificationsSettings>();
            _logger = logger;
        }

        public async Task SendWebPushNotificationAsync(string endpoint, string p256dh, string auth, string title, string body, string icon, string url)
        {
            try
            {
                // Check if WebPush keys are configured
                if (string.IsNullOrEmpty(_settings.WebPushVapidPublicKey) || string.IsNullOrEmpty(_settings.WebPushVapidPrivateKey))
                {
                    await _logger.InsertLogAsync(Nop.Core.Domain.Logging.LogLevel.Warning,
                        "Web Push Configuration Missing",
                        "Web Push VAPID keys are not configured. Please configure them in the plugin settings.");
                    return;
                }

                // Create the payload
                var payload = JsonSerializer.Serialize(new
                {
                    title = title ?? string.Empty,
                    body = body ?? string.Empty,
                    icon = icon ?? string.Empty,
                    data = new { url = url ?? "/" }
                });

                await _logger.InsertLogAsync(Nop.Core.Domain.Logging.LogLevel.Information,
                    "Web Push Notification Attempted", 
                    $"Web Push notification for endpoint: {endpoint}. Title: {title}. " +
                    $"Please run 'dotnet restore' to install WebPush library for full functionality.");

                var webPushClient = new WebPushClient();
                var vapidDetails = new VapidDetails(_settings.WebPushSubject, _settings.WebPushVapidPublicKey, _settings.WebPushVapidPrivateKey);
                var subscription = new PushSubscription(endpoint, p256dh, auth);
                await webPushClient.SendNotificationAsync(subscription, payload, vapidDetails);
                
                await _logger.InsertLogAsync(Nop.Core.Domain.Logging.LogLevel.Information, 
                    "Web Push Notification Sent", 
                    $"Successfully sent Web Push notification to {endpoint}");
            }
            catch (Exception ex)
            {
                await _logger.InsertLogAsync(Nop.Core.Domain.Logging.LogLevel.Error, 
                    "Web Push Notification Error", 
                    $"Error sending Web Push notification: {ex.Message}");
            }
        }
    }
}
