using System;
using Nop.Web.Framework.Models;
using Nop.Web.Framework.Mvc.ModelBinding;

namespace Nop.Plugin.Misc.DeliveryTimePicker.Models
{
    public record HolidayModel : BaseNopEntityModel
    {
        [NopResourceDisplayName("Plugins.Misc.DeliveryTimePicker.Holidays.Fields.Date")]
        public DateTime Date { get; set; }

        [NopResourceDisplayName("Plugins.Misc.DeliveryTimePicker.Holidays.Fields.Name")]
        public string Name { get; set; }

        [NopResourceDisplayName("Plugins.Misc.DeliveryTimePicker.Holidays.Fields.IsRecurring")]
        public bool IsRecurring { get; set; }

        [NopResourceDisplayName("Plugins.Misc.DeliveryTimePicker.Holidays.Fields.CountryCode")]
        public string CountryCode { get; set; }

        [NopResourceDisplayName("Plugins.Misc.DeliveryTimePicker.Holidays.Fields.IsActive")]
        public bool IsActive { get; set; }
    }
}
