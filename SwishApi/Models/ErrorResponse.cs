using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace SwishApi.Models
{
    public class ErrorResponse
    {
        public List<Error> Errors { get; } = new List<Error>();
    }

    public class Error
    {
        [JsonPropertyName("errorCode")]
        public string ErrorCode { get; set; }

        [JsonPropertyName("errorMessage")]
        public string ErrorMessage { get; set; }

        [JsonPropertyName("additionalInformation")]
        public string AdditionalInformation { get; set; }

        public Exception Exception { get; set; }
    }
}