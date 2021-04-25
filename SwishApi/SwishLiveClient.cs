using System;
using System.Security.Cryptography.X509Certificates;
using Microsoft.Extensions.Logging;

namespace SwishApi
{
    public sealed class SwishLiveClient : SwishClient, ISwishClient
    {
        private static readonly Uri EndpointUri = new Uri("https://cpc.getswish.net");

        public SwishLiveClient(ISwishCertificateProvider swishCertificateProvider, ISwishSettingsProvider settingsProvider, ILogger<SwishLiveClient> logger) : 
            base(swishCertificateProvider, settingsProvider, EndpointUri, logger)
        {
        }
    }
}