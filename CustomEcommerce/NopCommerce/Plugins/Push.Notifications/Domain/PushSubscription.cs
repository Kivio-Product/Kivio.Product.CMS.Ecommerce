using Nop.Core;
using System;

namespace Nop.Plugin.Misc.PushNotifications.Domain
{
    public class PushSubscription : BaseEntity
    {
        public int CustomerId { get; set; }
        public string Token { get; set; }
        public DateTime CreatedOnUtc { get; set; }
    }
}
