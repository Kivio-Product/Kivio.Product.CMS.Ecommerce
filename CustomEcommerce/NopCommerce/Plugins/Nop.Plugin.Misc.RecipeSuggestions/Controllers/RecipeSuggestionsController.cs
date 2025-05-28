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

    public RecipeSuggestionsController(
        IRecipeSuggestionService recipeSuggestionService,
        ISettingService settingService,
        IStoreContext storeContext,
        INotificationService notificationService)
    {
        _recipeSuggestionService = recipeSuggestionService;
        _settingService = settingService;
        _storeContext = storeContext;
        _notificationService = notificationService;
    }


    [CheckPermission(StandardPermission.Configuration.MANAGE_WIDGETS)]
    public async Task<IActionResult> Configure()
    {
        var settings = await _settingService.LoadSettingAsync<RecipeSuggestionSettings>(_storeContext.GetCurrentStore().Id);
        return View("~/Plugins/Nop.Plugin.Misc.RecipeSuggestions/Views/Configure.cshtml", settings);
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
        settings.CacheExpiryMinutes = model.CacheExpiryMinutes;

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
        await _settingService.SaveSettingOverridablePerStoreAsync(
            settings,
            x => x.CacheExpiryMinutes,
            model.CacheExpiryMinutesOverrideForStore,
            storeScope,
            false);
        await _settingService.ClearCacheAsync();

        _notificationService.SuccessNotification(await _localizationService.GetResourceAsync("Admin.Plugins.Saved"));

        return await Configure();
    }
}