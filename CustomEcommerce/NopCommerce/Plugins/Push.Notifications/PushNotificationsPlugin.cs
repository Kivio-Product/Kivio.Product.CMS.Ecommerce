
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
                FirebaseCredentials = ""
            };
            await _settingService.SaveSettingAsync(settings);

            await _localizationService.AddOrUpdateLocaleResourceAsync(new Dictionary<string, string>
            {
                ["Plugins.Misc.PushNotifications.FriendlyName"] = "Push Notifications",
                ["Plugins.Misc.PushNotifications.Settings.FirebaseCredentials"] = "Firebase Credentials (JSON)",
                ["Plugins.Misc.PushNotifications.Settings.FirebaseCredentials.Hint"] = "Paste the JSON content of your Firebase service account credentials file here.",
                ["Plugins.Misc.PushNotifications.Settings.SendTestNotification"] = "Send Test Notification",
                ["Plugins.Misc.PushNotifications.Settings.SendTestNotification.Hint"] = "Send a test notification to all subscribed devices.",
                ["Plugins.Misc.PushNotifications.Settings.TestNotificationTitle"] = "Test Title",
                ["Plugins.Misc.PushNotifications.Settings.TestNotificationMessage"] = "Test Message",
                ["Plugins.Misc.PushNotifications.Send"] = "Send",
                ["Plugins.Misc.PushNotifications.Success"] = "Success",
                ["Plugins.Misc.PushNotifications.Error"] = "Error",
                ["Plugins.Misc.PushNotifications.TestNotificationSent"] = "Test notification sent successfully.",
                ["Plugins.Misc.PushNotifications.TestNotificationFailed"] = "Failed to send test notification."
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
