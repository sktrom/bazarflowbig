using System;

namespace Supermarket.Domain.Entities
{
    public class BlackBoxEvent
    {
        public long Id { get; set; }
        public long? EmployeeId { get; set; }
        public Employee? Employee { get; set; }
        public long? SessionId { get; set; }
        public CashSession? Session { get; set; }
        public string? DeviceCode { get; set; }
        public string? Route { get; set; }
        public string? PageName { get; set; }
        public string ActionType { get; set; } = string.Empty;
        public string? ElementKey { get; set; }
        public string? EntityType { get; set; }
        public string? EntityId { get; set; }
        public string Result { get; set; } = string.Empty;
        public string? Message { get; set; }
        public string? MetadataJson { get; set; }
        public bool MetadataTruncated { get; set; }
        public string? IpAddress { get; set; }
        public string? UserAgent { get; set; }
        public DateTime CreatedAtUtc { get; set; }
    }
}
