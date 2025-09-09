using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;
using Nop.Web.Framework.Models;
using Nop.Web.Framework.Mvc.ModelBinding;

namespace Nop.Plugin.Widgets.PopUp.Models;

/// <summary>
/// Represents a configuration model for PopUp widget
/// </summary>
public record ConfigurationModel : BaseNopModel
{
    #region Properties

    [UIHint("Picture")]
    [NopResourceDisplayName("Plugins.Widgets.PopUp.Picture")]
    public int PictureId { get; set; }

    [NopResourceDisplayName("Plugins.Widgets.PopUp.TitleText")]
    public string TitleText { get; set; } = string.Empty;

    [NopResourceDisplayName("Plugins.Widgets.PopUp.LinkUrl")]
    public string LinkUrl { get; set; } = string.Empty;

    [NopResourceDisplayName("Plugins.Widgets.PopUp.AltText")]
    public string AltText { get; set; } = string.Empty;

    [NopResourceDisplayName("Plugins.Widgets.PopUp.IsEnabled")]
    public bool IsEnabled { get; set; }

    [NopResourceDisplayName("Plugins.Widgets.PopUp.DisplayPages")]
    public string DisplayPages { get; set; } = string.Empty;

    // Individual page properties for checkboxes
    [NopResourceDisplayName("Plugins.Widgets.PopUp.ShowOnHome")]
    public bool ShowOnHome { get; set; }

    [NopResourceDisplayName("Plugins.Widgets.PopUp.ShowOnCategory")]
    public bool ShowOnCategory { get; set; }

    [NopResourceDisplayName("Plugins.Widgets.PopUp.ShowOnProduct")]
    public bool ShowOnProduct { get; set; }

    [NopResourceDisplayName("Plugins.Widgets.PopUp.ShowOnManufacturer")]
    public bool ShowOnManufacturer { get; set; }

    [NopResourceDisplayName("Plugins.Widgets.PopUp.ShowOnTopic")]
    public bool ShowOnTopic { get; set; }

    [NopResourceDisplayName("Plugins.Widgets.PopUp.ShowOnBlog")]
    public bool ShowOnBlog { get; set; }

    [NopResourceDisplayName("Plugins.Widgets.PopUp.ShowOnNews")]
    public bool ShowOnNews { get; set; }

    [NopResourceDisplayName("Plugins.Widgets.PopUp.ShowOnAll")]
    public bool ShowOnAll { get; set; }

    [NopResourceDisplayName("Plugins.Widgets.PopUp.ShowOncePerSession")]
    public bool ShowOncePerSession { get; set; }

    #endregion
}
