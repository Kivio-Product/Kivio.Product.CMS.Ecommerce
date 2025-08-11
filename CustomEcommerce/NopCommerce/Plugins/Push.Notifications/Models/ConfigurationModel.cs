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
    }
}
