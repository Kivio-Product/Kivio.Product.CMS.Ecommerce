using Nop.Web.Framework.Models;
using Nop.Web.Framework.Mvc.ModelBinding;

namespace Nop.Plugin.Misc.RecipeSuggestions.Models
{
    public record ConfigurationModel : BaseNopModel
    {
        public int ActiveStoreScopeConfiguration { get; set; }

        [NopResourceDisplayName("Plugins.Misc.RecipeSuggestions.Settings.Enabled")]
        public bool Enabled { get; set; }
        public bool Enabled_OverrideForStore { get; set; }

        [NopResourceDisplayName("Plugins.Misc.RecipeSuggestions.Settings.GeminiApiKey")]
        public string GeminiApiKey { get; set; }
        public bool GeminiApiKey_OverrideForStore { get; set; }
        [NopResourceDisplayName("Plugins.Misc.RecipeSuggestions.Settings.NewProductsBatchSize")]
        public int NewProductsBatchSize { get; set; }
        public bool NewProductsBatchSize_OverrideForStore { get; set; }
        [NopResourceDisplayName("Plugins.Misc.RecipeSuggestions.Settings.RefreshProductsBatchSize")]
        public int RefreshProductsBatchSize { get; set; }
        public bool RefreshProductsBatchSize_OverrideForStore { get; set; }
        [NopResourceDisplayName("Plugins.Misc.RecipeSuggestions.Settings.RefreshRecipeAgeDays")]
        public int RefreshRecipeAgeDays { get; set; }
        public bool RefreshRecipeAgeDays_OverrideForStore { get; set; }
        [NopResourceDisplayName("Plugins.Misc.RecipeSuggestions.Settings.WidgetZone")]
        public string WidgetZone { get; set; }
        public bool WidgetZone_OverrideForStore { get; set; }

    }
}
