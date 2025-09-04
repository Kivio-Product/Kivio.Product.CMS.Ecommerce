using Nop.Web.Framework.Models;
using Nop.Web.Framework.Mvc.ModelBinding;

namespace Plugin.ElectronicInvoice.SIIGO.Models
{
    public record ConfigurationModel : BaseNopModel
    {
        public ConfigurationModel()
        {
            RecentInvoicedOrders = new List<InvoicedOrderModel>();
            TaxCategoryMappings = new TaxCategoryMappingConfigurationModel();
        }

        public int ActiveStoreScopeConfiguration { get; set; }

        [NopResourceDisplayName("Plugins.ElectronicInvoice.SIIGO.Fields.IsEnabled")]
        public bool IsEnabled { get; set; }
        public bool IsEnabled_OverrideForStore { get; set; }

        [NopResourceDisplayName("Plugins.ElectronicInvoice.SIIGO.Fields.TestMode")]
        public bool TestMode { get; set; }
        public bool TestMode_OverrideForStore { get; set; }

        [NopResourceDisplayName("Plugins.ElectronicInvoice.SIIGO.Fields.ApiBaseUrl")]
        public string ApiBaseUrl { get; set; }
        public bool ApiBaseUrl_OverrideForStore { get; set; }

        [NopResourceDisplayName("Plugins.ElectronicInvoice.SIIGO.Fields.PartnerId")]
        public string PartnerId { get; set; }
        public bool PartnerId_OverrideForStore { get; set; }

        [NopResourceDisplayName("Plugins.ElectronicInvoice.SIIGO.Fields.Username")]
        public string Username { get; set; }
        public bool Username_OverrideForStore { get; set; }

        [NopResourceDisplayName("Plugins.ElectronicInvoice.SIIGO.Fields.AccessKey")]
        public string AccessKey { get; set; }
        public bool AccessKey_OverrideForStore { get; set; }

        [NopResourceDisplayName("Plugins.ElectronicInvoice.SIIGO.Fields.DocumentId")]
        public int DocumentId { get; set; }
        public bool DocumentId_OverrideForStore { get; set; }

        [NopResourceDisplayName("Plugins.ElectronicInvoice.SIIGO.Fields.DefaultItemCode")]
        public string DefaultItemCode { get; set; }
        public bool DefaultItemCode_OverrideForStore { get; set; }

        [NopResourceDisplayName("Plugins.ElectronicInvoice.SIIGO.Fields.SellerId")]
        public int SellerId { get; set; }
        public bool SellerId_OverrideForStore { get; set; }

        [NopResourceDisplayName("Plugins.ElectronicInvoice.SIIGO.Fields.PaymentMethodId")]
        public int PaymentMethodId { get; set; }
        public bool PaymentMethodId_OverrideForStore { get; set; }

        [NopResourceDisplayName("Plugins.ElectronicInvoice.SIIGO.Fields.AccountGroup")]
        public int AccountGroup { get; set; }
        public bool AccountGroup_OverrideForStore { get; set; }

        [NopResourceDisplayName("Plugins.ElectronicInvoice.SIIGO.Fields.SendByEmail")]
        public bool SendByEmail { get; set; }
        public bool SendByEmail_OverrideForStore { get; set; }

        [NopResourceDisplayName("Plugins.ElectronicInvoice.SIIGO.Fields.SendStamp")]
        public bool SendStamp { get; set; }
        public bool SendStamp_OverrideForStore { get; set; }

        [NopResourceDisplayName("Plugins.ElectronicInvoice.SIIGO.Fields.CopyToEmail")]
        public string CopyToEmail { get; set; }
        public bool CopyToEmail_OverrideForStore { get; set; }

        [NopResourceDisplayName("Plugins.ElectronicInvoice.SIIGO.Fields.CurrencyCode")]
        public string CurrencyCode { get; set; }
        public bool CurrencyCode_OverrideForStore { get; set; }

        [NopResourceDisplayName("Plugins.ElectronicInvoice.SIIGO.Fields.ExchangeRate")]
        public string ExchangeRate { get; set; }
        public bool ExchangeRate_OverrideForStore { get; set; }

        [NopResourceDisplayName("Plugins.ElectronicInvoice.SIIGO.Fields.LogEnabled")]
        public bool LogEnabled { get; set; }
        public bool LogEnabled_OverrideForStore { get; set; }

        public TaxCategoryMappingConfigurationModel TaxCategoryMappings { get; set; }

        public List<InvoicedOrderModel> RecentInvoicedOrders { get; set; }
    }

    public record InvoicedOrderModel : BaseNopModel
    {
        public int OrderId { get; set; }
        public string OrderGuid { get; set; }
        public DateTime OrderDate { get; set; }
        public string CustomerEmail { get; set; }
        public decimal OrderTotal { get; set; }
        public string SiigoInvoiceId { get; set; }
        public long SiigoInvoiceNumber { get; set; }
        public DateTime? SiigoInvoiceDate { get; set; }
        public string SiigoInvoiceStatus { get; set; }
    }
}
