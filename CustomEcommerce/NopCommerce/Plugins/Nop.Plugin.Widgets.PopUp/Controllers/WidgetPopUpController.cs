using Microsoft.AspNetCore.Mvc;
using Nop.Plugin.Widgets.PopUp.Models;
using Nop.Services.Configuration;
using Nop.Services.Localization;
using Nop.Services.Messages;
using Nop.Services.Security;
using Nop.Web.Framework;
using Nop.Web.Framework.Controllers;
using Nop.Web.Framework.Mvc.Filters;

namespace Nop.Plugin.Widgets.PopUp.Controllers;

[AuthorizeAdmin]
[Area(AreaNames.ADMIN)]
[AutoValidateAntiforgeryToken]
public class WidgetPopUpController : BasePluginController
{
    #region Fields

    private readonly ILocalizationService _localizationService;
    private readonly INotificationService _notificationService;
    private readonly IPermissionService _permissionService;
    private readonly ISettingService _settingService;

    #endregion

    #region Ctor

    public WidgetPopUpController(ILocalizationService localizationService,
        INotificationService notificationService,
        IPermissionService permissionService,
        ISettingService settingService)
    {
        _localizationService = localizationService;
        _notificationService = notificationService;
        _permissionService = permissionService;
        _settingService = settingService;
    }

    #endregion

    #region Methods

    [CheckPermission(StandardPermission.Configuration.MANAGE_WIDGETS)]
    public async Task<IActionResult> Configure()
    {
        var popUpSettings = await _settingService.LoadSettingAsync<PopUpSettings>();
        var model = new ConfigurationModel
        {
            PictureId = popUpSettings.PictureId,
            TitleText = popUpSettings.TitleText,
            LinkUrl = popUpSettings.LinkUrl,
            AltText = popUpSettings.AltText,
            IsEnabled = popUpSettings.IsEnabled
        };

        return View("~/Plugins/Widgets.PopUp/Views/Configure.cshtml", model);
    }

    [HttpPost]
    [CheckPermission(StandardPermission.Configuration.MANAGE_WIDGETS)]
    public async Task<IActionResult> Configure(ConfigurationModel model)
    {
        if (!ModelState.IsValid)
            return await Configure();

        //save settings
        var popUpSettings = await _settingService.LoadSettingAsync<PopUpSettings>();
        popUpSettings.PictureId = model.PictureId;
        popUpSettings.TitleText = model.TitleText;
        popUpSettings.LinkUrl = model.LinkUrl;
        popUpSettings.AltText = model.AltText;
        popUpSettings.IsEnabled = model.IsEnabled;

        await _settingService.SaveSettingAsync(popUpSettings);

        _notificationService.SuccessNotification(await _localizationService.GetResourceAsync("Admin.Plugins.Saved"));

        return await Configure();
    }

    #endregion
}
