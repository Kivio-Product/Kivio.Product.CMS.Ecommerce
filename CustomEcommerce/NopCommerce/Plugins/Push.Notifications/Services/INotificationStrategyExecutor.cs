using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Nop.Plugin.Misc.PushNotifications.Strategies;

namespace Nop.Plugin.Misc.PushNotifications.Services
{
    public interface INotificationStrategyExecutor
    {
        Task<(string Title, string Body, string Url)> ExecuteRandomStrategyAsync();
    }

    public class NotificationStrategyExecutor : INotificationStrategyExecutor
    {
        private readonly IEnumerable<INotificationStrategy> _strategies;
        private readonly Random _random;

        public NotificationStrategyExecutor(IEnumerable<INotificationStrategy> strategies)
        {
            _strategies = strategies;
            _random = new Random();
        }

    public async Task<(string Title, string Body, string Url)> ExecuteRandomStrategyAsync()
        {
            var availableStrategies = new List<INotificationStrategy>();

            foreach (var strategy in _strategies)
            {
                if (await strategy.CanExecuteAsync())
                {
                    availableStrategies.Add(strategy);
                }
            }

            if (!availableStrategies.Any())
                return (null, null, null);

            var selectedStrategy = availableStrategies[_random.Next(availableStrategies.Count)];
            return await selectedStrategy.GenerateNotificationAsync();
        }
    }
}
