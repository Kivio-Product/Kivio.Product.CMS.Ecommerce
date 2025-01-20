
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
        private readonly IWorkContext _workContext;

        private string GetPayUUrl => _payUPaymentSettings.UseSandbox
            ? "https://sandbox.checkout.payulatam.com/ppp-web-gateway-payu"
            : "https://checkout.payulatam.com/ppp-web-gateway-payu";

        public PayUService(
            ILogger logger,
            PayUPaymentSettings payUPaymentSettings,
            IHttpContextAccessor httpContextAccessor,
            IWebHelper webHelper,
            IOrderProcessingService orderProcessingService,
            ICurrencyService currencyService,
            IOrderService orderService,
            IWorkContext workContext)
        {
            _logger = logger;
            _payUPaymentSettings = payUPaymentSettings;
            _httpContextAccessor = httpContextAccessor;
            _webHelper = webHelper;
            _orderProcessingService = orderProcessingService;
            _currencyService = currencyService;
            _orderService = orderService;
            _workContext = workContext;
        }

        public async void RedirectToPayUPayment(PostProcessPaymentRequest postProcessPaymentRequest)
        {
            string merchantId = _payUPaymentSettings.MerchantId; // ID del comercio (PayU)
            string accountId = _payUPaymentSettings.AccountId; // ID de cuenta (PayU)
            string apiKey = _payUPaymentSettings.ClientSecretKey; // Clave API de PayU
            string currency = "COP"; // Moneda de la transacción
            string description = _payUPaymentSettings.PaymentDescription; // Descripción de la transacción
            string testMode = _payUPaymentSettings.UseSandbox ? "1" : "0"; // 1 = Sandbox, 0 = Producción

            var order = postProcessPaymentRequest.Order;
            var referenceCode = $"Orden_{order.Id}"; // Código de referencia único
            var amount = order.OrderTotal.ToString("F2", System.Globalization.CultureInfo.InvariantCulture); // Monto con formato decimal
            var tax = "0"; // Impuestos aplicados
            var taxReturnBase = "0"; // Base de devolución de impuestos

            var currentCustomer = await _workContext.GetCurrentCustomerAsync();            

            string responseUrl = $"{_webHelper.GetStoreLocation()}Plugins/PaymentPayU/Return";
            string confirmationUrl = $"{_webHelper.GetStoreLocation()}Plugins/PaymentPayU/ProcessingPayment?orderId={order.Id}";

            string signatureString = $"{apiKey}~{merchantId}~{referenceCode}~{amount}~{currency}";
            string signature = GenerateMD5Hash(signatureString); // Método para calcular MD5

            var formBuilder = new StringBuilder();
            formBuilder.AppendLine($"<form id='payuForm' method='post' action='{GetPayUUrl}'>");
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
            formBuilder.AppendLine($"<input name='buyerEmail' type='hidden' value='{currentCustomer.Email}' />");
            formBuilder.AppendLine($"<input name='responseUrl' type='hidden' value='{responseUrl}' />");
            formBuilder.AppendLine($"<input name='confirmationUrl' type='hidden' value='{confirmationUrl}' />");
            formBuilder.AppendLine("<input type='submit' value='Pagar ahora' />");
            formBuilder.AppendLine("</form>");
            formBuilder.AppendLine("<script>document.getElementById('payuForm').submit();</script>");

            _httpContextAccessor.HttpContext.Response.Clear();
            _httpContextAccessor.HttpContext.Response.ContentType = "text/html";
            _httpContextAccessor.HttpContext.Response.WriteAsync(formBuilder.ToString()).Wait();
        }

        public async Task<(bool succeeded, int orderId)> ReturnAsync(PaymentResponse paymentResponse)
        {
            if (paymentResponse == null || string.IsNullOrEmpty(paymentResponse.ReferenceCode))
            {
                _logger.Error("PaymentResponse no contiene información válida.");
                return (false, 0);
            }

            var order = await GetOrderByResponseAsync(paymentResponse);
            if (order == null)
            {
                _logger.Error($"No se encontró el pedido para la referencia {paymentResponse.ReferenceCode}");
                return (false, 0);
            }

            if (order.PaymentStatus == PaymentStatus.Paid)
            {
                _logger.Information($"La transacción ya fue procesada para el pedido {order.Id}");
                return (true, order.Id);
            }

            // Ajustar el valor de TX_VALUE según las reglas de redondeo
            var roundedValue = RoundToPayURules(paymentResponse.TXValue);

            // Generar la cadena para la firma
            var signatureString = $"{_payUPaymentSettings.ClientSecretKey}~{paymentResponse.MerchantId}~{paymentResponse.ReferenceCode}~{roundedValue.ToString("F1", CultureInfo.InvariantCulture)}~{paymentResponse.Currency}~{paymentResponse.TransactionState}";
            var expectedSignature = GenerateMD5Hash(signatureString);

            // Validar la firma
            if (!string.Equals(expectedSignature, paymentResponse.Signature, StringComparison.InvariantCultureIgnoreCase))
            {
                _logger.Error($"Firma no válida para la referencia {paymentResponse.ReferenceCode}. Esperado: {expectedSignature}, recibido: {paymentResponse.Signature}");
                OrderRejectedAsync(order);
                return (false, order.Id);
            }

            return ProcessTransactionState(paymentResponse, order);
        }

        private decimal RoundToPayURules(decimal value)
        {
            var rounded = Math.Round(value, 1, MidpointRounding.ToEven); // "Round half to even"
            return rounded;
        }

        private (bool succeeded, int orderId) ProcessTransactionState(PaymentResponse paymentResponse, Order order)
        {
            var stateMessage = paymentResponse.TransactionState switch
            {
                4 => "Transacción aprobada",
                6 => "Transacción rechazada",
                104 => "Error en la transacción",
                7 => "Pago pendiente",
                _ => "Estado desconocido"
            };

            _logger.Information($"{stateMessage} para el pedido {order.Id}");

            switch (paymentResponse.TransactionState)
            {
                case 4: // Transacción aprobada
                    OrderCompletedAsync(order);
                    return (true, order.Id);

                case 6: // Transacción rechazada
                    OrderRejectedAsync(order);
                    return (false, order.Id);

                case 104: // Error en la transacción
                    OrderCanceledAsync(order);
                    return (false, order.Id);

                case 7: // Pago pendiente
                    OrderPendingAsync(order);
                    return (true, order.Id);

                default: // Estado desconocido
                    OrderRejectedAsync(order);
                    return (false, order.Id);
            }
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
                    OrderPendingAsync(order);
                    break;
                case "WAITING_FOR_CONFIRMATION":
                    //Not implemented. This status is only available when "auto odbiór" is disabled in PayU settings.
                    break;
                case "COMPLETED":
                    OrderCompletedAsync(order);
                    break;
                case "CANCELED":
                    OrderCanceledAsync(order);
                    break;
                case "REJECTED":
                    OrderRejectedAsync(order);
                    break;
            }
        }

        public bool VerifySignature(string body)
        {
            var openPayUSignature = _httpContextAccessor.HttpContext.Request.Headers["OpenPayu-Signature"].ToString();

            var signatureMatch = Regex.Match(openPayUSignature, "(signature=)([A-z,0-9]*)");
            var signature = signatureMatch.Groups[2].Value;

            var algorithmMatch = Regex.Match(openPayUSignature, "(algorithm=)([A-z,0-9,-]*)");
            var algorithm = algorithmMatch.Groups[2].Value;

            var verifyHash = GenerateHash(algorithm, body);

            return verifyHash == signature.ToLower();
        }

        private async void OrderPendingAsync(Order order)
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

        private async void OrderCompletedAsync(Order order)
        {
            if (_orderProcessingService.CanMarkOrderAsPaid(order))
            {
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

        private async void OrderCanceledAsync(Order order)
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

        private async void OrderRejectedAsync(Order order)
        {
            OrderCanceledAsync(order);

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

        private async Task<Order> GetOrderByResponseAsync(PaymentResponse paymentResponse)
        {
            var orderId = int.Parse(paymentResponse.ReferenceCode.Split('_')[1]);

            return await _orderService.GetOrderByIdAsync(orderId);
        }
    }
}