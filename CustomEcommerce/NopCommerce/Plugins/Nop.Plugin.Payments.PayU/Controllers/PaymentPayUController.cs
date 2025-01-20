using System.Text;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json.Linq;
using Nop.Core;
using Nop.Plugin.Payments.PayU.Models;
using Nop.Plugin.Payments.PayU.Services;
using Nop.Services.Configuration;
using Nop.Services.Localization;
using Nop.Services.Logging;
using Nop.Services.Messages;
using Nop.Services.Security;
using Nop.Services.Stores;
using Nop.Web.Framework;
using Nop.Web.Framework.Controllers;
using Nop.Web.Framework.Mvc.Filters;

namespace Nop.Plugin.Payments.PayU.Controllers
{
    public class PaymentPayUController : BasePaymentController
    {
        private readonly ILogger _logger;
        private readonly ISettingService _settingService;
        private readonly IPermissionService _permissionService;
        private readonly ILocalizationService _localizationService;
        private readonly IPayUService _payUService;
        private readonly IStoreContext _storeContext;
        private readonly INotificationService _notificationService;


        public PaymentPayUController(
            ILogger logger,
            IStoreService storeService,
            ISettingService settingService,
            IPermissionService permissionService,
            ILocalizationService localizationService,
            IPayUService payUService,
            IWorkContext workContext,
            IStoreContext storeContext,
            INotificationService notificationService)
        {
            _logger = logger;
            _settingService = settingService;
            _permissionService = permissionService;
            _localizationService = localizationService;
            _payUService = payUService;
            _storeContext = storeContext;
            _notificationService = notificationService;
        }

        [AuthorizeAdmin]
        [Area(AreaNames.ADMIN)]
        public async Task<IActionResult> Configure()
        {
            var storeScope = await _storeContext.GetActiveStoreScopeConfigurationAsync();
            var payUPaymentSettings = await _settingService.LoadSettingAsync<PayUPaymentSettings>(storeScope);

            var model = new ConfigurationModel()
            {
                ActiveStoreScopeConfiguration = storeScope,
                UseSandbox = payUPaymentSettings.UseSandbox,
                AccountId = payUPaymentSettings.AccountId,
                MerchantId = payUPaymentSettings.MerchantId,
                ClientLoginId = payUPaymentSettings.ClientLoginId,
                ClientSecretKey = payUPaymentSettings.ClientSecretKey,
                ClientPublicKey = payUPaymentSettings.ClientPublicKey,
                PaymentDescription = payUPaymentSettings.PaymentDescription
            };

            return View("~/Plugins/Payments.PayU/Views/Configure.cshtml", model);
        }

        [HttpPost]
        [CheckPermission(StandardPermission.Configuration.MANAGE_PAYMENT_METHODS)]
        [Area(AreaNames.ADMIN)]
        public async Task<IActionResult> Configure(ConfigurationModel model)
        {

            if (!ModelState.IsValid)
                return View("~/Plugins/Payments.PayU/Views/Configure.cshtml", model);

            var storeScope = await _storeContext.GetActiveStoreScopeConfigurationAsync();
            var payUPaymentSettings = await _settingService.LoadSettingAsync<PayUPaymentSettings>(storeScope);

            payUPaymentSettings.UseSandbox = model.UseSandbox;
            payUPaymentSettings.AccountId = model.AccountId;
            payUPaymentSettings.MerchantId = model.MerchantId;
            payUPaymentSettings.ClientLoginId = model.ClientLoginId;
            payUPaymentSettings.ClientSecretKey = model.ClientSecretKey;
            payUPaymentSettings.ClientPublicKey = model.ClientPublicKey;
            payUPaymentSettings.PaymentDescription = model.PaymentDescription;

            await _settingService.SaveSettingOverridablePerStoreAsync(payUPaymentSettings,
                x => x.UseSandbox, model.UseSandboxOverrideForStore, storeScope, false);

            await _settingService.SaveSettingOverridablePerStoreAsync(payUPaymentSettings,
                 x => x.AccountId, model.AccountIdOverrideForStore, storeScope, false);

            await _settingService.SaveSettingOverridablePerStoreAsync(payUPaymentSettings,
                 x => x.MerchantId, model.MerchantIdOverrideForStore, storeScope, false);

            await _settingService.SaveSettingOverridablePerStoreAsync(payUPaymentSettings,
                 x => x.ClientLoginId, model.ClientLoginIdOverrideForStore, storeScope, false);

            await _settingService.SaveSettingOverridablePerStoreAsync(payUPaymentSettings,
                 x => x.ClientSecretKey, model.ClientSecretKeyOverrideForStore, storeScope, false);

            await _settingService.SaveSettingOverridablePerStoreAsync(payUPaymentSettings,
                 x => x.ClientPublicKey, model.ClientPublicKeyOverrideForStore, storeScope, false);

            await _settingService.SaveSettingOverridablePerStoreAsync(payUPaymentSettings,
                  x => x.PaymentDescription, model.PaymentDescriptionOverrideForStore, storeScope, false);

            await _settingService.ClearCacheAsync();

            _notificationService.SuccessNotification(await _localizationService.GetResourceAsync("Admin.Plugins.Saved"));

            return await Configure();
        }

        [HttpGet]
         public async Task<IActionResult> Return()
        {
            var paymentResponse = PaymentResponse.FromHttpRequest(HttpContext.Request);

            var (succeeded, orderId) = await this._payUService.ReturnAsync(paymentResponse);

            if (!succeeded)
            {
                return RedirectToAction("Index", "Home", new { area = "" });
            }

            return RedirectToRoute("CheckoutCompleted", new { orderId = orderId });
        }


        public IActionResult ProcessingPayment(int orderId)
        {
            this._payUService.CompleteOrderById(orderId);
            return RedirectToRoute("CheckoutCompleted", new { orderId });
        }
    }
}
