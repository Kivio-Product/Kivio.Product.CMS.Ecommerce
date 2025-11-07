using System;
using System.Threading.Tasks;
using Nop.Core.Domain.Orders;
using Nop.Core.Events;
using Nop.Plugin.Misc.DeliveryTimePicker.Services;
using Nop.Services.Common;
using Nop.Services.Customers;
using Nop.Services.Events;
using Nop.Services.Logging;

namespace Nop.Plugin.Misc.DeliveryTimePicker.Infrastructure
{
    /// <summary>
    /// Event consumer for OrderPlaced event
    /// Copies delivery time attributes from customer to order and confirms reservation
    /// </summary>
    public class OrderPlacedEventConsumer : IConsumer<OrderPlacedEvent>
    {
        #region Fields

        private readonly IGenericAttributeService _genericAttributeService;
        private readonly ICustomerService _customerService;
        private readonly IDeliveryTimeService _deliveryTimeService;
        private readonly ILogger _logger;

        #endregion

        #region Ctor

        public OrderPlacedEventConsumer(
            IGenericAttributeService genericAttributeService,
            ICustomerService customerService,
            IDeliveryTimeService deliveryTimeService,
            ILogger logger)
        {
            _genericAttributeService = genericAttributeService;
            _customerService = customerService;
            _deliveryTimeService = deliveryTimeService;
            _logger = logger;
        }

        #endregion

        #region Methods

        /// <summary>
        /// Handle the OrderPlaced event
        /// </summary>
        /// <param name="eventMessage">Event message</param>
        public async Task HandleEventAsync(OrderPlacedEvent eventMessage)
        {
            if (eventMessage?.Order == null)
                return;

            var order = eventMessage.Order;
            var customer = await _customerService.GetCustomerByIdAsync(order.CustomerId);

            if (customer == null)
                return;

            try
            {
                // Get delivery time attributes from customer
                var deliveryDate = await _genericAttributeService.GetAttributeAsync<string>(
                    customer, "Delivery.Date", order.StoreId);
                
                var deliveryMinTime = await _genericAttributeService.GetAttributeAsync<string>(
                    customer, "Delivery.MinTime", order.StoreId);
                
                var deliveryMaxTime = await _genericAttributeService.GetAttributeAsync<string>(
                    customer, "Delivery.MaxTime", order.StoreId);
                
                var reservationIdStr = await _genericAttributeService.GetAttributeAsync<string>(
                    customer, "Delivery.ReservationId", order.StoreId);

                // Only copy if we have delivery time data
                if (!string.IsNullOrEmpty(deliveryDate) && 
                    !string.IsNullOrEmpty(deliveryMinTime) && 
                    !string.IsNullOrEmpty(deliveryMaxTime))
                {
                    // Copy to order generic attributes
                    await _genericAttributeService.SaveAttributeAsync(order, "Delivery.Date", deliveryDate,  order.StoreId);
                    await _genericAttributeService.SaveAttributeAsync(order, "Delivery.MinTime", deliveryMinTime,  order.StoreId);
                    await _genericAttributeService.SaveAttributeAsync(order, "Delivery.MaxTime", deliveryMaxTime,  order.StoreId);

                    // Confirm the reservation if exists
                    if (!string.IsNullOrEmpty(reservationIdStr) && int.TryParse(reservationIdStr, out int reservationId))
                    {
                        await _genericAttributeService.SaveAttributeAsync(order, "Delivery.ReservationId", reservationIdStr,  order.StoreId);

                        // Confirm the temporary reservation (makes it permanent and links to order)
                        await _deliveryTimeService.ConfirmReservationAsync(reservationId, order.Id);
                        
                        await _logger.InformationAsync(
                            $"Delivery reservation {reservationId} confirmed for order {order.Id}");
                    }

                    await _logger.InformationAsync(
                        $"Delivery time copied to order {order.Id}: Date={deliveryDate}, Time={deliveryMinTime}-{deliveryMaxTime}");
                    
                    // Optional: Clean up customer attributes after copying to order
                    // This prevents the delivery time from persisting for future orders
                    await _genericAttributeService.SaveAttributeAsync<string>(customer, "Delivery.Date", null, order.StoreId);
                    await _genericAttributeService.SaveAttributeAsync<string>(customer, "Delivery.MinTime", null, order.StoreId);
                    await _genericAttributeService.SaveAttributeAsync<string>(customer, "Delivery.MaxTime", null, order.StoreId);
                    await _genericAttributeService.SaveAttributeAsync<string>(customer, "Delivery.ReservationId", null, order.StoreId);
                }
            }
            catch (Exception ex)
            {
                await _logger.ErrorAsync(
                    $"Error copying delivery time to order {order.Id}", ex);
            }
        }

        #endregion
    }
}
