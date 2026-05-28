using System;
using System.Collections.Generic;

namespace Supermarket.Contracts.BlackBox
{
    public class CreateBlackBoxEventRequest
    {
        public string? Route { get; set; }
        public string? PageName { get; set; }
        public string ActionType { get; set; } = string.Empty;
        public string? ElementKey { get; set; }
        public string? EntityType { get; set; }
        public string? EntityId { get; set; }
        public string Result { get; set; } = string.Empty;
        public string? Message { get; set; }
        public Dictionary<string, object?>? Metadata { get; set; }
    }

    public class CreateBlackBoxEventResponse
    {
        public bool Success { get; set; }
        public long Id { get; set; }
    }

    public class BlackBoxEventQuery
    {
        public DateTime? DateFrom { get; set; }
        public DateTime? DateTo { get; set; }
        public long? EmployeeId { get; set; }
        public string? DeviceCode { get; set; }
        public string? ActionType { get; set; }
        public string? PageName { get; set; }
        public string? Result { get; set; }
        public string? EntityType { get; set; }
        public string? EntityId { get; set; }
        public string? Route { get; set; }
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 50;
    }

    public class BlackBoxEventListItem
    {
        public long Id { get; set; }
        public long? EmployeeId { get; set; }
        public string? EmployeeName { get; set; }
        public long? SessionId { get; set; }
        public string? DeviceCode { get; set; }
        public string? Route { get; set; }
        public string? PageName { get; set; }
        public string ActionType { get; set; } = string.Empty;
        public string? ElementKey { get; set; }
        public string? EntityType { get; set; }
        public string? EntityId { get; set; }
        public string Result { get; set; } = string.Empty;
        public string? Message { get; set; }
        public bool HasMetadata { get; set; }
        public bool MetadataTruncated { get; set; }
        public DateTime CreatedAtUtc { get; set; }
    }

    public class BlackBoxEventListResponse
    {
        public int TotalCount { get; set; }
        public int Page { get; set; }
        public int PageSize { get; set; }
        public List<BlackBoxEventListItem> Items { get; set; } = new();
    }

    public class BlackBoxEventDetailResponse : BlackBoxEventListItem
    {
        public string? MetadataJson { get; set; }
        public string? IpAddress { get; set; }
        public string? UserAgent { get; set; }
    }
}
