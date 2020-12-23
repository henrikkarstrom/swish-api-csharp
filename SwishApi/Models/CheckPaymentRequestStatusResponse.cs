using System;
using System.Text.Json.Serialization;

namespace SwishApi.Models
{
    public class CheckPaymentRequestStatusResponse
    {
        [JsonPropertyName("id")]
        public string Id { get; set; }

        [JsonPropertyName("payeePaymentReference")]
        public string PayeePaymentReference { get; set; }

        [JsonPropertyName("paymentReference")]
        public string PaymentReference { get; set; }

        [JsonPropertyName("callbackUrl")]
        public string CallbackUrl { get; set; }

        [JsonPropertyName("payerAlias")]
        public string PayerAlias { get; set; }

        [JsonPropertyName("payeeAlias")]
        public string PayeeAlias { get; set; }

        [JsonPropertyName("amount")]
        public decimal Amount { get; set; }

        [JsonPropertyName("currency")]
        public string Currency { get; set; }

        [JsonPropertyName("message")]
        public string Message { get; set; }

        [JsonPropertyName("status")]
        public PaymentStatus Status { get; set; }

        [JsonPropertyName("dateCreated")]
        public DateTime DateCreated { get; set; }

        [JsonPropertyName("datePaid")]
        public DateTime? DatePaid { get; set; }
    }
}
