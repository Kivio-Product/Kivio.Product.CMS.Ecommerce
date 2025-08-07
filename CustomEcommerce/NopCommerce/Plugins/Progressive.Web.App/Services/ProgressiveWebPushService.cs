using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Nop.Data;
using Nop.Plugin.Progressive.Web.App.Domain;

namespace Nop.Plugin.Progressive.Web.App.Services
{
    public class ProgressiveWebPushService : IProgressiveWebPushService
    {
        private readonly IRepository<SubscriptionRecord> _subscritionRepository;

        public ProgressiveWebPushService(IRepository<SubscriptionRecord> subscritionRepository)
        {
            _subscritionRepository = subscritionRepository;
        }

        public async Task<SubscriptionRecord> GetSubscriptionByCustomerIdAsync(int customerId)
        {
            if (customerId <= 0)
                throw new Exception("Invalid customerId");
            return await _subscritionRepository.Table.FirstOrDefaultAsync(x => x.CustomerId == customerId);
        }

        public async Task<List<SubscriptionRecord>> GetSubscriptionByCustomerIdsAsync(int[] customerIds)
        {
            if (customerIds == null)
                throw new NullReferenceException(nameof(customerIds));
            return await _subscritionRepository.Table.Where(x => customerIds.Contains(x.CustomerId)).ToListAsync();
        }

        public async Task<List<int>> GetSubscriptionsCustomerIdsAsync()
        {
            return await _subscritionRepository.Table.Select(x => x.CustomerId).ToListAsync();
        }

        public async Task CreateSubscriptionAsync(SubscriptionRecord subscriptionRecord)
        {
            if (subscriptionRecord == null)
                throw new ArgumentNullException(nameof(subscriptionRecord));

            await _subscritionRepository.InsertAsync(subscriptionRecord);
        }

        public async Task RemoveSubscriptionAsync(SubscriptionRecord subscriptionRecord)
        {
            if (subscriptionRecord == null)
                throw new ArgumentNullException(nameof(subscriptionRecord));

            await _subscritionRepository.DeleteAsync(subscriptionRecord);
        }

        public async Task UpdateSubscriptionAsync(SubscriptionRecord subscriptionRecord)
        {
            if (subscriptionRecord == null)
                throw new ArgumentNullException(nameof(subscriptionRecord));

            await _subscritionRepository.UpdateAsync(subscriptionRecord);
        }

        public async Task RemoveSubscriptionByCustomerIdAsync(int customerId)
        {
            if (customerId <= 0)
                throw new Exception("Invalid customerId");

            var subscription = await GetSubscriptionByCustomerIdAsync(customerId);
            if (subscription != null)
                await RemoveSubscriptionAsync(subscription);
        }
    }
}