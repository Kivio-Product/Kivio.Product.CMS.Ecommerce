namespace Nop.Plugin.Payments.PayU.Tests
{

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Moq;
using Nop.Core;
using Nop.Core.Domain.Orders;
using Nop.Core.Domain.Payments;
using Nop.Plugin.Payments.PayU;
using Nop.Plugin.Payments.PayU.Services;
using Nop.Plugin.Payments.PayU.Components;
using Nop.Services.Configuration;
using Nop.Services.Localization;
using Nop.Services.Payments;
using NUnit.Framework;

    [TestFixture]
    public class PayUPaymentProcessorTests
    {
        private Mock<ISettingService> _settingServiceMock;
        private Mock<IPayUService> _payUServiceMock;
        private Mock<IWebHelper> _webHelperMock;
        private Mock<ILocalizationService> _localizationServiceMock;
        private PayUPaymentProcessor _payUPaymentProcessor;

        [SetUp]
        public void SetUp()
        {
            _settingServiceMock = new Mock<ISettingService>();
            _payUServiceMock = new Mock<IPayUService>();
            _webHelperMock = new Mock<IWebHelper>();
            _localizationServiceMock = new Mock<ILocalizationService>();

            _payUPaymentProcessor = new PayUPaymentProcessor(
                _settingServiceMock.Object,
                _payUServiceMock.Object,
                _webHelperMock.Object,
                _localizationServiceMock.Object);
        }

        [Test]
        public void SupportCapture_ShouldReturnFalse()
        {
            Assert.That(_payUPaymentProcessor.SupportCapture, Is.False);
        }

        [Test]
        public void SupportVoid_ShouldReturnFalse()
        {
            Assert.That(_payUPaymentProcessor.SupportVoid, Is.False);
        }

        [Test]
        public void SupportPartiallyRefund_ShouldReturnTrue()
        {
            Assert.That(_payUPaymentProcessor.SupportPartiallyRefund, Is.True);
        }

        [Test]
        public void SupportRefund_ShouldReturnTrue()
        {
            Assert.That(_payUPaymentProcessor.SupportRefund, Is.True);
        }

        [Test]
        public void RecurringPaymentType_ShouldReturnNotSupported()
        {
            Assert.That(_payUPaymentProcessor.RecurringPaymentType, Is.EqualTo(RecurringPaymentType.NotSupported));
        }

        [Test]
        public void PaymentMethodType_ShouldReturnRedirection()
        {
            Assert.That(_payUPaymentProcessor.PaymentMethodType, Is.EqualTo(PaymentMethodType.Redirection));
        }

        [Test]
        public void SkipPaymentInfo_ShouldReturnFalse()
        {
            Assert.That(_payUPaymentProcessor.SkipPaymentInfo, Is.False);
        }

        [Test]
        public async Task GetPaymentMethodDescriptionAsync_ShouldReturnDescription()
        {
            var expectedDescription = "You will be redirected to PayU site to complete the payment";
            _localizationServiceMock.Setup(x => x.GetResourceAsync("Plugins.Payments.PayU.PaymentMethodDescription"))
                .ReturnsAsync(expectedDescription);

            var result = await _payUPaymentProcessor.GetPaymentMethodDescriptionAsync();

            Assert.That(result, Is.EqualTo(expectedDescription));
        }

        [Test]
        public async Task InstallAsync_ShouldSaveSettingsAndAddLocaleResources()
        {
            await _payUPaymentProcessor.InstallAsync();

            _settingServiceMock.Verify(x => x.SaveSettingAsync(It.IsAny<PayUPaymentSettings>(),0), Times.Once);
            _localizationServiceMock.Verify(x => x.AddOrUpdateLocaleResourceAsync(It.IsAny<string>(), It.IsAny<string>(),null), Times.AtLeastOnce);
        }

        [Test]
        public async Task UninstallAsync_ShouldDeleteSettingsAndLocaleResources()
        {
            await _payUPaymentProcessor.UninstallAsync();

            _settingServiceMock.Verify(x => x.DeleteSettingAsync<PayUPaymentSettings>(), Times.Once);
            _localizationServiceMock.Verify(x => x.DeleteLocaleResourcesAsync(It.IsAny<string>(),null), Times.AtLeastOnce);
        }

        [Test]
        public void GetConfigurationPageUrl_ShouldReturnCorrectUrl()
        {
            var expectedUrl = "Admin/PaymentPayU/Configure";
            _webHelperMock.Setup(x => x.GetStoreLocation(true)).Returns("http://example.com/");

            var result = _payUPaymentProcessor.GetConfigurationPageUrl();

            Assert.That(result, Is.EqualTo(expectedUrl));
        }

        [Test]
        public async Task CanRePostProcessPaymentAsync_ShouldReturnFalse()
        {
            var order = new Order();

            var result = await _payUPaymentProcessor.CanRePostProcessPaymentAsync(order);

            Assert.That(result, Is.False);
        }

        [Test]
        public async Task GetAdditionalHandlingFeeAsync_ShouldReturnZero()
        {
            var cart = new List<ShoppingCartItem>();

            var result = await _payUPaymentProcessor.GetAdditionalHandlingFeeAsync(cart);

            Assert.That(result, Is.EqualTo(0m));
        }

        [Test]
        public void GetPaymentInfo_ShouldReturnProcessPaymentRequest()
        {
            var form = new FormCollection(new Dictionary<string, Microsoft.Extensions.Primitives.StringValues>());

            var result = _payUPaymentProcessor.GetPaymentInfo(form);

            Assert.That(result, Is.InstanceOf<ProcessPaymentRequest>());
        }

        [Test]
        public void GetPublicViewComponentName_ShouldReturnCorrectName()
        {
            var result = _payUPaymentProcessor.GetPublicViewComponentName();

            Assert.That(result, Is.EqualTo("PaymentPayU"));
        }

        [Test]
        public async Task HidePaymentMethodAsync_ShouldReturnFalse()
        {
            var cart = new List<ShoppingCartItem>();

            var result = await _payUPaymentProcessor.HidePaymentMethodAsync(cart);

            Assert.That(result, Is.False);
        }

        [Test]
        public async Task ProcessPaymentAsync_ShouldReturnPendingStatus()
        {
            var processPaymentRequest = new ProcessPaymentRequest();

            var result = await _payUPaymentProcessor.ProcessPaymentAsync(processPaymentRequest);

            Assert.That(result.NewPaymentStatus, Is.EqualTo(PaymentStatus.Pending));
        }

        [Test]
        public async Task PostProcessPaymentAsync_ShouldCallRedirectToPayUPayment()
        {
            var postProcessPaymentRequest = new PostProcessPaymentRequest();

            await _payUPaymentProcessor.PostProcessPaymentAsync(postProcessPaymentRequest);

            _payUServiceMock.Verify(x => x.RedirectToPayUPayment(postProcessPaymentRequest), Times.Once);
        }

        [Test]
        public async Task RefundAsync_ShouldReturnRefundPaymentResult()
        {
            var refundPaymentRequest = new RefundPaymentRequest();

            var result = await _payUPaymentProcessor.RefundAsync(refundPaymentRequest);

            Assert.That(result, Is.InstanceOf<RefundPaymentResult>());
        }

        [Test]
        public async Task ValidatePaymentFormAsync_ShouldReturnEmptyList()
        {
            var form = new FormCollection(new Dictionary<string, Microsoft.Extensions.Primitives.StringValues>());

            var result = await _payUPaymentProcessor.ValidatePaymentFormAsync(form);

            Assert.That(result, Is.Empty);
        }

        [Test]
        public async Task CaptureAsync_ShouldReturnCapturePaymentResult()
        {
            var capturePaymentRequest = new CapturePaymentRequest();

            var result = await _payUPaymentProcessor.CaptureAsync(capturePaymentRequest);

            Assert.That(result, Is.InstanceOf<CapturePaymentResult>());
        }

        [Test]
        public async Task VoidAsync_ShouldReturnVoidPaymentResult()
        {
            var voidPaymentRequest = new VoidPaymentRequest();

            var result = await _payUPaymentProcessor.VoidAsync(voidPaymentRequest);

            Assert.That(result, Is.InstanceOf<VoidPaymentResult>());
        }

        [Test]
        public async Task ProcessRecurringPaymentAsync_ShouldReturnProcessPaymentResult()
        {
            var processPaymentRequest = new ProcessPaymentRequest();

            var result = await _payUPaymentProcessor.ProcessRecurringPaymentAsync(processPaymentRequest);

            Assert.That(result, Is.InstanceOf<ProcessPaymentResult>());
        }

        [Test]
        public async Task CancelRecurringPaymentAsync_ShouldReturnCancelRecurringPaymentResult()
        {
            var cancelPaymentRequest = new CancelRecurringPaymentRequest();

            var result = await _payUPaymentProcessor.CancelRecurringPaymentAsync(cancelPaymentRequest);

            Assert.That(result, Is.InstanceOf<CancelRecurringPaymentResult>());
        }

        [Test]
        public async Task GetPaymentInfoAsync_ShouldReturnProcessPaymentRequest()
        {
            var form = new FormCollection(new Dictionary<string, Microsoft.Extensions.Primitives.StringValues>());

            var result = await _payUPaymentProcessor.GetPaymentInfoAsync(form);

            Assert.That(result, Is.InstanceOf<ProcessPaymentRequest>());
        }

        [Test]
        public void GetPublicViewComponent_ShouldReturnCorrectType()
        {
            var result = _payUPaymentProcessor.GetPublicViewComponent();

            Assert.That(result, Is.EqualTo(typeof(PaymentPayUViewComponent)));
        }
    }
}