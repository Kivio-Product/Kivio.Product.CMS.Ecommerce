using Microsoft.AspNetCore.Mvc;
using Nop.Core;
using Nop.Services.Configuration;
using Nop.Services.Localization;
using Nop.Services.Messages;
using Nop.Services.Security;
using Nop.Services.Orders;
using Nop.Services.Customers;
using Nop.Services.Common;
using Nop.Services.Tax;
using Nop.Services.Payments;
using Nop.Web.Framework;
using Nop.Web.Framework.Controllers;
using Nop.Web.Framework.Mvc.Filters;
using Plugin.ElectronicInvoice.SIIGO.Models;
using Plugin.ElectronicInvoice.SIIGO.Services;
using Plugin.ElectronicInvoice.SIIGO.Data;
using Microsoft.AspNetCore.Mvc.Rendering;

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
        private readonly ITaxCategoryService _taxCategoryService;
        private readonly IPaymentPluginManager _paymentPluginManager;

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
            IGenericAttributeService genericAttributeService,
            ITaxCategoryService taxCategoryService,
            IPaymentPluginManager paymentPluginManager)
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
            _taxCategoryService = taxCategoryService;
            _paymentPluginManager = paymentPluginManager;
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
                IdentificationAddressAttributeId = siigoSettings.IdentificationAddressAttributeId,
                IdentificationAddressAttributeId_OverrideForStore = await _settingService.SettingExistsAsync(siigoSettings, x => x.IdentificationAddressAttributeId, storeId),
                RecentInvoicedOrders = await LoadRecentInvoicedOrdersAsync(),
                TaxCategoryMappings = await LoadTaxCategoryMappingsAsync(storeId),
                PaymentMethodMappings = await LoadPaymentMethodMappingsAsync(storeId)
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
            siigoSettings.AccountGroup = model.AccountGroup;
            siigoSettings.SendByEmail = model.SendByEmail;
            siigoSettings.SendStamp = model.SendStamp;
            siigoSettings.CopyToEmail = model.CopyToEmail;
            siigoSettings.CurrencyCode = model.CurrencyCode;
            siigoSettings.ExchangeRate = model.ExchangeRate;
            siigoSettings.IsEnabled = model.IsEnabled;
            siigoSettings.TestMode = model.TestMode;
            siigoSettings.LogEnabled = model.LogEnabled;
            siigoSettings.IdentificationAddressAttributeId = model.IdentificationAddressAttributeId;

            await _settingService.SaveSettingOverridablePerStoreAsync(siigoSettings, x => x.ApiBaseUrl, model.ApiBaseUrl_OverrideForStore, storeId, false);
            await _settingService.SaveSettingOverridablePerStoreAsync(siigoSettings, x => x.PartnerId, model.PartnerId_OverrideForStore, storeId, false);
            await _settingService.SaveSettingOverridablePerStoreAsync(siigoSettings, x => x.Username, model.Username_OverrideForStore, storeId, false);
            await _settingService.SaveSettingOverridablePerStoreAsync(siigoSettings, x => x.AccessKey, model.AccessKey_OverrideForStore, storeId, false);
            await _settingService.SaveSettingOverridablePerStoreAsync(siigoSettings, x => x.DocumentId, model.DocumentId_OverrideForStore, storeId, false);
            await _settingService.SaveSettingOverridablePerStoreAsync(siigoSettings, x => x.DefaultItemCode, model.DefaultItemCode_OverrideForStore, storeId, false);
            await _settingService.SaveSettingOverridablePerStoreAsync(siigoSettings, x => x.SellerId, model.SellerId_OverrideForStore, storeId, false);
            await _settingService.SaveSettingOverridablePerStoreAsync(siigoSettings, x => x.PaymentMethodId, model.PaymentMethodId_OverrideForStore, storeId, false);
            await _settingService.SaveSettingOverridablePerStoreAsync(siigoSettings, x => x.AccountGroup, model.AccountGroup_OverrideForStore, storeId, false);
            await _settingService.SaveSettingOverridablePerStoreAsync(siigoSettings, x => x.SendByEmail, model.SendByEmail_OverrideForStore, storeId, false);
            await _settingService.SaveSettingOverridablePerStoreAsync(siigoSettings, x => x.SendStamp, model.SendStamp_OverrideForStore, storeId, false);
            await _settingService.SaveSettingOverridablePerStoreAsync(siigoSettings, x => x.CopyToEmail, model.CopyToEmail_OverrideForStore, storeId, false);
            await _settingService.SaveSettingOverridablePerStoreAsync(siigoSettings, x => x.CurrencyCode, model.CurrencyCode_OverrideForStore, storeId, false);
            await _settingService.SaveSettingOverridablePerStoreAsync(siigoSettings, x => x.ExchangeRate, model.ExchangeRate_OverrideForStore, storeId, false);
            await _settingService.SaveSettingOverridablePerStoreAsync(siigoSettings, x => x.IsEnabled, model.IsEnabled_OverrideForStore, storeId, false);
            await _settingService.SaveSettingOverridablePerStoreAsync(siigoSettings, x => x.TestMode, model.TestMode_OverrideForStore, storeId, false);
            await _settingService.SaveSettingOverridablePerStoreAsync(siigoSettings, x => x.LogEnabled, model.LogEnabled_OverrideForStore, storeId, false);
            await _settingService.SaveSettingOverridablePerStoreAsync(siigoSettings, x => x.IdentificationAddressAttributeId, model.IdentificationAddressAttributeId_OverrideForStore, storeId, false);

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

        [HttpPost]
        [CheckPermission(StandardPermission.Configuration.MANAGE_PLUGINS)]
        public async Task<IActionResult> AddTaxCategoryMapping(ConfigurationModel model)
        {
            try
            {
                if (model.TaxCategoryMappings.NewTaxCategoryId <= 0)
                {
                    _notificationService.ErrorNotification("Please select a valid tax category.");
                    return await Configure();
                }

                if (model.TaxCategoryMappings.NewSiigoTaxCode <= 0)
                {
                    _notificationService.ErrorNotification("Please enter a valid SIIGO tax code.");
                    return await Configure();
                }

                var storeId = await _storeContext.GetActiveStoreScopeConfigurationAsync();
                var mappingSettings = await _settingService.LoadSettingAsync<SiigoTaxCategoryMappingSettings>(storeId);

                // Log current state for debugging
                var currentCount = mappingSettings.TaxCategoryMappings.Count;
                
                // Check if mapping already exists
                var existingMapping = mappingSettings.TaxCategoryMappings
                    .FirstOrDefault(m => m.TaxCategoryId == model.TaxCategoryMappings.NewTaxCategoryId);

                // Add or update mapping using the helper method
                mappingSettings.AddOrUpdateMapping(
                    model.TaxCategoryMappings.NewTaxCategoryId,
                    model.TaxCategoryMappings.NewSiigoTaxCode,
                    model.TaxCategoryMappings.NewIsEnabled);

                // Verify the change was applied
                var newCount = mappingSettings.TaxCategoryMappings.Count;
                var jsonData = mappingSettings.TaxCategoryMappingsJson;

                await _settingService.SaveSettingAsync(mappingSettings, storeId);

                if (existingMapping != null)
                {
                    _notificationService.SuccessNotification($"Tax category mapping updated successfully. (Total mappings: {newCount})");
                }
                else
                {
                    _notificationService.SuccessNotification($"Tax category mapping added successfully. (Was {currentCount}, now {newCount})");
                }
            }
            catch (Exception ex)
            {
                _notificationService.ErrorNotification($"Error saving tax category mapping: {ex.Message}");
            }

            return await Configure();
        }

        [HttpPost]
        [CheckPermission(StandardPermission.Configuration.MANAGE_PLUGINS)]
        public async Task<IActionResult> DeleteTaxCategoryMapping(int deleteTaxCategoryMapping)
        {
            try
            {
                var storeId = await _storeContext.GetActiveStoreScopeConfigurationAsync();
                var mappingSettings = await _settingService.LoadSettingAsync<SiigoTaxCategoryMappingSettings>(storeId);

                // Remove mapping using the helper method
                var removed = mappingSettings.RemoveMapping(deleteTaxCategoryMapping);

                if (removed)
                {
                    await _settingService.SaveSettingAsync(mappingSettings, storeId);
                    _notificationService.SuccessNotification("Tax category mapping deleted successfully.");
                }
                else
                {
                    _notificationService.ErrorNotification("Tax category mapping not found.");
                }
            }
            catch (Exception ex)
            {
                _notificationService.ErrorNotification($"Error deleting tax category mapping: {ex.Message}");
            }

            return await Configure();
        }

        private async Task<TaxCategoryMappingConfigurationModel> LoadTaxCategoryMappingsAsync(int storeId)
        {
            var model = new TaxCategoryMappingConfigurationModel();
            
            try
            {
                // Load tax category mappings
                var mappingSettings = await _settingService.LoadSettingAsync<SiigoTaxCategoryMappingSettings>(storeId);
                
                // Load all tax categories
                var allTaxCategories = await _taxCategoryService.GetAllTaxCategoriesAsync();
                
                // Create mapping models
                foreach (var mapping in mappingSettings.TaxCategoryMappings)
                {
                    var taxCategory = allTaxCategories.FirstOrDefault(tc => tc.Id == mapping.TaxCategoryId);
                    if (taxCategory != null)
                    {
                        model.TaxCategoryMappings.Add(new SiigoTaxCategoryMappingModel
                        {
                            Id = mapping.TaxCategoryId, // Using TaxCategoryId as the model ID
                            TaxCategoryId = mapping.TaxCategoryId,
                            TaxCategoryName = taxCategory.Name,
                            SiigoTaxCode = mapping.SiigoTaxCode,
                            IsEnabled = mapping.IsEnabled
                        });
                    }
                }

                // Populate available tax categories (those not yet mapped)
                var mappedTaxCategoryIds = mappingSettings.TaxCategoryMappings.Select(m => m.TaxCategoryId).ToList();
                var unmappedTaxCategories = allTaxCategories.Where(tc => !mappedTaxCategoryIds.Contains(tc.Id));
                
                model.AvailableTaxCategories = unmappedTaxCategories
                    .Select(tc => new SelectListItem
                    {
                        Value = tc.Id.ToString(),
                        Text = tc.Name
                    }).ToList();

                // Add default option
                model.AvailableTaxCategories.Insert(0, new SelectListItem
                {
                    Value = "0",
                    Text = "Select a tax category..."
                });
            }
            catch (Exception)
            {
                // Log error but don't throw - return empty model
                await _localizationService.GetResourceAsync("Admin.Common.Alert.Save.Error");
            }

            return model;
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

        private async Task<PaymentMethodMappingConfigurationModel> LoadPaymentMethodMappingsAsync(int storeId)
        {
            var model = new PaymentMethodMappingConfigurationModel();
            
            try
            {
                // Load payment method mappings
                var mappingSettings = await _settingService.LoadSettingAsync<SiigoPaymentMethodMappingSettings>(storeId);
                
                // Load all active payment methods
                var allPaymentMethods = await _paymentPluginManager.LoadActivePluginsAsync();
                
                // Create mapping models
                foreach (var mapping in mappingSettings.PaymentMethodMappings)
                {
                    var paymentMethod = allPaymentMethods.FirstOrDefault(pm => pm.PluginDescriptor.SystemName.Equals(mapping.PaymentMethodSystemName, StringComparison.OrdinalIgnoreCase));
                    var friendlyName = paymentMethod?.PluginDescriptor.FriendlyName ?? mapping.PaymentMethodSystemName;
                    
                    model.PaymentMethodMappings.Add(new SiigoPaymentMethodMappingModel
                    {
                        Id = model.PaymentMethodMappings.Count + 1, // Simple incrementing ID for the model
                        PaymentMethodSystemName = mapping.PaymentMethodSystemName,
                        PaymentMethodFriendlyName = friendlyName,
                        SiigoPaymentMethodCode = mapping.SiigoPaymentMethodCode,
                        IsEnabled = mapping.IsEnabled
                    });
                }

                // Populate available payment methods (those not yet mapped)
                var mappedPaymentMethodSystemNames = mappingSettings.PaymentMethodMappings.Select(m => m.PaymentMethodSystemName).ToList();
                var unmappedPaymentMethods = allPaymentMethods.Where(pm => !mappedPaymentMethodSystemNames.Contains(pm.PluginDescriptor.SystemName, StringComparer.OrdinalIgnoreCase));
                
                model.AvailablePaymentMethods = unmappedPaymentMethods
                    .Select(pm => new SelectListItem
                    {
                        Value = pm.PluginDescriptor.SystemName,
                        Text = pm.PluginDescriptor.FriendlyName
                    }).ToList();

                // Add default option
                model.AvailablePaymentMethods.Insert(0, new SelectListItem
                {
                    Value = "",
                    Text = "Select a payment method..."
                });
            }
            catch (Exception)
            {
                // Log error but don't throw - return empty model
                await _localizationService.GetResourceAsync("Admin.Common.Alert.Save.Error");
            }

            return model;
        }

        [HttpPost]
        [CheckPermission(StandardPermission.Configuration.MANAGE_PLUGINS)]
        public async Task<IActionResult> AddPaymentMethodMapping(ConfigurationModel model)
        {
            try
            {
                if (string.IsNullOrEmpty(model.PaymentMethodMappings.NewPaymentMethodSystemName))
                {
                    _notificationService.ErrorNotification("Please select a valid payment method.");
                    return await Configure();
                }

                if (model.PaymentMethodMappings.NewSiigoPaymentMethodCode <= 0)
                {
                    _notificationService.ErrorNotification("Please enter a valid SIIGO payment method code.");
                    return await Configure();
                }

                var storeId = await _storeContext.GetActiveStoreScopeConfigurationAsync();
                var mappingSettings = await _settingService.LoadSettingAsync<SiigoPaymentMethodMappingSettings>(storeId);

                // Log current state for debugging
                var currentCount = mappingSettings.PaymentMethodMappings.Count;
                
                // Check if mapping already exists
                var existingMapping = mappingSettings.PaymentMethodMappings
                    .FirstOrDefault(m => m.PaymentMethodSystemName.Equals(model.PaymentMethodMappings.NewPaymentMethodSystemName, StringComparison.OrdinalIgnoreCase));

                // Add or update mapping using the helper method
                mappingSettings.AddOrUpdateMapping(
                    model.PaymentMethodMappings.NewPaymentMethodSystemName,
                    model.PaymentMethodMappings.NewSiigoPaymentMethodCode,
                    model.PaymentMethodMappings.NewIsEnabled);

                // Verify the change was applied
                var newCount = mappingSettings.PaymentMethodMappings.Count;

                await _settingService.SaveSettingAsync(mappingSettings, storeId);

                if (existingMapping != null)
                {
                    _notificationService.SuccessNotification($"Payment method mapping updated successfully. (Total mappings: {newCount})");
                }
                else
                {
                    _notificationService.SuccessNotification($"Payment method mapping added successfully. (Was {currentCount}, now {newCount})");
                }
            }
            catch (Exception ex)
            {
                _notificationService.ErrorNotification($"Error saving payment method mapping: {ex.Message}");
            }

            return await Configure();
        }

        [HttpPost]
        [CheckPermission(StandardPermission.Configuration.MANAGE_PLUGINS)]
        public async Task<IActionResult> DeletePaymentMethodMapping(string deletePaymentMethodMapping)
        {
            try
            {
                var storeId = await _storeContext.GetActiveStoreScopeConfigurationAsync();
                var mappingSettings = await _settingService.LoadSettingAsync<SiigoPaymentMethodMappingSettings>(storeId);

                // Remove mapping using the helper method
                var removed = mappingSettings.RemoveMapping(deletePaymentMethodMapping);

                if (removed)
                {
                    await _settingService.SaveSettingAsync(mappingSettings, storeId);
                    _notificationService.SuccessNotification("Payment method mapping deleted successfully.");
                }
                else
                {
                    _notificationService.ErrorNotification("Payment method mapping not found.");
                }
            }
            catch (Exception ex)
            {
                _notificationService.ErrorNotification($"Error deleting payment method mapping: {ex.Message}");
            }

            return await Configure();
        }
    }
}
