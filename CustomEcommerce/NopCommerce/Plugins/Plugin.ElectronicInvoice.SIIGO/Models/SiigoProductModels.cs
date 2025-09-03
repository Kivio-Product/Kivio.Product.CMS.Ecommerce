using Newtonsoft.Json;

namespace Plugin.ElectronicInvoice.SIIGO.Models
{
    public class SiigoProductRequest
    {
        [JsonProperty("code")]
        public string Code { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("account_group")]
        public int AccountGroup { get; set; }
    }

    public class SiigoProductResponse
    {
        [JsonProperty("id")]
        public string Id { get; set; }

        [JsonProperty("code")]
        public string Code { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("account_group")]
        public SiigoProductAccountGroup AccountGroup { get; set; }

        [JsonProperty("type")]
        public string Type { get; set; }

        [JsonProperty("stock_control")]
        public bool StockControl { get; set; }

        [JsonProperty("active")]
        public bool Active { get; set; }

        [JsonProperty("tax_classification")]
        public string TaxClassification { get; set; }

        [JsonProperty("tax_included")]
        public bool TaxIncluded { get; set; }

        [JsonProperty("tax_consumption_value")]
        public decimal? TaxConsumptionValue { get; set; }

        [JsonProperty("taxes")]
        public List<SiigoProductTax> Taxes { get; set; }

        [JsonProperty("prices")]
        public List<SiigoProductPrice> Prices { get; set; }

        [JsonProperty("unit")]
        public SiigoProductUnit Unit { get; set; }

        [JsonProperty("additional_fields")]
        public SiigoProductAdditionalFields AdditionalFields { get; set; }
    }

    public class SiigoProductAccountGroup
    {
        [JsonProperty("id")]
        public int Id { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }
    }

    public class SiigoProductTax
    {
        [JsonProperty("id")]
        public int Id { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("percentage")]
        public decimal Percentage { get; set; }
    }

    public class SiigoProductPrice
    {
        [JsonProperty("currency_code")]
        public string CurrencyCode { get; set; }

        [JsonProperty("price_list")]
        public List<SiigoProductPriceList> PriceList { get; set; }
    }

    public class SiigoProductPriceList
    {
        [JsonProperty("position")]
        public int Position { get; set; }

        [JsonProperty("value")]
        public decimal Value { get; set; }
    }

    public class SiigoProductUnit
    {
        [JsonProperty("code")]
        public string Code { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }
    }

    public class SiigoProductAdditionalFields
    {
        [JsonProperty("barcode")]
        public string Barcode { get; set; }

        [JsonProperty("brand")]
        public string Brand { get; set; }

        [JsonProperty("tariff")]
        public string Tariff { get; set; }

        [JsonProperty("model")]
        public string Model { get; set; }
    }

    public class SiigoProductSearchResponse
    {
        [JsonProperty("pagination")]
        public SiigoProductPagination Pagination { get; set; }

        [JsonProperty("results")]
        public List<SiigoProductResponse> Results { get; set; }
    }

    public class SiigoProductPagination
    {
        [JsonProperty("page")]
        public int Page { get; set; }

        [JsonProperty("size")]
        public int Size { get; set; }

        [JsonProperty("total")]
        public int Total { get; set; }
    }
}
