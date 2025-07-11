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

    public CustomEventConsumer(ICustomerService customerService, ILocalizationService localization, ILogger logger, IAddressService addressService)
    {
        _customerService = customerService;
        _localization = localization;
        _logger = logger;
        _addressService = addressService;
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
            language = customer.LanguageId
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


}