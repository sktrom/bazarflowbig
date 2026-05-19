using System;
using System.Collections.Generic;

namespace Supermarket.Contracts.AuditLogs
{
    public class AuditLogListItem
    {
        public long Id { get; set; }
        public long? EmployeeId { get; set; }
        public string? EmployeeName { get; set; }
        public long? SessionId { get; set; }
        public string Action { get; set; } = string.Empty;
        public string EntityType { get; set; } = string.Empty;
        public string? EntityId { get; set; }
        public string? EntityDisplayName { get; set; }
        public DateTime CreatedAt { get; set; }
        public bool HasBefore { get; set; }
        public bool HasAfter { get; set; }
        public bool HasMetadata { get; set; }
    }

    public class AuditLogListResponse
    {
        public int TotalCount { get; set; }
        public int Page { get; set; }
        public int PageSize { get; set; }
        public List<AuditLogListItem> Items { get; set; } = new();
    }

    public class AuditLogDetailResponse
    {
        public long Id { get; set; }
        public long? EmployeeId { get; set; }
        public string? EmployeeName { get; set; }
        public long? SessionId { get; set; }
        public string Action { get; set; } = string.Empty;
        public string EntityType { get; set; } = string.Empty;
        public string? EntityId { get; set; }
        public string? EntityDisplayName { get; set; }
        public DateTime CreatedAt { get; set; }
        public bool HasBefore { get; set; }
        public bool HasAfter { get; set; }
        public bool HasMetadata { get; set; }
        public string? BeforeJson { get; set; }
        public string? AfterJson { get; set; }
        public string? MetadataJson { get; set; }
        public string? IpAddress { get; set; }
        public string? UserAgent { get; set; }
    }
}
