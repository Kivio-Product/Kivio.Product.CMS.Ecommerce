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
using Plugin.ElectronicInvoice.SIIGO.Models;
using System.Text;

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
            HttpClient httpClient)
        {
            _settingService = settingService;
            _logger = logger;
            _orderService = orderService;
            _customerService = customerService;
            _countryService = countryService;
            _stateProvinceService = stateProvinceService;
            _webHelper = webHelper;
            _httpClient = httpClient;
            
            LoadCountryData();
        }

        public async Task<SiigoInvoiceResponse> CreateInvoiceAsync(Order order)
        {
            try
            {
                var siigoSettings = await _settingService.LoadSettingAsync<SiigoSettings>();
                
                if (!ValidateConfiguration(siigoSettings))
                {
                    throw new Exception("Invalid SIIGO configuration");
                }

                var invoiceRequest = await BuildInvoiceRequestAsync(order, siigoSettings);
                var response = await SendInvoiceToSiigoAsync(invoiceRequest, siigoSettings);

                if (siigoSettings.LogEnabled)
                {
                    await _logger.InformationAsync($"SIIGO invoice created successfully for order {order.Id}. SIIGO ID: {response.Id}");
                }

                return response;
            }
            catch (Exception ex)
            {
                await _logger.ErrorAsync($"Error creating SIIGO invoice for order {order.Id}: {ex.Message}", ex);
                throw;
            }
        }

        public bool ValidateConfiguration()
        {
            var siigoSettings = _settingService.LoadSetting<SiigoSettings>();
            return ValidateConfiguration(siigoSettings);
        }

        private bool ValidateConfiguration(SiigoSettings siigoSettings)
        {
            return !string.IsNullOrEmpty(siigoSettings.BearerToken) &&
                   !string.IsNullOrEmpty(siigoSettings.PartnerId) &&
                   !string.IsNullOrEmpty(siigoSettings.ApiBaseUrl) &&
                   siigoSettings.DocumentId > 0;
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
            stateCode ??= "25";
            cityCode ??= "25001";

            var invoiceRequest = new SiigoInvoiceRequest
            {
                Document = new SiigoDocument { Id = siigoSettings.DocumentId },
                Date = DateTime.Now.ToString("yyyy-MM-dd"),
                Customer = new SiigoCustomer
                {
                    PersonType = "Person",
                    IdType = "13", // National ID
                    Identification = customer.Id.ToString(),
                    Name = new List<string> { $"{customer.FirstName} {customer.LastName}".Trim() },
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
                            Number = billingAddress?.PhoneNumber ?? "0000000",
                            Extension = ""
                        }
                    }
                },
                Currency = new SiigoCurrency
                {
                    Code = "COP", // Colombian peso
                    ExchangeRate = "1"
                },
                Seller = siigoSettings.SellerId,
                Stamp = new SiigoStamp { Send = siigoSettings.SendStamp },
                Mail = new SiigoMail { Send = siigoSettings.SendByEmail },
                Observations = $"Automatically generated invoice for order #{order.Id} - {DateTime.Now:yyyy-MM-dd HH:mm:ss}",
                Items = new List<SiigoItem>
                {
                    new SiigoItem
                    {
                        Code = siigoSettings.DefaultItemCode,
                        Description = $"Order #{order.Id} - Various products",
                        Quantity = 1,
                        Price = order.OrderSubtotalInclTax,
                        Taxes = new List<SiigoTax>
                        {
                            new SiigoTax { Id = order.OrderTax > 0 ? siigoSettings.TaxIdWithTax : siigoSettings.TaxIdWithoutTax }
                        }
                    }
                },
                Payments = new List<SiigoPayment>
                {
                    new SiigoPayment
                    {
                        Id = siigoSettings.PaymentMethodId,
                        Value = order.OrderTotal,
                        DueDate = DateTime.Now.ToString("yyyy-MM-dd")
                    }
                },
                AdditionalFields = new SiigoAdditionalFields
                {
                    BillingId = order.OrderGuid.ToString(),
                    CustomerEmail = customer.Email,
                    GeneratedBy = "NopCommerce SIIGO Plugin",
                    PosId = _webHelper.GetCurrentIpAddress()
                }
            };

            return invoiceRequest;
        }

        private async Task<SiigoInvoiceResponse> SendInvoiceToSiigoAsync(SiigoInvoiceRequest invoiceRequest, SiigoSettings siigoSettings)
        {
            var json = JsonConvert.SerializeObject(invoiceRequest, Formatting.None);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            _httpClient.DefaultRequestHeaders.Clear();
            _httpClient.DefaultRequestHeaders.Add("Partner-Id", siigoSettings.PartnerId);
            _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {siigoSettings.BearerToken}");

            var response = await _httpClient.PostAsync($"{siigoSettings.ApiBaseUrl}/v1/invoices", content);
            var responseContent = await response.Content.ReadAsStringAsync();

            if (siigoSettings.LogEnabled)
            {
                await _logger.InformationAsync($"SIIGO API Request: {json}");
                await _logger.InformationAsync($"SIIGO API Response: {responseContent}");
            }

            if (!response.IsSuccessStatusCode)
            {
                var errorResponse = JsonConvert.DeserializeObject<SiigoErrorResponse>(responseContent);
                var errorMessage = errorResponse?.Errors?.FirstOrDefault()?.Detail ?? "Unknown error";
                throw new Exception($"SIIGO API Error: {errorMessage}");
            }

            return JsonConvert.DeserializeObject<SiigoInvoiceResponse>(responseContent);
        }

        private void LoadCountryData()
        {
            try
            {
                var pluginPath = Path.Combine(AppContext.BaseDirectory, "Plugins", "Plugin.ElectronicInvoice.SIIGO", "Data", "countries.json");
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
