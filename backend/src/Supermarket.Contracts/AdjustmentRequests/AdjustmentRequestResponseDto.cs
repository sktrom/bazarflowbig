using System;
using System.Collections.Generic;

namespace Supermarket.Contracts.AdjustmentRequests
{
    public class AdjustmentRequestResponseDto
    {
        public long RequestId { get; set; }
        public long InvoiceId { get; set; }
        public string Status { get; set; } = string.Empty;
        public string RequestType { get; set; } = string.Empty;
        public string Reason { get; set; } = string.Empty;
        public long RequestedByEmployeeId { get; set; }
        public long? ReviewedByEmployeeId { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? ReviewedAt { get; set; }
        public List<AdjustmentRequestLineResponseDto> Lines { get; set; } = new();
    }

    public class AdjustmentRequestLineResponseDto
    {
        public long? InvoiceLineId { get; set; }
        public string ActionType { get; set; } = string.Empty;
        public decimal? RequestedQuantity { get; set; }
        public decimal? RequestedLineTotalUsd { get; set; }
    }
}
