using Nop.Services.ScheduleTasks;
using Nop.Plugin.Misc.PushNotifications.Services;
using System.Threading.Tasks;

namespace Nop.Plugin.Misc.PushNotifications.Tasks
{
    public class SendPushNotificationsTask : IScheduleTask
    {
        private readonly IPushNotificationService _pushNotificationService;
        private readonly INotificationStrategyExecutor _strategyExecutor;

        public SendPushNotificationsTask(IPushNotificationService pushNotificationService,
            INotificationStrategyExecutor strategyExecutor)
        {
            _pushNotificationService = pushNotificationService;
            _strategyExecutor = strategyExecutor;
        }

        public async Task ExecuteAsync()
        {
            var (title, body) = await _strategyExecutor.ExecuteRandomStrategyAsync();
            
            if (!string.IsNullOrEmpty(title) && !string.IsNullOrEmpty(body))
            {
                await _pushNotificationService.SendNotificationToAllAsync(title, body);
            }
        }
    }
}
