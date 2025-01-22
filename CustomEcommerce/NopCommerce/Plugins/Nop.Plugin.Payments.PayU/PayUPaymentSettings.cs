using Nop.Core.Configuration;

namespace Nop.Plugin.Payments.PayU
{
    public class PayUPaymentSettings : ISettings
    {
        public bool UseSandbox { get; set; }
        public string AccountId { get; set; }
        public string MerchantId { get; set; }
        public string ClientLoginId { get; set; }
        public string ClientSecretKey { get; set; }
        public string ClientPublicKey { get; set; }
        public string PaymentDescription { get; set; }
    }
}
