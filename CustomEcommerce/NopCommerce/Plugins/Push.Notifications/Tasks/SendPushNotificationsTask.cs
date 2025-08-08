using Nop.Services.ScheduleTasks;
using Nop.Plugin.Misc.PushNotifications.Services;
using System.Threading.Tasks;

namespace Nop.Plugin.Misc.PushNotifications.Tasks
{
    public class SendPushNotificationsTask : IScheduleTask
    {
        private readonly IPushNotificationService _pushNotificationService;

        public SendPushNotificationsTask(IPushNotificationService pushNotificationService)
        {
            _pushNotificationService = pushNotificationService;
        }

        public async Task ExecuteAsync()
        {
            await _pushNotificationService.SendNotificationToAllAsync("New Products!", "Check out our latest products.");
        }
    }
}
