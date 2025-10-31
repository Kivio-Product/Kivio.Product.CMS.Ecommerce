using System;

namespace Nop.Plugin.Misc.DeliveryTimePicker.Models
{
    /// <summary>
    /// Model for reserving a delivery time slot
    /// </summary>
    public record ReserveDeliveryTimeRequest
    {
        public DateTime DeliveryDate { get; set; }
        public TimeSpan MinDeliveryTime { get; set; }
        public TimeSpan MaxDeliveryTime { get; set; }
        public int? TimeSlotId { get; set; }
        public int CustomerId { get; set; }
        public bool IsTemporary { get; set; } = true;
    }

    /// <summary>
    /// Response for reservation request
    /// </summary>
    public record ReserveDeliveryTimeResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; }
        public int? ReservationId { get; set; }
    }
}
