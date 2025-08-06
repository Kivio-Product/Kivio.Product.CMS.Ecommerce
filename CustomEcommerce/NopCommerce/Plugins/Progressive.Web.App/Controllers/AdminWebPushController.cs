using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Nop.Core;
using Nop.Core.Domain.Catalog;
using Nop.Core.Domain.Customers;
using Nop.Core.Domain.Orders;
using Nop.Plugin.Progressive.Web.App.Models;
using Nop.Plugin.Progressive.Web.App.Security;
using Nop.Plugin.Progressive.Web.App.Settings;
using Nop.Services.Catalog;
using Nop.Services.Configuration;
using Nop.Services.Customers;
using Nop.Services.Helpers;
using Nop.Services.Localization;
using Nop.Services.Security;
using Nop.Services.Stores;
using Nop.Services.Vendors;
using Nop.Web.Areas.Admin.Factories; 
using Nop.Web.Areas.Admin.Infrastructure.Mapper.Extensions; 
using Nop.Web.Areas.Admin.Models.Catalog; 
using Nop.Web.Areas.Admin.Models.Customers;
using Nop.Web.Framework;
using Nop.Web.Framework.Controllers;
using Nop.Web.Framework.Models.Extensions;
using Nop.Web.Framework.Mvc.Filters;
using ICustomerServiceExtend = Nop.Plugin.Progressive.Web.App.Services.ICustomerServiceExtend;

namespace Nop.Plugin.Progressive.Web.App.Controllers
{
    [Area(AreaNames.ADMIN)]
    public class AdminWebPushController : BasePluginController
    {
        #region fields
        private readonly ProgressiveWebAppSettings _progressiveWebAppSettings;
        private readonly ISettingService _settingService;
        private readonly ILocalizationService _localizationService;
        private readonly IPermissionService _permissionService;
        private readonly ICategoryService _categoryService;
        private readonly IManufacturerService _manufacturerService;
        private readonly IStoreService _storeService;
        private readonly IVendorService _vendorService;
        private readonly IProductService _productService;
        private readonly ICustomerService _customerService;
        private readonly ICustomerServiceExtend _customerServiceExtend;
        private readonly CustomerSettings _customerSettings;
        private readonly IDateTimeHelper _dateTimeHelper;
        private readonly ICategoryModelFactory _categoryModelFactory; 
        private readonly IProductModelFactory _productModelFactory; 
        #endregion

        #region ctor
        public AdminWebPushController(ProgressiveWebAppSettings progressiveWebAppSettings,
            ISettingService settingService,
            ILocalizationService localizationService,
            IPermissionService permissionService,
            ICategoryService categoryService,
            IStoreService storeService,
            IVendorService vendorService,
            IManufacturerService manufacturerService,
            IProductService productService,
            ICustomerServiceExtend customerServiceExtend,
            ICustomerService customerService,
            CustomerSettings customerSettings,
            IDateTimeHelper dateTimeHelper,
            ICategoryModelFactory categoryModelFactory,
            IProductModelFactory productModelFactory)
        {
            _progressiveWebAppSettings = progressiveWebAppSettings;
            _settingService = settingService;
            _localizationService = localizationService;
            _permissionService = permissionService;
            _categoryService = categoryService;
            _storeService = storeService;
            _vendorService = vendorService;
            _manufacturerService = manufacturerService;
            _productService = productService;
            _customerService = customerService;
            _customerServiceExtend = customerServiceExtend;
            _customerSettings = customerSettings;
            _dateTimeHelper = dateTimeHelper;
            _categoryModelFactory = categoryModelFactory; 
            _productModelFactory = productModelFactory; 
        }
        #endregion

        #region configuration
        [AuthorizeAdmin]
        [CheckPermission(StandardPermission.Configuration.MANAGE_PLUGINS)]
        public async Task<IActionResult> Configure()
        {
            var model = new ConfigurationModel
            {
                ProgressiveWebAppCode = _progressiveWebAppSettings.ProgressiveWebAppCode,
                ProgressiveWebAppHeaderTags = _progressiveWebAppSettings.ProgressiveWebAppHeaderTags,
                PushNotificationHtml = _progressiveWebAppSettings.PushNotificationHtml,
                PublicKey = _progressiveWebAppSettings.PublicKey,
                PrivateKey = _progressiveWebAppSettings.PrivateKey
            };
            return View("~/Plugins/Progressive.Web.App/Views/Configure.cshtml", model);
        }

        [HttpPost]
        [AuthorizeAdmin]
        [CheckPermission(StandardPermission.Configuration.MANAGE_PLUGINS)]
        public async Task<IActionResult> Configure(ConfigurationModel model)
        {
            _progressiveWebAppSettings.ProgressiveWebAppCode = model.ProgressiveWebAppCode;
            _progressiveWebAppSettings.ProgressiveWebAppHeaderTags = model.ProgressiveWebAppHeaderTags;
            _progressiveWebAppSettings.PushNotificationHtml = model.PushNotificationHtml;
            _progressiveWebAppSettings.PrivateKey = model.PrivateKey;
            _progressiveWebAppSettings.PublicKey = model.PublicKey;
            await _settingService.SaveSettingAsync(_progressiveWebAppSettings);
            TempData["nop.admin.notifications.success"] = await _localizationService.GetResourceAsync("Admin.Plugins.ProgressiveWebApp.Save.Config.Success");

            return await Configure();
        }
        #endregion

        #region OfferType
        public async Task<IActionResult> ProductAddPopupList()
        {
            if (!await _permissionService.AuthorizeAsync(StandardPermission.Catalog.PRODUCTS_CREATE_EDIT_DELETE) &&
                !await _permissionService.AuthorizeAsync(ProgressivePermissionProvider.ProgressivePermissionRecord))
                return AccessDeniedView();

            var model = await _productModelFactory.PrepareProductSearchModelAsync(new ProductSearchModel());

            var addOfferModel = new AddOfferTypeModel
            {
                AvailableCategories = model.AvailableCategories,
                AvailableManufacturers = model.AvailableManufacturers,
                AvailableStores = model.AvailableStores,
                AvailableVendors = model.AvailableVendors,
                AvailableProductTypes = model.AvailableProductTypes
            };

            return View("~/Plugins/Progressive.Web.App/Views/ProductAddPopup.cshtml", addOfferModel);
        }

        [HttpPost]
        public virtual async Task<IActionResult> ProductAddPopupList(ProductSearchModel searchModel)
        {
            if (!await _permissionService.AuthorizeAsync(StandardPermission.Catalog.PRODUCTS_CREATE_EDIT_DELETE) &&
                !await _permissionService.AuthorizeAsync(ProgressivePermissionProvider.ProgressivePermissionRecord))
                return AccessDeniedView();

            var productListModel = await _productModelFactory.PrepareProductListModelAsync(searchModel);
            return Json(productListModel);
        }

        public async Task<IActionResult> ProductAddPopup(int selectedOfferId)
        {
            if (!await _permissionService.AuthorizeAsync(StandardPermission.Catalog.PRODUCTS_CREATE_EDIT_DELETE) &&
                !await _permissionService.AuthorizeAsync(ProgressivePermissionProvider.ProgressivePermissionRecord))
                return AccessDeniedView();

            var model = new AddOfferTypeModel();
            if (selectedOfferId > 0)
            {
                var product = await _productService.GetProductByIdAsync(selectedOfferId);
                if (product != null)
                {
                    model.SelectedOfferId = product.Id;
                    model.OfferType = OfferType.Product;
                    model.OfferName = product.Name;
                }
            }

            model.SelectOfferType = true;
            return View("~/Plugins/Progressive.Web.App/Views/ProductAddPopup.cshtml", model);
        }

        public async Task<IActionResult> CategoryAddPopupList()
        {
            if (!await _permissionService.AuthorizeAsync(StandardPermission.Catalog.CATEGORIES_CREATE_EDIT_DELETE) &&
                !await _permissionService.AuthorizeAsync(ProgressivePermissionProvider.ProgressivePermissionRecord))
                return AccessDeniedView();

            var model = new AddOfferTypeModel();
            model.AvailableStores.Add(new SelectListItem { Text = await _localizationService.GetResourceAsync("Admin.Common.All"), Value = "0" });
            foreach (var s in await _storeService.GetAllStoresAsync())
                model.AvailableStores.Add(new SelectListItem { Text = s.Name, Value = s.Id.ToString() });

            return View("~/Plugins/Progressive.Web.App/Views/CategoryAddPopup.cshtml", model);
        }

        [HttpPost]
        public virtual async Task<IActionResult> CategoryAddPopupList(CategorySearchModel searchModel)
        {
            if (!await _permissionService.AuthorizeAsync(StandardPermission.Catalog.CATEGORIES_CREATE_EDIT_DELETE) &&
                !await _permissionService.AuthorizeAsync(ProgressivePermissionProvider.ProgressivePermissionRecord))
                return AccessDeniedView();

            var categoryListModel = await _categoryModelFactory.PrepareCategoryListModelAsync(searchModel);
            return Json(categoryListModel);
        }

        public async Task<IActionResult> CategoryAddPopup(int selectedOfferId)
        {
            if (!await _permissionService.AuthorizeAsync(StandardPermission.Catalog.CATEGORIES_CREATE_EDIT_DELETE) &&
                !await _permissionService.AuthorizeAsync(ProgressivePermissionProvider.ProgressivePermissionRecord))
                return AccessDeniedView();

            var model = new AddOfferTypeModel();
            if (selectedOfferId > 0)
            {
                var category = await _categoryService.GetCategoryByIdAsync(selectedOfferId);
                if (category != null)
                {
                    model.SelectedOfferId = category.Id;
                    model.OfferType = OfferType.Category;
                    model.OfferName = category.Name;
                }
            }

            model.SelectOfferType = true;
            return View("~/Plugins/Progressive.Web.App/Views/CategoryAddPopup.cshtml", model);
        }
        #endregion

        #region Customers
        public virtual async Task<IActionResult> List()
        {
            if (!await _permissionService.AuthorizeAsync(StandardPermission.Catalog.CATEGORIES_CREATE_EDIT_DELETE) &&
                !await _permissionService.AuthorizeAsync(ProgressivePermissionProvider.ProgressivePermissionRecord))
                return AccessDeniedView();

            var registeredRole = await _customerService.GetCustomerRoleBySystemNameAsync(NopCustomerDefaults.RegisteredRoleName);
            var defaultRoleIds = new List<int> { registeredRole.Id };

            var model = new OfferTypeModel
            {
                UsernamesEnabled = _customerSettings.UsernamesEnabled,
                DateOfBirthEnabled = _customerSettings.DateOfBirthEnabled,
                SearchCustomerRoleIds = defaultRoleIds,
            };

            var allRoles = await _customerService.GetAllCustomerRolesAsync(true);
            foreach (var role in allRoles)
            {
                model.AvailableCustomerRoles.Add(new SelectListItem
                {
                    Text = role.Name,
                    Value = role.Id.ToString(),
                    Selected = defaultRoleIds.Any(x => x == role.Id)
                });
            }

            return View("~/Plugins/Progressive.Web.App/Views/SentOffer.cshtml", model);
        }

        [HttpPost]
        public virtual async Task<IActionResult> CustomerList(CustomerSearchModel searchModel, OfferTypeModel offerModel, int[] searchCustomerRoleIds)
        {
            if (!await _permissionService.AuthorizeAsync(StandardPermission.Customers.CUSTOMERS_VIEW) &&
                !await _permissionService.AuthorizeAsync(ProgressivePermissionProvider.ProgressivePermissionRecord))
                return AccessDeniedView();

            int.TryParse(searchModel.SearchDayOfBirth, out var dayOfBirth);
            int.TryParse(searchModel.SearchMonthOfBirth, out var monthOfBirth);

            var customers =  _customerServiceExtend.GetAllCustomersExtend(
                customerRoleIds: searchCustomerRoleIds,
                email: searchModel.SearchEmail,
                username: searchModel.SearchUsername,
                dayOfBirth: dayOfBirth,
                monthOfBirth: monthOfBirth,
                ipAddress: searchModel.SearchIpAddress,
                loadOnlyWithShoppingCart: offerModel.HasShoppingCart || offerModel.HasWishList,
                sct: (offerModel.HasShoppingCart && offerModel.HasWishList) || (!offerModel.HasShoppingCart && !offerModel.HasWishList)
                    ? (ShoppingCartType?)null
                    : offerModel.HasShoppingCart
                        ? ShoppingCartType.ShoppingCart
                        : ShoppingCartType.Wishlist,
                hasOfferInShoppingCartOrWishlist: offerModel.HasOfferInShoppingCartOrWishlist,
                offerType: offerModel.OfferType,
                offerId: offerModel.OfferId,
                hasSubscription: offerModel.HasSubscription,
                pageIndex: searchModel.Page - 1,
                pageSize: searchModel.PageSize);

            var gridModel = await new CustomerListModel().PrepareToGridAsync(searchModel, customers, () =>
            {
                return customers.SelectAwait(async customer =>
                {
                    var customerModel = customer.ToModel<CustomerModel>();
                    customerModel.Email = customer.Email;
                    customerModel.FullName = customer.FirstName + customer.LastName;
                    customerModel.CustomerRoleNames = string.Join(", ", (await _customerService.GetCustomerRolesAsync(customer)).Select(role => role.Name));
                    customerModel.CreatedOn = await _dateTimeHelper.ConvertToUserTimeAsync(customer.CreatedOnUtc, DateTimeKind.Utc);
                    customerModel.LastActivityDate = await _dateTimeHelper.ConvertToUserTimeAsync(customer.LastActivityDateUtc, DateTimeKind.Utc);

                    return customerModel;
                });
            });

            return Json(gridModel);
        }
        #endregion
    }
}