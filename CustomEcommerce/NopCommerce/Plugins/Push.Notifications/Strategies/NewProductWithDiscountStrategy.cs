using Nop.Core.Domain.Catalog;
using Nop.Plugin.Misc.PushNotifications.Domain;
using Nop.Plugin.Misc.PushNotifications.Services;
using Nop.Plugin.Misc.PushNotifications.Helpers;
using Nop.Services.Catalog;
using Nop.Services.Discounts;
using System;
using System.Linq;
using System.Threading.Tasks;
using DotnetGeminiSDK.Client.Interfaces;
using Newtonsoft.Json;
using Nop.Services.Logging;

namespace Nop.Plugin.Misc.PushNotifications.Strategies
{
    public class NewProductWithDiscountStrategy : INotificationStrategy
    {
        private readonly IProductService _productService;
        private readonly IPushNotificationService _pushNotificationService;
        private readonly IDiscountService _discountService;
        private readonly IGeminiClient _geminiClient;
        private readonly PushNotificationsSettings _pushNotificationsSettings;
        private readonly ILogger _logger;

        public string StrategyType => "NewProductWithDiscount";

        public NewProductWithDiscountStrategy(IProductService productService,
            IPushNotificationService pushNotificationService,
            IDiscountService discountService,
            IGeminiClient geminiClient,
            PushNotificationsSettings pushNotificationsSettings,
            ILogger logger)
        {
            _productService = productService;
            _pushNotificationService = pushNotificationService;
            _discountService = discountService;
            _geminiClient = geminiClient;
            _pushNotificationsSettings = pushNotificationsSettings;
            _logger = logger;
            _pushNotificationsSettings = pushNotificationsSettings;
        }

        public async Task<bool> CanExecuteAsync()
        {
            if (!_pushNotificationsSettings.EnableNewProductStrategy)
                return false;

            var newProducts = await _productService.GetProductsMarkedAsNewAsync();
            var notifiedProductIds = (await _pushNotificationService.GetLogsByStrategyTypeAsync(StrategyType)).Select(l => l.EntityId);

            return newProducts.Any(p => !notifiedProductIds.Contains(p.Id));
        }

        public async Task<(string Title, string Body)> GenerateNotificationAsync()
        {
            var newProducts = await _productService.GetProductsMarkedAsNewAsync();
            var notifiedProductIds = (await _pushNotificationService.GetLogsByStrategyTypeAsync(StrategyType)).Select(l => l.EntityId);

            var product = newProducts.FirstOrDefault(p => !notifiedProductIds.Contains(p.Id));
            if (product == null)
                return (null, null);

            var discounts = await _discountService.GetAppliedDiscountsAsync(product);
            var discount = discounts.FirstOrDefault();
            var discountText = discount != null ? $" with a {discount.Name} discount!" : "";

            var prompt = $"{_pushNotificationsSettings.AIPromptBase} Generate a push notification for a new product: {product.Name}{discountText}. Make it engaging and persuasive.";
            
            try
            {
                var schema = NotificationSchemaHelper.CreateProductNotificationSchema();
                var aiResponse = await _geminiClient.StructuredOutputPrompt(prompt, schema);
                var responseText = aiResponse?.Candidates?.FirstOrDefault()?.Content?.Parts?.FirstOrDefault()?.Text;
                
                if (!string.IsNullOrEmpty(responseText))
                {
                    var notificationData = JsonConvert.DeserializeObject<dynamic>(responseText);
                    var title = notificationData?.title?.ToString();
                    var body = notificationData?.body?.ToString();
                    var emoji = notificationData?.emoji?.ToString();
                    
                    if (title == null || body == null)
                        throw new Exception("Invalid AI response format." + responseText);

                    // Add emoji to title if available
                    if (!string.IsNullOrEmpty(emoji))
                        title = $"{emoji} {title}";

                    await _pushNotificationService.LogNotificationAsync(new PushNotificationLog
                    {
                        StrategyType = StrategyType,
                        EntityId = product.Id,
                        Title = title,
                        Body = body,
                        SentDateUtc = DateTime.UtcNow
                    });

                    return (title, body);
                }
            }
            catch (Exception ex)
            {
                _logger?.ErrorAsync($"Error generating new product notification: {ex.Message}");
            }

            return (null, null);
        }
    }
}
