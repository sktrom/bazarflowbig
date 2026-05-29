namespace Supermarket.Application.Auth.Interfaces
{
    public interface ILoginThrottleService
    {
        Task<bool> IsBlockedAsync(string username, string ipAddress);
        Task RecordFailedAttemptAsync(string username, string ipAddress, string userAgent, string reason);
        Task RecordBlockedAttemptAsync(string username, string ipAddress, string userAgent);
        Task ResetAsync(string username, string ipAddress);
    }
}
