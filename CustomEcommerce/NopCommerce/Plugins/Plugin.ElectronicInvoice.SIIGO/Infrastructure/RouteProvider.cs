using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Nop.Web.Framework.Mvc.Routing;

namespace Plugin.ElectronicInvoice.SIIGO.Infrastructure
{
    public class RouteProvider : IRouteProvider
    {
        public void RegisterRoutes(IEndpointRouteBuilder endpointRouteBuilder)
        {
            // SIIGO Plugin Configuration Routes
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

            // SIIGO Order Processing Routes
            endpointRouteBuilder.MapControllerRoute(
                name: "Plugin.ElectronicInvoice.SIIGO.CheckPaymentMethodBeforeMarkAsPaid",
                pattern: "Admin/SiigoOrder/CheckPaymentMethodBeforeMarkAsPaid",
                defaults: new { controller = "SiigoOrder", action = "CheckPaymentMethodBeforeMarkAsPaid" },
                constraints: new { area = "Admin" });

            endpointRouteBuilder.MapControllerRoute(
                name: "Plugin.ElectronicInvoice.SIIGO.MarkAsPaidWithSubOption",
                pattern: "Admin/SiigoOrder/MarkAsPaidWithSubOption",
                defaults: new { controller = "SiigoOrder", action = "MarkAsPaidWithSubOption" },
                constraints: new { area = "Admin" });

            endpointRouteBuilder.MapControllerRoute(
                name: "Plugin.ElectronicInvoice.SIIGO.GetOrderPaymentSubOptionInfo",
                pattern: "Admin/SiigoOrder/GetOrderPaymentSubOptionInfo",
                defaults: new { controller = "SiigoOrder", action = "GetOrderPaymentSubOptionInfo" },
                constraints: new { area = "Admin" });

            endpointRouteBuilder.MapControllerRoute(
                name: "Plugin.ElectronicInvoice.SIIGO.GetLocalizedResources",
                pattern: "Admin/SiigoOrder/GetLocalizedResources",
                defaults: new { controller = "SiigoOrder", action = "GetLocalizedResources" },
                constraints: new { area = "Admin" });
        }

        public int Priority => 0;
    }
}
