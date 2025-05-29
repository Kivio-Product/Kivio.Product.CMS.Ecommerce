using Microsoft.AspNetCore.Mvc;
using Nop.Core;
using Nop.Services.Security;
using Nop.Plugin.Misc.RecipeSuggestions;
using Nop.Plugin.Misc.RecipeSuggestions.Interfaces;
using Nop.Services.Configuration;
using Nop.Services.Messages;
using Nop.Web.Framework;
using Nop.Web.Framework.Controllers;
using Nop.Web.Framework.Mvc.Filters;
using Nop.Plugin.Misc.RecipeSuggestions.Models;
using Nop.Services.Localization;

namespace Nop.Plugin.Misc.RecipeSuggestions.Controllers;

[AuthorizeAdmin]
[Area(AreaNames.ADMIN)]
[AutoValidateAntiforgeryToken]
public class RecipeSuggestionsController : BasePluginController
{
    private readonly IRecipeSuggestionService _recipeSuggestionService;
    private readonly ISettingService _settingService;
    private readonly IStoreContext _storeContext;
    private readonly INotificationService _notificationService;
    private readonly ILocalizationService _localizationService;

    public RecipeSuggestionsController(
        IRecipeSuggestionService recipeSuggestionService,
        ISettingService settingService,
        IStoreContext storeContext,
        INotificationService notificationService,
        ILocalizationService localizationService)
    {
        _recipeSuggestionService = recipeSuggestionService;
        _settingService = settingService;
        _storeContext = storeContext;
        _notificationService = notificationService;
        _localizationService = localizationService;
    }


    [CheckPermission(StandardPermission.Configuration.MANAGE_WIDGETS)]
    public async Task<IActionResult> Configure()
    {
        var storeScope = await _storeContext.GetActiveStoreScopeConfigurationAsync();
        var settings = await _settingService.LoadSettingAsync<RecipeSuggestionSettings>(storeScope);

        var model = new ConfigurationModel
        {
            Enabled = settings.Enabled,
            WidgetZone = settings.WidgetZone,
            NewProductsBatchSize = settings.NewProductsBatchSize,
            RefreshProductsBatchSize = settings.RefreshProductsBatchSize,
            RefreshRecipeAgeDays = settings.RefreshRecipeAgeDays,
            GeminiApiKey = settings.GeminiApiKey,
            EnabledOverrideForStore = await _settingService.GetSettingOverridablePerStoreAsync(settings, x => x.Enabled, storeScope),
            WidgetZoneOverrideForStore = await _settingService.GetSettingOverridablePerStoreAsync(settings, x => x.WidgetZone, storeScope),
            NewProductsBatchSizeOverrideForStore = await _settingService.GetSettingOverridablePerStoreAsync(settings, x => x.NewProductsBatchSize, storeScope),
            RefreshProductsBatchSizeOverrideForStore = await _settingService.GetSettingOverridablePerStoreAsync(settings, x => x.RefreshProductsBatchSize, storeScope),
            RefreshRecipeAgeDaysOverrideForStore = await _settingService.GetSettingOverridablePerStoreAsync(settings, x => x.RefreshRecipeAgeDays, storeScope),
        };

        model.ActiveStoreScopeConfiguration = storeScope;
        if (storeScope > 0)
        {
            model.Enabled = await _settingService.GetSettingOverridablePerStoreAsync(settings, x => x.Enabled, storeScope);
            model.WidgetZone = await _settingService.GetSettingOverridablePerStoreAsync(settings, x => x.WidgetZone, storeScope);
            model.NewProductsBatchSize = await _settingService.GetSettingOverridablePerStoreAsync(settings, x => x.NewProductsBatchSize, storeScope);
            model.RefreshProductsBatchSize = await _settingService.GetSettingOverridablePerStoreAsync(settings, x => x.RefreshProductsBatchSize, storeScope);
            model.RefreshRecipeAgeDays = await _settingService.GetSettingOverridablePerStoreAsync(settings, x => x.RefreshRecipeAgeDays, storeScope);
        }
        return View("~/Plugins/Misc.RecipeSuggestions/Views/Configure.cshtml", settings);
    }

    [HttpPost]
    [CheckPermission(StandardPermission.Configuration.MANAGE_WIDGETS)]
    public async Task<IActionResult> Configure(ConfigurationModel model)
    {
        var storeScope = await _storeContext.GetActiveStoreScopeConfigurationAsync();
        var settings = await _settingService.LoadSettingAsync<RecipeSuggestionSettings>(storeScope);
        settings.Enabled = model.Enabled;
        settings.WidgetZone = model.WidgetZone;
        settings.NewProductsBatchSize = model.NewProductsBatchSize;
        settings.RefreshProductsBatchSize = model.RefreshProductsBatchSize;
        settings.RefreshRecipeAgeDays = model.RefreshRecipeAgeDays;

        await _settingService.SaveSettingOverridablePerStoreAsync(
            settings,
            x => x.Enabled,
            model.EnabledOverrideForStore,
            storeScope,
            false);
        await _settingService.SaveSettingOverridablePerStoreAsync(
            settings,
            x => x.WidgetZone,
            model.WidgetZoneOverrideForStore,
            storeScope,
            false);
        await _settingService.SaveSettingOverridablePerStoreAsync(
            settings,
            x => x.NewProductsBatchSize,
            model.NewProductsBatchSizeOverrideForStore,
            storeScope,
            false);
        await _settingService.SaveSettingOverridablePerStoreAsync(
            settings,
            x => x.RefreshProductsBatchSize,
            model.RefreshProductsBatchSizeOverrideForStore,
            storeScope,
            false);
        await _settingService.SaveSettingOverridablePerStoreAsync(
            settings,
            x => x.RefreshRecipeAgeDays,
            model.RefreshRecipeAgeDaysOverrideForStore,
            storeScope,
            false);
        await _settingService.ClearCacheAsync();

        _notificationService.SuccessNotification(await _localizationService.GetResourceAsync("Admin.Plugins.Saved"));

        return await Configure();
    }
}