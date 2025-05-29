using Nop.Web.Framework.Models;
using Nop.Web.Framework.Mvc.ModelBinding;

namespace Nop.Plugin.Misc.RecipeSuggestions.Models
{
    public record ConfigurationModel : BaseNopModel
    {
        public int ActiveStoreScopeConfiguration { get; set; }

        [NopResourceDisplayName("Plugins.Misc.RecipeSuggestions.Settings.Enabled")]
        public bool Enabled { get; set; }
        public bool EnabledOverrideForStore { get; set; }

        [NopResourceDisplayName("Plugins.Misc.RecipeSuggestions.Settings.GeminiApiKey")]
        public string GeminiApiKey { get; set; }
        public bool GeminiApiKeyOverrideForStore { get; set; }
        [NopResourceDisplayName("Plugins.Misc.RecipeSuggestions.Settings.NewProductsBatchSize")]
        public int NewProductsBatchSize { get; set; }
        public bool NewProductsBatchSizeOverrideForStore { get; set; }
        [NopResourceDisplayName("Plugins.Misc.RecipeSuggestions.Settings.RefreshProductsBatchSize")]
        public int RefreshProductsBatchSize { get; set; }
        public bool RefreshProductsBatchSizeOverrideForStore { get; set; }
        [NopResourceDisplayName("Plugins.Misc.RecipeSuggestions.Settings.RefreshRecipeAgeDays")]
        public int RefreshRecipeAgeDays { get; set; }
        public bool RefreshRecipeAgeDaysOverrideForStore { get; set; }
        [NopResourceDisplayName("Plugins.Misc.RecipeSuggestions.Settings.WidgetZone")]
        public string WidgetZone { get; set; }
        public bool WidgetZoneOverrideForStore { get; set; }

    }
}
