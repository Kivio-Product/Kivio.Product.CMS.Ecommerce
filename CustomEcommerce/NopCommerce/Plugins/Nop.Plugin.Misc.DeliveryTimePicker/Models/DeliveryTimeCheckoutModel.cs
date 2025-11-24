namespace Nop.Plugin.Misc.DeliveryTimePicker.Models
{
    /// <summary>
    /// Model for delivery time selection in checkout
    /// </summary>
    public class DeliveryTimeCheckoutModel
    {
        /// <summary>
        /// Selected delivery date (YYYY-MM-DD format)
        /// </summary>
        public string DeliveryDate { get; set; }

        /// <summary>
        /// Selected minimum delivery time (HH:mm format)
        /// </summary>
        public string MinDeliveryTime { get; set; }

        /// <summary>
        /// Selected maximum delivery time (HH:mm format)
        /// </summary>
        public string MaxDeliveryTime { get; set; }

        /// <summary>
        /// Reservation ID if a slot was reserved
        /// </summary>
        public int? ReservationId { get; set; }
    }
}
