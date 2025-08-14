using FirebaseAdmin;
using FirebaseAdmin.Messaging;
using Google.Apis.Auth.OAuth2;
using Nop.Core.Domain.Logging;
using Nop.Data;
using Nop.Plugin.Misc.PushNotifications.Domain;
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

        public PushNotificationService(IRepository<PushSubscription> subscriptionRepository, 
            IRepository<PushNotificationLog> logRepository,
            ISettingService settingService, 
            ILogger logger)
        {
            _subscriptionRepository = subscriptionRepository;
            _logRepository = logRepository;
            _settings = settingService.LoadSetting<PushNotificationsSettings>();
            _logger = logger;

            if (FirebaseApp.DefaultInstance == null && !string.IsNullOrEmpty(_settings.FirebaseCredentials))
            {
                var credential = GoogleCredential.FromJson(_settings.FirebaseCredentials);
                FirebaseApp.Create(new AppOptions()
                {
                    Credential = credential
                });
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
                    CreatedOnUtc = System.DateTime.UtcNow
                });
            }
            else
            {
                subscription.CreatedOnUtc = System.DateTime.UtcNow;
                subscription.Token = token;
                await _subscriptionRepository.UpdateAsync(subscription);
            }
        }

        public async Task SendNotificationToAllAsync(string title, string body, string url = "/")
        {
            var subscriptions = _subscriptionRepository.Table.ToList();
            var tokens = subscriptions.Select(s => s.Token).ToList();

            if (tokens.Any())
            {
                var tokensBatch = tokens.Take(500).ToList();
                var batchResponse = await FirebaseMessaging.DefaultInstance.SendEachForMulticastAsync(new MulticastMessage
                {
                    Tokens = tokensBatch,
                    Notification = new Notification
                    {
                        Title = title,
                        Body = body
                    },
                    Data = new Dictionary<string, string>
                    {
                        { "urlToOpen", string.IsNullOrWhiteSpace(url) ? "/" : url }
                    }
                });
                _logger.InsertLog(LogLevel.Information, "Push Notification Sent", $"Successfully sent notification to {tokens.Count} devices.");
            }
        }

        public async Task SendUniqueNotification(string title, string body, string token, string url = "/")
        {
            var message = new Message()
            {
                Token = token,
                Notification = new Notification
                {
                    Title = title,
                    Body = body
                },
                Data = new Dictionary<string, string>
                {
                    { "urlToOpen", string.IsNullOrWhiteSpace(url) ? "/" : url }
                }
            };
            var response = await FirebaseMessaging.DefaultInstance.SendAsync(message);

            if (response != null)
            {
                _logger.InsertLog(LogLevel.Information, "Push Notification Sent", $"Notification sent to token: {token}");
            }
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
