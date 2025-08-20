using Nop.Plugin.Misc.PushNotifications.Domain;

namespace Nop.Plugin.Misc.PushNotifications.Models
{
    public class PushSubscriptionModel
    {
        public string Token { get; set; }
        public NotificationType Type { get; set; }
        public string UserAgent { get; set; }
        public string Endpoint { get; set; }
        public string P256dh { get; set; }
        public string Auth { get; set; }
    }
}
