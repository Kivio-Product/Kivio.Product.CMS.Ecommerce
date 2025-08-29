using Microsoft.AspNetCore.Mvc;
using Nop.Core;
using Nop.Services.Configuration;
using Nop.Services.Localization;
using Nop.Services.Messages;
using Nop.Services.Security;
using Nop.Web.Framework;
using Nop.Web.Framework.Controllers;
using Nop.Web.Framework.Mvc.Filters;
using Plugin.ElectronicInvoice.SIIGO.Models;
using Plugin.ElectronicInvoice.SIIGO.Services;

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

        public SiigoController(
            IStoreContext storeContext,
            ISettingService settingService,
            ILocalizationService localizationService,
            INotificationService notificationService,
            IPermissionService permissionService,
            ISiigoInvoiceService siigoInvoiceService)
        {
            _storeContext = storeContext;
            _settingService = settingService;
            _localizationService = localizationService;
            _notificationService = notificationService;
            _permissionService = permissionService;
            _siigoInvoiceService = siigoInvoiceService;
        }

        [CheckPermission(StandardPermission.Configuration.MANAGE_PLUGINS)]
        public async Task<IActionResult> Configure()
        {
            var storeId = await _storeContext.GetActiveStoreScopeConfigurationAsync();
            var siigoSettings = await _settingService.LoadSettingAsync<SiigoSettings>(storeId);

            var model = new ConfigurationModel
            {
                ApiBaseUrl = siigoSettings.ApiBaseUrl,
                PartnerId = siigoSettings.PartnerId,
                BearerToken = siigoSettings.BearerToken,
                DocumentId = siigoSettings.DocumentId,
                DefaultItemCode = siigoSettings.DefaultItemCode,
                SellerId = siigoSettings.SellerId,
                PaymentMethodId = siigoSettings.PaymentMethodId,
                TaxIdWithTax = siigoSettings.TaxIdWithTax,
                TaxIdWithoutTax = siigoSettings.TaxIdWithoutTax,
                SendByEmail = siigoSettings.SendByEmail,
                SendStamp = siigoSettings.SendStamp,
                IsEnabled = siigoSettings.IsEnabled,
                TestMode = siigoSettings.TestMode,
                LogEnabled = siigoSettings.LogEnabled
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
            siigoSettings.BearerToken = model.BearerToken;
            siigoSettings.DocumentId = model.DocumentId;
            siigoSettings.DefaultItemCode = model.DefaultItemCode;
            siigoSettings.SellerId = model.SellerId;
            siigoSettings.PaymentMethodId = model.PaymentMethodId;
            siigoSettings.TaxIdWithTax = model.TaxIdWithTax;
            siigoSettings.TaxIdWithoutTax = model.TaxIdWithoutTax;
            siigoSettings.SendByEmail = model.SendByEmail;
            siigoSettings.SendStamp = model.SendStamp;
            siigoSettings.IsEnabled = model.IsEnabled;
            siigoSettings.TestMode = model.TestMode;
            siigoSettings.LogEnabled = model.LogEnabled;

            await _settingService.SaveSettingAsync(siigoSettings, storeId);
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
                
                if (isValid)
                {
                    _notificationService.SuccessNotification("Valid configuration. SIIGO connection successful.");
                }
                else
                {
                    _notificationService.ErrorNotification("Invalid configuration. Please verify connection parameters.");
                }
            }
            catch (Exception ex)
            {
                _notificationService.ErrorNotification($"Error testing connection: {ex.Message}");
            }

            return await Configure();
        }
    }
}
