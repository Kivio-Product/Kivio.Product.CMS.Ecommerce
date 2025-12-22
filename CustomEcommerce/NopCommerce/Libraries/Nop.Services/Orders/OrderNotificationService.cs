using Nop.Core.Domain.Localization;
using Nop.Core.Domain.Orders;
using Nop.Services.Common;
using Nop.Services.Localization;
using Nop.Services.Messages;
using Nop.Services.Vendors;

namespace Nop.Services.Orders;

/// <summary>
/// Order notification service
/// </summary>
public class OrderNotificationService : IOrderNotificationService
{
    #region Fields

    protected readonly ILocalizationService _localizationService;
    protected readonly IOrderService _orderService;
    protected readonly IPdfService _pdfService;
    protected readonly IVendorService _vendorService;
    protected readonly IWorkflowMessageService _workflowMessageService;
    protected readonly LocalizationSettings _localizationSettings;
    protected readonly OrderSettings _orderSettings;

    #endregion

    #region Ctor

    public OrderNotificationService(
        ILocalizationService localizationService,
        IOrderService orderService,
        IPdfService pdfService,
        IVendorService vendorService,
        IWorkflowMessageService workflowMessageService,
        LocalizationSettings localizationSettings,
        OrderSettings orderSettings)
    {
        _localizationService = localizationService;
        _orderService = orderService;
        _pdfService = pdfService;
        _vendorService = vendorService;
        _workflowMessageService = workflowMessageService;
        _localizationSettings = localizationSettings;
        _orderSettings = orderSettings;
    }

    #endregion

    #region Methods

    /// <summary>
    /// Send "order placed" notifications and save order notes
    /// </summary>
    /// <param name="order">Order</param>
    /// <returns>A task that represents the asynchronous operation</returns>
    public virtual async Task SendOrderPlacedNotificationsAsync(Order order)
    {
        //notes, messages
        await AddOrderNoteAsync(order, "Order placed");

        //send email notifications
        var orderPlacedStoreOwnerNotificationQueuedEmailIds = await _workflowMessageService.SendOrderPlacedStoreOwnerNotificationAsync(order, _localizationSettings.DefaultAdminLanguageId);
        if (orderPlacedStoreOwnerNotificationQueuedEmailIds.Any())
            await AddOrderNoteAsync(order, $"\"Order placed\" email (to store owner) has been queued. Queued email identifiers: {string.Join(", ", orderPlacedStoreOwnerNotificationQueuedEmailIds)}.");

        var orderPlacedAttachmentFilePath = _orderSettings.AttachPdfInvoiceToOrderPlacedEmail ?
            (await _pdfService.SaveOrderPdfToDiskAsync(order)) : null;
        var orderPlacedAttachmentFileName = _orderSettings.AttachPdfInvoiceToOrderPlacedEmail ?
            (string.Format(await _localizationService.GetResourceAsync("PDFInvoice.FileName"), order.CustomOrderNumber) + ".pdf") : null;
        var orderPlacedCustomerNotificationQueuedEmailIds = await _workflowMessageService
            .SendOrderPlacedCustomerNotificationAsync(order, order.CustomerLanguageId, orderPlacedAttachmentFilePath, orderPlacedAttachmentFileName);
        if (orderPlacedCustomerNotificationQueuedEmailIds.Any())
            await AddOrderNoteAsync(order, $"\"Order placed\" email (to customer) has been queued. Queued email identifiers: {string.Join(", ", orderPlacedCustomerNotificationQueuedEmailIds)}.");

        var vendors = await GetVendorsInOrderAsync(order);
        foreach (var vendor in vendors)
        {
            var orderPlacedVendorNotificationQueuedEmailIds = await _workflowMessageService.SendOrderPlacedVendorNotificationAsync(order, vendor, _localizationSettings.DefaultAdminLanguageId);
            if (orderPlacedVendorNotificationQueuedEmailIds.Any())
                await AddOrderNoteAsync(order, $"\"Order placed\" email (to vendor) has been queued. Queued email identifiers: {string.Join(", ", orderPlacedVendorNotificationQueuedEmailIds)}.");
        }

        if (order.AffiliateId == 0)
            return;

        var orderPlacedAffiliateNotificationQueuedEmailIds = await _workflowMessageService.SendOrderPlacedAffiliateNotificationAsync(order, _localizationSettings.DefaultAdminLanguageId);
        if (orderPlacedAffiliateNotificationQueuedEmailIds.Any())
            await AddOrderNoteAsync(order, $"\"Order placed\" email (to affiliate) has been queued. Queued email identifiers: {string.Join(", ", orderPlacedAffiliateNotificationQueuedEmailIds)}.");
    }

    #endregion

    #region Utilities

    /// <summary>
    /// Add order note
    /// </summary>
    /// <param name="order">Order</param>
    /// <param name="note">Note text</param>
    /// <returns>A task that represents the asynchronous operation</returns>
    protected virtual async Task AddOrderNoteAsync(Order order, string note)
    {
        await _orderService.InsertOrderNoteAsync(new OrderNote
        {
            OrderId = order.Id,
            Note = note,
            DisplayToCustomer = false,
            CreatedOnUtc = DateTime.UtcNow
        });
    }

    /// <summary>
    /// Get a list of vendors in order (order items)
    /// </summary>
    /// <param name="order">Order</param>
    /// <returns>
    /// A task that represents the asynchronous operation
    /// The task result contains the vendors
    /// </returns>
    protected virtual async Task<IList<Nop.Core.Domain.Vendors.Vendor>> GetVendorsInOrderAsync(Order order)
    {
        var pIds = (await _orderService.GetOrderItemsAsync(order.Id)).Select(x => x.ProductId).ToArray();

        return await _vendorService.GetVendorsByProductIdsAsync(pIds);
    }

    #endregion
}
