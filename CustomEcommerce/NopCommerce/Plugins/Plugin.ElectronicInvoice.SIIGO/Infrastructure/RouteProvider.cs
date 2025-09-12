using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Nop.Web.Framework.Mvc.Routing;

namespace Plugin.ElectronicInvoice.SIIGO.Infrastructure
{
    public class RouteProvider : IRouteProvider
    {
        public void RegisterRoutes(IEndpointRouteBuilder endpointRouteBuilder)
        {
            endpointRouteBuilder.MapControllerRoute(
                name: "Plugin.ElectronicInvoice.SIIGO.Configure",
                pattern: "Admin/Siigo/Configure",
                defaults: new { controller = "Siigo", action = "Configure" },
                constraints: new { area = "Admin" });

            endpointRouteBuilder.MapControllerRoute(
                name: "Plugin.ElectronicInvoice.SIIGO.TestConnection",
                pattern: "Admin/Siigo/TestConnection",
                defaults: new { controller = "Siigo", action = "TestConnection" },
                constraints: new { area = "Admin" });
        }

        public int Priority => 0;
    }
}
