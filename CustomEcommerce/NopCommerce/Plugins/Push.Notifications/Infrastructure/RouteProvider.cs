using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Nop.Web.Framework.Mvc.Routing;

namespace Nop.Plugin.Misc.PushNotifications.Infrastructure
{
    public class RouteProvider : IRouteProvider
    {
        public void RegisterRoutes(IEndpointRouteBuilder endpointRouteBuilder)
        {
            endpointRouteBuilder.MapControllerRoute(
                "Plugin.Misc.PushNotifications.FirebaseMessagingSw",
                "firebase-messaging-sw.js",
                new { controller = "PushNotificationsJs", action = "FirebaseMessagingSw" });

            endpointRouteBuilder.MapControllerRoute(
                "Plugin.Misc.PushNotifications.WebPushSw",
                "webpush-sw.js",
                new { controller = "PushNotificationsJs", action = "WebPushSw" });
        }

        public int Priority => 0;
    }
}