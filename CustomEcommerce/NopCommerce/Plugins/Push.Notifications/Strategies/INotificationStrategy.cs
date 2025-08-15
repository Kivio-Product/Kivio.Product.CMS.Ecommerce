using System.Threading.Tasks;

namespace Nop.Plugin.Misc.PushNotifications.Strategies
{
    public interface INotificationStrategy
    {
        Task<bool> CanExecuteAsync();
        Task<(string Title, string Body, string Url)> GenerateNotificationAsync();
        string StrategyType { get; }
    }
}
