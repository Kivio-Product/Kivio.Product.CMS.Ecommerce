using Newtonsoft.Json;

namespace Plugin.ElectronicInvoice.SIIGO.Models
{
    public class SiigoInvoiceResponse
    {
        [JsonProperty("id")]
        public string Id { get; set; }

        [JsonProperty("document")]
        public SiigoDocumentResponse Document { get; set; }

        [JsonProperty("prefix")]
        public string Prefix { get; set; }

        [JsonProperty("number")]
        public long Number { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("date")]
        public string Date { get; set; }

        [JsonProperty("customer")]
        public SiigoCustomerResponse Customer { get; set; }

        [JsonProperty("cost_center")]
        public int? CostCenter { get; set; }

        [JsonProperty("currency")]
        public SiigoCurrencyResponse Currency { get; set; }

        [JsonProperty("seller")]
        public int Seller { get; set; }

        [JsonProperty("total")]
        public decimal Total { get; set; }

        [JsonProperty("balance")]
        public decimal Balance { get; set; }

        [JsonProperty("observations")]
        public string Observations { get; set; }

        [JsonProperty("items")]
        public List<SiigoInvoiceItemResponse> Items { get; set; }

        [JsonProperty("payments")]
        public List<SiigoInvoicePaymentResponse> Payments { get; set; }

        [JsonProperty("stamp")]
        public SiigoStampResponse Stamp { get; set; }

        [JsonProperty("mail")]
        public SiigoMailResponse Mail { get; set; }

        [JsonProperty("metadata")]
        public SiigoMetadataResponse Metadata { get; set; }

        [JsonProperty("public_url")]
        public string PublicUrl { get; set; }

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
        [JsonProperty("id")]
        public string Id { get; set; }

        [JsonProperty("identification")]
        public string Identification { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("branch_office")]
        public int BranchOffice { get; set; }
    }

    public class SiigoCurrencyResponse
    {
        [JsonProperty("code")]
        public string Code { get; set; }

        [JsonProperty("symbol")]
        public string Symbol { get; set; }

        [JsonProperty("exchange_rate")]
        public decimal? ExchangeRate { get; set; }
    }

    public class SiigoInvoiceItemResponse
    {
        [JsonProperty("id")]
        public string Id { get; set; }

        [JsonProperty("code")]
        public string Code { get; set; }

        [JsonProperty("quantity")]
        public decimal Quantity { get; set; }

        [JsonProperty("price")]
        public decimal Price { get; set; }

        [JsonProperty("description")]
        public string Description { get; set; }

        [JsonProperty("total")]
        public decimal Total { get; set; }
    }

    public class SiigoInvoicePaymentResponse
    {
        [JsonProperty("id")]
        public int Id { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("value")]
        public decimal Value { get; set; }
    }

    public class SiigoStampResponse
    {
        [JsonProperty("status")]
        public string Status { get; set; }
    }

    public class SiigoMailResponse
    {
        [JsonProperty("status")]
        public string Status { get; set; }

        [JsonProperty("observations")]
        public string Observations { get; set; }
    }

    public class SiigoMetadataResponse
    {
        [JsonProperty("created")]
        public string Created { get; set; }
    }

    public class SiigoSellerResponse
    {
        [JsonProperty("id")]
        public int Id { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }
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
