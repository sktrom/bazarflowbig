using System;

namespace Supermarket.Domain.Entities
{
    public class AuditLog
    {
        public long Id { get; set; }
        public long? EmployeeId { get; set; }
        public Employee? Employee { get; set; }
        public long? SessionId { get; set; }
        public string Action { get; set; } = string.Empty;
        public string EntityType { get; set; } = string.Empty;
        public string? EntityId { get; set; }
        public string? EntityDisplayName { get; set; }
        public string? BeforeJson { get; set; }
        public string? AfterJson { get; set; }
        public string? MetadataJson { get; set; }
        public string? IpAddress { get; set; }
        public string? UserAgent { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
