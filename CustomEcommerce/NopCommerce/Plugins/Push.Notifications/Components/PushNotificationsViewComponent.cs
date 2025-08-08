using Microsoft.AspNetCore.Mvc;
using Nop.Web.Framework.Components;

namespace Nop.Plugin.Misc.PushNotifications.Components
{
    [ViewComponent(Name = "PushNotifications")]
    public class PushNotificationsViewComponent : NopViewComponent
    {
        public IViewComponentResult Invoke(string widgetZone, object additionalData)
        {
            return View("~/Plugins/Misc.PushNotifications/Views/Public.cshtml");
        }
    }
}
