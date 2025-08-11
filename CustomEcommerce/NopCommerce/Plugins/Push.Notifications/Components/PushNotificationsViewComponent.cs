using Microsoft.AspNetCore.Mvc;
using Nop.Web.Framework.Components;
using Nop.Plugin.Misc.PushNotifications.Models;
using Nop.Services.Configuration;

namespace Nop.Plugin.Misc.PushNotifications.Components
{
    [ViewComponent(Name = "PushNotifications")]
    public class PushNotificationsViewComponent : NopViewComponent
    {
        private readonly ISettingService _settingService;

        public PushNotificationsViewComponent(ISettingService settingService)
        {
            _settingService = settingService;
        }

        public IViewComponentResult Invoke(string widgetZone, object additionalData)
        {
            var settings = _settingService.LoadSetting<PushNotificationsSettings>();
            var model = new PublicInfoModel
            {
                VapidPublicKey = settings.VapidPublicKey
            };
            return View("~/Plugins/Misc.PushNotifications/Views/Public.cshtml", model);
        }
    }
}
