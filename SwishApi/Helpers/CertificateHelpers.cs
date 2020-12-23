using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Security.Cryptography.X509Certificates;
using System.Text;

namespace SwishApi.Helpers
{
    public static class CertificateHelpers
    {
        public static (X509Certificate2 PrivateCertificate, X509Certificate2Collection CertificateChain) GetCertificate(string certificatePath, string certificatePassword)
        {
            var certs = new X509Certificate2Collection();
            certs.Import(certificatePath, certificatePassword, X509KeyStorageFlags.DefaultKeySet);
            X509Certificate2 privateCertificate = null;
            X509Certificate2Collection certificateCollection = new X509Certificate2Collection();
            foreach (X509Certificate2 cert in certs)
            {
                if (cert.HasPrivateKey)
                {
                    privateCertificate = cert;
                }
                else
                {
                    certificateCollection.Add(cert);
                }
            }

            return (privateCertificate, certificateCollection);
        }
    }
}
