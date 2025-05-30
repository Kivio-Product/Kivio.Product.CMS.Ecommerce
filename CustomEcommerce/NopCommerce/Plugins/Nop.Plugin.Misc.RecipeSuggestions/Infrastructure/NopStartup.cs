using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Nop.Core.Infrastructure;
using DotnetGeminiSDK.Client.Interfaces;
using Nop.Plugin.Misc.RecipeSuggestions.Interfaces;
using Nop.Plugin.Misc.RecipeSuggestions.Services;
using DotnetGeminiSDK.Config;
using DotnetGeminiSDK.Client;
using DotnetGeminiSDK.Requester;
using DotnetGeminiSDK.Requester.Interfaces;
using Nop.Services.Configuration;
using Microsoft.IdentityModel.Tokens;

namespace Nop.Plugin.Misc.RecipeSuggestions.Infrastructure;

public class NopStartup : INopStartup
{
    public void ConfigureServices(IServiceCollection services, IConfiguration configuration)
    {
        services.AddScoped<IAIRecipeService, AIRecipeService>();
        services.AddScoped<IRecipeSuggestionService, RecipeSuggestionService>();
        services.AddScoped<ICacheService, CacheService>();


        // Gemini SDK configuration
        var serviceProvider = services.BuildServiceProvider();
        var settingService = serviceProvider.GetRequiredService<ISettingService>();
        var recipeSuggestionsSettings = settingService.LoadSetting<RecipeSuggestionSettings>();

        string geminiApiKey = recipeSuggestionsSettings.GeminiApiKey;
        var config = new GoogleGeminiConfig()
        {
            ApiKey = geminiApiKey.IsNullOrEmpty() ? "" : geminiApiKey.Trim(),
            TextBaseUrl = "https://generativelanguage.googleapis.com/v1beta/models/gemini-2.5-flash-preview-05-20",
            ImageBaseUrl = "https://generativelanguage.googleapis.com/v1beta/models/gemini-2.5-flash-preview-05-20",
            GenerateImageBaseURL = "https://generativelanguage.googleapis.com/v1beta/models/gemini-2.0-flash-preview-image-generation"
        };

        services.AddSingleton(config);
        services.AddSingleton<IApiRequester, ApiRequester>();
        services.AddSingleton<IGeminiClient, GeminiClient>();
    }
    public void Configure(IApplicationBuilder application)
    {

    }
    public int Order => 10001; // Define the order of this startup configuration
}
