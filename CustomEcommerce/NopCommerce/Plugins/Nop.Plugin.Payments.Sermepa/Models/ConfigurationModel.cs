using Nop.Web.Framework.Models;
using Nop.Web.Framework.Mvc.ModelBinding;

namespace Nop.Plugin.Payments.Sermepa.Models
{
    public record ConfigurationModel : BaseNopModel
    {
        [NopResourceDisplayName("Plugins.Payments.Sermepa.Fields.NombreComercio")]
        public string NombreComercio { get; set; }
        public bool NombreComercio_OverrideForStore { get; set; }

        [NopResourceDisplayName("Plugins.Payments.Sermepa.Fields.Titular")]
        public string Titular { get; set; }
        public bool Titular_OverrideForStore { get; set; }

        [NopResourceDisplayName("Plugins.Payments.Sermepa.Fields.Producto")]
        public string Producto { get; set; }
        public bool Producto_OverrideForStore { get; set; }

        [NopResourceDisplayName("Plugins.Payments.Sermepa.Fields.FUC")]
        public string FUC { get; set; }
        public bool FUC_OverrideForStore { get; set; }

        [NopResourceDisplayName("Plugins.Payments.Sermepa.Fields.Terminal")]
        public string Terminal { get; set; }
        public bool Terminal_OverrideForStore { get; set; }

        [NopResourceDisplayName("Plugins.Payments.Sermepa.Fields.Moneda")]
        public string Moneda { get; set; }
        public bool Moneda_OverrideForStore { get; set; }

        [NopResourceDisplayName("Plugins.Payments.Sermepa.Fields.ClaveReal")]
        public string ClaveReal { get; set; }
        public bool ClaveReal_OverrideForStore { get; set; }

        [NopResourceDisplayName("Plugins.Payments.Sermepa.Fields.ClavePruebas")]
        public string ClavePruebas { get; set; }
        public bool ClavePruebas_OverrideForStore { get; set; }

        [NopResourceDisplayName("Plugins.Payments.Sermepa.Fields.Pruebas")]
        public bool Pruebas { get; set; }
        public bool Pruebas_OverrideForStore { get; set; }

        [NopResourceDisplayName("Plugins.Payments.Sermepa.Fields.AdditionalFee")]
        public decimal AdditionalFee { get; set; }
        public bool AdditionalFee_OverrideForStore { get; set; }

        [NopResourceDisplayName("Plugins.Payments.Sermepa.Fields.AdditionalFeePercentage")]
        public bool AdditionalFeePercentage { get; set; }
        public bool AdditionalFeePercentage_OverrideForStore { get; set; }

        public int ActiveStoreScopeConfiguration { get; set; }
    }
}