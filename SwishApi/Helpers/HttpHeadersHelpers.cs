using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Net.Http.Headers;
using System.Text;

namespace SwishApi.Helpers
{
    public static class HttpHeadersHelpers
    {
        public static string GetPaymentRequestToken(this HttpResponseHeaders headers)
        {
            if (headers.Any(x => x.Key == "PaymentRequestToken"))
            {
                return headers.GetValues("PaymentRequestToken").FirstOrDefault();
            }

            return null;
        }
    }

    public static class DoubleHelpers
    {
        private static readonly NumberFormatInfo FormatInfo = new NumberFormatInfo() { NumberDecimalSeparator = "." };
        public static string ToSwishAmount(this decimal value)
        {
            return value.ToString("##.##", FormatInfo);
        }
    }

    public static class GuidHelpers
    {
        public static string ToSwishId(this Guid value)
        {
            return value.ToString().ToUpperInvariant().Replace("-", "");
        }
    }
}
