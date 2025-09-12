using Nop.Web.Framework.Models;

namespace Nop.Plugin.Widgets.PopUp.Models;

/// <summary>
/// Represents a public model for PopUp widget
/// </summary>
public record PublicInfoModel : BaseNopModel
{
    #region Properties

    /// <summary>
    /// Gets or sets picture URL
    /// </summary>
    public string PictureUrl { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the image title
    /// </summary>
    public string TitleText { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the link URL
    /// </summary>
    public string LinkUrl { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the image alternate text
    /// </summary>
    public string AltText { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets a value indicating whether the popup is enabled
    /// </summary>
    public bool IsEnabled { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether to show popup only once per browser session
    /// </summary>
    public bool ShowOncePerSession { get; set; }

    #endregion
}
