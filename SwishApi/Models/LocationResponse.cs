using System;

namespace SwishApi.Models
{
    /// <summary>
    /// Response object from a Swish for Merchant Payment Request
    /// </summary>
    public class LocationResponse
    {
        public Uri Location { get; set; }
    }
}
