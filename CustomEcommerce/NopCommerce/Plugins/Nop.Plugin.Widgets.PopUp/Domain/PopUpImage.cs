namespace Nop.Plugin.Widgets.PopUp.Domain;

/// <summary>
/// Represents a popup image with assigned weekdays
/// </summary>
public class PopUpImage
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
    /// Gets or sets the weekdays when this image should be displayed (0=Sunday, 1=Monday, ..., 6=Saturday)
    /// Multiple days can be assigned to the same image, but a day cannot have multiple images
    /// </summary>
    public List<int> Weekdays { get; set; } = [];
}
