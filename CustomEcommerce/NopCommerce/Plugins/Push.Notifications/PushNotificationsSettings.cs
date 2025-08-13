using Nop.Core.Configuration;

namespace Nop.Plugin.Misc.PushNotifications
{
    public class PushNotificationsSettings : ISettings
    {
        public string FirebaseCredentials { get; set; }
        public string VapidPublicKey { get; set; }
        public string FirebaseConfig { get; set; }
        public string ProductDataSynchronizationTask { get; set; }
        public string GeminiApiKey { get; set; }
        public bool EnableNewProductStrategy { get; set; }
        public bool EnableCategoryStrategy { get; set; }
        public bool EnableCustomStrategy { get; set; }
        public string AIPromptBase { get; set; }
        public string CustomStrategyPrompt { get; set; }
    }
}
