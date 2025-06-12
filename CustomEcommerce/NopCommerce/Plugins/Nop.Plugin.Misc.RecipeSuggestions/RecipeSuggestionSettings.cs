using Nop.Core.Configuration;

namespace Nop.Plugin.Misc.RecipeSuggestions;
public class RecipeSuggestionSettings : ISettings
{
    #region Properties
    public bool Enabled { get; set; }
    public string GeminiApiKey { get; set; }
    public int NewProductsBatchSize { get; set; }
    public int RefreshProductsBatchSize { get; set; }
    public int RefreshRecipeAgeDays { get; set; }
    public string ExcludeCategoryIds { get; set; }
    public string WidgetZone { get; set; }
    #endregion
}