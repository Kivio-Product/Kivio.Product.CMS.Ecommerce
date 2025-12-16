using Microsoft.AspNetCore.Mvc;
using Nop.Plugin.Widgets.PopUp.Models;
using Nop.Plugin.Widgets.PopUp.Domain;
using Nop.Services.Configuration;
using Nop.Services.Localization;
using Nop.Services.Messages;
using Nop.Services.Security;
using Nop.Services.Media;
using Nop.Web.Framework;
using Nop.Web.Framework.Controllers;
using Nop.Web.Framework.Mvc.Filters;
using Nop.Web.Framework.Mvc;
using Nop.Web.Framework.Models.Extensions;
using Newtonsoft.Json;

namespace Nop.Plugin.Widgets.PopUp.Controllers;

[AuthorizeAdmin]
[Area(AreaNames.ADMIN)]
[AutoValidateAntiforgeryToken]
public class WidgetPopUpController(ILocalizationService localizationService,
    INotificationService notificationService,
    IPermissionService permissionService,
    ISettingService settingService,
    IPictureService pictureService) : BasePluginController
{
    #region Fields

    private readonly ILocalizationService _localizationService = localizationService;
    private readonly INotificationService _notificationService = notificationService;
    private readonly IPermissionService _permissionService = permissionService;
    private readonly ISettingService _settingService = settingService;
    private readonly IPictureService _pictureService = pictureService;

    #endregion
    #region Ctor

    #endregion

    #region Utilities

    protected virtual async Task<List<PopUpImage>> GetImagesAsync()
    {
        var popUpSettings = await _settingService.LoadSettingAsync<PopUpSettings>();
        
        if (string.IsNullOrEmpty(popUpSettings.ImagesJson))
            return [];

        try
        {
            return JsonConvert.DeserializeObject<List<PopUpImage>>(popUpSettings.ImagesJson) ?? [];
        }
        catch
        {
            return [];
        }
    }

    /// <summary>
    /// Get weekdays list from image model checkboxes
    /// </summary>
    /// <param name="imageModel">Popup image model</param>
    /// <returns>List of weekday numbers (0=Sunday, 1=Monday, ..., 6=Saturday)</returns>
    protected virtual List<int> GetWeekdaysFromModel(PopUpImageModel imageModel)
    {
        ArgumentNullException.ThrowIfNull(imageModel);

        var weekdays = new List<int>();
        if (imageModel.Sunday) weekdays.Add(0);
        if (imageModel.Monday) weekdays.Add(1);
        if (imageModel.Tuesday) weekdays.Add(2);
        if (imageModel.Wednesday) weekdays.Add(3);
        if (imageModel.Thursday) weekdays.Add(4);
        if (imageModel.Friday) weekdays.Add(5);
        if (imageModel.Saturday) weekdays.Add(6);

        return weekdays;
    }

    /// <summary>
    /// Validate that each weekday is only assigned to one image, excluding the specified picture ID
    /// </summary>
    protected virtual async Task<string> ValidateWeekdayAssignment(int pictureId, List<int> newWeekdays)
    {
        var existingImages = await GetImagesAsync();
        var weekdayAssignments = new Dictionary<int, int>(); // weekday -> picture id

        // Check existing images (excluding the one being added/edited)
        foreach (var img in existingImages.Where(i => i.PictureId != pictureId))
        {
            foreach (var weekday in img.Weekdays)
            {
                weekdayAssignments[weekday] = img.PictureId;
            }
        }

        // Check if any new weekday is already assigned
        foreach (var weekday in newWeekdays)
        {
            if (weekdayAssignments.ContainsKey(weekday))
            {
                var weekdayName = GetWeekdayName(weekday);
                return $"{weekdayName} is already assigned to another image.";
            }
        }

        return string.Empty;
    }

    /// <summary>
    /// Get weekday name for display
    /// </summary>
    protected virtual string GetWeekdayName(int weekday)
    {
        return weekday switch
        {
            0 => "Sunday",
            1 => "Monday",
            2 => "Tuesday",
            3 => "Wednesday",
            4 => "Thursday",
            5 => "Friday",
            6 => "Saturday",
            _ => "Unknown"
        };
    }

    /// <summary>
    /// Get display string for weekdays
    /// </summary>
    protected virtual string GetWeekdaysDisplay(List<int> weekdays)
    {
        if (weekdays == null || weekdays.Count == 0)
            return "None";

        var dayNames = weekdays.OrderBy(d => d).Select(GetWeekdayName);
        return string.Join(", ", dayNames);
    }

    #endregion

    #region Methods

    [CheckPermission(StandardPermission.Configuration.MANAGE_WIDGETS)]
    public async Task<IActionResult> Configure()
    {
        var popUpSettings = await _settingService.LoadSettingAsync<PopUpSettings>();
        var model = new ConfigurationModel
        {
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
        popUpSettings.IsEnabled = model.IsEnabled;
        popUpSettings.DisplayPages = GetDisplayPagesFromModel(model);
        popUpSettings.ShowOncePerSession = model.ShowOncePerSession;

        await _settingService.SaveSettingAsync(popUpSettings);

        _notificationService.SuccessNotification(await _localizationService.GetResourceAsync("Admin.Plugins.Saved"));

        return await Configure();
    }

    [IgnoreAntiforgeryToken]
    [CheckPermission(StandardPermission.Configuration.MANAGE_WIDGETS)]
    [HttpPost, ActionName("Configure")]
    [FormValueRequired("add-image")]
    public virtual async Task<IActionResult> ImageAdd(PopUpImageModel model)
    {
        if (!ModelState.IsValid)
            return await Configure();

        if (model.PictureId == 0)
        {
            _notificationService.ErrorNotification(await _localizationService.GetResourceAsync("Plugins.Widgets.PopUp.Picture.Required"));
            return await Configure();
        }

        var weekdays = GetWeekdaysFromModel(model);
        
        var validationError = await ValidateWeekdayAssignment(0, weekdays);
        if (!string.IsNullOrEmpty(validationError))
        {
            _notificationService.ErrorNotification(validationError);
            return await Configure();
        }

        var images = await GetImagesAsync();

        images.Add(new PopUpImage
        {
            PictureId = model.PictureId,
            AltText = model.AltText ?? string.Empty,
            TitleText = model.TitleText ?? string.Empty,
            LinkUrl = model.LinkUrl ?? string.Empty,
            Weekdays = weekdays
        });

        var popUpSettings = await _settingService.LoadSettingAsync<PopUpSettings>();
        popUpSettings.ImagesJson = JsonConvert.SerializeObject(images);
        await _settingService.SaveSettingAsync(popUpSettings);

        _notificationService.SuccessNotification(await _localizationService.GetResourceAsync("Plugins.Widgets.PopUp.Image.Added"));

        return RedirectToAction(nameof(Configure));
    }

    [HttpPost]
    [IgnoreAntiforgeryToken]
    [CheckPermission(StandardPermission.Configuration.MANAGE_WIDGETS)]
    public async Task<IActionResult> ImageList(PopUpImagesSearchModel imagesSearchModel)
    {
        var images = await GetImagesAsync();

        if (images == null)
            images = [];

        var pagedImages = images.ToPagedList(imagesSearchModel);

        var model = await new PopUpImageListModel().PrepareToGridAsync(imagesSearchModel, pagedImages, () =>
        {
            return images
                .Where(i => i.PictureId != 0)
                .SelectAwait(async item =>
                {
                    var picture = await _pictureService.GetPictureByIdAsync(item.PictureId);

                    if (picture is null)
                        return null;

                    return new PopUpImageModel
                    {
                        PictureId = item.PictureId,
                        PictureUrl = (await _pictureService.GetPictureUrlAsync(picture, 200)).Url,
                        TitleText = item.TitleText ?? string.Empty,
                        AltText = item.AltText ?? string.Empty,
                        LinkUrl = item.LinkUrl ?? string.Empty,
                        WeekdaysDisplay = GetWeekdaysDisplay(item.Weekdays),
                        Sunday = item.Weekdays.Contains(0),
                        Monday = item.Weekdays.Contains(1),
                        Tuesday = item.Weekdays.Contains(2),
                        Wednesday = item.Weekdays.Contains(3),
                        Thursday = item.Weekdays.Contains(4),
                        Friday = item.Weekdays.Contains(5),
                        Saturday = item.Weekdays.Contains(6)
                    };
                }).Where(item => item != null);
        });

        return Json(model);
    }

    [HttpPost]
    [IgnoreAntiforgeryToken]
    [CheckPermission(StandardPermission.Configuration.MANAGE_WIDGETS)]
    public virtual async Task<IActionResult> ImageDelete(int pictureId)
    {
        var images = await GetImagesAsync();

        if (images.Count == 0)
            return new NullJsonResult();

        if (images.RemoveAll(i => i.PictureId == pictureId) == 0)
            return new NullJsonResult();

        var popUpSettings = await _settingService.LoadSettingAsync<PopUpSettings>();

        if (images.Count == 0)
        {
            popUpSettings.ImagesJson = string.Empty;
        }
        else
        {
            popUpSettings.ImagesJson = JsonConvert.SerializeObject(images);
        }

        await _settingService.SaveSettingAsync(popUpSettings);

        return new NullJsonResult();
    }

    [HttpPost]
    [IgnoreAntiforgeryToken]
    [CheckPermission(StandardPermission.Configuration.MANAGE_WIDGETS)]
    public virtual async Task<IActionResult> ImageEdit(PopUpImageModel model)
    {
        var images = await GetImagesAsync();
        if (images.Count == 0)
            return Content("No images");

        var image = images.FirstOrDefault(i => i.PictureId == model.PictureId)
            ?? throw new ArgumentException("No image found with the specified picture id");

        var weekdays = GetWeekdaysFromModel(model);
        
        var validationError = await ValidateWeekdayAssignment(model.PictureId, weekdays);
        if (!string.IsNullOrEmpty(validationError))
        {
            return Content(validationError);
        }

        image.TitleText = model.TitleText ?? string.Empty;
        image.AltText = model.AltText ?? string.Empty;
        image.LinkUrl = model.LinkUrl ?? string.Empty;
        image.Weekdays = weekdays;

        var popUpSettings = await _settingService.LoadSettingAsync<PopUpSettings>();
        popUpSettings.ImagesJson = JsonConvert.SerializeObject(images);
        await _settingService.SaveSettingAsync(popUpSettings);

        return new NullJsonResult();
    }

    #endregion

    #region Utilities (Page Checkboxes)

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
            .ToList() ?? [];

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
