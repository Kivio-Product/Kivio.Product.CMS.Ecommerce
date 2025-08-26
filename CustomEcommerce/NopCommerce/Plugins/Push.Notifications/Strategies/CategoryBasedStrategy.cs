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
using Nop.Services.Seo;
using Nop.Web.Framework.Mvc.Routing;


namespace Nop.Plugin.Misc.PushNotifications.Strategies
{
    public class CategoryBasedStrategy : INotificationStrategy
    {
        private readonly ICategoryService _categoryService;
        private readonly IPushNotificationService _pushNotificationService;
        private readonly IGeminiClient _geminiClient;
        private readonly PushNotificationsSettings _pushNotificationsSettings;
        private readonly ILogger _logger;
        private readonly IUrlRecordService _urlRecordService;
        private readonly INopUrlHelper _nopUrlHelper;

        public string StrategyType => "CategoryBased";

        public CategoryBasedStrategy(ICategoryService categoryService,
            IPushNotificationService pushNotificationService,
            IGeminiClient geminiClient,
            PushNotificationsSettings pushNotificationsSettings,
            ILogger logger,
            IUrlRecordService urlRecordService,
            INopUrlHelper nopUrlHelper)
        {
            _categoryService = categoryService;
            _pushNotificationService = pushNotificationService;
            _geminiClient = geminiClient;
            _pushNotificationsSettings = pushNotificationsSettings;
            _logger = logger;
            _urlRecordService = urlRecordService;
            _nopUrlHelper = nopUrlHelper;
        }

        public async Task<bool> CanExecuteAsync()
        {
            if (!_pushNotificationsSettings.EnableCategoryStrategy)
                return false;

            var categories = await _categoryService.GetAllCategoriesAsync();
            return categories.Any(c => c.Published);
        }

        public async Task<(string Title, string Body, string Url)> GenerateNotificationAsync()
        {
            var categories = await _categoryService.GetAllCategoriesAsync();
            var publishedCategories = categories.Where(c => c.Published).ToList();

            if (!publishedCategories.Any())
                return (null, null, null);

            // Select a random published category
            var random = new Random();
            var category = publishedCategories[random.Next(publishedCategories.Count)];

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

                    var seName = await _urlRecordService.GetSeNameAsync(category);
                    var url = await _nopUrlHelper.RouteGenericUrlAsync<Category>(new { SeName = seName });
                    return (title, body, url);
                }
            }
            catch (Exception ex)
            {
                _logger?.ErrorAsync($"Error generating category-based notification: {ex.Message}");
            }

            return (null, null, null);
        }
    }
}
