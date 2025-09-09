
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Nop.Core.Infrastructure;
using Nop.Plugin.Widgets.PopUp.Services;

namespace Nop.Plugin.Widgets.PopUp.Infrastructure;

public class NopStartup : INopStartup
{
    public void ConfigureServices(IServiceCollection services, IConfiguration configuration)
    {
        services.AddScoped<IPageTypeService, PageTypeService>();
    }
    public void Configure(IApplicationBuilder application)
    {
    }
    public int Order => 10001; // Define the order of this startup configuration
}