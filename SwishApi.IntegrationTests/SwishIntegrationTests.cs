using System;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using SwishApi.Helpers;
using SwishApi.Models;
using Xunit;
using Xunit.Abstractions;

namespace SwishApi.IntegrationTests
{
    public class SwishIntegrationTests
    {
        private readonly ITestOutputHelper _testOutputHelper;
        private readonly FakeLogger<SwishTestClient> _logger;

        public SwishIntegrationTests(ITestOutputHelper testOutputHelper)
        {
            _testOutputHelper = testOutputHelper;
            _logger = new FakeLogger<SwishTestClient>(_testOutputHelper);
        }

        private (X509Certificate2 PrivateCertificate, X509Certificate2Collection CertificateChain) GetPrivateCertificate()
        {
            // Get the path for the test certificate in the TestCert folder in the console application folder, being always copy to the output folder
            string certificatePath = Environment.CurrentDirectory + "\\TestCert\\Swish_Merchant_TestCertificate_1234679304.p12";
            var certificate = CertificateHelpers.GetCertificate(certificatePath, "swish");

            var tlsCertPath = Environment.CurrentDirectory + "\\TestCert\\Swish_TLS_RootCA.pem";
            var tlsCertificate = CertificateHelpers.GetCertificate(tlsCertPath, "swish");
            certificate.CertificateChain.AddRange(tlsCertificate.CertificateChain);
            return certificate;
        }

        [Fact]
        public async Task SetupPaymentRequest_InvalidNumber_Returns_ErrorBE18()
        {
            // Assert
            var certificate = GetPrivateCertificate();
            ISwishClient client = new SwishTestClient(certificate.PrivateCertificate, certificate.CertificateChain, new Uri("https://tabetaltmedswish.se/Test/Callback/"), _logger);
            var id = Guid.NewGuid();

            // ACT
            var request = await client.MakePaymentRequestAsync(id, "46", 1, "Test");


            // Assert
            Assert.Null(request.Response);
            Assert.NotNull(request.Error);
            _logger.LogError(string.Join(", ", request.Error.Errors.Select(t=>t.ErrorMessage)));
            Assert.Contains(request.Error.Errors, error => error.ErrorCode.Equals("BE18"));
        }


        [Fact]
        public async Task SetupPaymentRequest_Returns_Ok()
        {
            // Assert
            var certificate = GetPrivateCertificate();
            ISwishClient client = new SwishTestClient(certificate.PrivateCertificate, certificate.CertificateChain, new Uri("https://tabetaltmedswish.se/Test/Callback/"), _logger);
            var id = Guid.NewGuid();
            var amount = 1.5m;
            var message = "Test " + Guid.NewGuid();
            var phoneNumber = "46731596605";

            // ACT
            var request = await client.MakePaymentRequestAsync(id, phoneNumber, amount, message);


            // Assert
            Assert.Null(request.Error);
            Assert.NotNull(request.Response);
            Assert.EndsWith(id.ToString(), request.Response.Location.ToString());


            await Task.Delay(5000);

            // ACT Status Check
            // Make the payment status check
            var statusResponse = await client.CheckPaymentStatusAsync(id);

            // Assert Status Check
            Assert.Null(request.Error);
            Assert.NotNull(request.Response);
            Assert.Equal(id.ToString(), statusResponse.Response.Id);
            Assert.Equal(PaymentStatus.PAID, statusResponse.Response.Status);
            Assert.Equal(amount, statusResponse.Response.Amount);
            Assert.Equal(message, statusResponse.Response.Message);
            Assert.Equal(phoneNumber, statusResponse.Response.PayerAlias);
            Assert.Equal(SwishTestClient.PayeeAlias, statusResponse.Response.PayeeAlias);
            Assert.Equal(SwishTestClient.PayeePaymentReference, statusResponse.Response.PayeePaymentReference);
        }
        

        [Fact]
        public async Task Refund()
        {
            // Arrange
            var certificate = GetPrivateCertificate();
            ISwishClient client = new SwishTestClient(certificate.PrivateCertificate, certificate.CertificateChain, new Uri("https://tabetaltmedswish.se/Test/Callback/"), _logger);
            var id = Guid.NewGuid();
            decimal amount = 1.5m;

            // ACT Make Payment request
            var request = await client.MakePaymentRequestAsync(id, "46731596605", amount, "Test");


            // Assert Payment request
            Assert.Null(request.Error);
            Assert.NotNull(request.Response);
            Assert.EndsWith(id.ToString(), request.Response.Location.ToString());


            await Task.Delay(5000);

            // ACT Status Check
            // Make the payment status check
            var statusResponse = await client.CheckPaymentStatusAsync(id);

            // Assert Status Check
            Assert.Null(request.Error);
            Assert.NotNull(request.Response);
            Assert.Equal(id.ToString(), statusResponse.Response.Id);
            Assert.Equal(PaymentStatus.PAID, statusResponse.Response.Status);
            Assert.Equal(amount, statusResponse.Response.Amount);

            // Arrange Complete refund

            var refundId = Guid.NewGuid();

            // ACT Refund
            var refundRequest = await client.RefundAsync(id, refundId, amount, "Refund");

            // Arrange Refund
            Assert.Null(request.Error);
            Assert.NotNull(request.Response);
            Assert.EndsWith(refundId.ToString(), refundRequest.Response.Location.ToString());
            
            await Task.Delay(2000);

            // ACT Get Refund status
            var refundStatus = await client.CheckRefundStatusAsync(refundId);

            // Assert
            Assert.Null(request.Error);
            Assert.NotNull(request.Response);
            Assert.Equal(refundId.ToString(), refundStatus.Response.Id);
            Assert.Equal(RefundStatus.CREATED, refundStatus.Response.Status);
        }

        [Fact(Skip = "500 error from server")]
        public async Task CancelPayment()
        {
            // Arrange
            var certificate = GetPrivateCertificate();
            ISwishClient client = new SwishTestClient(certificate.PrivateCertificate, certificate.CertificateChain, new Uri("https://tabetaltmedswish.se/Test/Callback/"), _logger);
            var id = Guid.NewGuid();
            decimal amount = 1.5m;

            // ACT Make Payment request
            var request = await client.MakePaymentRequestAsync(id, "46731596605", amount, "Test");


            // Assert Payment request
            Assert.Null(request.Error);
            Assert.NotNull(request.Response);
            Assert.EndsWith(id.ToString(), request.Response.Location.ToString());


            // ACT Cancel payment
            // Make the payment status check
            var cancelPaymentAsync = await client.CancelPaymentAsync(id);

            // Assert Status Check
            Assert.Null(cancelPaymentAsync.Error);
            Assert.NotNull(cancelPaymentAsync.Response);
            Assert.Equal(id.ToString(), cancelPaymentAsync.Response.Id);
            Assert.Equal(PaymentStatus.PAID, cancelPaymentAsync.Response.Status);
            Assert.Equal(amount, cancelPaymentAsync.Response.Amount);
        }
    }
}
