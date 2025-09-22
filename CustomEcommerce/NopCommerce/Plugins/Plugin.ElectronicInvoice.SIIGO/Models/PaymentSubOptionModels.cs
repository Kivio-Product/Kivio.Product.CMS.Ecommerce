using Nop.Web.Framework.Models;
using Nop.Web.Framework.Mvc.ModelBinding;

namespace Plugin.ElectronicInvoice.SIIGO.Models
{
    /// <summary>
    /// Model for payment sub-option selection modal
    /// </summary>
    public record PaymentSubOptionSelectionModel : BaseNopModel
    {
        public PaymentSubOptionSelectionModel()
        {
            AvailableSubOptions = new List<PaymentSubOptionModel>();
        }

        public int OrderId { get; set; }
        public string PaymentMethodName { get; set; }
        public List<PaymentSubOptionModel> AvailableSubOptions { get; set; }
        public int SelectedSubOptionCode { get; set; }
    }

    /// <summary>
    /// Model for individual payment sub-option
    /// </summary>
    public record PaymentSubOptionModel : BaseNopModel
    {
        public string Name { get; set; }
        public int SiigoCode { get; set; }
        public string Description { get; set; }
        public bool IsSelected { get; set; }
    }

    /// <summary>
    /// Model for configuring payment sub-options in dashboard
    /// </summary>
    public record PaymentSubOptionConfigurationModel : BaseNopModel
    {
        public PaymentSubOptionConfigurationModel()
        {
            SubOptions = new List<PaymentSubOptionConfigModel>();
        }

        public string PaymentMethodSystemName { get; set; }
        public string PaymentMethodFriendlyName { get; set; }
        public List<PaymentSubOptionConfigModel> SubOptions { get; set; }
        
        // For adding new sub-options
        public string NewSubOptionName { get; set; }
        public int NewSubOptionSiigoCode { get; set; }
        public string NewSubOptionDescription { get; set; }
        public bool NewSubOptionIsEnabled { get; set; } = true;
    }

    /// <summary>
    /// Model for individual sub-option configuration
    /// </summary>
    public record PaymentSubOptionConfigModel : BaseNopEntityModel
    {
        [NopResourceDisplayName("Plugins.ElectronicInvoice.SIIGO.Fields.SubOptionName")]
        public string Name { get; set; }

        [NopResourceDisplayName("Plugins.ElectronicInvoice.SIIGO.Fields.SubOptionSiigoCode")]
        public int SiigoCode { get; set; }

        [NopResourceDisplayName("Plugins.ElectronicInvoice.SIIGO.Fields.SubOptionDescription")]
        public string Description { get; set; }

        [NopResourceDisplayName("Plugins.ElectronicInvoice.SIIGO.Fields.IsEnabled")]
        public bool IsEnabled { get; set; }
    }
}