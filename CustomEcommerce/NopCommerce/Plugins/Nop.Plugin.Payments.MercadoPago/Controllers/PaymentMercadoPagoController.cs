using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using Nop.Core;
using Nop.Plugin.Payments.MercadoPago.Models;
using Nop.Plugin.Payments.MercadoPago.Services;
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

namespace Nop.Plugin.Payments.MercadoPago.Controllers
{
    public class PaymentMercadoPagoController : BasePaymentController
    {
        private readonly ILogger _logger;
        private readonly ISettingService _settingService;
        private readonly ILocalizationService _localizationService;
        private readonly IMercadoPagoService _mercadoPagoService;
        private readonly IStoreContext _storeContext;
        private readonly INotificationService _notificationService;
        private readonly ICurrencyService _currencyService;
        private readonly IWorkContext _workContext;

        public PaymentMercadoPagoController(
            ILogger logger,
            ISettingService settingService,
            ILocalizationService localizationService,
            IMercadoPagoService mercadoPagoService,
            IStoreContext storeContext,
            INotificationService notificationService,
            ICurrencyService currencyService,
            IWorkContext workContext)
        {
            _logger = logger;
            _settingService = settingService;
            _localizationService = localizationService;
            _mercadoPagoService = mercadoPagoService;
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
            var mercadoPagoPaymentSettings = await _settingService.LoadSettingAsync<MercadoPagoPaymentSettings>(storeScope);

            var model = new ConfigurationModel()
            {
                ActiveStoreScopeConfiguration = storeScope,
                AdditionalFeeEnabled = mercadoPagoPaymentSettings.AdditionalFeeEnabled,
                AccessToken = mercadoPagoPaymentSettings.AccessToken,
                PublicKey = mercadoPagoPaymentSettings.PublicKey,
                PaymentDescription = mercadoPagoPaymentSettings.PaymentDescription,
                SelectedCurrencyIds = mercadoPagoPaymentSettings.SelectedCurrencyIdList ?? new List<int>()
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

            return View("~/Plugins/Payments.MercadoPago/Views/Configure.cshtml", model);
        }

        [HttpPost]
        [CheckPermission(StandardPermission.Configuration.MANAGE_PAYMENT_METHODS)]
        [Area(AreaNames.ADMIN)]
        public async Task<IActionResult> Configure(ConfigurationModel model)
        {
            if (!ModelState.IsValid)
                return View("~/Plugins/Payments.MercadoPago/Views/Configure.cshtml", model);

            var storeScope = await _storeContext.GetActiveStoreScopeConfigurationAsync();
            var mercadoPagoPaymentSettings = await _settingService.LoadSettingAsync<MercadoPagoPaymentSettings>(storeScope);

            mercadoPagoPaymentSettings.AdditionalFeeEnabled = model.AdditionalFeeEnabled;
            mercadoPagoPaymentSettings.AccessToken = model.AccessToken;
            mercadoPagoPaymentSettings.PublicKey = model.PublicKey;
            mercadoPagoPaymentSettings.PaymentDescription = model.PaymentDescription;
            mercadoPagoPaymentSettings.SelectedCurrencyIdList = model.SelectedCurrencyIds;

            await _settingService.SaveSettingOverridablePerStoreAsync(
                mercadoPagoPaymentSettings,
                x => x.AdditionalFeeEnabled,
                model.AdditionalFeeEnabledOverrideForStore,
                storeScope,
                false);

            await _settingService.SaveSettingOverridablePerStoreAsync(
                mercadoPagoPaymentSettings,
                x => x.AccessToken,
                model.AccessTokenOverrideForStore,
                storeScope,
                false);

            await _settingService.SaveSettingOverridablePerStoreAsync(
                mercadoPagoPaymentSettings,
                x => x.PublicKey,
                model.PublicKeyOverrideForStore,
                storeScope,
                false);

            await _settingService.SaveSettingOverridablePerStoreAsync(
                mercadoPagoPaymentSettings,
                x => x.PaymentDescription,
                model.PaymentDescriptionOverrideForStore,
                storeScope,
                false);

            await _settingService.SaveSettingOverridablePerStoreAsync(
                mercadoPagoPaymentSettings,
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

            var (succeeded, orderId) = await _mercadoPagoService.ReturnAsync(paymentResponse);

            if (!succeeded)
            {
                return RedirectToRoute("OrderDetails", new { orderId = orderId });
            }

            return RedirectToRoute("CheckoutCompleted", new { orderId = orderId });
        }


        [AcceptVerbs("GET", "POST")]
        public async Task<IActionResult> Confirm()
        {
            try
            {
            var confirmationResponse = await ConfirmationResponse.FromHttpRequestAsync(HttpContext.Request);
                var (succeeded, orderId) = await _mercadoPagoService.ConfirmAsync(confirmationResponse);

                if (!succeeded)
                {
                    _logger.Error($"Fallo en la confirmación del pedido con ID {orderId}.");
                }

                return Ok();
            }
            catch (Exception ex)
            {
                _logger.Error($"Error procesando la confirmación de MercadoPago: {ex.Message}", ex);

                return Ok();
            }
        }
    }
}
