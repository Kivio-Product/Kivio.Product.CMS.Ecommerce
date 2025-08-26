using System.Threading.Tasks;

namespace Nop.Plugin.Misc.PushNotifications.Services
{
    public interface IWebPushService
    {
        Task SendWebPushNotificationAsync(string endpoint, string p256dh, string auth, string title, string body, string icon, string url);
    }
}
