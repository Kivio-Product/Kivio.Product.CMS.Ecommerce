using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc.Rendering;
using Nop.Web.Framework.Models;
using Nop.Web.Framework.Mvc.ModelBinding;

namespace Nop.Plugin.Misc.DeliveryTimePicker.Models
{
    public record ConfigurationModel : BaseNopModel
    {
        public ConfigurationModel()
        {
            AvailableTimeZones = new List<SelectListItem>();
        }

        [NopResourceDisplayName("Plugins.Misc.DeliveryTimePicker.Fields.Enabled")]
        public bool Enabled { get; set; }

        [NopResourceDisplayName("Plugins.Misc.DeliveryTimePicker.Fields.CutoffHour")]
        public int CutoffHour { get; set; }

        [NopResourceDisplayName("Plugins.Misc.DeliveryTimePicker.Fields.MaxCapacityPerSlot")]
        public int MaxCapacityPerSlot { get; set; }

        [NopResourceDisplayName("Plugins.Misc.DeliveryTimePicker.Fields.DisableWeekends")]
        public bool DisableWeekends { get; set; }

        [NopResourceDisplayName("Plugins.Misc.DeliveryTimePicker.Fields.TimeZoneId")]
        public string TimeZoneId { get; set; }

        [NopResourceDisplayName("Plugins.Misc.DeliveryTimePicker.Fields.ExitoProductSkuPrefix")]
        public string ExitoProductSkuPrefix { get; set; }

        [NopResourceDisplayName("Plugins.Misc.DeliveryTimePicker.Fields.AutoFetchHolidays")]
        public bool AutoFetchHolidays { get; set; }

        [NopResourceDisplayName("Plugins.Misc.DeliveryTimePicker.Fields.HolidayCountryCode")]
        public string HolidayCountryCode { get; set; }

        [NopResourceDisplayName("Plugins.Misc.DeliveryTimePicker.Fields.ReservationTimeoutMinutes")]
        public int ReservationTimeoutMinutes { get; set; }

        public IList<SelectListItem> AvailableTimeZones { get; set; }
    }
}
