using Nop.Core;
using Nop.Core.Domain.Cms;
using Nop.Plugin.Widgets.PopUp.Components;
using Nop.Services.Cms;
using Nop.Services.Configuration;
using Nop.Services.Localization;
using Nop.Services.Plugins;
using Nop.Web.Framework.Infrastructure;

namespace Nop.Plugin.Widgets.PopUp;

/// <summary>
/// Represents PopUp widget plugin
/// </summary>
public class PopUpPlugin : BasePlugin, IWidgetPlugin
{
    #region Fields

    private readonly ILocalizationService _localizationService;
    private readonly ISettingService _settingService;
    private readonly IWebHelper _webHelper;
    private readonly WidgetSettings _widgetSettings;

    #endregion

    #region Ctor

    public PopUpPlugin(ILocalizationService localizationService,
        ISettingService settingService,
        IWebHelper webHelper,
        WidgetSettings widgetSettings)
    {
        _localizationService = localizationService;
        _settingService = settingService;
        _webHelper = webHelper;
        _widgetSettings = widgetSettings;
    }

    #endregion

    #region Methods

    /// <summary>
    /// Gets widget zones where this widget should be rendered
    /// </summary>
    /// <returns>
    /// A task that represents the asynchronous operation
    /// The task result contains the widget zones
    /// </returns>
    public Task<IList<string>> GetWidgetZonesAsync()
    {
        return Task.FromResult<IList<string>>(new List<string> { PublicWidgetZones.HomepageTop });
    }

    /// <summary>
    /// Gets a configuration page URL
    /// </summary>
    public override string GetConfigurationPageUrl()
    {
        return _webHelper.GetStoreLocation() + "Admin/WidgetPopUp/Configure";
    }

    /// <summary>
    /// Gets a name of a view component for displaying widget
    /// </summary>
    /// <param name="widgetZone">Name of the widget zone</param>
    /// <returns>View component name</returns>
    public Type GetWidgetViewComponent(string widgetZone)
    {
        return typeof(WidgetPopUpViewComponent);
    }

    /// <summary>
    /// Install plugin
    /// </summary>
    /// <returns>A task that represents the asynchronous operation</returns>
    public override async Task InstallAsync()
    {
        //settings
        var settings = new PopUpSettings
        {
            IsEnabled = false,
            PictureId = 0,
            TitleText = string.Empty,
            LinkUrl = string.Empty,
            AltText = string.Empty
        };
        await _settingService.SaveSettingAsync(settings);

        if (!_widgetSettings.ActiveWidgetSystemNames.Contains(PluginDescriptor.SystemName))
        {
            _widgetSettings.ActiveWidgetSystemNames.Add(PluginDescriptor.SystemName);
            await _settingService.SaveSettingAsync(_widgetSettings);
        }

        //locales
        await _localizationService.AddOrUpdateLocaleResourceAsync(new Dictionary<string, string>
        {
            ["Plugins.Widgets.PopUp.Picture"] = "Picture",
            ["Plugins.Widgets.PopUp.Picture.Hint"] = "Upload picture for the popup.",
            ["Plugins.Widgets.PopUp.Picture.Required"] = "Picture is required",
            ["Plugins.Widgets.PopUp.TitleText"] = "Title",
            ["Plugins.Widgets.PopUp.TitleText.Hint"] = "Enter title for picture. Leave empty if you don't want to display any text.",
            ["Plugins.Widgets.PopUp.LinkUrl"] = "URL",
            ["Plugins.Widgets.PopUp.LinkUrl.Hint"] = "Enter URL. Leave empty if you don't want this picture to be clickable.",
            ["Plugins.Widgets.PopUp.AltText"] = "Image alternate text",
            ["Plugins.Widgets.PopUp.AltText.Hint"] = "Enter alternate text that will be added to image.",
            ["Plugins.Widgets.PopUp.IsEnabled"] = "Enabled",
            ["Plugins.Widgets.PopUp.IsEnabled.Hint"] = "Check to enable the popup widget."
        });

        await base.InstallAsync();
    }

    /// <summary>
    /// Uninstall plugin
    /// </summary>
    /// <returns>A task that represents the asynchronous operation</returns>
    public override async Task UninstallAsync()
    {
        //settings
        await _settingService.DeleteSettingAsync<PopUpSettings>();
        if (_widgetSettings.ActiveWidgetSystemNames.Contains(PluginDescriptor.SystemName))
        {
            _widgetSettings.ActiveWidgetSystemNames.Remove(PluginDescriptor.SystemName);
            await _settingService.SaveSettingAsync(_widgetSettings);
        }

        //locales
        await _localizationService.DeleteLocaleResourcesAsync("Plugins.Widgets.PopUp");

        await base.UninstallAsync();
    }

    #endregion

    #region Properties

    /// <summary>
    /// Gets a value indicating whether to hide this plugin on the widget list page in the admin area
    /// </summary>
    public bool HideInWidgetList => false;

    #endregion
}
