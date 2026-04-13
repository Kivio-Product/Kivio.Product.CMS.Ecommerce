using Nop.Core.Configuration;

namespace Nop.Plugin.Payments.MercadoPago
{
    public class MercadoPagoPaymentSettings : ISettings
    {
        public bool AdditionalFeeEnabled { get; set; }
        public string AccessToken { get; set; }
        public string PublicKey { get; set; }
        public string PaymentDescription { get; set; }
        public string SelectedCurrencyIds { get; set; }

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
