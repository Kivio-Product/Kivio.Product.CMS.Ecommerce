using FirebaseAdmin;
using FirebaseAdmin.Messaging;
using Google.Apis.Auth.OAuth2;
using Nop.Data;
using Nop.Plugin.Misc.PushNotifications.Domain;
using Nop.Services.Configuration;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Nop.Plugin.Misc.PushNotifications.Services
{
    public class PushNotificationService : IPushNotificationService
    {
        private readonly IRepository<PushSubscription> _subscriptionRepository;
        private readonly PushNotificationsSettings _settings;

        public PushNotificationService(IRepository<PushSubscription> subscriptionRepository, ISettingService settingService)
        {
            _subscriptionRepository = subscriptionRepository;
            _settings = settingService.LoadSetting<PushNotificationsSettings>();

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

        public async Task SendNotificationToAllAsync(string title, string body)
        {
            var subscriptions = _subscriptionRepository.Table.ToList();
            var tokens = subscriptions.Select(s => s.Token).ToList();

            if (tokens.Any())
            {
                var message = new MulticastMessage()
                {
                    Tokens = tokens,
                    Notification = new Notification
                    {
                        Title = title,
                        Body = body
                    }
                };

                await FirebaseMessaging.DefaultInstance.SendMulticastAsync(message);
            }
        }
    }
}
