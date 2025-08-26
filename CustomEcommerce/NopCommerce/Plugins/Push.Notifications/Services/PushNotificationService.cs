using FirebaseAdmin;
using FirebaseAdmin.Messaging;
using Google.Apis.Auth.OAuth2;
using Nop.Core.Domain.Logging;
using Nop.Data;
using Nop.Plugin.Misc.PushNotifications.Domain;
using Nop.Plugin.Misc.PushNotifications.Models;
using Nop.Plugin.Misc.PushNotifications.Helpers;
using Nop.Plugin.Misc.PushNotifications.Constants;
using Nop.Services.Configuration;
using Nop.Services.Logging;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Nop.Plugin.Misc.PushNotifications.Services
{
    public class PushNotificationService : IPushNotificationService
    {
        private readonly IRepository<PushSubscription> _subscriptionRepository;
        private readonly IRepository<PushNotificationLog> _logRepository;
        private readonly PushNotificationsSettings _settings;
        private readonly ILogger _logger;
        private readonly IWebPushService _webPushService;

        public PushNotificationService(IRepository<PushSubscription> subscriptionRepository, 
            IRepository<PushNotificationLog> logRepository,
            ISettingService settingService, 
            ILogger logger,
            IWebPushService webPushService)
        {
            _subscriptionRepository = subscriptionRepository;
            _logRepository = logRepository;
            _settings = settingService.LoadSetting<PushNotificationsSettings>();
            _logger = logger;
            _webPushService = webPushService;

            if (FirebaseApp.DefaultInstance == null && !string.IsNullOrEmpty(_settings.FirebaseCredentials))
            {
                var credential = GoogleCredential.FromJson(_settings.FirebaseCredentials);
                FirebaseApp.Create(new AppOptions()
                {
                    Credential = credential
                });
            }
        }

        public async Task RegisterDeviceAsync(int customerId, PushSubscriptionModel subscriptionModel)
        {
            PushSubscription subscription = null;
            
            if (subscriptionModel.Type == NotificationTypes.FCM)
            {
                subscription = _subscriptionRepository.Table.FirstOrDefault(s => 
                    s.CustomerId == customerId && 
                    s.Type == NotificationTypes.FCM &&
                    s.Token == subscriptionModel.Token);
            }
            else if (subscriptionModel.Type == NotificationTypes.WebPush)
            {
                subscription = _subscriptionRepository.Table.FirstOrDefault(s => 
                    s.CustomerId == customerId && 
                    s.Type == NotificationTypes.WebPush &&
                    s.Endpoint == subscriptionModel.Endpoint);
            }
            
            if (subscription == null)
            {
                await _subscriptionRepository.InsertAsync(new PushSubscription
                {
                    CustomerId = customerId,
                    Token = subscriptionModel.Token,
                    Type = subscriptionModel.Type,
                    UserAgent = subscriptionModel.UserAgent,
                    Endpoint = subscriptionModel.Endpoint,
                    P256dh = subscriptionModel.P256dh,
                    Auth = subscriptionModel.Auth,
                    CreatedOnUtc = System.DateTime.UtcNow
                });
            }
            else
            {
                subscription.Token = subscriptionModel.Token;
                subscription.Type = subscriptionModel.Type;
                subscription.UserAgent = subscriptionModel.UserAgent;
                subscription.Endpoint = subscriptionModel.Endpoint;
                subscription.P256dh = subscriptionModel.P256dh;
                subscription.Auth = subscriptionModel.Auth;
                subscription.CreatedOnUtc = System.DateTime.UtcNow;
                await _subscriptionRepository.UpdateAsync(subscription);
            }
        }

        public async Task RegisterDeviceAsync(int customerId, string token)
        {
            var subscription = _subscriptionRepository.Table.FirstOrDefault(s => s.CustomerId == customerId && s.Token == token);
            if (subscription == null)
            {
                await _subscriptionRepository.InsertAsync(new PushSubscription
                {
                    CustomerId = customerId,
                    Token = token,
                    Type = NotificationTypes.FCM,
                    CreatedOnUtc = System.DateTime.UtcNow
                });
            }
            else
            {
                subscription.CreatedOnUtc = System.DateTime.UtcNow;
                await _subscriptionRepository.UpdateAsync(subscription);
            }
        }

        public async Task SendNotificationToAllAsync(string title, string body, string url = "/")
        {
            var subscriptions = _subscriptionRepository.Table.ToList();
            
            // Separate FCM and Web Push subscriptions
            var fcmSubscriptions = subscriptions.Where(s => s.Type == NotificationTypes.FCM).ToList();
            var webPushSubscriptions = subscriptions.Where(s => s.Type == NotificationTypes.WebPush).ToList();

            // Send FCM notifications
            if (fcmSubscriptions.Any())
            {
                await SendFCMNotifications(fcmSubscriptions, title, body, url);
            }

            // Send Web Push notifications
            if (webPushSubscriptions.Any())
            {
                await SendWebPushNotifications(webPushSubscriptions, title, body, url);
            }
        }

        public async Task SendUniqueNotification(string title, string body, string token, string url = "/")
        {
            // First try to find by token (FCM)
            var subscription = _subscriptionRepository.Table.FirstOrDefault(s => 
                s.Type == NotificationTypes.FCM && s.Token == token);
            
            // If not found, try to find by endpoint (WebPush)
            if (subscription == null)
            {
                subscription = _subscriptionRepository.Table.FirstOrDefault(s => 
                    s.Type == NotificationTypes.WebPush && s.Endpoint == token);
            }
            
            if (subscription == null)
            {
                await _logger.InsertLogAsync(LogLevel.Warning, "Push Notification", $"Subscription not found for token: {token}");
                return;
            }

            if (subscription.Type == NotificationTypes.FCM)
            {
                await SendFCMUniqueNotification(subscription, title, body, url);
            }
            else
            {
                await SendWebPushUniqueNotification(subscription, title, body, url);
            }
        }

        private async Task SendFCMNotifications(IList<PushSubscription> subscriptions, string title, string body, string url)
        {
            var tokens = subscriptions.Select(s => s.Token).Distinct().Where(t => !string.IsNullOrEmpty(t)).ToList();

            if (tokens.Any())
            {
                var tokensBatch = tokens.Take(500).ToList();
                var dataPayload = new Dictionary<string, string>
                {
                    { "title", title ?? string.Empty },
                    { "body", body ?? string.Empty },
                    { "icon", string.IsNullOrWhiteSpace(_settings.NotificationIconUrl) ? "/Plugins/Misc.PushNotifications/logo.jpg" : _settings.NotificationIconUrl },
                    { "urlToOpen", string.IsNullOrWhiteSpace(url) ? "/" : url }
                };

                var batchResponse = await FirebaseMessaging.DefaultInstance.SendEachForMulticastAsync(new MulticastMessage
                {
                    Tokens = tokensBatch,
                    Data = dataPayload
                });
                
                await _logger.InsertLogAsync(LogLevel.Information, "FCM Push Notification Sent", $"Successfully sent FCM notification to {tokens.Count} devices.");
            }
        }

        private async Task SendWebPushNotifications(IList<PushSubscription> subscriptions, string title, string body, string url)
        {
            var icon = string.IsNullOrWhiteSpace(_settings.NotificationIconUrl) ? "/Plugins/Misc.PushNotifications/logo.jpg" : _settings.NotificationIconUrl;
            
            foreach (var subscription in subscriptions.Where(s => !string.IsNullOrEmpty(s.Endpoint)))
            {
                await _webPushService.SendWebPushNotificationAsync(
                    subscription.Endpoint,
                    subscription.P256dh,
                    subscription.Auth,
                    title,
                    body,
                    icon,
                    url
                );
            }
            
            await _logger.InsertLogAsync(LogLevel.Information, "Web Push Notification Sent", $"Successfully sent Web Push notification to {subscriptions.Count} devices.");
        }

        private async Task SendFCMUniqueNotification(PushSubscription subscription, string title, string body, string url)
        {
            var dataPayload = new Dictionary<string, string>
            {
                { "title", title ?? string.Empty },
                { "body", body ?? string.Empty },
                { "icon", string.IsNullOrWhiteSpace(_settings.NotificationIconUrl) ? "/Plugins/Misc.PushNotifications/logo.jpg" : _settings.NotificationIconUrl },
                { "urlToOpen", string.IsNullOrWhiteSpace(url) ? "/" : url }
            };
            
            var message = new Message()
            {
                Token = subscription.Token,
                Data = dataPayload
            };
            
            var response = await FirebaseMessaging.DefaultInstance.SendAsync(message);

            if (response != null)
            {
                await _logger.InsertLogAsync(LogLevel.Information, "FCM Push Notification Sent", $"FCM notification sent to token: {subscription.Token}");
            }
        }

        private async Task SendWebPushUniqueNotification(PushSubscription subscription, string title, string body, string url)
        {
            var icon = string.IsNullOrWhiteSpace(_settings.NotificationIconUrl) ? "/Plugins/Misc.PushNotifications/logo.jpg" : _settings.NotificationIconUrl;
            
            await _webPushService.SendWebPushNotificationAsync(
                subscription.Endpoint,
                subscription.P256dh,
                subscription.Auth,
                title,
                body,
                icon,
                url
            );
        }

        public async Task LogNotificationAsync(PushNotificationLog log)
        {
            await _logRepository.InsertAsync(log);
        }

        public async Task<IList<PushNotificationLog>> GetLogsByStrategyTypeAsync(string strategyType)
        {
            return await _logRepository.Table.Where(l => l.StrategyType == strategyType).ToListAsync();
        }
    }
}
