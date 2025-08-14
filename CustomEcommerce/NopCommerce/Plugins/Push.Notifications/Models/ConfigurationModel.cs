using Nop.Web.Framework.Models;
using Nop.Web.Framework.Mvc.ModelBinding;

namespace Nop.Plugin.Misc.PushNotifications.Models
{
    public record ConfigurationModel : BaseNopModel
    {
        [NopResourceDisplayName("Plugins.Misc.PushNotifications.Settings.FirebaseCredentials")]
        public string FirebaseCredentials { get; set; }

        [NopResourceDisplayName("Plugins.Misc.PushNotifications.Settings.VapidPublicKey")]
        public string VapidPublicKey { get; set; }

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

    // Scheduling
    [NopResourceDisplayName("Plugins.Misc.PushNotifications.Settings.AllowedDays")] 
    public string AllowedDays { get; set; }
    [NopResourceDisplayName("Plugins.Misc.PushNotifications.Settings.AllowedHours")] 
    public string AllowedHours { get; set; }
    [NopResourceDisplayName("Plugins.Misc.PushNotifications.Settings.UseUtcTime")] 
    public bool UseUtcTime { get; set; }

    [NopResourceDisplayName("Plugins.Misc.PushNotifications.Settings.MinHoursBetweenNotifications")] 
    public int MinHoursBetweenNotifications { get; set; }
    }
}
