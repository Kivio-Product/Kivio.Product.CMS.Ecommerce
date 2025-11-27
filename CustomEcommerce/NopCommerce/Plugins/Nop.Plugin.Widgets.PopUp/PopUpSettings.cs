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

    /// <summary>
    /// Gets or sets the pages where the popup should appear (comma-separated list)
    /// Available options: home, category, product, manufacturer, topic, blog, newsitem, all
    /// </summary>
    public string DisplayPages { get; set; } = "home";

    /// <summary>
    /// Gets or sets a value indicating whether to show popup only once per browser session
    /// </summary>
    public bool ShowOncePerSession { get; set; } = true;

    /// <summary>
    /// Gets or sets the images with their assigned weekdays (serialized as JSON)
    /// Format: List of PopUpImage objects with PictureId, TitleText, LinkUrl, AltText, and Weekdays
    /// </summary>
    public string ImagesJson { get; set; } = string.Empty;
}
