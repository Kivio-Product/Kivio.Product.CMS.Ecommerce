using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;
using Nop.Web.Framework.Models;
using Nop.Web.Framework.Mvc.ModelBinding;

namespace Nop.Plugin.Misc.DeliveryTimePicker.Models
{
    public record DeliveryTimeSlotModel : BaseNopEntityModel
    {
        [NopResourceDisplayName("Plugins.Misc.DeliveryTimePicker.TimeSlots.Fields.DayOfWeek")]
        [Required]
        public int DayOfWeek { get; set; }

        public string DayOfWeekName { get; set; }

        [NopResourceDisplayName("Plugins.Misc.DeliveryTimePicker.TimeSlots.Fields.StartTime")]
        public string StartTime { get; set; }

        [NopResourceDisplayName("Plugins.Misc.DeliveryTimePicker.TimeSlots.Fields.StartTime")]
        [Required(ErrorMessage = "La hora de inicio es requerida")]
        public string StartTimeString { get; set; } = "09:00";

        [NopResourceDisplayName("Plugins.Misc.DeliveryTimePicker.TimeSlots.Fields.EndTime")]
        public string EndTime { get; set; }

        [NopResourceDisplayName("Plugins.Misc.DeliveryTimePicker.TimeSlots.Fields.EndTime")]
        [Required(ErrorMessage = "La hora de fin es requerida")]
        public string EndTimeString { get; set; } = "11:00";

        [NopResourceDisplayName("Plugins.Misc.DeliveryTimePicker.TimeSlots.Fields.IsEnabled")]
        public bool IsEnabled { get; set; } = true;

        [NopResourceDisplayName("Plugins.Misc.DeliveryTimePicker.TimeSlots.Fields.MaxCapacity")]
        public int? MaxCapacity { get; set; }

        [NopResourceDisplayName("Plugins.Misc.DeliveryTimePicker.TimeSlots.Fields.DisplayOrder")]
        public int DisplayOrder { get; set; }

        public IList<SelectListItem> AvailableDaysOfWeek { get; set; } = new List<SelectListItem>();
    }
}
