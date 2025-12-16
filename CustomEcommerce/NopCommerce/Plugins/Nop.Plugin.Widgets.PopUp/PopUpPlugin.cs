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
        return Task.FromResult<IList<string>>(new List<string> { PublicWidgetZones.BodyStartHtmlTagAfter });
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
            AltText = string.Empty,
            DisplayPages = "home",
            ShowOncePerSession = true
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
            ["Plugins.Widgets.PopUp.IsEnabled.Hint"] = "Check to enable the popup widget.",
            ["Plugins.Widgets.PopUp.DisplayPages"] = "Show on pages",
            ["Plugins.Widgets.PopUp.DisplayPages.Hint"] = "Select the pages where the popup should be displayed",
            ["Plugins.Widgets.PopUp.ShowOncePerSession"] = "Show once per session",
            ["Plugins.Widgets.PopUp.ShowOncePerSession.Hint"] = "Check to show the popup only once per browser session (until browser is closed).",
            ["Plugins.Widgets.PopUp.Pages.Home"] = "Home page",
            ["Plugins.Widgets.PopUp.Pages.Category"] = "Category pages",
            ["Plugins.Widgets.PopUp.Pages.Product"] = "Product pages",
            ["Plugins.Widgets.PopUp.Pages.Manufacturer"] = "Manufacturer pages",
            ["Plugins.Widgets.PopUp.Pages.Topic"] = "Topic pages",
            ["Plugins.Widgets.PopUp.Pages.Blog"] = "Blog pages",
            ["Plugins.Widgets.PopUp.Pages.News"] = "News pages",
            ["Plugins.Widgets.PopUp.Pages.All"] = "All pages",
            ["Plugins.Widgets.PopUp.ShowOnHome"] = "Home page",
            ["Plugins.Widgets.PopUp.ShowOnCategory"] = "Category pages",
            ["Plugins.Widgets.PopUp.ShowOnProduct"] = "Product pages",
            ["Plugins.Widgets.PopUp.ShowOnManufacturer"] = "Manufacturer pages",
            ["Plugins.Widgets.PopUp.ShowOnTopic"] = "Topic pages",
            ["Plugins.Widgets.PopUp.ShowOnBlog"] = "Blog pages",
            ["Plugins.Widgets.PopUp.ShowOnNews"] = "News pages",
            ["Plugins.Widgets.PopUp.ShowOnAll"] = "All pages",
            ["Plugins.Widgets.PopUp.Configure"] = "Configure",
            ["Plugins.Widgets.PopUp.Settings"] = "General Settings",
            ["Plugins.Widgets.PopUp.ImageList"] = "Popup Images",
            ["Plugins.Widgets.PopUp.Image"] = "Image",
            ["Plugins.Widgets.PopUp.AddImage"] = "Add New Image",
            ["Plugins.Widgets.PopUp.Image.Added"] = "Image added successfully",
            ["Plugins.Widgets.PopUp.Weekdays"] = "Display on weekdays",
            ["Plugins.Widgets.PopUp.EditWeekdays"] = "Edit Weekdays",
            ["Plugins.Widgets.PopUp.Weekday.Monday"] = "Monday",
            ["Plugins.Widgets.PopUp.Weekday.Tuesday"] = "Tuesday",
            ["Plugins.Widgets.PopUp.Weekday.Wednesday"] = "Wednesday",
            ["Plugins.Widgets.PopUp.Weekday.Thursday"] = "Thursday",
            ["Plugins.Widgets.PopUp.Weekday.Friday"] = "Friday",
            ["Plugins.Widgets.PopUp.Weekday.Saturday"] = "Saturday",
            ["Plugins.Widgets.PopUp.Weekday.Sunday"] = "Sunday"
        });

        // Spanish locales
        await _localizationService.AddOrUpdateLocaleResourceAsync(new Dictionary<string, string>
        {
            ["Plugins.Widgets.PopUp.Picture"] = "Imagen",
            ["Plugins.Widgets.PopUp.Picture.Hint"] = "Suba una imagen para el popup.",
            ["Plugins.Widgets.PopUp.Picture.Required"] = "La imagen es requerida",
            ["Plugins.Widgets.PopUp.TitleText"] = "Título",
            ["Plugins.Widgets.PopUp.TitleText.Hint"] = "Ingrese el título de la imagen. Déjelo vacío si no desea mostrar texto.",
            ["Plugins.Widgets.PopUp.LinkUrl"] = "URL",
            ["Plugins.Widgets.PopUp.LinkUrl.Hint"] = "Ingrese la URL. Déjelo vacío si no desea que la imagen sea clickeable.",
            ["Plugins.Widgets.PopUp.AltText"] = "Texto alternativo",
            ["Plugins.Widgets.PopUp.AltText.Hint"] = "Ingrese el texto alternativo que se agregará a la imagen.",
            ["Plugins.Widgets.PopUp.IsEnabled"] = "Habilitado",
            ["Plugins.Widgets.PopUp.IsEnabled.Hint"] = "Marque para habilitar el widget de popup.",
            ["Plugins.Widgets.PopUp.DisplayPages"] = "Mostrar en páginas",
            ["Plugins.Widgets.PopUp.DisplayPages.Hint"] = "Seleccione las páginas donde se mostrará el popup",
            ["Plugins.Widgets.PopUp.ShowOncePerSession"] = "Mostrar una vez por sesión",
            ["Plugins.Widgets.PopUp.ShowOncePerSession.Hint"] = "Marque para mostrar el popup solo una vez por sesión del navegador (hasta que se cierre).",
            ["Plugins.Widgets.PopUp.Pages.Home"] = "Página de inicio",
            ["Plugins.Widgets.PopUp.Pages.Category"] = "Páginas de categoría",
            ["Plugins.Widgets.PopUp.Pages.Product"] = "Páginas de producto",
            ["Plugins.Widgets.PopUp.Pages.Manufacturer"] = "Páginas de fabricante",
            ["Plugins.Widgets.PopUp.Pages.Topic"] = "Páginas de tema",
            ["Plugins.Widgets.PopUp.Pages.Blog"] = "Páginas de blog",
            ["Plugins.Widgets.PopUp.Pages.News"] = "Páginas de noticias",
            ["Plugins.Widgets.PopUp.Pages.All"] = "Todas las páginas",
            ["Plugins.Widgets.PopUp.ShowOnHome"] = "Página de inicio",
            ["Plugins.Widgets.PopUp.ShowOnCategory"] = "Páginas de categoría",
            ["Plugins.Widgets.PopUp.ShowOnProduct"] = "Páginas de producto",
            ["Plugins.Widgets.PopUp.ShowOnManufacturer"] = "Páginas de fabricante",
            ["Plugins.Widgets.PopUp.ShowOnTopic"] = "Páginas de tema",
            ["Plugins.Widgets.PopUp.ShowOnBlog"] = "Páginas de blog",
            ["Plugins.Widgets.PopUp.ShowOnNews"] = "Páginas de noticias",
            ["Plugins.Widgets.PopUp.ShowOnAll"] = "Todas las páginas",
            ["Plugins.Widgets.PopUp.Configure"] = "Configurar",
            ["Plugins.Widgets.PopUp.Settings"] = "Configuración General",
            ["Plugins.Widgets.PopUp.ImageList"] = "Imágenes de Popup",
            ["Plugins.Widgets.PopUp.Image"] = "Imagen",
            ["Plugins.Widgets.PopUp.AddImage"] = "Agregar Nueva Imagen",
            ["Plugins.Widgets.PopUp.Image.Added"] = "Imagen agregada exitosamente",
            ["Plugins.Widgets.PopUp.Weekdays"] = "Mostrar en días de la semana",
            ["Plugins.Widgets.PopUp.EditWeekdays"] = "Editar Días de la Semana",
            ["Plugins.Widgets.PopUp.Weekday.Monday"] = "Lunes",
            ["Plugins.Widgets.PopUp.Weekday.Tuesday"] = "Martes",
            ["Plugins.Widgets.PopUp.Weekday.Wednesday"] = "Miércoles",
            ["Plugins.Widgets.PopUp.Weekday.Thursday"] = "Jueves",
            ["Plugins.Widgets.PopUp.Weekday.Friday"] = "Viernes",
            ["Plugins.Widgets.PopUp.Weekday.Saturday"] = "Sábado",
            ["Plugins.Widgets.PopUp.Weekday.Sunday"] = "Domingo"
        }, languageId: 2);

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
