
using Nop.Core;
using Nop.Core.Domain.Cms;
using Nop.Core.Domain.ScheduleTasks;
using Nop.Data;
using Nop.Plugin.Misc.PushNotifications.Components;
using Nop.Services.Cms;
using Nop.Services.Configuration;
using Nop.Services.Localization;
using Nop.Services.Plugins;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Nop.Plugin.Misc.PushNotifications
{
    public class PushNotificationsPlugin : BasePlugin, IWidgetPlugin
    {
        private readonly ISettingService _settingService;
        private readonly ILocalizationService _localizationService;
        private readonly IWebHelper _webHelper;
        private readonly INopDataProvider _dataProvider;

        public PushNotificationsPlugin(ISettingService settingService,
                                      ILocalizationService localizationService,
                                      IWebHelper webHelper,
                                      INopDataProvider dataProvider)
        {
            _settingService = settingService;
            _localizationService = localizationService;
            _webHelper = webHelper;
            _dataProvider = dataProvider;
        }

        public bool HideInWidgetList => false;

        public Task<IList<string>> GetWidgetZonesAsync()
        {
            return Task.FromResult<IList<string>>(new List<string> { "body_end_html_tag_before" });
        }

        public string GetWidgetViewComponentName(string widgetZone)
        {
            return "PushNotifications";
        }
        
        public override string GetConfigurationPageUrl()
        {
            return $"{_webHelper.GetStoreLocation()}Admin/PushNotifications/Configure";
        }

        public override async Task InstallAsync()
        {
            var settings = new PushNotificationsSettings
            {
                FirebaseCredentials = "",
                FirebaseConfig = "",
                FirebaseVapidPublicKey = "",
                WebPushVapidPublicKey = "",
                WebPushVapidPrivateKey = "",
                GeminiApiKey = "Your-Gemini-API-Key-Here",
                EnableNewProductStrategy = true,
                EnableCategoryStrategy = true,
                EnableCustomStrategy = false,
                AIPromptBase = "You are a marketing expert. Create an engaging push notification with a catchy title and compelling body text",
                CustomStrategyPrompt = "Create a push notification to encourage users to visit our ecommerce store. Make it exciting and compelling.",
                AllowedDays = "Mon-Fri",
                AllowedHours = "09:00-21:00",
                UseUtcTime = false,
                WebPushSubject = "mailto:admin@example.com",
                ForceWebPushForIOS = true
            };
            await _settingService.SaveSettingAsync(settings);

            await _localizationService.AddOrUpdateLocaleResourceAsync(new Dictionary<string, string>
            {
                ["Plugins.Misc.PushNotifications.FriendlyName"] = "Push Notifications",
                
                // Configuration Sections
                ["Plugins.Misc.PushNotifications.Settings.FirebaseConfiguration"] = "Firebase Configuration",
                ["Plugins.Misc.PushNotifications.Settings.WebPushConfiguration"] = "Web Push Configuration", 
                ["Plugins.Misc.PushNotifications.Settings.GeminiConfiguration"] = "Gemini AI Configuration",
                ["Plugins.Misc.PushNotifications.Settings.NotificationConfiguration"] = "Notification Configuration",
                ["Plugins.Misc.PushNotifications.Settings.StrategyConfiguration"] = "Strategy Configuration",
                ["Plugins.Misc.PushNotifications.Settings.Scheduling"] = "Scheduling",
                
                // Firebase Settings
                ["Plugins.Misc.PushNotifications.Settings.FirebaseCredentials"] = "Firebase Credentials (JSON)",
                ["Plugins.Misc.PushNotifications.Settings.FirebaseCredentials.Hint"] = "Paste the JSON content of your Firebase service account credentials file here.",
                ["Plugins.Misc.PushNotifications.Settings.FirebaseVapidPublicKey"] = "Firebase VAPID Public Key",
                ["Plugins.Misc.PushNotifications.Settings.FirebaseVapidPublicKey.Hint"] = "Enter your Firebase VAPID public key for FCM notifications.",
                ["Plugins.Misc.PushNotifications.Settings.FirebaseConfig"] = "Firebase Config (JSON)",
                ["Plugins.Misc.PushNotifications.Settings.FirebaseConfig.Hint"] = "Paste the JSON content of your Firebase config for web.",
                
                // Web Push Settings
                ["Plugins.Misc.PushNotifications.Settings.WebPushVapidPublicKey"] = "Web Push VAPID Public Key",
                ["Plugins.Misc.PushNotifications.Settings.WebPushVapidPublicKey.Hint"] = "Enter your Web Push VAPID public key for native Web Push notifications.",
                ["Plugins.Misc.PushNotifications.Settings.WebPushVapidPrivateKey"] = "Web Push VAPID Private Key",
                ["Plugins.Misc.PushNotifications.Settings.WebPushVapidPrivateKey.Hint"] = "Enter your Web Push VAPID private key for Web Push authentication. Keep this secure!",
                ["Plugins.Misc.PushNotifications.Settings.WebPushSubject"] = "Web Push Subject",
                ["Plugins.Misc.PushNotifications.Settings.WebPushSubject.Hint"] = "Subject for VAPID headers. Use mailto:email@domain.com or https://yourdomain.com format.",
                ["Plugins.Misc.PushNotifications.Settings.ForceWebPushForIOS"] = "Force Web Push for iOS",
                ["Plugins.Misc.PushNotifications.Settings.ForceWebPushForIOS.Hint"] = "Force iOS devices to use Web Push instead of FCM (recommended for iOS Safari compatibility).",
                
                // Gemini Settings
                ["Plugins.Misc.PushNotifications.Settings.GeminiApiKey"] = "Gemini API Key (Required restart application)",
                ["Plugins.Misc.PushNotifications.Settings.GeminiApiKey.Hint"] = "Enter your API key for the Gemini service.",
                
                // Notification Settings
                ["Plugins.Misc.PushNotifications.Settings.NotificationIconUrl"] = "Notification Icon URL",
                ["Plugins.Misc.PushNotifications.Settings.NotificationIconUrl.Hint"] = "URL of the icon to be displayed in push notifications.",
                
                // Strategy Settings
                ["Plugins.Misc.PushNotifications.Settings.EnableNewProductStrategy"] = "Enable New Product Strategy",
                ["Plugins.Misc.PushNotifications.Settings.EnableNewProductStrategy.Hint"] = "Enable automatic notifications for new products with discounts.",
                ["Plugins.Misc.PushNotifications.Settings.EnableCategoryStrategy"] = "Enable Category Strategy",
                ["Plugins.Misc.PushNotifications.Settings.EnableCategoryStrategy.Hint"] = "Enable automatic notifications for product categories.",
                ["Plugins.Misc.PushNotifications.Settings.EnableCustomStrategy"] = "Enable Custom Strategy",
                ["Plugins.Misc.PushNotifications.Settings.EnableCustomStrategy.Hint"] = "Enable custom notifications based on AI prompts.",
                ["Plugins.Misc.PushNotifications.Settings.AIPromptBase"] = "Base AI Prompt",
                ["Plugins.Misc.PushNotifications.Settings.AIPromptBase.Hint"] = "Base prompt that will be used by AI to generate notifications for products and categories.",
                ["Plugins.Misc.PushNotifications.Settings.CustomStrategyPrompt"] = "Custom Strategy Prompt",
                ["Plugins.Misc.PushNotifications.Settings.CustomStrategyPrompt.Hint"] = "Specific prompt for the custom strategy notifications.",
                
                // Scheduling Settings
                ["Plugins.Misc.PushNotifications.Settings.AllowedDays"] = "Allowed Days",
                ["Plugins.Misc.PushNotifications.Settings.AllowedDays.Hint"] = "Days when notifications can be sent. Examples: 'Mon-Fri', 'Sat,Sun', 'Mon,Wed,Fri'",
                ["Plugins.Misc.PushNotifications.Settings.AllowedHours"] = "Allowed Hours",
                ["Plugins.Misc.PushNotifications.Settings.AllowedHours.Hint"] = "One or more time ranges in 24h format, comma-separated. Example: '09:00-12:00, 18:00-21:00'",
                ["Plugins.Misc.PushNotifications.Settings.UseUtcTime"] = "Use UTC Time",
                ["Plugins.Misc.PushNotifications.Settings.UseUtcTime.Hint"] = "If enabled, the schedule is evaluated using UTC; otherwise, server local time is used.",
                ["Plugins.Misc.PushNotifications.Settings.MinHoursBetweenNotifications"] = "Minimum Hours Between Notifications",
                ["Plugins.Misc.PushNotifications.Settings.MinHoursBetweenNotifications.Hint"] = "Minimum hours that must pass between consecutive notifications. 0 means no limit.",
                
                // Test Notification Settings
                ["Plugins.Misc.PushNotifications.Settings.SendTestNotification"] = "Send Test Notification",
                ["Plugins.Misc.PushNotifications.Settings.SendTestNotification.Hint"] = "Send a test notification to all subscribed devices.",
                ["Plugins.Misc.PushNotifications.Settings.TestNotificationTitle"] = "Test Title",
                ["Plugins.Misc.PushNotifications.Settings.TestNotificationMessage"] = "Test Message",
                
                // Actions and Messages
                ["Plugins.Misc.PushNotifications.Send"] = "Send",
                ["Plugins.Misc.PushNotifications.Success"] = "Success",
                ["Plugins.Misc.PushNotifications.Error"] = "Error",
                ["Plugins.Misc.PushNotifications.TestNotificationSent"] = "Test notification sent successfully.",
                ["Plugins.Misc.PushNotifications.TestNotificationFailed"] = "Failed to send test notification.",
                
                // Platform Detection Messages
                ["Plugins.Misc.PushNotifications.Platform.FCM"] = "FCM (Firebase Cloud Messaging)",
                ["Plugins.Misc.PushNotifications.Platform.WebPush"] = "Web Push (Native)",
                ["Plugins.Misc.PushNotifications.Platform.iOS.Detected"] = "iOS device detected - using Web Push",
                ["Plugins.Misc.PushNotifications.Platform.Safari.Detected"] = "Safari browser detected - using Web Push",
                
                // Error Messages  
                ["Plugins.Misc.PushNotifications.Error.VapidKeysRequired"] = "VAPID keys are required for Web Push notifications",
                ["Plugins.Misc.PushNotifications.Error.FirebaseCredentialsRequired"] = "Firebase credentials are required for FCM notifications",
                ["Plugins.Misc.PushNotifications.Error.InvalidVapidSubject"] = "Invalid VAPID subject format. Use mailto:email@domain.com or https://yourdomain.com",
                ["Plugins.Misc.PushNotifications.Error.FirebaseVapidKeyRequired"] = "Firebase VAPID public key is required for FCM notifications",
                ["Plugins.Misc.PushNotifications.Error.WebPushVapidKeysRequired"] = "Web Push VAPID keys are required for native Web Push notifications",
                
                // Help Messages
                ["Plugins.Misc.PushNotifications.Help.Title"] = "Configuration Help",
                ["Plugins.Misc.PushNotifications.Help.VapidKeyGeneration"] = "To generate VAPID keys, use: npm install -g web-push && web-push generate-vapid-keys",
                ["Plugins.Misc.PushNotifications.Help.FirebaseVsWebPush"] = "Firebase keys are for FCM (Android/Chrome). Web Push keys are for native browser notifications (iOS Safari, Firefox, etc.)",
                
                // Dashboard Statistics (for future use)
                ["Plugins.Misc.PushNotifications.Stats.TotalSubscriptions"] = "Total Subscriptions",
                ["Plugins.Misc.PushNotifications.Stats.FCMSubscriptions"] = "FCM Subscriptions", 
                ["Plugins.Misc.PushNotifications.Stats.WebPushSubscriptions"] = "Web Push Subscriptions",
                ["Plugins.Misc.PushNotifications.Stats.iOSDevices"] = "iOS Devices",
                ["Plugins.Misc.PushNotifications.Stats.AndroidDevices"] = "Android Devices",
                ["Plugins.Misc.PushNotifications.Stats.DesktopDevices"] = "Desktop Devices"
            });

            var tasks = new List<ScheduleTask>
            {
                new ScheduleTask
                {
                    Name = "Send Push Notifications",
                    Seconds = 3600, // 1 hour
                    Type = "Nop.Plugin.Misc.PushNotifications.Tasks.SendPushNotificationsTask, Nop.Plugin.Misc.PushNotifications",
                    Enabled = true,
                    StopOnError = false
                }
            };

            await _dataProvider.BulkInsertEntitiesAsync(tasks);
            await base.InstallAsync();
        }

        public override async Task UninstallAsync()
        {
            await _settingService.DeleteSettingAsync<PushNotificationsSettings>();
            await _localizationService.DeleteLocaleResourcesAsync("Plugins.Misc.PushNotifications");
            await base.UninstallAsync();
        }

        public Type GetWidgetViewComponent(string widgetZone)
        {
            return typeof(PushNotificationsViewComponent);
        }

    }
}
