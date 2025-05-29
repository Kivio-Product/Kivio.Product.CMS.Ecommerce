using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Nop.Core.Infrastructure;
using Nop.Plugin.Misc.RecipeSuggestions.Interfaces;
using Nop.Plugin.Misc.RecipeSuggestions.Services;

namespace Nop.Plugin.Misc.RecipeSuggestions.Infrastructure;

public class NopStartup : INopStartup
{
    public void ConfigureServices(IServiceCollection services, IConfiguration configuration)
    {
        services.AddScoped<IGeminiRecipeService, GeminiRecipeService>();
        services.AddScoped<IRecipeSuggestionService, RecipeSuggestionService>();
        services.AddScoped<ICacheService, CacheService>();
    }
    public void Configure(IApplicationBuilder application)
    {

    }
    public int Order => 3001; // Define the order of this startup configuration
}
