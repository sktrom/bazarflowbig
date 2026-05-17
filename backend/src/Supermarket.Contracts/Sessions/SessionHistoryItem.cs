using System;

namespace Supermarket.Contracts.Sessions
{
    public class SessionHistoryItem
    {
        public long SessionId { get; set; }
        public string DeviceCode { get; set; } = string.Empty;
        public DateTime StartedAt { get; set; }
        public DateTime? EndedAt { get; set; }
        public string Status { get; set; } = string.Empty;
    }
}
