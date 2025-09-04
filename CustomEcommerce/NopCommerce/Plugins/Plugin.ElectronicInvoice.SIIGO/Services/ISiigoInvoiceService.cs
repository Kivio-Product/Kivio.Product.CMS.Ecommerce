using Nop.Core.Domain.Orders;
using Plugin.ElectronicInvoice.SIIGO.Models;

namespace Plugin.ElectronicInvoice.SIIGO.Services
{
    public interface ISiigoInvoiceService
    {
        Task<SiigoInvoiceResponse> CreateInvoiceAsync(Order order);
        bool ValidateConfiguration();
        Task<(string stateCode, string cityCode)> GetLocationCodesAsync(string stateProvinceName, string cityName);
        Task<(bool hasInvoice, string invoiceId, long invoiceNumber, DateTime? invoiceDate, string status)> GetOrderInvoiceInfoAsync(Order order);
        Task<bool> SendInvoiceEmailAsync(string invoiceId, string mailTo, string copyTo = null);
    }
}
