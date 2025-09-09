using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
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
            IsEnabled = popUpSettings.IsEnabled,
            DisplayPages = popUpSettings.DisplayPages,
            ShowOncePerSession = popUpSettings.ShowOncePerSession
        };

        await PreparePageCheckboxesAsync(model);

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
        popUpSettings.DisplayPages = GetDisplayPagesFromModel(model);
        popUpSettings.ShowOncePerSession = model.ShowOncePerSession;

        await _settingService.SaveSettingAsync(popUpSettings);

        _notificationService.SuccessNotification(await _localizationService.GetResourceAsync("Admin.Plugins.Saved"));

        return await Configure();
    }

    #endregion

    #region Utilities

    /// <summary>
    /// Prepare page checkboxes based on display pages setting
    /// </summary>
    /// <param name="model">Configuration model</param>
    /// <returns>A task that represents the asynchronous operation</returns>
    protected virtual async Task PreparePageCheckboxesAsync(ConfigurationModel model)
    {
        ArgumentNullException.ThrowIfNull(model);

        var displayPages = model.DisplayPages?.Split(',', StringSplitOptions.RemoveEmptyEntries)
            .Select(p => p.Trim().ToLowerInvariant())
            .ToList() ?? new List<string>();

        model.ShowOnHome = displayPages.Contains("home");
        model.ShowOnCategory = displayPages.Contains("category");
        model.ShowOnProduct = displayPages.Contains("product");
        model.ShowOnManufacturer = displayPages.Contains("manufacturer");
        model.ShowOnTopic = displayPages.Contains("topic");
        model.ShowOnBlog = displayPages.Contains("blog");
        model.ShowOnNews = displayPages.Contains("newsitem");
        model.ShowOnAll = displayPages.Contains("all");

        await Task.CompletedTask;
    }

    /// <summary>
    /// Get display pages string from model checkboxes
    /// </summary>
    /// <param name="model">Configuration model</param>
    /// <returns>Comma-separated display pages string</returns>
    protected virtual string GetDisplayPagesFromModel(ConfigurationModel model)
    {
        ArgumentNullException.ThrowIfNull(model);

        var selectedPages = new List<string>();

        if (model.ShowOnAll)
        {
            selectedPages.Add("all");
        }
        else
        {
            if (model.ShowOnHome) selectedPages.Add("home");
            if (model.ShowOnCategory) selectedPages.Add("category");
            if (model.ShowOnProduct) selectedPages.Add("product");
            if (model.ShowOnManufacturer) selectedPages.Add("manufacturer");
            if (model.ShowOnTopic) selectedPages.Add("topic");
            if (model.ShowOnBlog) selectedPages.Add("blog");
            if (model.ShowOnNews) selectedPages.Add("newsitem");
        }

        return string.Join(",", selectedPages);
    }

    #endregion
}
