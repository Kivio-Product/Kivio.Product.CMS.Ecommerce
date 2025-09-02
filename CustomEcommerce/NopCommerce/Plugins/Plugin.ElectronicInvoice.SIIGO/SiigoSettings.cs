using Nop.Core.Configuration;

namespace Plugin.ElectronicInvoice.SIIGO
{
    public class SiigoSettings : ISettings
    {
        public string ApiBaseUrl { get; set; }
        public string PartnerId { get; set; }
        public string Username { get; set; }
        public string AccessKey { get; set; }
        public int DocumentId { get; set; }
        public string DefaultItemCode { get; set; }
        public int SellerId { get; set; }
        public int PaymentMethodId { get; set; }
        public int TaxIdWithTax { get; set; }
        public int TaxIdWithoutTax { get; set; }
        public bool SendByEmail { get; set; }
        public bool SendStamp { get; set; }
        public bool IsEnabled { get; set; }
        public bool TestMode { get; set; }
        public bool LogEnabled { get; set; }
    }
}
