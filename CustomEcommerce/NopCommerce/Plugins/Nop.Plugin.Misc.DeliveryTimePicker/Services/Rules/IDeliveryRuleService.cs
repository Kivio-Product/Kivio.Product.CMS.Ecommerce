using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Nop.Plugin.Misc.DeliveryTimePicker.Services.Rules
{
    /// <summary>
    /// Service for evaluating delivery time rules
    /// </summary>
    public interface IDeliveryRuleService
    {
        /// <summary>
        /// Evaluates a delivery slot against all applicable rules
        /// </summary>
        /// <param name="context">The rule evaluation context</param>
        /// <param name="slot">The delivery slot to evaluate</param>
        /// <returns>Result indicating if the slot is allowed and why</returns>
        Task<RuleResult> EvaluateSlotAsync(DeliveryRuleContext context, DeliverySlot slot);

        /// <summary>
        /// Gets the list of supplier identifiers from a customer's shopping cart
        /// </summary>
        /// <param name="customerId">The customer ID</param>
        /// <returns>List of supplier identifiers found in the cart</returns>
        Task<HashSet<string>> GetSuppliersFromCartAsync(int customerId);

        /// <summary>
        /// Builds a rule context for the given customer
        /// </summary>
        /// <param name="customerId">The customer ID</param>
        /// <param name="now">Optional current date/time (defaults to Now in Colombia timezone)</param>
        /// <returns>Configured rule context</returns>
        Task<DeliveryRuleContext> BuildContextAsync(int customerId, DateTime? now = null);

        /// <summary>
        /// Gets all applicable rules for the given suppliers
        /// </summary>
        /// <param name="suppliers">List of supplier identifiers</param>
        /// <returns>List of rules to apply</returns>
        List<SlotRule> GetApplicableRules(HashSet<string> suppliers);
    }
}
