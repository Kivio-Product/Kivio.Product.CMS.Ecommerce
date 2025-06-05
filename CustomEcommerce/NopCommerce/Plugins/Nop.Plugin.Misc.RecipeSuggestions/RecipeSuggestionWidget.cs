using Nop.Core;
using Nop.Plugin.Misc.RecipeSuggestions.Components;
using Nop.Services.Cms; 
using Nop.Services.Configuration; 
using Nop.Services.Localization;
using Nop.Services.Plugins;
using Nop.Web.Framework.Infrastructure;

namespace Nop.Plugin.Misc.RecipeSuggestions
{
    public class RecipeSuggestionWidget : BasePlugin, IWidgetPlugin
    {
        private readonly ISettingService _settingService;
        private readonly ILocalizationService _localizationService;
        private readonly IWebHelper _webHelper;
        private readonly IStoreContext _storeContext;

        public RecipeSuggestionWidget(ISettingService settingService,
                                      ILocalizationService localizationService,
                                      IWebHelper webHelper,
                                      IStoreContext storeContext)
        {
            _settingService = settingService;
            _localizationService = localizationService;
            _webHelper = webHelper;
            _storeContext = storeContext;
        }

        public bool HideInWidgetList => false; 

        public async Task<IList<string>> GetWidgetZonesAsync()
        {
            var settings = await _settingService.LoadSettingAsync<RecipeSuggestionSettings>(_storeContext.GetCurrentStore().Id);
            string zone = !string.IsNullOrWhiteSpace(settings?.WidgetZone) ? settings.WidgetZone : PublicWidgetZones.ProductDetailsBottom;
            
            return new List<string> { zone };
        }

        public string GetPublicViewComponentName()
        {
            return "RecipeSuggestion"; 
        }

        public override string GetConfigurationPageUrl()
        {
            return $"{_webHelper.GetStoreLocation()}Admin/RecipeSuggestions/Configure";
        }

        public override async Task InstallAsync()
        {
            var settings = new RecipeSuggestionSettings
            {
                NewProductsBatchSize = 5,
                RefreshProductsBatchSize = 5,
                RefreshRecipeAgeDays = 7,
                WidgetZone = PublicWidgetZones.ProductDetailsBottom,
                GeminiApiKey = "Your-Gemini-API-Key-Here"
            };
            await _settingService.SaveSettingAsync(settings);

            await _localizationService.AddOrUpdateLocaleResourceAsync(new Dictionary<string, string>
            {
                // Plugin Info
                ["Plugins.Misc.RecipeSuggestions.FriendlyName"] = "Recipe Suggestions",
                // Configuration Page
                ["Plugins.Misc.RecipeSuggestions.Settings.Enabled"] = "Enabled",
                ["Plugins.Misc.RecipeSuggestions.Settings.Enabled.Hint"] = "Enable or disable the recipe suggestions feature.",
                ["Plugins.Misc.RecipeSuggestions.Settings.GeminiApiKey"] = "Gemini API Key (Required restart application)",
                ["Plugins.Misc.RecipeSuggestions.Settings.GeminiApiKey.Hint"] = "Enter your API key for the Gemini service.",
                ["Plugins.Misc.RecipeSuggestions.Settings.NewProductsBatchSize"] = "New Products Batch Size",
                ["Plugins.Misc.RecipeSuggestions.Settings.NewProductsBatchSize.Hint"] = "Max number of new products to process in each task run.",
                ["Plugins.Misc.RecipeSuggestions.Settings.RefreshProductsBatchSize"] = "Refresh Products Batch Size",
                ["Plugins.Misc.RecipeSuggestions.Settings.RefreshProductsBatchSize.Hint"] = "Max number of old recipes to refresh in each task run.",
                ["Plugins.Misc.RecipeSuggestions.Settings.RefreshRecipeAgeDays"] = "Refresh Recipe Age (days)",
                ["Plugins.Misc.RecipeSuggestions.Settings.RefreshRecipeAgeDays.Hint"] = "How old a recipe must be (in days) to be considered for refresh.",
                ["Plugins.Misc.RecipeSuggestions.Settings.WidgetZone"] = "Widget Zone",
                ["Plugins.Misc.RecipeSuggestions.Settings.WidgetZone.Hint"] = "The widget zone where the recipe suggestions will be displayed (e.g., productdetails_bottom).",
                // Public View
                ["Plugins.Misc.RecipeSuggestions.PublicView.Title"] = "Suggested Recipe",
                ["Plugins.Misc.RecipeSuggestions.PublicView.NewIngredient"] = "(New Ingredient)",
                ["Plugins.Misc.RecipeSuggestions.PublicView.ViewProduct"] = "View Product",

            });

            await base.InstallAsync();
        }

        public override async Task UninstallAsync()
        {
            // Remove settings
            await _settingService.DeleteSettingAsync<RecipeSuggestionSettings>();

            // Remove localization resources
            await _localizationService.DeleteLocaleResourcesAsync("Plugins.Misc.RecipeSuggestions");
            
            await base.UninstallAsync();
        }

        public Type GetWidgetViewComponent(string widgetZone)
        {
            return typeof(RecipeSuggestionViewComponent);
        }
    }
}
