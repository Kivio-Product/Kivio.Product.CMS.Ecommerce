using Nop.Web.Framework.Models;
using Nop.Web.Framework.Mvc.ModelBinding;

namespace Nop.Plugin.Misc.PushNotifications.Models
{
    public record ConfigurationModel : BaseNopModel
    {
        [NopResourceDisplayName("Plugins.Misc.PushNotifications.Settings.FirebaseCredentials")]
        public string FirebaseCredentials { get; set; }

        // Firebase VAPID Keys
        [NopResourceDisplayName("Plugins.Misc.PushNotifications.Settings.FirebaseVapidPublicKey")]
        public string FirebaseVapidPublicKey { get; set; }

        // Web Push VAPID Keys
        [NopResourceDisplayName("Plugins.Misc.PushNotifications.Settings.WebPushVapidPublicKey")]
        public string WebPushVapidPublicKey { get; set; }

        [NopResourceDisplayName("Plugins.Misc.PushNotifications.Settings.WebPushVapidPrivateKey")]
        public string WebPushVapidPrivateKey { get; set; }

        [NopResourceDisplayName("Plugins.Misc.PushNotifications.Settings.TestNotificationTitle")]
        public string TestNotificationTitle { get; set; }

        [NopResourceDisplayName("Plugins.Misc.PushNotifications.Settings.TestNotificationMessage")]
        public string TestNotificationMessage { get; set; }

        [NopResourceDisplayName("Plugins.Misc.PushNotifications.Settings.FirebaseConfig")]
        public string FirebaseConfig { get; set; }

        [NopResourceDisplayName("Plugins.Misc.PushNotifications.Settings.GeminiApiKey")]
        public string GeminiApiKey { get; set; }

        [NopResourceDisplayName("Plugins.Misc.PushNotifications.Settings.EnableNewProductStrategy")]
        public bool EnableNewProductStrategy { get; set; }

        [NopResourceDisplayName("Plugins.Misc.PushNotifications.Settings.EnableCategoryStrategy")]
        public bool EnableCategoryStrategy { get; set; }

        [NopResourceDisplayName("Plugins.Misc.PushNotifications.Settings.EnableCustomStrategy")]
        public bool EnableCustomStrategy { get; set; }

        [NopResourceDisplayName("Plugins.Misc.PushNotifications.Settings.AIPromptBase")]
        public string AIPromptBase { get; set; }

        [NopResourceDisplayName("Plugins.Misc.PushNotifications.Settings.CustomStrategyPrompt")]
        public string CustomStrategyPrompt { get; set; }

        [NopResourceDisplayName("Plugins.Misc.PushNotifications.Settings.NotificationIconUrl")]
        public string NotificationIconUrl { get; set; }

        // Scheduling
        [NopResourceDisplayName("Plugins.Misc.PushNotifications.Settings.AllowedDays")] 
        public string AllowedDays { get; set; }
        [NopResourceDisplayName("Plugins.Misc.PushNotifications.Settings.AllowedHours")] 
        public string AllowedHours { get; set; }
        [NopResourceDisplayName("Plugins.Misc.PushNotifications.Settings.UseUtcTime")] 
        public bool UseUtcTime { get; set; }

        [NopResourceDisplayName("Plugins.Misc.PushNotifications.Settings.MinHoursBetweenNotifications")] 
        public int MinHoursBetweenNotifications { get; set; }

        // Web Push settings
        [NopResourceDisplayName("Plugins.Misc.PushNotifications.Settings.WebPushSubject")]
        public string WebPushSubject { get; set; }

        [NopResourceDisplayName("Plugins.Misc.PushNotifications.Settings.ForceWebPushForIOS")]
        public bool ForceWebPushForIOS { get; set; }
    }
}
