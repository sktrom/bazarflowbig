using System;

namespace Supermarket.Contracts.Settings
{
    public record PublicSettingsResponse(
        string StoreName,
        decimal ExchangeRate
    );
}
