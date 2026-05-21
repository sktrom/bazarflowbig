using System;

namespace Supermarket.Contracts.Sessions
{
    public class ActiveSessionResponse
    {
        public long SessionId { get; set; }
        public long EmployeeId { get; set; }
        public string EmployeeName { get; set; } = string.Empty;
        public string Username { get; set; } = string.Empty;
        public long DeviceId { get; set; }
        public string DeviceCode { get; set; } = string.Empty;
        public string DeviceName { get; set; } = string.Empty;
        public DateTime StartedAt { get; set; }
        public DateTime? LastSeenAt { get; set; }
        public DateTime? ExpiresAt { get; set; }
    }
}
