using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Nop.Core.Infrastructure;
using Nop.Plugin.Misc.PushNotifications.Services;

namespace Nop.Plugin.Misc.PushNotifications.Infrastructure
{
    public class NopStartup : INopStartup
    {
        public void ConfigureServices(IServiceCollection services, IConfiguration configuration)
        {
            services.AddScoped<IPushNotificationService, PushNotificationService>();
        }

        public void Configure(IApplicationBuilder application)
        {
        }

        public int Order => 10001;
    }
}
