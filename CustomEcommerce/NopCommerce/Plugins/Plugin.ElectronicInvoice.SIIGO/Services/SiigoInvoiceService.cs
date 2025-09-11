using Newtonsoft.Json;
using Nop.Core;
using Nop.Core.Domain.Common;
using Nop.Core.Domain.Customers;
using Nop.Core.Domain.Directory;
using Nop.Services.Logging;
using Nop.Core.Domain.Orders;
using Nop.Services.Configuration;
using Nop.Services.Customers;
using Nop.Services.Directory;
using Nop.Services.Orders;
using Nop.Services.Common;
using Nop.Services.Catalog;
using Nop.Services.Stores;
using Plugin.ElectronicInvoice.SIIGO.Models;
using Plugin.ElectronicInvoice.SIIGO.Data;
using System.Text;
using Nop.Core.Domain.Logging;
using Microsoft.IdentityModel.Tokens;

namespace Plugin.ElectronicInvoice.SIIGO.Services
{
    public class SiigoInvoiceService : ISiigoInvoiceService
    {
        private readonly ISettingService _settingService;
        private readonly ILogger _logger;
        private readonly IOrderService _orderService;
        private readonly ICustomerService _customerService;
        private readonly ICountryService _countryService;
        private readonly IStateProvinceService _stateProvinceService;
        private readonly IWebHelper _webHelper;
        private readonly IGenericAttributeService _genericAttributeService;
        private readonly ISiigoAuthService _siigoAuthService;
        private readonly IProductService _productService;
        private readonly IStoreContext _storeContext;
        private readonly HttpClient _httpClient;
        private List<CountryData> _countryData;

        public SiigoInvoiceService(
            ISettingService settingService,
            ILogger logger,
            IOrderService orderService,
            ICustomerService customerService,
            ICountryService countryService,
            IStateProvinceService stateProvinceService,
            IWebHelper webHelper,
            IGenericAttributeService genericAttributeService,
            ISiigoAuthService siigoAuthService,
            IProductService productService,
            IStoreContext storeContext,
            HttpClient httpClient)
        {
            _settingService = settingService;
            _logger = logger;
            _orderService = orderService;
            _customerService = customerService;
            _countryService = countryService;
            _stateProvinceService = stateProvinceService;
            _webHelper = webHelper;
            _genericAttributeService = genericAttributeService;
            _siigoAuthService = siigoAuthService;
            _productService = productService;
            _storeContext = storeContext;
            _httpClient = httpClient;
            
            LoadCountryData();
        }

        public async Task<SiigoInvoiceResponse> CreateInvoiceAsync(Order order)
        {
            try
            {
                // Check if the order already has a SIIGO invoice
                if (await order.HasSiigoInvoiceAsync(_genericAttributeService))
                {
                    var existingInvoiceId = await order.GetSiigoInvoiceIdAsync(_genericAttributeService);
                    var existingInvoiceNumber = await order.GetSiigoInvoiceNumberAsync(_genericAttributeService);
                    var existingStatus = await order.GetSiigoInvoiceStatusAsync(_genericAttributeService);
                    
                    await _logger.InformationAsync($"Order {order.Id} already has a SIIGO invoice. ID: {existingInvoiceId}, Number: {existingInvoiceNumber}, Status: {existingStatus}");
                    
                    return null;
                }

                var siigoSettings = await _settingService.LoadSettingAsync<SiigoSettings>();
                
                if (!ValidateConfiguration(siigoSettings))
                {
                    throw new Exception("Invalid SIIGO configuration");
                }

                // Set invoice status to "Processing" before creating
                await order.SetSiigoInvoiceStatusAsync(_genericAttributeService, "Processing");

                var invoiceRequest = await BuildInvoiceRequestAsync(order, siigoSettings);
                var response = await SendInvoiceToSiigoAsync(invoiceRequest, siigoSettings);

                // Persist SIIGO invoice data using OrderExtensions
                await order.SetSiigoInvoiceIdAsync(_genericAttributeService, response.Id);
                await order.SetSiigoInvoiceNumberAsync(_genericAttributeService, response.Number);
                await order.SetSiigoInvoiceDateAsync(_genericAttributeService, DateTime.Now);
                await order.SetSiigoInvoiceStatusAsync(_genericAttributeService, response.Status ?? "Created");

                if (siigoSettings.LogEnabled)
                {
                    await _logger.InformationAsync($"SIIGO invoice created successfully for order {order.Id}. SIIGO ID: {response.Id}, Number: {response.Number}");
                }

                // Send email if configured
                if (siigoSettings.SendByEmail)
                {
                    var customer = await _customerService.GetCustomerByIdAsync(order.CustomerId);
                    if (!string.IsNullOrEmpty(customer?.Email))
                    {
                        var emailSent = await SendInvoiceEmailAsync(response.Id, customer.Email, siigoSettings.CopyToEmail);
                        if (emailSent && siigoSettings.LogEnabled)
                        {
                            await _logger.InformationAsync($"SIIGO invoice email sent successfully for order {order.Id} to {customer.Email}");
                        }
                        else if (!emailSent && siigoSettings.LogEnabled)
                        {
                            await _logger.WarningAsync($"Failed to send SIIGO invoice email for order {order.Id}");
                        }
                    }
                }

                return response;
            }
            catch (Exception ex)
            {
                // Set error status if creation failed
                await order.SetSiigoInvoiceStatusAsync(_genericAttributeService, $"Error: {ex.Message}");
                
                await _logger.ErrorAsync($"Error creating SIIGO invoice for order {order.Id}: {ex.Message}", ex);
                throw;
            }
        }

        public bool ValidateConfiguration()
        {
            var siigoSettings = _settingService.LoadSetting<SiigoSettings>();
            return ValidateConfiguration(siigoSettings);
        }

        public async Task<(bool hasInvoice, string invoiceId, long invoiceNumber, DateTime? invoiceDate, string status)> GetOrderInvoiceInfoAsync(Order order)
        {
            try
            {
                var hasInvoice = await order.HasSiigoInvoiceAsync(_genericAttributeService);
                if (!hasInvoice)
                {
                    return (false, null, 0, null, null);
                }

                var invoiceId = await order.GetSiigoInvoiceIdAsync(_genericAttributeService);
                var invoiceNumber = await order.GetSiigoInvoiceNumberAsync(_genericAttributeService);
                var invoiceDate = await order.GetSiigoInvoiceDateAsync(_genericAttributeService);
                var status = await order.GetSiigoInvoiceStatusAsync(_genericAttributeService);

                return (true, invoiceId, invoiceNumber, invoiceDate, status);
            }
            catch (Exception ex)
            {
                await _logger.ErrorAsync($"Error getting SIIGO invoice info for order {order.Id}: {ex.Message}", ex);
                return (false, null, 0, null, null);
            }
        }

        private bool ValidateConfiguration(SiigoSettings siigoSettings)
        {
            return !string.IsNullOrEmpty(siigoSettings.Username) &&
                   !string.IsNullOrEmpty(siigoSettings.AccessKey) &&
                   !string.IsNullOrEmpty(siigoSettings.PartnerId) &&
                   !string.IsNullOrEmpty(siigoSettings.ApiBaseUrl) &&
                   siigoSettings.DocumentId > 0 &&
                   siigoSettings.AccountGroup > 0;
        }

        public async Task<(string stateCode, string cityCode)> GetLocationCodesAsync(string stateProvinceName, string cityName)
        {
            try
            {
                if (_countryData == null || !_countryData.Any())
                    return (null, null);

                var countryInfo = _countryData.FirstOrDefault();
                if (countryInfo?.States == null) return (null, null);

                var state = countryInfo.States.FirstOrDefault(s => 
                    s.Name.Equals(stateProvinceName, StringComparison.OrdinalIgnoreCase));

                if (state == null) return (null, null);

                var city = state.Cities?.FirstOrDefault(c => 
                    c.Name.Equals(cityName, StringComparison.OrdinalIgnoreCase));

                var cityCode = city != null ? $"{state.Code}{city.Code}" : state.Code;

                return (state.Code, cityCode);
            }
            catch (Exception ex)
            {
                await _logger.ErrorAsync($"Error getting location codes: {ex.Message}", ex);
                return (null, null);
            }
        }

        /// <summary>
        /// Extracts the identification document from billing address custom attributes (XML format)
        /// Expected structure: <Attributes><AddressAttribute ID="X"><AddressAttributeValue><Value>DOCUMENT</Value></AddressAttributeValue></AddressAttribute></Attributes>
        /// </summary>
        private async Task<string> ExtractIdentificationFromCustomAttributesAsync(string customAttributes, int targetAddressAttributeId)
        {
            try
            {
                if (string.IsNullOrEmpty(customAttributes))
                    return "222222222222"; // Default fallback

                // Parse XML custom attributes to find identification document
                var xmlDoc = new System.Xml.XmlDocument();
                xmlDoc.LoadXml(customAttributes);

                // First, try to find the specific AddressAttribute by ID
                var specificAttributeNode = xmlDoc.SelectSingleNode($"//AddressAttribute[@ID='{targetAddressAttributeId}']/AddressAttributeValue/Value");
                if (specificAttributeNode != null && !string.IsNullOrEmpty(specificAttributeNode.InnerText))
                {
                    // Extract only numbers from the value (7-15 digits for Colombian documents)
                    var numbers = System.Text.RegularExpressions.Regex.Match(specificAttributeNode.InnerText, @"\d{7,15}");
                    if (numbers.Success)
                    {
                        await _logger.InformationAsync($"Found identification '{numbers.Value}' from AddressAttribute ID='{targetAddressAttributeId}' (configured target)");
                        return numbers.Value;
                    }
                    else
                    {
                        await _logger.WarningAsync($"AddressAttribute ID='{targetAddressAttributeId}' found but does not contain a valid identification number (7-15 digits). Value: '{specificAttributeNode.InnerText}'");
                    }
                }
                else
                {
                    await _logger.WarningAsync($"AddressAttribute ID='{targetAddressAttributeId}' not found in custom attributes. Available attributes: {GetAvailableAddressAttributeIds(xmlDoc)}");
                }

                // Fallback: Look for any AddressAttribute with a valid identification number
                var addressAttributeValues = xmlDoc.SelectNodes("//AddressAttribute/AddressAttributeValue/Value");
                if (addressAttributeValues != null)
                {
                    foreach (System.Xml.XmlNode valueNode in addressAttributeValues)
                    {
                        if (!string.IsNullOrEmpty(valueNode.InnerText))
                        {
                            // Extract only numbers from the value (7-15 digits for Colombian documents)
                            var numbers = System.Text.RegularExpressions.Regex.Match(valueNode.InnerText, @"\d{7,15}");
                            if (numbers.Success)
                            {
                                // Get the parent AddressAttribute ID for logging
                                var parentAttribute = valueNode.SelectSingleNode("ancestor::AddressAttribute");
                                var attributeId = parentAttribute?.Attributes?["ID"]?.Value ?? "unknown";
                                
                                await _logger.InformationAsync($"Found identification '{numbers.Value}' from AddressAttribute ID='{attributeId}' (fallback search)");
                                return numbers.Value;
                            }
                        }
                    }
                }

                // Final fallback: Extract any sequence of 7-15 digits from the entire XML content
                var anyNumbers = System.Text.RegularExpressions.Regex.Match(customAttributes, @"\d{7,15}");
                if (anyNumbers.Success)
                {
                    await _logger.InformationAsync($"Found identification '{anyNumbers.Value}' using final fallback regex pattern");
                    return anyNumbers.Value;
                }
                
                await _logger.WarningAsync($"No identification found in custom attributes XML. Target ID: {targetAddressAttributeId}. Content: {customAttributes}");
                
                return "222222222222"; // Default fallback
            }
            catch (System.Xml.XmlException xmlEx)
            {
                await _logger.WarningAsync($"Invalid XML in custom attributes, trying fallback parsing: {xmlEx.Message}. Content: {customAttributes}");
                
                // Fallback to simple string parsing if XML is malformed
                var fallbackNumbers = System.Text.RegularExpressions.Regex.Match(customAttributes, @"\d{7,15}");
                if (fallbackNumbers.Success)
                {
                    await _logger.InformationAsync($"Found identification '{fallbackNumbers.Value}' using XML fallback regex");
                    return fallbackNumbers.Value;
                }
                
                return "222222222222"; // Default fallback
            }
            catch (Exception ex)
            {
                await _logger.WarningAsync($"Error extracting identification from custom attributes: {ex.Message}");
                return "222222222222"; // Default fallback
            }
        }

        /// <summary>
        /// Helper method to get available AddressAttribute IDs for logging purposes
        /// </summary>
        private string GetAvailableAddressAttributeIds(System.Xml.XmlDocument xmlDoc)
        {
            try
            {
                var attributeNodes = xmlDoc.SelectNodes("//AddressAttribute[@ID]");
                if (attributeNodes == null || attributeNodes.Count == 0)
                    return "none";

                var ids = new List<string>();
                foreach (System.Xml.XmlNode node in attributeNodes)
                {
                    var id = node.Attributes?["ID"]?.Value;
                    if (!string.IsNullOrEmpty(id))
                        ids.Add(id);
                }

                return string.Join(", ", ids);
            }
            catch
            {
                return "error reading IDs";
            }
        }

        private async Task<SiigoInvoiceRequest> BuildInvoiceRequestAsync(Order order, SiigoSettings siigoSettings)
        {
            var customer = await _customerService.GetCustomerByIdAsync(order.CustomerId);
            var billingAddress = await _customerService.GetCustomerBillingAddressAsync(customer);
            
            // Get state province information
            string stateProvinceName = "Cundinamarca";
            if (billingAddress?.StateProvinceId.HasValue == true)
            {
                var stateProvince = await _stateProvinceService.GetStateProvinceByIdAsync(billingAddress.StateProvinceId.Value);
                stateProvinceName = stateProvince?.Name ?? "Cundinamarca";
            }
            
            var (stateCode, cityCode) = await GetLocationCodesAsync(
                stateProvinceName,
                billingAddress?.City ?? "Bogotá");

            // If no codes found, use defaults for Bogotá
            stateCode ??= "11";
            cityCode ??= "11001";

            // Create items from order items
            var siigoItems = await CreateSiigoItemsFromOrderAsync(order, siigoSettings);

            // Extract identification document from billing address custom attributes
            var identification = await ExtractIdentificationFromCustomAttributesAsync(billingAddress?.CustomAttributes, siigoSettings.IdentificationAddressAttributeId);

            var invoiceRequest = new SiigoInvoiceRequest
            {
                Document = new SiigoDocument { Id = siigoSettings.DocumentId },
                Date = DateTime.Now.ToString("yyyy-MM-dd"),
                Customer = new SiigoCustomer
                {
                    PersonType = "Person",
                    IdType = "13", // National ID
                    Identification = identification,
                    Name = new List<string> { 
                        billingAddress?.FirstName ?? customer.FirstName ?? "Consumidor", 
                        billingAddress?.LastName ?? customer.LastName ?? "Final" 
                    },
                    Address = new SiigoAddress
                    {
                        Address = billingAddress?.Address1 ?? "Not specified",
                        City = new SiigoCity
                        {
                            CountryCode = "CO",
                            CountryName = "Colombia",
                            StateCode = stateCode,
                            StateName = stateProvinceName,
                            CityCode = cityCode,
                            CityName = billingAddress?.City ?? "Bogotá"
                        },
                        PostalCode = billingAddress?.ZipPostalCode ?? cityCode
                    },
                    Phones = new List<SiigoPhone>
                    {
                        new SiigoPhone
                        {
                            Indicative = "57",
                            Number = billingAddress?.PhoneNumber ?? "3211111111",
                            Extension = ""
                        }
                    }
                },
                Currency = new SiigoCurrency
                {
                    Code = !string.IsNullOrEmpty(siigoSettings.CurrencyCode) ? siigoSettings.CurrencyCode : "COP",
                    ExchangeRate = !string.IsNullOrEmpty(siigoSettings.ExchangeRate) ? siigoSettings.ExchangeRate : "1"
                },
                Seller = siigoSettings.SellerId,
                Stamp = new SiigoStamp { Send = siigoSettings.SendStamp },
                Mail = new SiigoMail { Send = siigoSettings.SendByEmail },
                Observations = $"Automatically generated invoice for order #{order.Id} - {DateTime.Now:yyyy-MM-dd HH:mm:ss}",
                Items = siigoItems,
                Payments = new List<SiigoPayment>
                {
                    new SiigoPayment
                    {
                        Id = siigoSettings.PaymentMethodId,
                        Value = Math.Round(order.OrderSubtotalInclTax, 1),
                        DueDate = DateTime.Now.ToString("yyyy-MM-dd")
                    }
                },
                AdditionalFields = new SiigoAdditionalFields
                {
                    BillingId = order.OrderGuid.ToString(),
                    CustomerEmail = customer.Email ?? "no-email@gmail.com",
                    GeneratedBy = "NopCommerce SIIGO Plugin",
                    PosId = _webHelper.GetCurrentIpAddress()
                }
            };

            return invoiceRequest;
        }

        /// <summary>
        /// Creates SIIGO items from order items, ensuring each product exists in SIIGO
        /// </summary>
        private async Task<List<SiigoItem>> CreateSiigoItemsFromOrderAsync(Order order, SiigoSettings siigoSettings)
        {
            var siigoItems = new List<SiigoItem>();
            var orderItems = await _orderService.GetOrderItemsAsync(order.Id);

            // Load tax category mappings
            var storeId = await _storeContext.GetActiveStoreScopeConfigurationAsync();
            var taxMappingSettings = await _settingService.LoadSettingAsync<SiigoTaxCategoryMappingSettings>(storeId);

            foreach (var orderItem in orderItems)
            {
                try
                {
                    var product = await _productService.GetProductByIdAsync(orderItem.ProductId);
                    if (product == null)
                    {
                        await _logger.WarningAsync($"Product with ID {orderItem.ProductId} not found for order item {orderItem.Id}");
                        continue;
                    }

                    // Generate SIIGO-compatible product SKU
                    var productSku = await GenerateSiigoProductSkuAsync(product);
                    
                    if (siigoSettings.LogEnabled && productSku != product.Sku)
                    {
                        var reason = string.IsNullOrEmpty(product.Sku) ? "no original SKU" : "original SKU too long or invalid";
                        await _logger.InformationAsync($"Product {product.Id} ({reason}), using generated SKU: {productSku}");
                    }

                    // Ensure the product exists in SIIGO
                    var siigoProduct = await EnsureSiigoProductExistsAsync(productSku, product.Name, siigoSettings);

                    // Determine tax information based on product's tax category and our mappings
                    List<SiigoTax> taxes = null;
                    
                    // Only add taxes if the item has tax applied AND we have a valid mapping
                    if (orderItem.UnitPriceInclTax > orderItem.UnitPriceExclTax && product.TaxCategoryId > 0)
                    {
                        var siigoTaxCode = taxMappingSettings.GetSiigoTaxCode(product.TaxCategoryId);
                        if (siigoTaxCode.HasValue)
                        {
                            taxes = new List<SiigoTax>
                            {
                                new SiigoTax { Id = siigoTaxCode.Value }
                            };

                            if (siigoSettings.LogEnabled)
                            {
                                await _logger.InformationAsync($"Applied SIIGO tax code {siigoTaxCode.Value} for product {product.Name} (Tax Category ID: {product.TaxCategoryId})");
                            }
                        }
                        else
                        {
                            if (siigoSettings.LogEnabled)
                            {
                                await _logger.WarningAsync($"Product {product.Name} has tax category {product.TaxCategoryId} but no SIIGO tax code mapping found. No tax will be applied to this item.");
                            }
                        }
                    }

                    // Create the SIIGO item
                    var siigoItem = new SiigoItem
                    {
                        Code = productSku,
                        Description = product.Name,
                        Quantity = orderItem.Quantity,
                        Price = Math.Round(orderItem.UnitPriceExclTax, 1),
                        Taxes = taxes // Will be null if no tax should be applied
                    };

                    siigoItems.Add(siigoItem);

                    if (siigoSettings.LogEnabled)
                    {
                        var taxInfo = taxes != null ? $"with tax code {taxes.First().Id}" : "without tax";
                        await _logger.InformationAsync($"Created SIIGO item for product {product.Name} (SKU: {productSku}) - Qty: {orderItem.Quantity}, Price: {orderItem.UnitPriceInclTax} {taxInfo}");
                    }
                }
                catch (Exception ex)
                {
                    await _logger.ErrorAsync($"Error creating SIIGO item for order item {orderItem.Id}: {ex.Message}", ex);
                    
                    // Create a fallback item if individual item creation fails
                    var fallbackItem = new SiigoItem
                    {
                        Code = siigoSettings.DefaultItemCode,
                        Description = $"Order item #{orderItem.Id} - Product unavailable",
                        Quantity = orderItem.Quantity,
                        Price = Math.Round(orderItem.UnitPriceInclTax, 1),
                        Taxes = null // No tax mapping available for fallback items
                    };
                    
                    siigoItems.Add(fallbackItem);
                }
            }

            // If no items were created successfully, create a single consolidated item
            if (!siigoItems.Any())
            {
                await _logger.WarningAsync($"No individual items could be created for order {order.Id}, creating consolidated item");
                
                siigoItems.Add(new SiigoItem
                {
                    Code = siigoSettings.DefaultItemCode,
                    Description = $"Order #{order.Id} - Various products",
                    Quantity = 1,
                    Price = Math.Round(order.OrderSubtotalInclTax, 1),
                    Taxes = null // No tax mapping available for consolidated items
                });
            }

            return siigoItems;
        }

        private async Task<SiigoInvoiceResponse> SendInvoiceToSiigoAsync(SiigoInvoiceRequest invoiceRequest, SiigoSettings siigoSettings)
        {
            var json = JsonConvert.SerializeObject(invoiceRequest, Formatting.None);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            // Get valid token from auth service
            var bearerToken = await _siigoAuthService.GetValidTokenAsync();

            _httpClient.DefaultRequestHeaders.Clear();
            _httpClient.DefaultRequestHeaders.Add("Partner-Id", siigoSettings.PartnerId);
            _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {bearerToken}");
            
            var response = await _httpClient.PostAsync($"{siigoSettings.ApiBaseUrl}/v1/invoices", content);
            var responseContent = await response.Content.ReadAsStringAsync();

            if (siigoSettings.LogEnabled)
            {
                await _logger.InsertLogAsync(LogLevel.Information,$"SIIGO INVOICE API Request:",json);
                await _logger.InsertLogAsync(LogLevel.Information,$"SIIGO INVOICE API Response:",responseContent);
            }

            // If we get an unauthorized response, try refreshing the token once
            if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
            {
                await _logger.WarningAsync("SIIGO API returned 401 Unauthorized, attempting token refresh");
                
                try
                {
                    bearerToken = await _siigoAuthService.RefreshTokenAsync();
                    
                    _httpClient.DefaultRequestHeaders.Clear();
                    _httpClient.DefaultRequestHeaders.Add("Partner-Id", siigoSettings.PartnerId);
                    _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {bearerToken}");
                    
                    response = await _httpClient.PostAsync($"{siigoSettings.ApiBaseUrl}/v1/invoices", content);
                    responseContent = await response.Content.ReadAsStringAsync();
                    
                    if (siigoSettings.LogEnabled)
                    {
                        await _logger.InsertLogAsync(LogLevel.Information,"SIIGO INVOICE API Retry Response", responseContent);
                    }
                }
                catch (Exception tokenEx)
                {
                    await _logger.ErrorAsync($"Failed to refresh SIIGO token: {tokenEx.Message}", tokenEx);
                    throw new Exception($"Authentication failed with SIIGO API: {tokenEx.Message}");
                }
            }

            if (!response.IsSuccessStatusCode)
            {
                var errorResponse = JsonConvert.DeserializeObject<SiigoErrorResponse>(responseContent);
                var errorMessage = errorResponse?.Errors?.FirstOrDefault()?.Detail ?? "Unknown error";
                throw new Exception($"SIIGO API Error: {errorMessage}");
            }

            return JsonConvert.DeserializeObject<SiigoInvoiceResponse>(responseContent);
        }

        /// <summary>
        /// Generates a SIIGO-compatible product SKU with proper length constraints
        /// </summary>
        /// <param name="product">The product</param>
        /// <param name="storeName">Store name (optional, will be fetched if not provided)</param>
        /// <returns>A SKU that is guaranteed to be 30 characters or less</returns>
        private async Task<string> GenerateSiigoProductSkuAsync(Nop.Core.Domain.Catalog.Product product, string storeName = null)
        {
            // Use existing SKU if it's valid and within length limit
            if (!string.IsNullOrEmpty(product.Sku) && product.Sku.Length <= 30)
            {
                return product.Sku;
            }

            // Get store name if not provided
            if (string.IsNullOrEmpty(storeName))
            {
                var store = await _storeContext.GetCurrentStoreAsync();
                storeName = store?.Name?.Replace(" ", "").ToUpper() ?? "STORE";
            }

            // Ensure store name is not too long (max 15 chars to leave room for prefix and ID)
            if (storeName.Length > 15)
                storeName = storeName.Substring(0, 15);

            // Generate custom SKU: PROD_{STORE_NAME}_{PRODUCT_ID}
            var generatedSku = $"PROD_{storeName}_{product.Id}";

            // Ensure final SKU is within 30 character limit
            if (generatedSku.Length > 30)
            {
                // Fallback to shorter format if still too long
                generatedSku = $"P_{product.Id}";
            }

            return generatedSku;
        }

        /// <summary>
        /// Validates if a product exists in SIIGO by its SKU
        /// </summary>
        private async Task<SiigoProductResponse> GetSiigoProductBySkuAsync(string sku, SiigoSettings siigoSettings)
        {
            try
            {
                var bearerToken = await _siigoAuthService.GetValidTokenAsync();

                _httpClient.DefaultRequestHeaders.Clear();
                _httpClient.DefaultRequestHeaders.Add("Partner-Id", siigoSettings.PartnerId);
                _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {bearerToken}");

                var response = await _httpClient.GetAsync($"{siigoSettings.ApiBaseUrl}/v1/products?code={Uri.EscapeDataString(sku)}");
                var responseContent = await response.Content.ReadAsStringAsync();

                if (siigoSettings.LogEnabled)
                {
                    await _logger.InsertLogAsync(LogLevel.Information,"SIIGO Get Product API Response", $"SIIGO Get Product API Response: {responseContent}");
                }

                if (response.IsSuccessStatusCode)
                {
                    var searchResponse = JsonConvert.DeserializeObject<SiigoProductSearchResponse>(responseContent);
                    return searchResponse?.Results?.FirstOrDefault();
                }

                return null;
            }
            catch (Exception ex)
            {
                await _logger.ErrorAsync($"Error getting SIIGO product by SKU {sku}: {ex.Message}", ex);
                return null;
            }
        }

        /// <summary>
        /// Creates a product in SIIGO
        /// </summary>
        private async Task<SiigoProductResponse> CreateSiigoProductAsync(string sku, string productName, SiigoSettings siigoSettings)
        {
            try
            {
                var productRequest = new SiigoProductRequest
                {
                    Code = sku,
                    Name = productName,
                    AccountGroup = siigoSettings.AccountGroup
                };

                var json = JsonConvert.SerializeObject(productRequest, Formatting.None);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var bearerToken = await _siigoAuthService.GetValidTokenAsync();

                _httpClient.DefaultRequestHeaders.Clear();
                _httpClient.DefaultRequestHeaders.Add("Partner-Id", siigoSettings.PartnerId);
                _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {bearerToken}");

                var response = await _httpClient.PostAsync($"{siigoSettings.ApiBaseUrl}/v1/products", content);
                var responseContent = await response.Content.ReadAsStringAsync();

                if (siigoSettings.LogEnabled)
                {
                    await _logger.InsertLogAsync(LogLevel.Information,"SIIGO Create Product API Request", json);
                    await _logger.InsertLogAsync(LogLevel.Information,"SIIGO Create Product API Response", responseContent);
                }

                // If we get an unauthorized response, try refreshing the token once
                if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                {
                    await _logger.WarningAsync("SIIGO API returned 401 Unauthorized during product creation, attempting token refresh");
                    
                    try
                    {
                        bearerToken = await _siigoAuthService.RefreshTokenAsync();
                        
                        _httpClient.DefaultRequestHeaders.Clear();
                        _httpClient.DefaultRequestHeaders.Add("Partner-Id", siigoSettings.PartnerId);
                        _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {bearerToken}");
                        
                        response = await _httpClient.PostAsync($"{siigoSettings.ApiBaseUrl}/v1/products", content);
                        responseContent = await response.Content.ReadAsStringAsync();
                        
                        if (siigoSettings.LogEnabled)
                        {
                            await _logger.InsertLogAsync(LogLevel.Information,"SIIGO Create Product API Retry Response", responseContent);
                        }
                    }
                    catch (Exception tokenEx)
                    {
                        await _logger.ErrorAsync($"Failed to refresh SIIGO token during product creation: {tokenEx.Message}", tokenEx);
                        throw new Exception($"Authentication failed with SIIGO API during product creation: {tokenEx.Message}");
                    }
                }

                if (!response.IsSuccessStatusCode)
                {
                    var errorResponse = JsonConvert.DeserializeObject<SiigoErrorResponse>(responseContent);
                    var errorMessage = errorResponse?.Errors?.FirstOrDefault()?.Detail ?? "Unknown error";
                    throw new Exception($"SIIGO Product Creation API Error: {errorMessage}");
                }

                return JsonConvert.DeserializeObject<SiigoProductResponse>(responseContent);
            }
            catch (Exception ex)
            {
                await _logger.ErrorAsync($"Error creating SIIGO product {sku}: {ex.Message}", ex);
                throw;
            }
        }

        /// <summary>
        /// Ensures a product exists in SIIGO, creates it if it doesn't exist
        /// </summary>
        private async Task<SiigoProductResponse> EnsureSiigoProductExistsAsync(string sku, string productName, SiigoSettings siigoSettings)
        {
            try
            {
                // First, try to get the product
                var existingProduct = await GetSiigoProductBySkuAsync(sku, siigoSettings);
                if (existingProduct != null)
                {
                    if (siigoSettings.LogEnabled)
                    {
                        await _logger.InformationAsync($"Product with SKU {sku} already exists in SIIGO");
                    }
                    return existingProduct;
                }

                // Product doesn't exist, create it
                if (siigoSettings.LogEnabled)
                {
                    await _logger.InformationAsync($"Product with SKU {sku} not found in SIIGO, creating it");
                }

                return await CreateSiigoProductAsync(sku, productName, siigoSettings);
            }
            catch (Exception ex)
            {
                await _logger.ErrorAsync($"Error ensuring SIIGO product exists for SKU {sku}: {ex.Message}", ex);
                throw;
            }
        }

        public async Task<bool> SendInvoiceEmailAsync(string invoiceId, string mailTo, string copyTo = null)
        {
            try
            {
                var siigoSettings = await _settingService.LoadSettingAsync<SiigoSettings>();
                
                if (string.IsNullOrEmpty(invoiceId) || string.IsNullOrEmpty(mailTo))
                {
                    await _logger.WarningAsync("Cannot send SIIGO invoice email: missing invoice ID or mail_to address");
                    return false;
                }

                var emailRequest = new SiigoEmailRequest
                {
                    MailTo = mailTo,
                    CopyTo = copyTo
                };

                var json = JsonConvert.SerializeObject(emailRequest, Formatting.None);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var bearerToken = await _siigoAuthService.GetValidTokenAsync();

                _httpClient.DefaultRequestHeaders.Clear();
                _httpClient.DefaultRequestHeaders.Add("Partner-Id", siigoSettings.PartnerId);
                _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {bearerToken}");

                var response = await _httpClient.PostAsync($"{siigoSettings.ApiBaseUrl}/v1/invoices/{invoiceId}/mail", content);
                var responseContent = await response.Content.ReadAsStringAsync();

                if (siigoSettings.LogEnabled)
                {
                    await _logger.InsertLogAsync(LogLevel.Information,"SIIGO Email API Request", json);
                    await _logger.InsertLogAsync(LogLevel.Information,"SIIGO Email API Response", responseContent);
                }

                // If we get an unauthorized response, try refreshing the token once
                if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                {
                    await _logger.WarningAsync("SIIGO API returned 401 Unauthorized during email send, attempting token refresh");
                    
                    try
                    {
                        bearerToken = await _siigoAuthService.RefreshTokenAsync();
                        
                        _httpClient.DefaultRequestHeaders.Clear();
                        _httpClient.DefaultRequestHeaders.Add("Partner-Id", siigoSettings.PartnerId);
                        _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {bearerToken}");
                        
                        response = await _httpClient.PostAsync($"{siigoSettings.ApiBaseUrl}/v1/invoices/{invoiceId}/mail", content);
                        responseContent = await response.Content.ReadAsStringAsync();
                        
                        if (siigoSettings.LogEnabled)
                        {
                            await _logger.InsertLogAsync(LogLevel.Information,"SIIGO Email API Retry Response", responseContent);
                        }
                    }
                    catch (Exception tokenEx)
                    {
                        await _logger.ErrorAsync($"Failed to refresh SIIGO token for email send: {tokenEx.Message}", tokenEx);
                        return false;
                    }
                }

                if (response.IsSuccessStatusCode)
                {
                    if (siigoSettings.LogEnabled)
                    {
                        await _logger.InformationAsync($"SIIGO invoice email sent successfully. Invoice ID: {invoiceId}, Mail to: {mailTo}, Copy to: {copyTo}");
                    }
                    return true;
                }
                else
                {
                    await _logger.ErrorAsync($"SIIGO email API error. Status: {response.StatusCode}, Content: {responseContent}");
                    return false;
                }
            }
            catch (Exception ex)
            {
                await _logger.ErrorAsync($"Error sending SIIGO invoice email for invoice {invoiceId}: {ex.Message}", ex);
                return false;
            }
        }

        private void LoadCountryData()
        {
            try
            {
                var pluginPath = Path.Combine(AppContext.BaseDirectory, "Plugins", "ElectronicInvoice.SIIGO", "countries.json");
                if (File.Exists(pluginPath))
                {
                    var json = File.ReadAllText(pluginPath);
                    _countryData = JsonConvert.DeserializeObject<List<CountryData>>(json);
                }
            }
            catch (Exception ex)
            {
                _logger.ErrorAsync($"Error loading country data: {ex.Message}", ex);
                _countryData = new List<CountryData>();
            }
        }
    }
}
