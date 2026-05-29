using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Supermarket.Application.Common.Exports;
using Supermarket.Application.Exports.Interfaces;
using Supermarket.Application.InventoryQueries.Interfaces;
using Supermarket.Application.InvoicesQuery.Interfaces;
using Supermarket.Application.Offers.Interfaces;
using Supermarket.Application.Products.Interfaces;
using Supermarket.Application.Reports.Interfaces;
using Supermarket.Contracts.Exports;

namespace Supermarket.Application.Exports.Services
{
    public class ExportsService : IExportsService
    {
        private readonly IInvoicesQueryService _invoicesQueryService;
        private readonly IInventoryQueryService _inventoryQueryService;
        private readonly IProductService _productService;
        private readonly IOfferService _offerService;
        private readonly IReportsService _reportsService;
        private readonly IExportFormatBuilder _exportBuilder;
        private readonly IPrintHtmlBuilder _printBuilder;

        public ExportsService(
            IInvoicesQueryService invoicesQueryService,
            IInventoryQueryService inventoryQueryService,
            IProductService productService,
            IOfferService offerService,
            IReportsService reportsService,
            IExportFormatBuilder exportBuilder,
            IPrintHtmlBuilder printBuilder)
        {
            _invoicesQueryService = invoicesQueryService;
            _inventoryQueryService = inventoryQueryService;
            _productService = productService;
            _offerService = offerService;
            _reportsService = reportsService;
            _exportBuilder = exportBuilder;
            _printBuilder = printBuilder;
        }

        private string BuildFileName(string? requestedName, string resourceType, string format)
        {
            string ext = Supermarket.Application.Common.Security.SafeFileNamePolicy.ValidateAndGetFormatExtension(format);
            string fallback = $"{resourceType}_{DateTime.UtcNow:yyyy-MM-dd_HH-mm}{ext}";

            string safeName = Supermarket.Application.Common.Security.SafeFileNamePolicy.GetSafeFileName(requestedName, fallback);

            if (!safeName.EndsWith(ext, StringComparison.OrdinalIgnoreCase))
            {
                // To avoid exceeding max length by appending extension blindly,
                // GetSafeFileName already takes care of length, but if requestedName didn't have extension:
                if (safeName.Length + ext.Length > Supermarket.Application.Common.Security.SafeFileNamePolicy.MaxFileNameLength)
                {
                    safeName = safeName.Substring(0, Supermarket.Application.Common.Security.SafeFileNamePolicy.MaxFileNameLength - ext.Length);
                }
                safeName += ext;
            }
            return safeName;
        }

        private string GetContentType(string format)
        {
            return format.ToLower() == "excel" 
                ? "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet" 
                : "application/pdf";
        }

        public async Task<ExportResult> ExportProductsAsync(ExportProductsRequest request)
        {
            var data = await _productService.GetAllAsync();
            var bytes = _exportBuilder.BuildExportFile(data.Items, request.Columns, request.Format);
            return new ExportResult
            {
                Data = bytes,
                ContentType = GetContentType(request.Format),
                FileName = BuildFileName(request.FileName, "products", request.Format)
            };
        }

        public async Task<ExportResult> ExportInvoicesAsync(ExportInvoicesRequest request)
        {
            var data = await _invoicesQueryService.GetInvoicesAsync(
                status: request.Status,
                customerName: request.CustomerName,
                dateFrom: request.DateFrom,
                dateTo: request.DateTo,
                page: 1,
                pageSize: int.MaxValue);
            var bytes = _exportBuilder.BuildExportFile(data.Items, request.Columns, request.Format);
            return new ExportResult
            {
                Data = bytes,
                ContentType = GetContentType(request.Format),
                FileName = BuildFileName(request.FileName, "invoices", request.Format)
            };
        }

        public async Task<ExportResult> ExportOffersAsync(ExportOffersRequest request)
        {
            var data = await _offerService.GetAllAsync();
            var bytes = _exportBuilder.BuildExportFile(data.Items, request.Columns, request.Format);
            return new ExportResult
            {
                Data = bytes,
                ContentType = GetContentType(request.Format),
                FileName = BuildFileName(request.FileName, "offers", request.Format)
            };
        }

        public async Task<ExportResult> ExportInventoryAsync(ExportInventoryRequest request)
        {
            var data = await _inventoryQueryService.GetInventoryListAsync(
                request.Search, request.CategoryId, request.IsActive, request.HasStock, request.HasExpiry, 1, int.MaxValue);
            var bytes = _exportBuilder.BuildExportFile(data.Items, request.Columns, request.Format);
            return new ExportResult
            {
                Data = bytes,
                ContentType = GetContentType(request.Format),
                FileName = BuildFileName(request.FileName, "inventory", request.Format)
            };
        }

        public async Task<ExportResult> ExportReportAsync(string reportKey, ExportReportRequest request)
        {
            byte[] bytes;
            switch (reportKey.ToLower())
            {
                case "sales-invoices":
                    var siData = await _reportsService.GetSalesInvoicesAsync(request.DateFrom, request.DateTo, request.Status);
                    bytes = _exportBuilder.BuildExportFile(siData.Items, request.Columns, request.Format);
                    break;
                case "sales-items":
                    var sitData = await _reportsService.GetSalesItemsAsync(request.DateFrom, request.DateTo);
                    bytes = _exportBuilder.BuildExportFile(sitData.Items, request.Columns, request.Format);
                    break;
                case "products-summary":
                    var psData = await _reportsService.GetProductsSummaryAsync(request.CategoryId);
                    bytes = _exportBuilder.BuildExportFile(psData.Items, request.Columns, request.Format);
                    break;
                case "products-movements":
                    var pmData = await _reportsService.GetProductsMovementsAsync(request.DateFrom, request.DateTo, request.ProductId);
                    bytes = _exportBuilder.BuildExportFile(pmData.Items, request.Columns, request.Format);
                    break;
                case "employees-summary":
                    var esData = await _reportsService.GetEmployeesSummaryAsync(request.DateFrom, request.DateTo);
                    bytes = _exportBuilder.BuildExportFile(esData.Items, request.Columns, request.Format);
                    break;
                case "employees-activity":
                    var eaData = await _reportsService.GetEmployeesActivityAsync(request.DateFrom, request.DateTo, request.EmployeeId);
                    bytes = _exportBuilder.BuildExportFile(eaData.Items, request.Columns, request.Format);
                    break;
                case "inventory-summary":
                    var insData = await _reportsService.GetInventorySummaryAsync(request.CategoryId);
                    bytes = _exportBuilder.BuildExportFile(insData.Items, request.Columns, request.Format);
                    break;
                case "inventory-batches":
                    var inbData = await _reportsService.GetInventoryBatchesAsync(request.DateFrom, request.DateTo);
                    bytes = _exportBuilder.BuildExportFile(inbData.Items, request.Columns, request.Format);
                    break;
                case "expiry-summary":
                    var exsData = await _reportsService.GetExpirySummaryAsync();
                    bytes = _exportBuilder.BuildExportFile(exsData.Items, request.Columns, request.Format);
                    break;
                case "expiry-batches":
                    var exbData = await _reportsService.GetExpiryBatchesAsync();
                    bytes = _exportBuilder.BuildExportFile(exbData.Items, request.Columns, request.Format);
                    break;
                default:
                    throw new ArgumentException("INVALID_REPORT_KEY");
            }

            return new ExportResult
            {
                Data = bytes,
                ContentType = GetContentType(request.Format),
                FileName = BuildFileName(request.FileName, $"reports_{reportKey}", request.Format)
            };
        }

        public async Task<string> PrintReportAsync(string reportKey, PrintReportRequest request)
        {
            string html;
            string title = string.IsNullOrWhiteSpace(request.FileName) ? $"Report: {reportKey}" : request.FileName.Trim();

            switch (reportKey.ToLower())
            {
                case "sales-invoices":
                    var siData = await _reportsService.GetSalesInvoicesAsync(request.DateFrom, request.DateTo, request.Status);
                    html = _printBuilder.BuildPrintHtml(siData.Items, title);
                    break;
                case "sales-items":
                    var sitData = await _reportsService.GetSalesItemsAsync(request.DateFrom, request.DateTo);
                    html = _printBuilder.BuildPrintHtml(sitData.Items, title);
                    break;
                case "products-summary":
                    var psData = await _reportsService.GetProductsSummaryAsync(request.CategoryId);
                    html = _printBuilder.BuildPrintHtml(psData.Items, title);
                    break;
                case "products-movements":
                    var pmData = await _reportsService.GetProductsMovementsAsync(request.DateFrom, request.DateTo, request.ProductId);
                    html = _printBuilder.BuildPrintHtml(pmData.Items, title);
                    break;
                case "employees-summary":
                    var esData = await _reportsService.GetEmployeesSummaryAsync(request.DateFrom, request.DateTo);
                    html = _printBuilder.BuildPrintHtml(esData.Items, title);
                    break;
                case "employees-activity":
                    var eaData = await _reportsService.GetEmployeesActivityAsync(request.DateFrom, request.DateTo, request.EmployeeId);
                    html = _printBuilder.BuildPrintHtml(eaData.Items, title);
                    break;
                case "inventory-summary":
                    var insData = await _reportsService.GetInventorySummaryAsync(request.CategoryId);
                    html = _printBuilder.BuildPrintHtml(insData.Items, title);
                    break;
                case "inventory-batches":
                    var inbData = await _reportsService.GetInventoryBatchesAsync(request.DateFrom, request.DateTo);
                    html = _printBuilder.BuildPrintHtml(inbData.Items, title);
                    break;
                case "expiry-summary":
                    var exsData = await _reportsService.GetExpirySummaryAsync();
                    html = _printBuilder.BuildPrintHtml(exsData.Items, title);
                    break;
                case "expiry-batches":
                    var exbData = await _reportsService.GetExpiryBatchesAsync();
                    html = _printBuilder.BuildPrintHtml(exbData.Items, title);
                    break;
                default:
                    throw new ArgumentException("INVALID_REPORT_KEY");
            }

            return html;
        }
    }
}
