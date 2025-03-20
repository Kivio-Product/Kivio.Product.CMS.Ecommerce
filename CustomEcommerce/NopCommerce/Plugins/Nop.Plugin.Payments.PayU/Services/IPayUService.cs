using Nop.Plugin.Payments.PayU.Models;
using Nop.Services.Payments;

namespace Nop.Plugin.Payments.PayU.Services
{
    public interface IPayUService
    {
        public void RedirectToPayUPayment(PostProcessPaymentRequest postProcessPaymentRequest);
        public Task<(bool succeeded, int orderId)> ReturnAsync(PaymentResponse paymentResponse);
        public Task<(bool succeeded, int orderId)> ConfirmAsync(ConfirmationResponse confirmationResponse);
        public Task<bool> HidePaymentMethodAsync();

    }
}
