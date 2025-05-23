using Nop.Core.Configuration;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace Nop.Plugin.Payments.PayU
{
    public class PayUPaymentSettings : ISettings
    {
        public bool UseSandbox { get; set; }
        public bool AdditionalFeeEnabled { get; set; }
        public string AccountId { get; set; }
        public string MerchantId { get; set; }
        public string ClientLoginId { get; set; }
        public string ClientSecretKey { get; set; }
        public string ClientPublicKey { get; set; }
        public string PaymentDescription { get; set; }
        // <summary>
        /// Almacena internamente los IDs como string separados por comas
        /// </summary>
        public string SelectedCurrencyIds { get; set; } 

        /// <summary>
        /// Propiedad de ayuda para convertir el string a una lista de ints
        /// </summary>
        public IList<int> SelectedCurrencyIdList
        {
            get
            {
                var result = new List<int>();
                if (!string.IsNullOrEmpty(SelectedCurrencyIds))
                {
                    result.AddRange(SelectedCurrencyIds
                        .Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
                        .Select(id => int.Parse(id.Trim())));
                }
                return result;
            }
            set
            {
                if (value != null && value.Any())
                    SelectedCurrencyIds = string.Join(",", value);
                else
                    SelectedCurrencyIds = string.Empty;
            }
        }
    }
}
