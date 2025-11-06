using Nop.Core.Domain.Orders;
using Nop.Services.Common;

namespace Plugin.ElectronicInvoice.SIIGO.Data
{
    /// <summary>
    /// Extensions for Order entity to store SIIGO billing information
    /// </summary>
    public static class OrderExtensions
    {
        private const string SIIGO_INVOICE_ID_KEY = "SiigoInvoiceId";
        private const string SIIGO_INVOICE_NUMBER_KEY = "SiigoInvoiceNumber";
        private const string SIIGO_INVOICE_DATE_KEY = "SiigoInvoiceDate";
        private const string SIIGO_INVOICE_STATUS_KEY = "SiigoInvoiceStatus";
        private const string SIIGO_INVOICE_PUBLIC_URL_KEY = "SiigoInvoicePublicUrl";

        /// <summary>
        /// Gets the SIIGO invoice ID
        /// </summary>
        public static async Task<string> GetSiigoInvoiceIdAsync(this Order order, IGenericAttributeService genericAttributeService)
        {
            return await genericAttributeService.GetAttributeAsync<string>(order, SIIGO_INVOICE_ID_KEY);
        }

        /// <summary>
        /// Sets the SIIGO invoice ID
        /// </summary>
        public static async Task SetSiigoInvoiceIdAsync(this Order order, IGenericAttributeService genericAttributeService, string invoiceId)
        {
            await genericAttributeService.SaveAttributeAsync(order, SIIGO_INVOICE_ID_KEY, invoiceId);
        }

        /// <summary>
        /// Gets the SIIGO invoice number
        /// </summary>
        public static async Task<long> GetSiigoInvoiceNumberAsync(this Order order, IGenericAttributeService genericAttributeService)
        {
            return await genericAttributeService.GetAttributeAsync<long>(order, SIIGO_INVOICE_NUMBER_KEY);
        }

        /// <summary>
        /// Sets the SIIGO invoice number
        /// </summary>
        public static async Task SetSiigoInvoiceNumberAsync(this Order order, IGenericAttributeService genericAttributeService, long invoiceNumber)
        {
            await genericAttributeService.SaveAttributeAsync(order, SIIGO_INVOICE_NUMBER_KEY, invoiceNumber);
        }

        /// <summary>
        /// Gets the SIIGO invoice date
        /// </summary>
        public static async Task<DateTime?> GetSiigoInvoiceDateAsync(this Order order, IGenericAttributeService genericAttributeService)
        {
            var dateString = await genericAttributeService.GetAttributeAsync<string>(order, SIIGO_INVOICE_DATE_KEY);
            return DateTime.TryParse(dateString, out var date) ? date : null;
        }

        /// <summary>
        /// Sets the SIIGO invoice date
        /// </summary>
        public static async Task SetSiigoInvoiceDateAsync(this Order order, IGenericAttributeService genericAttributeService, DateTime invoiceDate)
        {
            await genericAttributeService.SaveAttributeAsync(order, SIIGO_INVOICE_DATE_KEY, invoiceDate.ToString("yyyy-MM-dd HH:mm:ss"));
        }

        /// <summary>
        /// Gets the SIIGO invoice status
        /// </summary>
        public static async Task<string> GetSiigoInvoiceStatusAsync(this Order order, IGenericAttributeService genericAttributeService)
        {
            return await genericAttributeService.GetAttributeAsync<string>(order, SIIGO_INVOICE_STATUS_KEY);
        }

        /// <summary>
        /// Sets the SIIGO invoice status
        /// </summary>
        public static async Task SetSiigoInvoiceStatusAsync(this Order order, IGenericAttributeService genericAttributeService, string status)
        {
            await genericAttributeService.SaveAttributeAsync(order, SIIGO_INVOICE_STATUS_KEY, status);
        }

        /// <summary>
        /// Checks if the order already has an associated SIIGO invoice
        /// </summary>
        public static async Task<bool> HasSiigoInvoiceAsync(this Order order, IGenericAttributeService genericAttributeService)
        {
            var invoiceId = await order.GetSiigoInvoiceIdAsync(genericAttributeService);
            return !string.IsNullOrEmpty(invoiceId);
        }

        /// <summary>
        /// Gets the SIIGO invoice public URL
        /// </summary>
        public static async Task<string> GetSiigoInvoicePublicUrlAsync(this Order order, IGenericAttributeService genericAttributeService)
        {
            return await genericAttributeService.GetAttributeAsync<string>(order, SIIGO_INVOICE_PUBLIC_URL_KEY);
        }

        /// <summary>
        /// Sets the SIIGO invoice public URL
        /// </summary>
        public static async Task SetSiigoInvoicePublicUrlAsync(this Order order, IGenericAttributeService genericAttributeService, string publicUrl)
        {
            await genericAttributeService.SaveAttributeAsync(order, SIIGO_INVOICE_PUBLIC_URL_KEY, publicUrl);
        }
    }
}
