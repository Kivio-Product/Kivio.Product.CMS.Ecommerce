using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Nop.Web.Framework.Mvc.Routing;

namespace Nop.Plugin.Payments.MercadoPago
{
    public partial class RouteProvider : IRouteProvider
    {
        public void RegisterRoutes(IEndpointRouteBuilder endpointRouteBuilder)
        {
            endpointRouteBuilder.MapControllerRoute("Plugin.Payments.MercadoPago.Return", "Plugins/PaymentMercadoPago/Return",
                new { controller = "PaymentMercadoPago", action = "Return" }); 
            endpointRouteBuilder.MapControllerRoute("Plugin.Payments.MercadoPago.ProcessingPayment", "Plugins/PaymentMercadoPago/Confirm",
                new { controller = "PaymentMercadoPago", action = "Confirm" });
        }

        public int Priority
        {
            get { return -1; }
        }
    }
}
