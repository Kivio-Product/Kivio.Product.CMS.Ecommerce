using Microsoft.AspNetCore.Http;

namespace Nop.Plugin.Payments.MercadoPago.Models
{
    public class PaymentResponse
    {
        public int OrderId { get; set; }
        public string ExternalReference { get; set; }
        public string Status { get; set; }
        public string CollectionStatus { get; set; }
        public string PaymentId { get; set; }

        public static PaymentResponse FromHttpRequest(HttpRequest request)
        {
            var parameters = request.Query;

            var orderId = 0;
            var orderIdRaw = parameters["orderId"].ToString();
            if (!string.IsNullOrWhiteSpace(orderIdRaw))
                int.TryParse(orderIdRaw, out orderId);

            return new PaymentResponse
            {
                OrderId = orderId,
                ExternalReference = parameters["external_reference"],
                Status = parameters["status"],
                CollectionStatus = parameters["collection_status"],
                PaymentId = parameters["payment_id"]
            };
        }
    }
}