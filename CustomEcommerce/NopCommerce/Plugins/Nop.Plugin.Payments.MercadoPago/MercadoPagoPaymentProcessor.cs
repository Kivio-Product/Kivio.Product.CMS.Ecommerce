using Microsoft.AspNetCore.Http;
using Nop.Core;
using Nop.Core.Domain.Orders;
using Nop.Core.Domain.Payments;
using Nop.Plugin.Payments.MercadoPago.Services;
using Nop.Plugin.Payments.MercadoPago.Components;
using Nop.Services.Configuration;
using Nop.Services.Localization;
using Nop.Services.Plugins;
using Nop.Services.Payments;

namespace Nop.Plugin.Payments.MercadoPago
{
    public class MercadoPagoPaymentProcessor : BasePlugin, IPaymentMethod
    {
        private readonly ISettingService _settingService;
        private readonly IMercadoPagoService _mercadoPagoService;
        private readonly IWebHelper _webHelper;
        private readonly ILocalizationService _localizationService;

        public bool SupportCapture => false; // Not imlpemented
        public bool SupportVoid => false; // Not implemented
        public bool SupportPartiallyRefund => false; // Not implemented
        public bool SupportRefund => false; // Not implemented

        public RecurringPaymentType RecurringPaymentType => RecurringPaymentType.NotSupported;
        public PaymentMethodType PaymentMethodType => PaymentMethodType.Redirection;
        public bool SkipPaymentInfo => false;
        public string PaymentMethodDescription => _localizationService.GetResourceAsync("Plugins.Payments.MercadoPago.PaymentMethodDescription").Result;

        public MercadoPagoPaymentProcessor(
            ISettingService settingService,
            IMercadoPagoService mercadoPagoService,
            IWebHelper webHelper,
            ILocalizationService localizationService)
        {
            _settingService = settingService;
            _mercadoPagoService = mercadoPagoService;
            _webHelper = webHelper;
            _localizationService = localizationService;
        }

        public override async Task InstallAsync()
        {
            await _settingService.SaveSettingAsync(new MercadoPagoPaymentSettings()
            {
                AccessToken = "APP_USR-1683302810237968-033107-c0924fe514fac654b1a4215e25b93918-3304049115",
                PublicKey = "APP_USR-092fd15f-a238-4470-8d06-f8af6fd97e38",
                PaymentDescription = "Compra en KivioCommerce"
            });

            await _localizationService.AddOrUpdateLocaleResourceAsync("Plugins.Payments.MercadoPago.Fields.AdditionalFeeEnabled", "Additional fee");
            await _localizationService.AddOrUpdateLocaleResourceAsync("Plugins.Payments.MercadoPago.Fields.AdditionalFeeEnabled.Hint", "Enable this option to add an additional fee for using this payment method.");

            await _localizationService.AddOrUpdateLocaleResourceAsync("Plugins.Payments.MercadoPago.Fields.AccessToken", "Access Token");
            await _localizationService.AddOrUpdateLocaleResourceAsync("Plugins.Payments.MercadoPago.Fields.AccessToken.Hint", "Enter the MercadoPago access token.");

            await _localizationService.AddOrUpdateLocaleResourceAsync("Plugins.Payments.MercadoPago.Fields.PublicKey", "Public Key");
            await _localizationService.AddOrUpdateLocaleResourceAsync("Plugins.Payments.MercadoPago.Fields.PublicKey.Hint", "Enter the MercadoPago public key.");

            await _localizationService.AddOrUpdateLocaleResourceAsync("Plugins.Payments.MercadoPago.Fields.PaymentDescription", "Payment Description");
            await _localizationService.AddOrUpdateLocaleResourceAsync("Plugins.Payments.MercadoPago.Fields.PaymentDescription.Hint", "Enter a description for the payment that will be displayed to the customer.");

            await _localizationService.AddOrUpdateLocaleResourceAsync("Plugins.Payments.MercadoPago.Fields.SelectedCurrencies", "Allowed Currencies");
            await _localizationService.AddOrUpdateLocaleResourceAsync("Plugins.Payments.MercadoPago.Fields.SelectedCurrencies.Hint", "Select the currencies for which this payment method will be available.");

            await _localizationService.AddOrUpdateLocaleResourceAsync("Plugins.Payments.MercadoPago.PaymentMethodDescription",
                "You will be redirected to MercadoPago to complete the payment");
            await _localizationService.AddOrUpdateLocaleResourceAsync("Plugins.Payments.MercadoPago.PaymentInfo",
                 "You will be redirected to MercadoPago to complete the order.");

            await base.InstallAsync();
        }

        public override async Task UninstallAsync()
        {
            await _settingService.DeleteSettingAsync<MercadoPagoPaymentSettings>();

            await _localizationService.DeleteLocaleResourcesAsync("Plugins.Payments.MercadoPago.Fields.AdditionalFeeEnabled");
            await _localizationService.DeleteLocaleResourcesAsync("Plugins.Payments.MercadoPago.Fields.AdditionalFeeEnabled.Hint");
            await _localizationService.DeleteLocaleResourcesAsync("Plugins.Payments.MercadoPago.Fields.AccessToken");
            await _localizationService.DeleteLocaleResourcesAsync("Plugins.Payments.MercadoPago.Fields.AccessToken.Hint");
            await _localizationService.DeleteLocaleResourcesAsync("Plugins.Payments.MercadoPago.Fields.PublicKey");
            await _localizationService.DeleteLocaleResourcesAsync("Plugins.Payments.MercadoPago.Fields.PublicKey.Hint");
            await _localizationService.DeleteLocaleResourcesAsync("Plugins.Payments.MercadoPago.Fields.PaymentDescription");
            await _localizationService.DeleteLocaleResourcesAsync("Plugins.Payments.MercadoPago.Fields.PaymentDescription.Hint");
            await _localizationService.DeleteLocaleResourcesAsync("Plugins.Payments.MercadoPago.Fields.SelectedCurrencies");
            await _localizationService.DeleteLocaleResourcesAsync("Plugins.Payments.MercadoPago.Fields.SelectedCurrencies.Hint");

            await _localizationService.DeleteLocaleResourcesAsync("Plugins.Payments.MercadoPago.PaymentMethodDescription");
            await _localizationService.DeleteLocaleResourcesAsync("Plugins.Payments.MercadoPago.PaymentInfo");

            await base.UninstallAsync();
        }

        public override string GetConfigurationPageUrl()
        {
            return $"{_webHelper.GetStoreLocation()}Admin/PaymentMercadoPago/Configure";
        }

        public Task<bool> CanRePostProcessPaymentAsync(Order order)
        {
            if (order == null)
                throw new ArgumentNullException("order");

            return Task.FromResult(false);
        }

        public Task<decimal> GetAdditionalHandlingFeeAsync(IList<ShoppingCartItem> cart)
        {
            return _mercadoPagoService.GetAdditionalFeeAsync(cart);
        }

        public ProcessPaymentRequest GetPaymentInfo(IFormCollection form)
        {
            return new ProcessPaymentRequest();
        }

        public string GetPublicViewComponentName()
        {
            return "PaymentMercadoPago";
        }

        public Task<bool> HidePaymentMethodAsync(IList<ShoppingCartItem> cart)
        {
            return _mercadoPagoService.HidePaymentMethodAsync();
        }

        public Task<ProcessPaymentResult> ProcessPaymentAsync(ProcessPaymentRequest processPaymentRequest)
        {
            var result = new ProcessPaymentResult { NewPaymentStatus = PaymentStatus.Pending };
            return Task.FromResult(result);
        }

        public async Task PostProcessPaymentAsync(PostProcessPaymentRequest postProcessPaymentRequest)
        {
            await _mercadoPagoService.RedirectToMercadoPagoPayment(postProcessPaymentRequest);
            return;
        }

        public Task<RefundPaymentResult> RefundAsync(RefundPaymentRequest refundPaymentRequest)
        {
            //return _mercadoPagoService.Refund(refundPaymentRequest);
            return Task.FromResult(new RefundPaymentResult());
        }

        public Task<IList<string>> ValidatePaymentFormAsync(IFormCollection form)
        {
            return Task.FromResult<IList<string>>(new List<string>());
        }

        public Task<CapturePaymentResult> CaptureAsync(CapturePaymentRequest capturePaymentRequest)
        {
            return Task.FromResult(new CapturePaymentResult());
        }

        public Task<VoidPaymentResult> VoidAsync(VoidPaymentRequest voidPaymentRequest)
        {
            return Task.FromResult(new VoidPaymentResult());
        }

        public Task<ProcessPaymentResult> ProcessRecurringPaymentAsync(ProcessPaymentRequest processPaymentRequest)
        {
            return Task.FromResult(new ProcessPaymentResult());
        }

        public Task<CancelRecurringPaymentResult> CancelRecurringPaymentAsync(CancelRecurringPaymentRequest cancelPaymentRequest)
        {
            return Task.FromResult(new CancelRecurringPaymentResult());
        }

        public Task<ProcessPaymentRequest> GetPaymentInfoAsync(IFormCollection form)
        {
            return Task.FromResult(new ProcessPaymentRequest());
        }

        public Type GetPublicViewComponent()
        {
            return typeof(PaymentMercadoPagoViewComponent);
        }

        public async Task<string> GetPaymentMethodDescriptionAsync()
        {
            return await _localizationService.GetResourceAsync("Plugins.Payments.MercadoPago.PaymentMethodDescription");

        }
    }
}