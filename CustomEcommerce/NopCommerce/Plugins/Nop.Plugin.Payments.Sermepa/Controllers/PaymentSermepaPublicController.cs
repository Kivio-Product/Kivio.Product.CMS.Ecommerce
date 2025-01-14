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

        public async Task<IActionResult> Return(FormCollection form)
        {
            //ID de Pedido
            var orderId = HttpContext.Request.Query["Ds_Order"];
            var strDs_Merchant_Order = HttpContext.Request.Query["Ds_Order"];

            var strDs_Merchant_Amount = HttpContext.Request.Query["Ds_Amount"];
            var strDs_Merchant_MerchantCode = HttpContext.Request.Query["Ds_MerchantCode"];
            var strDs_Merchant_Currency = HttpContext.Request.Query["Ds_Currency"];

            //Respuesta del TPV
            var str_Merchant_Response = HttpContext.Request.Query["Ds_Response"];
            var dsResponse = Convert.ToInt32(HttpContext.Request.Query["Ds_Response"]);

            //Clave
            var pruebas = _sermepaPaymentSettings.Pruebas;
            var clave = pruebas ? _sermepaPaymentSettings.ClavePruebas : _sermepaPaymentSettings.ClaveReal;

            //Calculo de la firma
            var sha = string.Format("{0}{1}{2}{3}{4}{5}",
                strDs_Merchant_Amount,
                strDs_Merchant_Order,
                strDs_Merchant_MerchantCode,
                strDs_Merchant_Currency,
                str_Merchant_Response,
                clave);

            SHA1 shaM = new SHA1Managed();
            var shaResult = shaM.ComputeHash(Encoding.Default.GetBytes(sha));
            var shaResultStr = BitConverter.ToString(shaResult).Replace("-", "");

            //Firma enviada
            var signature = CommonHelper.EnsureNotNull(HttpContext.Request.Query["Ds_Signature"]);

            //Comprobamos la integridad de las comunicaciones con las claves
            //LogManager.InsertLog(LogTypeEnum.OrderError, "TPV SERMEPA: Clave generada", "CLAVE GENERADA: " + SHAresultStr);
            //LogManager.InsertLog(LogTypeEnum.OrderError, "TPV SERMEPA: Clave obtenida", "CLAVE OBTENIDA: " + signature);
            if (!signature.Equals(shaResultStr))
            {
                await _logger.ErrorAsync("TPV SERMEPA: Clave incorrecta. Las claves enviada y generada no coinciden: " + shaResultStr + " != " + signature);

                return RedirectToAction("Index", "Home", new { area = "" });
            }

            //Pedido
            var order = await _orderService.GetOrderByIdAsync(Convert.ToInt32(orderId));
            if (order == null)
                throw new NopException(string.Format("El pedido de ID {0} no existe", orderId));

            //Actualizamos el pedido
            if (dsResponse > -1 && dsResponse < 100)
            {
                //Lo marcamos como pagado
                if (_orderProcessingService.CanMarkOrderAsPaid(order))
                {
                    await _orderProcessingService.MarkOrderAsPaidAsync(order);
                }

                //order note
                await _orderService.InsertOrderNoteAsync(new OrderNote
                {
                    Note = "Información del pago: " + Request.Form,
                    DisplayToCustomer = false,
                    CreatedOnUtc = DateTime.UtcNow
                });
                return RedirectToRoute("CheckoutCompleted", new { orderId = order.Id });
            }

            await _logger.ErrorAsync("TPV SERMEPA: Pago no autorizado con ERROR: " + dsResponse);

            //order note
            await _orderService.InsertOrderNoteAsync(new OrderNote
            {
                Note = "!!! PAGO DENEGADO !!! " + Request.Form,
                DisplayToCustomer = false,
                CreatedOnUtc = DateTime.UtcNow
            });
            return RedirectToAction("Index", "Home", new { area = "" });
        }

        public IActionResult Error()
        {
            return RedirectToRoute("Homepage");
        }
    }
}