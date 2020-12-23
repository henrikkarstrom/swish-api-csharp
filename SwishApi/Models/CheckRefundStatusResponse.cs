using System;
using System.Text.Json.Serialization;

namespace SwishApi.Models
{
    public class CheckRefundStatusResponse
    {

        [JsonPropertyName("id")]
        public string Id { get; set; }

        public string payerPaymentReference { get; set; }
        public string originalPaymentReference { get; set; }
        public string callbackUrl { get; set; }
        public string payerAlias { get; set; }
        public string payeeAlias { get; set; }

        [JsonPropertyName("amount")]
        public double Amount { get; set; }

        [JsonPropertyName("currency")]
        public string Currency { get; set; }

        [JsonPropertyName("message")]
        public string Message { get; set; }

        [JsonPropertyName("status")]
        public RefundStatus Status { get; set; }

        public DateTime dateCreated { get; set; }
        public DateTime? datePaid { get; set; }
    }
}
