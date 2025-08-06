using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Nop.Core.Infrastructure;
using Nop.Plugin.Progressive.Web.App.Services;

namespace Nop.Plugin.Progressive.Web.App.Infrastructure
{
    public class NopStartup : INopStartup
    {
        public void ConfigureServices(IServiceCollection services, IConfiguration configuration)
        {
            services.AddScoped<IProgressiveWebPushService, ProgressiveWebPushService>();
            services.AddScoped<ICustomerServiceExtend, CustomerServiceExtend>();
        }

        public void Configure(IApplicationBuilder application)
        {
        }

        public int Order => 11; 
    }
}