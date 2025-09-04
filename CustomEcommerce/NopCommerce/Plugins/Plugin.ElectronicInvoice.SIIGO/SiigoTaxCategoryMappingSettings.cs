using Nop.Core.Configuration;
using Newtonsoft.Json;

namespace Plugin.ElectronicInvoice.SIIGO
{
    /// <summary>
    /// Represents a mapping between a tax category and SIIGO tax code
    /// </summary>
    public class SiigoTaxCategoryMapping
    {
        public int TaxCategoryId { get; set; }
        public int SiigoTaxCode { get; set; }
        public bool IsEnabled { get; set; }
    }

    /// <summary>
    /// Settings for SIIGO tax category mappings
    /// </summary>
    public class SiigoTaxCategoryMappingSettings : ISettings
    {
        public SiigoTaxCategoryMappingSettings()
        {
            TaxCategoryMappings = new List<SiigoTaxCategoryMapping>();
        }

        /// <summary>
        /// Tax category mappings stored as JSON
        /// </summary>
        public string TaxCategoryMappingsJson { get; set; }

        /// <summary>
        /// Gets or sets the tax category mappings (not persisted directly)
        /// </summary>
        [JsonIgnore]
        public List<SiigoTaxCategoryMapping> TaxCategoryMappings
        {
            get
            {
                if (string.IsNullOrEmpty(TaxCategoryMappingsJson))
                    return new List<SiigoTaxCategoryMapping>();

                try
                {
                    return JsonConvert.DeserializeObject<List<SiigoTaxCategoryMapping>>(TaxCategoryMappingsJson) 
                           ?? new List<SiigoTaxCategoryMapping>();
                }
                catch
                {
                    return new List<SiigoTaxCategoryMapping>();
                }
            }
            set
            {
                TaxCategoryMappingsJson = JsonConvert.SerializeObject(value ?? new List<SiigoTaxCategoryMapping>());
            }
        }

        /// <summary>
        /// Gets the SIIGO tax code for a specific tax category
        /// </summary>
        /// <param name="taxCategoryId">Tax category ID</param>
        /// <returns>SIIGO tax code if mapping exists and is enabled, null otherwise</returns>
        public int? GetSiigoTaxCode(int taxCategoryId)
        {
            var mapping = TaxCategoryMappings
                .FirstOrDefault(m => m.TaxCategoryId == taxCategoryId && m.IsEnabled);
            
            return mapping?.SiigoTaxCode;
        }

        /// <summary>
        /// Checks if a tax category has a valid SIIGO mapping
        /// </summary>
        /// <param name="taxCategoryId">Tax category ID</param>
        /// <returns>True if mapping exists and is enabled</returns>
        public bool HasValidMapping(int taxCategoryId)
        {
            return TaxCategoryMappings
                .Any(m => m.TaxCategoryId == taxCategoryId && m.IsEnabled && m.SiigoTaxCode > 0);
        }

        /// <summary>
        /// Adds or updates a tax category mapping
        /// </summary>
        /// <param name="taxCategoryId">Tax category ID</param>
        /// <param name="siigoTaxCode">SIIGO tax code</param>
        /// <param name="isEnabled">Whether the mapping is enabled</param>
        public void AddOrUpdateMapping(int taxCategoryId, int siigoTaxCode, bool isEnabled)
        {
            var mappings = TaxCategoryMappings.ToList();
            var existingMapping = mappings.FirstOrDefault(m => m.TaxCategoryId == taxCategoryId);

            if (existingMapping != null)
            {
                existingMapping.SiigoTaxCode = siigoTaxCode;
                existingMapping.IsEnabled = isEnabled;
            }
            else
            {
                mappings.Add(new SiigoTaxCategoryMapping
                {
                    TaxCategoryId = taxCategoryId,
                    SiigoTaxCode = siigoTaxCode,
                    IsEnabled = isEnabled
                });
            }

            TaxCategoryMappings = mappings;
        }

        /// <summary>
        /// Removes a tax category mapping
        /// </summary>
        /// <param name="taxCategoryId">Tax category ID</param>
        /// <returns>True if mapping was found and removed</returns>
        public bool RemoveMapping(int taxCategoryId)
        {
            var mappings = TaxCategoryMappings.ToList();
            var mappingToRemove = mappings.FirstOrDefault(m => m.TaxCategoryId == taxCategoryId);

            if (mappingToRemove != null)
            {
                mappings.Remove(mappingToRemove);
                TaxCategoryMappings = mappings;
                return true;
            }

            return false;
        }

        /// <summary>
        /// Gets debug information about the current mappings
        /// </summary>
        /// <returns>Debug info string</returns>
        public string GetDebugInfo()
        {
            return $"JSON: {TaxCategoryMappingsJson ?? "null"}, Count: {TaxCategoryMappings.Count}";
        }
    }
}
