using Newtonsoft.Json;

namespace Plugin.ElectronicInvoice.SIIGO.Models
{
    public class SiigoEmailRequest
    {
        [JsonProperty("mail_to")]
        public string MailTo { get; set; }

        [JsonProperty("copy_to")]
        public string CopyTo { get; set; }
    }

    public class SiigoEmailResponse
    {
        [JsonProperty("success")]
        public bool Success { get; set; }

        [JsonProperty("message")]
        public string Message { get; set; }
    }
}
