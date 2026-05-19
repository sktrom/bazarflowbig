using System;
using System.Collections.Generic;

namespace Supermarket.Contracts.Reports
{
    public class ProfitSalesInvoiceDto
    {
        public long InvoiceId { get; set; }
        public string InvoiceNumber { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public decimal RevenueUsd { get; set; }
        public decimal KnownCostUsd { get; set; }
        public decimal ProfitUsd { get; set; }
        public decimal? MarginPercent { get; set; }
        public bool HasMissingCost { get; set; }
        public bool IsProfitComplete { get; set; }
        public decimal MissingCostQuantity { get; set; }
    }

    public class ProfitProductDto
    {
        public long ProductId { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public decimal QuantitySold { get; set; }
        public decimal RevenueUsd { get; set; }
        public decimal KnownCostUsd { get; set; }
        public decimal ProfitUsd { get; set; }
        public decimal? MarginPercent { get; set; }
        public bool HasMissingCost { get; set; }
        public bool IsProfitComplete { get; set; }
        public decimal MissingCostQuantity { get; set; }
    }

    public class InventoryValuationDto
    {
        public long ProductId { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public string CategoryName { get; set; } = string.Empty;
        public decimal TotalQuantityAvailable { get; set; }
        public decimal KnownCostQuantity { get; set; }
        public decimal MissingCostQuantity { get; set; }
        public decimal KnownStockValueUsd { get; set; }
        public bool HasMissingCost { get; set; }
        public bool IsValuationComplete { get; set; }
    }

    public class ProfitReportListResponse<T>
    {
        public List<T> Items { get; set; } = new();
    }
}
