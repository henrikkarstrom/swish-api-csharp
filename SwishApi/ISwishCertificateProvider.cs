using System.Security.Cryptography.X509Certificates;

namespace SwishApi
{
    public interface ISwishCertificateProvider
    {
        X509Certificate2Collection GetSwishCertificates();
    }
}
