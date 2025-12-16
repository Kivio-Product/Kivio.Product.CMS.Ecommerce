using System;
using System.Collections.Generic;

namespace Nop.Plugin.Misc.DeliveryTimePicker.Services.Rules
{
    /// <summary>
    /// Context containing all information needed to evaluate delivery rules
    /// </summary>
    public class DeliveryRuleContext
    {
        /// <summary>
        /// Current date and time in the configured timezone
        /// </summary>
        public DateTime Now { get; init; }

        /// <summary>
        /// Set of supplier identifiers present in the cart (extracted from SKUs)
        /// </summary>
        public HashSet<string> SuppliersInCart { get; init; } = new();

        /// <summary>
        /// Function to check if a given date is a business day (excludes weekends and holidays)
        /// </summary>
        public Func<DateTime, bool> IsBusinessDay { get; init; } = _ => true;

        /// <summary>
        /// Cutoff hour for same-day delivery (e.g., 13 for 1:00 PM)
        /// </summary>
        public int CutoffHour { get; init; }

        /// <summary>
        /// Whether weekends are disabled for delivery
        /// </summary>
        public bool DisableWeekends { get; init; }
        /// <summary>
        /// Hours after which same-day delivery is not available
        /// </summary>
        public int SameDayDeliveryCutoffHour { get; init; }
    }

    /// <summary>
    /// Represents a delivery slot to be evaluated
    /// </summary>
    public record DeliverySlot
    {
        /// <summary>
        /// The date of the delivery
        /// </summary>
        public DateTime Date { get; init; }

        /// <summary>
        /// Start time of the slot
        /// </summary>
        public TimeSpan StartTime { get; init; }

        /// <summary>
        /// End time of the slot
        /// </summary>
        public TimeSpan EndTime { get; init; }

        /// <summary>
        /// Database ID of the time slot
        /// </summary>
        public int SlotId { get; init; }

        /// <summary>
        /// Day of week for the slot
        /// </summary>
        public int DayOfWeek { get; init; }
    }

    /// <summary>
    /// Result of a rule evaluation
    /// </summary>
    public record RuleResult
    {
        /// <summary>
        /// Whether the slot is allowed by this rule
        /// </summary>
        public bool IsAllowed { get; init; }

        /// <summary>
        /// Optional message explaining why the slot is not allowed
        /// </summary>
        public string Message { get; init; }

        public RuleResult(bool isAllowed, string message = null)
        {
            IsAllowed = isAllowed;
            Message = message;
        }

        /// <summary>
        /// Success result
        /// </summary>
        public static RuleResult Success => new(true);

        /// <summary>
        /// Failure result with message
        /// </summary>
        public static RuleResult Fail(string message) => new(false, message);
    }

    /// <summary>
    /// Delegate for delivery slot rules
    /// </summary>
    public delegate RuleResult SlotRule(DeliveryRuleContext context, DeliverySlot slot);
}
