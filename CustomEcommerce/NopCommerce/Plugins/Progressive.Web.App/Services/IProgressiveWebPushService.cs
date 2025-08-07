using System.Collections.Generic;
using System.Threading.Tasks;
using Nop.Plugin.Progressive.Web.App.Domain;

namespace Nop.Plugin.Progressive.Web.App.Services
{
    public interface IProgressiveWebPushService
    {
        Task<SubscriptionRecord> GetSubscriptionByCustomerIdAsync(int customerId);
        Task CreateSubscriptionAsync(SubscriptionRecord subscriptionRecord);
        Task RemoveSubscriptionByCustomerIdAsync(int customerId);
        Task RemoveSubscriptionAsync(SubscriptionRecord subscriptionRecord);
        Task UpdateSubscriptionAsync(SubscriptionRecord sub);
        Task<List<SubscriptionRecord>> GetSubscriptionByCustomerIdsAsync(int[] customerIds);
        Task<List<int>> GetSubscriptionsCustomerIdsAsync();
    }
}