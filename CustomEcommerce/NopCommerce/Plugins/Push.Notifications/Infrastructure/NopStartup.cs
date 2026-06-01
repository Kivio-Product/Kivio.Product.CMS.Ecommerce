using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Nop.Core.Infrastructure;
using Nop.Plugin.Misc.PushNotifications.Services;
using Nop.Plugin.Misc.PushNotifications.Strategies;
using DotnetGeminiSDK.Client.Interfaces;
using DotnetGeminiSDK.Config;
using DotnetGeminiSDK.Client;
using DotnetGeminiSDK.Requester;
using DotnetGeminiSDK.Requester.Interfaces;
using Nop.Services.Configuration;
using Microsoft.IdentityModel.Tokens;
using System.Collections.Generic;

namespace Nop.Plugin.Misc.PushNotifications.Infrastructure
{
    public class NopStartup : INopStartup
    {
        public void ConfigureServices(IServiceCollection services, IConfiguration configuration)
        {
            services.AddScoped<IPushNotificationService, PushNotificationService>();
            services.AddScoped<IWebPushService, WebPushService>();
            
            // Register settings
            services.AddScoped(provider =>
            {
                var settingService = provider.GetRequiredService<ISettingService>();
                return settingService.LoadSetting<PushNotificationsSettings>();
            });
            
            // Register notification strategies as concrete types
            services.AddScoped<NewProductWithDiscountStrategy>();
            services.AddScoped<CategoryBasedStrategy>();
            services.AddScoped<CustomStrategy>();
            
            // Register strategy factory
            services.AddScoped<INotificationStrategyExecutor>(provider =>
            {
                var strategies = new List<INotificationStrategy>
                {
                    provider.GetRequiredService<NewProductWithDiscountStrategy>(),
                    provider.GetRequiredService<CategoryBasedStrategy>(),
                    provider.GetRequiredService<CustomStrategy>()
                };
                return new NotificationStrategyExecutor(strategies);
            });

            // Gemini SDK configuration
            var serviceProvider = services.BuildServiceProvider();
            var settingService = serviceProvider.GetRequiredService<ISettingService>();
            var pushNotificationsSettings = settingService.LoadSetting<PushNotificationsSettings>();

            string geminiApiKey = pushNotificationsSettings.GeminiApiKey;
            var config = new GoogleGeminiConfig()
            {
                ApiKey = geminiApiKey.IsNullOrEmpty() ? "" : geminiApiKey.Trim(),
                TextBaseUrl = "https://generativelanguage.googleapis.com/v1beta/models/gemini-flash-lite-latest",
                ImageBaseUrl = "https://generativelanguage.googleapis.com/v1beta/models/gemini-flash-lite-latest",
                GenerateImageBaseURL = "https://generativelanguage.googleapis.com/v1beta/models/gemini-2.0-flash-preview-image-generation"
            };

            services.AddSingleton(config);
            services.AddSingleton<IApiRequester, ApiRequester>();
            services.AddSingleton<IGeminiClient, GeminiClient>();
        }

        public void Configure(IApplicationBuilder application)
        {
        }

        public int Order => 10001;
    }
}
