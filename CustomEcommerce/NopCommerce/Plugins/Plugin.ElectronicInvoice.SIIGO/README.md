# SIIGO Electronic Invoice Plugin for NopCommerce

## Description
This plugin enables automatic integration with SIIGO API to generate electronic invoices in Colombia when an order is marked as paid in NopCommerce.

## Features
- Automatic electronic invoicing when completing an order
- Automatic email delivery to customer
- Automatic mapping of Colombian locations (states and cities)
- Complete configuration from admin panel
- Detailed logging for debugging
- Test mode

## Configuration

### Prerequisites
1. Active SIIGO account
2. Valid SIIGO API authentication token
3. Partner ID configuration in SIIGO

### Installation
1. Copy plugin to NopCommerce `Plugins` folder
2. Restart application
3. Go to Admin → Configuration → Local Plugins
4. Install "SIIGO Electronic Invoice" plugin

### Plugin Configuration
1. Go to Admin → Configuration → Payment Methods → SIIGO Electronic Invoice
2. Configure the following parameters:
   - **API Base URL**: `https://api.siigo.com`
   - **Partner ID**: Your SIIGO Partner ID (e.g., kivio)
   - **Bearer Token**: SIIGO API authentication token
   - **Document ID**: Invoice document type ID in SIIGO
   - **Seller ID**: Seller ID in SIIGO
   - **Payment Method ID**: Payment method ID in SIIGO
   - **Tax IDs**: Tax IDs for cases with and without VAT

### SIIGO Configuration
To obtain required configuration values:

1. **Bearer Token**: Generate from SIIGO developer console
2. **Document ID**: Query document catalog in SIIGO API
3. **Seller ID**: Seller ID configured in SIIGO
4. **Payment Method ID**: Payment method ID configured in SIIGO
5. **Tax IDs**: Tax IDs configured in SIIGO

## Operation

### Automatic Process
1. Customer completes an order in the store
2. When order is marked as "Paid", event is triggered
3. Plugin extracts order and customer information
4. Locations are mapped using `countries.json` file
5. Invoice is created in SIIGO API
6. Email is sent to customer (if configured)
7. Information is logged in order logs

### Location Mapping
Plugin uses `countries.json` file to map Colombian state and city names with codes required by SIIGO.

## SIIGO API

### Invoice Creation Endpoint
```
POST https://api.siigo.com/v1/invoices
```

### Required Headers
```
Content-Type: application/json
Partner-Id: [your-partner-id]
Authorization: Bearer [your-token]
```

## Project Structure
```
Plugin.ElectronicInvoice.SIIGO/
├── Controllers/
│   └── SiigoController.cs
├── Events/
│   └── OrderPaidEventConsumer.cs
├── Infrastructure/
│   ├── DependencyRegistrar.cs
│   ├── PluginNopStartup.cs
│   └── RouteProvider.cs
├── Models/
│   ├── ConfigurationModel.cs
│   ├── CountryModels.cs
│   ├── SiigoInvoiceRequest.cs
│   └── SiigoInvoiceResponse.cs
├── Services/
│   ├── ISiigoInvoiceService.cs
│   └── SiigoInvoiceService.cs
├── Views/
│   └── Configure.cshtml
├── countries.json
├── plugin.json
├── Plugin.ElectronicInvoice.SIIGO.csproj
├── SiigoPlugin.cs
└── SiigoSettings.cs
```

## Logging and Debugging
Plugin generates detailed logs that can be reviewed at:
- Admin → System → Log

Logs include:
- SIIGO API requests and responses
- Processing errors
- Status of created invoices

## Customization
Plugin is designed to be easily customizable:

1. **Data models**: Modify in `/Models/`
2. **Business logic**: Adjust in `/Services/SiigoInvoiceService.cs`
3. **Configuration interface**: Edit `/Views/Configure.cshtml`
4. **Events**: Add new consumers in `/Events/`

## Support
For technical support, contact Kivio development team.

## License
Copyright © Kivio SAS 2025. All rights reserved.
