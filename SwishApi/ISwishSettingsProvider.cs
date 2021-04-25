using System;

namespace SwishApi
{
    public interface ISwishSettingsProvider
    {
        Uri CallbackUri { get; }
        string PayeeAlias { get; }
        string PayeePaymentReference { get; }
    }
}
