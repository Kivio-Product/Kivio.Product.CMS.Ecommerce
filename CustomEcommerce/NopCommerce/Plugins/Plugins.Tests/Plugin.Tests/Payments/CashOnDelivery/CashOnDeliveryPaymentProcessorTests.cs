namespace Plugins.Tests.Plugin.CashOnDelivery
{
    using Moq;
    using Nop.Core.Domain.Payments;
    using Nop.Core;
    using Nop.Services.Configuration;
    using Nop.Services.Localization;
    using Nop.Services.Orders;
    using Nop.Services.Payments;
    using NUnit.Framework;
    using Nop.Plugin.Payments.CashOnDelivery;
    using Nop.Core.Domain.Orders;
    using static SkiaSharp.HarfBuzz.SKShaper;
    using Microsoft.AspNetCore.Http;
    using Nop.Services.Customers;
    using Nop.Services.Common;
    using Nop.Services.Catalog;
    using Nop.Services.Directory;

    [TestFixture]
    public class CashOnDeliveryPaymentProcessorTests
    {
        private Mock<CashOnDeliveryPaymentSettings> _mockSettings;
        private Mock<ILocalizationService> _mockLocalizationService;
        private Mock<IOrderTotalCalculationService> _mockOrderTotalService;
        private Mock<ISettingService> _mockSettingService;
        private Mock<IShoppingCartService> _mockShoppingCartService;
        private Mock<IWebHelper> _mockWebHelper;
        private Mock<IOrderService> _mockOrderService;
        private Mock<ICustomerService> _mockCustomerService;
        private Mock<IAddressService> _mockAddressService;
        private Mock<IProductService> _mockProductService;
        private Mock<IStateProvinceService> _mockStateProvinceService;
        private Mock<ICountryService> _mockCountryService;
        private CashOnDeliveryPaymentProcessor _paymentProcessor;

        [SetUp]
        public void Setup()
        {
            // Arrange: Initialize mocks and dependencies
            _mockSettings = new Mock<CashOnDeliveryPaymentSettings>();
            _mockLocalizationService = new Mock<ILocalizationService>();
            _mockOrderTotalService = new Mock<IOrderTotalCalculationService>();
            _mockSettingService = new Mock<ISettingService>();
            _mockShoppingCartService = new Mock<IShoppingCartService>();
            _mockWebHelper = new Mock<IWebHelper>();
            _mockOrderService = new Mock<IOrderService>();
            _mockCustomerService = new Mock<ICustomerService>();
            _mockAddressService = new Mock<IAddressService>();
            _mockProductService = new Mock<IProductService>();
            _mockStateProvinceService = new Mock<IStateProvinceService>();
            _mockCountryService = new Mock<ICountryService>();

            // Create an instance of the class under test
            _paymentProcessor = new CashOnDeliveryPaymentProcessor(
                _mockSettings.Object,
                _mockLocalizationService.Object,
                _mockOrderTotalService.Object,
                _mockSettingService.Object,
                _mockShoppingCartService.Object,
                _mockWebHelper.Object,
                _mockOrderService.Object,
                _mockCustomerService.Object,
                _mockAddressService.Object,
                _mockProductService.Object,
                _mockStateProvinceService.Object,
                _mockCountryService.Object
            );
        }

        [Test]
        public async Task ProcessPaymentAsync_AlwaysReturnsPendingStatus()
        {
            // Arrange
            var request = new ProcessPaymentRequest();

            // Act
            var result = await _paymentProcessor.ProcessPaymentAsync(request);

            // Assert
            Assert.That(result.NewPaymentStatus, Is.EqualTo(PaymentStatus.Pending));
        }

        [Test]
        public async Task HidePaymentMethodAsync_ShippableProductRequiredAndCartDoesNotRequireShipping_ReturnsTrue()
        {
            // Arrange
            _mockSettings.Setup(s => s.ShippableProductRequired).Returns(true);
            var cart = new List<ShoppingCartItem>();
            _mockShoppingCartService.Setup(s => s.ShoppingCartRequiresShippingAsync(cart))
                                    .ReturnsAsync(false);

            // Act
            var result = await _paymentProcessor.HidePaymentMethodAsync(cart);

            // Assert
            Assert.That(result, Is.True);
        }

        [Test]
        public async Task HidePaymentMethodAsync_ShippableProductNotRequired_ReturnsFalse()
        {
            // Arrange
            _mockSettings.Setup(s => s.ShippableProductRequired).Returns(false);

            // Act
            var result = await _paymentProcessor.HidePaymentMethodAsync(new List<ShoppingCartItem>());

            // Assert
            Assert.That(result, Is.False);
        }

        [Test]
        public async Task GetAdditionalHandlingFeeAsync_CalculatesFeeUsingOrderTotalService()
        {
            // Arrange
            var cart = new List<ShoppingCartItem>();
            _mockSettings.Setup(s => s.AdditionalFee).Returns(5);
            _mockSettings.Setup(s => s.AdditionalFeePercentage).Returns(true);
            _mockOrderTotalService.Setup(s => s.CalculatePaymentAdditionalFeeAsync(
                                        cart, 5, true))
                                .ReturnsAsync(10);

            // Act
            var fee = await _paymentProcessor.GetAdditionalHandlingFeeAsync(cart);

            // Assert
            Assert.That(10, Is.EqualTo(fee));
        }

        [Test]
        public async Task CanRePostProcessPaymentAsync_AlwaysReturnsFalse()
        {
            // Arrange
            var order = new Order();

            // Act
            var result = await _paymentProcessor.CanRePostProcessPaymentAsync(order);

            // Assert
            Assert.That(result, Is.False);
        }

        [Test]
        public async Task CaptureAsync_AlwaysReturnsNotSupportedError()
        {
            // Act
            var result = await _paymentProcessor.CaptureAsync(new CapturePaymentRequest());

            // Assert
            Assert.That("Capture method not supported", Is.EqualTo(result.Errors.First()));
        }

        [Test]
        public async Task RefundAsync_AlwaysReturnsNotSupportedError()
        {
            // Act
            var result = await _paymentProcessor.RefundAsync(new RefundPaymentRequest());

            // Assert
            Assert.That("Refund method not supported", Is.EqualTo(result.Errors.First()));
        }

        [Test]
        public void SupportCapture_AlwaysReturnsFalse()
        {
            // Assert
            Assert.That(_paymentProcessor.SupportCapture,Is.False);
        }

        [Test]
        public void SkipPaymentInfo_ReturnsValueFromSettings()
        {
            // Arrange
            _mockSettings.Setup(s => s.SkipPaymentInfo).Returns(true);

            // Assert
            Assert.That(_paymentProcessor.SkipPaymentInfo,Is.True);
        }

        [Test]
        public async Task ProcessRecurringPaymentAsync_AlwaysReturnsNotSupportedError()
        {
            // Act
            var result = await _paymentProcessor.ProcessRecurringPaymentAsync(new ProcessPaymentRequest());

            // Assert
            Assert.That(result.Errors, Contains.Item("Recurring payment not supported"));
        }

        [Test]
        public async Task CancelRecurringPaymentAsync_AlwaysReturnsNotSupportedError()
        {
            // Act
            var result = await _paymentProcessor.CancelRecurringPaymentAsync(new CancelRecurringPaymentRequest());

            // Assert
            Assert.That(result.Errors, Contains.Item("Recurring payment not supported"));
        }

        [Test]
        public async Task ValidatePaymentFormAsync_AlwaysReturnsEmptyList()
        {
            // Arrange
            var form = new FormCollection(new Dictionary<string, Microsoft.Extensions.Primitives.StringValues>());

            // Act
            var result = await _paymentProcessor.ValidatePaymentFormAsync(form);

            // Assert
            Assert.That(result, Is.Empty);
        }

        [Test]
        public async Task GetPaymentInfoAsync_AlwaysReturnsNewProcessPaymentRequest()
        {
            // Arrange
            var form = new FormCollection(new Dictionary<string, Microsoft.Extensions.Primitives.StringValues>());

            // Act
            var result = await _paymentProcessor.GetPaymentInfoAsync(form);

            // Assert
            Assert.That(result, Is.TypeOf<ProcessPaymentRequest>());
        }

        [Test]
        public void GetConfigurationPageUrl_ReturnsCorrectUrl()
        {
            // Arrange
            _mockWebHelper.Setup(w => w.GetStoreLocation(true)).Returns("https://example.com/");

            // Act
            var url = _paymentProcessor.GetConfigurationPageUrl();

            // Assert
            Assert.That(url, Is.EqualTo("Admin/PaymentCashOnDelivery/Configure"));
        }

        [Test]
        public async Task GetPaymentMethodDescriptionAsync_ReturnsDescriptionFromLocalizationService()
        {
            // Arrange
            _mockLocalizationService.Setup(l => l.GetResourceAsync("Plugins.Payment.CashOnDelivery.PaymentMethodDescription"))
                                    .ReturnsAsync("Pay by \"Cash on delivery\"");

            // Act
            var description = await _paymentProcessor.GetPaymentMethodDescriptionAsync();

            // Assert
            Assert.That(description, Is.EqualTo("Pay by \"Cash on delivery\""));
        }

        [Test]
        public void SupportCapture_ReturnsFalse()
        {
            // Act & Assert
            Assert.That(_paymentProcessor.SupportCapture, Is.False);
        }

        [Test]
        public void SupportPartiallyRefund_ReturnsFalse()
        {
            // Act & Assert
            Assert.That(_paymentProcessor.SupportPartiallyRefund, Is.False);
        }

        [Test]
        public void SupportRefund_ReturnsFalse()
        {
            // Act & Assert
            Assert.That(_paymentProcessor.SupportRefund, Is.False);
        }

        [Test]
        public void SupportVoid_ReturnsFalse()
        {
            // Act & Assert
            Assert.That(_paymentProcessor.SupportVoid, Is.False);
        }

        [Test]
        public void RecurringPaymentType_ReturnsNotSupported()
        {
            // Act & Assert
            Assert.That(_paymentProcessor.RecurringPaymentType, Is.EqualTo(RecurringPaymentType.NotSupported));
        }

        [Test]
        public void PaymentMethodType_ReturnsStandard()
        {
            // Act & Assert
            Assert.That(_paymentProcessor.PaymentMethodType, Is.EqualTo(PaymentMethodType.Standard));
        }
    }
}
