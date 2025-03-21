using Microsoft.AspNetCore.Mvc;
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
using Nop.Services.Directory;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace Nop.Plugin.Payments.PayU.Controllers
{
    public class PaymentPayUController : BasePaymentController
    {
        private readonly ILogger _logger;
        private readonly ISettingService _settingService;
        private readonly ILocalizationService _localizationService;
        private readonly IPayUService _payUService;
        private readonly IStoreContext _storeContext;
        private readonly INotificationService _notificationService;
        private readonly ICurrencyService _currencyService;
        private readonly IWorkContext _workContext;

        public PaymentPayUController(
            ILogger logger,
            ISettingService settingService,
            ILocalizationService localizationService,
            IPayUService payUService,
            IStoreContext storeContext,
            INotificationService notificationService,
            ICurrencyService currencyService,
            IWorkContext workContext)
        {
            _logger = logger;
            _settingService = settingService;
            _localizationService = localizationService;
            _payUService = payUService;
            _storeContext = storeContext;
            _notificationService = notificationService;
            _currencyService = currencyService;
            _workContext = workContext;
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
                PaymentDescription = payUPaymentSettings.PaymentDescription,
                SelectedCurrencyIds = payUPaymentSettings.SelectedCurrencyIdList ?? new List<int>()
            };

            var storeLanguage = await _workContext.GetWorkingLanguageAsync();
            var currencies = await _currencyService.GetAllCurrenciesAsync();

            model.AvailableCurrencies = await currencies
                .SelectAwait(async currency =>
                {
                    var localizedName = await _localizationService.GetLocalizedAsync(currency, x => x.Name, storeLanguage.Id);

                    return new SelectListItem
                    {
                        Text = localizedName,
                        Value = currency.Id.ToString(),
                        Selected = model.SelectedCurrencyIds.Contains(currency.Id)
                    };
                })
                .ToListAsync();

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
            payUPaymentSettings.SelectedCurrencyIdList = model.SelectedCurrencyIds;

            await _settingService.SaveSettingOverridablePerStoreAsync(
                payUPaymentSettings,
                x => x.UseSandbox,
                model.UseSandboxOverrideForStore,
                storeScope,
                false);

            await _settingService.SaveSettingOverridablePerStoreAsync(
                payUPaymentSettings,
                x => x.AccountId,
                model.AccountIdOverrideForStore,
                storeScope,
                false);

            await _settingService.SaveSettingOverridablePerStoreAsync(
                payUPaymentSettings,
                x => x.MerchantId,
                model.MerchantIdOverrideForStore,
                storeScope,
                false);

            await _settingService.SaveSettingOverridablePerStoreAsync(
                payUPaymentSettings,
                x => x.ClientLoginId,
                model.ClientLoginIdOverrideForStore,
                storeScope,
                false);

            await _settingService.SaveSettingOverridablePerStoreAsync(
                payUPaymentSettings,
                x => x.ClientSecretKey,
                model.ClientSecretKeyOverrideForStore,
                storeScope,
                false);

            await _settingService.SaveSettingOverridablePerStoreAsync(
                payUPaymentSettings,
                x => x.ClientPublicKey,
                model.ClientPublicKeyOverrideForStore,
                storeScope,
                false);

            await _settingService.SaveSettingOverridablePerStoreAsync(
                payUPaymentSettings,
                x => x.PaymentDescription,
                model.PaymentDescriptionOverrideForStore,
                storeScope,
                false);

            await _settingService.SaveSettingOverridablePerStoreAsync(
                payUPaymentSettings,
                x => x.SelectedCurrencyIds,
                model.SelectedCurrencyIdsOverrideForStore,
                storeScope,
                false);

            await _settingService.ClearCacheAsync();

            _notificationService.SuccessNotification(await _localizationService.GetResourceAsync("Admin.Plugins.Saved"));

            return await Configure();
        }

        [HttpGet]
        public async Task<IActionResult> Return()
        {
            var paymentResponse = PaymentResponse.FromHttpRequest(HttpContext.Request);

            var (succeeded, orderId) = await _payUService.ReturnAsync(paymentResponse);

            if (!succeeded)
            {
                return RedirectToRoute("OrderCancelled", new { orderId = orderId });
            }

            return RedirectToRoute("CheckoutCompleted", new { orderId = orderId });
        }


        [HttpPost]
        public async Task<IActionResult> Confirm()
        {
            try
            {
                var confirmationResponse = ConfirmationResponse.FromHttpRequest(HttpContext.Request);
                var (succeeded, orderId) = await _payUService.ConfirmAsync(confirmationResponse);

                if (!succeeded)
                {
                    _logger.Error($"Fallo en la confirmación del pedido con ID {orderId}.");
                }

                return Ok();
            }
            catch (Exception ex)
            {
                _logger.Error($"Error procesando la confirmación de PayU: {ex.Message}", ex);

                return Ok();
            }
        }
    }
}
