using Microsoft.AspNetCore.Mvc;
using Nop.Plugin.Widgets.PopUp.Models;
using Nop.Plugin.Widgets.PopUp.Services;
using Nop.Plugin.Widgets.PopUp.Domain;
using Nop.Services.Configuration;
using Nop.Services.Media;
using Nop.Web.Framework.Components;
using Newtonsoft.Json;

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

        if (!popUpSettings.IsEnabled)
            return Content("");

        // Check if popup should be displayed on the current page
        if (!_pageTypeService.ShouldDisplayPopup(HttpContext, popUpSettings.DisplayPages))
            return Content("");

        // Get current day of week (0=Sunday, 1=Monday, ..., 6=Saturday)
        var currentDayOfWeek = (int)DateTime.Now.DayOfWeek;

        // Try to get image for today from the new images list
        PopUpImage todayImage = null;
        if (!string.IsNullOrEmpty(popUpSettings.ImagesJson))
        {
            try
            {
                var images = JsonConvert.DeserializeObject<List<PopUpImage>>(popUpSettings.ImagesJson);
                if (images != null && images.Count != 0)
                {
                    // Find the image that has the current day assigned
                    todayImage = images.FirstOrDefault(img => img.Weekdays.Contains(currentDayOfWeek));
                }
            }
            catch
            {
                // If deserialization fails, fall back to legacy behavior
            }
        }

        // If still no image, don't display popup
        if (todayImage == null || todayImage.PictureId == 0)
            return Content("");

        var picture = await _pictureService.GetPictureByIdAsync(todayImage.PictureId);
        if (picture == null)
            return Content("");

        var model = new PublicInfoModel
        {
            PictureUrl = await _pictureService.GetPictureUrlAsync(todayImage.PictureId),
            TitleText = todayImage.TitleText,
            LinkUrl = todayImage.LinkUrl,
            AltText = todayImage.AltText,
            IsEnabled = popUpSettings.IsEnabled,
            ShowOncePerSession = popUpSettings.ShowOncePerSession
        };

        return View("~/Plugins/Widgets.PopUp/Views/PublicInfo.cshtml", model);
    }

    #endregion
}
