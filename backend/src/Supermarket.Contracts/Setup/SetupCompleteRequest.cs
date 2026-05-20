namespace Supermarket.Contracts.Setup
{
    public record SetupCompleteRequest(
        string AdminFullName,
        string AdminUsername,
        string AdminPassword,
        string StoreName,
        decimal ExchangeRate,
        string DeviceCode,
        string DeviceName
    );
}
