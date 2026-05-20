namespace Supermarket.Application.Auth.Interfaces
{
    public interface ILoginThrottleService
    {
        bool IsThrottled(string username, string deviceCode);
        void RecordFailedAttempt(string username, string deviceCode);
        void Reset(string username, string deviceCode);
    }
}
