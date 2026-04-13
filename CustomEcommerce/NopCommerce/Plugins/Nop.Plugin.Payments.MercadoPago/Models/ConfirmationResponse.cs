
using Microsoft.AspNetCore.Http;
using System.Text;
using System.Text.Json;

namespace Nop.Plugin.Payments.MercadoPago.Models
{
    public class ConfirmationResponse
    {
        public string Topic { get; set; }
        public string Type { get; set; }
        public string PaymentId { get; set; }
        public string RawBody { get; set; }

        public static async Task<ConfirmationResponse> FromHttpRequestAsync(HttpRequest request)
        {
            request.EnableBuffering();

            string rawBody;
            using (var reader = new StreamReader(request.Body, Encoding.UTF8, detectEncodingFromByteOrderMarks: false, leaveOpen: true))
                rawBody = await reader.ReadToEndAsync();

            request.Body.Position = 0;

            var response = new ConfirmationResponse
            {
                Topic = request.Query["topic"].ToString(),
                Type = request.Query["type"].ToString(),
                PaymentId = request.Query["id"].ToString(),
                RawBody = rawBody
            };

            if (string.IsNullOrWhiteSpace(response.PaymentId) && !string.IsNullOrWhiteSpace(rawBody))
            {
                try
                {
                    using var doc = JsonDocument.Parse(rawBody);
                    if (string.IsNullOrWhiteSpace(response.Type) && doc.RootElement.TryGetProperty("type", out var typeProp))
                        response.Type = typeProp.GetString();

                    if (doc.RootElement.TryGetProperty("data", out var dataProp)
                        && dataProp.TryGetProperty("id", out var idProp))
                        response.PaymentId = idProp.GetRawText().Trim('"');
                }
                catch
                {
                }
            }

            if (string.IsNullOrWhiteSpace(response.Type))
                response.Type = response.Topic;

            return response;
        }
    }
}