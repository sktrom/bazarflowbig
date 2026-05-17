using System;
using System.Collections.Generic;

namespace Supermarket.Contracts.Reports
{
    public class ProductSummaryReportDto
    {
        public long ProductId { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public string CategoryName { get; set; } = string.Empty;
        public decimal TotalStockQuantity { get; set; }
        public decimal TotalStockValueUsd { get; set; }
        public bool IsActive { get; set; }
    }

    public class ProductMovementReportDto
    {
        public long ProductId { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public DateTime MovementDate { get; set; }
        public string MovementType { get; set; } = string.Empty; // "Sale"
        public decimal Quantity { get; set; }
        public string ReferenceNumber { get; set; } = string.Empty; // InvoiceNumber
    }

    public class ProductChartDto
    {
        public string ProductName { get; set; } = string.Empty;
        public decimal TotalSalesRevenueUsd { get; set; }
    }

    public class ProductsReportListResponse<T>
    {
        public List<T> Items { get; set; } = new();
    }
}
