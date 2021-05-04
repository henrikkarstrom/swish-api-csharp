// -------------------------------------------------------------------------------------------------
// Copyright (c) Julius Biljettservice AB. All rights reserved.
// -------------------------------------------------------------------------------------------------

using System;
using System.Threading.Tasks;
using SwishApi;
using Xunit;
using Xunit.Abstractions;

namespace SwishAPI.Tests
{
    public class UnitTest1
    {
        private ITestOutputHelper _testOutputWriter;
        private TestLogger<SwishClient> _logger;
        private Random _random;
        private const string PayeeAlias = "1234679304";

        public UnitTest1(ITestOutputHelper testOutputHelper)
        {
            _testOutputWriter = testOutputHelper;
            _logger = new TestLogger<SwishClient>(testOutputHelper);
            _random = new Random();
        }

        [Fact]
        public void CertificateLoadTest()
        {
            // Assert
            var callbackUri = new Uri("https://tabetaltmedswish.se/Test/Callback/");
            var certificateProvider = new TestCertificateProvider();

            var sut = new SwishApi.SwishClient(certificateProvider, callbackUri, _logger)
            {
                UseMSS = true,
            };

            // Act
            Assert.Equal(PayeeAlias, sut.PayeeAlias);
        }

        [Theory]
        [InlineData("Chokolad!", false)]
        [InlineData("RF07", true)]
        public async Task MakePaymentTest(string message, bool expectedError)
        {
            // Assert
            var callbackUri = new Uri("https://tabetaltmedswish.se/Test/Callback/");
            var certificateProvider = new TestCertificateProvider();
            var paymentId = Guid.NewGuid();
            var amount = (decimal)_random.Next(1, 1000);
            var phoneNumber = "46701740605"; // See: https://www.pts.se/sv/bransch/telefoni/nummer-och-adressering/telefonnummer-for-anvandning-i-bocker-och-filmer-etc/

            var sut = new SwishClient(certificateProvider, callbackUri, _logger)
            {
                UseMSS = true,
            };

            // Act Payment Request
            var response = await sut.PaymentRequestAsync(paymentId, phoneNumber, amount, message, "1001");

            // Assert
            Assert.Null(response.Error);
            Assert.NotNull(response.Response);

            // Act Check Status
            var status = await sut.CheckPaymentStatusAsync(paymentId);

            // Assert Check Status
            while (status.Response.Status == SwishApi.Models.PaymentStatus.CREATED)
            {
                Assert.Null(status.Error);
                Assert.NotNull(status.Response);

                Assert.Equal(amount, status.Response.Amount);
                Assert.Equal(phoneNumber, status.Response.PayerAlias);
                Assert.Equal(PayeeAlias, status.Response.PayeeAlias);
                Assert.Equal(message, status.Response.Message);
                Assert.Equal("SEK", status.Response.Currency);
                Assert.Null(status.Response.DatePaid);

                Assert.Equal(SwishApi.Models.PaymentStatus.CREATED, status.Response.Status);

                await Task.Delay(500);
                status = await sut.CheckPaymentStatusAsync(paymentId);
            }

            if (expectedError)
            {
                Assert.Equal(SwishApi.Models.PaymentStatus.ERROR, status.Response.Status);
                Assert.Null(status.Response.DatePaid);
            }
            else
            {
                Assert.Equal(SwishApi.Models.PaymentStatus.PAID, status.Response.Status);
                Assert.NotNull(status.Response.DatePaid);
            }
        }
    }
}
