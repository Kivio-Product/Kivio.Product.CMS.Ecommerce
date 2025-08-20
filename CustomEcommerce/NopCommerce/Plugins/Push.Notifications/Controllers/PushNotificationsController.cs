using Microsoft.AspNetCore.Mvc;
using Nop.Core;
using Nop.Services.Security;
using Nop.Services.Configuration;
using Nop.Services.Messages;
using Nop.Web.Framework;
using Nop.Web.Framework.Controllers;
using Nop.Web.Framework.Mvc.Filters;
using Nop.Plugin.Misc.PushNotifications.Models;
using Nop.Services.Localization;
using System.Threading.Tasks;
using Nop.Plugin.Misc.PushNotifications.Services;

namespace Nop.Plugin.Misc.PushNotifications.Controllers
{
    [AuthorizeAdmin]
    [Area(AreaNames.ADMIN)]
    [AutoValidateAntiforgeryToken]
    public class PushNotificationsController : BasePluginController
    {
        private readonly ISettingService _settingService;
        private readonly INotificationService _notificationService;
        private readonly ILocalizationService _localizationService;
        private readonly IPushNotificationService _pushNotificationService;

        public PushNotificationsController(
            ISettingService settingService,
            INotificationService notificationService,
            ILocalizationService localizationService,
            IPushNotificationService pushNotificationService)
        {
            _settingService = settingService;
            _notificationService = notificationService;
            _localizationService = localizationService;
            _pushNotificationService = pushNotificationService;
        }

        public async Task<IActionResult> Configure()
        {
            var settings = await _settingService.LoadSettingAsync<PushNotificationsSettings>();
            var model = new ConfigurationModel
            {
                FirebaseCredentials = settings.FirebaseCredentials,
                VapidPublicKey = settings.VapidPublicKey,
                VapidPrivateKey = settings.VapidPrivateKey,
                FirebaseConfig = settings.FirebaseConfig,
                GeminiApiKey = settings.GeminiApiKey,
                EnableNewProductStrategy = settings.EnableNewProductStrategy,
                EnableCategoryStrategy = settings.EnableCategoryStrategy,
                EnableCustomStrategy = settings.EnableCustomStrategy,
                AIPromptBase = settings.AIPromptBase,
                CustomStrategyPrompt = settings.CustomStrategyPrompt,
                NotificationIconUrl = settings.NotificationIconUrl,
                AllowedDays = settings.AllowedDays,
                AllowedHours = settings.AllowedHours,
                UseUtcTime = settings.UseUtcTime,
                MinHoursBetweenNotifications = settings.MinHoursBetweenNotifications,
                WebPushSubject = settings.WebPushSubject,
                ForceWebPushForIOS = settings.ForceWebPushForIOS
            };
            return View("~/Plugins/Misc.PushNotifications/Views/Configure.cshtml", model);
        }

        [HttpPost]
        public async Task<IActionResult> Configure(ConfigurationModel model)
        {
            var settings = await _settingService.LoadSettingAsync<PushNotificationsSettings>();
            settings.FirebaseCredentials = model.FirebaseCredentials;
            settings.VapidPublicKey = model.VapidPublicKey;
            settings.VapidPrivateKey = model.VapidPrivateKey;
            settings.FirebaseConfig = model.FirebaseConfig;
            settings.GeminiApiKey = model.GeminiApiKey;
            settings.EnableNewProductStrategy = model.EnableNewProductStrategy;
            settings.EnableCategoryStrategy = model.EnableCategoryStrategy;
            settings.EnableCustomStrategy = model.EnableCustomStrategy;
            settings.AIPromptBase = model.AIPromptBase;
            settings.CustomStrategyPrompt = model.CustomStrategyPrompt;
            settings.NotificationIconUrl = model.NotificationIconUrl;
            settings.AllowedDays = model.AllowedDays;
            settings.AllowedHours = model.AllowedHours;
            settings.UseUtcTime = model.UseUtcTime;
            settings.MinHoursBetweenNotifications = model.MinHoursBetweenNotifications;
            settings.WebPushSubject = model.WebPushSubject;
            settings.ForceWebPushForIOS = model.ForceWebPushForIOS;
            await _settingService.SaveSettingAsync(settings);
            _notificationService.SuccessNotification(await _localizationService.GetResourceAsync("Admin.Plugins.Saved"));
            return await Configure();
        }

        [HttpPost]
        public async Task<IActionResult> SendTestNotification(ConfigurationModel model)
        {
            try
            {
                await _pushNotificationService.SendNotificationToAllAsync(model.TestNotificationTitle, model.TestNotificationMessage, "/");
                _notificationService.SuccessNotification(await _localizationService.GetResourceAsync("Plugins.Misc.PushNotifications.TestNotificationSent"));
            }
            catch
            {
                _notificationService.ErrorNotification(await _localizationService.GetResourceAsync("Plugins.Misc.PushNotifications.TestNotificationFailed"));
            }

            return await Configure();
        }
    }
}
