using Nop.Core.Configuration;

namespace Nop.Plugin.Misc.PushNotifications
{
    public class PushNotificationsSettings : ISettings
    {
        public string FirebaseCredentials { get; set; }
        
        // Firebase VAPID Keys
        public string FirebaseVapidPublicKey { get; set; }
        
        // Web Push VAPID Keys
        public string WebPushVapidPublicKey { get; set; }
        public string WebPushVapidPrivateKey { get; set; }
        
        public string FirebaseConfig { get; set; }
        public string ProductDataSynchronizationTask { get; set; }
        public string GeminiApiKey { get; set; }
        public bool EnableNewProductStrategy { get; set; }
        public bool EnableCategoryStrategy { get; set; }
        public bool EnableCustomStrategy { get; set; }
        public string AIPromptBase { get; set; }
        public string CustomStrategyPrompt { get; set; }
        public string NotificationIconUrl { get; set; }
        // Scheduling
        public string AllowedDays { get; set; }
        public string AllowedHours { get; set; }
        public bool UseUtcTime { get; set; }
        // Minimum number of hours to wait before sending another notification (0 disables)
        public int MinHoursBetweenNotifications { get; set; }
        // Web Push settings
        public string WebPushSubject { get; set; }
        public bool ForceWebPushForIOS { get; set; }
    }
}
