using Nop.Core;
using System;

namespace Nop.Plugin.Misc.PushNotifications.Domain
{
    public class PushSubscription : BaseEntity
    {
        public int CustomerId { get; set; }
        public string Token { get; set; }
        public DateTime CreatedOnUtc { get; set; }
        public string Type { get; set; }
        public string UserAgent { get; set; }
        public string Endpoint { get; set; }
        public string P256dh { get; set; }
        public string Auth { get; set; }
    }
}
