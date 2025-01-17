
using System.Globalization;
using System.Net.Http.Headers;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Http;
using Newtonsoft.Json;
using Nop.Core;
using Nop.Core.Domain.Directory;
using Nop.Core.Domain.Orders;
using Nop.Core.Domain.Payments;
using Nop.Plugin.Payments.PayU.Models;
using Nop.Plugin.Payments.PayU.Models.Notifications;
using Nop.Plugin.Payments.PayU.Models.Requests;
using Nop.Services.Directory;
using Nop.Services.Payments;
using Nop.Services.Logging;
using Nop.Services.Orders;

namespace Nop.Plugin.Payments.PayU.Services
{
    public class PayUService : IPayUService
    {
        private readonly ILogger _logger;
        private readonly PayUPaymentSettings _payUPaymentSettings;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly IWebHelper _webHelper;
        private readonly IOrderProcessingService _orderProcessingService;
        private readonly ICurrencyService _currencyService;
        private readonly IOrderService _orderService;

        private string GetPayUUrl => _payUPaymentSettings.UseSandbox
            ? "https://sandbox.checkout.payulatam.com/ppp-web-gateway-payu"
            : "https://checkout.payulatam.com/ppp-web-gateway-payu";

        private string ClientId => _payUPaymentSettings.UseSandbox
            ? _payUPaymentSettings.SandboxClientId
            : _payUPaymentSettings.ClientId;

        private string ClientSecret => _payUPaymentSettings.UseSandbox
            ? _payUPaymentSettings.SandboxClientSecret
            : _payUPaymentSettings.ClientSecret;

        private string SecondKey => _payUPaymentSettings.UseSandbox
            ? _payUPaymentSettings.SandboxSecondKey
            : _payUPaymentSettings.SecondKey;

        public PayUService(
            ILogger logger,
            PayUPaymentSettings payUPaymentSettings,
            IHttpContextAccessor httpContextAccessor,
            IWebHelper webHelper,
            IOrderProcessingService orderProcessingService,
            ICurrencyService currencyService,
            IOrderService orderService)
        {
            _logger = logger;
            _payUPaymentSettings = payUPaymentSettings;
            _httpContextAccessor = httpContextAccessor;
            _webHelper = webHelper;
            _orderProcessingService = orderProcessingService;
            _currencyService = currencyService;
            _orderService = orderService;
        }

        public void RedirectToPayUPayment(PostProcessPaymentRequest postProcessPaymentRequest)
        {
            // Datos del comercio y las credenciales
            string merchantId = "508029"; // ID del comercio (PayU)
            string accountId = "512321"; // ID de cuenta (PayU)
            string apiKey = "4Vj8eK4rloUd272L48hsrarnUA"; // Clave API de PayU
            string currency = "COP"; // Moneda de la transacción
            string description = "Compra en NopCommerce"; // Descripción de la transacción
            string testMode = "1"; // 1 = Sandbox, 0 = Producción

            // Obtiene los detalles del pedido
            var order = postProcessPaymentRequest.Order;
            var referenceCode = $"Order_{order.Id}"; // Código de referencia único
            var amount = order.OrderTotal.ToString("F2", System.Globalization.CultureInfo.InvariantCulture); // Monto con formato decimal
            var tax = "0"; // Impuestos aplicados
            var taxReturnBase = "0"; // Base de devolución de impuestos

            // URLs de respuesta y confirmación
            string responseUrl = $"{_webHelper.GetStoreLocation()}Plugins/PaymentPayU/Response";
            string confirmationUrl = $"{_webHelper.GetStoreLocation()}Plugins/PaymentPayU/Confirmation";

            // Generación de la firma
            string signatureString = $"{apiKey}~{merchantId}~{referenceCode}~{amount}~{currency}";
            string signature = GenerateMD5Hash(signatureString); // Método para calcular MD5

            // Crear formulario HTML para redirección
            var formBuilder = new StringBuilder();
            formBuilder.AppendLine("<form id='payuForm' method='post' action='https://sandbox.checkout.payulatam.com/ppp-web-gateway-payu/'>");
            formBuilder.AppendLine($"<input name='merchantId' type='hidden' value='{merchantId}' />");
            formBuilder.AppendLine($"<input name='accountId' type='hidden' value='{accountId}' />");
            formBuilder.AppendLine($"<input name='description' type='hidden' value='{description}' />");
            formBuilder.AppendLine($"<input name='referenceCode' type='hidden' value='{referenceCode}' />");
            formBuilder.AppendLine($"<input name='amount' type='hidden' value='{amount}' />");
            formBuilder.AppendLine($"<input name='tax' type='hidden' value='{tax}' />");
            formBuilder.AppendLine($"<input name='taxReturnBase' type='hidden' value='{taxReturnBase}' />");
            formBuilder.AppendLine($"<input name='currency' type='hidden' value='{currency}' />");
            formBuilder.AppendLine($"<input name='signature' type='hidden' value='{signature}' />");
            formBuilder.AppendLine($"<input name='test' type='hidden' value='{testMode}' />");
            formBuilder.AppendLine($"<input name='buyerEmail' type='hidden' value='arnedoesu@gmail.com' />");
            formBuilder.AppendLine($"<input name='responseUrl' type='hidden' value='{responseUrl}' />");
            formBuilder.AppendLine($"<input name='confirmationUrl' type='hidden' value='{confirmationUrl}' />");
            formBuilder.AppendLine("<input type='submit' value='Pagar ahora' />");
            formBuilder.AppendLine("</form>");
            formBuilder.AppendLine("<script>document.getElementById('payuForm').submit();</script>");

            // Enviar el formulario al cliente
            _httpContextAccessor.HttpContext.Response.Clear();
            _httpContextAccessor.HttpContext.Response.ContentType = "text/html";
            _httpContextAccessor.HttpContext.Response.WriteAsync(formBuilder.ToString()).Wait();
        }

        // Método para generar el hash MD5
        private string GenerateMD5Hash(string input)
        {
            using (var md5 = System.Security.Cryptography.MD5.Create())
            {
                var inputBytes = Encoding.UTF8.GetBytes(input);
                var hashBytes = md5.ComputeHash(inputBytes);
                return BitConverter.ToString(hashBytes).Replace("-", "").ToLowerInvariant();
            }
        }

        public async void Notify(Notification notification)
        {
            if (!int.TryParse(notification.Order.ExtOrderId, out var orderId))
            {
                return;
            }

            var order = await _orderService.GetOrderByIdAsync(orderId);
            if (order == null)
            {
                return;
            }

            switch (notification.Order.Status.ToUpperInvariant())
            {
                case "PENDING":
                    OrderPending(order);
                    break;
                case "WAITING_FOR_CONFIRMATION":
                    //Not implemented. This status is only available when "auto odbiór" is disabled in PayU settings.
                    break;
                case "COMPLETED":
                    OrderCompleted(notification, order);
                    break;
                case "CANCELED":
                    OrderCanceled(order);
                    break;
                case "REJECTED":
                    OrderRejected(order);
                    break;
            }
        }

        public async Task<RefundPaymentResult> Refund(RefundPaymentRequest refundPaymentRequest)
        {
            if (string.IsNullOrEmpty(refundPaymentRequest?.Order?.CaptureTransactionId))
            {
                return RefundPayUOrderIdNotFound();
            }

            var bearer = GetAuthorizationData().AccessToken;

            using (var httpClient = new HttpClient(new HttpClientHandler
            {
                AllowAutoRedirect = false
            })
            {
                BaseAddress = new Uri(GetPayUUrl)
            })
            {
                httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", bearer);
                httpClient.DefaultRequestHeaders
                    .Accept
                    .Add(new MediaTypeWithQualityHeaderValue("application/json"));

                var content = await GetRefundJson(refundPaymentRequest);

                using (var response = httpClient.PostAsync($"/api/v2_1/orders/{refundPaymentRequest.Order.CaptureTransactionId}/refunds", content).Result)
                {
                    return RefundResult((int)response.StatusCode, refundPaymentRequest.IsPartialRefund);
                }
            }
        }

        public bool VerifySignature(string body)
        {
            var openPayUSignature = _httpContextAccessor.HttpContext.Request.Headers["OpenPayu-Signature"].ToString();

            var signatureMatch = Regex.Match(openPayUSignature, "(signature=)([A-z,0-9]*)");
            var signature = signatureMatch.Groups[2].Value;

            var algorithmMatch = Regex.Match(openPayUSignature, "(algorithm=)([A-z,0-9,-]*)");
            var algorithm = algorithmMatch.Groups[2].Value;

            var verifyHash = GenerateHash(algorithm, body + SecondKey);

            return verifyHash == signature.ToLower();
        }

        private AuthorizationRequest GetAuthorizationData()
        {
            using (var httpClient = new HttpClient { BaseAddress = new Uri(GetPayUUrl) })
            {
                var content =
                    new StringContent(
                        $"grant_type=client_credentials&client_id={ClientId}&client_secret={ClientSecret}",
                        Encoding.Default,
                        "application/x-www-form-urlencoded");
                using (var response = httpClient.PostAsync("/pl/standard/user/oauth/authorize", content).Result)
                {
                    AuthorizationRequest authRequest;
                    try
                    {
                        response.EnsureSuccessStatusCode();
                        var responseContent = response.Content.ReadAsStringAsync().Result;
                        authRequest = JsonConvert.DeserializeObject<AuthorizationRequest>(responseContent);
                    }
                    catch
                    {
                        authRequest = null;
                    }

                    return authRequest;
                }
            }
        }

        private async Task<OrderRequest> PrepareOrder(PostProcessPaymentRequest postProcessPaymentRequest)
        {
            var orderId = postProcessPaymentRequest.Order.Id.ToString();

            var targetCurrency =
                await _currencyService.GetCurrencyByCodeAsync(postProcessPaymentRequest.Order.CustomerCurrencyCode);

            var orderTotal = await PriceInPayUStandard(postProcessPaymentRequest.Order.OrderTotal, targetCurrency);

            var result = new OrderRequest
            {
                ExtOrderId = orderId,
                NotifyUrl = $"{_webHelper.GetStoreLocation()}Plugins/PaymentPayU/Notify",
                ContinueUrl = $"{_webHelper.GetStoreLocation()}Plugins/PaymentPayU/ProcessingPayment?orderId={orderId}",
                BuyerRequest = new BuyerRequest
                {
                    ExtCustomerId = postProcessPaymentRequest.Order.CustomerId.ToString(),
                    Email = postProcessPaymentRequest.Order.CustomerId.ToString(), // Cambiar por email del cliente
                },
                CurrencyCode = postProcessPaymentRequest.Order.CustomerCurrencyCode,
                CustomerIp = postProcessPaymentRequest.Order.CustomerIp,
                Description = $"External id {orderId}",
                MerchantPosId = ClientId,
                TotalAmount = orderTotal.ToString(), // CultureInfo.InvariantCulture
                Products = new List<ProductRequest>()
            };

            // foreach (var item in postProcessPaymentRequest.Order.)
            // {
            //     var itemPrice = PriceInPayUStandard(item.UnitPriceInclTax, targetCurrency);

            //     result.Products.Add(new ProductRequest
            //     {
            //         Name = item.Product.Name,
            //         Quantity = item.Quantity.ToString(),
            //         UnitPrice = itemPrice.ToString(CultureInfo.InvariantCulture)
            //     });
            // }

            return result;
        }

        private async Task<decimal> PriceInPayUStandard(decimal price, Currency targetCurrency)
        {
            return Math.Round(await _currencyService.ConvertFromPrimaryStoreCurrencyAsync(price * 100, targetCurrency),
                MidpointRounding.ToEven);
        }

        private async void OrderPending(Order order)
        {
            await _orderService.InsertOrderNoteAsync(new OrderNote
            {
                Note = $"Order id {order.Id}. PayU order pending.",
                DisplayToCustomer = false,
                CreatedOnUtc = DateTime.UtcNow,
                OrderId = order.Id,
                Id = order.Id
            });

            await _orderService.UpdateOrderAsync(order);
        }

        private async void OrderCompleted(Notification notification, Order order)
        {
            if (!decimal.TryParse(notification?.Order?.TotalAmount, out var totalAmount))
            {
                return;
            }

            var targetCurrency =
                await _currencyService.GetCurrencyByCodeAsync(order.CustomerCurrencyCode);

            var orderTotal = await PriceInPayUStandard(order.OrderTotal, targetCurrency);

            if (totalAmount == orderTotal)
            {
                if (_orderProcessingService.CanMarkOrderAsPaid(order))
                {
                    order.CaptureTransactionId = notification?.Order?.OrderId;

                    await _orderService.InsertOrderNoteAsync(new OrderNote
                    {
                        Note = $"PayU order id {order.CaptureTransactionId}",
                        DisplayToCustomer = false,
                        CreatedOnUtc = DateTime.UtcNow,
                        OrderId = order.Id,
                        Id = order.Id
                    });

                    await _orderService.UpdateOrderAsync(order);
                    await _orderProcessingService.MarkOrderAsPaidAsync(order);
                }
            }
            else
            {
                var error =
                    $"PayU order id {notification?.Order?.OrderId}. Order id {order.Id}. PayU returned order total {totalAmount}. Order total should be equal to {order.OrderTotal}.";

                _logger.Error(error);

                await _orderService.InsertOrderNoteAsync(new OrderNote
                {
                    Note = error,
                    DisplayToCustomer = false,
                    CreatedOnUtc = DateTime.UtcNow,
                    OrderId = order.Id,
                    Id = order.Id
                });

                await _orderService.UpdateOrderAsync(order);
            }
        }

        private async void OrderCanceled(Order order)
        {
            if (_orderProcessingService.CanCancelOrder(order))
            {
                await _orderProcessingService.CancelOrderAsync(order, false);
            }

            await _orderService.InsertOrderNoteAsync(new OrderNote
            {
                Note = $"Order id {order.Id}. PayU order canceled.",
                DisplayToCustomer = false,
                CreatedOnUtc = DateTime.UtcNow,
                OrderId = order.Id,
                Id = order.Id
            });
            await _orderService.UpdateOrderAsync(order);
        }

        private async void OrderRejected(Order order)
        {
            OrderCanceled(order);

            await _orderService.InsertOrderNoteAsync(new OrderNote
            {
                Note = $"Order id {order.Id}. PayU order rejected.",
                DisplayToCustomer = false,
                CreatedOnUtc = DateTime.UtcNow,
                OrderId = order.Id,
                Id = order.Id
            });

            await _orderService.UpdateOrderAsync(order);
        }

        private RefundPaymentResult RefundPayUOrderIdNotFound()
        {
            var refund = new RefundPaymentResult();
            var error =
                "PayU order id not found. Probably payment settled manually. Refund can be done using PayU site.";
            refund.Errors.Add(error);
            _logger.Error(error);

            return refund;
        }

        private RefundPaymentResult RefundResult(int statusCode, bool partialRefund)
        {
            switch (statusCode)
            {
                case 200:
                case 204:
                    return new RefundPaymentResult
                    {
                        NewPaymentStatus = partialRefund ? PaymentStatus.PartiallyRefunded : PaymentStatus.Refunded
                    };
                default:
                    var error = $"PayU refund error code {statusCode}";
                    _logger.Error(error);
                    return new RefundPaymentResult
                    {
                        Errors = new List<string>
                        {
                            error
                        }
                    };
            }
        }

        private async Task<StringContent> GetRefundJson(RefundPaymentRequest refundPaymentRequest)
        {
            var refundData = new RefundRequest();
            if (refundPaymentRequest.IsPartialRefund)
            {
                var targetCurrency = await _currencyService.GetCurrencyByCodeAsync(refundPaymentRequest.Order.CustomerCurrencyCode);
                var refundAmount = await PriceInPayUStandard(refundPaymentRequest.AmountToRefund, targetCurrency);
                refundData.Refund = new ParialRefundDataRequest
                {
                    Amount = refundAmount.ToString(),
                    Description = $"Partial refund, amount: {refundAmount} {targetCurrency.CurrencyCode}"
                };
            }
            else
            {
                refundData.Refund = new RefundDataRequest
                {
                    Description = "Full refund"
                };
            }

            var refundJson = JsonConvert.SerializeObject(refundData);

            return new StringContent(refundJson, Encoding.UTF8, "application/json");
        }

        private string GenerateHash(string hashName, string input)
        {
            switch (hashName.ToLowerInvariant())
            {
                case "md5":
                    return input.ConvertToMd5();
                case "sha-256":
                    return input.ConvertToSha256();
                case "sha-384":
                    return input.ConvertToSha384();
                case "sha-512":
                    return input.ConvertToSha512();
                default:
                    var error = $"Hash name: {hashName}. This hash is not supported.";
                    _logger.Error(error);
                    throw new Exception(error);
            }
        }
    }
}
