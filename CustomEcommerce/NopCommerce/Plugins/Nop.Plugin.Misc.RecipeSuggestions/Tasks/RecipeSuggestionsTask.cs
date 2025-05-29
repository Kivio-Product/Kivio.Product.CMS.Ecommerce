using Nop.Core;
using Nop.Plugin.Misc.RecipeSuggestions;
using Nop.Plugin.Misc.RecipeSuggestions.Interfaces;
using Nop.Services.Configuration;
using Nop.Services.ScheduleTasks;

public partial class RecipeSuggestionsTask : IScheduleTask
{
    private readonly IRecipeSuggestionService _recipeSuggestionService;
    private readonly ISettingService _settingService;
    private readonly IStoreContext _storeContext;
    private RecipeSuggestionSettings _settings;

    public RecipeSuggestionsTask(IRecipeSuggestionService recipeSuggestionService, 
                                RecipeSuggestionSettings settings,
                                ISettingService settingService,
                                IStoreContext storeContext)
    {
        _recipeSuggestionService = recipeSuggestionService;
        _settings = settings;
        _settingService = settingService;
        _storeContext = storeContext;
    }

    public async Task ExecuteAsync()
    {
        var settings = await _settingService.LoadSettingAsync<RecipeSuggestionSettings>(_storeContext.GetCurrentStore().Id);

        // If not enabled, skip execution
        if (!_settings.Enabled)
        {
            return;
        }
        // Fetch new products and generate recipe suggestions
        await _recipeSuggestionService.GenerateRecipeSuggestionsForNewProductsAsync(_settings.NewProductsBatchSize);

        // Refresh existing recipe suggestions
        await _recipeSuggestionService.RefreshRecipeSuggestionsAsync(_settings.RefreshProductsBatchSize, _settings.RefreshRecipeAgeDays);
    }
}