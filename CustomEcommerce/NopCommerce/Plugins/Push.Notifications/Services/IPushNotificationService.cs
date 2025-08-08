using System.Threading.Tasks;

namespace Nop.Plugin.Misc.PushNotifications.Services
{
    public interface IPushNotificationService
    {
        Task RegisterDeviceAsync(int customerId, string token);
        Task SendNotificationToAllAsync(string title, string body);
    }
}
