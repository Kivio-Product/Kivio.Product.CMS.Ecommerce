using Newtonsoft.Json;
using Nop.Services.Configuration;
using Nop.Services.Logging;
using System.Text;

namespace Plugin.ElectronicInvoice.SIIGO.Services
{
    public class SiigoAuthService : ISiigoAuthService
    {
        private readonly ISettingService _settingService;
        private readonly ILogger _logger;
        private readonly HttpClient _httpClient;
        
        // Token cache
        private static string _cachedToken;
        private static DateTime _tokenExpiry = DateTime.MinValue;
        private static readonly object _lockObject = new object();

        public SiigoAuthService(
            ISettingService settingService,
            ILogger logger,
            HttpClient httpClient)
        {
            _settingService = settingService;
            _logger = logger;
            _httpClient = httpClient;
        }

        public async Task<string> GetValidTokenAsync()
        {
            lock (_lockObject)
            {
                // Check if we have a valid cached token
                if (!string.IsNullOrEmpty(_cachedToken) && DateTime.Now < _tokenExpiry)
                {
                    return _cachedToken;
                }
            }

            // Token is expired or doesn't exist, refresh it
            return await RefreshTokenAsync();
        }

        public async Task<string> RefreshTokenAsync()
        {
            try
            {
                var siigoSettings = await _settingService.LoadSettingAsync<SiigoSettings>();
                
                if (string.IsNullOrEmpty(siigoSettings.Username) || string.IsNullOrEmpty(siigoSettings.AccessKey))
                {
                    throw new Exception("SIIGO username and access key are required for authentication");
                }

                var authRequest = new
                {
                    username = siigoSettings.Username,
                    access_key = siigoSettings.AccessKey
                };

                var json = JsonConvert.SerializeObject(authRequest);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                _httpClient.DefaultRequestHeaders.Clear();
                _httpClient.DefaultRequestHeaders.Add("Partner-Id", siigoSettings.PartnerId);

                var response = await _httpClient.PostAsync($"{siigoSettings.ApiBaseUrl}/auth", content);
                var responseContent = await response.Content.ReadAsStringAsync();

                if (siigoSettings.LogEnabled)
                {
                    await _logger.InformationAsync($"SIIGO Auth Request: {json}");
                    await _logger.InformationAsync($"SIIGO Auth Response: {responseContent}");
                }

                if (!response.IsSuccessStatusCode)
                {
                    throw new Exception($"Failed to authenticate with SIIGO API. Status: {response.StatusCode}, Response: {responseContent}");
                }

                var authResponse = JsonConvert.DeserializeObject<SiigoAuthResponse>(responseContent);
                
                if (string.IsNullOrEmpty(authResponse?.access_token))
                {
                    throw new Exception("Invalid response from SIIGO authentication endpoint");
                }

                lock (_lockObject)
                {
                    _cachedToken = authResponse.access_token;
                    // Set expiry to 50 minutes (tokens usually expire in 1 hour)
                    _tokenExpiry = DateTime.Now.AddMinutes(50);
                }

                if (siigoSettings.LogEnabled)
                {
                    await _logger.InformationAsync($"SIIGO token refreshed successfully. Expires at: {_tokenExpiry}");
                }

                return _cachedToken;
            }
            catch (Exception ex)
            {
                await _logger.ErrorAsync($"Error refreshing SIIGO token: {ex.Message}", ex);
                
                lock (_lockObject)
                {
                    _cachedToken = null;
                    _tokenExpiry = DateTime.MinValue;
                }
                
                throw;
            }
        }

        public bool IsTokenValid()
        {
            lock (_lockObject)
            {
                return !string.IsNullOrEmpty(_cachedToken) && DateTime.Now < _tokenExpiry;
            }
        }
    }

    public class SiigoAuthResponse
    {
        public string access_token { get; set; }
        public string token_type { get; set; }
        public int expires_in { get; set; }
        public string scope { get; set; }
    }
}
