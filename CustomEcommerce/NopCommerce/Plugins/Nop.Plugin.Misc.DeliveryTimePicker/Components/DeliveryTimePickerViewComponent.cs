using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Nop.Core;
using Nop.Plugin.Misc.DeliveryTimePicker.Models;
using Nop.Plugin.Misc.DeliveryTimePicker.Services;
using Nop.Plugin.Misc.DeliveryTimePicker.Services.Rules;
using Nop.Services.Configuration;
using Nop.Web.Framework.Components;

namespace Nop.Plugin.Misc.DeliveryTimePicker.Components
{
    [ViewComponent(Name = "DeliveryTimePicker")]
    public class DeliveryTimePickerViewComponent : NopViewComponent
    {
        private readonly IDeliveryRuleService _deliveryRuleService;
        private readonly IWorkContext _workContext;
        private readonly ISettingService _settingService;
        private readonly DeliveryTimePickerSettings _settings;

        public DeliveryTimePickerViewComponent(
            IDeliveryRuleService deliveryRuleService,
            IWorkContext workContext,
            ISettingService settingService,
            DeliveryTimePickerSettings settings)
        {
            _deliveryRuleService = deliveryRuleService;
            _workContext = workContext;
            _settingService = settingService;
            _settings = settings;
        }

        public async Task<IViewComponentResult> InvokeAsync(string widgetZone, object additionalData)
        {
            // Check if plugin is enabled
            if (!_settings.Enabled)
                return Content(string.Empty);

            var customer = await _workContext.GetCurrentCustomerAsync();
            
            // Get suppliers from cart using rule service
            var suppliers = await _deliveryRuleService.GetSuppliersFromCartAsync(customer.Id);
            var hasExitoProducts = suppliers.Contains("EXITO");

            var model = new DeliveryTimePickerPublicModel
            {
                HasExitoProducts = hasExitoProducts,
                CutoffHour = _settings.CutoffHour,
                DisableWeekends = _settings.DisableWeekends
            };

            return View("~/Plugins/Misc.DeliveryTimePicker/Views/DeliveryTimePicker.cshtml", model);
        }
    }
}
