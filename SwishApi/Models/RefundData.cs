using System.Text.Json.Serialization;

namespace SwishApi.Models
{
    public class RefundData
    {
        [JsonPropertyName("originalPaymentReference")]
        public string OriginalPaymentReference { get; set; }

        [JsonPropertyName("callbackUrl")]
        public string CallbackUrl { get; set; }

        [JsonPropertyName("payerAlias")]
        public string PayerAlias { get; set; }

        [JsonPropertyName("amount")]
        public string Amount { get; set; }

        [JsonPropertyName("currency")]
        public string Currency { get; set; }

        [JsonPropertyName("message")]
        public string Message { get; set; }
    }
}
