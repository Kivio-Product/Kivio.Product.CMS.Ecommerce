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

    #endregion
}
