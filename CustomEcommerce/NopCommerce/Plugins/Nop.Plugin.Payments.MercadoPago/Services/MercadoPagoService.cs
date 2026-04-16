using System.Globalization;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Nop.Core;
using Nop.Core.Domain.Orders;
using Nop.Core.Domain.Payments;
using Nop.Plugin.Payments.MercadoPago.Models;
using Nop.Services.Configuration;
using Nop.Services.Directory;
using Nop.Services.Logging;
using Nop.Services.Orders;
using Nop.Services.Payments;

namespace Nop.Plugin.Payments.MercadoPago.Services
{
    public class MercadoPagoService : IMercadoPagoService
    {
        private readonly ILogger _logger;
        private readonly MercadoPagoPaymentSettings _mercadoPagoPaymentSettings;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly IWebHelper _webHelper;
        private readonly IOrderProcessingService _orderProcessingService;
        private readonly IOrderNotificationService _orderNotificationService;
        private readonly ICurrencyService _currencyService;
        private readonly IOrderService _orderService;
        private readonly IWorkContext _workContext;
        private readonly ISettingService _settingService;
        private readonly IStoreContext _storeContext;
        private readonly IOrderTotalCalculationService _orderTotalCalculationService;
        private const string MercadoPagoApiBaseUrl = "https://api.mercadopago.com";

        public MercadoPagoService(
            ILogger logger,
            MercadoPagoPaymentSettings mercadoPagoPaymentSettings,
            IHttpContextAccessor httpContextAccessor,
            IWebHelper webHelper,
            IOrderProcessingService orderProcessingService,
            IOrderNotificationService orderNotificationService,
            ICurrencyService currencyService,
            IOrderService orderService,
            IWorkContext workContext,
            ISettingService settingService,
            IStoreContext storeContext,
            IOrderTotalCalculationService orderTotalCalculationService)
        {
            _logger = logger;
            _mercadoPagoPaymentSettings = mercadoPagoPaymentSettings;
            _httpContextAccessor = httpContextAccessor;
            _webHelper = webHelper;
            _orderProcessingService = orderProcessingService;
            _orderNotificationService = orderNotificationService;
            _currencyService = currencyService;
            _orderService = orderService;
            _workContext = workContext;
            _settingService = settingService;
            _storeContext = storeContext;
            _orderTotalCalculationService = orderTotalCalculationService;
        }

        public async Task<decimal> GetAdditionalFeeAsync(IList<ShoppingCartItem> cart)
        {
            if (!_mercadoPagoPaymentSettings.AdditionalFeeEnabled)
            {
                return 0m;
            }

            const decimal mercadopagoPercRate = 0.0329m;
            const decimal mercadopagoFixedFee = 800m;
            const decimal taxRate = 0.19m;
            var orderTotal = (await _orderTotalCalculationService.GetShoppingCartTotalAsync(cart, usePaymentMethodAdditionalFee: false)).shoppingCartTotal ?? 0;

            decimal fixedFeeWithTax = mercadopagoFixedFee * (1m + taxRate);
            decimal percFeeWithTax = mercadopagoPercRate * (1m + taxRate);
            decimal numerator = orderTotal + fixedFeeWithTax;
            decimal denominator = 1m - percFeeWithTax;

            if (denominator <= 0)
            {
                return decimal.MaxValue;
            }

            decimal totalToChargeCustomer = numerator / denominator;
            decimal additionalFee = totalToChargeCustomer - orderTotal;

            if (additionalFee < 0 && orderTotal == 0)
            {
                return 0m;
            }

            additionalFee = Math.Round(additionalFee, 0, MidpointRounding.ToEven);

            return additionalFee;
        }

        public async Task<bool> HidePaymentMethodAsync()
        {
            var currentCurrency = (await _workContext.GetWorkingCurrencyAsync()).Id;
            var store = await _storeContext.GetCurrentStoreAsync();
            var storeId = store?.Id ?? 0;
            var mercadoPagoPaymentSettings = await _settingService.LoadSettingAsync<MercadoPagoPaymentSettings>(storeId);
            return !mercadoPagoPaymentSettings.SelectedCurrencyIdList.Contains(currentCurrency);
        }

        public async Task RedirectToMercadoPagoPayment(PostProcessPaymentRequest postProcessPaymentRequest)
        {
            var accessToken = ResolveAccessToken();
            if (string.IsNullOrWhiteSpace(accessToken))
                throw new NopException("MercadoPago Access Token is not configured. Set it in plugin settings or environment variable MP_ACCESS_TOKEN.");

            var order = postProcessPaymentRequest.Order;
            var externalReference = $"ORDER_{order.Id}";
            var currentCustomer = await _workContext.GetCurrentCustomerAsync();
            var email = string.IsNullOrWhiteSpace(currentCustomer.Email) ? "no-reply@localhost" : currentCustomer.Email;
            var currency = string.IsNullOrWhiteSpace(order.CustomerCurrencyCode) ? "COP" : order.CustomerCurrencyCode;

            var backendUrl = ResolveBackendUrl();
            var returnUrl = $"{backendUrl}Plugins/PaymentMercadoPago/Return?orderId={order.Id}";
            var notificationUrl = $"{backendUrl}Plugins/PaymentMercadoPago/Confirm";

            var payload = new
            {
                external_reference = externalReference,
                notification_url = notificationUrl,
                back_urls = new
                {
                    success = returnUrl,
                    pending = returnUrl,
                    failure = returnUrl
                },
                auto_return = "approved",
                payer = new
                {
                    email
                },
                items = new[]
                {
                    new
                    {
                        title = string.IsNullOrWhiteSpace(_mercadoPagoPaymentSettings.PaymentDescription)
                            ? $"Order #{order.Id}"
                            : _mercadoPagoPaymentSettings.PaymentDescription,
                        quantity = 1,
                        currency_id = currency,
                        unit_price = NormalizeUnitPrice(order.OrderTotal, currency)
                    }
                }
            };

            using var client = CreateHttpClient(accessToken);
            using var response = await client.PostAsync(
                $"{MercadoPagoApiBaseUrl}/checkout/preferences",
                new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json"));

            var responseContent = await response.Content.ReadAsStringAsync();
            if (!response.IsSuccessStatusCode)
            {
                _logger.Error($"Error creating MercadoPago preference: {response.StatusCode}. {responseContent}");
                throw new NopException($"Unable to create MercadoPago preference. HTTP {(int)response.StatusCode} ({response.StatusCode}). Response: {responseContent}");
            }

            using var doc = JsonDocument.Parse(responseContent);
            var initPoint = doc.RootElement.GetPropertyOrDefault("init_point");

            if (string.IsNullOrWhiteSpace(initPoint))
                throw new NopException("MercadoPago preference did not return an init_point.");

            _httpContextAccessor.HttpContext.Response.Redirect(initPoint);
        }

        public async Task<(bool succeeded, int orderId)> ReturnAsync(PaymentResponse paymentResponse)
        {
            if (paymentResponse == null)
            {
                _logger.Error("PaymentResponse no contiene información válida.");
                return (false, 0);
            }

            var orderId = paymentResponse.OrderId;
            if (orderId <= 0)
                orderId = ParseOrderIdFromExternalReference(paymentResponse.ExternalReference);

            if (orderId <= 0)
            {
                _logger.Error("MercadoPago return does not include a valid order id.");
                return (false, 0);
            }

            var order = await _orderService.GetOrderByIdAsync(orderId);
            if (order == null)
            {
                _logger.Error($"No se encontró el pedido para orderId={orderId}");
                return (false, 0);
            }

            if (order.PaymentStatus == PaymentStatus.Paid)
            {
                _logger.Information($"La transacción ya fue procesada para el pedido {order.Id}");
                return (true, order.Id);
            }

            var status = paymentResponse.Status;
            if (string.IsNullOrWhiteSpace(status))
                status = paymentResponse.CollectionStatus;

            return await ProcessPaymentStatusAsync(status, order);
        }

        public async Task<(bool succeeded, int orderId)> ConfirmAsync(ConfirmationResponse confirmationResponse)
        {
            if (confirmationResponse == null)
            {
                _logger.Error("MercadoPago webhook payload inválido.");
                return (false, 0);
            }

            var isPaymentNotification = string.Equals(confirmationResponse.Type, "payment", StringComparison.OrdinalIgnoreCase)
                                      || string.Equals(confirmationResponse.Topic, "payment", StringComparison.OrdinalIgnoreCase);

            if (!isPaymentNotification)
            {
                _logger.Information("Webhook recibido sin topic/type=payment. Se ignora.");
                return (false, 0);
            }

            if (string.IsNullOrWhiteSpace(confirmationResponse.PaymentId))
            {
                _logger.Error("Webhook de MercadoPago sin payment id.");
                return (false, 0);
            }

            var paymentData = await GetPaymentDetailsAsync(confirmationResponse.PaymentId);
            if (paymentData == null)
            {
                _logger.Error($"No fue posible consultar el pago {confirmationResponse.PaymentId} en MercadoPago.");
                return (false, 0);
            }

            var orderId = ParseOrderIdFromExternalReference(paymentData.ExternalReference);
            if (orderId <= 0)
            {
                _logger.Error($"No se pudo obtener order id desde external_reference '{paymentData.ExternalReference}'.");
                return (false, 0);
            }

            var order = await _orderService.GetOrderByIdAsync(orderId);
            if (order == null)
            {
                _logger.Error($"No se encontró el pedido para orderId={orderId}");
                return (false, 0);
            }

            if (order.PaymentStatus == PaymentStatus.Paid)
            {
                _logger.Information($"Webhook: La transacción ya fue procesada para el pedido {order.Id}. Se omite.");
                return (true, order.Id);
            }

            return await ProcessPaymentStatusAsync(paymentData.Status, order);
        }

        private async Task<(bool succeeded, int orderId)> ProcessPaymentStatusAsync(string status, Order order)
        {
            var normalizedStatus = status?.Trim().ToLowerInvariant() ?? string.Empty;

            switch (normalizedStatus)
            {
                case "approved":
                    _logger.Information($"Transacción aprobada para el pedido {order.Id}");
                    await OrderCompletedAsync(order);
                    return (true, order.Id);

                case "pending":
                case "in_process":
                case "in_mediation":
                    _logger.Information($"Pago pendiente para el pedido {order.Id}. Status={normalizedStatus}");
                    await OrderPendingAsync(order);
                    return (true, order.Id);

                case "rejected":
                case "cancelled":
                case "refunded":
                case "charged_back":
                    _logger.Information($"Pago rechazado/cancelado para el pedido {order.Id}. Status={normalizedStatus}");
                    await OrderRejectedAsync(order);
                    return (false, order.Id);

                default:
                    _logger.Information($"Estado de pago desconocido para el pedido {order.Id}. Status={normalizedStatus}");
                    await OrderPendingAsync(order);
                    return (false, order.Id);
            }
        }

        private HttpClient CreateHttpClient(string accessToken)
        {
            var client = new HttpClient();
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            return client;
        }

        private string ResolveAccessToken()
        {
            if (!string.IsNullOrWhiteSpace(_mercadoPagoPaymentSettings.AccessToken))
                return _mercadoPagoPaymentSettings.AccessToken;

            return Environment.GetEnvironmentVariable("MP_ACCESS_TOKEN");
        }

        private string ResolveBackendUrl()
        {
            var backendUrl = Environment.GetEnvironmentVariable("PUBLIC_BACKEND_URL");

            if (string.IsNullOrWhiteSpace(backendUrl))
                backendUrl = _webHelper.GetStoreLocation();

            if (!backendUrl.EndsWith('/'))
                backendUrl += "/";

            return backendUrl;
        }

        private static decimal NormalizeUnitPrice(decimal amount, string currencyCode)
        {
            var code = currencyCode?.Trim().ToUpperInvariant() ?? string.Empty;

            if (code is "COP" or "CLP" or "PYG")
                return Math.Round(amount, 0, MidpointRounding.AwayFromZero);

            return Math.Round(amount, 2, MidpointRounding.AwayFromZero);
        }

        private async Task<MercadoPagoPaymentData> GetPaymentDetailsAsync(string paymentId)
        {
            if (string.IsNullOrWhiteSpace(_mercadoPagoPaymentSettings.AccessToken))
                return null;

            using var client = CreateHttpClient(_mercadoPagoPaymentSettings.AccessToken);
            using var response = await client.GetAsync($"{MercadoPagoApiBaseUrl}/v1/payments/{paymentId}");
            var content = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                _logger.Error($"Error getting MercadoPago payment {paymentId}: {response.StatusCode}. {content}");
                return null;
            }

            using var doc = JsonDocument.Parse(content);
            var root = doc.RootElement;

            return new MercadoPagoPaymentData
            {
                Status = root.GetPropertyOrDefault("status"),
                ExternalReference = root.GetPropertyOrDefault("external_reference")
            };
        }

        private static int ParseOrderIdFromExternalReference(string externalReference)
        {
            if (string.IsNullOrWhiteSpace(externalReference))
                return 0;

            var parts = externalReference.Split('_', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length == 0)
                return 0;

            var idToken = parts[^1];
            return int.TryParse(idToken, NumberStyles.Integer, CultureInfo.InvariantCulture, out var id) ? id : 0;
        }

        private async Task OrderPendingAsync(Order order)
        {
            await _orderService.InsertOrderNoteAsync(new OrderNote
            {
                Note = $"Order id {order.Id}. MercadoPago order pending.",
                DisplayToCustomer = false,
                CreatedOnUtc = DateTime.UtcNow,
                OrderId = order.Id,
                Id = order.Id
            });

            await _orderService.UpdateOrderAsync(order);
        }

        private async Task OrderCompletedAsync(Order order)
        {
            // Re-fetch the order to get the latest state and avoid race conditions
            // between the Return (user redirect) and Confirm (webhook) endpoints
            order = await _orderService.GetOrderByIdAsync(order.Id);
            if (order == null || !_orderProcessingService.CanMarkOrderAsPaid(order))
            {
                _logger.Information($"El pedido {order?.Id} ya fue procesado o no se puede marcar como pagado. Se omite.");
                return;
            }

            await _orderService.InsertOrderNoteAsync(new OrderNote
            {
                Note = $"MercadoPago order id {order.Id}",
                DisplayToCustomer = false,
                CreatedOnUtc = DateTime.UtcNow,
                OrderId = order.Id,
                Id = order.Id
            });

            await _orderService.UpdateOrderAsync(order);
            await _orderProcessingService.MarkOrderAsPaidAsync(order);

            // Send order placed notifications after payment confirmation
            await _orderNotificationService.SendOrderPlacedNotificationsAsync(order);
        }

        private async Task OrderCanceledAsync(Order order)
        {
            if (_orderProcessingService.CanCancelOrder(order))
            {
                await _orderProcessingService.CancelOrderAsync(order, false);
            }

            await _orderService.InsertOrderNoteAsync(new OrderNote
            {
                Note = $"Order id {order.Id}. MercadoPago order canceled.",
                DisplayToCustomer = false,
                CreatedOnUtc = DateTime.UtcNow,
                OrderId = order.Id,
                Id = order.Id
            });
            await _orderService.UpdateOrderAsync(order);
        }

        private async Task OrderRejectedAsync(Order order)
        {
            await OrderCanceledAsync(order);

            await _orderService.InsertOrderNoteAsync(new OrderNote
            {
                Note = $"Order id {order.Id}. MercadoPago order rejected.",
                DisplayToCustomer = false,
                CreatedOnUtc = DateTime.UtcNow,
                OrderId = order.Id,
                Id = order.Id
            });

            await _orderService.UpdateOrderAsync(order);
        }

        private class MercadoPagoPaymentData
        {
            public string Status { get; set; }
            public string ExternalReference { get; set; }
        }

    }

    internal static class JsonElementExtensions
    {
        public static string GetPropertyOrDefault(this JsonElement jsonElement, string name)
        {
            if (jsonElement.ValueKind != JsonValueKind.Object)
                return null;

            if (!jsonElement.TryGetProperty(name, out var value))
                return null;

            return value.ValueKind switch
            {
                JsonValueKind.String => value.GetString(),
                JsonValueKind.Number => value.GetRawText(),
                JsonValueKind.True => "true",
                JsonValueKind.False => "false",
                _ => value.GetRawText()
            };
        }
    }
}