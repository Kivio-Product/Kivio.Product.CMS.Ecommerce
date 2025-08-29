using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Nop.Core.Infrastructure;
using Plugin.ElectronicInvoice.SIIGO.Services;

namespace Plugin.ElectronicInvoice.SIIGO.Infrastructure
{
    public class PluginNopStartup : INopStartup
    {
        public void ConfigureServices(IServiceCollection services, IConfiguration configuration)
        {
            services.AddScoped<ISiigoInvoiceService, SiigoInvoiceService>();
            services.AddScoped<ISiigoNotificationService, SiigoNotificationService>();
            services.AddHttpClient<SiigoInvoiceService>();
        }

        public void Configure(IApplicationBuilder application)
        {
        }

        public int Order => 11;
    }
}
