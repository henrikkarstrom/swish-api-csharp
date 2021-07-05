// -------------------------------------------------------------------------------------------------
// Copyright (c) Julius Biljettservice AB. All rights reserved.
// -------------------------------------------------------------------------------------------------

using SwishApi;
using System;
using System.Security.Cryptography.X509Certificates;

namespace SwishAPI.Tests.Helpers
{
    public class TestCertificateProvider : ISwishCertificateProvider
    {
        private string _certificatePath = Environment.CurrentDirectory + "\\Certificates\\Swish_Merchant_TestCertificate_1234679304.p12";
        private string _rootCertificatePath = Environment.CurrentDirectory + "\\Certificates\\Swish_TLS_RootCA.crt";
        private string _password = "swish";
        private X509Certificate2Collection _certificates;

        public TestCertificateProvider()
        {
            _certificates = new X509Certificate2Collection();
            _certificates.Import(_certificatePath, _password, X509KeyStorageFlags.DefaultKeySet);
            _certificates.Import(_rootCertificatePath, _password, X509KeyStorageFlags.DefaultKeySet);

            using (X509Store store = new X509Store(StoreName.CertificateAuthority, StoreLocation.CurrentUser))
            {
                store.Open(OpenFlags.ReadWrite);

                foreach (X509Certificate2 cert in _certificates)
                {
                    if (!cert.HasPrivateKey)
                    {
                        store.Add(cert);
                    }
                }
            }
        }

        public X509Certificate2Collection GetSwishCertificates()
        {
            return _certificates;
        }
    }
}