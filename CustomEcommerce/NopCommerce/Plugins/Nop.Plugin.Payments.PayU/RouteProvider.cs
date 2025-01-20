using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Nop.Web.Framework.Mvc.Routing;

namespace Nop.Plugin.Payments.PayU
{
    public partial class RouteProvider : IRouteProvider
    {
        public void RegisterRoutes(IEndpointRouteBuilder endpointRouteBuilder)
        {
            endpointRouteBuilder.MapControllerRoute("Plugin.Payments.PayU.Notify", "Plugins/PaymentPayU/Notify",
                new { controller = "PaymentPayU", action = "Notify" });
            endpointRouteBuilder.MapControllerRoute("Plugin.Payments.PayU.Return", "Plugins/PaymentPayU/Return",
                new { controller = "PaymentPayU", action = "Return" }); 
            endpointRouteBuilder.MapControllerRoute("Plugin.Payments.PayU.ProcessingPayment", "Plugins/PaymentPayU/ProcessingPayment",
                new { controller = "PaymentPayU", action = "ProcessingPayment" });
        }

        public int Priority
        {
            get { return -1; }
        }
    }
}
