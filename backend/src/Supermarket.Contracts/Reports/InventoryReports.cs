using System;
using System.Collections.Generic;

namespace Supermarket.Contracts.Reports
{
    public class InventorySummaryReportDto
    {
        public long ProductId { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public string CategoryName { get; set; } = string.Empty;
        public decimal TotalQuantityAvailable { get; set; }
        public decimal TotalStockValueUsd { get; set; }
        public string StockStatus { get; set; } = string.Empty;
    }

    public class InventoryBatchReportDto
    {
        public long BatchId { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public decimal QuantityReceived { get; set; }
        public decimal QuantityAvailable { get; set; }
        public DateTime? EntryDate { get; set; }
        public string EntryInvoiceNumber { get; set; } = string.Empty;
    }

    public class InventoryChartDto
    {
        public string CategoryName { get; set; } = string.Empty;
        public decimal TotalStockValueUsd { get; set; }
    }

    public class InventoryReportListResponse<T>
    {
        public List<T> Items { get; set; } = new();
    }
}
