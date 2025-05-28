using System.Threading.Tasks;
public partial class RecipeSuggestionsTask : IScheduleTask
{
    private readonly IRecipeSuggestionService _recipeSuggestionService;
    private readonly ArtificialIntelligenceSettings _settings;

    public RecipeSuggestionsTask(IRecipeSuggestionService recipeSuggestionService, ArtificialIntelligenceSettings settings)
    {
        _recipeSuggestionService = recipeSuggestionService;
        _settings = settings;
    }

    public async Task ExecuteAsync()
    {
        // Ensure the settings are loaded
        if (_settings == null)
        {
            _settings = await _recipeSuggestionService.LoadSettingsAsync();
        }

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