using Nop.Core;

namespace Nop.Plugin.Misc.PushNotifications.Domain
{
    public class PushNotificationLog : BaseEntity
    {
        public string StrategyType { get; set; }
        public int EntityId { get; set; }
        public string Title { get; set; }
        public string Body { get; set; }
        public DateTime SentDateUtc { get; set; }
    }
}
