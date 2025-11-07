using Nop.Core.Configuration;

namespace Nop.Plugin.Misc.DeliveryTimePicker
{
    /// <summary>
    /// Represents the settings for the Delivery Time Picker plugin
    /// </summary>
    public class DeliveryTimePickerSettings : ISettings
    {
        /// <summary>
        /// Gets or sets a value indicating whether the plugin is enabled
        /// </summary>
        public bool Enabled { get; set; }

        /// <summary>
        /// Gets or sets the cutoff time (in hours, 24h format) for same-day delivery
        /// Default: 13 (1:00 PM)
        /// </summary>
        public int CutoffHour { get; set; } = 13;

        /// <summary>
        /// Gets or sets the maximum capacity per time slot (global)
        /// Default: 3
        /// </summary>
        public int MaxCapacityPerSlot { get; set; } = 3;

        /// <summary>
        /// Gets or sets a value indicating whether weekends are disabled
        /// </summary>
        public bool DisableWeekends { get; set; } = true;

        /// <summary>
        /// Gets or sets the time zone ID (e.g., "America/Bogota")
        /// Default: Uses system timezone or "SA Pacific Standard Time" (Colombia)
        /// </summary>
        public string TimeZoneId { get; set; } = "SA Pacific Standard Time";

        /// <summary>
        /// Gets or sets the prefix to identify "Éxito" products in SKU
        /// Default: "EXITO"
        /// </summary>
        public string ExitoProductSkuPrefix { get; set; } = "EXITO";

        /// <summary>
        /// Gets or sets a value indicating whether to automatically fetch holidays from external service
        /// </summary>
        public bool AutoFetchHolidays { get; set; } = true;

        /// <summary>
        /// Gets or sets the country code for holiday fetching (ISO 3166-1 alpha-2)
        /// Default: "CO" (Colombia)
        /// </summary>
        public string HolidayCountryCode { get; set; } = "CO";

        /// <summary>
        /// Gets or sets the reservation timeout in minutes
        /// After this time, unreserved slots will be released automatically
        /// Default: 30 minutes
        /// </summary>
        public int ReservationTimeoutMinutes { get; set; } = 30;
    }
}
