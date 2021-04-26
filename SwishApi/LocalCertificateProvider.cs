using System.Runtime.CompilerServices;
using System.Security.Cryptography.X509Certificates;

[assembly: InternalsVisibleTo("SwishApi.IntegrationTests")]
namespace SwishApi
{
    internal class LocalCertificateProvider : ISwishCertificateProvider
    {
        private readonly X509Certificate2 _certificate;
        private readonly X509Certificate2Collection _certificateCollection;

        internal LocalCertificateProvider(X509Certificate2 certificate, X509Certificate2Collection certificateCollection)
        {
            _certificate = certificate;
            _certificateCollection = certificateCollection;
        }

        public X509Certificate2Collection GetSwishCertificates()
        {
            return _certificateCollection;
        }
    }

}