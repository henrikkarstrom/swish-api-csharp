using System;

namespace SwishApi
{
    public interface ISwishSettingsProvider
    {
        Uri CallbackUri { get; }
    }
}
