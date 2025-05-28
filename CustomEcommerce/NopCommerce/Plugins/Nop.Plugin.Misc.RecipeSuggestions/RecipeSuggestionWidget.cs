using Nop.Core; // For IWebHelper, IStoreContext
using Nop.Plugin.Misc.RecipeSuggestions.Models; // For RecipeSuggestionSettings
using Nop.Services.Cms; // For IWidgetPlugin, PublicWidgetZones
using Nop.Services.Configuration; // For ISettingService
using Nop.Services.Localization; // For ILocalizationService, LocaleStringResource
using Nop.Services.Plugins; // For BasePlugin
using System.Collections.Generic;
using System.Threading.Tasks;

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

        public bool HideInWidgetList => false; // Set to true if you don't want it to appear in the admin widget list

        public async Task<IList<string>> GetWidgetZonesAsync()
        {
            // Make this configurable later via settings if needed.
            // For now, hardcode to productdetails_bottom or use the setting.
            var settings = await _settingService.LoadSettingAsync<RecipeSuggestionSettings>(_storeContext.GetCurrentStore().Id);
            string zone = !string.IsNullOrWhiteSpace(settings?.WidgetZone) ? settings.WidgetZone : PublicWidgetZones.ProductDetailsBottom;
            
            return new List<string> { zone };
        }

        public string GetPublicViewComponentName()
        {
            return "RecipeSuggestion"; // This should match the ViewComponent name
        }

        public override string GetConfigurationPageUrl()
        {
            return $"{_webHelper.GetStoreLocation()}Admin/RecipeSuggestionAdmin/Configure"; // Matches the controller route
        }

        public override async Task InstallAsync()
        {
            // Default settings
            var settings = new RecipeSuggestionSettings
            {
                GeminiApiKey = "YOUR_API_KEY_HERE",
                RecipeApiEndpoint = "https://api.example.com/recipes",
                ImageApiEndpoint = "https://api.example.com/images",
                CacheExpiryMinutes = 720, // 12 hours
                ScheduledTaskTime = "02:00:00", // 2 AM
                NewProductsBatchSize = 100,
                RefreshProductsBatchSize = 50,
                RefreshRecipeAgeDays = 30,
                WidgetZone = PublicWidgetZones.ProductDetailsBottom 
            };
            await _settingService.SaveSettingAsync(settings);

            // Localization resources
            await _localizationService.AddOrUpdateLocaleResourceAsync(new Dictionary<string, string>
            {
                // Plugin Info
                ["Plugins.Misc.RecipeSuggestions.FriendlyName"] = "Recipe Suggestions",
                // Configuration Page
                ["Plugins.Misc.RecipeSuggestions.Settings.GeminiApiKey"] = "Gemini API Key",
                ["Plugins.Misc.RecipeSuggestions.Settings.GeminiApiKey.Hint"] = "Enter your API key for the Gemini service.",
                ["Plugins.Misc.RecipeSuggestions.Settings.RecipeApiEndpoint"] = "Recipe API Endpoint",
                ["Plugins.Misc.RecipeSuggestions.Settings.RecipeApiEndpoint.Hint"] = "The URL for the recipe generation API.",
                ["Plugins.Misc.RecipeSuggestions.Settings.ImageApiEndpoint"] = "Image API Endpoint",
                ["Plugins.Misc.RecipeSuggestions.Settings.ImageApiEndpoint.Hint"] = "The URL for the image generation API.",
                ["Plugins.Misc.RecipeSuggestions.Settings.CacheExpiryMinutes"] = "Cache Expiry (minutes)",
                ["Plugins.Misc.RecipeSuggestions.Settings.CacheExpiryMinutes.Hint"] = "How long to cache recipe suggestions.",
                ["Plugins.Misc.RecipeSuggestions.Settings.ScheduledTaskTime"] = "Scheduled Task Time (HH:mm:ss)",
                ["Plugins.Misc.RecipeSuggestions.Settings.ScheduledTaskTime.Hint"] = "Time when the daily recipe update task runs (e.g., 02:00:00 for 2 AM).",
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
    }
}
