using Nop.Plugin.Payments.PayU.Models;
using Nop.Services.Payments;
using Nop.Core.Domain.Orders;
using Nop.Core.Domain.Payments;

namespace Nop.Plugin.Payments.PayU.Services
{
    public interface IPayUService
    {
        public void RedirectToPayUPayment(PostProcessPaymentRequest postProcessPaymentRequest);
        public Task<(bool succeeded, int orderId)> ReturnAsync(PaymentResponse paymentResponse);
        public Task<(bool succeeded, int orderId)> ConfirmAsync(ConfirmationResponse confirmationResponse);
        public Task<bool> HidePaymentMethodAsync();
        public Task<decimal> GetAdditionalFeeAsync(IList<ShoppingCartItem> cart);

    }
}
