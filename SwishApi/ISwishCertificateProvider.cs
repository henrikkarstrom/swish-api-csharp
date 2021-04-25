using System.Security.Cryptography.X509Certificates;

namespace SwishApi
{
    public interface ISwishCertificateProvider
    {
        (X509Certificate2 PrivateCertificate, X509Certificate2Collection CertificateChain) GetSwishCertificates();
    }
}
