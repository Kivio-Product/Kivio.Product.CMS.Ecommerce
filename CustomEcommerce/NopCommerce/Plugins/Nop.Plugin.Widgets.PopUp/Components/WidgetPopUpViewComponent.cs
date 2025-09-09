using Microsoft.AspNetCore.Mvc;
using Nop.Plugin.Widgets.PopUp.Models;
using Nop.Plugin.Widgets.PopUp.Services;
using Nop.Services.Configuration;
using Nop.Services.Media;
using Nop.Web.Framework.Components;

namespace Nop.Plugin.Widgets.PopUp.Components;

/// <summary>
/// PopUp widget component
/// </summary>
public class WidgetPopUpViewComponent : NopViewComponent
{
    #region Fields

    private readonly IPictureService _pictureService;
    private readonly ISettingService _settingService;
    private readonly IPageTypeService _pageTypeService;

    #endregion

    #region Ctor

    public WidgetPopUpViewComponent(IPictureService pictureService,
        ISettingService settingService,
        IPageTypeService pageTypeService)
    {
        _pictureService = pictureService;
        _settingService = settingService;
        _pageTypeService = pageTypeService;
    }

    #endregion

    #region Methods

    /// <summary>
    /// Invoke view component
    /// </summary>
    /// <param name="widgetZone">Widget zone name</param>
    /// <param name="additionalData">Additional data</param>
    /// <returns>
    /// A task that represents the asynchronous operation
    /// The task result contains the view component result
    /// </returns>
    public async Task<IViewComponentResult> InvokeAsync(string widgetZone, object additionalData)
    {
        var popUpSettings = await _settingService.LoadSettingAsync<PopUpSettings>();

        if (!popUpSettings.IsEnabled || popUpSettings.PictureId == 0)
            return Content("");

        // Check if popup should be displayed on the current page
        if (!_pageTypeService.ShouldDisplayPopup(HttpContext, popUpSettings.DisplayPages))
            return Content("");

        var picture = await _pictureService.GetPictureByIdAsync(popUpSettings.PictureId);
        if (picture == null)
            return Content("");

        var model = new PublicInfoModel
        {
            PictureUrl = await _pictureService.GetPictureUrlAsync(popUpSettings.PictureId),
            TitleText = popUpSettings.TitleText,
            LinkUrl = popUpSettings.LinkUrl,
            AltText = popUpSettings.AltText,
            IsEnabled = popUpSettings.IsEnabled,
            ShowOncePerSession = popUpSettings.ShowOncePerSession
        };

        return View("~/Plugins/Widgets.PopUp/Views/PublicInfo.cshtml", model);
    }

    #endregion
}
