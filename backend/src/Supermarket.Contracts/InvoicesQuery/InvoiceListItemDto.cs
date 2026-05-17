using System;

namespace Supermarket.Contracts.InvoicesQuery
{
    public class InvoiceListItemDto
    {
        public long InvoiceId { get; set; }
        public string InvoiceNumber { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string? CustomerName { get; set; }
        public long OriginalEmployeeId { get; set; }
        public string? EmployeeName { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? CompletedAt { get; set; }
        public decimal TotalUsd { get; set; }
        public decimal? TotalSyp { get; set; }
        public bool HasManualPriceEdit { get; set; }
        public bool HasAdjustmentRequest { get; set; }
        public string? AdjustmentRequestStatus { get; set; }
        public long? AdjustmentRequestId { get; set; }
        public string? SuspensionReason { get; set; }
    }
}
