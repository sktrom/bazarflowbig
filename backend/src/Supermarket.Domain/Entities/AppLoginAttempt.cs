using System;

namespace Supermarket.Domain.Entities
{
    public class AppLoginAttempt
    {
        public long Id { get; set; }
        public string UsernameNormalized { get; set; } = string.Empty;
        public string IpAddress { get; set; } = string.Empty;
        public string? UserAgent { get; set; }
        public string Result { get; set; } = string.Empty; // "Failed", "Blocked"
        public string? FailureReason { get; set; }
        public DateTime CreatedAtUtc { get; set; }
    }
}
