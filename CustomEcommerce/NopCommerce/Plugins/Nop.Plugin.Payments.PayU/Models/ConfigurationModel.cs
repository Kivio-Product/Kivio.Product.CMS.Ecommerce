using Nop.Web.Framework.Models;
using Nop.Web.Framework.Mvc.ModelBinding;

namespace Nop.Plugin.Payments.PayU.Models
{
    public record ConfigurationModel : BaseNopModel
    {
        public int ActiveStoreScopeConfiguration { get; set; }

        [NopResourceDisplayName("Plugins.Payments.PayU.Fields.UseSandbox")]
        public bool UseSandbox { get; set; }
        public bool UseSandboxOverrideForStore { get; set; }

        [NopResourceDisplayName("Plugins.Payments.PayU.Fields.AccountId")]
        public string AccountId { get; set; }
        public bool AccountIdOverrideForStore { get; set; }

        [NopResourceDisplayName("Plugins.Payments.PayU.Fields.MerchantId")]
        public string MerchantId { get; set; }
        public bool MerchantIdOverrideForStore { get; set; }

        [NopResourceDisplayName("Plugins.Payments.PayU.Fields.ClientLoginId")]
        public string ClientLoginId { get; set; }
        public bool ClientLoginIdOverrideForStore { get; set; }

        [NopResourceDisplayName("Plugins.Payments.PayU.Fields.ClientSecretKey")]
        public string ClientSecretKey { get; set; }
        public bool ClientSecretKeyOverrideForStore { get; set; }

        [NopResourceDisplayName("Plugins.Payments.PayU.Fields.ClientPublicKey")]
        public string ClientPublicKey { get; set; }
        public bool ClientPublicKeyOverrideForStore { get; set; }

        [NopResourceDisplayName("Plugins.Payments.PayU.Fields.PaymentDescription")]
        public string PaymentDescription { get; set; }
        public bool PaymentDescriptionOverrideForStore { get; set; }
    }
}