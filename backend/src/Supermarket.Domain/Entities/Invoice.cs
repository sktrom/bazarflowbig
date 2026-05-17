using System;
using Supermarket.Domain.Enums;

namespace Supermarket.Domain.Entities
{
    public class Invoice
    {
        public long Id { get; set; }
        public string InvoiceNumber { get; set; } = string.Empty;
        
        public long OriginalEmployeeId { get; set; }
        public Employee? OriginalEmployee { get; set; }
        
        public string? CustomerName { get; set; }
        
        public InvoiceStatus Status { get; set; }
        public InvoiceSuspensionReason? SuspensionReason { get; set; }
        
        public InvoiceDiscountType? InvoiceDiscountType { get; set; }
        public decimal? InvoiceDiscountValue { get; set; }
        
        public decimal SubtotalUsd { get; set; }
        public decimal TotalUsd { get; set; }
        
        public decimal? ExchangeRateSypSnapshot { get; set; }
        public decimal? TotalSyp { get; set; }
        
        public bool HasManualPriceEdit { get; set; }
        public bool HasAdjustmentRequest { get; set; }
        
        public DateTime CreatedAt { get; set; }
        public DateTime? CompletedAt { get; set; }
    }
}
