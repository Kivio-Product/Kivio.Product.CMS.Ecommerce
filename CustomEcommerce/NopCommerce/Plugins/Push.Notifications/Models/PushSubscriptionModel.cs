namespace Nop.Plugin.Misc.PushNotifications.Models
{
    public class PushSubscriptionModel
    {
        public string Token { get; set; }
        public string Type { get; set; }
        public string UserAgent { get; set; }
        public string Endpoint { get; set; }
        public string P256dh { get; set; }
        public string Auth { get; set; }
    }
}
