using Nop.Services.Configuration;
using Nop.Services.Logging;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Web;

namespace Nop.Plugin.Misc.PushNotifications.Services
{
    public class WebPushService : IWebPushService
    {
        private readonly PushNotificationsSettings _settings;
        private readonly ILogger _logger;
        private readonly HttpClient _httpClient;

        public WebPushService(ISettingService settingService, ILogger logger)
        {
            _settings = settingService.LoadSetting<PushNotificationsSettings>();
            _logger = logger;
            _httpClient = new HttpClient();
        }

        public async Task SendWebPushNotificationAsync(string endpoint, string p256dh, string auth, string title, string body, string icon, string url)
        {
            try
            {
                var payload = JsonSerializer.Serialize(new
                {
                    title = title ?? string.Empty,
                    body = body ?? string.Empty,
                    icon = icon ?? string.Empty,
                    data = new { url = url ?? "/" }
                });

                var encryptedPayload = EncryptPayload(payload, p256dh, auth);
                
                var request = new HttpRequestMessage(HttpMethod.Post, endpoint);
                request.Content = new ByteArrayContent(encryptedPayload.CipherText);
                request.Content.Headers.Add("Content-Type", "application/octet-stream");
                request.Content.Headers.Add("Content-Encoding", "aes128gcm");
                
                // VAPID headers
                var vapidHeaders = GenerateVapidHeaders(endpoint);
                request.Headers.Add("Authorization", vapidHeaders.Authorization);
                request.Headers.Add("Crypto-Key", vapidHeaders.CryptoKey);

                var response = await _httpClient.SendAsync(request);
                
                if (response.IsSuccessStatusCode)
                {
                    await _logger.InsertLogAsync(Nop.Core.Domain.Logging.LogLevel.Information, 
                        "Web Push Notification Sent", 
                        $"Successfully sent Web Push notification to {endpoint}");
                }
                else
                {
                    await _logger.InsertLogAsync(Nop.Core.Domain.Logging.LogLevel.Error, 
                        "Web Push Notification Failed", 
                        $"Failed to send Web Push notification. Status: {response.StatusCode}, Reason: {response.ReasonPhrase}");
                }
            }
            catch (Exception ex)
            {
                await _logger.InsertLogAsync(Nop.Core.Domain.Logging.LogLevel.Error, 
                    "Web Push Notification Error", 
                    $"Error sending Web Push notification: {ex.Message}");
            }
        }

        private (string Authorization, string CryptoKey) GenerateVapidHeaders(string endpoint)
        {
            var audience = new Uri(endpoint).GetLeftPart(UriPartial.Authority);
            var subject = _settings.WebPushSubject ?? "mailto:admin@example.com";
            
            var header = new { typ = "JWT", alg = "ES256" };
            var payload = new 
            { 
                aud = audience, 
                exp = DateTimeOffset.UtcNow.AddHours(12).ToUnixTimeSeconds(), 
                sub = subject 
            };

            var headerJson = JsonSerializer.Serialize(header);
            var payloadJson = JsonSerializer.Serialize(payload);
            
            var headerBase64 = Base64UrlEncode(Encoding.UTF8.GetBytes(headerJson));
            var payloadBase64 = Base64UrlEncode(Encoding.UTF8.GetBytes(payloadJson));
            
            var unsignedToken = $"{headerBase64}.{payloadBase64}";
            
            // This is a simplified implementation. In production, you should use proper ECDSA signing
            var signature = Base64UrlEncode(Encoding.UTF8.GetBytes("signature_placeholder"));
            var token = $"{unsignedToken}.{signature}";
            
            var publicKeyBytes = Convert.FromBase64String(_settings.VapidPublicKey);
            var publicKeyBase64Url = Base64UrlEncode(publicKeyBytes);
            
            return (
                Authorization: $"vapid t={token}, k={publicKeyBase64Url}",
                CryptoKey: $"p256ecdsa={publicKeyBase64Url}"
            );
        }

        private (byte[] CipherText, byte[] Salt, byte[] ServerPublicKey) EncryptPayload(string payload, string p256dh, string auth)
        {
            // This is a simplified implementation of the Web Push encryption
            // In production, you should use a proper Web Push encryption library
            var payloadBytes = Encoding.UTF8.GetBytes(payload);
            var salt = new byte[16];
            var serverPublicKey = new byte[65];
            
            using (var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(salt);
                rng.GetBytes(serverPublicKey);
            }
            
            // For this example, we'll return the payload as-is
            // In production, implement proper ECDH key exchange and AES-GCM encryption
            return (payloadBytes, salt, serverPublicKey);
        }

        private string Base64UrlEncode(byte[] input)
        {
            return Convert.ToBase64String(input)
                .Replace('+', '-')
                .Replace('/', '_')
                .TrimEnd('=');
        }
    }
}
