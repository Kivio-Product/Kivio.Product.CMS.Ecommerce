using System.ComponentModel.DataAnnotations;
using Nop.Web.Framework.Models;
using Nop.Web.Framework.Mvc.ModelBinding;

namespace Nop.Plugin.Widgets.PopUp.Models;

/// <summary>
/// Represents a popup image model for configuration
/// </summary>
public record PopUpImageModel : BaseNopModel
{
    #region Properties

    [UIHint("Picture")]
    [NopResourceDisplayName("Plugins.Widgets.PopUp.Picture")]
    public int PictureId { get; set; }

    public string PictureUrl { get; set; } = string.Empty;

    [NopResourceDisplayName("Plugins.Widgets.PopUp.TitleText")]
    public string TitleText { get; set; } = string.Empty;

    [NopResourceDisplayName("Plugins.Widgets.PopUp.LinkUrl")]
    public string LinkUrl { get; set; } = string.Empty;

    [NopResourceDisplayName("Plugins.Widgets.PopUp.AltText")]
    public string AltText { get; set; } = string.Empty;

    [NopResourceDisplayName("Plugins.Widgets.PopUp.Weekdays")]
    public string WeekdaysDisplay { get; set; } = string.Empty;

    // Weekday checkboxes (0=Sunday, 1=Monday, ..., 6=Saturday)
    [NopResourceDisplayName("Plugins.Widgets.PopUp.Weekday.Sunday")]
    public bool Sunday { get; set; }

    [NopResourceDisplayName("Plugins.Widgets.PopUp.Weekday.Monday")]
    public bool Monday { get; set; }

    [NopResourceDisplayName("Plugins.Widgets.PopUp.Weekday.Tuesday")]
    public bool Tuesday { get; set; }

    [NopResourceDisplayName("Plugins.Widgets.PopUp.Weekday.Wednesday")]
    public bool Wednesday { get; set; }

    [NopResourceDisplayName("Plugins.Widgets.PopUp.Weekday.Thursday")]
    public bool Thursday { get; set; }

    [NopResourceDisplayName("Plugins.Widgets.PopUp.Weekday.Friday")]
    public bool Friday { get; set; }

    [NopResourceDisplayName("Plugins.Widgets.PopUp.Weekday.Saturday")]
    public bool Saturday { get; set; }

    #endregion
}
