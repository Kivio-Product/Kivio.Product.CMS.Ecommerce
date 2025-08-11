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
                VapidPublicKey = settings.VapidPublicKey
            };
            return View("~/Plugins/Misc.PushNotifications/Views/Configure.cshtml", model);
        }

        [HttpPost]
        public async Task<IActionResult> Configure(ConfigurationModel model)
        {
            var settings = await _settingService.LoadSettingAsync<PushNotificationsSettings>();
            settings.FirebaseCredentials = model.FirebaseCredentials;
            settings.VapidPublicKey = model.VapidPublicKey;
            await _settingService.SaveSettingAsync(settings);
            _notificationService.SuccessNotification(await _localizationService.GetResourceAsync("Admin.Plugins.Saved"));
            return await Configure();
        }

        [HttpPost]
        public async Task<IActionResult> SendTestNotification(ConfigurationModel model)
        {
            try
            {
                await _pushNotificationService.SendNotificationToAllAsync(model.TestNotificationTitle, model.TestNotificationMessage);
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
