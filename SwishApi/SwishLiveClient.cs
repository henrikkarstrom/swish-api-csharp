using System;
using System.Security.Cryptography.X509Certificates;
using Microsoft.Extensions.Logging;

namespace SwishApi
{
    public sealed class SwishLiveClient : SwishClient, ISwishClient
    {
        private static readonly Uri EndpointUri = new Uri("https://cpc.getswish.net");

        public SwishLiveClient(X509Certificate2 certificate, X509Certificate2Collection certificateCollection, Uri callbackUri, string payeeAlias, string payeePaymentReference, ILogger<SwishLiveClient> logger) : 
            base(certificate, certificateCollection, callbackUri, payeeAlias, payeePaymentReference, EndpointUri, logger)
        {
        }
    }
}