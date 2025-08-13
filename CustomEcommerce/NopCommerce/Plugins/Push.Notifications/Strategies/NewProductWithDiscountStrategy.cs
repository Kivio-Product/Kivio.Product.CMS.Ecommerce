using Nop.Core.Domain.Catalog;
using Nop.Plugin.Misc.PushNotifications.Domain;
using Nop.Plugin.Misc.PushNotifications.Services;
using Nop.Services.Catalog;
using Nop.Services.Discounts;
using System;
using System.Linq;
using System.Threading.Tasks;
using DotnetGeminiSDK.Client.Interfaces;

namespace Nop.Plugin.Misc.PushNotifications.Strategies
{
    public class NewProductWithDiscountStrategy : INotificationStrategy
    {
        private readonly IProductService _productService;
        private readonly IPushNotificationService _pushNotificationService;
        private readonly IDiscountService _discountService;
        private readonly IGeminiClient _geminiClient;
        private readonly PushNotificationsSettings _pushNotificationsSettings;

        public string StrategyType => "NewProductWithDiscount";

        public NewProductWithDiscountStrategy(IProductService productService,
            IPushNotificationService pushNotificationService,
            IDiscountService discountService,
            IGeminiClient geminiClient,
            PushNotificationsSettings pushNotificationsSettings)
        {
            _productService = productService;
            _pushNotificationService = pushNotificationService;
            _discountService = discountService;
            _geminiClient = geminiClient;
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

            var prompt = $"{_pushNotificationsSettings.AIPromptBase} Generate a push notification for a new product: {product.Name}{discountText}.";
            
            try
            {
                var aiResponse = await _geminiClient.TextPrompt(prompt);
                var lines = aiResponse?.Candidates?.FirstOrDefault()?.Content?.Parts?.FirstOrDefault()?.Text?.Split('\n');
                
                var title = lines?.FirstOrDefault()?.Trim() ?? $"New Product: {product.Name}";
                var body = lines?.Skip(1).FirstOrDefault()?.Trim() ?? $"Check out our new product: {product.Name}{discountText}";
                
                // Clean up any markdown formatting
                title = title.Replace("**", "").Replace("#", "").Trim();
                body = body.Replace("**", "").Replace("#", "").Trim();
                
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
            catch
            {
                // Fallback to default notification if AI fails
                var title = $"New Product: {product.Name}";
                var body = $"Check out our new product: {product.Name}{discountText}";

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
    }
}
