using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Nop.Web.Framework.Mvc.Routing;

namespace Nop.Plugin.Misc.DeliveryTimePicker.Infrastructure
{
    /// <summary>
    /// Represents plugin route provider
    /// </summary>
    public class RouteProvider : IRouteProvider
    {
        /// <summary>
        /// Register routes
        /// </summary>
        /// <param name="endpointRouteBuilder">Route builder</param>
        public void RegisterRoutes(IEndpointRouteBuilder endpointRouteBuilder)
        {
            endpointRouteBuilder.MapControllerRoute(
                name: "Plugin.Misc.DeliveryTimePicker.GetAvailableSlots",
                pattern: "DeliveryTimePublic/GetAvailableSlots",
                defaults: new { controller = "DeliveryTimePublic", action = "GetAvailableSlots" });

            endpointRouteBuilder.MapControllerRoute(
                name: "Plugin.Misc.DeliveryTimePicker.ReserveSlot",
                pattern: "DeliveryTimePublic/ReserveSlot",
                defaults: new { controller = "DeliveryTimePublic", action = "ReserveSlot" });

            endpointRouteBuilder.MapControllerRoute(
                name: "Plugin.Misc.DeliveryTimePicker.ConfirmReservation",
                pattern: "DeliveryTimePublic/ConfirmReservation",
                defaults: new { controller = "DeliveryTimePublic", action = "ConfirmReservation" });

            endpointRouteBuilder.MapControllerRoute(
                name: "Plugin.Misc.DeliveryTimePicker.ReleaseReservation",
                pattern: "DeliveryTimePublic/ReleaseReservation",
                defaults: new { controller = "DeliveryTimePublic", action = "ReleaseReservation" });
        }

        /// <summary>
        /// Gets a priority of route provider
        /// </summary>
        public int Priority => 0;
    }
}
