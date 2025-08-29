using Nop.Core.Domain.Orders;
using Plugin.ElectronicInvoice.SIIGO.Models;

namespace Plugin.ElectronicInvoice.SIIGO.Services
{
    public interface ISiigoInvoiceService
    {
        Task<SiigoInvoiceResponse> CreateInvoiceAsync(Order order);
        bool ValidateConfiguration();
        Task<(string stateCode, string cityCode)> GetLocationCodesAsync(string stateProvinceName, string cityName);
    }
}
