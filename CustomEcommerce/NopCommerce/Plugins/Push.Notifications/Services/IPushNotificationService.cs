using System.Collections.Generic;
using System.Threading.Tasks;
using Nop.Plugin.Misc.PushNotifications.Domain;
using Nop.Plugin.Misc.PushNotifications.Models;

namespace Nop.Plugin.Misc.PushNotifications.Services
{
    public interface IPushNotificationService
    {
        Task RegisterDeviceAsync(int customerId, PushSubscriptionModel subscriptionModel);
        Task RegisterDeviceAsync(int customerId, string token);
        Task SendNotificationToAllAsync(string title, string body, string url = "/");
        Task SendUniqueNotification(string title, string body, string token, string url = "/");
        Task LogNotificationAsync(PushNotificationLog log);
        Task<IList<PushNotificationLog>> GetLogsByStrategyTypeAsync(string strategyType);
    }
}
