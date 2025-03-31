using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Moq;
using Nop.Core;
using Nop.Core.Domain.Orders;
using Nop.Core.Domain.Payments;
using Nop.Plugin.Payments.Sermepa;
using Nop.Plugin.Payments.Sermepa.Components;
using Nop.Services.Configuration;
using Nop.Services.Localization;
using Nop.Services.Payments;
using Nop.Services.Plugins;
using NUnit.Framework;
using Newtonsoft.Json;

namespace Nop.Plugin.Payments.Sermepa.Tests
{
    [TestFixture]
    public class SermepaPaymentProcessorTests
    {
        private Mock<SermepaPaymentSettings> _sermepaPaymentSettingsMock;
        private Mock<ISettingService> _settingServiceMock;
        private Mock<IWebHelper> _webHelperMock;
        private Mock<ILocalizationService> _localizationServiceMock;
        private Mock<IHttpContextAccessor> _httpContextAccessorMock;
        private SermepaPaymentProcessor _sermepaPaymentProcessor;
        private Mock<IWorkContext> _workContextMock;
        private Mock<IStoreContext> _storeContextMock;

        [SetUp]
        public void SetUp()
        {
            _sermepaPaymentSettingsMock = new Mock<SermepaPaymentSettings>();
            _settingServiceMock = new Mock<ISettingService>();
            _webHelperMock = new Mock<IWebHelper>();
            _localizationServiceMock = new Mock<ILocalizationService>();
            _httpContextAccessorMock = new Mock<IHttpContextAccessor>();
            _workContextMock = new Mock<IWorkContext>();
            _storeContextMock = new Mock<IStoreContext>();

            _sermepaPaymentProcessor = new SermepaPaymentProcessor(
                _sermepaPaymentSettingsMock.Object,
                _settingServiceMock.Object,
                _webHelperMock.Object,
                _localizationServiceMock.Object,
                _httpContextAccessorMock.Object);
        }

        [Test]
        public void SupportCapture_ShouldReturnFalse()
        {
            Assert.That(_sermepaPaymentProcessor.SupportCapture, Is.False);
        }

        [Test]
        public void SupportPartiallyRefund_ShouldReturnFalse()
        {
            Assert.That(_sermepaPaymentProcessor.SupportPartiallyRefund, Is.False);
        }

        [Test]
        public void SupportRefund_ShouldReturnFalse()
        {
            Assert.That(_sermepaPaymentProcessor.SupportRefund, Is.False);
        }

        [Test]
        public void SupportVoid_ShouldReturnFalse()
        {
            Assert.That(_sermepaPaymentProcessor.SupportVoid, Is.False);
        }

        [Test]
        public void RecurringPaymentType_ShouldReturnNotSupported()
        {
            Assert.That(_sermepaPaymentProcessor.RecurringPaymentType, Is.EqualTo(RecurringPaymentType.NotSupported));
        }

        [Test]
        public void PaymentMethodType_ShouldReturnRedirection()
        {
            Assert.That(_sermepaPaymentProcessor.PaymentMethodType, Is.EqualTo(PaymentMethodType.Redirection));
        }

        [Test]
        public void SkipPaymentInfo_ShouldReturnFalse()
        {
            Assert.That(_sermepaPaymentProcessor.SkipPaymentInfo, Is.False);
        }

        [Test]
        public async Task GetPaymentMethodDescriptionAsync_ShouldReturnDescription()
        {
            var expectedDescription = "You will be redirected to Sermepa site to complete the order.";
            _localizationServiceMock
                .Setup(x => x.GetResourceAsync("Plugins.Payments.Sermepa.PaymentMethodDescription"))
                .ReturnsAsync(expectedDescription);

            var result = await _sermepaPaymentProcessor.GetPaymentMethodDescriptionAsync();

            Assert.That(result, Is.EqualTo(expectedDescription));
        }

        [Test]
        public async Task InstallAsync_ShouldSaveSettingsAndAddLocaleResources()
        {
            // Arrange
            _localizationServiceMock
                .Setup(x => x.AddOrUpdateLocaleResourceAsync(
                    It.IsAny<string>(), // resourceName
                    It.IsAny<string>(), // resourceValue
                    It.IsAny<string>()  // languageCulture (optional parameter)
                ))
                .Returns(Task.CompletedTask);

            // Act
            await _sermepaPaymentProcessor.InstallAsync();

            // Assert
            _settingServiceMock.Verify(x => x.SaveSettingAsync(It.IsAny<SermepaPaymentSettings>(), 0), Times.Once);
            _localizationServiceMock.Verify(
                x => x.AddOrUpdateLocaleResourceAsync(
                    It.IsAny<string>(), // resourceName
                    It.IsAny<string>(), // resourceValue
                    It.IsAny<string>()  // languageCulture (optional parameter)
                ),
                Times.AtLeastOnce);
        }

        [Test]
        public async Task UninstallAsync_ShouldDeleteSettingsAndLocaleResources()
        {
            await _sermepaPaymentProcessor.UninstallAsync();

            _localizationServiceMock.Verify(x => x.DeleteLocaleResourcesAsync("Plugins.Payments.Sermepa", null), Times.Once);
        }

        [Test]
        public void GetConfigurationPageUrl_ShouldReturnCorrectUrl()
        {
            var expectedUrl = "http://example.com/Admin/PaymentSermepa/Configure";
            _webHelperMock.Setup(x => x.GetStoreLocation(null)).Returns("http://example.com/");

            var result = _sermepaPaymentProcessor.GetConfigurationPageUrl();

            Assert.That(result, Is.EqualTo(expectedUrl));
        }

        [Test]
        public async Task CanRePostProcessPaymentAsync_ShouldReturnFalse()
        {
            var order = new Order();

            var result = await _sermepaPaymentProcessor.CanRePostProcessPaymentAsync(order);

            Assert.That(result, Is.False);
        }

        [Test]
        public async Task GetAdditionalHandlingFeeAsync_ShouldReturnAdditionalFee()
        {
            var cart = new List<ShoppingCartItem>();
            _sermepaPaymentSettingsMock.Setup(x => x.AdditionalFee).Returns(5.0m);

            var result = await _sermepaPaymentProcessor.GetAdditionalHandlingFeeAsync(cart);

            Assert.That(result, Is.EqualTo(5.0m));
        }

        [Test]
        public void GetPublicViewComponentName_ShouldReturnCorrectName()
        {
            var result = _sermepaPaymentProcessor.GetPublicViewComponentName();

            Assert.That(result, Is.EqualTo("PaymentSermepa"));
        }

        [Test]
        public async Task ProcessPaymentAsync_ShouldReturnPendingStatus()
        {
            var processPaymentRequest = new ProcessPaymentRequest();

            var result = await _sermepaPaymentProcessor.ProcessPaymentAsync(processPaymentRequest);

            Assert.That(result.NewPaymentStatus, Is.EqualTo(PaymentStatus.Pending));
        }

        [Test]
        public async Task CaptureAsync_ShouldReturnNotSupportedError()
        {
            var capturePaymentRequest = new CapturePaymentRequest();

            var result = await _sermepaPaymentProcessor.CaptureAsync(capturePaymentRequest);

            Assert.That(result.Errors, Contains.Item("Capture method not supported"));
        }

        [Test]
        public async Task RefundAsync_ShouldReturnNotSupportedError()
        {
            var refundPaymentRequest = new RefundPaymentRequest();

            var result = await _sermepaPaymentProcessor.RefundAsync(refundPaymentRequest);

            Assert.That(result.Errors, Contains.Item("Refund method not supported"));
        }

        [Test]
        public async Task VoidAsync_ShouldReturnNotSupportedError()
        {
            var voidPaymentRequest = new VoidPaymentRequest();

            var result = await _sermepaPaymentProcessor.VoidAsync(voidPaymentRequest);

            Assert.That(result.Errors, Contains.Item("Void method not supported"));
        }

        [Test]
        public async Task ProcessRecurringPaymentAsync_ShouldReturnNotSupportedError()
        {
            var processPaymentRequest = new ProcessPaymentRequest();

            var result = await _sermepaPaymentProcessor.ProcessRecurringPaymentAsync(processPaymentRequest);

            Assert.That(result.Errors, Contains.Item("Recurring payment not supported"));
        }

        [Test]
        public async Task CancelRecurringPaymentAsync_ShouldReturnNotSupportedError()
        {
            var cancelPaymentRequest = new CancelRecurringPaymentRequest();

            var result = await _sermepaPaymentProcessor.CancelRecurringPaymentAsync(cancelPaymentRequest);

            Assert.That(result.Errors, Contains.Item("Recurring payment not supported"));
        }

        [Test]
        public async Task ValidatePaymentFormAsync_ShouldReturnEmptyList()
        {
            var form = new FormCollection(new Dictionary<string, Microsoft.Extensions.Primitives.StringValues>());

            var result = await _sermepaPaymentProcessor.ValidatePaymentFormAsync(form);

            Assert.That(result, Is.Empty);
        }

        [Test]
        public async Task GetPaymentInfoAsync_ShouldReturnProcessPaymentRequest()
        {
            var form = new FormCollection(new Dictionary<string, Microsoft.Extensions.Primitives.StringValues>());

            var result = await _sermepaPaymentProcessor.GetPaymentInfoAsync(form);

            Assert.That(result, Is.InstanceOf<ProcessPaymentRequest>());
        }

        [Test]
        public void GetPublicViewComponent_ShouldReturnCorrectType()
        {
            var result = _sermepaPaymentProcessor.GetPublicViewComponent();

            Assert.That(result, Is.EqualTo(typeof(PaymentSermepaViewComponent)));
        }
    }
}