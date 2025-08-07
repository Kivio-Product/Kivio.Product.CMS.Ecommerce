using System;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Nop.Core;
using Nop.Core.Domain.Customers;
using Nop.Core.Domain.Orders;
using Nop.Data;
using Nop.Plugin.Progressive.Web.App.Models;
using Nop.Services.Catalog;

namespace Nop.Plugin.Progressive.Web.App.Services
{
    public partial class CustomerServiceExtend : ICustomerServiceExtend
    {
        private readonly IRepository<Customer> _customerRepository;
        private readonly IRepository<ShoppingCartItem> _shoppingCartItemRepository;
        private readonly ICategoryService _categoryService;
        private readonly IProgressiveWebPushService _progressiveWebPushService;

        public CustomerServiceExtend(
            IRepository<Customer> customerRepository,
            IRepository<ShoppingCartItem> shoppingCartItemRepository,
            ICategoryService categoryService, 
            IProgressiveWebPushService progressiveWebPushService)
        {
            _customerRepository = customerRepository;
            _shoppingCartItemRepository = shoppingCartItemRepository;
            _categoryService = categoryService;
            _progressiveWebPushService = progressiveWebPushService;
        }

        public virtual async Task<IPagedList<Customer>> GetAllCustomersExtendAsync(DateTime? createdFromUtc = null,
            DateTime? createdToUtc = null, int affiliateId = 0, int vendorId = 0,
            int[] customerRoleIds = null, string email = null, string username = null,
            string firstName = null, string lastName = null,
            int dayOfBirth = 0, int monthOfBirth = 0,
            string company = null, string phone = null, string zipPostalCode = null,
            string ipAddress = null, bool loadOnlyWithShoppingCart = false, ShoppingCartType? sct = null,
            bool hasOfferInShoppingCartOrWishlist = false, OfferType offerType = OfferType.Product, int offerId = 0,
            bool hasSubscription = true, int pageIndex = 0, int pageSize = int.MaxValue)
        {
            var query = _customerRepository.Table;
            if (createdFromUtc.HasValue)
                query = query.Where(c => createdFromUtc.Value <= c.CreatedOnUtc);
            if (createdToUtc.HasValue)
                query = query.Where(c => createdToUtc.Value >= c.CreatedOnUtc);
            if (affiliateId > 0)
                query = query.Where(c => affiliateId == c.AffiliateId);
            if (vendorId > 0)
                query = query.Where(c => vendorId == c.VendorId);
            query = query.Where(c => !c.Deleted);
            if (!string.IsNullOrWhiteSpace(email))
                query = query.Where(c => c.Email.Contains(email));
            if (!string.IsNullOrWhiteSpace(username))
                query = query.Where(c => c.Username.Contains(username));

            if (!string.IsNullOrWhiteSpace(firstName))
                query = query.Where(c => c.FirstName.Contains(firstName));

            if (!string.IsNullOrWhiteSpace(lastName))
                query = query.Where(c => c.LastName.Contains(lastName));

            if (dayOfBirth > 0 && monthOfBirth > 0)
                query = query.Where(c => c.DateOfBirth.HasValue && c.DateOfBirth.Value.Day == dayOfBirth && c.DateOfBirth.Value.Month == monthOfBirth);
            else if (dayOfBirth > 0)
                query = query.Where(c => c.DateOfBirth.HasValue && c.DateOfBirth.Value.Day == dayOfBirth);
            else if (monthOfBirth > 0)
                query = query.Where(c => c.DateOfBirth.HasValue && c.DateOfBirth.Value.Month == monthOfBirth);

            if (!string.IsNullOrWhiteSpace(company))
                query = query.Where(c => c.Company.Contains(company));

            if (!string.IsNullOrWhiteSpace(phone))
                query = query.Where(c => c.Phone.Contains(phone));

            if (!string.IsNullOrWhiteSpace(zipPostalCode))
                query = query.Where(c => c.ZipPostalCode.Contains(zipPostalCode));

            if (!string.IsNullOrWhiteSpace(ipAddress) && IPAddress.TryParse(ipAddress, out _))
            {
                query = query.Where(w => w.LastIpAddress == ipAddress);
            }

            if (loadOnlyWithShoppingCart)
            {
                var shoppingCartQuery = _shoppingCartItemRepository.Table;
                if (sct.HasValue)
                {
                    var sctId = (int)sct.Value;
                    shoppingCartQuery = shoppingCartQuery.Where(sci => sci.ShoppingCartTypeId == sctId);
                }

                if (hasOfferInShoppingCartOrWishlist && offerId > 0)
                {
                    if (offerType == OfferType.Category)
                    {
                        var productIds = (await _categoryService.GetProductCategoriesByCategoryIdAsync(offerId))
                            .Select(pc => pc.ProductId);
                        shoppingCartQuery = shoppingCartQuery.Where(sci => productIds.Contains(sci.ProductId));
                    }
                    else
                    {
                        shoppingCartQuery = shoppingCartQuery.Where(sci => sci.ProductId == offerId);
                    }
                }

                var customerIdsWithCart = shoppingCartQuery.Select(sci => sci.CustomerId).Distinct();
                query = query.Where(c => customerIdsWithCart.Contains(c.Id));
            }

            if (hasSubscription)
            {
                var subscriptionsCustomersIds = await _progressiveWebPushService.GetSubscriptionsCustomerIdsAsync();
                query = query.Where(x => subscriptionsCustomersIds.Contains(x.Id));
            }

            query = query.OrderByDescending(c => c.CreatedOnUtc);

            return await query.ToPagedListAsync(pageIndex, pageSize);
        }
    }
}
