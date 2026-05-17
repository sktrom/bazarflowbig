using System;

namespace Supermarket.Contracts.InvoicesQuery
{
    public class InvoiceSummaryResponse
    {
        public long InvoiceId { get; set; }
        public string InvoiceNumber { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string? CustomerName { get; set; }
        public long OriginalEmployeeId { get; set; }
        public string? EmployeeName { get; set; }
        public string? InvoiceDiscountType { get; set; }
        public decimal? InvoiceDiscountValue { get; set; }
        public decimal SubtotalUsd { get; set; }
        public decimal TotalUsd { get; set; }
        public decimal? ExchangeRateSypSnapshot { get; set; }
        public decimal? TotalSyp { get; set; }
        public bool HasManualPriceEdit { get; set; }
        public bool HasAdjustmentRequest { get; set; }
        public string? AdjustmentRequestStatus { get; set; }
        public long? AdjustmentRequestId { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? CompletedAt { get; set; }
        public string? SuspensionReason { get; set; }
    }
}
