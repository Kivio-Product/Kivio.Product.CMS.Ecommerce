using System;
using System.Linq;
using System.Threading.Tasks;
using Nop.Plugin.Misc.PushNotifications.Domain;
using Nop.Plugin.Misc.PushNotifications.Services;
using Nop.Plugin.Misc.PushNotifications.Helpers;
using DotnetGeminiSDK.Client.Interfaces;
using Newtonsoft.Json;
using Nop.Services.Logging;

namespace Nop.Plugin.Misc.PushNotifications.Strategies
{
    public class CustomStrategy : INotificationStrategy
    {
        private readonly IPushNotificationService _pushNotificationService;
        private readonly IGeminiClient _geminiClient;
        private readonly PushNotificationsSettings _pushNotificationsSettings;
        private readonly ILogger _logger;

        public string StrategyType => "Custom";

        public CustomStrategy(IPushNotificationService pushNotificationService,
            IGeminiClient geminiClient,
            PushNotificationsSettings pushNotificationsSettings,
            ILogger logger)
        {
            _pushNotificationService = pushNotificationService;
            _geminiClient = geminiClient;
            _pushNotificationsSettings = pushNotificationsSettings;
            _logger = logger;
        }

        public Task<bool> CanExecuteAsync()
        {
            return Task.FromResult(_pushNotificationsSettings.EnableCustomStrategy);
        }

        public async Task<(string Title, string Body, string Url)> GenerateNotificationAsync()
        {
            var prompt = _pushNotificationsSettings.CustomStrategyPrompt;

            try
            {
                var schema = NotificationSchemaHelper.CreatePushNotificationSchema();
                var aiResponse = await _geminiClient.StructuredOutputPrompt(prompt, schema);
                var responseText = aiResponse?.Candidates?.FirstOrDefault()?.Content?.Parts?.FirstOrDefault()?.Text;

                if (!string.IsNullOrEmpty(responseText))
                {
                    var notificationData = JsonConvert.DeserializeObject<dynamic>(responseText);
                    var title = notificationData?.title?.ToString();
                    var body = notificationData?.body?.ToString();

                    if (title == null || body == null)
                        throw new Exception("Invalid AI response format." + responseText);

                    await _pushNotificationService.LogNotificationAsync(new PushNotificationLog
                    {
                        StrategyType = StrategyType,
                        EntityId = 0, 
                        Title = title,
                        Body = body,
                        SentDateUtc = DateTime.UtcNow
                    });

                    // For custom strategy, open home page
                    return (title, body, "/");
                }
            }
            catch (Exception ex)
            {
                _logger?.ErrorAsync($"Error generating custom strategy notification: {ex.Message}");
            }

            return (null, null, null);
        }
    }
}
