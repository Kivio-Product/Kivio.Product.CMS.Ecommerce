using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Nop.Core;
using Nop.Data;
using Nop.Plugin.Misc.DeliveryTimePicker.Components;
using Nop.Plugin.Misc.DeliveryTimePicker.Domain;
using Nop.Services.Cms;
using Nop.Services.Configuration;
using Nop.Services.Localization;
using Nop.Services.Plugins;
using Nop.Web.Framework.Infrastructure;

namespace Nop.Plugin.Misc.DeliveryTimePicker
{
    /// <summary>
    /// Delivery Time Picker plugin
    /// </summary>
    public class DeliveryTimePickerPlugin : BasePlugin, IWidgetPlugin
    {
        #region Fields

        private readonly ISettingService _settingService;
        private readonly ILocalizationService _localizationService;
        private readonly IWebHelper _webHelper;
        private readonly IRepository<DeliveryTimeSlot> _timeSlotRepository;

        #endregion

        #region Ctor

        public DeliveryTimePickerPlugin(
            ISettingService settingService,
            ILocalizationService localizationService,
            IWebHelper webHelper,
            IRepository<DeliveryTimeSlot> timeSlotRepository)
        {
            _settingService = settingService;
            _localizationService = localizationService;
            _webHelper = webHelper;
            _timeSlotRepository = timeSlotRepository;
        }

        #endregion

        #region Properties

        /// <summary>
        /// Gets a value indicating whether to hide this plugin on the widget list page in the admin area
        /// </summary>
        public bool HideInWidgetList => false;

        #endregion

        #region Methods

        /// <summary>
        /// Gets widget zones where this widget should be rendered
        /// </summary>
        /// <returns>
        /// A task that represents the asynchronous operation
        /// The task result contains the widget zones
        /// </returns>
        public Task<IList<string>> GetWidgetZonesAsync()
        {
            return Task.FromResult<IList<string>>(new List<string>
            {
                PublicWidgetZones.CheckoutShippingMethodBottom,      // Normal checkout
                PublicWidgetZones.OpCheckoutShippingMethodBottom     // One Page Checkout
            });
        }

        /// <summary>
        /// Gets a type of a view component for displaying widget
        /// </summary>
        /// <param name="widgetZone">Name of the widget zone</param>
        /// <returns>View component type</returns>
        public Type GetWidgetViewComponent(string widgetZone)
        {
            return typeof(DeliveryTimePickerViewComponent);
        }

        /// <summary>
        /// Gets a configuration page URL
        /// </summary>
        public override string GetConfigurationPageUrl()
        {
            return $"{_webHelper.GetStoreLocation()}Admin/DeliveryTimePicker/Configure";
        }

        /// <summary>
        /// Install the plugin
        /// </summary>
        /// <returns>A task that represents the asynchronous operation</returns>
        public override async Task InstallAsync()
        {
            // Settings
            var settings = new DeliveryTimePickerSettings
            {
                Enabled = true,
                CutoffHour = 13,
                MaxCapacityPerSlot = 3,
                DisableWeekends = true,
                TimeZoneId = "SA Pacific Standard Time", // Colombia timezone
                ExitoProductSkuPrefix = "EXITO",
                AutoFetchHolidays = true,
                HolidayCountryCode = "CO",
                ReservationTimeoutMinutes = 30
            };

            await _settingService.SaveSettingAsync(settings);

            // Localization
            await _localizationService.AddOrUpdateLocaleResourceAsync(new Dictionary<string, string>
            {
                ["Plugins.Misc.DeliveryTimePicker.FriendlyName"] = "Delivery Time Picker",
                ["Plugins.Misc.DeliveryTimePicker.Description"] = "Allows customers to select delivery date and time during checkout",
                
                // Configuration
                ["Plugins.Misc.DeliveryTimePicker.Fields.Enabled"] = "Enabled",
                ["Plugins.Misc.DeliveryTimePicker.Fields.Enabled.Hint"] = "Check to enable the delivery time picker functionality",
                ["Plugins.Misc.DeliveryTimePicker.Fields.CutoffHour"] = "Cutoff Hour (24h format)",
                ["Plugins.Misc.DeliveryTimePicker.Fields.CutoffHour.Hint"] = "Orders placed before this hour can be delivered the same day (for Éxito products). Default: 13 (1:00 PM)",
                ["Plugins.Misc.DeliveryTimePicker.Fields.MaxCapacityPerSlot"] = "Max Capacity Per Slot",
                ["Plugins.Misc.DeliveryTimePicker.Fields.MaxCapacityPerSlot.Hint"] = "Maximum number of customers per time slot. Default: 3",
                ["Plugins.Misc.DeliveryTimePicker.Fields.DisableWeekends"] = "Disable Weekends",
                ["Plugins.Misc.DeliveryTimePicker.Fields.DisableWeekends.Hint"] = "Check to disable delivery on weekends",
                ["Plugins.Misc.DeliveryTimePicker.Fields.TimeZoneId"] = "Time Zone",
                ["Plugins.Misc.DeliveryTimePicker.Fields.TimeZoneId.Hint"] = "Time zone for delivery scheduling. Default: Colombia (SA Pacific Standard Time)",
                ["Plugins.Misc.DeliveryTimePicker.Fields.ExitoProductSkuPrefix"] = "Éxito Product SKU Prefix",
                ["Plugins.Misc.DeliveryTimePicker.Fields.ExitoProductSkuPrefix.Hint"] = "SKU prefix to identify Éxito products. Default: EXITO",
                ["Plugins.Misc.DeliveryTimePicker.Fields.AutoFetchHolidays"] = "Auto-Fetch Holidays",
                ["Plugins.Misc.DeliveryTimePicker.Fields.AutoFetchHolidays.Hint"] = "Automatically fetch holidays from external service",
                ["Plugins.Misc.DeliveryTimePicker.Fields.HolidayCountryCode"] = "Holiday Country Code",
                ["Plugins.Misc.DeliveryTimePicker.Fields.HolidayCountryCode.Hint"] = "ISO 3166-1 alpha-2 country code for holidays. Default: CO (Colombia)",
                ["Plugins.Misc.DeliveryTimePicker.Fields.ReservationTimeoutMinutes"] = "Reservation Timeout (minutes)",
                ["Plugins.Misc.DeliveryTimePicker.Fields.ReservationTimeoutMinutes.Hint"] = "How long to hold a temporary reservation. Default: 30 minutes",
                
                // Time Slots
                ["Plugins.Misc.DeliveryTimePicker.TimeSlots"] = "Time Slots",
                ["Plugins.Misc.DeliveryTimePicker.TimeSlots.AddNew"] = "Add New Time Slot",
                ["Plugins.Misc.DeliveryTimePicker.TimeSlots.Edit"] = "Edit Time Slot",
                ["Plugins.Misc.DeliveryTimePicker.TimeSlots.Fields.DayOfWeek"] = "Day of Week",
                ["Plugins.Misc.DeliveryTimePicker.TimeSlots.Fields.DayOfWeek.Hint"] = "Select day of week or 'All Days' (-1)",
                ["Plugins.Misc.DeliveryTimePicker.TimeSlots.Fields.StartTime"] = "Start Time",
                ["Plugins.Misc.DeliveryTimePicker.TimeSlots.Fields.StartTime.Hint"] = "Slot start time",
                ["Plugins.Misc.DeliveryTimePicker.TimeSlots.Fields.EndTime"] = "End Time",
                ["Plugins.Misc.DeliveryTimePicker.TimeSlots.Fields.EndTime.Hint"] = "Slot end time",
                ["Plugins.Misc.DeliveryTimePicker.TimeSlots.Fields.IsEnabled"] = "Enabled",
                ["Plugins.Misc.DeliveryTimePicker.TimeSlots.Fields.IsEnabled.Hint"] = "Is this slot enabled?",
                ["Plugins.Misc.DeliveryTimePicker.TimeSlots.Fields.MaxCapacity"] = "Max Capacity",
                ["Plugins.Misc.DeliveryTimePicker.TimeSlots.Fields.MaxCapacity.Hint"] = "Override global capacity for this slot (leave empty to use global setting)",
                ["Plugins.Misc.DeliveryTimePicker.TimeSlots.Fields.DisplayOrder"] = "Display Order",
                ["Plugins.Misc.DeliveryTimePicker.TimeSlots.Fields.DisplayOrder.Hint"] = "Display order",
                
                // Holidays
                ["Plugins.Misc.DeliveryTimePicker.Holidays"] = "Holidays",
                ["Plugins.Misc.DeliveryTimePicker.Holidays.AddNew"] = "Add Holiday",
                ["Plugins.Misc.DeliveryTimePicker.Holidays.Import"] = "Import Holidays",
                ["Plugins.Misc.DeliveryTimePicker.Holidays.Imported"] = "Holidays imported successfully",
                ["Plugins.Misc.DeliveryTimePicker.Holidays.ImportError"] = "Error importing holidays",
                ["Plugins.Misc.DeliveryTimePicker.Holidays.Fields.Date"] = "Date",
                ["Plugins.Misc.DeliveryTimePicker.Holidays.Fields.Date.Hint"] = "Holiday date",
                ["Plugins.Misc.DeliveryTimePicker.Holidays.Fields.Name"] = "Name",
                ["Plugins.Misc.DeliveryTimePicker.Holidays.Fields.Name.Hint"] = "Holiday name",
                ["Plugins.Misc.DeliveryTimePicker.Holidays.Fields.IsRecurring"] = "Recurring",
                ["Plugins.Misc.DeliveryTimePicker.Holidays.Fields.IsRecurring.Hint"] = "Does this holiday repeat every year?",
                ["Plugins.Misc.DeliveryTimePicker.Holidays.Fields.CountryCode"] = "Country Code",
                ["Plugins.Misc.DeliveryTimePicker.Holidays.Fields.CountryCode.Hint"] = "ISO country code",
                ["Plugins.Misc.DeliveryTimePicker.Holidays.Fields.IsActive"] = "Active",
                ["Plugins.Misc.DeliveryTimePicker.Holidays.Fields.IsActive.Hint"] = "Is this holiday active?",
                
                // Public facing
                ["Plugins.Misc.DeliveryTimePicker.Public.Title"] = "Fecha y hora de envío",
                ["Plugins.Misc.DeliveryTimePicker.Public.SelectDate"] = "Definir Fecha",
                ["Plugins.Misc.DeliveryTimePicker.Public.SelectTimeRange"] = "Rango de hora de entrega",
                ["Plugins.Misc.DeliveryTimePicker.Public.MinTime"] = "Horario mínima de entrega",
                ["Plugins.Misc.DeliveryTimePicker.Public.MaxTime"] = "Horario máxima de entrega",
                ["Plugins.Misc.DeliveryTimePicker.Public.NoAvailableSlots"] = "No hay horarios disponibles para la fecha seleccionada",
                ["Plugins.Misc.DeliveryTimePicker.Public.SlotFull"] = "Este horario ya está lleno",
                ["Plugins.Misc.DeliveryTimePicker.Public.SelectDateTime"] = "Por favor seleccione fecha y hora de entrega",
                
                // Messages
                ["Plugins.Misc.DeliveryTimePicker.Messages.Required"] = "Debe seleccionar una fecha y hora de entrega",
                ["Plugins.Misc.DeliveryTimePicker.Messages.SlotNotAvailable"] = "El horario seleccionado ya no está disponible",
                ["Plugins.Misc.DeliveryTimePicker.Messages.InvalidDate"] = "La fecha seleccionada no es válida",
            });

            // Create default time slots
            await CreateDefaultTimeSlotsAsync();

            await base.InstallAsync();
        }

        /// <summary>
        /// Create default time slots for Monday to Friday
        /// </summary>
        private async Task CreateDefaultTimeSlotsAsync()
        {
            var now = DateTime.UtcNow;
            var defaultSlots = new List<DeliveryTimeSlot>();

            // Days: 1 = Monday, 2 = Tuesday, 3 = Wednesday, 4 = Thursday, 5 = Friday
            for (int dayOfWeek = 1; dayOfWeek <= 5; dayOfWeek++)
            {
                // Morning slot: 9:00 AM - 11:00 AM
                defaultSlots.Add(new DeliveryTimeSlot
                {
                    DayOfWeek = dayOfWeek,
                    StartTime = new TimeSpan(9, 0, 0),
                    EndTime = new TimeSpan(11, 0, 0),
                    IsEnabled = true,
                    MaxCapacity = null, // Use global setting
                    DisplayOrder = 1,
                    CreatedOnUtc = now,
                    UpdatedOnUtc = now
                });

                // Mid-day slot: 11:00 AM - 1:00 PM
                defaultSlots.Add(new DeliveryTimeSlot
                {
                    DayOfWeek = dayOfWeek,
                    StartTime = new TimeSpan(11, 0, 0),
                    EndTime = new TimeSpan(13, 0, 0),
                    IsEnabled = true,
                    MaxCapacity = null,
                    DisplayOrder = 2,
                    CreatedOnUtc = now,
                    UpdatedOnUtc = now
                });

                // Afternoon slot: 2:00 PM - 4:00 PM
                defaultSlots.Add(new DeliveryTimeSlot
                {
                    DayOfWeek = dayOfWeek,
                    StartTime = new TimeSpan(14, 0, 0),
                    EndTime = new TimeSpan(16, 0, 0),
                    IsEnabled = true,
                    MaxCapacity = null,
                    DisplayOrder = 3,
                    CreatedOnUtc = now,
                    UpdatedOnUtc = now
                });

                // Éxito slot 1: 4:00 PM - 5:00 PM (for same-day delivery)
                defaultSlots.Add(new DeliveryTimeSlot
                {
                    DayOfWeek = dayOfWeek,
                    StartTime = new TimeSpan(16, 0, 0),
                    EndTime = new TimeSpan(17, 0, 0),
                    IsEnabled = true,
                    MaxCapacity = null,
                    DisplayOrder = 4,
                    CreatedOnUtc = now,
                    UpdatedOnUtc = now
                });

                // Éxito slot 2: 5:00 PM - 6:00 PM (for same-day delivery)
                defaultSlots.Add(new DeliveryTimeSlot
                {
                    DayOfWeek = dayOfWeek,
                    StartTime = new TimeSpan(17, 0, 0),
                    EndTime = new TimeSpan(18, 0, 0),
                    IsEnabled = true,
                    MaxCapacity = null,
                    DisplayOrder = 5,
                    CreatedOnUtc = now,
                    UpdatedOnUtc = now
                });
            }

            // Insert all time slots
            await _timeSlotRepository.InsertAsync(defaultSlots);
        }

        /// <summary>
        /// Uninstall the plugin
        /// </summary>
        /// <returns>A task that represents the asynchronous operation</returns>
        public override async Task UninstallAsync()
        {
            // Settings
            await _settingService.DeleteSettingAsync<DeliveryTimePickerSettings>();

            // Localization
            await _localizationService.DeleteLocaleResourcesAsync("Plugins.Misc.DeliveryTimePicker");

            await base.UninstallAsync();
        }

        #endregion
    }
}
