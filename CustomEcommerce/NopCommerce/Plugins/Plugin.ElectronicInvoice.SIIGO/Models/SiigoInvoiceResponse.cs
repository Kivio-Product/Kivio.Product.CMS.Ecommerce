using Newtonsoft.Json;

namespace Plugin.ElectronicInvoice.SIIGO.Models
{
    public class SiigoInvoiceResponse
    {
        [JsonProperty("id")]
        public string Id { get; set; }

        [JsonProperty("number")]
        public string Number { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("document")]
        public SiigoDocumentResponse Document { get; set; }

        [JsonProperty("date")]
        public string Date { get; set; }

        [JsonProperty("customer")]
        public SiigoCustomerResponse Customer { get; set; }

        [JsonProperty("seller")]
        public SiigoSellerResponse Seller { get; set; }

        [JsonProperty("total")]
        public decimal Total { get; set; }

        [JsonProperty("balance")]
        public decimal Balance { get; set; }

        [JsonProperty("currency")]
        public SiigoCurrencyResponse Currency { get; set; }

        [JsonProperty("status")]
        public string Status { get; set; }

        [JsonProperty("created")]
        public string Created { get; set; }

        [JsonProperty("updated")]
        public string Updated { get; set; }
    }

    public class SiigoDocumentResponse
    {
        [JsonProperty("id")]
        public int Id { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }
    }

    public class SiigoCustomerResponse
    {
        [JsonProperty("identification")]
        public string Identification { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }
    }

    public class SiigoSellerResponse
    {
        [JsonProperty("id")]
        public int Id { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }
    }

    public class SiigoCurrencyResponse
    {
        [JsonProperty("code")]
        public string Code { get; set; }

        [JsonProperty("symbol")]
        public string Symbol { get; set; }
    }

    public class SiigoErrorResponse
    {
        [JsonProperty("Errors")]
        public List<SiigoError> Errors { get; set; }
    }

    public class SiigoError
    {
        [JsonProperty("Code")]
        public string Code { get; set; }

        [JsonProperty("Detail")]
        public string Detail { get; set; }

        [JsonProperty("Params")]
        public Dictionary<string, object> Params { get; set; }
    }
}
