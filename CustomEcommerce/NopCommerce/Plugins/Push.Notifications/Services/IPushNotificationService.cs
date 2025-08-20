using System.Collections.Generic;
using System.Threading.Tasks;
using Nop.Plugin.Misc.PushNotifications.Domain;

namespace Nop.Plugin.Misc.PushNotifications.Services
{
    public interface IPushNotificationService
    {
        Task RegisterDeviceAsync(int customerId, string token);
        Task SendNotificationToAllAsync(string title, string body, string url = "/");
        Task LogNotificationAsync(PushNotificationLog log);
        Task<IList<PushNotificationLog>> GetLogsByStrategyTypeAsync(string strategyType);
    }
}
