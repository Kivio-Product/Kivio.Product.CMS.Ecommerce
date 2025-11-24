using System;
using Nop.Core;

namespace Nop.Plugin.Misc.DeliveryTimePicker.Domain
{
    /// <summary>
    /// Represents a delivery time reservation for an order
    /// </summary>
    public class DeliveryTimeReservation : BaseEntity
    {
        /// <summary>
        /// Gets or sets the order ID
        /// </summary>
        public int OrderId { get; set; }

        /// <summary>
        /// Gets or sets the delivery date
        /// </summary>
        public DateTime DeliveryDate { get; set; }

        /// <summary>
        /// Gets or sets the minimum delivery time
        /// </summary>
        public TimeSpan MinDeliveryTime { get; set; }

        /// <summary>
        /// Gets or sets the maximum delivery time
        /// </summary>
        public TimeSpan MaxDeliveryTime { get; set; }

        /// <summary>
        /// Gets or sets the time slot ID (if applicable)
        /// </summary>
        public int? TimeSlotId { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether this reservation is confirmed
        /// </summary>
        public bool IsConfirmed { get; set; }

        /// <summary>
        /// Gets or sets the customer ID
        /// </summary>
        public int CustomerId { get; set; }

        /// <summary>
        /// Gets or sets the date created (UTC)
        /// </summary>
        public DateTime CreatedOnUtc { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Gets or sets the date reserved until (for temporary reservations)
        /// </summary>
        public DateTime? ReservedUntilUtc { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether this reservation has Éxito products
        /// </summary>
        public bool HasExitoProducts { get; set; }
    }
}
