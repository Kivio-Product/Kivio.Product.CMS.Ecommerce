using Nop.Core;
using Nop.Core.Domain.Customers;
using Nop.Core.Domain.Messages;
using Nop.Core.Domain.Orders;
using Nop.Core.Domain.Stores;
using Nop.Services.Configuration;
using Nop.Services.Customers;
using Nop.Services.Logging;
using Nop.Services.Messages;
using Plugin.ElectronicInvoice.SIIGO.Models;

namespace Plugin.ElectronicInvoice.SIIGO.Services
{
    public class SiigoNotificationService : ISiigoNotificationService
    {
        private readonly IEmailSender _emailSender;
        private readonly IEmailAccountService _emailAccountService;
        private readonly EmailAccountSettings _emailAccountSettings;
        private readonly ICustomerService _customerService;
        private readonly ISettingService _settingService;
        private readonly ILogger _logger;
        private readonly IStoreContext _storeContext;

        public SiigoNotificationService(
            IEmailSender emailSender,
            IEmailAccountService emailAccountService,
            EmailAccountSettings emailAccountSettings,
            ICustomerService customerService,
            ISettingService settingService,
            ILogger logger,
            IStoreContext storeContext)
        {
            _emailSender = emailSender;
            _emailAccountService = emailAccountService;
            _emailAccountSettings = emailAccountSettings;
            _customerService = customerService;
            _settingService = settingService;
            _logger = logger;
            _storeContext = storeContext;
        }

        public async Task SendInvoiceNotificationAsync(Order order, SiigoInvoiceResponse invoiceResponse)
        {
            try
            {
                var siigoSettings = await _settingService.LoadSettingAsync<SiigoSettings>();
                
                if (!siigoSettings.SendByEmail)
                    return;

                var customer = await _customerService.GetCustomerByIdAsync(order.CustomerId);
                var store = await _storeContext.GetCurrentStoreAsync();
                var defaultEmailAccount = await _emailAccountService.GetEmailAccountByIdAsync(_emailAccountSettings.DefaultEmailAccountId);

                var subject = $"Electronic Invoice #{invoiceResponse.Number} - {store.Name}";
                var body = BuildInvoiceEmailBody(order, invoiceResponse, customer, store);

                await _emailSender.SendEmailAsync(
                    emailAccount: defaultEmailAccount,
                    subject: subject,
                    body: body,
                    fromAddress: defaultEmailAccount?.Email ?? "noreply@store.com",
                    fromName: store.Name,
                    toAddress: customer.Email,
                    toName: $"{customer.FirstName} {customer.LastName}");

                if (siigoSettings.LogEnabled)
                {
                    await _logger.InformationAsync($"SIIGO invoice notification sent to {customer.Email} for order {order.Id}");
                }
            }
            catch (Exception ex)
            {
                await _logger.ErrorAsync($"Error sending SIIGO invoice notification for order {order.Id}: {ex.Message}", ex);
            }
        }

        public async Task SendErrorNotificationAsync(Order order, string error)
        {
            try
            {
                var siigoSettings = await _settingService.LoadSettingAsync<SiigoSettings>();
                
                if (!siigoSettings.LogEnabled)
                    return;

                var store = await _storeContext.GetCurrentStoreAsync();
                var defaultEmailAccount = await _emailAccountService.GetEmailAccountByIdAsync(_emailAccountSettings.DefaultEmailAccountId);
                var adminEmail = defaultEmailAccount?.Email ?? "admin@store.com";

                var subject = $"SIIGO Invoice Error - Order #{order.Id}";
                var body = BuildErrorEmailBody(order, error, store);

                await _emailSender.SendEmailAsync(
                    emailAccount: defaultEmailAccount,
                    subject: subject,
                    body: body,
                    fromAddress: adminEmail,
                    fromName: store.Name,
                    toAddress: adminEmail,
                    toName: "Administrator");

                await _logger.WarningAsync($"SIIGO error notification sent for order {order.Id}");
            }
            catch (Exception ex)
            {
                await _logger.ErrorAsync($"Error sending SIIGO error notification for order {order.Id}: {ex.Message}", ex);
            }
        }

        private string BuildInvoiceEmailBody(Order order, SiigoInvoiceResponse invoiceResponse, Customer customer, Store store)
        {
            return $@"
                <!DOCTYPE html>
                <html>
                <head>
                    <meta charset='utf-8'>
                    <title>Electronic Invoice</title>
                    <style>
                        body {{ font-family: Arial, sans-serif; }}
                        .container {{ max-width: 600px; margin: 0 auto; padding: 20px; }}
                        .header {{ background-color: #f8f9fa; padding: 20px; text-align: center; }}
                        .content {{ padding: 20px; }}
                        .footer {{ background-color: #f8f9fa; padding: 10px; text-align: center; font-size: 12px; }}
                        .invoice-details {{ background-color: #e9ecef; padding: 15px; margin: 15px 0; }}
                    </style>
                </head>
                <body>
                    <div class='container'>
                        <div class='header'>
                            <h1>{store.Name}</h1>
                            <h2>Electronic Invoice</h2>
                        </div>
                        <div class='content'>
                            <p>Dear {customer.FirstName} {customer.LastName},</p>
                            
                            <p>Your electronic invoice has been generated successfully.</p>
                            
                            <div class='invoice-details'>
                                <h3>Invoice Details</h3>
                                <p><strong>Invoice Number:</strong> {invoiceResponse.Number}</p>
                                <p><strong>Date:</strong> {invoiceResponse.Date}</p>
                                <p><strong>SIIGO ID:</strong> {invoiceResponse.Id}</p>
                                <p><strong>Order:</strong> #{order.Id}</p>
                                <p><strong>Total:</strong> ${invoiceResponse.Total:N2} {invoiceResponse.Currency?.Code}</p>
                                <p><strong>Status:</strong> {invoiceResponse.Status}</p>
                            </div>
                            
                            <p>This invoice complies with Colombian electronic invoicing regulations.</p>
                            
                            <p>If you have any questions, please don't hesitate to contact us.</p>
                            
                            <p>Thank you for your purchase.</p>
                        </div>
                        <div class='footer'>
                            <p>This is an automated email, please do not reply.</p>
                            <p>{store.Name} - SIIGO Electronic Invoicing</p>
                        </div>
                    </div>
                </body>
                </html>";
        }

        private string BuildErrorEmailBody(Order order, string error, Store store)
        {
            return $@"
                <!DOCTYPE html>
                <html>
                <head>
                    <meta charset='utf-8'>
                    <title>SIIGO Invoice Error</title>
                    <style>
                        body {{ font-family: Arial, sans-serif; }}
                        .container {{ max-width: 600px; margin: 0 auto; padding: 20px; }}
                        .header {{ background-color: #dc3545; color: white; padding: 20px; text-align: center; }}
                        .content {{ padding: 20px; }}
                        .error-details {{ background-color: #f8d7da; padding: 15px; margin: 15px 0; color: #721c24; }}
                    </style>
                </head>
                <body>
                    <div class='container'>
                        <div class='header'>
                            <h1>SIIGO Invoice Error</h1>
                        </div>
                        <div class='content'>
                            <p>An error occurred while generating the electronic invoice.</p>
                            
                            <div class='error-details'>
                                <h3>Error Details</h3>
                                <p><strong>Order:</strong> #{order.Id}</p>
                                <p><strong>Customer:</strong> {order.CustomerId}</p>
                                <p><strong>Order Total:</strong> ${order.OrderTotal:N2}</p>
                                <p><strong>Error Date:</strong> {DateTime.Now:yyyy-MM-dd HH:mm:ss}</p>
                                <p><strong>Error:</strong> {error}</p>
                            </div>
                            
                            <p>Please review the SIIGO plugin configuration and check the logs for more details.</p>
                        </div>
                    </div>
                </body>
                </html>";
        }
    }
}
