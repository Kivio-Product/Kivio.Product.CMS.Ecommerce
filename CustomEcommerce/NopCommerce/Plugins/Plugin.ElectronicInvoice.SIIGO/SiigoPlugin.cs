using Microsoft.AspNetCore.Mvc;
using Nop.Core;
using Nop.Services.Common;
using Nop.Services.Configuration;
using Nop.Services.Localization;
using Nop.Services.Plugins;
using Nop.Services.Security;
using Nop.Web.Framework.Menu;
using Plugin.ElectronicInvoice.SIIGO.Services;

namespace Plugin.ElectronicInvoice.SIIGO
{
    public class SiigoPlugin : BasePlugin, IMiscPlugin
    {
        private readonly IWebHelper _webHelper;
        private readonly ISettingService _settingService;
        private readonly ILocalizationService _localizationService;

        public SiigoPlugin(
            IWebHelper webHelper,
            ISettingService settingService,
            ILocalizationService localizationService)
        {
            _webHelper = webHelper;
            _settingService = settingService;
            _localizationService = localizationService;
        }

        public override string GetConfigurationPageUrl()
        {
            return $"{_webHelper.GetStoreLocation()}Admin/Siigo/Configure";
        }

        public override async Task InstallAsync()
        {
            var settings = new SiigoSettings
            {
                ApiBaseUrl = "https://api.siigo.com",
                PartnerId = "kivio",
                DocumentId = 27860,
                DefaultItemCode = "1",
                SellerId = 876,
                PaymentMethodId = 10948,
                AccountGroup = 1528,
                SendByEmail = true,
                SendStamp = false,
                CopyToEmail = "",
                CurrencyCode = "USD",
                ExchangeRate = "4000",
                IsEnabled = false,
                TestMode = true,
                LogEnabled = true,
                IdentificationAddressAttributeId = 1
            };

            await _settingService.SaveSettingAsync(settings);
            
            var mappingSettings = new SiigoTaxCategoryMappingSettings();
            await _settingService.SaveSettingAsync(mappingSettings);
            
            await InstallLocalizationResourcesAsync();
            await base.InstallAsync();
        }

        public override async Task UninstallAsync()
        {
            await _settingService.DeleteSettingAsync<SiigoSettings>();
            await _settingService.DeleteSettingAsync<SiigoTaxCategoryMappingSettings>();
            await DeleteLocalizationResourcesAsync();
            await base.UninstallAsync();
        }

        private async Task InstallLocalizationResourcesAsync()
        {
            await _localizationService.AddOrUpdateLocaleResourceAsync(new Dictionary<string, string>
            {
                ["Plugins.ElectronicInvoice.SIIGO.Fields.ApiBaseUrl"] = "API Base URL",
                ["Plugins.ElectronicInvoice.SIIGO.Fields.ApiBaseUrl.Hint"] = "SIIGO API base URL (e.g., https://api.siigo.com)",
                ["Plugins.ElectronicInvoice.SIIGO.Fields.PartnerId"] = "Partner ID",
                ["Plugins.ElectronicInvoice.SIIGO.Fields.PartnerId.Hint"] = "Partner ID for SIIGO authentication",
                ["Plugins.ElectronicInvoice.SIIGO.Fields.Username"] = "Username",
                ["Plugins.ElectronicInvoice.SIIGO.Fields.Username.Hint"] = "SIIGO username for authentication",
                ["Plugins.ElectronicInvoice.SIIGO.Fields.AccessKey"] = "Access Key",
                ["Plugins.ElectronicInvoice.SIIGO.Fields.AccessKey.Hint"] = "SIIGO access key for authentication (generates bearer tokens automatically)",
                ["Plugins.ElectronicInvoice.SIIGO.Fields.DocumentId"] = "Document ID",
                ["Plugins.ElectronicInvoice.SIIGO.Fields.DocumentId.Hint"] = "Invoice document type ID in SIIGO",
                ["Plugins.ElectronicInvoice.SIIGO.Fields.DefaultItemCode"] = "Default Item Code",
                ["Plugins.ElectronicInvoice.SIIGO.Fields.DefaultItemCode.Hint"] = "Default product/service code",
                ["Plugins.ElectronicInvoice.SIIGO.Fields.SellerId"] = "Seller ID",
                ["Plugins.ElectronicInvoice.SIIGO.Fields.SellerId.Hint"] = "Seller ID in SIIGO",
                ["Plugins.ElectronicInvoice.SIIGO.Fields.PaymentMethodId"] = "Payment Method ID",
                ["Plugins.ElectronicInvoice.SIIGO.Fields.PaymentMethodId.Hint"] = "Payment method ID in SIIGO",
                ["Plugins.ElectronicInvoice.SIIGO.Fields.AccountGroup"] = "Account Group",
                ["Plugins.ElectronicInvoice.SIIGO.Fields.AccountGroup.Hint"] = "Account group ID for products created in SIIGO",
                ["Plugins.ElectronicInvoice.SIIGO.Fields.TaxIdWithoutTax"] = "Tax ID (Without Tax)",
                ["Plugins.ElectronicInvoice.SIIGO.Fields.TaxIdWithoutTax.Hint"] = "Tax ID when tax does not apply",
                ["Plugins.ElectronicInvoice.SIIGO.Fields.SendByEmail"] = "Send by Email",
                ["Plugins.ElectronicInvoice.SIIGO.Fields.SendByEmail.Hint"] = "Send invoice by email automatically",
                ["Plugins.ElectronicInvoice.SIIGO.Fields.SendStamp"] = "Send Stamp",
                ["Plugins.ElectronicInvoice.SIIGO.Fields.SendStamp.Hint"] = "Include timestamp in invoice",
                ["Plugins.ElectronicInvoice.SIIGO.Fields.CopyToEmail"] = "Copy To Email",
                ["Plugins.ElectronicInvoice.SIIGO.Fields.CopyToEmail.Hint"] = "Email address to receive a copy of all electronic invoices",
                ["Plugins.ElectronicInvoice.SIIGO.Fields.CurrencyCode"] = "Currency Code",
                ["Plugins.ElectronicInvoice.SIIGO.Fields.CurrencyCode.Hint"] = "Currency code for invoices (e.g., COP, USD, EUR)",
                ["Plugins.ElectronicInvoice.SIIGO.Fields.ExchangeRate"] = "Exchange Rate",
                ["Plugins.ElectronicInvoice.SIIGO.Fields.ExchangeRate.Hint"] = "Exchange rate for currency conversion (use '1' for local currency)",
                ["Plugins.ElectronicInvoice.SIIGO.Fields.IsEnabled"] = "Enabled",
                ["Plugins.ElectronicInvoice.SIIGO.Fields.IsEnabled.Hint"] = "Enable automatic electronic invoicing",
                ["Plugins.ElectronicInvoice.SIIGO.Fields.TestMode"] = "Test Mode",
                ["Plugins.ElectronicInvoice.SIIGO.Fields.TestMode.Hint"] = "Run in test mode",
                ["Plugins.ElectronicInvoice.SIIGO.Fields.LogEnabled"] = "Logging Enabled",
                ["Plugins.ElectronicInvoice.SIIGO.Fields.LogEnabled.Hint"] = "Enable logging for debugging",
                ["Plugins.ElectronicInvoice.SIIGO.Fields.IdentificationAddressAttributeId"] = "Identification AddressAttribute ID",
                ["Plugins.ElectronicInvoice.SIIGO.Fields.IdentificationAddressAttributeId.Hint"] = "ID of the AddressAttribute that contains the customer identification document (cedula/NIT). Default is 1.",
                
                // Tax Category Mapping Resources
                ["Plugins.ElectronicInvoice.SIIGO.Fields.TaxCategoryId"] = "Tax Category",
                ["Plugins.ElectronicInvoice.SIIGO.Fields.TaxCategoryName"] = "Tax Category Name",
                ["Plugins.ElectronicInvoice.SIIGO.Fields.SiigoTaxCode"] = "SIIGO Tax Code",
                ["Plugins.ElectronicInvoice.SIIGO.Fields.SiigoTaxCode.Hint"] = "Tax code ID from SIIGO system"
            });

            await base.InstallAsync();
        }

        private async Task DeleteLocalizationResourcesAsync()
        {
            await _settingService.DeleteSettingAsync<SiigoSettings>();
            await _localizationService.DeleteLocaleResourcesAsync("Plugins.ElectronicInvoice.SIIGO");
            await base.UninstallAsync();
        }
    }
}
