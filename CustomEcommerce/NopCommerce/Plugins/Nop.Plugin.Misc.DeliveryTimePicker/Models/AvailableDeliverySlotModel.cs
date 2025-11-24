using System;
using System.Collections.Generic;

namespace Nop.Plugin.Misc.DeliveryTimePicker.Models
{
    /// <summary>
    /// Model for available delivery slots
    /// </summary>
    public record AvailableDeliverySlotModel
    {
        public DateTime Date { get; set; }
        public string DateFormatted { get; set; }
        public List<TimeSlotOption> TimeSlots { get; set; } = new();
        public bool IsAvailable { get; set; }
        public string UnavailableReason { get; set; }
    }

    /// <summary>
    /// Represents a time slot option
    /// </summary>
    public record TimeSlotOption
    {
        public int? SlotId { get; set; }
        public TimeSpan MinTime { get; set; }
        public TimeSpan MaxTime { get; set; }
        public string DisplayText { get; set; }
        public bool IsAvailable { get; set; }
        public int AvailableCapacity { get; set; }
        public int MaxCapacity { get; set; }
    }
}
