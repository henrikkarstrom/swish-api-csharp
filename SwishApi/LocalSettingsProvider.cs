using System;
using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("SwishApi.IntegrationTests")]
namespace SwishApi
{
    internal class LocalSettingsProvider : ISwishSettingsProvider
    {
        public LocalSettingsProvider(Uri callbackUri, string payeeAlias, string payeePaymentReference)
        {
            CallbackUri = callbackUri;
            PayeeAlias = payeeAlias;
            PayeePaymentReference = payeePaymentReference;
        }

        public Uri CallbackUri { get; }

        public string PayeeAlias { get; }

        public string PayeePaymentReference { get; }
    }

}