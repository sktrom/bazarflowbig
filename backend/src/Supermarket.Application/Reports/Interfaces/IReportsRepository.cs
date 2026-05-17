using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Supermarket.Contracts.Reports;

namespace Supermarket.Application.Reports.Interfaces
{
    public interface IReportsRepository
    {
        // Sales
        Task<List<SalesInvoiceReportDto>> GetSalesInvoicesAsync(DateTime? dateFrom, DateTime? dateTo, string? status);
        Task<List<SalesItemReportDto>> GetSalesItemsAsync(DateTime? dateFrom, DateTime? dateTo);
        Task<List<SalesChartDto>> GetSalesChartsAsync(DateTime? dateFrom, DateTime? dateTo);

        // Products
        Task<List<ProductSummaryReportDto>> GetProductsSummaryAsync(long? categoryId);
        Task<List<ProductMovementReportDto>> GetProductsMovementsAsync(DateTime? dateFrom, DateTime? dateTo, long? productId);
        Task<List<ProductChartDto>> GetProductsChartsAsync(DateTime? dateFrom, DateTime? dateTo);

        // Employees
        Task<List<EmployeeSummaryReportDto>> GetEmployeesSummaryAsync(DateTime? dateFrom, DateTime? dateTo);
        Task<List<EmployeeActivityReportDto>> GetEmployeesActivityAsync(DateTime? dateFrom, DateTime? dateTo, long? employeeId);
        Task<List<EmployeeChartDto>> GetEmployeesChartsAsync(DateTime? dateFrom, DateTime? dateTo);

        // Inventory
        Task<List<InventorySummaryReportDto>> GetInventorySummaryAsync(long? categoryId, decimal stockAlertThreshold);
        Task<List<InventoryBatchReportDto>> GetInventoryBatchesAsync(DateTime? dateFrom, DateTime? dateTo);
        Task<List<InventoryChartDto>> GetInventoryChartsAsync();

        // Expiry
        Task<List<ExpirySummaryReportDto>> GetExpirySummaryAsync(decimal expiryAlertDays);
        Task<List<ExpiryBatchReportDto>> GetExpiryBatchesAsync(decimal expiryAlertDays);
        Task<List<ExpiryChartDto>> GetExpiryChartsAsync(decimal expiryAlertDays);
    }
}
