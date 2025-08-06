using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using Nop.Core;
using Nop.Core.Domain.Catalog;
using Nop.Core.Domain.Messages;
using Nop.Plugin.Progressive.Web.App.Helpers;
using Nop.Plugin.Progressive.Web.App.Models;
using Nop.Plugin.Progressive.Web.App.Services;
using Nop.Plugin.Progressive.Web.App.Settings;
using Nop.Services.Catalog;
using Nop.Services.Logging;
using Nop.Services.Messages;
using Nop.Web.Factories;
using Nop.Web.Framework.Controllers;
using Nop.Web.Models.Catalog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using WebPush;

namespace Nop.Plugin.Progressive.Web.App.Controllers
{
    public class WebPushController : BasePluginController
    {
        #region fields
        private readonly IProgressiveWebPushService _progressiveWebPushService;
        private readonly IWorkContext _workContext;
        private readonly IEmailAccountService _emailAccountService;
        private readonly EmailAccountSettings _emailAccountSettings;
        private readonly IProductService _productService;
        private readonly IProductModelFactory _productModelFactory;
        private readonly ILogger _logger;
        private readonly ICategoryService _categoryService;
        private readonly ICatalogModelFactory _catalogModelFactory;
        private readonly ProgressiveWebAppSettings _progressiveWebAppSettings;
        #endregion

        #region ctor
        public WebPushController(IProgressiveWebPushService progressiveWebPushService,
            IWorkContext workContext,
            IEmailAccountService emailAccountService,
            EmailAccountSettings emailAccountSettings,
            IProductService productService,
            IProductModelFactory productModelFactory,
            ILogger logger,
            ICategoryService categoryService,
            ICatalogModelFactory catalogModelFactory,
            ProgressiveWebAppSettings progressiveWebAppSettings)
        {
            _progressiveWebPushService = progressiveWebPushService;
            _workContext = workContext;
            _emailAccountService = emailAccountService;
            _emailAccountSettings = emailAccountSettings;
            _productService = productService;
            _productModelFactory = productModelFactory;
            _logger = logger;
            _categoryService = categoryService;
            _catalogModelFactory = catalogModelFactory;
            _progressiveWebAppSettings = progressiveWebAppSettings;
        }
        #endregion

        #region subscription

        [HttpPost]
        public async Task<IActionResult> CreateOrUpdateSubscription([FromBody] SubscriptionModel subscriptionModel)
        {
            try
            {
                var customer = await _workContext.GetCurrentCustomerAsync();

                var subscriptionRecord = _progressiveWebPushService.GetSubscriptionByCustomerId(customer.Id);
                if (subscriptionRecord == null)
                     _progressiveWebPushService.CreateSubscription(subscriptionModel.ToSubscriptionRecord(customer.Id));
                else
                     _progressiveWebPushService.UpdateSuscription(subscriptionRecord);
            }
            catch (Exception e)
            {
                await _logger.ErrorAsync(e.Message, e);
                return Json(new { Success = false, e.Message });
            }
            return Json(new { Success = true });
        }

        [HttpPost]
        public async Task<IActionResult> RemoveSubscription()
        {
            try
            {
                var customer = await _workContext.GetCurrentCustomerAsync();
                _progressiveWebPushService.RemoveSubscriptionByCustomerId(customer.Id);
            }
            catch (Exception e)
            {
                await _logger.ErrorAsync(e.Message, e);
                return Json(new { Success = false, e.Message });
            }
            return Json(new { Success = true });
        }

        #endregion subscription

        #region notifications

        public async Task<IActionResult> AddToCartNotification()
        {
            var payload = JsonConvert.SerializeObject(new { notificationType = NotificationType.Cart.ToString() });
            var customer = await _workContext.GetCurrentCustomerAsync();
            var customerIds = new int[] { customer.Id };

            var result = await SentNotificationAsync(customerIds, payload);
            return Json(result);
        }

        [HttpPost]
        public async Task<IActionResult> SendNotification(int[] customerIds, string payload)
        {
            var result = await SentNotificationAsync(customerIds, payload);
            return Json(result);
        }

        [HttpPost]
        public async Task<IActionResult> SendNotificationOffer(SentNotificationModel model)
        {
            if (model.SelectedIds == null)
                return Json(new ResultMessageModel { Success = false, Message = "No Customers select" });

            var customerIds = model.SelectedIds
                .Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(x => Convert.ToInt32(x))
                .ToArray();

            string payload;

            switch (model.OfferType)
            {
                case OfferType.Product:
                    var product = await _productService.GetProductByIdAsync(model.OfferId);
                    if (product == null)
                        return Json(new ResultMessageModel { Success = false, Message = "No product found" });

                    var products = new List<Product> { product };

                    var productModels = await _productModelFactory.PrepareProductOverviewModelsAsync(products);
                    var productModel = productModels.FirstOrDefault();
                    if (productModel == null)
                        return Json(new ResultMessageModel { Success = false, Message = "Could not prepare product model" });

                    payload = JsonConvert.SerializeObject(new
                    {
                        offer = new
                        {
                            productModel.Id,
                            productModel.Name,
                            productModel.SeName,
                            productModel.ProductPrice.Price,
                            productModel.PictureModels.FirstOrDefault().ImageUrl
                        },
                        notificationType = NotificationType.Offer.ToString()
                    });
                    break;

                case OfferType.Category:
                    var category = await _categoryService.GetCategoryByIdAsync(model.OfferId);
                    if (category == null)
                        return Json(new ResultMessageModel { Success = false, Message = "No category found" });

                    var categoryModel = await _catalogModelFactory.PrepareCategoryModelAsync(category, new CatalogProductsCommand());
                    if (categoryModel == null)
                        return Json(new ResultMessageModel { Success = false, Message = "Could not prepare category model" });

                    payload = JsonConvert.SerializeObject(new
                    {
                        offer = new
                        {
                            categoryModel.Id,
                            categoryModel.Name,
                            categoryModel.SeName,
                            categoryModel.PictureModel.ImageUrl
                        },
                        notificationType = NotificationType.Offer.ToString()
                    });
                    break;
                default:
                    return Json(new ResultMessageModel { Success = false, Message = "No offer type selected" });
            }

            var result = await SentNotificationAsync(customerIds, payload);
            return Json(result);
        }

        private async Task<ResultMessageModel> SentNotificationAsync(int[] customerIds, string payload)
        {
            var defaultEmail = await _emailAccountService.GetEmailAccountByIdAsync(_emailAccountSettings.DefaultEmailAccountId);

            if (!customerIds.Any())
                return new ResultMessageModel { Success = false, Message = "No customerIds" };

            if (defaultEmail == null)
                return new ResultMessageModel { Success = false, Message = "Default email account not configured." };

            var vadipDetails = new VapidDetails($@"mailto:{defaultEmail.Email}",
                _progressiveWebAppSettings.PublicKey,
                _progressiveWebAppSettings.PrivateKey);

            var subscriptions = _progressiveWebPushService.GetSubscriptionByCustomerIds(customerIds);
            if (subscriptions == null || !subscriptions.Any())
                return new ResultMessageModel { Success = false, Message = "No Subcriptions" };

            var sendNotificationNumber = 0;

            foreach (var subscription in subscriptions)
            {
                try
                {
                    var webPushClient = new WebPushClient();
                    await webPushClient.SendNotificationAsync(new PushSubscription(subscription.Endpoint, subscription.P256DHKey, subscription.AuthKey), payload, vadipDetails);
                    sendNotificationNumber++;
                }
                catch (WebPushException e)
                {
                    await _logger.WarningAsync(e.Message, e, await _workContext.GetCurrentCustomerAsync());
                }
            }
            return new ResultMessageModel { Success = true, Message = $"Send {sendNotificationNumber} from {subscriptions.Count} Notifications" };
        }

        #endregion

        public async Task<IActionResult> GetOffer(int customerId)
        {
            var product = await _productService.GetProductByIdAsync(18);
            if (product == null) return Json("No Offer for product 18");

            var products = new List<Product> { product };
            var productModels = await _productModelFactory.PrepareProductOverviewModelsAsync(products);
            var offer = productModels.FirstOrDefault();

            if (offer == null) return Json("No Offer");

            var payload = JsonConvert.SerializeObject(new
            {
                offer = new
                {
                    offer.Id,
                    offer.Name,
                    offer.SeName,
                    offer.ProductPrice.Price,
                    offer.PictureModels.FirstOrDefault().ImageUrl
                },
                notificationType = NotificationType.Offer.ToString()
            });

            var customerIds = new[] { customerId };
            var result = await SentNotificationAsync(customerIds, payload);
            return Json(result);
        }
    }
}