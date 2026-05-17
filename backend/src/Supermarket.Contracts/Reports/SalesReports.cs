using System;
using System.Collections.Generic;

namespace Supermarket.Contracts.Reports
{
    public class SalesInvoiceReportDto
    {
        public long InvoiceId { get; set; }
        public string InvoiceNumber { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public string Status { get; set; } = string.Empty;
        public decimal TotalUsd { get; set; }
        public decimal? TotalSyp { get; set; }
        public string EmployeeName { get; set; } = string.Empty;
    }

    public class SalesItemReportDto
    {
        public long ProductId { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public decimal TotalQuantitySold { get; set; }
        public decimal TotalRevenueUsd { get; set; }
    }

    public class SalesChartDto
    {
        public string DateLabel { get; set; } = string.Empty;
        public decimal RevenueUsd { get; set; }
    }

    public class SalesReportListResponse<T>
    {
        public List<T> Items { get; set; } = new();
    }
}
