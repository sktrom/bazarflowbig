using System;

namespace Supermarket.Contracts.Auth
{
    public class CurrentEmployeeResponse
    {
        public long EmployeeId { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string Username { get; set; } = string.Empty;
        public long SessionId { get; set; }
        public string DeviceCode { get; set; } = string.Empty;
        public DateTime SessionStartedAt { get; set; }
    }
}
