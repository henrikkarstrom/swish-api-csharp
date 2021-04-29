using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace SwishApi.Models
{
    public class ErrorResponse 
    {

        public ErrorResponse(params Error[] errors)
        {
            Errors = new List<Error>(errors);
        }
        public IReadOnlyList<Error> Errors { get; }
    }

    public class Error
    {
        [JsonPropertyName("errorCode")]
        public string ErrorCode { get; internal set; }

        [JsonPropertyName("errorMessage")]
        public string ErrorMessage { get; internal set; }

        [JsonPropertyName("additionalInformation")]
        public string AdditionalInformation { get; internal set; }

        public Exception Exception { get; internal set; }
    }
}