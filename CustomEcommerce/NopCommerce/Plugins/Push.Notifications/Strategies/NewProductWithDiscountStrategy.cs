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
    public class NewProductWithDiscountStrategy : INotificationStrategy
    {
        private readonly IProductService _productService;
        private readonly IPushNotificationService _pushNotificationService;
        private readonly IGeminiClient _geminiClient;
        private readonly PushNotificationsSettings _pushNotificationsSettings;
        private readonly ILogger _logger;
        private readonly IUrlRecordService _urlRecordService;
        private readonly INopUrlHelper _nopUrlHelper;

        public string StrategyType => "NewProductWithDiscount";

        public NewProductWithDiscountStrategy(IProductService productService,
            IPushNotificationService pushNotificationService,
            IGeminiClient geminiClient,
            PushNotificationsSettings pushNotificationsSettings,
            ILogger logger,
            IUrlRecordService urlRecordService,
            INopUrlHelper nopUrlHelper)
        {
            _productService = productService;
            _pushNotificationService = pushNotificationService;
            _geminiClient = geminiClient;
            _pushNotificationsSettings = pushNotificationsSettings;
            _logger = logger;
            _pushNotificationsSettings = pushNotificationsSettings;
            _urlRecordService = urlRecordService;
            _nopUrlHelper = nopUrlHelper;
        }

        public async Task<bool> CanExecuteAsync()
        {
            if (!_pushNotificationsSettings.EnableNewProductStrategy)
                return false;

            var newProducts = await _productService.GetProductsMarkedAsNewAsync();
            var notifiedProductIds = (await _pushNotificationService.GetLogsByStrategyTypeAsync(StrategyType)).Select(l => l.EntityId);

            return newProducts.Any(p => !notifiedProductIds.Contains(p.Id));
        }

        public async Task<(string Title, string Body, string Url)> GenerateNotificationAsync()
        {
            var newProducts = await _productService.GetProductsMarkedAsNewAsync();
            var notifiedProductIds = (await _pushNotificationService.GetLogsByStrategyTypeAsync(StrategyType)).Select(l => l.EntityId);

            var product = newProducts.FirstOrDefault(p => !notifiedProductIds.Contains(p.Id));
            if (product == null)
                return (null, null, null);

            // Calculate discount from OldPrice vs Price
            var discountText = "";
            if (product.OldPrice > 0 && product.Price > 0 && product.OldPrice > product.Price)
            {
                var discountAmount = product.OldPrice - product.Price;
                var discountPercentage = Math.Round(discountAmount / product.OldPrice * 100, 0);
                discountText = $" with a {discountPercentage}% discount!";
            }

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

                    var seName = await _urlRecordService.GetSeNameAsync(product);
                    var url = await _nopUrlHelper.RouteGenericUrlAsync<Product>(new { SeName = seName });
                    return (title, body, url);
                }
            }
            catch (Exception ex)
            {
                _logger?.ErrorAsync($"Error generating new product notification: {ex.Message}");
            }

            return (null, null, null);
        }
    }
}
