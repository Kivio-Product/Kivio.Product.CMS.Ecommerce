using Nop.Core.Domain.Orders;
using Nop.Services.Common;

namespace Plugin.ElectronicInvoice.SIIGO.Data
{
    /// <summary>
    /// Extensiones para la entidad Order para almacenar información de facturación SIIGO
    /// </summary>
    public static class OrderExtensions
    {
        private const string SIIGO_INVOICE_ID_KEY = "SiigoInvoiceId";
        private const string SIIGO_INVOICE_NUMBER_KEY = "SiigoInvoiceNumber";
        private const string SIIGO_INVOICE_DATE_KEY = "SiigoInvoiceDate";
        private const string SIIGO_INVOICE_STATUS_KEY = "SiigoInvoiceStatus";

        /// <summary>
        /// Obtiene el ID de la factura SIIGO
        /// </summary>
        public static async Task<string> GetSiigoInvoiceIdAsync(this Order order, IGenericAttributeService genericAttributeService)
        {
            return await genericAttributeService.GetAttributeAsync<string>(order, SIIGO_INVOICE_ID_KEY);
        }

        /// <summary>
        /// Establece el ID de la factura SIIGO
        /// </summary>
        public static async Task SetSiigoInvoiceIdAsync(this Order order, IGenericAttributeService genericAttributeService, string invoiceId)
        {
            await genericAttributeService.SaveAttributeAsync(order, SIIGO_INVOICE_ID_KEY, invoiceId);
        }

        /// <summary>
        /// Obtiene el número de la factura SIIGO
        /// </summary>
        public static async Task<string> GetSiigoInvoiceNumberAsync(this Order order, IGenericAttributeService genericAttributeService)
        {
            return await genericAttributeService.GetAttributeAsync<string>(order, SIIGO_INVOICE_NUMBER_KEY);
        }

        /// <summary>
        /// Establece el número de la factura SIIGO
        /// </summary>
        public static async Task SetSiigoInvoiceNumberAsync(this Order order, IGenericAttributeService genericAttributeService, string invoiceNumber)
        {
            await genericAttributeService.SaveAttributeAsync(order, SIIGO_INVOICE_NUMBER_KEY, invoiceNumber);
        }

        /// <summary>
        /// Obtiene la fecha de la factura SIIGO
        /// </summary>
        public static async Task<DateTime?> GetSiigoInvoiceDateAsync(this Order order, IGenericAttributeService genericAttributeService)
        {
            var dateString = await genericAttributeService.GetAttributeAsync<string>(order, SIIGO_INVOICE_DATE_KEY);
            return DateTime.TryParse(dateString, out var date) ? date : null;
        }

        /// <summary>
        /// Establece la fecha de la factura SIIGO
        /// </summary>
        public static async Task SetSiigoInvoiceDateAsync(this Order order, IGenericAttributeService genericAttributeService, DateTime invoiceDate)
        {
            await genericAttributeService.SaveAttributeAsync(order, SIIGO_INVOICE_DATE_KEY, invoiceDate.ToString("yyyy-MM-dd HH:mm:ss"));
        }

        /// <summary>
        /// Obtiene el estado de la factura SIIGO
        /// </summary>
        public static async Task<string> GetSiigoInvoiceStatusAsync(this Order order, IGenericAttributeService genericAttributeService)
        {
            return await genericAttributeService.GetAttributeAsync<string>(order, SIIGO_INVOICE_STATUS_KEY);
        }

        /// <summary>
        /// Establece el estado de la factura SIIGO
        /// </summary>
        public static async Task SetSiigoInvoiceStatusAsync(this Order order, IGenericAttributeService genericAttributeService, string status)
        {
            await genericAttributeService.SaveAttributeAsync(order, SIIGO_INVOICE_STATUS_KEY, status);
        }

        /// <summary>
        /// Verifica si la orden ya tiene una factura SIIGO asociada
        /// </summary>
        public static async Task<bool> HasSiigoInvoiceAsync(this Order order, IGenericAttributeService genericAttributeService)
        {
            var invoiceId = await order.GetSiigoInvoiceIdAsync(genericAttributeService);
            return !string.IsNullOrEmpty(invoiceId);
        }
    }
}
