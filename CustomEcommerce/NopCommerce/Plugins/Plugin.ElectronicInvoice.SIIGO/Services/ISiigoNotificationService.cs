using Nop.Core.Domain.Orders;
using Nop.Services.Messages;
using Plugin.ElectronicInvoice.SIIGO.Models;

namespace Plugin.ElectronicInvoice.SIIGO.Services
{
    public interface ISiigoNotificationService
    {
        Task SendInvoiceNotificationAsync(Order order, SiigoInvoiceResponse invoiceResponse);
        Task SendErrorNotificationAsync(Order order, string error);
    }
}
