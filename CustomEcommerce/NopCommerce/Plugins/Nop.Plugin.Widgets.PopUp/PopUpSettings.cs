using Nop.Core.Configuration;

namespace Nop.Plugin.Widgets.PopUp;

/// <summary>
/// Represents settings of the PopUp plugin
/// </summary>
public class PopUpSettings : ISettings
{
    /// <summary>
    /// Gets or sets picture identifier
    /// </summary>
    public int PictureId { get; set; }

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
    public bool IsEnabled { get; set; } = true;
}
