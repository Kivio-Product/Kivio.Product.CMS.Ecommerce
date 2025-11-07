using Microsoft.AspNetCore.Mvc;
using Nop.Core;
using Nop.Plugin.Misc.DeliveryTimePicker.Models;
using Nop.Services.Common;
using Nop.Services.Orders;
using Nop.Web.Framework.Components;

namespace Nop.Plugin.Misc.DeliveryTimePicker.Components
{
    /// <summary>
    /// ViewComponent to display delivery time information in admin order details
    /// </summary>
    public class OrderDeliveryTimeInfoViewComponent : NopViewComponent
    {
        private readonly IGenericAttributeService _genericAttributeService;
        private readonly IOrderService _orderService;
        private readonly IStoreContext _storeContext;

        public OrderDeliveryTimeInfoViewComponent(
            IGenericAttributeService genericAttributeService,
            IOrderService orderService,
            IStoreContext storeContext)
        {
            _genericAttributeService = genericAttributeService;
            _orderService = orderService;
            _storeContext = storeContext;
        }

        public async Task<IViewComponentResult> InvokeAsync(string widgetZone, object additionalData)
        {
            dynamic data = additionalData;

            var order = await _orderService.GetOrderByIdAsync(data.Id as int? ?? 0);
            if (order == null)
                return Content("");

            var store = await _storeContext.GetCurrentStoreAsync();

            // Get delivery time information from order's generic attributes
            var deliveryDate = await _genericAttributeService.GetAttributeAsync<string>(order, "Delivery.Date", store.Id);
            var deliveryMinTime = await _genericAttributeService.GetAttributeAsync<string>(order, "Delivery.MinTime", store.Id);
            var deliveryMaxTime = await _genericAttributeService.GetAttributeAsync<string>(order, "Delivery.MaxTime", store.Id);
            var reservationId = await _genericAttributeService.GetAttributeAsync<string>(order, "Delivery.ReservationId", store.Id);

            // If no delivery information, don't show the widget
            if (string.IsNullOrEmpty(deliveryDate) || string.IsNullOrEmpty(deliveryMinTime) || string.IsNullOrEmpty(deliveryMaxTime))
                return Content("");

            var model = new OrderDeliveryTimeInfoModel
            {
                DeliveryDate = deliveryDate,
                DeliveryMinTime = deliveryMinTime,
                DeliveryMaxTime = deliveryMaxTime,
                ReservationId = reservationId
            };

            return View("~/Plugins/Misc.DeliveryTimePicker/Views/OrderDeliveryTimeInfo.cshtml", model);
        }
    }
}
