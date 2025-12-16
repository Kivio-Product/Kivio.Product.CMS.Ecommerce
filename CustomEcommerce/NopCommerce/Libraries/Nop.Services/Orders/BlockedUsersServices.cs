using Nop.Core;
using Nop.Services.Configuration;

public class BlockedCustomerService : IBlockedCustomerService
{
    private readonly IWorkContext _workContext;
    private readonly ISettingService _settingService;


    public BlockedCustomerService(IWorkContext workContext,
        ISettingService settingService)
    {
        _workContext = workContext;
        _settingService = settingService;
    }
    public async Task<bool> IsCustomerBlocked()
    {
        var customer = await _workContext.GetCurrentCustomerAsync();
        if (customer == null)
            return false;

        var blockedUserIps = await _settingService.GetSettingByKeyAsync<string>("BlockedUserIps", "");
        if (string.IsNullOrWhiteSpace(blockedUserIps))
            return false;

        var blockedUser = blockedUserIps.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
            .FirstOrDefault(ip => ip.Trim().Equals(customer.LastIpAddress, StringComparison.InvariantCultureIgnoreCase));
        
        if (blockedUser != null)
            return true;

        return false;
    }
}