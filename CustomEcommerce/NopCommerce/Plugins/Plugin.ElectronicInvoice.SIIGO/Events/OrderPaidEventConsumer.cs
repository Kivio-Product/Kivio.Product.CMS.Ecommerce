using Nop.Core.Domain.Orders;
using Nop.Services.Configuration;
using Nop.Services.Events;
using Nop.Services.Logging;
using Plugin.ElectronicInvoice.SIIGO.Services;

namespace Plugin.ElectronicInvoice.SIIGO.Events
{
    public class OrderPaidEventConsumer : IConsumer<OrderPaidEvent>
    {
        private readonly ISiigoInvoiceService _siigoInvoiceService;
        private readonly ISettingService _settingService;
        private readonly ILogger _logger;

        public OrderPaidEventConsumer(
            ISiigoInvoiceService siigoInvoiceService,
            ISettingService settingService,
            ILogger logger)
        {
            _siigoInvoiceService = siigoInvoiceService;
            _settingService = settingService;
            _logger = logger;
        }

        public async Task HandleEventAsync(OrderPaidEvent eventMessage)
        {
            try
            {
                var siigoSettings = await _settingService.LoadSettingAsync<SiigoSettings>();

                if (!siigoSettings.IsEnabled)
                {
                    if (siigoSettings.LogEnabled)
                    {
                        await _logger.InformationAsync("SIIGO plugin disabled. Electronic invoice will not be generated.");
                    }
                    return;
                }

                if (eventMessage?.Order == null)
                {
                    await _logger.WarningAsync("Order paid event received with null order.");
                    return;
                }

                var order = eventMessage.Order;

                if (siigoSettings.LogEnabled)
                {
                    await _logger.InformationAsync($"Starting electronic invoicing process for order {order.Id}");
                }

                var siigoResponse = await _siigoInvoiceService.CreateInvoiceAsync(order);

                if (siigoResponse != null && !string.IsNullOrEmpty(siigoResponse.Id))
                {
                    var orderNote = $"SIIGO electronic invoice created - ID: {siigoResponse.Id}, Number: {siigoResponse.Number}";

                    if (siigoSettings.LogEnabled)
                    {
                        await _logger.InformationAsync($"Electronic invoice created successfully: {orderNote}");
                    }
                }
            }
            catch (Exception ex)
            {
                var errorMessage = $"Error processing electronic invoicing for order {eventMessage?.Order?.Id}: {ex.Message}";
                await _logger.ErrorAsync(errorMessage, ex);
            }
        }
    }
}
