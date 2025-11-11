using Nop.Core.Domain.Customers;
using Nop.Core.Domain.Orders;
using Nop.Services.Common;
using Nop.Services.Customers;
using Nop.Services.Events;
using Nop.Services.Localization;
using Nop.Services.Logging;
using System.Text.Json;

namespace Nop.Services.Orders.CustomWebhookEvents;

public class CustomEventConsumer : IConsumer<OrderPlacedEvent>, IConsumer<OrderStatusChangedEvent>
{
    private readonly ICustomerService _customerService;
    private readonly ILocalizationService _localization;
    private readonly ILogger _logger;
    private readonly IAddressService _addressService;
    private readonly IGenericAttributeService _genericAttributeService;


    public CustomEventConsumer(ICustomerService customerService, ILocalizationService localization, ILogger logger, IAddressService addressService, IGenericAttributeService genericAttributeService)
    {
        _customerService = customerService;
        _localization = localization;
        _logger = logger;
        _addressService = addressService;
        _genericAttributeService = genericAttributeService;

    }

    public async Task HandleEventAsync(OrderPlacedEvent eventMessage)
    {
        var order = eventMessage.Order;
        var customer = await _customerService.GetCustomerByIdAsync(order.CustomerId);
        var billingAddress = await _addressService.GetAddressByIdAsync(order.BillingAddressId);
        var webHookUrl = _localization.GetLocaleStringResourceByName("CustomWebhookBaseUrl", order.CustomerLanguageId, true).ResourceValue ?? string.Empty;

        if (string.IsNullOrEmpty(webHookUrl))
            return;

        // Prepare the payload for the webhook
        var payload = new
        {
            billingAddress = billingAddress,
            order = order,
            eventType = "orderPlaced",
            customer = customer,
            language = customer.LanguageId,
            deliveryTime = await GetOrderDeliverTimeAsync(order, customer)
        };

        // Send the webhook request (implementation not shown)
        await SendWebhookRequestAsync(webHookUrl, payload);
    }

    public async Task HandleEventAsync(OrderStatusChangedEvent eventMessage)
    {
        var order = eventMessage.Order;
        var customer = await _customerService.GetCustomerByIdAsync(order.CustomerId);
        var billingAddress = await _addressService.GetAddressByIdAsync(order.BillingAddressId);
        var webHookUrl = _localization.GetLocaleStringResourceByName("CustomWebhookBaseUrl", order.CustomerLanguageId, true).ResourceValue ?? string.Empty;
        if (string.IsNullOrEmpty(webHookUrl))
            return;

        // Prepare the payload for the webhook
        var payload = new
        {
            order = order,
            billingAddress = billingAddress,
            eventType = "orderStatusChanged",
            customer = customer,
            newStatus = order.OrderStatus.ToString(),
            oldStatus = eventMessage.PreviousOrderStatus.ToString(),
        };
        // Send the webhook request
        await SendWebhookRequestAsync(webHookUrl, payload);
    }

    private async Task SendWebhookRequestAsync(string url, object payload)
    {
        var jsonPayload = JsonSerializer.Serialize(payload);
        using var httpClient = new HttpClient();
        var content = new StringContent(jsonPayload, System.Text.Encoding.UTF8, "application/json");
        var response = await httpClient.PostAsync(url, content);

        if (!response.IsSuccessStatusCode)
        {
            var errorMessage = await response.Content.ReadAsStringAsync();
            _logger.Error(errorMessage, new Exception("Failed to send webhook request."));
        }
    }

    private async Task<object> GetOrderDeliverTimeAsync(Order order, Customer customer)
    {
        var deliveryDate = await _genericAttributeService.GetAttributeAsync<string>(order, "Delivery.Date", order.StoreId);
        var deliveryMinTime = await _genericAttributeService.GetAttributeAsync<string>(order, "Delivery.MinTime", order.StoreId);
        var deliveryMaxTime = await _genericAttributeService.GetAttributeAsync<string>(order, "Delivery.MaxTime", order.StoreId);

        if (string.IsNullOrEmpty(deliveryDate) || string.IsNullOrEmpty(deliveryMinTime) || string.IsNullOrEmpty(deliveryMaxTime))
        {
            deliveryDate ??= await _genericAttributeService.GetAttributeAsync<string>(customer, "Delivery.Date", order.StoreId);
            deliveryMinTime ??= await _genericAttributeService.GetAttributeAsync<string>(customer, "Delivery.MinTime", order.StoreId);
            deliveryMaxTime ??= await _genericAttributeService.GetAttributeAsync<string>(customer, "Delivery.MaxTime", order.StoreId);

            if (string.IsNullOrEmpty(deliveryDate) || string.IsNullOrEmpty(deliveryMinTime) || string.IsNullOrEmpty(deliveryMaxTime))
                return null;
        }

        return new
        {
            DeliveryDate = deliveryDate,
            DeliveryMinTime = deliveryMinTime,
            DeliveryMaxTime = deliveryMaxTime,
        };
    }


}