using Nop.Core.Domain.Catalog;
using Nop.Plugin.Misc.PushNotifications.Domain;
using Nop.Plugin.Misc.PushNotifications.Services;
using Nop.Services.Catalog;
using System;
using System.Linq;
using System.Threading.Tasks;
using DotnetGeminiSDK.Client.Interfaces;

namespace Nop.Plugin.Misc.PushNotifications.Strategies
{
    public class CategoryBasedStrategy : INotificationStrategy
    {
        private readonly ICategoryService _categoryService;
        private readonly IPushNotificationService _pushNotificationService;
        private readonly IGeminiClient _geminiClient;
        private readonly PushNotificationsSettings _pushNotificationsSettings;

        public string StrategyType => "CategoryBased";

        public CategoryBasedStrategy(ICategoryService categoryService,
            IPushNotificationService pushNotificationService,
            IGeminiClient geminiClient,
            PushNotificationsSettings pushNotificationsSettings)
        {
            _categoryService = categoryService;
            _pushNotificationService = pushNotificationService;
            _geminiClient = geminiClient;
            _pushNotificationsSettings = pushNotificationsSettings;
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

            var prompt = $"{_pushNotificationsSettings.AIPromptBase} Generate a push notification for the category: {category.Name}.";
            
            try
            {
                var aiResponse = await _geminiClient.TextPrompt(prompt);
                var lines = aiResponse?.Candidates?.FirstOrDefault()?.Content?.Parts?.FirstOrDefault()?.Text?.Split('\n');
                
                var title = lines?.FirstOrDefault()?.Trim() ?? $"Explore {category.Name}";
                var body = lines?.Skip(1).FirstOrDefault()?.Trim() ?? $"Discover amazing products in our {category.Name} category.";
                
                // Clean up any markdown formatting
                title = title.Replace("**", "").Replace("#", "").Trim();
                body = body.Replace("**", "").Replace("#", "").Trim();

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
            catch
            {
                // Fallback to default notification if AI fails
                var title = $"Explore {category.Name}";
                var body = $"Discover amazing products in our {category.Name} category.";

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
    }
}
