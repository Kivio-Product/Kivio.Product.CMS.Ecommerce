using Newtonsoft.Json;

namespace Plugin.ElectronicInvoice.SIIGO.Models
{
    public class SiigoInvoiceRequest
    {
        [JsonProperty("document")]
        public SiigoDocument Document { get; set; }

        [JsonProperty("date")]
        public string Date { get; set; }

        [JsonProperty("customer")]
        public SiigoCustomer Customer { get; set; }

        [JsonProperty("currency")]
        public SiigoCurrency Currency { get; set; }

        [JsonProperty("seller")]
        public int Seller { get; set; }

        [JsonProperty("stamp")]
        public SiigoStamp Stamp { get; set; }

        [JsonProperty("mail")]
        public SiigoMail Mail { get; set; }

        [JsonProperty("observations")]
        public string Observations { get; set; }

        [JsonProperty("items")]
        public List<SiigoItem> Items { get; set; }

        [JsonProperty("payments")]
        public List<SiigoPayment> Payments { get; set; }

        [JsonProperty("additional_fields")]
        public SiigoAdditionalFields AdditionalFields { get; set; }
    }

    public class SiigoDocument
    {
        [JsonProperty("id")]
        public int Id { get; set; }
    }

    public class SiigoCustomer
    {
        [JsonProperty("person_type")]
        public string PersonType { get; set; }

        [JsonProperty("id_type")]
        public string IdType { get; set; }

        [JsonProperty("identification")]
        public string Identification { get; set; }

        [JsonProperty("name")]
        public List<string> Name { get; set; }

        [JsonProperty("address")]
        public SiigoAddress Address { get; set; }

        [JsonProperty("phones")]
        public List<SiigoPhone> Phones { get; set; }
    }

    public class SiigoAddress
    {
        [JsonProperty("address")]
        public string Address { get; set; }

        [JsonProperty("city")]
        public SiigoCity City { get; set; }

        [JsonProperty("postal_code")]
        public string PostalCode { get; set; }
    }

    public class SiigoCity
    {
        [JsonProperty("country_code")]
        public string CountryCode { get; set; }

        [JsonProperty("country_name")]
        public string CountryName { get; set; }

        [JsonProperty("state_code")]
        public string StateCode { get; set; }

        [JsonProperty("state_name")]
        public string StateName { get; set; }

        [JsonProperty("city_code")]
        public string CityCode { get; set; }

        [JsonProperty("city_name")]
        public string CityName { get; set; }
    }

    public class SiigoPhone
    {
        [JsonProperty("indicative")]
        public string Indicative { get; set; }

        [JsonProperty("number")]
        public string Number { get; set; }

        [JsonProperty("extension")]
        public string Extension { get; set; }
    }

    public class SiigoCurrency
    {
        [JsonProperty("code")]
        public string Code { get; set; }

        [JsonProperty("exchange_rate")]
        public string ExchangeRate { get; set; }
    }

    public class SiigoStamp
    {
        [JsonProperty("send")]
        public bool Send { get; set; }
    }

    public class SiigoMail
    {
        [JsonProperty("send")]
        public bool Send { get; set; }
    }

    public class SiigoItem
    {
        [JsonProperty("code")]
        public string Code { get; set; }

        [JsonProperty("description")]
        public string Description { get; set; }

        [JsonProperty("quantity")]
        public int Quantity { get; set; }

        [JsonProperty("price")]
        public decimal Price { get; set; }

        [JsonProperty("taxes")]
        public List<SiigoTax> Taxes { get; set; }
    }

    public class SiigoTax
    {
        [JsonProperty("id")]
        public int Id { get; set; }
    }

    public class SiigoPayment
    {
        [JsonProperty("id")]
        public int Id { get; set; }

        [JsonProperty("value")]
        public decimal Value { get; set; }

        [JsonProperty("due_date")]
        public string DueDate { get; set; }
    }

    public class SiigoAdditionalFields
    {
        [JsonProperty("billing_id")]
        public string BillingId { get; set; }

        [JsonProperty("customer_email")]
        public string CustomerEmail { get; set; }

        [JsonProperty("generated_by")]
        public string GeneratedBy { get; set; }

        [JsonProperty("pos_id")]
        public string PosId { get; set; }
    }
}
