using System;

namespace SwishApi.Models
{
    internal class PaymentRequest
    {
        public string payeePaymentReference { get; set; }
        public Uri callbackUrl { get; set; }
        public string payerAlias { get; set; }
        public string payeeAlias { get; set; }
        public string amount { get; set; }
        public string currency { get; set; }
        public string message { get; set; }
    }
}
