using System;
using System.Collections.Generic;

namespace Supermarket.Contracts.Reports
{
    public class EmployeeSummaryReportDto
    {
        public long EmployeeId { get; set; }
        public string EmployeeName { get; set; } = string.Empty;
        public int TotalInvoicesHandled { get; set; }
        public decimal TotalSalesRevenueUsd { get; set; }
    }

    public class EmployeeActivityReportDto
    {
        public long EmployeeId { get; set; }
        public string EmployeeName { get; set; } = string.Empty;
        public DateTime ActivityDate { get; set; }
        public string ActivityType { get; set; } = string.Empty; // "SessionOpened", "SessionClosed", "InvoiceCompleted"
        public string Details { get; set; } = string.Empty;
    }

    public class EmployeeChartDto
    {
        public string EmployeeName { get; set; } = string.Empty;
        public decimal TotalSalesRevenueUsd { get; set; }
    }

    public class EmployeesReportListResponse<T>
    {
        public List<T> Items { get; set; } = new();
    }
}
