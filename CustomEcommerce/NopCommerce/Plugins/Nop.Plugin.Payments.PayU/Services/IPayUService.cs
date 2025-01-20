using Nop.Plugin.Payments.PayU.Models;
using Nop.Plugin.Payments.PayU.Models.Notifications;
using Nop.Services.Payments;

namespace Nop.Plugin.Payments.PayU.Services
{
    public interface IPayUService
    {
        void RedirectToPayUPayment(PostProcessPaymentRequest postProcessPaymentRequest);
        //void Notify(Notification notification);
        //Task<RefundPaymentResult> Refund(RefundPaymentRequest refundPaymentRequest);
        bool VerifySignature(string body);
        public Task<(bool succeeded, int orderId)> ReturnAsync(PaymentResponse paymentResponse);
    }
}
