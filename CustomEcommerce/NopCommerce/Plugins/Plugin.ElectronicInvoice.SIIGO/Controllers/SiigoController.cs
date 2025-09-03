using Microsoft.AspNetCore.Mvc;
using Nop.Core;
using Nop.Services.Configuration;
using Nop.Services.Localization;
using Nop.Services.Messages;
using Nop.Services.Security;
using Nop.Services.Orders;
using Nop.Services.Customers;
using Nop.Services.Common;
using Nop.Web.Framework;
using Nop.Web.Framework.Controllers;
using Nop.Web.Framework.Mvc.Filters;
using Plugin.ElectronicInvoice.SIIGO.Models;
using Plugin.ElectronicInvoice.SIIGO.Services;
using Plugin.ElectronicInvoice.SIIGO.Data;

namespace Plugin.ElectronicInvoice.SIIGO.Controllers
{
    [Area(AreaNames.ADMIN)]
    [AuthorizeAdmin]
    [AutoValidateAntiforgeryToken]
    public class SiigoController : BasePluginController
    {
        private readonly IStoreContext _storeContext;
        private readonly ISettingService _settingService;
        private readonly ILocalizationService _localizationService;
        private readonly INotificationService _notificationService;
        private readonly IPermissionService _permissionService;
        private readonly ISiigoInvoiceService _siigoInvoiceService;
        private readonly ISiigoAuthService _siigoAuthService;
        private readonly IOrderService _orderService;
        private readonly ICustomerService _customerService;
        private readonly IGenericAttributeService _genericAttributeService;

        public SiigoController(
            IStoreContext storeContext,
            ISettingService settingService,
            ILocalizationService localizationService,
            INotificationService notificationService,
            IPermissionService permissionService,
            ISiigoInvoiceService siigoInvoiceService,
            ISiigoAuthService siigoAuthService,
            IOrderService orderService,
            ICustomerService customerService,
            IGenericAttributeService genericAttributeService)
        {
            _storeContext = storeContext;
            _settingService = settingService;
            _localizationService = localizationService;
            _notificationService = notificationService;
            _permissionService = permissionService;
            _siigoInvoiceService = siigoInvoiceService;
            _siigoAuthService = siigoAuthService;
            _orderService = orderService;
            _customerService = customerService;
            _genericAttributeService = genericAttributeService;
        }

        [CheckPermission(StandardPermission.Configuration.MANAGE_PLUGINS)]
        public async Task<IActionResult> Configure()
        {
            var storeId = await _storeContext.GetActiveStoreScopeConfigurationAsync();
            var siigoSettings = await _settingService.LoadSettingAsync<SiigoSettings>(storeId);

            var model = new ConfigurationModel
            {
                ActiveStoreScopeConfiguration = storeId,
                ApiBaseUrl = siigoSettings.ApiBaseUrl,
                ApiBaseUrl_OverrideForStore = await _settingService.SettingExistsAsync(siigoSettings, x => x.ApiBaseUrl, storeId),
                PartnerId = siigoSettings.PartnerId,
                PartnerId_OverrideForStore = await _settingService.SettingExistsAsync(siigoSettings, x => x.PartnerId, storeId),
                Username = siigoSettings.Username,
                Username_OverrideForStore = await _settingService.SettingExistsAsync(siigoSettings, x => x.Username, storeId),
                AccessKey = siigoSettings.AccessKey,
                AccessKey_OverrideForStore = await _settingService.SettingExistsAsync(siigoSettings, x => x.AccessKey, storeId),
                DocumentId = siigoSettings.DocumentId,
                DocumentId_OverrideForStore = await _settingService.SettingExistsAsync(siigoSettings, x => x.DocumentId, storeId),
                DefaultItemCode = siigoSettings.DefaultItemCode,
                DefaultItemCode_OverrideForStore = await _settingService.SettingExistsAsync(siigoSettings, x => x.DefaultItemCode, storeId),
                SellerId = siigoSettings.SellerId,
                SellerId_OverrideForStore = await _settingService.SettingExistsAsync(siigoSettings, x => x.SellerId, storeId),
                PaymentMethodId = siigoSettings.PaymentMethodId,
                PaymentMethodId_OverrideForStore = await _settingService.SettingExistsAsync(siigoSettings, x => x.PaymentMethodId, storeId),
                TaxIdWithTax = siigoSettings.TaxIdWithTax,
                TaxIdWithTax_OverrideForStore = await _settingService.SettingExistsAsync(siigoSettings, x => x.TaxIdWithTax, storeId),
                AccountGroup = siigoSettings.AccountGroup,
                AccountGroup_OverrideForStore = await _settingService.SettingExistsAsync(siigoSettings, x => x.AccountGroup, storeId),
                SendByEmail = siigoSettings.SendByEmail,
                SendByEmail_OverrideForStore = await _settingService.SettingExistsAsync(siigoSettings, x => x.SendByEmail, storeId),
                SendStamp = siigoSettings.SendStamp,
                SendStamp_OverrideForStore = await _settingService.SettingExistsAsync(siigoSettings, x => x.SendStamp, storeId),
                CopyToEmail = siigoSettings.CopyToEmail,
                CopyToEmail_OverrideForStore = await _settingService.SettingExistsAsync(siigoSettings, x => x.CopyToEmail, storeId),
                CurrencyCode = siigoSettings.CurrencyCode,
                CurrencyCode_OverrideForStore = await _settingService.SettingExistsAsync(siigoSettings, x => x.CurrencyCode, storeId),
                ExchangeRate = siigoSettings.ExchangeRate,
                ExchangeRate_OverrideForStore = await _settingService.SettingExistsAsync(siigoSettings, x => x.ExchangeRate, storeId),
                IsEnabled = siigoSettings.IsEnabled,
                IsEnabled_OverrideForStore = await _settingService.SettingExistsAsync(siigoSettings, x => x.IsEnabled, storeId),
                TestMode = siigoSettings.TestMode,
                TestMode_OverrideForStore = await _settingService.SettingExistsAsync(siigoSettings, x => x.TestMode, storeId),
                LogEnabled = siigoSettings.LogEnabled,
                LogEnabled_OverrideForStore = await _settingService.SettingExistsAsync(siigoSettings, x => x.LogEnabled, storeId),
                RecentInvoicedOrders = await LoadRecentInvoicedOrdersAsync()
            };

            return View("~/Plugins/ElectronicInvoice.SIIGO/Views/Configure.cshtml", model);
        }

        [HttpPost]
        [CheckPermission(StandardPermission.Configuration.MANAGE_PLUGINS)]
        public async Task<IActionResult> Configure(ConfigurationModel model)
        {
            if (!ModelState.IsValid)
                return await Configure();

            var storeId = await _storeContext.GetActiveStoreScopeConfigurationAsync();
            var siigoSettings = await _settingService.LoadSettingAsync<SiigoSettings>(storeId);

            siigoSettings.ApiBaseUrl = model.ApiBaseUrl;
            siigoSettings.PartnerId = model.PartnerId;
            siigoSettings.Username = model.Username;
            siigoSettings.AccessKey = model.AccessKey;
            siigoSettings.DocumentId = model.DocumentId;
            siigoSettings.DefaultItemCode = model.DefaultItemCode;
            siigoSettings.SellerId = model.SellerId;
            siigoSettings.PaymentMethodId = model.PaymentMethodId;
            siigoSettings.TaxIdWithTax = model.TaxIdWithTax;
            siigoSettings.AccountGroup = model.AccountGroup;
            siigoSettings.SendByEmail = model.SendByEmail;
            siigoSettings.SendStamp = model.SendStamp;
            siigoSettings.CopyToEmail = model.CopyToEmail;
            siigoSettings.CurrencyCode = model.CurrencyCode;
            siigoSettings.ExchangeRate = model.ExchangeRate;
            siigoSettings.IsEnabled = model.IsEnabled;
            siigoSettings.TestMode = model.TestMode;
            siigoSettings.LogEnabled = model.LogEnabled;

            await _settingService.SaveSettingOverridablePerStoreAsync(siigoSettings, x => x.ApiBaseUrl, model.ApiBaseUrl_OverrideForStore, storeId, false);
            await _settingService.SaveSettingOverridablePerStoreAsync(siigoSettings, x => x.PartnerId, model.PartnerId_OverrideForStore, storeId, false);
            await _settingService.SaveSettingOverridablePerStoreAsync(siigoSettings, x => x.Username, model.Username_OverrideForStore, storeId, false);
            await _settingService.SaveSettingOverridablePerStoreAsync(siigoSettings, x => x.AccessKey, model.AccessKey_OverrideForStore, storeId, false);
            await _settingService.SaveSettingOverridablePerStoreAsync(siigoSettings, x => x.DocumentId, model.DocumentId_OverrideForStore, storeId, false);
            await _settingService.SaveSettingOverridablePerStoreAsync(siigoSettings, x => x.DefaultItemCode, model.DefaultItemCode_OverrideForStore, storeId, false);
            await _settingService.SaveSettingOverridablePerStoreAsync(siigoSettings, x => x.SellerId, model.SellerId_OverrideForStore, storeId, false);
            await _settingService.SaveSettingOverridablePerStoreAsync(siigoSettings, x => x.PaymentMethodId, model.PaymentMethodId_OverrideForStore, storeId, false);
            await _settingService.SaveSettingOverridablePerStoreAsync(siigoSettings, x => x.TaxIdWithTax, model.TaxIdWithTax_OverrideForStore, storeId, false);
            await _settingService.SaveSettingOverridablePerStoreAsync(siigoSettings, x => x.AccountGroup, model.AccountGroup_OverrideForStore, storeId, false);
            await _settingService.SaveSettingOverridablePerStoreAsync(siigoSettings, x => x.SendByEmail, model.SendByEmail_OverrideForStore, storeId, false);
            await _settingService.SaveSettingOverridablePerStoreAsync(siigoSettings, x => x.SendStamp, model.SendStamp_OverrideForStore, storeId, false);
            await _settingService.SaveSettingOverridablePerStoreAsync(siigoSettings, x => x.CopyToEmail, model.CopyToEmail_OverrideForStore, storeId, false);
            await _settingService.SaveSettingOverridablePerStoreAsync(siigoSettings, x => x.CurrencyCode, model.CurrencyCode_OverrideForStore, storeId, false);
            await _settingService.SaveSettingOverridablePerStoreAsync(siigoSettings, x => x.ExchangeRate, model.ExchangeRate_OverrideForStore, storeId, false);
            await _settingService.SaveSettingOverridablePerStoreAsync(siigoSettings, x => x.IsEnabled, model.IsEnabled_OverrideForStore, storeId, false);
            await _settingService.SaveSettingOverridablePerStoreAsync(siigoSettings, x => x.TestMode, model.TestMode_OverrideForStore, storeId, false);
            await _settingService.SaveSettingOverridablePerStoreAsync(siigoSettings, x => x.LogEnabled, model.LogEnabled_OverrideForStore, storeId, false);

            await _settingService.ClearCacheAsync();

            _notificationService.SuccessNotification(await _localizationService.GetResourceAsync("Admin.Plugins.Saved"));

            return await Configure();
        }

        [HttpPost]
        [CheckPermission(StandardPermission.Configuration.MANAGE_PLUGINS)]
        public async Task<IActionResult> TestConnection()
        {
            try
            {
                var isValid = _siigoInvoiceService.ValidateConfiguration();
                
                if (!isValid)
                {
                    _notificationService.ErrorNotification("Invalid configuration. Please verify connection parameters.");
                    return await Configure();
                }

                // Test authentication with SIIGO API
                var token = await _siigoAuthService.GetValidTokenAsync();
                
                if (!string.IsNullOrEmpty(token))
                {
                    _notificationService.SuccessNotification("Valid configuration. SIIGO authentication successful.");
                }
                else
                {
                    _notificationService.ErrorNotification("Authentication failed. Please verify your credentials.");
                }
            }
            catch (Exception ex)
            {
                _notificationService.ErrorNotification($"Error testing connection: {ex.Message}");
            }

            return await Configure();
        }

        private async Task<List<InvoicedOrderModel>> LoadRecentInvoicedOrdersAsync()
        {
            try
            {
                var invoicedOrders = new List<InvoicedOrderModel>();
                
                // Get recent orders (last 50 orders to check)
                var orders = await _orderService.SearchOrdersAsync(pageSize: 50);
                
                foreach (var order in orders)
                {
                    // Check if order has SIIGO invoice
                    if (await order.HasSiigoInvoiceAsync(_genericAttributeService))
                    {
                        var customer = await _customerService.GetCustomerByIdAsync(order.CustomerId);
                        var invoiceInfo = await _siigoInvoiceService.GetOrderInvoiceInfoAsync(order);
                        
                        invoicedOrders.Add(new InvoicedOrderModel
                        {
                            OrderId = order.Id,
                            OrderGuid = order.OrderGuid.ToString(),
                            OrderDate = order.CreatedOnUtc,
                            CustomerEmail = customer?.Email ?? "N/A",
                            OrderTotal = order.OrderTotal,
                            SiigoInvoiceId = invoiceInfo.invoiceId ?? "N/A",
                            SiigoInvoiceNumber = invoiceInfo.invoiceNumber,
                            SiigoInvoiceDate = invoiceInfo.invoiceDate,
                            SiigoInvoiceStatus = invoiceInfo.status ?? "Unknown"
                        });
                    }
                }
                
                // Return most recent first (limit to 20 for display)
                return invoicedOrders.OrderByDescending(x => x.OrderDate).Take(20).ToList();
            }
            catch (Exception)
            {
                // Return empty list on error
                return new List<InvoicedOrderModel>();
            }
        }
    }
}
