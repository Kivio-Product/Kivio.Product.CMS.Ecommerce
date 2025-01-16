using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Nop.Core;
using Nop.Plugin.Payments.Sermepa.Models;
using Nop.Services.Configuration;
using Nop.Services.Localization;
using Nop.Services.Messages;
using Nop.Services.Security;
using Nop.Web.Framework;
using Nop.Web.Framework.Controllers;
using Nop.Web.Framework.Mvc.Filters;

namespace Nop.Plugin.Payments.Sermepa.Controllers
{
    [AuthorizeAdmin]
    [Area(AreaNames.ADMIN)]
    public class PaymentSermepaController : BasePaymentController
    {
        private readonly ISettingService _settingService;
        private readonly IPermissionService _permissionService;
        private readonly IStoreContext _storeContext;
        private readonly ILocalizationService _localizationService;
        private readonly INotificationService _notificationService;

        public PaymentSermepaController(ISettingService settingService,
            IPermissionService permissionService,
            IStoreContext storeContext,
            ILocalizationService localizationService,
            INotificationService notificationService)
        {
            _settingService = settingService;
            _permissionService = permissionService;
            _storeContext = storeContext;
            _localizationService = localizationService;
            _notificationService = notificationService;
        }

        [CheckPermission(StandardPermission.Configuration.MANAGE_PAYMENT_METHODS)]
        public async Task<IActionResult> Configure()
        {
            //load settings for a chosen store scope
            var storeScope = await _storeContext.GetActiveStoreScopeConfigurationAsync();
            var sermepaPaymentSettings = await _settingService.LoadSettingAsync<SermepaPaymentSettings>(storeScope);

            var model = new ConfigurationModel
            {
                NombreComercio = sermepaPaymentSettings.NombreComercio,
                Titular = sermepaPaymentSettings.Titular,
                Producto = sermepaPaymentSettings.Producto,
                FUC = sermepaPaymentSettings.FUC,
                Terminal = sermepaPaymentSettings.Terminal,
                Moneda = sermepaPaymentSettings.Moneda,
                ClaveReal = sermepaPaymentSettings.ClaveReal,
                ClavePruebas = sermepaPaymentSettings.ClavePruebas,
                Pruebas = sermepaPaymentSettings.Pruebas,
                AdditionalFee = sermepaPaymentSettings.AdditionalFee,
                AdditionalFeePercentage = sermepaPaymentSettings.AdditionalFeePercentage,
                ActiveStoreScopeConfiguration = storeScope
            };

            if (storeScope <= 0)
                return View("~/Plugins/Payments.Sermepa/Views/Configure.cshtml", model);

            model.NombreComercio_OverrideForStore = await _settingService.SettingExistsAsync(sermepaPaymentSettings, x => x.NombreComercio, storeScope);
            model.Titular_OverrideForStore = await _settingService.SettingExistsAsync(sermepaPaymentSettings, x => x.Titular, storeScope);
            model.Producto_OverrideForStore = await _settingService.SettingExistsAsync(sermepaPaymentSettings, x => x.Producto, storeScope);
            model.FUC_OverrideForStore = await _settingService.SettingExistsAsync(sermepaPaymentSettings, x => x.FUC, storeScope);
            model.Terminal_OverrideForStore = await _settingService.SettingExistsAsync(sermepaPaymentSettings, x => x.Terminal, storeScope);
            model.Moneda_OverrideForStore = await _settingService.SettingExistsAsync(sermepaPaymentSettings, x => x.Moneda, storeScope);
            model.ClavePruebas_OverrideForStore = await _settingService.SettingExistsAsync(sermepaPaymentSettings, x => x.ClavePruebas, storeScope);
            model.Pruebas_OverrideForStore = await _settingService.SettingExistsAsync(sermepaPaymentSettings, x => x.Pruebas, storeScope);
            model.AdditionalFee_OverrideForStore = await _settingService.SettingExistsAsync(sermepaPaymentSettings, x => x.AdditionalFee, storeScope);
            model.AdditionalFeePercentage_OverrideForStore = await _settingService.SettingExistsAsync(sermepaPaymentSettings, x => x.AdditionalFeePercentage, storeScope);

            return View("~/Plugins/Payments.Sermepa/Views/Configure.cshtml", model);
        }

        [HttpPost]
        [CheckPermission(StandardPermission.Configuration.MANAGE_PAYMENT_METHODS)]
        public async Task<IActionResult> Configure(ConfigurationModel model)
        {

            if (!ModelState.IsValid)
                return await Configure();

            //load settings for a chosen store scope
            var storeScope = await _storeContext.GetActiveStoreScopeConfigurationAsync();
            var sermepaPaymentSettings = await _settingService.LoadSettingAsync<SermepaPaymentSettings>(storeScope);

            //save settings
            sermepaPaymentSettings.NombreComercio = model.NombreComercio;
            sermepaPaymentSettings.Titular = model.Titular;
            sermepaPaymentSettings.Producto = model.Producto;
            sermepaPaymentSettings.FUC = model.FUC;
            sermepaPaymentSettings.Terminal = model.Terminal;
            sermepaPaymentSettings.Moneda = model.Moneda;
            sermepaPaymentSettings.ClaveReal = model.ClaveReal;
            sermepaPaymentSettings.ClavePruebas = model.ClavePruebas;
            sermepaPaymentSettings.Pruebas = model.Pruebas;
            sermepaPaymentSettings.AdditionalFee = model.AdditionalFee;
            sermepaPaymentSettings.AdditionalFeePercentage = model.AdditionalFeePercentage;

            /* We do not clear cache after each setting update.
             * This behavior can increase performance because cached settings will not be cleared 
             * and loaded from database after each update */
            await _settingService.SaveSettingOverridablePerStoreAsync(sermepaPaymentSettings, x => x.NombreComercio, model.NombreComercio_OverrideForStore, storeScope, false);
            await _settingService.SaveSettingOverridablePerStoreAsync(sermepaPaymentSettings, x => x.Titular, model.Titular_OverrideForStore, storeScope, false);
            await _settingService.SaveSettingOverridablePerStoreAsync(sermepaPaymentSettings, x => x.Producto, model.Producto_OverrideForStore, storeScope, false);
            await _settingService.SaveSettingOverridablePerStoreAsync(sermepaPaymentSettings, x => x.FUC, model.FUC_OverrideForStore, storeScope, false);
            await _settingService.SaveSettingOverridablePerStoreAsync(sermepaPaymentSettings, x => x.Terminal, model.Terminal_OverrideForStore, storeScope, false);
            await _settingService.SaveSettingOverridablePerStoreAsync(sermepaPaymentSettings, x => x.Moneda, model.Moneda_OverrideForStore, storeScope, false);
            await _settingService.SaveSettingOverridablePerStoreAsync(sermepaPaymentSettings, x => x.ClaveReal, model.ClaveReal_OverrideForStore, storeScope, false);
            await _settingService.SaveSettingOverridablePerStoreAsync(sermepaPaymentSettings, x => x.ClavePruebas, model.ClavePruebas_OverrideForStore, storeScope, false);
            await _settingService.SaveSettingOverridablePerStoreAsync(sermepaPaymentSettings, x => x.Pruebas, model.Pruebas_OverrideForStore, storeScope, false);
            await _settingService.SaveSettingOverridablePerStoreAsync(sermepaPaymentSettings, x => x.AdditionalFee, model.AdditionalFee_OverrideForStore, storeScope, false);
            await _settingService.SaveSettingOverridablePerStoreAsync(sermepaPaymentSettings, x => x.AdditionalFeePercentage, model.AdditionalFeePercentage_OverrideForStore, storeScope, false);

            //now clear settings cache
            await _settingService.ClearCacheAsync();

            _notificationService.SuccessNotification(await _localizationService.GetResourceAsync("Admin.Plugins.Saved"));

           return await Configure();
        }
    }
}