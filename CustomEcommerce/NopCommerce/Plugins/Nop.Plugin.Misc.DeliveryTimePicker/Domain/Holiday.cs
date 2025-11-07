using System;
using Nop.Core;

namespace Nop.Plugin.Misc.DeliveryTimePicker.Domain
{
    /// <summary>
    /// Represents a holiday or non-working day
    /// </summary>
    public class Holiday : BaseEntity
    {
        /// <summary>
        /// Gets or sets the holiday date
        /// </summary>
        public DateTime Date { get; set; }

        /// <summary>
        /// Gets or sets the holiday name
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether this is a recurring holiday (e.g., every year on same date)
        /// </summary>
        public bool IsRecurring { get; set; }

        /// <summary>
        /// Gets or sets the country code (ISO 3166-1 alpha-2)
        /// </summary>
        public string CountryCode { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether this holiday was automatically imported
        /// </summary>
        public bool IsAutoImported { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether this holiday is active
        /// </summary>
        public bool IsActive { get; set; } = true;

        /// <summary>
        /// Gets or sets the date created (UTC)
        /// </summary>
        public DateTime CreatedOnUtc { get; set; } = DateTime.UtcNow;
    }
}
