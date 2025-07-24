using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using Nop.Core;
using Nop.Core.Caching;
using Nop.Plugin.Widgets.Swiper.Domain;
using Nop.Plugin.Widgets.Swiper.Infrastructure.Cache;
using Nop.Plugin.Widgets.Swiper.Models;
using Nop.Services.Configuration;
using Nop.Services.Media;
using Nop.Web.Framework.Components;

namespace Nop.Plugin.Widgets.Swiper.Components;

public class WidgetSwiperViewComponent : NopViewComponent
{
    #region Fields

    protected readonly IPictureService _pictureService;
    protected readonly IStaticCacheManager _staticCacheManager;
    protected readonly ISettingService _settingService;
    protected readonly IStoreContext _storeContext;
    protected readonly IWebHelper _webHelper;

    #endregion

    #region Ctor

    public WidgetSwiperViewComponent(IPictureService pictureService,
    IStaticCacheManager staticCacheManager,
    ISettingService settingService,
    IStoreContext storeContext,
    IWebHelper webHelper)
    {
        _pictureService = pictureService;
        _staticCacheManager = staticCacheManager;
        _settingService = settingService;
        _storeContext = storeContext;
        _webHelper = webHelper;
    }

    #endregion

    #region Utilities

    /// <returns>A task that represents the asynchronous operation</returns>
    private async Task<string> GetPictureUrlAsync(int pictureId)
    {
        if (pictureId == 0)
            return string.Empty;

        var cacheKey = _staticCacheManager.PrepareKeyForDefaultCache(ModelCacheEventConsumer.PictureUrlModelKey,
            pictureId, _webHelper.IsCurrentConnectionSecured());

        return await _staticCacheManager.GetAsync(cacheKey, async () =>
        {
            //little hack here. nulls aren't cacheable so set it to ""
            var url = await _pictureService.GetPictureUrlAsync(pictureId, showDefaultPicture: false) ?? "";
            return url;
        });
    }

    /// <summary>
    /// Extrae los hashtags de contexto del AltText
    /// </summary>
    private List<string> ExtractHashtagsFromAltText(string altText)
    {
        var hashtags = new List<string>();
        
        if (string.IsNullOrEmpty(altText))
            return hashtags;

        // Buscar todas las palabras que empiecen con #
        var words = altText.Split(new char[] { ' ', '\t', '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);
        
        foreach (var word in words)
        {
            if (word.StartsWith('#') && word.Length > 1)
            {
                var tag = word.Substring(1).ToUpperInvariant();
                tag = System.Text.RegularExpressions.Regex.Replace(tag, @"[^\w\d]", "");
        
                if (!string.IsNullOrEmpty(tag))
                {
                    hashtags.Add(tag);
                }
            }
        }

        return hashtags.Distinct().ToList();
    }

    /// <summary>
    /// Limpia el altText removiendo los hashtags de contexto
    /// </summary>
    private string CleanAltText(string altText)
    {
        if (string.IsNullOrEmpty(altText))
            return string.Empty;

        var cleanText = System.Text.RegularExpressions.Regex.Replace(altText, @"#\w+", "");
        cleanText = System.Text.RegularExpressions.Regex.Replace(cleanText, @"\s+", " ").Trim();
        
        return cleanText;
    }

    /// <summary>
    /// Verifica si un slide coincide con el contexto solicitado usando hashtags
    /// </summary>
    private bool SlideMatchesContext(List<string> slideHashtags, string requestedContext)
    {
        if (string.IsNullOrEmpty(requestedContext))
        {
            return !slideHashtags.Any() || slideHashtags.Contains("GENERAL");
        }

        return slideHashtags.Contains(requestedContext.ToUpperInvariant());
    }

    /// <summary>
    /// Verifica si un slide coincide con múltiples contextos solicitados
    /// </summary>
    private bool SlideMatchesAnyContext(List<string> slideHashtags, List<string> requestedContexts)
    {
        if (!requestedContexts.Any())
            return !slideHashtags.Any() || slideHashtags.Contains("GENERAL");

        return slideHashtags.Any(tag => requestedContexts.Contains(tag.ToUpperInvariant()));
    }

    /// <summary>
    /// Obtiene los identificadores de contexto desde additionalData
    /// </summary>
    private List<string> GetContextIdentifiers(object additionalData)
    {
        var contexts = new List<string>();
        
        if (additionalData == null)
            return contexts;

        if (additionalData is Dictionary<string, object> data)
        {
            // Soportar un solo contexto
            if (data.ContainsKey("Context"))
                contexts.Add(data["Context"].ToString().ToUpperInvariant());
            
            if (data.ContainsKey("SectionContext"))
                contexts.Add(data["SectionContext"].ToString().ToUpperInvariant());
                
            if (data.ContainsKey("Zone"))
                contexts.Add(data["Zone"].ToString().ToUpperInvariant());

            // Soportar múltiples contextos
            if (data.ContainsKey("Contexts") && data["Contexts"] is List<string> contextList)
            {
                contexts.AddRange(contextList.Select(c => c.ToUpperInvariant()));
            }
            
            if (data.ContainsKey("Contexts") && data["Contexts"] is string[] contextArray)
            {
                contexts.AddRange(contextArray.Select(c => c.ToUpperInvariant()));
            }
        }

        // Si additionalData es un string directo
        if (additionalData is string contextString)
            contexts.Add(contextString.ToUpperInvariant());

        // Si additionalData es una lista de strings
        if (additionalData is List<string> contextStringList)
            contexts.AddRange(contextStringList.Select(c => c.ToUpperInvariant()));

        return contexts.Distinct().ToList();
    }

    #endregion

    #region Methods

    /// <returns>A task that represents the asynchronous operation</returns>
    public async Task<IViewComponentResult> InvokeAsync(string widgetZone, object additionalData)
    {
        var store = await _storeContext.GetCurrentStoreAsync();
        var sliderSettings = await _settingService.LoadSettingAsync<SwiperSettings>(store.Id);

        if (string.IsNullOrEmpty(sliderSettings.Slides))
            return Content("");

        // Obtener los contextos solicitados
        var requestedContexts = GetContextIdentifiers(additionalData);

        var model = new PublicInfoModel
        {
            ShowNavigation = sliderSettings.ShowNavigation,
            ShowPagination = sliderSettings.ShowPagination,
            Autoplay = sliderSettings.Autoplay,
            AutoplayDelay = sliderSettings.AutoplayDelay,
        };

        var slides = JsonConvert.DeserializeObject<List<Slide>>(sliderSettings.Slides);
        
        foreach (var slide in slides)
        {
            var picUrl = await GetPictureUrlAsync(slide.PictureId);
            if (string.IsNullOrEmpty(picUrl))
                continue;

            // Extraer los hashtags del AltText
            var slideHashtags = ExtractHashtagsFromAltText(slide.AltText);

            // Filtrar por contexto usando hashtags
            if (!SlideMatchesAnyContext(slideHashtags, requestedContexts))
                continue;

            model.Slides.Add(new()
            {
                PictureUrl = picUrl,
                TitleText = slide.TitleText,
                LinkUrl = slide.LinkUrl,
                AltText = CleanAltText(slide.AltText),
                LazyLoading = sliderSettings.LazyLoading
            });
        }

        // Si no hay slides para este contexto, no mostrar nada
        if (!model.Slides.Any())
            return Content("");

        return View("~/Plugins/Widgets.Swiper/Views/PublicInfo.cshtml", model);
    }

    #endregion
}