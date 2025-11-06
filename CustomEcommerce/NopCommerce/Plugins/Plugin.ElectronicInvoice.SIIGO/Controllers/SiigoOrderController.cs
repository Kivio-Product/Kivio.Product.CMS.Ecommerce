using Microsoft.AspNetCore.Mvc;
using Nop.Core;
using Nop.Core.Domain.Orders;
using Nop.Services.Configuration;
using Nop.Services.Orders;
using Nop.Services.Common;
using Nop.Services.Security;
using Nop.Services.Logging;
using Nop.Services.Localization;
using Nop.Web.Framework;
using Nop.Web.Framework.Controllers;
using Nop.Web.Framework.Mvc.Filters;
using Plugin.ElectronicInvoice.SIIGO.Models;

namespace Plugin.ElectronicInvoice.SIIGO.Controllers
{
    [Area(AreaNames.ADMIN)]
    [AuthorizeAdmin]
    [AutoValidateAntiforgeryToken]
    public class SiigoOrderController : BasePluginController
    {
        private readonly IStoreContext _storeContext;
        private readonly ISettingService _settingService;
        private readonly IOrderService _orderService;
        private readonly IGenericAttributeService _genericAttributeService;
        private readonly IOrderProcessingService _orderProcessingService;
        private readonly IPermissionService _permissionService;
        private readonly ILogger _logger;
        private readonly ILocalizationService _localizationService;

        public SiigoOrderController(
            IStoreContext storeContext,
            ISettingService settingService,
            IOrderService orderService,
            IGenericAttributeService genericAttributeService,
            IOrderProcessingService orderProcessingService,
            IPermissionService permissionService,
            ILogger logger,
            ILocalizationService localizationService)
        {
            _storeContext = storeContext;
            _settingService = settingService;
            _orderService = orderService;
            _genericAttributeService = genericAttributeService;
            _orderProcessingService = orderProcessingService;
            _permissionService = permissionService;
            _logger = logger;
            _localizationService = localizationService;
        }

        [HttpPost]
        [CheckPermission(StandardPermission.Orders.ORDERS_CREATE_EDIT_DELETE)]
        public async Task<IActionResult> CheckPaymentMethodBeforeMarkAsPaid(int orderId)
        {
            try
            {
                var order = await _orderService.GetOrderByIdAsync(orderId);
                if (order == null)
                    return Json(new { success = false, message = "Order not found" });

                // Check if it's CashOnDelivery
                if (order.PaymentMethodSystemName?.Equals("Payments.CashOnDelivery", StringComparison.OrdinalIgnoreCase) == true)
                {
                    // Check if SIIGO plugin is enabled
                    var siigoSettings = await _settingService.LoadSettingAsync<SiigoSettings>();
                    if (siigoSettings.IsEnabled)
                    {
                        // Load payment method mappings to check for sub-options
                        var storeId = await _storeContext.GetActiveStoreScopeConfigurationAsync();
                        var paymentMappings = await _settingService.LoadSettingAsync<SiigoPaymentMethodMappingSettings>(storeId);
                        
                        var cashOnDeliveryMapping = paymentMappings.PaymentMethodMappings
                            .FirstOrDefault(m => m.PaymentMethodSystemName.Equals("Payments.CashOnDelivery", StringComparison.OrdinalIgnoreCase));

                        if (cashOnDeliveryMapping?.HasSubOptions == true)
                        {
                            var enabledSubOptions = cashOnDeliveryMapping.SubOptions?.Where(so => so.IsEnabled).ToList();
                            if (enabledSubOptions?.Any() == true)
                            {
                                var model = new PaymentSubOptionSelectionModel
                                {
                                    OrderId = orderId,
                                    PaymentMethodName = "Pago Contra Entrega",
                                    AvailableSubOptions = enabledSubOptions.Select(so => new PaymentSubOptionModel
                                    {
                                        Name = so.Name,
                                        SiigoCode = so.SiigoCode,
                                        Description = string.IsNullOrEmpty(so.Description) 
                                            ? $"{so.Name} (Código SIIGO: {so.SiigoCode})"
                                            : $"{so.Name} - {so.Description} (Código SIIGO: {so.SiigoCode})"
                                    }).ToList()
                                };

                                await _logger.InformationAsync($"Order {orderId} requires payment sub-option selection for CashOnDelivery. Available options: {enabledSubOptions.Count}");

                                return Json(new { 
                                    success = true, 
                                    requiresSubOption = true, 
                                    model = model 
                                });
                            }
                        }
                    }
                }

                // No sub-option required, proceed normally
                return Json(new { success = true, requiresSubOption = false });
            }
            catch (Exception ex)
            {
                await _logger.ErrorAsync($"Error checking payment method for order {orderId}: {ex.Message}", ex);
                return Json(new { success = false, message = "Error checking payment method" });
            }
        }

        [HttpPost]
        [CheckPermission(StandardPermission.Orders.ORDERS_CREATE_EDIT_DELETE)]
        public async Task<IActionResult> MarkAsPaidWithSubOption(int orderId, int selectedSubOptionCode, string selectedSubOptionName = null)
        {
            try
            {
                var order = await _orderService.GetOrderByIdAsync(orderId);
                if (order == null)
                    return Json(new { success = false, message = "Order not found" });

                // Save the selected sub-option as order attributes
                await _genericAttributeService.SaveAttributeAsync(order, "SelectedPaymentSubOption", selectedSubOptionCode);
                await _genericAttributeService.SaveAttributeAsync(order, "SelectedPaymentSubOptionName", selectedSubOptionName ?? "Unknown");
                await _genericAttributeService.SaveAttributeAsync(order, "SelectedPaymentSubOptionDate", DateTime.UtcNow);

                await _logger.InformationAsync($"Order {orderId} payment sub-option saved: Code={selectedSubOptionCode}, Name={selectedSubOptionName}");

                // Mark the order as paid using the standard service
                await _orderProcessingService.MarkOrderAsPaidAsync(order);

                await _logger.InformationAsync($"Order {orderId} marked as paid with CashOnDelivery sub-option: {selectedSubOptionName} (Code: {selectedSubOptionCode})");

                return Json(new { success = true, message = "Order marked as paid successfully" });
            }
            catch (Exception ex)
            {
                await _logger.ErrorAsync($"Error marking order {orderId} as paid with sub-option: {ex.Message}", ex);
                return Json(new { success = false, message = $"Error: {ex.Message}" });
            }
        }

        [HttpPost]
        [CheckPermission(StandardPermission.Orders.ORDERS_VIEW)]
        public async Task<IActionResult> GetOrderPaymentSubOptionInfo(int orderId)
        {
            try
            {
                var order = await _orderService.GetOrderByIdAsync(orderId);
                if (order == null)
                    return Json(new { success = false, message = "Order not found" });

                var selectedSubOptionCode = await _genericAttributeService.GetAttributeAsync<int>(order, "SelectedPaymentSubOption");
                var selectedSubOptionName = await _genericAttributeService.GetAttributeAsync<string>(order, "SelectedPaymentSubOptionName");
                var selectedSubOptionDate = await _genericAttributeService.GetAttributeAsync<DateTime?>(order, "SelectedPaymentSubOptionDate");
                
                // Get SIIGO invoice public URL
                var publicUrl = await _genericAttributeService.GetAttributeAsync<string>(order, "SiigoInvoicePublicUrl");

                return Json(new { 
                    success = true,
                    hasSubOption = selectedSubOptionCode > 0,
                    subOptionCode = selectedSubOptionCode,
                    subOptionName = selectedSubOptionName,
                    subOptionDate = selectedSubOptionDate?.ToString("yyyy-MM-dd HH:mm"),
                    publicUrl = publicUrl
                });
            }
            catch (Exception ex)
            {
                await _logger.ErrorAsync($"Error getting payment sub-option info for order {orderId}: {ex.Message}", ex);
                return Json(new { success = false, message = "Error getting payment info" });
            }
        }

        /// <summary>
        /// Get localized resources for JavaScript
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetLocalizedResources()
        {
            var resources = new
            {
                ModalTitle = await _localizationService.GetResourceAsync("Plugins.ElectronicInvoice.SIIGO.PaymentSubOption.Modal.Title"),
                SelectLabel = await _localizationService.GetResourceAsync("Plugins.ElectronicInvoice.SIIGO.PaymentSubOption.Modal.SelectLabel"),
                SelectPlaceholder = await _localizationService.GetResourceAsync("Plugins.ElectronicInvoice.SIIGO.PaymentSubOption.Modal.SelectPlaceholder"),
                InfoMessage = await _localizationService.GetResourceAsync("Plugins.ElectronicInvoice.SIIGO.PaymentSubOption.Modal.InfoMessage"),
                CancelButton = await _localizationService.GetResourceAsync("Plugins.ElectronicInvoice.SIIGO.PaymentSubOption.Modal.CancelButton"),
                ConfirmButton = await _localizationService.GetResourceAsync("Plugins.ElectronicInvoice.SIIGO.PaymentSubOption.Modal.ConfirmButton"),
                ProcessingButton = await _localizationService.GetResourceAsync("Plugins.ElectronicInvoice.SIIGO.PaymentSubOption.Modal.ProcessingButton"),
                ValidationError = await _localizationService.GetResourceAsync("Plugins.ElectronicInvoice.SIIGO.PaymentSubOption.Modal.ValidationError"),
                SuccessMessage = await _localizationService.GetResourceAsync("Plugins.ElectronicInvoice.SIIGO.PaymentSubOption.Modal.SuccessMessage"),
                ErrorMessage = await _localizationService.GetResourceAsync("Plugins.ElectronicInvoice.SIIGO.PaymentSubOption.Modal.ErrorMessage"),
                LoadingMessage = await _localizationService.GetResourceAsync("Plugins.ElectronicInvoice.SIIGO.PaymentSubOption.Modal.LoadingMessage")
            };

            return Json(resources);
        }
    }
}