using System;
using System.Collections.Generic;

namespace Supermarket.Contracts.Reports
{
    public class ExpirySummaryReportDto
    {
        public long ProductId { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public int ExpiredBatchesCount { get; set; }
        public int ExpiringSoonBatchesCount { get; set; }
        public decimal TotalExpiredValueUsd { get; set; }
    }

    public class ExpiryBatchReportDto
    {
        public long BatchId { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public decimal QuantityAvailable { get; set; }
        public DateTime? ExpiryDate { get; set; }
        public string ExpiryStatus { get; set; } = string.Empty;
        public int? DaysUntilExpiry { get; set; }
    }

    public class ExpiryChartDto
    {
        public string ExpiryStatus { get; set; } = string.Empty; // "Expired", "ExpiringSoon", "Fresh"
        public int BatchCount { get; set; }
    }

    public class ExpiryReportListResponse<T>
    {
        public List<T> Items { get; set; } = new();
    }
}
