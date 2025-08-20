using Nop.Services.ScheduleTasks;
using Nop.Plugin.Misc.PushNotifications.Services;
using System.Threading.Tasks;
using Nop.Services.Configuration;
using Nop.Plugin.Misc.PushNotifications.Helpers;
using Nop.Core.Caching;
using System;

namespace Nop.Plugin.Misc.PushNotifications.Tasks
{
    public class SendPushNotificationsTask : IScheduleTask
    {
        private readonly IPushNotificationService _pushNotificationService;
        private readonly INotificationStrategyExecutor _strategyExecutor;
        private readonly ISettingService _settingService;
        private readonly IStaticCacheManager _staticCacheManager;
        private CacheKey LastSentCacheKey = new("Nop.Plugin.Misc.PushNotifications.LastSent");

        public SendPushNotificationsTask(IPushNotificationService pushNotificationService,
            INotificationStrategyExecutor strategyExecutor,
            ISettingService settingService,
            IStaticCacheManager staticCacheManager)
        {
            _pushNotificationService = pushNotificationService;
            _strategyExecutor = strategyExecutor;
            _settingService = settingService;
            _staticCacheManager = staticCacheManager;
        }

        public async Task ExecuteAsync()
        {
            // Check scheduling window
            var settings = await _settingService.LoadSettingAsync<PushNotificationsSettings>();
            if (!ScheduleHelper.IsAllowedNow(settings.AllowedDays, settings.AllowedHours, settings.UseUtcTime))
            {
                return;
            }

            // Check min hours between notifications throttle
            if (settings.MinHoursBetweenNotifications > 0)
            {
                LastSentCacheKey.CacheTime = settings.MinHoursBetweenNotifications * 60; // in minutes
                var now = settings.UseUtcTime ? DateTime.UtcNow : DateTime.Now;
                var lastSent = await _staticCacheManager.GetAsync<DateTime>(
                    LastSentCacheKey,
                    () => Task.FromResult(DateTime.MinValue)
                );

                if (lastSent != DateTime.MinValue)
                {
                    var elapsedHours = (now - lastSent).TotalHours;
                    if (elapsedHours < settings.MinHoursBetweenNotifications)
                    {
                        return;
                    }
                }
            }

            var (title, body, url) = await _strategyExecutor.ExecuteRandomStrategyAsync();
            
            if (!string.IsNullOrEmpty(title) && !string.IsNullOrEmpty(body))
            {
                await _pushNotificationService.SendNotificationToAllAsync(title, body, url);
                var nowSent = settings.UseUtcTime ? DateTime.UtcNow : DateTime.Now;
                await _staticCacheManager.SetAsync(LastSentCacheKey, nowSent);
            }
        }
    }
}
