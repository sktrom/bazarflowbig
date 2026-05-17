namespace Supermarket.Application.Common.Interfaces
{
    public interface ISessionContext
    {
        long EmployeeId { get; }
        long SessionId { get; }
        string DeviceCode { get; }
        bool IsAuthenticated { get; }
    }
}
