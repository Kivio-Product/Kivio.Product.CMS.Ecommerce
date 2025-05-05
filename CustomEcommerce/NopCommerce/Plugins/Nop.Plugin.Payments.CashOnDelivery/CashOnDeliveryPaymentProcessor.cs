using Microsoft.AspNetCore.Http;
using Nop.Core;
using Nop.Core.Domain.Orders;
using Nop.Core.Domain.Payments;
using Nop.Plugin.Payments.CashOnDelivery.Components;
using Nop.Services.Configuration;
using Nop.Services.Localization;
using Nop.Services.Orders;
using Nop.Services.Payments;
using Nop.Services.Plugins;
using Nop.Services.Customers;
using Nop.Services.Common;
using Nop.Services.Catalog;
using Nop.Services.Directory;

namespace Nop.Plugin.Payments.CashOnDelivery;

/// <summary>
/// CashOnDelivery payment processor
/// </summary>
public class CashOnDeliveryPaymentProcessor : BasePlugin, IPaymentMethod
{
    #region Fields

    private readonly CashOnDeliveryPaymentSettings _cashOnDeliveryPaymentSettings;
    private readonly ILocalizationService _localizationService;
    private readonly IOrderTotalCalculationService _orderTotalCalculationService;
    private readonly ISettingService _settingService;
    private readonly IShoppingCartService _shoppingCartService;
    private readonly IWebHelper _webHelper;
    private readonly IOrderService _orderService;
    private readonly ICustomerService _customerService;
    private readonly IAddressService _addressService;
    private readonly IProductService _productService;
    private readonly IStateProvinceService _stateProvinceService;
    private readonly ICountryService _countryService;
    private readonly IHttpContextAccessor _httpContextAccessor;

    #endregion

    #region Ctor

    public CashOnDeliveryPaymentProcessor(CashOnDeliveryPaymentSettings cashOnDeliveryPaymentSettings,
        ILocalizationService localizationService,
        IOrderTotalCalculationService orderTotalCalculationService,
        ISettingService settingService,
        IShoppingCartService shoppingCartService,
        IWebHelper webHelper,
        IOrderService orderService,
        ICustomerService customerService,
        IAddressService addressService,
        IProductService productService,
        IStateProvinceService stateProvinceService,
        ICountryService countryService,
        IHttpContextAccessor httpContextAccessor)
    {
        _cashOnDeliveryPaymentSettings = cashOnDeliveryPaymentSettings;
        _localizationService = localizationService;
        _orderTotalCalculationService = orderTotalCalculationService;
        _settingService = settingService;
        _shoppingCartService = shoppingCartService;
        _webHelper = webHelper;
        _orderService = orderService;
        _customerService = customerService;
        _addressService = addressService;
        _productService = productService;
        _stateProvinceService = stateProvinceService;
        _countryService = countryService;
        _httpContextAccessor = httpContextAccessor;
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
        return Task.FromResult(new ProcessPaymentResult { NewPaymentStatus = PaymentStatus.Pending });
    }

    /// <summary>
    /// Post process payment (used by payment gateways that require redirecting to a third-party URL)
    /// </summary>
    /// <param name="postProcessPaymentRequest">Payment info required for an order processing</param>
    public async Task PostProcessPaymentAsync(PostProcessPaymentRequest postProcessPaymentRequest)
    {
        var order = await _orderService.GetOrderByIdAsync(postProcessPaymentRequest.Order.Id);
        if (order == null)
            return;

        var customer = await _customerService.GetCustomerByIdAsync(order.CustomerId);
        if (customer == null)
            return;

        var billingAddress = await _addressService.GetAddressByIdAsync(order.BillingAddressId);
        var shippingAddress = order.ShippingAddressId.HasValue 
            ? await _addressService.GetAddressByIdAsync(order.ShippingAddressId.Value) 
            : billingAddress;

        var message = $"¡Hola! Soy {customer.FirstName} {customer.LastName} y quiero confirmar mi pedido.\n\n" +
                     $"*Detalles del Pedido contraentrega*\n" +
                     $"Referencia: {order.CustomOrderNumber}\n" +
                     $"Fecha: {order.CreatedOnUtc:dd/MM/yyyy HH:mm}\n\n" +
                     $"*Productos:*\n";

        var orderItems = await _orderService.GetOrderItemsAsync(order.Id);
        foreach (var item in orderItems)
        {
            var product = await _productService.GetProductByIdAsync(item.ProductId);
            message += $"• {product.Name} x {item.Quantity} - {item.PriceInclTax:C}\n";
        }

        var stateProvince = shippingAddress.StateProvinceId.HasValue 
            ? await _stateProvinceService.GetStateProvinceByIdAsync(shippingAddress.StateProvinceId.Value) 
            : null;
        var country = shippingAddress.CountryId.HasValue 
            ? await _countryService.GetCountryByIdAsync(shippingAddress.CountryId.Value) 
            : null;

        message += $"\n*Dirección de Envío:*\n" +
                  $"{shippingAddress.FirstName} {shippingAddress.LastName}\n" +
                  $"{shippingAddress.Address1}\n" +
                  $"{shippingAddress.City}, {stateProvince?.Name} {shippingAddress.ZipPostalCode}\n" +
                  $"{country?.Name}\n" +
                  $"Teléfono: {shippingAddress.PhoneNumber}\n\n" +
                  $"*Resumen de Pago:*\n" +
                  $"Subtotal: {order.OrderSubtotalInclTax:C}\n" +
                  $"Envío: {order.OrderShippingInclTax:C}\n" +
                  $"Total: {order.OrderTotal:C}\n\n" +
                  $"Por favor, confirma mi pedido contraentrega y avísame cuando esté listo para envío. ¡Gracias!";

        var whatsappNumber = _cashOnDeliveryPaymentSettings.WhatsAppNumber?.TrimStart('+');
        if (string.IsNullOrEmpty(whatsappNumber))
        {
            return;
        }

        var whatsappUrl = $"https://wa.me/{whatsappNumber}?text={Uri.EscapeDataString(message)}";

        var httpContext = _httpContextAccessor.HttpContext;
        if (httpContext != null)
        {
            httpContext.Response.Clear();
            httpContext.Response.ContentType = "text/html";
            var scriptRedirect = $"<script>window.location.href='{whatsappUrl}';</script>";
            await httpContext.Response.WriteAsync(scriptRedirect);
        }
    }

    /// <summary>
    /// Returns a value indicating whether payment method should be hidden during checkout
    /// </summary>
    /// <param name="cart">Shoping cart</param>
    /// <returns>true - hide; false - display.</returns>
    public async Task<bool> HidePaymentMethodAsync(IList<ShoppingCartItem> cart)
    {
        //you can put any logic here
        //for example, hide this payment method if all products in the cart are downloadable
        //or hide this payment method if current customer is from certain country
        return _cashOnDeliveryPaymentSettings.ShippableProductRequired && !await _shoppingCartService.ShoppingCartRequiresShippingAsync(cart);
    }

    /// <summary>
    /// Gets additional handling fee
    /// </summary>
    /// <param name="cart">Shoping cart</param>
    /// <returns>Additional handling fee</returns>
    public async Task<decimal> GetAdditionalHandlingFeeAsync(IList<ShoppingCartItem> cart)
    {
        return await _orderTotalCalculationService.CalculatePaymentAdditionalFeeAsync(cart,
            _cashOnDeliveryPaymentSettings.AdditionalFee, _cashOnDeliveryPaymentSettings.AdditionalFeePercentage);
    }

    /// <summary>
    /// Captures payment
    /// </summary>
    /// <param name="capturePaymentRequest">Capture payment request</param>
    /// <returns>Capture payment result</returns>
    public Task<CapturePaymentResult> CaptureAsync(CapturePaymentRequest capturePaymentRequest)
    {
        return Task.FromResult(new CapturePaymentResult { Errors = new[] { "Capture method not supported" } });
    }

    /// <summary>
    /// Refunds a payment
    /// </summary>
    /// <param name="refundPaymentRequest">Request</param>
    /// <returns>Result</returns>
    public Task<RefundPaymentResult> RefundAsync(RefundPaymentRequest refundPaymentRequest)
    {
        return Task.FromResult(new RefundPaymentResult { Errors = new[] { "Refund method not supported" } });
    }

    /// <summary>
    /// Voids a payment
    /// </summary>
    /// <param name="voidPaymentRequest">Request</param>
    /// <returns>Result</returns>
    public Task<VoidPaymentResult> VoidAsync(VoidPaymentRequest voidPaymentRequest)
    {
        return Task.FromResult(new VoidPaymentResult { Errors = new[] { "Void method not supported" } });
    }

    /// <summary>
    /// Process recurring payment
    /// </summary>
    /// <param name="processPaymentRequest">Payment info required for an order processing</param>
    /// <returns>Process payment result</returns>
    public Task<ProcessPaymentResult> ProcessRecurringPaymentAsync(ProcessPaymentRequest processPaymentRequest)
    {
        return Task.FromResult(new ProcessPaymentResult { Errors = new[] { "Recurring payment not supported" } });
    }

    /// <summary>
    /// Cancels a recurring payment
    /// </summary>
    /// <param name="cancelPaymentRequest">Request</param>
    /// <returns>Result</returns>
    public Task<CancelRecurringPaymentResult> CancelRecurringPaymentAsync(CancelRecurringPaymentRequest cancelPaymentRequest)
    {
        return Task.FromResult(new CancelRecurringPaymentResult { Errors = new[] { "Recurring payment not supported" } });
    }

    /// <summary>
    /// Gets a value indicating whether customers can complete a payment after order is placed but not completed (for redirection payment methods)
    /// </summary>
    /// <param name="order">Order</param>
    /// <returns>Result</returns>
    public Task<bool> CanRePostProcessPaymentAsync(Order order)
    {
        ArgumentNullException.ThrowIfNull(order);

        //it's not a redirection payment method. So we always return false
        return Task.FromResult(false);
    }

    public Task<IList<string>> ValidatePaymentFormAsync(IFormCollection form)
    {
        return Task.FromResult<IList<string>>(new List<string>());
    }

    public Task<ProcessPaymentRequest> GetPaymentInfoAsync(IFormCollection form)
    {
        return Task.FromResult(new ProcessPaymentRequest());
    }

    public override string GetConfigurationPageUrl()
    {
        return $"{_webHelper.GetStoreLocation()}Admin/PaymentCashOnDelivery/Configure";
    }

    public override async Task InstallAsync()
    {
        var settings = new CashOnDeliveryPaymentSettings
        {
            DescriptionText = "<p>In cases where an order is placed, an authorized representative will contact you, personally or over telephone, to confirm the order.<br />After the order is confirmed, it will be processed.<br />Orders once confirmed, cannot be cancelled.</p><p>P.S. You can edit this text from admin panel.</p>",
            SkipPaymentInfo = false,
            WhatsAppNumber = "+1234567890" // Número de WhatsApp por defecto
        };

        await _settingService.SaveSettingAsync(settings);

        //locales
        await _localizationService.AddOrUpdateLocaleResourceAsync(new Dictionary<string, string>
        {
            ["Plugins.Payment.CashOnDelivery.DescriptionText"] = "Description",
            ["Plugins.Payment.CashOnDelivery.DescriptionText.Hint"] = "Enter info that will be shown to customers during checkout",
            ["Plugins.Payment.CashOnDelivery.AdditionalFee"] = "Additional fee",
            ["Plugins.Payment.CashOnDelivery.AdditionalFee.Hint"] = "The additional fee.",
            ["Plugins.Payment.CashOnDelivery.AdditionalFeePercentage"] = "Additional fee. Use percentage",
            ["Plugins.Payment.CashOnDelivery.AdditionalFeePercentage.Hint"] = "Determines whether to apply a percentage additional fee to the order total. If not enabled, a fixed value is used.",
            ["Plugins.Payment.CashOnDelivery.ShippableProductRequired"] = "Shippable product required",
            ["Plugins.Payment.CashOnDelivery.ShippableProductRequired.Hint"] = "An option indicating whether shippable products are required in order to display this payment method during checkout.",
            ["Plugins.Payment.CashOnDelivery.PaymentMethodDescription"] = "Será redireccionado a Whatsapp para completar el pedido",
            ["Plugins.Payment.CashOnDelivery.SkipPaymentInfo"] = "Skip payment information page",
            ["Plugins.Payment.CashOnDelivery.SkipPaymentInfo.Hint"] = "An option indicating whether we should display a payment information page for this plugin.",
            ["Plugins.Payment.CashOnDelivery.WhatsAppNumber"] = "WhatsApp Number",
            ["Plugins.Payment.CashOnDelivery.WhatsAppNumber.Hint"] = "Enter the WhatsApp number where customers will be redirected to confirm their order."
        });

        await base.InstallAsync();
    }

    public override async Task UninstallAsync()
    {
        //settings
        await _settingService.DeleteSettingAsync<CashOnDeliveryPaymentSettings>();

        //locales
        await _localizationService.DeleteLocaleResourcesAsync("Plugins.Payment.CashOnDelivery");

        await base.UninstallAsync();
    }

    /// <summary>
    /// Gets a name of a view component for displaying plugin in public store ("payment info" checkout step)
    /// </summary>
    /// <returns>View component name</returns>
    public Type GetPublicViewComponent()
    {
        return typeof(PaymentCashOnDeliveryViewComponent);
    }

    /// <summary>
    /// Gets a payment method description that will be displayed on checkout pages in the public store
    /// </summary>
    /// <remarks>
    /// return description of this payment method to be display on "payment method" checkout step. good practice is to make it localizable
    /// for example, for a redirection payment method, description may be like this: "You will be redirected to PayPal site to complete the payment"
    /// </remarks>
    /// <returns>A task that represents the asynchronous operation</returns>
    public async Task<string> GetPaymentMethodDescriptionAsync()
    {
        return await _localizationService.GetResourceAsync("Plugins.Payment.CashOnDelivery.PaymentMethodDescription");
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
    public bool SkipPaymentInfo => _cashOnDeliveryPaymentSettings.SkipPaymentInfo;

    /// <summary>
    /// Gets a whatsapp number
    /// </summary>
    public string WhatsAppNumber => _cashOnDeliveryPaymentSettings.WhatsAppNumber;

    #endregion
}
