using System;
using Nop.Core;

namespace Nop.Plugin.Misc.DeliveryTimePicker.Domain
{
    /// <summary>
    /// Represents a delivery time slot configuration
    /// </summary>
    public class DeliveryTimeSlot : BaseEntity
    {
        /// <summary>
        /// Gets or sets the day of week (0 = Sunday, 1 = Monday, ..., 6 = Saturday)
        /// -1 means applies to all days
        /// </summary>
        public int DayOfWeek { get; set; }

        /// <summary>
        /// Gets or sets the start time (TimeSpan format: HH:mm)
        /// </summary>
        public TimeSpan StartTime { get; set; }

        /// <summary>
        /// Gets or sets the end time (TimeSpan format: HH:mm)
        /// </summary>
        public TimeSpan EndTime { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether this slot is enabled
        /// </summary>
        public bool IsEnabled { get; set; } = true;

        /// <summary>
        /// Gets or sets the maximum capacity for this specific slot (overrides global setting if set)
        /// Null means use global setting
        /// </summary>
        public int? MaxCapacity { get; set; }

        /// <summary>
        /// Gets or sets the display order
        /// </summary>
        public int DisplayOrder { get; set; }

        /// <summary>
        /// Gets or sets the date created (UTC)
        /// </summary>
        public DateTime CreatedOnUtc { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Gets or sets the date updated (UTC)
        /// </summary>
        public DateTime UpdatedOnUtc { get; set; } = DateTime.UtcNow;
    }
}
