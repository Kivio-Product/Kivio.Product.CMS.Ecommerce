using System;
using System.Linq;
using System.Threading.Tasks;
using Nop.Plugin.Misc.PushNotifications.Domain;
using Nop.Plugin.Misc.PushNotifications.Services;
using DotnetGeminiSDK.Client.Interfaces;

namespace Nop.Plugin.Misc.PushNotifications.Strategies
{
    public class CustomStrategy : INotificationStrategy
    {
        private readonly IPushNotificationService _pushNotificationService;
        private readonly IGeminiClient _geminiClient;
        private readonly PushNotificationsSettings _pushNotificationsSettings;

        public string StrategyType => "Custom";

        public CustomStrategy(IPushNotificationService pushNotificationService,
            IGeminiClient geminiClient,
            PushNotificationsSettings pushNotificationsSettings)
        {
            _pushNotificationService = pushNotificationService;
            _geminiClient = geminiClient;
            _pushNotificationsSettings = pushNotificationsSettings;
        }

        public Task<bool> CanExecuteAsync()
        {
            return Task.FromResult(_pushNotificationsSettings.EnableCustomStrategy);
        }

        public async Task<(string Title, string Body)> GenerateNotificationAsync()
        {
            var prompt = _pushNotificationsSettings.CustomStrategyPrompt;
            
            try
            {
                var aiResponse = await _geminiClient.TextPrompt(prompt);
                var lines = aiResponse?.Candidates?.FirstOrDefault()?.Content?.Parts?.FirstOrDefault()?.Text?.Split('\n');
                
                var title = lines?.FirstOrDefault()?.Trim() ?? "Special Offer!";
                var body = lines?.Skip(1).FirstOrDefault()?.Trim() ?? "Visit our store for amazing deals.";
                
                // Clean up any markdown formatting
                title = title.Replace("**", "").Replace("#", "").Trim();
                body = body.Replace("**", "").Replace("#", "").Trim();

                await _pushNotificationService.LogNotificationAsync(new PushNotificationLog
                {
                    StrategyType = StrategyType,
                    EntityId = 0, // No specific entity for this strategy
                    Title = title,
                    Body = body,
                    SentDateUtc = DateTime.UtcNow
                });

                return (title, body);
            }
            catch
            {
                // Fallback to default notification if AI fails
                var title = "Special Offer!";
                var body = "Visit our store for amazing deals.";

                await _pushNotificationService.LogNotificationAsync(new PushNotificationLog
                {
                    StrategyType = StrategyType,
                    EntityId = 0, // No specific entity for this strategy
                    Title = title,
                    Body = body,
                    SentDateUtc = DateTime.UtcNow
                });

                return (title, body);
            }
        }
    }
}
