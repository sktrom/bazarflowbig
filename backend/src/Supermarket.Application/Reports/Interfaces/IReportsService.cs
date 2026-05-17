using System;
using System.Threading.Tasks;
using Supermarket.Contracts.Reports;

namespace Supermarket.Application.Reports.Interfaces
{
    public interface IReportsService
    {
        // Sales
        Task<SalesReportListResponse<SalesInvoiceReportDto>> GetSalesInvoicesAsync(DateTime? dateFrom, DateTime? dateTo, string? status);
        Task<SalesReportListResponse<SalesItemReportDto>> GetSalesItemsAsync(DateTime? dateFrom, DateTime? dateTo);
        Task<SalesReportListResponse<SalesChartDto>> GetSalesChartsAsync(DateTime? dateFrom, DateTime? dateTo);

        // Products
        Task<ProductsReportListResponse<ProductSummaryReportDto>> GetProductsSummaryAsync(long? categoryId);
        Task<ProductsReportListResponse<ProductMovementReportDto>> GetProductsMovementsAsync(DateTime? dateFrom, DateTime? dateTo, long? productId);
        Task<ProductsReportListResponse<ProductChartDto>> GetProductsChartsAsync(DateTime? dateFrom, DateTime? dateTo);

        // Employees
        Task<EmployeesReportListResponse<EmployeeSummaryReportDto>> GetEmployeesSummaryAsync(DateTime? dateFrom, DateTime? dateTo);
        Task<EmployeesReportListResponse<EmployeeActivityReportDto>> GetEmployeesActivityAsync(DateTime? dateFrom, DateTime? dateTo, long? employeeId);
        Task<EmployeesReportListResponse<EmployeeChartDto>> GetEmployeesChartsAsync(DateTime? dateFrom, DateTime? dateTo);

        // Inventory
        Task<InventoryReportListResponse<InventorySummaryReportDto>> GetInventorySummaryAsync(long? categoryId);
        Task<InventoryReportListResponse<InventoryBatchReportDto>> GetInventoryBatchesAsync(DateTime? dateFrom, DateTime? dateTo);
        Task<InventoryReportListResponse<InventoryChartDto>> GetInventoryChartsAsync();

        // Expiry
        Task<ExpiryReportListResponse<ExpirySummaryReportDto>> GetExpirySummaryAsync();
        Task<ExpiryReportListResponse<ExpiryBatchReportDto>> GetExpiryBatchesAsync();
        Task<ExpiryReportListResponse<ExpiryChartDto>> GetExpiryChartsAsync();
    }
}
