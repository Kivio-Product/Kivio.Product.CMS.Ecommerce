using System.Collections.Generic;
using System.Threading.Tasks; // <-- Cambiado/Añadido
using Nop.Core;
using Nop.Core.Domain.Customers;
using Nop.Plugin.Progressive.Web.App.Data;
using Nop.Plugin.Progressive.Web.App.Security;
using Nop.Plugin.Progressive.Web.App.Settings;
using Nop.Services.Cms;
using Nop.Services.Configuration;
using Nop.Services.Customers;
using Nop.Services.Localization;
using Nop.Services.Security;
using Nop.Web.Framework.Menu;
using Nop.Services.Plugins;
using Nop.Plugin.Progressive.Web.App.Components;

namespace Nop.Plugin.Progressive.Web.App
{
    public class ProgressiveWebAppPlugin : BasePlugin, IWidgetPlugin
    {
        #region fields
        private readonly ISettingService _settingService;
        private readonly ProgressiveWebAppSettings _progressiveWebAppSettings;
        private readonly IWebHelper _webHelper;
        private readonly ICustomerService _customerService;
        private readonly ILocalizationService _localizationService; // <-- Añadido para la localización asíncrona

        public bool HideInWidgetList => false;
        #endregion

        #region ctor
        public ProgressiveWebAppPlugin(ISettingService settingService,
                                     ProgressiveWebAppSettings progressiveWebAppSettings,
                                     IWebHelper webHelper,
                                     ICustomerService customerService,
                                     ILocalizationService localizationService) 
        {
            _settingService = settingService;
            _progressiveWebAppSettings = progressiveWebAppSettings;
            _webHelper = webHelper;
            _customerService = customerService;
            _localizationService = localizationService; 
        }
        #endregion

        #region plugin methods
        public Task<IList<string>> GetWidgetZonesAsync()
        {
            var zones = new List<string>()
            {
                "head_html_tag",
                "body_end_html_tag_before",
                "header_selectors"
            };
            return Task.FromResult<IList<string>>(zones);
        }

        public string GetConfigurationPageUrl()
        {
            // Esta implementación sigue siendo válida
            return $"{_webHelper.GetStoreLocation()}Admin/AdminWebPush/Configure";
        }
        #endregion

        #region installation
        public override async Task InstallAsync()
        {
            // Instalar roles del sistema
            await InstallProgressiveSystemRoleAsync();

            // Añadir recursos de localización de forma asíncrona
            await _localizationService.AddOrUpdateLocaleResourceAsync(new Dictionary<string, string>
            {
                ["Admin.Common.Or"] = "Or",
                ["Admin.Common.Name"] = "Name",
                ["Admin.Plugins.ProgressiveWebApp.HasShoppingCart"] = "Has Cart Items",
                ["Admin.Plugins.ProgressiveWebApp.HasWishlist"] = "Has Wishlist Items",
                ["Admin.Plugins.ProgressiveWebApp.Send.Offer"] = "Send Offer Notification",
                ["Admin.Plugins.ProgressiveWebApp.Send.Offer.Button"] = "Send Notification",
                ["Admin.Plugins.ProgressiveWebApp.HasOfferInShoppingCartOrWishlist"] = "Has Offer Type To Cart Or Wishlist",
                ["Admin.Plugins.ProgressiveWebApp.HasSubscription"] = "Has Active Subscription",
                ["Admin.Plugins.ProgressiveWebApp.Offer.Type"] = "Offer Type For Send",
                ["Admin.Plugins.ProgressiveWebApp.Product.AddNew"] = "Add Product To Offer",
                ["Admin.Plugins.ProgressiveWebApp.Category.AddNew"] = "Add Category To Offer",
                ["Admin.Plugins.ProgressiveWebApp.Customers"] = "Customers To Send Offer",
                ["Admin.Plugins.ProgressiveWebApp.Products.AddToOffer"] = "Add Product To Offer",
                ["Admin.Plugins.ProgressiveWebApp.Category.AddToOffer"] = "Add Category To Offer",
                ["Admin.Plugins.ProgressiveWebApp.Category.Fields.Name"] = "Name",
                ["Admin.Plugins.ProgressiveWebApp.Category.Fields.Published"] = "Published",
                ["Admin.Plugins.ProgressiveWebApp.Web.App.Code"] = "Html Script-CSS Sources",
                ["Admin.Plugins.ProgressiveWebApp.Web.HeaderTags"] = "Html Header Tag Sources",
                ["Admin.Plugins.ProgressiveWebApp.Web.Push.Notification.Html"] = "Html For Notification Icon Header",
                ["Admin.Plugins.ProgressiveWebApp.Save.Config.Success"] = "Configuration Saved Succefully",
                ["Admin.Plugins.ProgressiveWebApp.Web.Push.PublicKey"] = "Your WebPush PublicKey",
                ["Admin.Plugins.ProgressiveWebApp.Web.Push.PrivateKey"] = "Your WebPush PrivateKey"
            });

            var rootPluginFolder = "/Plugins/Progressive.Web.App";

            var progressiveWebAppCode = $@"<script src='{rootPluginFolder}/Content/Scripts/pwa-db.js' type='text/javascript'></script>
                                            <script src='{rootPluginFolder}/Content/Scripts/pwa-client.js' type='text/javascript'></script>
                                            <script src='{rootPluginFolder}/Content/Scripts/pwa-push-notification.js' type='text/javascript'></script>
                                            <script src='{rootPluginFolder}/Content/Scripts/pwa-sw.js' type='text/javascript'></script>
                                            <script src='{rootPluginFolder}/Content/Scripts/pwa-site.js' type='text/javascript'></script>
                                            <link href='{rootPluginFolder}/Content/Fonts/font-awesome-4.7.0/css/font-awesome.min.css' rel='stylesheet' type='text/css'>
                                            <link href='{rootPluginFolder}/Content/Css/site.css' rel='stylesheet' type='text/css'>";

            var progressiveWebAppHeaderTags = $@"<link rel='manifest' href='{rootPluginFolder}/Content/manifest.json'>
                                                <link rel='apple-touch-icon' sizes='180x180' href='{rootPluginFolder}/Content/Icons/apple-touch-icon.png'>
                                                <link rel='icon' type='image/png' sizes='32x32' href='{rootPluginFolder}/Content/Icons/favicon-32x32.png'>
                                                <link rel='icon' type='image/png' sizes='16x16' href='{rootPluginFolder}/Content/Icons/favicon-16x16.png'>
                                                <link rel='mask-icon' href='{rootPluginFolder}/Content/Icons/safari-pinned-tab.svg' color='#286893'>
                                                <meta name='theme-color' content = '#286893'> ";

            var pushNotificationHtml = @"<div id='notifybtn'><i id='notifyicon' class='fa'></i></div>
                                         <input type='hidden' id ='push-notification-publickey' name='push-notification-publickey' value='{push-notification-publickey-value}'/>";

            _progressiveWebAppSettings.ProgressiveWebAppHeaderTags = progressiveWebAppHeaderTags;
            _progressiveWebAppSettings.ProgressiveWebAppCode = progressiveWebAppCode;
            _progressiveWebAppSettings.PushNotificationHtml = pushNotificationHtml;

            // Guardar configuración de forma asíncrona
            await _settingService.SaveSettingAsync(_progressiveWebAppSettings);

            await base.InstallAsync();
        }

        public override async Task UninstallAsync()
        {
            // Eliminar recursos de localización
            await _localizationService.DeleteLocaleResourcesAsync("Admin.Common.Or");
            await _localizationService.DeleteLocaleResourcesAsync("Admin.Common.Name");
            await _localizationService.DeleteLocaleResourcesAsync("Admin.Plugins.ProgressiveWebApp.HasShoppingCart");
            await _localizationService.DeleteLocaleResourcesAsync("Admin.Plugins.ProgressiveWebApp.HasWishlist");
            await _localizationService.DeleteLocaleResourcesAsync("Admin.Plugins.ProgressiveWebApp.Send.Offer");
            await _localizationService.DeleteLocaleResourcesAsync("Admin.Plugins.ProgressiveWebApp.Send.Offer.Button");
            await _localizationService.DeleteLocaleResourcesAsync("Admin.Plugins.ProgressiveWebApp.HasOfferInShoppingCartOrWishlist");
            await _localizationService.DeleteLocaleResourcesAsync("Admin.Plugins.ProgressiveWebApp.Offer.Type");
            await _localizationService.DeleteLocaleResourcesAsync("Admin.Plugins.ProgressiveWebApp.Product.AddNew");
            await _localizationService.DeleteLocaleResourcesAsync("Admin.Plugins.ProgressiveWebApp.Category.AddNew");
            await _localizationService.DeleteLocaleResourcesAsync("Admin.Plugins.ProgressiveWebApp.Customers");
            await _localizationService.DeleteLocaleResourcesAsync("Admin.Plugins.ProgressiveWebApp.Products.AddToOffer");
            await _localizationService.DeleteLocaleResourcesAsync("Admin.Plugins.ProgressiveWebApp.Category.AddToOffer");
            await _localizationService.DeleteLocaleResourcesAsync("Admin.Plugins.ProgressiveWebApp.Category.Fields.Name");
            await _localizationService.DeleteLocaleResourcesAsync("Admin.Plugins.ProgressiveWebApp.Category.Fields.Published"); // Corregido el resource name
            await _localizationService.DeleteLocaleResourcesAsync("Admin.Plugins.ProgressiveWebApp.Web.App.Code");
            await _localizationService.DeleteLocaleResourcesAsync("Admin.Plugins.ProgressiveWebApp.Web.HeaderTags");
            await _localizationService.DeleteLocaleResourcesAsync("Admin.Plugins.ProgressiveWebApp.Web.Push.Notification.Html");
            await _localizationService.DeleteLocaleResourcesAsync("Admin.Plugins.ProgressiveWebApp.Save.Config.Success");
            await _localizationService.DeleteLocaleResourcesAsync("Admin.Plugins.ProgressiveWebApp.HasSubscription");
            await _localizationService.DeleteLocaleResourcesAsync("Admin.Plugins.ProgressiveWebApp.Web.Push.PublicKey");
            await _localizationService.DeleteLocaleResourcesAsync("Admin.Plugins.ProgressiveWebApp.Web.Push.PrivateKey");

            // Desinstalar roles del sistema
            await UninstallProgressiveSystemRoleAsync();

            await base.UninstallAsync();
        }

        protected virtual async Task InstallProgressiveSystemRoleAsync()
        {
            var roles = ProgressiveAppSystemNames.GetSystemRoleNames();
            foreach (var roleName in roles)
            {
                var role = await _customerService.GetCustomerRoleBySystemNameAsync(roleName);
                if (role == null)
                {
                    await _customerService.InsertCustomerRoleAsync(new CustomerRole()
                    {
                        Name = roleName,
                        SystemName = roleName,
                        Active = true,
                        IsSystemRole = false,
                    });
                }
            }
        }

        protected virtual async Task UninstallProgressiveSystemRoleAsync()
        {
            var roles = ProgressiveAppSystemNames.GetSystemRoleNames();
            foreach (var roleName in roles)
            {
                var role = await _customerService.GetCustomerRoleBySystemNameAsync(roleName);
                if (role != null)
                    await _customerService.DeleteCustomerRoleAsync(role);
            }
        }

        public Type GetWidgetViewComponent(string widgetZone)
        {
            Type viewComponent = widgetZone switch
            {
                "head_html_tag" => typeof(ProgressiveWebAppHeaderTagsViewComponent),
                "body_end_html_tag_before" => typeof(ProgressiveWebAppCodeViewComponent),
                "home_page_top" => typeof(PushNotificationViewComponent),
                _ => typeof(PushNotificationViewComponent),
            };

            return viewComponent;
        }
        #endregion
    }
}