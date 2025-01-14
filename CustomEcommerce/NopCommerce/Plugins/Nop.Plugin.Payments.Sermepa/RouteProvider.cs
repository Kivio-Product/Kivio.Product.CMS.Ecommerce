using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Nop.Web.Framework.Mvc.Routing;

namespace Nop.Plugin.Payments.Sermepa
{
    public partial class RouteProvider : IRouteProvider
    {
        public void RegisterRoutes(IEndpointRouteBuilder endpointRouteBuilder)
        {
            endpointRouteBuilder.MapControllerRoute("Plugin.Payments.Sermepa.Return", "Plugins/PaymentSermepa/Return",
                new { controller = "PaymentSermepaPublic", action = "Return" });

            endpointRouteBuilder.MapControllerRoute("Plugin.Payments.Sermepa.Error", "Plugins/PaymentSermepa/Error",
                new { controller = "PaymentSermepaPublic", action = "Error" });
        }

        public int Priority
        {
            get
            {
                return 0;
            }
        }
    }
}
