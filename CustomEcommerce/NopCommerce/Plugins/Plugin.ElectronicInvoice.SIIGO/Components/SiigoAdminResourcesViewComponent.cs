using Microsoft.AspNetCore.Mvc;
using Nop.Web.Framework.Components;

namespace Plugin.ElectronicInvoice.SIIGO.Components
{
    [ViewComponent(Name = "SiigoAdminResources")]
    public class SiigoAdminResourcesViewComponent : NopViewComponent
    {
        public IViewComponentResult Invoke(string widgetZone)
        {
            // Only load resources on admin order pages
            if (HttpContext.Request.Path.Value?.Contains("/Admin/Order/Edit/") == true)
            {
                return View("~/Plugins/ElectronicInvoice.SIIGO/Views/Components/SiigoAdminResources/Default.cshtml");
            }

            return Content("");
        }
    }
}