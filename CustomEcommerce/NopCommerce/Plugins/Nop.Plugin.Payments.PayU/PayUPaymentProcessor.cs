using System.Collections.Generic;
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
                SandboxClientId = "300746",
                SandboxClientSecret = "2ee86a66e5d97e3fadc400c9f19b065d",
                SandboxSecondKey = "b6ca15b0d1020e8094d9b5f8d163db54"
            });

            await _localizationService.AddOrUpdateLocaleResourceAsync("Plugins.Payments.PayU.Fields.UseSandbox", "Use Sandbox");
            await _localizationService.AddOrUpdateLocaleResourceAsync("Plugins.Payments.PayU.Fields.SandboxClientId", "Sandbox client id");
            await _localizationService.AddOrUpdateLocaleResourceAsync("Plugins.Payments.PayU.Fields.SandboxClientSecret",
                 "Sandbox client secret");
            await _localizationService.AddOrUpdateLocaleResourceAsync("Plugins.Payments.PayU.Fields.SandboxSecondKey",
                 "Sandbox second key");
            await _localizationService.AddOrUpdateLocaleResourceAsync("Plugins.Payments.PayU.Fields.ClientId", "Client id");
            await _localizationService.AddOrUpdateLocaleResourceAsync("Plugins.Payments.PayU.Fields.ClientSecret", "Client secret");
            await _localizationService.AddOrUpdateLocaleResourceAsync("Plugins.Payments.PayU.Fields.SecondKey", "Second key");

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
            await _localizationService.DeleteLocaleResourcesAsync("Plugins.Payments.PayU.Fields.SandboxClientId");
            await _localizationService.DeleteLocaleResourcesAsync("Plugins.Payments.PayU.Fields.SandboxClientSecret");
            await _localizationService.DeleteLocaleResourcesAsync("Plugins.Payments.PayU.Fields.SandboxSecondKey");
            await _localizationService.DeleteLocaleResourcesAsync("Plugins.Payments.PayU.Fields.ClientId");
            await _localizationService.DeleteLocaleResourcesAsync("Plugins.Payments.PayU.Fields.ClientSecret");
            await _localizationService.DeleteLocaleResourcesAsync("Plugins.Payments.PayU.Fields.SecondKey");

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
            return _payUService.Refund(refundPaymentRequest);
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