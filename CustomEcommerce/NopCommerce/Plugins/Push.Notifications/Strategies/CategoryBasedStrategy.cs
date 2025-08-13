using Nop.Core.Domain.Catalog;
using Nop.Plugin.Misc.PushNotifications.Domain;
using Nop.Plugin.Misc.PushNotifications.Services;
using Nop.Plugin.Misc.PushNotifications.Helpers;
using Nop.Services.Catalog;
using System;
using System.Linq;
using System.Threading.Tasks;
using DotnetGeminiSDK.Client.Interfaces;
using Newtonsoft.Json;
using Nop.Services.Logging;


namespace Nop.Plugin.Misc.PushNotifications.Strategies
{
    public class CategoryBasedStrategy : INotificationStrategy
    {
        private readonly ICategoryService _categoryService;
        private readonly IPushNotificationService _pushNotificationService;
        private readonly IGeminiClient _geminiClient;
        private readonly PushNotificationsSettings _pushNotificationsSettings;
        private readonly ILogger _logger;

        public string StrategyType => "CategoryBased";

        public CategoryBasedStrategy(ICategoryService categoryService,
            IPushNotificationService pushNotificationService,
            IGeminiClient geminiClient,
            PushNotificationsSettings pushNotificationsSettings,
            ILogger logger)
        {
            _categoryService = categoryService;
            _pushNotificationService = pushNotificationService;
            _geminiClient = geminiClient;
            _pushNotificationsSettings = pushNotificationsSettings;
            _logger = logger;
        }

        public async Task<bool> CanExecuteAsync()
        {
            if (!_pushNotificationsSettings.EnableCategoryStrategy)
                return false;

            var categories = await _categoryService.GetAllCategoriesAsync();
            var notifiedCategoryIds = (await _pushNotificationService.GetLogsByStrategyTypeAsync(StrategyType)).Select(l => l.EntityId);

            return categories.Any(c => !notifiedCategoryIds.Contains(c.Id));
        }

        public async Task<(string Title, string Body)> GenerateNotificationAsync()
        {
            var categories = await _categoryService.GetAllCategoriesAsync();
            var notifiedCategoryIds = (await _pushNotificationService.GetLogsByStrategyTypeAsync(StrategyType)).Select(l => l.EntityId);

            var category = categories.FirstOrDefault(c => !notifiedCategoryIds.Contains(c.Id));
            if (category == null)
                return (null, null);

            var prompt = $"{_pushNotificationsSettings.AIPromptBase} Generate a push notification for the category: {category.Name}. Make it compelling and encourage exploration.";
            
            try
            {
                var schema = NotificationSchemaHelper.CreateCategoryNotificationSchema();
                var aiResponse = await _geminiClient.StructuredOutputPrompt(prompt, schema);
                var responseText = aiResponse?.Candidates?.FirstOrDefault()?.Content?.Parts?.FirstOrDefault()?.Text;
                
                if (!string.IsNullOrEmpty(responseText))
                {
                    var notificationData = JsonConvert.DeserializeObject<dynamic>(responseText);
                    var title = notificationData?.title?.ToString();
                    var body = notificationData?.body?.ToString();
                    var callToAction = notificationData?.callToAction?.ToString();

                    if (title == null || body == null)
                        throw new Exception("Invalid AI response format." + responseText);
                    
                    // Add call to action to body if available
                    if (!string.IsNullOrEmpty(callToAction))
                        body = $"{body} {callToAction}";

                    await _pushNotificationService.LogNotificationAsync(new PushNotificationLog
                    {
                        StrategyType = StrategyType,
                        EntityId = category.Id,
                        Title = title,
                        Body = body,
                        SentDateUtc = DateTime.UtcNow
                    });

                    return (title, body);
                }
            }
            catch (Exception ex)
            {
                _logger?.ErrorAsync($"Error generating category-based notification: {ex.Message}");
            }
            
            return (null, null);
        }
    }
}
