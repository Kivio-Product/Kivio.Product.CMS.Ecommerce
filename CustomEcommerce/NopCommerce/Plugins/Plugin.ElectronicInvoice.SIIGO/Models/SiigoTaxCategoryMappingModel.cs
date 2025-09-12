using Nop.Web.Framework.Models;
using Nop.Web.Framework.Mvc.ModelBinding;

namespace Plugin.ElectronicInvoice.SIIGO.Models
{
    /// <summary>
    /// Represents a mapping between NopCommerce tax categories and SIIGO tax codes
    /// </summary>
    public record SiigoTaxCategoryMappingModel : BaseNopEntityModel
    {
        [NopResourceDisplayName("Plugins.ElectronicInvoice.SIIGO.Fields.TaxCategoryId")]
        public int TaxCategoryId { get; set; }

        [NopResourceDisplayName("Plugins.ElectronicInvoice.SIIGO.Fields.TaxCategoryName")]
        public string TaxCategoryName { get; set; }

        [NopResourceDisplayName("Plugins.ElectronicInvoice.SIIGO.Fields.SiigoTaxCode")]
        public int SiigoTaxCode { get; set; }

        [NopResourceDisplayName("Plugins.ElectronicInvoice.SIIGO.Fields.IsEnabled")]
        public bool IsEnabled { get; set; }
    }

    /// <summary>
    /// Model for tax category mapping configuration
    /// </summary>
    public record TaxCategoryMappingConfigurationModel : BaseNopModel
    {
        public TaxCategoryMappingConfigurationModel()
        {
            TaxCategoryMappings = new List<SiigoTaxCategoryMappingModel>();
            AvailableTaxCategories = new List<Microsoft.AspNetCore.Mvc.Rendering.SelectListItem>();
        }

        public List<SiigoTaxCategoryMappingModel> TaxCategoryMappings { get; set; }
        public List<Microsoft.AspNetCore.Mvc.Rendering.SelectListItem> AvailableTaxCategories { get; set; }

        // Add new mapping fields
        public int NewTaxCategoryId { get; set; }
        public int NewSiigoTaxCode { get; set; }
        public bool NewIsEnabled { get; set; }
    }
}
