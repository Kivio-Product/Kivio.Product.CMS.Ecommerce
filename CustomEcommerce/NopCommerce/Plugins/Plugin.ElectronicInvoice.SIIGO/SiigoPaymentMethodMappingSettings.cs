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
        
        /// <summary>
        /// Sub-options for this payment method (e.g., for CashOnDelivery: Cash, Transfer, etc.)
        /// </summary>
        public List<PaymentSubOption> SubOptions { get; set; } = new List<PaymentSubOption>();
        
        /// <summary>
        /// Indicates if this payment method has sub-options configured
        /// </summary>
        public bool HasSubOptions => SubOptions?.Any() == true;
    }

    /// <summary>
    /// Represents a sub-option for a payment method (e.g., Cash, Transfer for CashOnDelivery)
    /// </summary>
    public class PaymentSubOption
    {
        public string Name { get; set; }  // "Efectivo", "Transferencia", "Nequi"
        public int SiigoCode { get; set; }  // 1234, 5678
        public bool IsEnabled { get; set; } = true;
        public string Description { get; set; }  // Optional description
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
                // Keep existing sub-options
            }
            else
            {
                mappings.Add(new SiigoPaymentMethodMapping
                {
                    PaymentMethodSystemName = paymentMethodSystemName,
                    SiigoPaymentMethodCode = siigoPaymentMethodCode,
                    IsEnabled = isEnabled,
                    SubOptions = new List<PaymentSubOption>()
                });
            }

            PaymentMethodMappings = mappings;
        }

        /// <summary>
        /// Adds or updates a sub-option for a specific payment method
        /// </summary>
        /// <param name="paymentMethodSystemName">Payment method system name</param>
        /// <param name="subOptionName">Sub-option name</param>
        /// <param name="siigoCode">SIIGO code for this sub-option</param>
        /// <param name="isEnabled">Whether the sub-option is enabled</param>
        /// <param name="description">Optional description</param>
        public void AddOrUpdateSubOption(string paymentMethodSystemName, string subOptionName, int siigoCode, bool isEnabled, string description = null)
        {
            if (string.IsNullOrEmpty(paymentMethodSystemName) || string.IsNullOrEmpty(subOptionName))
                return;

            var mappings = PaymentMethodMappings.ToList();
            var mapping = mappings.FirstOrDefault(m => 
                m.PaymentMethodSystemName.Equals(paymentMethodSystemName, StringComparison.OrdinalIgnoreCase));

            if (mapping != null)
            {
                if (mapping.SubOptions == null)
                    mapping.SubOptions = new List<PaymentSubOption>();

                var existingSubOption = mapping.SubOptions.FirstOrDefault(so => 
                    so.Name.Equals(subOptionName, StringComparison.OrdinalIgnoreCase));

                if (existingSubOption != null)
                {
                    existingSubOption.SiigoCode = siigoCode;
                    existingSubOption.IsEnabled = isEnabled;
                    existingSubOption.Description = description;
                }
                else
                {
                    mapping.SubOptions.Add(new PaymentSubOption
                    {
                        Name = subOptionName,
                        SiigoCode = siigoCode,
                        IsEnabled = isEnabled,
                        Description = description
                    });
                }

                PaymentMethodMappings = mappings;
            }
        }

        /// <summary>
        /// Removes a sub-option from a payment method
        /// </summary>
        /// <param name="paymentMethodSystemName">Payment method system name</param>
        /// <param name="subOptionName">Sub-option name to remove</param>
        /// <returns>True if removed successfully</returns>
        public bool RemoveSubOption(string paymentMethodSystemName, string subOptionName)
        {
            if (string.IsNullOrEmpty(paymentMethodSystemName) || string.IsNullOrEmpty(subOptionName))
                return false;

            var mappings = PaymentMethodMappings.ToList();
            var mapping = mappings.FirstOrDefault(m => 
                m.PaymentMethodSystemName.Equals(paymentMethodSystemName, StringComparison.OrdinalIgnoreCase));

            if (mapping?.SubOptions != null)
            {
                var subOptionToRemove = mapping.SubOptions.FirstOrDefault(so => 
                    so.Name.Equals(subOptionName, StringComparison.OrdinalIgnoreCase));

                if (subOptionToRemove != null)
                {
                    mapping.SubOptions.Remove(subOptionToRemove);
                    PaymentMethodMappings = mappings;
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Gets sub-options for a specific payment method
        /// </summary>
        /// <param name="paymentMethodSystemName">Payment method system name</param>
        /// <returns>List of enabled sub-options</returns>
        public List<PaymentSubOption> GetSubOptions(string paymentMethodSystemName)
        {
            if (string.IsNullOrEmpty(paymentMethodSystemName))
                return new List<PaymentSubOption>();

            var mapping = PaymentMethodMappings
                .FirstOrDefault(m => m.PaymentMethodSystemName.Equals(paymentMethodSystemName, StringComparison.OrdinalIgnoreCase) && m.IsEnabled);

            return mapping?.SubOptions?.Where(so => so.IsEnabled).ToList() ?? new List<PaymentSubOption>();
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