using Nop.Web.Framework.Models;
using Nop.Web.Framework.Mvc.ModelBinding;

namespace Plugin.ElectronicInvoice.SIIGO.Models
{
    /// <summary>
    /// Represents a mapping between NopCommerce payment methods and SIIGO payment method codes
    /// </summary>
    public record SiigoPaymentMethodMappingModel : BaseNopEntityModel
    {
        public SiigoPaymentMethodMappingModel()
        {
            SubOptionsConfiguration = new PaymentSubOptionConfigurationModel();
        }

        [NopResourceDisplayName("Plugins.ElectronicInvoice.SIIGO.Fields.PaymentMethodSystemName")]
        public string PaymentMethodSystemName { get; set; }

        [NopResourceDisplayName("Plugins.ElectronicInvoice.SIIGO.Fields.PaymentMethodFriendlyName")]
        public string PaymentMethodFriendlyName { get; set; }

        [NopResourceDisplayName("Plugins.ElectronicInvoice.SIIGO.Fields.SiigoPaymentMethodCode")]
        public int SiigoPaymentMethodCode { get; set; }

        [NopResourceDisplayName("Plugins.ElectronicInvoice.SIIGO.Fields.IsEnabled")]
        public bool IsEnabled { get; set; }

        /// <summary>
        /// Indicates if this payment method has sub-options configured
        /// </summary>
        public bool HasSubOptions { get; set; }

        /// <summary>
        /// Number of configured sub-options
        /// </summary>
        public int SubOptionsCount { get; set; }

        /// <summary>
        /// Configuration for sub-options (used in detail view)
        /// </summary>
        public PaymentSubOptionConfigurationModel SubOptionsConfiguration { get; set; }
    }

    /// <summary>
    /// Model for payment method mapping configuration
    /// </summary>
    public record PaymentMethodMappingConfigurationModel : BaseNopModel
    {
        public PaymentMethodMappingConfigurationModel()
        {
            PaymentMethodMappings = new List<SiigoPaymentMethodMappingModel>();
            AvailablePaymentMethods = new List<Microsoft.AspNetCore.Mvc.Rendering.SelectListItem>();
        }

        public List<SiigoPaymentMethodMappingModel> PaymentMethodMappings { get; set; }
        public List<Microsoft.AspNetCore.Mvc.Rendering.SelectListItem> AvailablePaymentMethods { get; set; }

        // Add new mapping fields
        public string NewPaymentMethodSystemName { get; set; }
        public int NewSiigoPaymentMethodCode { get; set; }
        public bool NewIsEnabled { get; set; }
    }
}