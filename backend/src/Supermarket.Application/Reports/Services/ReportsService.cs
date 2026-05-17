using System;
using System.Threading.Tasks;
using Supermarket.Application.CartFinalization.Interfaces;
using Supermarket.Application.Reports.Interfaces;
using Supermarket.Contracts.Reports;

namespace Supermarket.Application.Reports.Services
{
    public class ReportsService : IReportsService
    {
        private readonly IReportsRepository _repository;
        private readonly IAppSettingsRepository _appSettingsRepository;

        public ReportsService(IReportsRepository repository, IAppSettingsRepository appSettingsRepository)
        {
            _repository = repository;
            _appSettingsRepository = appSettingsRepository;
        }

        public async Task<SalesReportListResponse<SalesInvoiceReportDto>> GetSalesInvoicesAsync(DateTime? dateFrom, DateTime? dateTo, string? status)
        {
            var items = await _repository.GetSalesInvoicesAsync(dateFrom, dateTo, status);
            return new SalesReportListResponse<SalesInvoiceReportDto> { Items = items };
        }

        public async Task<SalesReportListResponse<SalesItemReportDto>> GetSalesItemsAsync(DateTime? dateFrom, DateTime? dateTo)
        {
            var items = await _repository.GetSalesItemsAsync(dateFrom, dateTo);
            return new SalesReportListResponse<SalesItemReportDto> { Items = items };
        }

        public async Task<SalesReportListResponse<SalesChartDto>> GetSalesChartsAsync(DateTime? dateFrom, DateTime? dateTo)
        {
            var items = await _repository.GetSalesChartsAsync(dateFrom, dateTo);
            return new SalesReportListResponse<SalesChartDto> { Items = items };
        }

        public async Task<ProductsReportListResponse<ProductSummaryReportDto>> GetProductsSummaryAsync(long? categoryId)
        {
            var items = await _repository.GetProductsSummaryAsync(categoryId);
            return new ProductsReportListResponse<ProductSummaryReportDto> { Items = items };
        }

        public async Task<ProductsReportListResponse<ProductMovementReportDto>> GetProductsMovementsAsync(DateTime? dateFrom, DateTime? dateTo, long? productId)
        {
            var items = await _repository.GetProductsMovementsAsync(dateFrom, dateTo, productId);
            return new ProductsReportListResponse<ProductMovementReportDto> { Items = items };
        }

        public async Task<ProductsReportListResponse<ProductChartDto>> GetProductsChartsAsync(DateTime? dateFrom, DateTime? dateTo)
        {
            var items = await _repository.GetProductsChartsAsync(dateFrom, dateTo);
            return new ProductsReportListResponse<ProductChartDto> { Items = items };
        }

        public async Task<EmployeesReportListResponse<EmployeeSummaryReportDto>> GetEmployeesSummaryAsync(DateTime? dateFrom, DateTime? dateTo)
        {
            var items = await _repository.GetEmployeesSummaryAsync(dateFrom, dateTo);
            return new EmployeesReportListResponse<EmployeeSummaryReportDto> { Items = items };
        }

        public async Task<EmployeesReportListResponse<EmployeeActivityReportDto>> GetEmployeesActivityAsync(DateTime? dateFrom, DateTime? dateTo, long? employeeId)
        {
            var items = await _repository.GetEmployeesActivityAsync(dateFrom, dateTo, employeeId);
            return new EmployeesReportListResponse<EmployeeActivityReportDto> { Items = items };
        }

        public async Task<EmployeesReportListResponse<EmployeeChartDto>> GetEmployeesChartsAsync(DateTime? dateFrom, DateTime? dateTo)
        {
            var items = await _repository.GetEmployeesChartsAsync(dateFrom, dateTo);
            return new EmployeesReportListResponse<EmployeeChartDto> { Items = items };
        }

        public async Task<InventoryReportListResponse<InventorySummaryReportDto>> GetInventorySummaryAsync(long? categoryId)
        {
            var threshold = await _appSettingsRepository.GetRequiredDecimalAsync("stock_alert_threshold");
            var items = await _repository.GetInventorySummaryAsync(categoryId, threshold);
            return new InventoryReportListResponse<InventorySummaryReportDto> { Items = items };
        }

        public async Task<InventoryReportListResponse<InventoryBatchReportDto>> GetInventoryBatchesAsync(DateTime? dateFrom, DateTime? dateTo)
        {
            var items = await _repository.GetInventoryBatchesAsync(dateFrom, dateTo);
            return new InventoryReportListResponse<InventoryBatchReportDto> { Items = items };
        }

        public async Task<InventoryReportListResponse<InventoryChartDto>> GetInventoryChartsAsync()
        {
            var items = await _repository.GetInventoryChartsAsync();
            return new InventoryReportListResponse<InventoryChartDto> { Items = items };
        }

        public async Task<ExpiryReportListResponse<ExpirySummaryReportDto>> GetExpirySummaryAsync()
        {
            var expiryAlertDays = await _appSettingsRepository.GetRequiredDecimalAsync("expiry_alert_days");
            var items = await _repository.GetExpirySummaryAsync(expiryAlertDays);
            return new ExpiryReportListResponse<ExpirySummaryReportDto> { Items = items };
        }

        public async Task<ExpiryReportListResponse<ExpiryBatchReportDto>> GetExpiryBatchesAsync()
        {
            var expiryAlertDays = await _appSettingsRepository.GetRequiredDecimalAsync("expiry_alert_days");
            var items = await _repository.GetExpiryBatchesAsync(expiryAlertDays);
            return new ExpiryReportListResponse<ExpiryBatchReportDto> { Items = items };
        }

        public async Task<ExpiryReportListResponse<ExpiryChartDto>> GetExpiryChartsAsync()
        {
            var expiryAlertDays = await _appSettingsRepository.GetRequiredDecimalAsync("expiry_alert_days");
            var items = await _repository.GetExpiryChartsAsync(expiryAlertDays);
            return new ExpiryReportListResponse<ExpiryChartDto> { Items = items };
        }
    }
}
