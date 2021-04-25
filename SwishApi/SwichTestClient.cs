using System;
using System.Runtime.CompilerServices;
using System.Security.Cryptography.X509Certificates;
using Microsoft.Extensions.Logging;

[assembly: InternalsVisibleTo("SwishApi.IntegrationTests")]
namespace SwishApi
{

    public sealed class SwishTestClient : SwishClient, ISwishClient
    {
        internal const string PayeePaymentReference = "01234679304";
        internal const string PayeeAlias = "1234679304";
        private static readonly Uri EndpointUri = new Uri("https://mss.cpc.getswish.net");

        public SwishTestClient(X509Certificate2 certificate, X509Certificate2Collection certificateCollection, Uri callbackUri, ILogger<SwishTestClient> logger) :
            base(new LocalCertificateProvider(certificate, certificateCollection), new LocalSettingsProvider(callbackUri, PayeeAlias, PayeePaymentReference), EndpointUri, logger)
        {
        }
    }

}