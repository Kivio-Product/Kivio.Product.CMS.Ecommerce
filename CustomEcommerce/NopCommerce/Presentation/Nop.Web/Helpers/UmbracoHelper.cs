using Microsoft.Extensions.Configuration;

namespace Nop.Web.Helpers;

public class UmbracoHelper
{
    private readonly IConfiguration _configuration;
    private string nopCommerceCssFolder;
    private string nopCommerceCssFile;
    private string umbracoUrl;
    private bool isEnabled;

    private const string TITLE_ENPOINT = "/umbraco/api/assets/title";
    private const string LOGO_ENPOINT = "/umbraco/api/assets/logo";
    private const string FAVICON_ENPOINT = "/umbraco/api/assets/favicon";
    private const string CSS_ENPOINT = "/umbraco/api/assets/css?";

    public UmbracoHelper(IConfiguration configuration)
    {
        _configuration = configuration;
        nopCommerceCssFolder = _configuration.GetValue<string>("KivioModules:Umbraco:CssFolder") ?? string.Empty;
        nopCommerceCssFile = _configuration.GetValue<string>("KivioModules:Umbraco:CssFiles") ?? string.Empty;
        umbracoUrl = _configuration.GetValue<string>("KivioModules:Umbraco:BaseUrl") ?? string.Empty;
        isEnabled = _configuration.GetValue<bool>("KivioModules:Umbraco:Enabled");
    }

    /// <summary>
    /// Verifica si el módulo Umbraco está habilitado y 
    /// si el sitio de Umbraco responde correctamente.
    /// </summary>
    public async Task<bool> IsUmbracoEnabledAsync()
    {

        if (!isEnabled)
            return false;

        if (string.IsNullOrEmpty(umbracoUrl))
            return false;

        try
        {
            using var httpClient = new HttpClient();
            var response = await httpClient.GetAsync(umbracoUrl);
            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Devuelve la URL completa para el Logo
    /// tomando la respuesta de /umbraco/api/assets/logo.
    /// </summary>
    public async Task<string?> GetLogoUrlAsync()
    {
        if (!await IsUmbracoEnabledAsync())
            return null;

        string endpoint = $"{umbracoUrl.TrimEnd('/')}{LOGO_ENPOINT}";

        try
        {
            using var httpClient = new HttpClient();
            string relativePath = await httpClient.GetStringAsync(endpoint);

            return CombineUrl(umbracoUrl, relativePath);
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Devuelve la URL completa para el Favicon
    /// tomando la respuesta de /umbraco/api/assets/favicon.
    /// </summary>
    public async Task<string?> GetFaviconUrlAsync()
    {
        if (!await IsUmbracoEnabledAsync())
            return null;

        string endpoint = $"{umbracoUrl.TrimEnd('/')}{FAVICON_ENPOINT}";

        try
        {
            using var httpClient = new HttpClient();
            string relativePath = await httpClient.GetStringAsync(endpoint);

            return CombineUrl(umbracoUrl, relativePath);
        }
        catch
        {
            return null;
        }
    }

    public async Task<string?> GetTitleAsync()
    {
        if (!await IsUmbracoEnabledAsync())
            return null;

        string endpoint = $"{umbracoUrl.TrimEnd('/')}{TITLE_ENPOINT}";

        try
        {
            using var httpClient = new HttpClient();
            return await httpClient.GetStringAsync(endpoint);
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Devuelve la ruta de los archivos CSS
    /// especificados en la configuración.
    /// </summary>
    public async Task<string?[]?> GetCssAsync()
    {
        if (!await IsUmbracoEnabledAsync())
            return null;

        string[] cssFiles = nopCommerceCssFile.Split(',');
        List<string?> cssContents = [];

        foreach (var cssFile in cssFiles)
        {
            string endpoint = $"{umbracoUrl.TrimEnd('/')}{CSS_ENPOINT}folder={nopCommerceCssFolder}&file={cssFile.Trim()}";

            try
            {
                using var httpClient = new HttpClient();
                string content = await httpClient.GetStringAsync(endpoint);
                cssContents.Add(content);
            }
            catch
            {
                cssContents.Add(null);
            }
        }

        return [.. cssContents];
    }

    /// <summary>
    /// Métodos de utilidad para evitar problemas de slashes en la concatenación.
    /// </summary>
    private static string CombineUrl(string baseUrl, string relativePath)
    {
        string cleanBase = baseUrl.TrimEnd('/');
        string cleanRelative = relativePath.TrimStart('/');
        return $"{cleanBase}/{cleanRelative}";
    }
}
