using Microsoft.AspNetCore.Mvc;
using Nop.Core;
using Nop.Core.Domain.Orders;
using Nop.Services.Common;
using Nop.Services.Orders;
using Nop.Web.Framework.Components;

namespace Plugin.ElectronicInvoice.SIIGO.Components
{
    [ViewComponent(Name = "SiigoPaymentSubOptionInfo")]
    public class SiigoPaymentSubOptionInfoViewComponent : NopViewComponent
    {
        private readonly IOrderService _orderService;
        private readonly IGenericAttributeService _genericAttributeService;

        public SiigoPaymentSubOptionInfoViewComponent(
            IOrderService orderService,
            IGenericAttributeService genericAttributeService)
        {
            _orderService = orderService;
            _genericAttributeService = genericAttributeService;
        }

        public async Task<IViewComponentResult> InvokeAsync(object orderModel)
        {
            if (orderModel == null)
                return Content("");

            // Get order ID from the model
            var orderIdProperty = orderModel.GetType().GetProperty("Id");
            if (orderIdProperty == null)
                return Content("");

            var orderId = (int)orderIdProperty.GetValue(orderModel);
            if (orderId <= 0)
                return Content("");

            // Get the order
            var order = await _orderService.GetOrderByIdAsync(orderId);
            if (order == null)
                return Content("");

            // Get the selected payment sub-option from generic attributes
            var selectedSubOptionCode = await _genericAttributeService.GetAttributeAsync<int>(order, "SelectedPaymentSubOption");
            var selectedSubOptionName = await _genericAttributeService.GetAttributeAsync<string>(order, "SelectedPaymentSubOptionName");
            var selectedSubOptionDate = await _genericAttributeService.GetAttributeAsync<DateTime?>(order, "SelectedPaymentSubOptionDate");

            if (selectedSubOptionCode <= 0 || string.IsNullOrEmpty(selectedSubOptionName))
                return Content("");

            // Return view with the selected sub-option data
            ViewBag.SelectedSubOptionCode = selectedSubOptionCode;
            ViewBag.SelectedSubOptionName = selectedSubOptionName;
            ViewBag.SelectedSubOptionDate = selectedSubOptionDate;
            ViewBag.PaymentMethodName = order.PaymentMethodSystemName;
            
            return View("~/Plugins/Plugin.ElectronicInvoice.SIIGO/Views/Components/SiigoPaymentSubOptionInfo/Default.cshtml");
        }
    }
}