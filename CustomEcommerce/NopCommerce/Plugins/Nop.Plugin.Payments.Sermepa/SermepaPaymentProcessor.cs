//Contributor: Noel Revuelta
using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;
using Nop.Core;
using Nop.Core.Domain.Orders;
using Nop.Core.Domain.Payments;
using Nop.Services.Plugins;
using Nop.Services.Configuration;
using Nop.Services.Localization;
using Nop.Services.Payments;
using Nop.Web.Framework;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Nop.Plugin.Payments.Sermepa.Components;

namespace Nop.Plugin.Payments.Sermepa
{
    /// <summary>
    /// Sermepa payment processor
    /// </summary>
    public class SermepaPaymentProcessor : BasePlugin, IPaymentMethod
    {
        #region Fields

        private readonly SermepaPaymentSettings _sermepaPaymentSettings;
        private readonly ISettingService _settingService;
        private readonly IWebHelper _webHelper;
        private readonly ILocalizationService _localizationService;
        private readonly IHttpContextAccessor _httpContextAccessor;

        #endregion

        #region Ctor

        public SermepaPaymentProcessor(SermepaPaymentSettings sermepaPaymentSettings,
            ISettingService settingService,
            IWebHelper webHelper,
            ILocalizationService localizationService,
            IHttpContextAccessor httpContextAccessor)
        {
            _sermepaPaymentSettings = sermepaPaymentSettings;
            _settingService = settingService;
            _webHelper = webHelper;
            _localizationService = localizationService;
            _httpContextAccessor = httpContextAccessor;
        }

        #endregion

        #region Utilities

        /// <summary>
        /// Gets Sermepa URL
        /// </summary>
        /// <returns></returns>
        private string GetSermepaUrl()
        {
            return _sermepaPaymentSettings.Pruebas ? "https://sis-t.sermepa.es:25443/sis/realizarPago" :
                "https://sis.sermepa.es/sis/realizarPago";
        }

        #endregion

        #region Methods

        /// <summary>
        /// Process a payment
        /// </summary>
        /// <param name="processPaymentRequest">Payment info required for an order processing</param>
        /// <returns>Process payment result</returns>
        public Task<ProcessPaymentResult> ProcessPaymentAsync(ProcessPaymentRequest processPaymentRequest)
        {
            var result = new ProcessPaymentResult { NewPaymentStatus = PaymentStatus.Pending };
            return Task.FromResult(result);
        }

        /// <summary>
        /// Post process payment (used by payment gateways that require redirecting to a third-party URL)
        /// </summary>
        /// <param name="postProcessPaymentRequest">Payment info required for an order processing</param>
        public Task PostProcessPaymentAsync(PostProcessPaymentRequest postProcessPaymentRequest)
        {
            //Notificación On-Line
            var strDs_Merchant_MerchantURL = _webHelper.GetStoreLocation(false) + "Plugins/PaymentSermepa/Return";

            //URL OK
            var strDs_Merchant_UrlOK = _webHelper.GetStoreLocation(false) + "checkout/completed";

            //URL KO
            var strDs_Merchant_UrlKO = _webHelper.GetStoreLocation(false) + "Plugins/PaymentSermepa/Error";

            //Numero de pedido
            //You have to change the id of the orders table to begin with a number of at least 4 digits.
            var strDs_Merchant_Order = postProcessPaymentRequest.Order.Id.ToString("0000");

            //Nombre del comercio
            var strDs_Merchant_MerchantName = _sermepaPaymentSettings.NombreComercio;

            //Importe
            var amount = ((int)Convert.ToInt64(postProcessPaymentRequest.Order.OrderTotal * 100)).ToString();
            var strDs_Merchant_Amount = amount;

            //Código de comercio
            var strDs_Merchant_MerchantCode = _sermepaPaymentSettings.FUC;

            //Moneda
            var strDs_Merchant_Currency = _sermepaPaymentSettings.Moneda;

            //Terminal
            var strDs_Merchant_Terminal = _sermepaPaymentSettings.Terminal;

            //Tipo de transaccion (0 - Autorización)
            var strDs_Merchant_TransactionType = "0";

            //Clave
            var clave = _sermepaPaymentSettings.Pruebas ? _sermepaPaymentSettings.ClavePruebas : _sermepaPaymentSettings.ClaveReal;

            //Calculo de la firma
            var sha = string.Format("{0}{1}{2}{3}{4}{5}{6}",
                strDs_Merchant_Amount,
                strDs_Merchant_Order,
                strDs_Merchant_MerchantCode,
                strDs_Merchant_Currency,
                strDs_Merchant_TransactionType,
                strDs_Merchant_MerchantURL,
                clave);

            SHA1 shaM = new SHA1Managed();
            var shaResult = shaM.ComputeHash(Encoding.Default.GetBytes(sha));
            var shaResultStr = BitConverter.ToString(shaResult).Replace("-", "");

            //Creamos el POST
            var remotePostHelper = new RemotePost(_httpContextAccessor, _webHelper)
            {
                FormName = "form1",
                Url = GetSermepaUrl()
            };

            remotePostHelper.Add("Ds_Merchant_Amount", strDs_Merchant_Amount);
            remotePostHelper.Add("Ds_Merchant_Currency", strDs_Merchant_Currency);
            remotePostHelper.Add("Ds_Merchant_Order", strDs_Merchant_Order);
            remotePostHelper.Add("Ds_Merchant_MerchantCode", strDs_Merchant_MerchantCode);
            remotePostHelper.Add("Ds_Merchant_TransactionType", strDs_Merchant_TransactionType);
            remotePostHelper.Add("Ds_Merchant_MerchantURL", strDs_Merchant_MerchantURL);
            remotePostHelper.Add("Ds_Merchant_MerchantSignature", shaResultStr);
            remotePostHelper.Add("Ds_Merchant_Terminal", strDs_Merchant_Terminal);
            remotePostHelper.Add("Ds_Merchant_MerchantName", strDs_Merchant_MerchantName);
            remotePostHelper.Add("Ds_Merchant_UrlOK", strDs_Merchant_UrlOK);
            remotePostHelper.Add("Ds_Merchant_UrlKO", strDs_Merchant_UrlKO);

            remotePostHelper.Post();
            return Task.CompletedTask;
        }

        /// <summary>
        /// Returns a value indicating whether payment method should be hidden during checkout
        /// </summary>
        /// <param name="cart">Shoping cart</param>
        /// <returns>true - hide; false - display.</returns>
        public Task<bool> HidePaymentMethodAsync(IList<ShoppingCartItem> cart)
        {
            //you can put any logic here
            //for example, hide this payment method if all products in the cart are downloadable
            //or hide this payment method if current customer is from certain country
            return Task.FromResult(false);
        }

        /// <summary>
        /// Gets additional handling fee
        /// </summary>
        /// <param name="cart">Shoping cart</param>
        /// <returns>Additional handling fee</returns>
        public Task<decimal> GetAdditionalHandlingFeeAsync(IList<ShoppingCartItem> cart)
        {
            return Task.FromResult(_sermepaPaymentSettings.AdditionalFee);
        }

        /// <summary>
        /// Captures payment
        /// </summary>
        /// <param name="capturePaymentRequest">Capture payment request</param>
        /// <returns>Capture payment result</returns>
        public Task<CapturePaymentResult> CaptureAsync(CapturePaymentRequest capturePaymentRequest)
        {
            var result = new CapturePaymentResult();
            result.AddError("Capture method not supported");
            return Task.FromResult(result);
        }

        /// <summary>
        /// Refunds a payment
        /// </summary>
        /// <param name="refundPaymentRequest">Request</param>
        /// <returns>Result</returns>
        public Task<RefundPaymentResult> RefundAsync(RefundPaymentRequest refundPaymentRequest)
        {
            var result = new RefundPaymentResult();
            result.AddError("Refund method not supported");
            return Task.FromResult(result);
        }

        /// <summary>
        /// Voids a payment
        /// </summary>
        /// <param name="voidPaymentRequest">Request</param>
        /// <returns>Result</returns>
        public Task<VoidPaymentResult> VoidAsync(VoidPaymentRequest voidPaymentRequest)
        {
            var result = new VoidPaymentResult();
            result.AddError("Void method not supported");
            return Task.FromResult(result);
        }

        /// <summary>
        /// Process recurring payment
        /// </summary>
        /// <param name="processPaymentRequest">Payment info required for an order processing</param>
        /// <returns>Process payment result</returns>
        public Task<ProcessPaymentResult> ProcessRecurringPaymentAsync(ProcessPaymentRequest processPaymentRequest)
        {
            var result = new ProcessPaymentResult();
            result.AddError("Recurring payment not supported");
            return Task.FromResult(result);
        }

        /// <summary>
        /// Cancels a recurring payment
        /// </summary>
        /// <param name="cancelPaymentRequest">Request</param>
        /// <returns>Result</returns>
        public Task<CancelRecurringPaymentResult> CancelRecurringPaymentAsync(CancelRecurringPaymentRequest cancelPaymentRequest)
        {
            var result = new CancelRecurringPaymentResult();
            result.AddError("Recurring payment not supported");
            return Task.FromResult(result);
        }

        /// <summary>
        /// Gets a value indicating whether customers can complete a payment after order is placed but not completed (for redirection payment methods)
        /// </summary>
        /// <param name="order">Order</param>
        /// <returns>Result</returns>
        public Task<bool> CanRePostProcessPaymentAsync(Order order)
        {
            if (order == null)
                throw new ArgumentNullException("order");

            return Task.FromResult(false);
        }

        /// <summary>
        /// Gets a configuration page URL
        /// </summary>
        public override string GetConfigurationPageUrl()
        {
            return $"{_webHelper.GetStoreLocation()}Admin/PaymentSermepa/Configure";
        }

        /// <summary>
        /// Gets a name of a view component for displaying plugin in public store ("payment info" checkout step)
        /// </summary>
        /// <returns>View component name</returns>
        public string GetPublicViewComponentName()
        {
            return "PaymentSermepa";
        }

        /// <summary>
        /// Gets a payment method description that will be displayed on checkout pages in the public store
        /// </summary>
        public async Task<string> GetPaymentMethodDescriptionAsync()
        {
            return await _localizationService.GetResourceAsync("Plugins.Payments.Sermepa.PaymentMethodDescription");
        }

        /// <summary>
        /// Validate payment form
        /// </summary>
        /// <param name="form">The parsed form values</param>
        /// <returns>
        /// A task that represents the asynchronous operation
        /// The task result contains the list of validating errors
        /// </returns>
        public Task<IList<string>> ValidatePaymentFormAsync(IFormCollection form)
        {
            return Task.FromResult<IList<string>>(new List<string>());
        }

        /// <summary>
        /// Get payment information
        /// </summary>
        /// <param name="form">The parsed form values</param>
        /// <returns>
        /// A task that represents the asynchronous operation
        /// The task result contains the payment info holder
        /// </returns>
        public Task<ProcessPaymentRequest> GetPaymentInfoAsync(IFormCollection form)
        {
            return Task.FromResult(new ProcessPaymentRequest());
        }

        /// <summary>
        /// Gets a type of a view component for displaying plugin in public store ("payment info" checkout step)
        /// </summary>
        /// <returns>View component type</returns>
        public Type GetPublicViewComponent()
        {
            return typeof(PaymentSermepaViewComponent);
        }

        public override async Task InstallAsync()
        {
            var settings = new SermepaPaymentSettings()
            {
                NombreComercio = "",
                Titular = "",
                Producto = "",
                FUC = "",
                Terminal = "",
                Moneda = "",
                ClaveReal = "",
                ClavePruebas = "",
                Pruebas = true,
                AdditionalFee = 0,
            };
            await _settingService.SaveSettingAsync(settings);

            await _localizationService.AddOrUpdateLocaleResourceAsync("Plugins.Payments.Sermepa.Fields.NombreComercio", "Nombre del comercio");
            await _localizationService.AddOrUpdateLocaleResourceAsync("Plugins.Payments.Sermepa.Fields.Titular", "Nombre y Apellidos del titular");
            await _localizationService.AddOrUpdateLocaleResourceAsync("Plugins.Payments.Sermepa.Fields.Producto", "Descripción del producto");
            await _localizationService.AddOrUpdateLocaleResourceAsync("Plugins.Payments.Sermepa.Fields.FUC", "FUC comercio");
            await _localizationService.AddOrUpdateLocaleResourceAsync("Plugins.Payments.Sermepa.Fields.Terminal", "Terminal");
            await _localizationService.AddOrUpdateLocaleResourceAsync("Plugins.Payments.Sermepa.Fields.Moneda", "Moneda");
            await _localizationService.AddOrUpdateLocaleResourceAsync("Plugins.Payments.Sermepa.Fields.ClaveReal", "Clave Real");
            await _localizationService.AddOrUpdateLocaleResourceAsync("Plugins.Payments.Sermepa.Fields.ClavePruebas", "Clave Pruebas");
            await _localizationService.AddOrUpdateLocaleResourceAsync("Plugins.Payments.Sermepa.Fields.Pruebas", "En pruebas");
            await _localizationService.AddOrUpdateLocaleResourceAsync("Plugins.Payments.Sermepa.Fields.AdditionalFee", "Additional fee");
            await _localizationService.AddOrUpdateLocaleResourceAsync("Plugins.Payments.Sermepa.Fields.AdditionalFeePercentage", "Additional fee percentage");
            await _localizationService.AddOrUpdateLocaleResourceAsync("Plugins.Payments.Sermepa.Fields.NombreComercio.Hint", "Nombre del comercio");
            await _localizationService.AddOrUpdateLocaleResourceAsync("Plugins.Payments.Sermepa.Fields.Titular.Hint", "Nombre y Apellidos del titular");
            await _localizationService.AddOrUpdateLocaleResourceAsync("Plugins.Payments.Sermepa.Fields.Producto.Hint", "Descripción del producto");
            await _localizationService.AddOrUpdateLocaleResourceAsync("Plugins.Payments.Sermepa.Fields.FUC.Hint", "FUC comercio");
            await _localizationService.AddOrUpdateLocaleResourceAsync("Plugins.Payments.Sermepa.Fields.Terminal.Hint", "Terminal");
            await _localizationService.AddOrUpdateLocaleResourceAsync("Plugins.Payments.Sermepa.Fields.Moneda.Hint", "Moneda");
            await _localizationService.AddOrUpdateLocaleResourceAsync("Plugins.Payments.Sermepa.Fields.ClaveReal.Hint", "Clave Real");
            await _localizationService.AddOrUpdateLocaleResourceAsync("Plugins.Payments.Sermepa.Fields.ClavePruebas.Hint", "Clave Pruebas");
            await _localizationService.AddOrUpdateLocaleResourceAsync("Plugins.Payments.Sermepa.Fields.Pruebas.Hint", "En pruebas");
            await _localizationService.AddOrUpdateLocaleResourceAsync("Plugins.Payments.Sermepa.Fields.AdditionalFee.Hint", "Additional fee");
            await _localizationService.AddOrUpdateLocaleResourceAsync("Plugins.Payments.Sermepa.Fields.AdditionalFeePercentage.Hint", "Additional fee percentage");

            await _localizationService.AddOrUpdateLocaleResourceAsync("Plugins.Payments.Sermepa.RedirectionTip", "You will be redirected to Sermepa site to complete the order.");
            await _localizationService.AddOrUpdateLocaleResourceAsync("Plugins.Payments.Sermepa.PaymentMethodDescription", "You will be redirected to Sermepa site to complete the order.");

            await base.InstallAsync();
        }

        public override async Task UninstallAsync()
        {
            await _localizationService.DeleteLocaleResourcesAsync("Plugins.Payments.Sermepa");

            await base.UninstallAsync();
        }

        #endregion

        #region Properies

        /// <summary>
        /// Gets a value indicating whether capture is supported
        /// </summary>
        public bool SupportCapture => false;

        /// <summary>
        /// Gets a value indicating whether partial refund is supported
        /// </summary>
        public bool SupportPartiallyRefund => false;

        /// <summary>
        /// Gets a value indicating whether refund is supported
        /// </summary>
        public bool SupportRefund => false;

        /// <summary>
        /// Gets a value indicating whether void is supported
        /// </summary>
        public bool SupportVoid => false;

        /// <summary>
        /// Gets a recurring payment type of payment method
        /// </summary>
        public RecurringPaymentType RecurringPaymentType => RecurringPaymentType.NotSupported;

        /// <summary>
        /// Gets a payment method type
        /// </summary>
        public PaymentMethodType PaymentMethodType => PaymentMethodType.Redirection;

        /// <summary>
        /// Gets a value indicating whether we should display a payment information page for this plugin
        /// </summary>
        public bool SkipPaymentInfo => false;

        #endregion
    }
}
