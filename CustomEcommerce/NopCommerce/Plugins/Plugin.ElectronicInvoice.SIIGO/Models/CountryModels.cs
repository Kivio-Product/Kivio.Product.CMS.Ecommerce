using Newtonsoft.Json;

namespace Plugin.ElectronicInvoice.SIIGO.Models
{
    /// <summary>
    /// Modelos para el mapeo de países, estados y ciudades de Colombia
    /// </summary>
    public class CountryData
    {
        [JsonProperty("states")]
        public List<StateData> States { get; set; }
    }

    public class StateData
    {
        [JsonProperty("code")]
        public string Code { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("cities")]
        public List<CityData> Cities { get; set; }
    }

    public class CityData
    {
        [JsonProperty("code")]
        public string Code { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }
    }
}
