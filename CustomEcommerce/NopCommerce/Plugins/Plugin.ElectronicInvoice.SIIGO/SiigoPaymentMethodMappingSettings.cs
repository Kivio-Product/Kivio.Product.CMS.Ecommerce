using Nop.Core.Configuration;
using Newtonsoft.Json;

namespace Plugin.ElectronicInvoice.SIIGO
{
    /// <summary>
    /// Represents a mapping between a payment method and SIIGO payment method code
    /// </summary>
    public class SiigoPaymentMethodMapping
    {
        public string PaymentMethodSystemName { get; set; }
        public int SiigoPaymentMethodCode { get; set; }
        public bool IsEnabled { get; set; }
    }

    /// <summary>
    /// Settings for SIIGO payment method mappings
    /// </summary>
    public class SiigoPaymentMethodMappingSettings : ISettings
    {
        public SiigoPaymentMethodMappingSettings()
        {
            PaymentMethodMappings = new List<SiigoPaymentMethodMapping>();
        }

        /// <summary>
        /// Payment method mappings stored as JSON
        /// </summary>
        public string PaymentMethodMappingsJson { get; set; }

        /// <summary>
        /// Gets or sets the payment method mappings (not persisted directly)
        /// </summary>
        [JsonIgnore]
        public List<SiigoPaymentMethodMapping> PaymentMethodMappings
        {
            get
            {
                if (string.IsNullOrEmpty(PaymentMethodMappingsJson))
                    return new List<SiigoPaymentMethodMapping>();

                try
                {
                    return JsonConvert.DeserializeObject<List<SiigoPaymentMethodMapping>>(PaymentMethodMappingsJson) 
                           ?? new List<SiigoPaymentMethodMapping>();
                }
                catch
                {
                    return new List<SiigoPaymentMethodMapping>();
                }
            }
            set
            {
                PaymentMethodMappingsJson = JsonConvert.SerializeObject(value ?? new List<SiigoPaymentMethodMapping>());
            }
        }

        /// <summary>
        /// Gets the SIIGO payment method code for a specific payment method system name
        /// </summary>
        /// <param name="paymentMethodSystemName">Payment method system name</param>
        /// <returns>SIIGO payment method code if mapping exists and is enabled, null otherwise</returns>
        public int? GetSiigoPaymentMethodCode(string paymentMethodSystemName)
        {
            if (string.IsNullOrEmpty(paymentMethodSystemName))
                return null;

            var mapping = PaymentMethodMappings
                .FirstOrDefault(m => m.PaymentMethodSystemName.Equals(paymentMethodSystemName, StringComparison.OrdinalIgnoreCase) && m.IsEnabled);
            
            return mapping?.SiigoPaymentMethodCode;
        }

        /// <summary>
        /// Checks if a payment method has a valid SIIGO mapping
        /// </summary>
        /// <param name="paymentMethodSystemName">Payment method system name</param>
        /// <returns>True if mapping exists and is enabled</returns>
        public bool HasValidMapping(string paymentMethodSystemName)
        {
            if (string.IsNullOrEmpty(paymentMethodSystemName))
                return false;

            return PaymentMethodMappings
                .Any(m => m.PaymentMethodSystemName.Equals(paymentMethodSystemName, StringComparison.OrdinalIgnoreCase) 
                         && m.IsEnabled && m.SiigoPaymentMethodCode > 0);
        }

        /// <summary>
        /// Adds or updates a payment method mapping
        /// </summary>
        /// <param name="paymentMethodSystemName">Payment method system name</param>
        /// <param name="siigoPaymentMethodCode">SIIGO payment method code</param>
        /// <param name="isEnabled">Whether the mapping is enabled</param>
        public void AddOrUpdateMapping(string paymentMethodSystemName, int siigoPaymentMethodCode, bool isEnabled)
        {
            if (string.IsNullOrEmpty(paymentMethodSystemName))
                return;

            var mappings = PaymentMethodMappings.ToList();
            var existingMapping = mappings.FirstOrDefault(m => 
                m.PaymentMethodSystemName.Equals(paymentMethodSystemName, StringComparison.OrdinalIgnoreCase));

            if (existingMapping != null)
            {
                existingMapping.SiigoPaymentMethodCode = siigoPaymentMethodCode;
                existingMapping.IsEnabled = isEnabled;
            }
            else
            {
                mappings.Add(new SiigoPaymentMethodMapping
                {
                    PaymentMethodSystemName = paymentMethodSystemName,
                    SiigoPaymentMethodCode = siigoPaymentMethodCode,
                    IsEnabled = isEnabled
                });
            }

            PaymentMethodMappings = mappings;
        }

        /// <summary>
        /// Removes a payment method mapping
        /// </summary>
        /// <param name="paymentMethodSystemName">Payment method system name</param>
        /// <returns>True if mapping was found and removed</returns>
        public bool RemoveMapping(string paymentMethodSystemName)
        {
            if (string.IsNullOrEmpty(paymentMethodSystemName))
                return false;

            var mappings = PaymentMethodMappings.ToList();
            var mappingToRemove = mappings.FirstOrDefault(m => 
                m.PaymentMethodSystemName.Equals(paymentMethodSystemName, StringComparison.OrdinalIgnoreCase));

            if (mappingToRemove != null)
            {
                mappings.Remove(mappingToRemove);
                PaymentMethodMappings = mappings;
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
            return $"JSON: {PaymentMethodMappingsJson ?? "null"}, Count: {PaymentMethodMappings.Count}";
        }
    }
}