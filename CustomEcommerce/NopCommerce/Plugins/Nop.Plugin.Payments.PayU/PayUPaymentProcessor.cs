using Microsoft.AspNetCore.Http;
using Nop.Core;
using Nop.Core.Domain.Orders;
using Nop.Core.Domain.Payments;
using Nop.Plugin.Payments.PayU.Services;
using Nop.Plugin.Payments.PayU.Components;
using Nop.Services.Configuration;
using Nop.Services.Localization;
using Nop.Services.Plugins;
using Nop.Services.Payments;

namespace Nop.Plugin.Payments.PayU
{
    public class PayUPaymentProcessor : BasePlugin, IPaymentMethod
    {
        private readonly ISettingService _settingService;
        private readonly IPayUService _payUService;
        private readonly IWebHelper _webHelper;
        private readonly ILocalizationService _localizationService;

        public bool SupportCapture => false; // Not imlpemented
        public bool SupportVoid => false; // Not implemented
        public bool SupportPartiallyRefund => true;
        public bool SupportRefund => true;

        public RecurringPaymentType RecurringPaymentType => RecurringPaymentType.NotSupported;
        public PaymentMethodType PaymentMethodType => PaymentMethodType.Redirection;
        public bool SkipPaymentInfo => false;
        public string PaymentMethodDescription => _localizationService.GetResourceAsync("Plugins.Payments.PayU.PaymentMethodDescription").Result;

        public PayUPaymentProcessor(
            ISettingService settingService,
            IPayUService payUService,
            IWebHelper webHelper,
            ILocalizationService localizationService)
        {
            _settingService = settingService;
            _payUService = payUService;
            _webHelper = webHelper;
            _localizationService = localizationService;
        }

        public override async Task InstallAsync()
        {
            await _settingService.SaveSettingAsync(new PayUPaymentSettings()
            {
                UseSandbox = true,
                AccountId = "512321",
                MerchantId = "508029",
                ClientLoginId = "pRRXKOl8ikMmt9u",
                ClientSecretKey = "4Vj8eK4rloUd272L48hsrarnUA",
                ClientPublicKey = "PKaC6H4cEDJD919n705L544kSU",
                PaymentDescription = "Compra en KivioCommerce"
            });

            await _localizationService.AddOrUpdateLocaleResourceAsync("Plugins.Payments.PayU.Fields.UseSandbox", "Use Sandbox");
            await _localizationService.AddOrUpdateLocaleResourceAsync("Plugins.Payments.PayU.Fields.UseSandbox.Hint", "Enable this option to use the sandbox environment for testing purposes.");

            await _localizationService.AddOrUpdateLocaleResourceAsync("Plugins.Payments.PayU.Fields.AccountId", "Account ID");
            await _localizationService.AddOrUpdateLocaleResourceAsync("Plugins.Payments.PayU.Fields.AccountId.Hint", "Enter the Account ID provided by PayU.");

            await _localizationService.AddOrUpdateLocaleResourceAsync("Plugins.Payments.PayU.Fields.MerchantId", "Merchant ID");
            await _localizationService.AddOrUpdateLocaleResourceAsync("Plugins.Payments.PayU.Fields.MerchantId.Hint", "Enter the Merchant ID provided by PayU.");

            await _localizationService.AddOrUpdateLocaleResourceAsync("Plugins.Payments.PayU.Fields.ClientLoginId", "API Login");
            await _localizationService.AddOrUpdateLocaleResourceAsync("Plugins.Payments.PayU.Fields.ClientLoginId.Hint", "Enter the API Login ID provided by PayU.");

            await _localizationService.AddOrUpdateLocaleResourceAsync("Plugins.Payments.PayU.Fields.ClientSecretKey", "API Key");
            await _localizationService.AddOrUpdateLocaleResourceAsync("Plugins.Payments.PayU.Fields.ClientSecretKey.Hint", "Enter the API Key provided by PayU.");

            await _localizationService.AddOrUpdateLocaleResourceAsync("Plugins.Payments.PayU.Fields.ClientPublicKey", "Public Key");
            await _localizationService.AddOrUpdateLocaleResourceAsync("Plugins.Payments.PayU.Fields.ClientPublicKey.Hint", "Enter the Public Key provided by PayU.");

            await _localizationService.AddOrUpdateLocaleResourceAsync("Plugins.Payments.PayU.Fields.PaymentDescription", "Payment Description");
            await _localizationService.AddOrUpdateLocaleResourceAsync("Plugins.Payments.PayU.Fields.PaymentDescription.Hint", "Enter a description for the payment that will be displayed to the customer.");

            await _localizationService.AddOrUpdateLocaleResourceAsync("Plugins.Payments.PayU.PaymentMethodDescription",
                  "You will be redirected to PayU site to complete the payment");
            await _localizationService.AddOrUpdateLocaleResourceAsync("Plugins.Payments.PayU.PaymentInfo",
                 "You will be redirected to PayU site to complete the order.");

            await base.InstallAsync();
        }

        public override async Task UninstallAsync()
        {
            await _settingService.DeleteSettingAsync<PayUPaymentSettings>();

            await _localizationService.DeleteLocaleResourcesAsync("Plugins.Payments.PayU.Fields.UseSandbox");
            await _localizationService.DeleteLocaleResourcesAsync("Plugins.Payments.PayU.Fields.UseSandbox.Hint");
            await _localizationService.DeleteLocaleResourcesAsync("Plugins.Payments.PayU.Fields.AccountId");
            await _localizationService.DeleteLocaleResourcesAsync("Plugins.Payments.PayU.Fields.AccountId.Hint");
            await _localizationService.DeleteLocaleResourcesAsync("Plugins.Payments.PayU.Fields.MerchantId");
            await _localizationService.DeleteLocaleResourcesAsync("Plugins.Payments.PayU.Fields.MerchantId.Hint");
            await _localizationService.DeleteLocaleResourcesAsync("Plugins.Payments.PayU.Fields.ClientLoginId");
            await _localizationService.DeleteLocaleResourcesAsync("Plugins.Payments.PayU.Fields.ClientLoginId.Hint");
            await _localizationService.DeleteLocaleResourcesAsync("Plugins.Payments.PayU.Fields.ClientSecretKey");
            await _localizationService.DeleteLocaleResourcesAsync("Plugins.Payments.PayU.Fields.ClientSecretKey.Hint");
            await _localizationService.DeleteLocaleResourcesAsync("Plugins.Payments.PayU.Fields.ClientPublicKey");
            await _localizationService.DeleteLocaleResourcesAsync("Plugins.Payments.PayU.Fields.ClientPublicKey.Hint");
            await _localizationService.DeleteLocaleResourcesAsync("Plugins.Payments.PayU.Fields.PaymentDescription");
            await _localizationService.DeleteLocaleResourcesAsync("Plugins.Payments.PayU.Fields.PaymentDescription.Hint");

            await _localizationService.DeleteLocaleResourcesAsync("Plugins.Payments.PayU.PaymentMethodDescription");
            await _localizationService.DeleteLocaleResourcesAsync("Plugins.Payments.PayU.PaymentInfo");

            await base.UninstallAsync();
        }

        public override string GetConfigurationPageUrl()
        {
            return $"{_webHelper.GetStoreLocation()}Admin/PaymentPayU/Configure";
        }

        public Task<bool> CanRePostProcessPaymentAsync(Order order)
        {
            if (order == null)
                throw new ArgumentNullException("order");

            return Task.FromResult(false);
        }

        public Task<decimal> GetAdditionalHandlingFeeAsync(IList<ShoppingCartItem> cart)
        {
            return Task.FromResult(0m);
        }

        public ProcessPaymentRequest GetPaymentInfo(IFormCollection form)
        {
            return new ProcessPaymentRequest();
        }

        public string GetPublicViewComponentName()
        {
            return "PaymentPayU";
        }

        public Task<bool> HidePaymentMethodAsync(IList<ShoppingCartItem> cart)
        {
            return Task.FromResult(false);
        }

        public Task<ProcessPaymentResult> ProcessPaymentAsync(ProcessPaymentRequest processPaymentRequest)
        {
            var result = new ProcessPaymentResult { NewPaymentStatus = PaymentStatus.Pending };
            return Task.FromResult(result);
        }

        public Task PostProcessPaymentAsync(PostProcessPaymentRequest postProcessPaymentRequest)
        {
            _payUService.RedirectToPayUPayment(postProcessPaymentRequest);
            return Task.CompletedTask;

        }

        public Task<RefundPaymentResult> RefundAsync(RefundPaymentRequest refundPaymentRequest)
        {
            //return _payUService.Refund(refundPaymentRequest);
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
            return typeof(PaymentPayUViewComponent);
        }

        public async Task<string> GetPaymentMethodDescriptionAsync()
        {
            return await _localizationService.GetResourceAsync("Plugins.Payments.PayU.PaymentMethodDescription");

        }
    }
}