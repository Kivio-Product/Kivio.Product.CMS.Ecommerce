using System;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Nop.Core;
using Nop.Core.Domain.Orders;
using Nop.Services.Logging;
using Nop.Services.Orders;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Nop.Web.Framework.Controllers;

namespace Nop.Plugin.Payments.Sermepa.Controllers
{
    public class PaymentSermepaPublicController : BasePaymentController
    {
        private readonly IOrderService _orderService;
        private readonly IOrderProcessingService _orderProcessingService;
        private readonly ILogger _logger;
        private readonly SermepaPaymentSettings _sermepaPaymentSettings;

        public PaymentSermepaPublicController(IOrderService orderService,
            IOrderProcessingService orderProcessingService,
            ILogger logger,
            SermepaPaymentSettings sermepaPaymentSettings)
        {
            _orderService = orderService;
            _orderProcessingService = orderProcessingService;
            _logger = logger;
            _sermepaPaymentSettings = sermepaPaymentSettings;
        }

        public async Task<IActionResult> Return()
        {
            // Obtener los parámetros de la respuesta de Redsys
            var dsMerchantParameters = HttpContext.Request.Query["Ds_MerchantParameters"].ToString();
            var dsSignature = HttpContext.Request.Query["Ds_Signature"].ToString();
            var dsSignatureVersion = HttpContext.Request.Query["Ds_SignatureVersion"].ToString();

            if (string.IsNullOrEmpty(dsMerchantParameters) || string.IsNullOrEmpty(dsSignature) || string.IsNullOrEmpty(dsSignatureVersion))
            {
                await _logger.ErrorAsync("TPV SERMEPA: Falta información en la respuesta del TPV.");
                return RedirectToAction("Index", "Home", new { area = "" });
            }

            // Decodificar los parámetros de la respuesta
            string decodedParameters;
            try
            {
                decodedParameters = Encoding.UTF8.GetString(Convert.FromBase64String(dsMerchantParameters.Replace('-', '+').Replace('_', '/')));
            }
            catch (FormatException ex)
            {
                await _logger.ErrorAsync($"TPV SERMEPA: Error al decodificar los parámetros. {ex.Message}");
                return RedirectToAction("Index", "Home", new { area = "" });
            }

            var responseParameters = JsonConvert.DeserializeObject<Dictionary<string, string>>(decodedParameters);

            if (!responseParameters.TryGetValue("Ds_Order", out var orderId) || !responseParameters.TryGetValue("Ds_Response", out var responseCode))
            {
                await _logger.ErrorAsync("TPV SERMEPA: Falta información clave en los parámetros decodificados.");
                return RedirectToAction("Index", "Home", new { area = "" });
            }

            // Obtener clave
            var key = _sermepaPaymentSettings.Pruebas ? _sermepaPaymentSettings.ClavePruebas : _sermepaPaymentSettings.ClaveReal;
            var decodedKey = Convert.FromBase64String(key);

            // Generar la firma
            string signatureCalculated;
            try
            {
                // Decode Merchant Parameters to validate nested JSON if necessary
                string validatedParameters = ValidateNestedJson("Ds_EMV3DS", decodedParameters);

                // Extract the order ID from decoded parameters
                string extractedOrderId = ExtractOrderFromParameters(validatedParameters);

                // Generate the derived key using 3DES encryption with the decoded key and extracted order ID
                byte[] derivedKey = Encrypt3DES(extractedOrderId, decodedKey);

                // Calculate HMAC-SHA256 signature
                byte[] hmacSignature = GetHMACSHA256(dsMerchantParameters, derivedKey);

                // Convert the signature to Base64 and URL-safe format
                signatureCalculated = Convert.ToBase64String(hmacSignature).Replace('+', '-').Replace('/', '_');
            }
            catch (Exception ex)
            {
                await _logger.ErrorAsync($"TPV SERMEPA: Error al calcular la firma. {ex.Message}");
                return RedirectToAction("Index", "Home", new { area = "" });
            }

            // Validar la firma
            if (!signatureCalculated.Equals(dsSignature))
            {
                await _logger.ErrorAsync($"TPV SERMEPA: Firma incorrecta. Calculada: {signatureCalculated}, Recibida: {dsSignature}");
                return RedirectToAction("Index", "Home", new { area = "" });
            }

            // Obtener el pedido
            var order = await _orderService.GetOrderByIdAsync(Convert.ToInt32(orderId));
            if (order == null)
            {
                throw new NopException($"El pedido con ID {orderId} no existe.");
            }

            // Verificar el código de respuesta de Redsys
            int.TryParse(responseCode, out var dsResponse);
            if (dsResponse >= 0 && dsResponse < 100)
            {
                // Marcar el pedido como pagado
                if (_orderProcessingService.CanMarkOrderAsPaid(order))
                {
                    await _orderProcessingService.MarkOrderAsPaidAsync(order);
                }

                // Agregar nota al pedido
                await _orderService.InsertOrderNoteAsync(new OrderNote
                {
                    Note = $"Pago confirmado. Parámetros de Redsys: {decodedParameters}",
                    DisplayToCustomer = false,
                    CreatedOnUtc = DateTime.UtcNow,
                    OrderId = order.Id,
                    Id = order.Id
                });

                return RedirectToRoute("CheckoutCompleted", new { orderId = order.Id });
            }

            // Log del error
            await _logger.ErrorAsync($"TPV SERMEPA: Pago no autorizado. Código de error: {dsResponse}");

            // Agregar nota de error al pedido
            await _orderService.InsertOrderNoteAsync(new OrderNote
            {
                Note = $"!!! PAGO DENEGADO !!! Código de respuesta: {dsResponse}",
                DisplayToCustomer = false,
                CreatedOnUtc = DateTime.UtcNow,
                OrderId = order.Id,
                Id = order.Id
            });

            return RedirectToAction("Index", "Home", new { area = "" });
        }



        public async Task<IActionResult> Error()
        {

            var dsMerchantParameters = HttpContext.Request.Query["Ds_MerchantParameters"].ToString();
            var dsSignatureVersion = HttpContext.Request.Query["Ds_SignatureVersion"].ToString();
            var dsSignature = HttpContext.Request.Query["Ds_Signature"].ToString();


            // Decodificar los parámetros de la respuesta
            string decodedParameters;
            try
            {
                decodedParameters = Encoding.UTF8.GetString(Convert.FromBase64String(dsMerchantParameters.Replace('-', '+').Replace('_', '/')));
            }
            catch (FormatException ex)
            {
                await _logger.ErrorAsync($"TPV SERMEPA: Error al decodificar los parámetros. {ex.Message}");
                return RedirectToAction("Index", "Home", new { area = "" });
            }

            string validatedParameters = ValidateNestedJson("Ds_EMV3DS", decodedParameters);
            var orderId = ExtractOrderFromParameters(validatedParameters);
            var responseCode = ExtractResponseFromParameters(validatedParameters);

            var order = await _orderService.GetOrderByIdAsync(Convert.ToInt32(orderId));
            if (order == null)
            {
                // throw new NopException($"El pedido con ID {orderId} no existe.");
            }

            if (_orderProcessingService.CanCancelOrder(order))
            {
                await _orderProcessingService.CancelOrderAsync(order, true);
            }

            await _orderService.InsertOrderNoteAsync(new OrderNote
            {
                Note = $"!!! PAGO DENEGADO !!! Código de respuesta: {responseCode}",
                DisplayToCustomer = false,
                CreatedOnUtc = DateTime.UtcNow,
                OrderId = order.Id,
                Id = order.Id
            });

            await _logger.ErrorAsync("Orden de pago rechazada por el TPV SERMEPA.");
            return RedirectToRoute("Homepage");
        }


        private string ValidateNestedJson(string nestedKey, string json)
        {
            try
            {
                var jsonObject = JsonConvert.DeserializeObject<JObject>(json);
                if (jsonObject != null && jsonObject.ContainsKey(nestedKey))
                {
                    var nestedJson = jsonObject[nestedKey]?.ToString();
                    jsonObject.Remove(nestedKey);
                    jsonObject.Add(nestedKey, nestedJson);
                    return jsonObject.ToString();
                }
            }
            catch (JsonReaderException ex)
            {
                throw new JsonReaderException($"Error al validar JSON anidado: {ex.Message}");
            }
            return json;
        }

        private string ExtractOrderFromParameters(string parameters)
        {
            var paramDict = JsonConvert.DeserializeObject<Dictionary<string, string>>(parameters);
            if (paramDict != null && paramDict.TryGetValue("Ds_Order", out var orderId))
            {
                return orderId;
            }
            // throw new KeyNotFoundException("No se encontró Ds_Order en los parámetros decodificados.");
            return "";
        }

        private string ExtractResponseFromParameters(string parameters)
        {
            var paramDict = JsonConvert.DeserializeObject<Dictionary<string, string>>(parameters);
            if (paramDict != null && paramDict.TryGetValue("Ds_Response", out var responseCode))
            {
                return responseCode;
            }
            // throw new KeyNotFoundException("No se encontró Ds_Response en los parámetros decodificados.");
            return "No response code found";
        }

        private byte[] Encrypt3DES(string orderId, byte[] key)
        {
            byte[] orderBytes = Encoding.UTF8.GetBytes(orderId);
            byte[] iv = new byte[8]; // SALT: {0, 0, 0, 0, 0, 0, 0, 0}

            using (var tdes = TripleDES.Create())
            {
                tdes.Key = key;
                tdes.IV = iv;
                tdes.Mode = CipherMode.CBC;
                tdes.Padding = PaddingMode.Zeros;
                {
                    using var encryptor = tdes.CreateEncryptor();
                    return encryptor.TransformFinalBlock(orderBytes, 0, orderBytes.Length);
                }
            }
        }

        private byte[] GetHMACSHA256(string data, byte[] key)
        {
            using (var hmac = new HMACSHA256(key))
            {
                return hmac.ComputeHash(Encoding.UTF8.GetBytes(data));
            }
        }

    }

}