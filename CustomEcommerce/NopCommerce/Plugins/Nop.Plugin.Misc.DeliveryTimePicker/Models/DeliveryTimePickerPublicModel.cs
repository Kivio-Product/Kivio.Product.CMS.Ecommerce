namespace Nop.Plugin.Misc.DeliveryTimePicker.Models
{
    /// <summary>
    /// Represents the public model for the delivery time picker widget
    /// </summary>
    public class DeliveryTimePickerPublicModel
    {
        /// <summary>
        /// Gets or sets a value indicating whether the cart contains EXITO products
        /// </summary>
        public bool HasExitoProducts { get; set; }

        /// <summary>
        /// Gets or sets the cutoff hour for same-day delivery
        /// </summary>
        public int CutoffHour { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether weekends are disabled
        /// </summary>
        public bool DisableWeekends { get; set; }

        /// <summary>
        /// Gets or sets the saved delivery date if user is returning to the step
        /// </summary>
        public string SavedDate { get; set; }

        /// <summary>
        /// Gets or sets the saved minimum delivery time if user is returning to the step
        /// </summary>
        public string SavedMinTime { get; set; }

        /// <summary>
        /// Gets or sets the saved maximum delivery time if user is returning to the step
        /// </summary>
        public string SavedMaxTime { get; set; }
    }
}
