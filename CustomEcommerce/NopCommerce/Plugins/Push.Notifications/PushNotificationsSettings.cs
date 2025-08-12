using Nop.Core.Configuration;

namespace Nop.Plugin.Misc.PushNotifications
{
    public class PushNotificationsSettings : ISettings
    {
        public string FirebaseCredentials { get; set; }
        public string VapidPublicKey { get; set; }
        public string FirebaseConfig { get; set; }
        public string ProductDataSynchronizationTask { get; set; }
    }
}
