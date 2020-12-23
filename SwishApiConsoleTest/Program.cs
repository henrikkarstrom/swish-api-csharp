using System;
using System.IO;
using System.Net.Http;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging.Abstractions;
using SwishApi;
using SwishApi.Helpers;
using SwishApi.Models;

namespace SwishApiConsoleTest
{
    class Program
    {
        // MainTestPaymentAndRefund
        public static async Task Main(string[] args)
        {
            // Get the path for the test certificate in the TestCert folder in the console application folder, being always copy to the output folder
            string certificatePath = Environment.CurrentDirectory + "\\TestCert\\Swish_Merchant_TestCertificate_1234679304.p12";
            var certificate = CertificateHelpers.GetCertificate(certificatePath, "swish");
            // Create a Swishpi Client object with all data needed to run a test Swish payment
            ISwishClient client = new SwichTestClient(certificate.PrivateCertificate, certificate.CertificateChain,  new Uri("https://tabetaltmedswish.se/Test/Callback/"), new NullLogger<SwichTestClient>());

            // Make the Payement Request
            var response = await client.MakePaymentRequestAsync("46731596605", 1, "Test");

            // Check if the payment request got success and not got any error
            if (string.IsNullOrEmpty(response.Error))
            {
                // All OK
                var urlForCheckingPaymentStatus = response.Location;

                // If you do a webbapplication you here should wait some time, showing a "loading" view or something and try to do the payment status check as below, you maybe have some ajax request doing a call to a actionresult doing this code
                // Wait so that the payment request has been processed
                System.Threading.Thread.Sleep(5000);

                // Make the payment status check
                var statusResponse = await client.CheckPaymentStatusAsync(urlForCheckingPaymentStatus);

                // Check if the call is done correct
                if (string.IsNullOrEmpty(statusResponse.errorCode))
                {
                    // Call was maked without any problem
                    Console.WriteLine("Status: " + statusResponse.Status);

                    if (statusResponse.Status == PaymentStatus.PAID)
                    {
                        // "8FFBC84A91CD49A799176B1419AAE598"
                        var refundResponse = await client.RefundAsync(statusResponse.PaymentReference, statusResponse.Amount, "Återköp", "https://tabetaltmedswish.se/Test/RefundCallback/");

                        if (string.IsNullOrEmpty(refundResponse.Error))
                        {
                            // Request OK
                            Uri urlForCheckingRefundStatus = refundResponse.Location;

                            // If you do a webbapplication you here should wait some time, showing a "loading" view or something and try to do the refund status check as below, you maybe have some ajax request doing a call to a actionresult doing this code
                            // Wait so that the refund has been processed
                            System.Threading.Thread.Sleep(5000);

                            // Check refund status
                            var refundCheckResposne = await client.CheckRefundStatusAsync(urlForCheckingRefundStatus);

                            if (string.IsNullOrEmpty(refundCheckResposne.errorCode))
                            {
                                // Call was maked without any problem
                                Console.WriteLine("RefundChecKResponse - Status: " + statusResponse.Status);
                            }
                            else
                            {
                                // ERROR
                                Console.WriteLine("RefundCheckResponse: " + refundCheckResposne.errorCode + " - " + refundCheckResposne.errorMessage);
                            }
                        }
                        else
                        {
                            // ERROR
                            Console.WriteLine("Refund Error: " + refundResponse.Error);
                        }
                    }
                }
                else
                {
                    // ERROR
                    Console.WriteLine("CheckPaymentResponse: " + statusResponse.errorCode + " - " + statusResponse.errorMessage);
                }
            }
            else
            {
                // ERROR
                Console.WriteLine("MakePaymentRequest - ERROR: " + response.Error);
            }


            Console.WriteLine(">>> Press enter to exit <<<");
            Console.ReadLine();
        }
    }
}
