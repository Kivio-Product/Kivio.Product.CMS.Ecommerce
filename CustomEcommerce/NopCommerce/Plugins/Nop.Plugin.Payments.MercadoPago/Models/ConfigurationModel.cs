using Microsoft.AspNetCore.Mvc.Rendering;
using Nop.Web.Framework.Models;
using Nop.Web.Framework.Mvc.ModelBinding;
using System.Collections.Generic;

namespace Nop.Plugin.Payments.MercadoPago.Models
{
    public record ConfigurationModel : BaseNopModel
    {
        public int ActiveStoreScopeConfiguration { get; set; }

        [NopResourceDisplayName("Plugins.Payments.MercadoPago.Fields.AdditionalFeeEnabled")]
        public bool AdditionalFeeEnabled { get; set; }
        public bool AdditionalFeeEnabledOverrideForStore { get; set; }

        [NopResourceDisplayName("Plugins.Payments.MercadoPago.Fields.AccessToken")]
        public string AccessToken { get; set; }
        public bool AccessTokenOverrideForStore { get; set; }

        [NopResourceDisplayName("Plugins.Payments.MercadoPago.Fields.PublicKey")]
        public string PublicKey { get; set; }
        public bool PublicKeyOverrideForStore { get; set; }

        [NopResourceDisplayName("Plugins.Payments.MercadoPago.Fields.PaymentDescription")]
        public string PaymentDescription { get; set; }
        public bool PaymentDescriptionOverrideForStore { get; set; }

        [NopResourceDisplayName("Plugins.Payments.MercadoPago.Fields.SelectedCurrencies")]
        public IList<int> SelectedCurrencyIds { get; set; }
        public bool SelectedCurrencyIdsOverrideForStore { get; set; }

        public IList<SelectListItem> AvailableCurrencies { get; set; }
    }
}
